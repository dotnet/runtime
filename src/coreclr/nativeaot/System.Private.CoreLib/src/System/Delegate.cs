// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

using Internal.Reflection.Augments;
using Internal.Runtime;
using Internal.Runtime.CompilerServices;

namespace System
{
    public abstract partial class Delegate : ICloneable, ISerializable
    {
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
        protected Delegate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type target, string method)
        {
            // This constructor cannot be used by application code. To create a delegate by specifying the name of a method, an
            // overload of the public static CreateDelegate method is used. This will eventually end up calling into the internal
            // implementation of CreateDelegate below, and does not invoke this constructor.
            // The constructor is just for API compatibility with the public contract of the Delegate class.
            throw new PlatformNotSupportedException();
        }

        // New Delegate Implementation

        private object _firstParameter;
        private object _helperObject;
        private nint _extraFunctionPointerOrData;
        private IntPtr _functionPointer;

        // _helperObject may point to an array of delegates if this is a multicast delegate. We use this wrapper to distinguish between
        // our own array of delegates and user provided Wrapper[]. As a added benefit, this wrapper also eliminates array co-variance
        // overhead for our own array of delegates.
        private struct Wrapper
        {
            public Wrapper(Delegate value) => Value = value;
            public Delegate Value;
        }

        // WARNING: These constants are also declared in System.Private.TypeLoader\Internal\Runtime\TypeLoader\CallConverterThunk.cs
        // Do not change their values without updating the values in the calling convention converter component
        private protected const int MulticastThunk = 0;
        private protected const int ClosedStaticThunk = 1;
        private protected const int OpenStaticThunk = 2;
        private protected const int ClosedInstanceThunkOverGenericMethod = 3; // This may not exist
        private protected const int OpenInstanceThunk = 4;        // This may not exist
        private protected const int ObjectArrayThunk = 5;         // This may not exist

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
            typeOfFirstParameterIfInstanceDelegate = default(RuntimeTypeHandle);
            isOpenResolver = false;

            if (_extraFunctionPointerOrData != 0)
            {
                if (GetThunk(OpenInstanceThunk) == _functionPointer)
                {
                    typeOfFirstParameterIfInstanceDelegate = ((OpenMethodResolver*)_extraFunctionPointerOrData)->DeclaringType;
                    isOpenResolver = true;
                }
                return _extraFunctionPointerOrData;
            }
            else
            {
                if (_firstParameter != null)
                    typeOfFirstParameterIfInstanceDelegate = new RuntimeTypeHandle(_firstParameter.GetMethodTable());

                return _functionPointer;
            }
        }

        // This function is known to the compiler.
        private void InitializeClosedInstance(object firstParameter, IntPtr functionPointer)
        {
            if (firstParameter is null)
                throw new ArgumentException(SR.Arg_DlgtNullInst);

            _functionPointer = functionPointer;
            _firstParameter = firstParameter;
        }

        // This function is known to the compiler.
        private void InitializeClosedInstanceSlow(object firstParameter, IntPtr functionPointer)
        {
            // This method is like InitializeClosedInstance, but it handles ALL cases. In particular, it handles generic method with fun function pointers.

            if (firstParameter is null)
                throw new ArgumentException(SR.Arg_DlgtNullInst);

            if (!FunctionPointerOps.IsGenericMethodPointer(functionPointer))
            {
                _functionPointer = functionPointer;
                _firstParameter = firstParameter;
            }
            else
            {
                _firstParameter = this;
                _functionPointer = GetThunk(ClosedInstanceThunkOverGenericMethod);
                _extraFunctionPointerOrData = functionPointer;
                _helperObject = firstParameter;
            }
        }

        // This function is known to the compiler.
        private void InitializeClosedInstanceWithGVMResolution(object firstParameter, RuntimeMethodHandle tokenOfGenericVirtualMethod)
        {
            if (firstParameter is null)
                throw new NullReferenceException();

            IntPtr functionResolution = TypeLoaderExports.GVMLookupForSlot(firstParameter, tokenOfGenericVirtualMethod);

            if (functionResolution == IntPtr.Zero)
            {
                // TODO! What to do when GVM resolution fails. Should never happen
                throw new InvalidOperationException();
            }
            if (!FunctionPointerOps.IsGenericMethodPointer(functionResolution))
            {
                _functionPointer = functionResolution;
                _firstParameter = firstParameter;
            }
            else
            {
                _firstParameter = this;
                _functionPointer = GetThunk(ClosedInstanceThunkOverGenericMethod);
                _extraFunctionPointerOrData = functionResolution;
                _helperObject = firstParameter;
            }

            return;
        }

        // This function is known to the compiler.
        private void InitializeClosedInstanceToInterface(object firstParameter, IntPtr dispatchCell)
        {
            if (firstParameter is null)
                throw new NullReferenceException();

            _functionPointer = RuntimeImports.RhpResolveInterfaceMethod(firstParameter, dispatchCell);
            _firstParameter = firstParameter;
        }

        // This is used to implement MethodInfo.CreateDelegate() in a desktop-compatible way. Yes, the desktop really
        // let you use that api to invoke an instance method with a null 'this'.
        private void InitializeClosedInstanceWithoutNullCheck(object firstParameter, IntPtr functionPointer)
        {
            if (!FunctionPointerOps.IsGenericMethodPointer(functionPointer))
            {
                _functionPointer = functionPointer;
                _firstParameter = firstParameter;
            }
            else
            {
                _firstParameter = this;
                _functionPointer = GetThunk(ClosedInstanceThunkOverGenericMethod);
                _extraFunctionPointerOrData = functionPointer;
                _helperObject = firstParameter;
            }
        }

        // This function is known to the compiler.
        private void InitializeClosedStaticThunk(object firstParameter, IntPtr functionPointer, IntPtr functionPointerThunk)
        {
            _extraFunctionPointerOrData = functionPointer;
            _helperObject = firstParameter;
            _functionPointer = functionPointerThunk;
            _firstParameter = this;
        }

        // This function is known to the compiler.
        private void InitializeOpenStaticThunk(object _ /*firstParameter*/, IntPtr functionPointer, IntPtr functionPointerThunk)
        {
            // This sort of delegate is invoked by calling the thunk function pointer with the arguments to the delegate + a reference to the delegate object itself.
            _firstParameter = this;
            _functionPointer = functionPointerThunk;
            _extraFunctionPointerOrData = functionPointer;
        }

        private void InitializeOpenInstanceThunkDynamic(IntPtr functionPointer, IntPtr functionPointerThunk)
        {
            // This sort of delegate is invoked by calling the thunk function pointer with the arguments to the delegate + a reference to the delegate object itself.
            _firstParameter = this;
            _functionPointer = functionPointerThunk;
            _extraFunctionPointerOrData = functionPointer;
        }

        // This function is only ever called by the open instance method thunk, and in that case,
        // _extraFunctionPointerOrData always points to an OpenMethodResolver
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IntPtr GetActualTargetFunctionPointer(object thisObject)
        {
            return OpenMethodResolver.ResolveMethod(_extraFunctionPointerOrData, thisObject);
        }

        internal bool IsDynamicDelegate() => GetThunk(MulticastThunk) == IntPtr.Zero;

        [DebuggerGuidedStepThroughAttribute]
        protected virtual object? DynamicInvokeImpl(object?[]? args)
        {
            if (IsDynamicDelegate())
            {
                // DynamicDelegate case
                object? result = ((Func<object?[]?, object?>)_helperObject)(args);
                DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                return result;
            }
            else
            {
                DynamicInvokeInfo dynamicInvokeInfo = ReflectionAugments.ReflectionCoreCallbacks.GetDelegateDynamicInvokeInfo(GetType());

                object? result = dynamicInvokeInfo.Invoke(_firstParameter, _functionPointer,
                    args, binderBundle: null, wrapInTargetInvocationException: true);
                DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                return result;
            }
        }

        protected virtual MethodInfo GetMethodImpl()
        {
            // Multi-cast delegates return the Method of the last delegate in the list
            if (_helperObject is Wrapper[] invocationList)
            {
                int invocationCount = (int)_extraFunctionPointerOrData;
                return invocationList[invocationCount - 1].Value.GetMethodImpl();
            }

            // Return the delegate Invoke method for marshalled function pointers and LINQ expressions
            if ((_firstParameter is NativeFunctionPointerWrapper) || (_functionPointer == GetThunk(ObjectArrayThunk)))
            {
                return GetType().GetMethod("Invoke");
            }

            return ReflectionAugments.ReflectionCoreCallbacks.GetDelegateMethod(this);
        }

        public object? Target
        {
            get
            {
                // Multi-cast delegates return the Target of the last delegate in the list
                if (_helperObject is Wrapper[] invocationList)
                {
                    int invocationCount = (int)_extraFunctionPointerOrData;
                    return invocationList[invocationCount - 1].Value.Target;
                }

                // Closed static delegates place a value in _helperObject that they pass to the target method.
                if (_functionPointer == GetThunk(ClosedStaticThunk) ||
                    _functionPointer == GetThunk(ClosedInstanceThunkOverGenericMethod) ||
                    _functionPointer == GetThunk(ObjectArrayThunk))
                    return _helperObject;

                // Other non-closed thunks can be identified as the _firstParameter field points at this.
                if (object.ReferenceEquals(_firstParameter, this))
                {
                    return null;
                }

                // NativeFunctionPointerWrapper used by marshalled function pointers is not returned as a public target
                if (_firstParameter is NativeFunctionPointerWrapper)
                {
                    return null;
                }

                // Closed instance delegates place a value in _firstParameter, and we've ruled out all other types of delegates
                return _firstParameter;
            }
        }

        // V2 api: Creates open or closed delegates to static or instance methods - relaxed signature checking allowed.
        public static Delegate CreateDelegate(Type type, object? firstArgument, MethodInfo method, bool throwOnBindFailure) => ReflectionAugments.ReflectionCoreCallbacks.CreateDelegate(type, firstArgument, method, throwOnBindFailure);

        // V1 api: Creates open delegates to static or instance methods - relaxed signature checking allowed.
        public static Delegate CreateDelegate(Type type, MethodInfo method, bool throwOnBindFailure) => ReflectionAugments.ReflectionCoreCallbacks.CreateDelegate(type, method, throwOnBindFailure);

        // V1 api: Creates closed delegates to instance methods only, relaxed signature checking disallowed.
        [RequiresUnreferencedCode("The target method might be removed")]
        public static Delegate CreateDelegate(Type type, object target, string method, bool ignoreCase, bool throwOnBindFailure) => ReflectionAugments.ReflectionCoreCallbacks.CreateDelegate(type, target, method, ignoreCase, throwOnBindFailure);

        // V1 api: Creates open delegates to static methods only, relaxed signature checking disallowed.
        public static Delegate CreateDelegate(Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type target, string method, bool ignoreCase, bool throwOnBindFailure) => ReflectionAugments.ReflectionCoreCallbacks.CreateDelegate(type, target, method, ignoreCase, throwOnBindFailure);

        internal IntPtr TryGetOpenStaticFunctionPointer() => (GetThunk(OpenStaticThunk) == _functionPointer) ? _extraFunctionPointerOrData : 0;

        internal NativeFunctionPointerWrapper? TryGetNativeFunctionPointerWrapper() => _firstParameter as NativeFunctionPointerWrapper;

        internal static unsafe bool InternalEqualTypes(object a, object b)
        {
            return a.GetMethodTable() == b.GetMethodTable();
        }

        // Returns a new delegate of the specified type whose implementation is provided by the
        // provided delegate.
        internal static unsafe Delegate CreateObjectArrayDelegate(Type t, Func<object?[], object?> handler)
        {
            RuntimeTypeHandle typeHandle = t.TypeHandle;

            MethodTable* delegateEEType = typeHandle.ToMethodTable();
            Debug.Assert(delegateEEType != null);
            Debug.Assert(delegateEEType->IsCanonical);

            Delegate del = (Delegate)(RuntimeImports.RhNewObject(delegateEEType));

            IntPtr objArrayThunk = del.GetThunk(Delegate.ObjectArrayThunk);
            if (objArrayThunk == IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            del._helperObject = handler;
            del._functionPointer = objArrayThunk;
            del._firstParameter = del;
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
            Delegate del = (Delegate)RuntimeImports.RhNewObject(delegateEEType);

            // What? No constructor call? That's right, and it's not an oversight. All "construction" work happens in
            // the Initialize() methods. This helper has a hard dependency on this invariant.

            if (isStatic)
            {
                if (isOpen)
                {
                    IntPtr thunk = del.GetThunk(Delegate.OpenStaticThunk);
                    del.InitializeOpenStaticThunk(null, ldftnResult, thunk);
                }
                else
                {
                    IntPtr thunk = del.GetThunk(Delegate.ClosedStaticThunk);
                    del.InitializeClosedStaticThunk(thisObject, ldftnResult, thunk);
                }
            }
            else
            {
                if (isOpen)
                {
                    IntPtr thunk = del.GetThunk(Delegate.OpenInstanceThunk);
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
            // copy _functionPointer field rather than calling GetThunk to get it
            result._functionPointer = thisIsMultiCastAlready ? _functionPointer : GetThunk(MulticastThunk);
            result._firstParameter = result;
            result._helperObject = invocationList;
            result._extraFunctionPointerOrData = (IntPtr)invocationCount;

            return result;
        }

        private static bool TrySetSlot(Wrapper[] a, int index, Delegate o)
        {
            if (a[index].Value == null && System.Threading.Interlocked.CompareExchange(ref a[index].Value, o, null) == null)
                return true;

            // The slot may be already set because we have added and removed the same method before.
            // Optimize this case, because it's cheaper than copying the array.
            if (a[index].Value is Delegate dd)
            {
                if (object.ReferenceEquals(dd._firstParameter, o._firstParameter) &&
                    object.ReferenceEquals(dd._helperObject, o._helperObject) &&
                    dd._extraFunctionPointerOrData == o._extraFunctionPointerOrData &&
                    dd._functionPointer == o._functionPointer)
                {
                    return true;
                }
            }
            return false;
        }

        // This method will combine this delegate with the passed delegate
        //  to form a new delegate.
        protected virtual Delegate CombineImpl(Delegate? d)
        {
            if (d is null)
                return this;

            // Verify that the types are the same...
            if (!InternalEqualTypes(this, d))
                throw new ArgumentException(SR.Arg_DlgtTypeMis);

            if (IsDynamicDelegate())
                throw new InvalidOperationException();

            int followCount = 1;
            Wrapper[]? followList = d._helperObject as Wrapper[];
            if (followList != null)
                followCount = (int)d._extraFunctionPointerOrData;

            int resultCount;
            Wrapper[]? resultList;
            if (_helperObject is not Wrapper[] invocationList)
            {
                resultCount = 1 + followCount;
                resultList = new Wrapper[resultCount];
                resultList[0] = new Wrapper(this);
                if (followList == null)
                {
                    resultList[1] = new Wrapper(d);
                }
                else
                {
                    for (int i = 0; i < followCount; i++)
                        resultList[1 + i] = followList[i];
                }
                return NewMulticastDelegate(resultList, resultCount);
            }
            else
            {
                int invocationCount = (int)_extraFunctionPointerOrData;
                resultCount = invocationCount + followCount;
                resultList = null;
                if (resultCount <= invocationList.Length)
                {
                    resultList = invocationList;
                    if (followList == null)
                    {
                        if (!TrySetSlot(resultList, invocationCount, d))
                            resultList = null;
                    }
                    else
                    {
                        for (int i = 0; i < followCount; i++)
                        {
                            if (!TrySetSlot(resultList, invocationCount + i, followList[i].Value))
                            {
                                resultList = null;
                                break;
                            }
                        }
                    }
                }

                if (resultList == null)
                {
                    int allocCount = invocationList.Length;
                    while (allocCount < resultCount)
                        allocCount *= 2;

                    resultList = new Wrapper[allocCount];

                    for (int i = 0; i < invocationCount; i++)
                        resultList[i] = invocationList[i];

                    if (followList == null)
                    {
                        resultList[invocationCount] = new Wrapper(d);
                    }
                    else
                    {
                        for (int i = 0; i < followCount; i++)
                            resultList[invocationCount + i] = followList[i];
                    }
                }
                return NewMulticastDelegate(resultList, resultCount, true);
            }
        }

        private static Wrapper[] DeleteFromInvocationList(Wrapper[] invocationList, int invocationCount, int deleteIndex, int deleteCount)
        {
            int allocCount = invocationList.Length;
            while (allocCount / 2 >= invocationCount - deleteCount)
                allocCount /= 2;

            Wrapper[] newInvocationList = new Wrapper[allocCount];

            for (int i = 0; i < deleteIndex; i++)
                newInvocationList[i] = invocationList[i];

            for (int i = deleteIndex + deleteCount; i < invocationCount; i++)
                newInvocationList[i - deleteCount] = invocationList[i];

            return newInvocationList;
        }

        private static bool EqualInvocationLists(Wrapper[] a, Wrapper[] b, int start, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!(a[start + i].Value.Equals(b[i].Value)))
                    return false;
            }
            return true;
        }

        // This method currently looks backward on the invocation list
        //  for an element that has Delegate based equality with value.  (Doesn't
        //  look at the invocation list.)  If this is found we remove it from
        //  this list and return a new delegate.  If its not found a copy of the
        //  current list is returned.
        protected virtual Delegate? RemoveImpl(Delegate d)
        {
            // There is a special case were we are removing using a delegate as
            //    the value we need to check for this case
            //
            if (d is null)
                return this;
            if (d._helperObject is not Wrapper[] dInvocationList)
            {
                if (_helperObject is not Wrapper[] invocationList)
                {
                    // they are both not real Multicast
                    if (this.Equals(d))
                        return null;
                }
                else
                {
                    int invocationCount = (int)_extraFunctionPointerOrData;
                    for (int i = invocationCount; --i >= 0;)
                    {
                        if (d.Equals(invocationList[i]))
                        {
                            if (invocationCount == 2)
                            {
                                // Special case - only one value left, either at the beginning or the end
                                return invocationList[1 - i].Value;
                            }
                            else
                            {
                                Wrapper[] list = DeleteFromInvocationList(invocationList, invocationCount, i, 1);
                                return NewMulticastDelegate(list, invocationCount - 1, true);
                            }
                        }
                    }
                }
            }
            else
            {
                if (_helperObject is Wrapper[] invocationList)
                {
                    int invocationCount = (int)_extraFunctionPointerOrData;
                    int dInvocationCount = (int)d._extraFunctionPointerOrData;
                    for (int i = invocationCount - dInvocationCount; i >= 0; i--)
                    {
                        if (EqualInvocationLists(invocationList, dInvocationList, i, dInvocationCount))
                        {
                            if (invocationCount - dInvocationCount == 0)
                            {
                                // Special case - no values left
                                return null;
                            }
                            else if (invocationCount - dInvocationCount == 1)
                            {
                                // Special case - only one value left, either at the beginning or the end
                                return invocationList[i != 0 ? 0 : invocationCount - 1].Value;
                            }
                            else
                            {
                                Wrapper[] list = DeleteFromInvocationList(invocationList, invocationCount, i, dInvocationCount);
                                return NewMulticastDelegate(list, invocationCount - dInvocationCount, true);
                            }
                        }
                    }
                }
            }

            return this;
        }

        public virtual Delegate[] GetInvocationList()
        {
            if (_helperObject is Wrapper[] invocationList)
            {
                // Create an array of delegate copies and each
                //    element into the array
                int invocationCount = (int)_extraFunctionPointerOrData;

                var del = new Delegate[invocationCount];
                for (int i = 0; i < del.Length; i++)
                    del[i] = invocationList[i].Value;
                return del;
            }

            return new Delegate[] { this };
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
                return false;
            if (object.ReferenceEquals(this, obj))
                return true;
            if (!InternalEqualTypes(this, obj))
                return false;

            // Since this is a Delegate and we know the types are the same, obj should also be a Delegate
            Debug.Assert(obj is Delegate, "Shouldn't have failed here since we already checked the types are the same!");
            var d = Unsafe.As<Delegate>(obj);

            if (_helperObject is Wrapper[] invocationList)
            {
                if (d._extraFunctionPointerOrData != _extraFunctionPointerOrData)
                    return false;

                if (d._helperObject is not Wrapper[] dInvocationList)
                    return false;

                int invocationCount = (int)_extraFunctionPointerOrData;
                for (int i = 0; i < invocationCount; i++)
                {
                    if (!invocationList[i].Value.Equals(dInvocationList[i].Value))
                        return false;
                }
                return true;
            }

            if (!object.ReferenceEquals(_helperObject, d._helperObject) ||
                (!FunctionPointerOps.Compare(_extraFunctionPointerOrData, d._extraFunctionPointerOrData)) ||
                (!FunctionPointerOps.Compare(_functionPointer, d._functionPointer)))
            {
                return false;
            }

            // Those delegate kinds with thunks put themselves into the _firstParameter, so we can't
            // blindly compare the _firstParameter fields for equality.
            if (object.ReferenceEquals(_firstParameter, this))
            {
                return object.ReferenceEquals(d._firstParameter, d);
            }

            return object.ReferenceEquals(_firstParameter, d._firstParameter);
        }

        public override int GetHashCode()
        {
            if (_helperObject is Wrapper[] invocationList)
            {
                int hash = 0;
                for (int i = 0; i < (int)_extraFunctionPointerOrData; i++)
                {
                    hash = hash * 33 + invocationList[i].Value.GetHashCode();
                }
                return hash;
            }

            return base.GetHashCode();
        }

        public bool HasSingleTarget => _helperObject is not Wrapper[];

        // Used by delegate invocation list enumerator
        internal Delegate? TryGetAt(int index)
        {
            if (_helperObject is Wrapper[] invocationList)
            {
                return ((uint)index < (uint)_extraFunctionPointerOrData) ? invocationList[index].Value : null;
            }

            return (index == 0) ? this : null;
        }
    }
}
