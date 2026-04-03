using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace vFluxAutomation.Tools
{
    /// <summary>
    /// Parses a Gherkin .feature file and imports each Scenario into Vansah via REST API.
    /// Step 1 — POST /api/v1/testCase                    : create the test case
    /// Step 2 — PUT  /api/v1/testCase/{id}               : set script type to BDD
    /// Step 3 — POST /api/v1/testCase/{id}/testScript    : create the BDD testScript record
    /// Step 4 — PUT  /api/v1/testCase/testScript/{id}    : write Given/When/Then steps
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
            var scenarios    = new List<ScenarioData>();
            var lines        = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            ScenarioData current    = null;
            bool         inExamples = false;
            var          pendingTags = new List<string>();

            foreach (var raw in lines)
            {
                var line = raw.Trim();

                if (string.IsNullOrWhiteSpace(line)) continue;

                // Capture tags — accumulate until the next Scenario line
                if (line.StartsWith("@"))
                {
                    foreach (var token in line.Split(' '))
                        if (token.StartsWith("@")) pendingTags.Add(token);
                    continue;
                }

                if (line.StartsWith("#"))
                {
                    if (current != null && line.StartsWith("# Precondition:"))
                        current.Precondition = line.Replace("# Precondition:", "").Trim();
                    continue;
                }

                if (line.StartsWith("Feature:") || line.StartsWith("Background:"))
                {
                    pendingTags.Clear();
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
                        Title = line.Replace("Scenario Outline:", "").Replace("Scenario:", "").Trim(),
                        Tags  = new List<string>(pendingTags)
                    };
                    pendingTags.Clear();
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

        // ── Label resolver ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads @Automatable / @NonAutomatable from scenario tags and returns the
        /// Vansah label name, or null if neither tag is present.
        /// </summary>
        private static string ResolveLabel(ScenarioData scenario)
        {
            if (scenario.Tags.Contains("@Automatable"))    return "Automatable";
            if (scenario.Tags.Contains("@NonAutomatable")) return "Non-Automatable";
            return null;
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

            // Map @Automatable / @NonAutomatable tag → Vansah label field
            var label = ResolveLabel(scenario);
            if (label != null)
                body["label"] = new JArray { label };

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

                    var labelTag = ResolveLabel(scenario) ?? "—";
                    Console.WriteLine($"  [OK]   {scenario.Title}  →  {key}  [{labelTag}]");
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

        // ── Step 2: Attach BDD steps ──────────────────────────────────────────────

        private async Task SetBddStepsAsync(string caseKey, string testCaseIdentifier, ScenarioData scenario)
        {
            // Step 2a: PUT to set the test case script type to BDD and apply the label
            var setTypeBody = new JObject { ["scriptType"] = "bdd", ["caseVersion"] = 1 };
            var caseLabel   = ResolveLabel(scenario);
            if (caseLabel != null)
                setTypeBody["label"] = new JArray { caseLabel };
            var setTypeContent = new StringContent(setTypeBody.ToString(Formatting.None), Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"{_config.VansahApiUrl.TrimEnd('/')}/api/v1/testCase/{testCaseIdentifier}", setTypeContent);

            // Step 2b: POST to create the BDD testScript record
            // URL: /api/v1/testCase/{testCaseIdentifier}/testScript
            var createBody    = new JObject
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

            // Step 2c: PUT BDD steps to the testScript
            var body = new JObject
            {
                ["scriptType"]      = "bdd",
                ["project"]         = new JObject { ["key"] = _config.ProjectKey },
                ["bddData"]         = BuildStepsText(scenario),
                ["testCaseVersion"] = 1
            };
            var httpContent = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
            var url         = $"{_config.VansahApiUrl.TrimEnd('/')}/api/v1/testCase/testScript/{scriptId}";

            var response = await _httpClient.PutAsync(url, httpContent);
            var raw      = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                Console.WriteLine($"  [BDD]  {caseKey} → steps set OK (scriptId: {scriptId})");
            else
                Console.WriteLine($"  [WARN] BDD steps not set for {caseKey}: HTTP {(int)response.StatusCode}: {raw}");
        }

        // ── Builds the steps-only Gherkin text (no Scenario: header) ─────────────

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
            public List<string> Tags         { get; set; } = new List<string>();
        }
    }

    public class ImportResult
    {
        public string       FeatureFile { get; set; }
        public List<string> Succeeded   { get; set; } = new List<string>();
        public List<string> Failed      { get; set; } = new List<string>();
    }
}
