// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal partial class PipeOptions
        {
            internal const int PIPE_ACCESS_INBOUND = 1;
            internal const int PIPE_ACCESS_OUTBOUND = 2;
            internal const int PIPE_ACCESS_DUPLEX = 3;
            internal const int PIPE_TYPE_BYTE = 0;
            internal const int PIPE_TYPE_MESSAGE = 4;
            internal const int PIPE_READMODE_BYTE = 0;
            internal const int PIPE_READMODE_MESSAGE = 2;
            internal const int PIPE_UNLIMITED_INSTANCES = 255;
        }
    }
}
