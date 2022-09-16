// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Quic;
using System.Runtime.CompilerServices;

namespace System.Net.Test.Common
{
    public static class QuicLoad
    {
        [ModuleInitializer]
        internal static void InitializeQuic()
        {
            // This will load Quic (if supported) to avoid interference with RemoteExecutor
            // See https://github.com/dotnet/runtime/pull/75424 for more details
            // IsSupported currently does not unload lttng. If it does in the future,
            // we may need to call some real Quic API here to get everything loaded properly
            _ = OperatingSystem.IsLinux() && QuicConnection.IsSupported;
        }
    }
}
