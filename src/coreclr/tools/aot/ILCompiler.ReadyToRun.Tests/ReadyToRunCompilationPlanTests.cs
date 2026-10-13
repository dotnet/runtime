// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;

using System.Collections.Generic;

using ReadyToRunCompilationPlan = crossgen2::ILCompiler.ReadyToRunCompilationPlan;

using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

public class ReadyToRunCompilationPlanTests
{
    public static IEnumerable<object[]> CallGraphs()
    {
        yield return
        [
            new int[][]
            {
                [1],
                [2],
                [],
            },
            new[] { 2, 1, 0 },
        ];

        yield return
        [
            new int[][]
            {
                [1],
                [0, 2],
                [],
            },
            new[] { 1, 1, 0 },
        ];

        yield return
        [
            new int[][]
            {
                [1, 2],
                [3],
                [3],
                [],
                [],
            },
            new[] { 2, 1, 1, 0, 0 },
        ];
    }

    [Theory]
    [MemberData(nameof(CallGraphs))]
    public void ComputesCalleeFirstLevels(int[][] adjacency, int[] expectedLevels)
    {
        Assert.Equal(expectedLevels, ReadyToRunCompilationPlan.ComputeLevelsForTest(adjacency));
    }
}
