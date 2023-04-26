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
                AssemblyName aNameFromDisk = assemblyFromDisk.GetName();

                // Test AssemblyName properties
                Assert.Equal(s_assemblyName.Name, aNameFromDisk.Name);
                Assert.Equal(s_assemblyName.Version, aNameFromDisk.Version);
                Assert.Equal(s_assemblyName.CultureInfo, aNameFromDisk.CultureInfo);
                Assert.Equal(s_assemblyName.CultureName, aNameFromDisk.CultureName);
                Assert.Equal(s_assemblyName.ContentType, aNameFromDisk.ContentType);
                // Runtime assemblies adding AssemblyNameFlags.PublicKey in Assembly.GetName() overloads
                Assert.Equal(s_assemblyName.Flags | AssemblyNameFlags.PublicKey, aNameFromDisk.Flags);
                Assert.Empty(assemblyFromDisk.GetTypes());

                Module moduleFromDisk = assemblyFromDisk.Modules.FirstOrDefault();

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
                Assembly assemblyFromDisk = WriteAndLoadAssembly(types, file.Path);

                AssertTypesAndTypeMembers(types, assemblyFromDisk.Modules.First().GetTypes());
            }
        }

        private static void AssertTypesAndTypeMembers(Type[] types, Type[] typesFromDisk)
        {
            Assert.Equal(types.Length, typesFromDisk.Length);

            for (int i = 0; i < types.Length; i++)
            {
                Type sourceType = types[i];
                Type typeFromDisk = typesFromDisk[i];

                AssertTypeProperties(sourceType, typeFromDisk);
                AssertMethods(sourceType.GetMethods(), typeFromDisk.GetMethods());
                AssertFields(sourceType.GetFields(), typeFromDisk.GetFields());
            }
        }

        private static void AssertFields(FieldInfo[] declaredFields, FieldInfo[] fieldsFromDisk)
        {
            Assert.Equal(declaredFields.Length, fieldsFromDisk.Length);

            for (int j = 0; j < declaredFields.Length; j++)
            {
                FieldInfo sourceField = declaredFields[j];
                FieldInfo fieldFromDisk = fieldsFromDisk[j];

                Assert.Equal(sourceField.Name, fieldFromDisk.Name);
                Assert.Equal(sourceField.Attributes, fieldFromDisk.Attributes);
                Assert.Equal(sourceField.FieldType.FullName, fieldFromDisk.FieldType.FullName);
            }
        }

        private static void AssertMethods(MethodInfo[] sourceMethods, MethodInfo[] methodsFromDisk)
        {
            Assert.Equal(sourceMethods.Length, methodsFromDisk.Length);

            for (int j = 0; j < sourceMethods.Length; j++)
            {
                MethodInfo sourceMethod = sourceMethods[j];
                MethodInfo methodFromDisk = methodsFromDisk[j];

                Assert.Equal(sourceMethod.Name, methodFromDisk.Name);
                Assert.Equal(sourceMethod.Attributes, methodFromDisk.Attributes);
                Assert.Equal(sourceMethod.ReturnType.FullName, methodFromDisk.ReturnType.FullName);
            }
        }

        private static void AssertTypeProperties(Type sourceType, Type typeFromDisk)
        {
            Assert.Equal(sourceType.Name, typeFromDisk.Name);
            Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
            Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);
            Assert.Equal(sourceType.IsInterface, typeFromDisk.IsInterface);
            Assert.Equal(sourceType.IsValueType, typeFromDisk.IsValueType);
        }

        [Theory]
        [MemberData(nameof(VariousInterfacesStructsTestData))]
        public void WriteAssemblyWithVariousTypesToStreamAndReadBackTest(Type[] types)
        {
            using (var stream = new MemoryStream())
            {
                AssemblyTools.WriteAssemblyToStream(s_assemblyName, types, stream);
                Assembly assemblyFromStream = AssemblyTools.LoadAssemblyFromStream(stream);

                AssertTypesAndTypeMembers(types, assemblyFromStream.Modules.First().GetTypes());
            }
        }

        [Fact]
        public void CreateMembersThatUsesTypeLoadedFromCoreAssemblyTest()
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
                Module moduleFromDisk = assemblyFromDisk.Modules.First();

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

    // Test Types
    public interface INoMethod
    {
    }

    public interface IMultipleMethod
    {
        string Func();
        IOneMethod MoreFunc();
        StructWithFields DoIExist();
        void BuildAPerpetualMotionMachine();
    }

    internal interface IAccess
    {
        public Version BuildAI();
        public int DisableRogueAI();
    }

    public interface IOneMethod
    {
        object Func();
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
}
