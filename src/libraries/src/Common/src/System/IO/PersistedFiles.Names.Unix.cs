// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO
{
    internal static partial class PersistedFiles
    {
        // Temporary data, /tmp/.dotnet/corefx
        // User-persisted data, ~/.dotnet/corefx/
        // System-persisted data, /etc/dotnet/corefx/

        internal const string TopLevelDirectory = "dotnet";
        internal const string TopLevelHiddenDirectory = "." + TopLevelDirectory;
        internal const string SecondLevelDirectory = "corefx";
    }
}
