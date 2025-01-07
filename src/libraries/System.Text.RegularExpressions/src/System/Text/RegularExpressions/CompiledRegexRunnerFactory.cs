// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection.Emit;

namespace System.Text.RegularExpressions
{
    internal sealed class CompiledRegexRunnerFactory(DynamicMethod scanMethod, object[]? searchValues, CultureInfo? culture) : RegexRunnerFactory
    {
        private readonly DynamicMethod _scanMethod = scanMethod;
        private readonly object[]? _searchValues = searchValues;
        /// <summary>This field will only be set if the pattern has backreferences and uses RegexOptions.IgnoreCase</summary>
        private readonly CultureInfo? _culture = culture;

        // Delegate is lazily created to avoid forcing JIT'ing until the regex is actually executed.
        private CompiledRegexRunner.ScanDelegate? _scan;

        protected internal override RegexRunner CreateInstance() =>
            new CompiledRegexRunner(
                _scan ??= _scanMethod.CreateDelegate<CompiledRegexRunner.ScanDelegate>(), _searchValues, _culture);
    }
}
