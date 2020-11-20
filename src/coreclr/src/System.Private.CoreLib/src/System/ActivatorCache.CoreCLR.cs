// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    internal sealed partial class RuntimeType
    {
        /// <summary>
        /// A cache which allows optimizing <see cref="Activator.CreateInstance"/>,
        /// <see cref="RuntimeType.CreateInstanceDefaultCtor"/>, and related APIs.
        /// </summary>
        private sealed unsafe class ActivatorCache
        {
            // The managed calli to the newobj allocator, plus its first argument (MethodTable*).
            // In the case of the COM allocator, first arg is ComClassFactory*, not MethodTable*.
            private readonly delegate*<IntPtr, object?> _pfnAllocator;
            private readonly IntPtr _allocatorFirstArg;

            // The managed calli to the parameterless ctor, taking "this" (as object) as its first argument.
            // For value type ctors, we'll point to a special unboxing stub.
            private readonly delegate*<object?, void> _pfnCtor;

#if DEBUG
            private readonly WeakReference<RuntimeType> _originalRuntimeType; // don't prevent the RT from being collected
#endif

            internal ActivatorCache([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] RuntimeType rt)
            {
                Debug.Assert(rt != null);

#if DEBUG
                _originalRuntimeType = new WeakReference<RuntimeType>(rt);
#endif

                _pfnAllocator = (delegate*<IntPtr, object>)RuntimeTypeHandle.GetAllocatorFtn(rt, out MethodTable* pMT, forGetUninitializedObject: false);
                _allocatorFirstArg = (IntPtr)pMT;

                static void CtorNoopStub(object? uninitializedObject) { }
                _pfnCtor = &CtorNoopStub;

                RuntimeMethodHandleInternal defaultCtorRMH = RuntimeTypeHandle.GetDefaultConstructor(rt); // could be null

                if (pMT->IsValueType)
                {
                    if (pMT->IsNullable)
                    {
                        // Activator.CreateInstance returns null given typeof(Nullable<T>).

                        static object? ReturnNull(IntPtr _) => null;
                        _pfnAllocator = &ReturnNull;
                    }
                    else if (pMT->HasDefaultConstructor)
                    {
                        // ValueType with explicit parameterless ctor typed as (ref T) -> void.
                        // We'll point the ctor at our unboxing stub. It's not terribly efficient,
                        // but value types almost never have explicit parameterless ctors, so
                        // this shouldn't be a problem in practice.

                        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern",
                            Justification = "The linker requirements are already satisfied by the enclosing method.")]
                        static void CtorUnboxValueTypeStub(object? uninitializedObject)
                        {
                            Debug.Assert(uninitializedObject != null);
                            Debug.Assert(RuntimeHelpers.GetMethodTable(uninitializedObject)->IsValueType);

                            RuntimeType rt = (RuntimeType)uninitializedObject.GetType();
                            RuntimeMethodHandleInternal rmh = RuntimeTypeHandle.GetDefaultConstructor(rt);
                            IntPtr pfnCtor = RuntimeMethodHandle.GetFunctionPointer(rmh);
                            ((delegate*<ref byte, void>)pfnCtor)(ref uninitializedObject!.GetRawData());
                        }
                        _pfnCtor = &CtorUnboxValueTypeStub;
                    }
                    else
                    {
                        // ValueType with no explicit parameterless ctor; assume ctor returns default(T)

                        Debug.Assert(defaultCtorRMH.IsNullHandle());
                    }
                }
                else
                {
                    // Reference type - we can't proceed unless there's a default ctor we can call.

                    Debug.Assert(rt.IsClass);

                    if (pMT->IsComObject)
                    {
                        if (rt.IsGenericCOMObjectImpl())
                        {
                            // This is the __ComObject base type, which means that the MethodTable* we have
                            // doesn't contain CLSID information. The CLSID information is instead hanging
                            // off of the RuntimeType's sync block. We'll set the allocator to our stub, and
                            // instead of a MethodTable* we'll pass in the handle to the RuntimeType. The
                            // handles we create live for the lifetime of the app, but that's ok since it
                            // matches coreclr's internal implementation anyway (see GetComClassHelper).

                            static object AllocateComObject(IntPtr runtimeTypeHandle)
                            {
                                RuntimeType rt = (RuntimeType)GCHandle.FromIntPtr(runtimeTypeHandle).Target!;
                                Debug.Assert(rt != null);

                                return RuntimeTypeHandle.AllocateComObject(rt);
                            }
                            _pfnAllocator = &AllocateComObject;
                            _allocatorFirstArg = GCHandle.ToIntPtr(GCHandle.Alloc(rt));
                        }

                        // Neither __ComObject nor any derived type gets its parameterless ctor called.
                        // Activation is handled entirely by the allocator.

                        defaultCtorRMH = default;
                    }
                    else if (!pMT->HasDefaultConstructor)
                    {
                        throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, rt));
                    }
                    else
                    {
                        // Reference type with explicit parameterless ctor

                        Debug.Assert(!defaultCtorRMH.IsNullHandle());
                    }
                }

                if (defaultCtorRMH.IsNullHandle())
                {
                    CtorIsPublic = true; // implicit parameterless ctor is always considered public
                }
                else
                {
                    CtorIsPublic = (RuntimeMethodHandle.GetAttributes(defaultCtorRMH) & MethodAttributes.Public) != 0;
                }

                Debug.Assert(_pfnAllocator != null);
                Debug.Assert(_allocatorFirstArg != IntPtr.Zero);
                Debug.Assert(_pfnCtor != null); // we use null singleton pattern if no ctor call is necessary
            }

            internal bool CtorIsPublic { get; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal object? CreateUninitializedObject(RuntimeType rt)
            {
                // We don't use RuntimeType, but we force the caller to pass it so
                // that we can keep it alive on their behalf. Once the object is
                // constructed, we no longer need the reference to the type instance,
                // as the object itself will keep the type alive.

#if DEBUG
                Debug.Assert(_originalRuntimeType.TryGetTarget(out RuntimeType? originalRT) && originalRT == rt,
                    "Caller passed the wrong RuntimeType to this routine.");
#endif

                object? retVal = _pfnAllocator(_allocatorFirstArg);
                GC.KeepAlive(rt);
                return retVal;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void CallConstructor(object? uninitializedObject) => _pfnCtor(uninitializedObject);
        }
    }
}
