// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;

namespace ILCompiler.Metadata
{
    internal partial class Transform<TPolicy>
    {
        private List<CustomAttribute> HandleCustomAttributes(Cts.Ecma.EcmaModule module, Ecma.CustomAttributeHandleCollection attributes)
        {
            List<CustomAttribute> customAttributes = new List<CustomAttribute>(attributes.Count);
            foreach (Ecma.CustomAttributeHandle attributeHandle in attributes)
            {
                if (_policy.GeneratesMetadata(module, attributeHandle))
                    customAttributes.Add(HandleCustomAttribute(module, attributeHandle));
            }

            return customAttributes;
        }

        private CustomAttribute HandleCustomAttribute(Cts.Ecma.EcmaModule module, Ecma.CustomAttributeHandle attributeHandle)
        {
            Ecma.MetadataReader reader = module.MetadataReader;
            Ecma.CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
            Cts.MethodDesc constructor = module.GetMethod(attribute.Constructor);
            CustomAttribute result = new CustomAttribute { Constructor = HandleQualifiedMethod(constructor) };
            Ecma.BlobReader valueReader = reader.GetBlobReader(attribute.Value);

            // ECMA-335 II.23.3, with the prologue omitted and type names replaced by handles.
            ushort prolog = valueReader.ReadUInt16();
            Debug.Assert(prolog == 1);

            foreach (Cts.TypeDesc parameterType in constructor.Signature)
                TranscodeCustomAttributeValue(module, parameterType, ref valueReader, result.Blob);

            ushort namedCount = valueReader.ReadUInt16();
            result.Blob.WriteByte((byte)namedCount);
            result.Blob.WriteByte((byte)(namedCount >> 8));
            for (int i = 0; i < namedCount; i++)
            {
                result.Blob.WriteByte(valueReader.ReadByte());
                Cts.TypeDesc type = TranscodeFieldOrPropType(module, ref valueReader, result.Blob);
                result.Blob.WriteString(valueReader.ReadSerializedString());
                TranscodeCustomAttributeValue(module, type, ref valueReader, result.Blob);
            }

            Debug.Assert(valueReader.RemainingBytes == 0);
            return result;
        }

        private Cts.TypeDesc TranscodeFieldOrPropType(
            Cts.Ecma.EcmaModule module, ref Ecma.BlobReader reader, MetadataBlob blob)
        {
            Ecma.SerializationTypeCode code = reader.ReadSerializationTypeCode();
            blob.WriteByte((byte)code);
            switch (code)
            {
                case Ecma.SerializationTypeCode.SZArray:
                    return module.Context.GetArrayType(TranscodeFieldOrPropType(module, ref reader, blob));
                case Ecma.SerializationTypeCode.Enum:
                    Cts.TypeDesc enumType = Cts.CustomAttributeTypeNameParser.GetTypeByCustomAttributeTypeName(
                        module, reader.ReadSerializedString());
                    blob.WriteType(HandleType(enumType));
                    return enumType;
                case Ecma.SerializationTypeCode.Type:
                    return module.Context.SystemModule.GetType("System"u8, "Type"u8);
                case Ecma.SerializationTypeCode.String:
                    return module.Context.GetWellKnownType(Cts.WellKnownType.String);
                case Ecma.SerializationTypeCode.TaggedObject:
                    return module.Context.GetWellKnownType(Cts.WellKnownType.Object);
                case Ecma.SerializationTypeCode.Single:
                    return module.Context.GetWellKnownType(Cts.WellKnownType.Single);
                case Ecma.SerializationTypeCode.Double:
                    return module.Context.GetWellKnownType(Cts.WellKnownType.Double);
                case <= Ecma.SerializationTypeCode.UInt64:
                    return module.Context.GetWellKnownType((Cts.WellKnownType)code);
                default:
                    throw new UnreachableException();
            }
        }

        private void TranscodeCustomAttributeValue(
            Cts.Ecma.EcmaModule module, Cts.TypeDesc type, ref Ecma.BlobReader reader, MetadataBlob blob)
        {
            if (type.IsArray)
            {
                Ecma.BlobReader countReader = reader;
                int count = reader.ReadInt32();
                blob.WriteBytes(countReader.ReadBytes(sizeof(int)));
                if (count == -1)
                    return;

                Cts.TypeDesc elementType = ((Cts.ArrayType)type).ElementType;
                if (elementType.UnderlyingType.IsPrimitive)
                {
                    int size = GetCustomAttributePrimitiveSize(elementType.UnderlyingType.Category);
                    blob.WriteBytes(reader.ReadBytes(checked(count * size)));
                }
                else
                {
                    for (int i = 0; i < count; i++)
                        TranscodeCustomAttributeValue(module, elementType, ref reader, blob);
                }

                return;
            }

            if (type.IsObject)
            {
                Cts.TypeDesc actualType = TranscodeFieldOrPropType(module, ref reader, blob);
                TranscodeCustomAttributeValue(module, actualType, ref reader, blob);
            }
            else if (type.IsString)
            {
                blob.WriteString(reader.ReadSerializedString());
            }
            else if (type.UnderlyingType.IsPrimitive)
            {
                blob.WriteBytes(reader.ReadBytes(GetCustomAttributePrimitiveSize(type.UnderlyingType.Category)));
            }
            else
            {
                string typeName = reader.ReadSerializedString();
                blob.WriteType(typeName is null ? null :
                    HandleType(Cts.CustomAttributeTypeNameParser.GetTypeByCustomAttributeTypeName(module, typeName)));
            }
        }

        private static int GetCustomAttributePrimitiveSize(Cts.TypeFlags type) => type switch
        {
            Cts.TypeFlags.Boolean or Cts.TypeFlags.SByte or Cts.TypeFlags.Byte => 1,
            Cts.TypeFlags.Char or Cts.TypeFlags.Int16 or Cts.TypeFlags.UInt16 => 2,
            Cts.TypeFlags.Int32 or Cts.TypeFlags.UInt32 or Cts.TypeFlags.Single => 4,
            Cts.TypeFlags.Int64 or Cts.TypeFlags.UInt64 or Cts.TypeFlags.Double => 8,
            _ => throw new UnreachableException(),
        };
    }
}
