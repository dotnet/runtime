// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.IL;
using Internal.IL.Stubs;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler;

public partial class CompilerTypeSystemContext
{
    private readonly InlineArrayElementTypeHashtable _inlineArrayElementTypeMethods = new InlineArrayElementTypeHashtable();

    internal MethodIL GetInlineArrayLengthIntrinsicIL(MethodDesc method)
    {
        if (method.IsTypicalMethodDefinition || method.Instantiation.Length != 2)
            return null;

        // The buffer definition must be known to validate its layout.
        if (method.Instantiation[0] is not MetadataType inlineArrayType)
            return null;

        ILEmitter emitter = new ILEmitter();
        ILCodeStream code = emitter.NewCodeStream();
        MethodDesc definition = method.GetTypicalMethodDefinition();

        if (!inlineArrayType.IsInlineArray)
        {
            code.EmitLdc(0);
        }
        else
        {
            _ = inlineArrayType.InstanceFieldSize;
            int length = inlineArrayType.GetClassLayout().InlineArrayLength;
            FieldDesc elementField = GetInlineArrayElementField(inlineArrayType);
            TypeDesc elementType = method.Instantiation[1];

            if (!elementField.FieldType.IsCanonicalSubtype(CanonicalFormKind.Any)
                && !elementType.IsCanonicalSubtype(CanonicalFormKind.Any))
            {
                code.EmitLdc(elementField.FieldType == elementType ? length : 0);
            }
            else
            {
                MetadataType typeType = SystemModule.GetKnownType("System"u8, "Type"u8);
                MethodDesc getTypeFromHandle = typeType.GetKnownMethod("GetTypeFromHandle"u8, null);
                MethodDesc typeEquals = typeType.GetKnownMethod("op_Equality"u8, null);

                if (elementField.FieldType.IsCanonicalSubtype(CanonicalFormKind.Any))
                {
                    // Resolve the field type in the buffer's context, not the conversion's.
                    MethodDesc getter = _inlineArrayElementTypeMethods.GetOrCreateValue(
                        (MetadataType)inlineArrayType.GetTypeDefinition());
                    code.Emit(ILOpcode.call, emitter.NewToken(new InlineArrayElementTypeMethodReference(getter)));
                }
                else
                {
                    code.Emit(ILOpcode.ldtoken, emitter.NewToken(elementField.FieldType));
                    code.Emit(ILOpcode.call, emitter.NewToken(getTypeFromHandle));
                }

                code.Emit(ILOpcode.ldtoken, emitter.NewToken(GetSignatureVariable(1, true)));
                code.Emit(ILOpcode.call, emitter.NewToken(getTypeFromHandle));
                code.Emit(ILOpcode.call, emitter.NewToken(typeEquals));
                ILCodeLabel mismatch = emitter.NewCodeLabel();
                code.Emit(ILOpcode.brfalse, mismatch);
                code.EmitLdc(length);
                code.Emit(ILOpcode.ret);
                code.EmitLabel(mismatch);
                code.EmitLdc(0);
            }
        }

        code.Emit(ILOpcode.ret);
        return new InstantiatedMethodIL(method, emitter.Link(definition));
    }

    private static FieldDesc GetInlineArrayElementField(MetadataType type)
    {
        foreach (FieldDesc field in type.GetFields())
        {
            if (!field.IsStatic)
                return field;
        }

        throw new UnreachableException();
    }

    private sealed class InlineArrayElementTypeHashtable : LockFreeReaderHashtable<MetadataType, MethodDesc>
    {
        protected override int GetKeyHashCode(MetadataType key) => key.GetHashCode();
        protected override int GetValueHashCode(MethodDesc value) => value.OwningType.GetHashCode();
        protected override bool CompareKeyToValue(MetadataType key, MethodDesc value) => key == value.OwningType;
        protected override bool CompareValueToValue(MethodDesc left, MethodDesc right) => left.OwningType == right.OwningType;
        protected override MethodDesc CreateValueFromKey(MetadataType key) => new InlineArrayElementTypeMethod(key);
    }

    private sealed class InlineArrayElementTypeMethodReference : MethodDesc
    {
        private readonly MethodDesc _getter;

        public InlineArrayElementTypeMethodReference(MethodDesc getter) => _getter = getter;

        public override TypeSystemContext Context => _getter.Context;
        public override TypeDesc OwningType => _getter.OwningType;
        public override MethodSignature Signature => _getter.Signature;
        public override Utf8Span Name => _getter.Name;

        public override MethodDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            // The call's owner is !!0, in both canonical and runtime-determined IL.
            TypeDesc owner = methodInstantiation[0];
            if (owner is RuntimeDeterminedType runtimeDeterminedOwner)
                return Context.GetMethodForRuntimeDeterminedType(_getter, runtimeDeterminedOwner);

            return Context.GetMethodForInstantiatedType(_getter, (InstantiatedType)owner);
        }

        protected override int ClassCode => 1258049372;

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer) =>
            comparer.Compare(OwningType, other.OwningType);
    }

    private sealed class InlineArrayElementTypeMethod : SpecializableILStubMethod
    {
        public InlineArrayElementTypeMethod(MetadataType type)
        {
            OwningType = type;
            Signature = new MethodSignature(MethodSignatureFlags.Static, 0,
                Context.SystemModule.GetKnownType("System"u8, "Type"u8), TypeDesc.EmptyTypes);
        }

        public override TypeSystemContext Context => OwningType.Context;
        public override TypeDesc OwningType { get; }
        public override MethodSignature Signature { get; }
        public override Utf8Span Name => "<InlineArrayElementType>"u8;
        public override string DiagnosticName => "InlineArrayElementType";

        public override MethodIL EmitIL()
        {
            ILEmitter emitter = new ILEmitter();
            ILCodeStream code = emitter.NewCodeStream();
            FieldDesc field = GetInlineArrayElementField((MetadataType)((MetadataType)OwningType).InstantiateAsOpen());
            MethodDesc getTypeFromHandle = Context.SystemModule.GetKnownType("System"u8, "Type"u8)
                .GetKnownMethod("GetTypeFromHandle"u8, null);
            code.Emit(ILOpcode.ldtoken, emitter.NewToken(field.FieldType));
            code.Emit(ILOpcode.call, emitter.NewToken(getTypeFromHandle));
            code.Emit(ILOpcode.ret);
            return emitter.Link(this);
        }

        public override MethodIL EmitIL(MethodDesc method) => new InstantiatedMethodIL(method, EmitIL());

        protected override int ClassCode => 1258049371;

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer) =>
            comparer.Compare(OwningType, other.OwningType);
    }
}
