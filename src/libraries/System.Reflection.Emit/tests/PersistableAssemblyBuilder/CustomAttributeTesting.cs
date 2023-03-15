// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Xunit;

namespace System.Reflection.Emit.Experiment.Tests
{
    public class MyComparer : IEqualityComparer<ConstructorInfo>
    {
        public bool Equals(ConstructorInfo? x, ConstructorInfo? y) => x.MetadataToken == y.MetadataToken;
        public int GetHashCode([DisallowNull] ConstructorInfo obj) => obj.MetadataToken.GetHashCode();
    }

    public class CustomAttributeTesting : IDisposable
    {
        // Add three custom attributes to two types. One is pseudo custom attribute.
        private List<CustomAttributeBuilder> _attributesWithPseudo = new List<CustomAttributeBuilder>
        {
            new CustomAttributeBuilder(s_comVisiblePair.con, s_comVisiblePair.args),
            new CustomAttributeBuilder(s_guidPair.con, s_guidPair.args),
            new CustomAttributeBuilder(s_comImportPair.con, s_comImportPair.args)
        };

        private List<CustomAttributeBuilder> _attributes = new List<CustomAttributeBuilder>
        {
            new CustomAttributeBuilder(s_comVisiblePair.con, s_comVisiblePair.args),
            new CustomAttributeBuilder(s_guidPair.con, s_guidPair.args)
        };

        private static readonly Type s_comVisibleType = typeof(ComVisibleAttribute);
        private static readonly Type s_guideType = typeof(GuidAttribute);
        private static readonly Type s_comImportType = typeof(ComImportAttribute);
        private static readonly (ConstructorInfo con, object [] args) s_comVisiblePair = (s_comVisibleType.GetConstructor(new Type[] { typeof(bool) }), new object[] { true });
        private static readonly (ConstructorInfo con, object [] args) s_guidPair = (s_guideType.GetConstructor(new Type[] { typeof(string) }), new object[] { "9ED54F84-A89D-4fcd-A854-44251E925F09" });
        private static readonly (ConstructorInfo con, object [] args) s_comImportPair = (s_comImportType.GetConstructor(Type.EmptyTypes), new object[] { });
        private static readonly MyComparer s_comparer = new();
        internal string _fileLocation;

        public CustomAttributeTesting()
        {
            const bool keepFiles = true;
            TempFileCollection tfc;
            Directory.CreateDirectory("testDir");
            tfc = new TempFileCollection("testDir", false);
            _fileLocation = tfc.AddExtension("dll", keepFiles);
        }
        
        [Fact]
        public void TwoInterfaceWithCustomAttributes()
        {
            Type[] types = new Type[] { typeof(IMultipleMethod), typeof(INoMethod) };

            AssemblyTools.WriteAssemblyToDisk(PopulateAssemblyName(), types, _fileLocation, null, null, _attributesWithPseudo);
            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(_fileLocation);

            Assert.NotNull(assemblyFromDisk);

            Module moduleFromDisk = assemblyFromDisk.Modules.First();

            for (int i = 0; i < types.Length; i++)
            {
                Type sourceType = types[i];
                Type typeFromDisk = moduleFromDisk.GetTypes()[i];

                Assert.Equal(sourceType.Name, typeFromDisk.Name);
                Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
                Assert.Equal(sourceType.Attributes | TypeAttributes.Import, typeFromDisk.Attributes); // Pseudo-custom attributes are added to core TypeAttributes.

                List<CustomAttributeData> attributesFromDisk = typeFromDisk.GetCustomAttributesData().ToList();
                Assert.Equal(3, attributesFromDisk.Count);

                foreach (var attribute in attributesFromDisk)
                {
                    if (attribute.AttributeType.Name == "ComImportAttribute")
                    {
                        Assert.Equal(s_comImportPair.con, attribute.Constructor, s_comparer);
                        Assert.Empty(attribute.ConstructorArguments);
                    }
                    else
                    {
                        ValidateAttributes(attribute);
                    }
                }
            }
        }

        private static AssemblyName PopulateAssemblyName()
        {
            AssemblyName assemblyName = new AssemblyName("MyDynamicAssembly");
            assemblyName.Version = new Version("7.0.0.0");
            assemblyName.CultureInfo = Globalization.CultureInfo.InvariantCulture;
            return assemblyName;
        }

        [Fact]
        public void ModuleWithCustomAttributes()
        {
            AssemblyName assemblyName = PopulateAssemblyName();

            // These attributes not for Module, but seems existing AssemblyBuilder also just ignores the target
            AssemblyTools.WriteAssemblyToDisk(assemblyName, Type.EmptyTypes, _fileLocation, null, _attributes, null);

            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(_fileLocation);

            Assert.NotNull(assemblyFromDisk);

            Module moduleFromDisk = assemblyFromDisk.Modules.First();

            // Custom attributes comparisons
            List<CustomAttributeData> attributesFromDisk = moduleFromDisk.GetCustomAttributesData().ToList();

            Assert.Equal(2, attributesFromDisk.Count);

            ValidateAttributes(attributesFromDisk[0]);
            ValidateAttributes(attributesFromDisk[1]);
        }

        [Fact]
        public void MethodWithCustomAttributes()
        {
            AssemblyName assemblyName = PopulateAssemblyName();
            Type[] types = new Type[] { typeof(IMultipleMethod), typeof(IOneMethod) };

            AssemblyTools.WriteAssemblyToDisk(assemblyName, types, _fileLocation, null, methodAttributes: _attributes);

            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(_fileLocation);

            Assert.NotNull(assemblyFromDisk);

            Module moduleFromDisk = assemblyFromDisk.Modules.First();
            Type[] typesFromDisk = moduleFromDisk.GetTypes();

            for (int i = 0; i < types.Length; i++)
            {
                MethodInfo[] sourceMethods = types[i].GetMethods();
                MethodInfo[] methodsFromDisk = typesFromDisk[i].GetMethods();

                for (int j = 0; j < sourceMethods.Length; j++)
                {
                    MethodInfo sourceMethod = sourceMethods[j];
                    MethodInfo methodFromDisk = methodsFromDisk[j];

                    Assert.Equal(sourceMethod.Name, methodFromDisk.Name);
                    Assert.Equal(sourceMethod.Attributes, methodFromDisk.Attributes);
                    Assert.Equal(sourceMethod.ReturnType.FullName, methodFromDisk.ReturnType.FullName);

                    List<CustomAttributeData> attributesFromDisk = methodFromDisk.GetCustomAttributesData().ToList();
                    Assert.Equal(2, attributesFromDisk.Count);

                    foreach (var attribute in attributesFromDisk)
                    {
                        ValidateAttributes(attribute);
                    }
                }
            }
        }

        [Fact]
        public void FieldWithCustomAttributes()
        {
            AssemblyName assemblyName = PopulateAssemblyName();
            Type[] types = new Type[] { typeof(IOneMethod), typeof(StructWithField) };

            AssemblyTools.WriteAssemblyToDisk(assemblyName, types, _fileLocation, null, fieldAttributes: _attributes);

            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(_fileLocation);

            Assert.NotNull(assemblyFromDisk);

            Module moduleFromDisk = assemblyFromDisk.Modules.First();
            Type[] typesFromDisk = moduleFromDisk.GetTypes();

            Assert.Equal(2, typesFromDisk.Length);

            for (int i = 0; i < types.Length; i++)
            {
                MethodInfo[] sourceMethods = types[i].IsValueType ? types[i].GetMethods(BindingFlags.DeclaredOnly) : types[i].GetMethods();
                MethodInfo[] methodsFromDisk = typesFromDisk[i].IsValueType ? typesFromDisk[i].GetMethods(BindingFlags.DeclaredOnly) : typesFromDisk[i].GetMethods();

                for (int j = 0; j < methodsFromDisk.Length; j++)
                {
                    MethodInfo sourceMethod = sourceMethods[j];
                    MethodInfo methodFromDisk = methodsFromDisk[j];

                    Assert.Equal(sourceMethod.Name, methodFromDisk.Name);
                    Assert.Equal(sourceMethod.Attributes, methodFromDisk.Attributes);
                    Assert.Equal(sourceMethod.ReturnType.FullName, methodFromDisk.ReturnType.FullName);

                    Assert.Empty(methodFromDisk.GetCustomAttributesData());
                }

                var declaredFields = types[i].GetFields();

                for (int j = 0; j < declaredFields.Length; j++)
                {
                    FieldInfo sourceField = declaredFields[j];
                    FieldInfo fieldFromDisk = typesFromDisk[i].GetFields()[j];

                    Assert.Equal(sourceField.Name, fieldFromDisk.Name);
                    Assert.Equal(sourceField.Attributes, fieldFromDisk.Attributes);
                    Assert.Equal(sourceField.FieldType.FullName, fieldFromDisk.FieldType.FullName);

                    List<CustomAttributeData> attributesFromDisk = fieldFromDisk.GetCustomAttributesData().ToList();
                    Assert.Equal(2, attributesFromDisk.Count);

                    foreach (var attribute in attributesFromDisk)
                    {
                        ValidateAttributes(attribute);
                    }
                }
            }
        }

        [Fact]
        public void AssemblyWithCustomAttributesWriteToStream()
        {
            AssemblyName assemblyName = PopulateAssemblyName();
            using var stream = new MemoryStream();

            AssemblyTools.WriteAssemblyToStream(assemblyName, Type.EmptyTypes, stream, _attributes);

            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(stream);

            // AssemblyName comparison
            Assert.NotNull(assemblyFromDisk);
            Assert.Equal(assemblyName.Name, assemblyFromDisk.GetName().Name);
            Assert.Equal(assemblyName.Version, assemblyFromDisk.GetName().Version);
            Assert.Equal(assemblyName.CultureInfo, assemblyFromDisk.GetName().CultureInfo);

            // Custom attributes comparisons
            List<CustomAttributeData> attributesFromDisk = assemblyFromDisk.GetCustomAttributesData().ToList();

            Assert.Equal(2, attributesFromDisk.Count);

            ValidateAttributes(attributesFromDisk[0]);
            ValidateAttributes(attributesFromDisk[1]);
        }

        private void ValidateAttributes(CustomAttributeData customAttribute)
        {
            if (customAttribute.AttributeType.Name == s_comVisibleType.Name)
            {
                Assert.Equal(s_comVisiblePair.con, customAttribute.Constructor, s_comparer);

                Assert.Equal(s_comVisiblePair.args[0].GetType().FullName, customAttribute.ConstructorArguments[0].ArgumentType.FullName);
                Assert.Equal(true, customAttribute.ConstructorArguments[0].Value);
            }
            else
            {
                Assert.Equal(s_guidPair.con, customAttribute.Constructor, s_comparer);

                Assert.Equal(s_guidPair.args[0].GetType().FullName, customAttribute.ConstructorArguments[0].ArgumentType.FullName);
                Assert.Equal(customAttribute.AttributeType.Name, s_guideType.Name);
                Assert.Equal(s_guidPair.args[0], customAttribute.ConstructorArguments[0].Value);
            }
        }

        public void Dispose()
        {
        }
    }
}
