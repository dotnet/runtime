// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using Interlocked = System.Threading.Interlocked;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Thunk to dynamically invoke a method using reflection. The method accepts an object[] of parameters
    /// to target method, lays them out on the stack, and calls the target method. This thunk has heavy
    /// dependencies on the general dynamic invocation infrastructure in System.InvokeUtils and gets called from there
    /// at runtime. See comments in System.InvokeUtils for a more thorough explanation.
    /// </summary>
    public partial class DynamicInvokeMethodThunk : ILStubMethod
    {
        private TypeDesc _owningType;
        private DynamicInvokeMethodSignature _targetSignature;

        private TypeDesc[] _instantiation;
        private MethodSignature _signature;

        public DynamicInvokeMethodThunk(TypeDesc owningType, DynamicInvokeMethodSignature signature)
        {
            _owningType = owningType;
            _targetSignature = signature;
        }

        internal static bool SupportsDynamicInvoke(TypeSystemContext context)
        {
            return context.SystemModule.GetType("System", "InvokeUtils", throwIfNotFound: false) != null;
        }

        private static TypeDesc UnwrapByRef(TypeDesc type)
        {
            if (type.IsByRef)
                return ((ByRefType)type).ParameterType;
            return type;
        }

        private static bool ContainsFunctionPointer(TypeDesc type)
        {
            if (type.IsFunctionPointer)
                return true;
            else if (type.IsParameterizedType)
                return ContainsFunctionPointer(((ParameterizedType)type).ParameterType);
            else
                return false;
        }

        public static bool SupportsSignature(MethodSignature signature)
        {
            // ----------------------------------------------------------------
            // TODO: function pointer types are odd: https://github.com/dotnet/corert/issues/1929
            // ----------------------------------------------------------------

            // ----------------------------------------------------------------
            // Methods that take or return ByRef-like types can't be reflection invoked
            //
            // TODO: CoreCLR allows invoking methods that take ByRef-like types by value when the argument has
            // the default null value. It is a corner case that is unlikely to be exercised in practice.
            // ----------------------------------------------------------------

            TypeDesc unwrappedReturnType = UnwrapByRef(signature.ReturnType);
            if (ContainsFunctionPointer(unwrappedReturnType))
                return false;
            if (!unwrappedReturnType.IsSignatureVariable && unwrappedReturnType.IsByRefLike)
                return false;

            for (int i = 0; i < signature.Length; i++)
            {
                TypeDesc unwrappedParameterType = UnwrapByRef(signature[i]);
                if (ContainsFunctionPointer(unwrappedParameterType))
                    return false;
                if (!unwrappedParameterType.IsSignatureVariable && unwrappedParameterType.IsByRefLike)
                    return false;
            }

            return true;
        }

        public static TypeDesc[] GetThunkInstantiationForMethod(MethodDesc method)
        {
            MethodSignature sig = method.Signature;

            ParameterMetadata[] paramMetadata = null;
            TypeDesc[] instantiation = new TypeDesc[sig.ReturnType.IsVoid ? sig.Length : sig.Length + 1];

            for (int i = 0; i < sig.Length; i++)
            {
                TypeDesc parameterType = sig[i];
                if (parameterType.IsByRef)
                {
                    // strip ByRefType off the parameter (the method already has ByRef in the signature)
                    parameterType = ((ByRefType)parameterType).ParameterType;

                    // Strip off all the pointers. Pointers are not valid instantiation arguments and the thunk compensates for that
                    // by being specialized for the specific pointer depth.
                    while (parameterType.IsPointer)
                        parameterType = ((PointerType)parameterType).ParameterType;
                }
                else if (parameterType.IsPointer)
                {
                    // Strip off all the pointers. Pointers are not valid instantiation arguments and the thunk compensates for that
                    // by being specialized for the specific pointer depth.
                    while (parameterType.IsPointer)
                        parameterType = ((PointerType)parameterType).ParameterType;
                }
                else if (parameterType.IsEnum)
                {
                    // If the invoke method takes an enum as an input parameter and there is no default value for
                    // that paramter, we don't need to specialize on the exact enum type (we only need to specialize
                    // on the underlying integral type of the enum.)
                    if (paramMetadata == null)
                        paramMetadata = method.GetParameterMetadata();

                    bool hasDefaultValue = false;
                    foreach (var p in paramMetadata)
                    {
                        // Parameter metadata indexes are 1-based (0 is reserved for return "parameter")
                        if (p.Index == (i + 1) && p.HasDefault)
                        {
                            hasDefaultValue = true;
                            break;
                        }
                    }

                    if (!hasDefaultValue)
                        parameterType = parameterType.UnderlyingType;
                }

                instantiation[i] = parameterType;
            }

            if (!sig.ReturnType.IsVoid)
            {
                TypeDesc returnType = sig.ReturnType;

                // strip ByRefType off the return type (the method already has ByRef in the signature)
                if (returnType.IsByRef)
                    returnType = ((ByRefType)returnType).ParameterType;

                // If the invoke method return an object reference, we don't need to specialize on the
                // exact type of the object reference, as the behavior is not different.
                if ((returnType.IsDefType && !returnType.IsValueType) || returnType.IsArray)
                {
                    returnType = method.Context.GetWellKnownType(WellKnownType.Object);
                }

                // Strip off all the pointers. Pointers are not valid instantiation arguments and the thunk compensates for that
                // by being specialized for the specific pointer depth.
                while (returnType.IsPointer)
                    returnType = ((PointerType)returnType).ParameterType;

                instantiation[sig.Length] = returnType;
            }

            return instantiation;
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _owningType.Context;
            }
        }

        public override TypeDesc OwningType
        {
            get
            {
                return _owningType;
            }
        }

        private MetadataType InvokeUtilsType
        {
            get
            {
                return Context.SystemModule.GetKnownType("System", "InvokeUtils");
            }
        }

        private MetadataType ArgSetupStateType
        {
            get
            {
                return InvokeUtilsType.GetNestedType("ArgSetupState");
            }
        }

        public DynamicInvokeMethodSignature TargetSignature
        {
            get
            {
                return _targetSignature;
            }
        }

        public override MethodSignature Signature
        {
            get
            {
                if (_signature == null)
                {
                    _signature = new MethodSignature(
                        MethodSignatureFlags.Static,
                        Instantiation.Length,
                        Context.GetWellKnownType(WellKnownType.Object),
                        new TypeDesc[]
                        {
                            Context.GetWellKnownType(WellKnownType.Object),  // thisPtr
                            Context.GetWellKnownType(WellKnownType.IntPtr),  // methodToCall
                            ArgSetupStateType.MakeByRefType(),               // argSetupState
                            Context.GetWellKnownType(WellKnownType.Boolean), // targetIsThisCall
                        });
                }

                return _signature;
            }
        }

        public override Instantiation Instantiation
        {
            get
            {
                if (_instantiation == null)
                {
                    TypeDesc[] instantiation =
                        new TypeDesc[_targetSignature.HasReturnValue ? _targetSignature.Length + 1 : _targetSignature.Length];

                    for (int i = 0; i < _targetSignature.Length; i++)
                        instantiation[i] = new DynamicInvokeThunkGenericParameter(this, i);

                    if (_targetSignature.HasReturnValue)
                        instantiation[_targetSignature.Length] =
                            new DynamicInvokeThunkGenericParameter(this, _targetSignature.Length);

                    Interlocked.CompareExchange(ref _instantiation, instantiation, null);
                }

                return new Instantiation(_instantiation);
            }
        }

        public override string Name
        {
            get
            {
                StringBuilder sb = new StringBuilder("InvokeRet");

                switch (_targetSignature.ReturnType)
                {
                    case DynamicInvokeMethodParameterKind.None:
                        sb.Append('V');
                        break;
                    case DynamicInvokeMethodParameterKind.Pointer:
                        sb.Append('P');
                        for (int i = 0; i < _targetSignature.GetNumerOfReturnTypePointerIndirections() - 1; i++)
                            sb.Append('p');
                        break;
                    case DynamicInvokeMethodParameterKind.Reference:
                        sb.Append("R");
                        for (int i = 0; i < _targetSignature.GetNumerOfReturnTypePointerIndirections(); i++)
                            sb.Append('p');
                        break;
                    case DynamicInvokeMethodParameterKind.Value:
                        sb.Append('O');
                        break;
                    default:
                        Debug.Fail("Unreachable");
                        break;
                }

                for (int i = 0; i < _targetSignature.Length; i++)
                {
                    switch (_targetSignature[i])
                    {
                        case DynamicInvokeMethodParameterKind.Pointer:
                            sb.Append('P');

                            for (int j = 0; j < _targetSignature.GetNumberOfParameterPointerIndirections(i) - 1; j++)
                                sb.Append('p');

                            break;
                        case DynamicInvokeMethodParameterKind.Reference:
                            sb.Append("R");

                            for (int j = 0; j < _targetSignature.GetNumberOfParameterPointerIndirections(i); j++)
                                sb.Append('p');
                            break;
                        case DynamicInvokeMethodParameterKind.Value:
                            sb.Append("I");
                            break;
                        default:
                            Debug.Fail("Unreachable");
                            break;
                    }
                }

                return sb.ToString();
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return Name;
            }
        }

        public override MethodIL EmitIL()
        {
            ILEmitter emitter = new ILEmitter();
            ILCodeStream argSetupStream = emitter.NewCodeStream();
            ILCodeStream thisCallSiteSetupStream = emitter.NewCodeStream();
            ILCodeStream staticCallSiteSetupStream = emitter.NewCodeStream();
            ILCodeStream returnCodeStream = emitter.NewCodeStream();

            // This function will look like
            //
            // !For each parameter to the method
            //    !if (parameter is In Parameter)
            //       localX is TypeOfParameterX&
            //       ldarg.2
            //       ldtoken TypeOfParameterX
            //       call DynamicInvokeParamHelperIn(ref ArgSetupState, RuntimeTypeHandle)
            //       stloc localX
            //    !else
            //       localX is TypeOfParameter
            //       ldarg.2
            //       ldtoken TypeOfParameterX
            //       call DynamicInvokeParamHelperRef(ref ArgSetupState, RuntimeTypeHandle)
            //       stloc localX

            // ldarg.2
            // call DynamicInvokeArgSetupComplete(ref ArgSetupState)

            // *** Thiscall instruction stream starts here ***

            // ldarg.3 // Load targetIsThisCall
            // brfalse Not_this_call

            // ldarg.0 // Load this pointer
            // !For each parameter
            //    !if (parameter is In Parameter)
            //       ldloc localX
            //       ldobj TypeOfParameterX
            //    !else
            //       ldloc localX
            // ldarg.1
            // calli ReturnType thiscall(TypeOfParameter1, ...)
            // br Process_return

            // *** Static call instruction stream starts here ***

            // Not_this_call:
            // !For each parameter
            //    !if (parameter is In Parameter)
            //       ldloc localX
            //       ldobj TypeOfParameterX
            //    !else
            //       ldloc localX
            // ldarg.1
            // calli ReturnType (TypeOfParameter1, ...)

            // *** Return code stream starts here ***

            // Process_return:
            // !if (ReturnType is Byref)
            //    dup
            //    brfalse ByRefNull
            //    ldobj ReturnType
            // !if ((ReturnType == void)
            //    ldnull
            // !elif (ReturnType is pointer)
            //    System.Reflection.Pointer.Box(ReturnType)
            // !else
            //    box ReturnType
            // ret
            //
            // !if (ReturnType is ByRef)
            //   ByRefNull:
            //   pop
            //   call InvokeUtils.get_NullByRefValueSentinel
            //   ret

            ILCodeLabel lStaticCall = emitter.NewCodeLabel();
            ILCodeLabel lProcessReturn = emitter.NewCodeLabel();
            thisCallSiteSetupStream.EmitLdArg(3); // targetIsThisCall
            thisCallSiteSetupStream.Emit(ILOpcode.brfalse, lStaticCall);
            staticCallSiteSetupStream.EmitLabel(lStaticCall);

            thisCallSiteSetupStream.EmitLdArg(0); // thisPtr

            ILToken tokDynamicInvokeParamHelperRef =
                emitter.NewToken(InvokeUtilsType.GetKnownMethod("DynamicInvokeParamHelperRef", null));
            ILToken tokDynamicInvokeParamHelperIn =
                emitter.NewToken(InvokeUtilsType.GetKnownMethod("DynamicInvokeParamHelperIn", null));

            TypeDesc[] targetMethodSignature = new TypeDesc[_targetSignature.Length];

            for (int paramIndex = 0; paramIndex < _targetSignature.Length; paramIndex++)
            {
                TypeDesc paramType = Context.GetSignatureVariable(paramIndex, true);
                DynamicInvokeMethodParameterKind paramKind = _targetSignature[paramIndex];

                for (int i = 0; i < _targetSignature.GetNumberOfParameterPointerIndirections(paramIndex); i++)
                    paramType = paramType.MakePointerType();

                ILToken tokParamType = emitter.NewToken(paramType);
                ILLocalVariable local = emitter.NewLocal(paramType.MakeByRefType());

                thisCallSiteSetupStream.EmitLdLoc(local);
                staticCallSiteSetupStream.EmitLdLoc(local);

                argSetupStream.EmitLdArg(2); // argSetupState
                argSetupStream.Emit(ILOpcode.ldtoken, tokParamType);

                if (paramKind == DynamicInvokeMethodParameterKind.Reference)
                {
                    argSetupStream.Emit(ILOpcode.call, tokDynamicInvokeParamHelperRef);

                    targetMethodSignature[paramIndex] = paramType.MakeByRefType();
                }
                else
                {
                    argSetupStream.Emit(ILOpcode.call, tokDynamicInvokeParamHelperIn);

                    thisCallSiteSetupStream.EmitLdInd(paramType);
                    staticCallSiteSetupStream.EmitLdInd(paramType);

                    targetMethodSignature[paramIndex] = paramType;
                }
                argSetupStream.EmitStLoc(local);
            }

            argSetupStream.EmitLdArg(2); // argSetupState
            argSetupStream.Emit(ILOpcode.call, emitter.NewToken(InvokeUtilsType.GetKnownMethod("DynamicInvokeArgSetupComplete", null)));

            thisCallSiteSetupStream.EmitLdArg(1); // methodToCall
            staticCallSiteSetupStream.EmitLdArg(1); // methodToCall

            DynamicInvokeMethodParameterKind returnKind = _targetSignature.ReturnType;
            TypeDesc returnType = returnKind != DynamicInvokeMethodParameterKind.None ?
                Context.GetSignatureVariable(_targetSignature.Length, true) :
                Context.GetWellKnownType(WellKnownType.Void);

            for (int i = 0; i < _targetSignature.GetNumerOfReturnTypePointerIndirections(); i++)
                returnType = returnType.MakePointerType();

            if (returnKind == DynamicInvokeMethodParameterKind.Reference)
                returnType = returnType.MakeByRefType();

            MethodSignature thisCallMethodSig = new MethodSignature(0, 0, returnType, targetMethodSignature);
            thisCallSiteSetupStream.Emit(ILOpcode.calli, emitter.NewToken(thisCallMethodSig));
            thisCallSiteSetupStream.Emit(ILOpcode.br, lProcessReturn);

            MethodSignature staticCallMethodSig = new MethodSignature(MethodSignatureFlags.Static, 0, returnType, targetMethodSignature);
            staticCallSiteSetupStream.Emit(ILOpcode.calli, emitter.NewToken(staticCallMethodSig));

            returnCodeStream.EmitLabel(lProcessReturn);

            ILCodeLabel lByRefReturnNull = null;

            if (returnKind == DynamicInvokeMethodParameterKind.None)
            {
                returnCodeStream.Emit(ILOpcode.ldnull);
            }
            else
            {
                TypeDesc returnTypeForBoxing = returnType;

                if (returnType.IsByRef)
                {
                    // If this is a byref return, we need to dereference first
                    returnTypeForBoxing = ((ByRefType)returnType).ParameterType;
                    lByRefReturnNull = emitter.NewCodeLabel();
                    returnCodeStream.Emit(ILOpcode.dup);
                    returnCodeStream.Emit(ILOpcode.brfalse, lByRefReturnNull);
                    returnCodeStream.EmitLdInd(returnTypeForBoxing);
                }

                if (returnTypeForBoxing.IsPointer)
                {
                    // Pointers box differently
                    returnCodeStream.Emit(ILOpcode.ldtoken, emitter.NewToken(returnTypeForBoxing));
                    MethodDesc getTypeFromHandleMethod =
                        Context.SystemModule.GetKnownType("System", "Type").GetKnownMethod("GetTypeFromHandle", null);
                    returnCodeStream.Emit(ILOpcode.call, emitter.NewToken(getTypeFromHandleMethod));

                    MethodDesc pointerBoxMethod =
                        Context.SystemModule.GetKnownType("System.Reflection", "Pointer").GetKnownMethod("Box", null);
                    returnCodeStream.Emit(ILOpcode.call, emitter.NewToken(pointerBoxMethod));
                }
                else
                {
                    ILToken tokReturnType = emitter.NewToken(returnTypeForBoxing);
                    returnCodeStream.Emit(ILOpcode.box, tokReturnType);
                }
            }

            returnCodeStream.Emit(ILOpcode.ret);

            if (lByRefReturnNull != null)
            {
                returnCodeStream.EmitLabel(lByRefReturnNull);
                returnCodeStream.Emit(ILOpcode.pop);
                returnCodeStream.Emit(ILOpcode.call, emitter.NewToken(InvokeUtilsType.GetKnownMethod("get_NullByRefValueSentinel", null)));
                returnCodeStream.Emit(ILOpcode.ret);
            }

            return emitter.Link(this);
        }

        private partial class DynamicInvokeThunkGenericParameter : GenericParameterDesc
        {
            private DynamicInvokeMethodThunk _owningMethod;

            public DynamicInvokeThunkGenericParameter(DynamicInvokeMethodThunk owningMethod, int index)
            {
                _owningMethod = owningMethod;
                Index = index;
            }

            public override TypeSystemContext Context
            {
                get
                {
                    return _owningMethod.Context;
                }
            }

            public override int Index
            {
                get;
            }

            public override GenericParameterKind Kind
            {
                get
                {
                    return GenericParameterKind.Method;
                }
            }
        }
    }

    internal enum DynamicInvokeMethodParameterKind
    {
        None,
        Value,
        Reference,
        Pointer,
    }

    /// <summary>
    /// Wraps a <see cref="MethodSignature"/> to reduce it's fidelity.
    /// </summary>
    public struct DynamicInvokeMethodSignature : IEquatable<DynamicInvokeMethodSignature>
    {
        private MethodSignature _signature;

        public TypeSystemContext Context => _signature.ReturnType.Context;

        public bool HasReturnValue
        {
            get
            {
                return !_signature.ReturnType.IsVoid;
            }
        }

        public int Length
        {
            get
            {
                return _signature.Length;
            }
        }

        internal DynamicInvokeMethodParameterKind this[int index]
        {
            get
            {
                TypeDesc type = _signature[index];

                if (type.IsByRef)
                    return DynamicInvokeMethodParameterKind.Reference;
                else if (type.IsPointer)
                    return DynamicInvokeMethodParameterKind.Pointer;
                else
                    return DynamicInvokeMethodParameterKind.Value;
            }
        }

        public static int GetNumberOfIndirections(TypeDesc type)
        {
            // Strip byrefness off. This is to support "ref void**"-style signatures.
            if (type.IsByRef)
                type = ((ByRefType)type).ParameterType;

            int result = 0;
            while (type.IsPointer)
            {
                result++;
                type = ((PointerType)type).ParameterType;
            }

            return result;
        }

        public int GetNumberOfParameterPointerIndirections(int paramIndex)
        {
            return GetNumberOfIndirections(_signature[paramIndex]);
        }

        public int GetNumerOfReturnTypePointerIndirections()
        {
            return GetNumberOfIndirections(_signature.ReturnType);
        }

        internal DynamicInvokeMethodParameterKind ReturnType
        {
            get
            {
                TypeDesc type = _signature.ReturnType;
                if (type.IsPointer)
                    return DynamicInvokeMethodParameterKind.Pointer;
                else if (type.IsVoid)
                    return DynamicInvokeMethodParameterKind.None;
                else if (type.IsByRef)
                    return DynamicInvokeMethodParameterKind.Reference;
                else
                    return DynamicInvokeMethodParameterKind.Value;
            }
        }

        public DynamicInvokeMethodSignature(MethodSignature concreteSignature)
        {
            Debug.Assert(DynamicInvokeMethodThunk.SupportsSignature(concreteSignature));
            _signature = concreteSignature;
        }

        public override bool Equals(object obj)
        {
            return obj is DynamicInvokeMethodSignature && Equals((DynamicInvokeMethodSignature)obj);
        }

        public override int GetHashCode()
        {
            int hashCode = (int)this.ReturnType * 0x5498341 + 0x832424;

            for (int i = 0; i < Length; i++)
            {
                int value = (int)this[i] * 0x5498341 + 0x832424;
                hashCode = hashCode * 31 + value;
            }

            return hashCode;
        }

        public bool Equals(DynamicInvokeMethodSignature other)
        {
            DynamicInvokeMethodParameterKind thisReturnKind = ReturnType;
            if (thisReturnKind != other.ReturnType)
                return false;

            if (GetNumerOfReturnTypePointerIndirections() != other.GetNumerOfReturnTypePointerIndirections())
                return false;
            
            if (Length != other.Length)
                return false;

            for (int i = 0; i < Length; i++)
            {
                DynamicInvokeMethodParameterKind thisParamKind = this[i];
                if (thisParamKind != other[i])
                    return false;

                if (GetNumberOfParameterPointerIndirections(i) != other.GetNumberOfParameterPointerIndirections(i))
                    return false;
            }

            return true;
        }
    }
}
