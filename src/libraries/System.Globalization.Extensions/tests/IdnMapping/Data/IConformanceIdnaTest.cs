// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Globalization.Extensions.Tests
{
    public enum IdnType { Transitional, Nontransitional, Both };

    public interface IConformanceIdnaTest
    {
        IdnType Type { get; }
        string Source { get; }
        ConformanceIdnaTestResult GetUnicodeResult { get; }
        ConformanceIdnaTestResult GetASCIIResult { get; }
        int LineNumber { get; }
    }
}
