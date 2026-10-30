using System;
using Xunit;

namespace AutoBisect.Tests;

public class BisectAlgorithmTests
{
    [Fact]
    public void Constructor_EmptyCommits_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new BisectAlgorithm(Array.Empty<string>()));
    }

    [Fact]
    public void Constructor_ValidCommits_SetsRemainingCount()
    {
        var commits = new[] { "a", "b", "c", "d", "e" };
        var algo = new BisectAlgorithm(commits);

        Assert.Equal(5, algo.RemainingCount);
    }

    [Fact]
    public void SingleCommit_ImmediatelyComplete()
    {
        var algo = new BisectAlgorithm(new[] { "commit1" });

        var step = algo.GetNextStep();

        Assert.True(step.IsComplete);
        Assert.Equal(0, step.FirstBadCommitIndex);
        Assert.Equal(1, algo.RemainingCount);
    }

    [Fact]
    public void TwoCommits_FirstIsBad_FindsFirst()
    {
        // Commits: [good, bad] but if we test the first and it's bad, first is the culprit
        // Actually in bisect, we assume the range is [potentially_bad..potentially_bad]
        // Let's think: commits[0] could be first bad, commits[1] could be first bad
        // Test commits[0]: if bad, commits[0] is the first bad
        var algo = new BisectAlgorithm(new[] { "commit0", "commit1" });

        // With 2 commits, midIndex = 0 + (1-0)/2 = 0
        var step1 = algo.GetNextStep();
        Assert.False(step1.IsComplete);
        Assert.Equal(0, step1.CommitIndexToTest);
        Assert.Equal(2, algo.RemainingCount);

        // Test at index 0 fails (bad commit)
        algo.RecordResult(0, testFailed: true);

        // high = 0, low = 0, so should be complete
        var step2 = algo.GetNextStep();
        Assert.True(step2.IsComplete);
        Assert.Equal(0, step2.FirstBadCommitIndex);
    }

    [Fact]
    public void TwoCommits_SecondIsBad_FindsSecond()
    {
        var algo = new BisectAlgorithm(new[] { "commit0", "commit1" });

        // With 2 commits, midIndex = 0 + (1-0)/2 = 0
        var step1 = algo.GetNextStep();
        Assert.False(step1.IsComplete);
        Assert.Equal(0, step1.CommitIndexToTest);

        // Test at index 0 passes (good commit)
        algo.RecordResult(0, testFailed: false);

        // low = 1, high = 1, so should be complete
        var step2 = algo.GetNextStep();
        Assert.True(step2.IsComplete);
        Assert.Equal(1, step2.FirstBadCommitIndex);
    }

    [Fact]
    public void ThreeCommits_MiddleIsBad_FindsMiddle()
    {
        // Commits: [good, BAD, bad]
        // First bad is at index 1
        var algo = new BisectAlgorithm(new[] { "commit0", "commit1", "commit2" });

        // midIndex = 0 + (2-0)/2 = 1
        var step1 = algo.GetNextStep();
        Assert.False(step1.IsComplete);
        Assert.Equal(1, step1.CommitIndexToTest);
        Assert.Equal(3, algo.RemainingCount);

        // Test at index 1 fails
        algo.RecordResult(1, testFailed: true);
        // high = 1, low = 0

        // midIndex = 0 + (1-0)/2 = 0
        var step2 = algo.GetNextStep();
        Assert.False(step2.IsComplete);
        Assert.Equal(0, step2.CommitIndexToTest);

        // Test at index 0 passes
        algo.RecordResult(0, testFailed: false);
        // low = 1, high = 1

        var step3 = algo.GetNextStep();
        Assert.True(step3.IsComplete);
        Assert.Equal(1, step3.FirstBadCommitIndex);
    }

    [Fact]
    public void ThreeCommits_LastIsBad_FindsLast()
    {
        // Commits: [good, good, BAD]
        var algo = new BisectAlgorithm(new[] { "commit0", "commit1", "commit2" });

        // midIndex = 1
        var step1 = algo.GetNextStep();
        Assert.Equal(1, step1.CommitIndexToTest);

        // Test at index 1 passes
        algo.RecordResult(1, testFailed: false);
        // low = 2, high = 2

        var step2 = algo.GetNextStep();
        Assert.True(step2.IsComplete);
        Assert.Equal(2, step2.FirstBadCommitIndex);
    }

    [Fact]
    public void ThreeCommits_FirstIsBad_FindsFirst()
    {
        // Commits: [BAD, bad, bad]
        var algo = new BisectAlgorithm(new[] { "commit0", "commit1", "commit2" });

        // midIndex = 1
        var step1 = algo.GetNextStep();
        Assert.Equal(1, step1.CommitIndexToTest);

        // Test at index 1 fails
        algo.RecordResult(1, testFailed: true);
        // high = 1, low = 0

        // midIndex = 0
        var step2 = algo.GetNextStep();
        Assert.Equal(0, step2.CommitIndexToTest);

        // Test at index 0 fails
        algo.RecordResult(0, testFailed: true);
        // high = 0, low = 0

        var step3 = algo.GetNextStep();
        Assert.True(step3.IsComplete);
        Assert.Equal(0, step3.FirstBadCommitIndex);
    }

    [Fact]
    public void TenCommits_BadAtIndex7_FindsCorrectly()
    {
        // Commits 0-6 are good, commits 7-9 are bad
        // First bad is at index 7
        var commits = new[] { "c0", "c1", "c2", "c3", "c4", "c5", "c6", "c7", "c8", "c9" };
        var algo = new BisectAlgorithm(commits);

        var result = RunBisect(algo, firstBadIndex: 7);

        Assert.Equal(7, result);
    }

    [Fact]
    public void TenCommits_BadAtIndex0_FindsCorrectly()
    {
        var commits = new[] { "c0", "c1", "c2", "c3", "c4", "c5", "c6", "c7", "c8", "c9" };
        var algo = new BisectAlgorithm(commits);

        var result = RunBisect(algo, firstBadIndex: 0);

        Assert.Equal(0, result);
    }

    [Fact]
    public void TenCommits_BadAtIndex9_FindsCorrectly()
    {
        var commits = new[] { "c0", "c1", "c2", "c3", "c4", "c5", "c6", "c7", "c8", "c9" };
        var algo = new BisectAlgorithm(commits);

        var result = RunBisect(algo, firstBadIndex: 9);

        Assert.Equal(9, result);
    }

    [Fact]
    public void HundredCommits_BadAtIndex50_FindsInLogNSteps()
    {
        var commits = new string[100];
        for (int i = 0; i < 100; i++)
        {
            commits[i] = $"commit{i}";
        }

        var algo = new BisectAlgorithm(commits);
        var steps = 0;
        var firstBadIndex = 50;

        while (true)
        {
            var step = algo.GetNextStep();
            if (step.IsComplete)
            {
                Assert.Equal(firstBadIndex, step.FirstBadCommitIndex);
                break;
            }

            steps++;
            var testFailed = step.CommitIndexToTest >= firstBadIndex;
            algo.RecordResult(step.CommitIndexToTest, testFailed);
        }

        // Should complete in at most ceil(log2(100)) = 7 steps
        Assert.True(steps <= 7, $"Expected at most 7 steps, but took {steps}");
    }

    [Fact]
    public void HundredTwoCommits_BadAtMiddle_MatchesExpectedBehavior()
    {
        // This test verifies the specific scenario from the bug report
        // 102 commits, testing at index 51 should leave 51 commits when test passes
        var commits = new string[102];
        for (int i = 0; i < 102; i++)
        {
            commits[i] = $"commit{i}";
        }

        var algo = new BisectAlgorithm(commits);

        // First step: should test at index 51 (102/2 = 51)
        // Actually with low=0, high=101: mid = 0 + (101-0)/2 = 50
        var step1 = algo.GetNextStep();
        Assert.False(step1.IsComplete);
        Assert.Equal(50, step1.CommitIndexToTest); // (0 + 101) / 2 = 50

        // If test passes at index 50, remaining should be indices 51-101 = 51 commits
        algo.RecordResult(50, testFailed: false);
        Assert.Equal(51, algo.RemainingCount);

        // Next step should test the middle of the remaining range
        var step2 = algo.GetNextStep();
        Assert.False(step2.IsComplete);
        // low=51, high=101, mid = 51 + (101-51)/2 = 51 + 25 = 76
        Assert.Equal(76, step2.CommitIndexToTest);
    }

    [Fact]
    public void NoInfiniteLoop_TwoCommits_AllFailScenarios()
    {
        // This specifically tests the bug that caused infinite loops
        // Test all possible scenarios with 2 commits

        // Scenario 1: First is bad
        var algo1 = new BisectAlgorithm(new[] { "a", "b" });
        var result1 = RunBisectWithMaxSteps(algo1, firstBadIndex: 0, maxSteps: 10);
        Assert.Equal(0, result1);

        // Scenario 2: Second is bad (first is good)
        var algo2 = new BisectAlgorithm(new[] { "a", "b" });
        var result2 = RunBisectWithMaxSteps(algo2, firstBadIndex: 1, maxSteps: 10);
        Assert.Equal(1, result2);
    }

    [Fact]
    public void NoInfiniteLoop_ConsecutiveBadCommits()
    {
        // Test scenario where multiple consecutive commits are bad
        // Should find the FIRST bad commit
        var commits = new[] { "good1", "good2", "BAD1", "bad2", "bad3" };
        var algo = new BisectAlgorithm(commits);

        var result = RunBisectWithMaxSteps(algo, firstBadIndex: 2, maxSteps: 10);
        Assert.Equal(2, result);
    }

    [Fact]
    public void RecordResult_OutOfRange_ThrowsException()
    {
        var algo = new BisectAlgorithm(new[] { "a", "b", "c" });

        Assert.Throws<ArgumentOutOfRangeException>(() => algo.RecordResult(-1, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => algo.RecordResult(3, true));
    }

    /// <summary>
    /// Runs the bisect algorithm to completion and returns the index of the first bad commit.
    /// </summary>
    private static int RunBisect(BisectAlgorithm algo, int firstBadIndex)
    {
        while (true)
        {
            var step = algo.GetNextStep();
            if (step.IsComplete)
            {
                return step.FirstBadCommitIndex;
            }

            var testFailed = step.CommitIndexToTest >= firstBadIndex;
            algo.RecordResult(step.CommitIndexToTest, testFailed);
        }
    }

    /// <summary>
    /// Runs the bisect algorithm with a maximum number of steps to prevent infinite loops.
    /// Returns -1 if max steps exceeded.
    /// </summary>
    private static int RunBisectWithMaxSteps(BisectAlgorithm algo, int firstBadIndex, int maxSteps)
    {
        var steps = 0;
        while (steps < maxSteps)
        {
            var step = algo.GetNextStep();
            if (step.IsComplete)
            {
                return step.FirstBadCommitIndex;
            }

            steps++;
            var testFailed = step.CommitIndexToTest >= firstBadIndex;
            algo.RecordResult(step.CommitIndexToTest, testFailed);
        }

        Assert.Fail($"Bisect did not complete within {maxSteps} steps - possible infinite loop!");
        return -1;
    }
}
