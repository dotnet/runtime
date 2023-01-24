// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderCustomIntAttribute : Attribute
    {
        public MethodBuilderCustomIntAttribute(int intField)
        {
            _intField = intField;
        }

        public string Field12345;
        public int _intField;
    }

    public class MethodBuilderCustomAttribute : Attribute
    {
        public string TestString
        {
            get { return TestStringField; }
            set { TestStringField = value; }
        }

        public int TestInt32
        {
            get { return TestInt; }
            set { TestInt = value; }
        }

        public string GetOnlyString
        {
            get { return GetString; }
        }

        public int GetOnlyInt32
        {
            get { return GetInt; }
        }

        public string TestStringField;
        public int TestInt;
        public string GetString;
        public int GetInt;

        public MethodBuilderCustomAttribute() { }

        public MethodBuilderCustomAttribute(string getOnlyString, int getOnlyInt32)
        {
            GetString = getOnlyString;
            GetInt = getOnlyInt32;
        }

        public MethodBuilderCustomAttribute(string testString, int testInt32, string getOnlyString, int getOnlyInt32)
        {
            TestStringField = testString;
            TestInt = testInt32;
            GetString = getOnlyString;
            GetInt = getOnlyInt32;
        }
    }

    public class MethodBuilderSetCustomAttribute
    {
        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static);

            ILGenerator ilgen = method.GetILGenerator();
            ilgen.Emit(OpCodes.Ldc_I4, 100);
            ilgen.Emit(OpCodes.Ret);

            Type attributeType = typeof(MethodBuilderCustomIntAttribute);
            ConstructorInfo constructor = attributeType.GetConstructors()[0];
            FieldInfo field = attributeType.GetField("Field12345");

            CustomAttributeBuilder attribute = new CustomAttributeBuilder(constructor, new object[] { 4 }, new FieldInfo[] { field }, new object[] { "hello" });
            method.SetCustomAttribute(attribute);
            Type createdType = type.CreateType();

            object[] attributes = createdType.GetMethod("TestMethod").GetCustomAttributes(false).ToArray();
            Assert.Equal(1, attributes.Length);

            MethodBuilderCustomIntAttribute obj = (MethodBuilderCustomIntAttribute)attributes[0];
            Assert.Equal("hello", obj.Field12345);
            Assert.Equal(4, obj._intField);
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder_NullBuilder_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static);

            AssertExtensions.Throws<ArgumentNullException>("customBuilder", () => method.SetCustomAttribute(null));
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray()
        {
            byte[] binaryAttribute = Enumerable.Range(0, 128).Select(i => (byte)i).ToArray();

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);

            method.SetCustomAttribute(typeof(MethodBuilderCustomAttribute).GetConstructor(new Type[0]), binaryAttribute);
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_NullConstructor_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);

            AssertExtensions.Throws<ArgumentNullException>("con", () => method.SetCustomAttribute(null, new byte[0]));
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_NullBinaryAttribute_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder builder = type.DefineMethod("TestMethod", MethodAttributes.Public);

            AssertExtensions.Throws<ArgumentNullException>("binaryAttribute", () => builder.SetCustomAttribute(typeof(MethodBuilderCustomAttribute).GetConstructor(new Type[0]), null));
        }
    }
}
