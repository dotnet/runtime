// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace System.Reflection.Tests
{
    public class NullabilityInfoContextTests
    {
        private NullabilityInfoContext nullabilityContext = new NullabilityInfoContext();
        Type testType = typeof(Test);

        public static IEnumerable<object[]> FieldsTestData()
        {
            yield return new object[] { "FieldNullable", NullableState.Nullable, NullableState.Nullable, typeof(string) };
            yield return new object[] { "FieldUnknown", NullableState.Unknown, NullableState.Unknown, typeof(Test) };
            yield return new object[] { "FieldNonNullable", NullableState.NonNullable, NullableState.NonNullable, typeof(NullabilityInfoContextTests) };
            yield return new object[] { "FieldValueType1", NullableState.Unknown, NullableState.Unknown, typeof(int) };
            yield return new object[] { "FieldValueType2", NullableState.Unknown, NullableState.Unknown, typeof(double) };
            yield return new object[] { "FieldNullableValueType", NullableState.Nullable, NullableState.Nullable, typeof(int) };
            yield return new object[] { "FieldDisallowNull", NullableState.Nullable, NullableState.NonNullable, typeof(string) };
            yield return new object[] { "FieldAllowNull", NullableState.NonNullable, NullableState.Nullable, typeof(string) };
            yield return new object[] { "FieldDisallowNull2", NullableState.Nullable, NullableState.NonNullable, typeof(string) };
            yield return new object[] { "FieldAllowNull2", NullableState.NonNullable, NullableState.Nullable, typeof(string) };
        }

        [Theory]
        [MemberData(nameof(FieldsTestData))]
        public void FieldTest(string fieldName, NullableState readState, NullableState writeState, Type type)
        {
            var field = testType.GetField(fieldName);
            var nullability = nullabilityContext.Create(field);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(writeState, nullability.WriteState);
            Assert.Equal(type, nullability.Type);
            Assert.Null(nullability.TypeArguments);
            Assert.Null(nullability.ArrayElements);
        }

        public static IEnumerable<object[]> PropertiesTestData()
        {
            yield return new object[] { "PropertyNullable", NullableState.Nullable, NullableState.Nullable, typeof(Test) };
            yield return new object[] { "PropertyUnknown", NullableState.Unknown, NullableState.Unknown, typeof(string) };
            yield return new object[] { "PropertyNonNullable", NullableState.NonNullable, NullableState.NonNullable, typeof(NullabilityInfoContextTests) };
            yield return new object[] { "PropertyValueType1", NullableState.Unknown, NullableState.Unknown, typeof(short) };
            yield return new object[] { "PropertyValueType2", NullableState.Unknown, NullableState.Unknown, typeof(float) };
            yield return new object[] { "PropertyNullableValueType", NullableState.Nullable, NullableState.Nullable, typeof(long) };
            yield return new object[] { "PropertyDisallowNull", NullableState.Nullable, NullableState.NonNullable, typeof(string) };
            yield return new object[] { "PropertyAllowNull", NullableState.NonNullable, NullableState.Nullable, typeof(string) };
            yield return new object[] { "PropertyDisallowNull2", NullableState.Nullable, NullableState.NonNullable, typeof(string) };
            yield return new object[] { "PropertyAllowNull2", NullableState.NonNullable, NullableState.Nullable, typeof(string) };
        }

        [Theory]
        [MemberData(nameof(PropertiesTestData))]
        public void PropertyTest(string propertyName, NullableState readState, NullableState writeState, Type type)
        {
            var property = testType.GetProperty(propertyName);
            var nullability = nullabilityContext.Create(property);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(writeState, nullability.WriteState);
            Assert.Equal(type, nullability.Type);
            Assert.Null(nullability.TypeArguments);
            Assert.Null(nullability.ArrayElements);
        }

        public static IEnumerable<object[]> ArrayPropertiesTestData()
        {
            //yield return new object[] { "PropertyArrayUnknown", NullableState.Unknown, NullableState.Unknown };
            yield return new object[] { "PropertyArrayNullNull", NullableState.Unknown, NullableState.Unknown };
            /*yield return new object[] { "PropertyArrayNullNon", NullableState.NonNullable, NullableState.NonNullable };
            yield return new object[] { "PropertyDArrayNullNon", NullableState.Unknown, NullableState.Unknown };
            yield return new object[] { "PropertyDArrayNonNon", NullableState.Unknown, NullableState.Unknown };
            yield return new object[] { "PropertyJaggedArrayUnknown", NullableState.Nullable, NullableState.Nullable };
            yield return new object[] { "PropertyDisallowNull", NullableState.Nullable, NullableState.NonNullable };
            yield return new object[] { "PropertyAllowNull", NullableState.NonNullable, NullableState.Nullable };
            yield return new object[] { "PropertyDisallowNull2", NullableState.Nullable, NullableState.NonNullable };
            yield return new object[] { "PropertyAllowNull2", NullableState.NonNullable, NullableState.Nullable };*/
        }

        [Theory]
        [MemberData(nameof(ArrayPropertiesTestData))]
        public void ArrayOrGenericPropertyTest(string propertyName, NullableState readState, NullableState element)
        {
            var property = testType.GetProperty(propertyName);
            var nullability = nullabilityContext.Create(property);
            Assert.Equal(readState, nullability.ReadState);
            Assert.Equal(element, nullability.ArrayElements[0]);
            Assert.Null(nullability.TypeArguments);
            Assert.NotNull(nullability.ArrayElements);
        }
    }

    public class Test
    {
#nullable disable
        public string PropertyUnknown { get; set; }
        public short PropertyValueType1 { get; set; }
        public string[] PropertyArrayUnknown;
        public string[][] PropertyJaggedArrayUnknown { get; set; }
        public (string, string, string) PropertyTupleUnknown;

        public Test FieldUnknown;
        public int FieldValueType1;
#nullable enable
        public Test? PropertyNullable { get; set; }
        public NullabilityInfoContextTests PropertyNonNullable { get; set; } = null!;
        public float PropertyValueType2 { get; set; }
        public long? PropertyNullableValueType { get; set; }
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
        // only MabeNull matter
        [NotNull, MaybeNull] public string PropertyMaybeNull2 { get; set; }
        public string?[]?[]?[]? PropertyJaggedArray1 { get; set; }
        public string?[]?[] PropertyJaggedArray2 { get; set; } = null!;
        public string?[][]? PropertyJaggedArray3 { get; set; }
        public string[][]? PropertyJaggedArray4 { get; set; }
        public string?[]? PropertyArrayNullNull { get; set; }
        public string?[] PropertyDArrayNullNon { get; set; } = null!;
        public string[]? PropertyDArrayNonNull { get; set; }
        public string[] PropertyDArrayNonNon { get; set; } = null!;
        public (string?, string?, string?)? PropertyTuple1 { get; set; }
        public (string, string?, string) PropertyTuple2 { get; set; }
        public (string?, string, string?)? PropertyTuple3 { get; set; }
        public (string, string, string) PropertyTuple4 { get; set; }

        public string? FieldNullable = null;
        public NullabilityInfoContextTests FieldNonNullable = null!;
        public double FieldValueType2;
        public int? FieldNullableValueType;
        [DisallowNull] public string? FieldDisallowNull;
        [AllowNull] public string FieldAllowNull;
        [NotNull] public string? FieldNotNull = null;
        [MaybeNull] public string FieldMaybeNull;
        [AllowNull, DisallowNull] public string FieldAllowNull2;
        [AllowNull, DisallowNull] public string? FieldDisallowNull2;
        [NotNull, MaybeNull] public string? FieldNotNull2;
        [NotNull, MaybeNull] public string FieldMaybeNull2;

        public IEnumerable<string>? GenericMethodReturnNullable() => throw null!;
        public IEnumerable<string?> M2() => throw null!;
        public IEnumerable<string?>? M3() => throw null!;

        public IEnumerable<string>? M4() => throw null!;

        public IEnumerable<KeyValuePair<(string name, object? value), object?>>? M5() => throw null!;

        public string?[] M6() => throw null!;
        public string?[]? M7() => throw null!;
        public string[]? M8() => throw null!;

    }

    public class GenericTest<T>
    {
#nullable disable
        public T PropertyUnknown { get; set; }
        public T FieldUnknown;
#nullable enable
        public T? PropertyNullable { get; set; }
        public T PropertyNonNullable { get; set; } =  default!;
        [DisallowNull] public T? PropertyDisallowNull { get; set; }
        [AllowNull] public T PropertyAllowNull { get; set; }
        [AllowNull, DisallowNull] public T PropertyAllowNull2 { get; set; }
        [AllowNull, DisallowNull] public T? PropertyDisallowNull2 { get; set; }

        public T? FieldNullable = default;
        public T FieldNonNullable = default!;
        [DisallowNull] public T? FieldDisallowNull;
        [AllowNull] public T FieldAllowNull;
        [NotNull] public T? FieldNotNull = default;
        [MaybeNull] public T FieldMaybeNull;
        [AllowNull, DisallowNull] public T FieldAllowNull2;
        [AllowNull, DisallowNull] public T? FieldDisallowNull2;
        [NotNull, MaybeNull] public string? FieldNotNull2;
        [NotNull, MaybeNull] public string FieldMaybeNull2;
    }
}
