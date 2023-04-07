// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class CustomAttributeTesting
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
        private static readonly (ConstructorInfo con, object[] args) s_comVisiblePair = (s_comVisibleType.GetConstructor(new Type[] { typeof(bool) }), new object[] { true });
        private static readonly (ConstructorInfo con, object[] args) s_guidPair = (s_guideType.GetConstructor(new Type[] { typeof(string) }), new object[] { "9ED54F84-A89D-4fcd-A854-44251E925F09" });
        private static readonly (ConstructorInfo con, object[] args) s_comImportPair = (s_comImportType.GetConstructor(Type.EmptyTypes), new object[] { });

        private static AssemblyName PopulateAssemblyName()
        {
            AssemblyName assemblyName = new AssemblyName("MyDynamicAssembly");
            assemblyName.Version = new Version("7.0.0.0");
            assemblyName.CultureInfo = Globalization.CultureInfo.InvariantCulture;
            return assemblyName;
        }

        [Fact]
        public void AssemblyModuleWithCustomAttributes()
        {
            AssemblyName assemblyName = PopulateAssemblyName();

            using (TempFile file = TempFile.Create())
            {
                WriteAssemblyToDisk(assemblyName, Type.EmptyTypes, file.Path, _attributes, _attributes);

                Assembly assemblyFromDisk = AssemblyTools.LoadAssemblyFromPath(file.Path);
                Module moduleFromDisk = assemblyFromDisk.Modules.First();

                AssemblyTools.AssertAssemblyNameAndModule(assemblyName, assemblyFromDisk.GetName(), moduleFromDisk);

                IList<CustomAttributeData> assemblyAttributesFromDisk = assemblyFromDisk.GetCustomAttributesData();
                IList<CustomAttributeData> moduleAttributesFromDisk = moduleFromDisk.GetCustomAttributesData();

                Assert.Equal(_attributes.Count, assemblyAttributesFromDisk.Count);
                Assert.Equal(_attributes.Count, moduleAttributesFromDisk.Count);

                foreach (var attribute in assemblyAttributesFromDisk)
                {
                    ValidateAttributes(attribute);
                }

                foreach (var attribute in moduleAttributesFromDisk)
                {
                    ValidateAttributes(attribute);
                }
            }
        }

        [Fact]
        public void MethodFieldWithCustomAttributes()
        {
            AssemblyName assemblyName = PopulateAssemblyName();
            Type[] types = new Type[] { typeof(IMultipleMethod), typeof(IOneMethod), typeof(StructWithField) };

            using (TempFile file = TempFile.Create())
            {
                WriteAssemblyToDisk(assemblyName, types, file.Path, typeAttributes: _attributesWithPseudo,
                    methodAttributes: _attributes, fieldAttributes: _attributes);

                Assembly assemblyFromDisk = AssemblyTools.LoadAssemblyFromPath(file.Path);

                Module moduleFromDisk = assemblyFromDisk.Modules.First();
                Type[] typesFromDisk = moduleFromDisk.GetTypes();

                Assert.Equal(types.Length, typesFromDisk.Length);

                for (int i = 0; i < types.Length; i++)
                {
                    Type typeFromDisk = typesFromDisk[i];
                    Type sourceType = types[i];
                    MethodInfo[] methodsFromDisk = typeFromDisk.IsValueType ? typeFromDisk.GetMethods(BindingFlags.DeclaredOnly) : typeFromDisk.GetMethods();
                    FieldInfo[] fieldsFromDisk = typeFromDisk.GetFields();

                    IList<CustomAttributeData> typeAttributesFromDisk = typeFromDisk.GetCustomAttributesData();

                    Assert.Equal(_attributesWithPseudo.Count, typeAttributesFromDisk.Count);
                    Assert.Equal(sourceType.Attributes | TypeAttributes.Import, typeFromDisk.Attributes); // Pseudo-custom attributes are added to core TypeAttributes.
                    AssemblyTools.AssertMethods(sourceType.IsValueType ? sourceType.GetMethods(BindingFlags.DeclaredOnly) : sourceType.GetMethods(), methodsFromDisk);
                    AssemblyTools.AssertFields(sourceType.GetFields(), fieldsFromDisk);

                    foreach (var attribute in typeAttributesFromDisk)
                    {
                        if (attribute.AttributeType.Name == "ComImportAttribute")
                        {
                            Assert.Equal(s_comImportPair.con.MetadataToken, attribute.Constructor.MetadataToken);
                            Assert.Empty(attribute.ConstructorArguments);
                        }
                        else
                        {
                            ValidateAttributes(attribute);
                        }
                    }

                    for (int j = 0; j < methodsFromDisk.Length; j++)
                    {
                        IList<CustomAttributeData> attributesFromDisk = methodsFromDisk[j].GetCustomAttributesData();

                        Assert.Equal(_attributes.Count, attributesFromDisk.Count);

                        foreach (var attribute in attributesFromDisk)
                        {
                            ValidateAttributes(attribute);
                        }
                    }

                    for (int j = 0; j < fieldsFromDisk.Length; j++)
                    {
                        IList<CustomAttributeData> attributesFromDisk = fieldsFromDisk[j].GetCustomAttributesData();

                        Assert.Equal(_attributes.Count, attributesFromDisk.Count);

                        foreach (var attribute in attributesFromDisk)
                        {
                            ValidateAttributes(attribute);
                        }
                    }
                }
            }
        }

        private void ValidateAttributes(CustomAttributeData customAttribute)
        {
            if (customAttribute.AttributeType.Name == s_comVisibleType.Name)
            {
                Assert.Equal(s_comVisiblePair.con.MetadataToken, customAttribute.Constructor.MetadataToken);

                Assert.Equal(s_comVisiblePair.args[0].GetType().FullName, customAttribute.ConstructorArguments[0].ArgumentType.FullName);
                Assert.Equal(true, customAttribute.ConstructorArguments[0].Value);
            }
            else
            {
                Assert.Equal(s_guidPair.con.MetadataToken, customAttribute.Constructor.MetadataToken);

                Assert.Equal(s_guidPair.args[0].GetType().FullName, customAttribute.ConstructorArguments[0].ArgumentType.FullName);
                Assert.Equal(customAttribute.AttributeType.Name, s_guideType.Name);
                Assert.Equal(s_guidPair.args[0], customAttribute.ConstructorArguments[0].Value);
            }
        }

        private static void WriteAssemblyToDisk(AssemblyName assemblyName, Type[] types, string fileLocation, List<CustomAttributeBuilder>? assemblyAttributes = null,
            List<CustomAttributeBuilder>? moduleAttributes = null, List<CustomAttributeBuilder>? typeAttributes = null,
            List<CustomAttributeBuilder>? methodAttributes = null, List<CustomAttributeBuilder>? fieldAttributes = null)
        {
            AssemblyBuilder assemblyBuilder = AssemblyTools.PopulateAssemblyBuilderAndSaveMethod(
                assemblyName, assemblyAttributes, typeof(string), out MethodInfo saveMethod);

            ModuleBuilder mb = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            PopulateMembersForModule(mb, types, moduleAttributes, typeAttributes, methodAttributes, fieldAttributes);

            saveMethod.Invoke(assemblyBuilder, new object[] { fileLocation });
        }

        private static void PopulateMembersForModule(ModuleBuilder mb, Type[] types, List<CustomAttributeBuilder>? moduleAttributes,
            List<CustomAttributeBuilder>? typeAttributes, List<CustomAttributeBuilder>? methodAttributes, List<CustomAttributeBuilder>? fieldAttributes)
        {
            if (moduleAttributes != null)
            {
                foreach (var attribute in moduleAttributes)
                {
                    mb.SetCustomAttribute(attribute);
                }
            }

            foreach (Type type in types)
            {
                TypeBuilder tb = mb.DefineType(type.FullName, type.Attributes, type.BaseType);

                if (typeAttributes != null)
                {
                    foreach (CustomAttributeBuilder typeAttribute in typeAttributes)
                    {
                        tb.SetCustomAttribute(typeAttribute);
                    }
                }

                MethodInfo[] methods = type.IsInterface ? type.GetMethods() : type.GetMethods(BindingFlags.DeclaredOnly);
                foreach (var method in methods)
                {
                    MethodBuilder meb = tb.DefineMethod(method.Name, method.Attributes, method.CallingConvention, method.ReturnType, null);

                    if (methodAttributes != null)
                    {
                        foreach (CustomAttributeBuilder typeAttribute in methodAttributes)
                        {
                            meb.SetCustomAttribute(typeAttribute);
                        }
                    }
                }

                foreach (FieldInfo field in type.GetFields())
                {
                    FieldBuilder fb = tb.DefineField(field.Name, field.FieldType, field.Attributes);

                    if (fieldAttributes != null)
                    {
                        foreach (var attribute in fieldAttributes)
                        {
                            fb.SetCustomAttribute(attribute);
                        }
                    }
                }
            }
        }
    }
}
