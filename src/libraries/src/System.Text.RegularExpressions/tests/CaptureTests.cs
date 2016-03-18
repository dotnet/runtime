// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using System.Collections;
using Xunit;

namespace Test
{
    public class CaptureTests
    {
        [Fact]
        public static void Capture_Test()
        {
            Match match = Regex.Match("adfadsfSUCCESSadsfadsf", @".*\B(SUCCESS)\B.*");
            int[] iMatch1 = { 0, 22 };
            string strMatch1 = "adfadsfSUCCESSadsfadsf";

            string[] strGroup1 = { "adfadsfSUCCESSadsfadsf", "SUCCESS" };
            int[] iGroup1 = { 7, 7 };
            string[] strGrpCap1 = { "SUCCESS" };

            Assert.True(match.Success);

            Assert.Equal(strMatch1, match.Value);
            Assert.Equal(iMatch1[0], match.Index);
            Assert.Equal(iMatch1[1], match.Length);
            Assert.Equal(1, match.Captures.Count);

            Assert.Equal(strMatch1, match.Captures[0].Value);
            Assert.Equal(iMatch1[0], match.Captures[0].Index);
            Assert.Equal(iMatch1[1], match.Captures[0].Length);

            Assert.Equal(2, match.Groups.Count);

            // Group 0 always is the Match
            Assert.Equal(strMatch1, match.Groups[0].Value);
            Assert.Equal(iMatch1[0], match.Groups[0].Index);
            Assert.Equal(iMatch1[1], match.Groups[0].Length);
            Assert.Equal(1, match.Groups[0].Captures.Count);

            // Group 0's Capture is always the Match
            Assert.Equal(strMatch1, match.Groups[0].Captures[0].Value);
            Assert.Equal(iMatch1[0], match.Groups[0].Captures[0].Index);
            Assert.Equal(iMatch1[1], match.Groups[0].Captures[0].Length);

            for (int i = 1; i < match.Groups.Count; i++)
            {
                Assert.Equal(strGroup1[i], match.Groups[i].Value);
                Assert.Equal(iGroup1[0], match.Groups[i].Index);
                Assert.Equal(iGroup1[0], match.Groups[i].Length);
                Assert.Equal(1, match.Groups[i].Captures.Count);
            }
        }
    }
}
