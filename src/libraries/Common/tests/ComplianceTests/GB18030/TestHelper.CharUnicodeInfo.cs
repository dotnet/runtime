// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization.Tests;
using System.Linq;
using Xunit;

namespace GB18030.Tests;

// Requires CharUnicodeInfoTestData.cs and the UnicodeData.txt embedded resource, so it is compiled
// only into the projects whose GB18030 tests are driven by the Unicode character database.
public static partial class TestHelper
{
    private static IEnumerable<CharUnicodeInfoTestCase> GB18030CharUnicodeInfo { get; } = GetGB18030CharUnicodeInfo();

    private static IEnumerable<CharUnicodeInfoTestCase> GetGB18030CharUnicodeInfo()
    {
        const int CodePointsTotal = 9793; // Make sure a Unicode version downgrade doesn't make us lose coverage.

        var ret = CharUnicodeInfoTestData.TestCases.Where(tc => IsInGB18030Range(tc.CodePoint)).ToArray();
        Assert.Equal(CodePointsTotal, ret.Length);
        return ret;

        static bool IsInGB18030Range(int codePoint)
            => (codePoint >= 0x9FF0 && codePoint <= 0x9FFF) ||
            (codePoint >= 0x4DB6 && codePoint <= 0x4DBF) ||
            (codePoint >= 0x2A6D7 && codePoint <= 0x2A6DF) ||
            (codePoint >= 0x2B735 && codePoint <= 0x2B739) ||
            (codePoint >= 0x30000 && codePoint <= 0x3134A) ||
            (codePoint >= 0x31350 && codePoint <= 0x323AF) ||
            (codePoint >= 0x2EBF0 && codePoint <= 0x2EE5D);
    }

    public static IEnumerable<object[]> GB18030CharUnicodeInfoMemberData { get; } = GB18030CharUnicodeInfo.Select(data => new object[] { data }).ToArray();
}
