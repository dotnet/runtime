// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions
{
    internal sealed class CompiledRegexRunner : RegexRunner
    {
        private readonly ScanDelegate _scanMethod;

        internal delegate void ScanDelegate(RegexRunner runner, ReadOnlySpan<char> text);

        public CompiledRegexRunner(ScanDelegate scan, int trackCount)
        {
            _scanMethod = scan;
            runtrackcount = trackCount;
        }

        protected internal override void Scan(ReadOnlySpan<char> text)
            => _scanMethod(this, text);

        protected override void InitTrackCount() { }
    }
}
