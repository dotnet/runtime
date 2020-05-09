// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Reflection
{
    // Creates initialized instances of reference types or of value types.
    // For reference types, calls the parameterless ctor.
    // For value types, calls the parameterless ctor if it exists; otherwise
    // return a boxed default(T). Must not be used with Nullable<T>.
    internal unsafe sealed class ObjectFactory : UninitializedObjectFactory
    {
        private readonly void* _pfnCtor;
        private readonly bool _isNonPublicCtor;

        // Creates a factory from an existing parameterless ctor
        internal ObjectFactory(RuntimeMethodHandleInternal hCtor)
            : base(RuntimeMethodHandle.GetDeclaringType(hCtor))
        {
            _pfnCtor = (void*)RuntimeMethodHandle.GetFunctionPointer(hCtor);
            Debug.Assert(_pfnCtor != null);

            _isNonPublicCtor = (RuntimeMethodHandle.GetAttributes(hCtor) & MethodAttributes.MemberAccessMask) != MethodAttributes.Public;
        }

        private ObjectFactory(RuntimeType type)
            : base(type)
        {
            Debug.Assert(_pMT->IsValueType);
            _isNonPublicCtor = false; // default(T) is always "public"
        }

        // Creates a factory for "box(default(T))" around a value type
        internal static ObjectFactory CreateFactoryForValueTypeDefaultOfT(RuntimeType type)
        {
            return new ObjectFactory(type);
        }

        public bool IsNonPublicCtor => _isNonPublicCtor;

        public object CreateInstance()
        {
            object newObj = CreateUninitializedInstance();

            if (!_pMT->IsValueType)
            {
                // Common case: we're creating a reference type
                ((delegate*<object, void>)_pfnCtor)(newObj);
            }
            else
            {
                // Less common case: we're creating a boxed value type
                // If an explicit parameterless ctor exists, call it now.
                if (_pfnCtor != null)
                {
                    ((delegate*<ref byte, void>)_pfnCtor)(ref newObj.GetRawData());
                }
            }

            return newObj;
        }
    }

    // Similar to ObjectFactory, but does not box value types 'T'.
    internal unsafe sealed class ObjectFactory<T> : UninitializedObjectFactory
    {
        private readonly void* _pfnCtor;

        internal ObjectFactory()
            : base((RuntimeType)typeof(T))
        {
            RuntimeType type = (RuntimeType)typeof(T);
            
            // It's ok if there's no default constructor on a value type.
            // We'll return default(T). For reference types, the constructor
            // must be present. In all cases, if a constructor is present, it
            // must be public.

            RuntimeMethodHandleInternal hCtor = RuntimeMethodHandleInternal.EmptyHandle;
            if (_pMT->HasDefaultConstructor)
            {
                hCtor = RuntimeTypeHandle.GetDefaultConstructor(type);
                Debug.Assert(!hCtor.IsNullHandle());
                if ((RuntimeMethodHandle.GetAttributes(hCtor) & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
                {
                    // parameterless ctor exists but is not public
                    throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, type));
                }

                _pfnCtor = (void*)RuntimeMethodHandle.GetFunctionPointer(hCtor);
            }
            else
            {
                if (!_pMT->IsValueType)
                {
                    // parameterless ctor missing on reference type
                    throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, type));
                }
            }
        }

        public T CreateInstance()
        {
            if (typeof(T).IsValueType)
            {
                T value = default!;
                if (_pfnCtor != null)
                {
                    ((delegate*<ref T, void>)_pfnCtor)(ref value);
                }
                return value;
            }
            else
            {
                object value = CreateUninitializedInstance();
                Debug.Assert(_pfnCtor != null);
                ((delegate*<object, void>)_pfnCtor)(value!);
                return Unsafe.As<object, T>(ref value);
            }
        }
    }
}
