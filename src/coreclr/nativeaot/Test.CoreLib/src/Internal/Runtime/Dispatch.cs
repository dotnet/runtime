// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;

using Internal.Runtime.CompilerHelpers;

namespace Internal.Runtime
{
    internal static class Dispatch
    {
        [RuntimeExport("ResolveDispatch")]
        internal static unsafe IntPtr ResolveDispatch(object pObject, DispatchCell* pCell)
        {
            return IntPtr.Zero;
        }
    }
}
