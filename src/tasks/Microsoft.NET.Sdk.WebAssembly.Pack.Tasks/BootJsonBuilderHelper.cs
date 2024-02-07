// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.WebAssembly
{
    public class BootJsonBuilderHelper(TaskLoggingHelper Log)
    {
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

            AddDictionary(sb, bootConfig.resources.jsModuleWorker);
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

            bootConfig.resources.hash = Utils.ComputeTextIntegrity(sb.ToString());
        }

        public Dictionary<string, string>? GetNativeResourceTargetInBootConfig(BootJsonData bootConfig, string resourceName)
        {
            string resourceExtension = Path.GetExtension(resourceName);
            if (resourceName.StartsWith("dotnet.native.worker", StringComparison.OrdinalIgnoreCase) && string.Equals(resourceExtension, ".js", StringComparison.OrdinalIgnoreCase))
                return bootConfig.resources.jsModuleWorker ??= new();
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
            else if (resourceName.Equals("segmentation-rules.json", StringComparison.OrdinalIgnoreCase))
                return bootConfig.resources.icu ??= new();
            else
                Log.LogError($"The resource '{resourceName}' is not recognized as any native asset");

            return null;
        }
    }
}
