// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public sealed partial class ImpliedRepeatedFieldDesc : FieldDesc
    {
        private readonly FieldDesc _underlyingFieldDesc;

        public ImpliedRepeatedFieldDesc(DefType owningType, FieldDesc underlyingFieldDesc, int fieldIndex)
        {
            OwningType = owningType;
            _underlyingFieldDesc = underlyingFieldDesc;
            FieldIndex = fieldIndex;
        }

        public override DefType OwningType { get; }

        public override TypeDesc FieldType => _underlyingFieldDesc.FieldType;

        public override bool HasEmbeddedSignatureData => _underlyingFieldDesc.HasEmbeddedSignatureData;

        public override bool IsStatic => _underlyingFieldDesc.IsStatic;

        public override bool IsInitOnly => _underlyingFieldDesc.IsInitOnly;

        public override bool IsThreadStatic => _underlyingFieldDesc.IsThreadStatic;

        public override bool HasRva => _underlyingFieldDesc.HasRva;

        public override bool IsLiteral => _underlyingFieldDesc.IsLiteral;

        public override TypeSystemContext Context => _underlyingFieldDesc.Context;

        public int FieldIndex { get; }

        public override EmbeddedSignatureData[] GetEmbeddedSignatureData() => _underlyingFieldDesc.GetEmbeddedSignatureData();

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName) => _underlyingFieldDesc.HasCustomAttribute(attributeNamespace, attributeName);

        public override MarshalAsDescriptor GetMarshalAsDescriptor() => _underlyingFieldDesc.GetMarshalAsDescriptor();

        public override string Name => $"{_underlyingFieldDesc.Name}[{FieldIndex}]";
    }
}
