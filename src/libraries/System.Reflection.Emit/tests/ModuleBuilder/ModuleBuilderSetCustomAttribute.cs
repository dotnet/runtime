// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ModuleBuilderSetCustomAttribute
    {
        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            ConstructorInfo attributeConstructor = typeof(IntAllAttribute).GetConstructor(new Type[] { typeof(int) });
            module.SetCustomAttribute(attributeConstructor, new byte[] { 01, 00, 05, 00, 00, 00 });

            object[] attributes = module.GetCustomAttributes().ToArray();
            Assert.Equal(1, attributes.Length);
            Assert.True(attributes[0] is IntAllAttribute);
            Assert.Equal(5, ((IntAllAttribute)attributes[0])._i);
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_NullConstructor_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentNullException>("con", () => module.SetCustomAttribute(null, new byte[0]));
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_NullBinaryAttribute_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            ConstructorInfo constructor = typeof(IntAllAttribute).GetConstructor(new Type[] { typeof(int) });
            AssertExtensions.Throws<ArgumentNullException>("binaryAttribute", () => module.SetCustomAttribute(constructor, null));
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            ConstructorInfo attributeConstructor = typeof(IntAllAttribute).GetConstructor(new Type[] { typeof(int) });
            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(attributeConstructor, new object[] { 5 });
            module.SetCustomAttribute(attributeBuilder);

            object[] attributes = module.GetCustomAttributes().ToArray();
            Assert.Equal(1, attributes.Length);
            Assert.True(attributes[0] is IntAllAttribute);
            Assert.Equal(5, ((IntAllAttribute)attributes[0])._i);
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder_NullBuilder_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentNullException>("customBuilder", () => module.SetCustomAttribute(null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void SetCustomAttribute_GetCustomAttributesData_NonPublicVisibility_DefinedInternally()
        {
            ModuleBuilder module = Helpers.DynamicModule();

            TypeBuilder internalAttributeType = module.DefineType("DynamicInternalAttribute", TypeAttributes.NotPublic, typeof(Attribute));
            ConstructorBuilder internalAttributeCtor = internalAttributeType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            internalAttributeCtor.GetILGenerator().Emit(OpCodes.Ret);

            ConstructorInfo internalAttributeCtorInfo = internalAttributeType.CreateTypeInfo().GetConstructor(Array.Empty<Type>());
            CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(internalAttributeCtorInfo, Array.Empty<object>());

            module.SetCustomAttribute(customAttribute);
            Assert.Single(module.GetCustomAttributesData());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void SetCustomAttribute_GetCustomAttributes_NonPublicVisibility_DefinedInternally()
        {
            ModuleBuilder module = Helpers.DynamicModule();

            TypeBuilder internalAttributeType = module.DefineType("DynamicInternalAttribute", TypeAttributes.NotPublic, typeof(Attribute));
            ConstructorBuilder internalAttributeCtor = internalAttributeType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            internalAttributeCtor.GetILGenerator().Emit(OpCodes.Ret);

            ConstructorInfo internalAttributeCtorInfo = internalAttributeType.CreateTypeInfo().GetConstructor(Array.Empty<Type>());
            CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(internalAttributeCtorInfo, Array.Empty<object>());

            module.SetCustomAttribute(customAttribute);
            Assert.Single(module.GetCustomAttributes(false));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void SetCustomAttribute_GetCustomAttributesData_NonPublicVisibility_DefinedExternally()
        {
            ModuleBuilder module1 = Helpers.DynamicModule();
            ModuleBuilder module2 = Helpers.DynamicModule("AnotherTestAssembly", "AnotherTestModule");

            TypeBuilder internalAttributeType = module1.DefineType("DynamicInternalAttribute", TypeAttributes.NotPublic, typeof(Attribute));
            ConstructorBuilder internalAttributeCtor = internalAttributeType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            internalAttributeCtor.GetILGenerator().Emit(OpCodes.Ret);

            ConstructorInfo internalAttributeCtorInfo = internalAttributeType.CreateTypeInfo().GetConstructor(Array.Empty<Type>());
            CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(internalAttributeCtorInfo, Array.Empty<object>());

            module2.SetCustomAttribute(customAttribute);
            Assert.Single(module2.GetCustomAttributesData());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void SetCustomAttribute_GetCustomAttributes_NonPublicVisibility_DefinedExternally()
        {
            ModuleBuilder module1 = Helpers.DynamicModule();
            ModuleBuilder module2 = Helpers.DynamicModule("AnotherTestAssembly", "AnotherTestModule");

            TypeBuilder internalAttributeType = module1.DefineType("DynamicInternalAttribute", TypeAttributes.NotPublic, typeof(Attribute));
            ConstructorBuilder internalAttributeCtor = internalAttributeType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            internalAttributeCtor.GetILGenerator().Emit(OpCodes.Ret);

            ConstructorInfo internalAttributeCtorInfo = internalAttributeType.CreateTypeInfo().GetConstructor(Array.Empty<Type>());
            CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(internalAttributeCtorInfo, Array.Empty<object>());

            module2.SetCustomAttribute(customAttribute);
            Assert.Empty(module2.GetCustomAttributes(false));
        }
    }
}
