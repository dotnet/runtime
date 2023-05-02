// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Internal.TypeSystem;

namespace Microsoft.Diagnostics.Tools.Pgo.TypeRefTypeSystem
{
    class TypeRefTypeSystemField : FieldDesc
    {
        TypeRefTypeSystemType _type;
        string _name;
        TypeDesc _fieldType;
        EmbeddedSignatureData[] _embeddedSignatureData;

        public TypeRefTypeSystemField(TypeRefTypeSystemType type, string name, TypeDesc fieldType, EmbeddedSignatureData[] embeddedSigData)
        {
            _type = type;
            _name = name;
            _fieldType = fieldType;
            _embeddedSignatureData = embeddedSigData;
        }

        public override string Name => _name;
        public override DefType OwningType => _type;

        public override TypeDesc FieldType => _fieldType;

        public override EmbeddedSignatureData[] GetEmbeddedSignatureData() => _embeddedSignatureData;

        public override bool HasEmbeddedSignatureData => _embeddedSignatureData != null;

        public override bool IsStatic => throw new NotImplementedException();

        public override bool IsInitOnly => throw new NotImplementedException();

        public override bool IsThreadStatic => throw new NotImplementedException();

        public override bool HasRva => throw new NotImplementedException();

        public override bool IsLiteral => throw new NotImplementedException();

        public override TypeSystemContext Context => throw new NotImplementedException();

        protected override int ClassCode => throw new NotImplementedException();

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName) => throw new NotImplementedException();
        protected override int CompareToImpl(FieldDesc other, TypeSystemComparer comparer) => throw new NotImplementedException();
    }
}
