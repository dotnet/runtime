// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    internal sealed class CompiledRegexRunner : RegexRunner
    {
        private readonly ScanDelegate _scanMethod;

        private readonly IndexOfAnyValues<char>[]? _indexOfAnyValues;

        /// <summary>This field will only be set if the pattern contains backreferences and has RegexOptions.IgnoreCase</summary>
        private readonly CultureInfo? _culture;

#pragma warning disable CA1823 // Avoid unused private fields. Justification: Used via reflection to cache the Case behavior if needed.
#pragma warning disable CS0169
        private RegexCaseBehavior _caseBehavior;
#pragma warning restore CS0169
#pragma warning restore CA1823

        internal delegate void ScanDelegate(RegexRunner runner, ReadOnlySpan<char> text);

        public CompiledRegexRunner(ScanDelegate scan, IndexOfAnyValues<char>[]? indexOfAnyValues, CultureInfo? culture)
        {
            _scanMethod = scan;
            _indexOfAnyValues = indexOfAnyValues;
            _culture = culture;
        }

        protected internal override void Scan(ReadOnlySpan<char> text)
            => _scanMethod(this, text);
    }
}
