// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Differential fuzzer for <see cref="Matcher"/> (Microsoft.Extensions.FileSystemGlobbing) over in-memory file lists.
/// Patterns built from a small alphabet ('a', 'b', '*', '**', '/') are compared with a reference matcher: '*' matches any
/// characters within a segment, '**' any number of segments, and an exclude pattern removes files it matches or that live
/// under a directory it matches. Arbitrary patterns (dots, "..", backslashes) must not make Match throw.
/// </summary>
/// <remarks>Input layout: text split on '\n' into include patterns, then a '|' line, then exclude patterns, then files.</remarks>
internal sealed class GlobbingFuzzer : IFuzzer
{
    public string[] TargetAssemblies { get; } = ["Microsoft.Extensions.FileSystemGlobbing"];
    public string[] TargetCoreLibPrefixes => [];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2)
        {
            return;
        }

        bool arbitrary = (bytes[0] & 1) != 0;
        string text = new string(bytes.Slice(1).ToArray().Select(b => arbitrary ? (char)b : Alphabet[b % Alphabet.Length]).ToArray());
        string[] parts = text.Split('|');
        if (parts.Length < 3)
        {
            return;
        }

        string[] includes = Lines(parts[0]).Take(4).ToArray();
        string[] excludes = Lines(parts[1]).Take(4).ToArray();
        // Paths with a NUL are rejected by Path.GetFullPath (ArgumentException), which is fine.
        string[] files = Lines(parts[2]).Select(f => f.Replace("*", "", StringComparison.Ordinal)).Where(f => f.Length > 0 && !f.Contains('\0')).Distinct().Take(16).ToArray();
        if (includes.Length == 0 || files.Length == 0)
        {
            return;
        }

        var matcher = new Matcher(StringComparison.Ordinal);
        try
        {
            foreach (string include in includes)
            {
                matcher.AddInclude(include);
            }

            foreach (string exclude in excludes)
            {
                matcher.AddExclude(exclude);
            }
        }
        catch (ArgumentException) when (arbitrary)
        {
            return; // Documented for ".." anywhere but at the start of a pattern.
        }

        const string Root = "/root";
        PatternMatchingResult result = matcher.Match(Root, files.Select(f => Root + "/" + f));
        // Only canonical inputs (no empty segments, no leading/trailing '/') are compared with the reference; everything else
        // is only checked for not throwing.
        if (arbitrary || !files.All(IsCanonical) || !includes.All(IsCanonical) || !excludes.All(IsCanonical))
        {
            return;
        }

        var actual = result.Files.Select(f => f.Path).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        var expected = files
            .Where(f => includes.Any(p => Matches(p, f)) && !excludes.Any(p => ExcludedBy(p, f)))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        Check(actual.SequenceEqual(expected),
            $"includes [{string.Join(", ", includes)}], excludes [{string.Join(", ", excludes)}], files [{string.Join(", ", files)}]: matched [{string.Join(", ", actual)}], expected [{string.Join(", ", expected)}]");
    }

    private const string Alphabet = "aab*/**\n|a/b";

    private static IEnumerable<string> Lines(string text) => text.Split('\n').Where(l => l.Length > 0);

    private static bool IsCanonical(string path) =>
        !path.StartsWith('/') && !path.EndsWith('/') && !path.Contains("//", StringComparison.Ordinal);

    private static bool ExcludedBy(string pattern, string file)
    {
        // The file itself, or any of its parent directories.
        string[] segments = file.Split('/');
        for (int i = 1; i <= segments.Length; i++)
        {
            if (Matches(pattern, string.Join('/', segments.Take(i))))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Matches(string pattern, string path)
    {
        string[] patternSegments = pattern.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (patternSegments.Length > 0 && patternSegments[^1] == "**")
        {
            // A trailing "**" means every file below: "a/**" is "a/**/*" and doesn't match "a" itself.
            patternSegments = [.. patternSegments, "*"];
        }

        string[] pathSegments = path.Split('/');
        return MatchSegments(patternSegments, 0, pathSegments, 0);
    }

    private static bool MatchSegments(string[] pattern, int p, string[] path, int s)
    {
        if (p == pattern.Length)
        {
            return s == path.Length;
        }

        if (pattern[p] == "**")
        {
            for (int k = s; k <= path.Length; k++)
            {
                if (MatchSegments(pattern, p + 1, path, k))
                {
                    return true;
                }
            }

            return false;
        }

        return s < path.Length && SegmentMatches(pattern[p], path[s]) && MatchSegments(pattern, p + 1, path, s + 1);
    }

    private static bool SegmentMatches(string pattern, string segment) =>
        Regex.IsMatch(segment, "^" + Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal) + "$", RegexOptions.CultureInvariant);

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
