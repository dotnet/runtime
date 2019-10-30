// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceProcess
{
    public enum ServiceControllerStatus
    {
        ContinuePending = Interop.Advapi32.ServiceControlStatus.STATE_CONTINUE_PENDING,
        Paused = Interop.Advapi32.ServiceControlStatus.STATE_PAUSED,
        PausePending = Interop.Advapi32.ServiceControlStatus.STATE_PAUSE_PENDING,
        Running = Interop.Advapi32.ServiceControlStatus.STATE_RUNNING,
        StartPending = Interop.Advapi32.ServiceControlStatus.STATE_START_PENDING,
        Stopped = Interop.Advapi32.ServiceControlStatus.STATE_STOPPED,
        StopPending = Interop.Advapi32.ServiceControlStatus.STATE_STOP_PENDING
    }
}
