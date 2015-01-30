// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
/*============================================================
**
**
**
** Deriving from this class will cause any finalizer you define to be critical
** (i.e. the finalizer is guaranteed to run, won't be aborted by the host and is
** run after the finalizers of other objects collected at the same time).
**
** You must possess UnmanagedCode permission in order to derive from this class.
**
** 
===========================================================*/

using System;
using System.Security.Permissions;
using System.Runtime.InteropServices;

namespace System.Runtime.ConstrainedExecution
{
#if !FEATURE_CORECLR
    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode=true)]
#endif
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class CriticalFinalizerObject
    {
        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected CriticalFinalizerObject()
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        ~CriticalFinalizerObject()
        {
        }
    }
}
