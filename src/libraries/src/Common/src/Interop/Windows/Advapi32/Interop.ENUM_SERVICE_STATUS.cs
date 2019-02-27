// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Advapi32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal class ENUM_SERVICE_STATUS
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
        }
    }
}
