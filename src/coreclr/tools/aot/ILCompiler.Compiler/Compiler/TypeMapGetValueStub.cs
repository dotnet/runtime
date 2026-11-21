// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Stub with the signature <c>static TValue ()</c> that calls the static virtual getter
    /// ITypeMapValue&lt;TValue&gt;.Value on a type map value type. This is used when the compiler could not evaluate the getter
    /// at compile time, and always for reference type values. The type map stores the function pointer of the stub, so the mapped type itself doesn't need to be reflection visible.
    /// </summary>
    /// <remarks>
    /// Generated IL:
    ///     constrained. Target
    ///     call ITypeMapValue&lt;TValue&gt;.get_Value
    ///     ret
    /// </remarks>
    internal sealed class TypeMapGetValueStub : ILStubMethod, IPrefixMangledSignature
    {
        private readonly TypeDesc _owningType;
        private MethodSignature _signature;

        public TypeMapGetValueStub(TypeDesc owningType, TypeDesc target, TypeDesc valueType)
        {
            _owningType = owningType;
            Target = target;
            ValueType = valueType;
        }

        public TypeDesc Target { get; }
        public TypeDesc ValueType { get; }

        public override TypeSystemContext Context => _owningType.Context;

        public override TypeDesc OwningType => _owningType;

        public override MethodSignature Signature
            => _signature ??= new MethodSignature(MethodSignatureFlags.Static, 0, ValueType, []);

        public override Utf8Span Name => "TypeMapGetValueStub"u8;

        public override string DiagnosticName => "TypeMapGetValueStub__" + Target + "__" + ValueType;

        public override bool IsPInvoke => false;

        // A mapped type can have a stub for each value type it provides, so both are part of the symbol name.
        MethodSignature IPrefixMangledSignature.BaseSignature => new MethodSignature(MethodSignatureFlags.Static, 0, ValueType, [Target]);

        ReadOnlySpan<byte> IPrefixMangledSignature.Prefix => "TypeMapGetValueStub"u8;

        protected override int ClassCode => 1213990017;

        // All stubs share the owning type and the name.
        protected override int ComputeHashCode() => base.ComputeHashCode() ^ Target.GetHashCode() ^ (ValueType.GetHashCode() * 31);

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            TypeMapGetValueStub otherStub = (TypeMapGetValueStub)other;
            int result = comparer.Compare(Target, otherStub.Target);
            return result != 0 ? result : comparer.Compare(ValueType, otherStub.ValueType);
        }

        public override MethodIL EmitIL()
        {
            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            DefType valueInterface = ((MetadataType)Context.SystemModule.GetKnownType("System.Runtime.InteropServices"u8, "ITypeMapValue`1"u8))
                .MakeInstantiatedType(ValueType);
            MethodDesc getter = valueInterface.GetKnownMethod("get_Value"u8, null);

            codeStream.Emit(ILOpcode.constrained, emit.NewToken(Target));
            codeStream.Emit(ILOpcode.call, emit.NewToken(getter));
            codeStream.Emit(ILOpcode.ret);

            return emit.Link(this);
        }
    }
}
