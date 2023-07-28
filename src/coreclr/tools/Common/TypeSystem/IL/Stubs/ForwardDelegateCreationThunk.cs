// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using Internal.TypeSystem.Interop;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Thunk to create Delegates from native function pointer
    /// </summary>
    public partial class ForwardDelegateCreationThunk : ILStubMethod
    {
        private readonly TypeDesc _owningType;
        private readonly MetadataType _delegateType;
        private readonly InteropStateManager _interopStateManager;
        private MethodSignature _signature;


        public ForwardDelegateCreationThunk(MetadataType delegateType, TypeDesc owningType, InteropStateManager interopStateManager)
        {
            _owningType = owningType;
            _delegateType = delegateType;
            _interopStateManager = interopStateManager;
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

        public MetadataType DelegateType
        {
            get
            {
                return _delegateType;
            }
        }

        public override MethodSignature Signature
        {
            get
            {
                _signature ??= new MethodSignature(MethodSignatureFlags.Static, 0,
                        DelegateType,
                        new TypeDesc[] {
                            Context.GetWellKnownType(WellKnownType.IntPtr)
                            });
                return _signature;
            }
        }

        public override string Name
        {
            get
            {
                return "ForwardDelegateCreationStub__" + DelegateType.Name;
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return "ForwardDelegateCreationStub__" + DelegateType.DiagnosticName;
            }
        }

        /// <summary>
        /// This thunk creates a delegate from a native function pointer
        /// by first creating a PInvokeDelegateWrapper from the function pointer
        /// and then creating the delegate from the Invoke method of the wrapper
        ///
        /// Generated IL:
        ///     ldarg   0
        ///     newobj PInvokeDelegateWrapper.ctor
        ///     dup
        ///     ldvirtftn PInvokeDelegateWrapper.Invoke
        ///     newobj DelegateType.ctor
        ///     ret
        ///
        /// Equivalent C#
        ///     return new DelegateType(new PInvokeDelegateWrapper(functionPointer).Invoke)
        /// </summary>
        public override MethodIL EmitIL()
        {
            var emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();
            codeStream.EmitLdArg(0);

            codeStream.Emit(ILOpcode.newobj, emitter.NewToken(
                _interopStateManager.GetPInvokeDelegateWrapper(DelegateType)
                .GetPInvokeDelegateWrapperMethod(PInvokeDelegateWrapperMethodKind.Constructor)));

            codeStream.Emit(ILOpcode.dup);

            codeStream.Emit(ILOpcode.ldvirtftn, emitter.NewToken(
                _interopStateManager.GetPInvokeDelegateWrapper(DelegateType)
                .GetPInvokeDelegateWrapperMethod(PInvokeDelegateWrapperMethodKind.Invoke)));

            codeStream.Emit(ILOpcode.newobj, emitter.NewToken(
                _delegateType.GetMethod(".ctor",
                new MethodSignature(MethodSignatureFlags.None,
                    genericParameterCount: 0,
                    returnType: Context.GetWellKnownType(WellKnownType.Void),
                    parameters: new TypeDesc[] { Context.GetWellKnownType(WellKnownType.Object),
                        Context.GetWellKnownType(WellKnownType.IntPtr)}
                ))));

            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(this);
        }
    }
}
