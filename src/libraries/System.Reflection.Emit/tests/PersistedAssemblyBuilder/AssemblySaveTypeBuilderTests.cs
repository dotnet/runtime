﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
    public class AssemblySaveTypeBuilderTests
    {
        private static readonly AssemblyName s_assemblyName = new AssemblyName("MyDynamicAssembly")
        {
            Version = new Version("1.2.3.4"),
            CultureInfo = Globalization.CultureInfo.CurrentCulture,
            ContentType = AssemblyContentType.WindowsRuntime,
            Flags = AssemblyNameFlags.EnableJITcompileTracking | AssemblyNameFlags.Retargetable,
        };

        [Fact]
        public void EmptyAssemblyAndModuleTest()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblySaveTools.WriteAssemblyToDisk(s_assemblyName, Type.EmptyTypes, file.Path);
                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Assert.Empty(assemblyFromDisk.GetTypes());
                    AssemblySaveTools.AssertAssemblyNameAndModule(s_assemblyName, assemblyFromDisk.GetName(), assemblyFromDisk.Modules.FirstOrDefault());
                }
            }
        }

        public static IEnumerable<object[]> VariousInterfacesStructsTestData()
        {
            yield return new object[] { new Type[] { typeof(INoMethod) } };
            yield return new object[] { new Type[] { typeof(IMultipleMethod) } };
            yield return new object[] { new Type[] { typeof(INoMethod), typeof(IOneMethod) } };
            yield return new object[] { new Type[] { typeof(IMultipleMethod), typeof(EmptyTestClass) } };
            yield return new object[] { new Type[] { typeof(IMultipleMethod), typeof(EmptyTestClass), typeof(IAccess), typeof(IOneMethod), typeof(INoMethod) } };
            yield return new object[] { new Type[] { typeof(EmptyStruct) } };
            yield return new object[] { new Type[] { typeof(StructWithFields) } };
            yield return new object[] { new Type[] { typeof(StructWithFields), typeof(EmptyStruct) } };
            yield return new object[] { new Type[] { typeof(IMultipleMethod), typeof(StructWithFields), typeof(ClassWithFields), typeof(EmptyTestClass) } };
        }

        [Theory]
        [MemberData(nameof(VariousInterfacesStructsTestData))]
        public void WriteAssemblyWithVariousTypesToAFileAndReadBackTest(Type[] types)
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblySaveTools.WriteAssemblyToDisk(s_assemblyName, types, file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    AssertTypesAndTypeMembers(types, assemblyFromDisk.Modules.First().GetTypes());
                }
            }
        }

        private static void AssertTypesAndTypeMembers(Type[] types, Type[] typesFromDisk)
        {
            Assert.Equal(types.Length, typesFromDisk.Length);

            for (int i = 0; i < types.Length; i++)
            {
                Type sourceType = types[i];
                Type typeFromDisk = typesFromDisk[i];

                AssemblySaveTools.AssertTypeProperties(sourceType, typeFromDisk);
                AssemblySaveTools.AssertMethods(sourceType.GetMethods(BindingFlags.DeclaredOnly), typeFromDisk.GetMethods(BindingFlags.DeclaredOnly));
                AssemblySaveTools.AssertFields(sourceType.GetFields(), typeFromDisk.GetFields());
            }
        }

        [Theory]
        [MemberData(nameof(VariousInterfacesStructsTestData))]
        public void WriteAssemblyWithVariousTypesToStreamAndReadBackTest(Type[] types)
        {
            using (var stream = new MemoryStream())
            {
                AssemblySaveTools.WriteAssemblyToStream(s_assemblyName, types, stream);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromStream = mlc.LoadFromStream(stream);
                    AssertTypesAndTypeMembers(types, assemblyFromStream.Modules.First().GetTypes());
                }
            }
        }

        [Fact]
        public void CreateMembersThatUsesTypeLoadedFromCoreAssemblyTest()
        {
            using (TempFile file = TempFile.Create())
            {
                TypeBuilder tb = CreateAssemblyAndDefineType(out PersistedAssemblyBuilder assemblyBuilder);
                tb.DefineMethod("TestMethod", MethodAttributes.Public).GetILGenerator().Emit(OpCodes.Ret);
                tb.CreateType();
                assemblyBuilder.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Module moduleFromDisk = assemblyFromDisk.Modules.First();

                    Assert.Equal("MyModule", moduleFromDisk.ScopeName);
                    Assert.Equal(1, moduleFromDisk.GetTypes().Length);

                    Type testType = moduleFromDisk.GetTypes()[0];
                    Assert.Equal("TestInterface", testType.Name);

                    MethodInfo method = testType.GetMethods()[0];
                    Assert.Equal("TestMethod", method.Name);
                    Assert.Empty(method.GetParameters());
                    Assert.Equal("System.Void", method.ReturnType.FullName);
                }
            }
        }

        private static ModuleBuilder CreateAssembly(out PersistedAssemblyBuilder assemblyBuilder)
        {
            assemblyBuilder = AssemblySaveTools.PopulateAssemblyBuilder(s_assemblyName);
            return assemblyBuilder.DefineDynamicModule("MyModule");
        }

        private static TypeBuilder CreateAssemblyAndDefineType(out PersistedAssemblyBuilder assemblyBuilder)
        {
            return CreateAssembly(out assemblyBuilder).DefineType("TestInterface", TypeAttributes.Interface | TypeAttributes.Abstract);
        }

        [Fact]
        public void AddInterfaceImplementationTest()
        {
            using (TempFile file = TempFile.Create())
            {
                PersistedAssemblyBuilder assemblyBuilder = AssemblySaveTools.PopulateAssemblyBuilder(s_assemblyName);
                ModuleBuilder mb = assemblyBuilder.DefineDynamicModule("My Module");
                TypeBuilder tb = mb.DefineType("TestInterface", TypeAttributes.Interface | TypeAttributes.Abstract, null, [typeof(IOneMethod)]);
                tb.AddInterfaceImplementation(typeof(INoMethod));
                TypeBuilder nestedType = tb.DefineNestedType("NestedType", TypeAttributes.Interface | TypeAttributes.Abstract);
                tb.CreateType();
                nestedType.CreateType();
                assemblyBuilder.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Type testType = assemblyFromDisk.Modules.First().GetTypes()[0];
                    Type[] interfaces = testType.GetInterfaces();

                    Assert.Equal("TestInterface", testType.Name);
                    Assert.Equal(2, interfaces.Length);

                    Type iOneMethod = testType.GetInterface("IOneMethod");
                    Type iNoMethod = testType.GetInterface("INoMethod");
                    Type[] nt = testType.GetNestedTypes();
                    Assert.Equal(1, iOneMethod.GetMethods().Length);
                    Assert.Empty(iNoMethod.GetMethods());
                    Assert.NotNull(testType.GetNestedType("NestedType", BindingFlags.NonPublic));
                }
            }
        }

        public static IEnumerable<object[]> TypeParameters()
        {
            yield return new object[] { new string[] { "TFirst", "TSecond", "TThird" } };
            yield return new object[] { new string[] { "TFirst" } };
        }

        [Theory]
        [MemberData(nameof(TypeParameters))]
        public void SaveGenericTypeParametersForAType(string[] typeParamNames)
        {
            using (TempFile file = TempFile.Create())
            {
                TypeBuilder tb = CreateAssemblyAndDefineType(out PersistedAssemblyBuilder assemblyBuilder);
                MethodBuilder method = tb.DefineMethod("TestMethod", MethodAttributes.Public);
                method.GetILGenerator().Emit(OpCodes.Ldarg_0);
                GenericTypeParameterBuilder[] typeParams = tb.DefineGenericParameters(typeParamNames);
                if (typeParams.Length > 2)
                {
                    SetVariousGenericParameterValues(typeParams);
                }
                tb.CreateType();
                assemblyBuilder.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Type testType = assemblyFromDisk.Modules.First().GetTypes()[0];
                    MethodInfo testMethod = testType.GetMethod("TestMethod");
                    Type[] genericTypeParams = testType.GetGenericArguments();

                    Assert.True(testType.IsGenericType);
                    Assert.True(testType.IsGenericTypeDefinition);
                    Assert.True(testType.ContainsGenericParameters);
                    Assert.False(testMethod.IsGenericMethod);
                    Assert.False(testMethod.IsGenericMethodDefinition);
                    Assert.True(testMethod.ContainsGenericParameters);
                    AssertGenericParameters(typeParams, genericTypeParams);
                }
            }
        }

        private class GenericClassWithGenericField<T>
        {
#pragma warning disable CS0649
            public T F;
#pragma warning restore CS0649
        }

        private class GenericClassWithNonGenericField<T>
        {
#pragma warning disable CS0649
            public int F;
#pragma warning restore CS0649
        }

        public static IEnumerable<object[]> GenericTypesWithField()
        {
            yield return new object[] { typeof(GenericClassWithGenericField<int>), true };
            yield return new object[] { typeof(GenericClassWithNonGenericField<bool>), false };
        }

        [Theory]
        [MemberData(nameof(GenericTypesWithField))]
        public void SaveGenericField(Type declaringType, bool shouldFieldBeGeneric)
        {
            using (TempFile file = TempFile.Create())
            {
                ModuleBuilder mb = CreateAssembly(out PersistedAssemblyBuilder assemblyBuilder);
                TypeBuilder tb = mb.DefineType("C", TypeAttributes.Class);
                MethodBuilder method = tb.DefineMethod("TestMethod", MethodAttributes.Public, returnType: typeof(int), parameterTypes: null);
                ILGenerator il = method.GetILGenerator();
                il.Emit(OpCodes.Newobj, declaringType.GetConstructor([]));
                il.Emit(OpCodes.Ldfld, declaringType.GetField("F"));
                il.Emit(OpCodes.Ret);
                Type createdType = tb.CreateType();
                assemblyBuilder.Save(file.Path);

                using (FileStream stream = File.OpenRead(file.Path))
                {
                    using (PEReader peReader = new PEReader(stream))
                    {
                        bool found = false;
                        MetadataReader metadataReader = peReader.GetMetadataReader();
                        foreach (MemberReferenceHandle memberRefHandle in metadataReader.MemberReferences)
                        {
                            MemberReference memberRef = metadataReader.GetMemberReference(memberRefHandle);
                            if (memberRef.GetKind() == MemberReferenceKind.Field)
                            {
                                Assert.False(found);
                                found = true;

                                Assert.Equal("F", metadataReader.GetString(memberRef.Name));

                                // A reference to a generic field should point to the open generic field, and not the resolved generic type.
                                Assert.Equal(shouldFieldBeGeneric, IsGenericField(metadataReader.GetBlobReader(memberRef.Signature)));
                            }
                        }

                        Assert.True(found);
                    }
                }
            }

            static bool IsGenericField(BlobReader signatureReader)
            {
                while (signatureReader.RemainingBytes > 0)
                {
                    SignatureTypeCode typeCode = signatureReader.ReadSignatureTypeCode();
                    if (typeCode == SignatureTypeCode.GenericTypeParameter)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static void SetVariousGenericParameterValues(GenericTypeParameterBuilder[] typeParams)
        {
            typeParams[0].SetInterfaceConstraints([typeof(IAccess), typeof(INoMethod)]);
            typeParams[1].SetCustomAttribute(new CustomAttributeBuilder(typeof(DynamicallyAccessedMembersAttribute).GetConstructor(
                [typeof(DynamicallyAccessedMemberTypes)]), [DynamicallyAccessedMemberTypes.PublicProperties]));
            typeParams[2].SetBaseTypeConstraint(typeof(EmptyTestClass));
            typeParams[2].SetGenericParameterAttributes(GenericParameterAttributes.VarianceMask);
        }

        private static void AssertGenericParameters(GenericTypeParameterBuilder[] typeParams, Type[] genericTypeParams)
        {
            Assert.Equal("TFirst", genericTypeParams[0].Name);
            if (typeParams.Length > 2)
            {
                Assert.Equal("TSecond", genericTypeParams[1].Name);
                Assert.Equal("TThird", genericTypeParams[2].Name);
                Type[] constraints = genericTypeParams[0].GetTypeInfo().GetGenericParameterConstraints();
                Assert.Equal(2, constraints.Length);
                Assert.Equal(typeof(IAccess).FullName, constraints[0].FullName);
                Assert.Equal(typeof(INoMethod).FullName, constraints[1].FullName);
                Assert.Empty(genericTypeParams[1].GetTypeInfo().GetGenericParameterConstraints());
                Type[] constraints2 = genericTypeParams[2].GetTypeInfo().GetGenericParameterConstraints();
                Assert.Equal(1, constraints2.Length);
                Assert.Equal(typeof(EmptyTestClass).FullName, constraints2[0].FullName);
                Assert.Equal(GenericParameterAttributes.None, genericTypeParams[0].GenericParameterAttributes);
                Assert.Equal(GenericParameterAttributes.VarianceMask, genericTypeParams[2].GenericParameterAttributes);
                IList<CustomAttributeData> attributes = genericTypeParams[1].GetCustomAttributesData();
                Assert.Equal(1, attributes.Count);
                Assert.Equal("DynamicallyAccessedMembersAttribute", attributes[0].AttributeType.Name);
                Assert.Equal(DynamicallyAccessedMemberTypes.PublicProperties, (DynamicallyAccessedMemberTypes)attributes[0].ConstructorArguments[0].Value);
                Assert.Empty(genericTypeParams[0].GetCustomAttributesData());
            }
        }

        [Theory]
        [MemberData(nameof(TypeParameters))]
        public void SaveGenericTypeParametersForAMethod(string[] typeParamNames)
        {
            using (TempFile file = TempFile.Create())
            {
                TypeBuilder tb = CreateAssemblyAndDefineType(out PersistedAssemblyBuilder assemblyBuilder);
                MethodBuilder method = tb.DefineMethod("TestMethod", MethodAttributes.Public);
                GenericTypeParameterBuilder[] typeParams = method.DefineGenericParameters(typeParamNames);
                method.GetILGenerator().Emit(OpCodes.Ldarg_0);
                if (typeParams.Length > 2)
                {
                    SetVariousGenericParameterValues(typeParams);
                }
                tb.CreateType();
                assemblyBuilder.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type testType = mlc.LoadFromAssemblyPath(file.Path).Modules.First().GetTypes()[0];
                    MethodInfo testMethod = testType.GetMethod("TestMethod");
                    Type[] genericTypeParams = testMethod.GetGenericArguments();

                    Assert.False(testType.IsGenericType);
                    Assert.False(testType.IsGenericTypeDefinition);
                    Assert.False(testType.ContainsGenericParameters);
                    Assert.True(testMethod.IsGenericMethod);
                    Assert.True(testMethod.IsGenericMethodDefinition);
                    Assert.True(testMethod.ContainsGenericParameters);
                    AssertGenericParameters(typeParams, genericTypeParams);
                }
            }
        }

        [Theory]
        [InlineData(0, "TestInterface[]")]
        [InlineData(1, "TestInterface[]")] // not [*]
        [InlineData(2, "TestInterface[,]")]
        [InlineData(3, "TestInterface[,,]")]
        public void SaveArrayTypeSignature(int rank, string name)
        {
            using (TempFile file = TempFile.Create())
            {
                TypeBuilder tb = CreateAssemblyAndDefineType(out PersistedAssemblyBuilder assemblyBuilder);
                Type arrayType = rank == 0 ? tb.MakeArrayType() : tb.MakeArrayType(rank);
                MethodBuilder mb = tb.DefineMethod("TestMethod", MethodAttributes.Public);
                mb.SetReturnType(arrayType);
                mb.SetParameters([typeof(INoMethod), arrayType, typeof(int[,,,])]);
                mb.GetILGenerator().Emit(OpCodes.Ret);
                tb.CreateType();
                assemblyBuilder.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type testType = mlc.LoadFromAssemblyPath(file.Path).Modules.First().GetTypes()[0];
                    MethodInfo testMethod = testType.GetMethod("TestMethod");
                    Type intArray = testMethod.GetParameters()[2].ParameterType;

                    Assert.False(testMethod.GetParameters()[0].ParameterType.IsSZArray);
                    Assert.True(intArray.IsArray);
                    Assert.Equal(4, intArray.GetArrayRank());
                    Assert.Equal("Int32[,,,]", intArray.Name);
                    AssertArrayTypeSignature(rank, name, testMethod.ReturnType);
                    AssertArrayTypeSignature(rank, name, testMethod.GetParameters()[1].ParameterType);
                }
            }
        }

        private static void AssertArrayTypeSignature(int rank, string name, Type arrayType)
        {
            Assert.True(rank < 2 ? arrayType.IsSZArray : arrayType.IsArray);
            rank = rank == 0 ? rank + 1 : rank;
            Assert.Equal(rank, arrayType.GetArrayRank());
            Assert.Equal(name, arrayType.Name);
        }

        [Fact]
        public void SaveByRefTypeSignature()
        {
            using (TempFile file = TempFile.Create())
            {
                TypeBuilder tb = CreateAssemblyAndDefineType(out PersistedAssemblyBuilder assemblyBuilder);
                Type byrefType = tb.MakeByRefType();
                MethodBuilder mb = tb.DefineMethod("TestMethod", MethodAttributes.Public);
                mb.SetReturnType(byrefType);
                mb.SetParameters([typeof(INoMethod), byrefType]);
                mb.GetILGenerator().Emit(OpCodes.Ret);
                tb.CreateType();
                assemblyBuilder.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type testType = mlc.LoadFromAssemblyPath(file.Path).Modules.First().GetTypes()[0];
                    MethodInfo testMethod = testType.GetMethod("TestMethod");

                    Assert.False(testMethod.GetParameters()[0].ParameterType.IsByRef);
                    AssertByRefType(testMethod.GetParameters()[1].ParameterType);
                    AssertByRefType(testMethod.ReturnType);
                }
            }
        }

        private static void AssertByRefType(Type byrefParam)
        {
            Assert.True(byrefParam.IsByRef);
            Assert.Equal("TestInterface&", byrefParam.Name);
        }

        [Fact]
        public void SavePointerTypeSignature()
        {
            using (TempFile file = TempFile.Create())
            {
                TypeBuilder tb = CreateAssemblyAndDefineType(out PersistedAssemblyBuilder assemblyBuilder);
                Type pointerType = tb.MakePointerType();
                MethodBuilder mb = tb.DefineMethod("TestMethod", MethodAttributes.Public);
                mb.SetReturnType(pointerType);
                mb.SetParameters([typeof(INoMethod), pointerType]);
                mb.GetILGenerator().Emit(OpCodes.Ret);
                tb.CreateType();
                assemblyBuilder.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type testType = mlc.LoadFromAssemblyPath(file.Path).Modules.First().GetTypes()[0];
                    MethodInfo testMethod = testType.GetMethod("TestMethod");

                    Assert.False(testMethod.GetParameters()[0].ParameterType.IsPointer);
                    AssertPointerType(testMethod.GetParameters()[1].ParameterType);
                    AssertPointerType(testMethod.ReturnType);
                }
            }
        }

        private void AssertPointerType(Type testType)
        {
            Assert.True(testType.IsPointer);
            Assert.Equal("TestInterface*", testType.Name);
        }

        [Fact]
        public void GenericTypeParameter_MakePointerType_MakeByRefType_MakeArrayType()
        {
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters("T");
            Type pointerType = typeParams[0].MakePointerType();
            Type byrefType = typeParams[0].MakeByRefType();
            Type arrayType = typeParams[0].MakeArrayType();
            Type arrayType2 = typeParams[0].MakeArrayType(3);
            FieldBuilder field = type.DefineField("Field", pointerType, FieldAttributes.Public);
            FieldBuilder fieldArray = type.DefineField("FieldArray", arrayType2, FieldAttributes.Public);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);
            method.SetSignature(arrayType, null, null, [byrefType], null, null);
            method.GetILGenerator().Emit(OpCodes.Ret);
            Type genericIntType = type.MakeGenericType(typeof(int));
            type.CreateType();

            Assert.Equal(pointerType, type.GetField("Field").FieldType);
            Assert.True(type.GetField("Field").FieldType.IsPointer);
            Assert.Equal(arrayType2, type.GetField("FieldArray").FieldType);
            Assert.True(type.GetField("FieldArray").FieldType.IsArray);
            Assert.Equal(3, type.GetField("FieldArray").FieldType.GetArrayRank());
            MethodInfo testMethod = type.GetMethod("TestMethod");
            Assert.Equal(arrayType, testMethod.ReturnType);
            Assert.True(testMethod.ReturnType.IsArray);
            Assert.Equal(1, testMethod.GetParameters().Length);
            Assert.Equal(byrefType, testMethod.GetParameters()[0].ParameterType);
            Assert.True(testMethod.GetParameters()[0].ParameterType.IsByRef);
        }

        public static IEnumerable<object[]> SaveGenericType_TestData()
        {
            yield return new object[] { new string[] { "U", "T" }, new Type[] { typeof(string), typeof(int) }, "TestInterface[System.String,System.Int32]" };
            yield return new object[] { new string[] { "U", "T" }, new Type[] { typeof(MakeGenericTypeClass), typeof(MakeGenericTypeInterface) },
                        "TestInterface[System.Reflection.Emit.Tests.MakeGenericTypeClass,System.Reflection.Emit.Tests.MakeGenericTypeInterface]" };
            yield return new object[] { new string[] { "U" }, new Type[] { typeof(List<string>) }, "TestInterface[System.Collections.Generic.List`1[System.String]]" };
        }

        [Theory]
        [MemberData(nameof(SaveGenericType_TestData))]
        public void SaveGenericTypeSignature(string[] genericParams, Type[] typeArguments, string stringRepresentation)
        {
            using (TempFile file = TempFile.Create())
            {
                TypeBuilder tb = CreateAssemblyAndDefineType(out PersistedAssemblyBuilder assemblyBuilder);
                GenericTypeParameterBuilder[] typeGenParam = tb.DefineGenericParameters(genericParams);
                Type genericType = tb.MakeGenericType(typeArguments);
                MethodBuilder mb = tb.DefineMethod("TestMethod", MethodAttributes.Public);
                mb.SetReturnType(genericType);
                mb.SetParameters([typeof(INoMethod), genericType]);
                mb.GetILGenerator().Emit(OpCodes.Ret);
                tb.CreateType();
                assemblyBuilder.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type testType = mlc.LoadFromAssemblyPath(file.Path).Modules.First().GetTypes()[0];
                    MethodInfo testMethod = testType.GetMethod("TestMethod");
                    Type paramType = testMethod.GetParameters()[1].ParameterType;

                    Assert.False(testMethod.GetParameters()[0].ParameterType.IsGenericType);
                    AssertGenericType(stringRepresentation, paramType);
                    AssertGenericType(stringRepresentation, testMethod.ReturnType);
                }
            }
        }

        [Fact]
        public void TypeBuilder_GetMethod_ReturnsMethod()
        {
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            type.DefineGenericParameters("T");

            MethodBuilder genericMethod = type.DefineMethod("GM", MethodAttributes.Public | MethodAttributes.Static);
            GenericTypeParameterBuilder[] methodParams = genericMethod.DefineGenericParameters("U");
            genericMethod.SetParameters(new[] { methodParams[0] });

            Type genericIntType = type.MakeGenericType(typeof(int));
            MethodInfo createdConstructedTypeMethod = TypeBuilder.GetMethod(genericIntType, genericMethod);
            MethodInfo createdGenericMethod = TypeBuilder.GetMethod(type, genericMethod);

            Assert.True(createdConstructedTypeMethod.IsGenericMethodDefinition);
            Assert.True(createdGenericMethod.IsGenericMethodDefinition);
            Assert.Equal("Type: U", createdConstructedTypeMethod.GetGenericArguments()[0].ToString());
            Assert.Equal("Type: U", createdGenericMethod.GetGenericArguments()[0].ToString());
        }

        [Fact]
        public void TypeBuilder_GetField_DeclaringTypeOfFieldGeneric()
        {
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters("T");

            FieldBuilder field = type.DefineField("Field", typeParams[0].AsType(), FieldAttributes.Public);
            FieldBuilder field2 = type.DefineField("Field2", typeParams[0], FieldAttributes.Public);
            Type genericIntType = type.MakeGenericType(typeof(int));

            Assert.Equal("Field", TypeBuilder.GetField(type.AsType(), field).Name);
            Assert.Equal("Field2", TypeBuilder.GetField(genericIntType, field2).Name);
        }

        [Fact]
        public void GetField_TypeNotGeneric_ThrowsArgumentException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            FieldBuilder field = type.DefineField("Field", typeof(int), FieldAttributes.Public);

            AssertExtensions.Throws<ArgumentException>("field", () => TypeBuilder.GetField(type, field));
        }

        private static void AssertGenericType(string stringRepresentation, Type paramType)
        {
            Assert.True(paramType.IsGenericType);
            Assert.Equal(stringRepresentation, paramType.ToString());
            Assert.False(paramType.IsGenericParameter);
            Assert.False(paramType.IsGenericTypeDefinition);
            Assert.False(paramType.IsGenericTypeParameter);
            Assert.False(paramType.IsGenericMethodParameter);
        }

        [Fact]
        public void SaveGenericTypeSignatureWithGenericParameter()
        {
            using (TempFile file = TempFile.Create())
            {
                TypeBuilder tb = CreateAssemblyAndDefineType(out PersistedAssemblyBuilder assemblyBuilder);
                GenericTypeParameterBuilder[] typeParams = tb.DefineGenericParameters(["U", "T", "P"]);
                MethodBuilder mb = tb.DefineMethod("TestMethod", MethodAttributes.Public);
                GenericTypeParameterBuilder[] methodParams = mb.DefineGenericParameters(["M", "N"]);
                Type genericType = tb.MakeGenericType(typeParams);
                mb.SetReturnType(methodParams[0]);
                mb.SetParameters([typeof(INoMethod), genericType, typeParams[1]]);
                mb.GetILGenerator().Emit(OpCodes.Ret);
                tb.CreateType();
                assemblyBuilder.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type testType = mlc.LoadFromAssemblyPath(file.Path).Modules.First().GetTypes()[0];
                    MethodInfo testMethod = testType.GetMethod("TestMethod");
                    Type paramType = testMethod.GetParameters()[1].ParameterType;
                    Type genericParameter = testMethod.GetParameters()[2].ParameterType;

                    Assert.False(testMethod.GetParameters()[0].ParameterType.IsGenericType);
                    AssertGenericType("TestInterface[U,T,P]", paramType);
                    Assert.False(genericParameter.IsGenericType);
                    Assert.True(genericParameter.IsGenericParameter);
                    Assert.False(genericParameter.IsGenericTypeDefinition);
                    Assert.True(genericParameter.IsGenericTypeParameter);
                    Assert.False(genericParameter.IsGenericMethodParameter);
                    Assert.Equal("T", genericParameter.Name);
                    Assert.False(testMethod.ReturnType.IsGenericType);
                    Assert.True(testMethod.ReturnType.IsGenericParameter);
                    Assert.False(testMethod.ReturnType.IsGenericTypeDefinition);
                    Assert.False(testMethod.ReturnType.IsGenericTypeParameter);
                    Assert.True(testMethod.ReturnType.IsGenericMethodParameter);
                    Assert.Equal("M", testMethod.ReturnType.Name);
                }
            }
        }

        [Fact]
        public void SaveMultipleGenericTypeParametersToEnsureSortingWorks()
        {
            using (TempFile file = TempFile.Create())
            {
                PersistedAssemblyBuilder assemblyBuilder = AssemblySaveTools.PopulateAssemblyBuilder(s_assemblyName);
                ModuleBuilder mb = assemblyBuilder.DefineDynamicModule("My Module");
                TypeBuilder tb = mb.DefineType("TestInterface1", TypeAttributes.Interface | TypeAttributes.Abstract);
                GenericTypeParameterBuilder[] typeParams = tb.DefineGenericParameters(["U", "T"]);
                typeParams[1].SetInterfaceConstraints([typeof(INoMethod), typeof(IOneMethod)]);
                MethodBuilder m11 = tb.DefineMethod("TwoParameters", MethodAttributes.Public);
                MethodBuilder m12 = tb.DefineMethod("FiveTypeParameters", MethodAttributes.Public);
                MethodBuilder m13 = tb.DefineMethod("OneParameter", MethodAttributes.Public);
                m11.DefineGenericParameters(["M", "N"]);
                m11.GetILGenerator().Emit(OpCodes.Ret);
                GenericTypeParameterBuilder[] methodParams = m12.DefineGenericParameters(["A", "B", "C", "D", "F"]);
                m12.GetILGenerator().Emit(OpCodes.Ret);
                methodParams[2].SetInterfaceConstraints([typeof(IMultipleMethod)]);
                m13.DefineGenericParameters(["T"]);
                m13.GetILGenerator().Emit(OpCodes.Ret);
                TypeBuilder tb2 = mb.DefineType("TestInterface2", TypeAttributes.Interface | TypeAttributes.Abstract);
                tb2.DefineGenericParameters(["TFirst", "TSecond", "TThird"]);
                MethodBuilder m21 = tb2.DefineMethod("TestMethod", MethodAttributes.Public);
                m21.DefineGenericParameters(["X", "Y", "Z"]);
                m21.GetILGenerator().Emit(OpCodes.Ret);
                TypeBuilder tb3 = mb.DefineType("TestType");
                GenericTypeParameterBuilder[] typePar = tb3.DefineGenericParameters(["TOne"]);
                typePar[0].SetBaseTypeConstraint(typeof(EmptyTestClass));
                tb3.CreateType();
                tb2.CreateType();
                tb.CreateType();
                assemblyBuilder.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Module m = mlc.LoadFromAssemblyPath(file.Path).Modules.First();
                    Type[] type1Params = m.GetTypes()[0].GetGenericArguments();
                    Type[] type2Params = m.GetTypes()[1].GetGenericArguments();
                    Type[] type3Params = m.GetTypes()[2].GetGenericArguments();

                    Assert.Equal("U", type1Params[0].Name);
                    Assert.Empty(type1Params[0].GetTypeInfo().GetGenericParameterConstraints());
                    Assert.Equal("T", type1Params[1].Name);
                    Assert.Equal(nameof(IOneMethod), type1Params[1].GetTypeInfo().GetGenericParameterConstraints()[1].Name);
                    Assert.Equal("TFirst", type2Params[0].Name);
                    Assert.Equal("TSecond", type2Params[1].Name);
                    Assert.Equal("TThird", type2Params[2].Name);
                    Assert.Equal("TOne", type3Params[0].Name);
                    Assert.Equal(nameof(EmptyTestClass), type3Params[0].GetTypeInfo().GetGenericParameterConstraints()[0].Name);

                    Type[] method11Params = m.GetTypes()[0].GetMethod("TwoParameters").GetGenericArguments();
                    Type[] method12Params = m.GetTypes()[0].GetMethod("FiveTypeParameters").GetGenericArguments();
                    Assert.Equal(nameof(IMultipleMethod), method12Params[2].GetTypeInfo().GetGenericParameterConstraints()[0].Name);
                    Type[] method13Params = m.GetTypes()[0].GetMethod("OneParameter").GetGenericArguments();
                    Type[] method21Params = m.GetTypes()[1].GetMethod("TestMethod").GetGenericArguments();

                    Assert.Equal("M", method11Params[0].Name);
                    Assert.Equal("N", method11Params[1].Name);
                    Assert.Equal("A", method12Params[0].Name);
                    Assert.Equal("F", method12Params[4].Name);
                    Assert.Equal("T", method13Params[0].Name);
                    Assert.Equal("X", method21Params[0].Name);
                    Assert.Equal("Z", method21Params[2].Name);
                }
            }
        }

        [Fact]
        public void MethodBuilderGetParametersReturnParameterTest()
        {
            PersistedAssemblyBuilder assemblyBuilder = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            MethodBuilder method1 = type.DefineMethod("Method1", MethodAttributes.Public, typeof(long), [typeof(int), typeof(string)]);
            MethodBuilder method2 = type.DefineMethod("Method2", MethodAttributes.Static);
            MethodBuilder method3 = type.DefineMethod("Method1", MethodAttributes.Public, typeof(int), [typeof(string)]);
            method1.DefineParameter(0, ParameterAttributes.Retval, null);
            method1.DefineParameter(1, ParameterAttributes.None, "index");
            method1.DefineParameter(2, ParameterAttributes.Out, "outParam");
            ParameterBuilder pb = method3.DefineParameter(1, ParameterAttributes.Optional, "name");
            pb.SetConstant("defaultName");
            //type.CreateType();

            ParameterInfo[] params1 = method1.GetParameters();
            Assert.Equal(2, params1.Length);
            Assert.Equal("index", params1[0].Name);
            Assert.Equal(typeof(int), params1[0].ParameterType);
            Assert.Equal("outParam", params1[1].Name);
            Assert.Equal(typeof(string), params1[1].ParameterType);
            Assert.Equal(ParameterAttributes.Out, params1[1].Attributes);
            Assert.True(params1[1].IsOut);
            Assert.Equal(typeof(long), method1.ReturnParameter.ParameterType);
            Assert.Null(method1.ReturnParameter.Name);
            Assert.True(method1.ReturnParameter.IsRetval);

            Assert.Empty(method2.GetParameters());
            Assert.Equal(typeof(void), method2.ReturnParameter.ParameterType);
            Assert.Null(method2.ReturnParameter.Name);
            Assert.True(method2.ReturnParameter.IsRetval);

            ParameterInfo[] params3 = method3.GetParameters();
            Assert.Equal(1, params3.Length);
            Assert.Equal("name", params3[0].Name);
            Assert.Equal(typeof(string), params3[0].ParameterType);
            Assert.True(params3[0].HasDefaultValue);
            Assert.Equal("defaultName", params3[0].DefaultValue);

            Assert.Equal(typeof(int), method3.ReturnParameter.ParameterType);
            Assert.Null(method3.ReturnParameter.Name);
            Assert.True(method3.ReturnParameter.IsRetval);
        }

        [Fact]
        public void GenericTypeWithTypeBuilderGenericParameter_UsedAsParent()
        {
            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder typeBuilder);

            Type type = typeBuilder.CreateType();
            var baseType = typeof(BaseType<>).GetGenericTypeDefinition().MakeGenericType(type);

            var typeBuilder2 = ab.GetDynamicModule("MyModule")
                .DefineType("TestService", TypeAttributes.Public | TypeAttributes.Class, baseType);
            typeBuilder2.CreateType();

            Assert.NotNull(type.GetConstructor(Type.EmptyTypes)); // Default constructor created
        }

        [Fact]
        public void CreateGenericTypeFromMetadataLoadContextSignatureTypes()
        {
            using TempFile file = TempFile.Create();

            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyAndModule(out ModuleBuilder module);

            TypeBuilder childType = module.DefineType("Child");
            TypeBuilder parentType = module.DefineType("Parent");

            // Get List<T> from MLC and make both reference and value type fields from that.
            using MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver());
            Type listOfTType = mlc.CoreAssembly.GetType(typeof(List<>).FullName!);

            // Currently MakeGenericSignatureType() must be used instead of MakeGenericType() for
            // generic type parameters created with TypeBuilder.
            Assert.Throws<ArgumentException>(() => listOfTType.MakeGenericType(childType));
            Type listOfReferenceTypes = Type.MakeGenericSignatureType(listOfTType, childType);
            parentType.DefineField("ReferenceTypeChildren", listOfReferenceTypes, FieldAttributes.Public);

            // Pre-existing types can use MakeGenericType().
            Type int32Type = mlc.CoreAssembly.GetType(typeof(int).FullName);
            Type listOfValueTypes = listOfTType.MakeGenericType(int32Type);
            parentType.DefineField("ValueTypeChildren", listOfValueTypes, FieldAttributes.Public);

            parentType.CreateType();
            childType.CreateType();

            // Save and load the dynamically created assembly.
            ab.Save(file.Path);
            Module mlcModule = mlc.LoadFromAssemblyPath(file.Path).Modules.First();

            Assert.Equal("Child", mlcModule.GetTypes()[0].Name);
            Assert.Equal("Parent", mlcModule.GetTypes()[1].Name);

            FieldInfo[] fields = mlcModule.GetTypes()[1].GetFields(BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal("ReferenceTypeChildren", fields[0].Name);
            Assert.False(fields[0].FieldType.GetGenericArguments()[0].IsValueType);
            Assert.Equal("ValueTypeChildren", fields[1].Name);
            Assert.True(fields[1].FieldType.GetGenericArguments()[0].IsValueType);
        }

        [Fact]
        public void SaveFunctionPointerFields()
        {
            using TempFile file = TempFile.Create();
            using MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver());

            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyAndModule(out ModuleBuilder mb);
            TypeBuilder tb = mb.DefineType("TestType", TypeAttributes.Public | TypeAttributes.Class);

            // delegate*<int, int>
            Type funcPtr1 = typeof(delegate*<int, int>);
            tb.DefineField("FuncPtr1", funcPtr1, FieldAttributes.Public | FieldAttributes.Static);

            // delegate* unmanaged[Cdecl]<int, float, double>
            Type funcPtr4 = new ModifiedTypeHelpers.FunctionPointer(
                typeof(delegate* unmanaged[Cdecl]<int, float, double>),
                [typeof(CallConvCdecl)]);
            tb.DefineField("FuncPtr2", funcPtr4, FieldAttributes.Public | FieldAttributes.Static);

            // delegate* unmanaged[Stdcall]<string, in int, void>
            Type funcPtr5 = new ModifiedTypeHelpers.FunctionPointer(
                typeof(delegate* unmanaged[Stdcall]<string, in int, void>),
                [typeof(CallConvStdcall)],
                customParameterTypes: [typeof(string), new ModifiedTypeHelpers.ModifiedType(typeof(int).MakeByRefType(), [typeof(InAttribute)], [])]);
            tb.DefineField("FuncPtr3", funcPtr5, FieldAttributes.Public | FieldAttributes.Static);

            tb.CreateType();
            ab.Save(file.Path);

            Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
            Type testType = assemblyFromDisk.Modules.First().GetType("TestType");
            Assert.NotNull(testType);

            FieldInfo field1 = testType.GetField("FuncPtr1");
            Assert.NotNull(field1);
            Assert.True(field1.FieldType.IsFunctionPointer);
            Assert.False(field1.FieldType.IsUnmanagedFunctionPointer);
            Type[] paramTypes1 = field1.FieldType.GetFunctionPointerParameterTypes();
            Assert.Equal(1, paramTypes1.Length);
            Assert.Equal(typeof(int).FullName, paramTypes1[0].FullName);
            Assert.Equal(typeof(int).FullName, field1.FieldType.GetFunctionPointerReturnType().FullName);

            FieldInfo field2 = testType.GetField("FuncPtr2");
            Type field2Type = field2.GetModifiedFieldType();
            Assert.NotNull(field2);
            Assert.True(field2Type.IsFunctionPointer);
            Assert.True(field2Type.IsUnmanagedFunctionPointer);
            Type[] paramTypes2 = field2Type.GetFunctionPointerParameterTypes();
            Assert.Equal(2, paramTypes2.Length);
            Assert.Equal(typeof(int).FullName, paramTypes2[0].FullName);
            Assert.Equal(typeof(float).FullName, paramTypes2[1].FullName);
            Assert.Equal(typeof(double).FullName, field2Type.GetFunctionPointerReturnType().FullName);
            Type[] callingConventions2 = field2Type.GetFunctionPointerCallingConventions();
            Assert.Contains(callingConventions2, t => t.FullName == typeof(CallConvCdecl).FullName);

            FieldInfo field3 = testType.GetField("FuncPtr3");
            Type field3Type = field3.GetModifiedFieldType();
            Assert.NotNull(field3);
            Assert.True(field3Type.IsFunctionPointer);
            Assert.True(field3Type.IsUnmanagedFunctionPointer);
            Type[] paramTypes3 = field3Type.GetFunctionPointerParameterTypes();
            Assert.Equal(2, paramTypes3.Length);
            Assert.Equal(typeof(string).FullName, paramTypes3[0].FullName);
            Assert.Equal(typeof(int).MakeByRefType().FullName, paramTypes3[1].FullName);
            Assert.Contains(paramTypes3[1].GetRequiredCustomModifiers(), t => t.FullName == typeof(InAttribute).FullName);
            Assert.Equal(typeof(void).FullName, field3Type.GetFunctionPointerReturnType().FullName);
            Type[] callingConventions3 = field3Type.GetFunctionPointerCallingConventions();
            Assert.Contains(callingConventions3, t => t.FullName == typeof(CallConvStdcall).FullName);
        }

        [Fact]
        public void ConsumeFunctionPointerFields()
        {
            // public unsafe class Container
            // {
            //     public static delegate*<int, int, int> Method;
            // 
            //     public static int Add(int a, int b) => a + b;
            //     public static void Init() => Method = &Add;
            // }

            TempFile assembly1Path = TempFile.Create();
            PersistedAssemblyBuilder assembly1 = new(new AssemblyName("Assembly1"), typeof(object).Assembly);
            ModuleBuilder mod1 = assembly1.DefineDynamicModule("Module1");
            TypeBuilder containerType = mod1.DefineType("Container", TypeAttributes.Public | TypeAttributes.Class);
            FieldBuilder methodField = containerType.DefineField("Method", typeof(delegate*<int, int, int>), FieldAttributes.Public | FieldAttributes.Static);
            MethodBuilder addMethod = containerType.DefineMethod("Add", MethodAttributes.Public | MethodAttributes.Static);
            addMethod.SetParameters(typeof(int), typeof(int));
            addMethod.SetReturnType(typeof(int));
            ILGenerator addMethodIL = addMethod.GetILGenerator();
            addMethodIL.Emit(OpCodes.Ldarg_0);
            addMethodIL.Emit(OpCodes.Ldarg_1);
            addMethodIL.Emit(OpCodes.Add);
            addMethodIL.Emit(OpCodes.Ret);
            MethodBuilder initMethod = containerType.DefineMethod("Init", MethodAttributes.Public | MethodAttributes.Static);
            initMethod.SetReturnType(typeof(void));
            ILGenerator initMethodIL = initMethod.GetILGenerator();
            initMethodIL.Emit(OpCodes.Ldftn, addMethod);
            initMethodIL.Emit(OpCodes.Stsfld, methodField);
            initMethodIL.Emit(OpCodes.Ret);
            containerType.CreateType();
            assembly1.Save(assembly1Path.Path);

            // class Program
            // {
            //     public static int Main()
            //     {
            //         Container.Init();
            //         return Container.Method(2, 3);
            //     }
            // }

            TestAssemblyLoadContext context = new();

            TempFile assembly2Path = TempFile.Create();
            Assembly assembly1FromDisk = context.LoadFromAssemblyPath(assembly1Path.Path);
            PersistedAssemblyBuilder assembly2 = new(new AssemblyName("Assembly2"), typeof(object).Assembly);
            ModuleBuilder mod2 = assembly2.DefineDynamicModule("Module2");
            TypeBuilder programType = mod2.DefineType("Program");
            MethodBuilder mainMethod = programType.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static);
            mainMethod.SetReturnType(typeof(int));
            ILGenerator il = mainMethod.GetILGenerator();
            il.Emit(OpCodes.Ldsfld, typeof(ClassWithFunctionPointerFields).GetField("field1"));
            il.Emit(OpCodes.Pop);
            // References to fields with unmanaged calling convention are broken
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/120909")]
            // il.Emit(OpCodes.Ldsfld, typeof(ClassWithFunctionPointerFields).GetField("field2"));
            // il.Emit(OpCodes.Pop);
            // il.Emit(OpCodes.Ldsfld, typeof(ClassWithFunctionPointerFields).GetField("field3"));
            // il.Emit(OpCodes.Pop);
            // il.Emit(OpCodes.Ldsfld, typeof(ClassWithFunctionPointerFields).GetField("field4"));
            // il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Call, assembly1FromDisk.GetType("Container").GetMethod("Init"));
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Ldc_I4_3);
            il.Emit(OpCodes.Ldsfld, assembly1FromDisk.GetType("Container").GetField("Method"));
            il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(int), [typeof(int), typeof(int)], null);
            il.Emit(OpCodes.Ret);
            programType.CreateType();
            assembly2.Save(assembly2Path.Path);

            Assembly assembly2FromDisk = context.LoadFromAssemblyPath(assembly2Path.Path);
            int result = (int)assembly2FromDisk.GetType("Program").GetMethod("Main").Invoke(null, null);
            Assert.Equal(5, result);

            assembly1Path.Dispose();
            assembly2Path.Dispose();
        }
    }

    // Test Types
    public class BaseType<T> { }

    public interface INoMethod
    {
    }

    public interface IMultipleMethod
    {
        string Func(int a, string b);
        IOneMethod MoreFunc();
        StructWithFields DoIExist(int a, string b, bool c);
        void BuildAPerpetualMotionMachine();
    }

    internal interface IAccess
    {
        public Version BuildAI(double field);
        public int DisableRogueAI();
    }

    public interface IOneMethod
    {
        object Func(string a, short b);
    }

    public struct EmptyStruct
    {
    }

    public struct StructWithFields
    {
        public int field1;
        public string field2;
    }

    public class EmptyTestClass
    {
    }

    public class ClassWithFields : EmptyTestClass
    {
        public EmptyTestClass field1;
        public byte field2;
    }

    public unsafe class ClassWithFunctionPointerFields
    {
        public static delegate*<ClassWithFunctionPointerFields> field1;
        public static delegate* unmanaged<int> field2;
        public static delegate* unmanaged[Cdecl]<Guid> field3;
        public static delegate* unmanaged[Cdecl, SuppressGCTransition]<Vector<int>, Vector<int>> field4;
    }
}
