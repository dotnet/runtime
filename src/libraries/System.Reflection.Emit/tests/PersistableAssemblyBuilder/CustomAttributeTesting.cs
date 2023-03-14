// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
            new CustomAttributeBuilder(typeof(ComImportAttribute).GetConstructor(new Type[] { }), new object[] { })
        };

        private List<CustomAttributeBuilder> _attributes = new List<CustomAttributeBuilder>
        {
            new CustomAttributeBuilder(s_comVisiblePair.con, s_comVisiblePair.args),
            new CustomAttributeBuilder(s_guidPair.con, s_guidPair.args)
        };
        private static readonly Type s_comVisibleType = typeof(ComVisibleAttribute);
        private static readonly Type s_guideType = typeof(GuidAttribute);
        private static readonly (ConstructorInfo con, object [] args) s_comVisiblePair = (s_comVisibleType.GetConstructor(new Type[] { typeof(bool) }), new object[] { true });
        private static readonly (ConstructorInfo con, object [] args) s_guidPair = (s_guideType.GetConstructor(new Type[] { typeof(string) }), new object[] { "9ED54F84-A89D-4fcd-A854-44251E925F09" });

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
                        continue;

                    ValidateAttributes(attribute);
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

            // These attributes not for Module, but seems existing AssemblyBuidler also just ignores the target
            AssemblyTools.WriteAssemblyToDisk(assemblyName, Type.EmptyTypes, _fileLocation, null, _attributes, null);

            Assembly assemblyFromDisk = AssemblyTools.TryLoadAssembly(_fileLocation);

            Assert.NotNull(assemblyFromDisk);
            Assert.Equal(assemblyName.Name, assemblyFromDisk.GetName().Name);
            Assert.Equal(assemblyName.Version, assemblyFromDisk.GetName().Version);

            Module moduleFromDisk = assemblyFromDisk.Modules.First();

            // Custom attributes comparisons
            List<CustomAttributeData> attributesFromDisk = moduleFromDisk.GetCustomAttributesData().ToList();

            Assert.Equal(2, attributesFromDisk.Count);

            ValidateAttributes(attributesFromDisk[0]);
            ValidateAttributes(attributesFromDisk[1]);
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
                Assert.Equal(s_comVisiblePair.con, customAttribute.Constructor, new MyComparer());

                //Assert.Equal(typeof(System.Boolean), customAttribute.ConstructorArguments[0].ArgumentType);
                Assert.Equal(true, customAttribute.ConstructorArguments[0].Value);
            }
            else
            {
                Assert.Equal(s_guidPair.con, customAttribute.Constructor, new MyComparer());

                //Assert.Equal(typeof(string), customAttribute.ConstructorArguments[0].ArgumentType);
                Assert.Equal(customAttribute.AttributeType.Name, s_guideType.Name);
                Assert.Equal("9ED54F84-A89D-4fcd-A854-44251E925F09", customAttribute.ConstructorArguments[0].Value);
            }
        }

        public void Dispose()
        {
        }
    }
}
