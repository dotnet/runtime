// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using Interlocked = System.Threading.Interlocked;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Thunk to dynamically invoke a method using reflection. The method accepts an parameters as byrefs
    /// lays them out on the stack, and calls the target method. This thunk has heavy dependencies
    /// on the general dynamic invocation infrastructure in System.InvokeUtils and gets called from there
    /// at runtime. See comments in System.InvokeUtils for a more thorough explanation.
    /// </summary>
    public sealed partial class DynamicInvokeMethodThunk : ILStubMethod
    {
        private TypeDesc _owningType;
        private MethodSignature _targetSignature;
        private MethodSignature _signature;

        public DynamicInvokeMethodThunk(TypeDesc owningType, MethodSignature targetSignature)
        {
            _owningType = owningType;
            _targetSignature = targetSignature;
        }

        // MethodSignature does not track information about the type of the this pointer. We will steal
        // one of the unused upper bits to track whether the this pointer is a byref or an object. This makes
        // the information passed around seamlesly, including name mangling.
        private static MethodSignatureFlags MethodSignatureFlags_ValueTypeInstanceMethod => (MethodSignatureFlags)0x8000;

        public static MethodSignature NormalizeSignature(MethodSignature sig, bool valueTypeInstanceMethod)
        {
            MethodSignatureFlags flags = 0;
            if (sig.IsStatic)
            {
                flags |= MethodSignatureFlags.Static;
            }
            // TODO: Reflection invoke assumes unboxing stubs. It means that the "this" pointer is always a regular boxed object reference and
            // that the dynamic invoke thunks cannot be used to invoke instance methods on unboxed value types currently. This will need
            // to be addressed for the eventual allocation-free reflection invoke.
            // else if (valueTypeInstanceMethod)
            // {
            //     flags |= MethodSignatureFlags_ValueTypeInstanceMethod;
            // }

            TypeDesc[] parameters = new TypeDesc[sig.Length];
            for (int i = 0; i < sig.Length; i++)
                parameters[i] = NormalizeType(sig[i]);
            return new MethodSignature(flags, 0, NormalizeType(sig.ReturnType), parameters);

            static TypeDesc NormalizeType(TypeDesc type)
            {
                Debug.Assert(!type.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true));

                if (type.IsByRef)
                    return type.Context.GetWellKnownType(WellKnownType.Byte).MakeByRefType();

                if (type.IsPointer || type.IsFunctionPointer)
                    return type.Context.GetWellKnownType(WellKnownType.Void).MakePointerType();

                if (type.IsEnum)
                    return type.UnderlyingType;

                if (type.IsValueType)
                    return type;

                return type.Context.GetWellKnownType(WellKnownType.Object);
            }
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

        public override MethodSignature Signature
        {
            get
            {
                if (_signature == null)
                {
                    var ptr = Context.GetWellKnownType(WellKnownType.Void).MakePointerType();
                    var byref = Context.GetWellKnownType(WellKnownType.Byte).MakeByRefType();

                    _signature = new MethodSignature(
                        MethodSignatureFlags.Static,
                        0,
                        byref,
                        new TypeDesc[]
                        {
                            ptr,    // fptr
                            byref,  // thisptr
                            byref,  // refbuf
                            ptr     // arguments
                        });
                }

                return _signature;
            }
        }

        public override string Name => "DynamicInvoke";

        public override string DiagnosticName => "DynamicInvoke";

        public MethodSignature TargetSignature => _targetSignature;

        public override MethodIL EmitIL()
        {
            ILEmitter emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();

            // Handle instance methods.
            if (!_targetSignature.IsStatic)
            {
                codeStream.EmitLdArg(1);
                if ((_targetSignature.Flags & MethodSignatureFlags_ValueTypeInstanceMethod) == 0)
                    codeStream.Emit(ILOpcode.ldind_ref);
            }

            // Push the arguments.
            if (_targetSignature.Length != 0)
            {
                var fieldByReferenceValueToken = emitter.NewToken(
                    Context.SystemModule.GetKnownType("System", "ByReference").GetKnownField("Value"));
                for (int i = 0; i < _targetSignature.Length; i++)
                {
                    codeStream.EmitLdArg(3);
                    if (i != 0)
                    {
                        codeStream.EmitLdc(i * Context.Target.PointerSize);
                        codeStream.Emit(ILOpcode.add);
                    }

                    codeStream.Emit(ILOpcode.ldfld, fieldByReferenceValueToken);

                    var parameterType = _targetSignature[i];
                    if (!parameterType.IsByRef)
                    {
                        codeStream.EmitLdInd(parameterType);
                    }
                }
            }

            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.calli, emitter.NewToken(_targetSignature));

            // Store the return value unless it is a byref
            var returnType = _targetSignature.ReturnType;
            if (!returnType.IsByRef)
            {
                if (!returnType.IsVoid)
                {
                    var retVal = emitter.NewLocal(returnType);
                    codeStream.EmitStLoc(retVal);

                    codeStream.EmitLdArg(2);
                    codeStream.EmitLdLoc(retVal);
                    codeStream.EmitStInd(returnType);
                }
                codeStream.EmitLdArg(2);
            }

            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(this);
        }
    }
}
