// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ModuleBuilderDefineType
    {
        [Fact]
        public void DefineType_String()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type = module.DefineType("TestType");
            Type createdType = type.CreateTypeInfo().AsType();
            Assert.Equal("TestType", createdType.Name);
        }

        [Theory]
        [InlineData(TypeAttributes.NotPublic)]
        [InlineData(TypeAttributes.Interface | TypeAttributes.Abstract)]
        [InlineData(TypeAttributes.Class)]
        public void DefineType_String_TypeAttributes(TypeAttributes attributes)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type = module.DefineType("TestType", attributes);

            Type createdType = type.CreateTypeInfo().AsType();
            Assert.Equal("TestType", createdType.Name);
            Assert.Equal(attributes, createdType.GetTypeInfo().Attributes);
        }

        [Theory]
        [InlineData(TypeAttributes.NotPublic)]
        [InlineData(TypeAttributes.Class)]
        public void DefineType_String_TypeAttributes_Type(TypeAttributes attributes)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type = module.DefineType("TestType", attributes, typeof(ModuleBuilderDefineType));

            Type createdType = type.CreateTypeInfo().AsType();
            Assert.Equal("TestType", createdType.Name);
            Assert.Equal(attributes, createdType.GetTypeInfo().Attributes);
            Assert.Equal(typeof(ModuleBuilderDefineType), createdType.GetTypeInfo().BaseType);
        }

        [Fact]
        public void DefineType_String_TypeAttributes_Type_TypeCreatedInModule()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type1 = module.DefineType("TestType1");
            Type parent = type1.CreateTypeInfo().AsType();

            TypeBuilder type2 = module.DefineType("TestType2", TypeAttributes.NotPublic, parent);
            Type createdType = type2.CreateTypeInfo().AsType();
            Assert.Equal("TestType2", createdType.Name);
            Assert.Equal(TypeAttributes.NotPublic, createdType.GetTypeInfo().Attributes);
            Assert.Equal(parent, createdType.GetTypeInfo().BaseType);
        }

        [Fact]
        public void DefineType_NullName_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            Assert.Throws<ArgumentNullException>("fullname", () => module.DefineType(null));
            Assert.Throws<ArgumentNullException>("fullname", () => module.DefineType(null, TypeAttributes.NotPublic));
            Assert.Throws<ArgumentNullException>("fullname", () => module.DefineType(null, TypeAttributes.NotPublic, typeof(ModuleBuilderDefineType)));
        }

        [Fact]
        public void DefineType_TypeAlreadyExists_ThrowsArgumentException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            module.DefineType("TestType");
            Assert.Throws<ArgumentException>(null, () => module.DefineType("TestType"));
            Assert.Throws<ArgumentException>(null, () => module.DefineType("TestType", TypeAttributes.NotPublic));
            Assert.Throws<ArgumentException>(null, () => module.DefineType("TestType", TypeAttributes.NotPublic, typeof(ModuleBuilderDefineType)));
        }

        [Fact]
        public void DefineType_NonAbstractInterface_ThrowsInvalidOperationException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            Assert.Throws<InvalidOperationException>(() => module.DefineType("A", TypeAttributes.Interface));
        }
    }
}
