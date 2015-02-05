// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ServiceProcess
{
    public enum ServiceControllerStatus
    {
        ContinuePending = Interop.STATE_CONTINUE_PENDING,
        Paused = Interop.STATE_PAUSED,
        PausePending = Interop.STATE_PAUSE_PENDING,
        Running = Interop.STATE_RUNNING,
        StartPending = Interop.STATE_START_PENDING,
        Stopped = Interop.STATE_STOPPED,
        StopPending = Interop.STATE_STOP_PENDING
    }
}
