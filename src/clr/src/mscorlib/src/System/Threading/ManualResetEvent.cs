// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    using System.Runtime.InteropServices;

    public sealed class ManualResetEvent : EventWaitHandle
    {        
        public ManualResetEvent(bool initialState) : base(initialState,EventResetMode.ManualReset){}
    }
}
    
