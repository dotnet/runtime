// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization.Tests
{
    public enum IdnType { Transitional, Nontransitional, Both };

    public interface IConformanceIdnaTest
    {
        IdnType Type { get; }
        string Source { get; }
        ConformanceIdnaUnicodeTestResult UnicodeResult { get; }
        ConformanceIdnaTestResult ASCIIResult { get; }
        int LineNumber { get; }
    }
}
