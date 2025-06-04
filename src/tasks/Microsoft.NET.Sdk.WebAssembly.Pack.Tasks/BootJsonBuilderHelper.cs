// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.WebAssembly
{
    public class BootJsonBuilderHelper(TaskLoggingHelper Log, string DebugLevel, bool IsMultiThreaded, bool IsPublish)
    {
#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
        internal static readonly Regex mergeWithPlaceholderRegex = new Regex(@"/\*!\s*dotnetBootConfig\s*\*/\s*{}");
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

        private static readonly string[] coreAssemblyNames = [
            "System.Private.CoreLib",
            "System.Runtime.InteropServices.JavaScript",
        ];

        private static readonly string[] extraMultiThreadedCoreAssemblyName = [
            "System.Threading.Channels",
            "System.Threading.ThreadPool",
            "System.Threading",
            "System.Collections",
            "System.Collections.Concurrent",
        ];

        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        public bool IsCoreAssembly(string fileName)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (coreAssemblyNames.Contains(fileNameWithoutExtension))
                return true;

            if (IsMultiThreaded && extraMultiThreadedCoreAssemblyName.Contains(fileNameWithoutExtension))
                return true;

            return false;
        }

        public void WriteConfigToFile(BootJsonData config, string outputPath, string? outputFileExtension = null, string? mergeWith = null)
        {
            var output = JsonSerializer.Serialize(config, JsonOptions);

            outputFileExtension ??= Path.GetExtension(outputPath);
            Log.LogMessage($"Write config in format '{outputFileExtension}'");
            if (mergeWith != null)
            {
                string existingContent = File.ReadAllText(mergeWith);
                output = mergeWithPlaceholderRegex.Replace(existingContent, e => $"/*json-start*/{output}/*json-end*/");
                if (existingContent.Equals(output))
                    Log.LogError($"Merging boot config into '{mergeWith}' failed to find the placeholder.");
            }
            else if (outputFileExtension == ".js")
            {
                output = $"export const config = /*json-start*/{output}/*json-end*/;";
            }

            File.WriteAllText(outputPath, output);
        }

        public void ComputeResourcesHash(BootJsonData bootConfig)
        {
            var sb = new StringBuilder();

            static void AddDictionary(StringBuilder sb, Dictionary<string, string>? res)
            {
                if (res == null)
                    return;

                foreach (var assetHash in res.Values.OrderBy(v => v))
                    sb.Append(assetHash);
            }

            AddDictionary(sb, bootConfig.resources.assembly);
            AddDictionary(sb, bootConfig.resources.coreAssembly);

            AddDictionary(sb, bootConfig.resources.jsModuleWorker);
            AddDictionary(sb, bootConfig.resources.jsModuleDiagnostics);
            AddDictionary(sb, bootConfig.resources.jsModuleNative);
            AddDictionary(sb, bootConfig.resources.jsModuleRuntime);
            AddDictionary(sb, bootConfig.resources.wasmNative);
            AddDictionary(sb, bootConfig.resources.wasmSymbols);
            AddDictionary(sb, bootConfig.resources.icu);
            AddDictionary(sb, bootConfig.resources.runtime);
            AddDictionary(sb, bootConfig.resources.lazyAssembly);

            if (bootConfig.resources.satelliteResources != null)
            {
                foreach (var culture in bootConfig.resources.satelliteResources)
                    AddDictionary(sb, culture.Value);
            }

            if (bootConfig.resources.vfs != null)
            {
                foreach (var entry in bootConfig.resources.vfs)
                    AddDictionary(sb, entry.Value);
            }

            if (bootConfig.resources.coreVfs != null)
            {
                foreach (var entry in bootConfig.resources.coreVfs)
                    AddDictionary(sb, entry.Value);
            }

            bootConfig.resources.hash = Utils.ComputeTextIntegrity(sb.ToString());
        }

        public Dictionary<string, string>? GetNativeResourceTargetInBootConfig(BootJsonData bootConfig, string resourceName)
        {
            string resourceExtension = Path.GetExtension(resourceName);
            if (resourceName.StartsWith("dotnet.native.worker", StringComparison.OrdinalIgnoreCase) && string.Equals(resourceExtension, ".mjs", StringComparison.OrdinalIgnoreCase))
                return bootConfig.resources.jsModuleWorker ??= new();
            else if (resourceName.StartsWith("dotnet.diagnostics", StringComparison.OrdinalIgnoreCase) && string.Equals(resourceExtension, ".js", StringComparison.OrdinalIgnoreCase))
                return bootConfig.resources.jsModuleDiagnostics ??= new();
            else if (resourceName.StartsWith("dotnet.native", StringComparison.OrdinalIgnoreCase) && string.Equals(resourceExtension, ".js", StringComparison.OrdinalIgnoreCase))
                return bootConfig.resources.jsModuleNative ??= new();
            else if (resourceName.StartsWith("dotnet.runtime", StringComparison.OrdinalIgnoreCase) && string.Equals(resourceExtension, ".js", StringComparison.OrdinalIgnoreCase))
                return bootConfig.resources.jsModuleRuntime ??= new();
            else if (resourceName.StartsWith("dotnet.native", StringComparison.OrdinalIgnoreCase) && string.Equals(resourceExtension, ".wasm", StringComparison.OrdinalIgnoreCase))
                return bootConfig.resources.wasmNative ??= new();
            else if (resourceName.StartsWith("dotnet", StringComparison.OrdinalIgnoreCase) && string.Equals(resourceExtension, ".js", StringComparison.OrdinalIgnoreCase))
                return null;
            else if (resourceName.StartsWith("dotnet.native", StringComparison.OrdinalIgnoreCase) && string.Equals(resourceExtension, ".symbols", StringComparison.OrdinalIgnoreCase))
                return bootConfig.resources.wasmSymbols ??= new();
            else if (resourceName.StartsWith("icudt", StringComparison.OrdinalIgnoreCase))
                return bootConfig.resources.icu ??= new();
            else
                Log.LogError($"The resource '{resourceName}' is not recognized as any native asset");

            return null;
        }

        public int GetDebugLevel(bool hasPdb)
        {
            int? debugLevel = ParseOptionalInt(DebugLevel);

            // If user didn't give us a value, check if we have any PDB.
            if (debugLevel == null && hasPdb)
                debugLevel = -1;

            // Fallback to -1 for build, or 0 for publish
            debugLevel ??= IsPublish ? 0 : -1;

            return debugLevel.Value;
        }

        public bool? ParseOptionalBool(string value)
        {
            if (string.IsNullOrEmpty(value) || !bool.TryParse(value, out var boolValue))
                return null;

            return boolValue;
        }

        public int? ParseOptionalInt(string value)
        {
            if (string.IsNullOrEmpty(value) || !int.TryParse(value, out var intValue))
                return null;

            return intValue;
        }

        public void TransformResourcesToAssets(BootJsonData config)
        {
            var resources = config.resources;
            var assets = new AssetsData();

            assets.hash = resources.hash;
            assets.jsModuleRuntime = MapJsAssets(resources.jsModuleRuntime);
            assets.jsModuleNative = MapJsAssets(resources.jsModuleNative);
            assets.jsModuleWorker = MapJsAssets(resources.jsModuleWorker);
            assets.jsModuleDiagnostics = MapJsAssets(resources.jsModuleDiagnostics);

            assets.wasmNative = resources.wasmNative?.Select(a => new WasmAsset()
            {
                url = a.Key,
                integrity = a.Value
            }).ToList();
            assets.wasmSymbols = resources.wasmSymbols?.Select(a => new SymbolsAsset()
            {
                url = a.Key,
            }).ToList();

            assets.icu = MapGeneralAssets(resources.icu);
            assets.coreAssembly = MapGeneralAssets(resources.coreAssembly);
            assets.assembly = MapGeneralAssets(resources.assembly);
            assets.corePdb = MapGeneralAssets(resources.corePdb);
            assets.pdb = MapGeneralAssets(resources.pdb);
            assets.lazyAssembly = MapGeneralAssets(resources.lazyAssembly);

            if (resources.satelliteResources != null)
            {
                assets.satelliteResources = resources.satelliteResources.ToDictionary(
                    kvp => kvp.Key,
                    kvp => MapGeneralAssets(kvp.Value)
                );
            }

            assets.libraryInitializers = MapJsAssets(resources.libraryInitializers);
            assets.modulesAfterConfigLoaded = MapJsAssets(resources.modulesAfterConfigLoaded);
            assets.modulesAfterRuntimeReady = MapJsAssets(resources.modulesAfterRuntimeReady);

            assets.extensions = resources.extensions;

            assets.coreVfs = MapVfsAssets(resources.coreVfs);
            assets.vfs = MapVfsAssets(resources.vfs);

            List<GeneralAsset>? MapGeneralAssets(Dictionary<string, string>? assets) => assets?.Select(a => new GeneralAsset()
            {
                name = resources.fingerprinting[a.Key],
                url = a.Key,
                integrity = a.Value
            }).ToList();

            List<JsAsset>? MapJsAssets(Dictionary<string, string>? assets) => assets?.Select(a => new JsAsset()
            {
                url = a.Key
            }).ToList();

            List<VfsAsset>? MapVfsAssets(Dictionary<string, Dictionary<string, string>>? assets) => assets?.Select(a => new VfsAsset()
            {
                virtualPath = a.Key,
                url = a.Value.Keys.First(),
                integrity = a.Value.Values.First()
            }).ToList();

            config.assets = assets;
            config.resources = null;
        }
    }
}
