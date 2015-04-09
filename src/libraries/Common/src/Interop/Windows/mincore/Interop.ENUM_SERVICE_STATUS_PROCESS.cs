// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal class ENUM_SERVICE_STATUS_PROCESS
        {
            internal string serviceName = null;
            internal string displayName = null;
            internal int serviceType = 0;
            internal int currentState = 0;
            internal int controlsAccepted = 0;
            internal int win32ExitCode = 0;
            internal int serviceSpecificExitCode = 0;
            internal int checkPoint = 0;
            internal int waitHint = 0;
            internal int processID = 0;
            internal int serviceFlags = 0;
        }
    }
}
