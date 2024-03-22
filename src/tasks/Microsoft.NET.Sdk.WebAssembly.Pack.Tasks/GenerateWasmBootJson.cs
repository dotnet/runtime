// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ResourceHashesByNameDictionary = System.Collections.Generic.Dictionary<string, string>;

namespace Microsoft.NET.Sdk.WebAssembly;

public class GenerateWasmBootJson : Task
{
    private static readonly string[] jiterpreterOptions = new[] { "jiterpreter-traces-enabled", "jiterpreter-interp-entry-enabled", "jiterpreter-jit-call-enabled" };

    [Required]
    public string AssemblyPath { get; set; }

    [Required]
    public ITaskItem[] Resources { get; set; }

    [Required]
    public bool DebugBuild { get; set; }

    public string DebugLevel { get; set; }

    [Required]
    public bool LinkerEnabled { get; set; }

    [Required]
    public bool CacheBootResources { get; set; }

    public bool LoadFullICUData { get; set; }

    public bool IsHybridGlobalization { get; set; }

    public bool LoadCustomIcuData { get; set; }

    public string InvariantGlobalization { get; set; }

    public ITaskItem[] ConfigurationFiles { get; set; }

    public ITaskItem[] Extensions { get; set; }

    public string StartupMemoryCache { get; set; }

    public string Jiterpreter { get; set; }

    public string RuntimeOptions { get; set; }

    [Required]
    public string TargetFrameworkVersion { get; set; }

    public ITaskItem[] ModuleAfterConfigLoaded { get; set; }

    public ITaskItem[] ModuleAfterRuntimeReady { get; set; }

    [Required]
    public string OutputPath { get; set; }

    public ITaskItem[] LazyLoadedAssemblies { get; set; }

    public bool IsPublish { get; set; }

    public override bool Execute()
    {
        using var fileStream = File.Create(OutputPath);
        var entryAssemblyName = AssemblyName.GetAssemblyName(AssemblyPath).Name;

        try
        {
            WriteBootJson(fileStream, entryAssemblyName);
        }
        catch (Exception ex)
        {
            Log.LogError(ex.ToString());
        }

        return !Log.HasLoggedErrors;
    }

    // Internal for tests
    public void WriteBootJson(Stream output, string entryAssemblyName)
    {
        var helper = new BootJsonBuilderHelper(Log);

        var result = new BootJsonData
        {
            resources = new ResourcesData(),
            startupMemoryCache = ParseOptionalBool(StartupMemoryCache),
        };

        if (IsTargeting80OrLater())
        {
            result.mainAssemblyName = entryAssemblyName;
            result.globalizationMode = GetGlobalizationMode().ToString().ToLowerInvariant();

            if (CacheBootResources)
                result.cacheBootResources = CacheBootResources;

            if (LinkerEnabled)
                result.linkerEnabled = LinkerEnabled;
        }
        else
        {
            result.cacheBootResources = CacheBootResources;
            result.linkerEnabled = LinkerEnabled;
            result.config = new();
            result.debugBuild = DebugBuild;
            result.entryAssembly = entryAssemblyName;
            result.icuDataMode = GetGlobalizationMode();
        }

        if (!string.IsNullOrEmpty(RuntimeOptions))
        {
            string[] runtimeOptions = RuntimeOptions.Split(' ');
            result.runtimeOptions = runtimeOptions;
        }

        bool? jiterpreter = ParseOptionalBool(Jiterpreter);
        if (jiterpreter != null)
        {
            var runtimeOptions = result.runtimeOptions?.ToHashSet() ?? new HashSet<string>(3);
            foreach (var jiterpreterOption in jiterpreterOptions)
            {
                if (jiterpreter == true)
                {
                    if (!runtimeOptions.Contains($"--no-{jiterpreterOption}"))
                        runtimeOptions.Add($"--{jiterpreterOption}");
                }
                else
                {
                    if (!runtimeOptions.Contains($"--{jiterpreterOption}"))
                        runtimeOptions.Add($"--no-{jiterpreterOption}");
                }
            }

            result.runtimeOptions = runtimeOptions.ToArray();
        }

        string[] moduleAfterConfigLoadedFullPaths = ModuleAfterConfigLoaded?.Select(s => s.GetMetadata("FullPath")).ToArray() ?? Array.Empty<string>();
        string[] moduleAfterRuntimeReadyFullPaths = ModuleAfterRuntimeReady?.Select(s => s.GetMetadata("FullPath")).ToArray() ?? Array.Empty<string>();

        // Build a two-level dictionary of the form:
        // - assembly:
        //   - UriPath (e.g., "System.Text.Json.dll")
        //     - ContentHash (e.g., "4548fa2e9cf52986")
        // - runtime:
        //   - UriPath (e.g., "dotnet.js")
        //     - ContentHash (e.g., "3448f339acf512448")
        if (Resources != null)
        {
            var remainingLazyLoadAssemblies = new List<ITaskItem>(LazyLoadedAssemblies ?? Array.Empty<ITaskItem>());
            var resourceData = result.resources;
            foreach (var resource in Resources)
            {
                ResourceHashesByNameDictionary resourceList = null;

                string behavior = null;
                var fileName = resource.GetMetadata("FileName");
                var fileExtension = resource.GetMetadata("Extension");
                var assetTraitName = resource.GetMetadata("AssetTraitName");
                var assetTraitValue = resource.GetMetadata("AssetTraitValue");
                var resourceName = Path.GetFileName(resource.GetMetadata("RelativePath"));

                if (TryGetLazyLoadedAssembly(resourceName, out var lazyLoad))
                {
                    Log.LogMessage(MessageImportance.Low, "Candidate '{0}' is defined as a lazy loaded assembly.", resource.ItemSpec);
                    remainingLazyLoadAssemblies.Remove(lazyLoad);
                    resourceData.lazyAssembly ??= new ResourceHashesByNameDictionary();
                    resourceList = resourceData.lazyAssembly;
                }
                else if (string.Equals("Culture", assetTraitName, StringComparison.OrdinalIgnoreCase))
                {
                    Log.LogMessage(MessageImportance.Low, "Candidate '{0}' is defined as satellite assembly with culture '{1}'.", resource.ItemSpec, assetTraitValue);
                    resourceData.satelliteResources ??= new Dictionary<string, ResourceHashesByNameDictionary>(StringComparer.OrdinalIgnoreCase);

                    if (!IsTargeting80OrLater())
                        resourceName = assetTraitValue + "/" + resourceName;

                    if (!resourceData.satelliteResources.TryGetValue(assetTraitValue, out resourceList))
                    {
                        resourceList = new ResourceHashesByNameDictionary();
                        resourceData.satelliteResources.Add(assetTraitValue, resourceList);
                    }
                }
                else if (string.Equals("symbol", assetTraitValue, StringComparison.OrdinalIgnoreCase))
                {
                    if (TryGetLazyLoadedAssembly($"{fileName}.dll", out _) || TryGetLazyLoadedAssembly($"{fileName}{Utils.WebcilInWasmExtension}", out _))
                    {
                        Log.LogMessage(MessageImportance.Low, "Candidate '{0}' is defined as a lazy loaded symbols file.", resource.ItemSpec);
                        resourceData.lazyAssembly ??= new ResourceHashesByNameDictionary();
                        resourceList = resourceData.lazyAssembly;
                    }
                    else
                    {
                        Log.LogMessage(MessageImportance.Low, "Candidate '{0}' is defined as symbols file.", resource.ItemSpec);
                        resourceData.pdb ??= new ResourceHashesByNameDictionary();
                        resourceList = resourceData.pdb;
                    }
                }
                else if (string.Equals("runtime", assetTraitValue, StringComparison.OrdinalIgnoreCase))
                {
                    Log.LogMessage(MessageImportance.Low, "Candidate '{0}' is defined as an app assembly.", resource.ItemSpec);
                    resourceList = resourceData.assembly;
                }
                else if (string.Equals(assetTraitName, "WasmResource", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(assetTraitValue, "native", StringComparison.OrdinalIgnoreCase))
                {
                    Log.LogMessage(MessageImportance.Low, "Candidate '{0}' is defined as a native application resource.", resource.ItemSpec);

                    if (IsTargeting80OrLater())
                    {
                        resourceList = helper.GetNativeResourceTargetInBootConfig(result, resourceName);
                    }
                    else
                    {
                        if (fileName.StartsWith("dotnet", StringComparison.OrdinalIgnoreCase) && string.Equals(fileExtension, ".wasm", StringComparison.OrdinalIgnoreCase))
                        {
                            behavior = "dotnetwasm";
                        }

                        resourceList = resourceData.runtime ??= new();
                    }
                }
                else if (string.Equals("JSModule", assetTraitName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(assetTraitValue, "JSLibraryModule", StringComparison.OrdinalIgnoreCase))
                {
                    Log.LogMessage(MessageImportance.Low, "Candidate '{0}' is defined as a library initializer resource.", resource.ItemSpec);

                    var targetPath = resource.GetMetadata("TargetPath");
                    Debug.Assert(!string.IsNullOrEmpty(targetPath), "Target path for '{0}' must exist.", resource.ItemSpec);

                    resourceList = resourceData.libraryInitializers ??= new ResourceHashesByNameDictionary();
                    AddResourceToList(resource, resourceList, targetPath);

                    if (IsTargeting80OrLater())
                    {
                        if (moduleAfterConfigLoadedFullPaths.Contains(resource.ItemSpec))
                        {
                            resourceList = resourceData.modulesAfterConfigLoaded ??= new();
                        }
                        else if (moduleAfterRuntimeReadyFullPaths.Contains(resource.ItemSpec))
                        {
                            resourceList = resourceData.modulesAfterRuntimeReady ??= new();
                        }
                        else if (File.Exists(resource.ItemSpec))
                        {
                            string fileContent = File.ReadAllText(resource.ItemSpec);
                            if (fileContent.Contains("onRuntimeConfigLoaded") || fileContent.Contains("beforeStart") || fileContent.Contains("afterStarted"))
                                resourceList = resourceData.modulesAfterConfigLoaded ??= new();
                            else
                                resourceList = resourceData.modulesAfterRuntimeReady ??= new();
                        }
                        else
                        {
                            resourceList = resourceData.modulesAfterConfigLoaded ??= new();
                        }

                        string newTargetPath = "../" + targetPath; // This needs condition once WasmRuntimeAssetsLocation is supported in Wasm SDK
                        AddResourceToList(resource, resourceList, newTargetPath);
                    }

                    continue;
                }
                else if (string.Equals("WasmResource", assetTraitName, StringComparison.OrdinalIgnoreCase) &&
                            assetTraitValue.StartsWith("extension:", StringComparison.OrdinalIgnoreCase))
                {
                    Log.LogMessage(MessageImportance.Low, "Candidate '{0}' is defined as an extension resource '{1}'.", resource.ItemSpec, assetTraitValue);
                    var extensionName = assetTraitValue.Substring("extension:".Length);
                    resourceData.extensions ??= new();
                    if (!resourceData.extensions.TryGetValue(extensionName, out resourceList))
                    {
                        resourceList = new();
                        resourceData.extensions[extensionName] = resourceList;
                    }
                    var targetPath = resource.GetMetadata("TargetPath");
                    Debug.Assert(!string.IsNullOrEmpty(targetPath), "Target path for '{0}' must exist.", resource.ItemSpec);
                    AddResourceToList(resource, resourceList, targetPath);
                    continue;
                }
                else
                {
                    Log.LogMessage(MessageImportance.Low, "Skipping resource '{0}' since it doesn't belong to a defined category.", resource.ItemSpec);
                    // This should include items such as XML doc files, which do not need to be recorded in the manifest.
                    continue;
                }

                if (resourceList != null)
                {
                    AddResourceToList(resource, resourceList, resourceName);
                }

                if (!string.IsNullOrEmpty(behavior))
                {
                    resourceData.runtimeAssets ??= new Dictionary<string, AdditionalAsset>();
                    AddToAdditionalResources(resource, resourceData.runtimeAssets, resourceName, behavior);
                }
            }

            if (remainingLazyLoadAssemblies.Count > 0)
            {
                const string message = "Unable to find '{0}' to be lazy loaded later. Confirm that project or " +
                    "package references are included and the reference is used in the project.";

                Log.LogError(
                    subcategory: null,
                    errorCode: "BLAZORSDK1001",
                    helpKeyword: null,
                    file: null,
                    lineNumber: 0,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: message,
                    string.Join(";", LazyLoadedAssemblies.Select(a => a.ItemSpec)));

                return;
            }
        }

        if (IsTargeting80OrLater())
        {
            int? debugLevel = ParseOptionalInt(DebugLevel);

            // If user didn't give us a value, check if we have any PDB.
            if (debugLevel == null && result.resources?.pdb?.Count > 0)
                debugLevel = -1;

            // Fallback to -1 for build, or 0 for publish
            debugLevel ??= IsPublish ? 0 : -1;

            result.debugLevel = debugLevel.Value;
        }

        if (ConfigurationFiles != null)
        {
            foreach (var configFile in ConfigurationFiles)
            {
                string configUrl = Path.GetFileName(configFile.ItemSpec);
                if (IsTargeting80OrLater())
                {
                    result.appsettings ??= new();

                    configUrl = "../" + configUrl; // This needs condition once WasmRuntimeAssetsLocation is supported in Wasm SDK
                    result.appsettings.Add(configUrl);
                }
                else
                {
                    result.config.Add(configUrl);
                }
            }
        }

        var jsonOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        if (Extensions != null && Extensions.Length > 0)
        {
            result.extensions = new Dictionary<string, Dictionary<string, object>>();
            foreach (var configExtension in Extensions)
            {
                var key = configExtension.GetMetadata("key");
                using var fs = File.OpenRead(configExtension.ItemSpec);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(fs, jsonOptions);
                result.extensions[key] = config;
            }
        }

        helper.ComputeResourcesHash(result);
        JsonSerializer.Serialize(output, result, jsonOptions);

        void AddResourceToList(ITaskItem resource, ResourceHashesByNameDictionary resourceList, string resourceKey)
        {
            if (!resourceList.ContainsKey(resourceKey))
            {
                Log.LogMessage(MessageImportance.Low, "Added resource '{0}' to the manifest.", resource.ItemSpec);
                resourceList.Add(resourceKey, $"sha256-{resource.GetMetadata("FileHash")}");
            }
        }
    }

    private GlobalizationMode GetGlobalizationMode()
    {
        if (string.Equals(InvariantGlobalization, "true", StringComparison.OrdinalIgnoreCase))
            return GlobalizationMode.Invariant;
        else if (IsHybridGlobalization)
            return GlobalizationMode.Hybrid;
        else if (LoadFullICUData)
            return GlobalizationMode.All;
        else if (LoadCustomIcuData)
            return GlobalizationMode.Custom;

        return GlobalizationMode.Sharded;
    }

    private static bool? ParseOptionalBool(string value)
    {
        if (string.IsNullOrEmpty(value) || !bool.TryParse(value, out var boolValue))
            return null;

        return boolValue;
    }

    private static int? ParseOptionalInt(string value)
    {
        if (string.IsNullOrEmpty(value) || !int.TryParse(value, out var intValue))
            return null;

        return intValue;
    }

    private void AddToAdditionalResources(ITaskItem resource, Dictionary<string, AdditionalAsset> additionalResources, string resourceName, string behavior)
    {
        if (!additionalResources.ContainsKey(resourceName))
        {
            Log.LogMessage(MessageImportance.Low, "Added resource '{0}' to the list of additional assets in the manifest.", resource.ItemSpec);
            additionalResources.Add(resourceName, new AdditionalAsset
            {
                hash = $"sha256-{resource.GetMetadata("FileHash")}",
                behavior = behavior
            });
        }
    }

    private bool TryGetLazyLoadedAssembly(string fileName, out ITaskItem lazyLoadedAssembly)
    {
        return (lazyLoadedAssembly = LazyLoadedAssemblies?.SingleOrDefault(a => a.ItemSpec == fileName)) != null;
    }

    private Version? parsedTargetFrameworkVersion;
    private static readonly Version version80 = new Version(8, 0);

    private bool IsTargeting80OrLater()
    {
        if (parsedTargetFrameworkVersion == null)
        {
            string tfv = TargetFrameworkVersion;
            if (tfv.StartsWith("v"))
                tfv = tfv.Substring(1);

            parsedTargetFrameworkVersion = Version.Parse(tfv);
        }

        return parsedTargetFrameworkVersion >= version80;
    }
}
