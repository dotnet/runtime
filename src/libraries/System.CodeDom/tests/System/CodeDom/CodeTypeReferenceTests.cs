// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
    public class CodeTypeReferenceTests : CodeObjectTestBase<CodeTypeReference>
    {
        [Fact]
        public void Ctor_Default()
        {
            var type = new CodeTypeReference();
            Assert.Null(type.ArrayElementType);
            Assert.Equal(0, type.ArrayRank);
            Assert.Empty(type.BaseType);
            Assert.Equal((CodeTypeReferenceOptions)0, type.Options);
            Assert.Empty(type.TypeArguments);
        }

        public static IEnumerable<object[]> Ctor_Type_TestData()
        {
            yield return new object[] { typeof(int), typeof(int).FullName };
            yield return new object[] { typeof(int[]), typeof(int).FullName };
            yield return new object[] { typeof(int[,]), typeof(int).FullName };
            yield return new object[] { typeof(int).MakeByRefType(), typeof(int).MakeByRefType().FullName };
            yield return new object[] { typeof(int).MakePointerType(), typeof(int).MakePointerType().FullName };
            yield return new object[] { typeof(List<int>), typeof(List<>).FullName };
            yield return new object[] { typeof(List<>), typeof(List<>).FullName };
            yield return new object[] { typeof(List<>).GetGenericArguments()[0], "T" };
            yield return new object[] { typeof(void), typeof(void).FullName };
            yield return new object[] { typeof(NestedClass), typeof(NestedClass).FullName };
            yield return new object[] { typeof(ClassWithoutNamespace), typeof(ClassWithoutNamespace).FullName };
            yield return new object[] { typeof(Nullable), typeof(Nullable).FullName };
        }

        [Theory]
        [MemberData(nameof(Ctor_Type_TestData))]
        public void Ctor_Type(Type type, string expectedBaseType)
        {
            var typeReference = new CodeTypeReference(type);
            string expectedArrayElementType = type.IsArray ? type.GetElementType().FullName : null;
            int expectedArrayRank = type.IsArray ? type.GetArrayRank() : 0;
            string[] expectedTypeArguments = type.GenericTypeArguments.Select(arg => arg.IsGenericType ? arg.GetGenericTypeDefinition().FullName : arg.FullName).ToArray();
            VerifyCodeTypeReference(typeReference, expectedBaseType, expectedArrayElementType, expectedArrayRank, expectedTypeArguments);
        }

        [Theory]
        [InlineData(typeof(int), (CodeTypeReferenceOptions)(-1))]
        [InlineData(typeof(object), (CodeTypeReferenceOptions)0)]
        [InlineData(typeof(void), CodeTypeReferenceOptions.GenericTypeParameter)]
        [InlineData(typeof(string), CodeTypeReferenceOptions.GenericTypeParameter + 1)]
        public void Ctor_Type_CodeReferenceTypeOptions_TestData(Type type, CodeTypeReferenceOptions options)
        {
            var typeReference = new CodeTypeReference(type, options);
            Assert.Equal(type.FullName, typeReference.BaseType);
            Assert.Equal(options, typeReference.Options);
        }

        [Fact]
        public void Ctor_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => new CodeTypeReference((Type)null));
            AssertExtensions.Throws<ArgumentNullException>("type", () => new CodeTypeReference((Type)null, CodeTypeReferenceOptions.GenericTypeParameter));
        }

        public static IEnumerable<object[]> Ctor_String_TestData()
        {
            // Basic
            yield return new object[] { "System.Int32", typeof(int).FullName, null, 0, new string[0] };
            yield return new object[] { "System.Int32*", typeof(int).MakePointerType().FullName, null, 0, new string[0] };
            yield return new object[] { "System.Int32&", typeof(int).MakeByRefType().FullName, null, 0, new string[0] };
            yield return new object[] { "NoSuchType", "NoSuchType", null, 0, new string[0] };

            // Arrays
            yield return new object[] { "System.Int32[]", typeof(int).FullName, typeof(int).FullName, 1, new string[0] };
            yield return new object[] { "System.Int32[,]", typeof(int).FullName, typeof(int).FullName, 2, new string[0] };
            yield return new object[] { "System.Int32[][]", typeof(int).FullName, typeof(int).FullName, 1, new string[0] };

            // Generic
            yield return new object[] { "System.Collections.Generic.Dictionary`2[[System.String, mscorlib, Version = 2.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089], [System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", typeof(Dictionary<,>).FullName, null, 0, new string[] { "System.String", "System.Collections.Generic.List`1" } };
            yield return new object[] { "[System.Collections.Generic.List[[System.String, mscorlib, Version=2.0.0.0, Culture=neutral,PublicKeyToken=b77a5c561934e089]], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]", "System.Collections.Generic.List", null, 0, new string[] { "System.String" } };

            // Empty
            yield return new object[] { null, typeof(void).FullName, null, 0, new string[0] };
            yield return new object[] { "", typeof(void).FullName, null, 0, new string[0] };
            yield return new object[] { " \r \t \n ", " \r \t \n ", null, 0, new string[0] };

            // Invalid assembly qualified name
            yield return new object[] { "[]", "[]", null, 0, new string[0] };
            yield return new object[] { "[]]", "[]]", null, 0, new string[0] };
            yield return new object[] { "[[]", "[", "[", 1, new string[0] };
            yield return new object[] { "[[]]", "[[]]", null, 0, new string[0] };
            yield return new object[] { "]", "]", null, 0, new string[0] };
            yield return new object[] { "[", "[", null, 0, new string[0] };
            yield return new object[] { "[,,,,]", "", null, 0, new string[0] };
            yield return new object[] { "[a,b,c,d,e]", "a", null, 0, new string[0] };
            yield return new object[] { "[System.Int32,b,c,d,e]", typeof(int).FullName, null, 0, new string[0] };

            // Invalid generic
            yield return new object[] { "System.Collections.Generic.Dictionary`2", "System.Collections.Generic.Dictionary`2", null, 0, new string[0] };
            yield return new object[] { "System.Collections.Generic.Dictionary`2[System.String]", "System.Collections.Generic.Dictionary`2", null, 0, new string[] { "System.String" } };
            yield return new object[] { "System.Collections.Generic.Dictionary`2[System.String,", "System.Collections.Generic.Dictionary`2[System.String,", null, 0, new string[0] };
            yield return new object[] { "System.Collections.Generic.Dictionary`2[System.String,]", "System.Collections.Generic.Dictionary`2", null, 0, new string[] { "System.String" } };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_TestData))]
        public void Ctor_String(string type, string expectedBaseType, string expectedArrayElementType, int expectedArrayRank, string[] expectedTypeArguments)
        {
            var typeReference = new CodeTypeReference(type);
            VerifyCodeTypeReference(typeReference, expectedBaseType, expectedArrayElementType, expectedArrayRank, expectedTypeArguments);
        }

        [Theory]
        [InlineData("System.Int32", (CodeTypeReferenceOptions)(-1), "System.Int32")]
        [InlineData("System.Object", (CodeTypeReferenceOptions)0, "System.Object")]
        [InlineData("System.Void", CodeTypeReferenceOptions.GenericTypeParameter, "System.Void")]
        [InlineData(null, (CodeTypeReferenceOptions)0, "System.Void")]
        [InlineData("System.String", CodeTypeReferenceOptions.GenericTypeParameter + 1, "System.String")]
        public void Ctor_String_CodeReferenceTypeOptions(string type, CodeTypeReferenceOptions options, string expectedBaseType)
        {
            var typeReference = new CodeTypeReference(type, options);
            Assert.Equal(expectedBaseType, typeReference.BaseType);
            Assert.Equal(options, typeReference.Options);
        }

        public static IEnumerable<object[]> Ctor_CodeTypeReference_Int_TestData()
        {
            yield return new object[] { new CodeTypeReference(typeof(int)), -1, "" };
            yield return new object[] { new CodeTypeReference(typeof(void)), 0, "" };
            yield return new object[] { new CodeTypeReference(typeof(string)), 1, "System.String" };
            yield return new object[] { null, 1, "" };
        }

        [Theory]
        [MemberData(nameof(Ctor_CodeTypeReference_Int_TestData))]
        public void Ctor_CodeTypeReference_Int(CodeTypeReference type, int rank, string expectedBaseType)
        {
            var typeReference = new CodeTypeReference(type, rank);
            Assert.Equal(expectedBaseType, typeReference.BaseType);
            Assert.Equal(type?.BaseType, typeReference.ArrayElementType?.BaseType);
            Assert.Equal(rank, typeReference.ArrayRank);
        }

        [Theory]
        [InlineData("System.Int32", -1, "")]
        [InlineData("System.Void", 0, "")]
        [InlineData("System.String", 1, "System.String")]
        public void Ctor_String_Int(string type, int rank, string expectedBaseType)
        {
            var typeReference = new CodeTypeReference(type, rank);
            Assert.Equal(expectedBaseType, typeReference.BaseType);
            Assert.Equal(type, typeReference.ArrayElementType.BaseType);
            Assert.Equal(rank, typeReference.ArrayRank);
        }

        public static IEnumerable<object[]> Ctor_String_ParamsCodeTypeReference_TestData()
        {
            yield return new object[] { "System.String", null, "System.String" };
            yield return new object[] { "System.Void", new CodeTypeReference[0], "System.Void" };
            yield return new object[] { "System.Int32", new CodeTypeReference[] { new CodeTypeReference("System.String") }, "System.Int32`1" };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_ParamsCodeTypeReference_TestData))]
        public void Ctor_String_ParamsCodeTypeReference(string name, CodeTypeReference[] typeArguments, string expectedBaseType)
        {
            var typeReference = new CodeTypeReference(name, typeArguments);
            Assert.Equal(expectedBaseType, typeReference.BaseType);
            Assert.Equal(typeArguments ?? new CodeTypeReference[0], typeReference.TypeArguments.Cast<CodeTypeReference>());
        }

        [Fact]
        public void Ctor_NullObjectInTypeArguments_ThrowsArgumentNullException()
        {
            CodeTypeReference[] typeArguments = new CodeTypeReference[] { null };
            AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeTypeReference("System.Int32", typeArguments));
        }

        public static IEnumerable<object[]> Ctor_CodeTypeParameter_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new CodeTypeParameter() };
            yield return new object[] { new CodeTypeParameter("T") };
            yield return new object[] { new CodeTypeParameter("System.Int32") };
        }

        [Theory]
        [MemberData(nameof(Ctor_CodeTypeParameter_TestData))]
        public void Ctor_CodeTypeParameter(CodeTypeParameter parameter)
        {
            var typeReference = new CodeTypeReference(parameter);
            Assert.Equal(string.IsNullOrEmpty(parameter?.Name) ? "System.Void" : parameter.Name, typeReference.BaseType);
            Assert.Equal(CodeTypeReferenceOptions.GenericTypeParameter, typeReference.Options);
        }

        [Theory]
        [MemberData(nameof(CodeTypeReference_TestData))]
        public void ArrayElementType_Set_Get_ReturnsExpected(CodeTypeReference value)
        {
            var codeTypeReference = new CodeTypeReference();
            codeTypeReference.ArrayElementType = value;
            Assert.Equal(value, codeTypeReference.ArrayElementType);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void ArrayRank_Set_Get_ReturnsExpected(int value)
        {
            var codeTypeReference = new CodeTypeReference();
            codeTypeReference.ArrayRank = value;
            Assert.Equal(value, codeTypeReference.ArrayRank);
        }

        [Theory]
        [InlineData(null, "System.Void")]
        [InlineData("", "System.Void")]
        [InlineData("System.Int32", "System.Int32")]
        public void BaseType_Set_Get_ReturnsExpected(string value, string expected)
        {
            var codeTypeReference = new CodeTypeReference();
            codeTypeReference.BaseType = value;
            Assert.Equal(expected, codeTypeReference.BaseType);
        }

        [Fact]
        public void TypeArguments_AddMultiple_ReturnsExpected()
        {
            var codeTypeReference = new CodeTypeReference();

            CodeTypeReference type1 = new CodeTypeReference(typeof(int));
            codeTypeReference.TypeArguments.Add(type1);
            Assert.Equal(new CodeTypeReference[] { type1 }, codeTypeReference.TypeArguments.Cast<CodeTypeReference>());

            CodeTypeReference type2 = new CodeTypeReference(typeof(char));
            codeTypeReference.TypeArguments.Add(type2);
            Assert.Equal(new CodeTypeReference[] { type1, type2 }, codeTypeReference.TypeArguments.Cast<CodeTypeReference>());
        }

        private static void VerifyCodeTypeReference(CodeTypeReference typeReference, string expectedBaseType, string expectedArrayElementType, int expectedArrayRank, string[] expectedTypeArguments)
        {
            Assert.Equal(expectedBaseType, typeReference.BaseType);

            Assert.Equal(expectedArrayElementType, typeReference.ArrayElementType?.BaseType);

            Assert.Equal(expectedArrayRank, typeReference.ArrayRank);

            Assert.Equal((CodeTypeReferenceOptions)0, typeReference.Options);

            CodeTypeReference[] actualTypeArguments = typeReference.TypeArguments.Cast<CodeTypeReference>().ToArray();
            Assert.Equal(expectedTypeArguments.Length, actualTypeArguments.Length);
            for (int i = 0; i < expectedTypeArguments.Length; i++)
            {
                Assert.Equal(expectedTypeArguments[i], actualTypeArguments[i].BaseType);
                Assert.Equal((CodeTypeReferenceOptions)0, actualTypeArguments[i].Options);
            }
        }

        public class NestedClass { }
    }
}

public class ClassWithoutNamespace { }
