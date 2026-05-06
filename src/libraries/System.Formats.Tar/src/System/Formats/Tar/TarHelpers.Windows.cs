// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace System.Formats.Tar
{
    internal static partial class TarHelpers
    {
        internal static SortedDictionary<string, UnixFileMode>? CreatePendingModesDictionary()
            => null;

#pragma warning disable IDE0060
        internal static void CreateDirectory(string fullPath, UnixFileMode? mode, SortedDictionary<string, UnixFileMode>? pendingModes)
            => Directory.CreateDirectory(fullPath);
#pragma warning restore IDE0060

        internal static void SetPendingModes(SortedDictionary<string, UnixFileMode>? pendingModes)
            => Debug.Assert(pendingModes is null);
    }
}
