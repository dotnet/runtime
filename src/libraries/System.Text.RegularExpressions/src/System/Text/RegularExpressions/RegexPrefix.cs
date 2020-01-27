// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.RegularExpressions
{
    internal readonly struct RegexPrefix
    {
        internal static RegexPrefix Empty => new RegexPrefix(string.Empty, caseInsensitive: false);

        internal RegexPrefix(string prefix, bool caseInsensitive)
        {
            Value = prefix;
            CaseInsensitive = caseInsensitive;
        }

        internal string Value { get; }

        internal bool CaseInsensitive { get; }
    }
}
