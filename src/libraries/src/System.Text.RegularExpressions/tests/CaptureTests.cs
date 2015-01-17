// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text.RegularExpressions;
using System.Collections;

namespace Test
{
    /// <summary>
    /// Tests the Capture class.
    /// </summary>
    public class CaptureTests
    {
        [Fact]
        public static void Capture_Test()
        {
            Match match = Regex.Match("adfadsfSUCCESSadsfadsf", @".*\B(SUCCESS)\B.*");
            Int32[] iMatch1 = { 0, 22 };
            String strMatch1 = "adfadsfSUCCESSadsfadsf";

            String[] strGroup1 = { "adfadsfSUCCESSadsfadsf", "SUCCESS" };
            Int32[] iGroup1 = { 7, 7 };
            String[] strGrpCap1 = { "SUCCESS" };

            Assert.True(match.Success, "Fail Do not found a match");

            Assert.True(match.Value.Equals(strMatch1), "Expected to return TRUE");
            Assert.Equal(match.Index, iMatch1[0]);
            Assert.Equal(match.Length, iMatch1[1]);
            Assert.Equal(match.Captures.Count, 1);

            Assert.True(match.Captures[0].Value.Equals(strMatch1), "Expected to return TRUE");
            Assert.Equal(match.Captures[0].Index, iMatch1[0]);
            Assert.Equal(match.Captures[0].Length, iMatch1[1]);

            Assert.Equal(match.Groups.Count, 2);

            //Group 0 always is the Match
            Assert.True(match.Groups[0].Value.Equals(strMatch1), "Expected to return TRUE");
            Assert.Equal(match.Groups[0].Index, iMatch1[0]);
            Assert.Equal(match.Groups[0].Length, iMatch1[1]);
            Assert.Equal(match.Groups[0].Captures.Count, 1);


            //Group 0's Capture is always the Match
            Assert.True(match.Groups[0].Captures[0].Value.Equals(strMatch1), "Expected to return TRUE");
            Assert.Equal(match.Groups[0].Captures[0].Index, iMatch1[0]);
            Assert.Equal(match.Groups[0].Captures[0].Length, iMatch1[1]);

            for (int i = 1; i < match.Groups.Count; i++)
            {
                Assert.True(match.Groups[i].Value.Equals(strGroup1[i]), "Expected to return TRUE");
                Assert.Equal(match.Groups[i].Index, iGroup1[0]);
                Assert.Equal(match.Groups[i].Length, iGroup1[0]);
                Assert.Equal(match.Groups[i].Captures.Count, 1);
            }

        }
    }
}
