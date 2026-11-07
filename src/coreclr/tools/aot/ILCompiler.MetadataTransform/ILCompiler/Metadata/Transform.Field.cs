// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;

using Debug = System.Diagnostics.Debug;
using FieldAttributes = System.Reflection.FieldAttributes;

namespace ILCompiler.Metadata
{
    internal partial class Transform<TPolicy>
    {
        internal EntityMap<Cts.FieldDesc, MetadataRecord> _fields =
            new EntityMap<Cts.FieldDesc, MetadataRecord>(EqualityComparer<Cts.FieldDesc>.Default);

        private Action<Cts.FieldDesc, Field> _initFieldDef;
        private Action<Cts.FieldDesc, MemberReference> _initFieldRef;

        public override MetadataRecord HandleQualifiedField(Cts.FieldDesc field)
        {
            if (_policy.GeneratesMetadata(field) && field.GetTypicalFieldDefinition() == field)
            {
                QualifiedField record = new QualifiedField();
                record.Field = HandleFieldDefinition(field);
                record.EnclosingType = (TypeDefinition)HandleType(field.OwningType);
                return record;
            }
            else
            {
                return HandleFieldReference(field);
            }
        }

        private Field HandleFieldDefinition(Cts.FieldDesc field)
        {
            Debug.Assert(field.GetTypicalFieldDefinition() == field);
            Debug.Assert(_policy.GeneratesMetadata(field));
            return (Field)_fields.GetOrCreate(field, _initFieldDef ??= InitializeFieldDefinition);
        }

        private void InitializeFieldDefinition(Cts.FieldDesc entity, Field record)
        {
            record.Name = HandleString(entity.GetName());
            record.Flags = GetFieldAttributes(entity);

            var ecmaField = entity as Cts.Ecma.EcmaField;
            if (ecmaField != null)
            {
                Ecma.MetadataReader reader = ecmaField.MetadataReader;
                Ecma.FieldDefinition fieldDef = reader.GetFieldDefinition(ecmaField.Handle);

                record.Signature = new FieldSignature();
                if (entity.HasEmbeddedSignatureData)
                    WriteFieldSignature(record.Signature.Blob, ecmaField.Module, fieldDef.Signature);
                else
                {
                    record.Signature.Blob.WriteByte((byte)Ecma.SignatureKind.Field);
                    WriteTypeSignature(record.Signature.Blob, entity.FieldType);
                }

                Ecma.ConstantHandle defaultValueHandle = fieldDef.GetDefaultValue();
                if (!defaultValueHandle.IsNil)
                {
                    record.DefaultValue = HandleConstant(ecmaField.Module, defaultValueHandle);
                }

                Ecma.CustomAttributeHandleCollection customAttributes = fieldDef.GetCustomAttributes();
                if (customAttributes.Count > 0)
                {
                    record.CustomAttributes = HandleCustomAttributes(ecmaField.Module, customAttributes);
                }

                int offset = fieldDef.GetOffset();
                if (offset >= 0)
                    record.Offset = (uint)offset;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void WriteFieldSignature(MetadataBlob blob, Cts.Ecma.EcmaModule module, Ecma.BlobHandle sigBlob)
        {
            Ecma.BlobReader reader = module.MetadataReader.GetBlobReader(sigBlob);
            Ecma.SignatureHeader header = reader.ReadSignatureHeader();
            Debug.Assert(header.Kind == Ecma.SignatureKind.Field);
            blob.WriteByte((byte)Ecma.SignatureKind.Field);
            WriteTypeSignature(blob, module, ref reader);
        }

        private MemberReference HandleFieldReference(Cts.FieldDesc field)
        {
            return (MemberReference)_fields.GetOrCreate(field, _initFieldRef ??= InitializeFieldReference);
        }

        private void InitializeFieldReference(Cts.FieldDesc entity, MemberReference record)
        {
            record.Name = HandleString(entity.GetName());
            record.Parent = HandleType(entity.OwningType);
            FieldSignature signature = new FieldSignature();
            signature.Blob.WriteByte((byte)Ecma.SignatureKind.Field);
            WriteTypeSignature(signature.Blob, entity.GetTypicalFieldDefinition().FieldType);
            record.Signature = signature;
        }

        private static FieldAttributes GetFieldAttributes(Cts.FieldDesc field)
        {
            FieldAttributes result;

            var ecmaField = field as Cts.Ecma.EcmaField;
            if (ecmaField != null)
            {
                var fieldDefinition = ecmaField.MetadataReader.GetFieldDefinition(ecmaField.Handle);
                result = fieldDefinition.Attributes;
            }
            else
            {
                result = 0;

                if (field.IsStatic)
                    result |= FieldAttributes.Static;
                if (field.IsInitOnly)
                    result |= FieldAttributes.InitOnly;
                if (field.IsLiteral)
                    result |= FieldAttributes.Literal;
                if (field.HasRva)
                    result |= FieldAttributes.HasFieldRVA;

                // Not set: Visibility, NotSerialized, SpecialName, RTSpecialName, HasFieldMarshal, HasDefault
            }

            return result;
        }
    }
}
