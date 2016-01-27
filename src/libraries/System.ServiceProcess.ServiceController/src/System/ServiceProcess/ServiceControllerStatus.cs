// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceProcess
{
    public enum ServiceControllerStatus
    {
        ContinuePending = Interop.mincore.ServiceControlStatus.STATE_CONTINUE_PENDING,
        Paused = Interop.mincore.ServiceControlStatus.STATE_PAUSED,
        PausePending = Interop.mincore.ServiceControlStatus.STATE_PAUSE_PENDING,
        Running = Interop.mincore.ServiceControlStatus.STATE_RUNNING,
        StartPending = Interop.mincore.ServiceControlStatus.STATE_START_PENDING,
        Stopped = Interop.mincore.ServiceControlStatus.STATE_STOPPED,
        StopPending = Interop.mincore.ServiceControlStatus.STATE_STOP_PENDING
    }
}
