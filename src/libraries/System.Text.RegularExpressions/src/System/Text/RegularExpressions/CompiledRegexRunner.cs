// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions
{
    internal sealed class CompiledRegexRunner : RegexRunner
    {
        private readonly ScanDelegate _scanMethod;

        internal delegate void ScanDelegate(RegexRunner runner, Regex regex, ReadOnlySpan<char> text, int textstart, int prevlen, bool quick);

        public CompiledRegexRunner(ScanDelegate scan, int trackCount)
        {
            _scanMethod = scan;
            runtrackcount = trackCount;
        }

        protected internal override void Scan(Regex regex, ReadOnlySpan<char> text, int textstart, int prevlen, bool quick)
            => _scanMethod(this, regex, text, textstart, prevlen, quick);

        protected override void InitTrackCount() { }
    }
}
