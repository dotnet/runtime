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
    private static StringComparison PathComparison => Path.DirectorySeparatorChar == '\\'
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    internal SourceRootPathMap(ITaskItem[]? sourceRoots)
    {
        if (sourceRoots is null || sourceRoots.Length == 0)
            throw new NotSupportedException("SourceRoots are required for shared cache identity.");
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
        CheckMappings();
    }

    private void CheckMappings()
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
                CheckOverlap(physical, logical, previous.Physical, previous.Logical);
                CheckOverlap(previous.Physical, previous.Logical, physical, logical);
            }
        }

        static void CheckOverlap(string child, string childLogical, string parent, string parentLogical)
        {
            if (!childLogical.StartsWith(parentLogical, StringComparison.Ordinal))
                return;
            string relative = childLogical.Substring(parentLogical.Length).TrimEnd('/').Replace('/', Path.DirectorySeparatorChar);
            if (!child.Equals(Path.Combine(parent, relative), PathComparison))
                throw new NotSupportedException("Overlapping logical SourceRoots must preserve their physical layout.");
        }
    }

    internal string Map(string path)
    {
        foreach (var root in _roots)
        {
            if (path.Equals(root.Physical, PathComparison))
                return "mapped:" + root.Logical;
            if (path.StartsWith(root.Physical + Path.DirectorySeparatorChar, PathComparison))
                return "mapped:" + root.Logical + path.Substring(root.Physical.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
        }
        return "absolute:" + path;
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
