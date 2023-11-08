// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySaveConstructorBuilderTests
    {
        [Fact]
        public void DefineDefaultConstructorTest()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                ConstructorBuilder constructor = type.DefineDefaultConstructor(MethodAttributes.Public);
                ConstructorBuilder constructor2 = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(int) });
                ILGenerator il = constructor2.GetILGenerator();
                il.Emit(OpCodes.Ret);
                saveMethod.Invoke(ab, new object[] { file.Path });
                
                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                ConstructorInfo[] ctors = typeFromDisk.GetConstructors();
                //Assert.Equal(2, ctors.Length);
            }
        }

        public static bool _ranConstructor = false;

        [Fact]
        public void DefineDefaultConstructor_GenericParentCreated_Works()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                type.DefineGenericParameters("T");

                ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                ILGenerator constructorILGenerator = constructor.GetILGenerator();
                constructorILGenerator.Emit(OpCodes.Ldarg_0);
                constructorILGenerator.Emit(OpCodes.Ldc_I4_1);
                constructorILGenerator.Emit(OpCodes.Stfld, typeof(AssemblySaveConstructorBuilderTests).GetField(nameof(_ranConstructor)));
                constructorILGenerator.Emit(OpCodes.Ret);

                Type genericParent = type.MakeGenericType(typeof(int));
                TypeBuilder type2 = ((ModuleBuilder)type.Module).DefineType("Type2");
                type2.SetParent(genericParent);
                //type2.DefineDefaultConstructor(MethodAttributes.Public);

                saveMethod.Invoke(ab, new object[] { file.Path });
            }
        }

        [Fact]
        public void DefineDefaultConstructor_Interface_ThrowsInvalidOperationException()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndSaveMethod(new AssemblyName("MyAssembly"), null, typeof(string), out var _);
            TypeBuilder type = ab.DefineDynamicModule("MyModule").DefineType("MyType", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            Assert.Throws<InvalidOperationException>(() => type.DefineDefaultConstructor(MethodAttributes.Public));
        }

        /*[Fact]
        public void DefineDefaultConstructor_StaticVirtual_ThrowsArgumentException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
            AssertExtensions.Throws<ArgumentException>(null, () => type.DefineDefaultConstructor(MethodAttributes.Virtual | MethodAttributes.Static));
        }*/

        [Fact]
        public void DefineDefaultConstructor_NoDefaultConstructor_ThrowsNotSupportedException()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
            FieldBuilder field = type.DefineField("TestField", typeof(int), FieldAttributes.Family);

            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] { typeof(int) });
            ILGenerator constructorIlGenerator = constructor.GetILGenerator();
            constructorIlGenerator.Emit(OpCodes.Ldarg_0);
            constructorIlGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            constructorIlGenerator.Emit(OpCodes.Ldarg_0);
            constructorIlGenerator.Emit(OpCodes.Ldarg_1);
            constructorIlGenerator.Emit(OpCodes.Stfld, field);
            constructorIlGenerator.Emit(OpCodes.Ret);

            TypeBuilder derivedType = ((ModuleBuilder)type.Module).DefineType("DerivedType", TypeAttributes.Public | TypeAttributes.Class);
            derivedType.SetParent(type);

            Assert.Throws<NotSupportedException>(() => derivedType.DefineDefaultConstructor(MethodAttributes.Public));
        }

        [Theory]
        [InlineData(MethodAttributes.Private)]
        [InlineData(MethodAttributes.PrivateScope)]
        public void DefineDefaultConstructor_PrivateDefaultConstructor_ThrowsNotSupportedException(MethodAttributes attributes)
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

            ConstructorBuilder ctor = type.DefineDefaultConstructor(MethodAttributes.PrivateScope | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            var constructor = TypeBuilder.GetConstructor(type, ctor);
            Assert.False(constructor.IsGenericMethodDefinition);
        }

        [Fact]
        public void GetConstructor()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            type.DefineGenericParameters("T");

            ConstructorBuilder ctor = type.DefineDefaultConstructor(MethodAttributes.PrivateScope | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            Type genericIntType = type.MakeGenericType(typeof(int));
            ConstructorInfo constructor = TypeBuilder.GetConstructor(genericIntType, ctor);
            Assert.False(constructor.IsGenericMethodDefinition);
        }

        [Fact]
        public void GetConstructor_DeclaringTypeOfConstructorNotGenericTypeDefinitionOfType_ThrowsArgumentException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type1, out MethodInfo _);
            type1.DefineGenericParameters("T");

            TypeBuilder type2 = ((ModuleBuilder)type1.Module).DefineType("TestType2", TypeAttributes.Class | TypeAttributes.Public);
            type2.DefineGenericParameters("T");

            ConstructorBuilder ctor1 = type1.DefineDefaultConstructor(MethodAttributes.PrivateScope | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            ConstructorBuilder ctor2 = type2.DefineDefaultConstructor(MethodAttributes.PrivateScope | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            Type genericInt = type1.MakeGenericType(typeof(int));
            AssertExtensions.Throws<ArgumentException>("type", () => TypeBuilder.GetConstructor(genericInt, ctor2));
        }

        [Fact]
        public void GetConstructor_TypeNotGeneric_ThrowsArgumentException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);

            ConstructorBuilder ctor = type.DefineDefaultConstructor(MethodAttributes.PrivateScope | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            AssertExtensions.Throws<ArgumentException>("constructor", () => TypeBuilder.GetConstructor(type.AsType(), ctor));
        }
    }
}
