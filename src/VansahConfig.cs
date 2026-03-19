namespace Vansah.Tools
{
    /// <summary>
    /// Maps to VansahConfig.json — fill in your values before running the importer.
    /// </summary>
    public class VansahConfig
    {
        /// <summary>
        /// Base API URL from Jira → Apps → Vansah → Settings → Vansah API Tokens.
        /// Example: "https://prodde.vansah.com"
        /// </summary>
        public string VansahApiUrl { get; set; }

        /// <summary>
        /// Vansah Connect Token generated from the API Tokens screen in Jira.
        /// </summary>
        public string VansahToken { get; set; }

        /// <summary>
        /// Jira project key (e.g. "VFL").
        /// </summary>
        public string ProjectKey { get; set; }

        /// <summary>
        /// Identifier of the Vansah test folder where cases will be created.
        /// Find it in the folder URL inside Vansah (the GUID-style string).
        /// Claude asks for this interactively — never hardcoded.
        /// </summary>
        public string FolderIdentifier { get; set; }

        /// <summary>
        /// Relative path (from project root) to the folder containing the feature file.
        /// Examples:
        ///   "FeatureFiles/AITestingBDD"
        ///   "FeatureFiles/AITestingBDD/UsingVansah"
        /// Claude updates this automatically when generating files.
        /// </summary>
        public string FeatureFilePath { get; set; }

        /// <summary>
        /// Name of the .feature file to import (filename only, e.g. "BNYComments.feature").
        /// Claude updates this automatically each time it generates a new feature file.
        /// </summary>
        public string FeatureFileName { get; set; }

        /// <summary>
        /// UUID of the Vansah test case type (e.g. Functional, Regression).
        /// Find it in Jira → Vansah → Settings → Test Case Types → copy the identifier.
        /// Leave empty to omit type from the request.
        /// </summary>
        public string TypeIdentifier { get; set; }
    }
}