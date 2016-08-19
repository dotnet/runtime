// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class EnumBuilderMethodTests
    {
        [Fact]
        public void DefineLiteral()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            FieldBuilder field = enumBuilder.DefineLiteral("FieldOne", 1);
            enumBuilder.AsType();
            Assert.True(field.IsLiteral);
            Assert.True(field.IsPublic);
            Assert.True(field.IsStatic);
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
        public void SetCustomAttribute_ConstructorInfo_ByteArray()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            enumBuilder.CreateTypeInfo().AsType();

            ConstructorInfo attributeConstructor = typeof(BoolAttribute).GetConstructor(new Type[] { typeof(bool) });
            enumBuilder.SetCustomAttribute(attributeConstructor, new byte[] { 01, 00, 01 });

            Attribute[] objVals = enumBuilder.GetCustomAttributes(true).ToArray();
            Assert.Equal(1, objVals.Length);
            Assert.Equal(new BoolAttribute(true), objVals[0]);
        }


        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            enumBuilder.CreateTypeInfo().AsType();

            ConstructorInfo attributeConstructor = typeof(BoolAttribute).GetConstructor(new Type[] { typeof(bool) });
            CustomAttributeBuilder myCustomAttribute = new CustomAttributeBuilder(attributeConstructor, new object[] { true });
            enumBuilder.SetCustomAttribute(myCustomAttribute);

            object[] objVals = enumBuilder.GetCustomAttributes(true).ToArray();
            Assert.Equal(1, objVals.Length);
            Assert.True(objVals[0].Equals(new BoolAttribute(true)));
        }

        public class BoolAttribute : Attribute
        {
            private bool _b;
            public BoolAttribute(bool myBool) { _b = myBool; }
        }
    }
}
