// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Emit;

namespace System.Text.RegularExpressions
{
    internal sealed class CompiledRegexRunnerFactory : RegexRunnerFactory
    {
        private readonly DynamicMethod _goMethod;
        private readonly DynamicMethod _findFirstCharMethod;
        private readonly int _trackcount;

        // Delegates are lazily created to avoid forcing JIT'ing until the regex is actually executed.
        private Action<RegexRunner>? _go;
        private Func<RegexRunner, bool>? _findFirstChar;

        public CompiledRegexRunnerFactory(DynamicMethod goMethod, DynamicMethod findFirstCharMethod, int trackcount)
        {
            _goMethod = goMethod;
            _findFirstCharMethod = findFirstCharMethod;
            _trackcount = trackcount;
        }

        protected internal override RegexRunner CreateInstance() =>
            new CompiledRegexRunner(
                _go ??= _goMethod.CreateDelegate<Action<RegexRunner>>(),
                _findFirstChar ??= _findFirstCharMethod.CreateDelegate<Func<RegexRunner, bool>>(),
                _trackcount);
    }
}
