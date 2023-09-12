// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.Runtime
{
    // Extensions to MethodTable that are specific to the use in the CoreLib.
    internal unsafe partial struct MethodTable
    {
#if !INPLACE_RUNTIME
        internal static MethodTable* GetArrayEEType()
        {

            return EETypePtr.EETypePtrOf<Array>().ToPointer();
        }
#endif
    }
}
