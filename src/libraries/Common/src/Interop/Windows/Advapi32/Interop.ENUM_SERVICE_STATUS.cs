// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal sealed class ENUM_SERVICE_STATUS
        {
            internal string? serviceName;
            internal string? displayName;
            internal int serviceType;
            internal int currentState;
            internal int controlsAccepted;
            internal int win32ExitCode;
            internal int serviceSpecificExitCode;
            internal int checkPoint;
            internal int waitHint;
        }
    }
}
