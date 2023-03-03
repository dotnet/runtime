// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Thunk to call the underlying type's GetHashCode method on enum types.
    /// This method prevents boxing of 'this' that would be required before a call to
    /// the System.Enum's default implementation.
    /// </summary>
    internal sealed partial class EnumGetHashCodeThunk : ILStubMethod
    {
        private TypeDesc _owningType;
        private MethodSignature _signature;

        public EnumGetHashCodeThunk(TypeDesc owningType)
        {
            Debug.Assert(owningType.IsEnum);
            _owningType = owningType;
            _signature = ObjectGetHashCodeMethod.Signature;
        }

        private MethodDesc ObjectGetHashCodeMethod
        {
            get
            {
                return Context.GetWellKnownType(WellKnownType.Object).GetKnownMethod("GetHashCode", null);
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
                return _signature;
            }
        }

        public override string Name
        {
            get
            {
                return "GetHashCode";
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return "GetHashCode";
            }
        }

        public override bool IsVirtual
        {
            get
            {
                // This would be implicit (false is the default), but let's be very explicit.
                // Making this an actual override would cause size bloat with very little benefit.
                // The usefulness of this method lies in it's ability to prevent boxing of 'this'.
                // The base implementation on System.Enum is adequate for everything else.
                return false;
            }
        }

        public override MethodIL EmitIL()
        {
            ILEmitter emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();

            codeStream.EmitLdArg(0);
            codeStream.Emit(ILOpcode.constrained, emitter.NewToken(_owningType.UnderlyingType));
            codeStream.Emit(ILOpcode.callvirt, emitter.NewToken(ObjectGetHashCodeMethod));
            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(this);
        }
    }

    /// <summary>
    /// Thunk to compare underlying values of enums in the Equals method.
    /// This method prevents boxing of 'this' that would be required before a call to
    /// the System.Enum's default implementation.
    /// </summary>
    internal sealed partial class EnumEqualsThunk : ILStubMethod
    {
        private TypeDesc _owningType;
        private MethodSignature _signature;

        public EnumEqualsThunk(TypeDesc owningType)
        {
            Debug.Assert(owningType.IsEnum);
            _owningType = owningType;
            _signature = ObjectEqualsMethod.Signature;
        }

        private MethodDesc ObjectEqualsMethod
        {
            get
            {
                return Context.GetWellKnownType(WellKnownType.Object).GetKnownMethod("Equals", null);
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
                return _signature;
            }
        }

        public override string Name
        {
            get
            {
                return "Equals";
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return "Equals";
            }
        }

        public override bool IsVirtual
        {
            get
            {
                // This would be implicit (false is the default), but let's be very explicit.
                // Making this an actual override would cause size bloat with very little benefit.
                // The usefulness of this method lies in it's ability to prevent boxing of 'this'.
                // The base implementation on System.Enum is adequate for everything else.
                return false;
            }
        }

        public override MethodIL EmitIL()
        {
            ILEmitter emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();

            // InstantiateAsOpen covers the weird case of generic enums
            TypeDesc owningTypeAsOpen = _owningType.InstantiateAsOpen();

            ILCodeLabel lNotEqual = emitter.NewCodeLabel();

            // if (!(obj is {enumtype}))
            //     return false;

            codeStream.EmitLdArg(1);
            codeStream.Emit(ILOpcode.isinst, emitter.NewToken(owningTypeAsOpen));
            codeStream.Emit(ILOpcode.dup);
            codeStream.Emit(ILOpcode.brfalse, lNotEqual);

            // return ({underlyingtype})this == ({underlyingtype})obj;

            // PREFER: ILOpcode.unbox, but the codegen for that is pretty bad
            codeStream.Emit(ILOpcode.ldflda, emitter.NewToken(Context.GetWellKnownType(WellKnownType.Object).GetKnownField("m_pEEType")));
            codeStream.EmitLdc(Context.Target.PointerSize);
            codeStream.Emit(ILOpcode.add);
            codeStream.EmitLdInd(owningTypeAsOpen);

            codeStream.EmitLdArg(0);
            codeStream.EmitLdInd(owningTypeAsOpen);

            codeStream.Emit(ILOpcode.ceq);

            codeStream.Emit(ILOpcode.ret);

            codeStream.EmitLabel(lNotEqual);
            codeStream.Emit(ILOpcode.pop);
            codeStream.EmitLdc(0);
            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(this);
        }
    }
}
