// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Text.RegularExpressions
{
    internal sealed class CompiledRegexRunner : RegexRunner
    {
        private readonly ScanDelegate _scanMethod;
        /// <summary>This field will only be set if the pattern contains backreferences and has RegexOptions.IgnoreCase</summary>
        private readonly CultureInfo? _culture;

        internal delegate void ScanDelegate(RegexRunner runner, ReadOnlySpan<char> text);

        public CompiledRegexRunner(ScanDelegate scan, CultureInfo? culture)
        {
            _scanMethod = scan;
            _culture = culture;
        }

        protected internal override void Scan(ReadOnlySpan<char> text)
            => _scanMethod(this, text);
    }
}
