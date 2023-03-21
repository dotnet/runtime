// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Experiment.Tests
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
        public void EmptyAssemblyTest()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyTools.WriteAssemblyToDisk(s_assemblyName, Type.EmptyTypes, file.Path);

                Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(file.Path);
                Assert.NotNull(assemblyFromDisk);

                AssemblyName aNameFromDisk = assemblyFromDisk.GetName();

                // Test AssemblyName properties
                Assert.Equal(s_assemblyName.Name, aNameFromDisk.Name);
                Assert.Equal(s_assemblyName.Version, aNameFromDisk.Version);
                Assert.Equal(s_assemblyName.CultureInfo, aNameFromDisk.CultureInfo);
                Assert.Equal(s_assemblyName.CultureName, aNameFromDisk.CultureName);
                Assert.Equal(s_assemblyName.Flags | AssemblyNameFlags.PublicKey, aNameFromDisk.Flags); // Not sure AssemblyNameFlags.PublicKey is expected

                Module moduleFromDisk = assemblyFromDisk.Modules.First();

                Assert.NotNull(moduleFromDisk);
                Assert.Empty(moduleFromDisk.GetTypes());
            }
        }

        [Fact]
        public void OneInterfaceWithMethodsUsingStream()
        {
            Type[] types = new Type[] { typeof(IMultipleMethod) };
            using MemoryStream stream = new MemoryStream();

            // Generate DLL from these and save it to Stream
            AssemblyTools.WriteAssemblyToStream(s_assemblyName, types, stream);

            // Read said assembly back from Stream using MetadataLoadContext
            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(stream);

            // AssemblyName
            Assert.NotNull(assemblyFromDisk);
            Assert.Equal(s_assemblyName.Name, assemblyFromDisk.GetName().Name);

            Module moduleFromDisk = assemblyFromDisk.Modules.First();
            Type[] typesFromDisk = moduleFromDisk.GetTypes();

            Assert.NotNull(moduleFromDisk);
            Assert.Equal(1, typesFromDisk.Length);

            Type sourceType = types[0];
            Type typeFromDisk = typesFromDisk[0];

            Assert.Equal(sourceType.Name, typeFromDisk.Name);
            Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
            Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);

            MethodInfo[] sourceMethods = sourceType.GetMethods();
            MethodInfo[] methodsFromDisk = typeFromDisk.GetMethods();

            // Method comparison
            for (int j = 0; j < sourceMethods.Length; j++)
            {
                MethodInfo sourceMethod = sourceMethods[j];
                MethodInfo methodFromDisk = methodsFromDisk[j];

                Assert.Equal(sourceMethod.Name, methodFromDisk.Name);
                Assert.Equal(sourceMethod.Attributes, methodFromDisk.Attributes);
                Assert.Equal(sourceMethod.ReturnType.FullName, methodFromDisk.ReturnType.FullName);
            }
        }

        [Fact]
        public void OneInterfaceWithoutMethods()
        {
            using (TempFile file = TempFile.Create())
            {
                Type[] types = new Type[] { typeof(INoMethod) };

                AssemblyTools.WriteAssemblyToDisk(s_assemblyName, types, file.Path);

                Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(file.Path);

                Assert.NotNull(assemblyFromDisk);

                Module moduleFromDisk = assemblyFromDisk.Modules.First();
                Type[] typesFromDisk = moduleFromDisk.GetTypes();

                Assert.NotNull(moduleFromDisk);
                Assert.Equal(1, typesFromDisk.Length);

                Type sourceType = types[0];
                Type typeFromDisk = typesFromDisk[0];

                Assert.Equal(sourceType.Name, typeFromDisk.Name);
                Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);

                Assert.Empty(typeFromDisk.GetMethods());
            }
        }

        [Fact]
        public void TwoEmptyInterfaces()
        {
            using (TempFile file = TempFile.Create())
            {
                Type[] types = new Type[] { typeof(INoMethod), typeof(INoMethod2) };

                AssemblyTools.WriteAssemblyToDisk(s_assemblyName, types, file.Path);

                Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(file.Path);

                Assert.NotNull(assemblyFromDisk);

                Module moduleFromDisk = assemblyFromDisk.Modules.First();
                Type[] typesFromDisk = moduleFromDisk.GetTypes();

                Assert.NotNull(moduleFromDisk);
                Assert.Equal(2, typesFromDisk.Length);

                // Type comparisons
                for (int i = 0; i < types.Length; i++)
                {
                    Type sourceType = types[i];
                    Type typeFromDisk = typesFromDisk[i];

                    Assert.Equal(sourceType.Name, typeFromDisk.Name);
                    Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                    Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);

                    Assert.Empty(typeFromDisk.GetMethods());
                }
            }
        }

        [Fact]
        public void TwoInterfaceOneMethod()
        {
            using (TempFile file = TempFile.Create())
            {
                Type[] types = new Type[] { typeof(INoMethod), typeof(IOneMethod) };

                AssemblyTools.WriteAssemblyToDisk(s_assemblyName, types, file.Path);

                Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(file.Path);

                Assert.NotNull(assemblyFromDisk);

                Module moduleFromDisk = assemblyFromDisk.Modules.First();
                Type[] typesFromDisk = moduleFromDisk.GetTypes();

                Assert.NotNull(moduleFromDisk);
                Assert.Equal(2, typesFromDisk.Length);

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

                    Assert.True(methodsFromDisk.Length <= 1 );

                    // Method comparisons
                    if (sourceMethods.Length > 0)
                    {
                        MethodInfo sourceMethod = sourceMethods[0];
                        MethodInfo methodFromDisk = methodsFromDisk[0];

                        Assert.Equal(sourceMethod.Name, methodFromDisk.Name);
                        Assert.Equal(sourceMethod.Attributes, methodFromDisk.Attributes);
                        Assert.Equal(sourceMethod.ReturnType.FullName, methodFromDisk.ReturnType.FullName);
                    }
                }
            }
        }

        [Fact]
        public void TwoInterfaceManyMethodsThenNone()
        {
            using (TempFile file = TempFile.Create())
            {
                Type[] types = new Type[] { typeof(IMultipleMethod), typeof(INoMethod2) };

                AssemblyTools.WriteAssemblyToDisk(s_assemblyName, types, file.Path);

                Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(file.Path);

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

        [Fact]
        public void VariousInterfaces()
        {
            using (TempFile file = TempFile.Create())
            {
                Type[] types = new Type[] { typeof(IMultipleMethod), typeof(INoMethod2), typeof(IAccess), typeof(IOneMethod), typeof(INoMethod) };

                AssemblyTools.WriteAssemblyToDisk(s_assemblyName, types, file.Path);

                Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(file.Path);

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

        [Fact]
        public void AnEmptyStruct()
        {
            using (TempFile file = TempFile.Create())
            {
                Type[] types = new Type[] { typeof(EmptyStruct) };

                AssemblyTools.WriteAssemblyToDisk(s_assemblyName, types, file.Path);

                Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(file.Path);

                Assert.NotNull(assemblyFromDisk);

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

                    Assert.Empty(sourceType.GetMethods(BindingFlags.DeclaredOnly));
                    Assert.Empty(sourceType.GetFields(BindingFlags.DeclaredOnly));
                    Assert.Empty(sourceType.GetProperties(BindingFlags.DeclaredOnly));
                }
            }
        }

        [Fact]
        public void EmptyStructAndStructWithAFieldWriteToStream()
        {
            Type[] types = new Type[] { typeof(EmptyStruct), typeof(StructWithField) };

            using var stream = new MemoryStream();
            AssemblyTools.WriteAssemblyToStream(s_assemblyName, types, stream);

            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(stream);

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
