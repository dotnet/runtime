// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

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
            return DictionaryAnalyzerConfigOptions.Empty;
        }

        public sealed override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return DictionaryAnalyzerConfigOptions.Empty;
        }
    }

    /// <summary>
    /// An implementation of <see cref="AnalyzerConfigOptions"/> backed by an <see cref="ImmutableDictionary{TKey, TValue}"/>.
    /// </summary>
    internal sealed class DictionaryAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        public static readonly DictionaryAnalyzerConfigOptions Empty = new(ImmutableDictionary<string, string>.Empty);

        private readonly ImmutableDictionary<string, string> _options;

        public DictionaryAnalyzerConfigOptions(ImmutableDictionary<string, string> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            => _options.TryGetValue(key, out value);
    }
}
