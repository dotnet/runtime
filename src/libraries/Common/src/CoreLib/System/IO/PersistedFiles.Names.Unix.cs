// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
