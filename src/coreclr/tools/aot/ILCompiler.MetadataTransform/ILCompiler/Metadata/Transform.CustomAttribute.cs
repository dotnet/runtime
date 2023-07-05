// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;

using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;

using Debug = System.Diagnostics.Debug;
using NamedArgumentMemberKind = Internal.Metadata.NativeFormat.NamedArgumentMemberKind;

namespace ILCompiler.Metadata
{
    internal partial class Transform<TPolicy>
    {
        private List<CustomAttribute> HandleCustomAttributes(Cts.Ecma.EcmaModule module, Ecma.CustomAttributeHandleCollection attributes)
        {
            List<CustomAttribute> customAttributes = new List<CustomAttribute>(attributes.Count);

            var attributeTypeProvider = new Cts.Ecma.CustomAttributeTypeProvider(module);

            Ecma.MetadataReader reader = module.MetadataReader;

            foreach (var attributeHandle in attributes)
            {
                if (!_policy.GeneratesMetadata(module, attributeHandle))
                    continue;

                Ecma.CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);

                // TODO-NICE: We can intern the attributes based on the CA constructor and blob bytes

                Cts.MethodDesc constructor = module.GetMethod(attribute.Constructor);
                var decodedValue = attribute.DecodeValue(attributeTypeProvider);

                customAttributes.Add(HandleCustomAttribute(constructor, decodedValue));
            }

            return customAttributes;
        }

        private CustomAttribute HandleCustomAttribute(Cts.MethodDesc constructor, Ecma.CustomAttributeValue<Cts.TypeDesc> decodedValue)
        {
            CustomAttribute result = new CustomAttribute
            {
                Constructor = HandleQualifiedMethod(constructor),
            };

            result.FixedArguments.Capacity = decodedValue.FixedArguments.Length;
            foreach (var decodedArgument in decodedValue.FixedArguments)
            {
                var fixedArgument = HandleCustomAttributeConstantValue(decodedArgument.Type, decodedArgument.Value);
                result.FixedArguments.Add(fixedArgument);
            }

            result.NamedArguments.Capacity = decodedValue.NamedArguments.Length;
            foreach (var decodedArgument in decodedValue.NamedArguments)
            {
                var namedArgument = new NamedArgument
                {
                    Flags = decodedArgument.Kind == Ecma.CustomAttributeNamedArgumentKind.Field ?
                        NamedArgumentMemberKind.Field : NamedArgumentMemberKind.Property,
                    Name = HandleString(decodedArgument.Name),
                    Type = HandleType(decodedArgument.Type),
                    Value = HandleCustomAttributeConstantValue(decodedArgument.Type, decodedArgument.Value)
                };
                result.NamedArguments.Add(namedArgument);
            }

            return result;
        }

        private MetadataRecord HandleCustomAttributeConstantValue(Cts.TypeDesc type, object value)
        {
            switch (type.UnderlyingType.Category)
            {
                case Cts.TypeFlags.Boolean:
                    return new ConstantBooleanValue { Value = (bool)value };
                case Cts.TypeFlags.Byte:
                    return new ConstantByteValue { Value = (byte)value };
                case Cts.TypeFlags.Char:
                    return new ConstantCharValue { Value = (char)value };
                case Cts.TypeFlags.Double:
                    return new ConstantDoubleValue { Value = (double)value };
                case Cts.TypeFlags.Int16:
                    return new ConstantInt16Value { Value = (short)value };
                case Cts.TypeFlags.Int32:
                    return new ConstantInt32Value { Value = (int)value };
                case Cts.TypeFlags.Int64:
                    return new ConstantInt64Value { Value = (long)value };
                case Cts.TypeFlags.SByte:
                    return new ConstantSByteValue { Value = (sbyte)value };
                case Cts.TypeFlags.Single:
                    return new ConstantSingleValue { Value = (float)value };
                case Cts.TypeFlags.UInt16:
                    return new ConstantUInt16Value { Value = (ushort)value };
                case Cts.TypeFlags.UInt32:
                    return new ConstantUInt32Value { Value = (uint)value };
                case Cts.TypeFlags.UInt64:
                    return new ConstantUInt64Value { Value = (ulong)value };
            }

            if (value == null)
            {
                return new ConstantReferenceValue();
            }

            if (type.IsString)
            {
                return HandleString((string)value);
            }

            if (type.IsSzArray)
            {
                return HandleCustomAttributeConstantArray(
                    (Cts.ArrayType)type,
                    (ImmutableArray<Ecma.CustomAttributeTypedArgument<Cts.TypeDesc>>)value);
            }

            Debug.Assert(value is Cts.TypeDesc);
            Debug.Assert(type is Cts.MetadataType
                && ((Cts.MetadataType)type).Name == "Type"
                && ((Cts.MetadataType)type).Namespace == "System");

            return HandleType((Cts.TypeDesc)value);
        }

        private MetadataRecord HandleCustomAttributeConstantArray(
            Cts.ArrayType type, ImmutableArray<Ecma.CustomAttributeTypedArgument<Cts.TypeDesc>> value)
        {
            Cts.TypeDesc elementType = type.ElementType;

            if (elementType.IsEnum)
            {
                Cts.TypeSystemContext context = type.Context;
                return new ConstantEnumArray
                {
                    ElementType = HandleType(elementType),
                    Value = HandleCustomAttributeConstantArray(context.GetArrayType(elementType.UnderlyingType), value),
                };
            }

            switch (elementType.Category)
            {
                case Cts.TypeFlags.Boolean:
                    return new ConstantBooleanArray { Value = GetCustomAttributeConstantArrayElements<bool>(value) };
                case Cts.TypeFlags.Byte:
                    return new ConstantByteArray { Value = GetCustomAttributeConstantArrayElements<byte>(value) };
                case Cts.TypeFlags.Char:
                    return new ConstantCharArray { Value = GetCustomAttributeConstantArrayElements<char>(value) };
                case Cts.TypeFlags.Double:
                    return new ConstantDoubleArray { Value = GetCustomAttributeConstantArrayElements<double>(value) };
                case Cts.TypeFlags.Int16:
                    return new ConstantInt16Array { Value = GetCustomAttributeConstantArrayElements<short>(value) };
                case Cts.TypeFlags.Int32:
                    return new ConstantInt32Array { Value = GetCustomAttributeConstantArrayElements<int>(value) };
                case Cts.TypeFlags.Int64:
                    return new ConstantInt64Array { Value = GetCustomAttributeConstantArrayElements<long>(value) };
                case Cts.TypeFlags.SByte:
                    return new ConstantSByteArray { Value = GetCustomAttributeConstantArrayElements<sbyte>(value) };
                case Cts.TypeFlags.Single:
                    return new ConstantSingleArray { Value = GetCustomAttributeConstantArrayElements<float>(value) };
                case Cts.TypeFlags.UInt16:
                    return new ConstantUInt16Array { Value = GetCustomAttributeConstantArrayElements<ushort>(value) };
                case Cts.TypeFlags.UInt32:
                    return new ConstantUInt32Array { Value = GetCustomAttributeConstantArrayElements<uint>(value) };
                case Cts.TypeFlags.UInt64:
                    return new ConstantUInt64Array { Value = GetCustomAttributeConstantArrayElements<ulong>(value) };
            }

            if (elementType.IsString)
            {
                var record = new ConstantStringArray();
                record.Value.Capacity = value.Length;
                foreach (var element in value)
                {
                    MetadataRecord elementRecord = element.Value == null ?
                        (MetadataRecord)new ConstantReferenceValue() : HandleString((string)element.Value);
                    record.Value.Add(elementRecord);
                }
                return record;
            }

            var result = new ConstantHandleArray();
            result.Value.Capacity = value.Length;
            for (int i = 0; i < value.Length; i++)
            {
                MetadataRecord elementRecord = HandleCustomAttributeConstantValue(value[i].Type, value[i].Value);
                if (value[i].Type.IsEnum)
                {
                    elementRecord = new ConstantBoxedEnumValue
                    {
                        Value = elementRecord,
                        Type = HandleType(value[i].Type)
                    };
                }
                result.Value.Add(elementRecord);
            }

            return result;
        }

        private static TValue[] GetCustomAttributeConstantArrayElements<TValue>(ImmutableArray<Ecma.CustomAttributeTypedArgument<Cts.TypeDesc>> value)
        {
            TValue[] result = new TValue[value.Length];
            for (int i = 0; i < value.Length; i++)
            {
                result[i] = (TValue)value[i].Value;
            }
            return result;
        }
    }

}
