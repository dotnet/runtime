// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;

namespace System.Text.RegularExpressions
{
    internal sealed class CompiledRegexRunnerFactory : RegexRunnerFactory
    {
        private readonly DynamicMethod _scanMethod;

        // Delegate is lazily created to avoid forcing JIT'ing until the regex is actually executed.
        private CompiledRegexRunner.ScanDelegate? _scan;

        public CompiledRegexRunnerFactory(DynamicMethod scanMethod)
        {
            _scanMethod = scanMethod;
        }

        protected internal override RegexRunner CreateInstance() =>
            new CompiledRegexRunner(
                _scan ??= _scanMethod.CreateDelegate<CompiledRegexRunner.ScanDelegate>());
    }
}
