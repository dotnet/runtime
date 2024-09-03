// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization.Tests
{
    public sealed class ConformanceIdnaUnicodeTestResult : ConformanceIdnaTestResult
    {
        public bool ValidDomainName { get; private set; }

        public ConformanceIdnaUnicodeTestResult(string entry, string fallbackValue, bool validDomainName = true)
            : base(entry, fallbackValue, IdnaTestResultType.ToUnicode)
        {
            ValidDomainName = validDomainName;
        }

        public ConformanceIdnaUnicodeTestResult(string entry, string fallbackValue, string statusValue, string statusFallbackValue, bool validDomainName = true)
            : base(entry, fallbackValue, statusValue, statusFallbackValue, IdnaTestResultType.ToUnicode)
        {
            ValidDomainName = validDomainName;
        }
    }
}
