// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

#nullable enable

namespace ILCompiler
{
    public static class AssemblyExtensions
    {
        public static Version? GetTargetFrameworkVersion(this EcmaAssembly assembly)
        {
            // Get the custom attributes from the assembly's metadata
            MetadataReader reader = assembly.MetadataReader;
            CustomAttributeHandle attrHandle = reader.GetCustomAttributeHandle(assembly.AssemblyDefinition.GetCustomAttributes(),
                "System.Runtime.Versioning", "TargetFrameworkAttribute");
            if (!attrHandle.IsNil)
            {
                CustomAttribute attr = reader.GetCustomAttribute(attrHandle);
                CustomAttributeValue<TypeDesc> decoded = attr.DecodeValue(new CustomAttributeTypeProvider(assembly));
                if (decoded.FixedArguments.Length == 1 && decoded.FixedArguments[0].Value is string tfm && !string.IsNullOrEmpty(tfm))
                {
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
            return null;
        }
    }
}
