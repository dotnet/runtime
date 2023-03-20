// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using SourceGenerators.Tests;

namespace Microsoft.Interop.UnitTests
{
    /// <summary>
    /// An implementation of <see cref="AnalyzerConfigOptions"/> that provides configuration in code
    /// of the target framework options. Used when testing interop source generators.
    /// </summary>
    public class TargetFrameworkConfigOptions : AnalyzerConfigOptions
    {
        private static readonly string _liveTargetFrameworkVersion;
        private readonly string _targetFrameworkIdentifier;
        private readonly string _targetFrameworkVersion;

        static TargetFrameworkConfigOptions()
        {
            Version liveVersion = Version.Parse(
                typeof(TargetFrameworkConfigOptions)
                    .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                    .InformationalVersion.Split('-')[0]);
            _liveTargetFrameworkVersion = $"v{liveVersion.ToString(2)}";
        }

        public TargetFrameworkConfigOptions(TestTargetFramework targetFramework)
        {
            _targetFrameworkIdentifier = targetFramework switch
            {
                TestTargetFramework.Framework => ".NETFramework",
                TestTargetFramework.Standard => ".NETStandard",
                _ => ".NETCoreApp"
            };
            _targetFrameworkVersion = targetFramework switch
            {
                TestTargetFramework.Framework => "v4.8",
                TestTargetFramework.Standard => "v2.1",
                TestTargetFramework.Core => "v3.1",
                TestTargetFramework.Net6 => "v6.0",
                TestTargetFramework.Net => _liveTargetFrameworkVersion,
                _ => throw new UnreachableException()
            };
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            switch (key)
            {
                case "build_property.TargetFrameworkIdentifier":
                    value = _targetFrameworkIdentifier;
                    return true;

                case "build_property.TargetFrameworkVersion":
                    value = _targetFrameworkVersion;
                    return true;

                default:
                    value = null;
                    return false;
            }
        }
    }

}
