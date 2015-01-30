// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
/*=============================================================================
**
**
**
** Purpose: An example of a WaitHandle class
**
**
=============================================================================*/
namespace System.Threading {
    
    using System;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;

    [HostProtection(Synchronization=true, ExternalThreading=true)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class ManualResetEvent : EventWaitHandle
    {        
        public ManualResetEvent(bool initialState) : base(initialState,EventResetMode.ManualReset){}
    }
}
    
