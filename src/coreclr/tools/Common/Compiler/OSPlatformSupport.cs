// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Metadata;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    internal sealed class OSPlatformSupport
    {
        private readonly string _targetOS;
        private readonly ConcurrentDictionary<TypeDesc, bool> _typeUnsupportedOnPlatform = new();
        private readonly ConcurrentDictionary<EcmaAssembly, bool> _assemblyUnsupportedOnPlatform = new();

        public OSPlatformSupport(string targetOS)
        {
            _targetOS = targetOS.ToLowerInvariant() switch
            {
                "win" => "windows",
                "osx" => "macos",
                "iossimulator" => "ios",
                "tvossimulator" => "tvos",
                _ => targetOS
            };
        }

        public bool IsUnsupportedOnPlatform(MethodDesc method)
            => method.GetTypicalMethodDefinition() is EcmaMethod ecmaMethod
                && EvaluatePlatformAttributes(ecmaMethod.GetDecodedCustomAttributes) switch
                {
                    PlatformSupport.Unsupported => true,
                    PlatformSupport.Supported => false,
                    _ => IsUnsupportedOnPlatform(ecmaMethod.OwningType)
                };

        private bool IsUnsupportedOnPlatform(TypeDesc type)
        {
            if (type is not EcmaType ecmaType)
                return false;

            if (_typeUnsupportedOnPlatform.TryGetValue(type, out bool cached))
                return cached;

            bool value = EvaluatePlatformAttributes(ecmaType.GetDecodedCustomAttributes) switch
            {
                PlatformSupport.Unsupported => true,
                PlatformSupport.Supported => false,
                _ when ecmaType.ContainingType is not null => IsUnsupportedOnPlatform(ecmaType.ContainingType),
                _ => IsAssemblyUnsupportedOnPlatform((EcmaAssembly)ecmaType.Module)
            };

            _typeUnsupportedOnPlatform[type] = value;
            return value;
        }

        private bool IsAssemblyUnsupportedOnPlatform(EcmaAssembly assembly)
        {
            if (!_assemblyUnsupportedOnPlatform.TryGetValue(assembly, out bool value))
            {
                _assemblyUnsupportedOnPlatform[assembly] =
                    value = EvaluatePlatformAttributes(assembly.GetDecodedCustomAttributes) == PlatformSupport.Unsupported;
            }

            return value;
        }

        private enum PlatformSupport
        {
            Unknown,
            Supported,
            Unsupported,
        }

        private PlatformSupport EvaluatePlatformAttributes(
            Func<string, string, IEnumerable<CustomAttributeValue<TypeDesc>>> getAttributes)
        {
            const string Namespace = "System.Runtime.Versioning";

            foreach (CustomAttributeValue<TypeDesc> attribute in getAttributes(Namespace, "UnsupportedOSPlatformAttribute"))
            {
                if (MatchesTargetOS(attribute))
                    return PlatformSupport.Unsupported;
            }

            bool hasSupportedOSPlatform = false;
            foreach (CustomAttributeValue<TypeDesc> attribute in getAttributes(Namespace, "SupportedOSPlatformAttribute"))
            {
                if (attribute.FixedArguments.Length == 0)
                    continue;

                hasSupportedOSPlatform = true;
                if (MatchesTargetOS(attribute))
                    return PlatformSupport.Supported;
            }

            return hasSupportedOSPlatform ? PlatformSupport.Unsupported : PlatformSupport.Unknown;
        }

        private bool MatchesTargetOS(CustomAttributeValue<TypeDesc> attribute)
        {
            if (attribute.FixedArguments.Length == 0)
                return false;

            string platformName = attribute.FixedArguments[0].Value?.ToString();
            if (string.Equals(platformName, _targetOS, StringComparison.OrdinalIgnoreCase))
                return true;

            // Linkage does not vary by OS version, so only check that the suffix is a version.
            if (platformName?.StartsWith(_targetOS, StringComparison.OrdinalIgnoreCase) != true)
                return false;

            return Version.TryParse(platformName.AsSpan(_targetOS.Length), out _);
        }
    }
}
