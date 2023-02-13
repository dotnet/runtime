// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Reflection.Metadata.Experiment.Tests
{
    public class ReflectionTesting : IDisposable
    {
        internal string fileLocation;
        public ReflectionTesting()
        {
            const bool _keepFiles = true;
            TempFileCollection _tfc;
            Directory.CreateDirectory("testDir");
            _tfc = new TempFileCollection("testDir", false);
            fileLocation = _tfc.AddExtension("dll", _keepFiles);
        }

        public void Dispose()
        { }

        [Fact]
        public void OneInterfaceWithMethods()
        {
            // Construct an assembly name.
            AssemblyName assemblyName = new AssemblyName("MyDynamicAssembly");

            //Construct its types via reflection.
            Type[] types = new Type[] { typeof(IMultipleMethod) };

            // Generate DLL from these and save it to Disk.
            AssemblyTools.WriteAssemblyToDisk(assemblyName, types, fileLocation);

            // Read said assembly back from Disk using MetadataLoadContext
            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(fileLocation);

            // Now compare them:

            // AssemblyName
            Assert.NotNull(assemblyFromDisk);
            Assert.Equal(assemblyName.Name, assemblyFromDisk.GetName().Name);

            // Module Name
            Module moduleFromDisk = assemblyFromDisk.Modules.First();
            Assert.Equal(assemblyName.Name, moduleFromDisk.ScopeName);

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
                        ParameterInfo sourceParamter = sourceMethod.GetParameters()[k];
                        ParameterInfo paramterFromDisk = methodFromDisk.GetParameters()[k];
                        Assert.Equal(sourceParamter.ParameterType.FullName, paramterFromDisk.ParameterType.FullName);
                    }
                }
            }
        }

        [Fact]
        public void OneInterfaceWithoutMethods()
        {
            // Construct an assembly name.
            AssemblyName assemblyName = new AssemblyName("MyDynamicAssembly");

            //Construct its types via reflection.
            Type[] types = new Type[] { typeof(INoMethod) };

            // Generate DLL from these and save it to Disk.
            AssemblyTools.WriteAssemblyToDisk(assemblyName, types, fileLocation, null);

            // Read said assembly back from Disk using MetadataLoadContext
            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(fileLocation);

            // Now compare them:

            // AssemblyName
            Assert.NotNull(assemblyFromDisk);
            Assert.Equal(assemblyName.Name, assemblyFromDisk.GetName().Name);

            // Module Name
            Module moduleFromDisk = assemblyFromDisk.Modules.First();
            Assert.Equal(assemblyName.Name, moduleFromDisk.ScopeName);

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
                        ParameterInfo sourceParamter = sourceMethod.GetParameters()[k];
                        ParameterInfo paramterFromDisk = methodFromDisk.GetParameters()[k];
                        Assert.Equal(sourceParamter.ParameterType.FullName, paramterFromDisk.ParameterType.FullName);
                    }
                }
            }
        }

        [Fact]
        public void EmptyInterfacesBetweenNonEmpty()
        {
            // Construct an assembly name.
            AssemblyName assemblyName = new AssemblyName("MyDynamicAssembly");

            //Construct its types via reflection.
            Type[] types = new Type[] { typeof(IAccess), typeof(INoMethod), typeof(INoMethod2), typeof(IMultipleMethod) };

            // Generate DLL from these and save it to Disk.
            AssemblyTools.WriteAssemblyToDisk(assemblyName, types, fileLocation);

            // Read said assembly back from Disk using MetadataLoadContext
            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(fileLocation);

            // Now compare them:

            // AssemblyName
            Assert.NotNull(assemblyFromDisk);
            Assert.Equal(assemblyName.Name, assemblyFromDisk.GetName().Name);

            // Module Name
            Module moduleFromDisk = assemblyFromDisk.Modules.First();
            Assert.Equal(assemblyName.Name, moduleFromDisk.ScopeName);

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
                        ParameterInfo sourceParamter = sourceMethod.GetParameters()[k];
                        ParameterInfo paramterFromDisk = methodFromDisk.GetParameters()[k];
                        Assert.Equal(sourceParamter.ParameterType.FullName, paramterFromDisk.ParameterType.FullName);
                    }
                }
            }
        }

        [Fact]
        public void TwoEmptyInterfaces()
        {
            // Construct an assembly name.
            AssemblyName assemblyName = new AssemblyName("MyDynamicAssembly");

            //Construct its types via reflection.
            Type[] types = new Type[] { typeof(INoMethod), typeof(INoMethod2) };

            // Generate DLL from these and save it to Disk.
            AssemblyTools.WriteAssemblyToDisk(assemblyName, types, fileLocation);

            // Read said assembly back from Disk using MetadataLoadContext
            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(fileLocation);

            // Now compare them:

            // AssemblyName
            Assert.NotNull(assemblyFromDisk);
            Assert.Equal(assemblyName.Name, assemblyFromDisk.GetName().Name);

            // Module Name
            Module moduleFromDisk = assemblyFromDisk.Modules.First();
            Assert.Equal(assemblyName.Name, moduleFromDisk.ScopeName);

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
                        ParameterInfo sourceParamter = sourceMethod.GetParameters()[k];
                        ParameterInfo paramterFromDisk = methodFromDisk.GetParameters()[k];
                        Assert.Equal(sourceParamter.ParameterType.FullName, paramterFromDisk.ParameterType.FullName);
                    }
                }
            }
        }

        [Fact]
        public void TwoInterfaceOneMethod()
        {
            // Construct an assembly name.
            AssemblyName assemblyName = new AssemblyName("MyDynamicAssembly");

            //Construct its types via reflection.
            Type[] types = new Type[] { typeof(INoMethod), typeof(IOneMethod) };

            // Generate DLL from these and save it to Disk.
            AssemblyTools.WriteAssemblyToDisk(assemblyName, types, fileLocation);

            // Read said assembly back from Disk using MetadataLoadContext
            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(fileLocation);

            // Now compare them:

            // AssemblyName
            Assert.NotNull(assemblyFromDisk);
            Assert.Equal(assemblyName.Name, assemblyFromDisk.GetName().Name);

            // Module Name
            Module moduleFromDisk = assemblyFromDisk.Modules.First();
            Assert.Equal(assemblyName.Name, moduleFromDisk.ScopeName);

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
                        ParameterInfo sourceParamter = sourceMethod.GetParameters()[k];
                        ParameterInfo paramterFromDisk = methodFromDisk.GetParameters()[k];
                        Assert.Equal(sourceParamter.ParameterType.FullName, paramterFromDisk.ParameterType.FullName);
                    }
                }
            }
        }



        [Fact]
        public void TwoIntefaceManyMethodsThenNone()
        {
            // Construct an assembly name.
            AssemblyName assemblyName = new AssemblyName("MyDynamicAssembly");

            //Construct its types via reflection.
            Type[] types = new Type[] { typeof(IMultipleMethod), typeof(INoMethod2) };

            // Generate DLL from these and save it to Disk.
            AssemblyTools.WriteAssemblyToDisk(assemblyName, types, fileLocation);

            // Read said assembly back from Disk using MetadataLoadContext
            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(fileLocation);

            // Now compare them:

            // AssemblyName
            Assert.NotNull(assemblyFromDisk);
            Assert.Equal(assemblyName.Name, assemblyFromDisk.GetName().Name);

            // Module Name
            Module moduleFromDisk = assemblyFromDisk.Modules.First();
            Assert.Equal(assemblyName.Name, moduleFromDisk.ScopeName);

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
        public void VariousInterfaces()
        {
            // Construct an assembly name.
            AssemblyName assemblyName = new AssemblyName("MyDynamicAssembly");

            //Construct its types via reflection.
            Type[] types = new Type[] { typeof(IMultipleMethod), typeof(INoMethod2), typeof(IAccess), typeof(IOneMethod), typeof(INoMethod) };

            // Generate DLL from these and save it to Disk.
            AssemblyTools.WriteAssemblyToDisk(assemblyName, types, fileLocation);

            // Read said assembly back from Disk using MetadataLoadContext
            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(fileLocation);

            // Now compare them:

            // AssemblyName
            Assert.NotNull(assemblyFromDisk);
            Assert.Equal(assemblyName.Name, assemblyFromDisk.GetName().Name);

            // Module Name
            Module moduleFromDisk = assemblyFromDisk.Modules.First();
            Assert.Equal(assemblyName.Name, moduleFromDisk.ScopeName);

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
        public void OneStructWithoutMethods()
        {
            // Construct an assembly name.
            AssemblyName assemblyName = new AssemblyName("MyDynamicAssembly");

            //Construct its types via reflection.
            Type[] types = new Type[] { typeof(EmptyStruct) };

            // Generate DLL from these and save it to Disk.
            AssemblyTools.WriteAssemblyToDisk(assemblyName, types, fileLocation, null);

            // Read said assembly back from Disk using MetadataLoadContext
            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(fileLocation);

            // Now compare them:

            // AssemblyName
            Assert.NotNull(assemblyFromDisk);
            Assert.Equal(assemblyName.Name, assemblyFromDisk.GetName().Name);

            // Module Name
            Module moduleFromDisk = assemblyFromDisk.Modules.First();
            Assert.Equal(assemblyName.Name, moduleFromDisk.ScopeName);

            // Type comparisons
            for (int i = 0; i < types.Length; i++)
            {
                Type sourceType = types[i];
                Type typeFromDisk = moduleFromDisk.GetTypes()[i];

                Assert.Equal(sourceType.Name, typeFromDisk.Name);
                Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);

                Assert.Empty(sourceType.GetMethods(BindingFlags.DeclaredOnly));
                Assert.Empty(sourceType.GetFields(BindingFlags.DeclaredOnly));
                Assert.Empty(sourceType.GetProperties(BindingFlags.DeclaredOnly));
            }
        }

        [Fact]
        public void EmptyStructAndStructWithField()
        {
            // Construct an assembly name.
            AssemblyName assemblyName = new AssemblyName("MyDynamicAssembly");

            //Construct its types via reflection.
            Type[] types = new Type[] { typeof(EmptyStruct), typeof(StructWithField) };

            // Generate DLL from these and save it to Disk.
            AssemblyTools.WriteAssemblyToDisk(assemblyName, types, fileLocation, null);

            // Read said assembly back from Disk using MetadataLoadContext
            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(fileLocation);

            // Now compare them:

            // AssemblyName
            Assert.NotNull(assemblyFromDisk);
            Assert.Equal(assemblyName.Name, assemblyFromDisk.GetName().Name);

            // Module Name
            Module moduleFromDisk = assemblyFromDisk.Modules.First();
            Assert.Equal(assemblyName.Name, moduleFromDisk.ScopeName);

            // Type comparisons
            for (int i = 0; i < types.Length; i++)
            {
                Type sourceType = types[i];
                Type typeFromDisk = moduleFromDisk.GetTypes()[i];

                Assert.Equal(sourceType.Name, typeFromDisk.Name);
                Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);
                Assert.Empty(sourceType.GetMethods(BindingFlags.DeclaredOnly));
                var declaredFields = sourceType.GetFields();
                // Method comparison
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
