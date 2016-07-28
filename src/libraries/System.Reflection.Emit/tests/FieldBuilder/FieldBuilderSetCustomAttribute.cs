// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class FieldBuilderSetCustomAttribute
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField("TestField", typeof(object), FieldAttributes.Public);
            ConstructorInfo attributeConstructor = typeof(EmptyAttribute).GetConstructor(new Type[0]);
            byte[] bytes = new byte[256];
            s_randomDataGenerator.GetBytes(bytes);

            field.SetCustomAttribute(attributeConstructor, bytes);
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_NullConstructorInfo_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField("TestField", typeof(object), FieldAttributes.Public);
            Assert.Throws<ArgumentNullException>("con", () => field.SetCustomAttribute(null, new byte[256]));
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_NullByteArray_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField("TestField", typeof(object), FieldAttributes.Public);
            ConstructorInfo attributeConstructor = typeof(EmptyAttribute).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("binaryAttribute", () => field.SetCustomAttribute(attributeConstructor, null));
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField("TestField", typeof(object), FieldAttributes.Public);
            ConstructorInfo attributeConstructor = typeof(EmptyAttribute).GetConstructor(new Type[0]);
            byte[] bytes = new byte[256];
            s_randomDataGenerator.GetBytes(bytes);
            type.CreateTypeInfo().AsType();

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
            Assert.Throws<ArgumentNullException>("customBuilder", () => field.SetCustomAttribute(null));
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField("TestField", typeof(object), FieldAttributes.Public);
            ConstructorInfo con = typeof(EmptyAttribute).GetConstructor(new Type[0]);
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, new object[0]);
            type.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => { field.SetCustomAttribute(attribute); });
        }
    }
}
