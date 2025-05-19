// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public static class BalancingGroupTests
    {
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public static void BalancingGroup_CaptureCountConsistentWithConditional(RegexOptions options)
        {
            string input = "00123xzacvb1";
            string pattern = @"\d+((?'x'[a-z-[b]]+)).(?<=(?'2-1'(?'x1'..)).{6})b(?(2)(?'Group2Captured'.)|(?'Group2NotCaptured'.))";
            
            Match match = new Regex(pattern, options).Match(input);
            
            // Since Group2Captured is matched, Group2 must have been evaluated as matched during conditional evaluation
            Assert.True(match.Groups["Group2Captured"].Success);
            Assert.False(match.Groups["Group2NotCaptured"].Success);
            
            // Group2 should have capture count consistent with conditional evaluation
            Assert.True(match.Groups[2].Success);
            Assert.Equal(1, match.Groups[2].Captures.Count);
        }
    }
}