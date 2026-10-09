// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace ILAssembler;

/// <summary>
/// Maps path prefixes to the paths that the debug information records, as the C# compiler's <c>-pathmap</c> option
/// does, so that the image and its Portable PDB need not depend on where the sources and the output are.
/// </summary>
/// <remarks>
/// <para>
/// A map is an ordered list of entries. Each entry has a key, a path prefix to replace, and a value, the path to
/// write in its place. Both end with a directory separator (see <see cref="TryParse"/>), so a key matches whole
/// leading directories only: the key <c>/src/</c> matches <c>/src/a.il</c> but not <c>/srcx/a.il</c>.
/// </para>
/// <para>
/// <see cref="Map"/> replaces the key of the first entry that is a prefix of the path. Keys are compared ordinally,
/// so case matters on every platform.
/// </para>
/// </remarks>
public sealed class PathMap
{
    private PathMap(ImmutableArray<KeyValuePair<string, string>> entries)
    {
        Entries = entries;
    }

    /// <summary>Gets the map without entries, which maps every path to itself.</summary>
    public static PathMap Empty { get; } = new(ImmutableArray<KeyValuePair<string, string>>.Empty);

    /// <summary>
    /// Gets the entries in the order they were given: each key, the path prefix to replace, and value, the path
    /// written in its place, both ending with a directory separator.
    /// </summary>
    public ImmutableArray<KeyValuePair<string, string>> Entries { get; }

    /// <summary>
    /// Parses a path map written as the C# compiler's <c>-pathmap</c> option takes it:
    /// <c>path1=sourcePath1,path2=sourcePath2,...</c>.
    /// </summary>
    /// <param name="text">The text to parse. An empty text is the empty map.</param>
    /// <param name="pathMap">The map, with its entries in the order of the text, when the text is valid.</param>
    /// <param name="error">A message that names the invalid entry, when the text is not valid.</param>
    /// <returns><see langword="true"/> when the text is valid.</returns>
    /// <remarks>
    /// <para>
    /// Entries are separated by <c>,</c>, and an entry's key is separated from its value by <c>=</c>. A doubled
    /// <c>,,</c> or <c>==</c> stands for the character itself, so a path can contain either. An empty entry, as
    /// after a trailing <c>,</c>, is skipped. An entry that does not have exactly one separating <c>=</c>, or whose
    /// key or value is empty, is an error.
    /// </para>
    /// <para>
    /// A key or value that does not end with <c>/</c> or <c>\</c> gets a directory separator appended: the one it
    /// already uses when it uses only <c>/</c> or only <c>\</c>, and otherwise
    /// <see cref="Path.DirectorySeparatorChar"/>.
    /// </para>
    /// </remarks>
    public static bool TryParse(string text, [NotNullWhen(true)] out PathMap? pathMap, [NotNullWhen(false)] out string? error)
    {
        var entries = ImmutableArray.CreateBuilder<KeyValuePair<string, string>>();
        foreach (string entry in SplitWithDoubledSeparatorEscaping(text, ','))
        {
            if (entry.Length == 0)
            {
                continue;
            }

            List<string> keyAndValue = SplitWithDoubledSeparatorEscaping(entry, '=');
            if (keyAndValue.Count != 2)
            {
                pathMap = null;
                error = $"Invalid path map entry '{entry}': expected <path>=<sourcePath>";
                return false;
            }

            if (keyAndValue[0].Length == 0 || keyAndValue[1].Length == 0)
            {
                pathMap = null;
                error = $"Invalid path map entry '{entry}': the path and the source path must not be empty";
                return false;
            }

            entries.Add(new KeyValuePair<string, string>(
                EnsureTrailingSeparator(keyAndValue[0]),
                EnsureTrailingSeparator(keyAndValue[1])));
        }

        pathMap = entries.Count == 0 ? Empty : new PathMap(entries.ToImmutable());
        error = null;
        return true;
    }

    /// <summary>
    /// Gets a map with the entries of this map followed by those of <paramref name="other"/>. Since the first
    /// matching entry wins, an entry of this map takes precedence over one of <paramref name="other"/>.
    /// </summary>
    public PathMap Concat(PathMap other)
    {
        if (other.Entries.IsEmpty)
        {
            return this;
        }

        return Entries.IsEmpty ? other : new PathMap(Entries.AddRange(other.Entries));
    }

    /// <summary>Gets the path to record for <paramref name="path"/>.</summary>
    /// <remarks>
    /// The first entry whose key is a prefix of the path, compared ordinally, gives the result: its value followed by
    /// the rest of the path. If that value uses only <c>/</c>, every <c>\</c> in the result becomes <c>/</c>; if it
    /// uses only <c>\</c>, every <c>/</c> becomes <c>\</c>. A path that no key matches, such as a relative path when
    /// the keys are full paths, is returned unchanged.
    /// </remarks>
    public string Map(string path)
    {
        foreach (KeyValuePair<string, string> entry in Entries)
        {
            if (!path.StartsWith(entry.Key, StringComparison.Ordinal))
            {
                continue;
            }

            string mapped = string.Concat(entry.Value, path.AsSpan(entry.Key.Length));
            bool hasSlash = entry.Value.Contains('/');
            bool hasBackslash = entry.Value.Contains('\\');
            if (hasSlash && !hasBackslash)
            {
                return mapped.Replace('\\', '/');
            }

            if (hasBackslash && !hasSlash)
            {
                return mapped.Replace('/', '\\');
            }

            return mapped;
        }

        return path;
    }

    // Splits the text at each single separator; a doubled separator stands for the character itself.
    private static List<string> SplitWithDoubledSeparatorEscaping(string text, char separator)
    {
        var parts = new List<string>();
        if (text.Length == 0)
        {
            return parts;
        }

        var part = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == separator)
            {
                if (i + 1 < text.Length && text[i + 1] == separator)
                {
                    i++;
                }
                else
                {
                    parts.Add(part.ToString());
                    part.Clear();
                    continue;
                }
            }

            part.Append(c);
        }

        parts.Add(part.ToString());
        return parts;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        char last = path[^1];
        if (last is '/' or '\\')
        {
            return path;
        }

        bool hasSlash = path.Contains('/');
        bool hasBackslash = path.Contains('\\');
        char separator = hasSlash && !hasBackslash ? '/'
            : hasBackslash && !hasSlash ? '\\'
            : Path.DirectorySeparatorChar;
        return path + separator;
    }
}
