// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    public sealed partial class FieldForInstantiatedType : FieldDesc
    {
        private readonly FieldDesc _fieldDef;
        private readonly InstantiatedType _instantiatedType;

        internal FieldForInstantiatedType(FieldDesc fieldDef, InstantiatedType instantiatedType)
        {
            Debug.Assert(fieldDef.GetTypicalFieldDefinition() == fieldDef);
            _fieldDef = fieldDef;
            _instantiatedType = instantiatedType;
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _fieldDef.Context;
            }
        }

        public override DefType OwningType
        {
            get
            {
                return _instantiatedType;
            }
        }

        public override string Name
        {
            get
            {
                return _fieldDef.Name;
            }
        }

        public override TypeDesc FieldType
        {
            get
            {
                return _fieldDef.FieldType.InstantiateSignature(_instantiatedType.Instantiation, default(Instantiation));
            }
        }

        public override EmbeddedSignatureData[] GetEmbeddedSignatureData()
        {
            return _fieldDef.GetEmbeddedSignatureData();
        }

        public override bool HasEmbeddedSignatureData
        {
            get
            {
                return _fieldDef.HasEmbeddedSignatureData;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return _fieldDef.IsStatic;
            }
        }

        public override bool IsInitOnly
        {
            get
            {
                return _fieldDef.IsInitOnly;
            }
        }

        public override bool IsThreadStatic
        {
            get
            {
                return _fieldDef.IsThreadStatic;
            }
        }

        public override bool HasRva
        {
            get
            {
                return _fieldDef.HasRva;
            }
        }

        public override bool IsLiteral
        {
            get
            {
                return _fieldDef.IsLiteral;
            }
        }

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            return _fieldDef.HasCustomAttribute(attributeNamespace, attributeName);
        }

        public override FieldDesc GetTypicalFieldDefinition()
        {
            return _fieldDef;
        }
    }
}
