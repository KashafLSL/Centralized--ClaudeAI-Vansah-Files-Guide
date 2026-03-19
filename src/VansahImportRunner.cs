using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Vansah.Tools
{
    /// <summary>
    /// NUnit test fixture that drives the Vansah BDD import.
    ///
    /// WORKFLOW:
    ///   1. Run /generate-bdd in Claude Code — it creates the .feature file
    ///      and updates VansahConfig.json (FeatureFileName, FeatureFilePath, FolderIdentifier).
    ///   2. Run the import:
    ///        dotnet test --filter "Category=VansahImport"
    ///      (or run ImportScenarios from Visual Studio Test Explorer)
    ///
    /// NO manual path changes are needed in this file.
    /// All configuration is driven by VansahConfig.json.
    /// </summary>
    [TestFixture]
    [Category("VansahImport")]
    public class VansahImportRunner
    {
        private VansahConfig _config;

        // Root of the project — used to resolve relative FeatureFilePath values.
        private static readonly string ProjectRoot =
            Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\<YourProjectFolder>"));   // ← update this to your project folder name

        [OneTimeSetUp]
        public void LoadConfig()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VansahConfig.json");

            Assert.IsTrue(File.Exists(configPath),
                $"VansahConfig.json not found at: {configPath}");

            _config = JsonConvert.DeserializeObject<VansahConfig>(File.ReadAllText(configPath));

            Assert.IsFalse(string.IsNullOrWhiteSpace(_config.VansahApiUrl),
                "VansahApiUrl is not set in VansahConfig.json");
            Assert.IsFalse(string.IsNullOrWhiteSpace(_config.VansahToken),
                "VansahToken is not set in VansahConfig.json");
            Assert.IsFalse(string.IsNullOrWhiteSpace(_config.ProjectKey),
                "ProjectKey is not set in VansahConfig.json");
            Assert.IsFalse(string.IsNullOrWhiteSpace(_config.FolderIdentifier),
                "FolderIdentifier is not set in VansahConfig.json.\n" +
                "Claude will ask for this when you run /generate-bdd.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(_config.FeatureFilePath),
                "FeatureFilePath is not set in VansahConfig.json.\n" +
                "Example: \"FeatureFilePath\": \"FeatureFiles/AITestingBDD\"");
            Assert.IsFalse(string.IsNullOrWhiteSpace(_config.FeatureFileName),
                "FeatureFileName is not set in VansahConfig.json.\n" +
                "Run /generate-bdd in Claude Code — it sets this automatically.\n" +
                "Or set manually, e.g.: \"FeatureFileName\": \"MyFeature.feature\"");
        }

        [Test]
        [Description("Imports the feature file named in VansahConfig.json into Vansah as BDD test cases.")]
        public async Task ImportScenarios()
        {
            var resolvedDir = Path.IsPathRooted(_config.FeatureFilePath)
                ? _config.FeatureFilePath
                : Path.GetFullPath(Path.Combine(ProjectRoot, _config.FeatureFilePath));

            var featurePath = Path.Combine(resolvedDir, _config.FeatureFileName);

            Assert.IsTrue(File.Exists(featurePath),
                $"Feature file not found: {featurePath}\n" +
                $"Make sure '{_config.FeatureFileName}' exists under '{_config.FeatureFilePath}'.");

            Console.WriteLine($"[Vansah] Importing : {_config.FeatureFileName}");
            Console.WriteLine($"[Vansah] Full path : {featurePath}");

            var importer = new VansahImporter(_config);
            var result   = await importer.ImportFeatureFileAsync(featurePath);

            Console.WriteLine($"\nImport Summary:");
            Console.WriteLine($"  Succeeded : {result.Succeeded.Count}");
            Console.WriteLine($"  Failed    : {result.Failed.Count}");

            if (result.Failed.Count > 0)
            {
                Console.WriteLine("\nFailed scenarios:");
                foreach (var title in result.Failed)
                    Console.WriteLine($"  - {title}");
            }

            Assert.AreEqual(0, result.Failed.Count,
                $"{result.Failed.Count} scenario(s) failed to import. See console output for details.");
        }
    }
}