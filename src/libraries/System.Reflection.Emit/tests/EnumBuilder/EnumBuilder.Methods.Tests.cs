// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class EnumBuilderMethodTests
    {
        public static IEnumerable<object[]> DefineLiteral_TestData()
        {
            foreach (object[] coreData in DefineLiteralTestDataCore())
            {
                yield return coreData.Concat(new object[] { true }).ToArray();
                yield return coreData.Concat(new object[] { false }).ToArray();
            }
        }

        private static IEnumerable<object[]> DefineLiteralTestDataCore()
        {
            yield return new object[] { typeof(byte), (byte)0 };
            yield return new object[] { typeof(byte), (byte)1 };

            yield return new object[] { typeof(sbyte), (sbyte)0 };
            yield return new object[] { typeof(sbyte), (sbyte)1 };

            yield return new object[] { typeof(ushort), (ushort)0 };
            yield return new object[] { typeof(ushort), (ushort)1 };

            yield return new object[] { typeof(short), (short)0 };
            yield return new object[] { typeof(short), (short)1 };

            yield return new object[] { typeof(uint), (uint)0 };
            yield return new object[] { typeof(uint), (uint)1 };

            yield return new object[] { typeof(int), 0 };
            yield return new object[] { typeof(int), 1 };

            yield return new object[] { typeof(ulong), (ulong)0 };
            yield return new object[] { typeof(ulong), (ulong)1 };

            yield return new object[] { typeof(long), (long)0 };
            yield return new object[] { typeof(long), (long)1 };

            yield return new object[] { typeof(char), (char)0 };
            yield return new object[] { typeof(char), (char)1 };

            yield return new object[] { typeof(bool), true };
            yield return new object[] { typeof(bool), false };

            yield return new object[] { typeof(float), 0f };
            yield return new object[] { typeof(float), 1.1f };

            yield return new object[] { typeof(double), 0d };
            yield return new object[] { typeof(double), 1.1d };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(DefineLiteral_TestData))]
        public void DefineLiteral(Type underlyingType, object literalValue, bool useCreateTypeInfo)
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, underlyingType);
            FieldBuilder literal = enumBuilder.DefineLiteral("FieldOne", literalValue);

            Assert.Equal("FieldOne", literal.Name);
            Assert.Equal(enumBuilder.Name, literal.DeclaringType.Name);
            Assert.Equal(FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal, literal.Attributes);
            Assert.Equal(enumBuilder.AsType(), literal.FieldType);

            Type createdEnum = useCreateTypeInfo ?
                enumBuilder.CreateType() :
                enumBuilder.CreateType();

            FieldInfo createdLiteral = createdEnum.GetField("FieldOne");
            Assert.Equal(createdEnum, createdLiteral.FieldType);

            if (literalValue is bool || literalValue is float || literalValue is double)
            {
                // EnumBuilder generates invalid data for non-integer enums
                Assert.Throws<FormatException>(() => createdLiteral.GetValue(null));
            }
            else
            {
                Assert.Equal(Enum.ToObject(createdEnum, literalValue), createdLiteral.GetValue(null));
            }
        }

        [Fact]
        public void DefineLiteral_NullLiteralName_ThrowsArgumentNullException()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            AssertExtensions.Throws<ArgumentNullException>("fieldName", () => enumBuilder.DefineLiteral(null, 1));
        }

        [Theory]
        [InlineData("")]
        [InlineData("\0")]
        [InlineData("\0abc")]
        public void DefineLiteral_EmptyLiteralName_ThrowsArgumentException(string literalName)
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            AssertExtensions.Throws<ArgumentException>("fieldName", () => enumBuilder.DefineLiteral(literalName, 1));
        }

        public static IEnumerable<object[]> DefineLiteral_InvalidLiteralValue_ThrowsArgumentException_TestData()
        {
            yield return new object[] { typeof(int), null };
            yield return new object[] { typeof(int), (short)1 };
            yield return new object[] { typeof(short), 1 };
            yield return new object[] { typeof(float), 1d };
            yield return new object[] { typeof(double), 1f };

            yield return new object[] { typeof(IntPtr), (IntPtr)1 };
            yield return new object[] { typeof(UIntPtr), (UIntPtr)1 };

            yield return new object[] { typeof(int).MakePointerType(), 1 };
            yield return new object[] { typeof(int).MakeByRefType(), 1 };
            yield return new object[] { typeof(int[]), new int[] { 1 } };
            yield return new object[] { typeof(int?), 1 };
            yield return new object[] { typeof(int?), null };
            yield return new object[] { typeof(string), null };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(DefineLiteral_InvalidLiteralValue_ThrowsArgumentException_TestData))]
        public void DefineLiteral_InvalidLiteralValue_ThrowsArgumentException(Type underlyingType, object literalValue)
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, underlyingType);
            AssertExtensions.Throws<ArgumentException>(null, () => enumBuilder.DefineLiteral("LiteralName", literalValue));
        }

        public static IEnumerable<object[]> DefineLiteral_InvalidLiteralValue_ThrowsTypeLoadExceptionOnCreation_TestData()
        {
            yield return new object[] { typeof(DateTime), DateTime.Now, true };
            yield return new object[] { typeof(DateTime), DateTime.Now, false };

            yield return new object[] { typeof(string), "", true };
            yield return new object[] { typeof(string), "", false };
        }

        [Theory]
        [MemberData(nameof(DefineLiteral_InvalidLiteralValue_ThrowsTypeLoadExceptionOnCreation_TestData))]
        public void DefineLiteral_InvalidLiteralValue_ThrowsTypeLoadExceptionOnCreation(Type underlyingType, object literalValue, bool useCreateTypeInfo)
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, underlyingType);
            FieldBuilder literal = enumBuilder.DefineLiteral("LiteralName", literalValue);
            if (useCreateTypeInfo)
            {
                Assert.Throws<TypeLoadException>(() => enumBuilder.CreateTypeInfo());
            }
            else
            {
                Assert.Throws<TypeLoadException>(() => enumBuilder.CreateType());
            }
        }

        [Fact]
        public void IsAssignableFrom()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            Assert.False(enumBuilder.IsAssignableFrom(null));
            Assert.True(enumBuilder.IsAssignableFrom(typeof(int).GetTypeInfo()));
            Assert.False(enumBuilder.IsAssignableFrom(typeof(short).GetTypeInfo()));
        }

        [Fact]
        public void GetElementType_ThrowsNotSupportedException()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            Assert.Throws<NotSupportedException>(() => enumBuilder.GetElementType());
        }

        [Fact]
        public void MakeArrayType()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int), enumName: "TestEnum");
            Type arrayType = enumBuilder.MakeArrayType();
            Assert.Equal(typeof(Array), arrayType.GetTypeInfo().BaseType);
            Assert.Equal("TestEnum[]", arrayType.Name);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(260)]
        public void MakeArrayType_Int(int rank)
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int), enumName: "TestEnum");
            Type arrayType = enumBuilder.MakeArrayType(rank);

            string ranks = rank == 1 ? "*" : string.Empty;
            for (int i = 1; i < rank; i++)
            {
                ranks += ",";
            }

            Assert.Equal(typeof(Array), arrayType.GetTypeInfo().BaseType);
            Assert.Equal($"TestEnum[{ranks}]", arrayType.Name);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void MakeArrayType_Int_RankLessThanOne_ThrowsIndexOutOfRange(int rank)
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int), enumName: "TestEnum");
            Assert.Throws<IndexOutOfRangeException>(() => enumBuilder.MakeArrayType(rank));
        }

        [Fact]
        public void MakeByRefType()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int), enumName: "TestEnum");
            Type arrayType = enumBuilder.MakeByRefType();
            Assert.Equal(typeof(Array), arrayType.GetTypeInfo().BaseType);
            Assert.Equal("TestEnum&", arrayType.Name);
        }

        [Fact]
        public void MakePointerType()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int), enumName: "TestEnum");
            Type arrayType = enumBuilder.MakePointerType();
            Assert.Equal(typeof(Array), arrayType.GetTypeInfo().BaseType);
            Assert.Equal("TestEnum*", arrayType.Name);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void SetCustomAttribute_ConstructorInfo_ByteArray()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            enumBuilder.CreateType();

            ConstructorInfo attributeConstructor = typeof(BoolAttribute).GetConstructor(new Type[] { typeof(bool) });
            enumBuilder.SetCustomAttribute(attributeConstructor, new byte[] { 01, 00, 01 });

            Attribute[] objVals = (Attribute[])CustomAttributeExtensions.GetCustomAttributes(enumBuilder, true).ToArray();
            Assert.Equal(new BoolAttribute(true), objVals[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void SetCustomAttribute_CustomAttributeBuilder()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            enumBuilder.CreateType();

            ConstructorInfo attributeConstructor = typeof(BoolAttribute).GetConstructor(new Type[] { typeof(bool) });
            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(attributeConstructor, new object[] { true });
            enumBuilder.SetCustomAttribute(attributeBuilder);

            object[] objVals = enumBuilder.GetCustomAttributes(true).ToArray();
            Assert.Equal(new BoolAttribute(true), objVals[0]);
        }

        public class BoolAttribute : Attribute
        {
            private bool _b;
            public BoolAttribute(bool myBool) { _b = myBool; }
        }
    }
}
