// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

using System.Reflection.Runtime.FieldInfos;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.MethodInfos.NativeFormat;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.TypeInfos.NativeFormat;

using Internal.Metadata.NativeFormat;

namespace System.Reflection
{
    internal sealed partial class RuntimeCustomAttributeData : CustomAttributeData
    {
        internal static IList<CustomAttributeData> GetCustomAttributes(
            MetadataReader? reader, CustomAttributeHandleCollection customAttributeHandles)
        {
            if (reader is null || customAttributeHandles.Count == 0)
                return Array.Empty<CustomAttributeData>();

            CustomAttributeData[] customAttributes = new CustomAttributeData[customAttributeHandles.Count];
            int index = 0;
            foreach (CustomAttributeHandle customAttributeHandle in customAttributeHandles)
                customAttributes[index++] = new RuntimeCustomAttributeData(reader, customAttributeHandle);

            return Array.AsReadOnly(customAttributes);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Metadata generation ensures custom attribute constructors are resolvable.")]
        private static ConstructorInfo ResolveAttributeConstructor(MetadataReader reader, CustomAttribute customAttribute)
        {
            if (customAttribute.Constructor.HandleType == HandleType.QualifiedMethod)
            {
                QualifiedMethod qualifiedMethod = customAttribute.Constructor.ToQualifiedMethodHandle(reader).GetQualifiedMethod(reader);
                TypeDefinitionHandle declaringType = qualifiedMethod.EnclosingType;
                MethodHandle methodHandle = qualifiedMethod.Method;
                NativeFormatRuntimeNamedTypeInfo namedAttributeType = NativeFormatRuntimeNamedTypeInfo.GetRuntimeNamedTypeInfo(reader, declaringType, default(RuntimeTypeHandle));
                return RuntimePlainConstructorInfo<NativeFormatMethodCommon>.GetRuntimePlainConstructorInfo(new NativeFormatMethodCommon(methodHandle, namedAttributeType, namedAttributeType));
            }

            MemberReference memberReference = customAttribute.Constructor.ToMemberReferenceHandle(reader).GetMemberReference(reader);

            // There is no chance a custom attribute type will be an open type specification so we can safely pass in the empty context here.
            TypeContext typeContext = new TypeContext(Array.Empty<RuntimeTypeInfo>(), Array.Empty<RuntimeTypeInfo>());
            RuntimeTypeInfo attributeType = memberReference.Parent.Resolve(reader, typeContext);
            MethodSignature signature = memberReference.Signature.ParseMethodSignature(reader);
            HandleCollection signatureParameters = signature.Parameters;
            Type[] expectedParameterTypes = new Type[signatureParameters.Count];
            int index = 0;
            foreach (Handle parameterHandle in signatureParameters)
            {
                expectedParameterTypes[index++] = parameterHandle.Resolve(reader, attributeType.TypeContext).ToType();
            }

            foreach (ConstructorInfo candidate in attributeType.ToType().GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                ReadOnlySpan<ParameterInfo> candidateParameters = candidate.GetParametersAsSpan();
                if (expectedParameterTypes.Length != candidateParameters.Length)
                    continue;

                bool matches = true;
                for (int i = 0; i < expectedParameterTypes.Length; i++)
                {
                    if (!expectedParameterTypes[i].Equals(candidateParameters[i].ParameterType))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                    return candidate;
            }

            throw new MissingMethodException();
        }
    }

    internal sealed partial class CustomAttributeEncodedArgument
    {
        /// <summary>
        /// Used to parse NativeFormat custom attribute data.
        /// </summary>
        private readonly struct CustomAttributeDataParser
        {
            private readonly CustomAttribute _attribute;
            private readonly MetadataReader _reader;

            public CustomAttributeDataParser(CustomAttribute attribute, MetadataReader reader)
            {
                _attribute = attribute;
                _reader = reader;
            }

            public CustomAttribute Attribute => _attribute;

            public bool ValidateProlog() => _reader is not null;

            public CustomAttributeEncodedArgument ParseValue(Handle value, CustomAttributeType type)
            {
                CustomAttributeType attributeType = type.EncodedType is CustomAttributeEncoding.Object
                    ? GetCustomAttributeType(value)
                    : type;
                if (value.HandleType is HandleType.ConstantEnumValue)
                {
                    value = value.ToConstantEnumValueHandle(_reader).GetConstantEnumValue(_reader).Value;
                }

                CustomAttributeEncodedArgument argument = new CustomAttributeEncodedArgument(attributeType);

                switch (value.HandleType)
                {
                    case HandleType.ConstantBooleanValue:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = value.ToConstantBooleanValueHandle(_reader).GetConstantBooleanValue(_reader).Value ? 1 : 0 };
                        break;
                    case HandleType.ConstantCharValue:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = value.ToConstantCharValueHandle(_reader).GetConstantCharValue(_reader).Value };
                        break;
                    case HandleType.ConstantByteValue:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = value.ToConstantByteValueHandle(_reader).GetConstantByteValue(_reader).Value };
                        break;
                    case HandleType.ConstantSByteValue:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = (byte)value.ToConstantSByteValueHandle(_reader).GetConstantSByteValue(_reader).Value };
                        break;
                    case HandleType.ConstantInt16Value:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = (ushort)value.ToConstantInt16ValueHandle(_reader).GetConstantInt16Value(_reader).Value };
                        break;
                    case HandleType.ConstantUInt16Value:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = value.ToConstantUInt16ValueHandle(_reader).GetConstantUInt16Value(_reader).Value };
                        break;
                    case HandleType.ConstantInt32Value:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = value.ToConstantInt32ValueHandle(_reader).GetConstantInt32Value(_reader).Value };
                        break;
                    case HandleType.ConstantUInt32Value:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = (int)value.ToConstantUInt32ValueHandle(_reader).GetConstantUInt32Value(_reader).Value };
                        break;
                    case HandleType.ConstantInt64Value:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte8 = value.ToConstantInt64ValueHandle(_reader).GetConstantInt64Value(_reader).Value };
                        break;
                    case HandleType.ConstantUInt64Value:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte8 = (long)value.ToConstantUInt64ValueHandle(_reader).GetConstantUInt64Value(_reader).Value };
                        break;
                    case HandleType.ConstantSingleValue:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte4 = BitConverter.SingleToInt32Bits(value.ToConstantSingleValueHandle(_reader).GetConstantSingleValue(_reader).Value) };
                        break;
                    case HandleType.ConstantDoubleValue:
                        argument.PrimitiveValue = new PrimitiveValue() { Byte8 = BitConverter.DoubleToInt64Bits(value.ToConstantDoubleValueHandle(_reader).GetConstantDoubleValue(_reader).Value) };
                        break;
                    case HandleType.ConstantStringValue:
                        argument.StringValue = value.ToConstantStringValueHandle(_reader).GetConstantStringValue(_reader).Value;
                        break;
                    case HandleType.TypeDefinition:
                    case HandleType.TypeReference:
                    case HandleType.TypeSpecification:
                        argument.TypeValue = value.Resolve(_reader, default).ToType();
                        break;
                    case HandleType.ConstantReferenceValue:
                        break;
                    default:
                        ParseArrayValue(value, attributeType, argument);
                        break;
                }

                return argument;
            }

            private void ParseArrayValue(
                Handle value,
                CustomAttributeType arrayType,
                CustomAttributeEncodedArgument argument)
            {
                if (value.HandleType is HandleType.ConstantEnumArray)
                {
                    value = value.ToConstantEnumArrayHandle(_reader).GetConstantEnumArray(_reader).Value;
                }

                if (value.HandleType is HandleType.ConstantReferenceValue)
                    return;

                CustomAttributeType elementType = new CustomAttributeType(
                    arrayType.EncodedArrayType,
                    CustomAttributeEncoding.Undefined,
                    arrayType.EncodedEnumType,
                    arrayType.EnumType);
                argument.ArrayValue = value.HandleType switch
                {
                    HandleType.ConstantBooleanArray => ToEncodedArguments(value.ToConstantBooleanArrayHandle(_reader).GetConstantBooleanArray(_reader).Value, elementType),
                    HandleType.ConstantCharArray => ToEncodedArguments(value.ToConstantCharArrayHandle(_reader).GetConstantCharArray(_reader).Value, elementType),
                    HandleType.ConstantByteArray => ToEncodedArguments(value.ToConstantByteArrayHandle(_reader).GetConstantByteArray(_reader).Value, elementType),
                    HandleType.ConstantSByteArray => ToEncodedArguments(value.ToConstantSByteArrayHandle(_reader).GetConstantSByteArray(_reader).Value, elementType),
                    HandleType.ConstantInt16Array => ToEncodedArguments(value.ToConstantInt16ArrayHandle(_reader).GetConstantInt16Array(_reader).Value, elementType),
                    HandleType.ConstantUInt16Array => ToEncodedArguments(value.ToConstantUInt16ArrayHandle(_reader).GetConstantUInt16Array(_reader).Value, elementType),
                    HandleType.ConstantInt32Array => ToEncodedArguments(value.ToConstantInt32ArrayHandle(_reader).GetConstantInt32Array(_reader).Value, elementType),
                    HandleType.ConstantUInt32Array => ToEncodedArguments(value.ToConstantUInt32ArrayHandle(_reader).GetConstantUInt32Array(_reader).Value, elementType),
                    HandleType.ConstantInt64Array => ToEncodedArguments(value.ToConstantInt64ArrayHandle(_reader).GetConstantInt64Array(_reader).Value, elementType),
                    HandleType.ConstantUInt64Array => ToEncodedArguments(value.ToConstantUInt64ArrayHandle(_reader).GetConstantUInt64Array(_reader).Value, elementType),
                    HandleType.ConstantSingleArray => ToEncodedArguments(value.ToConstantSingleArrayHandle(_reader).GetConstantSingleArray(_reader).Value, elementType),
                    HandleType.ConstantDoubleArray => ToEncodedArguments(value.ToConstantDoubleArrayHandle(_reader).GetConstantDoubleArray(_reader).Value, elementType),
                    HandleType.ConstantStringArray => ToEncodedArguments(value.ToConstantStringArrayHandle(_reader).GetConstantStringArray(_reader).Value, elementType),
                    HandleType.ConstantHandleArray => ToEncodedArguments(value.ToConstantHandleArrayHandle(_reader).GetConstantHandleArray(_reader).Value, elementType),
                    _ => throw new BadImageFormatException()
                };
            }

            private CustomAttributeEncodedArgument[] ToEncodedArguments(HandleCollection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (Handle value in values)
                {
                    result[index++] = ParseValue(value, elementType);
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(BooleanCollection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (bool value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = value ? 1 : 0 });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(CharCollection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (char value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(ByteCollection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (byte value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(SByteCollection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (sbyte value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = (byte)value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(Int16Collection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (short value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = (ushort)value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(UInt16Collection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (ushort value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(Int32Collection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (int value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(UInt32Collection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (uint value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = (int)value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(Int64Collection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (long value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte8 = value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(UInt64Collection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (ulong value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte8 = (long)value });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(SingleCollection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (float value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte4 = BitConverter.SingleToInt32Bits(value) });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument[] ToEncodedArguments(DoubleCollection values, CustomAttributeType elementType)
            {
                CustomAttributeEncodedArgument[] result = new CustomAttributeEncodedArgument[values.Count];
                int index = 0;
                foreach (double value in values)
                {
                    result[index++] = CreatePrimitiveArgument(elementType, new PrimitiveValue() { Byte8 = BitConverter.DoubleToInt64Bits(value) });
                }
                return result;
            }

            private static CustomAttributeEncodedArgument CreatePrimitiveArgument(CustomAttributeType type, PrimitiveValue value)
                => new CustomAttributeEncodedArgument(type) { PrimitiveValue = value };

            private CustomAttributeType GetCustomAttributeType(Handle value)
            {
                return value.HandleType switch
                {
                    HandleType.ConstantBooleanValue => CreateType(CustomAttributeEncoding.Boolean),
                    HandleType.ConstantCharValue => CreateType(CustomAttributeEncoding.Char),
                    HandleType.ConstantByteValue => CreateType(CustomAttributeEncoding.Byte),
                    HandleType.ConstantSByteValue => CreateType(CustomAttributeEncoding.SByte),
                    HandleType.ConstantInt16Value => CreateType(CustomAttributeEncoding.Int16),
                    HandleType.ConstantUInt16Value => CreateType(CustomAttributeEncoding.UInt16),
                    HandleType.ConstantInt32Value => CreateType(CustomAttributeEncoding.Int32),
                    HandleType.ConstantUInt32Value => CreateType(CustomAttributeEncoding.UInt32),
                    HandleType.ConstantInt64Value => CreateType(CustomAttributeEncoding.Int64),
                    HandleType.ConstantUInt64Value => CreateType(CustomAttributeEncoding.UInt64),
                    HandleType.ConstantSingleValue => CreateType(CustomAttributeEncoding.Float),
                    HandleType.ConstantDoubleValue => CreateType(CustomAttributeEncoding.Double),
                    // A null object argument is reported as a null string to match CoreCLR.
                    HandleType.ConstantStringValue or HandleType.ConstantReferenceValue => CreateType(CustomAttributeEncoding.String),
                    HandleType.TypeDefinition or HandleType.TypeReference or HandleType.TypeSpecification => CreateType(CustomAttributeEncoding.Type),
                    HandleType.ConstantEnumValue => CreateEnumType(
                        value.ToConstantEnumValueHandle(_reader).GetConstantEnumValue(_reader).Type,
                        isArray: false),
                    HandleType.ConstantBooleanArray => CreateArrayType(CustomAttributeEncoding.Boolean),
                    HandleType.ConstantCharArray => CreateArrayType(CustomAttributeEncoding.Char),
                    HandleType.ConstantByteArray => CreateArrayType(CustomAttributeEncoding.Byte),
                    HandleType.ConstantSByteArray => CreateArrayType(CustomAttributeEncoding.SByte),
                    HandleType.ConstantInt16Array => CreateArrayType(CustomAttributeEncoding.Int16),
                    HandleType.ConstantUInt16Array => CreateArrayType(CustomAttributeEncoding.UInt16),
                    HandleType.ConstantInt32Array => CreateArrayType(CustomAttributeEncoding.Int32),
                    HandleType.ConstantUInt32Array => CreateArrayType(CustomAttributeEncoding.UInt32),
                    HandleType.ConstantInt64Array => CreateArrayType(CustomAttributeEncoding.Int64),
                    HandleType.ConstantUInt64Array => CreateArrayType(CustomAttributeEncoding.UInt64),
                    HandleType.ConstantSingleArray => CreateArrayType(CustomAttributeEncoding.Float),
                    HandleType.ConstantDoubleArray => CreateArrayType(CustomAttributeEncoding.Double),
                    HandleType.ConstantStringArray => CreateArrayType(CustomAttributeEncoding.String),
                    HandleType.ConstantHandleArray => CreateArrayType(CustomAttributeEncoding.Object),
                    HandleType.ConstantEnumArray => CreateEnumType(
                        value.ToConstantEnumArrayHandle(_reader).GetConstantEnumArray(_reader).ElementType,
                        isArray: true),
                    _ => throw new BadImageFormatException()
                };
            }

            private CustomAttributeType CreateEnumType(Handle enumTypeHandle, bool isArray)
            {
                RuntimeType enumType = (RuntimeType)enumTypeHandle.Resolve(_reader, default).ToType();
                if (!enumType.IsEnum)
                {
                    throw new BadImageFormatException();
                }

                CustomAttributeEncoding underlyingType =
                    RuntimeCustomAttributeData.TypeToCustomAttributeEncoding((RuntimeType)enumType.GetEnumUnderlyingType());
                return isArray
                    ? new CustomAttributeType(CustomAttributeEncoding.Array, CustomAttributeEncoding.Enum, underlyingType, enumType)
                    : new CustomAttributeType(CustomAttributeEncoding.Enum, CustomAttributeEncoding.Undefined, underlyingType, enumType);
            }

            private static CustomAttributeType CreateType(CustomAttributeEncoding type)
                => new CustomAttributeType(
                    type,
                    CustomAttributeEncoding.Undefined,
                    CustomAttributeEncoding.Undefined,
                    enumType: null);

            private static CustomAttributeType CreateArrayType(CustomAttributeEncoding elementType)
                => new CustomAttributeType(
                    CustomAttributeEncoding.Array,
                    elementType,
                    CustomAttributeEncoding.Undefined,
                    enumType: null);
        }
    }

    internal static partial class PseudoCustomAttribute
    {
        private static FieldOffsetAttribute? GetFieldOffsetCustomAttribute(RuntimeFieldInfo field)
        {
            return field.DeclaringType!.IsExplicitLayout ?
                new FieldOffsetAttribute(field.ExplicitLayoutFieldOffsetData) :
                null;
        }
    }
}
