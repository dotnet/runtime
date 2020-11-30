// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// A factory which allows optimizing <see cref="Activator.CreateInstance(Type)"/>,
    /// <see cref="Activator.CreateFactory(Type)"/>, and related APIs.
    /// Requires a parameterless ctor (public or non-public).
    /// </summary>
    internal unsafe sealed class ActivationFactory
    {
        // The managed calli to the newobj allocator, plus its first argument (MethodTable*).
        // In the case of the COM allocator, first arg is ComClassFactory*, not MethodTable*.
        private readonly delegate*<void*, object?> _pfnAllocator;
        private readonly void* _allocatorFirstArg;

        // The managed calli to the parameterless ctor, taking "this" (as object) as its first argument.
        // mgd sig: object -> void
        private readonly delegate*<object?, void> _pfnCtor;
        private readonly bool _ctorIsPublic;

        private readonly RuntimeType _originalRuntimeType;

        internal ActivationFactory(RuntimeType rt)
        {
            Debug.Assert(rt != null);

            _originalRuntimeType = rt;

            // The check below is redundant since these same checks are performed at the
            // unmanaged layer, but this call will throw slightly different exceptions
            // than the unmanaged layer, and callers might be dependent on this.

            rt.CreateInstanceCheckThis();

            try
            {
                RuntimeTypeHandle.GetActivationInfo(rt,
                    out _pfnAllocator!, out _allocatorFirstArg,
                    out _pfnCtor!, out _ctorIsPublic);
            }
            catch (Exception ex)
            {
                TryThrowFriendlyException(_originalRuntimeType, ex);
                throw; // can't make a friendlier message, rethrow original exception
            }

            // Activator.CreateInstance returns null given typeof(Nullable<T>).

            if (_pfnAllocator == null)
            {
                Debug.Assert(Nullable.GetUnderlyingType(rt) != null,
                    "Null allocator should only be returned for Nullable<T>.");

                static object? ReturnNull(void* _) => null;
                _pfnAllocator = &ReturnNull;
            }

            // If no ctor is provided, we have Nullable<T>, a ctorless value type T,
            // or a ctorless __ComObject. In any case, we should replace the
            // ctor call with our no-op stub. The unmanaged GetActivationInfo layer
            // would have thrown an exception if 'rt' were a normal reference type
            // without a ctor.

            if (_pfnCtor == null)
            {
                static void CtorNoopStub(object? uninitializedObject) { }
                _pfnCtor = &CtorNoopStub; // we use null singleton pattern if no ctor call is necessary

                Debug.Assert(_ctorIsPublic); // implicit parameterless ctor is always considered public
            }

            // We don't need to worry about invoking cctors here. The runtime will figure it
            // out for us when the instance ctor is called. For value types, because we're
            // creating a boxed default(T), the static cctor is called when *any* instance
            // method is invoked.
        }

        internal bool CtorIsPublic => _ctorIsPublic;

        /// <summary>
        /// Calls the default ctor over an existing zero-inited instance of the target type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CallConstructor(object? uninitializedObject) => _pfnCtor(uninitializedObject);

        internal Delegate CreateDelegate(RuntimeType delegateType)
        {
            Debug.Assert(delegateType is not null);

            // We only allow Func<object> and Func<T> (the latter only for reference types).
            // This could probably be a Debug.Assert instead of a runtime check, but it's not *that*
            // expensive in the grand scheme of things, so may as well do it.

            if (delegateType != typeof(Func<object?>))
            {
                if (_originalRuntimeType.IsValueType || delegateType != typeof(Func<>).MakeGenericType(_originalRuntimeType))
                {
                    Debug.Fail($"Caller provided an unexpected RuntimeType: {delegateType}");
                    Environment.FailFast("Potential type safety violation in ActivationFactory.");
                }
            }

            return Delegate.CreateDelegateUnsafe(delegateType, this, (IntPtr)(delegate*<ActivationFactory, object?>)&CreateInstance);
        }

        /// <summary>
        /// Constructs a new instance of the target type, including calling the default ctor if needed.
        /// </summary>
        private static object? CreateInstance(ActivationFactory @this)
        {
            object? newObj = @this.GetUninitializedObject();
            @this.CallConstructor(newObj);
            return newObj;
        }

        /// <summary>
        /// Validates that this instance is a factory for the type desired by the caller.
        /// </summary>
        [Conditional("DEBUG")]
        internal void DebugValidateExpectedType(RuntimeType rt)
        {
            if (_originalRuntimeType != rt)
            {
                Debug.Fail("Caller passed the wrong RuntimeType to this routine."
                    + Environment.NewLineConst + "Expected: " + (_originalRuntimeType ?? (object)"<null>")
                    + Environment.NewLineConst + "Actual: " + (rt ?? (object)"<null>"));
            }
        }

        /// <summary>
        /// Allocates a new zero-inited instance of the target type, but does not run any ctors.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal object? GetUninitializedObject()
        {
            object? retVal = _pfnAllocator(_allocatorFirstArg);
            GC.KeepAlive(this); // roots RuntimeType until allocation is completed
            return retVal;
        }

        [StackTraceHidden]
        internal static void TryThrowFriendlyException(RuntimeType rt, Exception ex)
        {
            // Exception messages coming from the runtime won't include
            // the type name. Let's include it here to improve the
            // debugging experience for our callers.

            string friendlyMessage = SR.Format(SR.Activator_CannotCreateInstance, rt, ex.Message);
            switch (ex)
            {
                case ArgumentException: throw new ArgumentException(friendlyMessage);
                case PlatformNotSupportedException: throw new PlatformNotSupportedException(friendlyMessage);
                case NotSupportedException: throw new NotSupportedException(friendlyMessage);
                case MethodAccessException: throw new MethodAccessException(friendlyMessage);
                case MissingMethodException: throw new MissingMethodException(friendlyMessage);
                case MemberAccessException: throw new MemberAccessException(friendlyMessage);
            }
        }
    }

    /// <summary>
    /// An <see cref="ActivationFactory"/> geared toward structs.
    /// Requires no parameterless ctor or a public parameterless ctor.
    /// </summary>
    internal unsafe sealed class ActivationFactory<T> : IActivationFactory where T : struct
    {
        private readonly delegate*<ref T, void> _pfnCtor; // may be populated by CreateDelegate method

        public ActivationFactory()
        {
            delegate*<ref byte, void> pfnCtor;
            bool ctorIsPublic;

            try
            {
                RuntimeTypeHandle.GetActivationInfoForStruct((RuntimeType)typeof(T), out pfnCtor, out ctorIsPublic);
            }
            catch (Exception ex)
            {
                ActivationFactory.TryThrowFriendlyException((RuntimeType)typeof(T), ex);
                throw; // can't make a friendlier message, rethrow original exception
            }

            if (pfnCtor != null && !ctorIsPublic)
            {
                throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, (RuntimeType)typeof(T)));
            }

            _pfnCtor = (delegate*<ref T, void>)pfnCtor;
        }

        private T CreateNoCtor()
        {
            Debug.Assert(_pfnCtor == null);
            return default;
        }

        private T CreateWithCtor()
        {
            Debug.Assert(_pfnCtor != null);
            T newObj = default;
            _pfnCtor(ref newObj);
            return newObj;
        }

        Delegate IActivationFactory.GetCreateInstanceDelegate()
          => (_pfnCtor == null) ? (Func<T>)CreateNoCtor : (Func<T>)CreateWithCtor;
    }

    internal interface IActivationFactory
    {
        Delegate GetCreateInstanceDelegate();
    }
}
