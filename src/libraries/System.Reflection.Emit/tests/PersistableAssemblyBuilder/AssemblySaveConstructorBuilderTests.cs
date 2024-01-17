// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySaveConstructorBuilderTests
    {
        [Fact]
        public void DefineConstructorsTest()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                ConstructorBuilder constructor = type.DefineDefaultConstructor(MethodAttributes.Public);
                ConstructorBuilder constructor2 = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, [typeof(int)]);
                constructor2.DefineParameter(1, ParameterAttributes.None, "parameter1");
                FieldBuilder fieldBuilderA = type.DefineField("TestField", typeof(int), FieldAttributes.Private);
                ILGenerator il = constructor2.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, fieldBuilderA);
                il.Emit(OpCodes.Ret);
                type.CreateType();
                saveMethod.Invoke(ab, [file.Path]);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Type typeFromDisk = assemblyFromDisk.GetType("MyType");
                    ConstructorInfo[] ctors = typeFromDisk.GetConstructors();
                    Assert.Equal(2, ctors.Length);

                    Assert.Equal(constructor, type.GetConstructor(Type.EmptyTypes));
                    Assert.Equal(ctors[0], typeFromDisk.GetConstructor(Type.EmptyTypes));
                    Assert.Equal(ctors[1], typeFromDisk.GetConstructor([mlc.CoreAssembly.GetType("System.Int32")]));
                    Assert.True(ctors[0].Attributes.HasFlag(MethodAttributes.SpecialName));
                    Assert.True(ctors[0].Attributes.HasFlag(MethodAttributes.RTSpecialName));
                    Assert.True(ctors[1].Attributes.HasFlag(MethodAttributes.SpecialName));
                    Assert.True(ctors[1].Attributes.HasFlag(MethodAttributes.RTSpecialName));
                }
            }
        }

        [Fact]
        public void DefineDefaultConstructor_WithTypeBuilderParent()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            type.CreateType();
            TypeBuilder child = ab.GetDynamicModule("MyModule").DefineType("ChildType", TypeAttributes.Public | TypeAttributes.Class);
            child.SetParent(type);
            child.DefineDefaultConstructor(MethodAttributes.Family);
            child.CreateType();

            ConstructorInfo[] ctors = child.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal(1, ctors.Length);
            Assert.True(ctors[0].IsFamily);
            Assert.Empty(child.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        }

        [Fact]
        public void DefineDefaultConstructor_TypesWithGenericParents()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                type.DefineGenericParameters("T");
                ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                FieldBuilder field = type.DefineField("TestField", typeof(bool), FieldAttributes.Public | FieldAttributes.Static);
                ILGenerator constructorILGenerator = constructor.GetILGenerator();
                constructorILGenerator.Emit(OpCodes.Ldarg_0);
                constructorILGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
                constructorILGenerator.Emit(OpCodes.Ldc_I4_1);
                constructorILGenerator.Emit(OpCodes.Stsfld, field);
                constructorILGenerator.Emit(OpCodes.Ret);
                type.CreateType();

                Assert.True(type.IsGenericTypeDefinition);
                Assert.Equal("T", type.GetGenericTypeDefinition().GetGenericArguments()[0].Name);

                Type genericParent = type.MakeGenericType(typeof(int));
                TypeBuilder derived = ((ModuleBuilder)type.Module).DefineType("Derived");
                derived.SetParent(genericParent);
                derived.CreateType();
                Type genericList = typeof(List<>).MakeGenericType(typeof(int));
                TypeBuilder type2 = ab.GetDynamicModule("MyModule").DefineType("Type2");
                type2.SetParent(genericList);
                type2.DefineDefaultConstructor(MethodAttributes.Public);
                type2.CreateTypeInfo();
                saveMethod.Invoke(ab, [file.Path]);

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                ConstructorInfo[] ctors = assemblyFromDisk.GetType("MyType").GetConstructors();
                Assert.Equal(1, ctors.Length);
                Assert.Empty(ctors[0].GetParameters());
                Type derivedFromFile = assemblyFromDisk.GetType("Derived");
                Assert.NotNull(derivedFromFile.GetConstructor(Type.EmptyTypes));
                Assert.Equal(genericParent.FullName, derivedFromFile.BaseType.FullName);
                Assert.NotNull(assemblyFromDisk.GetType("Type2").GetConstructor(Type.EmptyTypes));
            }
        }

        [Fact]
        public void DefineDefaultConstructor_Interface_ThrowsInvalidOperationException()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndSaveMethod(new AssemblyName("MyAssembly"), null, typeof(string), out var _);
            TypeBuilder type = ab.DefineDynamicModule("MyModule").DefineType("MyType", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            Assert.Throws<InvalidOperationException>(() => type.DefineDefaultConstructor(MethodAttributes.Public));
        }

        [Fact]
        public void DefineDefaultConstructor_ThrowsNotSupportedException_IfParentNotCreated()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            TypeBuilder  child = ab.GetDynamicModule("MyModule").DefineType("MyType", TypeAttributes.Public);
            child.SetParent(type);
            Assert.Throws<NotSupportedException>(() => child.DefineDefaultConstructor(MethodAttributes.Public));
        }

        [Fact]
        public void DefineDefaultConstructor_StaticVirtual_ThrowsArgumentException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
            AssertExtensions.Throws<ArgumentException>(null, () => type.DefineDefaultConstructor(MethodAttributes.Virtual | MethodAttributes.Static));
        }

        [Fact]
        public void DefineDefaultConstructor_ParentNoDefaultConstructor_ThrowsNotSupportedException()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            FieldBuilder field = type.DefineField("TestField", typeof(int), FieldAttributes.Family);

            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] { typeof(int) });
            ILGenerator constructorIlGenerator = constructor.GetILGenerator();
            constructorIlGenerator.Emit(OpCodes.Ldarg_0);
            constructorIlGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            constructorIlGenerator.Emit(OpCodes.Ldarg_0);
            constructorIlGenerator.Emit(OpCodes.Ldarg_1);
            constructorIlGenerator.Emit(OpCodes.Stfld, field);
            constructorIlGenerator.Emit(OpCodes.Ret);

            TypeBuilder derivedType = ab.GetDynamicModule("MyModule").DefineType("DerivedType", TypeAttributes.Public | TypeAttributes.Class);
            derivedType.SetParent(type);

            Assert.Throws<NotSupportedException>(() => derivedType.DefineDefaultConstructor(MethodAttributes.Public));
        }

        [Theory]
        [InlineData(MethodAttributes.Private)]
        [InlineData(MethodAttributes.PrivateScope)]
        public void DefineDefaultConstructor_ParentPrivateDefaultConstructor_ThrowsNotSupportedException(MethodAttributes attributes)
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder baseType, out MethodInfo _);
            ConstructorBuilder constructor = baseType.DefineConstructor(attributes, CallingConventions.HasThis, new[] { typeof(int) });
            constructor.GetILGenerator().Emit(OpCodes.Ret);

            TypeBuilder type = ((ModuleBuilder)baseType.Module).DefineType("DerivedType", TypeAttributes.Public | TypeAttributes.Class);
            type.SetParent(baseType);
            Assert.Throws<NotSupportedException>(() => type.DefineDefaultConstructor(MethodAttributes.Public));
        }

        [Fact]
        public void GetConstructor_DeclaringTypeOfConstructorGenericTypeDefinition()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            type.DefineGenericParameters("T");

            ConstructorBuilder ctor = type.DefineDefaultConstructor(MethodAttributes.PrivateScope | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName);
            var constructor = TypeBuilder.GetConstructor(type, ctor);
            Assert.False(constructor.IsGenericMethodDefinition);
        }

        [Fact]
        public void TypeBuilder_GetConstructorWorks()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            type.DefineGenericParameters("T");

            ConstructorBuilder ctor = type.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName);

            Type genericIntType = type.MakeGenericType(typeof(int));
            ConstructorInfo constructor = TypeBuilder.GetConstructor(genericIntType, ctor);
            Assert.Equal(ctor.MetadataToken, constructor.MetadataToken);
            Assert.Equal(ctor.Attributes, constructor.Attributes);
        }

        [Fact]
        public void GetConstructor_DeclaringTypeOfConstructorNotGenericTypeDefinitionOfType_ThrowsArgumentException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type1, out MethodInfo _);
            type1.DefineGenericParameters("T");

            TypeBuilder type2 = ((ModuleBuilder)type1.Module).DefineType("TestType2", TypeAttributes.Class | TypeAttributes.Public);
            type2.DefineGenericParameters("T");

            ConstructorBuilder ctor1 = type1.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName);
            ConstructorBuilder ctor2 = type2.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName);

            Type genericInt = type1.MakeGenericType(typeof(int));
            AssertExtensions.Throws<ArgumentException>("type", () => TypeBuilder.GetConstructor(genericInt, ctor2));
        }

        [Fact]
        public void GetConstructor_TypeNotGeneric_ThrowsArgumentException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);

            ConstructorBuilder ctor = type.DefineDefaultConstructor(MethodAttributes.Public);

            AssertExtensions.Throws<ArgumentException>("constructor", () => TypeBuilder.GetConstructor(type.AsType(), ctor));
        }
    }
}
