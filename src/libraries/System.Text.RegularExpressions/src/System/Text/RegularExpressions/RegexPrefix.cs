// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.RegularExpressions
{
    internal readonly struct RegexPrefix
    {
        internal RegexPrefix(string prefix, bool ci)
        {
            Prefix = prefix;
            CaseInsensitive = ci;
        }

        internal bool CaseInsensitive { get; }

        internal static RegexPrefix Empty { get; } = new RegexPrefix(string.Empty, false);

        internal string Prefix { get; }
    }
}
