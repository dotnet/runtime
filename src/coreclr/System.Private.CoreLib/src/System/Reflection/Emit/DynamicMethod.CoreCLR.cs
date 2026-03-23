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

        /// <summary>
        /// Completes the dynamic method and creates a delegate that can be used to execute it.
        /// </summary>
        /// <param name="delegateType">A delegate type whose signature matches that of the dynamic method.</param>
        /// <returns>A delegate of the specified type, which can be used to execute the dynamic method.</returns>
        /// <exception cref="InvalidOperationException">The dynamic method has no method body.</exception>
        /// <exception cref="ArgumentException"><paramref name="delegateType" /> has the wrong number of parameters or the wrong parameter types.</exception>
        /// <remarks>
        /// For more information about this API, see <see href="https://github.com/dotnet/docs/raw/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod-createdelegate.md">Supplemental API remarks for DynamicMethod.CreateDelegate</see>.
        /// </remarks>
        public sealed override Delegate CreateDelegate(Type delegateType) =>
            CreateDelegate(delegateType, target: null);

        /// <summary>
        /// Completes the dynamic method and creates a delegate that can be used to execute it, specifying the delegate type and an object the delegate is bound to.
        /// </summary>
        /// <param name="delegateType">A delegate type whose signature matches that of the dynamic method, minus the first parameter.</param>
        /// <param name="target">An object the delegate is bound to. Must be of the same type as the first parameter of the dynamic method.</param>
        /// <returns>A delegate of the specified type, which can be used to execute the dynamic method with the specified target object.</returns>
        /// <exception cref="InvalidOperationException">The dynamic method has no method body.</exception>
        /// <exception cref="ArgumentException"><paramref name="delegateType" /> has the wrong number of parameters or the wrong parameter types.</exception>
        /// <remarks>
        /// For more information about this API, see <see href="https://github.com/dotnet/docs/raw/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod-createdelegate.md">Supplemental API remarks for DynamicMethod.CreateDelegate</see>.
        /// </remarks>
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

        /// <summary>
        /// Invokes the dynamic method using the specified parameters, under the constraints of the specified binder, with the specified culture information.
        /// </summary>
        /// <param name="obj">This parameter is ignored for dynamic methods, because they are static. Specify <see langword="null" />.</param>
        /// <param name="invokeAttr">A bitwise combination of <see cref="BindingFlags" /> values.</param>
        /// <param name="binder">A <see cref="Binder" /> object that enables the binding, coercion of argument types, invocation of members, and retrieval of <see cref="System.Reflection.MemberInfo" /> objects through reflection. If <paramref name="binder" /> is <see langword="null" />, the default binder is used.</param>
        /// <param name="parameters">An argument list. This is an array of arguments with the same number, order, and type as the parameters of the method to be invoked. If there are no parameters this parameter should be <see langword="null" />.</param>
        /// <param name="culture">An instance of <see cref="CultureInfo" /> used to govern the coercion of types. If this is <see langword="null" />, the <see cref="CultureInfo" /> for the current thread is used.</param>
        /// <returns>An <see cref="object" /> containing the return value of the invoked method.</returns>
        /// <exception cref="TargetParameterCountException">The number of elements in <paramref name="parameters" /> does not match the number of parameters in the dynamic method.</exception>
        /// <exception cref="NotSupportedException">The dynamic method contains unverifiable code.</exception>
        /// <exception cref="InvalidOperationException">The dynamic method has no method body.</exception>
        /// <remarks>
        /// For more information about this API, see <see href="https://github.com/dotnet/docs/raw/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod-invoke.md">Supplemental API remarks for DynamicMethod.Invoke</see>.
        /// </remarks>
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
            object? retValue = argCount switch
            {
                0 => Invoker.InvokeWithNoArgs(obj, invokeAttr),
                1 => Invoker.InvokeWithOneArg(obj, invokeAttr, binder, parameters!, culture),
                2 or 3 or 4 => Invoker.InvokeWithFewArgs(obj, invokeAttr, binder, parameters!, culture),
                _ => Invoker.InvokeWithManyArgs(obj, invokeAttr, binder, parameters!, culture),
            };
            GC.KeepAlive(this);
            return retValue;
        }

        /// <summary>
        /// Returns a <see cref="DynamicILInfo" /> object that can be used to generate a method body from metadata tokens, scopes, and Microsoft intermediate language (MSIL) streams.
        /// </summary>
        /// <returns>A <see cref="DynamicILInfo" /> object that can be used to generate a method body from metadata tokens, scopes, and MSIL streams.</returns>
        /// <remarks>
        /// The <see cref="DynamicILInfo" /> class is provided to support unmanaged code generation.
        /// </remarks>
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

        /// <summary>
        /// Returns a Microsoft intermediate language (MSIL) generator for the method with the specified MSIL stream size.
        /// </summary>
        /// <param name="streamSize">The size of the MSIL stream, in bytes.</param>
        /// <returns>An <see cref="ILGenerator" /> object for the method.</returns>
        /// <remarks>
        /// For more information about this API, see <see href="https://github.com/dotnet/docs/raw/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod-getilgenerator.md">Supplemental API remarks for DynamicMethod.GetILGenerator</see>.
        /// </remarks>
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
