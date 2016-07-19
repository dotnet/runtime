// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class EnumBuilderPropertyTests
    {
        [Fact]
        public void Assembly()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ModuleBuilder module = assembly.DefineDynamicModule("TestModule");
            EnumBuilder enumBuilder = module.DefineEnum("TestEnum", TypeAttributes.Public, typeof(int));
            Assert.Equal(assembly, enumBuilder.Assembly);
        }

        [Fact]
        public void AssemblyQualifiedName()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ModuleBuilder module = assembly.DefineDynamicModule("TestModule");
            EnumBuilder enumBuilder = module.DefineEnum("TestEnum", TypeAttributes.Public, typeof(int));
            Assert.Equal("TestEnum, " + assembly.FullName, enumBuilder.AssemblyQualifiedName);
        }

        [Fact]
        public void BaseType()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            Assert.Equal(typeof(Enum), enumBuilder.BaseType);
        }

        [Fact]
        public void DeclaringType()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            Assert.Null(enumBuilder.DeclaringType);
        }

        [Fact]
        public void FullName()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int), enumName: "TestEnum");
            enumBuilder.AsType();
            Assert.Equal("TestEnum", enumBuilder.FullName);
        }

        [Fact]
        public void Guid_TypeCreated()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            enumBuilder.CreateTypeInfo().AsType();
            Assert.NotEqual(Guid.Empty, enumBuilder.GUID);
        }

        [Fact]
        public void Guid_TypeNotCreated_ThrowsNotSupportedException()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            Assert.Throws<NotSupportedException>(() => enumBuilder.GUID);
        }

        [Fact]
        public void Module()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            EnumBuilder enumBuilder = module.DefineEnum("TestEnum", TypeAttributes.Public, typeof(int));
            Assert.Equal(module, enumBuilder.Module);
        }

        [Fact]
        public void Name()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int), enumName: "TestEnum");
            enumBuilder.AsType();
            Assert.Equal("TestEnum", enumBuilder.Name);
        }

        [Fact]
        public void Namespace()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            enumBuilder.AsType();
            Assert.Empty(enumBuilder.Namespace);
        }

        [Fact]
        public void UnderlyingField_TypeCreated()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            enumBuilder.AsType();
            Assert.Equal(typeof(int), enumBuilder.UnderlyingField.FieldType);
        }

        [Fact]
        public void UnderlyingField_TypeNotCreated()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            Assert.Equal(typeof(int), enumBuilder.UnderlyingField.FieldType);
        }
    }
}
