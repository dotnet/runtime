// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
namespace System.Runtime.ConstrainedExecution
{
    using System;
    using System.Runtime.InteropServices;

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false)]

    public sealed class PrePrepareMethodAttribute : Attribute
    {
        public PrePrepareMethodAttribute()
        {
        }
    }
}
