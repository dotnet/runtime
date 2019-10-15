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
            constructor.SetCustomAttribute(attributeConstructor, new byte[] { 1, 0, 5, 0, 0, 0 });
            constructor.GetILGenerator().Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo().AsType();
            ConstructorInfo createdConstructor = createdType.GetConstructor(new Type[] { typeof(int) });
            Attribute[] attributes = createdConstructor.GetCustomAttributes().ToArray();
            IntAllAttribute attribute = Assert.IsType<IntAllAttribute>(attributes[0]);
            Assert.Equal(5, attribute._i);
        }

        [Fact]
        public void SetCustomAttribute_ConstructorBuilder_ByteArray_NullConstructorBuilder_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            AssertExtensions.Throws<ArgumentNullException>("con", () => constructor.SetCustomAttribute(null, new byte[0]));
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            ILGenerator ilGenerator = constructor.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_1);

            ConstructorInfo attributeConstructor = typeof(IntAllAttribute).GetConstructor(new Type[] { typeof(int) });
            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(attributeConstructor, new object[] { 2 });

            constructor.SetCustomAttribute(attributeBuilder);
            Type createdType = type.CreateTypeInfo().AsType();

            ConstructorInfo createdConstructor = createdType.GetConstructor(new Type[0]);
            Attribute[] customAttributes = (Attribute[])CustomAttributeExtensions.GetCustomAttributes(createdConstructor, true).ToArray();

            Assert.Equal(1, customAttributes.Length);
            Assert.Equal(2, ((IntAllAttribute)customAttributes[0])._i);
        }

        [Fact]
        public void SetCustomAttribute_NullCustomAttributeBuilder_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            ILGenerator ilGenerator = constructor.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_1);

            AssertExtensions.Throws<ArgumentNullException>("customBuilder", () => constructor.SetCustomAttribute(null));
        }

        [Fact]
        public void GetCustomAttributes_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            Assert.Throws<NotSupportedException>(() => constructor.GetCustomAttributes());
        }

        [Fact]
        public void IsDefined_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            Assert.Throws<NotSupportedException>(() => constructor.IsDefined(typeof(IntAllAttribute)));
        }
    }
}
