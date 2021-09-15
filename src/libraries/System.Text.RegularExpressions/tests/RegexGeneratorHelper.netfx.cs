// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Text.RegularExpressions.Tests
{
    public sealed class RegexGeneratorHelper
    {
        internal static Task<Regex> SourceGenRegex(string pattern, RegexOptions options = RegexOptions.None, int matchTimeout = -1, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
