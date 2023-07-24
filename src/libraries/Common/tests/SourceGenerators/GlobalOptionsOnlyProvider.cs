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

namespace SourceGenerators.Tests
{
    /// <summary>
    /// An implementation of <see cref="AnalyzerConfigOptionsProvider"/> that provides configuration in code
    /// of global options.
    /// </summary>
    internal class GlobalOptionsOnlyProvider : AnalyzerConfigOptionsProvider
    {
        public GlobalOptionsOnlyProvider(AnalyzerConfigOptions globalOptions)
        {
            GlobalOptions = globalOptions;
        }

        public sealed override AnalyzerConfigOptions GlobalOptions  { get; }

        public sealed override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return EmptyOptions.Instance;
        }

        public sealed override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return EmptyOptions.Instance;
        }

        private sealed class EmptyOptions : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                value = null;
                return false;
            }

            public static AnalyzerConfigOptions Instance = new EmptyOptions();
        }
    }
}
