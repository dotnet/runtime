// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
