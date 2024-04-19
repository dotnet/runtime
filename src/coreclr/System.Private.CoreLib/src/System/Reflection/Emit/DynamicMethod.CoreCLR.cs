// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace System.Reflection.Emit
{
    public sealed partial class DynamicMethod : MethodInfo
    {
        private RuntimeType[] _parameterTypes;
        internal IRuntimeMethodInfo? _methodHandle;
        private RuntimeType _returnType;
        private DynamicILGenerator? _ilGenerator;
        private DynamicILInfo? _dynamicILInfo;
        private bool _initLocals;
        private Module _module;
        internal bool _skipVisibility;
        internal RuntimeType? _typeOwner;
        private MethodBaseInvoker? _invoker;
        private Signature? _signature;
        private string _name;
        private MethodAttributes _attributes;
        private CallingConventions _callingConvention;
        private RuntimeParameterInfo[]? _parameters;

        // needed to keep the object alive during jitting
        // assigned by the DynamicResolver ctor
        internal DynamicResolver? _resolver;

        internal bool _restrictedSkipVisibility;

        //
        // Delegate and method creation
        //

        public sealed override Delegate CreateDelegate(Type delegateType) =>
            CreateDelegate(delegateType, target: null);

        public sealed override Delegate CreateDelegate(Type delegateType, object? target)
        {
            if (_restrictedSkipVisibility)
            {
                // Compile the method since accessibility checks are done as part of compilation
                GetMethodDescriptor();
                IRuntimeMethodInfo? methodHandle = _methodHandle;
                CompileMethod(methodHandle != null ? methodHandle.Value : RuntimeMethodHandleInternal.EmptyHandle);
                GC.KeepAlive(methodHandle);
            }

            MulticastDelegate d = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, target, GetMethodDescriptor());
            // stash this MethodInfo by brute force.
            d.StoreDynamicMethod(this);
            return d;
        }

        // This is guaranteed to return a valid handle
        internal RuntimeMethodHandle GetMethodDescriptor()
        {
            if (_methodHandle == null)
            {
                lock (this)
                {
                    if (_methodHandle == null)
                    {
                        if (_dynamicILInfo != null)
                            _dynamicILInfo.GetCallableMethod((RuntimeModule)_module, this);
                        else
                        {
                            if (_ilGenerator == null || _ilGenerator.ILOffset == 0)
                                throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadEmptyMethodBody, Name));

                            _ilGenerator.GetCallableMethod((RuntimeModule)_module, this);
                        }
                    }
                }
            }
            return new RuntimeMethodHandle(_methodHandle!);
        }

        private MethodBaseInvoker Invoker
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _invoker ??= new MethodBaseInvoker(this, Signature);
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
                    Debug.Assert(_methodHandle != null);
                    Debug.Assert(_parameterTypes != null);

                    Signature newSig = new Signature(_methodHandle, _parameterTypes, _returnType, CallingConvention);
                    Volatile.Write(ref _signature, newSig);
                    return newSig;
                }

                return _signature ?? LazyCreateSignature();
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
            switch (argCount)
            {
                case 0:
                    retValue = Invoker.InvokeWithNoArgs(obj, invokeAttr);
                    break;
                case 1:
                    retValue = Invoker.InvokeWithOneArg(obj, invokeAttr, binder, parameters!, culture);
                    break;
                case 2:
                case 3:
                case 4:
                    retValue = Invoker.InvokeWithFewArgs(obj, invokeAttr, binder, parameters!, culture);
                    break;
                default:
                    retValue = Invoker.InvokeWithManyArgs(obj, invokeAttr, binder, parameters!, culture);
                    break;
            }

            GC.KeepAlive(this);
            return retValue;
        }

        public DynamicILInfo GetDynamicILInfo()
        {
            if (_dynamicILInfo == null)
            {
                byte[] methodSignature = SignatureHelper.GetMethodSigHelper(
                        null, CallingConvention, ReturnType, null, null, _parameterTypes, null, null).GetSignature(true);
                _dynamicILInfo = new DynamicILInfo(this, methodSignature);
            }
            return _dynamicILInfo;
        }

        public ILGenerator GetILGenerator(int streamSize)
        {
            if (_ilGenerator == null)
            {
                byte[] methodSignature = SignatureHelper.GetMethodSigHelper(
                    null, CallingConvention, ReturnType, null, null, _parameterTypes, null, null).GetSignature(true);
                _ilGenerator = new DynamicILGenerator(this, methodSignature, streamSize);
            }
            return _ilGenerator;
        }
    }
}
