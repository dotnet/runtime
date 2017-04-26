// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.XmlSerializer.Generator
{
    using System.IO;
    using System;
    using System.Security;
    using System.Collections;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using System.ComponentModel;
    using System.Globalization;
    using System.Diagnostics;
    using System.Threading;
    using System.Configuration;
    using System.Xml.Serialization.Configuration;


    internal class XmlSerializationReaderCodeGen : XmlSerializationCodeGen
    {
        private Hashtable _idNames = new Hashtable();
        private Hashtable _enums;
        private Hashtable _createMethods = new Hashtable();
        private int _nextCreateMethodNumber = 0;
        private int _nextIdNumber = 0;
        private int _nextWhileLoopIndex = 0;

        internal Hashtable Enums
        {
            get
            {
                if (_enums == null)
                {
                    _enums = new Hashtable();
                }
                return _enums;
            }
        }

        private class CreateCollectionInfo
        {
            private string _name;
            private TypeDesc _td;

            internal CreateCollectionInfo(string name, TypeDesc td)
            {
                _name = name;
                _td = td;
            }
            internal string Name
            {
                get { return _name; }
            }

            internal TypeDesc TypeDesc
            {
                get { return _td; }
            }
        }
        private class Member
        {
            private string _source;
            private string _arrayName;
            private string _arraySource;
            private string _choiceArrayName;
            private string _choiceSource;
            private string _choiceArraySource;
            private MemberMapping _mapping;
            private bool _isArray;
            private bool _isList;
            private bool _isNullable;
            private bool _multiRef;
            private int _fixupIndex = -1;
            private string _paramsReadSource;
            private string _checkSpecifiedSource;

            internal Member(XmlSerializationReaderCodeGen outerClass, string source, string arrayName, int i, MemberMapping mapping)
                : this(outerClass, source, null, arrayName, i, mapping, false, null)
            {
            }
            internal Member(XmlSerializationReaderCodeGen outerClass, string source, string arrayName, int i, MemberMapping mapping, string choiceSource)
                : this(outerClass, source, null, arrayName, i, mapping, false, choiceSource)
            {
            }
            internal Member(XmlSerializationReaderCodeGen outerClass, string source, string arraySource, string arrayName, int i, MemberMapping mapping)
                : this(outerClass, source, arraySource, arrayName, i, mapping, false, null)
            {
            }
            internal Member(XmlSerializationReaderCodeGen outerClass, string source, string arraySource, string arrayName, int i, MemberMapping mapping, string choiceSource)
                : this(outerClass, source, arraySource, arrayName, i, mapping, false, choiceSource)
            {
            }
            internal Member(XmlSerializationReaderCodeGen outerClass, string source, string arrayName, int i, MemberMapping mapping, bool multiRef)
                : this(outerClass, source, null, arrayName, i, mapping, multiRef, null)
            {
            }
            internal Member(XmlSerializationReaderCodeGen outerClass, string source, string arraySource, string arrayName, int i, MemberMapping mapping, bool multiRef, string choiceSource)
            {
                _source = source;
                _arrayName = arrayName + "_" + i.ToString(CultureInfo.InvariantCulture);
                _choiceArrayName = "choice_" + _arrayName;
                _choiceSource = choiceSource;
                ElementAccessor[] elements = mapping.Elements;

                if (mapping.TypeDesc.IsArrayLike)
                {
                    if (arraySource != null)
                        _arraySource = arraySource;
                    else
                        _arraySource = outerClass.GetArraySource(mapping.TypeDesc, _arrayName, multiRef);
                    _isArray = mapping.TypeDesc.IsArray;
                    _isList = !_isArray;
                    if (mapping.ChoiceIdentifier != null)
                    {
                        _choiceArraySource = outerClass.GetArraySource(mapping.TypeDesc, _choiceArrayName, multiRef);

                        string a = _choiceArrayName;
                        string c = "c" + a;
                        bool choiceUseReflection = mapping.ChoiceIdentifier.Mapping.TypeDesc.UseReflection;
                        string choiceTypeFullName = mapping.ChoiceIdentifier.Mapping.TypeDesc.CSharpName;
                        string castString = choiceUseReflection ? "" : "(" + choiceTypeFullName + "[])";

                        string init = a + " = " + castString +
                            "EnsureArrayIndex(" + a + ", " + c + ", " + outerClass.RaCodeGen.GetStringForTypeof(choiceTypeFullName, choiceUseReflection) + ");";
                        _choiceArraySource = init + outerClass.RaCodeGen.GetStringForArrayMember(a, c + "++", mapping.ChoiceIdentifier.Mapping.TypeDesc);
                    }
                    else
                    {
                        _choiceArraySource = _choiceSource;
                    }
                }
                else
                {
                    _arraySource = arraySource == null ? source : arraySource;
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

            internal bool MultiRef
            {
                get { return _multiRef; }
                set { _multiRef = value; }
            }

            internal int FixupIndex
            {
                get { return _fixupIndex; }
                set { _fixupIndex = value; }
            }

            internal string ParamsReadSource
            {
                get { return _paramsReadSource; }
                set { _paramsReadSource = value; }
            }

            internal string CheckSpecifiedSource
            {
                get { return _checkSpecifiedSource; }
                set { _checkSpecifiedSource = value; }
            }

            internal string ChoiceSource
            {
                get { return _choiceSource; }
            }
            internal string ChoiceArrayName
            {
                get { return _choiceArrayName; }
            }
            internal string ChoiceArraySource
            {
                get { return _choiceArraySource; }
            }
        }

        internal XmlSerializationReaderCodeGen(IndentedWriter writer, TypeScope[] scopes, string access, string className) : base(writer, scopes, access, className)
        {
        }

        internal void GenerateBegin()
        {
            Writer.Write(Access);
            Writer.Write(" class ");
            Writer.Write(ClassName);
            Writer.Write(" : ");
            Writer.Write(typeof(System.Xml.Serialization.XmlSerializationReader).FullName);
            Writer.WriteLine(" {");
            Writer.Indent++;
            foreach (TypeScope scope in Scopes)
            {
                foreach (TypeMapping mapping in scope.TypeMappings)
                {
                    if (mapping is StructMapping || mapping is EnumMapping || mapping is NullableMapping)
                        MethodNames.Add(mapping, NextMethodName(mapping.TypeDesc.Name));
                }
                RaCodeGen.WriteReflectionInit(scope);
            }
            // pre-generate read methods only for the encoded soap
            foreach (TypeScope scope in Scopes)
            {
                foreach (TypeMapping mapping in scope.TypeMappings)
                {
                    if (!mapping.IsSoap)
                        continue;
                    if (mapping is StructMapping)
                        WriteStructMethod((StructMapping)mapping);
                    else if (mapping is EnumMapping)
                        WriteEnumMethod((EnumMapping)mapping);
                    else if (mapping is NullableMapping)
                    {
                        WriteNullableMethod((NullableMapping)mapping);
                    }
                }
            }
        }

        internal override void GenerateMethod(TypeMapping mapping)
        {
            if (GeneratedMethods.Contains(mapping))
                return;

            GeneratedMethods[mapping] = mapping;
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

        internal void GenerateEnd()
        {
            GenerateEnd(new string[0], new XmlMapping[0], new Type[0]);
        }
        internal void GenerateEnd(string[] methods, XmlMapping[] xmlMappings, Type[] types)
        {
            GenerateReferencedMethods();
            GenerateInitCallbacksMethod();

            foreach (CreateCollectionInfo c in _createMethods.Values)
            {
                WriteCreateCollectionMethod(c);
            }

            Writer.WriteLine();
            foreach (string idName in _idNames.Values)
            {
                Writer.Write("string ");
                Writer.Write(idName);
                Writer.WriteLine(";");
            }

            Writer.WriteLine();
            Writer.WriteLine("protected override void InitIDs() {");
            Writer.Indent++;
            foreach (string id in _idNames.Keys)
            {
                // CONSIDER, erikc, switch to enumerating via DictionaryEntry when issue recolved in BCL
                string idName = (string)_idNames[id];
                Writer.Write(idName);
                Writer.Write(" = Reader.NameTable.Add(");
                WriteQuotedCSharpString(id);
                Writer.WriteLine(");");
            }
            Writer.Indent--;
            Writer.WriteLine("}");

            Writer.Indent--;
            Writer.WriteLine("}");
        }

        internal string GenerateElement(XmlMapping xmlMapping)
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

        private void WriteIsStartTag(string name, string ns)
        {
            Writer.Write("if (Reader.IsStartElement(");
            WriteID(name);
            Writer.Write(", ");
            WriteID(ns);
            Writer.WriteLine(")) {");
            Writer.Indent++;
        }

        private void WriteUnknownNode(string func, string node, ElementAccessor e, bool anyIfs)
        {
            if (anyIfs)
            {
                Writer.WriteLine("else {");
                Writer.Indent++;
            }
            Writer.Write(func);
            Writer.Write("(");
            Writer.Write(node);
            if (e != null)
            {
                Writer.Write(", ");
                string expectedElement = e.Form == XmlSchemaForm.Qualified ? e.Namespace : "";
                expectedElement += ":";
                expectedElement += e.Name;
                ReflectionAwareCodeGen.WriteQuotedCSharpString(Writer, expectedElement);
            }
            Writer.WriteLine(");");
            if (anyIfs)
            {
                Writer.Indent--;
                Writer.WriteLine("}");
            }
        }

        private void GenerateInitCallbacksMethod()
        {
            Writer.WriteLine();
            Writer.WriteLine("protected override void InitCallbacks() {");
            Writer.Indent++;

            string dummyArrayMethodName = NextMethodName("Array");
            bool needDummyArrayMethod = false;
            foreach (TypeScope scope in Scopes)
            {
                foreach (TypeMapping mapping in scope.TypeMappings)
                {
                    if (mapping.IsSoap &&
                        (mapping is StructMapping || mapping is EnumMapping || mapping is ArrayMapping || mapping is NullableMapping) &&
                        !mapping.TypeDesc.IsRoot)
                    {
                        string methodName;
                        if (mapping is ArrayMapping)
                        {
                            methodName = dummyArrayMethodName;
                            needDummyArrayMethod = true;
                        }
                        else
                            methodName = (string)MethodNames[mapping];

                        Writer.Write("AddReadCallback(");
                        WriteID(mapping.TypeName);
                        Writer.Write(", ");
                        WriteID(mapping.Namespace);
                        Writer.Write(", ");
                        Writer.Write(RaCodeGen.GetStringForTypeof(mapping.TypeDesc.CSharpName, mapping.TypeDesc.UseReflection));
                        Writer.Write(", new ");
                        Writer.Write(typeof(XmlSerializationReadCallback).FullName);
                        Writer.Write("(this.");
                        Writer.Write(methodName);
                        Writer.WriteLine("));");
                    }
                }
            }

            Writer.Indent--;
            Writer.WriteLine("}");

            if (needDummyArrayMethod)
            {
                Writer.WriteLine();
                Writer.Write("object ");
                Writer.Write(dummyArrayMethodName);
                Writer.WriteLine("() {");
                Writer.Indent++;
                Writer.WriteLine("// dummy array method");
                Writer.WriteLine("UnknownNode(null);");
                Writer.WriteLine("return null;");
                Writer.Indent--;
                Writer.WriteLine("}");
            }
        }


        private string GenerateMembersElement(XmlMembersMapping xmlMembersMapping)
        {
            if (xmlMembersMapping.Accessor.IsSoap)
                return GenerateEncodedMembersElement(xmlMembersMapping);
            else
                return GenerateLiteralMembersElement(xmlMembersMapping);
        }

        private string GetChoiceIdentifierSource(MemberMapping[] mappings, MemberMapping member)
        {
            string choiceSource = null;
            if (member.ChoiceIdentifier != null)
            {
                for (int j = 0; j < mappings.Length; j++)
                {
                    if (mappings[j].Name == member.ChoiceIdentifier.MemberName)
                    {
                        choiceSource = "p[" + j.ToString(CultureInfo.InvariantCulture) + "]";
                        break;
                    }
                }
#if DEBUG
                    // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                    if (choiceSource == null) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "Can not find " + member.ChoiceIdentifier.MemberName + " in the members mapping."));
#endif

            }
            return choiceSource;
        }

        private string GetChoiceIdentifierSource(MemberMapping mapping, string parent, TypeDesc parentTypeDesc)
        {
            if (mapping.ChoiceIdentifier == null) return "";
            CodeIdentifier.CheckValidIdentifier(mapping.ChoiceIdentifier.MemberName);
            return RaCodeGen.GetStringForMember(parent, mapping.ChoiceIdentifier.MemberName, parentTypeDesc);
        }

        private string GenerateLiteralMembersElement(XmlMembersMapping xmlMembersMapping)
        {
            ElementAccessor element = xmlMembersMapping.Accessor;
            MemberMapping[] mappings = ((MembersMapping)element.Mapping).Members;
            bool hasWrapperElement = ((MembersMapping)element.Mapping).HasWrapperElement;
            string methodName = NextMethodName(element.Name);
            Writer.WriteLine();
            Writer.Write("public object[] ");
            Writer.Write(methodName);
            Writer.WriteLine("() {");
            Writer.Indent++;
            Writer.WriteLine("Reader.MoveToContent();");

            Writer.Write("object[] p = new object[");
            Writer.Write(mappings.Length.ToString(CultureInfo.InvariantCulture));
            Writer.WriteLine("];");
            InitializeValueTypes("p", mappings);

            int wrapperLoopIndex = 0;
            if (hasWrapperElement)
            {
                wrapperLoopIndex = WriteWhileNotLoopStart();
                Writer.Indent++;
                WriteIsStartTag(element.Name, element.Form == XmlSchemaForm.Qualified ? element.Namespace : "");
            }

            Member anyText = null;
            Member anyElement = null;
            Member anyAttribute = null;

            ArrayList membersList = new ArrayList();
            ArrayList textOrArrayMembersList = new ArrayList();
            ArrayList attributeMembersList = new ArrayList();

            for (int i = 0; i < mappings.Length; i++)
            {
                MemberMapping mapping = mappings[i];
                string source = "p[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                string arraySource = source;
                if (mapping.Xmlns != null)
                {
                    arraySource = "((" + mapping.TypeDesc.CSharpName + ")" + source + ")";
                }
                string choiceSource = GetChoiceIdentifierSource(mappings, mapping);
                Member member = new Member(this, source, arraySource, "a", i, mapping, choiceSource);
                Member anyMember = new Member(this, source, null, "a", i, mapping, choiceSource);
                if (!mapping.IsSequence)
                    member.ParamsReadSource = "paramsRead[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (mapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
                {
                    string nameSpecified = mapping.Name + "Specified";
                    for (int j = 0; j < mappings.Length; j++)
                    {
                        if (mappings[j].Name == nameSpecified)
                        {
                            member.CheckSpecifiedSource = "p[" + j.ToString(CultureInfo.InvariantCulture) + "]";
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
                    for (int j = 0; j < mapping.Elements.Length; j++)
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
                else if (mapping.TypeDesc.IsArrayLike && !(mapping.Elements.Length == 1 && mapping.Elements[0].Mapping is ArrayMapping))
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
            Member[] members = (Member[])membersList.ToArray(typeof(Member));
            Member[] textOrArrayMembers = (Member[])textOrArrayMembersList.ToArray(typeof(Member));

            if (members.Length > 0 && members[0].Mapping.IsReturnValue) Writer.WriteLine("IsReturnValue = true;");

            WriteParamsRead(mappings.Length);

            if (attributeMembersList.Count > 0)
            {
                Member[] attributeMembers = (Member[])attributeMembersList.ToArray(typeof(Member));
                WriteMemberBegin(attributeMembers);
                WriteAttributes(attributeMembers, anyAttribute, "UnknownNode", "(object)p");
                WriteMemberEnd(attributeMembers);
                Writer.WriteLine("Reader.MoveToElement();");
            }

            WriteMemberBegin(textOrArrayMembers);

            if (hasWrapperElement)
            {
                Writer.WriteLine("if (Reader.IsEmptyElement) { Reader.Skip(); Reader.MoveToContent(); continue; }");
                Writer.WriteLine("Reader.ReadStartElement();");
            }
            if (IsSequence(members))
            {
                Writer.WriteLine("int state = 0;");
            }
            int loopIndex = WriteWhileNotLoopStart();
            Writer.Indent++;

            string unknownNode = "UnknownNode((object)p, " + ExpectedElements(members) + ");";
            WriteMemberElements(members, unknownNode, unknownNode, anyElement, anyText, null);

            Writer.WriteLine("Reader.MoveToContent();");
            WriteWhileLoopEnd(loopIndex);

            WriteMemberEnd(textOrArrayMembers);

            if (hasWrapperElement)
            {
                Writer.WriteLine("ReadEndElement();");

                Writer.Indent--;
                Writer.WriteLine("}");

                WriteUnknownNode("UnknownNode", "null", element, true);

                Writer.WriteLine("Reader.MoveToContent();");
                WriteWhileLoopEnd(wrapperLoopIndex);
            }

            Writer.WriteLine("return p;");
            Writer.Indent--;
            Writer.WriteLine("}");

            return methodName;
        }

        private void InitializeValueTypes(string arrayName, MemberMapping[] mappings)
        {
            for (int i = 0; i < mappings.Length; i++)
            {
                if (!mappings[i].TypeDesc.IsValueType)
                    continue;
                Writer.Write(arrayName);
                Writer.Write("[");
                Writer.Write(i.ToString(CultureInfo.InvariantCulture));
                Writer.Write("] = ");

                if (mappings[i].TypeDesc.IsOptionalValue && mappings[i].TypeDesc.BaseTypeDesc.UseReflection)
                {
                    Writer.Write("null");
                }
                else
                {
                    Writer.Write(RaCodeGen.GetStringForCreateInstance(mappings[i].TypeDesc.CSharpName, mappings[i].TypeDesc.UseReflection, false, false));
                }
                Writer.WriteLine(";");
            }
        }

        private string GenerateEncodedMembersElement(XmlMembersMapping xmlMembersMapping)
        {
            ElementAccessor element = xmlMembersMapping.Accessor;
            MembersMapping membersMapping = (MembersMapping)element.Mapping;
            MemberMapping[] mappings = membersMapping.Members;
            bool hasWrapperElement = membersMapping.HasWrapperElement;
            bool writeAccessors = membersMapping.WriteAccessors;
            string methodName = NextMethodName(element.Name);
            Writer.WriteLine();
            Writer.Write("public object[] ");
            Writer.Write(methodName);
            Writer.WriteLine("() {");
            Writer.Indent++;

            Writer.WriteLine("Reader.MoveToContent();");

            Writer.Write("object[] p = new object[");
            Writer.Write(mappings.Length.ToString(CultureInfo.InvariantCulture));
            Writer.WriteLine("];");
            InitializeValueTypes("p", mappings);

            if (hasWrapperElement)
            {
                WriteReadNonRoots();

                if (membersMapping.ValidateRpcWrapperElement)
                {
                    Writer.Write("if (!");
                    WriteXmlNodeEqual("Reader", element.Name, element.Form == XmlSchemaForm.Qualified ? element.Namespace : "");
                    Writer.WriteLine(") throw CreateUnknownNodeException();");
                }
                Writer.WriteLine("bool isEmptyWrapper = Reader.IsEmptyElement;");
                Writer.WriteLine("Reader.ReadStartElement();");
            }

            Member[] members = new Member[mappings.Length];
            for (int i = 0; i < mappings.Length; i++)
            {
                MemberMapping mapping = mappings[i];
                string source = "p[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                string arraySource = source;
                if (mapping.Xmlns != null)
                {
                    arraySource = "((" + mapping.TypeDesc.CSharpName + ")" + source + ")";
                }
                Member member = new Member(this, source, arraySource, "a", i, mapping);
                if (!mapping.IsSequence)
                    member.ParamsReadSource = "paramsRead[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                members[i] = member;

                if (mapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
                {
                    string nameSpecified = mapping.Name + "Specified";
                    for (int j = 0; j < mappings.Length; j++)
                    {
                        if (mappings[j].Name == nameSpecified)
                        {
                            member.CheckSpecifiedSource = "p[" + j.ToString(CultureInfo.InvariantCulture) + "]";
                            break;
                        }
                    }
                }
            }

            string fixupMethodName = "fixup_" + methodName;
            bool anyFixups = WriteMemberFixupBegin(members, fixupMethodName, "p");

            if (members.Length > 0 && members[0].Mapping.IsReturnValue) Writer.WriteLine("IsReturnValue = true;");

            string checkTypeHrefSource = (!hasWrapperElement && !writeAccessors) ? "hrefList" : null;
            if (checkTypeHrefSource != null)
                WriteInitCheckTypeHrefList(checkTypeHrefSource);

            WriteParamsRead(mappings.Length);
            int loopIndex = WriteWhileNotLoopStart();
            Writer.Indent++;

            string unrecognizedElementSource = checkTypeHrefSource == null ? "UnknownNode((object)p);" : "if (Reader.GetAttribute(\"id\", null) != null) { ReadReferencedElement(); } else { UnknownNode((object)p); }";
            WriteMemberElements(members, unrecognizedElementSource, "UnknownNode((object)p);", null, null, checkTypeHrefSource);
            Writer.WriteLine("Reader.MoveToContent();");

            WriteWhileLoopEnd(loopIndex);

            if (hasWrapperElement)
                Writer.WriteLine("if (!isEmptyWrapper) ReadEndElement();");

            if (checkTypeHrefSource != null)
                WriteHandleHrefList(members, checkTypeHrefSource);

            Writer.WriteLine("ReadReferencedElements();");
            Writer.WriteLine("return p;");

            Writer.Indent--;
            Writer.WriteLine("}");

            if (anyFixups) WriteFixupMethod(fixupMethodName, members, "object[]", false, false, "p");

            return methodName;
        }

        private void WriteCreateCollection(TypeDesc td, string source)
        {
            bool useReflection = td.UseReflection;
            string item = (td.ArrayElementTypeDesc == null ? "object" : td.ArrayElementTypeDesc.CSharpName) + "[]";
            bool arrayElementUseReflection = td.ArrayElementTypeDesc == null ? false : td.ArrayElementTypeDesc.UseReflection;

            //cannot call WriteArrayLocalDecl since 'ci' is always
            //array and 'td' corresponds to 'c'
            if (arrayElementUseReflection)
                item = typeof(Array).FullName;
            Writer.Write(item);
            Writer.Write(" ");
            Writer.Write("ci =");
            Writer.Write("(" + item + ")");
            Writer.Write(source);
            Writer.WriteLine(";");

            Writer.WriteLine("for (int i = 0; i < ci.Length; i++) {");
            Writer.Indent++;
            Writer.Write(RaCodeGen.GetStringForMethod("c", td.CSharpName, "Add", useReflection));

            //cannot call GetStringForArrayMember since 'ci' is always
            //array and 'td' corresponds to 'c'
            if (!arrayElementUseReflection)
                Writer.Write("ci[i]");
            else
                Writer.Write(RaCodeGen.GetReflectionVariable(typeof(Array).FullName, "0") + "[ci , i]");


            if (useReflection) Writer.WriteLine("}");
            Writer.WriteLine(");");
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private string GenerateTypeElement(XmlTypeMapping xmlTypeMapping)
        {
            ElementAccessor element = xmlTypeMapping.Accessor;
            TypeMapping mapping = element.Mapping;
            string methodName = NextMethodName(element.Name);
            Writer.WriteLine();
            Writer.Write("public object ");
            Writer.Write(methodName);
            Writer.WriteLine("() {");
            Writer.Indent++;
            Writer.WriteLine("object o = null;");
            MemberMapping member = new MemberMapping();
            member.TypeDesc = mapping.TypeDesc;
            //member.ReadOnly = !mapping.TypeDesc.HasDefaultConstructor;
            member.Elements = new ElementAccessor[] { element };
            Member[] members = new Member[] { new Member(this, "o", "o", "a", 0, member) };
            Writer.WriteLine("Reader.MoveToContent();");
            string unknownNode = "UnknownNode(null, " + ExpectedElements(members) + ");";
            WriteMemberElements(members, "throw CreateUnknownNodeException();", unknownNode, element.Any ? members[0] : null, null, null);
            if (element.IsSoap)
            {
                Writer.WriteLine("Referenced(o);");
                Writer.WriteLine("ReadReferencedElements();");
            }
            Writer.WriteLine("return (object)o;");
            Writer.Indent--;
            Writer.WriteLine("}");
            return methodName;
        }

        private string NextMethodName(string name)
        {
            return "Read" + (++NextMethodNumber).ToString(CultureInfo.InvariantCulture) + "_" + CodeIdentifier.MakeValidInternal(name);
        }

        private string NextIdName(string name)
        {
            return "id" + (++_nextIdNumber).ToString(CultureInfo.InvariantCulture) + "_" + CodeIdentifier.MakeValidInternal(name);
        }

        private void WritePrimitive(TypeMapping mapping, string source)
        {
            if (mapping is EnumMapping)
            {
                string enumMethodName = ReferenceMapping(mapping);
                if (enumMethodName == null) throw new InvalidOperationException(SR.Format(SR.XmlMissingMethodEnum, mapping.TypeDesc.Name));
                if (mapping.IsSoap)
                {
                    // SOAP methods are not strongly-typed (the return object), so we need to add a cast
                    Writer.Write("(");
                    Writer.Write(mapping.TypeDesc.CSharpName);
                    Writer.Write(")");
                }
                Writer.Write(enumMethodName);
                Writer.Write("(");
                if (!mapping.IsSoap) Writer.Write(source);
                Writer.Write(")");
            }
            else if (mapping.TypeDesc == StringTypeDesc)
            {
                Writer.Write(source);
            }
            else if (mapping.TypeDesc.FormatterName == "String")
            {
                if (mapping.TypeDesc.CollapseWhitespace)
                {
                    Writer.Write("CollapseWhitespace(");
                    Writer.Write(source);
                    Writer.Write(")");
                }
                else
                {
                    Writer.Write(source);
                }
            }
            else
            {
                if (!mapping.TypeDesc.HasCustomFormatter)
                {
                    Writer.Write(typeof(XmlConvert).FullName);
                    Writer.Write(".");
                }
                Writer.Write("To");
                Writer.Write(mapping.TypeDesc.FormatterName);
                Writer.Write("(");
                Writer.Write(source);
                Writer.Write(")");
            }
        }

        private string MakeUnique(EnumMapping mapping, string name)
        {
            string uniqueName = name;
            object m = Enums[uniqueName];
            if (m != null)
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
                    m = Enums[uniqueName];
                }
            }
            Enums.Add(uniqueName, mapping);
            return uniqueName;
        }

        private string WriteHashtable(EnumMapping mapping, string typeName)
        {
            CodeIdentifier.CheckValidIdentifier(typeName);
            string propName = MakeUnique(mapping, typeName + "Values");
            if (propName == null) return CodeIdentifier.GetCSharpName(typeName);
            string memberName = MakeUnique(mapping, "_" + propName);
            propName = CodeIdentifier.GetCSharpName(propName);

            Writer.WriteLine();
            Writer.Write(typeof(Hashtable).FullName);
            Writer.Write(" ");
            Writer.Write(memberName);
            Writer.WriteLine(";");
            Writer.WriteLine();

            Writer.Write("internal ");
            Writer.Write(typeof(Hashtable).FullName);
            Writer.Write(" ");
            Writer.Write(propName);
            Writer.WriteLine(" {");
            Writer.Indent++;

            Writer.WriteLine("get {");
            Writer.Indent++;

            Writer.Write("if ((object)");
            Writer.Write(memberName);
            Writer.WriteLine(" == null) {");
            Writer.Indent++;

            Writer.Write(typeof(Hashtable).FullName);
            Writer.Write(" h = new ");
            Writer.Write(typeof(Hashtable).FullName);
            Writer.WriteLine("();");

            ConstantMapping[] constants = mapping.Constants;

            for (int i = 0; i < constants.Length; i++)
            {
                Writer.Write("h.Add(");
                WriteQuotedCSharpString(constants[i].XmlName);
                if (!mapping.TypeDesc.UseReflection)
                {
                    Writer.Write(", (long)");
                    Writer.Write(mapping.TypeDesc.CSharpName);
                    Writer.Write(".@");
                    CodeIdentifier.CheckValidIdentifier(constants[i].Name);
                    Writer.Write(constants[i].Name);
                }
                else
                {
                    Writer.Write(", ");
                    Writer.Write(constants[i].Value.ToString(CultureInfo.InvariantCulture) + "L");
                }

                Writer.WriteLine(");");
            }

            Writer.Write(memberName);
            Writer.WriteLine(" = h;");

            Writer.Indent--;
            Writer.WriteLine("}");

            Writer.Write("return ");
            Writer.Write(memberName);
            Writer.WriteLine(";");

            Writer.Indent--;
            Writer.WriteLine("}");

            Writer.Indent--;
            Writer.WriteLine("}");

            return propName;
        }

        private void WriteEnumMethod(EnumMapping mapping)
        {
            string tableName = null;
            if (mapping.IsFlags)
                tableName = WriteHashtable(mapping, mapping.TypeDesc.Name);

            string methodName = (string)MethodNames[mapping];
            Writer.WriteLine();
            bool useReflection = mapping.TypeDesc.UseReflection;
            string fullTypeName = mapping.TypeDesc.CSharpName;

            if (mapping.IsSoap)
            {
                Writer.Write("object");
                Writer.Write(" ");
                Writer.Write(methodName);
                Writer.WriteLine("() {");
                Writer.Indent++;
                Writer.WriteLine("string s = Reader.ReadElementString();");
            }
            else
            {
                Writer.Write(useReflection ? "object" : fullTypeName);
                Writer.Write(" ");
                Writer.Write(methodName);
                Writer.WriteLine("(string s) {");
                Writer.Indent++;
            }

            ConstantMapping[] constants = mapping.Constants;
            if (mapping.IsFlags)
            {
                if (useReflection)
                {
                    Writer.Write("return ");
                    Writer.Write(typeof(Enum).FullName);
                    Writer.Write(".ToObject(");
                    Writer.Write(RaCodeGen.GetStringForTypeof(fullTypeName, useReflection));
                    Writer.Write(", ToEnum(s, ");
                    Writer.Write(tableName);
                    Writer.Write(", ");
                    WriteQuotedCSharpString(fullTypeName);
                    Writer.WriteLine("));");
                }
                else
                {
                    Writer.Write("return (");
                    Writer.Write(fullTypeName);
                    Writer.Write(")ToEnum(s, ");
                    Writer.Write(tableName);
                    Writer.Write(", ");
                    WriteQuotedCSharpString(fullTypeName);
                    Writer.WriteLine(");");
                }
            }
            else
            {
                Writer.WriteLine("switch (s) {");
                Writer.Indent++;
                Hashtable cases = new Hashtable();
                for (int i = 0; i < constants.Length; i++)
                {
                    ConstantMapping c = constants[i];

                    CodeIdentifier.CheckValidIdentifier(c.Name);
                    if (cases[c.XmlName] == null)
                    {
                        Writer.Write("case ");
                        WriteQuotedCSharpString(c.XmlName);
                        Writer.Write(": return ");
                        Writer.Write(RaCodeGen.GetStringForEnumMember(fullTypeName, c.Name, useReflection));
                        Writer.WriteLine(";");
                        cases[c.XmlName] = c.XmlName;
                    }
                }

                Writer.Write("default: throw CreateUnknownConstantException(s, ");
                Writer.Write(RaCodeGen.GetStringForTypeof(fullTypeName, useReflection));
                Writer.WriteLine(");");
                Writer.Indent--;
                Writer.WriteLine("}");
            }

            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteDerivedTypes(StructMapping mapping, bool isTypedReturn, string returnTypeName)
        {
            for (StructMapping derived = mapping.DerivedMappings; derived != null; derived = derived.NextDerivedMapping)
            {
                Writer.Write("else if (");
                WriteQNameEqual("xsiType", derived.TypeName, derived.Namespace);
                Writer.WriteLine(")");
                Writer.Indent++;

                string methodName = ReferenceMapping(derived);
#if DEBUG
                    // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                    if (methodName == null) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorMethod, derived.TypeDesc.Name));
#endif

                Writer.Write("return ");
                if (derived.TypeDesc.UseReflection && isTypedReturn)
                    Writer.Write("(" + returnTypeName + ")");
                Writer.Write(methodName);
                Writer.Write("(");
                if (derived.TypeDesc.IsNullable)
                    Writer.Write("isNullable, ");
                Writer.WriteLine("false);");

                Writer.Indent--;

                WriteDerivedTypes(derived, isTypedReturn, returnTypeName);
            }
        }

        private void WriteEnumAndArrayTypes()
        {
            foreach (TypeScope scope in Scopes)
            {
                foreach (Mapping m in scope.TypeMappings)
                {
                    if (m.IsSoap)
                        continue;
                    if (m is EnumMapping)
                    {
                        EnumMapping mapping = (EnumMapping)m;
                        Writer.Write("else if (");
                        WriteQNameEqual("xsiType", mapping.TypeName, mapping.Namespace);
                        Writer.WriteLine(") {");
                        Writer.Indent++;
                        Writer.WriteLine("Reader.ReadStartElement();");
                        string methodName = ReferenceMapping(mapping);
#if DEBUG
                            // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                            if (methodName == null) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorMethod, mapping.TypeDesc.Name));
#endif
                        Writer.Write("object e = ");
                        Writer.Write(methodName);
                        Writer.WriteLine("(CollapseWhitespace(Reader.ReadString()));");
                        Writer.WriteLine("ReadEndElement();");
                        Writer.WriteLine("return e;");
                        Writer.Indent--;
                        Writer.WriteLine("}");
                    }
                    else if (m is ArrayMapping)
                    {
                        ArrayMapping mapping = (ArrayMapping)m;
                        if (mapping.TypeDesc.HasDefaultConstructor)
                        {
                            Writer.Write("else if (");
                            WriteQNameEqual("xsiType", mapping.TypeName, mapping.Namespace);
                            Writer.WriteLine(") {");
                            Writer.Indent++;
                            MemberMapping memberMapping = new MemberMapping();
                            memberMapping.TypeDesc = mapping.TypeDesc;
                            memberMapping.Elements = mapping.Elements;
                            Member member = new Member(this, "a", "z", 0, memberMapping);

                            TypeDesc td = mapping.TypeDesc;
                            string fullTypeName = mapping.TypeDesc.CSharpName;
                            if (td.UseReflection)
                            {
                                if (td.IsArray)
                                    Writer.Write(typeof(Array).FullName);
                                else
                                    Writer.Write("object");
                            }
                            else
                                Writer.Write(fullTypeName);
                            Writer.Write(" a = ");
                            if (mapping.TypeDesc.IsValueType)
                            {
                                Writer.Write(RaCodeGen.GetStringForCreateInstance(fullTypeName, td.UseReflection, false, false));
                                Writer.WriteLine(";");
                            }
                            else
                                Writer.WriteLine("null;");

                            WriteArray(member.Source, member.ArrayName, mapping, false, false, -1);
                            Writer.WriteLine("return a;");
                            Writer.Indent--;
                            Writer.WriteLine("}");
                        }
                    }
                }
            }
        }

        private void WriteNullableMethod(NullableMapping nullableMapping)
        {
            string methodName = (string)MethodNames[nullableMapping];
            bool useReflection = nullableMapping.BaseMapping.TypeDesc.UseReflection;
            string typeName = useReflection ? "object" : nullableMapping.TypeDesc.CSharpName;
            Writer.WriteLine();

            Writer.Write(typeName);
            Writer.Write(" ");
            Writer.Write(methodName);
            Writer.WriteLine("(bool checkType) {");
            Writer.Indent++;

            Writer.Write(typeName);
            Writer.Write(" o = ");

            if (useReflection)
            {
                Writer.Write("null");
            }
            else
            {
                Writer.Write("default(");
                Writer.Write(typeName);
                Writer.Write(")");
            }
            Writer.WriteLine(";");

            Writer.WriteLine("if (ReadNull())");
            Writer.Indent++;

            Writer.WriteLine("return o;");
            Writer.Indent--;

            ElementAccessor element = new ElementAccessor();
            element.Mapping = nullableMapping.BaseMapping;
            element.Any = false;
            element.IsNullable = nullableMapping.BaseMapping.TypeDesc.IsNullable;

            WriteElement("o", null, null, element, null, null, false, false, -1, -1);
            Writer.WriteLine("return o;");

            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteStructMethod(StructMapping structMapping)
        {
            if (structMapping.IsSoap)
                WriteEncodedStructMethod(structMapping);
            else
                WriteLiteralStructMethod(structMapping);
        }

        private void WriteLiteralStructMethod(StructMapping structMapping)
        {
            string methodName = (string)MethodNames[structMapping];
            bool useReflection = structMapping.TypeDesc.UseReflection;
            string typeName = useReflection ? "object" : structMapping.TypeDesc.CSharpName;
            Writer.WriteLine();
            Writer.Write(typeName);
            Writer.Write(" ");
            Writer.Write(methodName);
            Writer.Write("(");
            if (structMapping.TypeDesc.IsNullable)
                Writer.Write("bool isNullable, ");
            Writer.WriteLine("bool checkType) {");
            Writer.Indent++;

            Writer.Write(typeof(XmlQualifiedName).FullName);
            Writer.WriteLine(" xsiType = checkType ? GetXsiType() : null;");
            Writer.WriteLine("bool isNull = false;");
            if (structMapping.TypeDesc.IsNullable)
                Writer.WriteLine("if (isNullable) isNull = ReadNull();");

            Writer.WriteLine("if (checkType) {");
            if (structMapping.TypeDesc.IsRoot)
            {
                Writer.Indent++;
                Writer.WriteLine("if (isNull) {");
                Writer.Indent++;
                Writer.WriteLine("if (xsiType != null) return (" + typeName + ")ReadTypedNull(xsiType);");
                Writer.Write("else return ");
                if (structMapping.TypeDesc.IsValueType)
                {
                    Writer.Write(RaCodeGen.GetStringForCreateInstance(structMapping.TypeDesc.CSharpName, useReflection, false, false));
                    Writer.WriteLine(";");
                }
                else
                    Writer.WriteLine("null;");

                Writer.Indent--;
                Writer.WriteLine("}");
            }
            Writer.Write("if (xsiType == null");
            if (!structMapping.TypeDesc.IsRoot)
            {
                Writer.Write(" || ");
                WriteQNameEqual("xsiType", structMapping.TypeName, structMapping.Namespace);
            }
            Writer.WriteLine(") {");
            if (structMapping.TypeDesc.IsRoot)
            {
                Writer.Indent++;
                Writer.WriteLine("return ReadTypedPrimitive(new System.Xml.XmlQualifiedName(\"" + Soap.UrType + "\", \"" + XmlSchema.Namespace + "\"));");
                Writer.Indent--;
            }
            Writer.WriteLine("}");
            WriteDerivedTypes(structMapping, !useReflection && !structMapping.TypeDesc.IsRoot, typeName);
            if (structMapping.TypeDesc.IsRoot) WriteEnumAndArrayTypes();
            Writer.WriteLine("else");
            Writer.Indent++;
            if (structMapping.TypeDesc.IsRoot)
                Writer.Write("return ReadTypedPrimitive((");
            else
                Writer.Write("throw CreateUnknownTypeException((");
            Writer.Write(typeof(XmlQualifiedName).FullName);
            Writer.WriteLine(")xsiType);");
            Writer.Indent--;
            Writer.WriteLine("}");

            if (structMapping.TypeDesc.IsNullable)
                Writer.WriteLine("if (isNull) return null;");

            if (structMapping.TypeDesc.IsAbstract)
            {
                Writer.Write("throw CreateAbstractTypeException(");
                WriteQuotedCSharpString(structMapping.TypeName);
                Writer.Write(", ");
                WriteQuotedCSharpString(structMapping.Namespace);
                Writer.WriteLine(");");
            }
            else
            {
                if (structMapping.TypeDesc.Type != null && typeof(XmlSchemaObject).IsAssignableFrom(structMapping.TypeDesc.Type))
                {
                    Writer.WriteLine("DecodeName = false;");
                }
                WriteCreateMapping(structMapping, "o");

                MemberMapping[] mappings = TypeScope.GetSettableMembers(structMapping);

                Member anyText = null;
                Member anyElement = null;
                Member anyAttribute = null;
                bool isSequence = structMapping.HasExplicitSequence();

                ArrayList arraysToDeclareList = new ArrayList(mappings.Length);
                ArrayList arraysToSetList = new ArrayList(mappings.Length);
                ArrayList allMembersList = new ArrayList(mappings.Length);

                for (int i = 0; i < mappings.Length; i++)
                {
                    MemberMapping mapping = mappings[i];
                    CodeIdentifier.CheckValidIdentifier(mapping.Name);
                    string source = RaCodeGen.GetStringForMember("o", mapping.Name, structMapping.TypeDesc);
                    Member member = new Member(this, source, "a", i, mapping, GetChoiceIdentifierSource(mapping, "o", structMapping.TypeDesc));
                    if (!mapping.IsSequence)
                        member.ParamsReadSource = "paramsRead[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                    member.IsNullable = mapping.TypeDesc.IsNullable;
                    if (mapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
                        member.CheckSpecifiedSource = RaCodeGen.GetStringForMember("o", mapping.Name + "Specified", structMapping.TypeDesc);
                    if (mapping.Text != null)
                        anyText = member;
                    if (mapping.Attribute != null && mapping.Attribute.Any)
                        anyAttribute = member;
                    if (!isSequence)
                    {
                        // find anyElement if present.
                        for (int j = 0; j < mapping.Elements.Length; j++)
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
                        StructMapping declaringMapping;
                        structMapping.FindDeclaringMapping(mapping, out declaringMapping, structMapping.TypeName);
                        throw new InvalidOperationException(SR.Format(SR.XmlSequenceHierarchy, structMapping.TypeDesc.FullName, mapping.Name, declaringMapping.TypeDesc.FullName, "Order"));
                    }
                    if (mapping.Attribute == null && mapping.Elements.Length == 1 && mapping.Elements[0].Mapping is ArrayMapping)
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
                        if (mapping.TypeDesc.IsArrayLike && !(mapping.Elements.Length == 1 && mapping.Elements[0].Mapping is ArrayMapping))
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

                Member[] arraysToDeclare = (Member[])arraysToDeclareList.ToArray(typeof(Member));
                Member[] arraysToSet = (Member[])arraysToSetList.ToArray(typeof(Member));
                Member[] allMembers = (Member[])allMembersList.ToArray(typeof(Member));

                WriteMemberBegin(arraysToDeclare);
                WriteParamsRead(mappings.Length);

                WriteAttributes(allMembers, anyAttribute, "UnknownNode", "(object)o");
                if (anyAttribute != null)
                    WriteMemberEnd(arraysToDeclare);

                Writer.WriteLine("Reader.MoveToElement();");

                Writer.WriteLine("if (Reader.IsEmptyElement) {");
                Writer.Indent++;
                Writer.WriteLine("Reader.Skip();");
                WriteMemberEnd(arraysToSet);
                Writer.WriteLine("return o;");
                Writer.Indent--;
                Writer.WriteLine("}");

                Writer.WriteLine("Reader.ReadStartElement();");
                if (IsSequence(allMembers))
                {
                    Writer.WriteLine("int state = 0;");
                }
                int loopIndex = WriteWhileNotLoopStart();
                Writer.Indent++;
                string unknownNode = "UnknownNode((object)o, " + ExpectedElements(allMembers) + ");";
                WriteMemberElements(allMembers, unknownNode, unknownNode, anyElement, anyText, null);
                Writer.WriteLine("Reader.MoveToContent();");

                WriteWhileLoopEnd(loopIndex);
                WriteMemberEnd(arraysToSet);

                Writer.WriteLine("ReadEndElement();");
                Writer.WriteLine("return o;");
            }
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteEncodedStructMethod(StructMapping structMapping)
        {
            if (structMapping.TypeDesc.IsRoot)
                return;
            bool useReflection = structMapping.TypeDesc.UseReflection;
            string methodName = (string)MethodNames[structMapping];
            Writer.WriteLine();
            Writer.Write("object");
            Writer.Write(" ");
            Writer.Write(methodName);
            Writer.Write("(");
            Writer.WriteLine(") {");
            Writer.Indent++;

            Member[] members;
            bool anyFixups;
            string fixupMethodName;

            if (structMapping.TypeDesc.IsAbstract)
            {
                Writer.Write("throw CreateAbstractTypeException(");
                WriteQuotedCSharpString(structMapping.TypeName);
                Writer.Write(", ");
                WriteQuotedCSharpString(structMapping.Namespace);
                Writer.WriteLine(");");
                members = new Member[0];
                anyFixups = false;
                fixupMethodName = null;
            }
            else
            {
                WriteCreateMapping(structMapping, "o");

                MemberMapping[] mappings = TypeScope.GetSettableMembers(structMapping);
                members = new Member[mappings.Length];
                for (int i = 0; i < mappings.Length; i++)
                {
                    MemberMapping mapping = mappings[i];
                    CodeIdentifier.CheckValidIdentifier(mapping.Name);
                    string source = RaCodeGen.GetStringForMember("o", mapping.Name, structMapping.TypeDesc);
                    Member member = new Member(this, source, source, "a", i, mapping, GetChoiceIdentifierSource(mapping, "o", structMapping.TypeDesc));
                    if (mapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
                        member.CheckSpecifiedSource = RaCodeGen.GetStringForMember("o", mapping.Name + "Specified", structMapping.TypeDesc);
                    if (!mapping.IsSequence)
                        member.ParamsReadSource = "paramsRead[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                    members[i] = member;
                }

                fixupMethodName = "fixup_" + methodName;
                anyFixups = WriteMemberFixupBegin(members, fixupMethodName, "o");

                // we're able to not do WriteMemberBegin here because we don't allow arrays as attributes

                WriteParamsRead(mappings.Length);
                WriteAttributes(members, null, "UnknownNode", "(object)o");
                Writer.WriteLine("Reader.MoveToElement();");

                Writer.WriteLine("if (Reader.IsEmptyElement) { Reader.Skip(); return o; }");
                Writer.WriteLine("Reader.ReadStartElement();");

                int loopIndex = WriteWhileNotLoopStart();
                Writer.Indent++;

                WriteMemberElements(members, "UnknownNode((object)o);", "UnknownNode((object)o);", null, null, null);
                Writer.WriteLine("Reader.MoveToContent();");

                WriteWhileLoopEnd(loopIndex);

                Writer.WriteLine("ReadEndElement();");
                Writer.WriteLine("return o;");
            }
            Writer.Indent--;
            Writer.WriteLine("}");

            if (anyFixups) WriteFixupMethod(fixupMethodName, members, structMapping.TypeDesc.CSharpName, structMapping.TypeDesc.UseReflection, true, "o");
        }

        private void WriteFixupMethod(string fixupMethodName, Member[] members, string typeName, bool useReflection, bool typed, string source)
        {
            Writer.WriteLine();
            Writer.Write("void ");
            Writer.Write(fixupMethodName);
            Writer.WriteLine("(object objFixup) {");
            Writer.Indent++;
            Writer.WriteLine("Fixup fixup = (Fixup)objFixup;");
            WriteLocalDecl(typeName, source, "fixup.Source", useReflection);
            Writer.WriteLine("string[] ids = fixup.Ids;");

            for (int i = 0; i < members.Length; i++)
            {
                Member member = members[i];
                if (member.MultiRef)
                {
                    string fixupIndex = member.FixupIndex.ToString(CultureInfo.InvariantCulture);
                    Writer.Write("if (ids[");
                    Writer.Write(fixupIndex);
                    Writer.WriteLine("] != null) {");
                    Writer.Indent++;

                    string memberSource = /*member.IsList ? source + ".Add(" :*/ member.ArraySource;

                    string targetSource = "GetTarget(ids[" + fixupIndex + "])";
                    TypeDesc td = member.Mapping.TypeDesc;
                    if (td.IsCollection || td.IsEnumerable)
                    {
                        WriteAddCollectionFixup(td, member.Mapping.ReadOnly, memberSource, targetSource);
                    }
                    else
                    {
                        if (typed)
                        {
                            Writer.WriteLine("try {");
                            Writer.Indent++;
                            WriteSourceBeginTyped(memberSource, member.Mapping.TypeDesc);
                        }
                        else
                            WriteSourceBegin(memberSource);

                        Writer.Write(targetSource);
                        WriteSourceEnd(memberSource);
                        Writer.WriteLine(";");

                        if (member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite && member.CheckSpecifiedSource != null && member.CheckSpecifiedSource.Length > 0)
                        {
                            Writer.Write(member.CheckSpecifiedSource);
                            Writer.WriteLine(" = true;");
                        }

                        if (typed)
                        {
                            WriteCatchCastException(member.Mapping.TypeDesc, targetSource, "ids[" + fixupIndex + "]");
                        }
                    }
                    Writer.Indent--;
                    Writer.WriteLine("}");
                }
            }
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteAddCollectionFixup(TypeDesc typeDesc, bool readOnly, string memberSource, string targetSource)
        {
            Writer.WriteLine("// get array of the collection items");
            bool useReflection = typeDesc.UseReflection;
            CreateCollectionInfo create = (CreateCollectionInfo)_createMethods[typeDesc];
            if (create == null)
            {
                string createName = "create" + (++_nextCreateMethodNumber).ToString(CultureInfo.InvariantCulture) + "_" + typeDesc.Name;
                create = new CreateCollectionInfo(createName, typeDesc);
                _createMethods.Add(typeDesc, create);
            }

            Writer.Write("if ((object)(");
            Writer.Write(memberSource);
            Writer.WriteLine(") == null) {");
            Writer.Indent++;

            if (readOnly)
            {
                Writer.Write("throw CreateReadOnlyCollectionException(");
                WriteQuotedCSharpString(typeDesc.CSharpName);
                Writer.WriteLine(");");
            }
            else
            {
                Writer.Write(memberSource);
                Writer.Write(" = ");
                Writer.Write(RaCodeGen.GetStringForCreateInstance(typeDesc.CSharpName, typeDesc.UseReflection, typeDesc.CannotNew, true));
                Writer.WriteLine(";");
            }

            Writer.Indent--;
            Writer.WriteLine("}");

            Writer.Write("CollectionFixup collectionFixup = new CollectionFixup(");
            Writer.Write(memberSource);
            Writer.Write(", ");
            Writer.Write("new ");
            Writer.Write(typeof(XmlSerializationCollectionFixupCallback).FullName);
            Writer.Write("(this.");
            Writer.Write(create.Name);
            Writer.Write("), ");
            Writer.Write(targetSource);
            Writer.WriteLine(");");
            Writer.WriteLine("AddFixup(collectionFixup);");
        }

        private void WriteCreateCollectionMethod(CreateCollectionInfo c)
        {
            Writer.Write("void ");
            Writer.Write(c.Name);
            Writer.WriteLine("(object collection, object collectionItems) {");
            Writer.Indent++;

            Writer.WriteLine("if (collectionItems == null) return;");
            Writer.WriteLine("if (collection == null) return;");

            TypeDesc td = c.TypeDesc;
            bool useReflection = td.UseReflection;
            string fullTypeName = td.CSharpName;
            WriteLocalDecl(fullTypeName, nameof(c), "collection", useReflection);

            WriteCreateCollection(td, "collectionItems");

            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteQNameEqual(string source, string name, string ns)
        {
            Writer.Write("((object) ((");
            Writer.Write(typeof(XmlQualifiedName).FullName);
            Writer.Write(")");
            Writer.Write(source);
            Writer.Write(").Name == (object)");
            WriteID(name);
            Writer.Write(" && (object) ((");
            Writer.Write(typeof(XmlQualifiedName).FullName);
            Writer.Write(")");
            Writer.Write(source);
            Writer.Write(").Namespace == (object)");
            WriteID(ns);
            Writer.Write(")");
        }

        private void WriteXmlNodeEqual(string source, string name, string ns)
        {
            Writer.Write("(");
            if (name != null && name.Length > 0)
            {
                Writer.Write("(object) ");
                Writer.Write(source);
                Writer.Write(".LocalName == (object)");
                WriteID(name);
                Writer.Write(" && ");
            }
            Writer.Write("(object) ");
            Writer.Write(source);
            Writer.Write(".NamespaceURI == (object)");
            WriteID(ns);
            Writer.Write(")");
        }

        private void WriteID(string name)
        {
            if (name == null)
            {
                //Writer.Write("null");
                //return;
                name = "";
            }
            string idName = (string)_idNames[name];
            if (idName == null)
            {
                idName = NextIdName(name);
                _idNames.Add(name, idName);
            }
            Writer.Write(idName);
        }

        private void WriteAttributes(Member[] members, Member anyAttribute, string elseCall, string firstParam)
        {
            int count = 0;
            Member xmlnsMember = null;
            ArrayList attributes = new ArrayList();

            Writer.WriteLine("while (Reader.MoveToNextAttribute()) {");
            Writer.Indent++;

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
                AttributeAccessor attribute = member.Mapping.Attribute;

                if (attribute == null) continue;
                if (attribute.Any) continue;

                attributes.Add(attribute);

                if (count++ > 0)
                    Writer.Write("else ");

                Writer.Write("if (");
                if (member.ParamsReadSource != null)
                {
                    Writer.Write("!");
                    Writer.Write(member.ParamsReadSource);
                    Writer.Write(" && ");
                }

                if (attribute.IsSpecialXmlNamespace)
                {
                    WriteXmlNodeEqual("Reader", attribute.Name, XmlReservedNs.NsXml);
                }
                else
                    WriteXmlNodeEqual("Reader", attribute.Name, attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : "");
                Writer.WriteLine(") {");
                Writer.Indent++;

                WriteAttribute(member);
                Writer.Indent--;
                Writer.WriteLine("}");
            }

            if (count > 0)
                Writer.Write("else ");

            if (xmlnsMember != null)
            {
                Writer.WriteLine("if (IsXmlnsAttribute(Reader.Name)) {");
                Writer.Indent++;

                Writer.Write("if (");
                Writer.Write(xmlnsMember.Source);
                Writer.Write(" == null) ");
                Writer.Write(xmlnsMember.Source);
                Writer.Write(" = new ");
                Writer.Write(xmlnsMember.Mapping.TypeDesc.CSharpName);
                Writer.WriteLine("();");

                //Writer.Write(xmlnsMember.ArraySource);
                Writer.Write("((" + xmlnsMember.Mapping.TypeDesc.CSharpName + ")" + xmlnsMember.ArraySource + ")");
                Writer.WriteLine(".Add(Reader.Name.Length == 5 ? \"\" : Reader.LocalName, Reader.Value);");

                Writer.Indent--;
                Writer.WriteLine("}");

                Writer.WriteLine("else {");
                Writer.Indent++;
            }
            else
            {
                Writer.WriteLine("if (!IsXmlnsAttribute(Reader.Name)) {");
                Writer.Indent++;
            }
            if (anyAttribute != null)
            {
                Writer.Write(typeof(XmlAttribute).FullName);
                Writer.Write(" attr = ");
                Writer.Write("(");
                Writer.Write(typeof(XmlAttribute).FullName);
                Writer.WriteLine(") Document.ReadNode(Reader);");
                Writer.WriteLine("ParseWsdlArrayType(attr);");
                WriteAttribute(anyAttribute);
            }
            else
            {
                Writer.Write(elseCall);
                Writer.Write("(");
                Writer.Write(firstParam);
                if (attributes.Count > 0)
                {
                    Writer.Write(", ");
                    string qnames = "";

                    for (int i = 0; i < attributes.Count; i++)
                    {
                        AttributeAccessor attribute = (AttributeAccessor)attributes[i];
                        if (i > 0)
                            qnames += ", ";
                        qnames += attribute.IsSpecialXmlNamespace ? XmlReservedNs.NsXml : (attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : "") + ":" + attribute.Name;
                    }
                    WriteQuotedCSharpString(qnames);
                }
                Writer.WriteLine(");");
            }
            Writer.Indent--;
            Writer.WriteLine("}");

            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteAttribute(Member member)
        {
            AttributeAccessor attribute = member.Mapping.Attribute;

            if (attribute.Mapping is SpecialMapping)
            {
                SpecialMapping special = (SpecialMapping)attribute.Mapping;

                if (special.TypeDesc.Kind == TypeKind.Attribute)
                {
                    WriteSourceBegin(member.ArraySource);
                    Writer.Write("attr");
                    WriteSourceEnd(member.ArraySource);
                    Writer.WriteLine(";");
                }
                else if (special.TypeDesc.CanBeAttributeValue)
                {
                    Writer.Write("if (attr is ");
                    Writer.Write(typeof(XmlAttribute).FullName);
                    Writer.WriteLine(") {");
                    Writer.Indent++;
                    WriteSourceBegin(member.ArraySource);
                    Writer.Write("(");
                    Writer.Write(typeof(XmlAttribute).FullName);
                    Writer.Write(")attr");
                    WriteSourceEnd(member.ArraySource);
                    Writer.WriteLine(";");
                    Writer.Indent--;
                    Writer.WriteLine("}");
                }
                else
                    throw new InvalidOperationException(SR.XmlInternalError);
            }
            else
            {
                if (attribute.IsList)
                {
                    Writer.WriteLine("string listValues = Reader.Value;");
                    Writer.WriteLine("string[] vals = listValues.Split(null);");
                    Writer.WriteLine("for (int i = 0; i < vals.Length; i++) {");
                    Writer.Indent++;

                    string attributeSource = GetArraySource(member.Mapping.TypeDesc, member.ArrayName);

                    WriteSourceBegin(attributeSource);
                    WritePrimitive(attribute.Mapping, "vals[i]");
                    WriteSourceEnd(attributeSource);
                    Writer.WriteLine(";");
                    Writer.Indent--;
                    Writer.WriteLine("}");
                }
                else
                {
                    WriteSourceBegin(member.ArraySource);
                    WritePrimitive(attribute.Mapping, attribute.IsList ? "vals[i]" : "Reader.Value");
                    WriteSourceEnd(member.ArraySource);
                    Writer.WriteLine(";");
                }
            }
            if (member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite && member.CheckSpecifiedSource != null && member.CheckSpecifiedSource.Length > 0)
            {
                Writer.Write(member.CheckSpecifiedSource);
                Writer.WriteLine(" = true;");
            }
            if (member.ParamsReadSource != null)
            {
                Writer.Write(member.ParamsReadSource);
                Writer.WriteLine(" = true;");
            }
        }

        private bool WriteMemberFixupBegin(Member[] members, string fixupMethodName, string source)
        {
            int fixupCount = 0;
            for (int i = 0; i < members.Length; i++)
            {
                Member member = (Member)members[i];
                if (member.Mapping.Elements.Length == 0)
                    continue;

                TypeMapping mapping = member.Mapping.Elements[0].Mapping;
                if (mapping is StructMapping || mapping is ArrayMapping || mapping is PrimitiveMapping || mapping is NullableMapping)
                {
                    member.MultiRef = true;
                    member.FixupIndex = fixupCount++;
                }
            }

            if (fixupCount > 0)
            {
                Writer.Write("Fixup fixup = new Fixup(");
                Writer.Write(source);
                Writer.Write(", ");
                Writer.Write("new ");
                Writer.Write(typeof(XmlSerializationFixupCallback).FullName);
                Writer.Write("(this.");
                Writer.Write(fixupMethodName);
                Writer.Write("), ");
                Writer.Write(fixupCount.ToString(CultureInfo.InvariantCulture));
                Writer.WriteLine(");");
                Writer.WriteLine("AddFixup(fixup);");
                return true;
            }
            return false;
        }

        private void WriteMemberBegin(Member[] members)
        {
            for (int i = 0; i < members.Length; i++)
            {
                Member member = (Member)members[i];

                if (member.IsArrayLike)
                {
                    string a = member.ArrayName;
                    string c = "c" + a;

                    TypeDesc typeDesc = member.Mapping.TypeDesc;
                    string typeDescFullName = typeDesc.CSharpName;

                    if (member.Mapping.TypeDesc.IsArray)
                    {
                        WriteArrayLocalDecl(typeDesc.CSharpName,
                                            a, "null", typeDesc);
                        Writer.Write("int ");
                        Writer.Write(c);
                        Writer.WriteLine(" = 0;");

                        if (member.Mapping.ChoiceIdentifier != null)
                        {
                            WriteArrayLocalDecl(member.Mapping.ChoiceIdentifier.Mapping.TypeDesc.CSharpName + "[]",
                                                member.ChoiceArrayName, "null",
                                                member.Mapping.ChoiceIdentifier.Mapping.TypeDesc);
                            Writer.Write("int c");
                            Writer.Write(member.ChoiceArrayName);
                            Writer.WriteLine(" = 0;");
                        }
                    }
                    else
                    {
                        bool useReflection = typeDesc.UseReflection;
                        if (member.Source[member.Source.Length - 1] == '(' || member.Source[member.Source.Length - 1] == '{')
                        {
                            WriteCreateInstance(typeDescFullName, a, useReflection, typeDesc.CannotNew);
                            Writer.Write(member.Source);
                            Writer.Write(a);
                            if (member.Source[member.Source.Length - 1] == '{')
                                Writer.WriteLine("});");
                            else
                                Writer.WriteLine(");");
                        }
                        else
                        {
                            if (member.IsList && !member.Mapping.ReadOnly && member.Mapping.TypeDesc.IsNullable)
                            {
                                // we need to new the Collections and ArrayLists
                                Writer.Write("if ((object)(");
                                Writer.Write(member.Source);
                                Writer.Write(") == null) ");
                                if (!member.Mapping.TypeDesc.HasDefaultConstructor)
                                {
                                    Writer.Write("throw CreateReadOnlyCollectionException(");
                                    WriteQuotedCSharpString(member.Mapping.TypeDesc.CSharpName);
                                    Writer.WriteLine(");");
                                }
                                else
                                {
                                    Writer.Write(member.Source);
                                    Writer.Write(" = ");
                                    Writer.Write(RaCodeGen.GetStringForCreateInstance(typeDescFullName, useReflection, typeDesc.CannotNew, true));
                                    Writer.WriteLine(";");
                                }
                            }
                            WriteLocalDecl(typeDescFullName, a, member.Source, useReflection);
                        }
                    }
                }
            }
        }

        private string ExpectedElements(Member[] members)
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

                ElementAccessor[] elements = member.Mapping.Elements;

                for (int j = 0; j < elements.Length; j++)
                {
                    ElementAccessor e = elements[j];
                    string ns = e.Form == XmlSchemaForm.Qualified ? e.Namespace : "";
                    if (e.Any && (e.Name == null || e.Name.Length == 0)) continue;

                    if (!firstElement)
                        qnames += ", ";
                    qnames += ns + ":" + e.Name;
                    firstElement = false;
                }
            }
            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
            ReflectionAwareCodeGen.WriteQuotedCSharpString(new IndentedWriter(writer, true), qnames);
            return writer.ToString();
        }

        private void WriteMemberElements(Member[] members, string elementElseString, string elseString, Member anyElement, Member anyText, string checkTypeHrefsSource)
        {
            bool checkType = (checkTypeHrefsSource != null && checkTypeHrefsSource.Length > 0);

            if (anyText != null)
            {
                Writer.WriteLine("string tmp = null;");
            }

            Writer.Write("if (Reader.NodeType == ");
            Writer.Write(typeof(XmlNodeType).FullName);
            Writer.WriteLine(".Element) {");
            Writer.Indent++;

            if (checkType)
            {
                WriteIfNotSoapRoot(elementElseString + " continue;");
                WriteMemberElementsCheckType(checkTypeHrefsSource);
            }
            else
            {
                WriteMemberElementsIf(members, anyElement, elementElseString, null);
            }

            Writer.Indent--;
            Writer.WriteLine("}");

            if (anyText != null)
                WriteMemberText(anyText, elseString);

            Writer.WriteLine("else {");
            Writer.Indent++;
            Writer.WriteLine(elseString);
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteMemberText(Member anyText, string elseString)
        {
            Writer.Write("else if (Reader.NodeType == ");
            Writer.Write(typeof(XmlNodeType).FullName);
            Writer.WriteLine(".Text || ");
            Writer.Write("Reader.NodeType == ");
            Writer.Write(typeof(XmlNodeType).FullName);
            Writer.WriteLine(".CDATA || ");
            Writer.Write("Reader.NodeType == ");
            Writer.Write(typeof(XmlNodeType).FullName);
            Writer.WriteLine(".Whitespace || ");
            Writer.Write("Reader.NodeType == ");
            Writer.Write(typeof(XmlNodeType).FullName);
            Writer.WriteLine(".SignificantWhitespace) {");
            Writer.Indent++;

            if (anyText != null)
            {
                WriteText(anyText);
            }
            else
            {
                Writer.Write(elseString);
                Writer.WriteLine(";");
            }
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteText(Member member)
        {
            TextAccessor text = member.Mapping.Text;

            if (text.Mapping is SpecialMapping)
            {
                SpecialMapping special = (SpecialMapping)text.Mapping;
                WriteSourceBeginTyped(member.ArraySource, special.TypeDesc);
                switch (special.TypeDesc.Kind)
                {
                    case TypeKind.Node:
                        Writer.Write("Document.CreateTextNode(Reader.ReadString())");
                        break;
                    default:
                        throw new InvalidOperationException(SR.XmlInternalError);
                }
                WriteSourceEnd(member.ArraySource);
            }
            else
            {
                if (member.IsArrayLike)
                {
                    WriteSourceBegin(member.ArraySource);
                    if (text.Mapping.TypeDesc.CollapseWhitespace)
                    {
                        Writer.Write("CollapseWhitespace(Reader.ReadString())");
                    }
                    else
                    {
                        Writer.Write("Reader.ReadString()");
                    }
                }
                else
                {
                    if (text.Mapping.TypeDesc == StringTypeDesc || text.Mapping.TypeDesc.FormatterName == "String")
                    {
                        Writer.Write("tmp = ReadString(tmp, ");
                        if (text.Mapping.TypeDesc.CollapseWhitespace)
                            Writer.WriteLine("true);");
                        else
                            Writer.WriteLine("false);");

                        WriteSourceBegin(member.ArraySource);
                        Writer.Write("tmp");
                    }
                    else
                    {
                        WriteSourceBegin(member.ArraySource);
                        WritePrimitive(text.Mapping, "Reader.ReadString()");
                    }
                }
                WriteSourceEnd(member.ArraySource);
            }

            Writer.WriteLine(";");
        }

        private void WriteMemberElementsCheckType(string checkTypeHrefsSource)
        {
            Writer.WriteLine("string refElemId = null;");
            Writer.WriteLine("object refElem = ReadReferencingElement(null, null, true, out refElemId);");

            Writer.WriteLine("if (refElemId != null) {");
            Writer.Indent++;
            Writer.Write(checkTypeHrefsSource);
            Writer.WriteLine(".Add(refElemId);");
            Writer.Write(checkTypeHrefsSource);
            Writer.WriteLine("IsObject.Add(false);");
            Writer.Indent--;
            Writer.WriteLine("}");
            Writer.WriteLine("else if (refElem != null) {");
            Writer.Indent++;
            Writer.Write(checkTypeHrefsSource);
            Writer.WriteLine(".Add(refElem);");
            Writer.Write(checkTypeHrefsSource);
            Writer.WriteLine("IsObject.Add(true);");
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteMemberElementsElse(Member anyElement, string elementElseString)
        {
            if (anyElement != null)
            {
                ElementAccessor[] elements = anyElement.Mapping.Elements;
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
                Writer.WriteLine(elementElseString);
            }
        }

        private bool IsSequence(Member[] members)
        {
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i].Mapping.IsParticle && members[i].Mapping.IsSequence)
                    return true;
            }
            return false;
        }
        private void WriteMemberElementsIf(Member[] members, Member anyElement, string elementElseString, string checkTypeSource)
        {
            bool checkType = checkTypeSource != null && checkTypeSource.Length > 0;
            //int count = checkType ? 1 : 0;
            int count = 0;

            bool isSequence = IsSequence(members);
            if (isSequence)
            {
                Writer.WriteLine("switch (state) {");
            }
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
                ChoiceIdentifierAccessor choice = member.Mapping.ChoiceIdentifier;
                ElementAccessor[] elements = member.Mapping.Elements;

                for (int j = 0; j < elements.Length; j++)
                {
                    ElementAccessor e = elements[j];
                    string ns = e.Form == XmlSchemaForm.Qualified ? e.Namespace : "";
                    if (!isSequence && e.Any && (e.Name == null || e.Name.Length == 0)) continue;
                    if (!firstElement || (!isSequence && count > 0))
                    {
                        Writer.Write("else ");
                    }
                    else if (isSequence)
                    {
                        Writer.Write("case ");
                        Writer.Write(cases.ToString(CultureInfo.InvariantCulture));
                        Writer.WriteLine(":");
                        Writer.Indent++;
                    }
                    count++;
                    firstElement = false;
                    Writer.Write("if (");
                    if (member.ParamsReadSource != null)
                    {
                        Writer.Write("!");
                        Writer.Write(member.ParamsReadSource);
                        Writer.Write(" && ");
                    }
                    if (checkType)
                    {
                        if (e.Mapping is NullableMapping)
                        {
                            TypeDesc td = ((NullableMapping)e.Mapping).BaseMapping.TypeDesc;
                            Writer.Write(RaCodeGen.GetStringForTypeof(td.CSharpName, td.UseReflection));
                        }
                        else
                        {
                            Writer.Write(RaCodeGen.GetStringForTypeof(e.Mapping.TypeDesc.CSharpName, e.Mapping.TypeDesc.UseReflection));
                        }
                        Writer.Write(".IsAssignableFrom(");
                        Writer.Write(checkTypeSource);
                        Writer.Write("Type)");
                    }
                    else
                    {
                        if (member.Mapping.IsReturnValue)
                            Writer.Write("(IsReturnValue || ");
                        if (isSequence && e.Any && e.AnyNamespaces == null)
                        {
                            Writer.Write("true");
                        }
                        else
                        {
                            WriteXmlNodeEqual("Reader", e.Name, ns);
                        }
                        if (member.Mapping.IsReturnValue)
                            Writer.Write(")");
                    }
                    Writer.WriteLine(") {");
                    Writer.Indent++;
                    if (checkType)
                    {
                        if (e.Mapping.TypeDesc.IsValueType || e.Mapping is NullableMapping)
                        {
                            Writer.Write("if (");
                            Writer.Write(checkTypeSource);
                            Writer.WriteLine(" != null) {");
                            Writer.Indent++;
                        }
                        if (e.Mapping is NullableMapping)
                        {
                            WriteSourceBegin(member.ArraySource);
                            TypeDesc td = ((NullableMapping)e.Mapping).BaseMapping.TypeDesc;
                            Writer.Write(RaCodeGen.GetStringForCreateInstance(e.Mapping.TypeDesc.CSharpName, e.Mapping.TypeDesc.UseReflection, false, true, "(" + td.CSharpName + ")" + checkTypeSource));
                        }
                        else
                        {
                            WriteSourceBeginTyped(member.ArraySource, e.Mapping.TypeDesc);
                            Writer.Write(checkTypeSource);
                        }
                        WriteSourceEnd(member.ArraySource);
                        Writer.WriteLine(";");
                        if (e.Mapping.TypeDesc.IsValueType)
                        {
                            Writer.Indent--;
                            Writer.WriteLine("}");
                        }
                        if (member.FixupIndex >= 0)
                        {
                            Writer.Write("fixup.Ids[");
                            Writer.Write(member.FixupIndex.ToString(CultureInfo.InvariantCulture));
                            Writer.Write("] = ");
                            Writer.Write(checkTypeSource);
                            Writer.WriteLine("Id;");
                        }
                    }
                    else
                    {
                        WriteElement(member.ArraySource, member.ArrayName, member.ChoiceArraySource, e, choice, member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite ? member.CheckSpecifiedSource : null, member.IsList && member.Mapping.TypeDesc.IsNullable, member.Mapping.ReadOnly, member.FixupIndex, j);
                    }
                    if (member.Mapping.IsReturnValue)
                        Writer.WriteLine("IsReturnValue = false;");
                    if (member.ParamsReadSource != null)
                    {
                        Writer.Write(member.ParamsReadSource);
                        Writer.WriteLine(" = true;");
                    }
                    Writer.Indent--;
                    Writer.WriteLine("}");
                }
                if (isSequence)
                {
                    if (member.IsArrayLike)
                    {
                        Writer.WriteLine("else {");
                        Writer.Indent++;
                    }
                    cases++;
                    Writer.Write("state = ");
                    Writer.Write(cases.ToString(CultureInfo.InvariantCulture));
                    Writer.WriteLine(";");
                    if (member.IsArrayLike)
                    {
                        Writer.Indent--;
                        Writer.WriteLine("}");
                    }
                    Writer.WriteLine("break;");
                    Writer.Indent--;
                }
            }
            if (count > 0)
            {
                if (isSequence)
                    Writer.WriteLine("default:");
                else
                    Writer.WriteLine("else {");
                Writer.Indent++;
            }
            WriteMemberElementsElse(anyElement, elementElseString);
            if (count > 0)
            {
                if (isSequence)
                {
                    Writer.WriteLine("break;");
                }
                Writer.Indent--;
                Writer.WriteLine("}");
            }
        }

        private string GetArraySource(TypeDesc typeDesc, string arrayName)
        {
            return GetArraySource(typeDesc, arrayName, false);
        }
        private string GetArraySource(TypeDesc typeDesc, string arrayName, bool multiRef)
        {
            string a = arrayName;
            string c = "c" + a;
            string init = "";

            if (multiRef)
            {
                init = "soap = (System.Object[])EnsureArrayIndex(soap, " + c + "+2, typeof(System.Object)); ";
            }
            bool useReflection = typeDesc.UseReflection;
            if (typeDesc.IsArray)
            {
                string arrayTypeFullName = typeDesc.ArrayElementTypeDesc.CSharpName;
                bool arrayUseReflection = typeDesc.ArrayElementTypeDesc.UseReflection;
                string castString = useReflection ? "" : "(" + arrayTypeFullName + "[])";
                init = init + a + " = " + castString +
                    "EnsureArrayIndex(" + a + ", " + c + ", " + RaCodeGen.GetStringForTypeof(arrayTypeFullName, arrayUseReflection) + ");";
                string arraySource = RaCodeGen.GetStringForArrayMember(a, c + "++", typeDesc);
                if (multiRef)
                {
                    init = init + " soap[1] = " + a + ";";
                    init = init + " if (ReadReference(out soap[" + c + "+2])) " + arraySource + " = null; else ";
                }
                return init + arraySource;
            }
            else
            {
                return RaCodeGen.GetStringForMethod(arrayName, typeDesc.CSharpName, "Add", useReflection);
            }
        }


        private void WriteMemberEnd(Member[] members)
        {
            WriteMemberEnd(members, false);
        }

        private void WriteMemberEnd(Member[] members, bool soapRefs)
        {
            for (int i = 0; i < members.Length; i++)
            {
                Member member = (Member)members[i];

                if (member.IsArrayLike)
                {
                    TypeDesc typeDesc = member.Mapping.TypeDesc;

                    if (typeDesc.IsArray)
                    {
                        WriteSourceBegin(member.Source);

                        if (soapRefs)
                            Writer.Write(" soap[1] = ");

                        string a = member.ArrayName;
                        string c = "c" + a;

                        bool arrayUseReflection = typeDesc.ArrayElementTypeDesc.UseReflection;
                        string arrayTypeFullName = typeDesc.ArrayElementTypeDesc.CSharpName;
                        if (!arrayUseReflection)
                            Writer.Write("(" + arrayTypeFullName + "[])");
                        Writer.Write("ShrinkArray(");
                        Writer.Write(a);
                        Writer.Write(", ");
                        Writer.Write(c);
                        Writer.Write(", ");
                        Writer.Write(RaCodeGen.GetStringForTypeof(arrayTypeFullName, arrayUseReflection));
                        Writer.Write(", ");
                        WriteBooleanValue(member.IsNullable);
                        Writer.Write(")");
                        WriteSourceEnd(member.Source);
                        Writer.WriteLine(";");

                        if (member.Mapping.ChoiceIdentifier != null)
                        {
                            WriteSourceBegin(member.ChoiceSource);
                            a = member.ChoiceArrayName;
                            c = "c" + a;

                            bool choiceUseReflection = member.Mapping.ChoiceIdentifier.Mapping.TypeDesc.UseReflection;
                            string choiceTypeName = member.Mapping.ChoiceIdentifier.Mapping.TypeDesc.CSharpName;
                            if (!choiceUseReflection)
                                Writer.Write("(" + choiceTypeName + "[])");
                            Writer.Write("ShrinkArray(");
                            Writer.Write(a);
                            Writer.Write(", ");
                            Writer.Write(c);
                            Writer.Write(", ");
                            Writer.Write(RaCodeGen.GetStringForTypeof(choiceTypeName, choiceUseReflection));
                            Writer.Write(", ");
                            WriteBooleanValue(member.IsNullable);
                            Writer.Write(")");
                            WriteSourceEnd(member.ChoiceSource);
                            Writer.WriteLine(";");
                        }
                    }
                    else if (typeDesc.IsValueType)
                    {
                        Writer.Write(member.Source);
                        Writer.Write(" = ");
                        Writer.Write(member.ArrayName);
                        Writer.WriteLine(";");
                    }
                }
            }
        }

        private void WriteSourceBeginTyped(string source, TypeDesc typeDesc)
        {
            WriteSourceBegin(source);
            if (typeDesc != null && !typeDesc.UseReflection)
            {
                Writer.Write("(");
                Writer.Write(typeDesc.CSharpName);
                Writer.Write(")");
            }
        }

        private void WriteSourceBegin(string source)
        {
            Writer.Write(source);
            if (source[source.Length - 1] != '(' && source[source.Length - 1] != '{')
                Writer.Write(" = ");
        }

        private void WriteSourceEnd(string source)
        {
            // source could be of the form "var", "arrayVar[i]",
            // "collection.Add(" or "methodInfo.Invoke(collection, new object[] {"
            if (source[source.Length - 1] == '(')
                Writer.Write(")");
            else if (source[source.Length - 1] == '{')
                Writer.Write("})");
        }

        private void WriteArray(string source, string arrayName, ArrayMapping arrayMapping, bool readOnly, bool isNullable, int fixupIndex)
        {
            if (arrayMapping.IsSoap)
            {
                Writer.Write("object rre = ");
                Writer.Write(fixupIndex >= 0 ? "ReadReferencingElement" : "ReadReferencedElement");
                Writer.Write("(");
                WriteID(arrayMapping.TypeName);
                Writer.Write(", ");
                WriteID(arrayMapping.Namespace);
                if (fixupIndex >= 0)
                {
                    Writer.Write(", ");
                    Writer.Write("out fixup.Ids[");
                    Writer.Write((fixupIndex).ToString(CultureInfo.InvariantCulture));
                    Writer.Write("]");
                }
                Writer.WriteLine(");");

                TypeDesc td = arrayMapping.TypeDesc;
                if (td.IsEnumerable || td.IsCollection)
                {
                    Writer.WriteLine("if (rre != null) {");
                    Writer.Indent++;
                    WriteAddCollectionFixup(td, readOnly, source, "rre");
                    Writer.Indent--;
                    Writer.WriteLine("}");
                }
                else
                {
                    Writer.WriteLine("try {");
                    Writer.Indent++;
                    WriteSourceBeginTyped(source, arrayMapping.TypeDesc);
                    Writer.Write("rre");
                    WriteSourceEnd(source);
                    Writer.WriteLine(";");
                    WriteCatchCastException(arrayMapping.TypeDesc, "rre", null);
                }
            }
            else
            {
                Writer.WriteLine("if (!ReadNull()) {");
                Writer.Indent++;

                MemberMapping memberMapping = new MemberMapping();
                memberMapping.Elements = arrayMapping.Elements;
                memberMapping.TypeDesc = arrayMapping.TypeDesc;
                memberMapping.ReadOnly = readOnly;
                Member member = new Member(this, source, arrayName, 0, memberMapping, false);
                member.IsNullable = false;//Note, sowmys: IsNullable is set to false since null condition (xsi:nil) is already handled by 'ReadNull()'

                Member[] members = new Member[] { member };
                WriteMemberBegin(members);

                if (readOnly)
                {
                    Writer.Write("if (((object)(");
                    Writer.Write(member.ArrayName);
                    Writer.Write(") == null) || ");
                }
                else
                {
                    Writer.Write("if (");
                }
                Writer.WriteLine("(Reader.IsEmptyElement)) {");
                Writer.Indent++;
                Writer.WriteLine("Reader.Skip();");
                Writer.Indent--;
                Writer.WriteLine("}");
                Writer.WriteLine("else {");
                Writer.Indent++;

                Writer.WriteLine("Reader.ReadStartElement();");
                int loopIndex = WriteWhileNotLoopStart();
                Writer.Indent++;

                string unknownNode = "UnknownNode(null, " + ExpectedElements(members) + ");";
                WriteMemberElements(members, unknownNode, unknownNode, null, null, null);
                Writer.WriteLine("Reader.MoveToContent();");

                WriteWhileLoopEnd(loopIndex);
                Writer.Indent--;
                Writer.WriteLine("ReadEndElement();");
                Writer.WriteLine("}");

                WriteMemberEnd(members, false);

                Writer.Indent--;
                Writer.WriteLine("}");
                if (isNullable)
                {
                    Writer.WriteLine("else {");
                    Writer.Indent++;
                    member.IsNullable = true;
                    WriteMemberBegin(members);
                    WriteMemberEnd(members);
                    Writer.Indent--;
                    Writer.WriteLine("}");
                }
            }
        }

        private void WriteElement(string source, string arrayName, string choiceSource, ElementAccessor element, ChoiceIdentifierAccessor choice, string checkSpecified, bool checkForNull, bool readOnly, int fixupIndex, int elementIndex)
        {
            if (checkSpecified != null && checkSpecified.Length > 0)
            {
                Writer.Write(checkSpecified);
                Writer.WriteLine(" = true;");
            }

            if (element.Mapping is ArrayMapping)
            {
                WriteArray(source, arrayName, (ArrayMapping)element.Mapping, readOnly, element.IsNullable, fixupIndex);
            }
            else if (element.Mapping is NullableMapping)
            {
                string methodName = ReferenceMapping(element.Mapping);
#if DEBUG
                // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                if (methodName == null) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorMethod, element.Mapping.TypeDesc.Name));
#endif
                WriteSourceBegin(source);
                Writer.Write(methodName);
                Writer.Write("(true)");
                WriteSourceEnd(source);
                Writer.WriteLine(";");
            }
            else if (!element.Mapping.IsSoap && (element.Mapping is PrimitiveMapping))
            {
                if (element.IsNullable)
                {
                    Writer.WriteLine("if (ReadNull()) {");
                    Writer.Indent++;
                    WriteSourceBegin(source);
                    if (element.Mapping.TypeDesc.IsValueType)
                    {
                        Writer.Write(RaCodeGen.GetStringForCreateInstance(element.Mapping.TypeDesc.CSharpName, element.Mapping.TypeDesc.UseReflection, false, false));
                    }
                    else
                    {
                        Writer.Write("null");
                    }
                    WriteSourceEnd(source);
                    Writer.WriteLine(";");
                    Writer.Indent--;
                    Writer.WriteLine("}");
                    Writer.Write("else ");
                }
                if (element.Default != null && element.Default != DBNull.Value && element.Mapping.TypeDesc.IsValueType)
                {
                    Writer.WriteLine("if (Reader.IsEmptyElement) {");
                    Writer.Indent++;
                    Writer.WriteLine("Reader.Skip();");
                    Writer.Indent--;
                    Writer.WriteLine("}");
                    Writer.WriteLine("else {");
                }
                else
                {
                    Writer.WriteLine("{");
                }
                Writer.Indent++;

                WriteSourceBegin(source);
                if (element.Mapping.TypeDesc == QnameTypeDesc)
                    Writer.Write("ReadElementQualifiedName()");
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

                WriteSourceEnd(source);
                Writer.WriteLine(";");
                Writer.Indent--;
                Writer.WriteLine("}");
            }
            else if (element.Mapping is StructMapping || (element.Mapping.IsSoap && element.Mapping is PrimitiveMapping))
            {
                TypeMapping mapping = element.Mapping;
                if (mapping.IsSoap)
                {
                    Writer.Write("object rre = ");
                    Writer.Write(fixupIndex >= 0 ? "ReadReferencingElement" : "ReadReferencedElement");
                    Writer.Write("(");
                    WriteID(mapping.TypeName);
                    Writer.Write(", ");
                    WriteID(mapping.Namespace);

                    if (fixupIndex >= 0)
                    {
                        Writer.Write(", out fixup.Ids[");
                        Writer.Write((fixupIndex).ToString(CultureInfo.InvariantCulture));
                        Writer.Write("]");
                    }
                    Writer.Write(")");
                    WriteSourceEnd(source);
                    Writer.WriteLine(";");

                    if (mapping.TypeDesc.IsValueType)
                    {
                        Writer.WriteLine("if (rre != null) {");
                        Writer.Indent++;
                    }

                    Writer.WriteLine("try {");
                    Writer.Indent++;
                    WriteSourceBeginTyped(source, mapping.TypeDesc);
                    Writer.Write("rre");
                    WriteSourceEnd(source);
                    Writer.WriteLine(";");
                    WriteCatchCastException(mapping.TypeDesc, "rre", null);
                    Writer.Write("Referenced(");
                    Writer.Write(source);
                    Writer.WriteLine(");");
                    if (mapping.TypeDesc.IsValueType)
                    {
                        Writer.Indent--;
                        Writer.WriteLine("}");
                    }
                }
                else
                {
                    string methodName = ReferenceMapping(mapping);
#if DEBUG
                        // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                        if (methodName == null) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorMethod, mapping.TypeDesc.Name));
#endif

                    if (checkForNull)
                    {
                        Writer.Write("if ((object)(");
                        Writer.Write(arrayName);
                        Writer.Write(") == null) Reader.Skip(); else ");
                    }
                    WriteSourceBegin(source);
                    Writer.Write(methodName);
                    Writer.Write("(");
                    if (mapping.TypeDesc.IsNullable)
                    {
                        WriteBooleanValue(element.IsNullable);
                        Writer.Write(", ");
                    }
                    Writer.Write("true");
                    Writer.Write(")");
                    WriteSourceEnd(source);
                    Writer.WriteLine(";");
                }
            }
            else if (element.Mapping is SpecialMapping)
            {
                SpecialMapping special = (SpecialMapping)element.Mapping;
                switch (special.TypeDesc.Kind)
                {
                    case TypeKind.Node:
                        bool isDoc = special.TypeDesc.FullName == typeof(XmlDocument).FullName;
                        WriteSourceBeginTyped(source, special.TypeDesc);
                        Writer.Write(isDoc ? "ReadXmlDocument(" : "ReadXmlNode(");
                        Writer.Write(element.Any ? "false" : "true");
                        Writer.Write(")");
                        WriteSourceEnd(source);
                        Writer.WriteLine(";");
                        break;
                    case TypeKind.Serializable:
                        SerializableMapping sm = (SerializableMapping)element.Mapping;
                        // check to see if we need to do the derivation
                        if (sm.DerivedMappings != null)
                        {
                            Writer.Write(typeof(XmlQualifiedName).FullName);
                            Writer.WriteLine(" tser = GetXsiType();");
                            Writer.Write("if (tser == null");
                            Writer.Write(" || ");
                            WriteQNameEqual("tser", sm.XsiType.Name, sm.XsiType.Namespace);

                            Writer.WriteLine(") {");
                            Writer.Indent++;
                        }
                        WriteSourceBeginTyped(source, sm.TypeDesc);
                        Writer.Write("ReadSerializable(( ");
                        Writer.Write(typeof(IXmlSerializable).FullName);
                        Writer.Write(")");
                        Writer.Write(RaCodeGen.GetStringForCreateInstance(sm.TypeDesc.CSharpName, sm.TypeDesc.UseReflection, sm.TypeDesc.CannotNew, false));
                        bool isWrappedAny = !element.Any && IsWildcard(sm);
                        if (isWrappedAny)
                        {
                            Writer.WriteLine(", true");
                        }
                        Writer.Write(")");
                        WriteSourceEnd(source);
                        Writer.WriteLine(";");
                        if (sm.DerivedMappings != null)
                        {
                            Writer.Indent--;
                            Writer.WriteLine("}");
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

                string enumTypeName = choice.Mapping.TypeDesc.CSharpName;
                Writer.Write(choiceSource);
                Writer.Write(" = ");
                CodeIdentifier.CheckValidIdentifier(choice.MemberIds[elementIndex]);
                Writer.Write(RaCodeGen.GetStringForEnumMember(enumTypeName, choice.MemberIds[elementIndex], choice.Mapping.TypeDesc.UseReflection));
                Writer.WriteLine(";");
            }
        }

        private void WriteDerivedSerializable(SerializableMapping head, SerializableMapping mapping, string source, bool isWrappedAny)
        {
            if (mapping == null)
                return;
            for (SerializableMapping derived = mapping.DerivedMappings; derived != null; derived = derived.NextDerivedMapping)
            {
                Writer.Write("else if (tser == null");
                Writer.Write(" || ");
                WriteQNameEqual("tser", derived.XsiType.Name, derived.XsiType.Namespace);

                Writer.WriteLine(") {");
                Writer.Indent++;

                if (derived.Type != null)
                {
                    if (head.Type.IsAssignableFrom(derived.Type))
                    {
                        WriteSourceBeginTyped(source, head.TypeDesc);
                        Writer.Write("ReadSerializable(( ");
                        Writer.Write(typeof(IXmlSerializable).FullName);
                        Writer.Write(")");
                        Writer.Write(RaCodeGen.GetStringForCreateInstance(derived.TypeDesc.CSharpName, derived.TypeDesc.UseReflection, derived.TypeDesc.CannotNew, false));
                        if (isWrappedAny)
                        {
                            Writer.WriteLine(", true");
                        }
                        Writer.Write(")");
                        WriteSourceEnd(source);
                        Writer.WriteLine(";");
                    }
                    else
                    {
                        Writer.Write("throw CreateBadDerivationException(");
                        WriteQuotedCSharpString(derived.XsiType.Name);
                        Writer.Write(", ");
                        WriteQuotedCSharpString(derived.XsiType.Namespace);
                        Writer.Write(", ");
                        WriteQuotedCSharpString(head.XsiType.Name);
                        Writer.Write(", ");
                        WriteQuotedCSharpString(head.XsiType.Namespace);
                        Writer.Write(", ");
                        WriteQuotedCSharpString(derived.Type.FullName);
                        Writer.Write(", ");
                        WriteQuotedCSharpString(head.Type.FullName);
                        Writer.WriteLine(");");
                    }
                }
                else
                {
                    Writer.WriteLine("// " + "missing real mapping for " + derived.XsiType);
                    Writer.Write("throw CreateMissingIXmlSerializableType(");
                    WriteQuotedCSharpString(derived.XsiType.Name);
                    Writer.Write(", ");
                    WriteQuotedCSharpString(derived.XsiType.Namespace);
                    Writer.Write(", ");
                    WriteQuotedCSharpString(head.Type.FullName);
                    Writer.WriteLine(");");
                }

                Writer.Indent--;
                Writer.WriteLine("}");

                WriteDerivedSerializable(head, derived, source, isWrappedAny);
            }
        }

        private int WriteWhileNotLoopStart()
        {
            Writer.WriteLine("Reader.MoveToContent();");
            int loopIndex = WriteWhileLoopStartCheck();
            Writer.Write("while (Reader.NodeType != ");
            Writer.Write(typeof(XmlNodeType).FullName);
            Writer.Write(".EndElement && Reader.NodeType != ");
            Writer.Write(typeof(XmlNodeType).FullName);
            Writer.WriteLine(".None) {");
            return loopIndex;
        }

        private void WriteWhileLoopEnd(int loopIndex)
        {
            WriteWhileLoopEndCheck(loopIndex);
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private int WriteWhileLoopStartCheck()
        {
            Writer.WriteLine(String.Format(CultureInfo.InvariantCulture, "int whileIterations{0} = 0;", _nextWhileLoopIndex));
            Writer.WriteLine(String.Format(CultureInfo.InvariantCulture, "int readerCount{0} = ReaderCount;", _nextWhileLoopIndex));
            return _nextWhileLoopIndex++;
        }

        private void WriteWhileLoopEndCheck(int loopIndex)
        {
            Writer.WriteLine(String.Format(CultureInfo.InvariantCulture, "CheckReaderCount(ref whileIterations{0}, ref readerCount{1});", loopIndex, loopIndex));
        }

        private void WriteParamsRead(int length)
        {
            Writer.Write("bool[] paramsRead = new bool[");
            Writer.Write(length.ToString(CultureInfo.InvariantCulture));
            Writer.WriteLine("];");
        }

        private void WriteReadNonRoots()
        {
            Writer.WriteLine("Reader.MoveToContent();");
            int loopIndex = WriteWhileLoopStartCheck();
            Writer.Write("while (Reader.NodeType == ");
            Writer.Write(typeof(XmlNodeType).FullName);
            Writer.WriteLine(".Element) {");
            Writer.Indent++;
            Writer.Write("string root = Reader.GetAttribute(\"root\", \"");
            Writer.Write(Soap.Encoding);
            Writer.WriteLine("\");");
            Writer.Write("if (root == null || ");
            Writer.Write(typeof(XmlConvert).FullName);
            Writer.WriteLine(".ToBoolean(root)) break;");
            Writer.WriteLine("ReadReferencedElement();");
            Writer.WriteLine("Reader.MoveToContent();");
            WriteWhileLoopEnd(loopIndex);
        }

        private void WriteBooleanValue(bool value)
        {
            Writer.Write(value ? "true" : "false");
        }

        private void WriteInitCheckTypeHrefList(string source)
        {
            Writer.Write(typeof(ArrayList).FullName);
            Writer.Write(" ");
            Writer.Write(source);
            Writer.Write(" = new ");
            Writer.Write(typeof(ArrayList).FullName);
            Writer.WriteLine("();");

            Writer.Write(typeof(ArrayList).FullName);
            Writer.Write(" ");
            Writer.Write(source);
            Writer.Write("IsObject = new ");
            Writer.Write(typeof(ArrayList).FullName);
            Writer.WriteLine("();");
        }

        private void WriteHandleHrefList(Member[] members, string listSource)
        {
            Writer.WriteLine("int isObjectIndex = 0;");
            Writer.Write("foreach (object obj in ");
            Writer.Write(listSource);
            Writer.WriteLine(") {");
            Writer.Indent++;
            Writer.WriteLine("bool isReferenced = true;");
            Writer.Write("bool isObject = (bool)");
            Writer.Write(listSource);
            Writer.WriteLine("IsObject[isObjectIndex++];");
            Writer.WriteLine("object refObj = isObject ? obj : GetTarget((string)obj);");
            Writer.WriteLine("if (refObj == null) continue;");
            Writer.Write(typeof(Type).FullName);
            Writer.WriteLine(" refObjType = refObj.GetType();");
            Writer.WriteLine("string refObjId = null;");

            WriteMemberElementsIf(members, null, "isReferenced = false;", "refObj");

            Writer.WriteLine("if (isObject && isReferenced) Referenced(refObj); // need to mark this obj as ref'd since we didn't do GetTarget");
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteIfNotSoapRoot(string source)
        {
            Writer.Write("if (Reader.GetAttribute(\"root\", \"");
            Writer.Write(Soap.Encoding);
            Writer.WriteLine("\") == \"0\") {");
            Writer.Indent++;
            Writer.WriteLine(source);
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteCreateMapping(TypeMapping mapping, string local)
        {
            string fullTypeName = mapping.TypeDesc.CSharpName;
            bool useReflection = mapping.TypeDesc.UseReflection;
            bool ctorInaccessible = mapping.TypeDesc.CannotNew;

            Writer.Write(useReflection ? "object" : fullTypeName);
            Writer.Write(" ");
            Writer.Write(local);
            Writer.WriteLine(";");

            if (ctorInaccessible)
            {
                Writer.WriteLine("try {");
                Writer.Indent++;
            }
            Writer.Write(local);
            Writer.Write(" = ");
            Writer.Write(RaCodeGen.GetStringForCreateInstance(fullTypeName, useReflection, mapping.TypeDesc.CannotNew, true));
            Writer.WriteLine(";");
            if (ctorInaccessible)
            {
                WriteCatchException(typeof(MissingMethodException));
                Writer.Indent++;
                Writer.Write("throw CreateInaccessibleConstructorException(");
                WriteQuotedCSharpString(fullTypeName);
                Writer.WriteLine(");");

                WriteCatchException(typeof(SecurityException));
                Writer.Indent++;

                Writer.Write("throw CreateCtorHasSecurityException(");
                WriteQuotedCSharpString(fullTypeName);
                Writer.WriteLine(");");

                Writer.Indent--;
                Writer.WriteLine("}");
            }
        }

        private void WriteCatchException(Type exceptionType)
        {
            Writer.Indent--;
            Writer.WriteLine("}");
            Writer.Write("catch (");
            Writer.Write(exceptionType.FullName);
            Writer.WriteLine(") {");
        }

        private void WriteCatchCastException(TypeDesc typeDesc, string source, string id)
        {
            WriteCatchException(typeof(InvalidCastException));
            Writer.Indent++;
            Writer.Write("throw CreateInvalidCastException(");
            Writer.Write(RaCodeGen.GetStringForTypeof(typeDesc.CSharpName, typeDesc.UseReflection));
            Writer.Write(", ");
            Writer.Write(source);
            if (id == null)
                Writer.WriteLine(", null);");
            else
            {
                Writer.Write(", (string)");
                Writer.Write(id);
                Writer.WriteLine(");");
            }
            Writer.Indent--;
            Writer.WriteLine("}");
        }
        private void WriteArrayLocalDecl(string typeName, string variableName, string initValue, TypeDesc arrayTypeDesc)
        {
            RaCodeGen.WriteArrayLocalDecl(typeName, variableName, initValue, arrayTypeDesc);
        }
        private void WriteCreateInstance(string escapedName, string source, bool useReflection, bool ctorInaccessible)
        {
            RaCodeGen.WriteCreateInstance(escapedName, source, useReflection, ctorInaccessible);
        }
        private void WriteLocalDecl(string typeFullName, string variableName, string initValue, bool useReflection)
        {
            RaCodeGen.WriteLocalDecl(typeFullName, variableName, initValue, useReflection);
        }
    }
}
