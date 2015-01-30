// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
/*=============================================================================
**
**
**
** Purpose: Enum to represent the different threading models
**
**
=============================================================================*/

namespace System.Threading {

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum ApartmentState
    {   
        /*=========================================================================
        ** Constants for thread apartment states.
        =========================================================================*/
        STA = 0,
        MTA = 1,
        Unknown = 2
    }
}
