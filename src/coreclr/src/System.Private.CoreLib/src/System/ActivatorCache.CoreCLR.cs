// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
            private readonly delegate*<void*, object?> _pfnAllocator;
            private readonly void* _allocatorFirstArg;

            // The managed calli to the parameterless ctor, taking "this" (as object) as its first argument.
            private readonly delegate*<object?, void> _pfnCtor;
            private readonly bool _ctorIsPublic;

#if DEBUG
            private readonly RuntimeType _originalRuntimeType;
#endif

            internal ActivatorCache(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] RuntimeType rt,
                bool wrapExceptions)
            {
                Debug.Assert(rt != null);

#if DEBUG
                _originalRuntimeType = rt;
#endif

                // The check below is redundant since these same checks are performed at the
                // unmanaged layer, but this call will throw slightly different exceptions
                // than the unmanaged layer, and callers might be dependent on this.

                rt.CreateInstanceCheckThis();

                RuntimeTypeHandle.GetActivationInfo(rt, forGetUninitializedInstance: false,
                    out _pfnAllocator!, out _allocatorFirstArg,
                    out _pfnCtor!, out _ctorIsPublic);

                Debug.Assert(_pfnAllocator != null);
                Debug.Assert(_allocatorFirstArg != null);

                if (_pfnCtor == null)
                {
                    if (!RuntimeTypeHandle.IsValueType(rt))
                    {
                        // Reference type with no parameterless ctor.
                        // Is it __ComObject? If not, we can't continue.

#if FEATURE_COMINTEROP
                        if (!rt.IsGenericCOMObjectImpl())
#endif
                        {
                            throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, rt));
                        }
                    }
                    else if (RuntimeTypeHandle.IsConstructedNullableType(rt))
                    {
                        // Activator.CreateInstance returns null given typeof(Nullable<T>).

                        static object? ReturnNull(void* _) => null;
                        _pfnAllocator = &ReturnNull;
                        _allocatorFirstArg = default;
                    }

                    // At this point, we have Nullable<T>, a ctorless value type T,
                    // or a ctorless __ComObject. In any case, we should replace the
                    // ctor call with our no-op stub.

                    static void CtorNoopStub(object? uninitializedObject) { }
                    _pfnCtor = &CtorNoopStub; // we use null singleton pattern if no ctor call is necessary
                    _ctorIsPublic = true; // implicit parameterless ctor is always considered public
                }

                // We don't need to worry about invoking cctors here. The runtime will figure it
                // out for us when the instance ctor is called. For value types, because we're
                // creating a boxed default(T), the static cctor is called when *any* instance
                // method is invoked.

                Debug.Assert(_pfnAllocator != null);
                Debug.Assert(_pfnCtor != null); // we use null singleton pattern if no ctor call is necessary
            }

            internal bool CtorIsPublic => _ctorIsPublic;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal object? CreateUninitializedObject(RuntimeType rt)
            {
                // We don't use RuntimeType, but we force the caller to pass it so
                // that we can keep it alive on their behalf. Once the object is
                // constructed, we no longer need the reference to the type instance,
                // as the object itself will keep the type alive.

#if DEBUG
                if (_originalRuntimeType != rt)
                {
                    Debug.Fail("Caller passed the wrong RuntimeType to this routine."
                        + Environment.NewLineConst + "Expected: " + (_originalRuntimeType ?? (object)"<null>")
                        + Environment.NewLineConst + "Actual: " + (rt ?? (object)"<null>"));
                }
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
