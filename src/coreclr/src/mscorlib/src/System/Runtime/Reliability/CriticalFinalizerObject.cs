// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using System.Runtime.InteropServices;

namespace System.Runtime.ConstrainedExecution
{
    public abstract class CriticalFinalizerObject
    {
        protected CriticalFinalizerObject()
        {
        }

        ~CriticalFinalizerObject()
        {
        }
    }
}
