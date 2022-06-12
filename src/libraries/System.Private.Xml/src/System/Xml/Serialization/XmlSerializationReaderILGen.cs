// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Security;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Schema;
    using System.Diagnostics.CodeAnalysis;

    internal sealed partial class XmlSerializationReaderILGen : XmlSerializationILGen
    {
        private readonly Dictionary<string, string> _idNames = new Dictionary<string, string>();
        // Mapping name->id_XXXNN field
        private readonly Dictionary<string, FieldBuilder> _idNameFields = new Dictionary<string, FieldBuilder>();
        private Dictionary<string, EnumMapping>? _enums;
        private int _nextIdNumber;

        internal Dictionary<string, EnumMapping> Enums
        {
            get
            {
                if (_enums == null)
                {
                    _enums = new Dictionary<string, EnumMapping>();
                }
                return _enums;
            }
        }

        private sealed class Member
        {
            private readonly string _source;
            private readonly string _arrayName;
            private readonly string _arraySource;
            private readonly string _choiceArrayName;
            private readonly string? _choiceSource;
            private readonly string? _choiceArraySource;
            private readonly MemberMapping _mapping;
            private readonly bool _isArray;
            private readonly bool _isList;
            private bool _isNullable;
            private int _fixupIndex = -1;
            private string? _paramsReadSource;
            private string? _checkSpecifiedSource;

            internal Member(XmlSerializationReaderILGen outerClass, string source, string? arrayName, int i, MemberMapping mapping)
                : this(outerClass, source, null, arrayName, i, mapping, false, null)
            {
            }
            internal Member(XmlSerializationReaderILGen outerClass, string source, string? arrayName, int i, MemberMapping mapping, string? choiceSource)
                : this(outerClass, source, null, arrayName, i, mapping, false, choiceSource)
            {
            }
            internal Member(XmlSerializationReaderILGen outerClass, string source, string? arraySource, string? arrayName, int i, MemberMapping mapping)
                : this(outerClass, source, arraySource, arrayName, i, mapping, false, null)
            {
            }
            internal Member(XmlSerializationReaderILGen outerClass, string source, string? arraySource, string? arrayName, int i, MemberMapping mapping, string? choiceSource)
                : this(outerClass, source, arraySource, arrayName, i, mapping, false, choiceSource)
            {
            }
            internal Member(XmlSerializationReaderILGen outerClass, string source, string? arrayName, int i, MemberMapping mapping, bool multiRef)
                : this(outerClass, source, null, arrayName, i, mapping, multiRef, null)
            {
            }
            internal Member(XmlSerializationReaderILGen outerClass, string source, string? arraySource, string? arrayName, int i, MemberMapping mapping, bool multiRef, string? choiceSource)
            {
                _source = source;
                _arrayName = string.Create(CultureInfo.InvariantCulture, $"{arrayName}_{i}");
                _choiceArrayName = $"choice_{_arrayName}";
                _choiceSource = choiceSource;

                if (mapping.TypeDesc!.IsArrayLike)
                {
                    if (arraySource != null)
                        _arraySource = arraySource;
                    else
                        _arraySource = XmlSerializationReaderILGen.GetArraySource(mapping.TypeDesc, _arrayName, multiRef);
                    _isArray = mapping.TypeDesc.IsArray;
                    _isList = !_isArray;
                    if (mapping.ChoiceIdentifier != null)
                    {
                        _choiceArraySource = XmlSerializationReaderILGen.GetArraySource(mapping.TypeDesc, _choiceArrayName, multiRef);

                        string a = _choiceArrayName;
                        string c = $"c{a}";
                        string choiceTypeFullName = mapping.ChoiceIdentifier.Mapping!.TypeDesc!.CSharpName;
                        string castString = $"({choiceTypeFullName}[])";

                        string init = $"{a} = {castString}EnsureArrayIndex({a}, {c}, {ReflectionAwareILGen.GetStringForTypeof(choiceTypeFullName)});";
                        _choiceArraySource = init + ReflectionAwareILGen.GetStringForArrayMember(a, $"{c}++", mapping.ChoiceIdentifier.Mapping.TypeDesc);
                    }
                    else
                    {
                        _choiceArraySource = _choiceSource;
                    }
                }
                else
                {
                    _arraySource = arraySource ?? source;
                    _choiceArraySource = _choiceSource;
                }
                _mapping = mapping;
            }

            internal MemberMapping Mapping
            {
                get { return _mapping; }
            }

            internal string Source
            {
                get { return _source; }
            }

            internal string ArrayName
            {
                get { return _arrayName; }
            }

            internal string ArraySource
            {
                get { return _arraySource; }
            }

            internal bool IsList
            {
                get { return _isList; }
            }

            internal bool IsArrayLike
            {
                get { return (_isArray || _isList); }
            }

            internal bool IsNullable
            {
                get { return _isNullable; }
                set { _isNullable = value; }
            }

            internal int FixupIndex
            {
                get { return _fixupIndex; }
                set { _fixupIndex = value; }
            }

            internal string? ParamsReadSource
            {
                get { return _paramsReadSource; }
                set { _paramsReadSource = value; }
            }

            internal string? CheckSpecifiedSource
            {
                get { return _checkSpecifiedSource; }
                set { _checkSpecifiedSource = value; }
            }

            internal string? ChoiceSource
            {
                get { return _choiceSource; }
            }
            internal string ChoiceArrayName
            {
                get { return _choiceArrayName; }
            }
            internal string? ChoiceArraySource
            {
                get { return _choiceArraySource; }
            }
        }

        [RequiresUnreferencedCode("Creates XmlSerializationILGen")]
        internal XmlSerializationReaderILGen(TypeScope[] scopes, string access, string className)
            : base(scopes, access, className)
        {
        }

        [RequiresUnreferencedCode("calls WriteReflectionInit")]
        internal void GenerateBegin()
        {
            this.typeBuilder = CodeGenerator.CreateTypeBuilder(
                ModuleBuilder,
                ClassName,
                TypeAttributes | TypeAttributes.BeforeFieldInit,
                typeof(XmlSerializationReader),
                Type.EmptyTypes);
            foreach (TypeScope scope in Scopes)
            {
                foreach (TypeMapping mapping in scope.TypeMappings)
                {
                    if (mapping is StructMapping || mapping is EnumMapping || mapping is NullableMapping)
                        MethodNames.Add(mapping, NextMethodName(mapping.TypeDesc!.Name));
                }
                ReflectionAwareILGen.WriteReflectionInit(scope);
            }
        }

        [RequiresUnreferencedCode("calls WriteStructMethod")]
        internal override void GenerateMethod(TypeMapping mapping)
        {
            if (!GeneratedMethods.Add(mapping))
                return;

            if (mapping is StructMapping)
            {
                WriteStructMethod((StructMapping)mapping);
            }
            else if (mapping is EnumMapping)
            {
                WriteEnumMethod((EnumMapping)mapping);
            }
            else if (mapping is NullableMapping)
            {
                WriteNullableMethod((NullableMapping)mapping);
            }
        }

        [RequiresUnreferencedCode("calls GenerateReferencedMethods")]
        internal void GenerateEnd(string[] methods, XmlMapping[] xmlMappings, Type[] types)
        {
            GenerateReferencedMethods();
            GenerateInitCallbacksMethod();

            ilg = new CodeGenerator(this.typeBuilder);
            ilg.BeginMethod(typeof(void), "InitIDs", Type.EmptyTypes, Array.Empty<string>(),
                CodeGenerator.ProtectedOverrideMethodAttributes);
            MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                 "get_Reader",
                 CodeGenerator.InstanceBindingFlags,
                 Type.EmptyTypes
                 )!;
            MethodInfo XmlReader_get_NameTable = typeof(XmlReader).GetMethod(
                "get_NameTable",
                CodeGenerator.InstanceBindingFlags,
                 Type.EmptyTypes
                )!;
            MethodInfo XmlNameTable_Add = typeof(XmlNameTable).GetMethod(
                "Add",
                CodeGenerator.InstanceBindingFlags,
                new Type[] { typeof(string) }
                )!;
            foreach (string id in _idNames.Keys)
            {
                ilg.Ldarg(0);
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_get_NameTable);
                ilg.Ldstr(GetCSharpString(id));
                ilg.Call(XmlNameTable_Add);
                Debug.Assert(_idNameFields.ContainsKey(id));
                ilg.StoreMember(_idNameFields[id]);
            }
            ilg.EndMethod();

            this.typeBuilder.DefineDefaultConstructor(
                CodeGenerator.PublicMethodAttributes);
            Type readerType = this.typeBuilder.CreateTypeInfo()!.AsType();
            CreatedTypes.Add(readerType.Name, readerType);
        }

        [RequiresUnreferencedCode("calls GenerateMembersElement")]
        internal string? GenerateElement(XmlMapping xmlMapping)
        {
            if (!xmlMapping.IsReadable)
                return null;
            if (!xmlMapping.GenerateSerializer)
                throw new ArgumentException(SR.XmlInternalError, nameof(xmlMapping));
            if (xmlMapping is XmlTypeMapping)
                return GenerateTypeElement((XmlTypeMapping)xmlMapping);
            else if (xmlMapping is XmlMembersMapping)
                return GenerateMembersElement((XmlMembersMapping)xmlMapping);
            else
                throw new ArgumentException(SR.XmlInternalError, nameof(xmlMapping));
        }

        [RequiresUnreferencedCode("calls LoadMember")]
        private void WriteIsStartTag(string? name, string? ns)
        {
            WriteID(name);
            WriteID(ns);
            MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                 "get_Reader",
                 CodeGenerator.InstanceBindingFlags,
                 Type.EmptyTypes
                 )!;
            MethodInfo XmlReader_IsStartElement = typeof(XmlReader).GetMethod(
                 "IsStartElement",
                 CodeGenerator.InstanceBindingFlags,
                 new Type[] { typeof(string), typeof(string) }
                 )!;
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_get_Reader);
            ilg.Ldarg(0);
            ilg.LoadMember(_idNameFields[name ?? string.Empty]);
            ilg.Ldarg(0);
            ilg.LoadMember(_idNameFields[ns ?? string.Empty]);
            ilg.Call(XmlReader_IsStartElement);
            ilg.If();
        }

        [RequiresUnreferencedCode("XmlSerializationReader methods have RequiresUnreferencedCode")]
        private void WriteUnknownNode(string func, string node, ElementAccessor? e, bool anyIfs)
        {
            if (anyIfs)
            {
                ilg.Else();
            }
            List<Type> argTypes = new List<Type>();
            ilg.Ldarg(0);
            Debug.Assert(node == "null" || node == "(object)p");
            if (node == "null")
                ilg.Load(null);
            else
            {
                object pVar = ilg.GetVariable("p");
                ilg.Load(pVar);
                ilg.ConvertValue(CodeGenerator.GetVariableType(pVar), typeof(object));
            }
            argTypes.Add(typeof(object));
            if (e != null)
            {
                string? expectedElement = e.Form == XmlSchemaForm.Qualified ? e.Namespace : "";
                expectedElement += ":";
                expectedElement += e.Name;
                ilg.Ldstr(ReflectionAwareILGen.GetCSharpString(expectedElement));
                argTypes.Add(typeof(string));
            }
            MethodInfo method = typeof(XmlSerializationReader).GetMethod(
                func,
                CodeGenerator.InstanceBindingFlags,
                argTypes.ToArray()
                )!;
            ilg.Call(method);
            if (anyIfs)
            {
                ilg.EndIf();
            }
        }

        private void GenerateInitCallbacksMethod()
        {
            ilg = new CodeGenerator(this.typeBuilder);
            ilg.BeginMethod(typeof(void), "InitCallbacks", Type.EmptyTypes, Array.Empty<string>(),
                CodeGenerator.ProtectedOverrideMethodAttributes);
            ilg.EndMethod();
        }

        [RequiresUnreferencedCode("calls GenerateLiteralMembersElement")]
        private string GenerateMembersElement(XmlMembersMapping xmlMembersMapping)
        {
            return GenerateLiteralMembersElement(xmlMembersMapping);
        }

        private static string GetChoiceIdentifierSource(MemberMapping[] mappings, MemberMapping member)
        {
            string? choiceSource = null;
            if (member.ChoiceIdentifier != null)
            {
                for (int j = 0; j < mappings.Length; j++)
                {
                    if (mappings[j].Name == member.ChoiceIdentifier.MemberName)
                    {
                        choiceSource = $"p[{j}]";
                        break;
                    }
                }
#if DEBUG
                // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                if (choiceSource == null) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "Can not find " + member.ChoiceIdentifier.MemberName + " in the members mapping."));
#endif

            }
            return choiceSource!;
        }

        private static string GetChoiceIdentifierSource(MemberMapping mapping, string parent, TypeDesc parentTypeDesc)
        {
            if (mapping.ChoiceIdentifier == null) return "";
            CodeIdentifier.CheckValidIdentifier(mapping.ChoiceIdentifier.MemberName);
            return ReflectionAwareILGen.GetStringForMember(parent, mapping.ChoiceIdentifier.MemberName, parentTypeDesc);
        }

        [RequiresUnreferencedCode("calls InitializeValueTypes")]
        private string GenerateLiteralMembersElement(XmlMembersMapping xmlMembersMapping)
        {
            ElementAccessor element = xmlMembersMapping.Accessor;
            MemberMapping[] mappings = ((MembersMapping)element.Mapping!).Members!;
            bool hasWrapperElement = ((MembersMapping)element.Mapping).HasWrapperElement;
            string methodName = NextMethodName(element.Name);
            ilg = new CodeGenerator(this.typeBuilder);
            ilg.BeginMethod(
                typeof(object[]),
                methodName,
                Type.EmptyTypes,
                Array.Empty<string>(),
                CodeGenerator.PublicMethodAttributes
                );
            ilg.Load(null);
            ilg.Stloc(ilg.ReturnLocal);
            MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                "get_Reader",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            MethodInfo XmlReader_MoveToContent = typeof(XmlReader).GetMethod(
               "MoveToContent",
               CodeGenerator.InstanceBindingFlags,
               Type.EmptyTypes
               )!;
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_get_Reader);
            ilg.Call(XmlReader_MoveToContent);
            ilg.Pop();

            LocalBuilder localP = ilg.DeclareLocal(typeof(object[]), "p");
            ilg.NewArray(typeof(object), mappings.Length);
            ilg.Stloc(localP);
            InitializeValueTypes("p", mappings);

            if (hasWrapperElement)
            {
                WriteWhileNotLoopStart();
                WriteIsStartTag(element.Name, element.Form == XmlSchemaForm.Qualified ? element.Namespace : "");
            }

            Member? anyText = null;
            Member? anyElement = null;
            Member? anyAttribute = null;

            var membersList = new List<Member>();
            var textOrArrayMembersList = new List<Member>();
            var attributeMembersList = new List<Member>();

            for (int i = 0; i < mappings.Length; i++)
            {
                MemberMapping mapping = mappings[i];
                string source = $"p[{i}]";
                string arraySource = source;
                if (mapping.Xmlns != null)
                {
                    arraySource = $"(({mapping.TypeDesc!.CSharpName}){source})";
                }
                string choiceSource = GetChoiceIdentifierSource(mappings, mapping);
                Member member = new Member(this, source, arraySource, "a", i, mapping, choiceSource);
                Member anyMember = new Member(this, source, null, "a", i, mapping, choiceSource);
                if (!mapping.IsSequence)
                    member.ParamsReadSource = $"paramsRead[{i}]";
                if (mapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
                {
                    string nameSpecified = $"{mapping.Name}Specified";
                    for (int j = 0; j < mappings.Length; j++)
                    {
                        if (mappings[j].Name == nameSpecified)
                        {
                            member.CheckSpecifiedSource = $"p[{j}]";
                            break;
                        }
                    }
                }
                bool foundAnyElement = false;
                if (mapping.Text != null) anyText = anyMember;
                if (mapping.Attribute != null && mapping.Attribute.Any)
                    anyAttribute = anyMember;
                if (mapping.Attribute != null || mapping.Xmlns != null)
                    attributeMembersList.Add(member);
                else if (mapping.Text != null)
                    textOrArrayMembersList.Add(member);

                if (!mapping.IsSequence)
                {
                    for (int j = 0; j < mapping.Elements!.Length; j++)
                    {
                        if (mapping.Elements[j].Any && mapping.Elements[j].Name.Length == 0)
                        {
                            anyElement = anyMember;
                            if (mapping.Attribute == null && mapping.Text == null)
                                textOrArrayMembersList.Add(anyMember);
                            foundAnyElement = true;
                            break;
                        }
                    }
                }
                if (mapping.Attribute != null || mapping.Text != null || foundAnyElement)
                    membersList.Add(anyMember);
                else if (mapping.TypeDesc!.IsArrayLike && !(mapping.Elements!.Length == 1 && mapping.Elements[0].Mapping is ArrayMapping))
                {
                    membersList.Add(anyMember);
                    textOrArrayMembersList.Add(anyMember);
                }
                else
                {
                    if (mapping.TypeDesc.IsArrayLike && !mapping.TypeDesc.IsArray)
                        member.ParamsReadSource = null; // collection
                    membersList.Add(member);
                }
            }
            Member[] members = membersList.ToArray();
            Member[] textOrArrayMembers = textOrArrayMembersList.ToArray();

            if (members.Length > 0 && members[0].Mapping.IsReturnValue)
            {
                MethodInfo XmlSerializationReader_set_IsReturnValue = typeof(XmlSerializationReader).GetMethod(
                    "set_IsReturnValue",
                    CodeGenerator.InstanceBindingFlags,
                    new Type[] { typeof(bool) }
                    )!;
                ilg.Ldarg(0);
                ilg.Ldc(true);
                ilg.Call(XmlSerializationReader_set_IsReturnValue);
            }

            WriteParamsRead(mappings.Length);

            if (attributeMembersList.Count > 0)
            {
                Member[] attributeMembers = attributeMembersList.ToArray();
                WriteMemberBegin(attributeMembers);
                WriteAttributes(attributeMembers, anyAttribute, "UnknownNode", localP);
                WriteMemberEnd(attributeMembers);
                MethodInfo XmlReader_MoveToElement = typeof(XmlReader).GetMethod(
                    "MoveToElement",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_MoveToElement);
                ilg.Pop();
            }

            WriteMemberBegin(textOrArrayMembers);

            if (hasWrapperElement)
            {
                MethodInfo XmlReader_get_IsEmptyElement = typeof(XmlReader).GetMethod(
                    "get_IsEmptyElement",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_get_IsEmptyElement);
                ilg.If();
                {
                    MethodInfo XmlReader_Skip = typeof(XmlReader).GetMethod(
                        "Skip",
                        CodeGenerator.InstanceBindingFlags,
                        Type.EmptyTypes
                        )!;
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_Skip);
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_MoveToContent);
                    ilg.Pop();
                    ilg.WhileContinue();
                }
                ilg.EndIf();
                MethodInfo XmlReader_ReadStartElement = typeof(XmlReader).GetMethod(
                    "ReadStartElement",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_ReadStartElement);
            }
            if (IsSequence(members))
            {
                ilg.Ldc(0);
                ilg.Stloc(typeof(int), "state");
            }
            WriteWhileNotLoopStart();

            string unknownNode = $"UnknownNode((object)p, {ExpectedElements(members)});";
            WriteMemberElements(members, unknownNode, unknownNode, anyElement, anyText);

            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_get_Reader);
            ilg.Call(XmlReader_MoveToContent);
            ilg.Pop();
            WriteWhileLoopEnd();

            WriteMemberEnd(textOrArrayMembers);

            if (hasWrapperElement)
            {
                MethodInfo XmlSerializationReader_ReadEndElement = typeof(XmlSerializationReader).GetMethod(
                    "ReadEndElement",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_ReadEndElement);


                WriteUnknownNode("UnknownNode", "null", element, true);

                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_MoveToContent);
                ilg.Pop();
                WriteWhileLoopEnd();
            }

            ilg.Ldloc(ilg.GetLocal("p"));
            ilg.EndMethod();

            return methodName;
        }

        [RequiresUnreferencedCode("calls ILGenForCreateInstance")]
        private void InitializeValueTypes(string arrayName, MemberMapping[] mappings)
        {
            for (int i = 0; i < mappings.Length; i++)
            {
                if (!mappings[i].TypeDesc!.IsValueType)
                    continue;
                LocalBuilder arrayLoc = ilg.GetLocal(arrayName);
                ilg.Ldloc(arrayLoc);
                ilg.Ldc(i);
                ReflectionAwareILGen.ILGenForCreateInstance(ilg, mappings[i].TypeDesc!.Type!, false, false);
                ilg.ConvertValue(mappings[i].TypeDesc!.Type!, typeof(object));
                ilg.Stelem(arrayLoc.LocalType.GetElementType()!);
            }
        }

        [RequiresUnreferencedCode("calls WriteMemberElements")]
        private string GenerateTypeElement(XmlTypeMapping xmlTypeMapping)
        {
            ElementAccessor element = xmlTypeMapping.Accessor;
            TypeMapping mapping = element.Mapping!;
            string methodName = NextMethodName(element.Name);
            ilg = new CodeGenerator(this.typeBuilder);
            ilg.BeginMethod(
                typeof(object),
                methodName,
                Type.EmptyTypes,
                Array.Empty<string>(),
                CodeGenerator.PublicMethodAttributes
                );
            LocalBuilder oLoc = ilg.DeclareLocal(typeof(object), "o");
            ilg.Load(null);
            ilg.Stloc(oLoc);
            MemberMapping member = new MemberMapping();
            member.TypeDesc = mapping.TypeDesc;
            //member.ReadOnly = !mapping.TypeDesc.HasDefaultConstructor;
            member.Elements = new ElementAccessor[] { element };
            Member[] members = new Member[] { new Member(this, "o", "o", "a", 0, member) };
            MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                "get_Reader",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            MethodInfo XmlReader_MoveToContent = typeof(XmlReader).GetMethod(
               "MoveToContent",
               CodeGenerator.InstanceBindingFlags,
               Type.EmptyTypes
               )!;
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_get_Reader);
            ilg.Call(XmlReader_MoveToContent);
            ilg.Pop();
            string unknownNode = $"UnknownNode(null, {ExpectedElements(members)});";
            WriteMemberElements(members, "throw CreateUnknownNodeException();", unknownNode, element.Any ? members[0] : null, null);
            ilg.Ldloc(oLoc);
            // for code compat as compiler does
            ilg.Stloc(ilg.ReturnLocal);
            ilg.Ldloc(ilg.ReturnLocal);
            ilg.EndMethod();
            return methodName;
        }

        private string NextMethodName(string name) =>
            string.Create(CultureInfo.InvariantCulture, $"Read{++NextMethodNumber}_{CodeIdentifier.MakeValidInternal(name)}");

        private string NextIdName(string name) =>
            string.Create(CultureInfo.InvariantCulture, $"id{++_nextIdNumber}_{CodeIdentifier.MakeValidInternal(name)}");

        [RequiresUnreferencedCode("XmlSerializationReader methods have RequiresUnreferencedCode")]
        private void WritePrimitive(TypeMapping mapping, string source)
        {
            System.Diagnostics.Debug.Assert(source == "Reader.ReadElementString()" || source == "Reader.ReadString()"
                || source == "false" || source == "Reader.Value" || source == "vals[i]");
            if (mapping is EnumMapping)
            {
                string? enumMethodName = ReferenceMapping(mapping);
                if (enumMethodName == null) throw new InvalidOperationException(SR.Format(SR.XmlMissingMethodEnum, mapping.TypeDesc!.Name));
                // For enum, its read method (eg. Read1_Gender) could be called multiple times
                // prior to its declaration.
                MethodBuilder methodBuilder = EnsureMethodBuilder(typeBuilder,
                    enumMethodName,
                    CodeGenerator.PrivateMethodAttributes,
                    mapping.TypeDesc!.Type,
                    new Type[] { typeof(string) }
                    );
                ilg.Ldarg(0);
                if (source == "Reader.ReadElementString()" || source == "Reader.ReadString()")
                {
                    MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                         "get_Reader",
                         CodeGenerator.InstanceBindingFlags,
                         Type.EmptyTypes
                         )!;
                    MethodInfo XmlReader_ReadXXXString = typeof(XmlReader).GetMethod(
                        source == "Reader.ReadElementString()" ? "ReadElementContentAsString" : "ReadContentAsString",
                        CodeGenerator.InstanceBindingFlags,
                        Type.EmptyTypes
                        )!;
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_ReadXXXString);
                }
                else if (source == "Reader.Value")
                {
                    MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                         "get_Reader",
                         CodeGenerator.InstanceBindingFlags,
                         Type.EmptyTypes
                         )!;
                    MethodInfo XmlReader_get_Value = typeof(XmlReader).GetMethod(
                          "get_Value",
                          CodeGenerator.InstanceBindingFlags,
                          Type.EmptyTypes
                          )!;
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_get_Value);
                }
                else if (source == "vals[i]")
                {
                    LocalBuilder locVals = ilg.GetLocal("vals");
                    LocalBuilder locI = ilg.GetLocal("i");
                    ilg.LoadArrayElement(locVals, locI);
                }
                else if (source == "false")
                {
                    ilg.Ldc(false);
                }
                else
                {
                    throw Globals.NotSupported($"Unexpected: {source}");
                }
                ilg.Call(methodBuilder);
            }
            else if (mapping.TypeDesc == StringTypeDesc)
            {
                if (source == "Reader.ReadElementString()" || source == "Reader.ReadString()")
                {
                    MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                         "get_Reader",
                         CodeGenerator.InstanceBindingFlags,
                         Type.EmptyTypes
                         )!;
                    MethodInfo XmlReader_ReadXXXString = typeof(XmlReader).GetMethod(
                        source == "Reader.ReadElementString()" ? "ReadElementContentAsString" : "ReadContentAsString",
                        CodeGenerator.InstanceBindingFlags,
                        Type.EmptyTypes
                        )!;
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_ReadXXXString);
                }
                else if (source == "Reader.Value")
                {
                    MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                         "get_Reader",
                         CodeGenerator.InstanceBindingFlags,
                         Type.EmptyTypes
                         )!;
                    MethodInfo XmlReader_get_Value = typeof(XmlReader).GetMethod(
                          "get_Value",
                          CodeGenerator.InstanceBindingFlags,
                          Type.EmptyTypes
                          )!;
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_get_Value);
                }
                else if (source == "vals[i]")
                {
                    LocalBuilder locVals = ilg.GetLocal("vals");
                    LocalBuilder locI = ilg.GetLocal("i");
                    ilg.LoadArrayElement(locVals, locI);
                }
                else
                {
                    throw Globals.NotSupported($"Unexpected: {source}");
                }
            }
            else if (mapping.TypeDesc!.FormatterName == "String")
            {
                System.Diagnostics.Debug.Assert(source == "Reader.Value" || source == "Reader.ReadElementString()" || source == "vals[i]");
                if (source == "vals[i]")
                {
                    if (mapping.TypeDesc.CollapseWhitespace)
                        ilg.Ldarg(0);
                    LocalBuilder locVals = ilg.GetLocal("vals");
                    LocalBuilder locI = ilg.GetLocal("i");
                    ilg.LoadArrayElement(locVals, locI);
                    if (mapping.TypeDesc.CollapseWhitespace)
                    {
                        MethodInfo XmlSerializationReader_CollapseWhitespace = typeof(XmlSerializationReader).GetMethod(
                            "CollapseWhitespace",
                            CodeGenerator.InstanceBindingFlags,
                            null,
                            new Type[] { typeof(string) },
                            null
                            )!;
                        ilg.Call(XmlSerializationReader_CollapseWhitespace);
                    }
                }
                else
                {
                    MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                         "get_Reader",
                         CodeGenerator.InstanceBindingFlags,
                         Type.EmptyTypes
                         )!;
                    MethodInfo XmlReader_method = typeof(XmlReader).GetMethod(
                        source == "Reader.Value" ? "get_Value" : "ReadElementContentAsString",
                        CodeGenerator.InstanceBindingFlags,
                        Type.EmptyTypes
                        )!;
                    if (mapping.TypeDesc.CollapseWhitespace)
                        ilg.Ldarg(0);
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_method);
                    if (mapping.TypeDesc.CollapseWhitespace)
                    {
                        MethodInfo XmlSerializationReader_CollapseWhitespace = typeof(XmlSerializationReader).GetMethod(
                            "CollapseWhitespace",
                            CodeGenerator.InstanceBindingFlags,
                            new Type[] { typeof(string) }
                            )!;
                        ilg.Call(XmlSerializationReader_CollapseWhitespace);
                    }
                }
            }
            else
            {
                Type argType = source == "false" ? typeof(bool) : typeof(string);
                MethodInfo ToXXX;
                if (mapping.TypeDesc.HasCustomFormatter)
                {
                    // Only these methods below that is non Static and need to ldarg("this") for Call.
                    BindingFlags bindingFlags = CodeGenerator.StaticBindingFlags;
                    if ((mapping.TypeDesc.FormatterName == "ByteArrayBase64" && source == "false")
                        || (mapping.TypeDesc.FormatterName == "ByteArrayHex" && source == "false")
                        || (mapping.TypeDesc.FormatterName == "XmlQualifiedName"))
                    {
                        bindingFlags = CodeGenerator.InstanceBindingFlags;
                        ilg.Ldarg(0);
                    }

                    ToXXX = typeof(XmlSerializationReader).GetMethod(
                        $"To{mapping.TypeDesc.FormatterName}",
                        bindingFlags,
                        new Type[] { argType }
                        )!;
                }
                else
                {
                    ToXXX = typeof(XmlConvert).GetMethod(
                        $"To{mapping.TypeDesc.FormatterName}",
                        CodeGenerator.StaticBindingFlags,
                        new Type[] { argType }
                        )!;
                }
                if (source == "Reader.ReadElementString()" || source == "Reader.ReadString()")
                {
                    MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                         "get_Reader",
                         CodeGenerator.InstanceBindingFlags,
                         Type.EmptyTypes
                         )!;
                    MethodInfo XmlReader_ReadXXXString = typeof(XmlReader).GetMethod(
                        source == "Reader.ReadElementString()" ? "ReadElementContentAsString" : "ReadContentAsString",
                        CodeGenerator.InstanceBindingFlags,
                        Type.EmptyTypes
                        )!;
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_ReadXXXString);
                }
                else if (source == "Reader.Value")
                {
                    MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                         "get_Reader",
                         CodeGenerator.InstanceBindingFlags,
                         Type.EmptyTypes
                         )!;
                    MethodInfo XmlReader_get_Value = typeof(XmlReader).GetMethod(
                          "get_Value",
                          CodeGenerator.InstanceBindingFlags,
                          Type.EmptyTypes
                          )!;
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_get_Value);
                }
                else if (source == "vals[i]")
                {
                    LocalBuilder locVals = ilg.GetLocal("vals");
                    LocalBuilder locI = ilg.GetLocal("i");
                    ilg.LoadArrayElement(locVals, locI);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(source == "false");
                    ilg.Ldc(false);
                }
                ilg.Call(ToXXX);
            }
        }

        private string? MakeUnique(EnumMapping mapping, string name)
        {
            string uniqueName = name;
            EnumMapping? m;
            if (Enums.TryGetValue(uniqueName, out m))
            {
                if (m == mapping)
                {
                    // we already have created the hashtable
                    return null;
                }
                int i = 0;
                while (m != null)
                {
                    i++;
                    uniqueName = name + i.ToString(CultureInfo.InvariantCulture);
                    Enums.TryGetValue(uniqueName, out m);
                }
            }
            Enums.Add(uniqueName, mapping);
            return uniqueName;
        }

        [RequiresUnreferencedCode("calls LoadMember")]
        private string WriteHashtable(EnumMapping mapping, string typeName, out MethodBuilder? get_TableName)
        {
            get_TableName = null;

            CodeIdentifier.CheckValidIdentifier(typeName);
            string? propName = MakeUnique(mapping, $"{typeName}Values");
            if (propName == null) return CodeIdentifier.GetCSharpName(typeName);
            string memberName = MakeUnique(mapping, $"_{propName}")!;
            propName = CodeIdentifier.GetCSharpName(propName);

            FieldBuilder fieldBuilder = this.typeBuilder.DefineField(
                memberName,
                typeof(Hashtable),
                FieldAttributes.Private
                );

            PropertyBuilder propertyBuilder = this.typeBuilder.DefineProperty(
                propName,
                PropertyAttributes.None,
                CallingConventions.HasThis,
                typeof(Hashtable),
                null, null, null, null, null);

            ilg = new CodeGenerator(this.typeBuilder);
            ilg.BeginMethod(
                typeof(Hashtable),
                $"get_{propName}",
                Type.EmptyTypes,
                Array.Empty<string>(),
                MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.SpecialName);

            ilg.Ldarg(0);
            ilg.LoadMember(fieldBuilder);
            ilg.Load(null);
            ilg.If(Cmp.EqualTo);

            ConstructorInfo Hashtable_ctor = typeof(Hashtable).GetConstructor(
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            LocalBuilder hLoc = ilg.DeclareLocal(typeof(Hashtable), "h");
            ilg.New(Hashtable_ctor);
            ilg.Stloc(hLoc);

            ConstantMapping[] constants = mapping.Constants!;
            MethodInfo Hashtable_Add = typeof(Hashtable).GetMethod(
                "Add",
                CodeGenerator.InstanceBindingFlags,
                new Type[] { typeof(object), typeof(object) }
                )!;

            for (int i = 0; i < constants.Length; i++)
            {
                ilg.Ldloc(hLoc);
                ilg.Ldstr(GetCSharpString(constants[i].XmlName));
                ilg.Ldc(Enum.ToObject(mapping.TypeDesc!.Type!, constants[i].Value));
                ilg.ConvertValue(mapping.TypeDesc.Type!, typeof(long));
                ilg.ConvertValue(typeof(long), typeof(object));

                ilg.Call(Hashtable_Add);
            }

            ilg.Ldarg(0);
            ilg.Ldloc(hLoc);
            ilg.StoreMember(fieldBuilder);

            ilg.EndIf();

            ilg.Ldarg(0);
            ilg.LoadMember(fieldBuilder);

            get_TableName = ilg.EndMethod();
            propertyBuilder.SetGetMethod(get_TableName!);

            return propName;
        }

        [RequiresUnreferencedCode("calls WriteHashtable")]
        private void WriteEnumMethod(EnumMapping mapping)
        {
            MethodBuilder? get_TableName = null;
            if (mapping.IsFlags)
                WriteHashtable(mapping, mapping.TypeDesc!.Name, out get_TableName);

            string? methodName;
            MethodNames.TryGetValue(mapping, out methodName);
            string fullTypeName = mapping.TypeDesc!.CSharpName;
            List<Type> argTypes = new List<Type>();
            List<string> argNames = new List<string>();
            Type returnType;
            Type underlyingType;

            returnType = mapping.TypeDesc.Type!;
            underlyingType = Enum.GetUnderlyingType(returnType);
            argTypes.Add(typeof(string));
            argNames.Add("s");
            ilg = new CodeGenerator(this.typeBuilder);
            ilg.BeginMethod(
                returnType,
                GetMethodBuilder(methodName!),
                argTypes.ToArray(),
                argNames.ToArray(),
                CodeGenerator.PrivateMethodAttributes);

            ConstantMapping[] constants = mapping.Constants!;
            if (mapping.IsFlags)
            {
                {
                    MethodInfo XmlSerializationReader_ToEnum = typeof(XmlSerializationReader).GetMethod(
                        "ToEnum",
                        CodeGenerator.StaticBindingFlags,
                        new Type[] { typeof(string), typeof(Hashtable), typeof(string) }
                        )!;
                    ilg.Ldarg("s");
                    ilg.Ldarg(0);
                    Debug.Assert(get_TableName != null);
                    ilg.Call(get_TableName);
                    ilg.Ldstr(GetCSharpString(fullTypeName));
                    ilg.Call(XmlSerializationReader_ToEnum);
                    // XmlSerializationReader_ToEnum return long!
                    if (underlyingType != typeof(long))
                    {
                        ilg.ConvertValue(typeof(long), underlyingType);
                    }
                    ilg.Stloc(ilg.ReturnLocal);
                    ilg.Br(ilg.ReturnLabel);
                }
            }
            else
            {
                List<Label> caseLabels = new List<Label>();
                List<object> retValues = new List<object>();
                Label defaultLabel = ilg.DefineLabel();
                Label endSwitchLabel = ilg.DefineLabel();
                // This local is necessary; otherwise, it becomes if/else
                LocalBuilder localTmp = ilg.GetTempLocal(typeof(string));
                ilg.Ldarg("s");
                ilg.Stloc(localTmp);
                ilg.Ldloc(localTmp);
                ilg.Brfalse(defaultLabel);
                var cases = new HashSet<string>();
                for (int i = 0; i < constants.Length; i++)
                {
                    ConstantMapping c = constants[i];

                    CodeIdentifier.CheckValidIdentifier(c.Name);
                    if (cases.Add(c.XmlName))
                    {
                        Label caseLabel = ilg.DefineLabel();
                        ilg.Ldloc(localTmp);
                        ilg.Ldstr(GetCSharpString(c.XmlName));
                        MethodInfo String_op_Equality = typeof(string).GetMethod(
                            "op_Equality",
                            CodeGenerator.StaticBindingFlags,
                            new Type[] { typeof(string), typeof(string) }
                            )!;
                        ilg.Call(String_op_Equality);
                        ilg.Brtrue(caseLabel);
                        caseLabels.Add(caseLabel);
                        retValues.Add(Enum.ToObject(mapping.TypeDesc.Type!, c.Value));
                    }
                }

                ilg.Br(defaultLabel);
                // Case bodies
                for (int i = 0; i < caseLabels.Count; i++)
                {
                    ilg.MarkLabel(caseLabels[i]);
                    ilg.Ldc(retValues[i]);
                    ilg.Stloc(ilg.ReturnLocal);
                    ilg.Br(ilg.ReturnLabel);
                }
                MethodInfo XmlSerializationReader_CreateUnknownConstantException = typeof(XmlSerializationReader).GetMethod(
                    "CreateUnknownConstantException",
                    CodeGenerator.InstanceBindingFlags,
                    new Type[] { typeof(string), typeof(Type) }
                    )!;
                // Default body
                ilg.MarkLabel(defaultLabel);
                ilg.Ldarg(0);
                ilg.Ldarg("s");
                // typeof(..)
                ilg.Ldc(mapping.TypeDesc.Type!);
                ilg.Call(XmlSerializationReader_CreateUnknownConstantException);
                ilg.Throw();
                // End switch
                ilg.MarkLabel(endSwitchLabel);
            }

            ilg.MarkLabel(ilg.ReturnLabel);
            ilg.Ldloc(ilg.ReturnLocal);
            ilg.EndMethod();
        }

        [RequiresUnreferencedCode("calls WriteQNameEqual")]
        private void WriteDerivedTypes(StructMapping mapping, bool isTypedReturn, string returnTypeName)
        {
            for (StructMapping? derived = mapping.DerivedMappings; derived != null; derived = derived.NextDerivedMapping)
            {
                ilg.InitElseIf();
                WriteQNameEqual("xsiType", derived.TypeName, derived.Namespace);
                ilg.AndIf();

                string? methodName = ReferenceMapping(derived);
#if DEBUG
                // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                if (methodName == null) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorMethod, derived.TypeDesc!.Name));
#endif

                List<Type> argTypes = new List<Type>();
                ilg.Ldarg(0);
                if (derived.TypeDesc!.IsNullable)
                {
                    ilg.Ldarg("isNullable");
                    argTypes.Add(typeof(bool));
                }
                ilg.Ldc(false);
                argTypes.Add(typeof(bool));

                MethodBuilder methodBuilder = EnsureMethodBuilder(typeBuilder,
                    methodName!,
                    CodeGenerator.PrivateMethodAttributes,
                    derived.TypeDesc.Type,
                    argTypes.ToArray()
                    );
                ilg.Call(methodBuilder);
                ilg.ConvertValue(methodBuilder.ReturnType, ilg.ReturnLocal.LocalType);
                ilg.Stloc(ilg.ReturnLocal);
                ilg.Br(ilg.ReturnLabel);

                WriteDerivedTypes(derived, isTypedReturn, returnTypeName);
            }
        }

        [RequiresUnreferencedCode("calls ILGenForCreateInstance")]
        private void WriteEnumAndArrayTypes()
        {
            foreach (TypeScope scope in Scopes)
            {
                foreach (Mapping m in scope.TypeMappings)
                {
                    if (m is EnumMapping)
                    {
                        EnumMapping mapping = (EnumMapping)m;
                        ilg.InitElseIf();
                        WriteQNameEqual("xsiType", mapping.TypeName, mapping.Namespace);
                        ilg.AndIf();
                        MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                            "get_Reader",
                            CodeGenerator.InstanceBindingFlags,
                            Type.EmptyTypes
                            )!;
                        MethodInfo XmlReader_ReadStartElement = typeof(XmlReader).GetMethod(
                            "ReadStartElement",
                            CodeGenerator.InstanceBindingFlags,
                            Type.EmptyTypes
                            )!;
                        ilg.Ldarg(0);
                        ilg.Call(XmlSerializationReader_get_Reader);
                        ilg.Call(XmlReader_ReadStartElement);
                        string? methodName = ReferenceMapping(mapping);
#if DEBUG
                        // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                        if (methodName == null) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorMethod, mapping.TypeDesc!.Name));
#endif
                        LocalBuilder eLoc = ilg.DeclareOrGetLocal(typeof(object), "e");
                        MethodBuilder methodBuilder = EnsureMethodBuilder(typeBuilder,
                            methodName!,
                            CodeGenerator.PrivateMethodAttributes,
                            mapping.TypeDesc!.Type,
                            new Type[] { typeof(string) }
                            );
                        MethodInfo XmlSerializationReader_CollapseWhitespace = typeof(XmlSerializationReader).GetMethod(
                            "CollapseWhitespace",
                            CodeGenerator.InstanceBindingFlags,
                            new Type[] { typeof(string) }
                            )!;
                        MethodInfo XmlReader_ReadString = typeof(XmlReader).GetMethod(
                            "ReadContentAsString",
                            CodeGenerator.InstanceBindingFlags,
                            Type.EmptyTypes
                            )!;
                        ilg.Ldarg(0);
                        ilg.Ldarg(0);
                        ilg.Ldarg(0);
                        ilg.Call(XmlSerializationReader_get_Reader);
                        ilg.Call(XmlReader_ReadString);
                        ilg.Call(XmlSerializationReader_CollapseWhitespace);
                        ilg.Call(methodBuilder);
                        ilg.ConvertValue(methodBuilder.ReturnType, eLoc.LocalType);
                        ilg.Stloc(eLoc);
                        MethodInfo XmlSerializationReader_ReadEndElement = typeof(XmlSerializationReader).GetMethod(
                            "ReadEndElement",
                            CodeGenerator.InstanceBindingFlags,
                            Type.EmptyTypes
                            )!;
                        ilg.Ldarg(0);
                        ilg.Call(XmlSerializationReader_ReadEndElement);
                        ilg.Ldloc(eLoc);
                        ilg.Stloc(ilg.ReturnLocal);
                        ilg.Br(ilg.ReturnLabel);
                        // Caller own calling ilg.EndIf();
                    }
                    else if (m is ArrayMapping)
                    {
                        ArrayMapping mapping = (ArrayMapping)m;
                        if (mapping.TypeDesc!.HasDefaultConstructor)
                        {
                            ilg.InitElseIf();
                            WriteQNameEqual("xsiType", mapping.TypeName, mapping.Namespace);
                            ilg.AndIf();
                            ilg.EnterScope();
                            MemberMapping memberMapping = new MemberMapping();
                            memberMapping.TypeDesc = mapping.TypeDesc;
                            memberMapping.Elements = mapping.Elements;
                            string aVar = "a";
                            string zVar = "z";
                            Member member = new Member(this, aVar, zVar, 0, memberMapping);

                            TypeDesc td = mapping.TypeDesc;
                            LocalBuilder aLoc = ilg.DeclareLocal(mapping.TypeDesc.Type!, aVar);
                            if (mapping.TypeDesc.IsValueType)
                            {
                                ReflectionAwareILGen.ILGenForCreateInstance(ilg, td.Type!, false, false);
                            }
                            else
                                ilg.Load(null);
                            ilg.Stloc(aLoc);

                            WriteArray(member.Source, member.ArrayName, mapping, false, false, -1, 0);
                            ilg.Ldloc(aLoc);
                            ilg.Stloc(ilg.ReturnLocal);
                            ilg.Br(ilg.ReturnLabel);
                            ilg.ExitScope();
                            // Caller own calling ilg.EndIf();
                        }
                    }
                }
            }
        }

        [RequiresUnreferencedCode("calls WriteElement")]
        private void WriteNullableMethod(NullableMapping nullableMapping)
        {
            string? methodName;
            MethodNames.TryGetValue(nullableMapping, out methodName);
            ilg = new CodeGenerator(this.typeBuilder);
            ilg.BeginMethod(
                nullableMapping.TypeDesc!.Type!,
                GetMethodBuilder(methodName!),
                new Type[] { typeof(bool) },
                new string[] { "checkType" },
                CodeGenerator.PrivateMethodAttributes);

            LocalBuilder oLoc = ilg.DeclareLocal(nullableMapping.TypeDesc.Type!, "o");

            ilg.LoadAddress(oLoc);
            ilg.InitObj(nullableMapping.TypeDesc.Type!);
            MethodInfo XmlSerializationReader_ReadNull = typeof(XmlSerializationReader).GetMethod(
                "ReadNull",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes)!;
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_ReadNull);
            ilg.If();
            {
                ilg.Ldloc(oLoc);
                ilg.Stloc(ilg.ReturnLocal);
                ilg.Br(ilg.ReturnLabel);
            }
            ilg.EndIf();

            ElementAccessor element = new ElementAccessor();
            element.Mapping = nullableMapping.BaseMapping;
            element.Any = false;
            element.IsNullable = nullableMapping.BaseMapping!.TypeDesc!.IsNullable;

            WriteElement("o", null, null, element, null, null, false, false, -1, -1);
            ilg.Ldloc(oLoc);
            ilg.Stloc(ilg.ReturnLocal);
            ilg.Br(ilg.ReturnLabel);

            ilg.MarkLabel(ilg.ReturnLabel);
            ilg.Ldloc(ilg.ReturnLocal);
            ilg.EndMethod();
        }

        [RequiresUnreferencedCode("calls WriteLiteralStructMethod")]
        private void WriteStructMethod(StructMapping structMapping)
        {
            WriteLiteralStructMethod(structMapping);
        }

        [RequiresUnreferencedCode("calls WriteEnumAndArrayTypes")]
        private void WriteLiteralStructMethod(StructMapping structMapping)
        {
            string? methodName;
            MethodNames.TryGetValue(structMapping, out methodName);
            string typeName = structMapping.TypeDesc!.CSharpName;
            ilg = new CodeGenerator(this.typeBuilder);
            List<Type> argTypes = new List<Type>();
            List<string> argNames = new List<string>();
            if (structMapping.TypeDesc.IsNullable)
            {
                argTypes.Add(typeof(bool));
                argNames.Add("isNullable");
            }
            argTypes.Add(typeof(bool));
            argNames.Add("checkType");
            ilg.BeginMethod(
                structMapping.TypeDesc.Type!,
                GetMethodBuilder(methodName!),
                argTypes.ToArray(),
                argNames.ToArray(),
                CodeGenerator.PrivateMethodAttributes);

            LocalBuilder locXsiType = ilg.DeclareLocal(typeof(XmlQualifiedName), "xsiType");
            LocalBuilder locIsNull = ilg.DeclareLocal(typeof(bool), "isNull");
            MethodInfo XmlSerializationReader_GetXsiType = typeof(XmlSerializationReader).GetMethod(
                "GetXsiType",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            MethodInfo XmlSerializationReader_ReadNull = typeof(XmlSerializationReader).GetMethod(
                 "ReadNull",
                 CodeGenerator.InstanceBindingFlags,
                 Type.EmptyTypes
                 )!;
            Label labelTrue = ilg.DefineLabel();
            Label labelEnd = ilg.DefineLabel();
            ilg.Ldarg("checkType");
            ilg.Brtrue(labelTrue);
            ilg.Load(null);
            ilg.Br_S(labelEnd);
            ilg.MarkLabel(labelTrue);
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_GetXsiType);
            ilg.MarkLabel(labelEnd);
            ilg.Stloc(locXsiType);
            ilg.Ldc(false);
            ilg.Stloc(locIsNull);
            if (structMapping.TypeDesc.IsNullable)
            {
                ilg.Ldarg("isNullable");
                ilg.If();
                {
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_ReadNull);
                    ilg.Stloc(locIsNull);
                }
                ilg.EndIf();
            }

            ilg.Ldarg("checkType");
            ilg.If(); // if (checkType)
            if (structMapping.TypeDesc.IsRoot)
            {
                ilg.Ldloc(locIsNull);
                ilg.If();
                ilg.Ldloc(locXsiType);
                ilg.Load(null);
                ilg.If(Cmp.NotEqualTo);
                MethodInfo XmlSerializationReader_ReadTypedNull = typeof(XmlSerializationReader).GetMethod(
                       "ReadTypedNull",
                       CodeGenerator.InstanceBindingFlags,
                       new Type[] { locXsiType.LocalType }
                       )!;
                ilg.Ldarg(0);
                ilg.Ldloc(locXsiType);
                ilg.Call(XmlSerializationReader_ReadTypedNull);
                ilg.Stloc(ilg.ReturnLocal);
                ilg.Br(ilg.ReturnLabel);
                ilg.Else();
                if (structMapping.TypeDesc.IsValueType)
                {
                    throw Globals.NotSupported(SR.Arg_NeverValueType);
                }
                else
                {
                    ilg.Load(null);
                    ilg.Stloc(ilg.ReturnLocal);
                    ilg.Br(ilg.ReturnLabel);
                }
                ilg.EndIf(); // if (xsiType != null)

                ilg.EndIf(); // if (isNull)
            }
            ilg.Ldloc(typeof(XmlQualifiedName), "xsiType");
            ilg.Load(null);
            ilg.Ceq();
            if (!structMapping.TypeDesc.IsRoot)
            {
                labelTrue = ilg.DefineLabel();
                labelEnd = ilg.DefineLabel();
                // xsiType == null
                ilg.Brtrue(labelTrue);
                WriteQNameEqual("xsiType", structMapping.TypeName, structMapping.Namespace);
                // Bool result for WriteQNameEqual is on the stack
                ilg.Br_S(labelEnd);
                ilg.MarkLabel(labelTrue);
                ilg.Ldc(true);
                ilg.MarkLabel(labelEnd);
            }
            ilg.If(); // if (xsiType == null
            if (structMapping.TypeDesc.IsRoot)
            {
                ConstructorInfo XmlQualifiedName_ctor = typeof(XmlQualifiedName).GetConstructor(
                    CodeGenerator.InstanceBindingFlags,
                    new Type[] { typeof(string), typeof(string) }
                    )!;
                MethodInfo XmlSerializationReader_ReadTypedPrimitive = typeof(XmlSerializationReader).GetMethod(
                    "ReadTypedPrimitive",
                    CodeGenerator.InstanceBindingFlags,
                    new Type[] { typeof(XmlQualifiedName) }
                    )!;
                ilg.Ldarg(0);
                ilg.Ldstr(Soap.UrType);
                ilg.Ldstr(XmlSchema.Namespace);
                ilg.New(XmlQualifiedName_ctor);
                ilg.Call(XmlSerializationReader_ReadTypedPrimitive);
                ilg.Stloc(ilg.ReturnLocal);
                ilg.Br(ilg.ReturnLabel);
            }
            WriteDerivedTypes(structMapping, !structMapping.TypeDesc.IsRoot, typeName);
            if (structMapping.TypeDesc.IsRoot) WriteEnumAndArrayTypes();
            ilg.Else(); // if (xsiType == null
            if (structMapping.TypeDesc.IsRoot)
            {
                MethodInfo XmlSerializationReader_ReadTypedPrimitive = typeof(XmlSerializationReader).GetMethod(
                    "ReadTypedPrimitive",
                    CodeGenerator.InstanceBindingFlags,
                    new Type[] { locXsiType.LocalType }
                    )!;
                ilg.Ldarg(0);
                ilg.Ldloc(locXsiType);
                ilg.Call(XmlSerializationReader_ReadTypedPrimitive);
                ilg.Stloc(ilg.ReturnLocal);
                ilg.Br(ilg.ReturnLabel);
            }
            else
            {
                MethodInfo XmlSerializationReader_CreateUnknownTypeException = typeof(XmlSerializationReader).GetMethod(
                     "CreateUnknownTypeException",
                     CodeGenerator.InstanceBindingFlags,
                     new Type[] { typeof(XmlQualifiedName) }
                     )!;
                ilg.Ldarg(0);
                ilg.Ldloc(locXsiType);
                ilg.Call(XmlSerializationReader_CreateUnknownTypeException);
                ilg.Throw();
            }
            ilg.EndIf(); // if (xsiType == null
            ilg.EndIf();  // checkType

            if (structMapping.TypeDesc.IsNullable)
            {
                ilg.Ldloc(typeof(bool), "isNull");
                ilg.If();
                {
                    ilg.Load(null);
                    ilg.Stloc(ilg.ReturnLocal);
                    ilg.Br(ilg.ReturnLabel);
                }
                ilg.EndIf();
            }

            if (structMapping.TypeDesc.IsAbstract)
            {
                MethodInfo XmlSerializationReader_CreateAbstractTypeException = typeof(XmlSerializationReader).GetMethod(
                    "CreateAbstractTypeException",
                    CodeGenerator.InstanceBindingFlags,
                    new Type[] { typeof(string), typeof(string) }
                    )!;
                ilg.Ldarg(0);
                ilg.Ldstr(GetCSharpString(structMapping.TypeName));
                ilg.Ldstr(GetCSharpString(structMapping.Namespace));
                ilg.Call(XmlSerializationReader_CreateAbstractTypeException);
                ilg.Throw();
            }
            else
            {
                if (structMapping.TypeDesc.Type != null && typeof(XmlSchemaObject).IsAssignableFrom(structMapping.TypeDesc.Type))
                {
                    MethodInfo XmlSerializationReader_set_DecodeName = typeof(XmlSerializationReader).GetMethod(
                         "set_DecodeName",
                         CodeGenerator.InstanceBindingFlags,
                         new Type[] { typeof(bool) }
                         )!;
                    ilg.Ldarg(0);
                    ilg.Ldc(false);
                    ilg.Call(XmlSerializationReader_set_DecodeName);
                }
                WriteCreateMapping(structMapping, "o");
                LocalBuilder oLoc = ilg.GetLocal("o");

                // this method populates the memberInfos dictionary based on the structMapping
                MemberMapping[] mappings = TypeScope.GetSettableMembers(structMapping, memberInfos);

                Member? anyText = null;
                Member? anyElement = null;
                Member? anyAttribute = null;
                bool isSequence = structMapping.HasExplicitSequence();

                var arraysToDeclareList = new List<Member>(mappings.Length);
                var arraysToSetList = new List<Member>(mappings.Length);
                var allMembersList = new List<Member>(mappings.Length);

                for (int i = 0; i < mappings.Length; i++)
                {
                    MemberMapping mapping = mappings[i];
                    CodeIdentifier.CheckValidIdentifier(mapping.Name);
                    string source = ReflectionAwareILGen.GetStringForMember("o", mapping.Name, structMapping.TypeDesc);
                    Member member = new Member(this, source, "a", i, mapping, GetChoiceIdentifierSource(mapping, "o", structMapping.TypeDesc));
                    if (!mapping.IsSequence)
                        member.ParamsReadSource = $"paramsRead[{i}]";
                    member.IsNullable = mapping.TypeDesc!.IsNullable;
                    if (mapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
                        member.CheckSpecifiedSource = ReflectionAwareILGen.GetStringForMember("o", $"{mapping.Name}Specified", structMapping.TypeDesc);
                    if (mapping.Text != null)
                        anyText = member;
                    if (mapping.Attribute != null && mapping.Attribute.Any)
                        anyAttribute = member;
                    if (!isSequence)
                    {
                        // find anyElement if present.
                        for (int j = 0; j < mapping.Elements!.Length; j++)
                        {
                            if (mapping.Elements[j].Any && (mapping.Elements[j].Name == null || mapping.Elements[j].Name.Length == 0))
                            {
                                anyElement = member;
                                break;
                            }
                        }
                    }
                    else if (mapping.IsParticle && !mapping.IsSequence)
                    {
                        StructMapping? declaringMapping;
                        structMapping.FindDeclaringMapping(mapping, out declaringMapping, structMapping.TypeName);
                        throw new InvalidOperationException(SR.Format(SR.XmlSequenceHierarchy, structMapping.TypeDesc.FullName, mapping.Name, declaringMapping!.TypeDesc!.FullName, "Order"));
                    }
                    if (mapping.Attribute == null && mapping.Elements!.Length == 1 && mapping.Elements[0].Mapping is ArrayMapping)
                    {
                        Member arrayMember = new Member(this, source, source, "a", i, mapping, GetChoiceIdentifierSource(mapping, "o", structMapping.TypeDesc));
                        arrayMember.CheckSpecifiedSource = member.CheckSpecifiedSource;
                        allMembersList.Add(arrayMember);
                    }
                    else
                    {
                        allMembersList.Add(member);
                    }

                    if (mapping.TypeDesc.IsArrayLike)
                    {
                        arraysToDeclareList.Add(member);
                        if (mapping.TypeDesc.IsArrayLike && !(mapping.Elements!.Length == 1 && mapping.Elements[0].Mapping is ArrayMapping))
                        {
                            member.ParamsReadSource = null; // flat arrays -- don't want to count params read.
                            if (member != anyText && member != anyElement)
                            {
                                arraysToSetList.Add(member);
                            }
                        }
                        else if (!mapping.TypeDesc.IsArray)
                        {
                            member.ParamsReadSource = null; // collection
                        }
                    }
                }
                if (anyElement != null) arraysToSetList.Add(anyElement);
                if (anyText != null && anyText != anyElement) arraysToSetList.Add(anyText);

                Member[] arraysToDeclare = arraysToDeclareList.ToArray();
                Member[] arraysToSet = arraysToSetList.ToArray();
                Member[] allMembers = allMembersList.ToArray();

                WriteMemberBegin(arraysToDeclare);
                WriteParamsRead(mappings.Length);

                WriteAttributes(allMembers, anyAttribute, "UnknownNode", oLoc);
                if (anyAttribute != null)
                    WriteMemberEnd(arraysToDeclare);

                MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                    "get_Reader",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                MethodInfo XmlReader_MoveToElement = typeof(XmlReader).GetMethod(
                    "MoveToElement",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_MoveToElement);
                ilg.Pop();

                MethodInfo XmlReader_get_IsEmptyElement = typeof(XmlReader).GetMethod(
                    "get_IsEmptyElement",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_get_IsEmptyElement);
                ilg.If();
                MethodInfo XmlReader_Skip = typeof(XmlReader).GetMethod(
                    "Skip",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_Skip);
                WriteMemberEnd(arraysToSet);
                ilg.Ldloc(oLoc);
                ilg.Stloc(ilg.ReturnLocal);
                ilg.Br(ilg.ReturnLabel);
                ilg.EndIf();

                MethodInfo XmlReader_ReadStartElement = typeof(XmlReader).GetMethod(
                    "ReadStartElement",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_ReadStartElement);
                if (IsSequence(allMembers))
                {
                    ilg.Ldc(0);
                    ilg.Stloc(typeof(int), "state");
                }
                WriteWhileNotLoopStart();
                string unknownNode = $"UnknownNode((object)o, {ExpectedElements(allMembers)});";
                WriteMemberElements(allMembers, unknownNode, unknownNode, anyElement, anyText);
                MethodInfo XmlReader_MoveToContent = typeof(XmlReader).GetMethod(
                    "MoveToContent",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_MoveToContent);
                ilg.Pop();

                WriteWhileLoopEnd();
                WriteMemberEnd(arraysToSet);

                MethodInfo XmlSerializationReader_ReadEndElement = typeof(XmlSerializationReader).GetMethod(
                    "ReadEndElement",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_ReadEndElement);
                ilg.Ldloc(structMapping.TypeDesc.Type!, "o");
                ilg.Stloc(ilg.ReturnLocal);
            }
            ilg.MarkLabel(ilg.ReturnLabel);
            ilg.Ldloc(ilg.ReturnLocal);
            ilg.EndMethod();
        }

        [RequiresUnreferencedCode("calls LoadMember")]
        private void WriteQNameEqual(string source, string? name, string? ns)
        {
            WriteID(name);
            WriteID(ns);
            // This api assume the source is local member of XmlQualifiedName type
            // It leaves bool result on the stack
            MethodInfo XmlQualifiedName_get_Name = typeof(XmlQualifiedName).GetMethod(
                "get_Name",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            MethodInfo XmlQualifiedName_get_Namespace = typeof(XmlQualifiedName).GetMethod(
                "get_Namespace",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            Label labelEnd = ilg.DefineLabel();
            Label labelFalse = ilg.DefineLabel();
            LocalBuilder sLoc = ilg.GetLocal(source);
            ilg.Ldloc(sLoc);
            ilg.Call(XmlQualifiedName_get_Name);
            ilg.Ldarg(0);
            ilg.LoadMember(_idNameFields[name ?? string.Empty]);
            ilg.Bne(labelFalse);
            ilg.Ldloc(sLoc);
            ilg.Call(XmlQualifiedName_get_Namespace);
            ilg.Ldarg(0);
            ilg.LoadMember(_idNameFields[ns ?? string.Empty]);
            ilg.Ceq();
            ilg.Br_S(labelEnd);
            ilg.MarkLabel(labelFalse);
            ilg.Ldc(false);
            ilg.MarkLabel(labelEnd);
        }

        [RequiresUnreferencedCode("XmlSerializationReader methods have RequiresUnreferencedCode")]
        private void WriteXmlNodeEqual(string source, string name, string? ns)
        {
            WriteXmlNodeEqual(source, name, ns, true);
        }
        [RequiresUnreferencedCode("XmlSerializationReader methods have RequiresUnreferencedCode")]
        private void WriteXmlNodeEqual(string source, string name, string? ns, bool doAndIf)
        {
            bool isNameNullOrEmpty = string.IsNullOrEmpty(name);
            if (!isNameNullOrEmpty)
            {
                WriteID(name);
            }
            WriteID(ns);
            // Only support Reader and XmlSerializationReaderReader only
            System.Diagnostics.Debug.Assert(source == "Reader");
            MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                $"get_{source}",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            MethodInfo XmlReader_get_LocalName = typeof(XmlReader).GetMethod(
                "get_LocalName",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            MethodInfo XmlReader_get_NamespaceURI = typeof(XmlReader).GetMethod(
                "get_NamespaceURI",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;

            Label labelFalse = ilg.DefineLabel();
            Label labelEnd = ilg.DefineLabel();

            if (!isNameNullOrEmpty)
            {
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_get_LocalName);
                ilg.Ldarg(0);
                ilg.LoadMember(_idNameFields[name ?? string.Empty]);
                ilg.Bne(labelFalse);
            }

            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_get_Reader);
            ilg.Call(XmlReader_get_NamespaceURI);
            ilg.Ldarg(0);
            ilg.LoadMember(_idNameFields[ns ?? string.Empty]);
            ilg.Ceq();

            if (!isNameNullOrEmpty)
            {
                ilg.Br_S(labelEnd);
                ilg.MarkLabel(labelFalse);
                ilg.Ldc(false);
                ilg.MarkLabel(labelEnd);
            }
            if (doAndIf)
                ilg.AndIf();
        }

        private void WriteID(string? name)
        {
            if (name == null)
            {
                //Writer.Write("null");
                //return;
                name = "";
            }
            if (!_idNames.ContainsKey(name))
            {
                string? idName = NextIdName(name);
                _idNames.Add(name, idName);
                _idNameFields.Add(name, this.typeBuilder.DefineField(idName, typeof(string), FieldAttributes.Private));
            }
        }

        [RequiresUnreferencedCode("calls WriteSourceEnd")]
        private void WriteAttributes(Member[] members, Member? anyAttribute, string elseCall, LocalBuilder firstParam)
        {
            int count = 0;
            Member? xmlnsMember = null;
            var attributes = new List<AttributeAccessor>();

            // Condition do at the end, so C# looks the same
            MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                "get_Reader",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            MethodInfo XmlReader_MoveToNextAttribute = typeof(XmlReader).GetMethod(
                "MoveToNextAttribute",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            ilg.WhileBegin();

            for (int i = 0; i < members.Length; i++)
            {
                Member member = (Member)members[i];
                if (member.Mapping.Xmlns != null)
                {
                    xmlnsMember = member;
                    continue;
                }
                if (member.Mapping.Ignore)
                    continue;

                AttributeAccessor? attribute = member.Mapping.Attribute;

                if (attribute == null) continue;
                if (attribute.Any) continue;

                attributes.Add(attribute);

                if (count++ > 0)
                    ilg.InitElseIf();
                else
                    ilg.InitIf();

                if (member.ParamsReadSource != null)
                {
                    ILGenParamsReadSource(member.ParamsReadSource);
                    ilg.Ldc(false);
                    ilg.AndIf(Cmp.EqualTo);
                }

                if (attribute.IsSpecialXmlNamespace)
                {
                    WriteXmlNodeEqual("Reader", attribute.Name, XmlReservedNs.NsXml);
                }
                else
                    WriteXmlNodeEqual("Reader", attribute.Name, attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : "");

                WriteAttribute(member);
            }

            if (count > 0)
                ilg.InitElseIf();
            else
                ilg.InitIf();

            if (xmlnsMember != null)
            {
                MethodInfo XmlSerializationReader_IsXmlnsAttribute = typeof(XmlSerializationReader).GetMethod(
                    "IsXmlnsAttribute",
                    CodeGenerator.InstanceBindingFlags,
                    new Type[] { typeof(string) }
                    )!;
                MethodInfo XmlReader_get_Name = typeof(XmlReader).GetMethod(
                    "get_Name",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                MethodInfo XmlReader_get_LocalName = typeof(XmlReader).GetMethod(
                    "get_LocalName",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                MethodInfo XmlReader_get_Value = typeof(XmlReader).GetMethod(
                    "get_Value",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ilg.Ldarg(0);
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_get_Name);
                ilg.Call(XmlSerializationReader_IsXmlnsAttribute);
                ilg.Ldc(true);
                ilg.AndIf(Cmp.EqualTo);

                ILGenLoad(xmlnsMember.Source);
                ilg.Load(null);
                ilg.If(Cmp.EqualTo);
                WriteSourceBegin(xmlnsMember.Source);
                ConstructorInfo ctor = xmlnsMember.Mapping.TypeDesc!.Type!.GetConstructor(
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ilg.New(ctor);
                WriteSourceEnd(xmlnsMember.Source, xmlnsMember.Mapping.TypeDesc.Type!);
                ilg.EndIf(); // if (xmlnsMember.Source == null

                Label labelEqual5 = ilg.DefineLabel();
                Label labelEndLength = ilg.DefineLabel();
                MethodInfo Add = xmlnsMember.Mapping.TypeDesc.Type!.GetMethod(
                    "Add",
                    CodeGenerator.InstanceBindingFlags,
                    new Type[] { typeof(string), typeof(string) }
                    )!;
                MethodInfo String_get_Length = typeof(string).GetMethod(
                    "get_Length",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ILGenLoad(xmlnsMember.ArraySource, xmlnsMember.Mapping.TypeDesc.Type);
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_get_Name);
                ilg.Call(String_get_Length);
                ilg.Ldc(5);
                ilg.Beq(labelEqual5);
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_get_LocalName);
                ilg.Br(labelEndLength);
                ilg.MarkLabel(labelEqual5);
                ilg.Ldstr(string.Empty);
                ilg.MarkLabel(labelEndLength);
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_get_Value);
                ilg.Call(Add);

                ilg.Else();
            }
            else
            {
                MethodInfo XmlSerializationReader_IsXmlnsAttribute = typeof(XmlSerializationReader).GetMethod(
                    "IsXmlnsAttribute",
                    CodeGenerator.InstanceBindingFlags,
                    new Type[] { typeof(string) }
                    )!;
                MethodInfo XmlReader_get_Name = typeof(XmlReader).GetMethod(
                    "get_Name",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                ilg.Ldarg(0);
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_get_Name);
                ilg.Call(XmlSerializationReader_IsXmlnsAttribute);
                ilg.Ldc(false);
                ilg.AndIf(Cmp.EqualTo);
            }
            if (anyAttribute != null)
            {
                LocalBuilder localAttr = ilg.DeclareOrGetLocal(typeof(XmlAttribute), "attr");
                MethodInfo XmlSerializationReader_get_Document = typeof(XmlSerializationReader).GetMethod(
                    "get_Document",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                MethodInfo XmlDocument_ReadNode = typeof(XmlDocument).GetMethod(
                    "ReadNode",
                    CodeGenerator.InstanceBindingFlags,
                    new Type[] { typeof(XmlReader) }
                    )!;
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Document);
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlDocument_ReadNode);
                ilg.ConvertValue(XmlDocument_ReadNode.ReturnType, localAttr.LocalType);
                ilg.Stloc(localAttr);
                MethodInfo XmlSerializationReader_ParseWsdlArrayType = typeof(XmlSerializationReader).GetMethod(
                    "ParseWsdlArrayType",
                    CodeGenerator.InstanceBindingFlags,
                    new Type[] { localAttr.LocalType }
                    )!;
                ilg.Ldarg(0);
                ilg.Ldloc(localAttr);
                ilg.Call(XmlSerializationReader_ParseWsdlArrayType);
                WriteAttribute(anyAttribute);
            }
            else
            {
                List<Type> argTypes = new List<Type>();
                ilg.Ldarg(0);
                argTypes.Add(typeof(object));
                ilg.Ldloc(firstParam);
                ilg.ConvertValue(firstParam.LocalType, typeof(object));
                if (attributes.Count > 0)
                {
                    string qnames = "";

                    for (int i = 0; i < attributes.Count; i++)
                    {
                        AttributeAccessor attribute = attributes[i];
                        if (i > 0)
                            qnames += ", ";
                        qnames += attribute.IsSpecialXmlNamespace ? XmlReservedNs.NsXml : $"{(attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : "")}:{attribute.Name}";
                    }
                    argTypes.Add(typeof(string));
                    ilg.Ldstr(qnames);
                }
                System.Diagnostics.Debug.Assert(elseCall == "UnknownNode");
                MethodInfo elseCallMethod = typeof(XmlSerializationReader).GetMethod(
                    elseCall,
                    CodeGenerator.InstanceBindingFlags,
                    argTypes.ToArray()
                    )!;
                ilg.Call(elseCallMethod);
            }
            ilg.EndIf();

            ilg.WhileBeginCondition();
            {
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_MoveToNextAttribute);
            }
            ilg.WhileEndCondition();
            ilg.WhileEnd();
        }

        [RequiresUnreferencedCode("calls WriteSourceEnd")]
        private void WriteAttribute(Member member)
        {
            AttributeAccessor attribute = member.Mapping.Attribute!;

            if (attribute.Mapping is SpecialMapping)
            {
                SpecialMapping special = (SpecialMapping)attribute.Mapping;

                if (special.TypeDesc!.Kind == TypeKind.Attribute)
                {
                    WriteSourceBegin(member.ArraySource);
                    ilg.Ldloc("attr");
                    WriteSourceEnd(member.ArraySource, member.Mapping.TypeDesc!.IsArrayLike ? member.Mapping.TypeDesc.ArrayElementTypeDesc!.Type! : member.Mapping.TypeDesc.Type!);
                }
                else if (special.TypeDesc.CanBeAttributeValue)
                {
                    LocalBuilder attrLoc = ilg.GetLocal("attr");
                    ilg.Ldloc(attrLoc);
                    // to get code compat
                    if (attrLoc.LocalType == typeof(XmlAttribute))
                    {
                        ilg.Load(null);
                        ilg.Cne();
                    }
                    else
                        ilg.IsInst(typeof(XmlAttribute));
                    ilg.If();
                    WriteSourceBegin(member.ArraySource);
                    ilg.Ldloc(attrLoc);
                    ilg.ConvertValue(attrLoc.LocalType, typeof(XmlAttribute));
                    WriteSourceEnd(member.ArraySource, member.Mapping.TypeDesc!.IsArrayLike ? member.Mapping.TypeDesc.ArrayElementTypeDesc!.Type! : member.Mapping.TypeDesc.Type!);
                    ilg.EndIf();
                }
                else
                    throw new InvalidOperationException(SR.XmlInternalError);
            }
            else
            {
                if (attribute.IsList)
                {
                    LocalBuilder locListValues = ilg.DeclareOrGetLocal(typeof(string), "listValues");
                    LocalBuilder locVals = ilg.DeclareOrGetLocal(typeof(string[]), "vals");
                    MethodInfo String_Split = typeof(string).GetMethod(
                        "Split",
                        CodeGenerator.InstanceBindingFlags,
                        new Type[] { typeof(char[]) }
                        )!;
                    MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                        "get_Reader",
                        CodeGenerator.InstanceBindingFlags,
                        Type.EmptyTypes
                        )!;
                    MethodInfo XmlReader_get_Value = typeof(XmlReader).GetMethod(
                        "get_Value",
                        CodeGenerator.InstanceBindingFlags,
                        Type.EmptyTypes
                        )!;
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_get_Value);
                    ilg.Stloc(locListValues);
                    ilg.Ldloc(locListValues);
                    ilg.Load(null);
                    ilg.Call(String_Split);
                    ilg.Stloc(locVals);
                    LocalBuilder localI = ilg.DeclareOrGetLocal(typeof(int), "i");
                    ilg.For(localI, 0, locVals);

                    string attributeSource = GetArraySource(member.Mapping.TypeDesc!, member.ArrayName);

                    WriteSourceBegin(attributeSource);
                    WritePrimitive(attribute.Mapping!, "vals[i]");
                    WriteSourceEnd(attributeSource, member.Mapping.TypeDesc!.ArrayElementTypeDesc!.Type!);
                    ilg.EndFor();
                }
                else
                {
                    WriteSourceBegin(member.ArraySource);
                    WritePrimitive(attribute.Mapping!, attribute.IsList ? "vals[i]" : "Reader.Value");
                    WriteSourceEnd(member.ArraySource, member.Mapping.TypeDesc!.IsArrayLike ? member.Mapping.TypeDesc.ArrayElementTypeDesc!.Type! : member.Mapping.TypeDesc.Type!);
                }
            }
            if (member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite && member.CheckSpecifiedSource != null && member.CheckSpecifiedSource.Length > 0)
            {
                ILGenSet(member.CheckSpecifiedSource, true);
            }
            if (member.ParamsReadSource != null)
            {
                ILGenParamsReadSource(member.ParamsReadSource, true);
            }
        }

        [RequiresUnreferencedCode("calls ILGenForCreateInstance")]
        private void WriteMemberBegin(Member[] members)
        {
            for (int i = 0; i < members.Length; i++)
            {
                Member member = (Member)members[i];

                if (member.IsArrayLike)
                {
                    string a = member.ArrayName;
                    string c = $"c{a}";

                    TypeDesc typeDesc = member.Mapping.TypeDesc!;

                    if (member.Mapping.TypeDesc!.IsArray)
                    {
                        WriteArrayLocalDecl(typeDesc.CSharpName,
                                            a, "null", typeDesc);
                        ilg.Ldc(0);
                        ilg.Stloc(typeof(int), c);

                        if (member.Mapping.ChoiceIdentifier != null)
                        {
                            WriteArrayLocalDecl($"{member.Mapping.ChoiceIdentifier.Mapping!.TypeDesc!.CSharpName}[]",
                                                member.ChoiceArrayName, "null",
                                                member.Mapping.ChoiceIdentifier.Mapping.TypeDesc);
                            ilg.Ldc(0);
                            ilg.Stloc(typeof(int), $"c{member.ChoiceArrayName}");
                        }
                    }
                    else
                    {
                        if (member.Source.EndsWith('(') || member.Source.EndsWith('{'))
                        {
                            WriteCreateInstance(a, typeDesc.CannotNew, typeDesc.Type!);
                            WriteSourceBegin(member.Source);
                            ilg.Ldloc(ilg.GetLocal(a));
                            WriteSourceEnd(member.Source, typeDesc.Type!);
                        }
                        else
                        {
                            if (member.IsList && !member.Mapping.ReadOnly && member.Mapping.TypeDesc.IsNullable)
                            {
                                // we need to new the Collections and ArrayLists
                                ILGenLoad(member.Source, typeof(object));
                                ilg.Load(null);
                                ilg.If(Cmp.EqualTo);
                                if (!member.Mapping.TypeDesc.HasDefaultConstructor)
                                {
                                    MethodInfo XmlSerializationReader_CreateReadOnlyCollectionException = typeof(XmlSerializationReader).GetMethod(
                                         "CreateReadOnlyCollectionException",
                                         CodeGenerator.InstanceBindingFlags,
                                         new Type[] { typeof(string) }
                                         )!;
                                    ilg.Ldarg(0);
                                    ilg.Ldstr(GetCSharpString(member.Mapping.TypeDesc.CSharpName));
                                    ilg.Call(XmlSerializationReader_CreateReadOnlyCollectionException);
                                    ilg.Throw();
                                }
                                else
                                {
                                    WriteSourceBegin(member.Source);
                                    ReflectionAwareILGen.ILGenForCreateInstance(ilg, member.Mapping.TypeDesc.Type!, typeDesc.CannotNew, true);
                                    WriteSourceEnd(member.Source, member.Mapping.TypeDesc.Type!);
                                }
                                ilg.EndIf(); // if ((object)(member.Source) == null
                            }
                            WriteLocalDecl(a, new SourceInfo(member.Source, member.Source, member.Mapping.MemberInfo, member.Mapping.TypeDesc.Type, ilg));
                        }
                    }
                }
            }
        }

        private static string ExpectedElements(Member[] members)
        {
            if (IsSequence(members))
                return "null";
            string qnames = string.Empty;
            bool firstElement = true;
            for (int i = 0; i < members.Length; i++)
            {
                Member member = (Member)members[i];
                if (member.Mapping.Xmlns != null)
                    continue;
                if (member.Mapping.Ignore)
                    continue;
                if (member.Mapping.IsText || member.Mapping.IsAttribute)
                    continue;

                ElementAccessor[] elements = member.Mapping.Elements!;

                for (int j = 0; j < elements.Length; j++)
                {
                    ElementAccessor e = elements[j];
                    string? ns = e.Form == XmlSchemaForm.Qualified ? e.Namespace : "";
                    if (e.Any && (e.Name == null || e.Name.Length == 0)) continue;

                    if (!firstElement)
                        qnames += ", ";
                    qnames += $"{ns}:{e.Name}";
                    firstElement = false;
                }
            }
            return ReflectionAwareILGen.GetQuotedCSharpString(qnames);
        }

        [RequiresUnreferencedCode("calls WriteMemberElementsIf")]
        private void WriteMemberElements(Member[] members, string elementElseString, string elseString, Member? anyElement, Member? anyText)
        {
            if (anyText != null)
            {
                ilg.Load(null);
                ilg.Stloc(typeof(string), "tmp");
            }

            MethodInfo XmlReader_get_NodeType = typeof(XmlReader).GetMethod(
                 "get_NodeType",
                 CodeGenerator.InstanceBindingFlags,
                 Type.EmptyTypes
                 )!;
            MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                "get_Reader",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            int XmlNodeType_Element = 1;
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_get_Reader);
            ilg.Call(XmlReader_get_NodeType);
            ilg.Ldc(XmlNodeType_Element);
            ilg.If(Cmp.EqualTo);

            WriteMemberElementsIf(members, anyElement, elementElseString);

            if (anyText != null)
                WriteMemberText(anyText, elseString);

            ilg.Else();
            ILGenElseString(elseString);
            ilg.EndIf();
        }

        [RequiresUnreferencedCode("calls WriteText")]
        private void WriteMemberText(Member anyText, string elseString)
        {
            ilg.InitElseIf();
            Label labelTrue = ilg.DefineLabel();
            Label labelEnd = ilg.DefineLabel();
            MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                "get_Reader",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            MethodInfo XmlReader_get_NodeType = typeof(XmlReader).GetMethod(
                "get_NodeType",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_get_Reader);
            ilg.Call(XmlReader_get_NodeType);
            ilg.Ldc(XmlNodeType.Text);
            ilg.Ceq();
            ilg.Brtrue(labelTrue);
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_get_Reader);
            ilg.Call(XmlReader_get_NodeType);
            ilg.Ldc(XmlNodeType.CDATA);
            ilg.Ceq();
            ilg.Brtrue(labelTrue);
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_get_Reader);
            ilg.Call(XmlReader_get_NodeType);
            ilg.Ldc(XmlNodeType.Whitespace);
            ilg.Ceq();
            ilg.Brtrue(labelTrue);
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_get_Reader);
            ilg.Call(XmlReader_get_NodeType);
            ilg.Ldc(XmlNodeType.SignificantWhitespace);
            ilg.Ceq();
            ilg.Br(labelEnd);
            ilg.MarkLabel(labelTrue);
            ilg.Ldc(true);
            ilg.MarkLabel(labelEnd);
            ilg.AndIf();

            if (anyText != null)
            {
                WriteText(anyText);
            }
            Debug.Assert(anyText != null);
        }

        [RequiresUnreferencedCode("calls WriteSourceEnd")]
        private void WriteText(Member member)
        {
            TextAccessor text = member.Mapping.Text!;

            if (text.Mapping is SpecialMapping)
            {
                SpecialMapping special = (SpecialMapping)text.Mapping;
                WriteSourceBeginTyped(member.ArraySource, special.TypeDesc);
                switch (special.TypeDesc!.Kind)
                {
                    case TypeKind.Node:
                        MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                            "get_Reader",
                            CodeGenerator.InstanceBindingFlags,
                            Type.EmptyTypes
                            )!;
                        MethodInfo XmlReader_ReadString = typeof(XmlReader).GetMethod(
                            "ReadContentAsString",
                            CodeGenerator.InstanceBindingFlags,
                            Type.EmptyTypes
                            )!;
                        MethodInfo XmlSerializationReader_get_Document = typeof(XmlSerializationReader).GetMethod(
                            "get_Document",
                            CodeGenerator.InstanceBindingFlags,
                            Type.EmptyTypes
                            )!;
                        MethodInfo XmlDocument_CreateTextNode = typeof(XmlDocument).GetMethod(
                            "CreateTextNode",
                            CodeGenerator.InstanceBindingFlags,
                            new Type[] { typeof(string) }
                            )!;
                        ilg.Ldarg(0);
                        ilg.Call(XmlSerializationReader_get_Document);
                        ilg.Ldarg(0);
                        ilg.Call(XmlSerializationReader_get_Reader);
                        ilg.Call(XmlReader_ReadString);
                        ilg.Call(XmlDocument_CreateTextNode);
                        break;
                    default:
                        throw new InvalidOperationException(SR.XmlInternalError);
                }
                WriteSourceEnd(member.ArraySource, special.TypeDesc.Type!);
            }
            else
            {
                if (member.IsArrayLike)
                {
                    WriteSourceBegin(member.ArraySource);
                    if (text.Mapping!.TypeDesc!.CollapseWhitespace)
                    {
                        ilg.Ldarg(0); // for calling CollapseWhitespace
                    }
                    else
                    {
                    }
                    MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                         "get_Reader",
                         CodeGenerator.InstanceBindingFlags,
                         Type.EmptyTypes
                         )!;
                    MethodInfo XmlReader_ReadString = typeof(XmlReader).GetMethod(
                        "ReadContentAsString",
                        CodeGenerator.InstanceBindingFlags,
                        Type.EmptyTypes
                        )!;
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_ReadString);
                    if (text.Mapping.TypeDesc.CollapseWhitespace)
                    {
                        MethodInfo XmlSerializationReader_CollapseWhitespace = typeof(XmlSerializationReader).GetMethod(
                            "CollapseWhitespace",
                            CodeGenerator.InstanceBindingFlags,
                            new Type[] { typeof(string) }
                            )!;
                        ilg.Call(XmlSerializationReader_CollapseWhitespace);
                    }
                }
                else
                {
                    if (text.Mapping!.TypeDesc == StringTypeDesc || text.Mapping.TypeDesc!.FormatterName == "String")
                    {
                        LocalBuilder tmpLoc = ilg.GetLocal("tmp");
                        MethodInfo XmlSerializationReader_ReadString = typeof(XmlSerializationReader).GetMethod(
                            "ReadString",
                            CodeGenerator.InstanceBindingFlags,
                            new Type[] { typeof(string), typeof(bool) }
                            )!;
                        ilg.Ldarg(0);
                        ilg.Ldloc(tmpLoc);
                        ilg.Ldc(text.Mapping.TypeDesc!.CollapseWhitespace);
                        ilg.Call(XmlSerializationReader_ReadString);
                        ilg.Stloc(tmpLoc);

                        WriteSourceBegin(member.ArraySource);
                        ilg.Ldloc(tmpLoc);
                    }
                    else
                    {
                        WriteSourceBegin(member.ArraySource);
                        WritePrimitive(text.Mapping, "Reader.ReadString()");
                    }
                }
                WriteSourceEnd(member.ArraySource, text.Mapping.TypeDesc.Type!);
            }
        }

        [RequiresUnreferencedCode("calls WriteElement")]
        private void WriteMemberElementsElse(Member? anyElement, string elementElseString)
        {
            if (anyElement != null)
            {
                ElementAccessor[] elements = anyElement.Mapping.Elements!;
                for (int i = 0; i < elements.Length; i++)
                {
                    ElementAccessor element = elements[i];
                    if (element.Any && element.Name.Length == 0)
                    {
                        WriteElement(anyElement.ArraySource, anyElement.ArrayName, anyElement.ChoiceArraySource, element, anyElement.Mapping.ChoiceIdentifier, anyElement.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite ? anyElement.CheckSpecifiedSource : null, false, false, -1, i);
                        break;
                    }
                }
            }
            else
            {
                ILGenElementElseString(elementElseString);
            }
        }

        private static bool IsSequence(Member[] members)
        {
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i].Mapping.IsParticle && members[i].Mapping.IsSequence)
                    return true;
            }
            return false;
        }

        [RequiresUnreferencedCode("calls WriteElement")]
        private void WriteMemberElementsIf(Member[] members, Member? anyElement, string elementElseString)
        {
            int count = 0;

            bool isSequence = IsSequence(members);
            int cases = 0;

            for (int i = 0; i < members.Length; i++)
            {
                Member member = (Member)members[i];
                if (member.Mapping.Xmlns != null)
                    continue;
                if (member.Mapping.Ignore)
                    continue;
                if (isSequence && (member.Mapping.IsText || member.Mapping.IsAttribute))
                    continue;

                bool firstElement = true;
                ChoiceIdentifierAccessor? choice = member.Mapping.ChoiceIdentifier;
                ElementAccessor[] elements = member.Mapping.Elements!;

                for (int j = 0; j < elements.Length; j++)
                {
                    ElementAccessor e = elements[j];
                    string? ns = e.Form == XmlSchemaForm.Qualified ? e.Namespace : "";
                    if (!isSequence && e.Any && (e.Name == null || e.Name.Length == 0)) continue;
                    if (!firstElement || (!isSequence && count > 0))
                    {
                        ilg.InitElseIf();
                    }
                    else if (isSequence)
                    {
                        if (cases > 0)
                            ilg.InitElseIf();
                        else
                            ilg.InitIf();
                        ilg.Ldloc("state");
                        ilg.Ldc(cases);
                        ilg.AndIf(Cmp.EqualTo);
                        ilg.InitIf();
                    }
                    else
                    {
                        ilg.InitIf();
                    }
                    count++;
                    firstElement = false;
                    if (member.ParamsReadSource != null)
                    {
                        ILGenParamsReadSource(member.ParamsReadSource);
                        ilg.Ldc(false);
                        ilg.AndIf(Cmp.EqualTo);
                    }
                    Label labelTrue = ilg.DefineLabel();
                    Label labelEnd = ilg.DefineLabel();
                    if (member.Mapping.IsReturnValue)
                    {
                        MethodInfo XmlSerializationReader_get_IsReturnValue = typeof(XmlSerializationReader).GetMethod(
                            "get_IsReturnValue",
                            CodeGenerator.InstanceBindingFlags,
                            Type.EmptyTypes
                            )!;
                        ilg.Ldarg(0);
                        ilg.Call(XmlSerializationReader_get_IsReturnValue);
                        ilg.Brtrue(labelTrue);
                    }
                    if (isSequence && e.Any && e.AnyNamespaces == null)
                    {
                        ilg.Ldc(true);
                    }
                    else
                    {
                        WriteXmlNodeEqual("Reader", e.Name, ns, false);
                    }
                    if (member.Mapping.IsReturnValue)
                    {
                        ilg.Br_S(labelEnd);
                        ilg.MarkLabel(labelTrue);
                        ilg.Ldc(true);
                        ilg.MarkLabel(labelEnd);
                    }
                    ilg.AndIf();

                    WriteElement(member.ArraySource, member.ArrayName, member.ChoiceArraySource, e, choice, member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite ? member.CheckSpecifiedSource : null, member.IsList && member.Mapping.TypeDesc!.IsNullable, member.Mapping.ReadOnly, member.FixupIndex, j);
                    if (member.Mapping.IsReturnValue)
                    {
                        MethodInfo XmlSerializationReader_set_IsReturnValue = typeof(XmlSerializationReader).GetMethod(
                            "set_IsReturnValue",
                            CodeGenerator.InstanceBindingFlags,
                            new Type[] { typeof(bool) }
                            )!;
                        ilg.Ldarg(0);
                        ilg.Ldc(false);
                        ilg.Call(XmlSerializationReader_set_IsReturnValue);
                    }
                    if (member.ParamsReadSource != null)
                    {
                        ILGenParamsReadSource(member.ParamsReadSource, true);
                    }
                }
                if (isSequence)
                {
                    if (member.IsArrayLike)
                    {
                        ilg.Else();
                    }
                    else
                    {
                        ilg.EndIf();
                    }
                    cases++;
                    ilg.Ldc(cases);
                    ilg.Stloc(ilg.GetLocal("state"));
                    if (member.IsArrayLike)
                    {
                        ilg.EndIf();
                    }
                }
            }
            if (count > 0)
            {
                ilg.Else();
            }
            WriteMemberElementsElse(anyElement, elementElseString);
            if (count > 0)
            {
                ilg.EndIf();
            }
        }

        private static string GetArraySource(TypeDesc typeDesc, string arrayName)
        {
            return GetArraySource(typeDesc, arrayName, false);
        }
        private static string GetArraySource(TypeDesc typeDesc, string arrayName, bool multiRef)
        {
            string a = arrayName;
            string c = $"c{a}";
            string init = "";

            if (multiRef)
            {
                init = $"soap = (System.Object[])EnsureArrayIndex(soap, {c}+2, typeof(System.Object)); ";
            }
            if (typeDesc.IsArray)
            {
                string arrayTypeFullName = typeDesc.ArrayElementTypeDesc!.CSharpName;
                init = $"{init}{a} = ({arrayTypeFullName}[])EnsureArrayIndex({a}, {c}, {ReflectionAwareILGen.GetStringForTypeof(arrayTypeFullName)});";
                string arraySource = ReflectionAwareILGen.GetStringForArrayMember(a, $"{c}++", typeDesc);
                if (multiRef)
                {
                    init = $"{init} soap[1] = {a};";
                    init = $"{init} if (ReadReference(out soap[{c}+2])) {arraySource} = null; else ";
                }
                return $"{init}{arraySource}";
            }
            else
            {
                return ReflectionAwareILGen.GetStringForMethod(arrayName, typeDesc.CSharpName, "Add");
            }
        }

        [RequiresUnreferencedCode("calls WriteMemberEnd")]
        private void WriteMemberEnd(Member[] members)
        {
            WriteMemberEnd(members, false);
        }

        [RequiresUnreferencedCode("calls WriteSourceEnd")]
        private void WriteMemberEnd(Member[] members, bool soapRefs)
        {
            for (int i = 0; i < members.Length; i++)
            {
                Member member = (Member)members[i];

                if (member.IsArrayLike)
                {
                    TypeDesc typeDesc = member.Mapping.TypeDesc!;

                    if (typeDesc.IsArray)
                    {
                        WriteSourceBegin(member.Source);

                        Debug.Assert(!soapRefs);

                        string a = member.ArrayName;
                        string c = $"c{a}";

                        MethodInfo XmlSerializationReader_ShrinkArray = typeof(XmlSerializationReader).GetMethod(
                            "ShrinkArray",
                            CodeGenerator.InstanceBindingFlags,
                            new Type[] { typeof(Array), typeof(int), typeof(Type), typeof(bool) }
                            )!;
                        ilg.Ldarg(0);
                        ilg.Ldloc(ilg.GetLocal(a));
                        ilg.Ldloc(ilg.GetLocal(c));
                        ilg.Ldc(typeDesc.ArrayElementTypeDesc!.Type!);
                        ilg.Ldc(member.IsNullable);
                        ilg.Call(XmlSerializationReader_ShrinkArray);
                        ilg.ConvertValue(XmlSerializationReader_ShrinkArray.ReturnType, typeDesc.Type!);
                        WriteSourceEnd(member.Source, typeDesc.Type!);

                        if (member.Mapping.ChoiceIdentifier != null)
                        {
                            WriteSourceBegin(member.ChoiceSource!);
                            a = member.ChoiceArrayName;
                            c = $"c{a}";

                            ilg.Ldarg(0);
                            ilg.Ldloc(ilg.GetLocal(a));
                            ilg.Ldloc(ilg.GetLocal(c));
                            ilg.Ldc(member.Mapping.ChoiceIdentifier.Mapping!.TypeDesc!.Type!);
                            ilg.Ldc(member.IsNullable);
                            ilg.Call(XmlSerializationReader_ShrinkArray);
                            ilg.ConvertValue(XmlSerializationReader_ShrinkArray.ReturnType, member.Mapping.ChoiceIdentifier.Mapping.TypeDesc.Type!.MakeArrayType());
                            WriteSourceEnd(member.ChoiceSource!, member.Mapping.ChoiceIdentifier.Mapping.TypeDesc.Type!.MakeArrayType());
                        }
                    }
                    else if (typeDesc.IsValueType)
                    {
                        LocalBuilder arrayLoc = ilg.GetLocal(member.ArrayName);
                        WriteSourceBegin(member.Source);
                        ilg.Ldloc(arrayLoc);
                        WriteSourceEnd(member.Source, arrayLoc.LocalType);
                    }
                }
            }
        }

        private void WriteSourceBeginTyped(string source, TypeDesc? typeDesc)
        {
            WriteSourceBegin(source);
        }

        [RegexGenerator("(?<locA1>[^ ]+) = .+EnsureArrayIndex[(](?<locA2>[^,]+), (?<locI1>[^,]+),[^;]+;(?<locA3>[^[]+)[[](?<locI2>[^+]+)[+][+][]]")]
        private static partial Regex EnsureArrayIndexRegex();

        [RegexGenerator("(?<a>[^[]+)[[](?<ia>.+)[]]")]
        private static partial Regex P0Regex();

        private void WriteSourceBegin(string source)
        {
            object? variable;
            if (ilg.TryGetVariable(source, out variable))
            {
                Type varType = CodeGenerator.GetVariableType(variable);
                if (CodeGenerator.IsNullableGenericType(varType))
                {
                    // local address to invoke ctor on WriteSourceEnd
                    ilg.LoadAddress(variable);
                }
                return;
            }
            // o.@Field
            if (source.StartsWith("o.@", StringComparison.Ordinal))
            {
                ilg.LdlocAddress(ilg.GetLocal("o"));
                return;
            }
            // a_0_0 = (global::System.Object[])EnsureArrayIndex(a_0_0, ca_0_0, typeof(global::System.Object));a_0_0[ca_0_0++]
            Match match = EnsureArrayIndexRegex().Match(source);
            if (match.Success)
            {
                Debug.Assert(match.Groups["locA1"].Value == match.Groups["locA2"].Value);
                Debug.Assert(match.Groups["locA1"].Value == match.Groups["locA3"].Value);
                Debug.Assert(match.Groups["locI1"].Value == match.Groups["locI2"].Value);

                LocalBuilder localA = ilg.GetLocal(match.Groups["locA1"].Value);
                LocalBuilder localI = ilg.GetLocal(match.Groups["locI1"].Value);
                Type arrayElementType = localA.LocalType.GetElementType()!;
                MethodInfo XmlSerializationReader_EnsureArrayIndex = typeof(XmlSerializationReader).GetMethod(
                    "EnsureArrayIndex",
                    CodeGenerator.InstanceBindingFlags,
                    new Type[] { typeof(Array), typeof(int), typeof(Type) }
                    )!;
                ilg.Ldarg(0);
                ilg.Ldloc(localA);
                ilg.Ldloc(localI);
                ilg.Ldc(arrayElementType);
                ilg.Call(XmlSerializationReader_EnsureArrayIndex);
                ilg.Castclass(localA.LocalType);
                ilg.Stloc(localA);

                // a_0[ca_0++]
                ilg.Ldloc(localA);
                ilg.Ldloc(localI);
                ilg.Dup();
                ilg.Ldc(1);
                ilg.Add();
                ilg.Stloc(localI);
                if (CodeGenerator.IsNullableGenericType(arrayElementType) || arrayElementType.IsValueType)
                {
                    ilg.Ldelema(arrayElementType);
                }
                return;
            }
            //"a_0_0.Add("
            if (source.EndsWith(".Add(", StringComparison.Ordinal))
            {
                int index = source.LastIndexOf(".Add(", StringComparison.Ordinal);
                LocalBuilder localA = ilg.GetLocal(source.Substring(0, index));
                ilg.LdlocAddress(localA);
                return;
            }

            // p[0]
            match = P0Regex().Match(source);
            if (match.Success)
            {
                System.Diagnostics.Debug.Assert(CodeGenerator.GetVariableType(ilg.GetVariable(match.Groups["a"].Value)).IsArray);
                ilg.Load(ilg.GetVariable(match.Groups["a"].Value));
                ilg.Load(ilg.GetVariable(match.Groups["ia"].Value));
                return;
            }
            throw Globals.NotSupported($"Unexpected: {source}");
        }

        [RequiresUnreferencedCode("calls WriteSourceEnd")]
        private void WriteSourceEnd(string source, Type elementType)
        {
            WriteSourceEnd(source, elementType, elementType);
        }
        [RequiresUnreferencedCode("string-based IL generation")]
        private void WriteSourceEnd(string source, Type elementType, Type stackType)
        {
            object? variable;
            if (ilg.TryGetVariable(source, out variable))
            {
                Type varType = CodeGenerator.GetVariableType(variable);
                if (CodeGenerator.IsNullableGenericType(varType))
                {
                    ilg.Call(varType.GetConstructor(varType.GetGenericArguments())!);
                }
                else
                {
                    Debug.Assert(elementType != null && variable is LocalBuilder);
                    ilg.ConvertValue(stackType, elementType);
                    ilg.ConvertValue(elementType, varType);
                    ilg.Stloc((LocalBuilder)variable);
                }
                return;
            }
            // o.@Field
            if (source.StartsWith("o.@", StringComparison.Ordinal))
            {
                Debug.Assert(memberInfos.ContainsKey(source.Substring(3)));
                MemberInfo memInfo = memberInfos[source.Substring(3)];
                ilg.ConvertValue(stackType, memInfo is FieldInfo ? ((FieldInfo)memInfo).FieldType : ((PropertyInfo)memInfo).PropertyType);
                ilg.StoreMember(memInfo);
                return;
            }
            // a_0_0 = (global::System.Object[])EnsureArrayIndex(a_0_0, ca_0_0, typeof(global::System.Object));a_0_0[ca_0_0++]
            Match match = EnsureArrayIndexRegex().Match(source);
            if (match.Success)
            {
                object oVar = ilg.GetVariable(match.Groups["locA1"].Value);
                Type arrayElementType = CodeGenerator.GetVariableType(oVar).GetElementType()!;
                ilg.ConvertValue(elementType, arrayElementType);
                if (CodeGenerator.IsNullableGenericType(arrayElementType) || arrayElementType.IsValueType)
                {
                    ilg.Stobj(arrayElementType);
                }
                else
                {
                    ilg.Stelem(arrayElementType);
                }
                return;
            }
            //"a_0_0.Add("
            if (source.EndsWith(".Add(", StringComparison.Ordinal))
            {
                int index = source.LastIndexOf(".Add(", StringComparison.Ordinal);
                LocalBuilder localA = ilg.GetLocal(source.Substring(0, index));
                Debug.Assert(!localA.LocalType.IsGenericType || (localA.LocalType.GetGenericArguments().Length == 1 && localA.LocalType.GetGenericArguments()[0].IsAssignableFrom(elementType)));
                MethodInfo Add = localA.LocalType.GetMethod(
                     "Add",
                     CodeGenerator.InstanceBindingFlags,
                     new Type[] { elementType }
                     )!;
                Debug.Assert(Add != null);
                Type addParameterType = Add.GetParameters()[0].ParameterType;
                ilg.ConvertValue(stackType, addParameterType);
                ilg.Call(Add);
                if (Add.ReturnType != typeof(void))
                    ilg.Pop();
                return;
            }
            // p[0]
            match = P0Regex().Match(source);
            if (match.Success)
            {
                Type varType = CodeGenerator.GetVariableType(ilg.GetVariable(match.Groups["a"].Value));
                System.Diagnostics.Debug.Assert(varType.IsArray);
                Type varElementType = varType.GetElementType()!;
                ilg.ConvertValue(stackType, varElementType);
                ilg.Stelem(varElementType);
                return;
            }
            throw Globals.NotSupported($"Unexpected: {source}");
        }

        [RequiresUnreferencedCode("calls WriteMemberBegin")]
        private void WriteArray(string source, string? arrayName, ArrayMapping arrayMapping, bool readOnly, bool isNullable, int fixupIndex, int elementIndex)
        {
            MethodInfo XmlSerializationReader_ReadNull = typeof(XmlSerializationReader).GetMethod(
                "ReadNull",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_ReadNull);
            ilg.IfNot();

            MemberMapping memberMapping = new MemberMapping();
            memberMapping.Elements = arrayMapping.Elements;
            memberMapping.TypeDesc = arrayMapping.TypeDesc;
            memberMapping.ReadOnly = readOnly;
            if (source.StartsWith("o.@", StringComparison.Ordinal))
            {
                Debug.Assert(memberInfos.ContainsKey(source.Substring(3)));
                memberMapping.MemberInfo = memberInfos[source.Substring(3)];
            }
            Member member = new Member(this, source, arrayName, elementIndex, memberMapping, false);
            member.IsNullable = false; //Note, IsNullable is set to false since null condition (xsi:nil) is already handled by 'ReadNull()'

            Member[] members = new Member[] { member };
            WriteMemberBegin(members);
            Label labelTrue = ilg.DefineLabel();
            Label labelEnd = ilg.DefineLabel();

            if (readOnly)
            {
                ilg.Load(ilg.GetVariable(member.ArrayName));
                ilg.Load(null);
                ilg.Beq(labelTrue);
            }
            else
            {
            }
            MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                "get_Reader",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            MethodInfo XmlReader_get_IsEmptyElement = typeof(XmlReader).GetMethod(
                "get_IsEmptyElement",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_get_Reader);
            ilg.Call(XmlReader_get_IsEmptyElement);
            if (readOnly)
            {
                ilg.Br_S(labelEnd);
                ilg.MarkLabel(labelTrue);
                ilg.Ldc(true);
                ilg.MarkLabel(labelEnd);
            }
            ilg.If();
            MethodInfo XmlReader_Skip = typeof(XmlReader).GetMethod(
                "Skip",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_get_Reader);
            ilg.Call(XmlReader_Skip);
            ilg.Else();

            MethodInfo XmlReader_ReadStartElement = typeof(XmlReader).GetMethod(
                "ReadStartElement",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_get_Reader);
            ilg.Call(XmlReader_ReadStartElement);
            WriteWhileNotLoopStart();

            string unknownNode = $"UnknownNode(null, {ExpectedElements(members)});";
            WriteMemberElements(members, unknownNode, unknownNode, null, null);
            MethodInfo XmlReader_MoveToContent = typeof(XmlReader).GetMethod(
                "MoveToContent",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_get_Reader);
            ilg.Call(XmlReader_MoveToContent);
            ilg.Pop();

            WriteWhileLoopEnd();
            MethodInfo XmlSerializationReader_ReadEndElement = typeof(XmlSerializationReader).GetMethod(
                "ReadEndElement",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_ReadEndElement);
            ilg.EndIf();

            WriteMemberEnd(members, false);

            if (isNullable)
            {
                ilg.Else();
                member.IsNullable = true;
                WriteMemberBegin(members);
                WriteMemberEnd(members);
            }
            ilg.EndIf();
        }

        [RequiresUnreferencedCode("calls ILGenForCreateInstance")]
        private void WriteElement(string source, string? arrayName, string? choiceSource, ElementAccessor element, ChoiceIdentifierAccessor? choice, string? checkSpecified, bool checkForNull, bool readOnly, int fixupIndex, int elementIndex)
        {
            if (checkSpecified != null && checkSpecified.Length > 0)
            {
                ILGenSet(checkSpecified, true);
            }

            if (element.Mapping is ArrayMapping)
            {
                WriteArray(source, arrayName, (ArrayMapping)element.Mapping, readOnly, element.IsNullable, fixupIndex, elementIndex);
            }
            else if (element.Mapping is NullableMapping)
            {
                string? methodName = ReferenceMapping(element.Mapping);
#if DEBUG
                // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                if (methodName == null) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorMethod, element.Mapping.TypeDesc!.Name));
#endif
                WriteSourceBegin(source);
                ilg.Ldarg(0);
                ilg.Ldc(true);
                MethodBuilder methodBuilder = EnsureMethodBuilder(typeBuilder,
                    methodName!,
                    CodeGenerator.PrivateMethodAttributes,
                    // See WriteNullableMethod for different return type logic
                    element.Mapping.TypeDesc!.Type,
                    new Type[] { typeof(bool) }
                    );
                ilg.Call(methodBuilder);
                WriteSourceEnd(source, element.Mapping.TypeDesc.Type!);
            }
            else if (element.Mapping is PrimitiveMapping)
            {
                bool doEndIf = false;
                if (element.IsNullable)
                {
                    MethodInfo XmlSerializationReader_ReadNull = typeof(XmlSerializationReader).GetMethod(
                         "ReadNull",
                         CodeGenerator.InstanceBindingFlags,
                         Type.EmptyTypes
                         )!;
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_ReadNull);
                    ilg.If();
                    WriteSourceBegin(source);
                    if (element.Mapping.TypeDesc!.IsValueType)
                    {
                        throw Globals.NotSupported("No such condition.  PrimitiveMapping && IsNullable = String, XmlQualifiedName and never IsValueType");
                    }
                    else
                    {
                        ilg.Load(null);
                    }
                    WriteSourceEnd(source, element.Mapping.TypeDesc.Type!);
                    ilg.Else();
                    doEndIf = true;
                }
                if (element.Default != null && element.Default != DBNull.Value && element.Mapping.TypeDesc!.IsValueType)
                {
                    MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                        "get_Reader",
                        CodeGenerator.InstanceBindingFlags,
                        Type.EmptyTypes
                        )!;
                    MethodInfo XmlReader_get_IsEmptyElement = typeof(XmlReader).GetMethod(
                        "get_IsEmptyElement",
                        CodeGenerator.InstanceBindingFlags,
                        Type.EmptyTypes
                        )!;
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_get_IsEmptyElement);
                    ilg.If();
                    MethodInfo XmlReader_Skip = typeof(XmlReader).GetMethod(
                        "Skip",
                        CodeGenerator.InstanceBindingFlags,
                        Type.EmptyTypes
                        )!;
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_Skip);
                    ilg.Else();
                    doEndIf = true;
                }
                else
                {
                }

                if ((element.Mapping.TypeDesc!.Type == typeof(TimeSpan)) || element.Mapping.TypeDesc!.Type == typeof(DateTimeOffset))
                {
                    MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                       "get_Reader",
                       CodeGenerator.InstanceBindingFlags,
                       Type.EmptyTypes
                       )!;
                    MethodInfo XmlReader_get_IsEmptyElement = typeof(XmlReader).GetMethod(
                        "get_IsEmptyElement",
                        CodeGenerator.InstanceBindingFlags,
                        Type.EmptyTypes
                        )!;
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_get_IsEmptyElement);
                    ilg.If();
                    WriteSourceBegin(source);
                    MethodInfo XmlReader_Skip = typeof(XmlReader).GetMethod(
                        "Skip",
                        CodeGenerator.InstanceBindingFlags,
                        Type.EmptyTypes
                        )!;
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_Skip);
                    LocalBuilder tmpLoc = ilg.GetTempLocal(element.Mapping.TypeDesc!.Type);
                    ilg.Ldloca(tmpLoc);
                    ilg.InitObj(element.Mapping.TypeDesc!.Type);
                    ilg.Ldloc(tmpLoc);
                    WriteSourceEnd(source, element.Mapping.TypeDesc.Type);
                    ilg.Else();
                    WriteSourceBegin(source);
                    WritePrimitive(element.Mapping, "Reader.ReadElementString()");
                    WriteSourceEnd(source, element.Mapping.TypeDesc.Type);
                    ilg.EndIf();
                }
                else
                {
                    WriteSourceBegin(source);
                    if (element.Mapping.TypeDesc == QnameTypeDesc)
                    {
                        MethodInfo XmlSerializationReader_ReadElementQualifiedName = typeof(XmlSerializationReader).GetMethod(
                               "ReadElementQualifiedName",
                               CodeGenerator.InstanceBindingFlags,
                               Type.EmptyTypes
                               )!;
                        ilg.Ldarg(0);
                        ilg.Call(XmlSerializationReader_ReadElementQualifiedName);
                    }
                    else
                    {
                        string readFunc;
                        switch (element.Mapping.TypeDesc.FormatterName)
                        {
                            case "ByteArrayBase64":
                            case "ByteArrayHex":
                                readFunc = "false";
                                break;
                            default:
                                readFunc = "Reader.ReadElementString()";
                                break;
                        }
                        WritePrimitive(element.Mapping, readFunc);
                    }

                    WriteSourceEnd(source, element.Mapping.TypeDesc.Type!);
                }

                if (doEndIf)
                    ilg.EndIf();
            }
            else if (element.Mapping is StructMapping)
            {
                TypeMapping mapping = element.Mapping;
                string? methodName = ReferenceMapping(mapping);
#if DEBUG
                // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                if (methodName == null) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorMethod, mapping.TypeDesc!.Name));
#endif

                if (checkForNull)
                {
                    MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                        "get_Reader",
                        CodeGenerator.InstanceBindingFlags,
                        Type.EmptyTypes
                        )!;
                    MethodInfo XmlReader_Skip = typeof(XmlReader).GetMethod(
                        "Skip",
                        CodeGenerator.InstanceBindingFlags,
                        Type.EmptyTypes
                        )!;
                    ilg.Ldloc(arrayName!);
                    ilg.Load(null);
                    ilg.If(Cmp.EqualTo);
                    ilg.Ldarg(0);
                    ilg.Call(XmlSerializationReader_get_Reader);
                    ilg.Call(XmlReader_Skip);
                    ilg.Else();
                }
                WriteSourceBegin(source);
                List<Type> argTypes = new List<Type>();
                ilg.Ldarg(0);
                if (mapping.TypeDesc!.IsNullable)
                {
                    ilg.Load(element.IsNullable);
                    argTypes.Add(typeof(bool));
                }
                ilg.Ldc(true);
                argTypes.Add(typeof(bool));
                MethodBuilder methodBuilder = EnsureMethodBuilder(typeBuilder,
                    methodName!,
                    CodeGenerator.PrivateMethodAttributes,
                    mapping.TypeDesc.Type,
                    argTypes.ToArray()
                    );
                ilg.Call(methodBuilder);
                WriteSourceEnd(source, mapping.TypeDesc.Type!);
                if (checkForNull)
                    // 'If' begins in checkForNull above
                    ilg.EndIf();
            }
            else if (element.Mapping is SpecialMapping)
            {
                SpecialMapping special = (SpecialMapping)element.Mapping;
                switch (special.TypeDesc!.Kind)
                {
                    case TypeKind.Node:
                        bool isDoc = special.TypeDesc.FullName == typeof(XmlDocument).FullName;
                        WriteSourceBeginTyped(source, special.TypeDesc);
                        MethodInfo XmlSerializationReader_ReadXmlXXX = typeof(XmlSerializationReader).GetMethod(
                              isDoc ? "ReadXmlDocument" : "ReadXmlNode",
                              CodeGenerator.InstanceBindingFlags,
                              new Type[] { typeof(bool) }
                              )!;
                        ilg.Ldarg(0);
                        ilg.Ldc(element.Any ? false : true);
                        ilg.Call(XmlSerializationReader_ReadXmlXXX);
                        // See logic in WriteSourceBeginTyped whether or not to castclass.
                        if (special.TypeDesc != null)
                            ilg.Castclass(special.TypeDesc.Type!);
                        WriteSourceEnd(source, special.TypeDesc!.Type!);
                        break;
                    case TypeKind.Serializable:
                        SerializableMapping sm = (SerializableMapping)element.Mapping;
                        // check to see if we need to do the derivation
                        if (sm.DerivedMappings != null)
                        {
                            MethodInfo XmlSerializationReader_GetXsiType = typeof(XmlSerializationReader).GetMethod(
                                "GetXsiType",
                                CodeGenerator.InstanceBindingFlags,
                                Type.EmptyTypes
                                )!;
                            Label labelTrue = ilg.DefineLabel();
                            Label labelEnd = ilg.DefineLabel();
                            LocalBuilder tserLoc = ilg.DeclareOrGetLocal(typeof(XmlQualifiedName), "tser");
                            ilg.Ldarg(0);
                            ilg.Call(XmlSerializationReader_GetXsiType);
                            ilg.Stloc(tserLoc);
                            ilg.Ldloc(tserLoc);
                            ilg.Load(null);
                            ilg.Ceq();
                            ilg.Brtrue(labelTrue);
                            WriteQNameEqual("tser", sm.XsiType!.Name, sm.XsiType.Namespace);

                            ilg.Br_S(labelEnd);
                            ilg.MarkLabel(labelTrue);
                            ilg.Ldc(true);
                            ilg.MarkLabel(labelEnd);
                            ilg.If();
                        }
                        WriteSourceBeginTyped(source, sm.TypeDesc!);
                        bool isWrappedAny = !element.Any && IsWildcard(sm);
                        MethodInfo XmlSerializationReader_ReadSerializable = typeof(XmlSerializationReader).GetMethod(
                             "ReadSerializable",
                             CodeGenerator.InstanceBindingFlags,
                             isWrappedAny ? new Type[] { typeof(IXmlSerializable), typeof(bool) } : new Type[] { typeof(IXmlSerializable) }
                             )!;
                        ilg.Ldarg(0);
                        ReflectionAwareILGen.ILGenForCreateInstance(ilg, sm.TypeDesc!.Type!, sm.TypeDesc.CannotNew, false);
                        if (sm.TypeDesc.CannotNew)
                            ilg.ConvertValue(typeof(object), typeof(IXmlSerializable));
                        if (isWrappedAny)
                            ilg.Ldc(true);
                        ilg.Call(XmlSerializationReader_ReadSerializable);
                        // See logic in WriteSourceBeginTyped whether or not to castclass.
                        if (sm.TypeDesc != null)
                            ilg.ConvertValue(typeof(IXmlSerializable), sm.TypeDesc.Type!);
                        WriteSourceEnd(source, sm.TypeDesc!.Type!);
                        if (sm.DerivedMappings != null)
                        {
                            WriteDerivedSerializable(sm, sm, source, isWrappedAny);
                            WriteUnknownNode("UnknownNode", "null", null, true);
                        }
                        break;
                    default:
                        throw new InvalidOperationException(SR.XmlInternalError);
                }
            }
            else
            {
                throw new InvalidOperationException(SR.XmlInternalError);
            }
            if (choice != null)
            {
#if DEBUG
                // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                if (choiceSource == null) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "need parent for the " + source));
#endif

                WriteSourceBegin(choiceSource!);
                CodeIdentifier.CheckValidIdentifier(choice.MemberIds![elementIndex]);
                ReflectionAwareILGen.ILGenForEnumMember(ilg, choice.Mapping!.TypeDesc!.Type!, choice.MemberIds[elementIndex]);
                WriteSourceEnd(choiceSource!, choice.Mapping.TypeDesc.Type!);
            }
        }

        [RequiresUnreferencedCode("calls ILGenForCreateInstance")]
        private void WriteDerivedSerializable(SerializableMapping head, SerializableMapping? mapping, string source, bool isWrappedAny)
        {
            if (mapping == null)
                return;
            for (SerializableMapping? derived = mapping.DerivedMappings; derived != null; derived = derived.NextDerivedMapping)
            {
                Label labelTrue = ilg.DefineLabel();
                Label labelEnd = ilg.DefineLabel();
                LocalBuilder tserLoc = ilg.GetLocal("tser");
                ilg.InitElseIf();
                ilg.Ldloc(tserLoc);
                ilg.Load(null);
                ilg.Ceq();
                ilg.Brtrue(labelTrue);
                WriteQNameEqual("tser", derived.XsiType!.Name, derived.XsiType.Namespace);

                ilg.Br_S(labelEnd);
                ilg.MarkLabel(labelTrue);
                ilg.Ldc(true);
                ilg.MarkLabel(labelEnd);
                ilg.AndIf();

                if (derived.Type != null)
                {
                    if (head.Type!.IsAssignableFrom(derived.Type))
                    {
                        WriteSourceBeginTyped(source, head.TypeDesc!);
                        MethodInfo XmlSerializationReader_ReadSerializable = typeof(XmlSerializationReader).GetMethod(
                             "ReadSerializable",
                             CodeGenerator.InstanceBindingFlags,
                             isWrappedAny ? new Type[] { typeof(IXmlSerializable), typeof(bool) } : new Type[] { typeof(IXmlSerializable) }
                             )!;
                        ilg.Ldarg(0);
                        ReflectionAwareILGen.ILGenForCreateInstance(ilg, derived.TypeDesc!.Type!, derived.TypeDesc.CannotNew, false);
                        if (derived.TypeDesc.CannotNew)
                            ilg.ConvertValue(typeof(object), typeof(IXmlSerializable));
                        if (isWrappedAny)
                            ilg.Ldc(true);
                        ilg.Call(XmlSerializationReader_ReadSerializable);
                        // See logic in WriteSourceBeginTyped whether or not to castclass.
                        if (head.TypeDesc != null)
                            ilg.ConvertValue(typeof(IXmlSerializable), head.TypeDesc.Type!);
                        WriteSourceEnd(source, head.TypeDesc!.Type!);
                    }
                    else
                    {
                        MethodInfo XmlSerializationReader_CreateBadDerivationException = typeof(XmlSerializationReader).GetMethod(
                           "CreateBadDerivationException",
                           CodeGenerator.InstanceBindingFlags,
                           new Type[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) }
                           )!;
                        ilg.Ldarg(0);
                        ilg.Ldstr(GetCSharpString(derived.XsiType.Name));
                        ilg.Ldstr(GetCSharpString(derived.XsiType.Namespace));
                        ilg.Ldstr(GetCSharpString(head.XsiType!.Name));
                        ilg.Ldstr(GetCSharpString(head.XsiType.Namespace));
                        ilg.Ldstr(GetCSharpString(derived.Type.FullName));
                        ilg.Ldstr(GetCSharpString(head.Type.FullName));
                        ilg.Call(XmlSerializationReader_CreateBadDerivationException);
                        ilg.Throw();
                    }
                }
                else
                {
                    MethodInfo XmlSerializationReader_CreateMissingIXmlSerializableType = typeof(XmlSerializationReader).GetMethod(
                       "CreateMissingIXmlSerializableType",
                       CodeGenerator.InstanceBindingFlags,
                       new Type[] { typeof(string), typeof(string), typeof(string) }
                       )!;
                    ilg.Ldarg(0);
                    ilg.Ldstr(GetCSharpString(derived.XsiType.Name));
                    ilg.Ldstr(GetCSharpString(derived.XsiType.Namespace));
                    ilg.Ldstr(GetCSharpString(head.Type!.FullName));
                    ilg.Call(XmlSerializationReader_CreateMissingIXmlSerializableType);
                    ilg.Throw();
                }


                WriteDerivedSerializable(head, derived, source, isWrappedAny);
            }
        }

        private void WriteWhileNotLoopStart()
        {
            MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                "get_Reader",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            MethodInfo XmlReader_MoveToContent = typeof(XmlReader).GetMethod(
                "MoveToContent",
                CodeGenerator.InstanceBindingFlags,
                Type.EmptyTypes
                )!;
            ilg.Ldarg(0);
            ilg.Call(XmlSerializationReader_get_Reader);
            ilg.Call(XmlReader_MoveToContent);
            ilg.Pop();
            ilg.WhileBegin();
        }

        private void WriteWhileLoopEnd()
        {
            ilg.WhileBeginCondition();
            {
                int XmlNodeType_None = 0;
                //int XmlNodeType_Element = 1;
                int XmlNodeType_EndElement = 15;

                MethodInfo XmlSerializationReader_get_Reader = typeof(XmlSerializationReader).GetMethod(
                    "get_Reader",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                MethodInfo XmlReader_get_NodeType = typeof(XmlReader).GetMethod(
                    "get_NodeType",
                    CodeGenerator.InstanceBindingFlags,
                    Type.EmptyTypes
                    )!;
                Label labelFalse = ilg.DefineLabel();
                Label labelEnd = ilg.DefineLabel();
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_get_NodeType);
                ilg.Ldc(XmlNodeType_EndElement);
                ilg.Beq(labelFalse);
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_get_Reader);
                ilg.Call(XmlReader_get_NodeType);
                ilg.Ldc(XmlNodeType_None);
                ilg.Cne();
                ilg.Br_S(labelEnd);
                ilg.MarkLabel(labelFalse);
                ilg.Ldc(false);
                ilg.MarkLabel(labelEnd);
            }
            ilg.WhileEndCondition();
            ilg.WhileEnd();
        }

        private void WriteParamsRead(int length)
        {
            const int StackallocLimit =
#if DEBUG
                3; // lower limit in debug to help test both stackalloc and array code paths
#else
                32; // arbitrary limit
#endif
            LocalBuilder paramsRead = ilg.DeclareLocal(typeof(Span<bool>), "paramsRead");
            if (length <= StackallocLimit)
            {
                ilg.StackallocSpan(typeof(bool), length);
            }
            else
            {
                ilg.NewArray(typeof(bool), length);
                ilg.New(typeof(Span<bool>).GetConstructor(new[] { typeof(bool[]) })!);
            }
            ilg.Stloc(paramsRead);
        }

        [RequiresUnreferencedCode("calls ILGenForCreateInstance")]
        private void WriteCreateMapping(TypeMapping mapping, string local)
        {
            string fullTypeName = mapping.TypeDesc!.CSharpName;
            bool ctorInaccessible = mapping.TypeDesc.CannotNew;

            LocalBuilder loc = ilg.DeclareLocal(
                mapping.TypeDesc.Type!,
                local);

            if (ctorInaccessible)
            {
                ilg.BeginExceptionBlock();
            }
            ReflectionAwareILGen.ILGenForCreateInstance(ilg, mapping.TypeDesc.Type!, mapping.TypeDesc.CannotNew, true);
            ilg.Stloc(loc);
            if (ctorInaccessible)
            {
                ilg.Leave();
                WriteCatchException(typeof(MissingMethodException));
                MethodInfo XmlSerializationReader_CreateInaccessibleConstructorException = typeof(XmlSerializationReader).GetMethod(
                      "CreateInaccessibleConstructorException",
                      CodeGenerator.InstanceBindingFlags,
                      new Type[] { typeof(string) }
                      )!;
                ilg.Ldarg(0);
                ilg.Ldstr(GetCSharpString(fullTypeName));
                ilg.Call(XmlSerializationReader_CreateInaccessibleConstructorException);
                ilg.Throw();

                WriteCatchException(typeof(SecurityException));
                MethodInfo XmlSerializationReader_CreateCtorHasSecurityException = typeof(XmlSerializationReader).GetMethod(
                    "CreateCtorHasSecurityException",
                    CodeGenerator.InstanceBindingFlags,
                    new Type[] { typeof(string) }
                    )!;
                ilg.Ldarg(0);
                ilg.Ldstr(GetCSharpString(fullTypeName));
                ilg.Call(XmlSerializationReader_CreateCtorHasSecurityException);
                ilg.Throw();

                ilg.EndExceptionBlock();
            }
        }

        private void WriteCatchException(Type exceptionType)
        {
            ilg.BeginCatchBlock(exceptionType);
            ilg.Pop();
        }

        [RequiresUnreferencedCode("calls WriteArrayLocalDecl")]
        private void WriteArrayLocalDecl(string typeName, string variableName, string initValue, TypeDesc arrayTypeDesc)
        {
            ReflectionAwareILGen.WriteArrayLocalDecl(typeName, variableName, new SourceInfo(initValue, initValue, null, arrayTypeDesc.Type, ilg), arrayTypeDesc);
        }

        [RequiresUnreferencedCode("calls WriteCreateInstance")]
        private void WriteCreateInstance(string source, bool ctorInaccessible, Type type)
        {
            ReflectionAwareILGen.WriteCreateInstance(source, ctorInaccessible, type, ilg);
        }

        [RequiresUnreferencedCode("calls WriteLocalDecl")]
        private static void WriteLocalDecl(string variableName, SourceInfo initValue)
        {
            ReflectionAwareILGen.WriteLocalDecl(variableName, initValue);
        }

        [RegexGenerator("UnknownNode[(]null, @[\"](?<qnames>[^\"]*)[\"][)];")]
        private static partial Regex UnknownNodeNullAnyTypeRegex();

        [RegexGenerator("UnknownNode[(][(]object[)](?<o>[^,]+), @[\"](?<qnames>[^\"]*)[\"][)];")]
        private static partial Regex UnknownNodeObjectEmptyRegex();

        [RegexGenerator("UnknownNode[(][(]object[)](?<o>[^,]+), null[)];")]
        private static partial Regex UnknownNodeObjectNullRegex();

        [RegexGenerator("UnknownNode[(][(]object[)](?<o>[^)]+)[)];")]
        private static partial Regex UnknownNodeObjectRegex();

        [RegexGenerator("paramsRead\\[(?<index>[0-9]+)\\]")]
        private static partial Regex ParamsReadRegex();

        private void ILGenElseString(string elseString)
        {
            MethodInfo XmlSerializationReader_UnknownNode1 = typeof(XmlSerializationReader).GetMethod(
                  "UnknownNode",
                  CodeGenerator.InstanceBindingFlags,
                  new Type[] { typeof(object) }
                  )!;
            MethodInfo XmlSerializationReader_UnknownNode2 = typeof(XmlSerializationReader).GetMethod(
                  "UnknownNode",
                  CodeGenerator.InstanceBindingFlags,
                  new Type[] { typeof(object), typeof(string) }
                  )!;
            // UnknownNode(null, @":anyType");
            Match match = UnknownNodeNullAnyTypeRegex().Match(elseString);
            if (match.Success)
            {
                ilg.Ldarg(0);
                ilg.Load(null);
                ilg.Ldstr(match.Groups["qnames"].Value);
                ilg.Call(XmlSerializationReader_UnknownNode2);
                return;
            }
            // UnknownNode((object)o, @"");
            match = UnknownNodeObjectEmptyRegex().Match(elseString);
            if (match.Success)
            {
                ilg.Ldarg(0);
                LocalBuilder localO = ilg.GetLocal(match.Groups["o"].Value);
                ilg.Ldloc(localO);
                ilg.ConvertValue(localO.LocalType, typeof(object));
                ilg.Ldstr(match.Groups["qnames"].Value);
                ilg.Call(XmlSerializationReader_UnknownNode2);
                return;
            }
            // UnknownNode((object)o, null);
            match = UnknownNodeObjectNullRegex().Match(elseString);
            if (match.Success)
            {
                ilg.Ldarg(0);
                LocalBuilder localO = ilg.GetLocal(match.Groups["o"].Value);
                ilg.Ldloc(localO);
                ilg.ConvertValue(localO.LocalType, typeof(object));
                ilg.Load(null);
                ilg.Call(XmlSerializationReader_UnknownNode2);
                return;
            }
            // "UnknownNode((object)o);"
            match = UnknownNodeObjectRegex().Match(elseString);
            if (match.Success)
            {
                ilg.Ldarg(0);
                LocalBuilder localO = ilg.GetLocal(match.Groups["o"].Value);
                ilg.Ldloc(localO);
                ilg.ConvertValue(localO.LocalType, typeof(object));
                ilg.Call(XmlSerializationReader_UnknownNode1);
                return;
            }
            throw Globals.NotSupported($"Unexpected: {elseString}");
        }
        private void ILGenParamsReadSource(string paramsReadSource)
        {
            Match match = ParamsReadRegex().Match(paramsReadSource);
            if (match.Success)
            {
                ilg.Ldloca(ilg.GetLocal("paramsRead"));
                ilg.Ldc(int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture));
                ilg.Call(typeof(Span<bool>).GetMethod("get_Item")!);
                ilg.LdindU1();
                return;
            }
            throw Globals.NotSupported($"Unexpected: {paramsReadSource}");
        }
        private void ILGenParamsReadSource(string paramsReadSource, bool value)
        {
            Match match = ParamsReadRegex().Match(paramsReadSource);
            if (match.Success)
            {
                ilg.Ldloca(ilg.GetLocal("paramsRead"));
                ilg.Ldc(int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture));
                ilg.Call(typeof(Span<bool>).GetMethod("get_Item")!);
                ilg.Ldc(value);
                ilg.StindI1();
                return;
            }
            throw Globals.NotSupported($"Unexpected: {paramsReadSource}");
        }
        private void ILGenElementElseString(string elementElseString)
        {
            if (elementElseString == "throw CreateUnknownNodeException();")
            {
                MethodInfo XmlSerializationReader_CreateUnknownNodeException = typeof(XmlSerializationReader).GetMethod(
                       "CreateUnknownNodeException",
                       CodeGenerator.InstanceBindingFlags,
                       Type.EmptyTypes
                       )!;
                ilg.Ldarg(0);
                ilg.Call(XmlSerializationReader_CreateUnknownNodeException);
                ilg.Throw();
                return;
            }
            if (elementElseString.StartsWith("UnknownNode(", StringComparison.Ordinal))
            {
                ILGenElseString(elementElseString);
                return;
            }
            throw Globals.NotSupported($"Unexpected: {elementElseString}");
        }

        [RequiresUnreferencedCode("calls WriteSourceEnd")]
        private void ILGenSet(string source, object value)
        {
            WriteSourceBegin(source);
            ilg.Load(value);
            WriteSourceEnd(source, value == null ? typeof(object) : value.GetType());
        }
    }
}
