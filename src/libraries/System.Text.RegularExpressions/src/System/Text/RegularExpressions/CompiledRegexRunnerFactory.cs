// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is the only concrete implementation of RegexRunnerFactory,
// but we cannot combine them due to RegexRunnerFactory having shipped public.

using System.Reflection.Emit;

namespace System.Text.RegularExpressions
{
    internal sealed class CompiledRegexRunnerFactory : RegexRunnerFactory
    {
        private readonly DynamicMethod _goMethod;
        private readonly DynamicMethod _findFirstCharMethod;
        private readonly DynamicMethod _initTrackCountMethod;

        public CompiledRegexRunnerFactory(DynamicMethod go, DynamicMethod findFirstChar, DynamicMethod initTrackCount)
        {
            _goMethod = go;
            _findFirstCharMethod = findFirstChar;
            _initTrackCountMethod = initTrackCount;
        }

        protected internal override RegexRunner CreateInstance() =>
            new CompiledRegexRunner(
                (Action<RegexRunner>)_goMethod.CreateDelegate(typeof(Action<RegexRunner>)),
                (Func<RegexRunner, bool>)_findFirstCharMethod.CreateDelegate(typeof(Func<RegexRunner, bool>)),
                (Action<RegexRunner>)_initTrackCountMethod.CreateDelegate(typeof(Action<RegexRunner>)));
    }
}
