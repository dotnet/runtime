// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySaveTestsWithVariousTypes
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
                Assembly assemblyFromDisk = WriteAndLoadAssembly(Type.EmptyTypes, file.Path);
                Assert.NotNull(assemblyFromDisk);

                AssemblyName aNameFromDisk = assemblyFromDisk.GetName();

                // Test AssemblyName properties
                Assert.Equal(s_assemblyName.Name, aNameFromDisk.Name);
                Assert.Equal(s_assemblyName.Version, aNameFromDisk.Version);
                Assert.Equal(s_assemblyName.CultureInfo, aNameFromDisk.CultureInfo);
                Assert.Equal(s_assemblyName.CultureName, aNameFromDisk.CultureName);
                Assert.Equal(s_assemblyName.ContentType, aNameFromDisk.ContentType);
                // Runtime assemblies adding AssemblyNameFlags.PublicKey in Assembly.GetName() overloads
                Assert.Equal(s_assemblyName.Flags | AssemblyNameFlags.PublicKey, aNameFromDisk.Flags);

                Module moduleFromDisk = assemblyFromDisk.Modules.First();

                Assert.NotNull(moduleFromDisk);
                Assert.Equal(s_assemblyName.Name, moduleFromDisk.ScopeName);
                Assert.Empty(moduleFromDisk.GetTypes());
            }
        }

        private static Assembly WriteAndLoadAssembly(Type[] types, string filePath)
        {
            AssemblyTools.WriteAssemblyToDisk(s_assemblyName, types, filePath);

            return AssemblyTools.LoadAssemblyFromPath(filePath);
        }

        public static IEnumerable<object[]> VariousInterfacesTestData()
        {
            yield return new object[] { new Type[] { typeof(INoMethod) } };
            yield return new object[] { new Type[] { typeof(IMultipleMethod) } };
            yield return new object[] { new Type[] { typeof(INoMethod), typeof(INoMethod2) } };
            yield return new object[] { new Type[] { typeof(INoMethod), typeof(IOneMethod) } };
            yield return new object[] { new Type[] { typeof(IMultipleMethod), typeof(INoMethod2) } };
            yield return new object[] { new Type[] { typeof(IMultipleMethod), typeof(INoMethod2), typeof(IAccess), typeof(IOneMethod), typeof(INoMethod) } };
        }

        [Theory]
        [MemberData(nameof(VariousInterfacesTestData))]
        public void WriteVariousInterfacesToFileTest(Type[] types)
        {
            using (TempFile file = TempFile.Create())
            {
                Assembly assemblyFromDisk = WriteAndLoadAssembly(types, file.Path);
                Assert.NotNull(assemblyFromDisk);

                Assert.NotNull(assemblyFromDisk);

                Module moduleFromDisk = assemblyFromDisk.Modules.First();
                Type[] typesFromDisk = moduleFromDisk.GetTypes();

                Assert.NotNull(moduleFromDisk);
                Assert.Equal(types.Length, typesFromDisk.Length);

                // Type comparisons
                for (int i = 0; i < types.Length; i++)
                {
                    Type sourceType = types[i];
                    Type typeFromDisk = typesFromDisk[i];

                    Assert.Equal(sourceType.Name, typeFromDisk.Name);
                    Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                    Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);

                    MethodInfo[] sourceMethods = sourceType.GetMethods();
                    MethodInfo[] methodsFromDisk = typeFromDisk.GetMethods();

                    Assert.Equal(sourceMethods.Length, methodsFromDisk.Length);

                    // Method comparisons
                    for (int j = 0; j < sourceMethods.Length; j++)
                    {
                        MethodInfo sourceMethod = sourceMethods[j];
                        MethodInfo methodFromDisk = methodsFromDisk[j];

                        Assert.Equal(sourceMethod.Name, methodFromDisk.Name);
                        Assert.Equal(sourceMethod.Attributes, methodFromDisk.Attributes);
                        Assert.Equal(sourceMethod.ReturnType.FullName, methodFromDisk.ReturnType.FullName);
                    }
                }
            }
        }

        public static IEnumerable<object[]> VariousStructsTestData()
        {
            yield return new object[] { new Type[] { typeof(EmptyStruct) } };
            yield return new object[] { new Type[] { typeof(StructWithField) } };
            yield return new object[] { new Type[] { typeof(EmptyStruct), typeof(StructWithField) } };
            yield return new object[] { new Type[] { typeof(StructWithField), typeof(EmptyStruct) } };
        }

        [Theory]
        [MemberData(nameof(VariousStructsTestData))]
        public void WriteVariousStructsToStream(Type[] types)
        {
            using var stream = new MemoryStream();
            AssemblyTools.WriteAssemblyToStream(s_assemblyName, types, stream);

            Assembly assemblyFromDisk = AssemblyTools.LoadAssemblyFromStream(stream);

            Assert.NotNull(assemblyFromDisk);
            Assert.Equal(s_assemblyName.Name, assemblyFromDisk.GetName().Name);

            Module moduleFromDisk = assemblyFromDisk.Modules.First();
            Type[] typesFromDisk = moduleFromDisk.GetTypes();

            Assert.NotNull(moduleFromDisk);
            Assert.Equal(types.Length, typesFromDisk.Length);

            for (int i = 0; i < types.Length; i++)
            {
                Type sourceType = types[i];
                Type typeFromDisk = typesFromDisk[i];

                Assert.True(sourceType.IsValueType);
                Assert.Equal(sourceType.Name, typeFromDisk.Name);
                Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);
                Assert.Empty(typeFromDisk.GetMethods(BindingFlags.DeclaredOnly));

                FieldInfo[] declaredFields = sourceType.GetFields();
                FieldInfo[] fieldsFromDisk = typeFromDisk.GetFields();

                Assert.Equal(declaredFields.Length, fieldsFromDisk.Length);

                // Field comparison
                for (int j = 0; j < declaredFields.Length; j++)
                {
                    FieldInfo sourceField = declaredFields[j];
                    FieldInfo fieldFromDisk = fieldsFromDisk[j];

                    Assert.Equal(sourceField.Name, fieldFromDisk.Name);
                    Assert.Equal(sourceField.Attributes, fieldFromDisk.Attributes);
                    Assert.Equal(sourceField.FieldType.FullName, fieldFromDisk.FieldType.FullName);
                }
            }
        }

        [Fact]
        public void MethodReturnTypeLoadedFromCoreAssemblyTest()
        {
            using (TempFile file = TempFile.Create())
            {
                MethodInfo defineDynamicAssemblyMethod = AssemblyTools.PopulateMethods(typeof(string), out MethodInfo saveMethod);
                AssemblyBuilder assemblyBuilder = (AssemblyBuilder)defineDynamicAssemblyMethod.Invoke(null,
                    new object[] { s_assemblyName, typeof(object).Assembly, null });

                ModuleBuilder mb = assemblyBuilder.DefineDynamicModule("My Module");
                TypeBuilder tb = mb.DefineType("TestInterface", TypeAttributes.Interface | TypeAttributes.Abstract);
                tb.DefineMethod("TestMethod", MethodAttributes.Public);

                saveMethod.Invoke(assemblyBuilder, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblyTools.LoadAssemblyFromPath(file.Path);
                Assert.NotNull(assemblyFromDisk);

                Module moduleFromDisk = assemblyFromDisk.Modules.First();

                Assert.NotNull(moduleFromDisk);
                Assert.Equal("My Module", moduleFromDisk.ScopeName);
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

    // Test Interfaces
    public interface INoMethod
    {
    }

    public interface INoMethod2
    {
    }

    public interface IMultipleMethod
    {
        string Func();
        bool MoreFunc();
        double DoIExist();
        void BuildAPerpetualMotionMachine();
    }

    internal interface IAccess
    {
        public int BuildAI();
        public int DisableRogueAI();
    }

    public interface IOneMethod
    {
        string Func();
    }

    public struct EmptyStruct
    {
    }

    public struct StructWithField
    {
        public int field;
    }
}
