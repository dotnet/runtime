// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Runtime.Versioning;

namespace System.Runtime.Versioning.Tests
{
    public static class VersioningHelperTests
    {
        [Fact]
        public static void MakeVersionSafeNameTest()
        {
            string str1 = VersioningHelper.MakeVersionSafeName("TestFile", ResourceScope.Process, ResourceScope.AppDomain);
            Assert.Equal($"TestFile_r3_ad{AppDomain.CurrentDomain.Id}", str1);
        }
    }
}
