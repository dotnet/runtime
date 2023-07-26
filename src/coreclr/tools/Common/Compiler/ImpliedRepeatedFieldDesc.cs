// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    public sealed class ImpliedRepeatedFieldDesc : FieldDesc
    {
        private readonly FieldDesc _underlyingFieldDesc;

        public ImpliedRepeatedFieldDesc(FieldDesc underlyingFieldDesc, int fieldIndex)
        {
            _underlyingFieldDesc = underlyingFieldDesc;
            FieldIndex = fieldIndex;
        }

        public override DefType OwningType => _underlyingFieldDesc.OwningType;

        public override TypeDesc FieldType => _underlyingFieldDesc.FieldType;

        public override bool HasEmbeddedSignatureData => _underlyingFieldDesc.HasEmbeddedSignatureData;

        public override bool IsStatic => _underlyingFieldDesc.IsStatic;

        public override bool IsInitOnly => _underlyingFieldDesc.IsInitOnly;

        public override bool IsThreadStatic => _underlyingFieldDesc.IsThreadStatic;

        public override bool HasRva => _underlyingFieldDesc.HasRva;

        public override bool IsLiteral => _underlyingFieldDesc.IsLiteral;

        public override TypeSystemContext Context => _underlyingFieldDesc.Context;

        public int FieldIndex { get; }

        protected override int ClassCode => 31666958;

        public override EmbeddedSignatureData[] GetEmbeddedSignatureData() => _underlyingFieldDesc.GetEmbeddedSignatureData();
        public override bool HasCustomAttribute(string attributeNamespace, string attributeName) => _underlyingFieldDesc.HasCustomAttribute(attributeNamespace, attributeName);
        protected override int CompareToImpl(FieldDesc other, TypeSystemComparer comparer)
        {
            if (other is not ImpliedRepeatedFieldDesc impliedRepeatedFieldDesc)
            {
                return -1;
            }

            int result = comparer.Compare(_underlyingFieldDesc, impliedRepeatedFieldDesc._underlyingFieldDesc);

            if (result != 0)
            {
                return result;
            }

            return FieldIndex.CompareTo(impliedRepeatedFieldDesc.FieldIndex);
        }

        public override LayoutInt Offset
        {
            get
            {
                LayoutInt elementSize = FieldType.GetElementSize();
                return elementSize.IsIndeterminate ? LayoutInt.Indeterminate : new LayoutInt(elementSize.AsInt * FieldIndex);
            }
        }

        public override MarshalAsDescriptor GetMarshalAsDescriptor() => _underlyingFieldDesc.GetMarshalAsDescriptor();
    }
}
