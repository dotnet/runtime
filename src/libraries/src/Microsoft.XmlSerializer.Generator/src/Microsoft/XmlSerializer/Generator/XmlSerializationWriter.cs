// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.XmlSerializer.Generator
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Xml.Schema;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Runtime.Versioning;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Serialization;

    internal class XmlSerializationWriterCodeGen : XmlSerializationCodeGen
    {
        internal XmlSerializationWriterCodeGen(IndentedWriter writer, TypeScope[] scopes, string access, string className) : base(writer, scopes, access, className)
        {
        }

        internal void GenerateBegin()
        {
            Writer.Write(Access);
            Writer.Write(" class ");
            Writer.Write(ClassName);
            Writer.Write(" : ");
            Writer.Write(typeof(System.Xml.Serialization.XmlSerializationWriter).FullName);
            Writer.WriteLine(" {");
            Writer.Indent++;

            foreach (TypeScope scope in Scopes)
            {
                foreach (TypeMapping mapping in scope.TypeMappings)
                {
                    if (mapping is StructMapping || mapping is EnumMapping)
                    {
                        MethodNames.Add(mapping, NextMethodName(mapping.TypeDesc.Name));
                    }
                }
                RaCodeGen.WriteReflectionInit(scope);
            }

            // pre-generate write methods only for the encoded soap
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
        }
        internal void GenerateEnd()
        {
            GenerateReferencedMethods();
            GenerateInitCallbacksMethod();
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        internal string GenerateElement(XmlMapping xmlMapping)
        {
            if (!xmlMapping.IsWriteable)
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

        private void GenerateInitCallbacksMethod()
        {
            Writer.WriteLine();
            Writer.WriteLine("protected override void InitCallbacks() {");
            Writer.Indent++;

            foreach (TypeScope scope in Scopes)
            {
                foreach (TypeMapping typeMapping in scope.TypeMappings)
                {
                    if (typeMapping.IsSoap &&
                        (typeMapping is StructMapping || typeMapping is EnumMapping) &&
                        !typeMapping.TypeDesc.IsRoot)
                    {
                        string methodName = (string)MethodNames[typeMapping];
                        Writer.Write("AddWriteCallback(");
                        Writer.Write(RaCodeGen.GetStringForTypeof(typeMapping.TypeDesc.CSharpName, typeMapping.TypeDesc.UseReflection));
                        Writer.Write(", ");
                        WriteQuotedCSharpString(typeMapping.TypeName);
                        Writer.Write(", ");
                        WriteQuotedCSharpString(typeMapping.Namespace);
                        Writer.Write(", new ");
                        Writer.Write(typeof(System.Xml.Serialization.XmlSerializationWriteCallback).FullName);
                        Writer.Write("(this.");
                        Writer.Write(methodName);
                        Writer.WriteLine("));");
                    }
                }
            }
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteQualifiedNameElement(string name, string ns, object defaultValue, string source, bool nullable, bool IsSoap, TypeMapping mapping)
        {
            bool hasDefault = defaultValue != null && defaultValue != DBNull.Value;
            if (hasDefault)
            {
                WriteCheckDefault(source, defaultValue, nullable);
                Writer.WriteLine(" {");
                Writer.Indent++;
            }
            string suffix = IsSoap ? "Encoded" : "Literal";
            Writer.Write(nullable ? ("WriteNullableQualifiedName" + suffix) : "WriteElementQualifiedName");
            Writer.Write("(");
            WriteQuotedCSharpString(name);
            if (ns != null)
            {
                Writer.Write(", ");
                WriteQuotedCSharpString(ns);
            }
            Writer.Write(", ");
            Writer.Write(source);

            if (IsSoap)
            {
                Writer.Write(", new System.Xml.XmlQualifiedName(");
                WriteQuotedCSharpString(mapping.TypeName);
                Writer.Write(", ");
                WriteQuotedCSharpString(mapping.Namespace);
                Writer.Write(")");
            }

            Writer.WriteLine(");");

            if (hasDefault)
            {
                Writer.Indent--;
                Writer.WriteLine("}");
            }
        }

        private void WriteEnumValue(EnumMapping mapping, string source)
        {
            string methodName = ReferenceMapping(mapping);

#if DEBUG
                // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                if (methodName == null) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorMethod, mapping.TypeDesc.Name) + Environment.StackTrace);
#endif

            Writer.Write(methodName);
            Writer.Write("(");
            Writer.Write(source);
            Writer.Write(")");
        }

        private void WritePrimitiveValue(TypeDesc typeDesc, string source, bool isElement)
        {
            if (typeDesc == StringTypeDesc || typeDesc.FormatterName == "String")
            {
                Writer.Write(source);
            }
            else
            {
                if (!typeDesc.HasCustomFormatter)
                {
                    Writer.Write(typeof(XmlConvert).FullName);
                    Writer.Write(".ToString((");
                    Writer.Write(typeDesc.CSharpName);
                    Writer.Write(")");
                    Writer.Write(source);
                    Writer.Write(")");
                }
                else
                {
                    Writer.Write("From");
                    Writer.Write(typeDesc.FormatterName);
                    Writer.Write("(");
                    Writer.Write(source);
                    Writer.Write(")");
                }
            }
        }

        private void WritePrimitive(string method, string name, string ns, object defaultValue, string source, TypeMapping mapping, bool writeXsiType, bool isElement, bool isNullable)
        {
            TypeDesc typeDesc = mapping.TypeDesc;
            bool hasDefault = defaultValue != null && defaultValue != DBNull.Value && mapping.TypeDesc.HasDefaultSupport;
            if (hasDefault)
            {
                if (mapping is EnumMapping)
                {
#if DEBUG
                        // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                        if (defaultValue.GetType() != typeof(string)) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, name + " has invalid default type " + defaultValue.GetType().Name));
#endif

                    Writer.Write("if (");
                    if (mapping.TypeDesc.UseReflection)
                        Writer.Write(RaCodeGen.GetStringForEnumLongValue(source, mapping.TypeDesc.UseReflection));
                    else
                        Writer.Write(source);
                    Writer.Write(" != ");
                    if (((EnumMapping)mapping).IsFlags)
                    {
                        Writer.Write("(");
                        string[] values = ((string)defaultValue).Split(null);
                        for (int i = 0; i < values.Length; i++)
                        {
                            if (values[i] == null || values[i].Length == 0)
                                continue;
                            if (i > 0)
                                Writer.WriteLine(" | ");
                            Writer.Write(RaCodeGen.GetStringForEnumCompare((EnumMapping)mapping, values[i], mapping.TypeDesc.UseReflection));
                        }
                        Writer.Write(")");
                    }
                    else
                    {
                        Writer.Write(RaCodeGen.GetStringForEnumCompare((EnumMapping)mapping, (string)defaultValue, mapping.TypeDesc.UseReflection));
                    }
                    Writer.Write(")");
                }
                else
                {
                    WriteCheckDefault(source, defaultValue, isNullable);
                }
                Writer.WriteLine(" {");
                Writer.Indent++;
            }
            Writer.Write(method);
            Writer.Write("(");
            WriteQuotedCSharpString(name);
            if (ns != null)
            {
                Writer.Write(", ");
                WriteQuotedCSharpString(ns);
            }
            Writer.Write(", ");

            if (mapping is EnumMapping)
            {
                WriteEnumValue((EnumMapping)mapping, source);
            }
            else
            {
                WritePrimitiveValue(typeDesc, source, isElement);
            }

            if (writeXsiType)
            {
                Writer.Write(", new System.Xml.XmlQualifiedName(");
                WriteQuotedCSharpString(mapping.TypeName);
                Writer.Write(", ");
                WriteQuotedCSharpString(mapping.Namespace);
                Writer.Write(")");
            }

            Writer.WriteLine(");");

            if (hasDefault)
            {
                Writer.Indent--;
                Writer.WriteLine("}");
            }
        }

        private void WriteTag(string methodName, string name, string ns)
        {
            Writer.Write(methodName);
            Writer.Write("(");
            WriteQuotedCSharpString(name);
            Writer.Write(", ");
            if (ns == null)
            {
                Writer.Write("null");
            }
            else
            {
                WriteQuotedCSharpString(ns);
            }
            Writer.WriteLine(");");
        }

        private void WriteTag(string methodName, string name, string ns, bool writePrefixed)
        {
            Writer.Write(methodName);
            Writer.Write("(");
            WriteQuotedCSharpString(name);
            Writer.Write(", ");
            if (ns == null)
            {
                Writer.Write("null");
            }
            else
            {
                WriteQuotedCSharpString(ns);
            }
            Writer.Write(", null, ");
            if (writePrefixed)
                Writer.Write("true");
            else
                Writer.Write("false");
            Writer.WriteLine(");");
        }

        private void WriteStartElement(string name, string ns, bool writePrefixed)
        {
            WriteTag("WriteStartElement", name, ns, writePrefixed);
        }

        private void WriteEndElement()
        {
            Writer.WriteLine("WriteEndElement();");
        }
        private void WriteEndElement(string source)
        {
            Writer.Write("WriteEndElement(");
            Writer.Write(source);
            Writer.WriteLine(");");
        }

        private void WriteEncodedNullTag(string name, string ns)
        {
            WriteTag("WriteNullTagEncoded", name, ns);
        }

        private void WriteLiteralNullTag(string name, string ns)
        {
            WriteTag("WriteNullTagLiteral", name, ns);
        }

        private void WriteEmptyTag(string name, string ns)
        {
            WriteTag("WriteEmptyTag", name, ns);
        }

        private string GenerateMembersElement(XmlMembersMapping xmlMembersMapping)
        {
            ElementAccessor element = xmlMembersMapping.Accessor;
            MembersMapping mapping = (MembersMapping)element.Mapping;
            bool hasWrapperElement = mapping.HasWrapperElement;
            bool writeAccessors = mapping.WriteAccessors;
            bool isRpc = xmlMembersMapping.IsSoap && writeAccessors;
            string methodName = NextMethodName(element.Name);
            Writer.WriteLine();
            Writer.Write("public void ");
            Writer.Write(methodName);
            Writer.WriteLine("(object[] p) {");
            Writer.Indent++;

            Writer.WriteLine("WriteStartDocument();");

            if (!mapping.IsSoap)
            {
                Writer.WriteLine("TopLevelElement();");
            }

            // in the top-level method add check for the parameters length, 
            // because visual basic does not have a concept of an <out> parameter it uses <ByRef> instead
            // so sometime we think that we have more parameters then supplied
            Writer.WriteLine("int pLength = p.Length;");

            if (hasWrapperElement)
            {
                WriteStartElement(element.Name, (element.Form == XmlSchemaForm.Qualified ? element.Namespace : ""), mapping.IsSoap);

                int xmlnsMember = FindXmlnsIndex(mapping.Members);
                if (xmlnsMember >= 0)
                {
                    MemberMapping member = mapping.Members[xmlnsMember];
                    string source = "((" + typeof(System.Xml.Serialization.XmlSerializerNamespaces).FullName + ")p[" + xmlnsMember.ToString(CultureInfo.InvariantCulture) + "])";

                    Writer.Write("if (pLength > ");
                    Writer.Write(xmlnsMember.ToString(CultureInfo.InvariantCulture));
                    Writer.WriteLine(") {");
                    Writer.Indent++;
                    WriteNamespaces(source);
                    Writer.Indent--;
                    Writer.WriteLine("}");
                }

                for (int i = 0; i < mapping.Members.Length; i++)
                {
                    MemberMapping member = mapping.Members[i];

                    if (member.Attribute != null && !member.Ignore)
                    {
                        string index = i.ToString(CultureInfo.InvariantCulture);
                        string source = "p[" + index + "]";

                        string specifiedSource = null;
                        int specifiedPosition = 0;
                        if (member.CheckSpecified != SpecifiedAccessor.None)
                        {
                            string memberNameSpecified = member.Name + "Specified";
                            for (int j = 0; j < mapping.Members.Length; j++)
                            {
                                if (mapping.Members[j].Name == memberNameSpecified)
                                {
                                    specifiedSource = "((bool) p[" + j.ToString(CultureInfo.InvariantCulture) + "])";
                                    specifiedPosition = j;
                                    break;
                                }
                            }
                        }

                        Writer.Write("if (pLength > ");
                        Writer.Write(index);
                        Writer.WriteLine(") {");
                        Writer.Indent++;

                        if (specifiedSource != null)
                        {
                            Writer.Write("if (pLength <= ");
                            Writer.Write(specifiedPosition.ToString(CultureInfo.InvariantCulture));
                            Writer.Write(" || ");
                            Writer.Write(specifiedSource);
                            Writer.WriteLine(") {");
                            Writer.Indent++;
                        }

                        WriteMember(source, member.Attribute, member.TypeDesc, "p");

                        if (specifiedSource != null)
                        {
                            Writer.Indent--;
                            Writer.WriteLine("}");
                        }

                        Writer.Indent--;
                        Writer.WriteLine("}");
                    }
                }
            }

            for (int i = 0; i < mapping.Members.Length; i++)
            {
                MemberMapping member = mapping.Members[i];
                if (member.Xmlns != null)
                    continue;
                if (member.Ignore)
                    continue;

                string specifiedSource = null;
                int specifiedPosition = 0;
                if (member.CheckSpecified != SpecifiedAccessor.None)
                {
                    string memberNameSpecified = member.Name + "Specified";

                    for (int j = 0; j < mapping.Members.Length; j++)
                    {
                        if (mapping.Members[j].Name == memberNameSpecified)
                        {
                            specifiedSource = "((bool) p[" + j.ToString(CultureInfo.InvariantCulture) + "])";
                            specifiedPosition = j;
                            break;
                        }
                    }
                }

                string index = i.ToString(CultureInfo.InvariantCulture);
                Writer.Write("if (pLength > ");
                Writer.Write(index);
                Writer.WriteLine(") {");
                Writer.Indent++;

                if (specifiedSource != null)
                {
                    Writer.Write("if (pLength <= ");
                    Writer.Write(specifiedPosition.ToString(CultureInfo.InvariantCulture));
                    Writer.Write(" || ");
                    Writer.Write(specifiedSource);
                    Writer.WriteLine(") {");
                    Writer.Indent++;
                }

                string source = "p[" + index + "]";
                string enumSource = null;
                if (member.ChoiceIdentifier != null)
                {
                    for (int j = 0; j < mapping.Members.Length; j++)
                    {
                        if (mapping.Members[j].Name == member.ChoiceIdentifier.MemberName)
                        {
                            if (member.ChoiceIdentifier.Mapping.TypeDesc.UseReflection)
                                enumSource = "p[" + j.ToString(CultureInfo.InvariantCulture) + "]";
                            else
                                enumSource = "((" + mapping.Members[j].TypeDesc.CSharpName + ")p[" + j.ToString(CultureInfo.InvariantCulture) + "]" + ")";
                            break;
                        }
                    }

#if DEBUG
                        // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                        if (enumSource == null) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "Can not find " + member.ChoiceIdentifier.MemberName + " in the members mapping."));
#endif
                }

                if (isRpc && member.IsReturnValue && member.Elements.Length > 0)
                {
                    Writer.Write("WriteRpcResult(");
                    WriteQuotedCSharpString(member.Elements[0].Name);
                    Writer.Write(", ");
                    WriteQuotedCSharpString("");
                    Writer.WriteLine(");");
                }

                // override writeAccessors choice when we've written a wrapper element
                WriteMember(source, enumSource, member.ElementsSortedByDerivation, member.Text, member.ChoiceIdentifier, member.TypeDesc, writeAccessors || hasWrapperElement);

                if (specifiedSource != null)
                {
                    Writer.Indent--;
                    Writer.WriteLine("}");
                }

                Writer.Indent--;
                Writer.WriteLine("}");
            }

            if (hasWrapperElement)
            {
                WriteEndElement();
            }
            if (element.IsSoap)
            {
                if (!hasWrapperElement && !writeAccessors)
                {
                    // doc/bare case -- allow extra members
                    Writer.Write("if (pLength > ");
                    Writer.Write(mapping.Members.Length.ToString(CultureInfo.InvariantCulture));
                    Writer.WriteLine(") {");
                    Writer.Indent++;

                    WriteExtraMembers(mapping.Members.Length.ToString(CultureInfo.InvariantCulture), "pLength");

                    Writer.Indent--;
                    Writer.WriteLine("}");
                }
                Writer.WriteLine("WriteReferencedElements();");
            }
            Writer.Indent--;
            Writer.WriteLine("}");
            return methodName;
        }

        private string GenerateTypeElement(XmlTypeMapping xmlTypeMapping)
        {
            ElementAccessor element = xmlTypeMapping.Accessor;
            TypeMapping mapping = element.Mapping;
            string methodName = NextMethodName(element.Name);
            Writer.WriteLine();
            Writer.Write("public void ");
            Writer.Write(methodName);
            Writer.WriteLine("(object o) {");
            Writer.Indent++;

            Writer.WriteLine("WriteStartDocument();");

            Writer.WriteLine("if (o == null) {");
            Writer.Indent++;
            if (element.IsNullable)
            {
                if (mapping.IsSoap)
                    WriteEncodedNullTag(element.Name, (element.Form == XmlSchemaForm.Qualified ? element.Namespace : ""));
                else
                    WriteLiteralNullTag(element.Name, (element.Form == XmlSchemaForm.Qualified ? element.Namespace : ""));
            }
            else
                WriteEmptyTag(element.Name, (element.Form == XmlSchemaForm.Qualified ? element.Namespace : ""));
            Writer.WriteLine("return;");
            Writer.Indent--;
            Writer.WriteLine("}");

            if (!mapping.IsSoap && !mapping.TypeDesc.IsValueType && !mapping.TypeDesc.Type.IsPrimitive)
            {
                Writer.WriteLine("TopLevelElement();");
            }

            WriteMember("o", null, new ElementAccessor[] { element }, null, null, mapping.TypeDesc, !element.IsSoap);

            if (mapping.IsSoap)
            {
                Writer.WriteLine("WriteReferencedElements();");
            }
            Writer.Indent--;
            Writer.WriteLine("}");
            return methodName;
        }

        private string NextMethodName(string name)
        {
            return "Write" + (++NextMethodNumber).ToString(null, NumberFormatInfo.InvariantInfo) + "_" + CodeIdentifier.MakeValidInternal(name);
        }

        private void WriteEnumMethod(EnumMapping mapping)
        {
            string methodName = (string)MethodNames[mapping];
            Writer.WriteLine();
            string fullTypeName = mapping.TypeDesc.CSharpName;
            if (mapping.IsSoap)
            {
                Writer.Write("void ");
                Writer.Write(methodName);
                Writer.WriteLine("(object e) {");
                WriteLocalDecl(fullTypeName, "v", "e", mapping.TypeDesc.UseReflection);
            }
            else
            {
                Writer.Write("string ");
                Writer.Write(methodName);
                Writer.Write("(");
                Writer.Write(mapping.TypeDesc.UseReflection ? "object" : fullTypeName);
                Writer.WriteLine(" v) {");
            }
            Writer.Indent++;
            Writer.WriteLine("string s = null;");
            ConstantMapping[] constants = mapping.Constants;

            if (constants.Length > 0)
            {
                Hashtable values = new Hashtable();
                if (mapping.TypeDesc.UseReflection)
                    Writer.WriteLine("switch (" + RaCodeGen.GetStringForEnumLongValue("v", mapping.TypeDesc.UseReflection) + " ){");
                else
                    Writer.WriteLine("switch (v) {");
                Writer.Indent++;
                for (int i = 0; i < constants.Length; i++)
                {
                    ConstantMapping c = constants[i];
                    if (values[c.Value] == null)
                    {
                        WriteEnumCase(fullTypeName, c, mapping.TypeDesc.UseReflection);
                        Writer.Write("s = ");
                        WriteQuotedCSharpString(c.XmlName);
                        Writer.WriteLine("; break;");
                        values.Add(c.Value, c.Value);
                    }
                }


                if (mapping.IsFlags)
                {
                    Writer.Write("default: s = FromEnum(");
                    Writer.Write(RaCodeGen.GetStringForEnumLongValue("v", mapping.TypeDesc.UseReflection));
                    Writer.Write(", new string[] {");
                    Writer.Indent++;
                    for (int i = 0; i < constants.Length; i++)
                    {
                        ConstantMapping c = constants[i];
                        if (i > 0)
                            Writer.WriteLine(", ");
                        WriteQuotedCSharpString(c.XmlName);
                    }
                    Writer.Write("}, new ");
                    Writer.Write(typeof(long).FullName);
                    Writer.Write("[] {");

                    for (int i = 0; i < constants.Length; i++)
                    {
                        ConstantMapping c = constants[i];
                        if (i > 0)
                            Writer.WriteLine(", ");
                        Writer.Write("(long)");
                        if (mapping.TypeDesc.UseReflection)
                            Writer.Write(c.Value.ToString(CultureInfo.InvariantCulture));
                        else
                        {
                            Writer.Write(fullTypeName);
                            Writer.Write(".@");
                            CodeIdentifier.CheckValidIdentifier(c.Name);
                            Writer.Write(c.Name);
                        }
                    }
                    Writer.Indent--;
                    Writer.Write("}, ");
                    WriteQuotedCSharpString(mapping.TypeDesc.FullName);
                    Writer.WriteLine("); break;");
                }
                else
                {
                    Writer.Write("default: throw CreateInvalidEnumValueException(");
                    Writer.Write(RaCodeGen.GetStringForEnumLongValue("v", mapping.TypeDesc.UseReflection));
                    Writer.Write(".ToString(System.Globalization.CultureInfo.InvariantCulture), ");
                    WriteQuotedCSharpString(mapping.TypeDesc.FullName);
                    Writer.WriteLine(");");
                }
                Writer.Indent--;
                Writer.WriteLine("}");
            }
            if (mapping.IsSoap)
            {
                Writer.Write("WriteXsiType(");
                WriteQuotedCSharpString(mapping.TypeName);
                Writer.Write(", ");
                WriteQuotedCSharpString(mapping.Namespace);
                Writer.WriteLine(");");
                Writer.WriteLine("Writer.WriteString(s);");
            }
            else
            {
                Writer.WriteLine("return s;");
            }
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteDerivedTypes(StructMapping mapping)
        {
            for (StructMapping derived = mapping.DerivedMappings; derived != null; derived = derived.NextDerivedMapping)
            {
                string fullTypeName = derived.TypeDesc.CSharpName;
                Writer.Write("else if (");
                WriteTypeCompare("t", fullTypeName, derived.TypeDesc.UseReflection);
                Writer.WriteLine(") {");
                Writer.Indent++;

                string methodName = ReferenceMapping(derived);

#if DEBUG
                    // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                    if (methodName == null) throw new InvalidOperationException("derived from " + mapping.TypeDesc.FullName + ", " + SR.Format(SR.XmlInternalErrorMethod, derived.TypeDesc.Name) + Environment.StackTrace);
#endif

                Writer.Write(methodName);
                Writer.Write("(n, ns,");
                if (!derived.TypeDesc.UseReflection) Writer.Write("(" + fullTypeName + ")");
                Writer.Write("o");
                if (derived.TypeDesc.IsNullable)
                    Writer.Write(", isNullable");
                Writer.Write(", true");
                Writer.WriteLine(");");
                Writer.WriteLine("return;");
                Writer.Indent--;
                Writer.WriteLine("}");

                WriteDerivedTypes(derived);
            }
        }

        private void WriteEnumAndArrayTypes()
        {
            foreach (TypeScope scope in Scopes)
            {
                foreach (Mapping m in scope.TypeMappings)
                {
                    if (m is EnumMapping && !m.IsSoap)
                    {
                        EnumMapping mapping = (EnumMapping)m;
                        string fullTypeName = mapping.TypeDesc.CSharpName;
                        Writer.Write("else if (");
                        WriteTypeCompare("t", fullTypeName, mapping.TypeDesc.UseReflection);
                        Writer.WriteLine(") {");
                        Writer.Indent++;

                        string methodName = ReferenceMapping(mapping);

#if DEBUG
                            // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                            if (methodName == null) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorMethod, mapping.TypeDesc.Name) + Environment.StackTrace);
#endif
                        Writer.WriteLine("Writer.WriteStartElement(n, ns);");
                        Writer.Write("WriteXsiType(");
                        WriteQuotedCSharpString(mapping.TypeName);
                        Writer.Write(", ");
                        WriteQuotedCSharpString(mapping.Namespace);
                        Writer.WriteLine(");");
                        Writer.Write("Writer.WriteString(");
                        Writer.Write(methodName);
                        Writer.Write("(");
                        if (!mapping.TypeDesc.UseReflection) Writer.Write("(" + fullTypeName + ")");
                        Writer.WriteLine("o));");
                        Writer.WriteLine("Writer.WriteEndElement();");
                        Writer.WriteLine("return;");
                        Writer.Indent--;
                        Writer.WriteLine("}");
                    }
                    else if (m is ArrayMapping && !m.IsSoap)
                    {
                        ArrayMapping mapping = m as ArrayMapping;
                        if (mapping == null || m.IsSoap) continue;
                        string fullTypeName = mapping.TypeDesc.CSharpName;
                        Writer.Write("else if (");
                        if (mapping.TypeDesc.IsArray)
                            WriteArrayTypeCompare("t", fullTypeName, mapping.TypeDesc.ArrayElementTypeDesc.CSharpName, mapping.TypeDesc.UseReflection);
                        else
                            WriteTypeCompare("t", fullTypeName, mapping.TypeDesc.UseReflection);
                        Writer.WriteLine(") {");
                        Writer.Indent++;

                        Writer.WriteLine("Writer.WriteStartElement(n, ns);");
                        Writer.Write("WriteXsiType(");
                        WriteQuotedCSharpString(mapping.TypeName);
                        Writer.Write(", ");
                        WriteQuotedCSharpString(mapping.Namespace);
                        Writer.WriteLine(");");

                        WriteMember("o", null, mapping.ElementsSortedByDerivation, null, null, mapping.TypeDesc, true);

                        Writer.WriteLine("Writer.WriteEndElement();");
                        Writer.WriteLine("return;");
                        Writer.Indent--;
                        Writer.WriteLine("}");
                    }
                }
            }
        }

        private void WriteStructMethod(StructMapping mapping)
        {
            if (mapping.IsSoap && mapping.TypeDesc.IsRoot) return;
            string methodName = (string)MethodNames[mapping];

            Writer.WriteLine();
            Writer.Write("void ");
            Writer.Write(methodName);

            string fullTypeName = mapping.TypeDesc.CSharpName;

            if (mapping.IsSoap)
            {
                Writer.WriteLine("(object s) {");
                Writer.Indent++;
                WriteLocalDecl(fullTypeName, "o", "s", mapping.TypeDesc.UseReflection);
            }
            else
            {
                Writer.Write("(string n, string ns, ");
                Writer.Write(mapping.TypeDesc.UseReflection ? "object" : fullTypeName);
                Writer.Write(" o");
                if (mapping.TypeDesc.IsNullable)
                    Writer.Write(", bool isNullable");
                Writer.WriteLine(", bool needType) {");
                Writer.Indent++;
                if (mapping.TypeDesc.IsNullable)
                {
                    Writer.WriteLine("if ((object)o == null) {");
                    Writer.Indent++;
                    Writer.WriteLine("if (isNullable) WriteNullTagLiteral(n, ns);");
                    Writer.WriteLine("return;");
                    Writer.Indent--;
                    Writer.WriteLine("}");
                }
                Writer.WriteLine("if (!needType) {");
                Writer.Indent++;

                Writer.Write(typeof(Type).FullName);
                Writer.WriteLine(" t = o.GetType();");
                Writer.Write("if (");
                WriteTypeCompare("t", fullTypeName, mapping.TypeDesc.UseReflection);
                Writer.WriteLine(") {");
                Writer.WriteLine("}");
                WriteDerivedTypes(mapping);
                if (mapping.TypeDesc.IsRoot)
                    WriteEnumAndArrayTypes();
                Writer.WriteLine("else {");

                Writer.Indent++;
                if (mapping.TypeDesc.IsRoot)
                {
                    Writer.WriteLine("WriteTypedPrimitive(n, ns, o, true);");
                    Writer.WriteLine("return;");
                }
                else
                {
                    Writer.WriteLine("throw CreateUnknownTypeException(o);");
                }
                Writer.Indent--;
                Writer.WriteLine("}");
                Writer.Indent--;
                Writer.WriteLine("}");
            }

            if (!mapping.TypeDesc.IsAbstract)
            {
                if (mapping.TypeDesc.Type != null && typeof(XmlSchemaObject).IsAssignableFrom(mapping.TypeDesc.Type))
                {
                    Writer.WriteLine("EscapeName = false;");
                }

                string xmlnsSource = null;
                MemberMapping[] members = TypeScope.GetAllMembers(mapping);
                int xmlnsMember = FindXmlnsIndex(members);
                if (xmlnsMember >= 0)
                {
                    MemberMapping member = members[xmlnsMember];
                    CodeIdentifier.CheckValidIdentifier(member.Name);
                    xmlnsSource = RaCodeGen.GetStringForMember("o", member.Name, mapping.TypeDesc);
                    if (mapping.TypeDesc.UseReflection)
                    {
                        xmlnsSource = "((" + member.TypeDesc.CSharpName + ")" + xmlnsSource + ")";
                    }
                }

                if (!mapping.IsSoap)
                {
                    Writer.Write("WriteStartElement(n, ns, o, false, ");
                    if (xmlnsSource == null)
                        Writer.Write("null");
                    else
                        Writer.Write(xmlnsSource);

                    Writer.WriteLine(");");
                    if (!mapping.TypeDesc.IsRoot)
                    {
                        Writer.Write("if (needType) WriteXsiType(");
                        WriteQuotedCSharpString(mapping.TypeName);
                        Writer.Write(", ");
                        WriteQuotedCSharpString(mapping.Namespace);
                        Writer.WriteLine(");");
                    }
                }
                else if (xmlnsSource != null)
                {
                    WriteNamespaces(xmlnsSource);
                }
                for (int i = 0; i < members.Length; i++)
                {
                    MemberMapping m = members[i];
                    if (m.Attribute != null)
                    {
                        CodeIdentifier.CheckValidIdentifier(m.Name);
                        if (m.CheckShouldPersist)
                        {
                            Writer.Write("if (");
                            string methodInvoke = RaCodeGen.GetStringForMethodInvoke("o", fullTypeName, "ShouldSerialize" + m.Name, mapping.TypeDesc.UseReflection);
                            if (mapping.TypeDesc.UseReflection) methodInvoke = "((" + typeof(bool).FullName + ")" + methodInvoke + ")";
                            Writer.Write(methodInvoke);
                            Writer.WriteLine(") {");
                            Writer.Indent++;
                        }
                        if (m.CheckSpecified != SpecifiedAccessor.None)
                        {
                            Writer.Write("if (");
                            string memberGet = RaCodeGen.GetStringForMember("o", m.Name + "Specified", mapping.TypeDesc);
                            if (mapping.TypeDesc.UseReflection) memberGet = "((" + typeof(bool).FullName + ")" + memberGet + ")";
                            Writer.Write(memberGet);
                            Writer.WriteLine(") {");
                            Writer.Indent++;
                        }
                        WriteMember(RaCodeGen.GetStringForMember("o", m.Name, mapping.TypeDesc), m.Attribute, m.TypeDesc, "o");

                        if (m.CheckSpecified != SpecifiedAccessor.None)
                        {
                            Writer.Indent--;
                            Writer.WriteLine("}");
                        }
                        if (m.CheckShouldPersist)
                        {
                            Writer.Indent--;
                            Writer.WriteLine("}");
                        }
                    }
                }

                for (int i = 0; i < members.Length; i++)
                {
                    MemberMapping m = members[i];
                    if (m.Xmlns != null)
                        continue;
                    CodeIdentifier.CheckValidIdentifier(m.Name);
                    bool checkShouldPersist = m.CheckShouldPersist && (m.Elements.Length > 0 || m.Text != null);

                    if (checkShouldPersist)
                    {
                        Writer.Write("if (");
                        string methodInvoke = RaCodeGen.GetStringForMethodInvoke("o", fullTypeName, "ShouldSerialize" + m.Name, mapping.TypeDesc.UseReflection);
                        if (mapping.TypeDesc.UseReflection) methodInvoke = "((" + typeof(bool).FullName + ")" + methodInvoke + ")";
                        Writer.Write(methodInvoke);
                        Writer.WriteLine(") {");
                        Writer.Indent++;
                    }
                    if (m.CheckSpecified != SpecifiedAccessor.None)
                    {
                        Writer.Write("if (");
                        string memberGet = RaCodeGen.GetStringForMember("o", m.Name + "Specified", mapping.TypeDesc);
                        if (mapping.TypeDesc.UseReflection) memberGet = "((" + typeof(bool).FullName + ")" + memberGet + ")";
                        Writer.Write(memberGet);
                        Writer.WriteLine(") {");
                        Writer.Indent++;
                    }

                    string choiceSource = null;
                    if (m.ChoiceIdentifier != null)
                    {
                        CodeIdentifier.CheckValidIdentifier(m.ChoiceIdentifier.MemberName);
                        choiceSource = RaCodeGen.GetStringForMember("o", m.ChoiceIdentifier.MemberName, mapping.TypeDesc);
                    }
                    WriteMember(RaCodeGen.GetStringForMember("o", m.Name, mapping.TypeDesc), choiceSource, m.ElementsSortedByDerivation, m.Text, m.ChoiceIdentifier, m.TypeDesc, true);

                    if (m.CheckSpecified != SpecifiedAccessor.None)
                    {
                        Writer.Indent--;
                        Writer.WriteLine("}");
                    }
                    if (checkShouldPersist)
                    {
                        Writer.Indent--;
                        Writer.WriteLine("}");
                    }
                }
                if (!mapping.IsSoap)
                {
                    WriteEndElement("o");
                }
            }
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private bool CanOptimizeWriteListSequence(TypeDesc listElementTypeDesc)
        {
            // check to see if we can write values of the attribute sequentially
            // currently we have only one data type (XmlQualifiedName) that we can not write "inline", 
            // because we need to output xmlns:qx="..." for each of the qnames

            return (listElementTypeDesc != null && listElementTypeDesc != QnameTypeDesc);
        }

        private void WriteMember(string source, AttributeAccessor attribute, TypeDesc memberTypeDesc, string parent)
        {
            if (memberTypeDesc.IsAbstract) return;
            if (memberTypeDesc.IsArrayLike)
            {
                Writer.WriteLine("{");
                Writer.Indent++;
                string fullTypeName = memberTypeDesc.CSharpName;
                WriteArrayLocalDecl(fullTypeName, "a", source, memberTypeDesc);
                if (memberTypeDesc.IsNullable)
                {
                    Writer.WriteLine("if (a != null) {");
                    Writer.Indent++;
                }
                if (attribute.IsList)
                {
                    if (CanOptimizeWriteListSequence(memberTypeDesc.ArrayElementTypeDesc))
                    {
                        Writer.Write("Writer.WriteStartAttribute(null, ");
                        WriteQuotedCSharpString(attribute.Name);
                        Writer.Write(", ");
                        string ns = attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : String.Empty;
                        if (ns != null)
                        {
                            WriteQuotedCSharpString(ns);
                        }
                        else
                        {
                            Writer.Write("null");
                        }
                        Writer.WriteLine(");");
                    }
                    else
                    {
                        Writer.Write(typeof(StringBuilder).FullName);
                        Writer.Write(" sb = new ");
                        Writer.Write(typeof(StringBuilder).FullName);
                        Writer.WriteLine("();");
                    }
                }
                TypeDesc arrayElementTypeDesc = memberTypeDesc.ArrayElementTypeDesc;

                if (memberTypeDesc.IsEnumerable)
                {
                    Writer.Write(" e = ");
                    Writer.Write(typeof(IEnumerator).FullName);
                    if (memberTypeDesc.IsPrivateImplementation)
                    {
                        Writer.Write("((");
                        Writer.Write(typeof(IEnumerable).FullName);
                        Writer.WriteLine(").GetEnumerator();");
                    }
                    else if (memberTypeDesc.IsGenericInterface)
                    {
                        if (memberTypeDesc.UseReflection)
                        {
                            // we use wildcard method name for generic GetEnumerator method, so we cannot use GetStringForMethodInvoke call here
                            Writer.Write("(");
                            Writer.Write(typeof(IEnumerator).FullName);
                            Writer.Write(")");
                            Writer.Write(RaCodeGen.GetReflectionVariable(memberTypeDesc.CSharpName, "System.Collections.Generic.IEnumerable*"));
                            Writer.WriteLine(".Invoke(a, new object[0]);");
                        }
                        else
                        {
                            Writer.Write("((System.Collections.Generic.IEnumerable<");
                            Writer.Write(arrayElementTypeDesc.CSharpName);
                            Writer.WriteLine(">)a).GetEnumerator();");
                        }
                    }
                    else
                    {
                        if (memberTypeDesc.UseReflection)
                        {
                            Writer.Write("(");
                            Writer.Write(typeof(IEnumerator).FullName);
                            Writer.Write(")");
                        }
                        Writer.Write(RaCodeGen.GetStringForMethodInvoke("a", memberTypeDesc.CSharpName, "GetEnumerator", memberTypeDesc.UseReflection));
                        Writer.WriteLine(";");
                    }
                    Writer.WriteLine("if (e != null)");
                    Writer.WriteLine("while (e.MoveNext()) {");
                    Writer.Indent++;

                    string arrayTypeFullName = arrayElementTypeDesc.CSharpName;
                    WriteLocalDecl(arrayTypeFullName, "ai", "e.Current", arrayElementTypeDesc.UseReflection);
                }
                else
                {
                    Writer.Write("for (int i = 0; i < ");
                    if (memberTypeDesc.IsArray)
                    {
                        Writer.WriteLine("a.Length; i++) {");
                    }
                    else
                    {
                        Writer.Write("((");
                        Writer.Write(typeof(ICollection).FullName);
                        Writer.WriteLine(")a).Count; i++) {");
                    }
                    Writer.Indent++;
                    string arrayTypeFullName = arrayElementTypeDesc.CSharpName;
                    WriteLocalDecl(arrayTypeFullName, "ai", RaCodeGen.GetStringForArrayMember("a", "i", memberTypeDesc), arrayElementTypeDesc.UseReflection);
                }
                if (attribute.IsList)
                {
                    // check to see if we can write values of the attribute sequentially
                    if (CanOptimizeWriteListSequence(memberTypeDesc.ArrayElementTypeDesc))
                    {
                        Writer.WriteLine("if (i != 0) Writer.WriteString(\" \");");
                        Writer.Write("WriteValue(");
                    }
                    else
                    {
                        Writer.WriteLine("if (i != 0) sb.Append(\" \");");
                        Writer.Write("sb.Append(");
                    }
                    if (attribute.Mapping is EnumMapping)
                        WriteEnumValue((EnumMapping)attribute.Mapping, "ai");
                    else
                        WritePrimitiveValue(arrayElementTypeDesc, "ai", true);
                    Writer.WriteLine(");");
                }
                else
                {
                    WriteAttribute("ai", attribute, parent);
                }
                Writer.Indent--;
                Writer.WriteLine("}");
                if (attribute.IsList)
                {
                    // check to see if we can write values of the attribute sequentially
                    if (CanOptimizeWriteListSequence(memberTypeDesc.ArrayElementTypeDesc))
                    {
                        Writer.WriteLine("Writer.WriteEndAttribute();");
                    }
                    else
                    {
                        Writer.WriteLine("if (sb.Length != 0) {");
                        Writer.Indent++;

                        Writer.Write("WriteAttribute(");
                        WriteQuotedCSharpString(attribute.Name);
                        Writer.Write(", ");
                        string ns = attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : String.Empty;
                        if (ns != null)
                        {
                            WriteQuotedCSharpString(ns);
                            Writer.Write(", ");
                        }
                        Writer.WriteLine("sb.ToString());");
                        Writer.Indent--;
                        Writer.WriteLine("}");
                    }
                }

                if (memberTypeDesc.IsNullable)
                {
                    Writer.Indent--;
                    Writer.WriteLine("}");
                }
                Writer.Indent--;
                Writer.WriteLine("}");
            }
            else
            {
                WriteAttribute(source, attribute, parent);
            }
        }

        private void WriteAttribute(string source, AttributeAccessor attribute, string parent)
        {
            if (attribute.Mapping is SpecialMapping)
            {
                SpecialMapping special = (SpecialMapping)attribute.Mapping;
                if (special.TypeDesc.Kind == TypeKind.Attribute || special.TypeDesc.CanBeAttributeValue)
                {
                    Writer.Write("WriteXmlAttribute(");
                    Writer.Write(source);
                    Writer.Write(", ");
                    Writer.Write(parent);
                    Writer.WriteLine(");");
                }
                else
                    throw new InvalidOperationException(SR.XmlInternalError);
            }
            else
            {
                TypeDesc typeDesc = attribute.Mapping.TypeDesc;
                if (!typeDesc.UseReflection) source = "((" + typeDesc.CSharpName + ")" + source + ")";
                WritePrimitive("WriteAttribute", attribute.Name, attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : "", attribute.Default, source, attribute.Mapping, false, false, false);
            }
        }

        private void WriteMember(string source, string choiceSource, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, TypeDesc memberTypeDesc, bool writeAccessors)
        {
            if (memberTypeDesc.IsArrayLike &&
                !(elements.Length == 1 && elements[0].Mapping is ArrayMapping))
                WriteArray(source, choiceSource, elements, text, choice, memberTypeDesc);
            else
                WriteElements(source, choiceSource, elements, text, choice, "a", writeAccessors, memberTypeDesc.IsNullable);
        }


        private void WriteArray(string source, string choiceSource, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, TypeDesc arrayTypeDesc)
        {
            if (elements.Length == 0 && text == null) return;
            Writer.WriteLine("{");
            Writer.Indent++;
            string arrayTypeName = arrayTypeDesc.CSharpName;
            WriteArrayLocalDecl(arrayTypeName, "a", source, arrayTypeDesc);
            if (arrayTypeDesc.IsNullable)
            {
                Writer.WriteLine("if (a != null) {");
                Writer.Indent++;
            }

            if (choice != null)
            {
                bool choiceUseReflection = choice.Mapping.TypeDesc.UseReflection;
                string choiceFullName = choice.Mapping.TypeDesc.CSharpName;
                WriteArrayLocalDecl(choiceFullName + "[]", "c", choiceSource, choice.Mapping.TypeDesc);
                // write check for the choice identifier array
                Writer.WriteLine("if (c == null || c.Length < a.Length) {");
                Writer.Indent++;
                Writer.Write("throw CreateInvalidChoiceIdentifierValueException(");
                WriteQuotedCSharpString(choice.Mapping.TypeDesc.FullName);
                Writer.Write(", ");
                WriteQuotedCSharpString(choice.MemberName);
                Writer.Write(");");
                Writer.Indent--;
                Writer.WriteLine("}");
            }

            WriteArrayItems(elements, text, choice, arrayTypeDesc, "a", "c");
            if (arrayTypeDesc.IsNullable)
            {
                Writer.Indent--;
                Writer.WriteLine("}");
            }
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteArrayItems(ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, TypeDesc arrayTypeDesc, string arrayName, string choiceName)
        {
            TypeDesc arrayElementTypeDesc = arrayTypeDesc.ArrayElementTypeDesc;

            if (arrayTypeDesc.IsEnumerable)
            {
                Writer.Write(typeof(IEnumerator).FullName);
                Writer.Write(" e = ");
                if (arrayTypeDesc.IsPrivateImplementation)
                {
                    Writer.Write("((");
                    Writer.Write(typeof(IEnumerable).FullName);
                    Writer.Write(")");
                    Writer.Write(arrayName);
                    Writer.WriteLine(").GetEnumerator();");
                }
                else if (arrayTypeDesc.IsGenericInterface)
                {
                    if (arrayTypeDesc.UseReflection)
                    {
                        // we use wildcard method name for generic GetEnumerator method, so we cannot use GetStringForMethodInvoke call here
                        Writer.Write("(");
                        Writer.Write(typeof(IEnumerator).FullName);
                        Writer.Write(")");
                        Writer.Write(RaCodeGen.GetReflectionVariable(arrayTypeDesc.CSharpName, "System.Collections.Generic.IEnumerable*"));
                        Writer.Write(".Invoke(");
                        Writer.Write(arrayName);
                        Writer.WriteLine(", new object[0]);");
                    }
                    else
                    {
                        Writer.Write("((System.Collections.Generic.IEnumerable<");
                        Writer.Write(arrayElementTypeDesc.CSharpName);
                        Writer.Write(">)");
                        Writer.Write(arrayName);
                        Writer.WriteLine(").GetEnumerator();");
                    }
                }
                else
                {
                    if (arrayTypeDesc.UseReflection)
                    {
                        Writer.Write("(");
                        Writer.Write(typeof(IEnumerator).FullName);
                        Writer.Write(")");
                    }
                    Writer.Write(RaCodeGen.GetStringForMethodInvoke(arrayName, arrayTypeDesc.CSharpName, "GetEnumerator", arrayTypeDesc.UseReflection));
                    Writer.WriteLine(";");
                }
                Writer.WriteLine("if (e != null)");
                Writer.WriteLine("while (e.MoveNext()) {");
                Writer.Indent++;
                string arrayTypeFullName = arrayElementTypeDesc.CSharpName;
                WriteLocalDecl(arrayTypeFullName, arrayName + "i", "e.Current", arrayElementTypeDesc.UseReflection);
                WriteElements(arrayName + "i", choiceName + "i", elements, text, choice, arrayName + "a", true, true);
            }
            else
            {
                Writer.Write("for (int i");
                Writer.Write(arrayName);
                Writer.Write(" = 0; i");
                Writer.Write(arrayName);
                Writer.Write(" < ");
                if (arrayTypeDesc.IsArray)
                {
                    Writer.Write(arrayName);
                    Writer.Write(".Length");
                }
                else
                {
                    Writer.Write("((");
                    Writer.Write(typeof(ICollection).FullName);
                    Writer.Write(")");
                    Writer.Write(arrayName);
                    Writer.Write(").Count");
                }
                Writer.Write("; i");
                Writer.Write(arrayName);
                Writer.WriteLine("++) {");
                Writer.Indent++;
                int count = elements.Length + (text == null ? 0 : 1);
                if (count > 1)
                {
                    string arrayTypeFullName = arrayElementTypeDesc.CSharpName;
                    WriteLocalDecl(arrayTypeFullName, arrayName + "i", RaCodeGen.GetStringForArrayMember(arrayName, "i" + arrayName, arrayTypeDesc), arrayElementTypeDesc.UseReflection);
                    if (choice != null)
                    {
                        string choiceFullName = choice.Mapping.TypeDesc.CSharpName;
                        WriteLocalDecl(choiceFullName, choiceName + "i", RaCodeGen.GetStringForArrayMember(choiceName, "i" + arrayName, choice.Mapping.TypeDesc), choice.Mapping.TypeDesc.UseReflection);
                    }
                    WriteElements(arrayName + "i", choiceName + "i", elements, text, choice, arrayName + "a", true, arrayElementTypeDesc.IsNullable);
                }
                else
                {
                    WriteElements(RaCodeGen.GetStringForArrayMember(arrayName, "i" + arrayName, arrayTypeDesc), elements, text, choice, arrayName + "a", true, arrayElementTypeDesc.IsNullable);
                }
            }
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteElements(string source, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, string arrayName, bool writeAccessors, bool isNullable)
        {
            WriteElements(source, null, elements, text, choice, arrayName, writeAccessors, isNullable);
        }

        private void WriteElements(string source, string enumSource, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, string arrayName, bool writeAccessors, bool isNullable)
        {
            if (elements.Length == 0 && text == null) return;
            if (elements.Length == 1 && text == null)
            {
                TypeDesc td = elements[0].IsUnbounded ? elements[0].Mapping.TypeDesc.CreateArrayTypeDesc() : elements[0].Mapping.TypeDesc;
                if (!elements[0].Any && !elements[0].Mapping.TypeDesc.UseReflection && !elements[0].Mapping.TypeDesc.IsOptionalValue)
                    source = "((" + td.CSharpName + ")" + source + ")";
                WriteElement(source, elements[0], arrayName, writeAccessors);
            }
            else
            {
                if (isNullable && choice == null)
                {
                    Writer.Write("if ((object)(");
                    Writer.Write(source);
                    Writer.Write(") != null)");
                }
                Writer.WriteLine("{");
                Writer.Indent++;
                int anyCount = 0;
                ArrayList namedAnys = new ArrayList();
                ElementAccessor unnamedAny = null; // can only have one
                bool wroteFirstIf = false;
                string enumTypeName = choice == null ? null : choice.Mapping.TypeDesc.FullName;

                for (int i = 0; i < elements.Length; i++)
                {
                    ElementAccessor element = elements[i];

                    if (element.Any)
                    {
                        anyCount++;
                        if (element.Name != null && element.Name.Length > 0)
                            namedAnys.Add(element);
                        else if (unnamedAny == null)
                            unnamedAny = element;
                    }
                    else if (choice != null)
                    {
                        bool useReflection = element.Mapping.TypeDesc.UseReflection;
                        string fullTypeName = element.Mapping.TypeDesc.CSharpName;
                        bool enumUseReflection = choice.Mapping.TypeDesc.UseReflection;
                        string enumFullName = (enumUseReflection ? "" : enumTypeName + ".@") + FindChoiceEnumValue(element, (EnumMapping)choice.Mapping, enumUseReflection);

                        if (wroteFirstIf) Writer.Write("else ");
                        else wroteFirstIf = true;
                        Writer.Write("if (");
                        Writer.Write(enumUseReflection ? RaCodeGen.GetStringForEnumLongValue(enumSource, enumUseReflection) : enumSource);
                        Writer.Write(" == ");
                        Writer.Write(enumFullName);
                        if (isNullable && !element.IsNullable)
                        {
                            Writer.Write(" && ((object)(");
                            Writer.Write(source);
                            Writer.Write(") != null)");
                        }
                        Writer.WriteLine(") {");
                        Writer.Indent++;

                        WriteChoiceTypeCheck(source, fullTypeName, useReflection, choice, enumFullName, element.Mapping.TypeDesc);

                        string castedSource = source;
                        if (!useReflection)
                            castedSource = "((" + fullTypeName + ")" + source + ")";
                        WriteElement(element.Any ? source : castedSource, element, arrayName, writeAccessors);
                        Writer.Indent--;
                        Writer.WriteLine("}");
                    }
                    else
                    {
                        bool useReflection = element.Mapping.TypeDesc.UseReflection;
                        TypeDesc td = element.IsUnbounded ? element.Mapping.TypeDesc.CreateArrayTypeDesc() : element.Mapping.TypeDesc;
                        string fullTypeName = td.CSharpName;
                        if (wroteFirstIf) Writer.Write("else ");
                        else wroteFirstIf = true;
                        Writer.Write("if (");
                        WriteInstanceOf(source, fullTypeName, useReflection);
                        Writer.WriteLine(") {");
                        Writer.Indent++;
                        string castedSource = source;
                        if (!useReflection)
                            castedSource = "((" + fullTypeName + ")" + source + ")";
                        WriteElement(element.Any ? source : castedSource, element, arrayName, writeAccessors);
                        Writer.Indent--;
                        Writer.WriteLine("}");
                    }
                }
                if (anyCount > 0)
                {
                    if (elements.Length - anyCount > 0) Writer.Write("else ");

                    string fullTypeName = typeof(XmlElement).FullName;

                    Writer.Write("if (");
                    Writer.Write(source);
                    Writer.Write(" is ");
                    Writer.Write(fullTypeName);
                    Writer.WriteLine(") {");
                    Writer.Indent++;

                    Writer.Write(fullTypeName);
                    Writer.Write(" elem = (");
                    Writer.Write(fullTypeName);
                    Writer.Write(")");
                    Writer.Write(source);
                    Writer.WriteLine(";");

                    int c = 0;

                    foreach (ElementAccessor element in namedAnys)
                    {
                        if (c++ > 0) Writer.Write("else ");

                        string enumFullName = null;

                        bool useReflection = element.Mapping.TypeDesc.UseReflection;
                        if (choice != null)
                        {
                            bool enumUseReflection = choice.Mapping.TypeDesc.UseReflection;
                            enumFullName = (enumUseReflection ? "" : enumTypeName + ".@") + FindChoiceEnumValue(element, (EnumMapping)choice.Mapping, enumUseReflection);
                            Writer.Write("if (");
                            Writer.Write(enumUseReflection ? RaCodeGen.GetStringForEnumLongValue(enumSource, enumUseReflection) : enumSource);
                            Writer.Write(" == ");
                            Writer.Write(enumFullName);
                            if (isNullable && !element.IsNullable)
                            {
                                Writer.Write(" && ((object)(");
                                Writer.Write(source);
                                Writer.Write(") != null)");
                            }
                            Writer.WriteLine(") {");
                            Writer.Indent++;
                        }
                        Writer.Write("if (elem.Name == ");
                        WriteQuotedCSharpString(element.Name);
                        Writer.Write(" && elem.NamespaceURI == ");
                        WriteQuotedCSharpString(element.Namespace);
                        Writer.WriteLine(") {");
                        Writer.Indent++;
                        WriteElement("elem", element, arrayName, writeAccessors);

                        if (choice != null)
                        {
                            Writer.Indent--;
                            Writer.WriteLine("}");
                            Writer.WriteLine("else {");
                            Writer.Indent++;

                            Writer.WriteLine("// throw Value '{0}' of the choice identifier '{1}' does not match element '{2}' from namespace '{3}'.");

                            Writer.Write("throw CreateChoiceIdentifierValueException(");
                            WriteQuotedCSharpString(enumFullName);
                            Writer.Write(", ");
                            WriteQuotedCSharpString(choice.MemberName);
                            Writer.WriteLine(", elem.Name, elem.NamespaceURI);");
                            Writer.Indent--;
                            Writer.WriteLine("}");
                        }
                        Writer.Indent--;
                        Writer.WriteLine("}");
                    }
                    if (c > 0)
                    {
                        Writer.WriteLine("else {");
                        Writer.Indent++;
                    }
                    if (unnamedAny != null)
                    {
                        WriteElement("elem", unnamedAny, arrayName, writeAccessors);
                    }
                    else
                    {
                        Writer.WriteLine("throw CreateUnknownAnyElementException(elem.Name, elem.NamespaceURI);");
                    }
                    if (c > 0)
                    {
                        Writer.Indent--;
                        Writer.WriteLine("}");
                    }
                    Writer.Indent--;
                    Writer.WriteLine("}");
                }
                if (text != null)
                {
                    bool useReflection = text.Mapping.TypeDesc.UseReflection;
                    string fullTypeName = text.Mapping.TypeDesc.CSharpName;
                    if (elements.Length > 0)
                    {
                        Writer.Write("else ");
                        Writer.Write("if (");
                        WriteInstanceOf(source, fullTypeName, useReflection);
                        Writer.WriteLine(") {");
                        Writer.Indent++;
                        string castedSource = source;
                        if (!useReflection)
                            castedSource = "((" + fullTypeName + ")" + source + ")";
                        WriteText(castedSource, text);
                        Writer.Indent--;
                        Writer.WriteLine("}");
                    }
                    else
                    {
                        string castedSource = source;
                        if (!useReflection)
                            castedSource = "((" + fullTypeName + ")" + source + ")";
                        WriteText(castedSource, text);
                    }
                }
                if (elements.Length > 0)
                {
                    Writer.Write("else ");

                    if (isNullable)
                    {
                        Writer.Write(" if ((object)(");
                        Writer.Write(source);
                        Writer.Write(") != null)");
                    }

                    Writer.WriteLine("{");
                    Writer.Indent++;

                    Writer.Write("throw CreateUnknownTypeException(");
                    Writer.Write(source);
                    Writer.WriteLine(");");

                    Writer.Indent--;
                    Writer.WriteLine("}");
                }
                Writer.Indent--;
                Writer.WriteLine("}");
            }
        }

        private void WriteText(string source, TextAccessor text)
        {
            if (text.Mapping is PrimitiveMapping)
            {
                PrimitiveMapping mapping = (PrimitiveMapping)text.Mapping;
                Writer.Write("WriteValue(");
                if (text.Mapping is EnumMapping)
                {
                    WriteEnumValue((EnumMapping)text.Mapping, source);
                }
                else
                {
                    WritePrimitiveValue(mapping.TypeDesc, source, false);
                }
                Writer.WriteLine(");");
            }
            else if (text.Mapping is SpecialMapping)
            {
                SpecialMapping mapping = (SpecialMapping)text.Mapping;
                switch (mapping.TypeDesc.Kind)
                {
                    case TypeKind.Node:
                        Writer.Write(source);
                        Writer.WriteLine(".WriteTo(Writer);");
                        break;
                    default:
                        throw new InvalidOperationException(SR.XmlInternalError);
                }
            }
        }

        private void WriteElement(string source, ElementAccessor element, string arrayName, bool writeAccessor)
        {
            string name = writeAccessor ? element.Name : element.Mapping.TypeName;
            string ns = element.Any && element.Name.Length == 0 ? null : (element.Form == XmlSchemaForm.Qualified ? (writeAccessor ? element.Namespace : element.Mapping.Namespace) : "");
            if (element.Mapping is NullableMapping)
            {
                Writer.Write("if (");
                Writer.Write(source);
                Writer.WriteLine(" != null) {");
                Writer.Indent++;
                string fullTypeName = element.Mapping.TypeDesc.BaseTypeDesc.CSharpName;
                string castedSource = source;
                if (!element.Mapping.TypeDesc.BaseTypeDesc.UseReflection)
                    castedSource = "((" + fullTypeName + ")" + source + ")";
                ElementAccessor e = element.Clone();
                e.Mapping = ((NullableMapping)element.Mapping).BaseMapping;
                WriteElement(e.Any ? source : castedSource, e, arrayName, writeAccessor);
                Writer.Indent--;
                Writer.WriteLine("}");
                if (element.IsNullable)
                {
                    Writer.WriteLine("else {");
                    Writer.Indent++;
                    WriteLiteralNullTag(element.Name, element.Form == XmlSchemaForm.Qualified ? element.Namespace : "");
                    Writer.Indent--;
                    Writer.WriteLine("}");
                }
            }
            else if (element.Mapping is ArrayMapping)
            {
                ArrayMapping mapping = (ArrayMapping)element.Mapping;
                if (mapping.IsSoap)
                {
                    Writer.Write("WritePotentiallyReferencingElement(");
                    WriteQuotedCSharpString(name);
                    Writer.Write(", ");
                    WriteQuotedCSharpString(ns);
                    Writer.Write(", ");
                    Writer.Write(source);
                    if (!writeAccessor)
                    {
                        Writer.Write(", ");
                        Writer.Write(RaCodeGen.GetStringForTypeof(mapping.TypeDesc.CSharpName, mapping.TypeDesc.UseReflection));
                        Writer.Write(", true, ");
                    }
                    else
                    {
                        Writer.Write(", null, false, ");
                    }
                    WriteValue(element.IsNullable);
                    Writer.WriteLine(");");
                }
                else if (element.IsUnbounded)
                {
                    TypeDesc td = mapping.TypeDesc.CreateArrayTypeDesc();
                    string fullTypeName = td.CSharpName;
                    string elementArrayName = "el" + arrayName;
                    string arrayIndex = "c" + elementArrayName;
                    Writer.WriteLine("{");
                    Writer.Indent++;
                    WriteArrayLocalDecl(fullTypeName, elementArrayName, source, mapping.TypeDesc);
                    if (element.IsNullable)
                    {
                        WriteNullCheckBegin(elementArrayName, element);
                    }
                    else
                    {
                        if (mapping.TypeDesc.IsNullable)
                        {
                            Writer.Write("if (");
                            Writer.Write(elementArrayName);
                            Writer.Write(" != null)");
                        }
                        Writer.WriteLine("{");
                        Writer.Indent++;
                    }

                    Writer.Write("for (int ");
                    Writer.Write(arrayIndex);
                    Writer.Write(" = 0; ");
                    Writer.Write(arrayIndex);
                    Writer.Write(" < ");

                    if (td.IsArray)
                    {
                        Writer.Write(elementArrayName);
                        Writer.Write(".Length");
                    }
                    else
                    {
                        Writer.Write("((");
                        Writer.Write(typeof(ICollection).FullName);
                        Writer.Write(")");
                        Writer.Write(elementArrayName);
                        Writer.Write(").Count");
                    }
                    Writer.Write("; ");
                    Writer.Write(arrayIndex);
                    Writer.WriteLine("++) {");
                    Writer.Indent++;

                    element.IsUnbounded = false;
                    WriteElement(elementArrayName + "[" + arrayIndex + "]", element, arrayName, writeAccessor);
                    element.IsUnbounded = true;

                    Writer.Indent--;
                    Writer.WriteLine("}");

                    Writer.Indent--;
                    Writer.WriteLine("}");
                    Writer.Indent--;
                    Writer.WriteLine("}");
                }
                else
                {
                    string fullTypeName = mapping.TypeDesc.CSharpName;
                    Writer.WriteLine("{");
                    Writer.Indent++;
                    WriteArrayLocalDecl(fullTypeName, arrayName, source, mapping.TypeDesc);
                    if (element.IsNullable)
                    {
                        WriteNullCheckBegin(arrayName, element);
                    }
                    else
                    {
                        if (mapping.TypeDesc.IsNullable)
                        {
                            Writer.Write("if (");
                            Writer.Write(arrayName);
                            Writer.Write(" != null)");
                        }
                        Writer.WriteLine("{");
                        Writer.Indent++;
                    }
                    WriteStartElement(name, ns, false);
                    WriteArrayItems(mapping.ElementsSortedByDerivation, null, null, mapping.TypeDesc, arrayName, null);
                    WriteEndElement();
                    Writer.Indent--;
                    Writer.WriteLine("}");
                    Writer.Indent--;
                    Writer.WriteLine("}");
                }
            }
            else if (element.Mapping is EnumMapping)
            {
                if (element.Mapping.IsSoap)
                {
                    string methodName = (string)MethodNames[element.Mapping];
                    Writer.Write("Writer.WriteStartElement(");
                    WriteQuotedCSharpString(name);
                    Writer.Write(", ");
                    WriteQuotedCSharpString(ns);
                    Writer.WriteLine(");");
                    Writer.Write(methodName);
                    Writer.Write("(");
                    Writer.Write(source);
                    Writer.WriteLine(");");
                    WriteEndElement();
                }
                else
                {
                    WritePrimitive("WriteElementString", name, ns, element.Default, source, element.Mapping, false, true, element.IsNullable);
                }
            }
            else if (element.Mapping is PrimitiveMapping)
            {
                PrimitiveMapping mapping = (PrimitiveMapping)element.Mapping;
                if (mapping.TypeDesc == QnameTypeDesc)
                    WriteQualifiedNameElement(name, ns, element.Default, source, element.IsNullable, mapping.IsSoap, mapping);
                else
                {
                    string suffixNullable = mapping.IsSoap ? "Encoded" : "Literal";
                    string suffixRaw = mapping.TypeDesc.XmlEncodingNotRequired ? "Raw" : "";
                    WritePrimitive(element.IsNullable ? ("WriteNullableString" + suffixNullable + suffixRaw) : ("WriteElementString" + suffixRaw),
                                   name, ns, element.Default, source, mapping, mapping.IsSoap, true, element.IsNullable);
                }
            }
            else if (element.Mapping is StructMapping)
            {
                StructMapping mapping = (StructMapping)element.Mapping;

                if (mapping.IsSoap)
                {
                    Writer.Write("WritePotentiallyReferencingElement(");
                    WriteQuotedCSharpString(name);
                    Writer.Write(", ");
                    WriteQuotedCSharpString(ns);
                    Writer.Write(", ");
                    Writer.Write(source);
                    if (!writeAccessor)
                    {
                        Writer.Write(", ");
                        Writer.Write(RaCodeGen.GetStringForTypeof(mapping.TypeDesc.CSharpName, mapping.TypeDesc.UseReflection));
                        Writer.Write(", true, ");
                    }
                    else
                    {
                        Writer.Write(", null, false, ");
                    }
                    WriteValue(element.IsNullable);
                }
                else
                {
                    string methodName = ReferenceMapping(mapping);

#if DEBUG
                        // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                        if (methodName == null) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorMethod, mapping.TypeDesc.Name) + Environment.StackTrace);
#endif
                    Writer.Write(methodName);
                    Writer.Write("(");
                    WriteQuotedCSharpString(name);
                    Writer.Write(", ");
                    if (ns == null)
                        Writer.Write("null");
                    else
                    {
                        WriteQuotedCSharpString(ns);
                    }
                    Writer.Write(", ");
                    Writer.Write(source);
                    if (mapping.TypeDesc.IsNullable)
                    {
                        Writer.Write(", ");
                        WriteValue(element.IsNullable);
                    }
                    Writer.Write(", false");
                }
                Writer.WriteLine(");");
            }
            else if (element.Mapping is SpecialMapping)
            {
                SpecialMapping mapping = (SpecialMapping)element.Mapping;
                bool useReflection = mapping.TypeDesc.UseReflection;
                TypeDesc td = mapping.TypeDesc;
                string fullTypeName = td.CSharpName;


                if (element.Mapping is SerializableMapping)
                {
                    WriteElementCall("WriteSerializable", typeof(IXmlSerializable), source, name, ns, element.IsNullable, !element.Any);
                }
                else
                {
                    // XmlNode, XmlElement
                    Writer.Write("if ((");
                    Writer.Write(source);
                    Writer.Write(") is ");
                    Writer.Write(typeof(XmlNode).FullName);
                    Writer.Write(" || ");
                    Writer.Write(source);
                    Writer.Write(" == null");
                    Writer.WriteLine(") {");
                    Writer.Indent++;

                    WriteElementCall("WriteElementLiteral", typeof(XmlNode), source, name, ns, element.IsNullable, element.Any);

                    Writer.Indent--;
                    Writer.WriteLine("}");
                    Writer.WriteLine("else {");
                    Writer.Indent++;

                    Writer.Write("throw CreateInvalidAnyTypeException(");
                    Writer.Write(source);
                    Writer.WriteLine(");");

                    Writer.Indent--;
                    Writer.WriteLine("}");
                }
            }
            else
            {
                throw new InvalidOperationException(SR.XmlInternalError);
            }
        }

        private void WriteElementCall(string func, Type cast, string source, string name, string ns, bool isNullable, bool isAny)
        {
            Writer.Write(func);
            Writer.Write("((");
            Writer.Write(cast.FullName);
            Writer.Write(")");
            Writer.Write(source);
            Writer.Write(", ");
            WriteQuotedCSharpString(name);
            Writer.Write(", ");
            WriteQuotedCSharpString(ns);
            Writer.Write(", ");
            WriteValue(isNullable);
            Writer.Write(", ");
            WriteValue(isAny);
            Writer.WriteLine(");");
        }

        private void WriteCheckDefault(string source, object value, bool isNullable)
        {
            Writer.Write("if (");

            if (value is string && ((string)value).Length == 0)
            {
                // special case for string compare
                Writer.Write("(");
                Writer.Write(source);
                if (isNullable)
                    Writer.Write(" == null) || (");
                else
                    Writer.Write(" != null) && (");
                Writer.Write(source);
                Writer.Write(".Length != 0)");
            }
            else
            {
                Writer.Write(source);
                Writer.Write(" != ");
                WriteValue(value);
            }
            Writer.Write(")");
        }

        private void WriteChoiceTypeCheck(string source, string fullTypeName, bool useReflection, ChoiceIdentifierAccessor choice, string enumName, TypeDesc typeDesc)
        {
            Writer.Write("if (((object)");
            Writer.Write(source);
            Writer.Write(") != null && !(");
            WriteInstanceOf(source, fullTypeName, useReflection);
            Writer.Write(")) throw CreateMismatchChoiceException(");
            WriteQuotedCSharpString(typeDesc.FullName);
            Writer.Write(", ");
            WriteQuotedCSharpString(choice.MemberName);
            Writer.Write(", ");
            WriteQuotedCSharpString(enumName);
            Writer.WriteLine(");");
        }

        private void WriteNullCheckBegin(string source, ElementAccessor element)
        {
            Writer.Write("if ((object)(");
            Writer.Write(source);
            Writer.WriteLine(") == null) {");
            Writer.Indent++;
            WriteLiteralNullTag(element.Name, element.Form == XmlSchemaForm.Qualified ? element.Namespace : "");
            Writer.Indent--;
            Writer.WriteLine("}");
            Writer.WriteLine("else {");
            Writer.Indent++;
        }

        private void WriteValue(object value)
        {
            if (value == null)
            {
                Writer.Write("null");
            }
            else
            {
                Type type = value.GetType();

                if (type == typeof(String))
                {
                    string s = (string)value;
                    WriteQuotedCSharpString(s);
                }
                else if (type == typeof(Char))
                {
                    Writer.Write('\'');
                    char ch = (char)value;
                    if (ch == '\'')
                        Writer.Write("\'");
                    else
                        Writer.Write(ch);
                    Writer.Write('\'');
                }
                else if (type == typeof(Int32))
                    Writer.Write(((Int32)value).ToString(null, NumberFormatInfo.InvariantInfo));
                else if (type == typeof(Double))
                    Writer.Write(((Double)value).ToString("R", NumberFormatInfo.InvariantInfo));
                else if (type == typeof(Boolean))
                    Writer.Write((bool)value ? "true" : "false");
                else if ((type == typeof(Int16)) || (type == typeof(Int64)) || (type == typeof(UInt16)) || (type == typeof(UInt32)) || (type == typeof(UInt64)) || (type == typeof(Byte)) || (type == typeof(SByte)))
                {
                    Writer.Write("(");
                    Writer.Write(type.FullName);
                    Writer.Write(")");
                    Writer.Write("(");
                    Writer.Write(Convert.ToString(value, NumberFormatInfo.InvariantInfo));
                    Writer.Write(")");
                }
                else if (type == typeof(Single))
                {
                    Writer.Write(((Single)value).ToString("R", NumberFormatInfo.InvariantInfo));
                    Writer.Write("f");
                }
                else if (type == typeof(Decimal))
                {
                    Writer.Write(((Decimal)value).ToString(null, NumberFormatInfo.InvariantInfo));
                    Writer.Write("m");
                }
                else if (type == typeof(DateTime))
                {
                    Writer.Write(" new ");
                    Writer.Write(type.FullName);
                    Writer.Write("(");
                    Writer.Write(((DateTime)value).Ticks.ToString(CultureInfo.InvariantCulture));
                    Writer.Write(")");
                }
                else
                {
                    if (type.IsEnum)
                    {
                        Writer.Write(((int)value).ToString(null, NumberFormatInfo.InvariantInfo));
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlUnsupportedDefaultType, type.FullName));
                    }
                }
            }
        }

        private void WriteNamespaces(string source)
        {
            Writer.Write("WriteNamespaceDeclarations(");
            Writer.Write(source);
            Writer.WriteLine(");");
        }

        private int FindXmlnsIndex(MemberMapping[] members)
        {
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i].Xmlns == null)
                    continue;
                return i;
            }
            return -1;
        }

        private void WriteExtraMembers(string loopStartSource, string loopEndSource)
        {
            Writer.Write("for (int i = ");
            Writer.Write(loopStartSource);
            Writer.Write("; i < ");
            Writer.Write(loopEndSource);
            Writer.WriteLine("; i++) {");
            Writer.Indent++;
            Writer.WriteLine("if (p[i] != null) {");
            Writer.Indent++;
            Writer.WriteLine("WritePotentiallyReferencingElement(null, null, p[i], p[i].GetType(), true, false);");
            Writer.Indent--;
            Writer.WriteLine("}");
            Writer.Indent--;
            Writer.WriteLine("}");
        }

        private void WriteLocalDecl(string typeName, string variableName, string initValue, bool useReflection)
        {
            RaCodeGen.WriteLocalDecl(typeName, variableName, initValue, useReflection);
        }

        private void WriteArrayLocalDecl(string typeName, string variableName, string initValue, TypeDesc arrayTypeDesc)
        {
            RaCodeGen.WriteArrayLocalDecl(typeName, variableName, initValue, arrayTypeDesc);
        }
        private void WriteTypeCompare(string variable, string escapedTypeName, bool useReflection)
        {
            RaCodeGen.WriteTypeCompare(variable, escapedTypeName, useReflection);
        }
        private void WriteInstanceOf(string source, string escapedTypeName, bool useReflection)
        {
            RaCodeGen.WriteInstanceOf(source, escapedTypeName, useReflection);
        }
        private void WriteArrayTypeCompare(string variable, string escapedTypeName, string elementTypeName, bool useReflection)
        {
            RaCodeGen.WriteArrayTypeCompare(variable, escapedTypeName, elementTypeName, useReflection);
        }

        private void WriteEnumCase(string fullTypeName, ConstantMapping c, bool useReflection)
        {
            RaCodeGen.WriteEnumCase(fullTypeName, c, useReflection);
        }

        private string FindChoiceEnumValue(ElementAccessor element, EnumMapping choiceMapping, bool useReflection)
        {
            string enumValue = null;

            for (int i = 0; i < choiceMapping.Constants.Length; i++)
            {
                string xmlName = choiceMapping.Constants[i].XmlName;

                if (element.Any && element.Name.Length == 0)
                {
                    if (xmlName == "##any:")
                    {
                        if (useReflection)
                            enumValue = choiceMapping.Constants[i].Value.ToString(CultureInfo.InvariantCulture);
                        else
                            enumValue = choiceMapping.Constants[i].Name;
                        break;
                    }
                    continue;
                }
                int colon = xmlName.LastIndexOf(':');
                string choiceNs = colon < 0 ? choiceMapping.Namespace : xmlName.Substring(0, colon);
                string choiceName = colon < 0 ? xmlName : xmlName.Substring(colon + 1);

                if (element.Name == choiceName)
                {
                    if ((element.Form == XmlSchemaForm.Unqualified && string.IsNullOrEmpty(choiceNs)) || element.Namespace == choiceNs)
                    {
                        if (useReflection)
                            enumValue = choiceMapping.Constants[i].Value.ToString(CultureInfo.InvariantCulture);
                        else
                            enumValue = choiceMapping.Constants[i].Name;
                        break;
                    }
                }
            }
            if (enumValue == null || enumValue.Length == 0)
            {
                if (element.Any && element.Name.Length == 0)
                {
                    // Type {0} is missing enumeration value '##any' for XmlAnyElementAttribute.
                    throw new InvalidOperationException(SR.Format(SR.XmlChoiceMissingAnyValue, choiceMapping.TypeDesc.FullName));
                }
                // Type {0} is missing value for '{1}'.
                throw new InvalidOperationException(SR.Format(SR.XmlChoiceMissingValue, choiceMapping.TypeDesc.FullName, element.Namespace + ":" + element.Name, element.Name, element.Namespace));
            }
            if (!useReflection)
                CodeIdentifier.CheckValidIdentifier(enumValue);
            return enumValue;
        }
    }
}
