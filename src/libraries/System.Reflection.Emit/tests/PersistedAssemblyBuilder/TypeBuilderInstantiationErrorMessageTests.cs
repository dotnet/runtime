// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
    public class TypeBuilderInstantiationErrorMessageTests
    {
        [Fact]
        public void TypeBuilderInstantiation_GetConstructor_ThrowsWithHelpfulMessage()
        {
            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            GenericTypeParameterBuilder[] genericParams = type.DefineGenericParameters("T");
            
            ConstructorBuilder constructor = type.DefineConstructor(
                MethodAttributes.Public, 
                CallingConventions.Standard, 
                null);
            constructor.GetILGenerator().Emit(OpCodes.Ret);
            
            Type createdType = type.CreateType();
            Type instantiatedType = createdType.MakeGenericType(typeof(string));
            
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => instantiatedType.GetConstructor(Type.EmptyTypes));
            Assert.Contains("TypeBuilder.GetConstructor", ex.Message);
        }

        [Fact]
        public void TypeBuilderInstantiation_GetConstructors_ThrowsWithHelpfulMessage()
        {
            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            GenericTypeParameterBuilder[] genericParams = type.DefineGenericParameters("T");
            
            ConstructorBuilder constructor = type.DefineConstructor(
                MethodAttributes.Public, 
                CallingConventions.Standard, 
                null);
            constructor.GetILGenerator().Emit(OpCodes.Ret);
            
            Type createdType = type.CreateType();
            Type instantiatedType = createdType.MakeGenericType(typeof(string));
            
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => instantiatedType.GetConstructors());
            Assert.Contains("TypeBuilder.GetConstructor", ex.Message);
        }

        [Fact]
        public void TypeBuilderInstantiation_GetMethod_ThrowsWithHelpfulMessage()
        {
            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            GenericTypeParameterBuilder[] genericParams = type.DefineGenericParameters("T");
            
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public, typeof(void), null);
            method.GetILGenerator().Emit(OpCodes.Ret);
            
            Type createdType = type.CreateType();
            Type instantiatedType = createdType.MakeGenericType(typeof(string));
            
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => instantiatedType.GetMethod("TestMethod"));
            Assert.Contains("TypeBuilder.GetMethod", ex.Message);
        }

        [Fact]
        public void TypeBuilderInstantiation_GetMethods_ThrowsWithHelpfulMessage()
        {
            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            GenericTypeParameterBuilder[] genericParams = type.DefineGenericParameters("T");
            
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public, typeof(void), null);
            method.GetILGenerator().Emit(OpCodes.Ret);
            
            Type createdType = type.CreateType();
            Type instantiatedType = createdType.MakeGenericType(typeof(string));
            
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => instantiatedType.GetMethods());
            Assert.Contains("TypeBuilder.GetMethod", ex.Message);
        }

        [Fact]
        public void TypeBuilderInstantiation_GetField_ThrowsWithHelpfulMessage()
        {
            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            GenericTypeParameterBuilder[] genericParams = type.DefineGenericParameters("T");
            
            FieldBuilder field = type.DefineField("TestField", typeof(int), FieldAttributes.Public);
            
            Type createdType = type.CreateType();
            Type instantiatedType = createdType.MakeGenericType(typeof(string));
            
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => instantiatedType.GetField("TestField"));
            Assert.Contains("TypeBuilder.GetField", ex.Message);
        }

        [Fact]
        public void TypeBuilderInstantiation_GetFields_ThrowsWithHelpfulMessage()
        {
            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            GenericTypeParameterBuilder[] genericParams = type.DefineGenericParameters("T");
            
            FieldBuilder field = type.DefineField("TestField", typeof(int), FieldAttributes.Public);
            
            Type createdType = type.CreateType();
            Type instantiatedType = createdType.MakeGenericType(typeof(string));
            
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => instantiatedType.GetFields());
            Assert.Contains("TypeBuilder.GetField", ex.Message);
        }

        [Fact]
        public void TypeBuilderInstantiation_GetInterface_ThrowsWithDefaultMessage()
        {
            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            GenericTypeParameterBuilder[] genericParams = type.DefineGenericParameters("T");
            
            Type createdType = type.CreateType();
            Type instantiatedType = createdType.MakeGenericType(typeof(string));
            
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => instantiatedType.GetInterface("SomeInterface", false));
            Assert.Equal("Specified method is not supported.", ex.Message);
        }

        [Fact]
        public void TypeBuilderInstantiation_GetEvent_ThrowsWithDefaultMessage()
        {
            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            GenericTypeParameterBuilder[] genericParams = type.DefineGenericParameters("T");
            
            Type createdType = type.CreateType();
            Type instantiatedType = createdType.MakeGenericType(typeof(string));
            
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => instantiatedType.GetEvent("SomeEvent"));
            Assert.Equal("Specified method is not supported.", ex.Message);
        }

        [Fact]
        public void TypeBuilderInstantiation_GetProperty_ThrowsWithDefaultMessage()
        {
            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            GenericTypeParameterBuilder[] genericParams = type.DefineGenericParameters("T");
            
            Type createdType = type.CreateType();
            Type instantiatedType = createdType.MakeGenericType(typeof(string));
            
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => instantiatedType.GetProperty("SomeProperty"));
            Assert.Equal("Specified method is not supported.", ex.Message);
        }

        [Fact]
        public void TypeBuilderInstantiation_GetNestedType_ThrowsWithDefaultMessage()
        {
            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            GenericTypeParameterBuilder[] genericParams = type.DefineGenericParameters("T");
            
            Type createdType = type.CreateType();
            Type instantiatedType = createdType.MakeGenericType(typeof(string));
            
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => instantiatedType.GetNestedType("SomeNestedType"));
            Assert.Equal("Specified method is not supported.", ex.Message);
        }

        [Fact]
        public void TypeBuilderInstantiation_GetMember_ThrowsWithDefaultMessage()
        {
            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            GenericTypeParameterBuilder[] genericParams = type.DefineGenericParameters("T");
            
            Type createdType = type.CreateType();
            Type instantiatedType = createdType.MakeGenericType(typeof(string));
            
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => instantiatedType.GetMember("SomeMember"));
            Assert.Equal("Specified method is not supported.", ex.Message);
        }
    }
}