// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySaveCustomAttributeTests
    {
        private List<CustomAttributeBuilder> _attributes = new List<CustomAttributeBuilder>
        {
            new CustomAttributeBuilder(s_comVisiblePair.con, s_comVisiblePair.args),
            new CustomAttributeBuilder(s_guidPair.con, s_guidPair.args)
        };

        private static readonly Type s_comVisibleType = typeof(ComVisibleAttribute);
        private static readonly Type s_guideType = typeof(GuidAttribute);
        private static readonly (ConstructorInfo con, object[] args) s_comVisiblePair = (s_comVisibleType.GetConstructor(new Type[] { typeof(bool) }), new object[] { true });
        private static readonly (ConstructorInfo con, object[] args) s_guidPair = (s_guideType.GetConstructor(new Type[] { typeof(string) }), new object[] { "9ED54F84-A89D-4fcd-A854-44251E925F09" });

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
            Type[] types = new Type[] { typeof(IMultipleMethod), typeof(IOneMethod), typeof(StructWithField) };

            using (TempFile file = TempFile.Create())
            {
                WriteAssemblyToDisk(PopulateAssemblyName(), types, file.Path, typeAttributes: _attributes,
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

                    Assert.Equal(_attributes.Count, typeAttributesFromDisk.Count);
                    AssemblyTools.AssertTypeProperties(sourceType, typeFromDisk);
                    AssemblyTools.AssertMethods(sourceType.IsValueType ? sourceType.GetMethods(BindingFlags.DeclaredOnly) : sourceType.GetMethods(), methodsFromDisk);
                    AssemblyTools.AssertFields(sourceType.GetFields(), fieldsFromDisk);

                    foreach (var attribute in typeAttributesFromDisk)
                    {
                        ValidateAttributes(attribute);
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

                DefineMethodsAndSetAttributes(methodAttributes, tb, type.IsInterface ? type.GetMethods() : type.GetMethods(BindingFlags.DeclaredOnly));
                DefineFieldsAndSetAttributes(fieldAttributes, type.GetFields(), tb);
            }
        }

        private static void DefineFieldsAndSetAttributes(List<CustomAttributeBuilder>? fieldAttributes, FieldInfo[] fields, TypeBuilder tb)
        {
            foreach (FieldInfo field in fields)
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

        private static void DefineMethodsAndSetAttributes(List<CustomAttributeBuilder> methodAttributes, TypeBuilder tb, MethodInfo[] methods)
        {
            foreach (var method in methods)
            {
                MethodBuilder meb = tb.DefineMethod(method.Name, method.Attributes, method.CallingConvention, method.ReturnType, null);

                if (methodAttributes != null)
                {
                    foreach (CustomAttributeBuilder attribute in methodAttributes)
                    {
                        meb.SetCustomAttribute(attribute);
                    }
                }
            }
        }

        [Fact]
        public void CreateStructWithPseudoCustomAttributesTest()
        {
            using (TempFile file = TempFile.Create())
            {
                Type type = typeof(StructWithField);
                CustomAttributeBuilder[] attributes = new[] { new CustomAttributeBuilder(typeof(SerializableAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                                                              new CustomAttributeBuilder(typeof(StructLayoutAttribute).GetConstructor(new Type[] { typeof(LayoutKind) }), new object[] { LayoutKind.Explicit },
                                                                    typeof(StructLayoutAttribute).GetFields() , new object[]{32, 64, CharSet.Unicode}),
                                                              new CustomAttributeBuilder(s_guidPair.con, s_guidPair.args),
                                                              new CustomAttributeBuilder(typeof(SpecialNameAttribute).GetConstructor(Type.EmptyTypes), new object[] { })
                                                            };
                CustomAttributeBuilder[] fieldAttributes = new[] { new CustomAttributeBuilder(typeof(NonSerializedAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                                                              new CustomAttributeBuilder(typeof(FieldOffsetAttribute).GetConstructor(new Type[] { typeof(int) }), new object[] { 0 }),
                                                              new CustomAttributeBuilder(s_guidPair.con, s_guidPair.args),
                                                              //new CustomAttributeBuilder(typeof(MarshalAsAttribute).GetConstructor(new Type[] { typeof(UnmanagedType) }), new object[] { UnmanagedType.I4}),
                                                              new CustomAttributeBuilder(typeof(SpecialNameAttribute).GetConstructor(Type.EmptyTypes), new object[] { })
                                                            };

                AssemblyBuilder ab = AssemblyTools.PopulateAssemblyBuilderAndSaveMethod(
                    PopulateAssemblyName(), null, typeof(string), out MethodInfo saveMethod);
                TypeBuilder tb = ab.DefineDynamicModule("Module").DefineType(type.FullName, type.Attributes);
                DefineFieldsAndSetAttributes(fieldAttributes.ToList(), type.GetFields(), tb);
                foreach (CustomAttributeBuilder attribute in attributes)
                {
                    tb.SetCustomAttribute(attribute);
                }

                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblyTools.LoadAssemblyFromPath(file.Path);
                Module moduleFromDisk = assemblyFromDisk.Modules.First();
                Type testType = moduleFromDisk.GetTypes()[0];

                IList<CustomAttributeData> attributesFromDisk = testType.GetCustomAttributesData();

                Assert.Equal(attributes.Length - 3, attributesFromDisk.Count); // 3 pseudo attributes 
                Assert.True((testType.Attributes & TypeAttributes.Serializable) != 0); // SerializableAttribute
                Assert.True((testType.Attributes & TypeAttributes.SpecialName) != 0); // SpecialNameAttribute
                Assert.True((testType.Attributes & TypeAttributes.ExplicitLayout) != 0); // StructLayoutAttribute
                Assert.True((testType.Attributes & TypeAttributes.UnicodeClass) != 0);   // StructLayoutAttribute, not sure if we could test the PackingSize and Size

                for (int i = 0; i < attributesFromDisk.Count; i++)
                {
                    switch (attributesFromDisk[i].AttributeType.Name)
                    {
                        case "GuidAttribute":
                            Assert.Equal(s_guidPair.args[0], attributesFromDisk[i].ConstructorArguments[0].Value);
                            break;
                        default:
                            Assert.Fail($"Not expected attribute : {attributesFromDisk[i].AttributeType.Name}");
                            break;
                    }
                }

                FieldInfo field = testType.GetFields()[0];
                IList<CustomAttributeData> fieldAttributesFromDisk = field.GetCustomAttributesData();

                Assert.True((field.Attributes & FieldAttributes.NotSerialized) != 0); // NonSerializedAttribute
                Assert.True((field.Attributes & FieldAttributes.SpecialName) != 0); // SpecialNameAttribute
                // Assert.True((field.Attributes & FieldAttributes.HasFieldMarshal) != 0); // MarshalAsAttribute

                for (int i = 0; i < fieldAttributesFromDisk.Count; i++)
                {
                    switch (fieldAttributesFromDisk[i].AttributeType.Name)
                    {
                        case "FieldOffsetAttribute":
                            // TODO: Assert.Equal(0, methodAttributesFromDisk[i].ConstructorArguments[0].Value);
                            break;
                        /*case "MarshalAsAttribute": // TODO: Need to support the UnmanagedType type
                            Assert.Equal(UnmanagedType.I4, methodAttributesFromDisk[i].ConstructorArguments[0].Value);
                            break;*/
                        case "GuidAttribute":
                            Assert.Equal(s_guidPair.args[0], fieldAttributesFromDisk[i].ConstructorArguments[0].Value);
                            break;
                        default:
                            Assert.Fail($"Not expected attribute : {fieldAttributesFromDisk[i].AttributeType.Name}");
                            break;
                    }
                }
            }
        }

        [Fact]
        public void InterfacesWithPseudoCustomAttributes()
        {
            using (TempFile file = TempFile.Create())
            {
                Type type = typeof(IMultipleMethod);
                CustomAttributeBuilder[] attributes = new[] { new CustomAttributeBuilder(typeof(ComImportAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                                                              new CustomAttributeBuilder(typeof(SuppressUnmanagedCodeSecurityAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                                                              new CustomAttributeBuilder(s_guidPair.con, s_guidPair.args)
                                                            };
                CustomAttributeBuilder[] methodAttributes = new[] { new CustomAttributeBuilder(typeof(PreserveSigAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                                                              new CustomAttributeBuilder(typeof(SuppressUnmanagedCodeSecurityAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                                                              new CustomAttributeBuilder(typeof(SpecialNameAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                                                              new CustomAttributeBuilder(s_guidPair.con, s_guidPair.args),
                                                              new CustomAttributeBuilder(typeof(MethodImplAttribute).GetConstructor(new Type[] { typeof(MethodImplOptions) }), new object[] { MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization }),
                                                            };

                AssemblyBuilder ab = AssemblyTools.PopulateAssemblyBuilderAndSaveMethod(
                    PopulateAssemblyName(), null, typeof(string), out MethodInfo saveMethod);
                TypeBuilder tb = ab.DefineDynamicModule("Module").DefineType(type.FullName, type.Attributes);
                DefineMethodsAndSetAttributes(methodAttributes.ToList(), tb, type.GetMethods());
                foreach (CustomAttributeBuilder attribute in attributes)
                {
                    tb.SetCustomAttribute(attribute);
                }

                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblyTools.LoadAssemblyFromPath(file.Path);
                Type testType = assemblyFromDisk.Modules.First().GetTypes()[0];
                IList<CustomAttributeData> attributesFromDisk = testType.GetCustomAttributesData();

                Assert.Equal(attributes.Length, attributesFromDisk.Count);

                for (int i = 0; i < attributesFromDisk.Count; i++)
                {
                    switch (attributesFromDisk[i].AttributeType.Name)
                    {
                        case "ComImportAttribute":
                            Assert.True((testType.Attributes & TypeAttributes.Import) != 0);
                            break;
                        case "SuppressUnmanagedCodeSecurityAttribute":
                            Assert.True((testType.Attributes & TypeAttributes.HasSecurity) != 0);
                            break;
                        case "GuidAttribute":
                            Assert.Equal(s_guidPair.args[0], attributesFromDisk[i].ConstructorArguments[0].Value);
                            break;
                        default:
                            Assert.Fail($"Not expected attribute : {attributesFromDisk[i].AttributeType.Name}");
                            break;
                    }
                }

                foreach (var method in testType.GetMethods())
                {
                    IList<CustomAttributeData> methodAttributesFromDisk = method.GetCustomAttributesData();

                    Assert.True((method.Attributes & MethodAttributes.HasSecurity) != 0); // SuppressUnmanagedCodeSecurityAttribute
                    Assert.True((method.Attributes & MethodAttributes.SpecialName) != 0); // SpecialNameAttribute
                    Assert.True((method.GetMethodImplementationFlags() & MethodImplAttributes.NoInlining) != 0); // MethodImplAttribute
                    Assert.True((method.GetMethodImplementationFlags() & MethodImplAttributes.AggressiveOptimization) != 0); // MethodImplAttribute
                    Assert.True((method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0); // PreserveSigAttribute
                    Assert.Equal(methodAttributes.Length-2, methodAttributesFromDisk.Count);

                    for (int i = 0; i < methodAttributesFromDisk.Count; i++)
                    {
                        switch (methodAttributesFromDisk[i].AttributeType.Name)
                        {
                            case "SuppressUnmanagedCodeSecurityAttribute":
                                break;
                            case "PreserveSigAttribute":
                                break;
                            case "GuidAttribute":
                                Assert.Equal(s_guidPair.args[0], methodAttributesFromDisk[i].ConstructorArguments[0].Value);
                                break;
                            default:
                                Assert.Fail($"Not expected attribute : {methodAttributesFromDisk[i].AttributeType.Name}");
                                break;
                        }
                    }
                }
            }
        }
    }
}
