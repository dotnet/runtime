using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AutoBisect;

/// <summary>
/// Helper class for Git operations.
/// </summary>
public static class GitHelper
{
    /// <summary>
    /// Gets the list of commit SHAs between two commits (exclusive of good, inclusive of bad).
    /// Returns commits in chronological order (oldest first).
    /// </summary>
    public static async Task<IReadOnlyList<string>> GetCommitRangeAsync(
        string goodCommit,
        string badCommit,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default
    )
    {
        // First, verify both commits exist locally
        if (!await ValidateCommitAsync(goodCommit, workingDirectory, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Commit {goodCommit} not found locally. Try running: git fetch origin {goodCommit}"
            );
        }

        if (!await ValidateCommitAsync(badCommit, workingDirectory, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Commit {badCommit} not found locally. Try running: git fetch origin {badCommit}"
            );
        }

        // git rev-list returns commits in reverse chronological order (newest first)
        // We use --ancestry-path to only get commits on the path from good to bad
        string result;
        try
        {
            result = await RunGitCommandAsync(
                $"rev-list --ancestry-path {goodCommit}..{badCommit}",
                workingDirectory,
                cancellationToken
            );
        }
        catch (InvalidOperationException)
        {
            // --ancestry-path may fail if commits aren't on a direct path
            // Fall back to simple range
            result = await RunGitCommandAsync(
                $"rev-list {goodCommit}..{badCommit}",
                workingDirectory,
                cancellationToken
            );
        }

        var commits = new List<string>();
        foreach (var line in result.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var sha = line.Trim();
            if (!string.IsNullOrEmpty(sha))
            {
                commits.Add(sha);
            }
        }

        // Reverse to get chronological order (oldest first)
        commits.Reverse();
        return commits;
    }

    /// <summary>
    /// Gets the short SHA for a commit.
    /// </summary>
    public static async Task<string> GetShortShaAsync(
        string commit,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default
    )
    {
        var result = await RunGitCommandAsync(
            $"rev-parse --short {commit}",
            workingDirectory,
            cancellationToken
        );
        return result.Trim();
    }

    /// <summary>
    /// Gets the commit message subject (first line) for a commit.
    /// </summary>
    public static async Task<string> GetCommitSubjectAsync(
        string commit,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default
    )
    {
        var result = await RunGitCommandAsync(
            $"log -1 --format=%s {commit}",
            workingDirectory,
            cancellationToken
        );
        return result.Trim();
    }

    /// <summary>
    /// Validates that a commit SHA exists in the repository.
    /// </summary>
    public static async Task<bool> ValidateCommitAsync(
        string commit,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await RunGitCommandAsync($"cat-file -t {commit}", workingDirectory, cancellationToken);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static async Task<string> RunGitCommandAsync(
        string arguments,
        string? workingDirectory,
        CancellationToken cancellationToken
    )
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await errorTask;
            throw new InvalidOperationException($"Git command failed: {error}");
        }

        return await outputTask;
    }
}
