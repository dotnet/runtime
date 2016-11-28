// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Extensions;
using System.Xml.Schema;

namespace System.Xml.Serialization
{
    internal class ReflectionXmlSerializationReader : XmlSerializationReader
    {
        private static TypeDesc StringTypeDesc { get; set; } = (new TypeScope()).GetTypeDesc(typeof(string));
        private static TypeDesc QnameTypeDesc { get; set; } = (new TypeScope()).GetTypeDesc(typeof(XmlQualifiedName));

        private XmlMapping _mapping;

        public ReflectionXmlSerializationReader(XmlMapping mapping, XmlReader xmlReader, XmlDeserializationEvents events, string encodingStyle)
        {
            Init(xmlReader, events, encodingStyle, tempAssembly: null);
            _mapping = mapping;
        }

        protected override void InitCallbacks()
        {
        }

        protected override void InitIDs()
        {
        }

        public object ReadObject()
        {
            var xmlMapping = _mapping;
            if (!xmlMapping.IsReadable)
                return null;
            if (!xmlMapping.GenerateSerializer)
                throw new ArgumentException(SR.Format(SR.XmlInternalError, "xmlMapping"));

            if (xmlMapping is XmlTypeMapping)
                return GenerateTypeElement((XmlTypeMapping)xmlMapping);
            else if (xmlMapping is XmlMembersMapping)
                return GenerateMembersElement((XmlMembersMapping)xmlMapping);
            else
                throw new ArgumentException(SR.Format(SR.XmlInternalError, "xmlMapping"));
        }

        private object GenerateMembersElement(XmlMembersMapping xmlMapping)
        {
            // #10675: we should implement this method. WCF is the major customer of the method
            // as WCF uses XmlReflectionImporter.ImportMembersMapping and generates special
            // serializers for OperationContracts.
            throw new NotImplementedException();
        }

        private object GenerateTypeElement(XmlTypeMapping xmlTypeMapping)
        {
            ElementAccessor element = xmlTypeMapping.Accessor;
            TypeMapping mapping = element.Mapping;

            Reader.MoveToContent();

            MemberMapping member = new MemberMapping();
            member.TypeDesc = mapping.TypeDesc;
            member.Elements = new ElementAccessor[] { element };

            UnknownNodeAction elementElseAction = UnknownNodeAction.CreateUnknownNodeException;
            UnknownNodeAction elseAction = UnknownNodeAction.ReadUnknownNode;

            object o = null;
            Member tempMember = null;
            var currentMember = new Member(member);
            WriteMemberElements(ref o, null/*collectionMember*/, out tempMember, new Member[] { currentMember }, elementElseAction, elseAction, element.Any ? currentMember : null, null, xmlTypeMapping);

            return o;
        }

        private void WriteMemberElements(ref object o, CollectionMember collectionMember, out Member member, Member[] expectedMembers, UnknownNodeAction elementElseAction, UnknownNodeAction elseAction, Member anyElement, Member anyText, object masterObject = null)
        {
            if (Reader.NodeType == XmlNodeType.Element)
            {
                WriteMemberElementsIf(ref o, collectionMember, out member, expectedMembers, anyElement, elementElseAction, masterObject);
            }
            else if (anyText != null && anyText.Mapping != null && WriteMemberText(out o, anyText))
            {
                member = anyText;
            }
            else
            {
                member = null;
                ProcessUnknownNode(elseAction);
            }
        }

        private void ProcessUnknownNode(UnknownNodeAction action)
        {
            if (UnknownNodeAction.ReadUnknownNode == action)
            {
                UnknownNode(null);
            }
            else
            {
                CreateUnknownNodeException();
            }
        }

        private void WriteMembers(ref object o, Member[] members, UnknownNodeAction elementElseAction, UnknownNodeAction elseAction, Member anyElement, Member anyText)
        {
            Reader.MoveToContent();

            var collectionMemberToBeSet = new HashSet<Member>();
            while (Reader.NodeType != XmlNodeType.EndElement && Reader.NodeType != XmlNodeType.None)
            {
                object value = null;
                Member member;
                WriteMemberElements(ref value, null, out member, members, elementElseAction, elseAction, anyElement, anyText, masterObject: o);
                if (member != null)
                {
                    PropertyInfo pi = member.Mapping.MemberInfo as PropertyInfo;
                    if (pi != null && typeof(IList).IsAssignableFrom(pi.PropertyType)
                        && (pi.SetMethod == null || !pi.SetMethod.IsPublic))
                    {
                        var getOnlyList = (IList)pi.GetValue(o);

                        var valueList = value as IList;
                        if (valueList != null)
                        {
                            foreach (var i in valueList)
                            {
                                getOnlyList.Add(i);
                            }
                        }
                        else
                        {
                            getOnlyList.Add(value);
                        }
                    }
                    else if (member.Collection != null)
                    {
                        member.Collection.Add(value);
                        collectionMemberToBeSet.Add(member);
                    }
                    else
                    {
                        SetMemberValue(o, value, member.Mapping.Name);
                    }
                }

                Reader.MoveToContent();
            }

            foreach (var member in collectionMemberToBeSet)
            {
                if (member.Collection != null)
                {
                    MemberInfo[] memberInfos = o.GetType().GetMember(member.Mapping.Name);
                    var memberInfo = memberInfos[0];
                    object collection = null;
                    SetCollectionObjectWithCollectionMember(ref collection, member.Collection, member.Mapping.TypeDesc.Type);
                    SetMemberValue(o, collection, memberInfo);
                }
            }
        }

        private void SetCollectionObjectWithCollectionMember(ref object collection, CollectionMember collectionMember, Type collectionType)
        {
            if (collectionType.IsArray)
            {
                Array a;
                Type elementType = collectionType.GetElementType();
                var currentArray = collection as Array;
                if (currentArray != null && currentArray.Length == collectionMember.Count)
                {
                    a = currentArray;
                }
                else
                {
                    a = Array.CreateInstance(elementType, collectionMember.Count);
                }

                for (int i = 0; i < collectionMember.Count; i++)
                {
                    a.SetValue(collectionMember[i], i);
                }

                collection = a;
            }
            else
            {
                if (collection == null)
                {
                    collection = ReflectionCreateObject(collectionType);
                }

                if (typeof(IList).IsAssignableFrom(collectionType))
                {
                    foreach (var item in collectionMember)
                    {
                        ((IList)collection).Add(item);
                    }
                }
                else
                {
                    foreach (var item in collectionMember)
                    {
                        MethodInfo addMethod = collectionType.GetMethod("Add");
                        if (addMethod == null)
                        {
                            throw new InvalidOperationException("(addMethod == null)");
                        }

                        addMethod.Invoke(collection, new object[] { item });
                    }
                }
            }
        }

        private static void SetMemberValue(object o, object value, string memberName)
        {
            MemberInfo[] memberInfos = o.GetType().GetMember(memberName);
            var memberInfo = memberInfos[0];
            SetMemberValue(o, value, memberInfo);
        }

        private static void SetMemberValue(object o, object value, MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
            {
                var propInfo = (PropertyInfo)memberInfo;
                propInfo.SetValue(o, value);
            }
            else if (memberInfo is FieldInfo)
            {
                var fieldInfo = (FieldInfo)memberInfo;
                fieldInfo.SetValue(o, value);
            }
            else
            {
                throw new InvalidOperationException("InvalidMember");
            }
        }

        private object GetMemberValue(object o, MemberInfo memberInfo)
        {
            var memberProperty = memberInfo as PropertyInfo;
            if (memberProperty != null)
            {
                return memberProperty.GetValue(o);
            }

            var memberField = memberInfo as FieldInfo;
            if (memberField != null)
            {
                return memberField.GetValue(o);
            }

            throw new InvalidOperationException("unknown member type");
        }

        private bool WriteMemberText(out object o, Member anyText)
        {
            MemberMapping anyTextMapping = anyText.Mapping;
            if ((Reader.NodeType == XmlNodeType.Text ||
                        Reader.NodeType == XmlNodeType.CDATA ||
                        Reader.NodeType == XmlNodeType.Whitespace ||
                        Reader.NodeType == XmlNodeType.SignificantWhitespace))
            {
                TextAccessor text = anyTextMapping.Text;

                if (text.Mapping is SpecialMapping)
                {
                    SpecialMapping special = (SpecialMapping)text.Mapping;
                    if (special.TypeDesc.Kind == TypeKind.Node)
                    {
                        o = Document.CreateTextNode(ReadString());
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlInternalError));
                    }
                }
                else
                {
                    if (anyTextMapping.TypeDesc.IsArrayLike)
                    {
                        if (text.Mapping.TypeDesc.CollapseWhitespace)
                        {
                            o = CollapseWhitespace(ReadString());
                        }
                        else
                        {
                            o = ReadString();
                        }
                    }
                    else
                    {
                        if (text.Mapping.TypeDesc == StringTypeDesc || text.Mapping.TypeDesc.FormatterName == "String")
                        {
                            o = ReadString(null, text.Mapping.TypeDesc.CollapseWhitespace);
                        }
                        else
                        {
                            o = WritePrimitive(text.Mapping, () => ReadString());
                        }
                    }
                }

                return true;
            }

            o = null;
            return false;
        }

        bool IsSequence(Member[] members)
        {
            // #10586: Currently the reflection based method treat this kind of type as normal types. 
            // But potentially we can do some optimization for types that have ordered properties. 

            //for (int i = 0; i < members.Length; i++)
            //{
            //    if (members[i].Mapping.IsParticle && members[i].Mapping.IsSequence)
            //        return true;
            //}
            return false;
        }

        private void WriteMemberElementsIf(ref object o, CollectionMember collectionMember, out Member member, Member[] expectedMembers, Member anyElementMember, UnknownNodeAction elementElseAction, object masterObject = null)
        {
            bool isSequence = IsSequence(expectedMembers);
            if (isSequence)
            {
                // #10586: Currently the reflection based method treat this kind of type as normal types. 
                // But potentially we can do some optimization for types that have ordered properties.
            }

            ElementAccessor e = null;
            member = null;
            bool foundElement = false;
            foreach (var m in  expectedMembers)
            {
                if (m.Mapping.Xmlns != null)
                    continue;
                if (m.Mapping.Ignore)
                    continue;
                if (isSequence && (m.Mapping.IsText || m.Mapping.IsAttribute))
                    continue;

                foreach (var ele in m.Mapping.Elements)
                {
                    if (ele.Name == Reader.LocalName && ele.Namespace == Reader.NamespaceURI)
                    {
                        e = ele;
                        member = m;
                        foundElement = true;
                        break;
                    }
                }

                if (foundElement)
                    break;
            }

            if (foundElement)
            {
                ChoiceIdentifierAccessor choice = member.Mapping.ChoiceIdentifier;
                string ns = e.Form == XmlSchemaForm.Qualified ? e.Namespace : "";
                bool isList = member.Mapping.TypeDesc.IsArrayLike && !member.Mapping.TypeDesc.IsArray;
                WriteElement(ref o, collectionMember, e, choice, member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite, isList && member.Mapping.TypeDesc.IsNullable, member.Mapping.ReadOnly, ns, masterObject);
            }
            else
            {
                if (anyElementMember!= null && anyElementMember.Mapping != null)
                {
                    var anyElement = anyElementMember.Mapping;

                    member = anyElementMember;
                    ChoiceIdentifierAccessor choice = member.Mapping.ChoiceIdentifier;
                    ElementAccessor[] elements = anyElement.Elements;
                    for (int i = 0; i < elements.Length; i++)
                    {
                        ElementAccessor element = elements[i];
                        if (element.Any && element.Name.Length == 0)
                        {
                            string ns = element.Form == XmlSchemaForm.Qualified ? element.Namespace : "";
                            WriteElement(ref o, collectionMember, element, choice, anyElement.CheckSpecified == SpecifiedAccessor.ReadWrite, false, false, ns, masterObject : masterObject);
                            break;
                        }
                    }
                }
                else
                {
                    member = null;
                    ProcessUnknownNode(elementElseAction);
                }
            }
        }

        private void WriteElement(ref object o, CollectionMember collectionMember, ElementAccessor element, ChoiceIdentifierAccessor choice, bool checkSpecified, bool checkForNull, bool readOnly, string defaultNamespace, object masterObject = null)
        {
            object value = null;
            if (element.Mapping is ArrayMapping)
            {
                WriteArray(ref value, (ArrayMapping)element.Mapping, readOnly, element.IsNullable, defaultNamespace);
            }
            else if (element.Mapping is NullableMapping)
            {
                value = WriteNullableMethod((NullableMapping)element.Mapping, true, defaultNamespace);
            }
            else if (!element.Mapping.IsSoap && (element.Mapping is PrimitiveMapping))
            {
                if (element.IsNullable && ReadNull())
                {
                    if (element.Mapping.TypeDesc.IsValueType)
                    {
                        value = ReflectionCreateObject(element.Mapping.TypeDesc.Type);
                    }
                    else
                    {
                        value = null;
                    }
                }
                else if ((element.Default != null && !Globals.IsDBNullValue(element.Default) && element.Mapping.TypeDesc.IsValueType)
                         && (Reader.IsEmptyElement))
                {
                    Reader.Skip();
                }
                else
                {
                    if (element.Mapping.TypeDesc == QnameTypeDesc)
                        value = ReadElementQualifiedName();
                    else
                    {

                        if (element.Mapping.TypeDesc.FormatterName == "ByteArrayBase64")
                        {
                            value = ToByteArrayBase64(false);
                        }
                        else if (element.Mapping.TypeDesc.FormatterName == "ByteArrayHex")
                        {
                            value = ToByteArrayHex(false);
                        }
                        else
                        {
                            Func<string> readFunc = () => Reader.ReadElementContentAsString();

                            value = WritePrimitive(element.Mapping, readFunc);
                        }
                    }
                }
            }
            else if (element.Mapping is StructMapping || (element.Mapping.IsSoap && element.Mapping is PrimitiveMapping))
            {
                TypeMapping mapping = element.Mapping;
                if (mapping.IsSoap)
                {
                    throw new PlatformNotSupportedException();
                }
                else
                {
                    if (checkForNull && o == null)
                    {
                        Reader.Skip();
                    }
                    else
                    {
                        value = WriteStructMethod(
                                mapping: (StructMapping)mapping,
                                isNullable: mapping.TypeDesc.IsNullable && element.IsNullable,
                                checkType: true,
                                defaultNamespace: defaultNamespace
                                );
                    }
                }
            }
            else if (element.Mapping is SpecialMapping)
            {
                SpecialMapping special = (SpecialMapping)element.Mapping;
                switch (special.TypeDesc.Kind)
                {
                    case TypeKind.Node:
                        bool isDoc = special.TypeDesc.FullName == typeof(XmlDocument).FullName;
                        if (isDoc)
                        {
                            value = ReadXmlDocument(!element.Any);
                        }
                        else
                        {                            
                            value = ReadXmlNode(!element.Any);
                        }

                        break;
                    case TypeKind.Serializable:
                        SerializableMapping sm = (SerializableMapping)element.Mapping;
                        // check to see if we need to do the derivation
                        bool flag = true;
                        if (sm.DerivedMappings != null)
                        {
                            XmlQualifiedName tser = GetXsiType();
                            if (tser == null || QNameEqual(tser, sm.XsiType.Name, sm.XsiType.Namespace, defaultNamespace))
                            {

                            }
                            else
                            {
                                flag = false;
                            }
                        }

                        if (flag)
                        {
                            bool isWrappedAny = !element.Any && IsWildcard(sm);
                            value = ReadSerializable((IXmlSerializable)ReflectionCreateObject(sm.TypeDesc.Type), isWrappedAny);
                        }

                        if (sm.DerivedMappings != null)
                        {
                            // #10587: To Support SpecialMapping Types Having DerivedMappings  
                            throw new NotImplementedException("sm.DerivedMappings != null");
                            //WriteDerivedSerializable(sm, sm, source, isWrappedAny);
                            //WriteUnknownNode("UnknownNode", "null", null, true);
                        }
                        break;
                    default:
                        throw new InvalidOperationException(SR.Format(SR.XmlInternalError));
                }
            }
            else
            {
                throw new InvalidOperationException(SR.Format(SR.XmlInternalError));
            }

            if (choice != null && masterObject != null)
            {
                foreach(var name in choice.MemberIds)
                {
                    if (name == element.Name)
                    {
                        object choiceValue = Enum.Parse(choice.Mapping.TypeDesc.Type, name);
                        SetOrAddValueToMember(masterObject, choiceValue, choice.MemberInfo);

                        break;
                    }
                }
            }

            if (collectionMember != null)
            {
                collectionMember.Add(value);
            }
            else
            {
                o = value;
            }
        }

        private static Type GetMemberType(MemberInfo memberInfo)
        {
            Type memberType;

            if (memberInfo is FieldInfo)
            {
                memberType = ((FieldInfo)memberInfo).FieldType;
            }
            else if (memberInfo is PropertyInfo)
            {
                memberType = ((PropertyInfo)memberInfo).PropertyType;
            }
            else
            {
                throw new InvalidOperationException("unknown member type");
            }

            return memberType;
        }

        private static bool IsWildcard(SpecialMapping mapping)
        {
            if (mapping is SerializableMapping)
                return ((SerializableMapping)mapping).IsAny;
            return mapping.TypeDesc.CanBeElementValue;
        }

        private void WriteArray(ref object o, ArrayMapping arrayMapping, bool readOnly, bool isNullable, string defaultNamespace)
        {
            if (arrayMapping.IsSoap)
            {
                throw new PlatformNotSupportedException("arrayMapping.IsSoap");
            }
            else
            {
                if (!ReadNull())
                {
                    MemberMapping memberMapping = new MemberMapping();
                    memberMapping.Elements = arrayMapping.Elements;
                    memberMapping.TypeDesc = arrayMapping.TypeDesc;
                    memberMapping.ReadOnly = readOnly;

                    Type collectionType = memberMapping.TypeDesc.Type;

                    if (o == null)
                    {
                        o = ReflectionCreateObject(memberMapping.TypeDesc.Type);
                    }

                    if (memberMapping.ChoiceIdentifier != null)
                    {
                        // #10588: To Support ArrayMapping Types Having ChoiceIdentifier
                        throw new NotImplementedException("memberMapping.ChoiceIdentifier != null");
                    }

                    var collectionMember = new CollectionMember();
                    if ((readOnly && o == null) || Reader.IsEmptyElement)
                    {
                        Reader.Skip();
                    }
                    else
                    {
                        Reader.ReadStartElement();
                        Reader.MoveToContent();
                        UnknownNodeAction unknownNode = UnknownNodeAction.ReadUnknownNode;
                        while (Reader.NodeType != XmlNodeType.EndElement && Reader.NodeType != XmlNodeType.None)
                        {
                            Member temp;
                            WriteMemberElements(ref o, collectionMember, out temp, new Member[] { new Member(memberMapping) }, unknownNode, unknownNode, null, null);
                            Reader.MoveToContent();
                        }
                        ReadEndElement();
                    }

                    SetCollectionObjectWithCollectionMember(ref o, collectionMember, collectionType);

                }
            }
        }

        private object WritePrimitive(TypeMapping mapping, Func<string> readFunc)
        {
            if (mapping is EnumMapping)
            {
                if (mapping.IsSoap)
                {
                    // #10676: As all SOAP relates APIs are not in CoreCLR yet, the reflection based method
                    // currently throws PlatformNotSupportedException when types require SOAP serialization.
                    throw new PlatformNotSupportedException();
                }

                return WriteEnumMethod((EnumMapping)mapping, readFunc);
            }
            else if (mapping.TypeDesc == StringTypeDesc)
            {
                return readFunc();
            }
            else if (mapping.TypeDesc.FormatterName == "String")
            {
                if (mapping.TypeDesc.CollapseWhitespace)
                {
                    return CollapseWhitespace(readFunc());
                }
                else
                {
                    return readFunc();
                }
            }
            else
            {
                if (!mapping.TypeDesc.HasCustomFormatter)
                {
                    string value = readFunc();
                    object retObj;
                    switch (mapping.TypeDesc.FormatterName)
                    {
                        case "Boolean":
                            retObj = XmlConvert.ToBoolean(value);
                            break;
                        case "Int32":
                            retObj = XmlConvert.ToInt32(value);
                            break;
                        case "Int16":
                            retObj = XmlConvert.ToInt16(value);
                            break;
                        case "Int64":
                            retObj = XmlConvert.ToInt64(value);
                            break;
                        case "Single":
                            retObj = XmlConvert.ToSingle(value);
                            break;
                        case "Double":
                            retObj = XmlConvert.ToDouble(value);
                            break;
                        case "Decimal":
                            retObj = XmlConvert.ToDecimal(value);
                            break;
                        case "Byte":
                            retObj = XmlConvert.ToByte(value);
                            break;
                        case "SByte":
                            retObj = XmlConvert.ToSByte(value);
                            break;
                        case "UInt16":
                            retObj = XmlConvert.ToUInt16(value);
                            break;
                        case "UInt32":
                            retObj = XmlConvert.ToUInt32(value);
                            break;
                        case "UInt64":
                            retObj = XmlConvert.ToUInt64(value);
                            break;
                        case "Guid":
                            retObj = XmlConvert.ToGuid(value);
                            break;
                        case "Char":
                            retObj = XmlConvert.ToChar(value);
                            break;
                        case "TimeSpan":
                            retObj = XmlConvert.ToTimeSpan(value);
                            break;
                        default:
                            throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, $"unknown FormatterName: {mapping.TypeDesc.FormatterName}"));
                    }

                    return retObj;
                }
                else
                {
                    string methodName = "To" + mapping.TypeDesc.FormatterName;
                    MethodInfo method = typeof(XmlSerializationReader).GetMethod(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[] { typeof(string) });
                    if (method == null)
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, $"unknown FormatterName: {mapping.TypeDesc.FormatterName}"));
                    }

                    return method.Invoke(this, new object[] { readFunc() });
                }
            }
        }

        object WriteStructMethod(StructMapping mapping, bool isNullable, bool checkType, string defaultNamespace)
        {
            if (mapping.IsSoap)
                throw new PlatformNotSupportedException();
            else
                return WriteLiteralStructMethod(mapping, isNullable, checkType, defaultNamespace);
        }

        object WriteNullableMethod(NullableMapping nullableMapping, bool checkType, string defaultNamespace)
        {
            object o = Activator.CreateInstance(nullableMapping.TypeDesc.Type);
            if (!ReadNull())
            { 
                ElementAccessor element = new ElementAccessor();
                element.Mapping = nullableMapping.BaseMapping;
                element.Any = false;
                element.IsNullable = nullableMapping.BaseMapping.TypeDesc.IsNullable;

                WriteElement(ref o, null, element, null, false, false, false, defaultNamespace);
            }

            return o;
        }

        private object WriteEnumMethod(EnumMapping mapping, Func<string> readFunc)
        {
            if (mapping.IsSoap)
            {
                // #10676: As all SOAP relates APIs are not in CoreCLR yet, the reflection based method
                // currently throws PlatformNotSupportedException when types require SOAP serialization.
                throw new PlatformNotSupportedException();
            }

            string source = readFunc();

            if (mapping.IsFlags)
            {
                Hashtable table = WriteHashtable(mapping, mapping.TypeDesc.Name);
                return Enum.ToObject(mapping.TypeDesc.Type, ToEnum(source, table, mapping.TypeDesc.Name));
            }
            else
            {
                return Enum.Parse(mapping.TypeDesc.Type, source);
            }
        }

        private Hashtable WriteHashtable(EnumMapping mapping, string name)
        {
            var h = new Hashtable();

            ConstantMapping[] constants = mapping.Constants;

            for (int i = 0; i < constants.Length; i++)
            {
                h.Add(constants[i].XmlName, constants[i].Value);
            }

            return h;
        }

        private object ReflectionCreateObject(Type type)
        {
            object obj = null;
            
            if (type.IsArray)
            {
                obj = Activator.CreateInstance(type, 32);
            }
            else
            {
                ConstructorInfo ci = GetDefaultConstructor(type);
                if (ci != null)
                {
                    obj = ci.Invoke(Array.Empty<object>());
                }
                else
                {
                    obj = Activator.CreateInstance(type);
                }
            }

            return obj;
        }

        private ConstructorInfo GetDefaultConstructor(Type type)
        {
            if (type.GetTypeInfo().IsValueType)
                return null;

            ConstructorInfo ctor = FindDefaultConstructor(type.GetTypeInfo());
            return ctor;
        }

        private static ConstructorInfo FindDefaultConstructor(TypeInfo ti)
        {
            foreach (var ci in ti.DeclaredConstructors)
            {
                if (!ci.IsStatic && ci.GetParameters().Length == 0)
                {
                    return ci;
                }
            }
            return null;
        }

        private object WriteLiteralStructMethod(StructMapping structMapping, bool isNullable, bool checkType, string defaultNamespace)
        {
            XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable)
            {
                isNull = ReadNull();
            }

            if (checkType)
            {
                if (structMapping.TypeDesc.IsRoot && isNull)
                {
                    if (xsiType != null)
                        return ReadTypedNull(xsiType);

                    else
                    {
                        if (structMapping.TypeDesc.IsValueType)
                        {
                            return ReflectionCreateObject(structMapping.TypeDesc.Type);

                        }
                        else
                        {
                            return null;
                        }
                    }
                }

                object o = null;
                if (xsiType == null || (!structMapping.TypeDesc.IsRoot && QNameEqual(xsiType, structMapping.TypeName, structMapping.Namespace, defaultNamespace)))
                {
                    if (structMapping.TypeDesc.IsRoot)
                    {
                        return ReadTypedPrimitive(new XmlQualifiedName(Soap.UrType, XmlSchemaConstants.Namespace));
                    }
                }
                else if (WriteDerivedTypes(out o, structMapping, xsiType, defaultNamespace, checkType, isNullable))
                {
                    return o;
                }
                else if (structMapping.TypeDesc.IsRoot && WriteEnumAndArrayTypes(out o, structMapping, xsiType, defaultNamespace))
                {
                    return o;
                }
                else
                {
                    if (structMapping.TypeDesc.IsRoot)
                        return ReadTypedPrimitive(xsiType);
                    else
                        throw CreateUnknownTypeException(xsiType);
                }
            }

            if (structMapping.TypeDesc.IsNullable && isNull)
            {
                return null;
            }
            else if (structMapping.TypeDesc.IsAbstract)
            {
                throw CreateAbstractTypeException(structMapping.TypeName, structMapping.Namespace);
            }
            else
            {
                if (structMapping.TypeDesc.Type != null && typeof(XmlSchemaObject).IsAssignableFrom(structMapping.TypeDesc.Type))
                {
                    // #10589: To Support Serializing XmlSchemaObject
                    throw new NotImplementedException("typeof(XmlSchemaObject)");
                }

                object o = ReflectionCreateObject(structMapping.TypeDesc.Type);

                MemberMapping[] mappings = TypeScope.GetSettableMembers(structMapping);
                MemberMapping anyText = null;
                MemberMapping anyElement = null;
                MemberMapping anyAttribute = null;
                Member anyElementMember = null;
                Member anyTextMember = null;

                bool isSequence = structMapping.HasExplicitSequence();

                if (isSequence)
                {
                   // #10586: Currently the reflection based method treat this kind of type as normal types. 
                   // But potentially we can do some optimization for types that have ordered properties.
                }

                var allMembersList = new List<Member>(mappings.Length);
                var allMemberMappingList = new List<MemberMapping>(mappings.Length);

                for (int i = 0; i < mappings.Length; i++)
                {
                    MemberMapping mapping = mappings[i];

                    if (mapping.Text != null)
                        anyText = mapping;
                    if (mapping.Attribute != null && mapping.Attribute.Any)
                        anyAttribute = mapping;
                    if (!isSequence)
                    {
                        // find anyElement if present.
                        for (int j = 0; j < mapping.Elements.Length; j++)
                        {
                            if (mapping.Elements[j].Any && (mapping.Elements[j].Name == null || mapping.Elements[j].Name.Length == 0))
                            {
                                anyElement = mapping;
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

                    var member = new Member(mapping);
                    if (mapping.TypeDesc.IsArrayLike)
                    {
                        if (mapping.TypeDesc.IsArrayLike && !(mapping.Elements.Length == 1 && mapping.Elements[0].Mapping is ArrayMapping))
                        {
                            member.Collection = new CollectionMember();
                        }
                        else if (!mapping.TypeDesc.IsArray)
                        {

                        }
                    }

                    allMemberMappingList.Add(mapping);
                    allMembersList.Add(member);

                    if (mapping == anyElement)
                    {
                        anyElementMember = member;
                    }
                    else if(mapping == anyText)
                    {
                        anyTextMember = member;
                    }
                }

                var allMembers = allMembersList.ToArray();
                var allMemberMappings = allMemberMappingList.ToArray();

                Action<object> unknownNodeAction = (n) => UnknownNode(n);
                WriteAttributes(allMemberMappings, anyAttribute, unknownNodeAction, ref o);

                Reader.MoveToElement();
                if (Reader.IsEmptyElement)
                {
                    Reader.Skip();
                    return o;
                }

                Reader.ReadStartElement();
                bool IsSequenceAllMembers = IsSequence(allMembers);
                if (IsSequenceAllMembers)
                {
                    // #10586: Currently the reflection based method treat this kind of type as normal types. 
                    // But potentially we can do some optimization for types that have ordered properties.
                }

                UnknownNodeAction unknownNode = UnknownNodeAction.ReadUnknownNode;
                WriteMembers(ref o, allMembers, unknownNode, unknownNode, anyElementMember, anyTextMember);

                ReadEndElement();
                return o;
            }
        }

        private bool WriteEnumAndArrayTypes(out object o, StructMapping mapping, XmlQualifiedName xsiType, string defaultNamespace)
        {
            foreach (var m in _mapping.Scope.TypeMappings)
            {
                var em = m as EnumMapping;
                if (em != null)
                {
                    if (QNameEqual(xsiType, em.TypeName, em.Namespace, defaultNamespace))
                    {
                        Reader.ReadStartElement();
                        o = WriteEnumMethod(em, () => (CollapseWhitespace(this.ReadString())));
                        ReadEndElement();
                        return true;
                    }

                    continue;
                }

                var am = m as ArrayMapping;
                if (am != null)
                {
                    if (QNameEqual(xsiType, am.TypeName, am.Namespace, defaultNamespace))
                    {
                        o = null;
                        WriteArray(ref o, am, false, false, defaultNamespace);
                        return true;
                    }

                    continue;
                }
            }

            o = null;
            return false;
        }

        bool WriteDerivedTypes(out object o, StructMapping mapping, XmlQualifiedName xsiType, string defaultNamespace, bool checkType, bool isNullable)
        {
            for (StructMapping derived = mapping.DerivedMappings; derived != null;derived = derived.NextDerivedMapping)
            {
                if (QNameEqual(xsiType, derived.TypeName, derived.Namespace, defaultNamespace))
                {
                    o = WriteStructMethod(derived, isNullable, checkType, defaultNamespace);
                    return true;
                }

                if (WriteDerivedTypes(out o, derived, xsiType, defaultNamespace, checkType, isNullable))
                {
                    return true;
                }
            }

            o = null;
            return false;
        }

        private void WriteAttributes(MemberMapping[] members, MemberMapping anyAttribute, Action<object> elseCall, ref object o)
        {
            MemberMapping xmlnsMember = null;
            var attributes = new List<AttributeAccessor>();

            while (Reader.MoveToNextAttribute())
            {
                bool memberFound = false;
                foreach (var member in members)
                {
                    if (member.Xmlns != null)
                    {
                        xmlnsMember = member;
                        continue;
                    }

                    if (member.Ignore)
                        continue;
                    AttributeAccessor attribute = member.Attribute;

                    if (attribute == null) continue;
                    if (attribute.Any) continue;

                    attributes.Add(attribute);

                    if (attribute.IsSpecialXmlNamespace)
                    {
                        memberFound = XmlNodeEqual(Reader, attribute.Name, XmlReservedNs.NsXml);
                    }
                    else
                        memberFound = XmlNodeEqual(Reader, attribute.Name, attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : "");

                    if (memberFound)
                    {
                        WriteAttribute(o, member);
                        memberFound = true;
                        break;
                    }
                }

                if (memberFound)
                {
                    continue;
                }

                bool flag2 = false;
                if (xmlnsMember != null)
                {
                    if (IsXmlnsAttribute(Reader.Name))
                    {
                        if (GetMemberType(xmlnsMember.MemberInfo) == typeof(XmlSerializerNamespaces))
                        {
                            var xmlnsMemberSource = GetMemberValue(o, xmlnsMember.MemberInfo) as XmlSerializerNamespaces;
                            if (xmlnsMemberSource == null)
                            {
                                xmlnsMemberSource = new XmlSerializerNamespaces();
                                SetMemberValue(o, xmlnsMemberSource, xmlnsMember.MemberInfo);
                            }

                            xmlnsMemberSource.Add(Reader.Name.Length == 5 ? "" : Reader.LocalName, Reader.Value);
                        }
                        else
                        {
                            throw new InvalidOperationException("xmlnsMemberSource is not of type XmlSerializerNamespaces");
                        }
                    }
                    else
                    {
                        flag2 = true;
                    }
                }
                else if (!IsXmlnsAttribute(Reader.Name))
                {
                    flag2 = true;
                }

                if (flag2)
                {
                    if (anyAttribute != null)
                    {
                        var attr = Document.ReadNode(Reader) as XmlAttribute;
                        ParseWsdlArrayType(attr);
                        WriteAttribute(o, anyAttribute, attr);
                    }
                    else
                    {

                        elseCall(o);
                    }
                }
            }
        }

        private void WriteAttribute(object o, MemberMapping member, object attr = null)
        {
            AttributeAccessor attribute = member.Attribute;
            object value = null;
            if (attribute.Mapping is SpecialMapping)
            {
                SpecialMapping special = (SpecialMapping)attribute.Mapping;

                if (special.TypeDesc.Kind == TypeKind.Attribute)
                {
                    value = attr;
                }
                else if (special.TypeDesc.CanBeAttributeValue)
                {
                    // #10590: To Support special.TypeDesc.CanBeAttributeValue == true
                    throw new NotImplementedException("special.TypeDesc.CanBeAttributeValue");
                }
                else
                    throw new InvalidOperationException(SR.Format(SR.XmlInternalError));
            }
            else
            {
                if (attribute.IsList)
                {

                    string listValues = Reader.Value;
                    string[] vals = listValues.Split(null);
                    Array arrayValue = Array.CreateInstance(member.TypeDesc.Type.GetElementType(), vals.Length);
                    for (int i = 0; i < vals.Length; i++)
                    {
                        arrayValue.SetValue(WritePrimitive(attribute.Mapping, () => vals[i]), i);
                    }

                    value = arrayValue;
                }
                else
                {
                    value = WritePrimitive(attribute.Mapping, () => Reader.Value);
                }
            }

            SetOrAddValueToMember(o, value, member.MemberInfo);

            if (member.CheckSpecified == SpecifiedAccessor.ReadWrite)
            {
                // #10591: we need to add tests for this block.
                string specifiedMemberName = member.Name + "Specified";
                MethodInfo specifiedMethodInfo = o.GetType().GetMethod("set_" + specifiedMemberName);
                if (specifiedMethodInfo != null)
                {
                    specifiedMethodInfo.Invoke(o, new object[] { true });
                }
            }
        }

        private void SetOrAddValueToMember(object o, object value, MemberInfo memberInfo)
        {
            Type memberType = GetMemberType(memberInfo);

            if (memberType == value.GetType())
            {
                SetMemberValue(o, value, memberInfo);
            }
            else if (memberType.IsArray)
            {
                AddItemInArrayMember(o, memberInfo, memberType, value);
            }
            else
            {
                SetMemberValue(o, value, memberInfo);
            }
        }

        private void AddItemInArrayMember(object o, MemberInfo memberInfo, Type memberType, object item)
        {
            Debug.Assert(memberType.IsArray);

            Array currentArray = (Array)GetMemberValue(o, memberInfo);

            int length;
            if (currentArray == null)
            {
                length = 0;
            }
            else
            {
                length = currentArray.Length;
            }

            var newArray = Array.CreateInstance(memberType.GetElementType(), length + 1);
            if (currentArray != null)
            {
                Array.Copy(currentArray, newArray, length);
            }

            newArray.SetValue(item, length);
            SetMemberValue(o, newArray, memberInfo);
        }

        // WriteXmlNodeEqual
        private bool XmlNodeEqual(XmlReader source, string name, string ns)
        {
            return source.LocalName == name && string.Equals(source.NamespaceURI, ns);
        }

        private bool QNameEqual(XmlQualifiedName xsiType, string name, string ns, string defaultNamespace)
        {
            return xsiType.Name == name && string.Equals(xsiType.Namespace, defaultNamespace);
        }

        internal class CollectionMember : List<object>
        {
        }

        internal class Member
        {
            public MemberMapping Mapping;
            public CollectionMember Collection;

            public Member(MemberMapping mapping)
            {
                Mapping = mapping;
            }

            public Member(MemberMapping mapping, CollectionMember collectionMember) : this(mapping)
            {
                Collection = collectionMember;
            }
        }

        enum UnknownNodeAction
        {
            CreateUnknownNodeException,
            ReadUnknownNode
        }
    }
}
