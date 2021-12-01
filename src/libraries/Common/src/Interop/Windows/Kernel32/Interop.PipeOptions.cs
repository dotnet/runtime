// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal static partial class PipeOptions
        {
            internal const uint PIPE_ACCESS_INBOUND = 1;
            internal const uint PIPE_ACCESS_OUTBOUND = 2;
            internal const uint PIPE_ACCESS_DUPLEX = 3;
            internal const uint PIPE_TYPE_BYTE = 0;
            internal const uint PIPE_TYPE_MESSAGE = 4;
            internal const uint PIPE_READMODE_BYTE = 0;
            internal const uint PIPE_READMODE_MESSAGE = 2;
            internal const uint PIPE_UNLIMITED_INSTANCES = 255;
        }
    }
}
