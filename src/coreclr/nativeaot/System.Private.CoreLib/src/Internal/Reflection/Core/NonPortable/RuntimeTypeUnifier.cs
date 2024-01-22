// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Internal.Runtime;
using Internal.Runtime.Augments;

namespace Internal.Reflection.Core.NonPortable
{
    // The RuntimeTypeUnifier maintains a record of all System.Type objects created by the runtime.
    internal sealed class RuntimeTypeUnifier : ConcurrentUnifierW<IntPtr, RuntimeType>
    {
        //
        // Retrieves the unified Type object for given RuntimeTypeHandle (this is basically the Type.GetTypeFromHandle() api without the input validation.)
        //
        internal static unsafe RuntimeType GetRuntimeTypeForMethodTable(MethodTable* eeType)
        {
            // If writable data is supported, we shouldn't be using the hashtable - the runtime type
            // is accessible through a couple indirections from the MethodTable which is much faster.
            Debug.Assert(!Internal.Runtime.MethodTable.SupportsWritableData);
            return s_instance.GetOrAdd((IntPtr)eeType);
        }

        protected sealed override unsafe RuntimeType Factory(IntPtr rawRuntimeTypeHandleKey)
        {
            return new RuntimeType((MethodTable*)rawRuntimeTypeHandleKey);
        }

        private static readonly RuntimeTypeUnifier s_instance = new RuntimeTypeUnifier();
    }
}
