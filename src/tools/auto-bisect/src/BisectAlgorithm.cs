using System;
using System.Collections.Generic;

namespace AutoBisect;

/// <summary>
/// Result of a single bisect step.
/// </summary>
public sealed class BisectStepResult
{
    /// <summary>
    /// The index of the commit to test next, or -1 if bisect is complete.
    /// </summary>
    public int CommitIndexToTest { get; init; }

    /// <summary>
    /// Whether the bisect is complete (found the culprit).
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// The index of the first bad commit, only valid when IsComplete is true.
    /// </summary>
    public int FirstBadCommitIndex { get; init; }
}

/// <summary>
/// Implements binary search (bisect) algorithm for finding the first bad commit.
/// Commits are ordered from oldest (good) to newest (bad).
/// </summary>
public sealed class BisectAlgorithm
{
    private readonly IReadOnlyList<string> _commits;
    private int _lowIndex;  // Inclusive - first index that could be the first bad commit
    private int _highIndex; // Inclusive - last index that could be the first bad commit

    /// <summary>
    /// Creates a new bisect algorithm instance.
    /// </summary>
    /// <param name="commits">List of commits ordered from oldest to newest.</param>
    public BisectAlgorithm(IReadOnlyList<string> commits)
    {
        if (commits.Count == 0)
        {
            throw new ArgumentException("Commits list cannot be empty.", nameof(commits));
        }

        _commits = commits;
        _lowIndex = 0;
        _highIndex = _commits.Count - 1;
    }

    /// <summary>
    /// Gets the total number of commits in the search space.
    /// </summary>
    public int TotalCommits => _commits.Count;

    /// <summary>
    /// Gets the number of commits remaining in the search space.
    /// </summary>
    public int RemainingCount => _highIndex - _lowIndex + 1;

    /// <summary>
    /// Gets the next step in the bisect process.
    /// </summary>
    public BisectStepResult GetNextStep()
    {
        if (_lowIndex > _highIndex)
        {
            throw new InvalidOperationException(
                $"Bisect invariant violated: _lowIndex ({_lowIndex}) > _highIndex ({_highIndex}). " +
                "This indicates a bug in the bisect algorithm or invalid test results.");
        }

        if (_lowIndex == _highIndex)
        {
            // Found the culprit
            return new BisectStepResult
            {
                IsComplete = true,
                CommitIndexToTest = -1,
                FirstBadCommitIndex = _lowIndex
            };
        }

        // Pick the middle commit to test
        var midIndex = _lowIndex + (_highIndex - _lowIndex) / 2;

        return new BisectStepResult
        {
            IsComplete = false,
            CommitIndexToTest = midIndex,
            FirstBadCommitIndex = -1
        };
    }

    /// <summary>
    /// Records the result of testing a commit.
    /// </summary>
    /// <param name="commitIndex">The index of the commit that was tested.</param>
    /// <param name="testFailed">True if the test failed (commit is bad), false if it passed (commit is good).</param>
    public void RecordResult(int commitIndex, bool testFailed)
    {
        if (commitIndex < _lowIndex || commitIndex > _highIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(commitIndex),
                $"Commit index {commitIndex} is outside the current search range [{_lowIndex}, {_highIndex}].");
        }

        if (testFailed)
        {
            // If test fails at mid, it means mid is bad. The first bad commit could be mid, or any
            // commit before mid.  So we search [low, mid]. When low >= high, we've found the first
            // bad commit.
            _highIndex = commitIndex;
        }
        else
        {
            // The test passed at this commit, so this commit is good.
            // The first bad commit must be after this one.
            // Narrow search to [commitIndex + 1, high].
            _lowIndex = commitIndex + 1;
        }
    }

    /// <summary>
    /// Gets the commit at the specified index.
    /// </summary>
    public string GetCommit(int index) => _commits[index];
}
