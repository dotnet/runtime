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
    using System.Runtime.InteropServices;

    [ComVisibleAttribute(false)]
    public delegate void ParameterizedThreadStart(object obj);
}
