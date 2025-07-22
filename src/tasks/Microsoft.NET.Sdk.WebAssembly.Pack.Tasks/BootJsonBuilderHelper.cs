// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        internal static readonly Regex bundlerFriendlyImportsRegex = new Regex(@"/\*!\s*bundlerFriendlyImports\s*\*/");
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

        public void WriteConfigToFile(BootJsonData config, string outputPath, string? outputFileExtension = null, string? mergeWith = null, string? imports = null)
        {
            var output = JsonSerializer.Serialize(config, JsonOptions);

            // Remove the $#[ and ]#$" that are used to mark JS variable usage.
            output = output
                .Replace("\"$#[", string.Empty)
                .Replace("]#$\"", string.Empty);

            outputFileExtension ??= Path.GetExtension(outputPath);
            Log.LogMessage($"Write config in format '{outputFileExtension}'");
            if (mergeWith != null)
            {
                string existingContent = File.ReadAllText(mergeWith);
                output = ReplaceWithAssert(
                    mergeWithPlaceholderRegex,
                    existingContent,
                    $"/*json-start*/{output}/*json-end*/",
                    $"Merging boot config into '{mergeWith}' failed to find the placeholder."
                );

                output = ReplaceWithAssert(
                    bundlerFriendlyImportsRegex,
                    output,
                    imports ?? string.Empty,
                    $"Failed to find the placeholder for bundler friendly imports."
                );
            }
            else if (outputFileExtension == ".js")
            {
                output = $"export const config = /*json-start*/{output}/*json-end*/;";
            }

            File.WriteAllText(outputPath, output);
        }

        private string ReplaceWithAssert(Regex regex, string content, string replacement, string errorMessage)
        {
            string existingContent = content;
            content = regex.Replace(content, e => replacement);
            if (existingContent.Equals(content))
                Log.LogError(errorMessage);

            return content;
        }

        public void ComputeResourcesHash(BootJsonData bootConfig)
        {
            ResourcesData resources = (ResourcesData)bootConfig.resources;

            var sb = new StringBuilder();

            static void AddDictionary(StringBuilder sb, Dictionary<string, string>? res)
            {
                if (res == null)
                    return;

                foreach (var assetHash in res.Values.OrderBy(v => v))
                    sb.Append(assetHash);
            }

            AddDictionary(sb, resources.assembly);
            AddDictionary(sb, resources.coreAssembly);

            AddDictionary(sb, resources.jsModuleWorker);
            AddDictionary(sb, resources.jsModuleDiagnostics);
            AddDictionary(sb, resources.jsModuleNative);
            AddDictionary(sb, resources.jsModuleRuntime);
            AddDictionary(sb, resources.wasmNative);
            AddDictionary(sb, resources.wasmSymbols);
            AddDictionary(sb, resources.icu);
            AddDictionary(sb, resources.runtime);
            AddDictionary(sb, resources.lazyAssembly);

            if (resources.satelliteResources != null)
            {
                foreach (var culture in resources.satelliteResources)
                    AddDictionary(sb, culture.Value);
            }

            if (resources.vfs != null)
            {
                foreach (var entry in resources.vfs)
                    AddDictionary(sb, entry.Value);
            }

            if (resources.coreVfs != null)
            {
                foreach (var entry in resources.coreVfs)
                    AddDictionary(sb, entry.Value);
            }

            resources.hash = Utils.ComputeTextIntegrity(sb.ToString());
        }

        public Dictionary<string, string>? GetNativeResourceTargetInBootConfig(BootJsonData bootConfig, string resourceName)
        {
            ResourcesData resources = (ResourcesData)bootConfig.resources;

            string resourceExtension = Path.GetExtension(resourceName);
            if (resourceName.StartsWith("dotnet.native.worker", StringComparison.OrdinalIgnoreCase) && string.Equals(resourceExtension, ".mjs", StringComparison.OrdinalIgnoreCase))
                return resources.jsModuleWorker ??= new();
            else if (resourceName.StartsWith("dotnet.diagnostics", StringComparison.OrdinalIgnoreCase) && string.Equals(resourceExtension, ".js", StringComparison.OrdinalIgnoreCase))
                return resources.jsModuleDiagnostics ??= new();
            else if (resourceName.StartsWith("dotnet.native", StringComparison.OrdinalIgnoreCase) && string.Equals(resourceExtension, ".js", StringComparison.OrdinalIgnoreCase))
                return resources.jsModuleNative ??= new();
            else if (resourceName.StartsWith("dotnet.runtime", StringComparison.OrdinalIgnoreCase) && string.Equals(resourceExtension, ".js", StringComparison.OrdinalIgnoreCase))
                return resources.jsModuleRuntime ??= new();
            else if (resourceName.StartsWith("dotnet.native", StringComparison.OrdinalIgnoreCase) && string.Equals(resourceExtension, ".wasm", StringComparison.OrdinalIgnoreCase))
                return resources.wasmNative ??= new();
            else if (resourceName.StartsWith("dotnet", StringComparison.OrdinalIgnoreCase) && string.Equals(resourceExtension, ".js", StringComparison.OrdinalIgnoreCase))
                return null;
            else if (resourceName.StartsWith("dotnet.native", StringComparison.OrdinalIgnoreCase) && string.Equals(resourceExtension, ".symbols", StringComparison.OrdinalIgnoreCase))
                return resources.wasmSymbols ??= new();
            else if (resourceName.StartsWith("icudt", StringComparison.OrdinalIgnoreCase))
                return resources.icu ??= new();
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

        public string TransformResourcesToAssets(BootJsonData config, bool bundlerFriendly = false)
        {
            List<string> imports = [];

            ResourcesData resources = (ResourcesData)config.resources;
            var assets = new AssetsData();

            assets.hash = resources.hash;
            assets.jsModuleRuntime = MapJsAssets(resources.jsModuleRuntime);
            assets.jsModuleNative = MapJsAssets(resources.jsModuleNative);
            assets.jsModuleWorker = MapJsAssets(resources.jsModuleWorker);
            assets.jsModuleDiagnostics = MapJsAssets(resources.jsModuleDiagnostics);

            assets.wasmNative = resources.wasmNative?.Select(a =>
            {
                var asset = new WasmAsset()
                {
                    name = a.Key,
                    integrity = a.Value
                };

                if (bundlerFriendly)
                {
                    string escaped = EscapeName(a.Key);
                    imports.Add($"import {escaped} from \"./{a.Key}\";");
                    asset.resolvedUrl = EncodeJavascriptVariableInJson(escaped);
                }

                return asset;
            }).ToList();
            assets.wasmSymbols = resources.wasmSymbols?.Select(a => new SymbolsAsset()
            {
                name = a.Key,
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
                    kvp => MapGeneralAssets(kvp.Value, variableNamePrefix: kvp.Key, subFolder: kvp.Key)
                );
            }

            assets.libraryInitializers = MapJsAssets(resources.libraryInitializers, subFolder: "..");
            assets.modulesAfterConfigLoaded = MapJsAssets(resources.modulesAfterConfigLoaded);
            assets.modulesAfterRuntimeReady = MapJsAssets(resources.modulesAfterRuntimeReady);

            assets.extensions = resources.extensions;

            assets.coreVfs = MapVfsAssets(resources.coreVfs);
            assets.vfs = MapVfsAssets(resources.vfs);

            if (bundlerFriendly && config.appsettings != null)
            {
                config.appsettings = config.appsettings.Select(a =>
                {
                    string escaped = EscapeName(a);
                    imports.Add($"import {escaped} from \"./{a}\";");
                    return EncodeJavascriptVariableInJson(escaped);
                }).ToList();
            }

            string EscapeName(string name) => Utils.FixupSymbolName(name);
            string EncodeJavascriptVariableInJson(string name) => $"$#[{name}]#$";

            List<GeneralAsset>? MapGeneralAssets(Dictionary<string, string>? assets, string? variableNamePrefix = null, string? subFolder = null) => assets?.Select(a =>
            {
                var asset = new GeneralAsset()
                {
                    virtualPath = resources.fingerprinting?[a.Key] ?? a.Key,
                    name = a.Key,
                    integrity = a.Value
                };

                if (bundlerFriendly)
                {
                    string escaped = EscapeName(string.Concat(subFolder, a.Key));
                    string subFolderWithSeparator = subFolder != null ? $"{subFolder}/" : string.Empty;
                    imports.Add($"import {escaped} from \"./{subFolderWithSeparator}{a.Key}\";");
                    asset.resolvedUrl = EncodeJavascriptVariableInJson(escaped);
                }

                return asset;
            }).ToList();

            List<JsAsset>? MapJsAssets(Dictionary<string, string>? assets, string? variableNamePrefix = null, string? subFolder = null) => assets?.Select(a =>
            {
                var asset = new JsAsset()
                {
                    name = a.Key
                };

                if (bundlerFriendly)
                {
                    string escaped = EscapeName(string.Concat(subFolder, a.Key));
                    string subFolderWithSeparator = subFolder != null ? $"{subFolder}/" : string.Empty;
                    imports.Add($"import * as {escaped} from \"./{subFolderWithSeparator}{a.Key}\";");
                    asset.moduleExports = EncodeJavascriptVariableInJson(escaped);
                }

                return asset;
            }).ToList();

            List<VfsAsset>? MapVfsAssets(Dictionary<string, Dictionary<string, string>>? assets) => assets?.Select(a =>
            {
                var asset = new VfsAsset()
                {
                    virtualPath = a.Key,
                    name = a.Value.Keys.First(),
                    integrity = a.Value.Values.First()
                };

                if (bundlerFriendly)
                {
                    string escaped = EscapeName(string.Concat(asset.name));
                    imports.Add($"import * as {escaped} from \"./{asset.name}\";");
                    asset.resolvedUrl = EncodeJavascriptVariableInJson(escaped);
                }

                return asset;
            }).ToList();

            config.resources = assets;

            return string.Join(Environment.NewLine, imports);
        }
    }
}
