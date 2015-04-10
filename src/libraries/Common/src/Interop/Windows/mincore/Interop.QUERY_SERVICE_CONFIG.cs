// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe class QUERY_SERVICE_CONFIG
        {
            internal int dwServiceType;
            internal int dwStartType;
            internal int dwErrorControl;
            internal char* lpBinaryPathName;
            internal char* lpLoadOrderGroup;
            internal int dwTagId;
            internal char* lpDependencies;
            internal char* lpServiceStartName;
            internal char* lpDisplayName;
        }
    }
}
