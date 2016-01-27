// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Reflection.Emit
{

    using System;
    using System.Globalization;
    using TextWriter = System.IO.TextWriter;
    using System.Diagnostics.SymbolStore;
    using System.Runtime.InteropServices;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Generic;
    using System.Security.Permissions;
    using System.Threading;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Security;

    internal class DynamicILGenerator : ILGenerator
    {

        internal DynamicScope m_scope;
        private int m_methodSigToken;

        internal unsafe DynamicILGenerator(DynamicMethod method, byte[] methodSignature, int size)
            : base(method, size)
        {
            m_scope = new DynamicScope();

            m_methodSigToken = m_scope.GetTokenFor(methodSignature);
        }


        [System.Security.SecurityCritical]  // auto-generated
        internal void GetCallableMethod(RuntimeModule module, DynamicMethod dm)
        {
            dm.m_methodHandle = ModuleHandle.GetDynamicMethod(dm,
                                          module,
                                          m_methodBuilder.Name,
                                          (byte[])m_scope[m_methodSigToken],
                                          new DynamicResolver(this));
        }

#if FEATURE_APPX
        private bool ProfileAPICheck
        {
            get
            {
                return ((DynamicMethod)m_methodBuilder).ProfileAPICheck;
            }
        }
#endif // FEATURE_APPX

        // *** ILGenerator api ***

        public override LocalBuilder DeclareLocal(Type localType, bool pinned)
        {
            LocalBuilder localBuilder;
            if (localType == null)
                throw new ArgumentNullException("localType");
            Contract.EndContractBlock();

            RuntimeType rtType = localType as RuntimeType;

            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));

#if FEATURE_APPX
            if (ProfileAPICheck && (rtType.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtType.FullName));
#endif

            localBuilder = new LocalBuilder(m_localCount, localType, m_methodBuilder);
            // add the localType to local signature
            m_localSignature.AddArgument(localType, pinned);
            m_localCount++;
            return localBuilder;
        }

        //
        //
        // Token resolution calls
        //
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void Emit(OpCode opcode, MethodInfo meth)
        {
            if (meth == null)
                throw new ArgumentNullException("meth");
            Contract.EndContractBlock();

            int stackchange = 0;
            int token = 0;
            DynamicMethod dynMeth = meth as DynamicMethod;
            if (dynMeth == null)
            {
                RuntimeMethodInfo rtMeth = meth as RuntimeMethodInfo;
                if (rtMeth == null)
                    throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"), "meth");

                RuntimeType declaringType = rtMeth.GetRuntimeType();
                if (declaringType != null && (declaringType.IsGenericType || declaringType.IsArray))
                    token = GetTokenFor(rtMeth, declaringType);
                else
                    token = GetTokenFor(rtMeth);
            }
            else
            {
                // rule out not allowed operations on DynamicMethods
                if (opcode.Equals(OpCodes.Ldtoken) || opcode.Equals(OpCodes.Ldftn) || opcode.Equals(OpCodes.Ldvirtftn))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOpCodeOnDynamicMethod"));
                }
                token = GetTokenFor(dynMeth);
            }

            EnsureCapacity(7);
            InternalEmit(opcode);

            if (opcode.StackBehaviourPush == StackBehaviour.Varpush
                && meth.ReturnType != typeof(void))
            {
                stackchange++;
            }
            if (opcode.StackBehaviourPop == StackBehaviour.Varpop)
            {
                stackchange -= meth.GetParametersNoCopy().Length;
            }
            // Pop the "this" parameter if the method is non-static,
            //  and the instruction is not newobj/ldtoken/ldftn.
            if (!meth.IsStatic &&
                !(opcode.Equals(OpCodes.Newobj) || opcode.Equals(OpCodes.Ldtoken) || opcode.Equals(OpCodes.Ldftn)))
            {
                stackchange--;
            }

            UpdateStackSize(opcode, stackchange);

            PutInteger4(token);
        }

        [System.Runtime.InteropServices.ComVisible(true)]
        public override void Emit(OpCode opcode, ConstructorInfo con)
        {
            if (con == null)
                throw new ArgumentNullException("con");
            Contract.EndContractBlock();

            RuntimeConstructorInfo rtConstructor = con as RuntimeConstructorInfo;
            if (rtConstructor == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"), "con");

            RuntimeType declaringType = rtConstructor.GetRuntimeType();
            int token;

            if (declaringType != null && (declaringType.IsGenericType || declaringType.IsArray))
                // need to sort out the stack size story
                token = GetTokenFor(rtConstructor, declaringType);
            else
                token = GetTokenFor(rtConstructor);

            EnsureCapacity(7);
            InternalEmit(opcode);

            // need to sort out the stack size story
            UpdateStackSize(opcode, 1);

            PutInteger4(token);
        }

        public override void Emit(OpCode opcode, Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();

            RuntimeType rtType = type as RuntimeType;

            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));

            int token = GetTokenFor(rtType);
            EnsureCapacity(7);
            InternalEmit(opcode);
            PutInteger4(token);
        }

        public override void Emit(OpCode opcode, FieldInfo field)
        {
            if (field == null)
                throw new ArgumentNullException("field");
            Contract.EndContractBlock();

            RuntimeFieldInfo runtimeField = field as RuntimeFieldInfo;
            if (runtimeField == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeFieldInfo"), "field");

            int token;
            if (field.DeclaringType == null)
                token = GetTokenFor(runtimeField);
            else
                token = GetTokenFor(runtimeField, runtimeField.GetRuntimeType());

            EnsureCapacity(7);
            InternalEmit(opcode);
            PutInteger4(token);
        }

        public override void Emit(OpCode opcode, String str)
        {
            if (str == null)
                throw new ArgumentNullException("str");
            Contract.EndContractBlock();

            int tempVal = GetTokenForString(str);
            EnsureCapacity(7);
            InternalEmit(opcode);
            PutInteger4(tempVal);
        }

        //
        //
        // Signature related calls (vararg, calli)
        //
        //
        [System.Security.SecuritySafeCritical] // overrides SC
        public override void EmitCalli(OpCode opcode,
                                       CallingConventions callingConvention,
                                       Type returnType,
                                       Type[] parameterTypes,
                                       Type[] optionalParameterTypes)
        {
            int stackchange = 0;
            SignatureHelper sig;
            if (optionalParameterTypes != null)
                if ((callingConvention & CallingConventions.VarArgs) == 0)

                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAVarArgCallingConvention"));

            sig = GetMemberRefSignature(callingConvention,
                                        returnType,
                                        parameterTypes,
                                        optionalParameterTypes);

            EnsureCapacity(7);
            Emit(OpCodes.Calli);

            // If there is a non-void return type, push one.
            if (returnType != typeof(void))
                stackchange++;
            // Pop off arguments if any.
            if (parameterTypes != null)
                stackchange -= parameterTypes.Length;
            // Pop off vararg arguments.
            if (optionalParameterTypes != null)
                stackchange -= optionalParameterTypes.Length;
            // Pop the this parameter if the method has a this parameter.
            if ((callingConvention & CallingConventions.HasThis) == CallingConventions.HasThis)
                stackchange--;
            // Pop the native function pointer.
            stackchange--;
            UpdateStackSize(OpCodes.Calli, stackchange);

            int token = GetTokenForSig(sig.GetSignature(true));
            PutInteger4(token);
        }

        public override void EmitCalli(OpCode opcode,
                                       CallingConvention unmanagedCallConv,
                                       Type returnType,
                                       Type[] parameterTypes)
        {
            int stackchange = 0;
            int cParams = 0;
            int i;
            SignatureHelper sig;

            if (parameterTypes != null)
                cParams = parameterTypes.Length;

            sig = SignatureHelper.GetMethodSigHelper(unmanagedCallConv, returnType);

            if (parameterTypes != null)
                for (i = 0; i < cParams; i++)
                    sig.AddArgument(parameterTypes[i]);

            // If there is a non-void return type, push one.
            if (returnType != typeof(void))
                stackchange++;

            // Pop off arguments if any.
            if (parameterTypes != null)
                stackchange -= cParams;

            // Pop the native function pointer.
            stackchange--;
            UpdateStackSize(OpCodes.Calli, stackchange);

            EnsureCapacity(7);
            Emit(OpCodes.Calli);
            int token = GetTokenForSig(sig.GetSignature(true));
            PutInteger4(token);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[] optionalParameterTypes)
        {
            if (methodInfo == null)
                throw new ArgumentNullException("methodInfo");

            if (!(opcode.Equals(OpCodes.Call) || opcode.Equals(OpCodes.Callvirt) || opcode.Equals(OpCodes.Newobj)))
                throw new ArgumentException(Environment.GetResourceString("Argument_NotMethodCallOpcode"), "opcode");

            if (methodInfo.ContainsGenericParameters)
                throw new ArgumentException(Environment.GetResourceString("Argument_GenericsInvalid"), "methodInfo");

            if (methodInfo.DeclaringType != null && methodInfo.DeclaringType.ContainsGenericParameters)
                throw new ArgumentException(Environment.GetResourceString("Argument_GenericsInvalid"), "methodInfo");
            Contract.EndContractBlock();

            int tk;
            int stackchange = 0;

            tk = GetMemberRefToken(methodInfo, optionalParameterTypes);

            EnsureCapacity(7);
            InternalEmit(opcode);

            // Push the return value if there is one.
            if (methodInfo.ReturnType != typeof(void))
                stackchange++;
            // Pop the parameters.
            stackchange -= methodInfo.GetParameterTypes().Length;
            // Pop the this parameter if the method is non-static and the
            // instruction is not newobj.
            if (!(methodInfo is SymbolMethod) && methodInfo.IsStatic == false && !(opcode.Equals(OpCodes.Newobj)))
                stackchange--;
            // Pop the optional parameters off the stack.
            if (optionalParameterTypes != null)
                stackchange -= optionalParameterTypes.Length;
            UpdateStackSize(opcode, stackchange);

            PutInteger4(tk);
        }

        public override void Emit(OpCode opcode, SignatureHelper signature)
        {
            if (signature == null)
                throw new ArgumentNullException("signature");
            Contract.EndContractBlock();

            int stackchange = 0;
            EnsureCapacity(7);
            InternalEmit(opcode);

            // The only IL instruction that has VarPop behaviour, that takes a
            // Signature token as a parameter is calli.  Pop the parameters and
            // the native function pointer.  To be conservative, do not pop the
            // this pointer since this information is not easily derived from
            // SignatureHelper.
            if (opcode.StackBehaviourPop == StackBehaviour.Varpop)
            {
                Contract.Assert(opcode.Equals(OpCodes.Calli),
                                "Unexpected opcode encountered for StackBehaviour VarPop.");
                // Pop the arguments..
                stackchange -= signature.ArgumentCount;
                // Pop native function pointer off the stack.
                stackchange--;
                UpdateStackSize(opcode, stackchange);
            }

            int token = GetTokenForSig(signature.GetSignature(true)); ;
            PutInteger4(token);
        }

        //
        //
        // Exception related generation
        //
        //
        public override Label BeginExceptionBlock()
        {
            return base.BeginExceptionBlock();
        }

        public override void EndExceptionBlock()
        {
            base.EndExceptionBlock();
        }

        public override void BeginExceptFilterBlock()
        {
            throw new NotSupportedException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod"));
        }

        public override void BeginCatchBlock(Type exceptionType)
        {
            if (CurrExcStackCount == 0)
                throw new NotSupportedException(Environment.GetResourceString("Argument_NotInExceptionBlock"));
            Contract.EndContractBlock();

            __ExceptionInfo current = CurrExcStack[CurrExcStackCount - 1];

            RuntimeType rtType = exceptionType as RuntimeType;

            if (current.GetCurrentState() == __ExceptionInfo.State_Filter)
            {
                if (exceptionType != null)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_ShouldNotSpecifyExceptionType"));
                }

                this.Emit(OpCodes.Endfilter);
            }
            else
            {
                // execute this branch if previous clause is Catch or Fault
                if (exceptionType == null)
                    throw new ArgumentNullException("exceptionType");

                if (rtType == null)
                    throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));

                Label endLabel = current.GetEndLabel();
                this.Emit(OpCodes.Leave, endLabel);

                // if this is a catch block the exception will be pushed on the stack and we need to update the stack info
                UpdateStackSize(OpCodes.Nop, 1);
            }

            current.MarkCatchAddr(ILOffset, exceptionType);


            // this is relying on too much implementation details of the base and so it's highly breaking
            // Need to have a more integreted story for exceptions
            current.m_filterAddr[current.m_currentCatch - 1] = GetTokenFor(rtType);
        }

        public override void BeginFaultBlock()
        {
            throw new NotSupportedException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod"));
        }

        public override void BeginFinallyBlock()
        {
            base.BeginFinallyBlock();
        }

        //
        //
        // debugger related calls. 
        //
        //
        [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
        public override void UsingNamespace(String ns)
        {
            throw new NotSupportedException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod"));
        }

        [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
        public override void MarkSequencePoint(ISymbolDocumentWriter document,
                                               int startLine,
                                               int startColumn,
                                               int endLine,
                                               int endColumn)
        {
            throw new NotSupportedException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod"));
        }

        public override void BeginScope()
        {
            throw new NotSupportedException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod"));
        }

        public override void EndScope()
        {
            throw new NotSupportedException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod"));
        }

        [System.Security.SecurityCritical]  // auto-generated
        private int GetMemberRefToken(MethodBase methodInfo, Type[] optionalParameterTypes)
        {
            Type[] parameterTypes;

            if (optionalParameterTypes != null && (methodInfo.CallingConvention & CallingConventions.VarArgs) == 0)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAVarArgCallingConvention"));

            RuntimeMethodInfo rtMeth = methodInfo as RuntimeMethodInfo;
            DynamicMethod dm = methodInfo as DynamicMethod;

            if (rtMeth == null && dm == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"), "methodInfo");

            ParameterInfo[] paramInfo = methodInfo.GetParametersNoCopy();
            if (paramInfo != null && paramInfo.Length != 0)
            {
                parameterTypes = new Type[paramInfo.Length];
                for (int i = 0; i < paramInfo.Length; i++)
                    parameterTypes[i] = paramInfo[i].ParameterType;
            }
            else
            {
                parameterTypes = null;
            }

            SignatureHelper sig = GetMemberRefSignature(methodInfo.CallingConvention,
                                                     MethodBuilder.GetMethodBaseReturnType(methodInfo),
                                                     parameterTypes,
                                                     optionalParameterTypes);

            if (rtMeth != null)
                return GetTokenForVarArgMethod(rtMeth, sig);
            else
                return GetTokenForVarArgMethod(dm, sig);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal override SignatureHelper GetMemberRefSignature(
                                                CallingConventions call,
                                                Type returnType,
                                                Type[] parameterTypes,
                                                Type[] optionalParameterTypes)
        {
            int cParams;
            int i;
            SignatureHelper sig;
            if (parameterTypes == null)
                cParams = 0;
            else
                cParams = parameterTypes.Length;
            sig = SignatureHelper.GetMethodSigHelper(call, returnType);
            for (i = 0; i < cParams; i++)
                sig.AddArgument(parameterTypes[i]);
            if (optionalParameterTypes != null && optionalParameterTypes.Length != 0)
            {
                // add the sentinel
                sig.AddSentinel();
                for (i = 0; i < optionalParameterTypes.Length; i++)
                    sig.AddArgument(optionalParameterTypes[i]);
            }
            return sig;
        }

        internal override void RecordTokenFixup()
        {
            // DynamicMethod doesn't need fixup.
        }

        #region GetTokenFor helpers
        private int GetTokenFor(RuntimeType rtType)
        {
#if FEATURE_APPX
            if (ProfileAPICheck && (rtType.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtType.FullName));
#endif

            return m_scope.GetTokenFor(rtType.TypeHandle);
        }

        private int GetTokenFor(RuntimeFieldInfo runtimeField)
        {
#if FEATURE_APPX
            if (ProfileAPICheck)
            {
                RtFieldInfo rtField = runtimeField as RtFieldInfo;
                if (rtField != null && (rtField.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtField.FullName));
            }
#endif

            return m_scope.GetTokenFor(runtimeField.FieldHandle);
        }

        private int GetTokenFor(RuntimeFieldInfo runtimeField, RuntimeType rtType)
        {
#if FEATURE_APPX
            if (ProfileAPICheck)
            {
                RtFieldInfo rtField = runtimeField as RtFieldInfo;
                if (rtField != null && (rtField.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtField.FullName));

                if ((rtType.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtType.FullName));
            }
#endif

            return m_scope.GetTokenFor(runtimeField.FieldHandle, rtType.TypeHandle);
        }

        private int GetTokenFor(RuntimeConstructorInfo rtMeth)
        {
#if FEATURE_APPX
            if (ProfileAPICheck && (rtMeth.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtMeth.FullName));
#endif

            return m_scope.GetTokenFor(rtMeth.MethodHandle);
        }

        private int GetTokenFor(RuntimeConstructorInfo rtMeth, RuntimeType rtType)
        {
#if FEATURE_APPX
            if (ProfileAPICheck)
            {
                if ((rtMeth.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtMeth.FullName));

                if ((rtType.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtType.FullName));
            }
#endif

            return m_scope.GetTokenFor(rtMeth.MethodHandle, rtType.TypeHandle);
        }

        private int GetTokenFor(RuntimeMethodInfo rtMeth)
        {
#if FEATURE_APPX
            if (ProfileAPICheck && (rtMeth.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtMeth.FullName));
#endif

            return m_scope.GetTokenFor(rtMeth.MethodHandle);
        }

        private int GetTokenFor(RuntimeMethodInfo rtMeth, RuntimeType rtType)
        {
#if FEATURE_APPX
            if (ProfileAPICheck)
            {
                if ((rtMeth.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtMeth.FullName));

                if ((rtType.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtType.FullName));
            }
#endif

            return m_scope.GetTokenFor(rtMeth.MethodHandle, rtType.TypeHandle);
        }

        private int GetTokenFor(DynamicMethod dm)
        {
            return m_scope.GetTokenFor(dm);
        }

        private int GetTokenForVarArgMethod(RuntimeMethodInfo rtMeth, SignatureHelper sig)
        {
#if FEATURE_APPX
            if (ProfileAPICheck && (rtMeth.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtMeth.FullName));
#endif
            VarArgMethod varArgMeth = new VarArgMethod(rtMeth, sig);
            return m_scope.GetTokenFor(varArgMeth);
        }

        private int GetTokenForVarArgMethod(DynamicMethod dm, SignatureHelper sig)
        {
            VarArgMethod varArgMeth = new VarArgMethod(dm, sig);
            return m_scope.GetTokenFor(varArgMeth);
        }

        private int GetTokenForString(String s)
        {
            return m_scope.GetTokenFor(s);
        }

        private int GetTokenForSig(byte[] sig)
        {
            return m_scope.GetTokenFor(sig);
        }
        #endregion

    }

    internal class DynamicResolver : Resolver
    {
        #region Private Data Members
        private __ExceptionInfo[] m_exceptions;
        private byte[] m_exceptionHeader;
        private DynamicMethod m_method;
        private byte[] m_code;
        private byte[] m_localSignature;
        private int m_stackSize;
        private DynamicScope m_scope;
        #endregion

        #region Internal Methods
        internal DynamicResolver(DynamicILGenerator ilGenerator)
        {
            m_stackSize = ilGenerator.GetMaxStackSize();
            m_exceptions = ilGenerator.GetExceptions();
            m_code = ilGenerator.BakeByteArray();
            m_localSignature = ilGenerator.m_localSignature.InternalGetSignatureArray();
            m_scope = ilGenerator.m_scope;

            m_method = (DynamicMethod)ilGenerator.m_methodBuilder;
            m_method.m_resolver = this;
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        internal DynamicResolver(DynamicILInfo dynamicILInfo)
        {
            m_stackSize = dynamicILInfo.MaxStackSize;
            m_code = dynamicILInfo.Code;
            m_localSignature = dynamicILInfo.LocalSignature;
            m_exceptionHeader = dynamicILInfo.Exceptions;
            //m_exceptions = dynamicILInfo.Exceptions;
            m_scope = dynamicILInfo.DynamicScope;

            m_method = dynamicILInfo.DynamicMethod;
            m_method.m_resolver = this;
        }

        //
        // We can destroy the unmanaged part of dynamic method only after the managed part is definitely gone and thus
        // nobody can call the dynamic method anymore. A call to finalizer alone does not guarantee that the managed 
        // part is gone. A malicious code can keep a reference to DynamicMethod in long weak reference that survives finalization,
        // or we can be running during shutdown where everything is finalized.
        //
        // The unmanaged resolver keeps a reference to the managed resolver in long weak handle. If the long weak handle 
        // is null, we can be sure that the managed part of the dynamic method is definitely gone and that it is safe to 
        // destroy the unmanaged part. (Note that the managed finalizer has to be on the same object that the long weak handle 
        // points to in order for this to work.) Unfortunately, we can not perform the above check when out finalizer 
        // is called - the long weak handle won't be cleared yet. Instead, we create a helper scout object that will attempt 
        // to do the destruction after next GC.
        //
        // The finalization does not have to be done using CriticalFinalizerObject. We have to go over all DynamicMethodDescs 
        // during AppDomain shutdown anyway to avoid leaks e.g. if somebody stores reference to DynamicMethod in static.
        //
        ~DynamicResolver()
        {
            DynamicMethod method = m_method;

            if (method == null)
                return;

            if (method.m_methodHandle == null)
                return;

            DestroyScout scout = null;
            try
            {
                scout = new DestroyScout();
            }
            catch
            {
                // We go over all DynamicMethodDesc during AppDomain shutdown and make sure
                // that everything associated with them is released. So it is ok to skip reregistration
                // for finalization during appdomain shutdown
                if (!Environment.HasShutdownStarted &&
                    !AppDomain.CurrentDomain.IsFinalizingForUnload())
                {
                    // Try again later.
                    GC.ReRegisterForFinalize(this);
                }
                return;
            }

            // We can never ever have two active destroy scouts for the same method. We need to initialize the scout
            // outside the try/reregister block to avoid possibility of reregistration for finalization with active scout.
            scout.m_methodHandle = method.m_methodHandle.Value;
        }

        private class DestroyScout
        {
            internal RuntimeMethodHandleInternal m_methodHandle;

            [System.Security.SecuritySafeCritical]  // auto-generated
            ~DestroyScout()
            {
                if (m_methodHandle.IsNullHandle())
                    return;

                // It is not safe to destroy the method if the managed resolver is alive.
                if (RuntimeMethodHandle.GetResolver(m_methodHandle) != null)
                {
                    if (!Environment.HasShutdownStarted &&
                        !AppDomain.CurrentDomain.IsFinalizingForUnload())
                    {
                        // Somebody might have been holding a reference on us via weak handle.
                        // We will keep trying. It will be hopefully released eventually.
                        GC.ReRegisterForFinalize(this);
                    }
                    return;
                }

                RuntimeMethodHandle.Destroy(m_methodHandle);
            }
        }

        // Keep in sync with vm/dynamicmethod.h
        [Flags]
        internal enum SecurityControlFlags
        {
            Default = 0x0,
            SkipVisibilityChecks = 0x1,
            RestrictedSkipVisibilityChecks = 0x2,
            HasCreationContext = 0x4,
            CanSkipCSEvaluation = 0x8,
        }

        internal override RuntimeType GetJitContext(ref int securityControlFlags)
        {
            RuntimeType typeOwner;

            SecurityControlFlags flags = SecurityControlFlags.Default;

            if (m_method.m_restrictedSkipVisibility)
                flags |= SecurityControlFlags.RestrictedSkipVisibilityChecks;
            else if (m_method.m_skipVisibility)
                flags |= SecurityControlFlags.SkipVisibilityChecks;

            typeOwner = m_method.m_typeOwner;

#if FEATURE_COMPRESSEDSTACK
            if (m_method.m_creationContext != null)
            {
                flags |= SecurityControlFlags.HasCreationContext;
                if(m_method.m_creationContext.CanSkipEvaluation)
                {
                    flags |= SecurityControlFlags.CanSkipCSEvaluation;
                }
            }
            
#endif // FEATURE_COMPRESSEDSTACK

            securityControlFlags = (int)flags;

            return typeOwner;
        }

        private static int CalculateNumberOfExceptions(__ExceptionInfo[] excp)
        {
            int num = 0;
            if (excp == null)
                return 0;
            for (int i = 0; i < excp.Length; i++)
                num += excp[i].GetNumberOfCatches();
            return num;
        }

        internal override byte[] GetCodeInfo(
            ref int stackSize, ref int initLocals, ref int EHCount)
        {
            stackSize = m_stackSize;
            if (m_exceptionHeader != null && m_exceptionHeader.Length != 0)
            {
                if (m_exceptionHeader.Length < 4)
                    throw new FormatException();

                byte header = m_exceptionHeader[0];

                if ((header & 0x40) != 0) // Fat
                {
                    byte[] size = new byte[4];
                    for (int q = 0; q < 3; q++)
                        size[q] = m_exceptionHeader[q + 1];
                    EHCount = (BitConverter.ToInt32(size, 0) - 4) / 24;
                }
                else
                    EHCount = (m_exceptionHeader[1] - 2) / 12;
            }
            else
            {
                EHCount = CalculateNumberOfExceptions(m_exceptions);
            }
            initLocals = (m_method.InitLocals) ? 1 : 0;
            return m_code;
        }

        internal override byte[] GetLocalsSignature()
        {
            return m_localSignature;
        }

        internal override unsafe byte[] GetRawEHInfo()
        {
            return m_exceptionHeader;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe void GetEHInfo(int excNumber, void* exc)
        {
            CORINFO_EH_CLAUSE* exception = (CORINFO_EH_CLAUSE*)exc;
            for (int i = 0; i < m_exceptions.Length; i++)
            {
                int excCount = m_exceptions[i].GetNumberOfCatches();
                if (excNumber < excCount)
                {
                    // found the right exception block
                    exception->Flags = m_exceptions[i].GetExceptionTypes()[excNumber];
                    exception->TryOffset = m_exceptions[i].GetStartAddress();
                    if ((exception->Flags & __ExceptionInfo.Finally) != __ExceptionInfo.Finally)
                        exception->TryLength = m_exceptions[i].GetEndAddress() - exception->TryOffset;
                    else
                        exception->TryLength = m_exceptions[i].GetFinallyEndAddress() - exception->TryOffset;
                    exception->HandlerOffset = m_exceptions[i].GetCatchAddresses()[excNumber];
                    exception->HandlerLength = m_exceptions[i].GetCatchEndAddresses()[excNumber] - exception->HandlerOffset;
                    // this is cheating because the filter address is the token of the class only for light code gen
                    exception->ClassTokenOrFilterOffset = m_exceptions[i].GetFilterAddresses()[excNumber];
                    break;
                }
                excNumber -= excCount;
            }
        }

        internal override String GetStringLiteral(int token) { return m_scope.GetString(token); }

#if FEATURE_COMPRESSEDSTACK
        internal override CompressedStack GetSecurityContext()
        {
            return m_method.m_creationContext;
        }
#endif // FEATURE_COMPRESSEDSTACK

        [System.Security.SecurityCritical]
        internal override void ResolveToken(int token, out IntPtr typeHandle, out IntPtr methodHandle, out IntPtr fieldHandle)
        {
            typeHandle = new IntPtr();
            methodHandle = new IntPtr();
            fieldHandle = new IntPtr();

            Object handle = m_scope[token];

            if (handle == null)
                throw new InvalidProgramException();

            if (handle is RuntimeTypeHandle)
            {
                typeHandle = ((RuntimeTypeHandle)handle).Value;
                return;
            }

            if (handle is RuntimeMethodHandle)
            {
                methodHandle = ((RuntimeMethodHandle)handle).Value;
                return;
            }

            if (handle is RuntimeFieldHandle)
            {
                fieldHandle = ((RuntimeFieldHandle)handle).Value;
                return;
            }

            DynamicMethod dm = handle as DynamicMethod;
            if (dm != null)
            {
                methodHandle = dm.GetMethodDescriptor().Value;
                return;
            }

            GenericMethodInfo gmi = handle as GenericMethodInfo;
            if (gmi != null)
            {
                methodHandle = gmi.m_methodHandle.Value;
                typeHandle = gmi.m_context.Value;
                return;
            }

            GenericFieldInfo gfi = handle as GenericFieldInfo;
            if (gfi != null)
            {
                fieldHandle = gfi.m_fieldHandle.Value;
                typeHandle = gfi.m_context.Value;
                return;
            }

            VarArgMethod vaMeth = handle as VarArgMethod;
            if (vaMeth != null)
            {
                if (vaMeth.m_dynamicMethod == null)
                {
                    methodHandle = vaMeth.m_method.MethodHandle.Value;
                    typeHandle = vaMeth.m_method.GetDeclaringTypeInternal().GetTypeHandleInternal().Value;
                }
                else
                    methodHandle = vaMeth.m_dynamicMethod.GetMethodDescriptor().Value;

                return;
            }
        }

        internal override byte[] ResolveSignature(int token, int fromMethod)
        {
            return m_scope.ResolveSignature(token, fromMethod);
        }

        internal override MethodInfo GetDynamicMethod()
        {
            return m_method.GetMethodInfo();
        }
        #endregion
    }


#if FEATURE_CORECLR
[System.Security.SecurityCritical] // auto-generated
#endif
    [System.Runtime.InteropServices.ComVisible(true)]
    public class DynamicILInfo
    {
        #region Private Data Members
        private DynamicMethod m_method;
        private DynamicScope m_scope;
        private byte[] m_exceptions;
        private byte[] m_code;
        private byte[] m_localSignature;
        private int m_maxStackSize;
        private int m_methodSignature;
        #endregion

        #region Constructor
        internal DynamicILInfo(DynamicScope scope, DynamicMethod method, byte[] methodSignature)
        {
            m_method = method;
            m_scope = scope;
            m_methodSignature = m_scope.GetTokenFor(methodSignature);
            m_exceptions = EmptyArray<Byte>.Value;
            m_code = EmptyArray<Byte>.Value;
            m_localSignature = EmptyArray<Byte>.Value;
        }
        #endregion

        #region Internal Methods
        [System.Security.SecurityCritical]  // auto-generated
        internal void GetCallableMethod(RuntimeModule module, DynamicMethod dm)
        {
            dm.m_methodHandle = ModuleHandle.GetDynamicMethod(dm,
                module, m_method.Name, (byte[])m_scope[m_methodSignature], new DynamicResolver(this));
        }

        internal byte[] LocalSignature
        {
            get
            {
                if (m_localSignature == null)
                    m_localSignature = SignatureHelper.GetLocalVarSigHelper().InternalGetSignatureArray();

                return m_localSignature;
            }
        }
        internal byte[] Exceptions { get { return m_exceptions; } }
        internal byte[] Code { get { return m_code; } }
        internal int MaxStackSize { get { return m_maxStackSize; } }
        #endregion

        #region Public ILGenerator Methods
        public DynamicMethod DynamicMethod { get { return m_method; } }
        internal DynamicScope DynamicScope { get { return m_scope; } }

        public void SetCode(byte[] code, int maxStackSize)
        {
            m_code = (code != null) ? (byte[])code.Clone() : EmptyArray<Byte>.Value;
            m_maxStackSize = maxStackSize;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        public unsafe void SetCode(byte* code, int codeSize, int maxStackSize)
        {
            if (codeSize < 0)
                throw new ArgumentOutOfRangeException("codeSize", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));

            if (codeSize > 0 && code == null)
                throw new ArgumentNullException("code");
            Contract.EndContractBlock();

            m_code = new byte[codeSize];
            for (int i = 0; i < codeSize; i++)
            {
                m_code[i] = *code;
                code++;
            }

            m_maxStackSize = maxStackSize;
        }

        public void SetExceptions(byte[] exceptions)
        {
            m_exceptions = (exceptions != null) ? (byte[])exceptions.Clone() : EmptyArray<Byte>.Value;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        public unsafe void SetExceptions(byte* exceptions, int exceptionsSize)
        {
            if (exceptionsSize < 0)
                throw new ArgumentOutOfRangeException("exceptionsSize", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));

            if (exceptionsSize > 0 && exceptions == null)
                throw new ArgumentNullException("exceptions");
            Contract.EndContractBlock();

            m_exceptions = new byte[exceptionsSize];

            for (int i = 0; i < exceptionsSize; i++)
            {
                m_exceptions[i] = *exceptions;
                exceptions++;
            }
        }

        public void SetLocalSignature(byte[] localSignature)
        {
            m_localSignature = (localSignature != null) ? (byte[])localSignature.Clone() : EmptyArray<Byte>.Value;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        public unsafe void SetLocalSignature(byte* localSignature, int signatureSize)
        {
            if (signatureSize < 0)
                throw new ArgumentOutOfRangeException("signatureSize", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));

            if (signatureSize > 0 && localSignature == null)
                throw new ArgumentNullException("localSignature");
            Contract.EndContractBlock();

            m_localSignature = new byte[signatureSize];
            for (int i = 0; i < signatureSize; i++)
            {
                m_localSignature[i] = *localSignature;
                localSignature++;
            }
        }
        #endregion

        #region Public Scope Methods
        [System.Security.SecuritySafeCritical]  // auto-generated
        public int GetTokenFor(RuntimeMethodHandle method)
        {
            return DynamicScope.GetTokenFor(method);
        }
        public int GetTokenFor(DynamicMethod method)
        {
            return DynamicScope.GetTokenFor(method);
        }
        public int GetTokenFor(RuntimeMethodHandle method, RuntimeTypeHandle contextType)
        {
            return DynamicScope.GetTokenFor(method, contextType);
        }
        public int GetTokenFor(RuntimeFieldHandle field)
        {
            return DynamicScope.GetTokenFor(field);
        }
        public int GetTokenFor(RuntimeFieldHandle field, RuntimeTypeHandle contextType)
        {
            return DynamicScope.GetTokenFor(field, contextType);
        }
        public int GetTokenFor(RuntimeTypeHandle type)
        {
            return DynamicScope.GetTokenFor(type);
        }
        public int GetTokenFor(string literal)
        {
            return DynamicScope.GetTokenFor(literal);
        }
        public int GetTokenFor(byte[] signature)
        {
            return DynamicScope.GetTokenFor(signature);
        }
        #endregion
    }

    internal class DynamicScope
    {
        #region Private Data Members
        internal List<Object> m_tokens;
        #endregion

        #region Constructor
        internal unsafe DynamicScope()
        {
            m_tokens = new List<Object>();
            m_tokens.Add(null);
        }
        #endregion

        #region Internal Methods
        internal object this[int token]
        {
            get
            {
                token &= 0x00FFFFFF;

                if (token < 0 || token > m_tokens.Count)
                    return null;

                return m_tokens[token];
            }
        }

        internal int GetTokenFor(VarArgMethod varArgMethod)
        {
            m_tokens.Add(varArgMethod);
            return m_tokens.Count - 1 | (int)MetadataTokenType.MemberRef;
        }
        internal string GetString(int token) { return this[token] as string; }

        internal byte[] ResolveSignature(int token, int fromMethod)
        {
            if (fromMethod == 0)
                return (byte[])this[token];

            VarArgMethod vaMethod = this[token] as VarArgMethod;

            if (vaMethod == null)
                return null;

            return vaMethod.m_signature.GetSignature(true);
        }
        #endregion

        #region Public Methods
        [System.Security.SecuritySafeCritical]  // auto-generated
        public int GetTokenFor(RuntimeMethodHandle method)
        {
            IRuntimeMethodInfo methodReal = method.GetMethodInfo();
            RuntimeMethodHandleInternal rmhi = methodReal.Value;

            if (methodReal != null && !RuntimeMethodHandle.IsDynamicMethod(rmhi))
            {
                RuntimeType type = RuntimeMethodHandle.GetDeclaringType(rmhi);
                if ((type != null) && RuntimeTypeHandle.HasInstantiation(type))
                {
                    // Do we really need to retrieve this much info just to throw an exception?
                    MethodBase m = RuntimeType.GetMethodBase(methodReal);
                    Type t = m.DeclaringType.GetGenericTypeDefinition();

                    throw new ArgumentException(String.Format(
                        CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_MethodDeclaringTypeGenericLcg"), m, t));
                }
            }

            m_tokens.Add(method);
            return m_tokens.Count - 1 | (int)MetadataTokenType.MethodDef;
        }
        public int GetTokenFor(RuntimeMethodHandle method, RuntimeTypeHandle typeContext)
        {
            m_tokens.Add(new GenericMethodInfo(method, typeContext));
            return m_tokens.Count - 1 | (int)MetadataTokenType.MethodDef;
        }
        public int GetTokenFor(DynamicMethod method)
        {
            m_tokens.Add(method);
            return m_tokens.Count - 1 | (int)MetadataTokenType.MethodDef;
        }
        public int GetTokenFor(RuntimeFieldHandle field)
        {
            m_tokens.Add(field);
            return m_tokens.Count - 1 | (int)MetadataTokenType.FieldDef;
        }
        public int GetTokenFor(RuntimeFieldHandle field, RuntimeTypeHandle typeContext)
        {
            m_tokens.Add(new GenericFieldInfo(field, typeContext));
            return m_tokens.Count - 1 | (int)MetadataTokenType.FieldDef;
        }
        public int GetTokenFor(RuntimeTypeHandle type)
        {
            m_tokens.Add(type);
            return m_tokens.Count - 1 | (int)MetadataTokenType.TypeDef;
        }
        public int GetTokenFor(string literal)
        {
            m_tokens.Add(literal);
            return m_tokens.Count - 1 | (int)MetadataTokenType.String;
        }
        public int GetTokenFor(byte[] signature)
        {
            m_tokens.Add(signature);
            return m_tokens.Count - 1 | (int)MetadataTokenType.Signature;
        }
        #endregion
    }

    internal sealed class GenericMethodInfo
    {
        internal RuntimeMethodHandle m_methodHandle;
        internal RuntimeTypeHandle m_context;

        internal GenericMethodInfo(RuntimeMethodHandle methodHandle, RuntimeTypeHandle context)
        {
            m_methodHandle = methodHandle;
            m_context = context;
        }
    }

    internal sealed class GenericFieldInfo
    {
        internal RuntimeFieldHandle m_fieldHandle;
        internal RuntimeTypeHandle m_context;

        internal GenericFieldInfo(RuntimeFieldHandle fieldHandle, RuntimeTypeHandle context)
        {
            m_fieldHandle = fieldHandle;
            m_context = context;
        }
    }

    internal sealed class VarArgMethod
    {
        internal RuntimeMethodInfo m_method;
        internal DynamicMethod m_dynamicMethod;
        internal SignatureHelper m_signature;

        internal VarArgMethod(DynamicMethod dm, SignatureHelper signature)
        {
            m_dynamicMethod = dm;
            m_signature = signature;
        }

        internal VarArgMethod(RuntimeMethodInfo method, SignatureHelper signature)
        {
            m_method = method;
            m_signature = signature;
        }
    }

}
