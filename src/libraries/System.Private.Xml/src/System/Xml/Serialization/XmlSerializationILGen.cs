// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Text.RegularExpressions;
    using System.Xml.Extensions;

    internal class XmlSerializationILGen
    {
        private int _nextMethodNumber;
        private readonly Dictionary<TypeMapping, string> _methodNames = new Dictionary<TypeMapping, string>();
        // Lookup name->created Method
        private readonly Dictionary<string, MethodBuilderInfo> _methodBuilders = new Dictionary<string, MethodBuilderInfo>();
        // Lookup name->created Type
        internal Dictionary<string, Type> CreatedTypes = new Dictionary<string, Type>();
        // Lookup name->class Member
        internal Dictionary<string, MemberInfo> memberInfos = new Dictionary<string, MemberInfo>();
        private readonly ReflectionAwareILGen _raCodeGen;
        private readonly TypeScope[] _scopes;
        private readonly TypeDesc? _stringTypeDesc;
        private readonly TypeDesc? _qnameTypeDesc;
        private readonly string _className;
        private TypeMapping[]? _referencedMethods;
        private int _references;
        private readonly HashSet<TypeMapping> _generatedMethods = new HashSet<TypeMapping>();
        private ModuleBuilder? _moduleBuilder;
        private readonly TypeAttributes _typeAttributes;
        protected TypeBuilder typeBuilder = null!;
        protected CodeGenerator ilg = null!;

        [RequiresUnreferencedCode("Calls GetTypeDesc")]
        internal XmlSerializationILGen(TypeScope[] scopes, string access, string className)
        {
            _scopes = scopes;
            if (scopes.Length > 0)
            {
                _stringTypeDesc = scopes[0].GetTypeDesc(typeof(string));
                _qnameTypeDesc = scopes[0].GetTypeDesc(typeof(XmlQualifiedName));
            }
            _raCodeGen = new ReflectionAwareILGen();
            _className = className;
            System.Diagnostics.Debug.Assert(access == "public");
            _typeAttributes = TypeAttributes.Public;
        }

        internal int NextMethodNumber { get { return _nextMethodNumber; } set { _nextMethodNumber = value; } }
        internal ReflectionAwareILGen RaCodeGen { get { return _raCodeGen; } }
        internal TypeDesc? StringTypeDesc { get { return _stringTypeDesc; } }
        internal TypeDesc? QnameTypeDesc { get { return _qnameTypeDesc; } }
        internal string ClassName { get { return _className; } }
        internal TypeScope[] Scopes { get { return _scopes; } }
        internal Dictionary<TypeMapping, string> MethodNames { get { return _methodNames; } }
        internal HashSet<TypeMapping> GeneratedMethods { get { return _generatedMethods; } }

        internal ModuleBuilder ModuleBuilder
        {
            get { System.Diagnostics.Debug.Assert(_moduleBuilder != null); return _moduleBuilder; }
            set { System.Diagnostics.Debug.Assert(_moduleBuilder == null && value != null); _moduleBuilder = value; }
        }
        internal TypeAttributes TypeAttributes { get { return _typeAttributes; } }

        internal MethodBuilder EnsureMethodBuilder(TypeBuilder typeBuilder, string methodName,
            MethodAttributes attributes, Type? returnType, Type[] parameterTypes)
        {
            MethodBuilderInfo? methodBuilderInfo;
            if (!_methodBuilders.TryGetValue(methodName, out methodBuilderInfo))
            {
                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                    methodName,
                    attributes,
                    returnType,
                    parameterTypes);
                methodBuilderInfo = new MethodBuilderInfo(methodBuilder, parameterTypes);
                _methodBuilders.Add(methodName, methodBuilderInfo);
            }
#if DEBUG
            else
            {
                methodBuilderInfo.Validate(returnType, parameterTypes, attributes);
            }
#endif
            return methodBuilderInfo.MethodBuilder;
        }

        internal MethodBuilderInfo GetMethodBuilder(string methodName)
        {
            System.Diagnostics.Debug.Assert(_methodBuilders.ContainsKey(methodName));
            return _methodBuilders[methodName];
        }
        [RequiresUnreferencedCode("calls WriteStructMethod")]
        internal virtual void GenerateMethod(TypeMapping mapping) { }

        [RequiresUnreferencedCode("calls GenerateMethod")]
        internal void GenerateReferencedMethods()
        {
            while (_references > 0)
            {
                TypeMapping mapping = _referencedMethods![--_references];
                GenerateMethod(mapping);
            }
        }

        internal string? ReferenceMapping(TypeMapping mapping)
        {
            if (!_generatedMethods.Contains(mapping))
            {
                _referencedMethods = EnsureArrayIndex(_referencedMethods, _references);
                _referencedMethods[_references++] = mapping;
            }

            string? methodName;
            _methodNames.TryGetValue(mapping, out methodName);
            return methodName;
        }

        private static TypeMapping[] EnsureArrayIndex(TypeMapping[]? a, int index)
        {
            if (a == null) return new TypeMapping[32];
            if (index < a.Length) return a;
            TypeMapping[] b = new TypeMapping[a.Length + 32];
            Array.Copy(a, b, index);
            return b;
        }

        [return: NotNullIfNotNull("value")]
        internal static string? GetCSharpString(string? value)
        {
            return ReflectionAwareILGen.GetCSharpString(value);
        }

        [RequiresUnreferencedCode("calls LoadMember")]
        internal FieldBuilder GenerateHashtableGetBegin(string privateName, string publicName, TypeBuilder serializerContractTypeBuilder)
        {
            FieldBuilder fieldBuilder = serializerContractTypeBuilder.DefineField(
                privateName,
                typeof(Hashtable),
                FieldAttributes.Private
                );
            ilg = new CodeGenerator(serializerContractTypeBuilder);
            PropertyBuilder propertyBuilder = serializerContractTypeBuilder.DefineProperty(
                publicName,
                PropertyAttributes.None,
                CallingConventions.HasThis,
                typeof(Hashtable),
                null, null, null, null, null);

            ilg.BeginMethod(
                typeof(Hashtable),
                $"get_{publicName}",
                Type.EmptyTypes,
                Array.Empty<string>(),
                CodeGenerator.PublicOverrideMethodAttributes | MethodAttributes.SpecialName);
            propertyBuilder.SetGetMethod(ilg.MethodBuilder!);

            ilg.Ldarg(0);
            ilg.LoadMember(fieldBuilder);
            ilg.Load(null);
            // this 'if' ends in GenerateHashtableGetEnd
            ilg.If(Cmp.EqualTo);

            ConstructorInfo Hashtable_ctor = typeof(Hashtable).GetConstructor(
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            LocalBuilder _tmpLoc = ilg.DeclareLocal(typeof(Hashtable), "_tmp");
            ilg.New(Hashtable_ctor);
            ilg.Stloc(_tmpLoc);

            return fieldBuilder;
        }

        [RequiresUnreferencedCode("calls LoadMember")]
        internal void GenerateHashtableGetEnd(FieldBuilder fieldBuilder)
        {
            ilg!.Ldarg(0);
            ilg.LoadMember(fieldBuilder);
            ilg.Load(null);
            ilg.If(Cmp.EqualTo);
            {
                ilg.Ldarg(0);
                ilg.Ldloc(typeof(Hashtable), "_tmp");
                ilg.StoreMember(fieldBuilder);
            }
            ilg.EndIf();
            // 'endif' from GenerateHashtableGetBegin
            ilg.EndIf();

            ilg.Ldarg(0);
            ilg.LoadMember(fieldBuilder);
            ilg.GotoMethodEnd();

            ilg.EndMethod();
        }

        [RequiresUnreferencedCode("calls GenerateHashtableGetBegin")]
        internal FieldBuilder GeneratePublicMethods(string privateName, string publicName, string[] methods, XmlMapping[] xmlMappings, TypeBuilder serializerContractTypeBuilder)
        {
            FieldBuilder fieldBuilder = GenerateHashtableGetBegin(privateName, publicName, serializerContractTypeBuilder);
            if (methods != null && methods.Length != 0 && xmlMappings != null && xmlMappings.Length == methods.Length)
            {
                MethodInfo Hashtable_set_Item = typeof(Hashtable).GetMethod(
                    "set_Item",
                    new Type[] { typeof(object), typeof(object) }
                    )!;
                for (int i = 0; i < methods.Length; i++)
                {
                    if (methods[i] == null)
                        continue;
                    ilg!.Ldloc(typeof(Hashtable), "_tmp");
                    ilg.Ldstr(GetCSharpString(xmlMappings[i].Key));
                    ilg.Ldstr(GetCSharpString(methods[i]));
                    ilg.Call(Hashtable_set_Item);
                }
            }
            GenerateHashtableGetEnd(fieldBuilder);
            return fieldBuilder;
        }

        internal void GenerateSupportedTypes(Type[] types, TypeBuilder serializerContractTypeBuilder)
        {
            ilg = new CodeGenerator(serializerContractTypeBuilder);
            ilg.BeginMethod(
                typeof(bool),
                "CanSerialize",
                new Type[] { typeof(Type) },
                new string[] { "type" },
                CodeGenerator.PublicOverrideMethodAttributes);
            var uniqueTypes = new HashSet<Type>();
            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];

                if (type == null)
                    continue;
                if (!type.IsPublic && !type.IsNestedPublic)
                    continue;
                if (!uniqueTypes.Add(type))
                    continue;
                // DDB172141: Wrong generated CS for serializer of List<string> type
                if (type.IsGenericType || type.ContainsGenericParameters)
                    continue;
                ilg.Ldarg("type");
                ilg.Ldc(type);
                ilg.If(Cmp.EqualTo);
                {
                    ilg.Ldc(true);
                    ilg.GotoMethodEnd();
                }
                ilg.EndIf();
            }
            ilg.Ldc(false);
            ilg.GotoMethodEnd();
            ilg.EndMethod();
        }

        [RequiresUnreferencedCode("Uses CreatedTypes Dictionary")]
        internal string GenerateBaseSerializer(string baseSerializer, string readerClass, string writerClass, CodeIdentifiers classes)
        {
            baseSerializer = CodeIdentifier.MakeValid(baseSerializer);
            baseSerializer = classes.AddUnique(baseSerializer, baseSerializer);

            TypeBuilder baseSerializerTypeBuilder = CodeGenerator.CreateTypeBuilder(
                _moduleBuilder!,
                CodeIdentifier.GetCSharpName(baseSerializer),
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                typeof(XmlSerializer),
                Type.EmptyTypes);

            ConstructorInfo readerCtor = CreatedTypes[readerClass].GetConstructor(
               CodeGenerator.InstanceBindingFlags,
               Type.EmptyTypes
               )!;
            ilg = new CodeGenerator(baseSerializerTypeBuilder);
            ilg.BeginMethod(typeof(XmlSerializationReader),
                "CreateReader",
                Type.EmptyTypes,
                Array.Empty<string>(),
                CodeGenerator.ProtectedOverrideMethodAttributes);
            ilg.New(readerCtor);
            ilg.EndMethod();

            ConstructorInfo writerCtor = CreatedTypes[writerClass].GetConstructor(
               CodeGenerator.InstanceBindingFlags,
               Type.EmptyTypes
               )!;
            ilg.BeginMethod(typeof(XmlSerializationWriter),
                "CreateWriter",
                Type.EmptyTypes,
                Array.Empty<string>(),
                CodeGenerator.ProtectedOverrideMethodAttributes);
            ilg.New(writerCtor);
            ilg.EndMethod();

            baseSerializerTypeBuilder.DefineDefaultConstructor(
                MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            Type baseSerializerType = baseSerializerTypeBuilder.CreateTypeInfo()!.AsType();
            CreatedTypes.Add(baseSerializerType.Name, baseSerializerType);

            return baseSerializer;
        }

        [RequiresUnreferencedCode("uses CreatedTypes dictionary")]
        internal string GenerateTypedSerializer(string readMethod, string writeMethod, XmlMapping mapping, CodeIdentifiers classes, string baseSerializer, string readerClass, string writerClass)
        {
            string serializerName = CodeIdentifier.MakeValid(Accessor.UnescapeName(mapping.Accessor.Mapping!.TypeDesc!.Name));
            serializerName = classes.AddUnique($"{serializerName}Serializer", mapping);

            TypeBuilder typedSerializerTypeBuilder = CodeGenerator.CreateTypeBuilder(
                _moduleBuilder!,
                CodeIdentifier.GetCSharpName(serializerName),
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                CreatedTypes[baseSerializer],
                Type.EmptyTypes
                );

            ilg = new CodeGenerator(typedSerializerTypeBuilder);
            ilg.BeginMethod(
                typeof(bool),
                "CanDeserialize",
                new Type[] { typeof(XmlReader) },
                new string[] { "xmlReader" },
                CodeGenerator.PublicOverrideMethodAttributes
            );

            if (mapping.Accessor.Any)
            {
                ilg.Ldc(true);
                ilg.Stloc(ilg.ReturnLocal);
                ilg.Br(ilg.ReturnLabel);
            }
            else
            {
                MethodInfo XmlReader_IsStartElement = typeof(XmlReader).GetMethod(
                     "IsStartElement",
                     CodeGenerator.InstanceBindingFlags,
                     new Type[] { typeof(string), typeof(string) }
                     )!;
                ilg.Ldarg(ilg.GetArg("xmlReader"));
                ilg.Ldstr(GetCSharpString(mapping.Accessor.Name));
                ilg.Ldstr(GetCSharpString(mapping.Accessor.Namespace));
                ilg.Call(XmlReader_IsStartElement);
                ilg.Stloc(ilg.ReturnLocal);
                ilg.Br(ilg.ReturnLabel);
            }
            ilg.MarkLabel(ilg.ReturnLabel);
            ilg.Ldloc(ilg.ReturnLocal);
            ilg.EndMethod();

            if (writeMethod != null)
            {
                ilg = new CodeGenerator(typedSerializerTypeBuilder);
                ilg.BeginMethod(
                    typeof(void),
                    "Serialize",
                    new Type[] { typeof(object), typeof(XmlSerializationWriter) },
                    new string[] { "objectToSerialize", "writer" },
                    CodeGenerator.ProtectedOverrideMethodAttributes);
                MethodInfo writerType_writeMethod = CreatedTypes[writerClass].GetMethod(
                    writeMethod,
                    CodeGenerator.InstanceBindingFlags,
                    new Type[] { (mapping is XmlMembersMapping) ? typeof(object[]) : typeof(object) }
                    )!;
                ilg.Ldarg("writer");
                ilg.Castclass(CreatedTypes[writerClass]);
                ilg.Ldarg("objectToSerialize");
                if (mapping is XmlMembersMapping)
                {
                    ilg.ConvertValue(typeof(object), typeof(object[]));
                }
                ilg.Call(writerType_writeMethod);
                ilg.EndMethod();
            }
            if (readMethod != null)
            {
                ilg = new CodeGenerator(typedSerializerTypeBuilder);
                ilg.BeginMethod(
                    typeof(object),
                    "Deserialize",
                    new Type[] { typeof(XmlSerializationReader) },
                    new string[] { "reader" },
                    CodeGenerator.ProtectedOverrideMethodAttributes);
                MethodInfo readerType_readMethod = CreatedTypes[readerClass].GetMethod(
                    readMethod,
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ilg.Ldarg("reader");
                ilg.Castclass(CreatedTypes[readerClass]);
                ilg.Call(readerType_readMethod);
                ilg.EndMethod();
            }
            typedSerializerTypeBuilder.DefineDefaultConstructor(CodeGenerator.PublicMethodAttributes);
            Type typedSerializerType = typedSerializerTypeBuilder.CreateTypeInfo()!.AsType();
            CreatedTypes.Add(typedSerializerType.Name, typedSerializerType);

            return typedSerializerType.Name;
        }

        [RequiresUnreferencedCode("calls GetConstructor")]
        private FieldBuilder GenerateTypedSerializers(Dictionary<string, string> serializers, TypeBuilder serializerContractTypeBuilder)
        {
            string privateName = "typedSerializers";
            FieldBuilder fieldBuilder = GenerateHashtableGetBegin(privateName, "TypedSerializers", serializerContractTypeBuilder);
            MethodInfo Hashtable_Add = typeof(Hashtable).GetMethod(
                "Add",
                CodeGenerator.InstanceBindingFlags,
                new Type[] { typeof(object), typeof(object) }
                )!;

            foreach (string key in serializers.Keys)
            {
                ConstructorInfo ctor = CreatedTypes[(string)serializers[key]].GetConstructor(
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ilg!.Ldloc(typeof(Hashtable), "_tmp");
                ilg.Ldstr(GetCSharpString(key));
                ilg.New(ctor);
                ilg.Call(Hashtable_Add);
            }
            GenerateHashtableGetEnd(fieldBuilder);
            return fieldBuilder;
        }

        //GenerateGetSerializer(serializers, xmlMappings);
        [RequiresUnreferencedCode("Uses CreatedTypes Dictionary")]
        private void GenerateGetSerializer(Dictionary<string, string> serializers, XmlMapping[] xmlMappings, TypeBuilder serializerContractTypeBuilder)
        {
            ilg = new CodeGenerator(serializerContractTypeBuilder);
            ilg.BeginMethod(
                typeof(XmlSerializer),
                "GetSerializer",
                new Type[] { typeof(Type) },
                new string[] { "type" },
                CodeGenerator.PublicOverrideMethodAttributes);

            for (int i = 0; i < xmlMappings.Length; i++)
            {
                if (xmlMappings[i] is XmlTypeMapping)
                {
                    Type? type = xmlMappings[i].Accessor.Mapping!.TypeDesc!.Type;
                    if (type == null)
                        continue;
                    if (!type.IsPublic && !type.IsNestedPublic)
                        continue;
                    // DDB172141: Wrong generated CS for serializer of List<string> type
                    if (type.IsGenericType || type.ContainsGenericParameters)
                        continue;
                    ilg.Ldarg("type");
                    ilg.Ldc(type);
                    ilg.If(Cmp.EqualTo);
                    {
                        ConstructorInfo ctor = CreatedTypes[(string)serializers[xmlMappings[i].Key!]].GetConstructor(
                            CodeGenerator.InstanceBindingFlags,
                            Type.EmptyTypes
                            )!;
                        ilg.New(ctor);
                        ilg.Stloc(ilg.ReturnLocal);
                        ilg.Br(ilg.ReturnLabel);
                    }
                    ilg.EndIf();
                }
            }
            ilg.Load(null);
            ilg.Stloc(ilg.ReturnLocal);
            ilg.Br(ilg.ReturnLabel);
            ilg.MarkLabel(ilg.ReturnLabel);
            ilg.Ldloc(ilg.ReturnLocal);
            ilg.EndMethod();
        }

        [RequiresUnreferencedCode("calls GenerateTypedSerializers")]
        internal void GenerateSerializerContract(string className, XmlMapping[] xmlMappings, Type[] types, string readerType, string[] readMethods, string writerType, string[] writerMethods, Dictionary<string, string> serializers)
        {
            TypeBuilder serializerContractTypeBuilder = CodeGenerator.CreateTypeBuilder(
                _moduleBuilder!,
                "XmlSerializerContract",
                TypeAttributes.Public | TypeAttributes.BeforeFieldInit,
                typeof(XmlSerializerImplementation),
                Type.EmptyTypes
                );

            ilg = new CodeGenerator(serializerContractTypeBuilder);
            PropertyBuilder propertyBuilder = serializerContractTypeBuilder.DefineProperty(
                "Reader",
                PropertyAttributes.None,
                typeof(XmlSerializationReader),
                null, null, null, null, null);
            ilg.BeginMethod(
                typeof(XmlSerializationReader),
                "get_Reader",
                Type.EmptyTypes,
                Array.Empty<string>(),
                CodeGenerator.PublicOverrideMethodAttributes | MethodAttributes.SpecialName);
            propertyBuilder.SetGetMethod(ilg.MethodBuilder!);
            ConstructorInfo ctor = CreatedTypes[readerType].GetConstructor(
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            ilg.New(ctor);
            ilg.EndMethod();

            ilg = new CodeGenerator(serializerContractTypeBuilder);
            propertyBuilder = serializerContractTypeBuilder.DefineProperty(
                "Writer",
                PropertyAttributes.None,
                typeof(XmlSerializationWriter),
                null, null, null, null, null);
            ilg.BeginMethod(
                typeof(XmlSerializationWriter),
                "get_Writer",
                Type.EmptyTypes,
                Array.Empty<string>(),
                CodeGenerator.PublicOverrideMethodAttributes | MethodAttributes.SpecialName);
            propertyBuilder.SetGetMethod(ilg.MethodBuilder!);
            ctor = CreatedTypes[writerType].GetConstructor(
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            ilg.New(ctor);
            ilg.EndMethod();

            FieldBuilder readMethodsField = GeneratePublicMethods(nameof(readMethods), "ReadMethods", readMethods, xmlMappings, serializerContractTypeBuilder);
            FieldBuilder writeMethodsField = GeneratePublicMethods("writeMethods", "WriteMethods", writerMethods, xmlMappings, serializerContractTypeBuilder);
            FieldBuilder typedSerializersField = GenerateTypedSerializers(serializers, serializerContractTypeBuilder);
            GenerateSupportedTypes(types, serializerContractTypeBuilder);
            GenerateGetSerializer(serializers, xmlMappings, serializerContractTypeBuilder);

            // Default ctor
            ConstructorInfo baseCtor = typeof(XmlSerializerImplementation).GetConstructor(
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            ilg = new CodeGenerator(serializerContractTypeBuilder);
            ilg.BeginMethod(
                typeof(void),
                ".ctor",
                Type.EmptyTypes,
                Array.Empty<string>(),
                CodeGenerator.PublicMethodAttributes | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName
                );
            ilg.Ldarg(0);
            ilg.Load(null);
            ilg.StoreMember(readMethodsField);
            ilg.Ldarg(0);
            ilg.Load(null);
            ilg.StoreMember(writeMethodsField);
            ilg.Ldarg(0);
            ilg.Load(null);
            ilg.StoreMember(typedSerializersField);
            ilg.Ldarg(0);
            ilg.Call(baseCtor);
            ilg.EndMethod();
            // Instantiate type
            Type serializerContractType = serializerContractTypeBuilder.CreateTypeInfo()!.AsType();
            CreatedTypes.Add(serializerContractType.Name, serializerContractType);
        }

        internal static bool IsWildcard(SpecialMapping mapping)
        {
            if (mapping is SerializableMapping)
                return ((SerializableMapping)mapping).IsAny;
            return mapping.TypeDesc!.CanBeElementValue;
        }

        [RequiresUnreferencedCode("calls ILGenLoad")]
        internal void ILGenLoad(string source)
        {
            ILGenLoad(source, null);
        }

        [RequiresUnreferencedCode("calls LoadMember")]
        internal void ILGenLoad(string source, Type? type)
        {
            if (source.StartsWith("o.@", StringComparison.Ordinal))
            {
                System.Diagnostics.Debug.Assert(memberInfos.ContainsKey(source.Substring(3)));
                MemberInfo memInfo = memberInfos[source.Substring(3)];
                ilg!.LoadMember(ilg.GetVariable("o"), memInfo);
                if (type != null)
                {
                    Type memType = (memInfo is FieldInfo) ? ((FieldInfo)memInfo).FieldType : ((PropertyInfo)memInfo).PropertyType;
                    ilg.ConvertValue(memType, type);
                }
            }
            else
            {
                SourceInfo info = new SourceInfo(source, null, null, null, ilg!);
                info.Load(type);
            }
        }
    }
}
