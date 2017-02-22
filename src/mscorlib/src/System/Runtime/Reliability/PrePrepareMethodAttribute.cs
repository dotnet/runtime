// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
/*============================================================
**
**
** Purpose: Serves as a hint to ngen that the decorated method
** (and its statically determinable call graph) should be
** prepared (as for Constrained Execution Region use). This
** is primarily useful in the scenario where the method in
** question will be prepared explicitly at runtime and the
** author of the method knows this and wishes to avoid the
** overhead of runtime preparation.
**
**
===========================================================*/

using System;
using System.Runtime.InteropServices;

namespace System.Runtime.ConstrainedExecution
{
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false)]

    internal sealed class PrePrepareMethodAttribute : Attribute
    {
        public PrePrepareMethodAttribute()
        {
        }
    }
}
