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
        private readonly DynamicMethod _initTrackCountMethod;

        // Delegates are lazily created to avoid forcing JIT'ing until the regex is actually executed.
        private Action<RegexRunner>? _go;
        private Func<RegexRunner, bool>? _findFirstChar;
        private Action<RegexRunner>? _initTrackCount;

        public CompiledRegexRunnerFactory(DynamicMethod goMethod, DynamicMethod findFirstCharMethod, DynamicMethod initTrackCountMethod)
        {
            _goMethod = goMethod;
            _findFirstCharMethod = findFirstCharMethod;
            _initTrackCountMethod = initTrackCountMethod;
        }

        protected internal override RegexRunner CreateInstance() =>
            new CompiledRegexRunner(
                _go ??= (Action<RegexRunner>)_goMethod.CreateDelegate(typeof(Action<RegexRunner>)),
                _findFirstChar ??= (Func<RegexRunner, bool>)_findFirstCharMethod.CreateDelegate(typeof(Func<RegexRunner, bool>)),
                _initTrackCount ??= (Action<RegexRunner>)_initTrackCountMethod.CreateDelegate(typeof(Action<RegexRunner>)));
    }
}
