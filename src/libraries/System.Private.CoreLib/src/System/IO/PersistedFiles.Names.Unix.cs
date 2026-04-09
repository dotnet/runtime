// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    internal static partial class PersistedFiles
    {
        // Temporary data, /tmp/.dotnet/corefx
        // User-persisted data, ~/.dotnet/corefx/
        // System-persisted data, /etc/dotnet/corefx/

        internal const string TopLevelDirectory = "dotnet";
        internal const string TopLevelHiddenDirectory = "." + TopLevelDirectory;
        // Do not update this corefx reference to libraries
        // as we need to keep the original directory structure.
        internal const string SecondLevelDirectory = "corefx";
    }
}
