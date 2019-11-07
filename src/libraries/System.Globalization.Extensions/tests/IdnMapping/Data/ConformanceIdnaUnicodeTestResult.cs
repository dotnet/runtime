// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization.Tests
{
    public sealed class ConformanceIdnaUnicodeTestResult : ConformanceIdnaTestResult
    {
        public bool ValidDomainName { get; private set; }

        public ConformanceIdnaUnicodeTestResult(string entry, string fallbackValue, bool validDomainName = true)
            : base(entry, fallbackValue)
        {
            ValidDomainName = validDomainName;
        }
    }
}
