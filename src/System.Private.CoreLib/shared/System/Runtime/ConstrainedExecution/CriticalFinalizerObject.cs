// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
** Deriving from this class will cause any finalizer you define to be critical
** (i.e. the finalizer is guaranteed to run, won't be aborted by the host and is
** run after the finalizers of other objects collected at the same time).
**
** 
===========================================================*/

#nullable enable
namespace System.Runtime.ConstrainedExecution
{
    public abstract class CriticalFinalizerObject
    {
        protected CriticalFinalizerObject()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1821:RemoveEmptyFinalizers")]
        ~CriticalFinalizerObject()
        {
        }
    }
}
