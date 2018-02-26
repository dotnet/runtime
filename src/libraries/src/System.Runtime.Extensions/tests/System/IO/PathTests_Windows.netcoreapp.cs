// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public partial class PathTests_Windows : PathTestsBase
    {
        [Theory,
            MemberData(nameof(TestData_GetPathRoot_Windows)),
            MemberData(nameof(TestData_GetPathRoot_Unc)),
            MemberData(nameof(TestData_GetPathRoot_DevicePaths))]
        public void GetPathRoot_Span(string value, string expected)
        {
            Assert.Equal(expected, new string(Path.GetPathRoot(value.AsSpan())));
            Assert.True(Path.IsPathRooted(value.AsSpan()));
        }

        [Theory, MemberData(nameof(TestData_UnicodeWhiteSpace))]
        public void GetFullPath_UnicodeWhiteSpaceStays(string component)
        {
            // When not NetFX full path should not cut off component
            string path = "C:\\Test" + component;
            Assert.Equal(path, Path.GetFullPath(path));
        }

        [Theory, MemberData(nameof(TestData_Periods))]
        public void GetFullPath_TrailingPeriodsCut(string component)
        {
            // Windows cuts off any simple white space added to a path
            string path = "C:\\Test" + component;
            Assert.Equal("C:\\Test", Path.GetFullPath(path));
        }
    }
}
