// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

using Internal.Reflection.Augments;
using Internal.Runtime;
using Internal.Runtime.Augments;
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

        internal object m_firstParameter;
        internal object m_helperObject;
        internal nint m_extraFunctionPointerOrData;
        internal IntPtr m_functionPointer;

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
        /// Used by various parts of the runtime as a replacement for Delegate.Method
        ///
        /// The Interop layer uses this to distinguish between different methods on a
        /// single type, and to get the function pointer for delegates to static functions
        ///
        /// The reflection apis use this api to figure out what MethodInfo is related
        /// to a delegate.
        ///
        /// </summary>
        /// <param name="typeOfFirstParameterIfInstanceDelegate">
        ///   This value indicates which type an delegate's function pointer is associated with
        ///   This value is ONLY set for delegates where the function pointer points at an instance method
        /// </param>
        /// <param name="isOpenResolver">
        ///   This value indicates if the returned pointer is an open resolver structure.
        /// </param>
        /// <param name="isInterpreterEntrypoint">
        ///   Delegate points to an object array thunk (the delegate wraps a Func&lt;object[], object&gt; delegate). This
        ///   is typically a delegate pointing to the LINQ expression interpreter.
        /// </param>
        /// <returns></returns>
        internal unsafe IntPtr GetFunctionPointer(out RuntimeTypeHandle typeOfFirstParameterIfInstanceDelegate, out bool isOpenResolver, out bool isInterpreterEntrypoint)
        {
            typeOfFirstParameterIfInstanceDelegate = default(RuntimeTypeHandle);
            isOpenResolver = false;
            isInterpreterEntrypoint = false;

            if (GetThunk(MulticastThunk) == m_functionPointer)
            {
                return IntPtr.Zero;
            }
            else if (GetThunk(ObjectArrayThunk) == m_functionPointer)
            {
                isInterpreterEntrypoint = true;
                return IntPtr.Zero;
            }
            else if (m_extraFunctionPointerOrData != 0)
            {
                if (GetThunk(OpenInstanceThunk) == m_functionPointer)
                {
                    typeOfFirstParameterIfInstanceDelegate = ((OpenMethodResolver*)m_extraFunctionPointerOrData)->DeclaringType;
                    isOpenResolver = true;
                }
                return m_extraFunctionPointerOrData;
            }
            else
            {
                if (m_firstParameter != null)
                    typeOfFirstParameterIfInstanceDelegate = new RuntimeTypeHandle(m_firstParameter.GetMethodTable());

                // TODO! Implementation issue for generic invokes here ... we need another IntPtr for uniqueness.

                return m_functionPointer;
            }
        }

        // This function is known to the IL Transformer.
        private void InitializeClosedInstance(object firstParameter, IntPtr functionPointer)
        {
            if (firstParameter is null)
                throw new ArgumentException(SR.Arg_DlgtNullInst);

            m_functionPointer = functionPointer;
            m_firstParameter = firstParameter;
        }

        // This function is known to the IL Transformer.
        private void InitializeClosedInstanceSlow(object firstParameter, IntPtr functionPointer)
        {
            // This method is like InitializeClosedInstance, but it handles ALL cases. In particular, it handles generic method with fun function pointers.

            if (firstParameter is null)
                throw new ArgumentException(SR.Arg_DlgtNullInst);

            if (!FunctionPointerOps.IsGenericMethodPointer(functionPointer))
            {
                m_functionPointer = functionPointer;
                m_firstParameter = firstParameter;
            }
            else
            {
                m_firstParameter = this;
                m_functionPointer = GetThunk(ClosedInstanceThunkOverGenericMethod);
                m_extraFunctionPointerOrData = functionPointer;
                m_helperObject = firstParameter;
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
                m_functionPointer = functionResolution;
                m_firstParameter = firstParameter;
            }
            else
            {
                m_firstParameter = this;
                m_functionPointer = GetThunk(ClosedInstanceThunkOverGenericMethod);
                m_extraFunctionPointerOrData = functionResolution;
                m_helperObject = firstParameter;
            }

            return;
        }

        private void InitializeClosedInstanceToInterface(object firstParameter, IntPtr dispatchCell)
        {
            if (firstParameter is null)
                throw new NullReferenceException();

            m_functionPointer = RuntimeImports.RhpResolveInterfaceMethod(firstParameter, dispatchCell);
            m_firstParameter = firstParameter;
        }

        // This is used to implement MethodInfo.CreateDelegate() in a desktop-compatible way. Yes, the desktop really
        // let you use that api to invoke an instance method with a null 'this'.
        private void InitializeClosedInstanceWithoutNullCheck(object firstParameter, IntPtr functionPointer)
        {
            if (!FunctionPointerOps.IsGenericMethodPointer(functionPointer))
            {
                m_functionPointer = functionPointer;
                m_firstParameter = firstParameter;
            }
            else
            {
                m_firstParameter = this;
                m_functionPointer = GetThunk(ClosedInstanceThunkOverGenericMethod);
                m_extraFunctionPointerOrData = functionPointer;
                m_helperObject = firstParameter;
            }
        }

        // This function is known to the compiler backend.
        private void InitializeClosedStaticThunk(object firstParameter, IntPtr functionPointer, IntPtr functionPointerThunk)
        {
            m_extraFunctionPointerOrData = functionPointer;
            m_helperObject = firstParameter;
            m_functionPointer = functionPointerThunk;
            m_firstParameter = this;
        }

        // This function is known to the compiler backend.
        private void InitializeOpenStaticThunk(object _ /*firstParameter*/, IntPtr functionPointer, IntPtr functionPointerThunk)
        {
            // This sort of delegate is invoked by calling the thunk function pointer with the arguments to the delegate + a reference to the delegate object itself.
            m_firstParameter = this;
            m_functionPointer = functionPointerThunk;
            m_extraFunctionPointerOrData = functionPointer;
        }

        private void InitializeOpenInstanceThunkDynamic(IntPtr functionPointer, IntPtr functionPointerThunk)
        {
            // This sort of delegate is invoked by calling the thunk function pointer with the arguments to the delegate + a reference to the delegate object itself.
            m_firstParameter = this;
            m_functionPointer = functionPointerThunk;
            m_extraFunctionPointerOrData = functionPointer;
        }

        internal void SetClosedStaticFirstParameter(object firstParameter)
        {
            // Closed static delegates place a value in m_helperObject that they pass to the target method.
            Debug.Assert(m_functionPointer == GetThunk(ClosedStaticThunk));
            m_helperObject = firstParameter;
        }

        // This function is only ever called by the open instance method thunk, and in that case,
        // m_extraFunctionPointerOrData always points to an OpenMethodResolver
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IntPtr GetActualTargetFunctionPointer(object thisObject)
        {
            return OpenMethodResolver.ResolveMethod(m_extraFunctionPointerOrData, thisObject);
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }

        internal bool IsDynamicDelegate()
        {
            if (this.GetThunk(MulticastThunk) == IntPtr.Zero)
            {
                return true;
            }

            return false;
        }

        [DebuggerGuidedStepThroughAttribute]
        protected virtual object? DynamicInvokeImpl(object?[]? args)
        {
            if (IsDynamicDelegate())
            {
                // DynamicDelegate case
                object? result = ((Func<object?[]?, object?>)m_helperObject)(args);
                DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                return result;
            }
            else
            {
                DynamicInvokeInfo dynamicInvokeInfo = ReflectionAugments.ReflectionCoreCallbacks.GetDelegateDynamicInvokeInfo(GetType());

                object? result = dynamicInvokeInfo.Invoke(m_firstParameter, m_functionPointer,
                    args, binderBundle: null, wrapInTargetInvocationException: true);
                DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                return result;
            }
        }

        protected virtual MethodInfo GetMethodImpl()
        {
            return ReflectionAugments.ReflectionCoreCallbacks.GetDelegateMethod(this);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            // It is expected that all real uses of the Equals method will hit the MulticastDelegate.Equals logic instead of this
            // therefore, instead of duplicating the desktop behavior where direct calls to this Equals function do not behave
            // correctly, we'll just throw here.
            throw new PlatformNotSupportedException();
        }

        public object Target
        {
            get
            {
                // Multi-cast delegates return the Target of the last delegate in the list
                if (m_functionPointer == GetThunk(MulticastThunk))
                {
                    Delegate[] invocationList = (Delegate[])m_helperObject;
                    int invocationCount = (int)m_extraFunctionPointerOrData;
                    return invocationList[invocationCount - 1].Target;
                }

                // Closed static delegates place a value in m_helperObject that they pass to the target method.
                if (m_functionPointer == GetThunk(ClosedStaticThunk) ||
                    m_functionPointer == GetThunk(ClosedInstanceThunkOverGenericMethod) ||
                    m_functionPointer == GetThunk(ObjectArrayThunk))
                    return m_helperObject;

                // Other non-closed thunks can be identified as the m_firstParameter field points at this.
                if (object.ReferenceEquals(m_firstParameter, this))
                {
                    return null;
                }

                // Closed instance delegates place a value in m_firstParameter, and we've ruled out all other types of delegates
                return m_firstParameter;
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

        internal bool IsOpenStatic
        {
            get
            {
                return GetThunk(OpenStaticThunk) == m_functionPointer;
            }
        }

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

            del.m_helperObject = handler;
            del.m_functionPointer = objArrayThunk;
            del.m_firstParameter = del;
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
    }
}
