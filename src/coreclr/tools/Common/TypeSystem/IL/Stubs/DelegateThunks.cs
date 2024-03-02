// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Base class for all delegate invocation thunks.
    /// </summary>
    public abstract partial class DelegateThunk : ILStubMethod
    {
        private DelegateInfo _delegateInfo;

        public DelegateThunk(DelegateInfo delegateInfo)
        {
            _delegateInfo = delegateInfo;
        }

        public sealed override TypeSystemContext Context
        {
            get
            {
                return _delegateInfo.Type.Context;
            }
        }

        public sealed override TypeDesc OwningType
        {
            get
            {
                return _delegateInfo.Type;
            }
        }

        public sealed override MethodSignature Signature
        {
            get
            {
                return _delegateInfo.Signature;
            }
        }

        public sealed override Instantiation Instantiation
        {
            get
            {
                return Instantiation.Empty;
            }
        }

        protected TypeDesc SystemDelegateType
        {
            get
            {
                return Context.GetWellKnownType(WellKnownType.MulticastDelegate).BaseType;
            }
        }

        protected FieldDesc ExtraFunctionPointerOrDataField
        {
            get
            {
                return SystemDelegateType.GetKnownField("m_extraFunctionPointerOrData");
            }
        }

        protected FieldDesc HelperObjectField
        {
            get
            {
                return SystemDelegateType.GetKnownField("m_helperObject");
            }
        }

        protected FieldDesc FirstParameterField
        {
            get
            {
                return SystemDelegateType.GetKnownField("m_firstParameter");
            }
        }

        protected FieldDesc FunctionPointerField
        {
            get
            {
                return SystemDelegateType.GetKnownField("m_functionPointer");
            }
        }

        public sealed override string DiagnosticName
        {
            get
            {
                return Name;
            }
        }
    }

    /// <summary>
    /// Invoke thunk for open delegates to static methods. Loads all arguments except
    /// the 'this' pointer and performs an indirect call to the delegate target.
    /// This method is injected into delegate types.
    /// </summary>
    public sealed partial class DelegateInvokeOpenStaticThunk : DelegateThunk
    {
        internal DelegateInvokeOpenStaticThunk(DelegateInfo delegateInfo)
            : base(delegateInfo)
        {
        }

        public override MethodIL EmitIL()
        {
            // Target has the same signature as the Invoke method, except it's static.
            MethodSignatureBuilder builder = new MethodSignatureBuilder(Signature);
            builder.Flags = Signature.Flags | MethodSignatureFlags.Static;

            var emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();

            // Load all arguments except 'this'
            for (int i = 0; i < Signature.Length; i++)
            {
                codeStream.EmitLdArg(i + 1);
            }

            // Indirectly call the delegate target static method.
            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(ExtraFunctionPointerOrDataField));

            codeStream.Emit(ILOpcode.calli, emitter.NewToken(builder.ToSignature()));

            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(this);
        }

        public override string Name
        {
            get
            {
                return "InvokeOpenStaticThunk";
            }
        }
    }

    /// <summary>
    /// Invoke thunk for open delegates to instance methods. This kind of thunk
    /// uses the first parameter as `this` that gets passed to the target instance method.
    /// The thunk also performs virtual resolution if necessary.
    /// This kind of delegates is typically created with Delegate.CreateDelegate
    /// and MethodInfo.CreateDelegate at runtime.
    /// </summary>
    public sealed partial class DelegateInvokeOpenInstanceThunk : DelegateThunk
    {
        internal DelegateInvokeOpenInstanceThunk(DelegateInfo delegateInfo)
            : base(delegateInfo)
        {
        }

        public override MethodIL EmitIL()
        {
            Debug.Assert(Signature.Length > 0);

            var emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();

            // Load all arguments except delegate's 'this'
            TypeDesc boxThisType = null;
            TypeDesc[] parameters = new TypeDesc[Signature.Length - 1];
            for (int i = 0; i < Signature.Length; i++)
            {
                codeStream.EmitLdArg(i + 1);

                if (i == 0)
                {
                    // Ensure that we're working with an object type by boxing it here.
                    // This is to allow delegates which are generic over thier first parameter
                    // to have valid code in their thunk.
                    if (Signature[i].IsSignatureVariable)
                    {
                        boxThisType = Signature[i];
                        codeStream.Emit(ILOpcode.box, emitter.NewToken(boxThisType));
                    }
                }
                else
                {
                    parameters[i - 1] = Signature[i];
                }
            }

            // Call a helper to get the actual method target
            codeStream.EmitLdArg(0);

            if (Signature[0].IsByRef)
            {
                codeStream.Emit(ILOpcode.ldnull);
            }
            else
            {
                codeStream.EmitLdArg(1);
                if (boxThisType != null)
                {
                    codeStream.Emit(ILOpcode.box, emitter.NewToken(boxThisType));
                }
            }

            codeStream.Emit(ILOpcode.call, emitter.NewToken(SystemDelegateType.GetKnownMethod("GetActualTargetFunctionPointer", null)));

            MethodSignature targetSignature = new MethodSignature(0, 0, Signature.ReturnType, parameters);
            codeStream.Emit(ILOpcode.calli, emitter.NewToken(targetSignature));
            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(this);
        }

        public override string Name
        {
            get
            {
                return "InvokeOpenInstanceThunk";
            }
        }
    }

    /// <summary>
    /// Invoke thunk for closed delegates to static methods. The target
    /// is a static method, but the first argument is captured by the delegate.
    /// The signature of the target has an extra object-typed argument, followed
    /// by the arguments that are delegate-compatible with the thunk signature.
    /// This method is injected into delegate types.
    /// </summary>
    public sealed partial class DelegateInvokeClosedStaticThunk : DelegateThunk
    {
        internal DelegateInvokeClosedStaticThunk(DelegateInfo delegateInfo)
            : base(delegateInfo)
        {
        }

        public override MethodIL EmitIL()
        {
            TypeDesc[] targetMethodParameters = new TypeDesc[Signature.Length + 1];
            targetMethodParameters[0] = Context.GetWellKnownType(WellKnownType.Object);

            for (int i = 0; i < Signature.Length; i++)
            {
                targetMethodParameters[i + 1] = Signature[i];
            }

            var targetMethodSignature = new MethodSignature(
                Signature.Flags | MethodSignatureFlags.Static, 0, Signature.ReturnType, targetMethodParameters);

            var emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();

            // Load the stored 'this'
            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(HelperObjectField));

            // Load all arguments except 'this'
            for (int i = 0; i < Signature.Length; i++)
            {
                codeStream.EmitLdArg(i + 1);
            }

            // Indirectly call the delegate target static method.
            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(ExtraFunctionPointerOrDataField));

            codeStream.Emit(ILOpcode.calli, emitter.NewToken(targetMethodSignature));

            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(this);
        }

        public override string Name
        {
            get
            {
                return "InvokeClosedStaticThunk";
            }
        }
    }

    /// <summary>
    /// Multicast invoke thunk for delegates that are a result of Delegate.Combine.
    /// Passes it's arguments to each of the delegates that got combined and calls them
    /// one by one. Returns the value of the last delegate executed.
    /// This method is injected into delegate types.
    /// </summary>
    public sealed partial class DelegateInvokeMulticastThunk : DelegateThunk
    {
        internal DelegateInvokeMulticastThunk(DelegateInfo delegateInfo)
            : base(delegateInfo)
        {
        }

        public override MethodIL EmitIL()
        {
            ILEmitter emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();

            TypeDesc delegateWrapperType = ((MetadataType)SystemDelegateType).GetKnownNestedType("Wrapper");
            ArrayType invocationListArrayType = delegateWrapperType.MakeArrayType();

            ILLocalVariable delegateArrayLocal = emitter.NewLocal(invocationListArrayType);
            ILLocalVariable invocationCountLocal = emitter.NewLocal(Context.GetWellKnownType(WellKnownType.Int32));
            ILLocalVariable iteratorLocal = emitter.NewLocal(Context.GetWellKnownType(WellKnownType.Int32));
            ILLocalVariable delegateToCallLocal = emitter.NewLocal(SystemDelegateType);

            ILLocalVariable returnValueLocal = 0;
            if (!Signature.ReturnType.IsVoid)
            {
                returnValueLocal = emitter.NewLocal(Signature.ReturnType);
            }

            // Fill in delegateArrayLocal
            // Wrapper[] delegateArrayLocal = (Wrapper[])this.m_helperObject

            // ldarg.0 (this pointer)
            // ldfld Delegate.HelperObjectField
            // castclass Wrapper[]
            // stloc delegateArrayLocal
            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(HelperObjectField));
            codeStream.Emit(ILOpcode.castclass, emitter.NewToken(invocationListArrayType));
            codeStream.EmitStLoc(delegateArrayLocal);

            // Fill in invocationCountLocal
            // int invocationCountLocal = this.m_extraFunctionPointerOrData
            // ldarg.0 (this pointer)
            // ldfld Delegate.m_extraFunctionPointerOrData
            // stloc invocationCountLocal
            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(ExtraFunctionPointerOrDataField));
            codeStream.EmitStLoc(invocationCountLocal);

            // Fill in iteratorLocal
            // int iteratorLocal = 0;

            // ldc.0
            // stloc iteratorLocal
            codeStream.EmitLdc(0);
            codeStream.EmitStLoc(iteratorLocal);

            // Loop across every element of the array.
            ILCodeLabel startOfLoopLabel = emitter.NewCodeLabel();
            codeStream.EmitLabel(startOfLoopLabel);

            // Implement as do/while loop. We only have this stub in play if we're in the multicast situation
            // Find the delegate to call
            // Delegate = delegateToCallLocal = delegateArrayLocal[iteratorLocal].Value;

            // ldloc delegateArrayLocal
            // ldloc iteratorLocal
            // ldelema System.Delegate
            // ldfld Wrapper.Value
            // stloc delegateToCallLocal
            codeStream.EmitLdLoc(delegateArrayLocal);
            codeStream.EmitLdLoc(iteratorLocal);
            codeStream.Emit(ILOpcode.ldelema, emitter.NewToken(delegateWrapperType));
            codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(delegateWrapperType.GetKnownField("Value")));
            codeStream.EmitStLoc(delegateToCallLocal);

            // Call the delegate
            // returnValueLocal = delegateToCallLocal(...);

            // ldloc delegateToCallLocal
            // ldfld System.Delegate.m_firstParameter
            // ldarg 1, n
            // ldloc delegateToCallLocal
            // ldfld System.Delegate.m_functionPointer
            // calli returnValueType thiscall (all the params)
            // IF there is a return value
            // stloc returnValueLocal

            codeStream.EmitLdLoc(delegateToCallLocal);
            codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(FirstParameterField));

            for (int i = 0; i < Signature.Length; i++)
            {
                codeStream.EmitLdArg(i + 1);
            }

            codeStream.EmitLdLoc(delegateToCallLocal);
            codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(FunctionPointerField));

            codeStream.Emit(ILOpcode.calli, emitter.NewToken(Signature));

            if (returnValueLocal != 0)
                codeStream.EmitStLoc(returnValueLocal);

            // Increment iteratorLocal
            // ++iteratorLocal;

            // ldloc iteratorLocal
            // ldc.i4.1
            // add
            // stloc iteratorLocal
            codeStream.EmitLdLoc(iteratorLocal);
            codeStream.EmitLdc(1);
            codeStream.Emit(ILOpcode.add);
            codeStream.EmitStLoc(iteratorLocal);

            // Check to see if the loop is done
            codeStream.EmitLdLoc(invocationCountLocal);
            codeStream.EmitLdLoc(iteratorLocal);
            codeStream.Emit(ILOpcode.bne_un, startOfLoopLabel);

            // Return to caller. If the delegate has a return value, be certain to return that.
            // return returnValueLocal;

            // ldloc returnValueLocal
            // ret
            if (returnValueLocal != 0)
                codeStream.EmitLdLoc(returnValueLocal);

            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(this);
        }

        public override string Name
        {
            get
            {
                return "InvokeMulticastThunk";
            }
        }
    }

    /// <summary>
    /// Invoke thunk for delegates that point to closed instance generic methods.
    /// These need a thunk because the function pointer to invoke might be a fat function
    /// pointer and we need a calli to unwrap it, inject the hidden argument, shuffle the
    /// rest of the arguments, and call the unwrapped function pointer.
    /// </summary>
    public sealed partial class DelegateInvokeInstanceClosedOverGenericMethodThunk : DelegateThunk
    {
        internal DelegateInvokeInstanceClosedOverGenericMethodThunk(DelegateInfo delegateInfo)
            : base(delegateInfo)
        {
        }

        public override MethodIL EmitIL()
        {
            var emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();

            // Load the stored 'this'
            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(HelperObjectField));

            // Load all arguments except 'this'
            for (int i = 0; i < Signature.Length; i++)
            {
                codeStream.EmitLdArg(i + 1);
            }

            // Indirectly call the delegate target
            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(ExtraFunctionPointerOrDataField));

            codeStream.Emit(ILOpcode.calli, emitter.NewToken(Signature));

            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(this);
        }

        public override string Name
        {
            get
            {
                return "InvokeInstanceClosedOverGenericMethodThunk";
            }
        }
    }

    /// <summary>
    /// Reverse invocation stub which goes from the strongly typed parameters the delegate
    /// accepts, converts them into an object array, and invokes a delegate with the
    /// object array, and then casts and returns the result back.
    /// This is used to support delegates pointing to the LINQ expression interpreter.
    /// </summary>
    public sealed partial class DelegateInvokeObjectArrayThunk : DelegateThunk
    {
        internal DelegateInvokeObjectArrayThunk(DelegateInfo delegateInfo)
            : base(delegateInfo)
        {
        }

        public override MethodIL EmitIL()
        {
            // We will generate the following code:
            //
            // object ret;
            // object[] args = new object[parameterCount];
            // args[0] = param0;
            // args[1] = param1;
            //  ...
            // try {
            //      ret = ((Func<object[], object>)dlg.m_helperObject)(args);
            // } finally {
            //      param0 = (T0)args[0];   // only generated for each byref argument
            // }
            // return (TRet)ret;

            ILEmitter emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();

            TypeDesc objectType = Context.GetWellKnownType(WellKnownType.Object);
            TypeDesc objectArrayType = objectType.MakeArrayType();

            ILLocalVariable argsLocal = emitter.NewLocal(objectArrayType);

            bool hasReturnValue = !Signature.ReturnType.IsVoid;

            bool hasRefArgs = false;
            if (Signature.Length > 0)
            {
                codeStream.EmitLdc(Signature.Length);
                codeStream.Emit(ILOpcode.newarr, emitter.NewToken(objectType));
                codeStream.EmitStLoc(argsLocal);

                for (int i = 0; i < Signature.Length; i++)
                {
                    TypeDesc paramType = Signature[i];
                    bool paramIsByRef = false;

                    if (paramType.IsByRef)
                    {
                        hasRefArgs |= paramType.IsByRef;
                        paramIsByRef = true;
                        paramType = ((ByRefType)paramType).ParameterType;
                    }

                    hasRefArgs |= paramType.IsByRef;

                    codeStream.EmitLdLoc(argsLocal);
                    codeStream.EmitLdc(i);
                    codeStream.EmitLdArg(i + 1);

                    ILToken paramToken = emitter.NewToken(paramType);

                    if (paramIsByRef)
                    {
                        codeStream.Emit(ILOpcode.ldobj, paramToken);
                    }
                    codeStream.Emit(ILOpcode.box, paramToken);
                    codeStream.Emit(ILOpcode.stelem_ref);
                }
            }
            else
            {
                MethodDesc emptyObjectArrayMethod = Context.GetHelperEntryPoint("DelegateHelpers", "GetEmptyObjectArray");
                codeStream.Emit(ILOpcode.call, emitter.NewToken(emptyObjectArrayMethod));
                codeStream.EmitStLoc(argsLocal);
            }

            ILExceptionRegionBuilder tryFinallyRegion = null;
            if (hasRefArgs)
            {
                // we emit a try/finally to update the args array even if an exception is thrown
                tryFinallyRegion = emitter.NewFinallyRegion();
                codeStream.BeginTry(tryFinallyRegion);
            }

            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.ldfld, emitter.NewToken(HelperObjectField));

            MetadataType funcType = Context.SystemModule.GetKnownType("System", "Func`2");
            TypeDesc instantiatedFunc = funcType.MakeInstantiatedType(objectArrayType, objectType);

            codeStream.Emit(ILOpcode.castclass, emitter.NewToken(instantiatedFunc));

            codeStream.EmitLdLoc(argsLocal);

            MethodDesc invokeMethod = instantiatedFunc.GetKnownMethod("Invoke", null);
            codeStream.Emit(ILOpcode.callvirt, emitter.NewToken(invokeMethod));

            ILLocalVariable retLocal = (ILLocalVariable)(-1);
            if (hasReturnValue)
            {
                retLocal = emitter.NewLocal(objectType);
                codeStream.EmitStLoc(retLocal);
            }
            else
            {
                codeStream.Emit(ILOpcode.pop);
            }

            if (hasRefArgs)
            {
                ILCodeLabel returnLabel = emitter.NewCodeLabel();
                codeStream.Emit(ILOpcode.leave, returnLabel);
                codeStream.EndTry(tryFinallyRegion);

                // copy back ref/out args
                codeStream.BeginHandler(tryFinallyRegion);
                for (int i = 0; i < Signature.Length; i++)
                {
                    TypeDesc paramType = Signature[i];
                    if (paramType.IsByRef)
                    {
                        paramType = ((ByRefType)paramType).ParameterType;
                        ILToken paramToken = emitter.NewToken(paramType);

                        // Update parameter
                        codeStream.EmitLdArg(i + 1);
                        codeStream.EmitLdLoc(argsLocal);
                        codeStream.EmitLdc(i);
                        codeStream.Emit(ILOpcode.ldelem_ref);
                        codeStream.Emit(ILOpcode.unbox_any, paramToken);
                        codeStream.Emit(ILOpcode.stobj, paramToken);
                    }
                }
                codeStream.Emit(ILOpcode.endfinally);
                codeStream.EndHandler(tryFinallyRegion);
                codeStream.EmitLabel(returnLabel);
            }

            if (hasReturnValue)
            {
                codeStream.EmitLdLoc(retLocal);
                codeStream.Emit(ILOpcode.unbox_any, emitter.NewToken(Signature.ReturnType));
            }

            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(this);
        }

        public override string Name
        {
            get
            {
                return "InvokeObjectArrayThunk";
            }
        }
    }

    /// <summary>
    /// Synthetic method override of "IntPtr Delegate.GetThunk(Int32)". This method is injected
    /// into all delegate types and provides means for System.Delegate to access the various thunks
    /// generated by the compiler.
    /// </summary>
    public sealed partial class DelegateGetThunkMethodOverride : ILStubMethod
    {
        private DelegateInfo _delegateInfo;
        private MethodSignature _signature;

        internal DelegateGetThunkMethodOverride(DelegateInfo delegateInfo)
        {
            _delegateInfo = delegateInfo;
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _delegateInfo.Type.Context;
            }
        }

        public override TypeDesc OwningType
        {
            get
            {
                return _delegateInfo.Type;
            }
        }

        public override MethodSignature Signature
        {
            get
            {
                if (_signature == null)
                {
                    TypeSystemContext context = _delegateInfo.Type.Context;
                    TypeDesc intPtrType = context.GetWellKnownType(WellKnownType.IntPtr);
                    TypeDesc int32Type = context.GetWellKnownType(WellKnownType.Int32);

                    _signature = new MethodSignature(0, 0, intPtrType, new[] { int32Type });
                }

                return _signature;
            }
        }

        public override MethodIL EmitIL()
        {
            ILEmitter emitter = new ILEmitter();

            var codeStream = emitter.NewCodeStream();

            ILCodeLabel returnNullLabel = emitter.NewCodeLabel();

            ILCodeLabel[] labels = new ILCodeLabel[(int)DelegateThunkCollection.MaxThunkKind];
            for (DelegateThunkKind i = 0; i < DelegateThunkCollection.MaxThunkKind; i++)
            {
                MethodDesc thunk = _delegateInfo.Thunks[i];
                if (thunk != null)
                    labels[(int)i] = emitter.NewCodeLabel();
                else
                    labels[(int)i] = returnNullLabel;
            }

            codeStream.EmitLdArg(1);
            codeStream.EmitSwitch(labels);

            codeStream.Emit(ILOpcode.br, returnNullLabel);

            for (DelegateThunkKind i = 0; i < DelegateThunkCollection.MaxThunkKind; i++)
            {
                MethodDesc thunk = _delegateInfo.Thunks[i];
                if (thunk == null)
                    continue;

                MethodDesc targetMethod = thunk.InstantiateAsOpen();

                codeStream.EmitLabel(labels[(int)i]);
                codeStream.Emit(ILOpcode.ldftn, emitter.NewToken(targetMethod));
                codeStream.Emit(ILOpcode.ret);
            }

            codeStream.EmitLabel(returnNullLabel);
            codeStream.EmitLdc(0);
            codeStream.Emit(ILOpcode.conv_i);
            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(this);
        }

        public override Instantiation Instantiation
        {
            get
            {
                return Instantiation.Empty;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return true;
            }
        }

        public override string Name
        {
            get
            {
                return "GetThunk";
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return "GetThunk";
            }
        }
    }
}
