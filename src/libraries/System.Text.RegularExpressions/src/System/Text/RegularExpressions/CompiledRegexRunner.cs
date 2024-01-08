// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

#pragma warning disable CA1823, CS0169, IDE0044 // Fields used via reflection

namespace System.Text.RegularExpressions
{
    internal sealed class CompiledRegexRunner(CompiledRegexRunner.ScanDelegate scan, object[]? searchValues, CultureInfo? culture) : RegexRunner
    {
        private readonly ScanDelegate _scanMethod = scan;

#pragma warning disable IDE0051, IDE0052 // Accessed via reflection
        /// <summary>Set if the regex uses any SearchValues instances. Accessed via reflection.</summary>
        /// <remarks>If the array is non-null, this contains instances of SearchValues{char} or SearchValues{string}.</remarks>
        private readonly object[]? _searchValues = searchValues;

        /// <summary>Set if the pattern contains backreferences and has RegexOptions.IgnoreCase. Accessed via reflection.</summary>
        private readonly CultureInfo? _culture = culture;

        /// <summary>Caches a RegexCaseBehavior. Accessed via reflection.</summary>
        private RegexCaseBehavior _caseBehavior;
#pragma warning restore IDE0051, IDE0052

        internal delegate void ScanDelegate(RegexRunner runner, ReadOnlySpan<char> text);

        protected internal override void Scan(ReadOnlySpan<char> text)
            => _scanMethod(this, text);
    }
}
