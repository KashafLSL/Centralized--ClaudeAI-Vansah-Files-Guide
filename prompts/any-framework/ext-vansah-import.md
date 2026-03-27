Run the Vansah BDD import for a feature file using vansah-import.exe. Works with any framework — reads config from vansah-config.json.

Usage: /non-dotnet:ext-vansah-import
No arguments needed — reads FeatureFilePath + FeatureFileName from vansah-config.json automatically.

---

## Step 1 — Show current config

Ask the user for the location of vansah-config.json if not already known.
Read and display vansah-config.json so the user can confirm:
- FeatureFilePath + FeatureFileName (the file that will be imported)
- FolderIdentifier (the target Vansah folder)
- ProjectKey

If FeatureFileName is empty or missing, ask the user:
  "Which feature file do you want to import? Please provide the path."
Store the answer as FEATURE_FILE_PATH.

---

## Step 2 — Run the import

Run vansah-import.exe using one of these two modes:

If FeatureFilePath + FeatureFileName are set in config (config-driven mode):
```bash
vansah-import
```

If the user provided a path manually (CLI argument mode):
```bash
vansah-import [FEATURE_FILE_PATH]
```

If vansah-import.exe is not on PATH, use the full path:
```bash
/path/to/vansah-import [FEATURE_FILE_PATH]
```

---

## Step 3 — Report results

Parse the output and report:
- How many scenarios were imported successfully
- The test case IDs that were created in Vansah
- Any failures with their error details
