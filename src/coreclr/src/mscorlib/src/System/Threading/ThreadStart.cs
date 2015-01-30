// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
/*=============================================================================
**
**
**
** Purpose: This class is a Delegate which defines the start method
**  for starting a thread.  That method must match this delegate.
**
**
=============================================================================*/

namespace System.Threading {
    using System.Security.Permissions;
    using System.Threading;

    // Define the delegate
    // NOTE: If you change the signature here, there is code in COMSynchronization
    //  that invokes this delegate in native.
[System.Runtime.InteropServices.ComVisible(true)]
    public delegate void ThreadStart();
}
