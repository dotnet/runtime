// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Security.Cryptography.Asn1
{
    internal class AsnSerializationConstraintException : CryptographicException
    {
        public AsnSerializationConstraintException()
        {
        }

        public AsnSerializationConstraintException(string message)
            : base(message)
        {
        }

        public AsnSerializationConstraintException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    internal class AsnAmbiguousFieldTypeException : AsnSerializationConstraintException
    {
        public AsnAmbiguousFieldTypeException(FieldInfo fieldInfo, Type ambiguousType)
            : base(SR.Format(SR.Cryptography_AsnSerializer_AmbiguousFieldType, fieldInfo.Name, fieldInfo.DeclaringType.FullName, ambiguousType.Namespace))
        {
        }
    }

    internal class AsnSerializerInvalidDefaultException : AsnSerializationConstraintException
    {
        internal AsnSerializerInvalidDefaultException()
        {
        }

        internal AsnSerializerInvalidDefaultException(Exception innerException)
            : base(string.Empty, innerException)
        {
        }
    }

    internal static class AsnSerializer
    {
        private const BindingFlags FieldFlags =
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance;

        private delegate void Serializer(object value, AsnWriter writer);
        private delegate object Deserializer(AsnReader reader);
        private delegate bool TryDeserializer<T>(AsnReader reader, out T value);

        private static readonly ConcurrentDictionary<Type, FieldInfo[]> s_orderedFields =
            new ConcurrentDictionary<Type, FieldInfo[]>();

        private static Deserializer TryOrFail<T>(TryDeserializer<T> tryDeserializer)
        {
            return reader =>
            {
                if (tryDeserializer(reader, out T value))
                    return value;

                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            };
        }

        private static FieldInfo[] GetOrderedFields(Type typeT)
        {
            return s_orderedFields.GetOrAdd(
                typeT,
                t =>
                {
                    // https://github.com/dotnet/corefx/issues/14606 asserts that ordering by the metadata
                    // token on a SequentialLayout will produce the fields in their layout order.
                    //
                    // Some other alternatives:
                    // * Add an attribute for controlling the field read order.
                    //    fieldInfos.Select(fi => (fi, fi.GetCustomAttribute<AsnFieldOrderAttribute>(false)).
                    //      Where(val => val.Item2 != null).OrderBy(val => val.Item2.OrderWeight).Select(val => val.Item1);
                    //
                    // * Use Marshal.OffsetOf as a sort key
                    //
                    // * Some sort of interface to return the fields in a declared order, using either
                    //   an existing object, or Activator.CreateInstance.  It would need to check that
                    //   any returned fields actually were declared on the type that was queried.
                    //
                    // * Invent more alternatives
                    FieldInfo[] fieldInfos = t.GetFields(FieldFlags);

                    if (fieldInfos.Length == 0)
                    {
                        return Array.Empty<FieldInfo>();
                    }

                    try
                    {
                        int token = fieldInfos[0].MetadataToken;
                    }
                    catch (InvalidOperationException)
                    {
                        // If MetadataToken isn't available (like in ILC) then just hope that
                        // the fields are returned in declared order.  For the most part that
                        // will result in data misaligning to fields and deserialization failing,
                        // thus a CryptographicException.
                        return fieldInfos;
                    }

                    Array.Sort(fieldInfos, (x, y) => x.MetadataToken.CompareTo(y.MetadataToken));
                    return fieldInfos;
                });
        }


        private static ChoiceAttribute GetChoiceAttribute(Type typeT)
        {
            ChoiceAttribute attr = typeT.GetCustomAttribute<ChoiceAttribute>(inherit: false);

            if (attr == null)
            {
                return null;
            }

            if (attr.AllowNull)
            {
                if (!CanBeNull(typeT))
                {
                    throw new AsnSerializationConstraintException(
                        SR.Format(SR.Cryptography_AsnSerializer_Choice_AllowNullNonNullable, typeT.FullName));
                }
            }

            return attr;
        }

        private static bool CanBeNull(Type t)
        {
            return !t.IsValueType ||
                   (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        private static void PopulateChoiceLookup(
            Dictionary<(TagClass, int), LinkedList<FieldInfo>> lookup,
            Type typeT,
            LinkedList<FieldInfo> currentSet)
        {
            foreach (FieldInfo fieldInfo in GetOrderedFields(typeT))
            {
                Type fieldType = fieldInfo.FieldType;

                if (!CanBeNull(fieldType))
                {
                    throw new AsnSerializationConstraintException(
                        SR.Format(
                            SR.Cryptography_AsnSerializer_Choice_NonNullableField,
                            fieldInfo.Name,
                            fieldInfo.DeclaringType.FullName));
                }

                fieldType = UnpackIfNullable(fieldType);

                if (currentSet.Contains(fieldInfo))
                {
                    throw new AsnSerializationConstraintException(
                        SR.Format(
                            SR.Cryptography_AsnSerializer_Choice_TypeCycle,
                            fieldInfo.Name,
                            fieldInfo.DeclaringType.FullName));
                }

                LinkedListNode<FieldInfo> newNode = new LinkedListNode<FieldInfo>(fieldInfo);
                currentSet.AddLast(newNode);

                if (GetChoiceAttribute(fieldType) != null)
                {
                    PopulateChoiceLookup(lookup, fieldType, currentSet);
                }
                else
                {
                    GetFieldInfo(
                        fieldType,
                        fieldInfo,
                        out SerializerFieldData fieldData);

                    if (fieldData.DefaultContents != null)
                    {
                        // ITU-T-REC-X.680-201508
                        // The application of the DEFAULT keyword is in SEQUENCE (sec 25, "ComponentType" grammar)
                        // There is no such keyword in the grammar for CHOICE.
                        throw new AsnSerializationConstraintException(
                            SR.Format(
                                SR.Cryptography_AsnSerializer_Choice_DefaultValueDisallowed,
                                fieldInfo.Name,
                                fieldInfo.DeclaringType.FullName));
                    }

                    var key = (fieldData.ExpectedTag.TagClass, fieldData.ExpectedTag.TagValue);

                    if (lookup.TryGetValue(key, out LinkedList<FieldInfo> existingSet))
                    {
                        FieldInfo existing = existingSet.Last.Value;

                        throw new AsnSerializationConstraintException(
                            SR.Format(
                                SR.Cryptography_AsnSerializer_Choice_ConflictingTagMapping,
                                fieldData.ExpectedTag.TagClass,
                                fieldData.ExpectedTag.TagValue,
                                fieldInfo.Name,
                                fieldInfo.DeclaringType.FullName,
                                existing.Name,
                                existing.DeclaringType.FullName));
                    }

                    lookup.Add(key, new LinkedList<FieldInfo>(currentSet));
                }

                currentSet.RemoveLast();
            }
        }

        private static void SerializeChoice(Type typeT, object value, AsnWriter writer)
        {
            // Ensure that the type is consistent with a Choice by using the same logic
            // as the deserializer.
            var lookup = new Dictionary<(TagClass, int), LinkedList<FieldInfo>>();
            LinkedList<FieldInfo> fields = new LinkedList<FieldInfo>();
            PopulateChoiceLookup(lookup, typeT, fields);

            FieldInfo relevantField = null;
            object relevantObject = null;

            // If the value is itself null and the choice allows it, write null.
            if (value == null)
            {
                if (GetChoiceAttribute(typeT).AllowNull)
                {
                    writer.WriteNull();
                    return;
                }
            }
            else
            {
                FieldInfo[] fieldInfos = GetOrderedFields(typeT);

                for (int i = 0; i < fieldInfos.Length; i++)
                {
                    FieldInfo fieldInfo = fieldInfos[i];
                    object fieldValue = fieldInfo.GetValue(value);

                    if (fieldValue != null)
                    {
                        if (relevantField != null)
                        {
                            throw new AsnSerializationConstraintException(
                                SR.Format(
                                    SR.Cryptography_AsnSerializer_Choice_TooManyValues,
                                    fieldInfo.Name,
                                    relevantField.Name,
                                    typeT.FullName));
                        }

                        relevantField = fieldInfo;
                        relevantObject = fieldValue;
                    }
                }
            }

            // If the element in the SEQUENCE (class/struct) is non-null it must resolve to a value.
            // If the SEQUENCE field was OPTIONAL then the element should have been null.
            if (relevantField == null)
            {
                throw new AsnSerializationConstraintException(
                    SR.Format(
                        SR.Cryptography_AsnSerializer_Choice_NoChoiceWasMade,
                        typeT.FullName));
            }

            Serializer serializer = GetSerializer(relevantField.FieldType, relevantField);
            serializer(relevantObject, writer);
        }

        private static object DeserializeChoice(AsnReader reader, Type typeT)
        {
            var lookup = new Dictionary<(TagClass, int), LinkedList<FieldInfo>>();
            LinkedList<FieldInfo> fields = new LinkedList<FieldInfo>();
            PopulateChoiceLookup(lookup, typeT, fields);

            Asn1Tag next = reader.PeekTag();

            if (next == Asn1Tag.Null)
            {
                ChoiceAttribute choiceAttr = GetChoiceAttribute(typeT);

                if (choiceAttr.AllowNull)
                {
                    reader.ReadNull();
                    return null;
                }

                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            var key = (next.TagClass, next.TagValue);

            if (lookup.TryGetValue(key, out LinkedList<FieldInfo> fieldInfos))
            {
                LinkedListNode<FieldInfo> currentNode = fieldInfos.Last;
                FieldInfo currentField = currentNode.Value;
                object currentObject = Activator.CreateInstance(currentField.DeclaringType);
                Deserializer deserializer = GetDeserializer(currentField.FieldType, currentField);
                object deserialized = deserializer(reader);
                currentField.SetValue(currentObject, deserialized);

                while (currentNode.Previous != null)
                {
                    currentNode = currentNode.Previous;
                    currentField = currentNode.Value;

                    object nextObject = Activator.CreateInstance(currentField.DeclaringType);
                    currentField.SetValue(nextObject, currentObject);

                    currentObject = nextObject;
                }

                return currentObject;
            }

            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
        }

        private static void SerializeCustomType(Type typeT, object value, AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            foreach (FieldInfo fieldInfo in typeT.GetFields(FieldFlags))
            {
                Serializer serializer = GetSerializer(fieldInfo.FieldType, fieldInfo);
                serializer(fieldInfo.GetValue(value), writer);
            }

            writer.PopSequence(tag);
        }

        private static object DeserializeCustomType(AsnReader reader, Type typeT, Asn1Tag expectedTag)
        {
            object target = Activator.CreateInstance(typeT);

            AsnReader sequence = reader.ReadSequence(expectedTag);

            foreach (FieldInfo fieldInfo in typeT.GetFields(FieldFlags))
            {
                Deserializer deserializer = GetDeserializer(fieldInfo.FieldType, fieldInfo);
                try
                {
                    fieldInfo.SetValue(target, deserializer(sequence));
                }
                catch (Exception e)
                {
                    throw new CryptographicException(
                        SR.Format(
                            SR.Cryptography_AsnSerializer_SetValueException,
                            fieldInfo.Name,
                            fieldInfo.DeclaringType.FullName),
                        e);
                }
            }

            sequence.ThrowIfNotEmpty();
            return target;
        }

        private static Deserializer ExplicitValueDeserializer(Deserializer valueDeserializer, Asn1Tag expectedTag)
        {
            return reader => ExplicitValueDeserializer(reader, valueDeserializer, expectedTag);
        }

        private static object ExplicitValueDeserializer(
            AsnReader reader,
            Deserializer valueDeserializer,
            Asn1Tag expectedTag)
        {
            AsnReader innerReader = reader.ReadSequence(expectedTag);
            object val = valueDeserializer(innerReader);

            innerReader.ThrowIfNotEmpty();
            return val;
        }

        private static Deserializer DefaultValueDeserializer(
            Deserializer valueDeserializer,
            Deserializer literalValueDeserializer,
            bool isOptional,
            byte[] defaultContents,
            Asn1Tag? expectedTag)
        {
            return reader =>
                DefaultValueDeserializer(
                    reader,
                    expectedTag,
                    valueDeserializer,
                    literalValueDeserializer,
                    defaultContents,
                    isOptional);
        }

        private static object DefaultValueDeserializer(
            AsnReader reader,
            Asn1Tag? expectedTag,
            Deserializer valueDeserializer,
            Deserializer literalValueDeserializer,
            byte[] defaultContents,
            bool isOptional)
        {
            if (reader.HasData)
            {
                Asn1Tag actualTag = reader.PeekTag();

                if (expectedTag == null ||
                    // Normalize the constructed bit so only class and value are compared
                    actualTag.AsPrimitive() == expectedTag.Value.AsPrimitive())
                {
                    return valueDeserializer(reader);
                }
            }

            if (isOptional)
            {
                return null;
            }

            if (defaultContents != null)
            {
                return DefaultValue(defaultContents, literalValueDeserializer);
            }

            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
        }

        private static Serializer GetSerializer(Type typeT, FieldInfo fieldInfo)
        {
            Serializer literalValueSerializer = GetSimpleSerializer(
                typeT,
                fieldInfo,
                out byte[] defaultContents,
                out bool isOptional,
                out Asn1Tag? explicitTag);

            Serializer serializer = literalValueSerializer;

            if (isOptional)
            {
                serializer = (obj, writer) =>
                {
                    if (obj != null)
                        literalValueSerializer(obj, writer);
                };
            }
            else if (defaultContents != null)
            {
                serializer = (obj, writer) =>
                {
                    AsnReader reader;

                    using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                    {
                        literalValueSerializer(obj, tmp);
                        reader = new AsnReader(tmp.Encode(), AsnEncodingRules.DER);
                    }

                    ReadOnlySpan<byte> encoded = reader.GetEncodedValue().Span;
                    bool equal = false;

                    if (encoded.Length == defaultContents.Length)
                    {
                        equal = true;

                        for (int i = 0; i < encoded.Length; i++)
                        {
                            if (encoded[i] != defaultContents[i])
                            {
                                equal = false;
                                break;
                            }
                        }
                    }

                    if (!equal)
                    {
                        literalValueSerializer(obj, writer);
                    }
                };
            }

            if (explicitTag.HasValue)
            {
                return (obj, writer) =>
                {
                    using (AsnWriter tmp = new AsnWriter(writer.RuleSet))
                    {
                        serializer(obj, tmp);

                        if (tmp.Encode().Length > 0)
                        {
                            writer.PushSequence(explicitTag.Value);
                            serializer(obj, writer);
                            writer.PopSequence(explicitTag.Value);
                        }
                    }
                };
            }

            return serializer;
        }

        private static Serializer GetSimpleSerializer(
            Type typeT,
            FieldInfo fieldInfo,
            out byte[] defaultContents,
            out bool isOptional,
            out Asn1Tag? explicitTag)
        {
            if (!typeT.IsSealed || typeT.ContainsGenericParameters)
            {
                throw new AsnSerializationConstraintException(
                    SR.Format(SR.Cryptography_AsnSerializer_NoOpenTypes, typeT.FullName));
            }

            GetFieldInfo(
                typeT,
                fieldInfo,
                out SerializerFieldData fieldData);

            defaultContents = fieldData.DefaultContents;
            isOptional = fieldData.IsOptional;

            typeT = UnpackIfNullable(typeT);
            bool isChoice = GetChoiceAttribute(typeT) != null;

            Asn1Tag tag;

            if (fieldData.HasExplicitTag)
            {
                explicitTag = fieldData.ExpectedTag;
                tag = new Asn1Tag(fieldData.TagType.GetValueOrDefault());
            }
            else
            {
                explicitTag = null;
                tag = fieldData.ExpectedTag;
            }

            // EXPLICIT Any can result in a TagType of null, as can CHOICE types.
            // In either of those cases we'll get a tag of EoC, but otherwise the tag should be valid.
            Debug.Assert(
                tag != Asn1Tag.EndOfContents || fieldData.IsAny || isChoice,
                $"Unknown state for typeT={typeT.FullName} on field {fieldInfo?.Name} of type {fieldInfo?.DeclaringType.FullName}");

            if (typeT.IsPrimitive)
            {
                // The AsnTypeAttribute resolved in GetFieldInfo currently doesn't allow
                // type modification for any of the primitive types.  If that changes, we
                // need to either pass it through here or do some other form of rerouting.
                Debug.Assert(!fieldData.WasCustomized);
                return GetPrimitiveSerializer(typeT, tag);
            }

            if (typeT.IsEnum)
            {
                if (typeT.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0)
                {
                    return (value, writer) => writer.WriteNamedBitList(tag, value);
                }

                return (value, writer) => writer.WriteEnumeratedValue(tag, value);
            }

            if (typeT == typeof(string))
            {
                if (fieldData.TagType == UniversalTagNumber.ObjectIdentifier)
                {
                    return (value, writer) =>
                        writer.WriteObjectIdentifier(
                            tag,
                            (string)value ?? throw new CryptographicException(SR.Argument_InvalidOidValue));
                }

                // Because all string types require an attribute saying their type, we'll
                // definitely have a TagType value.
                return (value, writer) =>
                    writer.WriteCharacterString(
                        tag,
                        fieldData.TagType.Value,
                        (string)value ?? throw new CryptographicException(SR.Argument_InvalidOidValue));
            }

            if (typeT == typeof(ReadOnlyMemory<byte>) && !fieldData.IsCollection)
            {
                if (fieldData.IsAny)
                {
                    // If a tag was specified via [ExpectedTag] (other than because it's wrapped in an EXPLICIT)
                    // then don't let the serializer write a violation of that expectation.
                    if (fieldData.SpecifiedTag && !fieldData.HasExplicitTag)
                    {
                        return (value, writer) =>
                        {
                            ReadOnlyMemory<byte> data = (ReadOnlyMemory<byte>)value;

                            if (!Asn1Tag.TryParse(data.Span, out Asn1Tag actualTag, out _) ||
                                actualTag.AsPrimitive() != fieldData.ExpectedTag.AsPrimitive())
                            {
                                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                            }

                            writer.WriteEncodedValue(data);
                        };
                    }

                    return (value, writer) => writer.WriteEncodedValue((ReadOnlyMemory<byte>)value);
                }

                if (fieldData.TagType == UniversalTagNumber.BitString)
                {
                    return (value, writer) => writer.WriteBitString(tag, ((ReadOnlyMemory<byte>)value).Span);
                }

                if (fieldData.TagType == UniversalTagNumber.OctetString)
                {
                    return (value, writer) => writer.WriteOctetString(tag, ((ReadOnlyMemory<byte>)value).Span);
                }

                if (fieldData.TagType == UniversalTagNumber.Integer)
                {
                    return (value, writer) => writer.WriteInteger(tag, ((ReadOnlyMemory<byte>)value).Span);
                }

                Debug.Fail($"No ReadOnlyMemory<byte> handler for {fieldData.TagType}");
                throw new CryptographicException();
            }

            if (typeT == typeof(Oid))
            {
                return (value, writer) => writer.WriteObjectIdentifier(fieldData.ExpectedTag, (Oid)value);
            }

            if (typeT.IsArray)
            {
                if (typeT.GetArrayRank() != 1)
                {
                    throw new AsnSerializationConstraintException(
                        SR.Format(SR.Cryptography_AsnSerializer_NoMultiDimensionalArrays, typeT.FullName));
                }

                Type baseType = typeT.GetElementType();

                if (baseType.IsArray)
                {
                    throw new AsnSerializationConstraintException(
                        SR.Format(SR.Cryptography_AsnSerializer_NoJaggedArrays, typeT.FullName));
                }

                Serializer serializer = GetSerializer(baseType, null);

                if (fieldData.TagType == UniversalTagNumber.SetOf)
                {
                    return (value, writer) =>
                    {
                        writer.PushSetOf(tag);

                        foreach (var item in (Array)value)
                        {
                            serializer(item, writer);
                        }

                        writer.PopSetOf(tag);
                    };
                }

                Debug.Assert(fieldData.TagType == 0 || fieldData.TagType == UniversalTagNumber.SequenceOf);
                return (value, writer) =>
                {
                    writer.PushSequence(tag);

                    foreach (var item in (Array)value)
                    {
                        serializer(item, writer);
                    }

                    writer.PopSequence(tag);
                };
            }

            if (typeT == typeof(DateTimeOffset))
            {
                if (fieldData.TagType == UniversalTagNumber.UtcTime)
                {
                    return (value, writer) => writer.WriteUtcTime(tag, (DateTimeOffset)value);
                }

                if (fieldData.TagType == UniversalTagNumber.GeneralizedTime)
                {
                    Debug.Assert(fieldData.DisallowGeneralizedTimeFractions != null);

                    return (value, writer) =>
                        writer.WriteGeneralizedTime(tag, (DateTimeOffset)value, fieldData.DisallowGeneralizedTimeFractions.Value);
                }

                Debug.Fail($"No DateTimeOffset handler for {fieldData.TagType}");
                throw new CryptographicException();
            }

            if (typeT == typeof(BigInteger))
            {
                return (value, writer) => writer.WriteInteger(tag, (BigInteger)value);
            }

            if (typeT.IsLayoutSequential)
            {
                if (isChoice)
                {
                    return (value, writer) => SerializeChoice(typeT, value, writer);
                }

                if (fieldData.TagType == UniversalTagNumber.Sequence)
                {
                    return (value, writer) => SerializeCustomType(typeT, value, writer, tag);
                }
            }

            throw new AsnSerializationConstraintException(
                SR.Format(SR.Cryptography_AsnSerializer_UnhandledType, typeT.FullName));
        }

        private static Deserializer GetDeserializer(Type typeT, FieldInfo fieldInfo)
        {
            Deserializer literalValueDeserializer = GetSimpleDeserializer(
                typeT,
                fieldInfo,
                out SerializerFieldData fieldData);

            Deserializer deserializer = literalValueDeserializer;

            if (fieldData.HasExplicitTag)
            {
                deserializer = ExplicitValueDeserializer(deserializer, fieldData.ExpectedTag);
            }

            if (fieldData.IsOptional || fieldData.DefaultContents != null)
            {
                Asn1Tag? expectedTag = null;

                if (fieldData.SpecifiedTag || fieldData.TagType != null)
                {
                    expectedTag = fieldData.ExpectedTag;
                }

                deserializer = DefaultValueDeserializer(
                    deserializer,
                    literalValueDeserializer,
                    fieldData.IsOptional,
                    fieldData.DefaultContents,
                    expectedTag);
            }

            return deserializer;
        }

        private static Deserializer GetSimpleDeserializer(
            Type typeT,
            FieldInfo fieldInfo,
            out SerializerFieldData fieldData)
        {
            if (!typeT.IsSealed || typeT.ContainsGenericParameters)
            {
                throw new AsnSerializationConstraintException(
                    SR.Format(SR.Cryptography_AsnSerializer_NoOpenTypes, typeT.FullName));
            }

            GetFieldInfo(typeT, fieldInfo, out fieldData);

            SerializerFieldData localFieldData = fieldData;
            typeT = UnpackIfNullable(typeT);

            if (fieldData.IsAny)
            {
                if (typeT == typeof(ReadOnlyMemory<byte>))
                {
                    Asn1Tag matchTag = fieldData.ExpectedTag;

                    // EXPLICIT Any can't be validated, just return the value.
                    if (fieldData.HasExplicitTag || !fieldData.SpecifiedTag)
                    {
                        return reader => reader.GetEncodedValue();
                    }

                    // If it's not declared EXPLICIT but an [ExpectedTag] was provided,
                    // use it as a validation/filter.
                    return reader =>
                    {
                        Asn1Tag nextTag = reader.PeekTag();

                        if (matchTag.TagClass != nextTag.TagClass ||
                            matchTag.TagValue != nextTag.TagValue)
                        {
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }

                        return reader.GetEncodedValue();
                    };
                }

                throw new AsnSerializationConstraintException(
                    SR.Format(SR.Cryptography_AsnSerializer_UnhandledType, typeT.FullName));
            }

            if (GetChoiceAttribute(typeT) != null)
            {
                return reader => DeserializeChoice(reader, typeT);
            }

            Debug.Assert(fieldData.TagType != null);

            Asn1Tag expectedTag = fieldData.HasExplicitTag ? new Asn1Tag(fieldData.TagType.Value) : fieldData.ExpectedTag;

            if (typeT.IsPrimitive)
            {
                // The AsnTypeAttribute resolved in GetFieldInfo currently doesn't allow
                // type modification for any of the primitive types.  If that changes, we
                // need to either pass it through here or do some other form of rerouting.
                Debug.Assert(!fieldData.WasCustomized);

                return GetPrimitiveDeserializer(typeT, expectedTag);
            }

            if (typeT.IsEnum)
            {
                if (typeT.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0)
                {
                    return reader => reader.GetNamedBitListValue(expectedTag, typeT);
                }

                return reader => reader.GetEnumeratedValue(expectedTag, typeT);
            }

            if (typeT == typeof(string))
            {
                if (fieldData.TagType == UniversalTagNumber.ObjectIdentifier)
                {
                    return reader => reader.ReadObjectIdentifierAsString(expectedTag);
                }

                // Because all string types require an attribute saying their type, we'll
                // definitely have a value.
                Debug.Assert(localFieldData.TagType != null);
                return reader => reader.GetCharacterString(expectedTag, localFieldData.TagType.Value);
            }

            if (typeT == typeof(ReadOnlyMemory<byte>) && !fieldData.IsCollection)
            {
                if (fieldData.TagType == UniversalTagNumber.BitString)
                {
                    return reader =>
                    {
                        if (reader.TryGetPrimitiveBitStringValue(expectedTag, out _, out ReadOnlyMemory<byte> contents))
                        {
                            return contents;
                        }

                        // Guaranteed too big, because it has the tag and length.
                        int length = reader.PeekEncodedValue().Length;
                        byte[] rented = ArrayPool<byte>.Shared.Rent(length);

                        try
                        {
                            if (reader.TryCopyBitStringBytes(expectedTag, rented, out _, out int bytesWritten))
                            {
                                return new ReadOnlyMemory<byte>(rented.AsSpan(0, bytesWritten).ToArray());
                            }

                            Debug.Fail("TryCopyBitStringBytes produced more data than the encoded size");
                            throw new CryptographicException();
                        }
                        finally
                        {
                            Array.Clear(rented, 0, length);
                            ArrayPool<byte>.Shared.Return(rented);
                        }
                    };
                }

                if (fieldData.TagType == UniversalTagNumber.OctetString)
                {
                    return reader =>
                    {
                        if (reader.TryGetPrimitiveOctetStringBytes(expectedTag, out ReadOnlyMemory<byte> contents))
                        {
                            return contents;
                        }

                        // Guaranteed too big, because it has the tag and length.
                        int length = reader.PeekEncodedValue().Length;
                        byte[] rented = ArrayPool<byte>.Shared.Rent(length);

                        try
                        {
                            if (reader.TryCopyOctetStringBytes(expectedTag, rented, out int bytesWritten))
                            {
                                return new ReadOnlyMemory<byte>(rented.AsSpan(0, bytesWritten).ToArray());
                            }

                            Debug.Fail("TryCopyOctetStringBytes produced more data than the encoded size");
                            throw new CryptographicException();
                        }
                        finally
                        {
                            Array.Clear(rented, 0, length);
                            ArrayPool<byte>.Shared.Return(rented);
                        }
                    };
                }

                if (fieldData.TagType == UniversalTagNumber.Integer)
                {
                    return reader => reader.GetIntegerBytes(expectedTag);
                }

                Debug.Fail($"No ReadOnlyMemory<byte> handler for {fieldData.TagType}");
                throw new CryptographicException();
            }

            if (typeT == typeof(Oid))
            {
                bool skipFriendlyName = !fieldData.PopulateOidFriendlyName.GetValueOrDefault();
                return reader => reader.ReadObjectIdentifier(expectedTag, skipFriendlyName);
            }

            if (typeT.IsArray)
            {
                if (typeT.GetArrayRank() != 1)
                {
                    throw new AsnSerializationConstraintException(
                        SR.Format(SR.Cryptography_AsnSerializer_NoMultiDimensionalArrays, typeT.FullName));
                }

                Type baseType = typeT.GetElementType();

                if (baseType.IsArray)
                {
                    throw new AsnSerializationConstraintException(
                        SR.Format(SR.Cryptography_AsnSerializer_NoJaggedArrays, typeT.FullName));
                }

                return reader =>
                {
                    LinkedList<object> linkedList = new LinkedList<object>();

                    AsnReader collectionReader;

                    if (localFieldData.TagType == UniversalTagNumber.SetOf)
                    {
                        collectionReader = reader.ReadSetOf(expectedTag);
                    }
                    else
                    {
                        Debug.Assert(localFieldData.TagType == 0 || localFieldData.TagType == UniversalTagNumber.SequenceOf);
                        collectionReader = reader.ReadSequence(expectedTag);
                    }

                    Deserializer deserializer = GetDeserializer(baseType, null);

                    while (collectionReader.HasData)
                    {
                        object elem = deserializer(collectionReader);
                        LinkedListNode<object> node = new LinkedListNode<object>(elem);
                        linkedList.AddLast(node);
                    }

                    object[] objArr = linkedList.ToArray();
                    Array arr = Array.CreateInstance(baseType, objArr.Length);
                    Array.Copy(objArr, arr, objArr.Length);
                    return arr;
                };
            }

            if (typeT == typeof(DateTimeOffset))
            {
                if (fieldData.TagType == UniversalTagNumber.UtcTime)
                {
                    if (fieldData.TwoDigitYearMax != null)
                    {
                        return reader =>
                            reader.GetUtcTime(expectedTag, localFieldData.TwoDigitYearMax.Value);
                    }

                    return reader => reader.GetUtcTime(expectedTag);
                }

                if (fieldData.TagType == UniversalTagNumber.GeneralizedTime)
                {
                    Debug.Assert(fieldData.DisallowGeneralizedTimeFractions != null);
                    bool disallowFractions = fieldData.DisallowGeneralizedTimeFractions.Value;

                    return reader => reader.GetGeneralizedTime(expectedTag, disallowFractions);
                }

                Debug.Fail($"No DateTimeOffset handler for {fieldData.TagType}");
                throw new CryptographicException();
            }

            if (typeT == typeof(BigInteger))
            {
                return reader => reader.GetInteger(expectedTag);
            }

            if (typeT.IsLayoutSequential)
            {
                if (fieldData.TagType == UniversalTagNumber.Sequence)
                {
                    return reader => DeserializeCustomType(reader, typeT, expectedTag);
                }
            }

            throw new AsnSerializationConstraintException(
                SR.Format(SR.Cryptography_AsnSerializer_UnhandledType, typeT.FullName));
        }

        private static object DefaultValue(
            byte[] defaultContents,
            Deserializer valueDeserializer)
        {
            Debug.Assert(defaultContents != null);

            try
            {
                AsnReader defaultValueReader = new AsnReader(defaultContents, AsnEncodingRules.DER);

                object obj = valueDeserializer(defaultValueReader);

                if (defaultValueReader.HasData)
                {
                    throw new AsnSerializerInvalidDefaultException();
                }

                return obj;
            }
            catch (AsnSerializerInvalidDefaultException)
            {
                throw;
            }
            catch (CryptographicException e)
            {
                throw new AsnSerializerInvalidDefaultException(e);
            }
        }

        private static void GetFieldInfo(
            Type typeT,
            FieldInfo fieldInfo,
            out SerializerFieldData serializerFieldData)
        {
            serializerFieldData = new SerializerFieldData();

            object[] typeAttrs = fieldInfo?.GetCustomAttributes(typeof(AsnTypeAttribute), false) ??
                                 Array.Empty<object>();

            if (typeAttrs.Length > 1)
            {
                throw new AsnSerializationConstraintException(
                    SR.Format(
                        fieldInfo.Name,
                        fieldInfo.DeclaringType.FullName,
                        typeof(AsnTypeAttribute).FullName));
            }

            Type unpackedType = UnpackIfNullable(typeT);

            if (typeAttrs.Length == 1)
            {
                Type[] expectedTypes;
                object attr = typeAttrs[0];
                serializerFieldData.WasCustomized = true;

                if (attr is AnyValueAttribute)
                {
                    serializerFieldData.IsAny = true;
                    expectedTypes = new[] { typeof(ReadOnlyMemory<byte>) };
                }
                else if (attr is IntegerAttribute)
                {
                    expectedTypes = new[] { typeof(ReadOnlyMemory<byte>) };
                    serializerFieldData.TagType = UniversalTagNumber.Integer;
                }
                else if (attr is BitStringAttribute)
                {
                    expectedTypes = new[] { typeof(ReadOnlyMemory<byte>) };
                    serializerFieldData.TagType = UniversalTagNumber.BitString;
                }
                else if (attr is OctetStringAttribute)
                {
                    expectedTypes = new[] { typeof(ReadOnlyMemory<byte>) };
                    serializerFieldData.TagType = UniversalTagNumber.OctetString;
                }
                else if (attr is ObjectIdentifierAttribute oid)
                {
                    serializerFieldData.PopulateOidFriendlyName = oid.PopulateFriendlyName;
                    expectedTypes = new[] { typeof(Oid), typeof(string) };
                    serializerFieldData.TagType = UniversalTagNumber.ObjectIdentifier;

                    if (oid.PopulateFriendlyName && unpackedType == typeof(string))
                    {
                        throw new AsnSerializationConstraintException(
                            SR.Format(
                                SR.Cryptography_AsnSerializer_PopulateFriendlyNameOnString,
                                fieldInfo.Name,
                                fieldInfo.DeclaringType.FullName,
                                typeof(Oid).FullName));
                    }
                }
                else if (attr is BMPStringAttribute)
                {
                    expectedTypes = new[] { typeof(string) };
                    serializerFieldData.TagType = UniversalTagNumber.BMPString;
                }
                else if (attr is IA5StringAttribute)
                {
                    expectedTypes = new[] { typeof(string) };
                    serializerFieldData.TagType = UniversalTagNumber.IA5String;
                }
                else if (attr is UTF8StringAttribute)
                {
                    expectedTypes = new[] { typeof(string) };
                    serializerFieldData.TagType = UniversalTagNumber.UTF8String;
                }
                else if (attr is PrintableStringAttribute)
                {
                    expectedTypes = new[] { typeof(string) };
                    serializerFieldData.TagType = UniversalTagNumber.PrintableString;
                }
                else if (attr is VisibleStringAttribute)
                {
                    expectedTypes = new[] { typeof(string) };
                    serializerFieldData.TagType = UniversalTagNumber.VisibleString;
                }
                else if (attr is SequenceOfAttribute)
                {
                    serializerFieldData.IsCollection = true;
                    expectedTypes = null;
                    serializerFieldData.TagType = UniversalTagNumber.SequenceOf;
                }
                else if (attr is SetOfAttribute)
                {
                    serializerFieldData.IsCollection = true;
                    expectedTypes = null;
                    serializerFieldData.TagType = UniversalTagNumber.SetOf;
                }
                else if (attr is UtcTimeAttribute utcAttr)
                {
                    expectedTypes = new[] { typeof(DateTimeOffset) };
                    serializerFieldData.TagType = UniversalTagNumber.UtcTime;

                    if (utcAttr.TwoDigitYearMax != 0)
                    {
                        serializerFieldData.TwoDigitYearMax = utcAttr.TwoDigitYearMax;

                        if (serializerFieldData.TwoDigitYearMax < 99)
                        {
                            throw new AsnSerializationConstraintException(
                                SR.Format(
                                    SR.Cryptography_AsnSerializer_UtcTimeTwoDigitYearMaxTooSmall,
                                    fieldInfo.Name,
                                    fieldInfo.DeclaringType.FullName,
                                    serializerFieldData.TwoDigitYearMax));
                        }
                    }
                }
                else if (attr is GeneralizedTimeAttribute genTimeAttr)
                {
                    expectedTypes = new[] { typeof(DateTimeOffset) };
                    serializerFieldData.TagType = UniversalTagNumber.GeneralizedTime;
                    serializerFieldData.DisallowGeneralizedTimeFractions = genTimeAttr.DisallowFractions;
                }
                else
                {
                    Debug.Fail($"Unregistered {nameof(AsnTypeAttribute)} kind: {attr.GetType().FullName}");
                    throw new CryptographicException();
                }

                Debug.Assert(serializerFieldData.IsCollection || expectedTypes != null);

                if (!serializerFieldData.IsCollection && Array.IndexOf(expectedTypes, unpackedType) < 0)
                {
                    throw new AsnSerializationConstraintException(
                        SR.Format(
                            SR.Cryptography_AsnSerializer_UnexpectedTypeForAttribute,
                            fieldInfo.Name,
                            fieldInfo.DeclaringType.Namespace,
                            unpackedType.FullName,
                            string.Join(", ", expectedTypes.Select(t => t.FullName))));
                }
            }

            var defaultValueAttr = fieldInfo?.GetCustomAttribute<DefaultValueAttribute>(false);
            serializerFieldData.DefaultContents = defaultValueAttr?.EncodedBytes;

            if (serializerFieldData.TagType == null && !serializerFieldData.IsAny)
            {
                if (unpackedType == typeof(bool))
                {
                    serializerFieldData.TagType = UniversalTagNumber.Boolean;
                }
                else if (unpackedType == typeof(sbyte) ||
                         unpackedType == typeof(byte) ||
                         unpackedType == typeof(short) ||
                         unpackedType == typeof(ushort) ||
                         unpackedType == typeof(int) ||
                         unpackedType == typeof(uint) ||
                         unpackedType == typeof(long) ||
                         unpackedType == typeof(ulong) ||
                         unpackedType == typeof(BigInteger))
                {
                    serializerFieldData.TagType = UniversalTagNumber.Integer;
                }
                else if (unpackedType.IsLayoutSequential)
                {
                    serializerFieldData.TagType = UniversalTagNumber.Sequence;
                }
                else if (unpackedType == typeof(ReadOnlyMemory<byte>) ||
                    unpackedType == typeof(string) ||
                    unpackedType == typeof(DateTimeOffset))
                {
                    throw new AsnAmbiguousFieldTypeException(fieldInfo, unpackedType);
                }
                else if (unpackedType == typeof(Oid))
                {
                    serializerFieldData.TagType = UniversalTagNumber.ObjectIdentifier;
                }
                else if (unpackedType.IsArray)
                {
                    serializerFieldData.TagType = UniversalTagNumber.SequenceOf;
                }
                else if (unpackedType.IsEnum)
                {
                    if (typeT.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0)
                    {
                        serializerFieldData.TagType = UniversalTagNumber.BitString;
                    }
                    else
                    {
                        serializerFieldData.TagType = UniversalTagNumber.Enumerated;
                    }
                }
                else if (fieldInfo != null)
                {
                    Debug.Fail($"No tag type bound for {fieldInfo.DeclaringType.FullName}.{fieldInfo.Name}");
                    throw new AsnSerializationConstraintException();
                }
            }

            serializerFieldData.IsOptional = fieldInfo?.GetCustomAttribute<OptionalValueAttribute>(false) != null;

            if (serializerFieldData.IsOptional && !CanBeNull(typeT))
            {
                throw new AsnSerializationConstraintException(
                    SR.Format(
                        SR.Cryptography_AsnSerializer_Optional_NonNullableField,
                        fieldInfo.Name,
                        fieldInfo.DeclaringType.FullName));
            }

            bool isChoice = GetChoiceAttribute(typeT) != null;

            var tagOverride = fieldInfo?.GetCustomAttribute<ExpectedTagAttribute>(false);

            if (tagOverride != null)
            {
                if (isChoice && !tagOverride.ExplicitTag)
                {
                    throw new AsnSerializationConstraintException(
                        SR.Format(
                            SR.Cryptography_AsnSerializer_SpecificTagChoice,
                            fieldInfo.Name,
                            fieldInfo.DeclaringType.FullName,
                            typeT.FullName));
                }

                // This will throw for unmapped TagClass values
                serializerFieldData.ExpectedTag = new Asn1Tag(tagOverride.TagClass, tagOverride.TagValue);
                serializerFieldData.HasExplicitTag = tagOverride.ExplicitTag;
                serializerFieldData.SpecifiedTag = true;
                return;
            }

            if (isChoice)
            {
                serializerFieldData.TagType = null;
            }

            serializerFieldData.SpecifiedTag = false;
            serializerFieldData.HasExplicitTag = false;
            serializerFieldData.ExpectedTag = new Asn1Tag(serializerFieldData.TagType.GetValueOrDefault());
        }

        private static Type UnpackIfNullable(Type typeT)
        {
            return Nullable.GetUnderlyingType(typeT) ?? typeT;
        }

        private static Deserializer GetPrimitiveDeserializer(Type typeT, Asn1Tag tag)
        {
            if (typeT == typeof(bool))
                return reader => reader.ReadBoolean(tag);
            if (typeT == typeof(int))
                return TryOrFail((AsnReader reader, out int value) => reader.TryReadInt32(tag, out value));
            if (typeT == typeof(uint))
                return TryOrFail((AsnReader reader, out uint value) => reader.TryReadUInt32(tag, out value));
            if (typeT == typeof(short))
                return TryOrFail((AsnReader reader, out short value) => reader.TryReadInt16(tag, out value));
            if (typeT == typeof(ushort))
                return TryOrFail((AsnReader reader, out ushort value) => reader.TryReadUInt16(tag, out value));
            if (typeT == typeof(byte))
                return TryOrFail((AsnReader reader, out byte value) => reader.TryReadUInt8(tag, out value));
            if (typeT == typeof(sbyte))
                return TryOrFail((AsnReader reader, out sbyte value) => reader.TryReadInt8(tag, out value));
            if (typeT == typeof(long))
                return TryOrFail((AsnReader reader, out long value) => reader.TryReadInt64(tag, out value));
            if (typeT == typeof(ulong))
                return TryOrFail((AsnReader reader, out ulong value) => reader.TryReadUInt64(tag, out value));

            throw new AsnSerializationConstraintException(
                SR.Format(
                    SR.Cryptography_AsnSerializer_UnhandledType,
                    typeT.FullName));
        }

        private static Serializer GetPrimitiveSerializer(Type typeT, Asn1Tag primitiveTag)
        {
            if (typeT == typeof(bool))
                return (value, writer) => writer.WriteBoolean(primitiveTag, (bool)value);
            if (typeT == typeof(int))
                return (value, writer) => writer.WriteInteger(primitiveTag, (int)value);
            if (typeT == typeof(uint))
                return (value, writer) => writer.WriteInteger(primitiveTag, (uint)value);
            if (typeT == typeof(short))
                return (value, writer) => writer.WriteInteger(primitiveTag, (short)value);
            if (typeT == typeof(ushort))
                return (value, writer) => writer.WriteInteger(primitiveTag, (ushort)value);
            if (typeT == typeof(byte))
                return (value, writer) => writer.WriteInteger(primitiveTag, (byte)value);
            if (typeT == typeof(sbyte))
                return (value, writer) => writer.WriteInteger(primitiveTag, (sbyte)value);
            if (typeT == typeof(long))
                return (value, writer) => writer.WriteInteger(primitiveTag, (long)value);
            if (typeT == typeof(ulong))
                return (value, writer) => writer.WriteInteger(primitiveTag, (ulong)value);

            throw new AsnSerializationConstraintException(
                SR.Format(
                    SR.Cryptography_AsnSerializer_UnhandledType,
                    typeT.FullName));
        }

        /// <summary>
        /// Read ASN.1 data from <paramref name="source"/> encoded under the specified encoding rules into
        /// the typed structure.
        /// </summary>
        /// <typeparam name="T">
        /// The type to deserialize as.
        /// In order to be deserialized the type must have sequential layout, be sealed, and be composed of
        /// members that are also able to be deserialized by this method.
        /// </typeparam>
        /// <param name="source">A view of the encoded bytes to be deserialized.</param>
        /// <param name="ruleSet">The ASN.1 encoding ruleset to use for reading <paramref name="source"/>.</param>
        /// <returns>A deserialized instance of <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// Except for where required to for avoiding ambiguity, this method does not check that there are
        /// no cycles in the type graph for <typeparamref name="T"/>.  If <typeparamref name="T"/> is a
        /// reference type (class) which includes a cycle in the type graph,
        /// then it is possible for the data in <paramref name="source"/> to cause
        /// an arbitrary extension to the maximum stack depth of this routine, leading to a
        /// <see cref="StackOverflowException"/>.
        ///
        /// If <typeparamref name="T"/> is a value type (struct) the compiler will enforce that there are no
        /// cycles in the type graph.
        ///
        /// When reference types are used the onus is on the caller of this method to prevent cycles, or to
        /// mitigate the possibility of the stack overflow.
        /// </remarks>
        /// <exception cref="AsnSerializationConstraintException">
        ///   A portion of <typeparamref name="T"/> is invalid for deserialization.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   Any of the data in <paramref name="source"/> is invalid for mapping to the return value,
        ///   or data remains after deserialization.
        /// </exception>
        public static T Deserialize<T>(ReadOnlyMemory<byte> source, AsnEncodingRules ruleSet)
        {
            Deserializer deserializer = GetDeserializer(typeof(T), null);

            AsnReader reader = new AsnReader(source, ruleSet);

            T t = (T)deserializer(reader);

            reader.ThrowIfNotEmpty();
            return t;
        }

        /// <summary>
        /// Read the first ASN.1 data element from <paramref name="source"/> encoded under the specified
        /// encoding rules into the typed structure.
        /// </summary>
        /// <typeparam name="T">
        /// The type to deserialize as.
        /// In order to be deserialized the type must have sequential layout, be sealed, and be composed of
        /// members that are also able to be deserialized by this method.
        /// </typeparam>
        /// <param name="source">A view of the encoded bytes to be deserialized.</param>
        /// <param name="ruleSet">The ASN.1 encoding ruleset to use for reading <paramref name="source"/>.</param>
        /// <param name="bytesRead">Receives the number of bytes read from <paramref name="source"/>.</param>
        /// <returns>A deserialized instance of <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// Except for where required to for avoiding ambiguity, this method does not check that there are
        /// no cycles in the type graph for <typeparamref name="T"/>.  If <typeparamref name="T"/> is a
        /// reference type (class) which includes a cycle in the type graph,
        /// then it is possible for the data in <paramref name="source"/> to cause
        /// an arbitrary extension to the maximum stack depth of this routine, leading to a
        /// <see cref="StackOverflowException"/>.
        ///
        /// If <typeparamref name="T"/> is a value type (struct) the compiler will enforce that there are no
        /// cycles in the type graph.
        ///
        /// When reference types are used the onus is on the caller of this method to prevent cycles, or to
        /// mitigate the possibility of the stack overflow.
        /// </remarks>
        /// <exception cref="AsnSerializationConstraintException">
        ///   A portion of <typeparamref name="T"/> is invalid for deserialization.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   Any of the data in <paramref name="source"/> is invalid for mapping to the return value.
        /// </exception>
        public static T Deserialize<T>(ReadOnlyMemory<byte> source, AsnEncodingRules ruleSet, out int bytesRead)
        {
            Deserializer deserializer = GetDeserializer(typeof(T), null);
            AsnReader reader = new AsnReader(source, ruleSet);
            ReadOnlyMemory<byte> firstElement = reader.PeekEncodedValue();

            T t = (T)deserializer(reader);
            bytesRead = firstElement.Length;
            return t;
        }

        /// <summary>
        /// Serialize <paramref name="value"/> into an ASN.1 writer under the specified encoding rules.
        /// </summary>
        /// <typeparam name="T">
        /// The type to serialize as.
        /// In order to be serialized the type must have sequential layout, be sealed, and be composed of
        /// members that are also able to be serialized by this method.
        /// </typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="ruleSet">The ASN.1 encoding ruleset to use for writing <paramref name="value"/>.</param>
        /// <returns>A deserialized instance of <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// Except for where required to for avoiding ambiguity, this method does not check that there are
        /// no cycles in the type graph for <typeparamref name="T"/>.  If <typeparamref name="T"/> is a
        /// reference type (class) which includes a cycle in the type graph, and there is a cycle within the
        /// object graph this method will consume memory and stack space until one is exhausted.
        ///
        /// If <typeparamref name="T"/> is a value type (struct) the compiler will enforce that there are no
        /// cycles in the type graph.
        ///
        /// When reference types are used the onus is on the caller of this method to prevent object cycles,
        /// or to mitigate the possibility of the stack overflow or memory exhaustion.
        /// </remarks>
        /// <exception cref="AsnSerializationConstraintException">
        ///   A portion of <typeparamref name="T"/> is invalid for deserialization.
        /// </exception>
        public static AsnWriter Serialize<T>(T value, AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            try
            {
                Serialize(value, writer);
                return writer;
            }
            catch
            {
                writer.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Serialize <paramref name="value"/> into an ASN.1 writer under the specified encoding rules.
        /// </summary>
        /// <typeparam name="T">
        /// The type to serialize as.
        /// In order to be serialized the type must have sequential layout, be sealed, and be composed of
        /// members that are also able to be serialized by this method.
        /// </typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="existingWriter">An existing writer into which <paramref name="value"/> should be written.</param>
        /// <returns>A deserialized instance of <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// Except for where required to for avoiding ambiguity, this method does not check that there are
        /// no cycles in the type graph for <typeparamref name="T"/>.  If <typeparamref name="T"/> is a
        /// reference type (class) which includes a cycle in the type graph, and there is a cycle within the
        /// object graph this method will consume memory and stack space until one is exhausted.
        ///
        /// If <typeparamref name="T"/> is a value type (struct) the compiler will enforce that there are no
        /// cycles in the type graph.
        ///
        /// When reference types are used the onus is on the caller of this method to prevent object cycles,
        /// or to mitigate the possibility of the stack overflow or memory exhaustion.
        /// </remarks>
        /// <exception cref="AsnSerializationConstraintException">
        ///   A portion of <typeparamref name="T"/> is invalid for deserialization.
        /// </exception>
        public static void Serialize<T>(T value, AsnWriter existingWriter)
        {
            if (existingWriter == null)
            {
                throw new ArgumentNullException(nameof(existingWriter));
            }

            Serializer serializer = GetSerializer(typeof(T), null);
            serializer(value, existingWriter);
        }

        private struct SerializerFieldData
        {
            internal bool WasCustomized;
            internal UniversalTagNumber? TagType;
            internal bool? PopulateOidFriendlyName;
            internal bool IsAny;
            internal bool IsCollection;
            internal byte[] DefaultContents;
            internal bool HasExplicitTag;
            internal bool SpecifiedTag;
            internal bool IsOptional;
            internal int? TwoDigitYearMax;
            internal Asn1Tag ExpectedTag;
            internal bool? DisallowGeneralizedTimeFractions;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class ExpectedTagAttribute : Attribute
    {
        public TagClass TagClass { get; }
        public int TagValue { get; }
        public bool ExplicitTag { get; set; }

        public ExpectedTagAttribute(int tagValue)
            : this(TagClass.ContextSpecific, tagValue)
        {
        }

        public ExpectedTagAttribute(TagClass tagClass, int tagValue)
        {
            TagClass = tagClass;
            TagValue = tagValue;
        }
    }

    internal abstract class AsnTypeAttribute : Attribute
    {
        internal AsnTypeAttribute()
        {
        }
    }

    internal abstract class AsnEncodingRuleAttribute : Attribute
    {
        internal AsnEncodingRuleAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class OctetStringAttribute : AsnTypeAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class BitStringAttribute : AsnTypeAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class AnyValueAttribute : AsnTypeAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class ObjectIdentifierAttribute : AsnTypeAttribute
    {
        public ObjectIdentifierAttribute()
        {
        }

        public bool PopulateFriendlyName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class BMPStringAttribute : AsnTypeAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class IA5StringAttribute : AsnTypeAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class UTF8StringAttribute : AsnTypeAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class PrintableStringAttribute : AsnTypeAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class VisibleStringAttribute : AsnTypeAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class SequenceOfAttribute : AsnTypeAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class SetOfAttribute : AsnTypeAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class IntegerAttribute : AsnTypeAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class UtcTimeAttribute : AsnTypeAttribute
    {
        public int TwoDigitYearMax { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class GeneralizedTimeAttribute : AsnTypeAttribute
    {
        public bool DisallowFractions { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class OptionalValueAttribute : AsnEncodingRuleAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class DefaultValueAttribute : AsnEncodingRuleAttribute
    {
        internal byte[] EncodedBytes { get; }

        public DefaultValueAttribute(params byte[] encodedValue)
        {
            EncodedBytes = encodedValue;
        }

        public ReadOnlyMemory<byte> EncodedValue => EncodedBytes;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    internal sealed class ChoiceAttribute : Attribute
    {
        public bool AllowNull { get; set; }
    }
}
