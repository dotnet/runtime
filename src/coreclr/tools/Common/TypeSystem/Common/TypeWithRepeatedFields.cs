// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Internal.TypeSystem
{
    /// <summary>
    /// This type represents a type that has one field in metadata,
    /// but has that field repeated at runtime to represent an array of elements inline.
    /// </summary>
    public sealed partial class TypeWithRepeatedFields : MetadataType
    {
        private int? _numFields;
        private FieldDesc[] _fields;

        public TypeWithRepeatedFields(MetadataType underlyingType)
        {
            MetadataType = underlyingType;
        }

        internal MetadataType MetadataType { get; }

        internal int NumFields
        {
            get
            {
                if (_numFields.HasValue)
                {
                    return _numFields.Value;
                }
                int size = MetadataType.InstanceFieldSize.AsInt;
                FieldDesc firstField = GetFirstInstanceField();

                _numFields = size / firstField.FieldType.GetElementSize().AsInt;
                return _numFields.Value;
            }
        }

        private FieldDesc GetFirstInstanceField()
        {
            FieldDesc firstField = null;
            foreach (var field in MetadataType.GetFields())
            {
                if (field.IsStatic)
                {
                    continue;
                }

                firstField = field;
                break;
            }

            Debug.Assert(firstField is not null);
            return firstField;
        }

        private FieldDesc[] ComputeFields()
        {
            var fields = new FieldDesc[NumFields];

            FieldDesc firstField = GetFirstInstanceField();

            for (int i = 0; i < NumFields; i++)
            {
                fields[i] = new ImpliedRepeatedFieldDesc(this, firstField, i);
            }

            return fields;
        }

        public override IEnumerable<FieldDesc> GetFields()
        {
            if (_fields is null)
            {
                Interlocked.CompareExchange(ref _fields, ComputeFields(), null);
            }
            return _fields;
        }

        public override ClassLayoutMetadata GetClassLayout() => MetadataType.GetClassLayout();
        public override bool HasCustomAttribute(string attributeNamespace, string attributeName) => MetadataType.HasCustomAttribute(attributeNamespace, attributeName);
        public override IEnumerable<MetadataType> GetNestedTypes() => (IEnumerable<MetadataType>)EmptyTypes;
        public override MetadataType GetNestedType(string name) => null;
        public override int GetInlineArrayLength() => MetadataType.GetInlineArrayLength();
        public override MethodImplRecord[] FindMethodsImplWithMatchingDeclName(string name) => MetadataType.FindMethodsImplWithMatchingDeclName(name);
        public override int GetHashCode() => MetadataType.GetHashCode();
        protected override MethodImplRecord[] ComputeVirtualMethodImplsForType() => Array.Empty<MethodImplRecord>();

        protected override TypeFlags ComputeTypeFlags(TypeFlags mask) => MetadataType.GetTypeFlags(mask);

        public override string Namespace => MetadataType.Namespace;

        public override string Name => MetadataType.Name;

        public override DefType[] ExplicitlyImplementedInterfaces => Array.Empty<DefType>();

        public override bool IsExplicitLayout => MetadataType.IsExplicitLayout;

        public override bool IsSequentialLayout => MetadataType.IsSequentialLayout;

        public override bool IsBeforeFieldInit => MetadataType.IsBeforeFieldInit;

        public override ModuleDesc Module => MetadataType.Module;

        public override MetadataType MetadataBaseType => MetadataType.MetadataBaseType;

        public override DefType BaseType => MetadataType.BaseType;

        public override bool IsSealed => true;

        public override bool IsAbstract => false;

        public override DefType ContainingType => MetadataType.ContainingType;

        public override PInvokeStringFormat PInvokeStringFormat => MetadataType.PInvokeStringFormat;

        public override TypeSystemContext Context => MetadataType.Context;

        public override IEnumerable<MethodDesc> GetMethods() => MethodDesc.EmptyMethods;
    }
}
