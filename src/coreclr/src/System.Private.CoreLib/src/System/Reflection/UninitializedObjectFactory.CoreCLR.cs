// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    // Creates zero-initialized instances of types.
    // For reference types, equivalent of allocating memory without running ctor.
    // For value types, equivalent of boxing default(T).
    // Must not be used with Nullable<T>.
    internal unsafe class UninitializedObjectFactory
    {
        protected readonly MethodTable* _pMT;
        private readonly delegate*<MethodTable*, object> _pfnAllocate;
        private readonly RuntimeType _type;

        internal UninitializedObjectFactory(RuntimeType type)
        {
            Debug.Assert(type != null);
            Debug.Assert(RuntimeHelpers.IsFastInstantiable(type));

            _type = type;
            _pMT = RuntimeTypeHandle.GetMethodTable(type);
            _pfnAllocate = RuntimeHelpers.GetNewobjHelper(type);

            Debug.Assert(_pMT != null);
            Debug.Assert(!_pMT->IsNullable);
            Debug.Assert(_pfnAllocate != null);
        }

        public object CreateUninitializedInstance()
        {
            // If a GC kicks in between the time we load the newobj
            // helper address and the time we calli it, we don't want
            // the Type object to be eligible for collection. To avoid
            // this, we KeepAlive(this) - and the referenced Type -
            // until we have an instance of the object. From that point
            // onward, the object itself will keep the Type alive.

            object newObj = _pfnAllocate(_pMT);
            GC.KeepAlive(this);
            return newObj;
        }
    }
}
