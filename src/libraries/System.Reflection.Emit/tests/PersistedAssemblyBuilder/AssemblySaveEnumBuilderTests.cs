﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySaveEnumBuilderTests
    {
        private static AssemblyName PopulateAssemblyName()
        {
            AssemblyName assemblyName = new AssemblyName("MyDynamicAssembly");
            assemblyName.Version = new Version("7.0.0.0");
            assemblyName.CultureInfo = Globalization.CultureInfo.InvariantCulture;
            return assemblyName;
        }

        public static IEnumerable<object[]> DefineLiteral_TestData()
        {
            yield return new object[] { typeof(byte), (byte)0 };
            yield return new object[] { typeof(byte), (byte)1 };

            yield return new object[] { typeof(sbyte), (sbyte)0 };
            yield return new object[] { typeof(sbyte), (sbyte)1 };

            yield return new object[] { typeof(ushort), (ushort)0 };
            yield return new object[] { typeof(ushort), (ushort)1 };

            yield return new object[] { typeof(short), (short)0 };
            yield return new object[] { typeof(short), (short)1 };

            yield return new object[] { typeof(uint), (uint)0 };
            yield return new object[] { typeof(uint), (uint)1 };

            yield return new object[] { typeof(int), 0 };
            yield return new object[] { typeof(int), 1 };

            yield return new object[] { typeof(ulong), (ulong)0 };
            yield return new object[] { typeof(ulong), (ulong)1 };

            yield return new object[] { typeof(long), (long)0 };
            yield return new object[] { typeof(long), (long)1 };

            yield return new object[] { typeof(char), (char)0 };
            yield return new object[] { typeof(char), (char)1 };

            yield return new object[] { typeof(bool), true };
            yield return new object[] { typeof(bool), false };

            yield return new object[] { typeof(float), 0f };
            yield return new object[] { typeof(float), 1.1f };

            yield return new object[] { typeof(double), 0d };
            yield return new object[] { typeof(double), 1.1d };
        }

        [Theory]
        [MemberData(nameof(DefineLiteral_TestData))]
        public void DefineLiteral(Type underlyingType, object literalValue)
        {
            using (TempFile file = TempFile.Create())
            {
                EnumBuilder enumBuilder = CreateAssemblyAndDefineEnum(out PersistedAssemblyBuilder assemblyBuilder, out TypeBuilder type, underlyingType);
                FieldBuilder literal = enumBuilder.DefineLiteral("FieldOne", literalValue);
                enumBuilder.CreateTypeInfo();
                type.CreateTypeInfo();
                assemblyBuilder.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Module moduleFromDisk = assemblyFromDisk.Modules.First();
                    Type testEnum = moduleFromDisk.GetType("TestEnum");

                    Assert.True(testEnum.IsEnum);
                    AssemblySaveTools.AssertTypeProperties(enumBuilder, testEnum);
                    Assert.Equal(underlyingType.FullName, testEnum.GetEnumUnderlyingType().FullName);

                    FieldInfo testField = testEnum.GetField("FieldOne");
                    Assert.Equal(enumBuilder.Name, testField.DeclaringType.Name);
                    Assert.Equal(FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, literal.Attributes);
                    Assert.Equal(enumBuilder.AsType().FullName, testField.FieldType.FullName);
                }
            }
        }

        [Theory]
        [InlineData(0, "TestEnum[]")]
        [InlineData(1, "TestEnum[]")]
        [InlineData(2, "TestEnum[,]")]
        [InlineData(3, "TestEnum[,,]")]
        public void SaveArrayTypeSignature(int rank, string name)
        {
            using (TempFile file = TempFile.Create())
            {
                EnumBuilder enumBuilder = CreateAssemblyAndDefineEnum(out PersistedAssemblyBuilder ab, out TypeBuilder tb);
                Type arrayType = rank == 0 ? enumBuilder.MakeArrayType() : enumBuilder.MakeArrayType(rank);
                MethodBuilder mb = tb.DefineMethod("TestMethod", MethodAttributes.Public);
                mb.SetReturnType(arrayType);
                mb.SetParameters(new Type[] { typeof(INoMethod), arrayType });
                mb.GetILGenerator().Emit(OpCodes.Ret);
                enumBuilder.CreateType();
                tb.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type testType = mlc.LoadFromAssemblyPath(file.Path).Modules.First().GetType("TestInterface");
                    MethodInfo testMethod = testType.GetMethod("TestMethod");

                    AssertArrayTypeSignature(rank, name, testMethod.ReturnType);
                    AssertArrayTypeSignature(rank, name, testMethod.GetParameters()[1].ParameterType);
                }
            }
        }

        private EnumBuilder CreateAssemblyAndDefineEnum(out PersistedAssemblyBuilder assemblyBuilder,
            out TypeBuilder type, Type? underlyingType = null)
        {
            assemblyBuilder = AssemblySaveTools.PopulateAssemblyBuilder(PopulateAssemblyName());
            ModuleBuilder mb = assemblyBuilder.DefineDynamicModule("My Module");
            type = mb.DefineType("TestInterface", TypeAttributes.Interface | TypeAttributes.Abstract);
            return mb.DefineEnum("TestEnum", TypeAttributes.Public, underlyingType == null ? typeof(int) : underlyingType);
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
                EnumBuilder eb = CreateAssemblyAndDefineEnum(out PersistedAssemblyBuilder assemblyBuilder, out TypeBuilder tb);
                Type byrefType = eb.MakeByRefType();
                MethodBuilder mb = tb.DefineMethod("TestMethod", MethodAttributes.Public);
                mb.SetReturnType(byrefType);
                mb.SetParameters(new Type[] { typeof(INoMethod), byrefType });
                mb.GetILGenerator().Emit(OpCodes.Ret);
                eb.CreateType();
                tb.CreateType();
                assemblyBuilder.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type testType = mlc.LoadFromAssemblyPath(file.Path).Modules.First().GetType("TestInterface");
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
            Assert.Equal("TestEnum&", byrefParam.Name);
        }

        [Fact]
        public void SavePointerTypeSignature()
        {
            using (TempFile file = TempFile.Create())
            {
                EnumBuilder eb = CreateAssemblyAndDefineEnum(out PersistedAssemblyBuilder assemblyBuilder, out TypeBuilder tb);
                Type pointerType = eb.MakePointerType();
                MethodBuilder mb = tb.DefineMethod("TestMethod", MethodAttributes.Public);
                mb.SetReturnType(pointerType);
                mb.SetParameters(new Type[] { typeof(INoMethod), pointerType });
                mb.GetILGenerator().Emit(OpCodes.Ret);
                eb.CreateType();
                tb.CreateType();
                assemblyBuilder.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type testType = mlc.LoadFromAssemblyPath(file.Path).Modules.First().GetType("TestInterface");
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
            Assert.Equal("TestEnum*", testType.Name);
        }

        public enum Test
        {
            First,
            Second,
            Third
        }

        [Fact]
        public void EnumTypeField_DefaultValueShouldMatchUnderlyingType()
        {
            using (TempFile file = TempFile.Create())
            {
                PersistedAssemblyBuilder assemblyBuilder = AssemblySaveTools.PopulateAssemblyBuilder(PopulateAssemblyName());
                ModuleBuilder mb = assemblyBuilder.DefineDynamicModule("My Module");
                TypeBuilder tb = mb.DefineType("TestType", TypeAttributes.Class | TypeAttributes.Public);
                EnumBuilder eb =  mb.DefineEnum("TestEnum", TypeAttributes.Public, typeof(int));
                FieldBuilder literal = eb.DefineLiteral("FieldOne", 1);
                FieldBuilder literal2 = eb.DefineLiteral("FieldTwo", 2);

                FieldBuilder field  = tb.DefineField("EnumField1", eb, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal);
                field.SetConstant(2);

                FieldBuilder field2 = tb.DefineField("EnumField2", typeof(Test), FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal);
                field2.SetConstant(Test.Third);

                tb.CreateType();
                eb.CreateType();

                assemblyBuilder.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Module moduleFromDisk = mlc.LoadFromAssemblyPath(file.Path).Modules.First();
                    Type testType = moduleFromDisk.GetType("TestType");
                    Type enumType = moduleFromDisk.GetType("TestEnum");
                    FieldInfo testField1 = testType.GetField("EnumField1");
                    FieldInfo testField2 = testType.GetField("EnumField2");

                    Assert.True(testField1.FieldType.IsEnum);
                    Assert.True(testField2.FieldType.IsEnum);
                    Assert.Equal(enumType.FullName, testField1.FieldType.FullName);
                    Assert.Equal(typeof(Test).FullName, testField2.FieldType.FullName);
                    Assert.Equal(2, testField1.GetRawConstantValue());
                    Assert.Equal(Test.Third, (Test)testField2.GetRawConstantValue());
                }
            }
        }
    }
}
