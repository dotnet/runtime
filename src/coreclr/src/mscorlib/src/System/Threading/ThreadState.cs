// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
/*=============================================================================
**
**
**
** Purpose: Enum to represent the different thread states
**
**
=============================================================================*/

namespace System.Threading {

[Serializable]
[Flags]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum ThreadState
    {   
        /*=========================================================================
        ** Constants for thread states.
        =========================================================================*/
        Running = 0,
        StopRequested = 1,
        SuspendRequested = 2,
        Background = 4,
        Unstarted = 8,
        Stopped = 16,
        WaitSleepJoin = 32,
        Suspended = 64,
        AbortRequested = 128,
        Aborted = 256
    }
}
