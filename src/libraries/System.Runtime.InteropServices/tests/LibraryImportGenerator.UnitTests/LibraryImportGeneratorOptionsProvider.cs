// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using SourceGenerators.Tests;

namespace LibraryImportGenerator.UnitTests
{
    /// <summary>
    /// An implementation of <see cref="AnalyzerConfigOptionsProvider"/> that provides configuration in code
    /// of the options supported by the LibraryImportGenerator source generator. Used for testing various configurations.
    /// </summary>
    internal sealed class LibraryImportGeneratorOptionsProvider : GlobalOptionsOnlyProvider
    {
        public LibraryImportGeneratorOptionsProvider(TestTargetFramework targetFramework, bool useMarshalType, bool generateForwarders)
            :base(new GlobalGeneratorOptions(targetFramework, useMarshalType, generateForwarders))
        {
        }

        private sealed class GlobalGeneratorOptions : TargetFrameworkConfigOptions
        {
            private readonly bool _useMarshalType = false;
            private readonly bool _generateForwarders = false;
            public GlobalGeneratorOptions(TestTargetFramework targetFramework, bool useMarshalType, bool generateForwarders)
                : base(targetFramework)
            {
                _useMarshalType = useMarshalType;
                _generateForwarders = generateForwarders;
            }

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                switch (key)
                {
                    case OptionsHelper.UseMarshalTypeOption:
                        value = _useMarshalType.ToString();
                        return true;

                    case OptionsHelper.GenerateForwardersOption:
                        value = _generateForwarders.ToString();
                        return true;

                    default:
                        return base.TryGetValue(key, out value);
                }
            }
        }
    }
}
