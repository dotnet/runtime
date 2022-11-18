// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.RegularExpressions.Tests
{
    public sealed class RegexGeneratorHelper
    {
        internal static Task<Regex> SourceGenRegexAsync(string pattern, CultureInfo? culture, RegexOptions? options = null, TimeSpan? matchTimeout = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        internal static Task<Regex[]> SourceGenRegexAsync((string pattern, CultureInfo? culture, RegexOptions? options, TimeSpan? matchTimeout)[] regexes, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
