// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.IO
{
    internal static partial class PathInternal
    {
        internal static unsafe int GetRootLength(ReadOnlySpan<char> path)
        {
            fixed (char* p = &MemoryMarshal.GetReference(path))
            {
                return (int)GetRootLength(p, (uint)path.Length);
            }
        }
    }

    internal static partial class PathHelpers
    {
        internal static string TrimEndingDirectorySeparator(string path) =>
            EndsInDirectorySeparator(path) ?
                path.Substring(0, path.Length - 1) :
                path;
    }
}
