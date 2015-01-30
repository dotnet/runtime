// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
/*=============================================================================
**
**
**
** Purpose: Enums for the priorities of a Thread
**
**
=============================================================================*/

namespace System.Threading {
    using System.Threading;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum ThreadPriority
    {   
        /*=========================================================================
        ** Constants for thread priorities.
        =========================================================================*/
        Lowest = 0,
        BelowNormal = 1,
        Normal = 2,
        AboveNormal = 3,
        Highest = 4
    
    }
}
