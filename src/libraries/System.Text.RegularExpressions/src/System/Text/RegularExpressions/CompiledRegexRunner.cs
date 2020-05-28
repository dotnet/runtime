// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.RegularExpressions
{
    internal sealed class CompiledRegexRunner : RegexRunner
    {
        private readonly Action<RegexRunner> _goMethod;
        private readonly Func<RegexRunner, bool> _findFirstCharMethod;

        public CompiledRegexRunner(Action<RegexRunner> go, Func<RegexRunner, bool> findFirstChar, int trackCount)
        {
            _goMethod = go;
            _findFirstCharMethod = findFirstChar;
            runtrackcount = trackCount;
        }

        protected override void Go() => _goMethod(this);

        protected override bool FindFirstChar() => _findFirstCharMethod(this);

        protected override void InitTrackCount() { }
    }
}
