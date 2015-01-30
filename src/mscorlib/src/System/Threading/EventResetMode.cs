// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
/*=============================================================================
**
** Enum: EventResetMode
**
**
** Purpose: Enum to determine the Event type to create
**
**
=============================================================================*/


namespace System.Threading
{
    using System.Runtime.InteropServices;
    [ComVisibleAttribute(false)]
    public enum EventResetMode
    {
        AutoReset = 0,
        ManualReset = 1
    }
}
