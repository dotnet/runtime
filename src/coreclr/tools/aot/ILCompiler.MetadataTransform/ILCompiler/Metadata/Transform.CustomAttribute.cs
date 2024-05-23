// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;

using Debug = System.Diagnostics.Debug;
using NamedArgumentMemberKind = Internal.Metadata.NativeFormat.NamedArgumentMemberKind;
using UnreachableException = System.Diagnostics.UnreachableException;

namespace ILCompiler.Metadata
{
    internal partial class Transform<TPolicy>
    {
        private List<CustomAttribute> HandleCustomAttributes(Cts.Ecma.EcmaModule module, Ecma.CustomAttributeHandleCollection attributes)
        {
            List<CustomAttribute> customAttributes = new List<CustomAttribute>(attributes.Count);

            foreach (var attributeHandle in attributes)
            {
                if (!_policy.GeneratesMetadata(module, attributeHandle))
                    continue;

                // TODO-NICE: We can intern the attributes based on the CA constructor and blob bytes
                customAttributes.Add(HandleCustomAttribute(module, attributeHandle));
            }

            return customAttributes;
        }

        private CustomAttribute HandleCustomAttribute(Cts.Ecma.EcmaModule module, Ecma.CustomAttributeHandle attributeHandle)
        {
            Ecma.MetadataReader reader = module.MetadataReader;
            Ecma.CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);

            Cts.MethodDesc constructor = module.GetMethod(attribute.Constructor);

            CustomAttribute result = new CustomAttribute
            {
                Constructor = HandleQualifiedMethod(constructor),
            };

            Ecma.BlobReader valueReader = reader.GetBlobReader(attribute.Value);

            ushort prolog = valueReader.ReadUInt16(); // Version
            Debug.Assert(prolog == 1);

            Cts.MethodSignature sig = constructor.Signature;
            result.FixedArguments.Capacity = sig.Length;
            foreach (Cts.TypeDesc paramType in sig)
            {
                var fixedArgument = paramType.IsArray ?
                    HandleCustomAttributeConstantArray(module, TypeDescToSerializationTypeCode(((Cts.ArrayType)paramType).ElementType), ref valueReader) :
                    HandleCustomAttributeConstantValue(module, TypeDescToSerializationTypeCode(paramType), ref valueReader);
                result.FixedArguments.Add(fixedArgument);
            }

            ushort numNamed = valueReader.ReadUInt16();
            result.NamedArguments.Capacity = numNamed;
            for (int i = 0; i < numNamed; i++)
            {
                byte flag = valueReader.ReadByte();
                Cts.TypeDesc type = SerializationTypeToType(module, ref valueReader);
                var namedArgument = new NamedArgument
                {
                    Flags = flag == (byte)Ecma.CustomAttributeNamedArgumentKind.Field ?
                        NamedArgumentMemberKind.Field : NamedArgumentMemberKind.Property,
                    Type = HandleType(type),
                    Name = HandleString(valueReader.ReadSerializedString()),
                    Value = type.IsArray ?
                        HandleCustomAttributeConstantArray(module, TypeDescToSerializationTypeCode(((Cts.ArrayType)type).ElementType), ref valueReader) :
                        HandleCustomAttributeConstantValue(module, TypeDescToSerializationTypeCode(type), ref valueReader)
                };
                result.NamedArguments.Add(namedArgument);
            }

            return result;
        }

        private static Ecma.SerializationTypeCode TypeDescToSerializationTypeCode(Cts.TypeDesc type)
        {
            Debug.Assert((int)Cts.TypeFlags.Boolean == (int)Ecma.SerializationTypeCode.Boolean);

            switch (type.UnderlyingType.Category)
            {
                case Cts.TypeFlags.Single: return Ecma.SerializationTypeCode.Single;
                case Cts.TypeFlags.Double: return Ecma.SerializationTypeCode.Double;
                case <= Cts.TypeFlags.UInt64: return (Ecma.SerializationTypeCode)type.UnderlyingType.Category;
                default:
                    if (type.IsObject)
                        return Ecma.SerializationTypeCode.TaggedObject;

                    if (type.IsString)
                        return Ecma.SerializationTypeCode.String;

                    if (type is not Cts.MetadataType { Name: "Type", Namespace: "System" })
                        throw new UnreachableException();

                    return Ecma.SerializationTypeCode.Type;
            }
        }

        private static Cts.TypeDesc SerializationTypeToType(Cts.Ecma.EcmaModule module, ref Ecma.BlobReader valueReader)
        {
            Ecma.SerializationTypeCode typeCode = valueReader.ReadSerializationTypeCode();

            switch (typeCode)
            {
                case Ecma.SerializationTypeCode.Type: return module.Context.SystemModule.GetType("System", "Type");
                case Ecma.SerializationTypeCode.SZArray: return module.Context.GetArrayType(SerializationTypeToType(module, ref valueReader));
                case Ecma.SerializationTypeCode.Enum: return Cts.CustomAttributeTypeNameParser.GetTypeByCustomAttributeTypeName(module, valueReader.ReadSerializedString());
                case Ecma.SerializationTypeCode.String: return module.Context.GetWellKnownType(Cts.WellKnownType.String);
                case Ecma.SerializationTypeCode.TaggedObject: return module.Context.GetWellKnownType(Cts.WellKnownType.Object);
                case Ecma.SerializationTypeCode.Single: return module.Context.GetWellKnownType(Cts.WellKnownType.Single);
                case Ecma.SerializationTypeCode.Double: return module.Context.GetWellKnownType(Cts.WellKnownType.Double);
                case <= Ecma.SerializationTypeCode.UInt64: return module.Context.GetWellKnownType((Cts.WellKnownType)typeCode);
            }

            Cts.ThrowHelper.ThrowBadImageFormatException();
            return null; // unreached
        }

        private MetadataRecord HandleCustomAttributeConstantValue(Cts.Ecma.EcmaModule module, Ecma.SerializationTypeCode typeCode, ref Ecma.BlobReader valueReader)
        {
            if (typeCode == Ecma.SerializationTypeCode.TaggedObject)
            {
                typeCode = valueReader.ReadSerializationTypeCode();
            }

            if (typeCode == Ecma.SerializationTypeCode.Enum)
            {
                Cts.TypeDesc enumType = Cts.CustomAttributeTypeNameParser.GetTypeByCustomAttributeTypeName(module, valueReader.ReadSerializedString());
                return new ConstantEnumValue
                {
                    Value = HandleCustomAttributeConstantValue(module, TypeDescToSerializationTypeCode(enumType), ref valueReader),
                    Type = HandleType(enumType)
                };
            }

            if (typeCode == Ecma.SerializationTypeCode.String)
            {
                string s = valueReader.ReadSerializedString();
                return s == null ? new ConstantReferenceValue() : HandleString(s);
            }

            if (typeCode == Ecma.SerializationTypeCode.Type)
            {
                string s = valueReader.ReadSerializedString();
                return s == null ? new ConstantReferenceValue() : HandleType(Cts.CustomAttributeTypeNameParser.GetTypeByCustomAttributeTypeName(module, s));
            }

            return typeCode switch
            {
                Ecma.SerializationTypeCode.Boolean => new ConstantBooleanValue { Value = valueReader.ReadBoolean() },
                Ecma.SerializationTypeCode.Char => new ConstantCharValue { Value = valueReader.ReadChar() },
                Ecma.SerializationTypeCode.Byte => new ConstantByteValue { Value = valueReader.ReadByte() },
                Ecma.SerializationTypeCode.SByte => new ConstantSByteValue { Value = valueReader.ReadSByte() },
                Ecma.SerializationTypeCode.Int16 => new ConstantInt16Value { Value = valueReader.ReadInt16() },
                Ecma.SerializationTypeCode.UInt16 => new ConstantUInt16Value { Value = valueReader.ReadUInt16() },
                Ecma.SerializationTypeCode.Int32 => new ConstantInt32Value { Value = valueReader.ReadInt32() },
                Ecma.SerializationTypeCode.UInt32 => new ConstantUInt32Value { Value = valueReader.ReadUInt32() },
                Ecma.SerializationTypeCode.Int64 => new ConstantInt64Value { Value = valueReader.ReadInt64() },
                Ecma.SerializationTypeCode.UInt64 => new ConstantUInt64Value { Value = valueReader.ReadUInt64() },
                Ecma.SerializationTypeCode.Single => new ConstantSingleValue { Value = valueReader.ReadSingle() },
                Ecma.SerializationTypeCode.Double => new ConstantDoubleValue { Value = valueReader.ReadDouble() },
                Ecma.SerializationTypeCode.SZArray => HandleCustomAttributeConstantArray(module, valueReader.ReadSerializationTypeCode(), ref valueReader),
                _ => throw new UnreachableException()
            };
        }

        private MetadataRecord HandleCustomAttributeConstantArray(Cts.Ecma.EcmaModule module, Ecma.SerializationTypeCode elementTypeCode, ref Ecma.BlobReader valueReader)
        {
            if (elementTypeCode == Ecma.SerializationTypeCode.Enum)
            {
                Cts.TypeDesc enumType = Cts.CustomAttributeTypeNameParser.GetTypeByCustomAttributeTypeName(module, valueReader.ReadSerializedString());
                return new ConstantEnumArray
                {
                    ElementType = HandleType(enumType),
                    Value = HandleCustomAttributeConstantArray(module, TypeDescToSerializationTypeCode(enumType), ref valueReader),
                };
            }

            int count = valueReader.ReadInt32();
            if (count == -1)
            {
                return new ConstantReferenceValue();
            }

            if (elementTypeCode is Ecma.SerializationTypeCode.String)
            {
                var handleArray = new ConstantStringArray();
                handleArray.Value.Capacity = count;
                for (int i = 0; i < count; i++)
                {
                    string val = valueReader.ReadSerializedString();
                    handleArray.Value.Add(val == null ? new ConstantReferenceValue() : HandleString(val));
                }
                return handleArray;
            }

            if (elementTypeCode is Ecma.SerializationTypeCode.TaggedObject or Ecma.SerializationTypeCode.Type)
            {
                var handleArray = new ConstantHandleArray();
                handleArray.Value.Capacity = count;
                for (int i = 0; i < count; i++)
                {
                    Ecma.SerializationTypeCode typecode = elementTypeCode == Ecma.SerializationTypeCode.Type ? Ecma.SerializationTypeCode.Type : valueReader.ReadSerializationTypeCode();
                    handleArray.Value.Add(HandleCustomAttributeConstantValue(module, typecode, ref valueReader));
                }
                return handleArray;
            }

            return elementTypeCode switch
            {
                Ecma.SerializationTypeCode.Boolean => new ConstantBooleanArray { Value = GetCustomAttributeConstantArrayElements<bool>(ref valueReader, count) },
                Ecma.SerializationTypeCode.Char => new ConstantCharArray { Value = GetCustomAttributeConstantArrayElements<char>(ref valueReader, count) },
                Ecma.SerializationTypeCode.SByte => new ConstantSByteArray { Value = GetCustomAttributeConstantArrayElements<sbyte>(ref valueReader, count) },
                Ecma.SerializationTypeCode.Byte => new ConstantByteArray { Value = GetCustomAttributeConstantArrayElements<byte>(ref valueReader, count) },
                Ecma.SerializationTypeCode.Int16 => new ConstantInt16Array { Value = GetCustomAttributeConstantArrayElements<short>(ref valueReader, count) },
                Ecma.SerializationTypeCode.UInt16 => new ConstantUInt16Array { Value = GetCustomAttributeConstantArrayElements<ushort>(ref valueReader, count) },
                Ecma.SerializationTypeCode.Int32 => new ConstantInt32Array { Value = GetCustomAttributeConstantArrayElements<int>(ref valueReader, count) },
                Ecma.SerializationTypeCode.UInt32 => new ConstantUInt32Array { Value = GetCustomAttributeConstantArrayElements<uint>(ref valueReader, count) },
                Ecma.SerializationTypeCode.Int64 => new ConstantInt64Array { Value = GetCustomAttributeConstantArrayElements<long>(ref valueReader, count) },
                Ecma.SerializationTypeCode.UInt64 => new ConstantUInt64Array { Value = GetCustomAttributeConstantArrayElements<ulong>(ref valueReader, count) },
                Ecma.SerializationTypeCode.Single => new ConstantSingleArray { Value = GetCustomAttributeConstantArrayElements<float>(ref valueReader, count) },
                Ecma.SerializationTypeCode.Double => new ConstantDoubleArray { Value = GetCustomAttributeConstantArrayElements<double>(ref valueReader, count) },
                _ => throw new UnreachableException()
            };
        }

        private static TValue[] GetCustomAttributeConstantArrayElements<TValue>(ref Ecma.BlobReader blobReader, int count)
        {
            TValue[] result = new TValue[count];
            for (int i = 0; i < count; i++)
            {
                if (typeof(TValue) == typeof(bool))
                    result[i] = (TValue)(object)blobReader.ReadBoolean();
                if (typeof(TValue) == typeof(char))
                    result[i] = (TValue)(object)blobReader.ReadChar();
                if (typeof(TValue) == typeof(sbyte))
                    result[i] = (TValue)(object)blobReader.ReadSByte();
                if (typeof(TValue) == typeof(byte))
                    result[i] = (TValue)(object)blobReader.ReadByte();
                if (typeof(TValue) == typeof(short))
                    result[i] = (TValue)(object)blobReader.ReadInt16();
                if (typeof(TValue) == typeof(ushort))
                    result[i] = (TValue)(object)blobReader.ReadUInt16();
                if (typeof(TValue) == typeof(int))
                    result[i] = (TValue)(object)blobReader.ReadInt32();
                if (typeof(TValue) == typeof(uint))
                    result[i] = (TValue)(object)blobReader.ReadUInt32();
                if (typeof(TValue) == typeof(long))
                    result[i] = (TValue)(object)blobReader.ReadInt64();
                if (typeof(TValue) == typeof(ulong))
                    result[i] = (TValue)(object)blobReader.ReadUInt64();
                if (typeof(TValue) == typeof(float))
                    result[i] = (TValue)(object)blobReader.ReadSingle();
                if (typeof(TValue) == typeof(double))
                    result[i] = (TValue)(object)blobReader.ReadDouble();
            }
            return result;
        }
    }

}
