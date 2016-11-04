// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace System.Xml.Serialization
{
    internal class ReflectionXmlSerializationWriter : XmlSerializationWriter
    {
        private XmlMapping _mapping;

        internal static TypeDesc StringTypeDesc { get; private set; } = (new TypeScope()).GetTypeDesc(typeof(string));
        internal static TypeDesc QnameTypeDesc { get; private set; } = (new TypeScope()).GetTypeDesc(typeof(XmlQualifiedName));

        public ReflectionXmlSerializationWriter(XmlMapping xmlMapping, XmlWriter xmlWriter, XmlSerializerNamespaces namespaces, string encodingStyle, string id)
        {
            Init(xmlWriter, namespaces, encodingStyle, id, null);

            if (!xmlMapping.IsWriteable || !xmlMapping.GenerateSerializer)
            {
                throw new ArgumentException(SR.Format(SR.XmlInternalError, nameof(xmlMapping)));
            }

            if (xmlMapping is XmlTypeMapping || xmlMapping is XmlMembersMapping)
            {
                _mapping = xmlMapping;
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.XmlInternalError, nameof(xmlMapping)));
            }
        }

        public void WriteObject(object o)
        {
            XmlMapping xmlMapping = _mapping;
            if (xmlMapping is XmlTypeMapping)
            {
                WriteObjectOfTypeElement(o, (XmlTypeMapping)xmlMapping);
            }
            else if (xmlMapping is XmlMembersMapping)
            {
                WriteMembersElement(o, (XmlMembersMapping)xmlMapping);
            }
        }

        private void WriteObjectOfTypeElement(object o, XmlTypeMapping mapping)
        {
            GenerateTypeElement(o, mapping);
        }

        private void GenerateTypeElement(object o, XmlTypeMapping xmlMapping)
        {
            ElementAccessor element = xmlMapping.Accessor;
            TypeMapping mapping = element.Mapping;

            WriteStartDocument();
            if (o == null)
            {
                if (element.IsNullable)
                {
                    if (mapping.IsSoap)
                    {
                        throw new PlatformNotSupportedException();
                    }
                    else
                    {
                        WriteNullTagLiteral(element.Name, (element.Form == XmlSchemaForm.Qualified ? element.Namespace : ""));
                    }
                }
                else
                {
                    WriteEmptyTag(element.Name, (element.Form == XmlSchemaForm.Qualified ? element.Namespace : ""));
                }

                return;
            }

            if (!mapping.TypeDesc.IsValueType && !mapping.TypeDesc.Type.GetTypeInfo().IsPrimitive)
            {
                TopLevelElement();
            }

            WriteMember(o, null, new ElementAccessor[] { element }, null, null, mapping.TypeDesc, !element.IsSoap, xmlMapping);

            if (mapping.IsSoap)
            {
                throw new PlatformNotSupportedException();
            }
        }

        private void WriteMember(object o, object choiceSource, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, TypeDesc memberTypeDesc, bool writeAccessors, XmlMapping parentMapping = null)
        {
            if (memberTypeDesc.IsArrayLike &&
                !(elements.Length == 1 && elements[0].Mapping is ArrayMapping))
            {
                WriteArray(o, choiceSource, elements, text, choice, memberTypeDesc);
            }
            else
            {
                WriteElements(o, choiceSource, elements, text, choice, "a", writeAccessors, memberTypeDesc.IsNullable, parentMapping);
            }
        }

        private void WriteArray(object o, object choiceSource, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, TypeDesc arrayTypeDesc)
        {
            if (elements.Length == 0 && text == null)
            {
                return;
            }

            if (arrayTypeDesc.IsNullable && o == null)
            {
                return;
            }

            if (choice != null)
            {
                if (choiceSource == null || ((Array)choiceSource).Length < ((Array)o).Length)
                {
                    throw CreateInvalidChoiceIdentifierValueException(choice.Mapping.TypeDesc.FullName, choice.MemberName);
                }
            }

            WriteArrayItems(elements, text, choice, arrayTypeDesc, o);
        }

        private void WriteArrayItems(ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, TypeDesc arrayTypeDesc, object o)
        {
            TypeDesc arrayElementTypeDesc = arrayTypeDesc.ArrayElementTypeDesc;

            var a = o as IEnumerable;

            //  #10593: This assert may not be true. We need more tests for this method.
            Debug.Assert(a != null);

            var e = a.GetEnumerator();

            if (e != null)
            {
                while (e.MoveNext())
                {
                    object ai = e.Current;
                    WriteElements(ai, null/*choiceName + "i"*/, elements, text, choice, (string)null/*arrayName + "a"*/, true, true);
                }
            }
        }

        private void WriteElements(object o, object enumSource, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, string arrayName, bool writeAccessors, bool isNullable, XmlMapping parentMapping = null)
        {
            if (elements.Length == 0 && text == null) return;
            if (elements.Length == 1 && text == null)
            {
                WriteElement(o, elements[0], arrayName, writeAccessors, parentMapping);
            }
            else
            {
                if (isNullable && choice == null && o == null)
                {
                    return;
                }

                int anyCount = 0;
                var namedAnys = new List<ElementAccessor>();
                ElementAccessor unnamedAny = null; // can only have one
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
                        if (o != null && o.GetType() == element.Mapping.TypeDesc.Type)
                        {
                            WriteElement(o, element, arrayName, writeAccessors);
                            return;
                        }
                    }
                    else
                    {
                        TypeDesc td = element.IsUnbounded ? element.Mapping.TypeDesc.CreateArrayTypeDesc() : element.Mapping.TypeDesc;
                        if (o.GetType() == td.Type)
                        {
                            WriteElement(o, element, arrayName, writeAccessors);
                            return;
                        }
                    }
                }

                if (anyCount > 0)
                {
                    var elem = o as XmlElement;
                    if (elem != null)
                    {
                        foreach (ElementAccessor element in namedAnys)
                        {
                            if (element.Name == elem.Name && element.Namespace == elem.NamespaceURI)
                            {
                                WriteElement(elem, element, arrayName, writeAccessors);
                                return;
                            }
                        }

                        if (choice != null)
                        {
                            throw CreateChoiceIdentifierValueException(choice.Mapping.TypeDesc.FullName, choice.MemberName, elem.Name, elem.NamespaceURI);
                        }

                        if (unnamedAny != null)
                        {
                            WriteElement(elem, unnamedAny, arrayName, writeAccessors);
                            return;
                        }

                        throw CreateUnknownAnyElementException(elem.Name, elem.NamespaceURI);
                    }
                }

                if (text != null)
                {
                    bool useReflection = text.Mapping.TypeDesc.UseReflection;
                    string fullTypeName = text.Mapping.TypeDesc.CSharpName;
                    WriteText(o, text);
                    return;
                }

                if (elements.Length > 0 && o != null)
                {
                    throw CreateUnknownTypeException(o);
                }
            }
        }

        private void WriteText(object o, TextAccessor text)
        {
            if (text.Mapping is PrimitiveMapping)
            {
                PrimitiveMapping mapping = (PrimitiveMapping)text.Mapping;
                string stringValue;
                if (text.Mapping is EnumMapping)
                {
                    stringValue = WriteEnumMethod((EnumMapping)mapping, o);

                }
                else
                {
                    if (!WritePrimitiveValue(mapping.TypeDesc, o, false, out stringValue))
                    {
                        // #10593: Add More Tests for Serialization Code
                        Debug.Assert(o is byte[]);
                    }
                }

                if (o is byte[])
                {
                    WriteValue((byte[])o);
                }
                else
                {
                    WriteValue(stringValue);
                }
            }
            else if (text.Mapping is SpecialMapping)
            {
                SpecialMapping mapping = (SpecialMapping)text.Mapping;
                switch (mapping.TypeDesc.Kind)
                {
                    case TypeKind.Node:
                        ((XmlNode)o).WriteTo(Writer);
                        break;
                    default:
                        throw new InvalidOperationException(SR.Format(SR.XmlInternalError));
                }
            }
        }

        private void WriteElement(object o, ElementAccessor element, string arrayName, bool writeAccessor, XmlMapping parentMapping = null)
        {
            string name = writeAccessor ? element.Name : element.Mapping.TypeName;
            string ns = element.Any && element.Name.Length == 0 ? null : (element.Form == XmlSchemaForm.Qualified ? (writeAccessor ? element.Namespace : element.Mapping.Namespace) : "");

            if (element.Mapping is NullableMapping)
            {
                if (o != null)
                {
                    ElementAccessor e = element.Clone();
                    e.Mapping = ((NullableMapping)element.Mapping).BaseMapping;
                    WriteElement(o, e, arrayName, writeAccessor);
                }
                else if (element.IsNullable)
                {
                    WriteNullTagLiteral(element.Name, ns);
                }
            }
            else if (element.Mapping is ArrayMapping)
            {
                var mapping = (ArrayMapping)element.Mapping;
                if (mapping.IsSoap)
                {
                    throw new PlatformNotSupportedException();
                }

                if (element.IsNullable && o == null)
                {
                    WriteNullTagLiteral(element.Name, element.Form == XmlSchemaForm.Qualified ? element.Namespace : "");
                }
                else
                {
                    if (element.IsUnbounded)
                    {
                        TypeDesc arrayTypeDesc = mapping.TypeDesc.CreateArrayTypeDesc();

                        var enumerable = (IEnumerable)o;
                        foreach (var e in enumerable)
                        {
                            element.IsUnbounded = false;
                            WriteElement(e, element, arrayName, writeAccessor);
                            element.IsUnbounded = true;
                        }
                    }
                    else
                    {
                        if (o != null)
                        {
                            WriteStartElement(name, ns, false);
                            WriteArrayItems(mapping.ElementsSortedByDerivation, null, null, mapping.TypeDesc, o);
                            WriteEndElement();
                        }
                    }
                }
            }
            else if (element.Mapping is EnumMapping)
            {
                if (element.Mapping.IsSoap)
                {
                    throw new PlatformNotSupportedException();
                }
                else
                {
                    WritePrimitive(WritePrimitiveMethodRequirement.WriteElementString, name, ns, element.Default, o, element.Mapping, false, true, element.IsNullable);
                }
            }
            else if (element.Mapping is PrimitiveMapping)
            {
                PrimitiveMapping mapping = (PrimitiveMapping)element.Mapping;
                if (mapping.TypeDesc == QnameTypeDesc)
                {
                    WriteQualifiedNameElement(name, ns, element.Default, (XmlQualifiedName)o, element.IsNullable, mapping.IsSoap, mapping);
                }
                else
                {
                    if (mapping.IsSoap)
                    {
                        throw new PlatformNotSupportedException();
                    }

                    WritePrimitiveMethodRequirement suffixRaw = mapping.TypeDesc.XmlEncodingNotRequired ? WritePrimitiveMethodRequirement.Raw : WritePrimitiveMethodRequirement.None;
                    WritePrimitive(element.IsNullable
                        ? WritePrimitiveMethodRequirement.WriteNullableStringLiteral | suffixRaw
                        : WritePrimitiveMethodRequirement.WriteElementString | suffixRaw,
                        name, ns, element.Default, o, mapping, mapping.IsSoap, true, element.IsNullable);
                }
            }
            else if (element.Mapping is StructMapping)
            {
                var mapping = (StructMapping)element.Mapping;
                if (mapping.IsSoap)
                {
                    throw new PlatformNotSupportedException();
                }

                WriteStructMethod(mapping, name, ns, o, mapping.TypeDesc.IsNullable, needType: false, parentMapping: parentMapping);
            }
            else if (element.Mapping is SpecialMapping)
            {
                if (element.Mapping is SerializableMapping)
                {
                    WriteSerializable((IXmlSerializable)o, name, ns, element.IsNullable, !element.Any);
                }
                else
                {
                    // XmlNode, XmlElement
                    var node = o as XmlNode;
                    if (node != null)
                    {
                        WriteElementLiteral(node, name, ns, element.IsNullable, element.Any);
                    }
                    else
                    {
                        throw CreateInvalidAnyTypeException(o);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException(SR.XmlInternalError);
            }
        }

        private void WriteQualifiedNameElement(string name, string ns, object defaultValue, XmlQualifiedName o, bool nullable, bool isSoap, PrimitiveMapping mapping)
        {
            bool hasDefault = defaultValue != null && !Globals.IsDBNullValue(defaultValue) && mapping.TypeDesc.HasDefaultSupport;
            if (hasDefault && IsDefaultValue(mapping, o, defaultValue, nullable))
                return;

            if (isSoap)
            {
                throw new PlatformNotSupportedException();
            }

            if (nullable)
            {
                WriteNullableQualifiedNameLiteral(name, ns, o);
            }
            else
            {
                WriteElementQualifiedName(name, ns, o);
            }
        }

        private void WriteStructMethod(StructMapping mapping, string n, string ns, object o, bool isNullable, bool needType, XmlMapping parentMapping = null)
        {
            if (mapping.IsSoap && mapping.TypeDesc.IsRoot) return;

            if (mapping.IsSoap)
            {
                throw new PlatformNotSupportedException();
            }

            if (o == null)
            {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }

            if (!needType
             && o.GetType() != mapping.TypeDesc.Type)
            {
                if (WriteDerivedTypes(mapping, n, ns, o, isNullable))
                {
                    return;
                }

                if (mapping.TypeDesc.IsRoot)
                {
                    if (WriteEnumAndArrayTypes(mapping, o, n, ns, parentMapping))
                    {
                        return;
                    }

                    WriteTypedPrimitive(n, ns, o, true);
                    return;
                }

                throw CreateUnknownTypeException(o);
            }

            if (!mapping.TypeDesc.IsAbstract)
            {
                if (mapping.TypeDesc.Type != null && typeof(XmlSchemaObject).IsAssignableFrom(mapping.TypeDesc.Type))
                {
                    throw new PlatformNotSupportedException(typeof(XmlSchemaObject).ToString());
                }

                XmlSerializerNamespaces xmlnsSource = null;
                MemberMapping[] members = TypeScope.GetAllMembers(mapping);
                int xmlnsMember = FindXmlnsIndex(members);
                if (xmlnsMember >= 0)
                {
                    MemberMapping member = members[xmlnsMember];
                    xmlnsSource = (XmlSerializerNamespaces)GetMemberValue(o, member.Name);
                }

                if (mapping.IsSoap)
                {
                    throw new PlatformNotSupportedException();
                }

                WriteStartElement(n, ns, o, false, xmlnsSource);

                if (!mapping.TypeDesc.IsRoot)
                {
                    if (needType)
                    {
                        WriteXsiType(mapping.TypeName, mapping.Namespace);
                    }
                }

                for (int i = 0; i < members.Length; i++)
                {
                    MemberMapping m = members[i];
                    string memberName = m.Name;
                    object memberValue = GetMemberValue(o, memberName);

                    bool isSpecified = true;
                    bool shouldPersist = true;
                    if (m.CheckSpecified != SpecifiedAccessor.None)
                    {
                        string specifiedMemberName = m.Name + "Specified";
                        isSpecified = (bool)GetMemberValue(o, specifiedMemberName);
                    }

                    if (m.CheckShouldPersist)
                    {
                        string methodInvoke = "ShouldSerialize" + m.Name;
                        MethodInfo method = o.GetType().GetTypeInfo().GetDeclaredMethod(methodInvoke);
                        shouldPersist = (bool)method.Invoke(o, Array.Empty<object>());
                    }

                    if (m.Attribute != null)
                    {
                        if (isSpecified && shouldPersist)
                        {
                            WriteMember(memberValue, m.Attribute, m.TypeDesc, o);
                        }
                    }
                }

                for (int i = 0; i < members.Length; i++)
                {
                    MemberMapping m = members[i];
                    string memberName = m.Name;
                    object memberValue = GetMemberValue(o, memberName);

                    bool isSpecified = true;
                    bool shouldPersist = true;
                    if (m.CheckSpecified != SpecifiedAccessor.None)
                    {
                        string specifiedMemberName = m.Name + "Specified";
                        isSpecified = (bool)GetMemberValue(o, specifiedMemberName);
                    }

                    if (m.CheckShouldPersist)
                    {
                        string methodInvoke = "ShouldSerialize" + m.Name;
                        MethodInfo method = o.GetType().GetTypeInfo().GetDeclaredMethod(methodInvoke);
                        shouldPersist = (bool)method.Invoke(o, Array.Empty<object>());
                    }

                    if (m.Xmlns != null)
                        continue;
                    
                    bool checkShouldPersist = m.CheckShouldPersist && (m.Elements.Length > 0 || m.Text != null);

                    if (!checkShouldPersist)
                    {
                        shouldPersist = true;
                    }

                    object choiceSource = null;
                    if (m.ChoiceIdentifier != null)
                    {
                        choiceSource = GetMemberValue(o, m.ChoiceIdentifier.MemberName);
                    }

                    if (isSpecified && shouldPersist)
                    {
                        WriteMember(memberValue, choiceSource, m.ElementsSortedByDerivation, m.Text, m.ChoiceIdentifier, m.TypeDesc, true, parentMapping);
                    }
                }
                if (!mapping.IsSoap)
                {
                    WriteEndElement(o);
                }
            }

        }

        private object GetMemberValue(object o, string memberName)
        {
            MemberInfo[] memberInfos = o.GetType().GetMember(memberName);

            if (memberInfos == null)
            {
                throw new InvalidOperationException("cannot find member:" + memberName);
            }

            var memberInfo = memberInfos[0];
            object memberValue = GetMemberValue(o, memberInfo);
            return memberValue;
        }

        private bool WriteEnumAndArrayTypes(StructMapping structMapping, object o, string n, string ns, XmlMapping parentMapping)
        {
            if (o is Enum)
            {
                Writer.WriteStartElement(n, ns);

                EnumMapping enumMapping = null;
                Type enumType = o.GetType();
                foreach (var m in parentMapping.Scope.TypeMappings)
                {
                    var em = m as EnumMapping;
                    if (em != null && em.TypeDesc.Type == enumType)
                    {
                        enumMapping = em;
                        break;
                    }
                }

                Debug.Assert(enumMapping != null);

                WriteXsiType(enumMapping.TypeName, ns);
                Writer.WriteString(WriteEnumMethod(enumMapping, o));
                Writer.WriteEndElement();
                return true;
            }

            if (o is Array)
            {
                Debug.Assert(parentMapping != null);
                Writer.WriteStartElement(n, ns);
                ArrayMapping arrayMapping = null;
                Type arrayType = o.GetType();
                foreach (var m in parentMapping.Scope.TypeMappings)
                {
                    var am = m as ArrayMapping;
                    if (am != null && am.TypeDesc.Type == arrayType)
                    {
                        arrayMapping = am;
                        break;
                    }
                }

                Debug.Assert(arrayMapping != null);
                WriteXsiType(arrayMapping.TypeName, ns);
                WriteMember(o, null, arrayMapping.ElementsSortedByDerivation, null, null, arrayMapping.TypeDesc, true);
                Writer.WriteEndElement();

                return true;
            }

            return false;
        }

        private string WriteEnumMethod(EnumMapping mapping, object v)
        {
            if (mapping != null && mapping.IsSoap)
            {
                throw new PlatformNotSupportedException();
            }

            if (mapping != null && mapping.IsFlags)
            {
                Type type = mapping.TypeDesc.Type;

                List<string> valueStrings = new List<string>();
                List<long> valueIds = new List<long>();
                foreach (var value in Enum.GetValues(type))
                {
                    valueStrings.Add(value.ToString());
                    valueIds.Add(Convert.ToInt64(value));
                }

                return FromEnum(Convert.ToInt64(v), valueStrings.ToArray(), valueIds.ToArray());
            }
            else
            {
                return v.ToString();
            }
        }

        private object GetMemberValue(object o, MemberInfo memberInfo)
        {
            PropertyInfo memberProperty = memberInfo as PropertyInfo;
            if (memberProperty != null)
            {
                return memberProperty.GetValue(o);
            }

            FieldInfo memberField = memberInfo as FieldInfo;
            if (memberField != null)
            {
                return memberField.GetValue(o);
            }

            throw new InvalidOperationException();
        }

        private void WriteMember(object memberValue, AttributeAccessor attribute, TypeDesc memberTypeDesc, object parent)
        {
            if (memberTypeDesc.IsAbstract) return;
            if (memberTypeDesc.IsArrayLike)
            {
                var sb = new StringBuilder();
                TypeDesc arrayElementTypeDesc = memberTypeDesc.ArrayElementTypeDesc;
                bool canOptimizeWriteListSequence = CanOptimizeWriteListSequence(arrayElementTypeDesc);
                if (attribute.IsList)
                {
                    if (canOptimizeWriteListSequence)
                    {
                        Writer.WriteStartAttribute(null, attribute.Name, attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : String.Empty);
                    }
                }

                var a = memberValue as IEnumerable;

                // #10593: Add More Tests for Serialization Code
                Debug.Assert(a != null);

                var e = a.GetEnumerator();
                bool shouldAppendWhitespace = false;
                if (e != null)
                {
                    while (e.MoveNext())
                    {
                        object ai = e.Current;

                        if (attribute.IsList)
                        {
                            string stringValue;
                            if (attribute.Mapping is EnumMapping)
                            {
                                stringValue = WriteEnumMethod((EnumMapping)attribute.Mapping, ai);
                            }
                            else
                            {
                                if (!WritePrimitiveValue(arrayElementTypeDesc, ai, true, out stringValue))
                                {
                                    // #10593: Add More Tests for Serialization Code
                                    Debug.Assert(ai is byte[]);
                                }
                            }

                            // check to see if we can write values of the attribute sequentially
                            if (canOptimizeWriteListSequence)
                            {
                                if (shouldAppendWhitespace)
                                {
                                    Writer.WriteString(" ");
                                }

                                if (ai is byte[])
                                {
                                    WriteValue((byte[])ai);
                                }
                                else
                                {
                                    WriteValue(stringValue);
                                }
                            }
                            else
                            {
                                if (shouldAppendWhitespace)
                                {
                                    sb.Append(" ");
                                }

                                sb.Append(stringValue);
                            }
                        }
                        else
                        {
                            WriteAttribute(ai, attribute, parent);
                        }

                        shouldAppendWhitespace = true;
                    }

                    if (attribute.IsList)
                    {
                        // check to see if we can write values of the attribute sequentially
                        if (canOptimizeWriteListSequence)
                        {
                            Writer.WriteEndAttribute();
                        }
                        else
                        {
                            if (sb.Length != 0)
                            {
                                string ns = attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : String.Empty;
                                WriteAttribute(attribute.Name, ns, sb.ToString());
                            }
                        }
                    }
                }
            }
            else
            {
                WriteAttribute(memberValue, attribute, parent);
            }
        }

        bool CanOptimizeWriteListSequence(TypeDesc listElementTypeDesc) {
            // check to see if we can write values of the attribute sequentially
            // currently we have only one data type (XmlQualifiedName) that we can not write "inline", 
            // because we need to output xmlns:qx="..." for each of the qnames
            return (listElementTypeDesc != null && listElementTypeDesc != QnameTypeDesc);
        }

        private void WriteAttribute(object memberValue, AttributeAccessor attribute, object parent)
        {
            if (attribute.Mapping is SpecialMapping)
            {
                // TODO: this block is never hit by our tests.
                SpecialMapping special = (SpecialMapping)attribute.Mapping;
                if (special.TypeDesc.Kind == TypeKind.Attribute || special.TypeDesc.CanBeAttributeValue)
                {
                    WriteXmlAttribute((XmlNode)memberValue, parent);
                }
                else
                    throw new InvalidOperationException(SR.XmlInternalError);
            }
            else
            {
                string ns = attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : "";
                WritePrimitive(WritePrimitiveMethodRequirement.WriteAttribute, attribute.Name, ns, attribute.Default, memberValue, attribute.Mapping, false, false, false);
            }
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

        bool WriteDerivedTypes(StructMapping mapping, string n, string ns, object o, bool isNullable)
        {
            Type t = o.GetType();
            for (StructMapping derived = mapping.DerivedMappings; derived != null; derived = derived.NextDerivedMapping)
            {
                if (t == derived.TypeDesc.Type)
                {
                    WriteStructMethod(derived, n, ns, o, isNullable, needType: true);
                    return true;
                }

                if (WriteDerivedTypes(derived, n, ns, o, isNullable))
                {
                    return true;
                }
            }

            return false;
        }

        private void WritePrimitive(WritePrimitiveMethodRequirement method, string name, string ns, object defaultValue, object o, TypeMapping mapping, bool writeXsiType, bool isElement, bool isNullable)
        {
            TypeDesc typeDesc = mapping.TypeDesc;
            bool hasDefault = defaultValue != null && !Globals.IsDBNullValue(defaultValue) && mapping.TypeDesc.HasDefaultSupport;
            if (hasDefault)
            {
                if (mapping is EnumMapping)
                {
                    if (((EnumMapping)mapping).IsFlags)
                    {
                        var defaultEnumFlagValues = defaultValue.ToString().Split(null).Where((s) => !string.IsNullOrWhiteSpace(s));
                        string defaultEnumFlagString = string.Join(", ", defaultEnumFlagValues);

                        if (o.ToString() == defaultEnumFlagString)
                            return;
                    }
                    else
                    {
                        if (o.ToString() == defaultValue.ToString())
                            return;
                    }
                }
                else
                {
                    if (IsDefaultValue(mapping, o, defaultValue, isNullable))
                    {
                        return;
                    }
                }
            }

            XmlQualifiedName xmlQualifiedName = null;
            if (writeXsiType)
            {
                xmlQualifiedName = new XmlQualifiedName(mapping.TypeName, mapping.Namespace);
            }

            string stringValue = null;
            bool hasValidStringValue = false;
            if (mapping is EnumMapping)
            {
                stringValue = WriteEnumMethod((EnumMapping)mapping, o);
                hasValidStringValue = true;
            }
            else
            {
                hasValidStringValue = WritePrimitiveValue(typeDesc, o, isElement, out stringValue);
            }

            if (hasValidStringValue)
            {
                if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteElementString))
                {
                    if (hasRequirement(method, WritePrimitiveMethodRequirement.Raw))
                    {
                        WriteElementString(name, ns, stringValue, xmlQualifiedName);
                    }
                    else
                    {
                        WriteElementStringRaw(name, ns, stringValue, xmlQualifiedName);
                    }
                }

                else if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteNullableStringLiteral))
                {
                    if (hasRequirement(method, WritePrimitiveMethodRequirement.Raw))
                    {
                        WriteNullableStringLiteral(name, ns, stringValue);
                    }
                    else
                    {
                        WriteNullableStringLiteralRaw(name, ns, stringValue);
                    }
                }
                else if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteAttribute))
                {
                    WriteAttribute(name, ns, stringValue);
                }
                else
                {
                    // #10593: Add More Tests for Serialization Code
                    Debug.Assert(false);
                }
            }
            else if (o is byte[])
            {
                byte[] a = (byte[])o;
                if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteElementString | WritePrimitiveMethodRequirement.Raw))
                {
                    WriteElementStringRaw(name, ns, FromByteArrayBase64(a));
                }
                else if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteNullableStringLiteral | WritePrimitiveMethodRequirement.Raw))
                {
                    WriteNullableStringLiteralRaw(name, ns, FromByteArrayBase64(a));
                }
                else if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteAttribute))
                {
                    WriteAttribute(name, ns, a);
                }
                else
                {
                    // #10593: Add More Tests for Serialization Code
                    Debug.Assert(false);
                }
            }
            else
            {
                // #10593: Add More Tests for Serialization Code
                Debug.Assert(false);
            }
        }

        private bool hasRequirement(WritePrimitiveMethodRequirement value, WritePrimitiveMethodRequirement requirement)
        {
            return (value & requirement) == requirement;
        }

        private bool IsDefaultValue(TypeMapping mapping, object o, object value, bool isNullable)
        {
            if (value is string && ((string)value).Length == 0)
            {
                string str = (string)o;
                return str == null || str.Length == 0;
            }
            else
            {
                return value.Equals(o);
            }
        }

        private bool WritePrimitiveValue(TypeDesc typeDesc, object o, bool isElement, out string stringValue)
        {
            if (typeDesc == StringTypeDesc || typeDesc.FormatterName == "String")
            {
                stringValue = (string)o;
                return true;
            }
            else
            {
                if (!typeDesc.HasCustomFormatter)
                {
                    stringValue = CovertPrimitiveToString(o, typeDesc);
                    return true;
                }
                else if (o is byte[] && typeDesc.FormatterName == "ByteArrayHex")
                {
                    stringValue = FromByteArrayHex((byte[])o);
                    return true;
                }
                else if (o is DateTime)
                {
                    if (typeDesc.FormatterName == "DateTime")
                    {
                        stringValue = FromDateTime((DateTime)o);
                        return true;
                    }
                    else if (typeDesc.FormatterName == "Date")
                    {
                        stringValue = FromDate((DateTime)o);
                        return true;
                    }
                    else if (typeDesc.FormatterName == "Time")
                    {
                        stringValue = FromTime((DateTime)o);
                        return true;
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "Invalid DateTime"));
                    }
                }
                else if (typeDesc == QnameTypeDesc)
                {
                    stringValue = FromXmlQualifiedName((XmlQualifiedName)o);
                    return true;
                }
                else if (o is string)
                {
                    switch (typeDesc.FormatterName)
                    {
                        case "XmlName":
                            stringValue = FromXmlName((string)o);
                            break;
                        case "XmlNCName":
                            stringValue = FromXmlNCName((string)o);
                            break;
                        case "XmlNmToken":
                            stringValue = FromXmlNmToken((string)o);
                            break;
                        case "XmlNmTokens":
                            stringValue = FromXmlNmTokens((string)o);
                            break;
                        default:
                            stringValue = null;
                            return false;
                    }

                    return true;
                }
                else if (o is char && typeDesc.FormatterName == "Char")
                {
                    stringValue = FromChar((char)o);
                    return true;
                }
                else if (o is byte[])
                {
                    // we deal with byte[] specially in WritePrimitive()
                }
                else
                {
                    throw new InvalidOperationException("Unknown type's HasCustomFormatter=true");
                }
            }

            stringValue = null;
            return false;
        }

        private string CovertPrimitiveToString(object o, TypeDesc typeDesc)
        {
            string stringValue;
            switch (typeDesc.FormatterName)
            {
                case "Boolean":
                    stringValue = XmlConvert.ToString((bool)o);
                    break;
                case "Int32":
                    stringValue = XmlConvert.ToString((int)o);
                    break;
                case "Int16":
                    stringValue = XmlConvert.ToString((short)o);
                    break;
                case "Int64":
                    stringValue = XmlConvert.ToString((long)o);
                    break;
                case "Single":
                    stringValue = XmlConvert.ToString((float)o);
                    break;
                case "Double":
                    stringValue = XmlConvert.ToString((double)o);
                    break;
                case "Decimal":
                    stringValue = XmlConvert.ToString((decimal)o);
                    break;
                case "Byte":
                    stringValue = XmlConvert.ToString((byte)o);
                    break;
                case "SByte":
                    stringValue = XmlConvert.ToString((sbyte)o);
                    break;
                case "UInt16":
                    stringValue = XmlConvert.ToString((ushort)o);
                    break;
                case "UInt32":
                    stringValue = XmlConvert.ToString((uint)o);
                    break;
                case "UInt64":
                    stringValue = XmlConvert.ToString((ulong)o);
                    break;
                // Types without direct mapping (ambiguous)
                case "Guid":
                    stringValue = XmlConvert.ToString((Guid)o);
                    break;
                case "Char":
                    stringValue = XmlConvert.ToString((char)o);
                    break;
                case "TimeSpan":
                    stringValue = XmlConvert.ToString((TimeSpan)o);
                    break;
                default:
                    stringValue = o.ToString();
                    break;
            }

            return stringValue;
        }

        private void WriteMembersElement(object o, XmlMembersMapping mapping)
        {
            // #10675: we should implement this method. WCF is the major customer of the method
            // as WCF uses XmlReflectionImporter.ImportMembersMapping and generates special
            // serializers for OperationContracts.
            throw new NotImplementedException();
        }

        protected override void InitCallbacks()
        {
        }

        [Flags]
        enum WritePrimitiveMethodRequirement
        {
            None = 0,
            Raw = 1,
            WriteAttribute = 2,
            WriteElementString = 4,
            WriteNullableStringLiteral = 8,
        }
    }
}
