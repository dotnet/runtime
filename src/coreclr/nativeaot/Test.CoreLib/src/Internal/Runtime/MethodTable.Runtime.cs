// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;

namespace Internal.Runtime
{
    // Extensions to MethodTable that are specific to the use in the CoreLib.
    internal unsafe partial struct MethodTable
    {
        internal MethodTable* GetArrayEEType()
        {
            return MethodTable.Of<Array>();
        }

        internal Exception GetClasslibException(ExceptionIDs id)
        {
            return RuntimeExceptionHelpers.GetRuntimeException(id);
        }
    }
}
