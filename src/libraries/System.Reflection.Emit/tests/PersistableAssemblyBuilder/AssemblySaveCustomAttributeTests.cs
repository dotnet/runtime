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
        private static readonly Type s_guidType = typeof(GuidAttribute);
        private static readonly (ConstructorInfo con, object[] args) s_comVisiblePair = (s_comVisibleType.GetConstructor(new Type[] { typeof(bool) }), new object[] { true });
        private static readonly (ConstructorInfo con, object[] args) s_guidPair = (s_guidType.GetConstructor(new Type[] { typeof(string) }), new object[] { "9ED54F84-A89D-4fcd-A854-44251E925F09" });

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

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Module moduleFromDisk = assemblyFromDisk.Modules.First();

                    AssemblySaveTools.AssertAssemblyNameAndModule(assemblyName, assemblyFromDisk.GetName(), moduleFromDisk);
                    ValidateAttributes(assemblyFromDisk.GetCustomAttributesData());
                    ValidateAttributes(moduleFromDisk.GetCustomAttributesData());
                }
            }
        }

        [Fact]
        public void MethodFieldWithCustomAttributes()
        {
            Type[] types = new Type[] { typeof(IMultipleMethod), typeof(IOneMethod), typeof(StructWithFields) };

            using (TempFile file = TempFile.Create())
            {
                WriteAssemblyToDisk(PopulateAssemblyName(), types, file.Path, typeAttributes: _attributes,
                    methodAttributes: _attributes, fieldAttributes: _attributes);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);

                    Module moduleFromDisk = assemblyFromDisk.Modules.First();
                    Type[] typesFromDisk = moduleFromDisk.GetTypes();

                    Assert.Equal(types.Length, typesFromDisk.Length);

                    for (int i = 0; i < types.Length; i++)
                    {
                        Type typeFromDisk = typesFromDisk[i];
                        Type sourceType = types[i];
                        MethodInfo[] methodsFromDisk = typeFromDisk.IsValueType ? typeFromDisk.GetMethods(BindingFlags.DeclaredOnly) : typeFromDisk.GetMethods();
                        FieldInfo[] fieldsFromDisk = typeFromDisk.GetFields();

                        AssemblySaveTools.AssertTypeProperties(sourceType, typeFromDisk);
                        AssemblySaveTools.AssertMethods(sourceType.IsValueType ? sourceType.GetMethods(BindingFlags.DeclaredOnly) : sourceType.GetMethods(), methodsFromDisk);
                        AssemblySaveTools.AssertFields(sourceType.GetFields(), fieldsFromDisk);
                        ValidateAttributes(typeFromDisk.GetCustomAttributesData());

                        for (int j = 0; j < methodsFromDisk.Length; j++)
                        {
                            ValidateAttributes(methodsFromDisk[j].GetCustomAttributesData());
                        }

                        for (int j = 0; j < fieldsFromDisk.Length; j++)
                        {
                            ValidateAttributes(fieldsFromDisk[j].GetCustomAttributesData());
                        }
                    }
                }
            }
        }

        private void ValidateAttributes(IList<CustomAttributeData> attributesFromDisk)
        {
            Assert.Equal(_attributes.Count, attributesFromDisk.Count);

            foreach (var attribute in attributesFromDisk)
            {
                if (attribute.AttributeType.Name == s_comVisibleType.Name)
                {
                    Assert.Equal(s_comVisiblePair.con.MetadataToken, attribute.Constructor.MetadataToken);
                    Assert.Equal(s_comVisiblePair.args[0].GetType().FullName, attribute.ConstructorArguments[0].ArgumentType.FullName);
                    Assert.Equal(true, attribute.ConstructorArguments[0].Value);
                }
                else
                {
                    Assert.Equal(s_guidPair.con.MetadataToken, attribute.Constructor.MetadataToken);
                    Assert.Equal(s_guidPair.args[0].GetType().FullName, attribute.ConstructorArguments[0].ArgumentType.FullName);
                    Assert.Equal(attribute.AttributeType.Name, s_guidType.Name);
                    Assert.Equal(s_guidPair.args[0], attribute.ConstructorArguments[0].Value);
                }
            }
        }

        private static void WriteAssemblyToDisk(AssemblyName assemblyName, Type[] types, string fileLocation, List<CustomAttributeBuilder>? assemblyAttributes = null,
            List<CustomAttributeBuilder>? moduleAttributes = null, List<CustomAttributeBuilder>? typeAttributes = null,
            List<CustomAttributeBuilder>? methodAttributes = null, List<CustomAttributeBuilder>? fieldAttributes = null)
        {
            AssemblyBuilder assemblyBuilder = AssemblySaveTools.PopulateAssemblyBuilder(assemblyName, assemblyAttributes);
            ModuleBuilder mb = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            PopulateMembersForModule(mb, types, moduleAttributes, typeAttributes, methodAttributes, fieldAttributes);
            assemblyBuilder.Save(fileLocation);
        }

        private static void PopulateMembersForModule(ModuleBuilder mb, Type[] types, List<CustomAttributeBuilder>? moduleAttributes,
            List<CustomAttributeBuilder>? typeAttributes, List<CustomAttributeBuilder>? methodAttributes, List<CustomAttributeBuilder>? fieldAttributes)
        {
            if (moduleAttributes != null)
            {
                moduleAttributes.ForEach(mb.SetCustomAttribute);
            }

            foreach (Type type in types)
            {
                TypeBuilder tb = mb.DefineType(type.FullName, type.Attributes, type.BaseType);
                typeAttributes.ForEach(tb.SetCustomAttribute);

                DefineMethodsAndSetAttributes(methodAttributes, tb, type.IsInterface ? type.GetMethods() : type.GetMethods(BindingFlags.DeclaredOnly), methodAttributes);
                DefineFieldsAndSetAttributes(fieldAttributes, type.GetFields(), tb);
                tb.CreateType();
            }
        }

        private static void DefineFieldsAndSetAttributes(List<CustomAttributeBuilder>? fieldAttributes, FieldInfo[] fields, TypeBuilder tb)
        {
            foreach (FieldInfo field in fields)
            {
                FieldBuilder fb = tb.DefineField(field.Name, field.FieldType, field.Attributes);
                fieldAttributes.ForEach(fb.SetCustomAttribute);
            }
        }

        private static void DefineMethodsAndSetAttributes(List<CustomAttributeBuilder> methodAttributes, TypeBuilder tb, MethodInfo[] methods, List<CustomAttributeBuilder> paramAttributes)
        {
            foreach (var method in methods)
            {
                ParameterInfo[] parameters = method.GetParameters();
                MethodBuilder meb = tb.DefineMethod(method.Name, method.Attributes, method.CallingConvention, method.ReturnType, parameters.Select(p => p.ParameterType).ToArray());
                methodAttributes.ForEach(meb.SetCustomAttribute);

                foreach (ParameterInfo param in parameters)
                {
                    ParameterBuilder pb = meb.DefineParameter(param.Position + 1, param.Attributes, param.Name);
                    paramAttributes.ForEach(pb.SetCustomAttribute);
                    if (param.ParameterType.Equals(typeof(string)))
                    {
                        pb.SetConstant("Hello");
                    }
                }
            }
        }

        [Fact]
        public void CreateStructWithPseudoCustomAttributesTest()
        {
            using (TempFile file = TempFile.Create())
            {
                Type type = typeof(StructWithFields);
                List<CustomAttributeBuilder> typeAttributes = new() { new CustomAttributeBuilder(typeof(SerializableAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                                                              new CustomAttributeBuilder(typeof(StructLayoutAttribute).GetConstructor(new Type[] { typeof(LayoutKind) }), new object[] { LayoutKind.Explicit },
                                                                    typeof(StructLayoutAttribute).GetFields() , new object[]{32, 64, CharSet.Unicode}),
                                                              new CustomAttributeBuilder(s_guidPair.con, s_guidPair.args),
                                                              new CustomAttributeBuilder(typeof(SpecialNameAttribute).GetConstructor(Type.EmptyTypes), new object[] { })
                                                            };
                CustomAttributeBuilder[] fieldAttributes = new[] { new CustomAttributeBuilder(typeof(NonSerializedAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                                                              new CustomAttributeBuilder(typeof(FieldOffsetAttribute).GetConstructor(new Type[] { typeof(int) }), new object[] { 2 }),
                                                              new CustomAttributeBuilder(s_guidPair.con, s_guidPair.args),
                                                              new CustomAttributeBuilder(typeof(MarshalAsAttribute).GetConstructor(new Type[] { typeof(UnmanagedType) }), new object[] { UnmanagedType.I4}),
                                                              new CustomAttributeBuilder(typeof(SpecialNameAttribute).GetConstructor(Type.EmptyTypes), new object[] { })
                                                            };

                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(PopulateAssemblyName());
                TypeBuilder tb = ab.DefineDynamicModule("Module").DefineType(type.FullName, type.Attributes, type.BaseType);
                DefineFieldsAndSetAttributes(fieldAttributes.ToList(), type.GetFields(), tb);
                typeAttributes.ForEach(tb.SetCustomAttribute);
                tb.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Module moduleFromDisk = assemblyFromDisk.Modules.First();
                    Type testType = moduleFromDisk.GetTypes()[0];
                    IList<CustomAttributeData> attributesFromDisk = testType.GetCustomAttributesData();

                    Assert.Equal(typeAttributes.Count - 3, attributesFromDisk.Count); // 3 pseudo attributes 
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

                    Assert.Equal(3, fieldAttributesFromDisk.Count);
                    Assert.True((field.Attributes & FieldAttributes.NotSerialized) != 0); // NonSerializedAttribute
                    Assert.True((field.Attributes & FieldAttributes.SpecialName) != 0); // SpecialNameAttribute
                    Assert.True((field.Attributes & FieldAttributes.HasFieldMarshal) != 0); // MarshalAsAttribute

                    for (int i = 0; i < fieldAttributesFromDisk.Count; i++)
                    {
                        switch (fieldAttributesFromDisk[i].AttributeType.Name)
                        {
                            case "FieldOffsetAttribute":
                                Assert.Equal(2, fieldAttributesFromDisk[i].ConstructorArguments[0].Value);
                                break;
                            case "MarshalAsAttribute":
                                Assert.Equal(UnmanagedType.I4, (UnmanagedType)fieldAttributesFromDisk[i].ConstructorArguments[0].Value);
                                break;
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
        }

        [Fact]
        public void InterfacesWithPseudoCustomAttributes()
        {
            using (TempFile file = TempFile.Create())
            {
                Type dllType = typeof(DllImportAttribute);
                Type type = typeof(IMultipleMethod);
                List<CustomAttributeBuilder> typeAttributes = new() {
                        new CustomAttributeBuilder(typeof(ComImportAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                        new CustomAttributeBuilder(typeof(SuppressUnmanagedCodeSecurityAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                        new CustomAttributeBuilder(s_guidPair.con, s_guidPair.args)};
                List<CustomAttributeBuilder> methodAttributes = new() {
                        new CustomAttributeBuilder(typeof(PreserveSigAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                        new CustomAttributeBuilder(typeof(SuppressUnmanagedCodeSecurityAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                        new CustomAttributeBuilder(typeof(SpecialNameAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                        new CustomAttributeBuilder(s_guidPair.con, s_guidPair.args),
                        new CustomAttributeBuilder(typeof(MethodImplAttribute).GetConstructor(new Type[] { typeof(MethodImplOptions) }),
                                new object[] { MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization }),
                        new CustomAttributeBuilder(dllType.GetConstructor(new Type[] { typeof(string) }), new object[] { "test.dll" }, new FieldInfo[]
                              { dllType.GetField("CharSet"), dllType.GetField("SetLastError"), dllType.GetField("CallingConvention"), dllType.GetField("BestFitMapping"),
                                dllType.GetField("ThrowOnUnmappableChar") }, new object[]{ CharSet.Ansi, true, CallingConvention.FastCall, true, false })};
                List<CustomAttributeBuilder> parameterAttributes = new() {
                        new CustomAttributeBuilder(typeof(InAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                        new CustomAttributeBuilder(typeof(OutAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                        new CustomAttributeBuilder(typeof(OptionalAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
                        new CustomAttributeBuilder(s_guidPair.con, s_guidPair.args),
                        new CustomAttributeBuilder(marshalAsEnumCtor, new object[] { UnmanagedType.CustomMarshaler },
                                new FieldInfo[] { typeof(MarshalAsAttribute).GetField("MarshalType")}, new object[] { typeof(EmptyTestClass).AssemblyQualifiedName })};

                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(PopulateAssemblyName());
                TypeBuilder tb = ab.DefineDynamicModule("Module").DefineType(type.FullName, type.Attributes);
                typeAttributes.ForEach(tb.SetCustomAttribute);
                DefineMethodsAndSetAttributes(methodAttributes, tb, type.GetMethods(), parameterAttributes);
                tb.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Type testType = assemblyFromDisk.Modules.First().GetTypes()[0];
                    IList<CustomAttributeData> attributesFromDisk = testType.GetCustomAttributesData();

                    Assert.Equal(typeAttributes.Count, attributesFromDisk.Count);
                    Assert.True((testType.Attributes & TypeAttributes.Import) != 0); // ComImportAttribute
                    Assert.True((testType.Attributes & TypeAttributes.HasSecurity) != 0); // SuppressUnmanagedCodeSecurityAttribute
                    for (int i = 0; i < attributesFromDisk.Count; i++)
                    {
                        switch (attributesFromDisk[i].AttributeType.Name)
                        {
                            case "ComImportAttribute": // just making sure that these attributes are expected
                            case "SuppressUnmanagedCodeSecurityAttribute":
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
                        MethodImplAttributes methodImpl = method.GetMethodImplementationFlags();
                        Assert.True((methodImpl & MethodImplAttributes.NoInlining) != 0); // MethodImplAttribute
                        Assert.True((methodImpl & MethodImplAttributes.AggressiveOptimization) != 0); // MethodImplAttribute
                        Assert.True((methodImpl & MethodImplAttributes.PreserveSig) != 0); // PreserveSigAttribute
                        Assert.Equal(methodAttributes.Count - 2, methodAttributesFromDisk.Count);

                        for (int i = 0; i < methodAttributesFromDisk.Count; i++)
                        {
                            switch (methodAttributesFromDisk[i].AttributeType.Name)
                            {
                                case "SuppressUnmanagedCodeSecurityAttribute":
                                case "PreserveSigAttribute":
                                    break;
                                case "GuidAttribute":
                                    Assert.Equal(s_guidPair.args[0], methodAttributesFromDisk[i].ConstructorArguments[0].Value);
                                    break;
                                case "DllImportAttribute":
                                    {
                                        CustomAttributeData attribute = methodAttributesFromDisk[i];
                                        Assert.Equal("test.dll", attribute.ConstructorArguments[0].Value);

                                        for (int j = 0; j < attribute.NamedArguments.Count; j++)
                                        {
                                            switch (attribute.NamedArguments[j].MemberName)
                                            {
                                                case "CharSet":
                                                    Assert.Equal(CharSet.Ansi, (CharSet)attribute.NamedArguments[j].TypedValue.Value);
                                                    break;
                                                case "SetLastError":
                                                    Assert.True((bool)attribute.NamedArguments[j].TypedValue.Value);
                                                    break;
                                                case "CallingConvention":
                                                    Assert.Equal(CallingConvention.FastCall, (CallingConvention)attribute.NamedArguments[j].TypedValue.Value);
                                                    break;
                                                case "BestFitMapping":
                                                    Assert.True((bool)attribute.NamedArguments[j].TypedValue.Value);
                                                    break;
                                                case "ThrowOnUnmappableChar":
                                                    Assert.False((bool)attribute.NamedArguments[j].TypedValue.Value);
                                                    break;
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    Assert.Fail($"Not expected attribute : {methodAttributesFromDisk[i].AttributeType.Name}");
                                    break;
                            }
                        }

                        foreach (ParameterInfo param in method.GetParameters())
                        {
                            IList<CustomAttributeData> paramAttributes = param.GetCustomAttributesData();

                            Assert.Equal(5, paramAttributes.Count);
                            Assert.True((param.Attributes & ParameterAttributes.In) != 0); // InAttribute
                            Assert.True((param.Attributes & ParameterAttributes.Out) != 0); // OutAttribute
                            Assert.True((param.Attributes & ParameterAttributes.Optional) != 0); // OptionalAttribute
                            Assert.True((param.Attributes & ParameterAttributes.HasFieldMarshal) != 0); // MarshalAsAttribute

                            if (param.ParameterType.Equals(typeof(string)))
                            {
                                Assert.Equal("Hello", param.DefaultValue);
                            }

                            for (int i = 0; i < paramAttributes.Count; i++)
                            {
                                switch (paramAttributes[i].AttributeType.Name)
                                {
                                    case "InAttribute":
                                    case "OutAttribute":
                                    case "OptionalAttribute":
                                        break;
                                    case "MarshalAsAttribute":
                                        Assert.Equal(UnmanagedType.CustomMarshaler, (UnmanagedType)paramAttributes[i].ConstructorArguments[0].Value);
                                        Assert.Equal(typeof(EmptyTestClass).AssemblyQualifiedName,
                                            paramAttributes[i].NamedArguments.First(na => na.MemberName == "MarshalType").TypedValue.Value);
                                        break;
                                    case "GuidAttribute":
                                        Assert.Equal(s_guidPair.args[0], paramAttributes[i].ConstructorArguments[0].Value);
                                        break;
                                    default:
                                        Assert.Fail($"Not expected attribute : {paramAttributes[i].AttributeType.Name}");
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static readonly ConstructorInfo marshalAsEnumCtor = typeof(MarshalAsAttribute).GetConstructor(new Type[] { typeof(UnmanagedType) });
        private static readonly ConstructorInfo marshalAsShortCtor = typeof(MarshalAsAttribute).GetConstructor(new Type[] { typeof(short) });

        public static IEnumerable<object[]> MarshalAsAttributeWithVariousFields()
        {
            yield return new object[] { new CustomAttributeBuilder(marshalAsEnumCtor, new object[] { UnmanagedType.LPStr }), UnmanagedType.LPStr };
            yield return new object[] { new CustomAttributeBuilder(marshalAsShortCtor, new object[] { (short)21 }), UnmanagedType.LPWStr };
            yield return new object[] { new CustomAttributeBuilder(marshalAsShortCtor, new object[] { (short)19 }), UnmanagedType.BStr };
            yield return new object[] { new CustomAttributeBuilder(marshalAsEnumCtor, new object[] { UnmanagedType.ByValTStr },
                new FieldInfo[] { typeof(MarshalAsAttribute).GetField("SizeConst") }, new object[] { 256 }) , UnmanagedType.ByValTStr };
            yield return new object[] { new CustomAttributeBuilder(marshalAsEnumCtor, new object[] { UnmanagedType.CustomMarshaler },
                new FieldInfo[] { typeof(MarshalAsAttribute).GetField("MarshalType"), typeof(MarshalAsAttribute).GetField("MarshalCookie")  },
                new object[] { typeof(EmptyTestClass).AssemblyQualifiedName, "MyCookie" }) , UnmanagedType.CustomMarshaler };
            // TODO: When array support added add test for LPArray/ByValArray/SafeArray
        }

        [Theory]
        [MemberData(nameof(MarshalAsAttributeWithVariousFields))]
        public void MarshalAsPseudoCustomAttributesTest(CustomAttributeBuilder attribute, UnmanagedType expectedType)
        {
            using (TempFile file = TempFile.Create())
            {
                Type type = typeof(StructWithFields);
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(PopulateAssemblyName());
                TypeBuilder tb = ab.DefineDynamicModule("Module").DefineType(type.FullName, type.Attributes, type.BaseType);
                FieldInfo stringField = type.GetFields()[1];
                FieldBuilder fb = tb.DefineField(stringField.Name, stringField.FieldType, stringField.Attributes);
                fb.SetCustomAttribute(attribute);
                tb.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    FieldInfo field = assemblyFromDisk.Modules.First().GetTypes()[0].GetFields()[0];
                    CustomAttributeData attributeFromDisk = field.GetCustomAttributesData()[0];

                    Assert.Equal(1, field.GetCustomAttributesData().Count);
                    Assert.True((field.Attributes & FieldAttributes.HasFieldMarshal) != 0);
                    Assert.Equal(expectedType, (UnmanagedType)attributeFromDisk.ConstructorArguments[0].Value);

                    switch (expectedType)
                    {
                        case UnmanagedType.CustomMarshaler:
                            Assert.Equal(typeof(EmptyTestClass).AssemblyQualifiedName,
                                attributeFromDisk.NamedArguments.First(na => na.MemberName == "MarshalType").TypedValue.Value);
                            Assert.Equal("MyCookie", attributeFromDisk.NamedArguments.First(na => na.MemberName == "MarshalCookie").TypedValue.Value);
                            break;
                        case UnmanagedType.ByValTStr:
                            Assert.Equal(256, attributeFromDisk.NamedArguments.First(na => na.MemberName == "SizeConst").TypedValue.Value);
                            break;
                    }
                }
            }
        }

        [Fact]
        public void EnumBuilderSetCustomAttributesTest()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(PopulateAssemblyName());
                EnumBuilder enumBuilder = ab.DefineDynamicModule("Module").DefineEnum("TestEnum", TypeAttributes.Public, typeof(int));

                ConstructorInfo attributeConstructor = typeof(BoolAttribute).GetConstructor(new Type[] { typeof(bool) });
                CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(attributeConstructor, [true]);
                enumBuilder.SetCustomAttribute(attributeBuilder);
                enumBuilder.SetCustomAttribute(new CustomAttributeBuilder(s_guidPair.con, s_guidPair.args));
                enumBuilder.CreateTypeInfo();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type testEnum = mlc.LoadFromAssemblyPath(file.Path).Modules.First().GetType("TestEnum");

                    Assert.True(testEnum.IsEnum);
                    AssemblySaveTools.AssertTypeProperties(enumBuilder, testEnum);

                    CustomAttributeData[] attributes = testEnum.GetCustomAttributesData().ToArray();
                    if (attributes[0].AttributeType.Name == s_guidType.Name)
                    {
                        AssertEnumAttributes(s_guidType.FullName, "9ED54F84-A89D-4fcd-A854-44251E925F09", attributes[0]);
                        AssertEnumAttributes(typeof(BoolAttribute).FullName, true, attributes[1]);
                    }
                    else
                    {
                        AssertEnumAttributes(s_guidType.FullName, "9ED54F84-A89D-4fcd-A854-44251E925F09", attributes[1]);
                        AssertEnumAttributes(typeof(BoolAttribute).FullName, true, attributes[0]);
                    }

                }
            }
        }

        private void AssertEnumAttributes(string fullName, object value, CustomAttributeData testAttribute)
        {
            Assert.Equal(fullName, testAttribute.AttributeType.FullName);
            Assert.Equal(value, testAttribute.ConstructorArguments[0].Value);
        }
    }

    public class BoolAttribute : Attribute
    {
        private bool _b;
        public BoolAttribute(bool myBool) { _b = myBool; }
    }
}
