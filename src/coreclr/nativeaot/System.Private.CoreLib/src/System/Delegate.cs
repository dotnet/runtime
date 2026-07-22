// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

using Internal.Reflection.Augments;
using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

namespace System
{
    public abstract partial class Delegate : ICloneable, ISerializable
    {
        // WARNING: These constants are also declared in System.Private.TypeLoader\Internal\Runtime\TypeLoader\CallConverterThunk.cs
        // Do not change their values without updating the values in the calling convention converter component
        private const int MulticastThunk = 0;
        private const int ClosedStaticThunk = 1;
        private const int OpenStaticThunk = 2;
        private const int ClosedInstanceThunkOverGenericMethod = 3; // This may not exist
        private const int OpenInstanceThunk = 4;        // This may not exist
        private const int ObjectArrayThunk = 5;         // This may not exist

        private object _helperObject;
        private object _target; // Keep _target and _methodPtr next to each other for optimal delegate invoke performance
        private IntPtr _methodPtr;
        private nint _extraFunctionPointerOrData;

        private bool IsDynamicDelegate => GetThunk(MulticastThunk) == IntPtr.Zero;

        public object? Target
        {
            get
            {
                // Multi-cast delegates return the Target of the last delegate in the list
                if (TryGetInvocations(out ReadOnlySpan<Wrapper> invocations))
                {
                    return invocations[^1].Value!.Target;
                }

                // Closed static delegates place a value in _helperObject that they pass to the target method.
                if (_methodPtr == GetThunk(ClosedStaticThunk) ||
                    _methodPtr == GetThunk(ClosedInstanceThunkOverGenericMethod) ||
                    _methodPtr == GetThunk(ObjectArrayThunk))
                    return _helperObject;

                // Other non-closed thunks can be identified as the _target field points at this.
                if (ReferenceEquals(_target, this))
                {
                    return null;
                }

                // NativeFunctionPointerWrapper used by marshalled function pointers is not returned as a public target
                if (_target is NativeFunctionPointerWrapper)
                {
                    return null;
                }

                // Closed instance delegates place a value in _target, and we've ruled out all other types of delegates
                return _target;
            }
        }

        // V1 API: Create closed instance delegates. Method name matching is case sensitive.
        [RequiresUnreferencedCode("The target method might be removed")]
        protected Delegate(object target, string method)
        {
            // This constructor cannot be used by application code. To create a delegate by specifying the name of a method, an
            // overload of the public static CreateDelegate method is used. This will eventually end up calling into the internal
            // implementation of CreateDelegate below, and does not invoke this constructor.
            // The constructor is just for API compatibility with the public contract of the Delegate class.
            throw new PlatformNotSupportedException();
        }

        // V1 API: Create open static delegates. Method name matching is case insensitive.
        protected Delegate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllMethods)] Type target, string method)
        {
            // This constructor cannot be used by application code. To create a delegate by specifying the name of a method, an
            // overload of the public static CreateDelegate method is used. This will eventually end up calling into the internal
            // implementation of CreateDelegate below, and does not invoke this constructor.
            // The constructor is just for API compatibility with the public contract of the Delegate class.
            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetInvocations(out ReadOnlySpan<Wrapper> invocations)
        {
            if (HasSingleTarget)
            {
                invocations = default;
                return false;
            }

            Debug.Assert(_helperObject is Wrapper[]);
            Wrapper[] invocationList = (Wrapper[])_helperObject;

            Debug.Assert(invocationList.Length > 1);
            Debug.Assert((uint)invocationList.Length >= (nuint)_extraFunctionPointerOrData);
            Debug.Assert(invocationList[0].Value is not null);

            invocations = new ReadOnlySpan<Wrapper>(invocationList, 0, (int)_extraFunctionPointerOrData);
            return true;
        }

        //
        // If the thunk does not exist, the function will return IntPtr.Zero.
        private protected virtual IntPtr GetThunk(int whichThunk)
        {
            // NativeAOT doesn't support Universal Shared Code, so let's make this method return null.
            return IntPtr.Zero;
        }

        /// <summary>
        /// The reflection apis use this api to figure out what MethodInfo is related
        /// to a delegate.
        /// </summary>
        /// <param name="typeOfFirstParameterIfInstanceDelegate">
        ///   This value indicates which type an delegate's function pointer is associated with
        ///   This value is ONLY set for delegates where the function pointer points at an instance method
        /// </param>
        /// <param name="isOpenResolver">
        ///   This value indicates if the returned pointer is an open resolver structure.
        /// </param>
        internal unsafe IntPtr GetDelegateLdFtnResult(out RuntimeTypeHandle typeOfFirstParameterIfInstanceDelegate, out bool isOpenResolver)
        {
            typeOfFirstParameterIfInstanceDelegate = default;
            isOpenResolver = false;

            if (_extraFunctionPointerOrData != 0)
            {
                if (GetThunk(OpenInstanceThunk) != _methodPtr)
                {
                    return _extraFunctionPointerOrData;
                }

                typeOfFirstParameterIfInstanceDelegate = ((OpenMethodResolver*)_extraFunctionPointerOrData)->DeclaringType;
                isOpenResolver = true;
                return _extraFunctionPointerOrData;
            }

            if (_target != null)
                typeOfFirstParameterIfInstanceDelegate = new RuntimeTypeHandle(_target.GetMethodTable());

            return _methodPtr;
        }

        // This function is known to the compiler.
        private void InitializeClosedInstance(object firstParameter, IntPtr functionPointer)
        {
            if (firstParameter is null)
                throw new ArgumentException(SR.Arg_DlgtNullInst);

            _methodPtr = functionPointer;
            _target = firstParameter;
        }

        // This function is known to the compiler.
        private void InitializeClosedInstanceSlow(object firstParameter, IntPtr functionPointer)
        {
            // This method is like InitializeClosedInstance, but it handles ALL cases. In particular, it handles generic method with fun function pointers.

            if (firstParameter is null)
                throw new ArgumentException(SR.Arg_DlgtNullInst);

            if (!FunctionPointerOps.IsGenericMethodPointer(functionPointer))
            {
                _methodPtr = functionPointer;
                _target = firstParameter;
            }
            else
            {
                _target = this;
                _methodPtr = GetThunk(ClosedInstanceThunkOverGenericMethod);
                _extraFunctionPointerOrData = functionPointer;
                _helperObject = firstParameter;
            }
        }

        // This function is known to the compiler.
        private void InitializeClosedInstanceWithGVMResolution(object firstParameter, IntPtr dispatchCell)
        {
            if (firstParameter is null)
                throw new NullReferenceException();

            IntPtr functionResolution = RuntimeImports.RhpResolveInterfaceMethod(firstParameter, dispatchCell);

            if (functionResolution == IntPtr.Zero)
            {
                // TODO! What to do when GVM resolution fails. Should never happen
                throw new InvalidOperationException();
            }
            if (!FunctionPointerOps.IsGenericMethodPointer(functionResolution))
            {
                _methodPtr = functionResolution;
                _target = firstParameter;
            }
            else
            {
                _target = this;
                _methodPtr = GetThunk(ClosedInstanceThunkOverGenericMethod);
                _extraFunctionPointerOrData = functionResolution;
                _helperObject = firstParameter;
            }
        }

        // This function is known to the compiler.
        private void InitializeClosedInstanceToInterface(object firstParameter, IntPtr dispatchCell)
        {
            if (firstParameter is null)
                throw new NullReferenceException();

            _methodPtr = RuntimeImports.RhpResolveInterfaceMethod(firstParameter, dispatchCell);
            _target = firstParameter;
        }

        // This is used to implement MethodInfo.CreateDelegate() in a desktop-compatible way. Yes, the desktop really
        // let you use that api to invoke an instance method with a null 'this'.
        private void InitializeClosedInstanceWithoutNullCheck(object firstParameter, IntPtr functionPointer)
        {
            if (!FunctionPointerOps.IsGenericMethodPointer(functionPointer))
            {
                _methodPtr = functionPointer;
                _target = firstParameter;
            }
            else
            {
                _target = this;
                _methodPtr = GetThunk(ClosedInstanceThunkOverGenericMethod);
                _extraFunctionPointerOrData = functionPointer;
                _helperObject = firstParameter;
            }
        }

        // This function is known to the compiler.
        private void InitializeClosedStaticThunk(object firstParameter, IntPtr functionPointer, IntPtr functionPointerThunk)
        {
            _extraFunctionPointerOrData = functionPointer;
            _helperObject = firstParameter;
            _methodPtr = functionPointerThunk;
            _target = this;
        }

        // This function is known to the compiler.
        private void InitializeOpenStaticThunk(object _ /*firstParameter*/, IntPtr functionPointer, IntPtr functionPointerThunk)
        {
            // This sort of delegate is invoked by calling the thunk function pointer with the arguments to the delegate + a reference to the delegate object itself.
            _target = this;
            _methodPtr = functionPointerThunk;
            _extraFunctionPointerOrData = functionPointer;
        }

        private void InitializeOpenInstanceThunkDynamic(IntPtr functionPointer, IntPtr functionPointerThunk)
        {
            // This sort of delegate is invoked by calling the thunk function pointer with the arguments to the delegate + a reference to the delegate object itself.
            _target = this;
            _methodPtr = functionPointerThunk;
            _extraFunctionPointerOrData = functionPointer;
        }

        // This function is only ever called by the open instance method thunk, and in that case,
        // _extraFunctionPointerOrData always points to an OpenMethodResolver
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IntPtr GetActualTargetFunctionPointer(object thisObject)
        {
            return OpenMethodResolver.ResolveMethod(_extraFunctionPointerOrData, thisObject);
        }
                
        [DebuggerGuidedStepThroughAttribute]
        protected virtual object? DynamicInvokeImpl(object?[]? args)
        {
            DynamicInvokeInfo dynamicInvokeInfo = ReflectionAugments.GetDelegateDynamicInvokeInfo(GetType());

            object? result = dynamicInvokeInfo.Invoke(_target, _methodPtr,
                args, binderBundle: null, wrapInTargetInvocationException: true);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        protected virtual MethodInfo GetMethodImpl()
        {
            // NOTE: this implementation is mirrored in GetDiagnosticMethodInfo below

            // Multi-cast delegates return the Method of the last delegate in the list
            if (TryGetInvocations(out ReadOnlySpan<Wrapper> invocations))
            {
                return invocations[^1].Value!.GetMethodImpl();
            }

            // Return the delegate Invoke method for marshalled function pointers and LINQ expressions
            if (_target is NativeFunctionPointerWrapper || _methodPtr == GetThunk(ObjectArrayThunk))
            {
                return GetInvokeMethod(GetType());
            }

            return ReflectionAugments.GetDelegateMethod(this);
        }

        internal DiagnosticMethodInfo GetDiagnosticMethodInfo()
        {
            // NOTE: this implementation is mirrored in GetMethodImpl above

            // Multi-cast delegates return the diagnostic method info of the last delegate in the list
            if (TryGetInvocations(out ReadOnlySpan<Wrapper> invocations))
            {
                return invocations[^1].Value!.GetDiagnosticMethodInfo();
            }

            // Return the delegate Invoke method for marshalled function pointers and LINQ expressions
            if (_target is NativeFunctionPointerWrapper || _methodPtr == GetThunk(ObjectArrayThunk))
            {
                Type t = GetType();
                return new DiagnosticMethodInfo("Invoke", t.FullName, t.Module.Assembly.FullName);
            }

            IntPtr ldftnResult = GetDelegateLdFtnResult(out RuntimeTypeHandle _, out bool isOpenResolver);
            if (isOpenResolver)
            {
                MethodInfo mi = ReflectionAugments.GetDelegateMethod(this);
                Type? declaringType = mi.DeclaringType;
                if (declaringType.IsConstructedGenericType)
                    declaringType = declaringType.GetGenericTypeDefinition();
                return new DiagnosticMethodInfo(mi.Name, declaringType.FullName, mi.Module.Assembly.FullName);
            }

            IntPtr functionPointer;
            if (FunctionPointerOps.IsGenericMethodPointer(ldftnResult))
            {
                unsafe
                {
                    GenericMethodDescriptor* realTargetData = FunctionPointerOps.ConvertToGenericDescriptor(ldftnResult);
                    functionPointer = RuntimeAugments.GetCodeTarget(realTargetData->MethodFunctionPointer);
                }
            }
            else
            {
                nint unboxedPointer = RuntimeAugments.GetCodeTarget(ldftnResult);
                if (unboxedPointer == ldftnResult)
                    unboxedPointer = RuntimeAugments.GetTargetOfUnboxingAndInstantiatingStub(ldftnResult);

                functionPointer = unboxedPointer != 0 ? unboxedPointer : ldftnResult;
            }
            return RuntimeAugments.StackTraceCallbacksIfAvailable?.TryGetDiagnosticMethodInfoFromStartAddress(functionPointer);
        }

        // V2 api: Creates open or closed delegates to static or instance methods - relaxed signature checking allowed.
        public static Delegate CreateDelegate(Type type, object? firstArgument, MethodInfo method, bool throwOnBindFailure) =>
            ReflectionAugments.CreateDelegate(type, firstArgument, method, throwOnBindFailure);

        // V1 api: Creates open delegates to static or instance methods - relaxed signature checking allowed.
        public static Delegate CreateDelegate(Type type, MethodInfo method, bool throwOnBindFailure) =>
            ReflectionAugments.CreateDelegate(type, method, throwOnBindFailure);

        // V1 api: Creates closed delegates to instance methods only, relaxed signature checking disallowed.
        [RequiresUnreferencedCode("The target method might be removed")]
        public static Delegate CreateDelegate(Type type, object target, string method, bool ignoreCase, bool throwOnBindFailure) =>
            ReflectionAugments.CreateDelegate(type, target, method, ignoreCase, throwOnBindFailure);

        // V1 api: Creates open delegates to static methods only, relaxed signature checking disallowed.
        public static Delegate CreateDelegate(Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllMethods)] Type target, string method, bool ignoreCase, bool throwOnBindFailure) =>
            ReflectionAugments.CreateDelegate(type, target, method, ignoreCase, throwOnBindFailure);

        internal IntPtr TryGetOpenStaticFunctionPointer() => GetThunk(OpenStaticThunk) == _methodPtr ? _extraFunctionPointerOrData : 0;

        internal NativeFunctionPointerWrapper? TryGetNativeFunctionPointerWrapper() => _target as NativeFunctionPointerWrapper;

        // Returns a new delegate of the specified type whose implementation is provided by the
        // provided delegate.
        internal static unsafe Delegate CreateObjectArrayDelegate(Type t, Func<object?[], object?> handler)
        {
            RuntimeTypeHandle typeHandle = t.TypeHandle;

            MethodTable* delegateEEType = typeHandle.ToMethodTable();
            Debug.Assert(delegateEEType != null);
            Debug.Assert(delegateEEType->IsCanonical);

            RuntimeAugments.EnsureMethodTableSafeToAllocate(delegateEEType);

            Delegate del = (Delegate)RuntimeImports.RhNewObject(delegateEEType);

            IntPtr objArrayThunk = del.GetThunk(ObjectArrayThunk);
            if (objArrayThunk == IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            del._helperObject = handler;
            del._methodPtr = objArrayThunk;
            del._target = del;
            return del;
        }

        //
        // Internal (and quite unsafe) helper to create delegates of an arbitrary type. This is used to support Reflection invoke.
        //
        // Note that delegates constructed the normal way do not come through here. The IL transformer generates the equivalent of
        // this code customized for each delegate type.
        //
        internal static unsafe Delegate CreateDelegate(MethodTable* delegateEEType, IntPtr ldftnResult, object thisObject, bool isStatic, bool isOpen)
        {
            RuntimeAugments.EnsureMethodTableSafeToAllocate(delegateEEType);

            Delegate del = (Delegate)RuntimeImports.RhNewObject(delegateEEType);

            // What? No constructor call? That's right, and it's not an oversight. All "construction" work happens in
            // the Initialize() methods. This helper has a hard dependency on this invariant.

            if (isStatic)
            {
                if (isOpen)
                {
                    IntPtr thunk = del.GetThunk(OpenStaticThunk);
                    del.InitializeOpenStaticThunk(null, ldftnResult, thunk);
                }
                else
                {
                    IntPtr thunk = del.GetThunk(ClosedStaticThunk);
                    del.InitializeClosedStaticThunk(thisObject, ldftnResult, thunk);
                }
            }
            else
            {
                if (isOpen)
                {
                    IntPtr thunk = del.GetThunk(OpenInstanceThunk);
                    del.InitializeOpenInstanceThunkDynamic(ldftnResult, thunk);
                }
                else
                {
                    del.InitializeClosedInstanceWithoutNullCheck(thisObject, ldftnResult);
                }
            }
            return del;
        }

        private unsafe Delegate NewMulticastDelegate(Wrapper[] invocationList, int invocationCount, bool thisIsMultiCastAlready = false)
        {
            // First, allocate a new delegate just like this one, i.e. same type as the this object
            Delegate result = Unsafe.As<Delegate>(RuntimeImports.RhNewObject(this.GetMethodTable()));

            // Performance optimization - if this already points to a true multicast delegate,
            // copy _methodPtr field rather than calling GetThunk to get it
            result._methodPtr = thisIsMultiCastAlready ? _methodPtr : GetThunk(MulticastThunk);
            result._target = result;
            result._helperObject = invocationList;
            result._extraFunctionPointerOrData = invocationCount;

            return result;
        }

        private static bool SlotEquals(Delegate previous, Delegate o) =>
            ReferenceEquals(previous._target, o._target) &&
            ReferenceEquals(previous._helperObject, o._helperObject) &&
            previous._extraFunctionPointerOrData == o._extraFunctionPointerOrData &&
            previous._methodPtr == o._methodPtr;

        private bool EqualsCore(Delegate other)
        {
            Debug.Assert(RuntimeHelpers.TypeEquivalent(this, other));

            if (TryGetInvocations(out ReadOnlySpan<Wrapper> invocations))
                return other.TryGetInvocations(out ReadOnlySpan<Wrapper> otherInvocations) && invocations.SequenceEqual(otherInvocations);

            if (_target is NativeFunctionPointerWrapper nativeFunctionPointerWrapper)
            {
                if (other._target is not NativeFunctionPointerWrapper dnativeFunctionPointerWrapper)
                    return false;

                return nativeFunctionPointerWrapper.NativeFunctionPointer == dnativeFunctionPointerWrapper.NativeFunctionPointer;
            }

            if (!ReferenceEquals(_helperObject, other._helperObject) ||
                !FunctionPointerOps.Compare(_extraFunctionPointerOrData, other._extraFunctionPointerOrData) ||
                !FunctionPointerOps.Compare(_methodPtr, other._methodPtr))
            {
                return false;
            }

            // Those delegate kinds with thunks put themselves into the _target, so we can't
            // blindly compare the _target fields for equality.
            return ReferenceEquals(ReferenceEquals(_target, this) ? other : _target, other._target);
        }

        public sealed override unsafe int GetHashCode()
        {
            if (TryGetInvocations(out ReadOnlySpan<Wrapper> invocations))
            {
                int hash = 0;
                foreach (ref readonly Wrapper wrapper in invocations)
                {
                    hash = hash * 33 + wrapper.GetHashCode();
                }
                return hash;
            }

            MethodTable* methodTable = this.GetMethodTable();
            if (_target is NativeFunctionPointerWrapper nativeFunctionPointerWrapper)
            {
                return HashCode.Combine((nuint)methodTable, nativeFunctionPointerWrapper.NativeFunctionPointer);
            }

            int hashCode = HashCode.Combine((nuint)methodTable,
                RuntimeHelpers.GetHashCode(_helperObject),
                FunctionPointerOps.GetHashCode(_extraFunctionPointerOrData),
                FunctionPointerOps.GetHashCode(_methodPtr));
            if (!ReferenceEquals(_target, this))
            {
                hashCode += RuntimeHelpers.GetHashCode(_target) * 33;
            }
            return hashCode;
        }
    }
}
