// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;
using Internal.NativeFormat;

namespace Internal.Runtime.TypeLoader
{
    /// <summary>
    /// Represents a field defined in native layout data, but without metadata
    /// </summary>
    internal class NativeLayoutFieldDesc : FieldDesc
    {
        private DefType _owningType;
        private TypeDesc _fieldType;
        private FieldStorage _fieldStorage;

        public NativeLayoutFieldDesc(DefType owningType, TypeDesc fieldType, FieldStorage fieldStorage)
        {
            _owningType = owningType;
            _fieldType = fieldType;
            _fieldStorage = fieldStorage;
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _owningType.Context;
            }
        }

        public override TypeDesc FieldType
        {
            get
            {
                return _fieldType;
            }
        }

        public override EmbeddedSignatureData[] GetEmbeddedSignatureData() => null;

        public override bool HasRva
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        public override bool IsInitOnly
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        public override bool IsLiteral
        {
            get
            {
                return false;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return _fieldStorage != FieldStorage.Instance;
            }
        }

        public override bool IsThreadStatic
        {
            get
            {
                return _fieldStorage == FieldStorage.TLSStatic;
            }
        }

        internal FieldStorage FieldStorage
        {
            get
            {
                return _fieldStorage;
            }
        }

        public override DefType OwningType
        {
            get
            {
                return _owningType;
            }
        }

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            throw NotImplemented.ByDesign;
        }
    }
}
