// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Metadata.Experiment.Tests
{
    //Currently hard-coding in Custom Attributes using the CustomAttributeBuilder.
    public class CustomAttributeTesting : IDisposable
    {
        List<CustomAttributeBuilder> customAttributes = new List<CustomAttributeBuilder>();
        Dictionary<ConstructorInfo, object[]> attibutesData = new Dictionary<ConstructorInfo, object[]>();
        internal string fileLocation;

        public CustomAttributeTesting()
        {
            const bool _keepFiles = true;
            TempFileCollection _tfc;
            Directory.CreateDirectory("testDir");
            _tfc = new TempFileCollection("testDir", false);
            fileLocation = _tfc.AddExtension("dll", _keepFiles);

            attibutesData.Add(typeof(ComImportAttribute).GetConstructor(new Type[] { }), new object[] { });
            attibutesData.Add(typeof(ComVisibleAttribute).GetConstructor(new Type[] { typeof(bool) }), new object[] { true });
            attibutesData.Add(typeof(GuidAttribute).GetConstructor(new Type[] { typeof(string) }), new object[] { "9ED54F84-A89D-4fcd-A854-44251E925F09" });
            foreach (var attribute in attibutesData)
            {
                customAttributes.Add(new CustomAttributeBuilder(attribute.Key, attribute.Value));
            }
        }

        // Add three custom attributes to two types. One is pseudo custom attribute.
        // This also tests that Save doesn't have unnecessary duplicate references to same assembly, type etc.
        [Fact]
        public void TwoInterfaceCustomAttribute()
        {
            // Construct an assembly name.
            AssemblyName assemblyName = new AssemblyName("MyDynamicAssembly");
            assemblyName.Version = new Version("7.0");

            //Construct its types via reflection.
            Type[] types = new Type[] { typeof(IMultipleMethod), typeof(INoMethod) };

            // Generate DLL from these and save it to Disk.
            AssemblyTools.WriteAssemblyToDisk(assemblyName, types, fileLocation, customAttributes);

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
                Assert.Equal(sourceType.Attributes | TypeAttributes.Import, typeFromDisk.Attributes); // Pseudo-custom attributes are added to core TypeAttributes.

                List<CustomAttributeData> attributesFromDisk = typeFromDisk.GetCustomAttributesData().ToList();

                foreach(var attribute in attibutesData)
                {

                }

                foreach(var attribute in attributesFromDisk)
                {
                    Assert.True(attibutesData.TryGetValue(attribute.Constructor, out var value));
                }
                /*for (int j = 0; j < customAttributes.Count; j++)
                {
                    CustomAttributeBuilder sourceAttribute = customAttributes[j];
                    CustomAttributeData attributeFromDisk = attributesFromDisk[j];
                    Debug.WriteLine(attributeFromDisk.AttributeType.ToString());
                    Assert.Equal(sourceAttribute.Constructor.DeclaringType.ToString(), attributeFromDisk.AttributeType.ToString());
                }*/

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

        public void Dispose()
        {
        }
    }
}
