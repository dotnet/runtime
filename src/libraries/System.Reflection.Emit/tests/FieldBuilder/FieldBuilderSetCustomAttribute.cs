// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class FieldBuilderSetCustomAttribute
    {
        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField("TestField", typeof(object), FieldAttributes.Public);
            ConstructorInfo attributeConstructor = typeof(EmptyAttribute).GetConstructor(new Type[0]);
            byte[] bytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

            field.SetCustomAttribute(attributeConstructor, bytes);
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_NullConstructorInfo_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField("TestField", typeof(object), FieldAttributes.Public);
            AssertExtensions.Throws<ArgumentNullException>("con", () => field.SetCustomAttribute(null, new byte[256]));
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_NullByteArray_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField("TestField", typeof(object), FieldAttributes.Public);
            ConstructorInfo attributeConstructor = typeof(EmptyAttribute).GetConstructor(new Type[0]);
            AssertExtensions.Throws<ArgumentNullException>("binaryAttribute", () => field.SetCustomAttribute(attributeConstructor, null));
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField("TestField", typeof(object), FieldAttributes.Public);
            ConstructorInfo attributeConstructor = typeof(EmptyAttribute).GetConstructor(new Type[0]);
            byte[] bytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
            type.CreateType();

            Assert.Throws<InvalidOperationException>(() => field.SetCustomAttribute(attributeConstructor, bytes));
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField("TestField", typeof(object), FieldAttributes.Public);
            ConstructorInfo attributeConstructor = typeof(EmptyAttribute).GetConstructor(new Type[0]);
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(attributeConstructor, new object[0]);

            field.SetCustomAttribute(attribute);
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder_NullBuilder_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField("TestField", typeof(object), FieldAttributes.Public);
            AssertExtensions.Throws<ArgumentNullException>("customBuilder", () => field.SetCustomAttribute(null));
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField("TestField", typeof(object), FieldAttributes.Public);
            ConstructorInfo con = typeof(EmptyAttribute).GetConstructor(new Type[0]);
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, new object[0]);
            type.CreateType();

            Assert.Throws<InvalidOperationException>(() => { field.SetCustomAttribute(attribute); });
        }
    }
}
