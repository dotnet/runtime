// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Microsoft.NET.Build.Tasks;

internal sealed class SourceRootPathMap
{
    private readonly List<(string Physical, string Logical)> _roots = new();
    internal string? ExecutionRoot { get; }
    private static StringComparison PathComparison => Path.DirectorySeparatorChar == '\\'
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    internal SourceRootPathMap(ITaskItem[]? sourceRoots)
    {
        if (sourceRoots is null || sourceRoots.Length == 0)
            throw new NotSupportedException("SourceRoots are required for relative execution.");
        foreach (ITaskItem root in sourceRoots)
        {
            if (!Path.IsPathRooted(root.ItemSpec))
                throw new NotSupportedException("SourceRoots must be absolute.");
            string physical = Path.GetFullPath(root.ItemSpec).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string logical = root.GetMetadata("MappedPath");
            if (physical.Length == 0 || physical.Equals(Path.GetPathRoot(physical), PathComparison))
                throw new NotSupportedException("SourceRoots must not be filesystem roots.");
            if (_roots.Any(previous => physical.Equals(previous.Physical, PathComparison)))
                throw new NotSupportedException("Duplicate physical SourceRoots are not supported.");
            _roots.Add((physical, logical));
        }
        _roots.Sort((left, right) => right.Physical.Length.CompareTo(left.Physical.Length));
        string outerRoot = _roots[_roots.Count - 1].Physical;
        if (_roots.All(root => root.Physical.Equals(outerRoot, PathComparison) ||
            root.Physical.StartsWith(outerRoot + Path.DirectorySeparatorChar, PathComparison)))
            ExecutionRoot = outerRoot;
    }

    internal void ValidateMappings()
    {
        for (int i = 0; i < _roots.Count; i++)
        {
            var (physical, logical) = _roots[i];
            if (!IsLogicalPath(logical) || logical[logical.Length - 1] != '/')
                throw new NotSupportedException("SourceRoots require stable /_/ or /_N/ MappedPath prefixes.");
            for (int j = 0; j < i; j++)
            {
                var previous = _roots[j];
                if (logical.Equals(previous.Logical, StringComparison.Ordinal))
                    throw new NotSupportedException("Duplicate logical SourceRoots are not supported.");
                CheckNested(physical, logical, previous.Physical, previous.Logical);
                CheckNested(previous.Physical, previous.Logical, physical, logical);
            }
        }

        static void CheckNested(string child, string childLogical, string parent, string parentLogical)
        {
            if (child.StartsWith(parent + Path.DirectorySeparatorChar, PathComparison) &&
                childLogical != parentLogical + child.Substring(parent.Length + 1).Replace('\\', '/') + "/")
                throw new NotSupportedException("Nested SourceRoots must preserve their relative layout.");
        }
    }

    internal string Map(string path)
    {
        path = GetValidatedFullPath(path);
        foreach (var root in _roots)
        {
            if (path.TrimEnd(Path.DirectorySeparatorChar).Equals(root.Physical, PathComparison))
                return "mapped:" + root.Logical;
            if (path.StartsWith(root.Physical + Path.DirectorySeparatorChar, PathComparison))
                return "mapped:" + root.Logical + path.Substring(root.Physical.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
        }
        return "absolute:" + path;
    }

    internal string Resolve(string identity)
    {
        const string AbsolutePrefix = "absolute:";
        const string MappedPrefix = "mapped:";
        if (identity.StartsWith(AbsolutePrefix, StringComparison.Ordinal))
        {
            string path = identity.Substring(AbsolutePrefix.Length);
            if (Path.IsPathRooted(path) && GetValidatedFullPath(path) == path)
                return path;
        }
        else if (identity.StartsWith(MappedPrefix, StringComparison.Ordinal))
        {
            foreach (var root in _roots)
            {
                string prefix = MappedPrefix + root.Logical;
                if (!identity.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                string path = Path.GetFullPath(Path.Combine(root.Physical,
                    identity.Substring(prefix.Length).Replace('/', Path.DirectorySeparatorChar)));
                if (Map(path) == identity)
                    return path;
                break;
            }
        }
        throw new ArgumentException($"Unknown or noncanonical invocation path identity '{identity}'.", nameof(identity));
    }

    internal string MapForExecution(string path)
    {
        // Resolve against the caller's cwd before changing the child process's cwd.
        path = GetValidatedFullPath(path);
        if (ExecutionRoot is null)
            return path;
        if (path.TrimEnd(Path.DirectorySeparatorChar).Equals(ExecutionRoot, PathComparison))
            return ".";
        if (path.StartsWith(ExecutionRoot + Path.DirectorySeparatorChar, PathComparison))
            return path.Substring(ExecutionRoot.Length + 1);
        return path;
    }

    private static string GetValidatedFullPath(string path)
    {
        path = Path.GetFullPath(path);
        if (path.Length > Path.GetPathRoot(path)!.Length)
            path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (path.AsSpan().IndexOfAny('"', '\r', '\n') >= 0 ||
            (Path.DirectorySeparatorChar == '/' && path.AsSpan().IndexOf('\\') >= 0))
            throw new NotSupportedException("Quoted or multiline paths are not supported by the shared cache.");
        return path;
    }

    internal static bool IsLogicalPath(string path)
    {
        if (!path.StartsWith("/_", StringComparison.Ordinal))
            return false;
        int index = 2;
        while (index < path.Length && path[index] is >= '0' and <= '9')
            index++;
        return index < path.Length && path[index] == '/' &&
            path.IndexOf('\\') < 0 && !path.Split('/').Any(part => part is "." or "..");
    }
}
