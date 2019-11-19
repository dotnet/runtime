// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is the only concrete implementation of RegexRunnerFactory,
// but we cannot combine them due to RegexRunnerFactory having shipped public.

namespace System.Text.RegularExpressions
{
    internal sealed class CompiledRegexRunnerFactory : RegexRunnerFactory
    {
        private readonly Action<RegexRunner> _go;
        private readonly Func<RegexRunner, bool> _findFirstChar;
        private readonly Action<RegexRunner> _initTrackCount;

        public CompiledRegexRunnerFactory(Action<RegexRunner> go, Func<RegexRunner, bool> findFirstChar, Action<RegexRunner> initTrackCount)
        {
            _go = go;
            _findFirstChar = findFirstChar;
            _initTrackCount = initTrackCount;
        }

        protected internal override RegexRunner CreateInstance() => new CompiledRegexRunner(_go, _findFirstChar, _initTrackCount);
    }
}
