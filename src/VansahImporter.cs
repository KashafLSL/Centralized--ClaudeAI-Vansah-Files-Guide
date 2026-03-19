using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Vansah.Tools
{
    /// <summary>
    /// Parses a Gherkin .feature file and imports each Scenario into Vansah via REST API.
    ///
    /// 4-step import flow (order is mandatory):
    ///   Step 1 — POST /api/v1/testCase                     : create the test case
    ///   Step 2 — PUT  /api/v1/testCase/{id}                : set script type to BDD
    ///   Step 3 — POST /api/v1/testCase/{id}/testScript     : create the BDD testScript record
    ///   Step 4 — PUT  /api/v1/testCase/testScript/{id}     : write Given/When/Then steps
    ///
    /// IMPORTANT: Step 2 (set BDD mode) MUST run before Step 3 (create testScript),
    /// otherwise steps are created in multi-step format instead of BDD format.
    /// </summary>
    public class VansahImporter
    {
        private readonly VansahConfig _config;
        private static readonly HttpClient _httpClient = new HttpClient();

        public VansahImporter(VansahConfig config)
        {
            _config = config;
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", _config.VansahToken);
        }

        // ── Public entry point ────────────────────────────────────────────────────

        public async Task<ImportResult> ImportFeatureFileAsync(string featureFilePath)
        {
            if (!File.Exists(featureFilePath))
                throw new FileNotFoundException($"Feature file not found: {featureFilePath}");

            var scenarios = ParseScenarios(File.ReadAllText(featureFilePath));
            var result    = new ImportResult { FeatureFile = featureFilePath };

            Console.WriteLine($"\n[Vansah Importer] Parsed {scenarios.Count} scenario(s) from: {Path.GetFileName(featureFilePath)}");
            Console.WriteLine($"[Vansah Importer] Target folder : {_config.FolderIdentifier}");
            Console.WriteLine($"[Vansah Importer] Project key   : {_config.ProjectKey}\n");

            foreach (var scenario in scenarios)
            {
                var success = await CreateTestCaseAsync(scenario);
                if (success) result.Succeeded.Add(scenario.Title);
                else         result.Failed.Add(scenario.Title);
            }

            Console.WriteLine($"\n[Vansah Importer] Done — {result.Succeeded.Count} created, {result.Failed.Count} failed.\n");
            return result;
        }

        // ── Gherkin parser ────────────────────────────────────────────────────────

        private List<ScenarioData> ParseScenarios(string content)
        {
            var scenarios  = new List<ScenarioData>();
            var lines      = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            ScenarioData current    = null;
            bool         inExamples = false;

            foreach (var raw in lines)
            {
                var line = raw.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("@")) continue;

                if (line.StartsWith("#"))
                {
                    if (current != null && line.StartsWith("# Precondition:"))
                        current.Precondition = line.Replace("# Precondition:", "").Trim();
                    continue;
                }

                if (line.StartsWith("Feature:") || line.StartsWith("Background:"))
                {
                    current = null;
                    continue;
                }

                if (line.StartsWith("Examples:")) { inExamples = true; continue; }

                if (line.StartsWith("Scenario Outline:") || line.StartsWith("Scenario:"))
                {
                    if (current != null) scenarios.Add(current);
                    inExamples = false;
                    current = new ScenarioData
                    {
                        Title = line.Replace("Scenario Outline:", "").Replace("Scenario:", "").Trim()
                    };
                    continue;
                }

                if (current == null) continue;

                if (inExamples)
                {
                    if (line.StartsWith("|")) current.ExampleRows.Add(line);
                    continue;
                }

                if (line.StartsWith("Given ") || line.StartsWith("When ")  ||
                    line.StartsWith("Then ")  || line.StartsWith("And ")   ||
                    line.StartsWith("But ")   || line.StartsWith("* "))
                {
                    current.Steps.Add(line);
                }
            }

            if (current != null) scenarios.Add(current);
            return scenarios;
        }

        // ── Step 1: Create test case ──────────────────────────────────────────────

        private async Task<bool> CreateTestCaseAsync(ScenarioData scenario)
        {
            var body = new JObject
            {
                ["headline"]     = scenario.Title,
                ["precondition"] = scenario.Precondition,
                ["project"]      = new JObject { ["key"] = _config.ProjectKey },
                ["folder"]       = new JArray { new JObject { ["identifier"] = _config.FolderIdentifier } }
            };

            if (!string.IsNullOrWhiteSpace(_config.TypeIdentifier))
                body["type"] = new JObject { ["identifier"] = _config.TypeIdentifier };

            var httpContent = new StringContent(body.ToString(Formatting.Indented), Encoding.UTF8, "application/json");

            try
            {
                var url      = $"{_config.VansahApiUrl.TrimEnd('/')}/api/v1/testCase";
                var response = await _httpClient.PostAsync(url, httpContent);
                var raw      = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var parsed     = JObject.Parse(raw);
                    var key        = parsed["data"]?["key"]?.ToString() ?? "N/A";
                    var identifier = parsed["data"]?["identifier"]?.ToString();

                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(identifier))
                        await SetBddStepsAsync(key, identifier, scenario);

                    Console.WriteLine($"  [OK]   {scenario.Title}  →  {key}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"  [FAIL] {scenario.Title}");
                    Console.WriteLine($"         HTTP {(int)response.StatusCode}: {raw}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] {scenario.Title} — {ex.Message}");
                return false;
            }
        }

        // ── Steps 2-4: Set BDD mode then attach steps ─────────────────────────────

        private async Task SetBddStepsAsync(string caseKey, string testCaseIdentifier, ScenarioData scenario)
        {
            // Step 2: PUT — set script type to BDD (must happen before creating testScript)
            var setTypeBody    = new JObject { ["scriptType"] = "bdd", ["caseVersion"] = 1 };
            var setTypeContent = new StringContent(setTypeBody.ToString(Formatting.None), Encoding.UTF8, "application/json");
            await _httpClient.PutAsync(
                $"{_config.VansahApiUrl.TrimEnd('/')}/api/v1/testCase/{testCaseIdentifier}",
                setTypeContent);

            // Step 3: POST — create the BDD testScript record
            var createBody = new JObject
            {
                ["scriptType"]      = "bdd",
                ["project"]         = new JObject { ["key"] = _config.ProjectKey },
                ["testCaseVersion"] = 1
            };
            var createContent = new StringContent(createBody.ToString(Formatting.None), Encoding.UTF8, "application/json");
            var createUrl     = $"{_config.VansahApiUrl.TrimEnd('/')}/api/v1/testCase/{testCaseIdentifier}/testScript";
            var createResp    = await _httpClient.PostAsync(createUrl, createContent);
            var createRaw     = await createResp.Content.ReadAsStringAsync();

            if (!createResp.IsSuccessStatusCode)
            {
                Console.WriteLine($"  [WARN] Could not create testScript for {caseKey}: HTTP {(int)createResp.StatusCode}: {createRaw}");
                return;
            }

            var scriptId = JObject.Parse(createRaw)?["data"]?["identifier"]?.ToString();
            if (string.IsNullOrEmpty(scriptId))
            {
                Console.WriteLine($"  [WARN] No testScript identifier returned for {caseKey}: {createRaw}");
                return;
            }

            // Step 4: PUT — write Given/When/Then steps to the testScript
            var stepsBody = new JObject
            {
                ["scriptType"]      = "bdd",
                ["project"]         = new JObject { ["key"] = _config.ProjectKey },
                ["bddData"]         = BuildStepsText(scenario),
                ["testCaseVersion"] = 1
            };
            var stepsContent = new StringContent(stepsBody.ToString(Formatting.None), Encoding.UTF8, "application/json");
            var stepsUrl     = $"{_config.VansahApiUrl.TrimEnd('/')}/api/v1/testCase/testScript/{scriptId}";
            var stepsResp    = await _httpClient.PutAsync(stepsUrl, stepsContent);
            var stepsRaw     = await stepsResp.Content.ReadAsStringAsync();

            if (stepsResp.IsSuccessStatusCode)
                Console.WriteLine($"  [BDD]  {caseKey} → steps set OK (scriptId: {scriptId})");
            else
                Console.WriteLine($"  [WARN] BDD steps not set for {caseKey}: HTTP {(int)stepsResp.StatusCode}: {stepsRaw}");
        }

        // ── Build steps-only Gherkin text (no Scenario: header) ──────────────────

        private string BuildStepsText(ScenarioData scenario)
        {
            var sb = new StringBuilder();

            foreach (var step in scenario.Steps)
                sb.AppendLine($"    {step}");

            if (scenario.ExampleRows.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("    Examples:");
                foreach (var row in scenario.ExampleRows)
                    sb.AppendLine($"      {row}");
            }

            return sb.ToString().TrimEnd();
        }

        // ── Inner types ───────────────────────────────────────────────────────────

        private class ScenarioData
        {
            public string       Title        { get; set; }
            public string       Precondition { get; set; } = "";
            public List<string> Steps        { get; set; } = new List<string>();
            public List<string> ExampleRows  { get; set; } = new List<string>();
        }
    }

    public class ImportResult
    {
        public string       FeatureFile { get; set; }
        public List<string> Succeeded   { get; set; } = new List<string>();
        public List<string> Failed      { get; set; } = new List<string>();
    }
}