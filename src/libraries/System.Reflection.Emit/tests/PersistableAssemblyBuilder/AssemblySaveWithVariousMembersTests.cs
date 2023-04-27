// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySaveWithVariousMembersTests
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

                Assert.Empty(assemblyFromDisk.GetTypes());
                AssemblyTools.AssertAssemblyNameAndModule(s_assemblyName, assemblyFromDisk.GetName(), assemblyFromDisk.Modules.FirstOrDefault());
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

                AssemblyTools.AssertTypeProperties(sourceType, typeFromDisk);
                AssemblyTools.AssertMethods(sourceType.GetMethods(), typeFromDisk.GetMethods());
                AssemblyTools.AssertFields(sourceType.GetFields(), typeFromDisk.GetFields());
            }
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
                AssemblyBuilder assemblyBuilder = AssemblyTools.PopulateAssemblyBuilderAndSaveMethod(
                    s_assemblyName, null, typeof(string), out MethodInfo saveMethod);

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
