﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Enumeration;
using System.Text.RegularExpressions;
using Xunit;

namespace System.Reflection.Tests
{
    public class NullabilityInfoContextTests
    {
        private static readonly NullabilityInfoContext nullabilityContext = new NullabilityInfoContext();
        private static readonly Type testType = typeof(TypeWithNotNullContext);
        private static readonly Type genericType = typeof(GenericTest<TypeWithNotNullContext>);
        private static readonly Type stringType = typeof(string);
        private static readonly BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static IEnumerable<object[]> FieldTestData()
        {
            yield return new object[] { "FieldNullable", NullabilityState.Nullable, NullabilityState.Nullable, typeof(string) };
            yield return new object[] { "FieldUnknown", NullabilityState.Unknown, NullabilityState.Unknown, typeof(TypeWithNotNullContext) };
            yield return new object[] { "FieldNonNullable", NullabilityState.NotNull, NullabilityState.NotNull, typeof(NullabilityInfoContextTests) };
            yield return new object[] { "FieldValueTypeUnknown", NullabilityState.NotNull, NullabilityState.NotNull, typeof(int) };
            yield return new object[] { "FieldValueTypeNotNull", NullabilityState.NotNull, NullabilityState.NotNull, typeof(double) };
            yield return new object[] { "FieldValueTypeNullable", NullabilityState.Nullable, NullabilityState.Nullable, typeof(int?) };
            yield return new object[] { "FieldDisallowNull", NullabilityState.Nullable, NullabilityState.NotNull, typeof(string) };
            yield return new object[] { "FieldAllowNull", NullabilityState.NotNull, NullabilityState.Nullable, typeof(string) };
            yield return new object[] { "FieldDisallowNull2", NullabilityState.Nullable, NullabilityState.NotNull, typeof(string) };
            yield return new object[] { "FieldAllowNull2", NullabilityState.NotNull, NullabilityState.Nullable, typeof(string) };
            yield return new object[] { "FieldNotNull", NullabilityState.NotNull, NullabilityState.Nullable, typeof(string) };
            yield return new object[] { "FieldMaybeNull", NullabilityState.Nullable, NullabilityState.NotNull, typeof(string) };
            yield return new object[] { "FieldMaybeNull", NullabilityState.Nullable, NullabilityState.NotNull, typeof(string) };
            yield return new object[] { "FieldNotNull2", NullabilityState.NotNull, NullabilityState.Nullable, typeof(string) };
        }

        [Theory]
        [MemberData(nameof(FieldTestData))]
        public void FieldTest(string fieldName, NullabilityState readState, NullabilityState writeState, Type type)
        {
            FieldInfo field = testType.GetField(fieldName, flags);
            NullabilityInfo nullability = nullabilityContext.Create(field);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(writeState, nullability.WriteState);
            Assert.Equal(type, nullability.Type);
            Assert.Empty(nullability.GenericTypeArguments);
            Assert.Null(nullability.ElementType);
        }

        public static IEnumerable<object[]> EventTestData()
        {
            yield return new object[] { "EventNullable", NullabilityState.Nullable, NullabilityState.Nullable, typeof(EventHandler) };
            yield return new object[] { "EventUnknown", NullabilityState.Unknown, NullabilityState.Unknown, typeof(EventHandler) };
            yield return new object[] { "EventNotNull", NullabilityState.NotNull, NullabilityState.NotNull, typeof(EventHandler) };
        }

        [Theory]
        [MemberData(nameof(EventTestData))]
        public void EventTest(string eventName, NullabilityState readState, NullabilityState writeState, Type type)
        {
            EventInfo @event = testType.GetEvent(eventName);
            NullabilityInfo nullability = nullabilityContext.Create(@event);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(writeState, nullability.WriteState);
            Assert.Equal(type, nullability.Type);
            Assert.Empty(nullability.GenericTypeArguments);
            Assert.Null(nullability.ElementType);
        }

        public static IEnumerable<object[]> PropertyTestData()
        {
            yield return new object[] { "PropertyNullable", NullabilityState.Nullable, NullabilityState.Nullable, typeof(TypeWithNotNullContext) };
            yield return new object[] { "PropertyNullableReadOnly", NullabilityState.Nullable, NullabilityState.Unknown, typeof(TypeWithNotNullContext) };
            yield return new object[] { "PropertyUnknown", NullabilityState.Unknown, NullabilityState.Unknown, typeof(string) };
            yield return new object[] { "PropertyNonNullable", NullabilityState.NotNull, NullabilityState.NotNull, typeof(NullabilityInfoContextTests) };
            yield return new object[] { "PropertyValueTypeUnknown", NullabilityState.NotNull, NullabilityState.NotNull, typeof(short) };
            yield return new object[] { "PropertyValueType", NullabilityState.NotNull, NullabilityState.NotNull, typeof(float) };
            yield return new object[] { "PropertyValueTypeNullable", NullabilityState.Nullable, NullabilityState.Nullable, typeof(long?) };
            yield return new object[] { "PropertyValueTypeDisallowNull", NullabilityState.Nullable, NullabilityState.NotNull, typeof(int?) };
            yield return new object[] { "PropertyValueTypeAllowNull", NullabilityState.NotNull, NullabilityState.NotNull, typeof(byte) };
            yield return new object[] { "PropertyValueTypeNotNull", NullabilityState.NotNull, NullabilityState.Nullable, typeof(int?) };
            yield return new object[] { "PropertyValueTypeMaybeNull", NullabilityState.NotNull, NullabilityState.NotNull, typeof(byte) };
            yield return new object[] { "PropertyDisallowNull", NullabilityState.Nullable, NullabilityState.NotNull, typeof(string) };
            yield return new object[] { "PropertyAllowNull", NullabilityState.NotNull, NullabilityState.Nullable, typeof(string) };
            yield return new object[] { "PropertyDisallowNull2", NullabilityState.Nullable, NullabilityState.NotNull, typeof(string) };
            yield return new object[] { "PropertyAllowNull2", NullabilityState.NotNull, NullabilityState.Nullable, typeof(string) };
            yield return new object[] { "PropertyNotNull", NullabilityState.NotNull, NullabilityState.Nullable, typeof(string) };
            yield return new object[] { "PropertyMaybeNull", NullabilityState.Nullable, NullabilityState.NotNull, typeof(string) };
            yield return new object[] { "PropertyMaybeNull2", NullabilityState.Nullable, NullabilityState.NotNull, typeof(string) };
            yield return new object[] { "PropertyNotNull2", NullabilityState.NotNull, NullabilityState.Nullable, typeof(string) };
        }

        [Theory]
        [MemberData(nameof(PropertyTestData))]
        public void PropertyTest(string propertyName, NullabilityState readState, NullabilityState writeState, Type type)
        {
            PropertyInfo property = testType.GetProperty(propertyName, flags);
            NullabilityInfo nullability = nullabilityContext.Create(property);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(readState, nullabilityContext.Create(property.GetMethod.ReturnParameter).ReadState);
            Assert.Equal(writeState, nullability.WriteState);
            if (property.SetMethod != null)
            {
                Assert.Equal(writeState, nullabilityContext.Create(property.SetMethod.GetParameters()[0]).WriteState);
            }
            Assert.Equal(type, nullability.Type);
            Assert.Empty(nullability.GenericTypeArguments);
            Assert.Null(nullability.ElementType);
        }

        public static IEnumerable<object[]> ArrayPropertyTestData()
        {
            yield return new object[] { "PropertyArrayUnknown", NullabilityState.Unknown, NullabilityState.Unknown };
            yield return new object[] { "PropertyArrayNullNull", NullabilityState.Nullable, NullabilityState.Nullable };
            yield return new object[] { "PropertyArrayNullNon", NullabilityState.Nullable, NullabilityState.NotNull };
            yield return new object[] { "PropertyArrayNonNull", NullabilityState.NotNull, NullabilityState.Nullable };
            yield return new object[] { "PropertyArrayNonNon", NullabilityState.NotNull, NullabilityState.NotNull };
        }

        [Theory]
        [SkipOnMono("Disabling NullablePublicOnly feature not work for Mono tests")]
        [MemberData(nameof(ArrayPropertyTestData))]
        public void ArrayPropertyTest(string propertyName, NullabilityState elementState, NullabilityState propertyState)
        {
            PropertyInfo property = testType.GetProperty(propertyName, flags);
            NullabilityInfo nullability = nullabilityContext.Create(property);
            Assert.Equal(propertyState, nullability.ReadState);
            Assert.NotNull(nullability.ElementType);
            Assert.Equal(elementState, nullability.ElementType.ReadState);
            Assert.Empty(nullability.GenericTypeArguments);
        }

        public static IEnumerable<object[]> GenericArrayPropertyTestData()
        {
            yield return new object[] { "PropertyArrayUnknown", NullabilityState.Unknown, NullabilityState.Unknown };
            yield return new object[] { "PropertyArrayNullNull", NullabilityState.Nullable, NullabilityState.Nullable }; // T?[]? PropertyArrayNullNull { get; set; }
            yield return new object[] { "PropertyArrayNullNon", NullabilityState.Nullable, NullabilityState.NotNull };   // T?[] PropertyArrayNullNon { get; set; } 
            yield return new object[] { "PropertyArrayNonNull", NullabilityState.Nullable, NullabilityState.Nullable };  // T[]? PropertyArrayNonNull { get; set; }
            yield return new object[] { "PropertyArrayNonNon", NullabilityState.Nullable, NullabilityState.NotNull };    // T[] PropertyArrayNonNon { get; set; }
        }

        [Theory]
        [SkipOnMono("Disabling NullablePublicOnly feature not work for Mono tests")]
        [MemberData(nameof(GenericArrayPropertyTestData))]
        public void GenericArrayPropertyTest(string propertyName, NullabilityState elementState, NullabilityState propertyState)
        {
            PropertyInfo property = genericType.GetProperty(propertyName, flags);
            NullabilityInfo nullability = nullabilityContext.Create(property);
            Assert.Equal(propertyState, nullability.ReadState);
            Assert.NotNull(nullability.ElementType);
            Assert.Equal(elementState, nullability.ElementType.ReadState);
            Assert.Empty(nullability.GenericTypeArguments);
        }

        public static IEnumerable<object[]> JaggedArrayPropertyTestData()
        {
            yield return new object[] { "PropertyJaggedArrayUnknown", NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown };
            yield return new object[] { "PropertyJaggedArrayNullNullNull", NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.Nullable };
            yield return new object[] { "PropertyJaggedArrayNullNullNon", NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.NotNull };
            yield return new object[] { "PropertyJaggedArrayNullNonNull", NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.Nullable };
            yield return new object[] { "PropertyJaggedArrayNonNullNull", NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.Nullable };
            yield return new object[] { "PropertyJaggedArrayNullNonNon", NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.NotNull };
            yield return new object[] { "PropertyJaggedArrayNonNonNull", NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.Nullable };
        }

        [Theory]
        [SkipOnMono("Disabling NullablePublicOnly feature not work for Mono tests")]
        [MemberData(nameof(JaggedArrayPropertyTestData))]
        public void JaggedArrayPropertyTest(string propertyName, NullabilityState innermodtElementState, NullabilityState elementState, NullabilityState propertyState)
        {
            PropertyInfo property = testType.GetProperty(propertyName, flags);
            NullabilityInfo nullability = nullabilityContext.Create(property);
            Assert.Equal(propertyState, nullability.ReadState);
            Assert.NotNull(nullability.ElementType);
            Assert.Equal(elementState, nullability.ElementType.ReadState);
            Assert.NotNull(nullability.ElementType.ElementType);
            Assert.Equal(innermodtElementState, nullability.ElementType.ElementType.ReadState);
            Assert.Empty(nullability.GenericTypeArguments);
        }

        public static IEnumerable<object[]> TuplePropertyTestData()
        {
            yield return new object[] { "PropertyTupleUnknown", NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown };
            yield return new object[] { "PropertyTupleNullNullNullNull", NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.Nullable };
            yield return new object[] { "PropertyTupleNonNullNonNon", NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.NotNull };
            yield return new object[] { "PropertyTupleNullNonNullNull", NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.Nullable };
            yield return new object[] { "PropertyTupleNonNullNonNull", NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.Nullable };
            yield return new object[] { "PropertyTupleNonNonNonNon", NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.NotNull };
        }

        [Theory]
        [SkipOnMono("Disabling NullablePublicOnly feature not work for Mono tests")]
        [MemberData(nameof(TuplePropertyTestData))]
        public void TuplePropertyTest(string propertyName, NullabilityState genericParam1, NullabilityState genericParam2, NullabilityState genericParam3, NullabilityState propertyState)
        {
            PropertyInfo property = testType.GetProperty(propertyName, flags);
            NullabilityInfo nullability = nullabilityContext.Create(property);
            Assert.Equal(propertyState, nullability.ReadState);
            Assert.NotEmpty(nullability.GenericTypeArguments);
            Assert.Equal(genericParam1, nullability.GenericTypeArguments[0].ReadState);
            Assert.Equal(genericParam2, nullability.GenericTypeArguments[1].ReadState);
            Assert.Equal(genericParam3, nullability.GenericTypeArguments[2].ReadState);
            Assert.Null(nullability.ElementType);
        }

        public static IEnumerable<object[]> GenericTuplePropertyTestData()
        {
            yield return new object[] { "PropertyTupleUnknown", NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown };
            yield return new object[] { "PropertyTupleNullNullNullNull", NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.Nullable }; // Tuple<T?, string?, string?>?
            yield return new object[] { "PropertyTupleNonNullNonNon", NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.NotNull };      // Tuple<T, T?, string>
            yield return new object[] { "PropertyTupleNullNonNullNull", NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.Nullable };  // Tuple<string?, T, T?>?
            yield return new object[] { "PropertyTupleNonNullNonNull", NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.Nullable };    // Tuple<T, string?, string>?
            yield return new object[] { "PropertyTupleNonNonNonNon", NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull };        // Tuple<string, string, T>
        }

        [Theory]
        [SkipOnMono("Disabling NullablePublicOnly feature not work for Mono tests")]
        [MemberData(nameof(GenericTuplePropertyTestData))]
        public void GenericTuplePropertyTest(string propertyName, NullabilityState genericParam1, NullabilityState genericParam2, NullabilityState genericParam3, NullabilityState propertyState)
        {
            PropertyInfo property = genericType.GetProperty(propertyName, flags);
            NullabilityInfo nullability = nullabilityContext.Create(property);
            Assert.Equal(propertyState, nullability.ReadState);
            Assert.NotEmpty(nullability.GenericTypeArguments);
            Assert.Equal(genericParam1, nullability.GenericTypeArguments[0].ReadState);
            Assert.Equal(genericParam2, nullability.GenericTypeArguments[1].ReadState);
            Assert.Equal(genericParam3, nullability.GenericTypeArguments[2].ReadState);
            Assert.Null(nullability.ElementType);
        }

        public static IEnumerable<object[]> DictionaryPropertyTestData()
        {
            yield return new object[] { "PropertyDictionaryUnknown", NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown };
            yield return new object[] { "PropertyDictionaryNullNullNullNon", NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.NotNull };
            yield return new object[] { "PropertyDictionaryNonNullNonNull", NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.Nullable };
            yield return new object[] { "PropertyDictionaryNullNonNonNull", NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.Nullable };
            yield return new object[] { "PropertyDictionaryNonNullNonNon", NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.NotNull };
            yield return new object[] { "PropertyDictionaryNonNonNonNull", NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.Nullable };
            yield return new object[] { "PropertyDictionaryNonNonNonNon", NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.NotNull };
        }

        [Theory]
        [SkipOnMono("Disabling NullablePublicOnly feature not work for Mono tests")]
        [MemberData(nameof(DictionaryPropertyTestData))]
        public void DictionaryPropertyTest(string propertyName, NullabilityState keyState, NullabilityState valueElement, NullabilityState valueState, NullabilityState propertyState)
        {
            PropertyInfo property = testType.GetProperty(propertyName, flags);
            NullabilityInfo nullability = nullabilityContext.Create(property);
            Assert.Equal(propertyState, nullability.ReadState);
            Assert.NotEmpty(nullability.GenericTypeArguments);
            Assert.Equal(keyState, nullability.GenericTypeArguments[0].ReadState);
            Assert.Equal(valueState, nullability.GenericTypeArguments[1].ReadState);
            Assert.Equal(valueElement, nullability.GenericTypeArguments[1].ElementType.ReadState);
            Assert.Null(nullability.ElementType);
        }

        public static IEnumerable<object[]> GenericDictionaryPropertyTestData()
        {
            yield return new object[] { "PropertyDictionaryUnknown", NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown };
            yield return new object[] { "PropertyDictionaryNullNullNullNon", NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.NotNull }; // IDictionary<T?, string?[]?> PropertyDictionaryNullNullNullNon { get; set; }
            yield return new object[] { "PropertyDictionaryNonNullNonNull", NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.Nullable }; // IDictionary<Type, T?[]>? PropertyDictionaryNonNullNonNull
            yield return new object[] { "PropertyDictionaryNullNonNonNull", NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.Nullable }; // IDictionary<T?, T[]>? PropertyDictionaryNullNonNonNull
            yield return new object[] { "PropertyDictionaryNonNullNonNon", NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.NotNull }; // IDictionary<Type, T?[]> PropertyDictionaryNonNullNonNon
            yield return new object[] { "PropertyDictionaryNonNonNonNull", NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.Nullable }; // IDictionary<T, T[]>? PropertyDictionaryNonNonNonNull
            yield return new object[] { "PropertyDictionaryNonNonNonNon", NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.NotNull }; // IDictionary<T, string[]> PropertyDictionaryNonNonNonNon
        }

        [Theory]
        [SkipOnMono("Disabling NullablePublicOnly feature not work for Mono tests")]
        [MemberData(nameof(GenericDictionaryPropertyTestData))]
        public void GenericDictionaryPropertyTest(string propertyName, NullabilityState keyState, NullabilityState valueElement, NullabilityState valueState, NullabilityState propertyState)
        {
            PropertyInfo property = genericType.GetProperty(propertyName, flags);
            NullabilityInfo nullability = nullabilityContext.Create(property);
            Assert.Equal(propertyState, nullability.ReadState);
            Assert.NotEmpty(nullability.GenericTypeArguments);
            Assert.Equal(keyState, nullability.GenericTypeArguments[0].ReadState);
            Assert.Equal(valueState, nullability.GenericTypeArguments[1].ReadState);
            Assert.Equal(valueElement, nullability.GenericTypeArguments[1].ElementType.ReadState);
            Assert.Null(nullability.ElementType);
        }

        public static IEnumerable<object[]> GenericPropertyReferenceTypeTestData()
        {
            yield return new object[] { "PropertyNullable", NullabilityState.Nullable, NullabilityState.Nullable, typeof(TypeWithNotNullContext) };
            yield return new object[] { "PropertyUnknown", NullabilityState.Unknown, NullabilityState.Unknown, typeof(TypeWithNotNullContext) };
            yield return new object[] { "PropertyNonNullable", NullabilityState.Nullable, NullabilityState.Nullable, typeof(TypeWithNotNullContext) };
            yield return new object[] { "PropertyDisallowNull", NullabilityState.Nullable, NullabilityState.NotNull, typeof(TypeWithNotNullContext) };
            yield return new object[] { "PropertyAllowNull", NullabilityState.Nullable, NullabilityState.Nullable, typeof(TypeWithNotNullContext) };
            yield return new object[] { "PropertyMaybeNull", NullabilityState.Nullable, NullabilityState.Nullable, typeof(TypeWithNotNullContext) };
        }

#nullable enable
        [Theory]
        [MemberData(nameof(GenericPropertyReferenceTypeTestData))]
        public void GenericPropertyReferenceTypeTest(string fieldName, NullabilityState readState, NullabilityState writeState, Type type)
        {
            PropertyInfo property = typeof(GenericTest<TypeWithNotNullContext?>).GetProperty(fieldName, flags)!;
            NullabilityInfo nullability = nullabilityContext.Create(property);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(writeState, nullability.WriteState);
            Assert.Equal(type, nullability.Type);
            Assert.Empty(nullability.GenericTypeArguments);
            Assert.Null(nullability.ElementType);

            property = typeof(GenericTest<TypeWithNotNullContext>).GetProperty(fieldName, flags)!;
            nullability = nullabilityContext.Create(property);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(writeState, nullability.WriteState);
            Assert.Equal(type, nullability.Type);

            property = typeof(GenericTest<>).GetProperty(fieldName, flags)!;
            nullability = nullabilityContext.Create(property);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(writeState, nullability.WriteState);
        }

        public static IEnumerable<object[]> GenericFieldReferenceTypeTestData()
        {
            yield return new object[] { "FieldNullable", NullabilityState.Nullable, NullabilityState.Nullable, typeof(TypeWithNotNullContext) };
            yield return new object[] { "FieldUnknown", NullabilityState.Unknown, NullabilityState.Unknown, typeof(TypeWithNotNullContext) };
            yield return new object[] { "FieldNonNullable", NullabilityState.Nullable, NullabilityState.Nullable, typeof(TypeWithNotNullContext) };
            yield return new object[] { "FieldDisallowNull", NullabilityState.Nullable, NullabilityState.NotNull, typeof(TypeWithNotNullContext) };
            yield return new object[] { "FieldAllowNull", NullabilityState.Nullable, NullabilityState.Nullable, typeof(TypeWithNotNullContext) };
            yield return new object[] { "FieldMaybeNull", NullabilityState.Nullable, NullabilityState.Nullable, typeof(TypeWithNotNullContext) };
        }

        [Theory]
        [MemberData(nameof(GenericFieldReferenceTypeTestData))]
        public void GenericFieldReferenceTypeTest(string fieldName, NullabilityState readState, NullabilityState writeState, Type type)
        {
            FieldInfo field = typeof(GenericTest<TypeWithNotNullContext?>).GetField(fieldName, flags)!;
            NullabilityInfo nullability = nullabilityContext.Create(field);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(writeState, nullability.WriteState);
            Assert.Equal(type, nullability.Type);
            Assert.Empty(nullability.GenericTypeArguments);
            Assert.Null(nullability.ElementType);

            field = typeof(GenericTest<TypeWithNotNullContext>).GetField(fieldName, flags)!;
            nullability = nullabilityContext.Create(field);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(writeState, nullability.WriteState);
            Assert.Equal(type, nullability.Type);

            field = typeof(GenericTest<>).GetField(fieldName, flags)!;
            nullability = nullabilityContext.Create(field);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(writeState, nullability.WriteState);
        }

        public static IEnumerable<object[]> GenericFieldValueTypeTestData()
        {
            yield return new object[] { "FieldNullable", typeof(int) };
            yield return new object[] { "FieldUnknown", typeof(int) };
            yield return new object[] { "FieldNonNullable", typeof(int) };
            yield return new object[] { "FieldDisallowNull", typeof(int) };
            yield return new object[] { "FieldAllowNull", typeof(int) };
            yield return new object[] { "FieldMaybeNull", typeof(int) };
            yield return new object[] { "FieldNotNull", typeof(int) };
        }

        [Theory]
        [MemberData(nameof(GenericFieldValueTypeTestData))]
        public void GenericFieldValueTypeTest(string fieldName, Type type)
        {
            FieldInfo field = typeof(GenericTest<int>).GetField(fieldName, flags)!;
            NullabilityInfo nullability = nullabilityContext.Create(field);
            Assert.Equal(NullabilityState.NotNull, nullability.ReadState);
            Assert.Equal(NullabilityState.NotNull, nullability.WriteState);
            Assert.Equal(type, nullability.Type);
        }

        public static IEnumerable<object[]> GenericFieldNullableValueTypeTestData()
        {
            yield return new object[] { "FieldNullable", NullabilityState.Nullable, NullabilityState.Nullable, typeof(int?) };
            yield return new object[] { "FieldUnknown", NullabilityState.Nullable, NullabilityState.Nullable, typeof(int?) };
            yield return new object[] { "FieldNonNullable", NullabilityState.Nullable, NullabilityState.Nullable, typeof(int?) };
            yield return new object[] { "FieldDisallowNull", NullabilityState.Nullable, NullabilityState.NotNull, typeof(int?) };
            yield return new object[] { "FieldAllowNull", NullabilityState.Nullable, NullabilityState.Nullable, typeof(int?) };
            yield return new object[] { "FieldMaybeNull", NullabilityState.Nullable, NullabilityState.Nullable, typeof(int?) };
            yield return new object[] { "FieldNotNull", NullabilityState.NotNull, NullabilityState.Nullable, typeof(int?) };
        }

        [Theory]
        [MemberData(nameof(GenericFieldNullableValueTypeTestData))]
        public void GenericFieldNullableValueTypeTest(string fieldName, NullabilityState readState, NullabilityState writeState, Type type)
        {
            FieldInfo field = typeof(GenericTest<int?>).GetField(fieldName, flags)!;
            NullabilityInfo nullability = nullabilityContext.Create(field);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(writeState, nullability.WriteState);
            Assert.Equal(type, nullability.Type);
        }

        public static IEnumerable<object[]> GenericNotnullConstraintTestData()
        {
            yield return new object[] { "FieldNullable", NullabilityState.NotNull, NullabilityState.NotNull, typeof(string) };
            yield return new object[] { "FieldUnknown", NullabilityState.Unknown, NullabilityState.Unknown, typeof(string) };
            yield return new object[] { "FieldNullableEnabled", NullabilityState.NotNull, NullabilityState.NotNull, typeof(string) };
        }

        [Theory]
        [MemberData(nameof(GenericNotnullConstraintTestData))]
        public void GenericNotNullConstraintTest(string fieldName, NullabilityState readState, NullabilityState writeState, Type type)
        {
            FieldInfo field = typeof(GenericTestConstrainedNotNull<string>).GetField(fieldName, flags)!;
            NullabilityInfo nullability = nullabilityContext.Create(field);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(writeState, nullability.WriteState);
            Assert.Equal(type, nullability.Type);
        }

        public static IEnumerable<object[]> GenericStructConstraintTestData()
        {
            yield return new object[] { "FieldNullable", NullabilityState.Nullable, NullabilityState.Nullable, typeof(int?) };
            yield return new object[] { "FieldUnknown", NullabilityState.NotNull, NullabilityState.NotNull, typeof(int) };
            yield return new object[] { "FieldNullableEnabled", NullabilityState.NotNull, NullabilityState.NotNull, typeof(int) };
        }

        [Theory]
        [MemberData(nameof(GenericStructConstraintTestData))]
        public void GenericStructConstraintTest(string fieldName, NullabilityState readState, NullabilityState writeState, Type type)
        {
            FieldInfo field = typeof(GenericTestConstrainedStruct<int>).GetField(fieldName, flags)!;
            NullabilityInfo nullability = nullabilityContext.Create(field);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(writeState, nullability.WriteState);
            Assert.Equal(type, nullability.Type);
        }

        [Fact]
        [SkipOnMono("Nullability attributes trimmed on Mono")]
        public void GenericListTest()
        {
            Type listNullable = typeof(List<string?>);
            MethodInfo addNullable = listNullable.GetMethod("Add")!;
            NullabilityInfo nullability = nullabilityContext.Create(addNullable.GetParameters()[0]);
            Assert.Equal(NullabilityState.Nullable, nullability.ReadState);
            Assert.Equal(NullabilityState.Nullable, nullability.WriteState);
            Assert.Equal(typeof(string), nullability.Type);

            Type lisNontNull = typeof(List<string>);
            MethodInfo addNotNull = lisNontNull.GetMethod("Add")!;
            nullability = nullabilityContext.Create(addNotNull.GetParameters()[0]);
            Assert.Equal(NullabilityState.Nullable, nullability.ReadState);
            Assert.Equal(typeof(string), nullability.Type);

            Type listOpen = typeof(List<>);
            MethodInfo addOpen = listOpen.GetMethod("Add")!;
            nullability = nullabilityContext.Create(addOpen.GetParameters()[0]);
            Assert.Equal(NullabilityState.Nullable, nullability.ReadState);
        }

        [Fact]
        [SkipOnMono("Nullability attributes trimmed on Mono")]
        public void GenericListAndDictionaryFieldTest()
        {
            Type typeNullable = typeof(GenericTest<string?>);
            FieldInfo listOfTNullable = typeNullable.GetField("FieldListOfT")!;
            NullabilityInfo listNullability = nullabilityContext.Create(listOfTNullable);
            Assert.Equal(NullabilityState.Nullable, listNullability.GenericTypeArguments[0].ReadState);
            Assert.Equal(typeof(string), listNullability.GenericTypeArguments[0].Type);

            FieldInfo dictStringToTNullable = typeNullable.GetField("FieldDictionaryStringToT")!;
            NullabilityInfo dictNullability = nullabilityContext.Create(dictStringToTNullable);
            Assert.Equal(NullabilityState.NotNull, dictNullability.GenericTypeArguments[0].ReadState);
            Assert.Equal(NullabilityState.Nullable, dictNullability.GenericTypeArguments[1].ReadState);
            Assert.Equal(typeof(string), dictNullability.GenericTypeArguments[1].Type);

            Type typeNonNull = typeof(GenericTest<string>);
            FieldInfo listOfTNotNull = typeNonNull.GetField("FieldListOfT")!;
            listNullability = nullabilityContext.Create(listOfTNotNull);
            Assert.Equal(NullabilityState.Nullable, listNullability.GenericTypeArguments[0].ReadState);
            Assert.Equal(typeof(string), listNullability.GenericTypeArguments[0].Type);

            FieldInfo dictStringToTNotNull = typeNonNull.GetField("FieldDictionaryStringToT")!;
            dictNullability = nullabilityContext.Create(dictStringToTNotNull);
            Assert.Equal(NullabilityState.NotNull, dictNullability.GenericTypeArguments[0].ReadState);
            Assert.Equal(NullabilityState.Nullable, dictNullability.GenericTypeArguments[1].ReadState);
            Assert.Equal(typeof(string), dictNullability.GenericTypeArguments[1].Type);

            Type typeOpen = typeof(GenericTest<>);
            FieldInfo listOfTOpen = typeOpen.GetField("FieldListOfT")!;
            listNullability = nullabilityContext.Create(listOfTOpen);
            Assert.Equal(NullabilityState.Nullable, listNullability.GenericTypeArguments[0].ReadState);
            // Assert.Equal(typeof(T), listNullability.TypeArguments[0].Type);

            FieldInfo dictStringToTOpen = typeOpen.GetField("FieldDictionaryStringToT")!;
            dictNullability = nullabilityContext.Create(dictStringToTOpen);
            Assert.Equal(NullabilityState.NotNull, dictNullability.GenericTypeArguments[0].ReadState);
            Assert.Equal(NullabilityState.Nullable, dictNullability.GenericTypeArguments[1].ReadState);
        }

        public static IEnumerable<object[]> MethodReturnParameterTestData()
        {
            yield return new object[] { "MethodReturnsUnknown", NullabilityState.Unknown, NullabilityState.Unknown};
            yield return new object[] { "MethodReturnsNullNon", NullabilityState.Nullable, NullabilityState.NotNull };
            yield return new object[] { "MethodReturnsNullNull", NullabilityState.Nullable, NullabilityState.Nullable };
            yield return new object[] { "MethodReturnsNonNull", NullabilityState.NotNull, NullabilityState.Nullable };
            yield return new object[] { "MethodReturnsNonNotNull", NullabilityState.NotNull, NullabilityState.NotNull };
            yield return new object[] { "MethodReturnsNonMaybeNull", NullabilityState.NotNull, NullabilityState.Nullable };
            yield return new object[] { "MethodReturnsNonNon", NullabilityState.NotNull, NullabilityState.NotNull };
        }

        [Theory]
        [SkipOnMono("Disabling NullablePublicOnly feature not work for Mono tests")]
        [MemberData(nameof(MethodReturnParameterTestData))]
        public void MethodReturnParameterTest(string methodName, NullabilityState elementState, NullabilityState readState)
        {
            MethodInfo method = testType.GetMethod(methodName, flags)!;
            NullabilityInfo nullability = nullabilityContext.Create(method.ReturnParameter);
            Assert.Equal(readState, nullability.ReadState);
            //Assert.Equal(readState, nullability.WriteState);
            Assert.NotNull(nullability.ElementType);
            Assert.Equal(elementState, nullability.ElementType!.ReadState);
            Assert.Empty(nullability.GenericTypeArguments);
        }

        public static IEnumerable<object[]> MethodGenericReturnParameterTestData()
        {
            yield return new object[] { "MethodReturnsUnknown", NullabilityState.Unknown, NullabilityState.Unknown };
            yield return new object[] { "MethodReturnsGeneric", NullabilityState.Nullable, NullabilityState.Unknown };
            yield return new object[] { "MethodReturnsNullGeneric", NullabilityState.Nullable, NullabilityState.Unknown };
            yield return new object[] { "MethodReturnsGenericNotNull", NullabilityState.NotNull, NullabilityState.Unknown };
            yield return new object[] { "MethodReturnsGenericMaybeNull", NullabilityState.Nullable, NullabilityState.Unknown };
            yield return new object[] { "MethodNonNullListNullGeneric", NullabilityState.NotNull, NullabilityState.Nullable };
            yield return new object[] { "MethodNullListNonNullGeneric", NullabilityState.Nullable, NullabilityState.Nullable };
        }

        [Theory]
        [SkipOnMono("Disabling NullablePublicOnly feature not work for Mono tests")]
        [MemberData(nameof(MethodGenericReturnParameterTestData))]
        public void MethodGenericReturnParameterTest(string methodName, NullabilityState readState, NullabilityState elementState)
        {
            MethodInfo method = typeof(GenericTest<TypeWithNotNullContext?>).GetMethod(methodName, flags)!;
            NullabilityInfo nullability = nullabilityContext.Create(method.ReturnParameter);
            Assert.Equal(readState, nullability.ReadState);
            if (nullability.GenericTypeArguments.Length > 0)
            {
                Assert.Equal(elementState, nullability.GenericTypeArguments[0].ReadState);
            }
        }

        public static IEnumerable<object[]> MethodParametersTestData()
        {
            yield return new object[] { "MethodParametersUnknown", NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown };
            yield return new object[] { "MethodNullNonNullNonNon", NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.NotNull };
            yield return new object[] { "MethodNonNullNonNullNotNull", NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull };
            yield return new object[] { "MethodNullNonNullNullNon", NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.NotNull };
            yield return new object[] { "MethodAllowNullNonNonNonNull", NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.Nullable };
        }

        [Theory]
        [SkipOnMono("Disabling NullablePublicOnly feature not work for Mono tests")]
        [MemberData(nameof(MethodParametersTestData))]
        public void MethodParametersTest(string methodName, NullabilityState stringState, NullabilityState dictKey, NullabilityState dictValueElement, NullabilityState dictValue, NullabilityState dictionaryState)
        {
            ParameterInfo[] parameters = testType.GetMethod(methodName, flags)!.GetParameters();
            NullabilityInfo stringNullability = nullabilityContext.Create(parameters[0]);
            NullabilityInfo dictionaryNullability = nullabilityContext.Create(parameters[1]);
            Assert.Equal(stringState, stringNullability.WriteState);
            Assert.Equal(dictionaryState, dictionaryNullability.ReadState);
            Assert.NotEmpty(dictionaryNullability.GenericTypeArguments);
            Assert.Equal(dictKey, dictionaryNullability.GenericTypeArguments[0].ReadState);
            Assert.Equal(dictValue, dictionaryNullability.GenericTypeArguments[1].ReadState);
            Assert.Equal(dictValueElement, dictionaryNullability.GenericTypeArguments[1].ElementType!.ReadState);
            Assert.Null(dictionaryNullability.ElementType);
        }

        public static IEnumerable<object[]> MethodGenericParametersTestData()
        {
            yield return new object[] { "MethodParametersUnknown", NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown};
            yield return new object[] { "MethodArgsNullGenericNullDictValueGeneric", NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.Nullable };
            yield return new object[] { "MethodArgsGenericDictValueNullGeneric", NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull };
        }

        [Theory]
        [SkipOnMono("Disabling NullablePublicOnly feature not work for Mono tests")]
        [MemberData(nameof(MethodGenericParametersTestData))]
        public void MethodGenericParametersTest(string methodName, NullabilityState param1State, NullabilityState dictKey, NullabilityState dictValue, NullabilityState dictionaryState)
        {
            ParameterInfo[] parameters = typeof(GenericTest<TypeWithNotNullContext>).GetMethod(methodName, flags)!.GetParameters();
            NullabilityInfo stringNullability = nullabilityContext.Create(parameters[0]);
            NullabilityInfo dictionaryNullability = nullabilityContext.Create(parameters[1]);
            Assert.Equal(param1State, stringNullability.WriteState);
            Assert.Equal(dictionaryState, dictionaryNullability.ReadState);
            Assert.NotEmpty(dictionaryNullability.GenericTypeArguments);
            Assert.Equal(dictKey, dictionaryNullability.GenericTypeArguments[0].ReadState);
            Assert.Equal(dictValue, dictionaryNullability.GenericTypeArguments[1].ReadState);
        }

        public static IEnumerable<object[]> StringTypeTestData()
        {
            yield return new object[] { "Format", NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.Nullable, new Type[] { typeof(string), typeof(object), typeof(object) } };
            yield return new object[] { "ReplaceCore", NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown, new Type[] { typeof(string), typeof(string), typeof(CompareInfo), typeof(CompareOptions) } };
            yield return new object[] { "Join", NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.NotNull, new Type[] { typeof(string), typeof(String?[]), typeof(int), typeof(int) } };
        }

        [Theory]
        [SkipOnMono("Nullability attributes trimmed on Mono")]
        [MemberData(nameof(StringTypeTestData))]
        public void NullablePublicOnlyStringTypeTest(string methodName, NullabilityState param1State, NullabilityState param2State, NullabilityState param3State, Type[] types)
        {
            ParameterInfo[] parameters = stringType.GetMethod(methodName, flags, types)!.GetParameters();
            NullabilityInfo param1 = nullabilityContext.Create(parameters[0]);
            NullabilityInfo param2 = nullabilityContext.Create(parameters[1]);
            NullabilityInfo param3 = nullabilityContext.Create(parameters[2]);
            Assert.Equal(param1State, param1.ReadState);
            Assert.Equal(param2State, param2.ReadState);
            Assert.Equal(param3State, param3.ReadState);
            if (param2.ElementType  != null)
            {
                Assert.Equal(NullabilityState.Nullable, param2.ElementType.ReadState);
            }
        }

        [Fact]
        [SkipOnMono("Nullability attributes trimmed on Mono")]
        public void NullablePublicOnlyOtherTypesTest()
        {
            Type type = typeof(Type);
            FieldInfo privateNullableField = type.GetField("s_defaultBinder", flags)!;
            NullabilityInfo info = nullabilityContext.Create(privateNullableField);
            Assert.Equal(NullabilityState.Unknown, info.ReadState);
            Assert.Equal(NullabilityState.Unknown, info.WriteState);

            MethodInfo internalNotNullableMethod = type.GetMethod("GetRootElementType", flags)!;
            info = nullabilityContext.Create(internalNotNullableMethod.ReturnParameter);
            Assert.Equal(NullabilityState.NotNull, info.ReadState);
            Assert.Equal(NullabilityState.NotNull, info.WriteState);

            PropertyInfo publicNullableProperty = type.GetProperty("DeclaringType", flags)!;
            info = nullabilityContext.Create(publicNullableProperty);
            Assert.Equal(NullabilityState.Nullable, info.ReadState);
            Assert.Equal(NullabilityState.Unknown, info.WriteState);

            PropertyInfo publicGetPrivateSetNullableProperty = typeof(FileSystemEntry).GetProperty("Directory", flags)!;
            info = nullabilityContext.Create(publicGetPrivateSetNullableProperty);
            Assert.Equal(NullabilityState.NotNull, info.ReadState);
            Assert.Equal(NullabilityState.Unknown, info.WriteState);

            MethodInfo protectedNullableReturnMethod = type.GetMethod("GetPropertyImpl", flags)!;
            info = nullabilityContext.Create(protectedNullableReturnMethod.ReturnParameter);
            Assert.Equal(NullabilityState.Nullable, info.ReadState);
            Assert.Equal(NullabilityState.Nullable, info.WriteState);

            MethodInfo privateValueTypeReturnMethod = type.GetMethod("BinarySearch", flags)!;
            info = nullabilityContext.Create(privateValueTypeReturnMethod.ReturnParameter);
            Assert.Equal(NullabilityState.Unknown, info.ReadState);
            Assert.Equal(NullabilityState.Unknown, info.WriteState);

            Type regexType = typeof(Regex);
            FieldInfo protectedInternalNullableField = regexType.GetField("pattern", flags)!;
            info = nullabilityContext.Create(protectedInternalNullableField);
            Assert.Equal(NullabilityState.Nullable, info.ReadState);
            Assert.Equal(NullabilityState.Nullable, info.WriteState);

            privateNullableField = regexType.GetField("_code", flags)!;
            info = nullabilityContext.Create(privateNullableField);
            Assert.Equal(NullabilityState.Unknown, info.ReadState);
            Assert.Equal(NullabilityState.Unknown, info.WriteState);
        }

        public static IEnumerable<object[]> DifferentContextTestData()
        {
            yield return new object[] { "PropertyDisabled", NullabilityState.Unknown, NullabilityState.Unknown, typeof(string) };
            yield return new object[] { "PropertyEnabledAllowNull", NullabilityState.NotNull, NullabilityState.Nullable, typeof(string) };
            yield return new object[] { "PropertyEnabledNotNull", NullabilityState.NotNull, NullabilityState.Nullable, typeof(string) };
            yield return new object[] { "PropertyEnabledMaybeNull", NullabilityState.Nullable, NullabilityState.NotNull, typeof(string) };
            yield return new object[] { "PropertyEnabledDisallowNull", NullabilityState.Nullable, NullabilityState.NotNull, typeof(string) };
            yield return new object[] { "PropertyEnabledNullable", NullabilityState.Nullable, NullabilityState.Nullable, typeof(string) };
            yield return new object[] { "PropertyEnabledNonNullable", NullabilityState.NotNull, NullabilityState.NotNull, typeof(string) };
        }
        [Theory]
        [MemberData(nameof(DifferentContextTestData))]
        public void NullabilityDifferentContextTest(string propertyName, NullabilityState readState, NullabilityState writeState, Type type)
        {
            Type noContext = typeof(TypeWithNoContext);
            PropertyInfo property = noContext.GetProperty(propertyName, flags)!;
            NullabilityInfo nullability = nullabilityContext.Create(property);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(writeState, nullability.WriteState);
            Assert.Equal(type, nullability.Type);

            Type nullableContext = typeof(TypeWithNullableContext);
            property = nullableContext.GetProperty(propertyName, flags)!;
            nullability = nullabilityContext.Create(property);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(writeState, nullability.WriteState);
            Assert.Equal(type, nullability.Type);
        }

        [Fact]
        public void AttributedParametersTest()
        {
            Type type = typeof(TypeWithNullableContext);

            // bool NotNullWhenParameter([DisallowNull] string? disallowNull, [NotNullWhen(true)] ref string? notNullWhen, Type? nullableType);
            ParameterInfo[] notNullWhenParameters = type.GetMethod("NotNullWhenParameter", flags)!.GetParameters();
            NullabilityInfo disallowNull = nullabilityContext.Create(notNullWhenParameters[0]);
            NullabilityInfo notNullWhen = nullabilityContext.Create(notNullWhenParameters[1]);
            Assert.Equal(NullabilityState.Nullable, disallowNull.ReadState);
            Assert.Equal(NullabilityState.NotNull, disallowNull.WriteState);
            Assert.Equal(NullabilityState.Nullable, notNullWhen.ReadState);
            Assert.Equal(NullabilityState.Nullable, notNullWhen.WriteState);
            Assert.Equal(NullabilityState.Nullable, nullabilityContext.Create(notNullWhenParameters[1]).ReadState);

            // bool MaybeNullParameters([MaybeNull] string maybeNull, [MaybeNullWhen(false)] out string maybeNullWhen, Type? nullableType)
            ParameterInfo[] maybeNullParameters = type.GetMethod("MaybeNullParameters", flags)!.GetParameters();
            NullabilityInfo maybeNull = nullabilityContext.Create(maybeNullParameters[0]);
            NullabilityInfo maybeNullWhen = nullabilityContext.Create(maybeNullParameters[1]);
            Assert.Equal(NullabilityState.Nullable, maybeNull.ReadState);
            Assert.Equal(NullabilityState.NotNull, maybeNull.WriteState);
            Assert.Equal(NullabilityState.Nullable, maybeNullWhen.ReadState);
            Assert.Equal(NullabilityState.NotNull, maybeNullWhen.WriteState);
            Assert.Equal(NullabilityState.Nullable, nullabilityContext.Create(maybeNullParameters[1]).ReadState);

            // string? AllowNullParameter([AllowNull] string allowNull, [NotNullIfNotNull("allowNull")] string? notNullIfNotNull)
            ParameterInfo[] allowNullParameter = type.GetMethod("AllowNullParameter", flags)!.GetParameters();
            NullabilityInfo allowNull = nullabilityContext.Create(allowNullParameter[0]);
            NullabilityInfo notNullIfNotNull = nullabilityContext.Create(allowNullParameter[1]);
            Assert.Equal(NullabilityState.NotNull, allowNull.ReadState);
            Assert.Equal(NullabilityState.Nullable, allowNull.WriteState);
            Assert.Equal(NullabilityState.Nullable, notNullIfNotNull.ReadState);
            Assert.Equal(NullabilityState.Nullable, notNullIfNotNull.WriteState);
            Assert.Equal(NullabilityState.Nullable, nullabilityContext.Create(allowNullParameter[1]).ReadState);

            // [return: NotNullIfNotNull("nullable")] public string? NullablNotNullIfNotNullReturn(string? nullable, [NotNull] ref string? readNotNull)
            ParameterInfo[] nullablNotNullIfNotNullReturn = type.GetMethod("NullablNotNullIfNotNullReturn", flags)!.GetParameters();
            NullabilityInfo returnNotNullIfNotNull = nullabilityContext.Create(type.GetMethod("NullablNotNullIfNotNullReturn", flags)!.ReturnParameter);
            NullabilityInfo readNotNull = nullabilityContext.Create(nullablNotNullIfNotNullReturn[1]);
            Assert.Equal(NullabilityState.Nullable, returnNotNullIfNotNull.ReadState);
            Assert.Equal(NullabilityState.Nullable, returnNotNullIfNotNull.WriteState);
            Assert.Equal(NullabilityState.NotNull, readNotNull.ReadState);
            Assert.Equal(NullabilityState.Nullable, readNotNull.WriteState);
            Assert.Equal(NullabilityState.Nullable, nullabilityContext.Create(nullablNotNullIfNotNullReturn[0]).ReadState);

            // public bool TryGetOutParameters(string id, [NotNullWhen(true)] out string? value, [MaybeNullWhen(false)] out string value2)
            ParameterInfo[] tryGetOutParameters = type.GetMethod("TryGetOutParameters", flags)!.GetParameters();
            NullabilityInfo notNullWhenParam = nullabilityContext.Create(tryGetOutParameters[1]);
            NullabilityInfo maybeNullWhenParam = nullabilityContext.Create(tryGetOutParameters[2]);
            Assert.Equal(NullabilityState.Nullable, notNullWhenParam.ReadState);
            Assert.Equal(NullabilityState.Nullable, notNullWhenParam.WriteState);
            Assert.Equal(NullabilityState.Nullable, maybeNullWhenParam.ReadState);
            Assert.Equal(NullabilityState.NotNull, maybeNullWhenParam.WriteState);
            Assert.Equal(NullabilityState.NotNull, nullabilityContext.Create(tryGetOutParameters[0]).ReadState);
        }

        public static IEnumerable<object[]> RefReturnData()
        {
            yield return new object[] { "RefReturnUnknown", NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown, NullabilityState.Unknown };
            // [return: MaybeNull] public ref string RefReturnMaybeNull([DisallowNull] ref string? id) 
            yield return new object[] { "RefReturnMaybeNull", NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull };
            // public ref string RefReturnNotNullable([MaybeNull] ref string id)
            yield return new object[] { "RefReturnNotNullable", NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull };
            // [return: NotNull]public ref string? RefReturnNotNull([NotNull] ref string? id)
            yield return new object[] { "RefReturnNotNull", NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.Nullable };
            // publiic ref string? RefReturnNullable([AllowNull] ref string id)
            yield return new object[] { "RefReturnNullable", NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.NotNull, NullabilityState.Nullable };
        }

        [Theory]
        [MemberData(nameof(RefReturnData))]
        public void RefReturnTestTest(string methodName, NullabilityState retReadState, NullabilityState retWriteState, NullabilityState paramReadState, NullabilityState paramWriteState)
        {
            MethodInfo method = typeof(TypeWithNullableContext).GetMethod(methodName, flags)!;
            NullabilityInfo returnNullability = nullabilityContext.Create(method.ReturnParameter);
            NullabilityInfo paramNullability = nullabilityContext.Create(method.GetParameters()[0]);
            Assert.Equal(retReadState, returnNullability.ReadState);
            Assert.Equal(retWriteState, returnNullability.WriteState);
            Assert.Equal(paramReadState, paramNullability.ReadState);
            Assert.Equal(paramWriteState, paramNullability.WriteState);
        }
    }

#pragma warning disable CS0649, CS0067, CS0414
    public class TypeWithNullableContext
    {
#nullable disable
        public string PropertyDisabled { get; set; }
        public ref string RefReturnUnknown(ref string id) { return ref id; }
#nullable enable
        [AllowNull] public string PropertyEnabledAllowNull { get; set; }
        [NotNull] public string? PropertyEnabledNotNull { get; set; } = null!;
        [DisallowNull] public string? PropertyEnabledDisallowNull { get; set; } = null!;
        [MaybeNull] public string PropertyEnabledMaybeNull { get; set; }
        public string? PropertyEnabledNullable { get; set; }
        public string PropertyEnabledNonNullable { get; set; } = null!;
        bool NotNullWhenParameter([DisallowNull] string? disallowNull, [NotNullWhen(true)] ref string? notNullWhen, Type? nullableType) { return false; }
        public bool MaybeNullParameters([MaybeNull] string maybeNull, [MaybeNullWhen(false)] out string maybeNullWhen, Type? nullableType) { maybeNullWhen = null; return false; }
        public string? AllowNullParameter([AllowNull] string allowNull, [NotNullIfNotNull("allowNull")] string? notNullIfNotNull) { return null; }
        [return: NotNullIfNotNull("nullable")] public string? NullablNotNullIfNotNullReturn(string? nullable, [NotNull] ref string? readNotNull) { readNotNull = string.Empty; return null!; }
        public ref string? RefReturnNullable([AllowNull] ref string id) { return ref id!; }
        [return: MaybeNull] public ref string RefReturnMaybeNull([DisallowNull] ref string? id) { return ref id; }
        [return: NotNull] public ref string? RefReturnNotNull([NotNull] ref string? id) { id = string.Empty; return ref id!; }
        public ref string RefReturnNotNullable([MaybeNull] ref string id) { return ref id; }
        public bool TryGetOutParameters(string id, [NotNullWhen(true)] out string? value, [MaybeNullWhen(false)] out string value2) { value = null; value2 = null; return false; }
    }

    public class TypeWithNoContext
    {
#nullable disable
        [AllowNull] public string PropertyDisabledAllowNull { get; set; }
        [MaybeNull] public string PropertyDisabledMaybeNull { get; set; }
        public string PropertyDisabled { get; set; }
#nullable enable
        [AllowNull] public string PropertyEnabledAllowNull { get; set; }
        [NotNull] public string? PropertyEnabledNotNull { get; set; } = null!;
        [DisallowNull] public string? PropertyEnabledDisallowNull { get; set; } = null!;
        [MaybeNull] public string PropertyEnabledMaybeNull { get; set; } 
        public string? PropertyEnabledNullable { get; set; }
        public string PropertyEnabledNonNullable { get; set; } = null!;
#nullable disable
        [return: NotNull, MaybeNull]
        public string MethodNullableDisabled([AllowNull] string value, string ret) { return null; }
    }

    public class TypeWithNotNullContext
    {
        public string PropertyUnknown { get; set; }
        short PropertyValueTypeUnknown { get; set; }
        public string[] PropertyArrayUnknown { get; set; }
        private string[][] PropertyJaggedArrayUnknown { get; set; }
        protected Tuple<string, string, string> PropertyTupleUnknown { get; set; }
        protected internal IDictionary<Type, string[]> PropertyDictionaryUnknown { get; set; }

        internal TypeWithNotNullContext FieldUnknown;
        public int FieldValueTypeUnknown;

        public event EventHandler EventUnknown;
        public string[] MethodReturnsUnknown() => null!;
        public void MethodParametersUnknown(string s, IDictionary<Type, string[]> dict) { }
#nullable enable 
        public TypeWithNotNullContext? PropertyNullable { get; set; }
        public TypeWithNotNullContext? PropertyNullableReadOnly { get; }
        private NullabilityInfoContextTests PropertyNonNullable { get; set; } = null!;
        internal float PropertyValueType { get; set; }
        protected long? PropertyValueTypeNullable { get; set; }
        [DisallowNull] public int? PropertyValueTypeDisallowNull { get; set; }
        [NotNull] protected int? PropertyValueTypeNotNull { get; set; }
        [MaybeNull] public byte PropertyValueTypeMaybeNull { get; set; }
        [AllowNull] public byte PropertyValueTypeAllowNull { get; set; }
        [DisallowNull] public string? PropertyDisallowNull { get; set; }
        [AllowNull] public string PropertyAllowNull { get; set; }
        [NotNull] public string? PropertyNotNull { get; set; }
        [MaybeNull] public string PropertyMaybeNull { get; set; }
        // only AllowNull matter
        [AllowNull, DisallowNull] public string PropertyAllowNull2 { get; set; }
        // only DisallowNull matter
        [AllowNull, DisallowNull] public string? PropertyDisallowNull2 { get; set; }
        // only NotNull matter
        [NotNull, MaybeNull] public string? PropertyNotNull2 { get; set; }
        // only MaybeNull matter
        [NotNull, MaybeNull] public string PropertyMaybeNull2 { get; set; }
        private protected string?[]?[]? PropertyJaggedArrayNullNullNull { get; set; }
        public static string?[]?[] PropertyJaggedArrayNullNullNon { get; set; } = null!;
        public string?[][]? PropertyJaggedArrayNullNonNull { get; set; }
        public static string[]?[]? PropertyJaggedArrayNonNullNull { get; set; }
        public string?[][] PropertyJaggedArrayNullNonNon { get; set; } = null!;
        private static string[][]? PropertyJaggedArrayNonNonNull { get; set; }
        public string?[]? PropertyArrayNullNull { get; set; }
        static string?[] PropertyArrayNullNon { get; set; } = null!;
        public string[]? PropertyArrayNonNull { get; } = null;
        public string[] PropertyArrayNonNon { get; set; } = null!;
        public Tuple<string?, string?, string?>? PropertyTupleNullNullNullNull { get; set; }
        public Tuple<string, string?, string> PropertyTupleNonNullNonNon { get; set; } = null!;
        internal Tuple<string?, string, string?>? PropertyTupleNullNonNullNull { get; set; }
        public Tuple<string, string?, string>? PropertyTupleNonNullNonNull { get; set; }
        protected Tuple<string, string, string> PropertyTupleNonNonNonNon { get; set; } = null!;
        public IDictionary<Type?, string?[]?> PropertyDictionaryNullNullNullNon { get; set; } = null!;
        public IDictionary<Type, string?[]>? PropertyDictionaryNonNullNonNull { get; set; }
        IDictionary<Type?, string[]>? PropertyDictionaryNullNonNonNull { get; set; }
        public IDictionary<Type, string?[]> PropertyDictionaryNonNullNonNon { get; set; } = null!;
        private IDictionary<Type, string[]>? PropertyDictionaryNonNonNonNull { get; set; }
        public IDictionary<Type, string[]> PropertyDictionaryNonNonNonNon { get; set; } = null!;

        private const string? FieldNullable = null;
        protected static NullabilityInfoContextTests FieldNonNullable = null!;
        public static double FieldValueTypeNotNull;
        public readonly int? FieldValueTypeNullable;
        [DisallowNull] public string? FieldDisallowNull;
        [AllowNull] public string FieldAllowNull;
        [NotNull] string? FieldNotNull = null;
        [MaybeNull] public string FieldMaybeNull;
        [AllowNull, DisallowNull] public string FieldAllowNull2;
        [AllowNull, DisallowNull] public string? FieldDisallowNull2;
        [NotNull, MaybeNull] internal string? FieldNotNull2;
        [NotNull, MaybeNull] public string FieldMaybeNull2;

        public event EventHandler? EventNullable;
        public event EventHandler EventNotNull = null!;
        public string?[] MethodReturnsNullNon() => null!;
        public string?[]? MethodReturnsNullNull() => null;
        public string[]? MethodReturnsNonNull() => null;
        [return: NotNull, MaybeNull] public string[]? MethodReturnsNonNotNull() => null!; // only NotNull is applicable
        [return: MaybeNull] public string[] MethodReturnsNonMaybeNull() => null;
        public string[] MethodReturnsNonNon() => null!;
        public Tuple<string?, string>? MethodTupleNullNonNull() => null;
        public IEnumerable<Tuple<(string name, object? value), object>?> MethodEnumerableNonNonNullUnknownNullNonNullNon() => null!;
        public void MethodNullNonNullNonNon(string? s, IDictionary<Type, string?[]> dict) { }
        public void MethodNonNullNonNullNotNull(string s, [NotNull] IDictionary<Type, string[]?>? dict) { dict = new Dictionary<Type, string[]?>(); }
        public void MethodNullNonNullNullNon(string? s, IDictionary<Type, string?[]?> dict) { }
        public void MethodAllowNullNonNonNonNull([AllowNull] string s, IDictionary<Type, string[]>? dict) { }
    }

    internal class GenericTest<T>
    {
#nullable disable
        public T PropertyUnknown { get; set; }
        protected T[] PropertyArrayUnknown { get; set; }
        public Tuple<T, string, TypeWithNotNullContext> PropertyTupleUnknown { get; set; }
        private IDictionary<Type, T[]> PropertyDictionaryUnknown { get; set; }
        public T FieldUnknown;
        public T MethodReturnsUnknown() => default!;
        public void MethodParametersUnknown(T s, IDictionary<Type, T> dict) { }
#nullable enable

        public T PropertyNonNullable { get; set; } = default!;
        public T? PropertyNullable { get; set; }
        [DisallowNull] public T PropertyDisallowNull { get; set; } = default!;
        [NotNull] public T PropertyNotNull { get; set; } = default!;
        [MaybeNull] public T PropertyMaybeNull { get; set; }
        [AllowNull] public T PropertyAllowNull { get; set; }
        internal T?[]? PropertyArrayNullNull { get; set; }
        public T?[] PropertyArrayNullNon { get; set; } = null!;
        T[]? PropertyArrayNonNull { get; set; }
        public T[] PropertyArrayNonNon { get; set; } = null!;
        public Tuple<T?, string?, string?>? PropertyTupleNullNullNullNull { get; set; }
        public Tuple<T, T?, string> PropertyTupleNonNullNonNon { get; set; } = null!;
        Tuple<string?, T, T?>? PropertyTupleNullNonNullNull { get; set; }
        public Tuple<T, string?, string>? PropertyTupleNonNullNonNull { get; set; }
        public Tuple<string, string, T> PropertyTupleNonNonNonNon { get; set; } = null!;
        private IDictionary<T?, string?[]?> PropertyDictionaryNullNullNullNon { get; set; } = null!;
        static IDictionary<Type, T?[]>? PropertyDictionaryNonNullNonNull { get; set; }
        public static IDictionary<T?, T[]>? PropertyDictionaryNullNonNonNull { get; set; }
        public IDictionary<Type, T?[]> PropertyDictionaryNonNullNonNon { get; set; } = null!;
        protected IDictionary<T, T[]>? PropertyDictionaryNonNonNonNull { get; set; }
        public IDictionary<T, string[]> PropertyDictionaryNonNonNonNon { get; set; } = null!;

        static T? FieldNullable = default;
        public T FieldNonNullable = default!;
        [DisallowNull] public T? FieldDisallowNull;
        [AllowNull] protected T FieldAllowNull;
        [NotNull] public T? FieldNotNull = default;
        [MaybeNull] protected internal  T FieldMaybeNull = default!;
        public List<T> FieldListOfT = default!;
        public Dictionary<string, T> FieldDictionaryStringToT = default!;

        public T MethodReturnsGeneric() => default!;
        public T? MethodReturnsNullGeneric() => default;
        [return: NotNull] public T MethodReturnsGenericNotNull() => default!;
        [return: MaybeNull] public T MethodReturnsGenericMaybeNull() => default;
        public List<T?> MethodNonNullListNullGeneric() => null!;
        public List<T>? MethodNullListNonNullGeneric() => null;
        public void MethodArgsNullGenericNullDictValueGeneric(T? s, IDictionary<Type, T>? dict) { }
        public void MethodArgsGenericDictValueNullGeneric(T s, IDictionary<string, T?> dict) { }
    }

    internal class GenericTestConstrainedNotNull<T> where T : notnull
    {
#nullable disable
        public T FieldUnknown;
        public T PropertyUnknown { get; set; }
#nullable enable

        public T FieldNullableEnabled = default!;
        public T? FieldNullable;
        public T PropertyNullableEnabled { get; set; } = default!;
    }

    internal class GenericTestConstrainedStruct<T> where T : struct
    {
#nullable disable
        public T FieldUnknown;
        public T PropertyUnknown { get; set; }
#nullable enable

        public T FieldNullableEnabled;
        public T? FieldNullable;
        public T PropertyNullableEnabled { get; set; }
    }
}
