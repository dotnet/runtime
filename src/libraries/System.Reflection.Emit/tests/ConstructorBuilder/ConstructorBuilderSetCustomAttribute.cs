// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ConstructorBuilderSetCustomAttribute
    {
        [Fact]
        public void SetCustomAttribute_ConstructorBuilder_ByteArray()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(int) });

            ConstructorInfo attributeConstructor = typeof(IntAllAttribute).GetConstructor(new Type[] { typeof(int) });
            constructor.SetCustomAttribute(attributeConstructor, new byte[] { 01, 00, 05, 00, 00, 00 });
        }

        [Fact]
        public void SetCustomAttribute_ConstructorBuilder_ByteArray_NullConstructorBuilder_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(int) });
            Assert.Throws<ArgumentNullException>("con", () => constructor.SetCustomAttribute(null, new byte[] { 01, 00, 05, 00, 00, 00 }));
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            ILGenerator ilGenerator = constructor.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_1);

            ConstructorInfo attributeConstructor = typeof(IntAllAttribute).GetConstructor(new Type[1] { typeof(int) });
            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(attributeConstructor, new object[1] { 2 });

            constructor.SetCustomAttribute(attributeBuilder);
            Type createdType = type.CreateTypeInfo().AsType();

            ConstructorInfo createdConstructor = createdType.GetConstructor(new Type[0]);
            Attribute[] customAttributes = createdConstructor.GetCustomAttributes(true).ToArray();

            Assert.Equal(1, customAttributes.Length);
            Assert.Equal(2, ((IntAllAttribute)customAttributes[0])._i);
        }

        [Fact]
        public void SetCustomAttribute_NullCustomAtributeBuilder_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            ILGenerator ilGenerator = constructor.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_1);

            Assert.Throws<ArgumentNullException>("customBuilder", () => constructor.SetCustomAttribute(null));
        }
    }
}
