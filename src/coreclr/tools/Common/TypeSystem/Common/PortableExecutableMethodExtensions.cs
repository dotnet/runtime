// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection.PortableExecutable;

namespace Internal.TypeSystem
{
    public static class PortableExecutableMethodExtensions
    {
        public static ImmutableArray<DebugDirectoryEntry> SafeReadDebugDirectory(this PEReader peReader)
        {
            int actualDbgDirSize = peReader.PEHeaders.PEHeader.DebugTableDirectory.Size;

            // This comes from the Size property of the DebugDirectoryEntry class.
            const int expectedDbgDirSizeBase = 28;

            if (actualDbgDirSize % expectedDbgDirSizeBase != 0)
                return ImmutableArray<DebugDirectoryEntry>.Empty;

            return peReader.ReadDebugDirectory();
        }
    }
}
