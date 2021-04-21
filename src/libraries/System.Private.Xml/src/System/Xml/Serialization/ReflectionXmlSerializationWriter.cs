// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Xml.Schema;

namespace System.Xml.Serialization
{
    internal sealed class ReflectionXmlSerializationWriter : XmlSerializationWriter
    {
        private readonly XmlMapping _mapping;

        public ReflectionXmlSerializationWriter(XmlMapping xmlMapping, XmlWriter xmlWriter, XmlSerializerNamespaces namespaces, string? encodingStyle, string? id)
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

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        protected override void InitCallbacks()
        {
            TypeScope scope = _mapping.Scope!;
            foreach (TypeMapping mapping in scope.TypeMappings)
            {
                if (mapping.IsSoap &&
                    (mapping is StructMapping || mapping is EnumMapping) &&
                    !mapping.TypeDesc!.IsRoot)
                {
                    AddWriteCallback(
                        mapping.TypeDesc.Type!,
                        mapping.TypeName!,
                        mapping.Namespace,
                        CreateXmlSerializationWriteCallback(mapping, mapping.TypeName!, mapping.Namespace, mapping.TypeDesc.IsNullable)
                    );
                }
            }
        }

        [RequiresUnreferencedCode("calls WriteObjectOfTypeElement")]
        public void WriteObject(object? o)
        {
            XmlMapping xmlMapping = _mapping;
            if (xmlMapping is XmlTypeMapping xmlTypeMapping)
            {
                WriteObjectOfTypeElement(o, xmlTypeMapping);
            }
            else if (xmlMapping is XmlMembersMapping xmlMembersMapping)
            {
                GenerateMembersElement(o!, xmlMembersMapping);
            }
        }

        [RequiresUnreferencedCode("calls GenerateTypeElement")]
        private void WriteObjectOfTypeElement(object? o, XmlTypeMapping mapping)
        {
            GenerateTypeElement(o, mapping);
        }

        [RequiresUnreferencedCode("calls WriteReferencedElements")]
        private void GenerateTypeElement(object? o, XmlTypeMapping xmlMapping)
        {
            ElementAccessor element = xmlMapping.Accessor;
            TypeMapping mapping = element.Mapping!;

            WriteStartDocument();
            if (o == null)
            {
                string? ns = (element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty);
                if (element.IsNullable)
                {
                    if (mapping.IsSoap)
                    {
                        WriteNullTagEncoded(element.Name, ns);
                    }
                    else
                    {
                        WriteNullTagLiteral(element.Name, ns);
                    }
                }
                else
                {
                    WriteEmptyTag(element.Name, ns);
                }

                return;
            }

            if (!mapping.TypeDesc!.IsValueType && !mapping.TypeDesc.Type!.IsPrimitive)
            {
                TopLevelElement();
            }

            WriteMember(o, null, new ElementAccessor[] { element }, null, null, mapping.TypeDesc, !element.IsSoap);
            if (mapping.IsSoap)
            {
                WriteReferencedElements();
            }
        }

        [RequiresUnreferencedCode("calls WriteElements")]
        private void WriteMember(object? o, object? choiceSource, ElementAccessor[] elements, TextAccessor? text, ChoiceIdentifierAccessor? choice, TypeDesc memberTypeDesc, bool writeAccessors)
        {
            if (memberTypeDesc.IsArrayLike &&
                !(elements.Length == 1 && elements[0].Mapping is ArrayMapping))
            {
                WriteArray(o!, choiceSource, elements, text, choice, memberTypeDesc);
            }
            else
            {
                WriteElements(o, choiceSource, elements, text, choice, writeAccessors, memberTypeDesc.IsNullable);
            }
        }

        [RequiresUnreferencedCode("calls WriteArrayItems")]
        private void WriteArray(object o, object? choiceSource, ElementAccessor[] elements, TextAccessor? text, ChoiceIdentifierAccessor? choice, TypeDesc arrayTypeDesc)
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
                    throw CreateInvalidChoiceIdentifierValueException(choice.Mapping!.TypeDesc!.FullName, choice.MemberName!);
                }
            }

            WriteArrayItems(elements, text, choice, arrayTypeDesc, o);
        }

        [RequiresUnreferencedCode("calls WriteElements")]
        private void WriteArrayItems(ElementAccessor[] elements, TextAccessor? text, ChoiceIdentifierAccessor? choice, TypeDesc? arrayTypeDesc, object o)
        {
            var arr = o as IList;

            if (arr != null)
            {
                for (int i = 0; i < arr.Count; i++)
                {
                    object? ai = arr[i];
                    WriteElements(ai, null/*choiceName + "i"*/, elements, text, choice, true, true);
                }
            }
            else
            {
                var a = o as IEnumerable;
                Debug.Assert(a != null);

                IEnumerator e = a.GetEnumerator();
                if (e != null)
                {
                    while (e.MoveNext())
                    {
                        object ai = e.Current;
                        WriteElements(ai, null/*choiceName + "i"*/, elements, text, choice, true, true);
                    }
                }
            }
        }

        [RequiresUnreferencedCode("calls CreateUnknownTypeException")]
        private void WriteElements(object? o, object? enumSource, ElementAccessor[] elements, TextAccessor? text, ChoiceIdentifierAccessor? choice, bool writeAccessors, bool isNullable)
        {
            if (elements.Length == 0 && text == null)
                return;

            if (elements.Length == 1 && text == null)
            {
                WriteElement(o, elements[0], writeAccessors);
            }
            else
            {
                if (isNullable && choice == null && o == null)
                {
                    return;
                }

                int anyCount = 0;
                var namedAnys = new List<ElementAccessor>();
                ElementAccessor? unnamedAny = null; // can only have one

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
                        if (o != null && o.GetType() == element.Mapping!.TypeDesc!.Type)
                        {
                            WriteElement(o, element, writeAccessors);
                            return;
                        }
                    }
                    else
                    {
                        TypeDesc td = element.IsUnbounded ? element.Mapping!.TypeDesc!.CreateArrayTypeDesc() : element.Mapping!.TypeDesc!;
                        if (o!.GetType() == td.Type)
                        {
                            WriteElement(o, element, writeAccessors);
                            return;
                        }
                    }
                }

                if (anyCount > 0)
                {
                    if (o is XmlElement elem)
                    {
                        foreach (ElementAccessor element in namedAnys)
                        {
                            if (element.Name == elem.Name && element.Namespace == elem.NamespaceURI)
                            {
                                WriteElement(elem, element, writeAccessors);
                                return;
                            }
                        }

                        if (choice != null)
                        {
                            throw CreateChoiceIdentifierValueException(choice.Mapping!.TypeDesc!.FullName, choice.MemberName!, elem.Name, elem.NamespaceURI);
                        }

                        if (unnamedAny != null)
                        {
                            WriteElement(elem, unnamedAny, writeAccessors);
                            return;
                        }

                        throw CreateUnknownAnyElementException(elem.Name, elem.NamespaceURI);
                    }
                }

                if (text != null)
                {
                    WriteText(o!, text);
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
            if (text.Mapping is PrimitiveMapping primitiveMapping)
            {
                string? stringValue;
                if (text.Mapping is EnumMapping enumMapping)
                {
                    stringValue = WriteEnumMethod(enumMapping, o);
                }
                else
                {
                    if (!WritePrimitiveValue(primitiveMapping.TypeDesc!, o, false, out stringValue))
                    {
                        Debug.Assert(o is byte[]);
                    }
                }

                if (o is byte[] byteArray)
                {
                    WriteValue(byteArray);
                }
                else
                {
                    WriteValue(stringValue);
                }
            }
            else if (text.Mapping is SpecialMapping specialMapping)
            {
                switch (specialMapping.TypeDesc!.Kind)
                {
                    case TypeKind.Node:
                        ((XmlNode)o).WriteTo(Writer);
                        break;
                    default:
                        throw new InvalidOperationException(SR.XmlInternalError);
                }
            }
        }

        [RequiresUnreferencedCode("calls WritePotentiallyReferencingElement")]
        private void WriteElement(object? o, ElementAccessor element, bool writeAccessor)
        {
            string name = writeAccessor ? element.Name : element.Mapping!.TypeName!;
            string? ns = element.Any && element.Name.Length == 0 ? null : (element.Form == XmlSchemaForm.Qualified ? (writeAccessor ? element.Namespace : element.Mapping!.Namespace) : string.Empty);

            if (element.Mapping is NullableMapping nullableMapping)
            {
                if (o != null)
                {
                    ElementAccessor e = element.Clone();
                    e.Mapping = nullableMapping.BaseMapping;
                    WriteElement(o, e, writeAccessor);
                }
                else if (element.IsNullable)
                {
                    WriteNullTagLiteral(element.Name, ns);
                }
            }
            else if (element.Mapping is ArrayMapping)
            {
                var mapping = element.Mapping as ArrayMapping;
                if (element.IsNullable && o == null)
                {
                    WriteNullTagLiteral(element.Name, element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty);
                }
                else if (mapping!.IsSoap)
                {
                    if (mapping.Elements == null || mapping.Elements.Length != 1)
                    {
                        throw new InvalidOperationException(SR.XmlInternalError);
                    }

                    if (!writeAccessor)
                    {
                        WritePotentiallyReferencingElement(name, ns, o, mapping.TypeDesc!.Type, true, element.IsNullable);
                    }
                    else
                    {
                        WritePotentiallyReferencingElement(name, ns, o, null, false, element.IsNullable);
                    }
                }
                else if (element.IsUnbounded)
                {
                    var enumerable = (IEnumerable)o!;
                    foreach (var e in enumerable)
                    {
                        element.IsUnbounded = false;
                        WriteElement(e, element, writeAccessor);
                        element.IsUnbounded = true;
                    }
                }
                else
                {
                    if (o != null)
                    {
                        WriteStartElement(name, ns, false);
                        WriteArrayItems(mapping.ElementsSortedByDerivation!, null, null, mapping.TypeDesc, o);
                        WriteEndElement();
                    }
                }
            }
            else if (element.Mapping is EnumMapping)
            {
                if (element.Mapping.IsSoap)
                {
                    Writer.WriteStartElement(name, ns);
                    WriteEnumMethod((EnumMapping)element.Mapping, o!);
                    WriteEndElement();
                }
                else
                {
                    WritePrimitive(WritePrimitiveMethodRequirement.WriteElementString, name, ns!, element.Default, o!, element.Mapping, false, true, element.IsNullable);
                }
            }
            else if (element.Mapping is PrimitiveMapping)
            {
                var mapping = element.Mapping as PrimitiveMapping;
                if (mapping!.TypeDesc == ReflectionXmlSerializationReader.QnameTypeDesc)
                {
                    WriteQualifiedNameElement(name, ns!, element.Default, (XmlQualifiedName)o!, element.IsNullable, mapping.IsSoap, mapping);
                }
                else
                {
                    WritePrimitiveMethodRequirement suffixNullable = mapping.IsSoap ? WritePrimitiveMethodRequirement.Encoded : WritePrimitiveMethodRequirement.None;
                    WritePrimitiveMethodRequirement suffixRaw = mapping.TypeDesc!.XmlEncodingNotRequired ? WritePrimitiveMethodRequirement.Raw : WritePrimitiveMethodRequirement.None;
                    WritePrimitive(element.IsNullable
                        ? WritePrimitiveMethodRequirement.WriteNullableStringLiteral | suffixNullable | suffixRaw
                        : WritePrimitiveMethodRequirement.WriteElementString | suffixRaw,
                        name, ns!, element.Default, o!, mapping, mapping.IsSoap, true, element.IsNullable);
                }
            }
            else if (element.Mapping is StructMapping)
            {
                var mapping = element.Mapping as StructMapping;
                if (mapping!.IsSoap)
                {
                    WritePotentiallyReferencingElement(name, ns, o, !writeAccessor ? mapping.TypeDesc!.Type : null, !writeAccessor, element.IsNullable);
                }
                else
                {
                    WriteStructMethod(mapping, name, ns, o, element.IsNullable, needType: false);
                }
            }
            else if (element.Mapping is SpecialMapping)
            {
                if (element.Mapping is SerializableMapping)
                {
                    WriteSerializable((IXmlSerializable)o!, name, ns, element.IsNullable, !element.Any);
                }
                else
                {
                    // XmlNode, XmlElement
                    if (o is XmlNode node)
                    {
                        WriteElementLiteral(node, name, ns, element.IsNullable, element.Any);
                    }
                    else
                    {
                        throw CreateInvalidAnyTypeException(o!);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException(SR.XmlInternalError);
            }
        }

        [RequiresUnreferencedCode("calls WriteStructMethod")]
        private XmlSerializationWriteCallback CreateXmlSerializationWriteCallback(TypeMapping mapping, string name, string? ns, bool isNullable)
        {
            if (mapping is StructMapping structMapping)
            {
                return Wrapper;
                [RequiresUnreferencedCode("calls WriteStructMethod")]
                void Wrapper(object o)
                {
                    WriteStructMethod(structMapping, name, ns, o, isNullable, needType: false);
                }
            }
            else if (mapping is EnumMapping enumMapping)
            {
                return (o) =>
                {
                    WriteEnumMethod(enumMapping, o);
                };
            }
            else
            {
                throw new InvalidOperationException(SR.XmlInternalError);
            }
        }

        private void WriteQualifiedNameElement(string name, string ns, object? defaultValue, XmlQualifiedName o, bool nullable, bool isSoap, PrimitiveMapping mapping)
        {
            bool hasDefault = defaultValue != null && defaultValue != DBNull.Value && mapping.TypeDesc!.HasDefaultSupport;
            if (hasDefault && IsDefaultValue(mapping, o, defaultValue!, nullable))
                return;

            if (isSoap)
            {
                if (nullable)
                {
                    WriteNullableQualifiedNameEncoded(name, ns, o, new XmlQualifiedName(mapping.TypeName, mapping.Namespace));
                }
                else
                {
                    WriteElementQualifiedName(name, ns, o, new XmlQualifiedName(mapping.TypeName, mapping.Namespace));
                }
            }
            else
            {
                if (nullable)
                {
                    WriteNullableQualifiedNameLiteral(name, ns, o);
                }
                else
                {
                    WriteElementQualifiedName(name, ns, o);
                }
            }
        }

        [RequiresUnreferencedCode("calls WriteTypedPrimitive")]
        private void WriteStructMethod(StructMapping mapping, string n, string? ns, object? o, bool isNullable, bool needType)
        {
            if (mapping.IsSoap && mapping.TypeDesc!.IsRoot) return;

            if (!mapping.IsSoap)
            {
                if (o == null)
                {
                    if (isNullable) WriteNullTagLiteral(n, ns);
                    return;
                }

                if (!needType
                 && o.GetType() != mapping.TypeDesc!.Type)
                {
                    if (WriteDerivedTypes(mapping, n, ns, o, isNullable))
                    {
                        return;
                    }

                    if (mapping.TypeDesc.IsRoot)
                    {
                        if (WriteEnumAndArrayTypes(mapping, o, n!, ns))
                        {
                            return;
                        }

                        WriteTypedPrimitive(n, ns, o, true);
                        return;
                    }

                    throw CreateUnknownTypeException(o);
                }
            }

            if (!mapping.TypeDesc!.IsAbstract)
            {
                if (mapping.TypeDesc.Type != null && typeof(XmlSchemaObject).IsAssignableFrom(mapping.TypeDesc.Type))
                {
                    EscapeName = false;
                }

                XmlSerializerNamespaces? xmlnsSource = null;
                MemberMapping[] members = TypeScope.GetAllMembers(mapping);
                int xmlnsMember = FindXmlnsIndex(members);
                if (xmlnsMember >= 0)
                {
                    MemberMapping member = members[xmlnsMember];
                    xmlnsSource = (XmlSerializerNamespaces?)GetMemberValue(o!, member.Name);
                }

                if (!mapping.IsSoap)
                {
                    WriteStartElement(n, ns, o, false, xmlnsSource);

                    if (!mapping.TypeDesc.IsRoot)
                    {
                        if (needType)
                        {
                            WriteXsiType(mapping.TypeName!, mapping.Namespace);
                        }
                    }
                }
                else if (xmlnsSource != null)
                {
                    WriteNamespaceDeclarations(xmlnsSource);
                }

                for (int i = 0; i < members.Length; i++)
                {
                    MemberMapping m = members[i];

                    bool isSpecified = true;
                    bool shouldPersist = true;
                    if (m.CheckSpecified != SpecifiedAccessor.None)
                    {
                        string specifiedMemberName = m.Name + "Specified";
                        isSpecified = (bool)GetMemberValue(o!, specifiedMemberName)!;
                    }

                    if (m.CheckShouldPersist)
                    {
                        string methodInvoke = "ShouldSerialize" + m.Name;
                        MethodInfo method = o!.GetType().GetMethod(methodInvoke, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
                        shouldPersist = (bool)method.Invoke(o, Array.Empty<object>())!;
                    }

                    if (m.Attribute != null)
                    {
                        if (isSpecified && shouldPersist)
                        {
                            object? memberValue = GetMemberValue(o!, m.Name);
                            WriteMember(memberValue, m.Attribute, m.TypeDesc!, o);
                        }
                    }
                }

                for (int i = 0; i < members.Length; i++)
                {
                    MemberMapping m = members[i];

                    if (m.Xmlns != null)
                        continue;

                    bool isSpecified = true;
                    bool shouldPersist = true;
                    if (m.CheckSpecified != SpecifiedAccessor.None)
                    {
                        string specifiedMemberName = m.Name + "Specified";
                        isSpecified = (bool)GetMemberValue(o!, specifiedMemberName)!;
                    }

                    if (m.CheckShouldPersist)
                    {
                        string methodInvoke = "ShouldSerialize" + m.Name;
                        MethodInfo method = o!.GetType().GetMethod(methodInvoke, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
                        shouldPersist = (bool)method.Invoke(o, Array.Empty<object>())!;
                    }

                    bool checkShouldPersist = m.CheckShouldPersist && (m.Elements!.Length > 0 || m.Text != null);

                    if (!checkShouldPersist)
                    {
                        shouldPersist = true;
                    }

                    if (isSpecified && shouldPersist)
                    {
                        object? choiceSource = null;
                        if (m.ChoiceIdentifier != null)
                        {
                            choiceSource = GetMemberValue(o!, m.ChoiceIdentifier.MemberName!);
                        }

                        object? memberValue = GetMemberValue(o!, m.Name);
                        WriteMember(memberValue, choiceSource, m.ElementsSortedByDerivation!, m.Text, m.ChoiceIdentifier, m.TypeDesc!, true);
                    }
                }

                if (!mapping.IsSoap)
                {
                    WriteEndElement(o);
                }
            }
        }

        [RequiresUnreferencedCode("Calls GetType on object")]
        private object? GetMemberValue(object o, string memberName)
        {
            MemberInfo memberInfo = ReflectionXmlSerializationHelper.GetMember(o.GetType(), memberName);
            object? memberValue = GetMemberValue(o, memberInfo);
            return memberValue;
        }

        [RequiresUnreferencedCode("calls WriteMember")]
        private bool WriteEnumAndArrayTypes(StructMapping structMapping, object o, string n, string? ns)
        {
            if (o is Enum)
            {
                Writer.WriteStartElement(n, ns);

                EnumMapping? enumMapping = null;
                Type enumType = o.GetType();
                foreach (var m in _mapping.Scope!.TypeMappings)
                {
                    if (m is EnumMapping em && em.TypeDesc!.Type == enumType)
                    {
                        enumMapping = em;
                        break;
                    }
                }

                if (enumMapping == null)
                    throw new InvalidOperationException(SR.XmlInternalError);

                WriteXsiType(enumMapping.TypeName!, ns);
                Writer.WriteString(WriteEnumMethod(enumMapping, o));
                Writer.WriteEndElement();
                return true;
            }

            if (o is Array)
            {
                Writer.WriteStartElement(n, ns);
                ArrayMapping? arrayMapping = null;
                Type arrayType = o.GetType();
                foreach (var m in _mapping.Scope!.TypeMappings)
                {
                    if (m is ArrayMapping am && am.TypeDesc!.Type == arrayType)
                    {
                        arrayMapping = am;
                        break;
                    }
                }

                if (arrayMapping == null)
                    throw new InvalidOperationException(SR.XmlInternalError);

                WriteXsiType(arrayMapping.TypeName!, ns);
                WriteMember(o, null, arrayMapping.ElementsSortedByDerivation!, null, null, arrayMapping.TypeDesc!, true);
                Writer.WriteEndElement();

                return true;
            }

            return false;
        }

        private string? WriteEnumMethod(EnumMapping mapping, object v)
        {
            string? returnString = null;
            if (mapping != null)
            {
                ConstantMapping[] constants = mapping.Constants!;
                if (constants.Length > 0)
                {
                    bool foundValue = false;
                    var enumValue = Convert.ToInt64(v);
                    for (int i = 0; i < constants.Length; i++)
                    {
                        ConstantMapping c = constants[i];
                        if (enumValue == c.Value)
                        {
                            returnString = c.XmlName;
                            foundValue = true;
                            break;
                        }
                    }

                    if (!foundValue)
                    {
                        if (mapping.IsFlags)
                        {
                            string[] xmlNames = new string[constants.Length];
                            long[] valueIds = new long[constants.Length];

                            for (int i = 0; i < constants.Length; i++)
                            {
                                xmlNames[i] = constants[i].XmlName;
                                valueIds[i] = constants[i].Value;
                            }

                            returnString = FromEnum(enumValue, xmlNames, valueIds);
                        }
                        else
                        {
                            throw CreateInvalidEnumValueException(v, mapping.TypeDesc!.FullName);
                        }
                    }
                }
            }
            else
            {
                returnString = v.ToString();
            }

            if (mapping!.IsSoap)
            {
                WriteXsiType(mapping.TypeName!, mapping.Namespace);
                Writer.WriteString(returnString);
                return null;
            }
            else
            {
                return returnString;
            }
        }

        private object? GetMemberValue(object? o, MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo memberProperty)
            {
                return memberProperty.GetValue(o);
            }
            else if (memberInfo is FieldInfo memberField)
            {
                return memberField.GetValue(o);
            }

            throw new InvalidOperationException(SR.XmlInternalError);
        }

        private void WriteMember(object? memberValue, AttributeAccessor attribute, TypeDesc memberTypeDesc, object? container)
        {
            if (memberTypeDesc.IsAbstract) return;
            if (memberTypeDesc.IsArrayLike)
            {
                var sb = new StringBuilder();
                TypeDesc? arrayElementTypeDesc = memberTypeDesc.ArrayElementTypeDesc;
                bool canOptimizeWriteListSequence = CanOptimizeWriteListSequence(arrayElementTypeDesc);
                if (attribute.IsList)
                {
                    if (canOptimizeWriteListSequence)
                    {
                        Writer.WriteStartAttribute(null, attribute.Name, attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : string.Empty);
                    }
                }

                if (memberValue != null)
                {
                    var a = (IEnumerable)memberValue;
                    IEnumerator e = a.GetEnumerator();
                    bool shouldAppendWhitespace = false;
                    if (e != null)
                    {
                        while (e.MoveNext())
                        {
                            object ai = e.Current;

                            if (attribute.IsList)
                            {
                                string? stringValue;
                                if (attribute.Mapping is EnumMapping enumMapping)
                                {
                                    stringValue = WriteEnumMethod(enumMapping, ai);
                                }
                                else
                                {
                                    if (!WritePrimitiveValue(arrayElementTypeDesc!, ai, true, out stringValue))
                                    {
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
                                        sb.Append(' ');
                                    }

                                    sb.Append(stringValue);
                                }
                            }
                            else
                            {
                                WriteAttribute(ai, attribute, container);
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
                                    string? ns = attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : string.Empty;
                                    WriteAttribute(attribute.Name, ns, sb.ToString());
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                WriteAttribute(memberValue!, attribute, container);
            }
        }

        private bool CanOptimizeWriteListSequence(TypeDesc? listElementTypeDesc)
        {
            // check to see if we can write values of the attribute sequentially
            // currently we have only one data type (XmlQualifiedName) that we can not write "inline",
            // because we need to output xmlns:qx="..." for each of the qnames
            return (listElementTypeDesc != null && listElementTypeDesc != ReflectionXmlSerializationReader.QnameTypeDesc);
        }

        private void WriteAttribute(object memberValue, AttributeAccessor attribute, object? container)
        {
            // TODO: this block is never hit by our tests.
            if (attribute.Mapping is SpecialMapping special)
            {
                if (special.TypeDesc!.Kind == TypeKind.Attribute || special.TypeDesc.CanBeAttributeValue)
                {
                    WriteXmlAttribute((XmlNode)memberValue, container);
                }
                else
                {
                    throw new InvalidOperationException(SR.XmlInternalError);
                }
            }
            else
            {
                string? ns = attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : string.Empty;
                WritePrimitive(WritePrimitiveMethodRequirement.WriteAttribute, attribute.Name, ns, attribute.Default, memberValue, attribute.Mapping!, false, false, false);
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

        [RequiresUnreferencedCode("calls WriteStructMethod")]
        private bool WriteDerivedTypes(StructMapping mapping, string n, string? ns, object o, bool isNullable)
        {
            Type t = o.GetType();
            for (StructMapping? derived = mapping.DerivedMappings; derived != null; derived = derived.NextDerivedMapping)
            {
                if (t == derived.TypeDesc!.Type)
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

        private void WritePrimitive(WritePrimitiveMethodRequirement method, string name, string? ns, object? defaultValue, object o, TypeMapping mapping, bool writeXsiType, bool isElement, bool isNullable)
        {
            TypeDesc typeDesc = mapping.TypeDesc!;
            bool hasDefault = defaultValue != null && defaultValue != DBNull.Value && mapping.TypeDesc!.HasDefaultSupport;
            if (hasDefault)
            {
                if (mapping is EnumMapping)
                {
                    if (((EnumMapping)mapping).IsFlags)
                    {
                        IEnumerable<string> defaultEnumFlagValues = defaultValue!.ToString()!.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                        string defaultEnumFlagString = string.Join(", ", defaultEnumFlagValues);

                        if (o.ToString() == defaultEnumFlagString)
                            return;
                    }
                    else
                    {
                        if (o.ToString() == defaultValue!.ToString())
                            return;
                    }
                }
                else
                {
                    if (IsDefaultValue(mapping, o, defaultValue!, isNullable))
                    {
                        return;
                    }
                }
            }

            XmlQualifiedName? xmlQualifiedName = null;
            if (writeXsiType)
            {
                xmlQualifiedName = new XmlQualifiedName(mapping.TypeName, mapping.Namespace);
            }

            string? stringValue = null;
            bool hasValidStringValue = false;
            if (mapping is EnumMapping enumMapping)
            {
                stringValue = WriteEnumMethod(enumMapping, o);
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
                        WriteElementStringRaw(name, ns, stringValue, xmlQualifiedName);
                    }
                    else
                    {
                        WriteElementString(name, ns, stringValue, xmlQualifiedName);
                    }
                }

                else if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteNullableStringLiteral))
                {
                    if (hasRequirement(method, WritePrimitiveMethodRequirement.Encoded))
                    {
                        if (hasRequirement(method, WritePrimitiveMethodRequirement.Raw))
                        {
                            WriteNullableStringEncodedRaw(name, ns, stringValue, xmlQualifiedName);
                        }
                        else
                        {
                            WriteNullableStringEncoded(name, ns, stringValue, xmlQualifiedName);
                        }
                    }
                    else
                    {
                        if (hasRequirement(method, WritePrimitiveMethodRequirement.Raw))
                        {
                            WriteNullableStringLiteralRaw(name, ns, stringValue);
                        }
                        else
                        {
                            WriteNullableStringLiteral(name, ns, stringValue);
                        }
                    }
                }
                else if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteAttribute))
                {
                    WriteAttribute(name, ns, stringValue);
                }
                else
                {
                    Debug.Fail("https://github.com/dotnet/runtime/issues/18037: Add More Tests for Serialization Code");
                }
            }
            else if (o is byte[] a)
            {
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
                    WriteAttribute(name, ns!, a);
                }
                else
                {
                    Debug.Fail("https://github.com/dotnet/runtime/issues/18037: Add More Tests for Serialization Code");
                }
            }
            else
            {
                Debug.Fail("https://github.com/dotnet/runtime/issues/18037: Add More Tests for Serialization Code");
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

        private bool WritePrimitiveValue(TypeDesc typeDesc, object? o, bool isElement, out string? stringValue)
        {
            if (typeDesc == ReflectionXmlSerializationReader.StringTypeDesc || typeDesc.FormatterName == "String")
            {
                stringValue = (string?)o;
                return true;
            }
            else
            {
                if (!typeDesc.HasCustomFormatter)
                {
                    stringValue = ConvertPrimitiveToString(o!, typeDesc);
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
                else if (typeDesc == ReflectionXmlSerializationReader.QnameTypeDesc)
                {
                    stringValue = FromXmlQualifiedName((XmlQualifiedName?)o);
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
                    throw new InvalidOperationException(SR.XmlInternalError);
                }
            }

            stringValue = null;
            return false;
        }

        private string ConvertPrimitiveToString(object o, TypeDesc typeDesc)
        {
            string stringValue = typeDesc.FormatterName switch
            {
                "Boolean" => XmlConvert.ToString((bool)o),
                "Int32" => XmlConvert.ToString((int)o),
                "Int16" => XmlConvert.ToString((short)o),
                "Int64" => XmlConvert.ToString((long)o),
                "Single" => XmlConvert.ToString((float)o),
                "Double" => XmlConvert.ToString((double)o),
                "Decimal" => XmlConvert.ToString((decimal)o),
                "Byte" => XmlConvert.ToString((byte)o),
                "SByte" => XmlConvert.ToString((sbyte)o),
                "UInt16" => XmlConvert.ToString((ushort)o),
                "UInt32" => XmlConvert.ToString((uint)o),
                "UInt64" => XmlConvert.ToString((ulong)o),
                // Types without direct mapping (ambiguous)
                "Guid" => XmlConvert.ToString((Guid)o),
                "Char" => XmlConvert.ToString((char)o),
                "TimeSpan" => XmlConvert.ToString((TimeSpan)o),
                _ => o.ToString()!,
            };
            return stringValue;
        }

        [RequiresUnreferencedCode("calls WritePotentiallyReferencingElement")]
        private void GenerateMembersElement(object o, XmlMembersMapping xmlMembersMapping)
        {
            ElementAccessor element = xmlMembersMapping.Accessor;
            MembersMapping mapping = (MembersMapping)element.Mapping!;
            bool hasWrapperElement = mapping.HasWrapperElement;
            bool writeAccessors = mapping.WriteAccessors;
            bool isRpc = xmlMembersMapping.IsSoap && writeAccessors;

            WriteStartDocument();

            if (!mapping.IsSoap)
            {
                TopLevelElement();
            }

            object[] p = (object[])o;
            int pLength = p.Length;

            if (hasWrapperElement)
            {
                WriteStartElement(element.Name, (element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty), mapping.IsSoap);

                int xmlnsMember = FindXmlnsIndex(mapping.Members!);
                if (xmlnsMember >= 0)
                {
                    var source = (XmlSerializerNamespaces)p[xmlnsMember];

                    if (pLength > xmlnsMember)
                    {
                        WriteNamespaceDeclarations(source);
                    }
                }

                for (int i = 0; i < mapping.Members!.Length; i++)
                {
                    MemberMapping member = mapping.Members[i];
                    if (member.Attribute != null && !member.Ignore)
                    {
                        object source = p[i];
                        bool? specifiedSource = null;
                        if (member.CheckSpecified != SpecifiedAccessor.None)
                        {
                            string memberNameSpecified = member.Name + "Specified";
                            for (int j = 0; j < Math.Min(pLength, mapping.Members.Length); j++)
                            {
                                if (mapping.Members[j].Name == memberNameSpecified)
                                {
                                    specifiedSource = (bool)p[j];
                                    break;
                                }
                            }
                        }

                        if (pLength > i && (specifiedSource == null || specifiedSource.Value))
                        {
                            WriteMember(source, member.Attribute, member.TypeDesc!, null);
                        }
                    }
                }
            }

            for (int i = 0; i < mapping.Members!.Length; i++)
            {
                MemberMapping member = mapping.Members[i];
                if (member.Xmlns != null)
                    continue;

                if (member.Ignore)
                    continue;

                bool? specifiedSource = null;
                if (member.CheckSpecified != SpecifiedAccessor.None)
                {
                    string memberNameSpecified = member.Name + "Specified";
                    for (int j = 0; j < Math.Min(pLength, mapping.Members.Length); j++)
                    {
                        if (mapping.Members[j].Name == memberNameSpecified)
                        {
                            specifiedSource = (bool)p[j];
                            break;
                        }
                    }
                }

                if (pLength > i)
                {
                    if (specifiedSource == null || specifiedSource.Value)
                    {

                        object source = p[i];
                        object? enumSource = null;
                        if (member.ChoiceIdentifier != null)
                        {
                            for (int j = 0; j < mapping.Members.Length; j++)
                            {
                                if (mapping.Members[j].Name == member.ChoiceIdentifier.MemberName)
                                {
                                    enumSource = p[j];
                                    break;
                                }
                            }
                        }

                        if (isRpc && member.IsReturnValue && member.Elements!.Length > 0)
                        {
                            WriteRpcResult(member.Elements[0].Name, string.Empty);
                        }

                        // override writeAccessors choice when we've written a wrapper element
                        WriteMember(source, enumSource, member.ElementsSortedByDerivation!, member.Text, member.ChoiceIdentifier, member.TypeDesc!, writeAccessors || hasWrapperElement);
                    }
                }
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
                    if (pLength > mapping.Members.Length)
                    {
                        for (int i = mapping.Members.Length; i < pLength; i++)
                        {
                            if (p[i] != null)
                            {
                                WritePotentiallyReferencingElement(null, null, p[i], p[i].GetType(), true, false);
                            }
                        }
                    }
                }

                WriteReferencedElements();
            }
        }

        [Flags]
        private enum WritePrimitiveMethodRequirement
        {
            None = 0,
            Raw = 1,
            WriteAttribute = 2,
            WriteElementString = 4,
            WriteNullableStringLiteral = 8,
            Encoded = 16
        }
    }

    internal static class ReflectionXmlSerializationHelper
    {
        [RequiresUnreferencedCode("Reflects over base members")]
        public static MemberInfo GetMember(Type declaringType, string memberName)
        {
            MemberInfo[] memberInfos = declaringType.GetMember(memberName);
            if (memberInfos == null || memberInfos.Length == 0)
            {
                bool foundMatchedMember = false;
                Type? currentType = declaringType.BaseType;
                while (currentType != null)
                {
                    memberInfos = currentType.GetMember(memberName);
                    if (memberInfos != null && memberInfos.Length != 0)
                    {
                        foundMatchedMember = true;
                        break;
                    }

                    currentType = currentType.BaseType;
                }

                if (!foundMatchedMember)
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, $"Could not find member named {memberName} of type {declaringType}"));
                }

                declaringType = currentType!;
            }

            MemberInfo memberInfo = memberInfos![0];
            if (memberInfos.Length != 1)
            {
                foreach (MemberInfo mi in memberInfos)
                {
                    if (declaringType == mi.DeclaringType)
                    {
                        memberInfo = mi;
                        break;
                    }
                }
            }

            return memberInfo;
        }
    }
}
