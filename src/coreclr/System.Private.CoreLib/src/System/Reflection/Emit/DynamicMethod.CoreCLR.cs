// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace System.Reflection.Emit
{
    public sealed partial class DynamicMethod : MethodInfo
    {
        private RuntimeType[] parameterTypes;
        internal IRuntimeMethodInfo? methodHandle;
        private RuntimeType returnType;
        private DynamicILGenerator? ilGenerator;
        private DynamicILInfo? dynamicILInfo;
        private bool initLocals;
        private Module module;
        internal bool skipVisibility;
        internal RuntimeType? typeOwner;
        private MethodInvoker? invoker;
        private Signature? signature;

        // We want the creator of the DynamicMethod to control who has access to the
        // DynamicMethod (just like we do for delegates). However, a user can get to
        // the corresponding RTDynamicMethod using Exception.TargetSite, StackFrame.GetMethod, etc.
        // If we allowed use of RTDynamicMethod, the creator of the DynamicMethod would
        // not be able to bound access to the DynamicMethod. Hence, we need to ensure that
        // we do not allow direct use of RTDynamicMethod.
        private RTDynamicMethod dynMethod;

        // needed to keep the object alive during jitting
        // assigned by the DynamicResolver ctor
        internal DynamicResolver? resolver;

        internal bool restrictedSkipVisibility;

        //
        // Delegate and method creation
        //

        public sealed override Delegate CreateDelegate(Type delegateType)
        {
            if (restrictedSkipVisibility)
            {
                // Compile the method since accessibility checks are done as part of compilation.
                GetMethodDescriptor();
                IRuntimeMethodInfo? methodHandle = this.methodHandle;
                System.Runtime.CompilerServices.RuntimeHelpers.CompileMethod(methodHandle != null ? methodHandle.Value : RuntimeMethodHandleInternal.EmptyHandle);
                GC.KeepAlive(methodHandle);
            }

            MulticastDelegate d = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, null, GetMethodDescriptor());
            // stash this MethodInfo by brute force.
            d.StoreDynamicMethod(GetMethodInfo());
            return d;
        }

        public sealed override Delegate CreateDelegate(Type delegateType, object? target)
        {
            if (restrictedSkipVisibility)
            {
                // Compile the method since accessibility checks are done as part of compilation
                GetMethodDescriptor();
                IRuntimeMethodInfo? methodHandle = this.methodHandle;
                System.Runtime.CompilerServices.RuntimeHelpers.CompileMethod(methodHandle != null ? methodHandle.Value : RuntimeMethodHandleInternal.EmptyHandle);
                GC.KeepAlive(methodHandle);
            }

            MulticastDelegate d = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, target, GetMethodDescriptor());
            // stash this MethodInfo by brute force.
            d.StoreDynamicMethod(GetMethodInfo());
            return d;
        }

        // This is guaranteed to return a valid handle
        internal RuntimeMethodHandle GetMethodDescriptor()
        {
            if (methodHandle == null)
            {
                lock (this)
                {
                    if (methodHandle == null)
                    {
                        if (dynamicILInfo != null)
                            dynamicILInfo.GetCallableMethod((RuntimeModule)module, this);
                        else
                        {
                            if (ilGenerator == null || ilGenerator.ILOffset == 0)
                                throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadEmptyMethodBody, Name));

                            ilGenerator.GetCallableMethod((RuntimeModule)module, this);
                        }
                    }
                }
            }
            return new RuntimeMethodHandle(methodHandle!);
        }

        private MethodInvoker Invoker
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return invoker ??= new MethodInvoker(this, Signature);
            }
        }

        internal Signature Signature
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                [MethodImpl(MethodImplOptions.NoInlining)] // move lazy sig generation out of the hot path
                Signature LazyCreateSignature()
                {
                    Debug.Assert(methodHandle != null);
                    Debug.Assert(parameterTypes != null);

                    Signature newSig = new Signature(methodHandle, parameterTypes, returnType, CallingConvention);
                    Volatile.Write(ref signature, newSig);
                    return newSig;
                }

                return signature ?? LazyCreateSignature();
            }
        }

        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
                throw new NotSupportedException(SR.NotSupported_CallToVarArg);

            //
            // We do not demand any permission here because the caller already has access
            // to the current DynamicMethod object, and it could just as easily emit another
            // Transparent DynamicMethod to call the current DynamicMethod.
            //

            _ = GetMethodDescriptor();
            // ignore obj since it's a static method

            // verify arguments
            int argCount = (parameters != null) ? parameters.Length : 0;
            if (Signature.Arguments.Length != argCount)
                throw new TargetParameterCountException(SR.Arg_ParmCnt);

            object? retValue;

            unsafe
            {
                if (argCount == 0)
                {
                    retValue = Invoker.InlinedInvoke(obj, args: default, invokeAttr);
                }
                else if (argCount > MaxStackAllocArgCount)
                {
                    Debug.Assert(parameters != null);
                    retValue = InvokeWithManyArguments(this, argCount, obj, invokeAttr, binder, parameters, culture);
                }
                else
                {
                    Debug.Assert(parameters != null);
                    StackAllocedArguments argStorage = default;
                    Span<object?> copyOfParameters = new(ref argStorage._arg0, argCount);
                    Span<ParameterCopyBackAction> shouldCopyBackParameters = new(ref argStorage._copyBack0, argCount);

                    StackAllocatedByRefs byrefStorage = default;
                    IntPtr* pByRefStorage = (IntPtr*)&byrefStorage;

                    CheckArguments(
                        copyOfParameters,
                        pByRefStorage,
                        shouldCopyBackParameters,
                        parameters,
                        Signature.Arguments,
                        binder,
                        culture,
                        invokeAttr);

                    retValue = Invoker.InlinedInvoke(obj, pByRefStorage, invokeAttr);

                    // Copy modified values out. This should be done only with ByRef or Type.Missing parameters.
                    for (int i = 0; i < argCount; i++)
                    {
                        ParameterCopyBackAction action = shouldCopyBackParameters[i];
                        if (action != ParameterCopyBackAction.None)
                        {
                            if (action == ParameterCopyBackAction.Copy)
                            {
                                parameters[i] = copyOfParameters[i];
                            }
                            else
                            {
                                Debug.Assert(action == ParameterCopyBackAction.CopyNullable);
                                Debug.Assert(copyOfParameters[i] != null);
                                Debug.Assert(((RuntimeType)copyOfParameters[i]!.GetType()).IsNullableOfT);
                                parameters[i] = RuntimeMethodHandle.ReboxFromNullable(copyOfParameters[i]);
                            }
                        }
                    }
                }
            }

            GC.KeepAlive(this);
            return retValue;
        }

        // Slower path that does a heap alloc for copyOfParameters and registers byrefs to those objects.
        // This is a separate method to support better performance for the faster paths.
        private static unsafe object? InvokeWithManyArguments(
            DynamicMethod mi,
            int argCount,
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[] parameters,
            CultureInfo? culture)
        {
            object[] objHolder = new object[argCount];
            Span<object?> copyOfParameters = new(objHolder, 0, argCount);

            // We don't check a max stack size since we are invoking a method which
            // naturally requires a stack size that is dependent on the arg count\size.
            IntPtr* pByRefStorage = stackalloc IntPtr[argCount];
            NativeMemory.Clear(pByRefStorage, (uint)(argCount * sizeof(IntPtr)));

            ParameterCopyBackAction* copyBackActions = stackalloc ParameterCopyBackAction[argCount];
            Span<ParameterCopyBackAction> shouldCopyBackParameters = new(copyBackActions, argCount);

            GCFrameRegistration reg = new(pByRefStorage, (uint)argCount, areByRefs: true);

            object? retValue;
            try
            {
                RegisterForGCReporting(&reg);
                mi.CheckArguments(
                    copyOfParameters,
                    pByRefStorage,
                    shouldCopyBackParameters,
                    parameters,
                    mi.Signature.Arguments,
                    binder,
                    culture,
                    invokeAttr);

                retValue = mi.Invoker.InlinedInvoke(obj, pByRefStorage, invokeAttr);
            }
            finally
            {
                UnregisterForGCReporting(&reg);
            }

            // Copy modified values out. This should be done only with ByRef or Type.Missing parameters.
            for (int i = 0; i < argCount; i++)
            {
                ParameterCopyBackAction action = shouldCopyBackParameters[i];
                if (action != ParameterCopyBackAction.None)
                {
                    if (action == ParameterCopyBackAction.Copy)
                    {
                        parameters[i] = copyOfParameters[i];
                    }
                    else
                    {
                        Debug.Assert(action == ParameterCopyBackAction.CopyNullable);
                        Debug.Assert(copyOfParameters[i] != null);
                        Debug.Assert(((RuntimeType)copyOfParameters[i]!.GetType()).IsNullableOfT);
                        parameters[i] = RuntimeMethodHandle.ReboxFromNullable(copyOfParameters[i]);
                    }
                }
            }

            return retValue;
        }

        public DynamicILInfo GetDynamicILInfo()
        {
            if (dynamicILInfo == null)
            {
                byte[] methodSignature = SignatureHelper.GetMethodSigHelper(
                        null, CallingConvention, ReturnType, null, null, parameterTypes, null, null).GetSignature(true);
                dynamicILInfo = new DynamicILInfo(this, methodSignature);
            }
            return dynamicILInfo;
        }

        public ILGenerator GetILGenerator(int streamSize)
        {
            if (ilGenerator == null)
            {
                byte[] methodSignature = SignatureHelper.GetMethodSigHelper(
                    null, CallingConvention, ReturnType, null, null, parameterTypes, null, null).GetSignature(true);
                ilGenerator = new DynamicILGenerator(this, methodSignature, streamSize);
            }
            return ilGenerator;
        }
    }
}
