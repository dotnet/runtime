// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Experiment.Tests
{
    public class AssemblySaveTestsWithVariousTypes : IDisposable
    {
        internal string _fileLocation;
        private static readonly AssemblyName s_assemblyName = new AssemblyName("MyDynamicAssembly")
        {
            Version = new Version("1.2.3.4"),
            CultureInfo = Globalization.CultureInfo.CurrentCulture,
            ContentType = AssemblyContentType.WindowsRuntime,
            Flags = AssemblyNameFlags.EnableJITcompileTracking | AssemblyNameFlags.Retargetable,
        };

        public AssemblySaveTestsWithVariousTypes()
        {
            const bool keepFiles = true;
            TempFileCollection tfc;
            Directory.CreateDirectory("testDir");
            tfc = new TempFileCollection("testDir", false);
            _fileLocation = tfc.AddExtension("dll", keepFiles);
        }

        public void Dispose()
        { }

        [Fact]
        public void EmptyAssemblyTest()
        {
            AssemblyTools.WriteAssemblyToDisk(s_assemblyName, Type.EmptyTypes, _fileLocation);

            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(_fileLocation);
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

        [Fact]
        public void OneInterfaceWithMethodsUsingStream()
        {
            Type[] types = new Type[] { typeof(IMultipleMethod) };
            using MemoryStream stream = new MemoryStream();

            // Generate DLL from these and save it to Strem
            AssemblyTools.WriteAssemblyToStream(s_assemblyName, types, stream);

            // Read said assembly back from Stream using MetadataLoadContext
            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(stream);

            // AssemblyName
            Assert.NotNull(assemblyFromDisk);
            Assert.Equal(s_assemblyName.Name, assemblyFromDisk.GetName().Name);

            Module moduleFromDisk = assemblyFromDisk.Modules.First();

            Assert.NotNull(moduleFromDisk);
            // Assert.Equal(assemblyName.Name, moduleFromDisk.ScopeName); Custom module not supported
            Assert.Equal(1, moduleFromDisk.GetTypes().Length);

            // Type comparisons
            for (int i = 0; i < types.Length; i++)
            {
                Type sourceType = types[i];
                Type typeFromDisk = moduleFromDisk.GetTypes()[i];

                Assert.Equal(sourceType.Name, typeFromDisk.Name);
                Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);

                // Method comparison
                for (int j = 0; j < sourceType.GetMethods().Length; j++)
                {
                    MethodInfo sourceMethod = sourceType.GetMethods()[j];
                    MethodInfo methodFromDisk = typeFromDisk.GetMethods()[j];

                    Assert.Equal(sourceMethod.Name, methodFromDisk.Name);
                    Assert.Equal(sourceMethod.Attributes, methodFromDisk.Attributes);
                    Assert.Equal(sourceMethod.ReturnType.FullName, methodFromDisk.ReturnType.FullName);

                    // Parameter comparison
                    for (int k = 0; k < sourceMethod.GetParameters().Length; k++)
                    {
                        ParameterInfo sourceParameter = sourceMethod.GetParameters()[k];
                        ParameterInfo parameterFromDisk = methodFromDisk.GetParameters()[k];
                        Assert.Equal(sourceParameter.ParameterType.FullName, parameterFromDisk.ParameterType.FullName);
                    }
                }
            }
        }

        [Fact]
        public void OneInterfaceWithoutMethods()
        {
            Type[] types = new Type[] { typeof(INoMethod) };

            AssemblyTools.WriteAssemblyToDisk(s_assemblyName, types, _fileLocation);

            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(_fileLocation);

            Assert.NotNull(assemblyFromDisk);

            Module moduleFromDisk = assemblyFromDisk.Modules.First();

            Assert.NotNull(moduleFromDisk);
            Assert.Equal(1, moduleFromDisk.GetTypes().Length);

            // Type comparisons
            for (int i = 0; i < types.Length; i++)
            {
                Type sourceType = types[i];
                Type typeFromDisk = moduleFromDisk.GetTypes()[i];

                Assert.Equal(sourceType.Name, typeFromDisk.Name);
                Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);

                Assert.Empty(typeFromDisk.GetMethods());
            }
        }

        [Fact]
        public void EmptyInterfacesBetweenNonEmpty()
        {
            Type[] types = new Type[] { typeof(IAccess), typeof(INoMethod), typeof(INoMethod2), typeof(IMultipleMethod) };

            AssemblyTools.WriteAssemblyToDisk(s_assemblyName, types, _fileLocation);

            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(_fileLocation);

            Assert.NotNull(assemblyFromDisk);

            Module moduleFromDisk = assemblyFromDisk.Modules.First();

            Assert.NotNull(moduleFromDisk);
            Assert.Equal(4, moduleFromDisk.GetTypes().Length);

            // Type comparisons
            for (int i = 0; i < types.Length; i++)
            {
                Type sourceType = types[i];
                Type typeFromDisk = moduleFromDisk.GetTypes()[i];

                Assert.Equal(sourceType.Name, typeFromDisk.Name);
                Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);

                // Method comparison
                for (int j = 0; j < sourceType.GetMethods().Length; j++)
                {
                    MethodInfo sourceMethod = sourceType.GetMethods()[j];
                    MethodInfo methodFromDisk = typeFromDisk.GetMethods()[j];

                    Assert.Equal(sourceMethod.Name, methodFromDisk.Name);
                    Assert.Equal(sourceMethod.Attributes, methodFromDisk.Attributes);
                    Assert.Equal(sourceMethod.ReturnType.FullName, methodFromDisk.ReturnType.FullName);

                    // Parameter comparison
                    for (int k = 0; k < sourceMethod.GetParameters().Length; k++)
                    {
                        ParameterInfo sourceParameter = sourceMethod.GetParameters()[k];
                        ParameterInfo parameterFromDisk = methodFromDisk.GetParameters()[k];
                        Assert.Equal(sourceParameter.ParameterType.FullName, parameterFromDisk.ParameterType.FullName);
                    }
                }
            }
        }

        [Fact]
        public void TwoEmptyInterfaces()
        {
            Type[] types = new Type[] { typeof(INoMethod), typeof(INoMethod2) };

            AssemblyTools.WriteAssemblyToDisk(s_assemblyName, types, _fileLocation);

            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(_fileLocation);

            Assert.NotNull(assemblyFromDisk);

            Module moduleFromDisk = assemblyFromDisk.Modules.First();

            Assert.NotNull(moduleFromDisk);
            Assert.Equal(2, moduleFromDisk.GetTypes().Length);
            
            // Type comparisons
            for (int i = 0; i < types.Length; i++)
            {
                Type sourceType = types[i];
                Type typeFromDisk = moduleFromDisk.GetTypes()[i];

                Assert.Equal(sourceType.Name, typeFromDisk.Name);
                Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);

                Assert.Empty(typeFromDisk.GetMethods());
            }
        }

        [Fact]
        public void TwoInterfaceOneMethod()
        {
            Type[] types = new Type[] { typeof(INoMethod), typeof(IOneMethod) };

            AssemblyTools.WriteAssemblyToDisk(s_assemblyName, types, _fileLocation);

            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(_fileLocation);

            Assert.NotNull(assemblyFromDisk);

            Module moduleFromDisk = assemblyFromDisk.Modules.First();

            Assert.NotNull(moduleFromDisk);
            Assert.Equal(2, moduleFromDisk.GetTypes().Length);

            // Type comparisons
            for (int i = 0; i < types.Length; i++)
            {
                Type sourceType = types[i];
                Type typeFromDisk = moduleFromDisk.GetTypes()[i];

                Assert.Equal(sourceType.Name, typeFromDisk.Name);
                Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);

                // Method comparison
                for (int j = 0; j < sourceType.GetMethods().Length; j++)
                {
                    MethodInfo sourceMethod = sourceType.GetMethods()[j];
                    MethodInfo methodFromDisk = typeFromDisk.GetMethods()[j];

                    Assert.Equal(sourceMethod.Name, methodFromDisk.Name);
                    Assert.Equal(sourceMethod.Attributes, methodFromDisk.Attributes);
                    Assert.Equal(sourceMethod.ReturnType.FullName, methodFromDisk.ReturnType.FullName);

                    // Parameter comparison
                    for (int k = 0; k < sourceMethod.GetParameters().Length; k++)
                    {
                        ParameterInfo sourceParameter = sourceMethod.GetParameters()[k];
                        ParameterInfo parameterFromDisk = methodFromDisk.GetParameters()[k];
                        Assert.Equal(sourceParameter.ParameterType.FullName, parameterFromDisk.ParameterType.FullName);
                    }
                }
            }
        }



        [Fact]
        public void TwoInterfaceManyMethodsThenNone()
        {
            Type[] types = new Type[] { typeof(IMultipleMethod), typeof(INoMethod2) };

            AssemblyTools.WriteAssemblyToDisk(s_assemblyName, types, _fileLocation);

            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(_fileLocation);

            Assert.NotNull(assemblyFromDisk);

            Module moduleFromDisk = assemblyFromDisk.Modules.First();

            Assert.NotNull(moduleFromDisk);
            Assert.Equal(2, moduleFromDisk.GetTypes().Length);

            // Type comparisons
            for (int i = 0; i < types.Length; i++)
            {
                Type sourceType = types[i];
                Type typeFromDisk = moduleFromDisk.GetTypes()[i];

                Assert.Equal(sourceType.Name, typeFromDisk.Name);
                Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);

                // Method comparison
                for (int j = 0; j < sourceType.GetMethods().Length; j++)
                {
                    MethodInfo sourceMethod = sourceType.GetMethods()[j];
                    MethodInfo methodFromDisk = typeFromDisk.GetMethods()[j];

                    Assert.Equal(sourceMethod.Name, methodFromDisk.Name);
                    Assert.Equal(sourceMethod.Attributes, methodFromDisk.Attributes);
                    Assert.Equal(sourceMethod.ReturnType.FullName, methodFromDisk.ReturnType.FullName);

                    // Parameter comparison
                    for (int k = 0; k < sourceMethod.GetParameters().Length; k++)
                    {
                        ParameterInfo sourceParameter = sourceMethod.GetParameters()[k];
                        ParameterInfo parameterFromDisk = methodFromDisk.GetParameters()[k];
                        Assert.Equal(sourceParameter.ParameterType.FullName, parameterFromDisk.ParameterType.FullName);
                    }
                }
            }
        }

        [Fact]
        public void VariousInterfaces()
        {
            Type[] types = new Type[] { typeof(IMultipleMethod), typeof(INoMethod2), typeof(IAccess), typeof(IOneMethod), typeof(INoMethod) };

            AssemblyTools.WriteAssemblyToDisk(s_assemblyName, types, _fileLocation);

            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(_fileLocation);

            Assert.NotNull(assemblyFromDisk);

            Module moduleFromDisk = assemblyFromDisk.Modules.First();

            Assert.NotNull(moduleFromDisk);
            Assert.Equal(5, moduleFromDisk.GetTypes().Length);

            // Type comparisons
            for (int i = 0; i < types.Length; i++)
            {
                Type sourceType = types[i];
                Type typeFromDisk = moduleFromDisk.GetTypes()[i];

                Assert.Equal(sourceType.Name, typeFromDisk.Name);
                Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);

                // Method comparison
                for (int j = 0; j < sourceType.GetMethods().Length; j++)
                {
                    MethodInfo[] sourceMethods = sourceType.GetMethods();
                    MethodInfo[] methodsFromDisk = typeFromDisk.GetMethods();
                    MethodInfo sourceMethod = sourceType.GetMethods()[j];
                    MethodInfo methodFromDisk = typeFromDisk.GetMethods()[j];

                    Assert.Equal(sourceMethod.Name, methodFromDisk.Name);
                    Assert.Equal(sourceMethod.Attributes, methodFromDisk.Attributes);
                    Assert.Equal(sourceMethod.ReturnType.FullName, methodFromDisk.ReturnType.FullName);
                    // Parameter comparison
                    for (int k = 0; k < sourceMethod.GetParameters().Length; k++)
                    {
                        ParameterInfo sourceParamter = sourceMethod.GetParameters()[k];
                        ParameterInfo paramterFromDisk = methodFromDisk.GetParameters()[k];
                        Assert.Equal(sourceParamter.ParameterType.FullName, paramterFromDisk.ParameterType.FullName);
                    }
                }
            }
        }

        [Fact]
        public void AnEmptyStruct()
        {
            Type[] types = new Type[] { typeof(EmptyStruct) };

            AssemblyTools.WriteAssemblyToDisk(s_assemblyName, types, _fileLocation);

            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(_fileLocation);

            Assert.NotNull(assemblyFromDisk);

            Module moduleFromDisk = assemblyFromDisk.Modules.First();

            Assert.NotNull(moduleFromDisk);
            Assert.Equal(1, moduleFromDisk.GetTypes().Length);

            for (int i = 0; i < types.Length; i++)
            {
                Type sourceType = types[i];
                Type typeFromDisk = moduleFromDisk.GetTypes()[i];

                Assert.True(sourceType.IsValueType);
                Assert.Equal(sourceType.Name, typeFromDisk.Name);
                Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);

                Assert.Empty(sourceType.GetMethods(BindingFlags.DeclaredOnly));
                Assert.Empty(sourceType.GetFields(BindingFlags.DeclaredOnly));
                Assert.Empty(sourceType.GetProperties(BindingFlags.DeclaredOnly));
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

            Assert.NotNull(moduleFromDisk);
            Assert.Equal(2, moduleFromDisk.GetTypes().Length);

            for (int i = 0; i < types.Length; i++)
            {
                Type sourceType = types[i];
                Type typeFromDisk = moduleFromDisk.GetTypes()[i];

                Assert.True(sourceType.IsValueType);
                Assert.Equal(sourceType.Name, typeFromDisk.Name);
                Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);
                Assert.Empty(sourceType.GetMethods(BindingFlags.DeclaredOnly));
                var declaredFields = sourceType.GetFields();

                // Field comparison
                for (int j = 0; j < declaredFields.Length; j++)
                {
                    FieldInfo sourceField = declaredFields[j];
                    FieldInfo fieldFromDisk = typeFromDisk.GetFields()[j];

                    Assert.Equal(sourceField.Name, fieldFromDisk.Name);
                    Assert.Equal(sourceField.Attributes, fieldFromDisk.Attributes);
                    Assert.Equal(sourceField.FieldType.FullName, fieldFromDisk.FieldType.FullName);
                }
            }
        }
    }

    //  Test Interfaces
    public interface INoMethod
    {
    }

    public interface INoMethod2
    {
    }

    public interface IMultipleMethod
    {
        string Func(int a, string b);
        bool MoreFunc(int a, string b, bool c);
        double DoIExist();
        void BuildAPerpetualMotionMachine();
    }

    internal interface IAccess
    {
        public int BuildAI(double field);
        public int DisableRogueAI();
    }

    public interface IOneMethod
    {
        string Func(int a, string b);
    }

    public struct EmptyStruct
    {
    }

    public struct StructWithField
    {
        public int field;
    }
}
