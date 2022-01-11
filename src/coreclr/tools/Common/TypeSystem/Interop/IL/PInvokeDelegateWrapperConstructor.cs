// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.IL;
using Internal.IL.Stubs;
using Debug = System.Diagnostics.Debug;
using System.Threading;

namespace Internal.TypeSystem.Interop
{
    /// <summary>
    /// Constructor for PInvokeDelegateWrapper which calls into the base class constructor
    /// </summary>
    public partial class PInvokeDelegateWrapperConstructor : ILStubMethod
    {
        public PInvokeDelegateWrapperConstructor(PInvokeDelegateWrapper owningType)
        {
            OwningType = owningType;
        }

        public override TypeDesc OwningType
        {
            get;
        }

        public override string Name
        {
            get
            {
                return ".ctor";
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return ".ctor";
            }
        }

        private MethodSignature _signature;
        public override MethodSignature Signature
        {
            get
            {
                if (_signature == null)
                {
                    _signature = new MethodSignature(MethodSignatureFlags.None, 
                        genericParameterCount: 0,
                        returnType: Context.GetWellKnownType(WellKnownType.Void),
                        parameters: new TypeDesc[] {
                        Context.GetWellKnownType(WellKnownType.IntPtr)
                        });
                }
                return _signature;
            }
        }

        public override TypeSystemContext Context
        {
            get
            {
                return OwningType.Context;
            }
        }

        public override MethodIL EmitIL()
        {
            var emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();
            codeStream.EmitLdArg(0);
            codeStream.EmitLdArg(1);
            codeStream.Emit(ILOpcode.call, emitter.NewToken(
                InteropTypes.GetNativeFunctionPointerWrapper(Context).GetMethod(".ctor", Signature)));
            codeStream.Emit(ILOpcode.ret);
            return emitter.Link(this);
        }
    }
}
