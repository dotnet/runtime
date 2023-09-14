// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Internal.Runtime
{
    // Extensions to MethodTable that are specific to the use in the CoreLib.
    internal unsafe partial struct MethodTable
    {
#if !INPLACE_RUNTIME
        internal static MethodTable* GetArrayEEType()
        {

            return MethodTable.Of<Array>();
        }

        internal static bool AreSameType(MethodTable* mt1, MethodTable* mt2)
        {
            return mt1 == mt2;
        }
#endif
    }
}
