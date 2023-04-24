// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
using System.Reflection.Emit;

namespace System.Text.RegularExpressions
{
    internal sealed class CompiledRegexRunnerFactory : RegexRunnerFactory
    {
        private readonly DynamicMethod _scanMethod;
        private readonly IndexOfAnyValues<char>[]? _indexOfAnyValues;
        /// <summary>This field will only be set if the pattern has backreferences and uses RegexOptions.IgnoreCase</summary>
        private readonly CultureInfo? _culture;

        // Delegate is lazily created to avoid forcing JIT'ing until the regex is actually executed.
        private CompiledRegexRunner.ScanDelegate? _scan;

        public CompiledRegexRunnerFactory(DynamicMethod scanMethod, IndexOfAnyValues<char>[]? indexOfAnyValues, CultureInfo? culture)
        {
            _scanMethod = scanMethod;
            _indexOfAnyValues = indexOfAnyValues;
            _culture = culture;
        }

        protected internal override RegexRunner CreateInstance() =>
            new CompiledRegexRunner(
                _scan ??= _scanMethod.CreateDelegate<CompiledRegexRunner.ScanDelegate>(), _indexOfAnyValues, _culture);
    }
}
