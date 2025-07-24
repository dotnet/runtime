// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Mono.Cecil;

namespace Mono.Linker
{
    public static class AssemblyDefinitionExtensions
    {
        public static EmbeddedResource? FindEmbeddedResource(this AssemblyDefinition assembly, string name)
        {
            foreach (var resource in assembly.MainModule.Resources)
            {
                if (resource is EmbeddedResource embeddedResource && embeddedResource.Name == name)
                    return embeddedResource;
            }
            return null;
        }

        public static Version? GetTargetFrameworkVersion(this AssemblyDefinition assembly)
        {
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute" && attr.HasConstructorArguments)
                {
                    var tfm = attr.ConstructorArguments[0].Value as string;
                    if (!string.IsNullOrEmpty(tfm))
                    {
                        // Try to extract the version from the TFM string, e.g. ".NETCoreApp,Version=v8.0"
                        var versionPrefix = "Version=v";
                        var idx = tfm.IndexOf(versionPrefix);
                        if (idx >= 0)
                        {
                            var versionStr = tfm.Substring(idx + versionPrefix.Length);
                            if (Version.TryParse(versionStr, out var version))
                                return version;
                        }
                    }
                }
            }
            return null;
        }
    }
}
