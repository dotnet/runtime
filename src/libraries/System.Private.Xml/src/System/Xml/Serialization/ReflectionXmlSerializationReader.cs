// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Extensions;
using System.Xml.Schema;

// UnconditionalSuppressMessage that specify a Target need to be at the assembly or module level for now. Also,
// they won't consider Target unless you also specify Scope to be either "member" or "type"
[assembly: UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:MakeGenericMethod",
       Target = "M:System.Xml.Serialization.ReflectionXmlSerializationReader.#cctor",
       Justification = "The reason why this warns is because the two static properties call GetTypeDesc() which internally will call " +
        "ImportTypeDesc() when the passed in type is not considered a primitive type. That said, for both properties here we are passing in string " +
        "and XmlQualifiedName which are considered primitive, so they are trim safe.",
       Scope = "member")]

namespace System.Xml.Serialization
{
    internal delegate void UnknownNodeAction(object? o);

    internal sealed class ReflectionXmlSerializationReader : XmlSerializationReader
    {
        private readonly XmlMapping _mapping;

        internal static TypeDesc StringTypeDesc { get; set; } = (new TypeScope()).GetTypeDesc(typeof(string));
        internal static TypeDesc QnameTypeDesc { get; set; } = (new TypeScope()).GetTypeDesc(typeof(XmlQualifiedName));

        public ReflectionXmlSerializationReader(XmlMapping mapping, XmlReader xmlReader, XmlDeserializationEvents events, string? encodingStyle)
        {
            Init(xmlReader, events, encodingStyle, tempAssembly: null);
            _mapping = mapping;
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        protected override void InitCallbacks()
        {
            TypeScope scope = _mapping.Scope!;
            foreach (TypeMapping mapping in scope.TypeMappings)
            {
                if (mapping.IsSoap &&
                        (mapping is StructMapping || mapping is EnumMapping || mapping is ArrayMapping || mapping is NullableMapping) &&
                        !mapping.TypeDesc!.IsRoot)
                {
                    AddReadCallback(
                        mapping.TypeName!,
                        mapping.Namespace!,
                        mapping.TypeDesc.Type!,
                        CreateXmlSerializationReadCallback(mapping));
                }
            }
        }

        protected override void InitIDs()
        {
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public object? ReadObject()
        {
            XmlMapping xmlMapping = _mapping;
            if (!xmlMapping.IsReadable)
                return null;

            if (!xmlMapping.GenerateSerializer)
                throw new ArgumentException(SR.Format(SR.XmlInternalError, "xmlMapping"));

            if (xmlMapping is XmlTypeMapping xmlTypeMapping)
            {
                return GenerateTypeElement(xmlTypeMapping);
            }
            else if (xmlMapping is XmlMembersMapping xmlMembersMapping)
            {
                return GenerateMembersElement(xmlMembersMapping);
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.XmlInternalError, "xmlMapping"));
            }
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private object GenerateMembersElement(XmlMembersMapping xmlMembersMapping)
        {
            if (xmlMembersMapping.Accessor.IsSoap)
            {
                return GenerateEncodedMembersElement(xmlMembersMapping);
            }
            else
            {
                return GenerateLiteralMembersElement(xmlMembersMapping);
            }
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private object GenerateLiteralMembersElement(XmlMembersMapping xmlMembersMapping)
        {
            ElementAccessor element = xmlMembersMapping.Accessor;
            MemberMapping[] mappings = ((MembersMapping)element.Mapping!).Members!;
            bool hasWrapperElement = ((MembersMapping)element.Mapping).HasWrapperElement;
            Reader.MoveToContent();

            object[] p = new object[mappings.Length];
            InitializeValueTypes(p, mappings);

            if (hasWrapperElement)
            {
                string elementName = element.Name;
                string elementNs = element.Form == XmlSchemaForm.Qualified ? element.Namespace! : string.Empty;
                Reader.MoveToContent();
                while (Reader.NodeType != XmlNodeType.EndElement && Reader.NodeType != XmlNodeType.None)
                {
                    if (Reader.IsStartElement(element.Name, elementNs))
                    {
                        if (!GenerateLiteralMembersElementInternal(mappings, hasWrapperElement, p))
                        {
                            continue;
                        }

                        ReadEndElement();
                    }
                    else
                    {

                        UnknownNode(null, $"{elementNs}:{elementName}");
                    }

                    Reader.MoveToContent();
                }
            }
            else
            {
                GenerateLiteralMembersElementInternal(mappings, hasWrapperElement, p);
            }

            return p;
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private bool GenerateLiteralMembersElementInternal(MemberMapping[] mappings, bool hasWrapperElement, object?[] p)
        {
            Member? anyText = null;
            Member? anyElement = null;
            Member? anyAttribute = null;

            var membersList = new List<Member>();
            var textOrArrayMembersList = new List<Member>();
            var attributeMembersList = new List<Member>();

            for (int i = 0; i < mappings.Length; i++)
            {
                int index = i;
                MemberMapping mapping = mappings[index];
                Action<object?> source = (o) => p[index] = o;

                Member member = new Member(mapping);
                Member anyMember = new Member(mapping);

                if (mapping.Xmlns != null)
                {
                    var xmlns = new XmlSerializerNamespaces();
                    p[index] = xmlns;
                    member.XmlnsSource = (ns, name) => xmlns.Add(ns, name);
                }

                member.Source = source;
                anyMember.Source = source;

                if (mapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
                {
                    string nameSpecified = mapping.Name + "Specified";
                    for (int j = 0; j < mappings.Length; j++)
                    {
                        if (mappings[j].Name == nameSpecified)
                        {
                            int indexJ = j;
                            member.CheckSpecifiedSource = (o) => p[indexJ] = o;
                        }
                    }
                }

                bool foundAnyElement = false;
                if (mapping.Text != null)
                {
                    anyText = anyMember;
                }

                if (mapping.Attribute != null && mapping.Attribute.Any)
                {
                    anyMember.Collection = new CollectionMember();
                    anyMember.ArraySource = anyMember.Source;
                    anyMember.Source = (item) =>
                    {
                        anyMember.Collection.Add(item);
                    };

                    anyAttribute = anyMember;
                }

                if (mapping.Attribute != null || mapping.Xmlns != null)
                {
                    attributeMembersList.Add(member);
                }
                else if (mapping.Text != null)
                {
                    textOrArrayMembersList.Add(member);
                }

                if (!mapping.IsSequence)
                {
                    for (int j = 0; j < mapping.Elements!.Length; j++)
                    {
                        if (mapping.Elements[j].Any && mapping.Elements[j].Name.Length == 0)
                        {
                            anyElement = anyMember;
                            if (mapping.Attribute == null && mapping.Text == null)
                            {
                                anyMember.Collection = new CollectionMember();
                                anyMember.ArraySource = (item) =>
                                {
                                    anyMember.Collection.Add(item);
                                };

                                textOrArrayMembersList.Add(anyMember);
                            }

                            foundAnyElement = true;
                            break;
                        }
                    }
                }

                if (mapping.Attribute != null || mapping.Text != null || foundAnyElement)
                {
                    membersList.Add(anyMember);
                }
                else if (mapping.TypeDesc!.IsArrayLike
                    && !(mapping.Elements!.Length == 1 && mapping.Elements[0].Mapping is ArrayMapping))
                {
                    anyMember.Collection = new CollectionMember();
                    anyMember.ArraySource = (item) =>
                    {
                        anyMember.Collection.Add(item);
                    };

                    membersList.Add(anyMember);
                    textOrArrayMembersList.Add(anyMember);
                }
                else
                {
                    membersList.Add(member);
                }
            }

            Member[] members = membersList.ToArray();
            Member[] textOrArrayMembers = textOrArrayMembersList.ToArray();

            if (members.Length > 0 && members[0].Mapping.IsReturnValue)
                IsReturnValue = true;

            if (attributeMembersList.Count > 0)
            {
                Member[] attributeMembers = attributeMembersList.ToArray();
                object? tempObject = null;
                WriteAttributes(attributeMembers, anyAttribute, UnknownNode, ref tempObject);
                Reader.MoveToElement();
            }

            if (hasWrapperElement)
            {
                if (Reader.IsEmptyElement)
                {
                    Reader.Skip();
                    Reader.MoveToContent();
                    return false;
                }

                Reader.ReadStartElement();
            }

            Reader.MoveToContent();
            while (Reader.NodeType != XmlNodeType.EndElement && Reader.NodeType != XmlNodeType.None)
            {
                WriteMemberElements(members, UnknownNode, UnknownNode, anyElement, anyText, null);
                Reader.MoveToContent();
            }

            foreach (Member member in textOrArrayMembers)
            {
                object? value = null;
                SetCollectionObjectWithCollectionMember(ref value, member.Collection!, member.Mapping.TypeDesc!.Type!);
                member.Source!(value);
            }

            if (anyAttribute != null)
            {
                object? value = null;
                SetCollectionObjectWithCollectionMember(ref value, anyAttribute.Collection!, anyAttribute.Mapping.TypeDesc!.Type!);
                anyAttribute.ArraySource!(value);
            }

            return true;
        }

        private void InitializeValueTypes(object?[] p, MemberMapping[] mappings)
        {
            for (int i = 0; i < mappings.Length; i++)
            {
                if (!mappings[i].TypeDesc!.IsValueType)
                    continue;


                if (mappings[i].TypeDesc!.IsOptionalValue && mappings[i].TypeDesc!.BaseTypeDesc!.UseReflection)
                {
                    p[i] = null;
                }
                else
                {
                    p[i] = ReflectionCreateObject(mappings[i].TypeDesc!.Type!);
                }
            }
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private object GenerateEncodedMembersElement(XmlMembersMapping xmlMembersMapping)
        {
            ElementAccessor element = xmlMembersMapping.Accessor;
            var membersMapping = (MembersMapping)element.Mapping!;
            MemberMapping[] mappings = membersMapping.Members!;
            bool hasWrapperElement = membersMapping.HasWrapperElement;
            bool writeAccessors = membersMapping.WriteAccessors;

            Reader.MoveToContent();

            object?[] p = new object[mappings.Length];
            InitializeValueTypes(p, mappings);

            bool isEmptyWrapper = true;
            if (hasWrapperElement)
            {
                Reader.MoveToContent();
                while (Reader.NodeType == XmlNodeType.Element)
                {
                    string? root = Reader.GetAttribute("root", Soap.Encoding);
                    if (root == null || XmlConvert.ToBoolean(root))
                        break;

                    ReadReferencedElement();
                    Reader.MoveToContent();
                }

                if (membersMapping.ValidateRpcWrapperElement)
                {
                    string name = element.Name;
                    string? ns = element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty;
                    if (!XmlNodeEqual(Reader, name, ns))
                    {
                        throw CreateUnknownNodeException();
                    }
                }

                isEmptyWrapper = Reader.IsEmptyElement;
                Reader.ReadStartElement();
            }

            Member[] members = new Member[mappings.Length];
            for (int i = 0; i < mappings.Length; i++)
            {
                int index = i;
                MemberMapping mapping = mappings[index];
                var member = new Member(mapping);
                member.Source = (value) => p[index] = value;
                members[index] = member;
                if (mapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
                {
                    string nameSpecified = mapping.Name + "Specified";
                    for (int j = 0; j < mappings.Length; j++)
                    {
                        if (mappings[j].Name == nameSpecified)
                        {
                            int indexOfSpecifiedMember = j;
                            member.CheckSpecifiedSource = (value) => p[indexOfSpecifiedMember] = value;
                            break;
                        }
                    }
                }

            }

            Fixup? fixup = WriteMemberFixupBegin(members, p);
            if (members.Length > 0 && members[0].Mapping.IsReturnValue)
            {
                IsReturnValue = true;
            }

            List<CheckTypeSource>? checkTypeHrefSource = null;
            if (!hasWrapperElement && !writeAccessors)
            {
                checkTypeHrefSource = new List<CheckTypeSource>();
            }

            Reader.MoveToContent();
            while (Reader.NodeType != XmlNodeType.EndElement && Reader.NodeType != XmlNodeType.None)
            {
                UnknownNodeAction unrecognizedElementSource;
                if (checkTypeHrefSource == null)
                {
                    unrecognizedElementSource = (_) => UnknownNode(p);
                }
                else
                {
                    unrecognizedElementSource = Wrapper;
                    [RequiresUnreferencedCode("calls ReadReferencedElement")]
                    void Wrapper(object? _)
                    {
                        if (Reader.GetAttribute("id", null) != null)
                        {
                            ReadReferencedElement();
                        }
                        else
                        {
                            UnknownNode(p);
                        }
                    }
                }

                WriteMemberElements(members, unrecognizedElementSource, (_) => UnknownNode(p), null, null, fixup: fixup, checkTypeHrefsSource: checkTypeHrefSource);
                Reader.MoveToContent();
            }

            if (!isEmptyWrapper)
            {
                ReadEndElement();
            }

            if (checkTypeHrefSource != null)
            {
                foreach (CheckTypeSource currentySource in checkTypeHrefSource)
                {
                    bool isReferenced = true;
                    bool isObject = currentySource.IsObject;
                    object? refObj = isObject ? currentySource.RefObject : GetTarget((string)currentySource.RefObject!);
                    if (refObj == null)
                    {
                        continue;
                    }

                    var checkTypeSource = new CheckTypeSource()
                    {
                        RefObject = refObj,
                        Type = refObj.GetType(),
                        Id = null
                    };
                    WriteMemberElementsIf(members, null, (_) => isReferenced = false, fixup, checkTypeSource);

                    if (isObject && isReferenced)
                    {
                        Referenced(refObj);
                    }
                }
            }

            ReadReferencedElements();
            return p;
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private object? GenerateTypeElement(XmlTypeMapping xmlTypeMapping)
        {
            ElementAccessor element = xmlTypeMapping.Accessor;
            TypeMapping mapping = element.Mapping!;

            Reader.MoveToContent();
            var memberMapping = new MemberMapping();
            memberMapping.TypeDesc = mapping.TypeDesc;
            memberMapping.Elements = new ElementAccessor[] { element };

            object? o = null;
            var holder = new ObjectHolder();
            var member = new Member(memberMapping);
            member.Source = (value) => holder.Object = value;
            member.GetSource = () => holder.Object;
            UnknownNodeAction elementElseAction = CreateUnknownNodeException;
            UnknownNodeAction elseAction = UnknownNode;
            WriteMemberElements(new Member[] { member }, elementElseAction, elseAction, element.Any ? member : null, null);
            o = holder.Object;

            if (element.IsSoap)
            {
                Referenced(o);
                ReadReferencedElements();
            }

            return o;
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private void WriteMemberElements(Member[] expectedMembers, UnknownNodeAction elementElseAction, UnknownNodeAction elseAction, Member? anyElement, Member? anyText, Fixup? fixup = null, List<CheckTypeSource>? checkTypeHrefsSource = null)
        {
            bool checkType = checkTypeHrefsSource != null;
            if (Reader.NodeType == XmlNodeType.Element)
            {
                if (checkType)
                {
                    if (Reader.GetAttribute("root", Soap.Encoding) == "0")
                    {
                        elementElseAction(null);
                        return;
                    }

                    WriteMemberElementsCheckType(checkTypeHrefsSource!);
                }
                else
                {
                    WriteMemberElementsIf(expectedMembers, anyElement, elementElseAction, fixup: fixup);
                }
            }
            else if (anyText != null && anyText.Mapping != null && WriteMemberText(anyText))
            {
            }
            else
            {
                ProcessUnknownNode(elseAction);
            }
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private void WriteMemberElementsCheckType(List<CheckTypeSource> checkTypeHrefsSource)
        {
            object? RefElememnt = ReadReferencingElement(null, null, true, out string? refElemId);
            var source = new CheckTypeSource();
            if (refElemId != null)
            {
                source.RefObject = refElemId;
                source.IsObject = false;
                checkTypeHrefsSource.Add(source);
            }
            else if (RefElememnt != null)
            {
                source.RefObject = RefElememnt;
                source.IsObject = true;
                checkTypeHrefsSource.Add(source);
            }
        }

        private void ProcessUnknownNode(UnknownNodeAction action)
        {
            action?.Invoke(null);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private void WriteMembers(ref object? o, Member[] members, UnknownNodeAction elementElseAction, UnknownNodeAction elseAction, Member? anyElement, Member? anyText)
        {
            Reader.MoveToContent();

            while (Reader.NodeType != XmlNodeType.EndElement && Reader.NodeType != XmlNodeType.None)
            {
                WriteMemberElements(members, elementElseAction, elseAction, anyElement, anyText);
                Reader.MoveToContent();
            }
        }

        private void SetCollectionObjectWithCollectionMember([NotNull] ref object? collection, CollectionMember collectionMember,
            [DynamicallyAccessedMembers(TrimmerConstants.AllMethods)] Type collectionType)
        {
            if (collectionType.IsArray)
            {
                Array a;
                if (collection is Array currentArray && currentArray.Length == collectionMember.Count)
                {
                    a = currentArray;
                }
                else
                {
                    Type elementType = collectionType.GetElementType()!;
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
                    collection = ReflectionCreateObject(collectionType)!;
                }

                AddObjectsIntoTargetCollection(collection, collectionMember, collectionType);
            }
        }

        private static void AddObjectsIntoTargetCollection(object targetCollection, List<object?> sourceCollection,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type targetCollectionType)
        {
            if (targetCollection is IList targetList)
            {
                foreach (object? item in sourceCollection)
                {
                    targetList.Add(item);
                }
            }
            else
            {
                MethodInfo? addMethod = targetCollectionType.GetMethod("Add");
                if (addMethod == null)
                {
                    throw new InvalidOperationException(SR.XmlInternalError);
                }

                object?[] arguments = new object?[1];
                foreach (object? item in sourceCollection)
                {
                    arguments[0] = item;
                    addMethod.Invoke(targetCollection, arguments);
                }
            }
        }

        private static readonly ConcurrentDictionary<(Type, string), ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate> s_setMemberValueDelegateCache = new ConcurrentDictionary<(Type, string), ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate>();

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private static ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate GetSetMemberValueDelegate(object o, string memberName)
        {
            Debug.Assert(o != null, "Object o should not be null");
            Debug.Assert(!string.IsNullOrEmpty(memberName), "memberName must have a value");
            (Type, string) typeMemberNameTuple = (o.GetType(), memberName);
            if (!s_setMemberValueDelegateCache.TryGetValue(typeMemberNameTuple, out ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate? result))
            {
                MemberInfo memberInfo = ReflectionXmlSerializationHelper.GetMember(o.GetType(), memberName);
                Debug.Assert(memberInfo != null, "memberInfo could not be retrieved");
                Type memberType;
                if (memberInfo is PropertyInfo propInfo)
                {
                    memberType = propInfo.PropertyType;
                }
                else if (memberInfo is FieldInfo fieldInfo)
                {
                    memberType = fieldInfo.FieldType;
                }
                else
                {
                    throw new InvalidOperationException(SR.XmlInternalError);
                }

                MethodInfo getSetMemberValueDelegateWithTypeGenericMi = typeof(ReflectionXmlSerializationReaderHelper).GetMethod("GetSetMemberValueDelegateWithType", BindingFlags.Static | BindingFlags.Public)!;
                MethodInfo getSetMemberValueDelegateWithTypeMi = getSetMemberValueDelegateWithTypeGenericMi.MakeGenericMethod(o.GetType(), memberType);
                var getSetMemberValueDelegateWithType = (Func<MemberInfo, ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate>)getSetMemberValueDelegateWithTypeMi.CreateDelegate(typeof(Func<MemberInfo, ReflectionXmlSerializationReaderHelper.SetMemberValueDelegate>));
                result = getSetMemberValueDelegateWithType(memberInfo);
                s_setMemberValueDelegateCache.TryAdd(typeMemberNameTuple, result);
            }

            return result;
        }

        private object? GetMemberValue(object o, MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo propertyInfo)
            {
                return propertyInfo.GetValue(o);
            }
            else if (memberInfo is FieldInfo fieldInfo)
            {
                return fieldInfo.GetValue(o);
            }

            throw new InvalidOperationException(SR.XmlInternalError);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private bool WriteMemberText(Member anyText)
        {
            object? value;
            MemberMapping anyTextMapping = anyText.Mapping;
            if ((Reader.NodeType == XmlNodeType.Text ||
                        Reader.NodeType == XmlNodeType.CDATA ||
                        Reader.NodeType == XmlNodeType.Whitespace ||
                        Reader.NodeType == XmlNodeType.SignificantWhitespace))
            {
                TextAccessor text = anyTextMapping.Text!;
                if (text.Mapping is SpecialMapping special)
                {
                    if (special.TypeDesc!.Kind == TypeKind.Node)
                    {
                        value = Document.CreateTextNode(Reader.ReadString());
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.XmlInternalError);
                    }
                }
                else
                {
                    if (anyTextMapping.TypeDesc!.IsArrayLike)
                    {
                        if (text.Mapping!.TypeDesc!.CollapseWhitespace)
                        {
                            value = CollapseWhitespace(Reader.ReadString());
                        }
                        else
                        {
                            value = Reader.ReadString();
                        }
                    }
                    else
                    {
                        if (text.Mapping!.TypeDesc == StringTypeDesc || text.Mapping.TypeDesc!.FormatterName == "String")
                        {
                            value = ReadString(null, text.Mapping.TypeDesc.CollapseWhitespace);
                        }
                        else
                        {
                            value = WritePrimitive(text.Mapping, (state) => ((ReflectionXmlSerializationReader)state).Reader.ReadString(), this);
                        }
                    }
                }

                anyText.Source!(value);
                return true;
            }

            return false;
        }

        private bool IsSequence(Member[] members)
        {
            // https://github.com/dotnet/runtime/issues/1402:
            // Currently the reflection based method treat this kind of type as normal types.
            // But potentially we can do some optimization for types that have ordered properties.
            return false;
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private void WriteMemberElementsIf(Member[] expectedMembers, Member? anyElementMember, UnknownNodeAction elementElseAction, Fixup? fixup = null, CheckTypeSource? checkTypeSource = null)
        {
            bool checkType = checkTypeSource != null;
            bool isSequence = IsSequence(expectedMembers);
            if (isSequence)
            {
                // https://github.com/dotnet/runtime/issues/1402:
                // Currently the reflection based method treat this kind of type as normal types.
                // But potentially we can do some optimization for types that have ordered properties.
            }

            ElementAccessor? e = null;
            Member? member = null;
            bool foundElement = false;
            int elementIndex = -1;
            foreach (Member m in expectedMembers)
            {
                if (m.Mapping.Xmlns != null)
                    continue;
                if (m.Mapping.Ignore)
                    continue;
                if (isSequence && (m.Mapping.IsText || m.Mapping.IsAttribute))
                    continue;

                for (int i = 0; i < m.Mapping.Elements!.Length; i++)
                {
                    ElementAccessor ele = m.Mapping.Elements[i];
                    string? ns = ele.Form == XmlSchemaForm.Qualified ? ele.Namespace : string.Empty;
                    if (checkType)
                    {
                        Type elementType;
                        if (ele.Mapping is NullableMapping nullableMapping)
                        {
                            TypeDesc td = nullableMapping.BaseMapping!.TypeDesc!;
                            elementType = td.Type!;
                        }
                        else
                        {
                            elementType = ele.Mapping!.TypeDesc!.Type!;
                        }

                        if (elementType.IsAssignableFrom(checkTypeSource!.Type))
                        {
                            foundElement = true;
                        }
                    }
                    else if (ele.Name == Reader.LocalName && ns == Reader.NamespaceURI)
                    {
                        foundElement = true;
                    }

                    if (foundElement)
                    {
                        e = ele;
                        member = m;
                        elementIndex = i;
                        break;
                    }
                }

                if (foundElement)
                    break;
            }

            if (foundElement)
            {
                if (checkType)
                {
                    member!.Source!(checkTypeSource!.RefObject!);

                    if (member.FixupIndex >= 0)
                    {
                        fixup!.Ids![member.FixupIndex] = checkTypeSource.Id;
                    }
                }
                else
                {
                    string? ns = e!.Form == XmlSchemaForm.Qualified ? e.Namespace : string.Empty;
                    bool isList = member!.Mapping.TypeDesc!.IsArrayLike && !member.Mapping.TypeDesc.IsArray;
                    WriteElement(e, member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite, isList && member.Mapping.TypeDesc.IsNullable, member.Mapping.ReadOnly, ns, member.FixupIndex, elementIndex, fixup, member);
                }
            }
            else
            {
                if (anyElementMember != null && anyElementMember.Mapping != null)
                {
                    MemberMapping anyElement = anyElementMember.Mapping;
                    member = anyElementMember;
                    ElementAccessor[] elements = anyElement.Elements!;
                    for (int i = 0; i < elements.Length; i++)
                    {
                        ElementAccessor element = elements[i];
                        if (element.Any && element.Name.Length == 0)
                        {
                            string? ns = element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty;
                            WriteElement(element, anyElement.CheckSpecified == SpecifiedAccessor.ReadWrite, false, false, ns, fixup: fixup, member: member);
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

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private object? WriteElement(ElementAccessor element, bool checkSpecified, bool checkForNull, bool readOnly, string? defaultNamespace, int fixupIndex = -1, int elementIndex = -1, Fixup? fixup = null, Member? member = null)
        {
            object? value = null;
            if (element.Mapping is ArrayMapping arrayMapping)
            {
                value = WriteArray(arrayMapping, readOnly, element.IsNullable, defaultNamespace, fixupIndex, fixup, member);
            }
            else if (element.Mapping is NullableMapping nullableMapping)
            {
                value = WriteNullableMethod(nullableMapping, true, defaultNamespace);
            }
            else if (!element.Mapping!.IsSoap && (element.Mapping is PrimitiveMapping))
            {
                if (element.IsNullable && ReadNull())
                {
                    if (element.Mapping.TypeDesc!.IsValueType)
                    {
                        value = ReflectionCreateObject(element.Mapping.TypeDesc.Type!);
                    }
                    else
                    {
                        value = null;
                    }
                }
                else if ((element.Default != null && element.Default != DBNull.Value && element.Mapping.TypeDesc!.IsValueType)
                         && (Reader.IsEmptyElement))
                {
                    Reader.Skip();
                }
                else if (element.Mapping.TypeDesc!.Type == typeof(TimeSpan) && Reader.IsEmptyElement)
                {
                    Reader.Skip();
                    value = default(TimeSpan);
                }
                else
                {
                    if (element.Mapping.TypeDesc == QnameTypeDesc)
                    {
                        value = ReadElementQualifiedName();
                    }
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
                            Func<object, string> readFunc = (state) => ((XmlReader)state).ReadElementContentAsString();
                            value = WritePrimitive(element.Mapping, readFunc, Reader);
                        }
                    }
                }
            }
            else if (element.Mapping is StructMapping || (element.Mapping.IsSoap && element.Mapping is PrimitiveMapping))
            {
                TypeMapping mapping = element.Mapping;
                if (mapping.IsSoap)
                {
                    object? rre = fixupIndex >= 0 ?
                          ReadReferencingElement(mapping.TypeName, mapping.Namespace, out fixup!.Ids![fixupIndex])
                        : ReadReferencedElement(mapping.TypeName, mapping.Namespace);

                    if (!mapping.TypeDesc!.IsValueType || rre != null)
                    {
                        value = rre;
                        Referenced(value);
                    }

                    if (fixupIndex >= 0)
                    {
                        if (member == null)
                        {
                            throw new InvalidOperationException(SR.XmlInternalError);
                        }

                        member.Source!(value!);
                        return value;
                    }
                }
                else
                {
                    if (checkForNull && (member!.Source == null && member.ArraySource == null))
                    {
                        Reader.Skip();
                    }
                    else
                    {
                        value = WriteStructMethod(
                                mapping: (StructMapping)mapping,
                                isNullable: mapping.TypeDesc!.IsNullable && element.IsNullable,
                                checkType: true,
                                defaultNamespace: defaultNamespace
                                );
                    }
                }
            }
            else if (element.Mapping is SpecialMapping specialMapping)
            {
                switch (specialMapping.TypeDesc!.Kind)
                {
                    case TypeKind.Node:
                        bool isDoc = specialMapping.TypeDesc.FullName == typeof(XmlDocument).FullName;
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
                            XmlQualifiedName? tser = GetXsiType();
                            if (tser == null || QNameEqual(tser, sm.XsiType!.Name, sm.XsiType.Namespace, defaultNamespace))
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
                            value = ReadSerializable((IXmlSerializable)ReflectionCreateObject(sm.TypeDesc!.Type!)!, isWrappedAny);
                        }

                        if (sm.DerivedMappings != null)
                        {
                            // https://github.com/dotnet/runtime/issues/1401:
                            // To Support SpecialMapping Types Having DerivedMappings
                            throw new NotImplementedException("sm.DerivedMappings != null");
                            //WriteDerivedSerializable(sm, sm, source, isWrappedAny);
                            //WriteUnknownNode("UnknownNode", "null", null, true);
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

            member?.ChoiceSource?.Invoke(element.Name);

            if (member?.ArraySource != null)
            {
                member?.ArraySource(value!);
            }
            else
            {
                member?.Source?.Invoke(value!);
                member?.CheckSpecifiedSource?.Invoke(true);
            }

            return value;
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private XmlSerializationReadCallback CreateXmlSerializationReadCallback(TypeMapping mapping)
        {
            if (mapping is StructMapping structMapping)
            {
                [RequiresUnreferencedCode("calls WriteStructMethod")]
                object? WriteStruct() => WriteStructMethod(structMapping, mapping.TypeDesc!.IsNullable, true, defaultNamespace: null);
                return WriteStruct;
            }
            else if (mapping is EnumMapping enumMapping)
            {
                return () => WriteEnumMethodSoap(enumMapping);
            }
            else if (mapping is NullableMapping nullableMapping)
            {
                [RequiresUnreferencedCode("calls WriteNullableMethod")]
                object? Wrapper() => WriteNullableMethod(nullableMapping, false, null);
                return Wrapper;
            }

            return DummyReadArrayMethod;
        }

        private static void NoopAction(object? o)
        {
        }

        private object? DummyReadArrayMethod()
        {
            UnknownNode(null);
            return null;
        }

        private static Type GetMemberType(MemberInfo memberInfo)
        {
            Type memberType;

            if (memberInfo is FieldInfo fieldInfo)
            {
                memberType = fieldInfo.FieldType;
            }
            else if (memberInfo is PropertyInfo propertyInfo)
            {
                memberType = propertyInfo.PropertyType;
            }
            else
            {
                throw new InvalidOperationException(SR.XmlInternalError);
            }

            return memberType;
        }

        private static bool IsWildcard(SpecialMapping mapping)
        {
            if (mapping is SerializableMapping serializableMapping)
                return serializableMapping.IsAny;

            return mapping.TypeDesc!.CanBeElementValue;
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private object? WriteArray(ArrayMapping arrayMapping, bool readOnly, bool isNullable, string? defaultNamespace, int fixupIndex = -1, Fixup? fixup = null, Member? member = null)
        {
            object? o = null;
            if (arrayMapping.IsSoap)
            {
                object? rre;
                if (fixupIndex >= 0)
                {
                    rre = ReadReferencingElement(arrayMapping.TypeName, arrayMapping.Namespace, out fixup!.Ids![fixupIndex]);
                }
                else
                {
                    rre = ReadReferencedElement(arrayMapping.TypeName, arrayMapping.Namespace);
                }

                TypeDesc td = arrayMapping.TypeDesc!;
                if (rre != null)
                {
                    if (td.IsEnumerable || td.IsCollection)
                    {
                        WriteAddCollectionFixup(member!.GetSource!, member.Source!, rre, td, readOnly);

                        // member.Source has been set at this point.
                        // Setting the source to no-op to avoid setting the
                        // source again.
                        member.Source = NoopAction;
                    }
                    else
                    {
                        if (member == null)
                        {
                            throw new InvalidOperationException(SR.XmlInternalError);
                        }

                        member.Source!(rre);
                    }
                }

                o = rre;
            }
            else
            {
                if (!ReadNull())
                {
                    var memberMapping = new MemberMapping()
                    {
                        Elements = arrayMapping.Elements,
                        TypeDesc = arrayMapping.TypeDesc,
                        ReadOnly = readOnly
                    };

                    Type collectionType = memberMapping.TypeDesc!.Type!;
                    o = ReflectionCreateObject(memberMapping.TypeDesc.Type!);

                    if (memberMapping.ChoiceIdentifier != null)
                    {
                        // https://github.com/dotnet/runtime/issues/1400:
                        // To Support ArrayMapping Types Having ChoiceIdentifier
                        throw new NotImplementedException("memberMapping.ChoiceIdentifier != null");
                    }

                    var arrayMember = new Member(memberMapping);
                    arrayMember.Collection = new CollectionMember();
                    arrayMember.ArraySource = (item) =>
                    {
                        arrayMember.Collection.Add(item);
                    };

                    if ((readOnly && o == null) || Reader.IsEmptyElement)
                    {
                        Reader.Skip();
                    }
                    else
                    {
                        Reader.ReadStartElement();
                        Reader.MoveToContent();
                        while (Reader.NodeType != XmlNodeType.EndElement && Reader.NodeType != XmlNodeType.None)
                        {
                            WriteMemberElements(new Member[] { arrayMember }, UnknownNode, UnknownNode, null, null);
                            Reader.MoveToContent();
                        }
                        ReadEndElement();
                    }

                    SetCollectionObjectWithCollectionMember(ref o, arrayMember.Collection, collectionType);
                }
            }

            return o;
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private object WritePrimitive(TypeMapping mapping, Func<object, string> readFunc, object funcState)
        {
            if (mapping is EnumMapping enumMapping)
            {
                return WriteEnumMethod(enumMapping, readFunc, funcState);
            }
            else if (mapping.TypeDesc == StringTypeDesc)
            {
                return readFunc(funcState);
            }
            else if (mapping.TypeDesc!.FormatterName == "String")
            {
                if (mapping.TypeDesc.CollapseWhitespace)
                {
                    return CollapseWhitespace(readFunc(funcState));
                }
                else
                {
                    return readFunc(funcState);
                }
            }
            else
            {
                if (!mapping.TypeDesc.HasCustomFormatter)
                {
                    string value = readFunc(funcState);
                    object retObj = mapping.TypeDesc.FormatterName switch
                    {
                        "Boolean" => XmlConvert.ToBoolean(value),
                        "Int32" => XmlConvert.ToInt32(value),
                        "Int16" => XmlConvert.ToInt16(value),
                        "Int64" => XmlConvert.ToInt64(value),
                        "Single" => XmlConvert.ToSingle(value),
                        "Double" => XmlConvert.ToDouble(value),
                        "Decimal" => XmlConvert.ToDecimal(value),
                        "Byte" => XmlConvert.ToByte(value),
                        "SByte" => XmlConvert.ToSByte(value),
                        "UInt16" => XmlConvert.ToUInt16(value),
                        "UInt32" => XmlConvert.ToUInt32(value),
                        "UInt64" => XmlConvert.ToUInt64(value),
                        "Guid" => XmlConvert.ToGuid(value),
                        "Char" => XmlConvert.ToChar(value),
                        "TimeSpan" => XmlConvert.ToTimeSpan(value),
                        _ => throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, $"unknown FormatterName: {mapping.TypeDesc.FormatterName}")),
                    };
                    return retObj;
                }
                else
                {
                    string methodName = "To" + mapping.TypeDesc.FormatterName;
                    MethodInfo? method = typeof(XmlSerializationReader).GetMethod(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[] { typeof(string) });
                    if (method == null)
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, $"unknown FormatterName: {mapping.TypeDesc.FormatterName}"));
                    }

                    return method.Invoke(this, new object[] { readFunc(funcState) })!;
                }
            }
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private object? WriteStructMethod(StructMapping mapping, bool isNullable, bool checkType, string? defaultNamespace)
        {
            if (mapping.IsSoap)
                return WriteEncodedStructMethod(mapping);
            else
                return WriteLiteralStructMethod(mapping, isNullable, checkType, defaultNamespace);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private object? WriteNullableMethod(NullableMapping nullableMapping, bool checkType, string? defaultNamespace)
        {
            object? o = Activator.CreateInstance(nullableMapping.TypeDesc!.Type!);
            if (!ReadNull())
            {
                ElementAccessor element = new ElementAccessor();
                element.Mapping = nullableMapping.BaseMapping;
                element.Any = false;
                element.IsNullable = nullableMapping.BaseMapping!.TypeDesc!.IsNullable;

                o = WriteElement(element, false, false, false, defaultNamespace);
            }

            return o;
        }

        private object WriteEnumMethod(EnumMapping mapping, Func<object, string> readFunc, object funcState)
        {
            Debug.Assert(!mapping.IsSoap, "mapping.IsSoap was true. Use WriteEnumMethodSoap for reading SOAP encoded enum value.");
            string source = readFunc(funcState);
            return WriteEnumMethod(mapping, source);
        }

        private object WriteEnumMethodSoap(EnumMapping mapping)
        {
            string source = Reader.ReadElementString();
            return WriteEnumMethod(mapping, source);
        }

        private object WriteEnumMethod(EnumMapping mapping, string source)
        {
            if (mapping.IsFlags)
            {
                Hashtable table = WriteHashtable(mapping, mapping.TypeDesc!.Name);
                return Enum.ToObject(mapping.TypeDesc.Type!, ToEnum(source, table, mapping.TypeDesc.Name));
            }
            else
            {
                foreach (ConstantMapping c in mapping.Constants!)
                {
                    if (string.Equals(c.XmlName, source))
                    {
                        return Enum.Parse(mapping.TypeDesc!.Type!, c.Name);
                    }
                }

                throw CreateUnknownConstantException(source, mapping.TypeDesc!.Type!);
            }
        }

        private Hashtable WriteHashtable(EnumMapping mapping, string name)
        {
            var h = new Hashtable();
            ConstantMapping[] constants = mapping.Constants!;
            for (int i = 0; i < constants.Length; i++)
            {
                h.Add(constants[i].XmlName, constants[i].Value);
            }

            return h;
        }

        private object? ReflectionCreateObject(
            [DynamicallyAccessedMembers(TrimmerConstants.AllMethods)] Type type)
        {
            object? obj;
            if (type.IsArray)
            {
                obj = Activator.CreateInstance(type, 32);
            }
            else
            {
                ConstructorInfo? ci = GetDefaultConstructor(type);
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

        private ConstructorInfo? GetDefaultConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
                | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type) =>
            type.IsValueType ? null :
            type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private object? WriteEncodedStructMethod(StructMapping structMapping)
        {
            if (structMapping.TypeDesc!.IsRoot)
                return null;

            Member[]? members = null;

            if (structMapping.TypeDesc.IsAbstract)
            {
                throw CreateAbstractTypeException(structMapping.TypeName!, structMapping.Namespace);
            }
            else
            {
                object? o = ReflectionCreateObject(structMapping.TypeDesc.Type!);

                MemberMapping[] mappings = TypeScope.GetSettableMembers(structMapping);
                members = new Member[mappings.Length];
                for (int i = 0; i < mappings.Length; i++)
                {
                    MemberMapping mapping = mappings[i];
                    var member = new Member(mapping);

                    TypeDesc td = member.Mapping.TypeDesc!;
                    if (td.IsCollection || td.IsEnumerable)
                    {
                        member.Source = Wrapper;
                        [RequiresUnreferencedCode("Calls WriteAddCollectionFixup")]
                        void Wrapper(object? value)
                        {
                            WriteAddCollectionFixup(o!, member, value!);
                        }
                    }
                    else if (!member.Mapping.ReadOnly)
                    {
                        var setterDelegate = GetSetMemberValueDelegate(o!, member.Mapping.MemberInfo!.Name);
                        member.Source = (value) => setterDelegate(o, value);
                    }
                    else
                    {
                        member.Source = NoopAction;
                    }

                    members[i] = member;
                }

                Fixup? fixup = WriteMemberFixupBegin(members, o);
                UnknownNodeAction unknownNodeAction = (_) => UnknownNode(o);
                WriteAttributes(members, null, unknownNodeAction, ref o);
                Reader.MoveToElement();
                if (Reader.IsEmptyElement)
                {
                    Reader.Skip();
                    return o;
                }

                Reader.ReadStartElement();
                Reader.MoveToContent();
                while (Reader.NodeType != XmlNodeType.EndElement && Reader.NodeType != XmlNodeType.None)
                {
                    WriteMemberElements(members, UnknownNode, UnknownNode, null, null, fixup: fixup);
                    Reader.MoveToContent();
                }

                ReadEndElement();
                return o;
            }
        }

        private Fixup? WriteMemberFixupBegin(Member[] members, object? o)
        {
            int fixupCount = 0;
            foreach (Member member in members)
            {
                if (member.Mapping.Elements!.Length == 0)
                    continue;

                TypeMapping? mapping = member.Mapping.Elements[0].Mapping;
                if (mapping is StructMapping || mapping is ArrayMapping || mapping is PrimitiveMapping || mapping is NullableMapping)
                {
                    member.MultiRef = true;
                    member.FixupIndex = fixupCount++;
                }
            }

            Fixup? fixup;
            if (fixupCount > 0)
            {
                fixup = new Fixup(o, CreateWriteFixupMethod(members), fixupCount);
                AddFixup(fixup);
            }
            else
            {
                fixup = null;
            }

            return fixup;
        }

        private XmlSerializationFixupCallback CreateWriteFixupMethod(Member[] members)
        {
            return (fixupObject) =>
            {
                var fixup = (Fixup)fixupObject;
                string[] ids = fixup.Ids!;
                foreach (Member member in members)
                {
                    if (member.MultiRef)
                    {
                        int fixupIndex = member.FixupIndex;
                        if (ids[fixupIndex] != null)
                        {
                            var memberValue = GetTarget(ids[fixupIndex]);
                            member.Source!(memberValue);
                        }
                    }
                }
            };
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private void WriteAddCollectionFixup(object o, Member member, object memberValue)
        {
            TypeDesc typeDesc = member.Mapping.TypeDesc!;
            bool readOnly = member.Mapping.ReadOnly;
            Func<object?> getSource = () => GetMemberValue(o, member.Mapping.MemberInfo!);
            var setterDelegate = GetSetMemberValueDelegate(o, member.Mapping.MemberInfo!.Name);
            Action<object?> setSource = (value) => setterDelegate(o, value);
            WriteAddCollectionFixup(getSource, setSource, memberValue, typeDesc, readOnly);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private object? WriteAddCollectionFixup(Func<object?> getSource, Action<object?> setSource, object memberValue, TypeDesc typeDesc, bool readOnly)
        {
            object? memberSource = getSource();
            if (memberSource == null)
            {
                if (readOnly)
                {
                    throw CreateReadOnlyCollectionException(typeDesc.CSharpName);
                }

                memberSource = ReflectionCreateObject(typeDesc.Type!);
                setSource(memberSource);
            }

            var collectionFixup = new CollectionFixup(
                memberSource,
                new XmlSerializationCollectionFixupCallback(GetCreateCollectionOfObjectsCallback(typeDesc.Type!)),
                memberValue);

            AddFixup(collectionFixup);
            return memberSource;
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private XmlSerializationCollectionFixupCallback GetCreateCollectionOfObjectsCallback(Type collectionType)
        {
            return Wrapper;
            [RequiresUnreferencedCode("Calls AddObjectsIntoTargetCollection")]
            void Wrapper(object? collection, object? collectionItems)
            {
                if (collectionItems == null)
                    return;

                if (collection == null)
                    return;

                var listOfItems = new List<object?>();
                if (collectionItems is IEnumerable enumerableItems)
                {
                    foreach (var item in enumerableItems)
                    {
                        listOfItems.Add(item);
                    }
                }
                else
                {
                    throw new InvalidOperationException(SR.XmlInternalError);
                }

                AddObjectsIntoTargetCollection(collection, listOfItems, collectionType);
            }
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private object? WriteLiteralStructMethod(StructMapping structMapping, bool isNullable, bool checkType, string? defaultNamespace)
        {
            XmlQualifiedName? xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable)
            {
                isNull = ReadNull();
            }

            if (checkType)
            {
                if (structMapping.TypeDesc!.IsRoot && isNull)
                {
                    if (xsiType != null)
                    {
                        return ReadTypedNull(xsiType);
                    }
                    else
                    {
                        if (structMapping.TypeDesc.IsValueType)
                        {
                            return ReflectionCreateObject(structMapping.TypeDesc.Type!);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }

                object? o = null;
                if (xsiType == null || (!structMapping.TypeDesc.IsRoot && QNameEqual(xsiType, structMapping.TypeName, structMapping.Namespace, defaultNamespace)))
                {
                    if (structMapping.TypeDesc.IsRoot)
                    {
                        return ReadTypedPrimitive(new XmlQualifiedName(Soap.UrType, XmlReservedNs.NsXs));
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

            if (structMapping.TypeDesc!.IsNullable && isNull)
            {
                return null;
            }
            else if (structMapping.TypeDesc.IsAbstract)
            {
                throw CreateAbstractTypeException(structMapping.TypeName!, structMapping.Namespace);
            }
            else
            {
                if (structMapping.TypeDesc.Type != null && typeof(XmlSchemaObject).IsAssignableFrom(structMapping.TypeDesc.Type))
                {
                    // https://github.com/dotnet/runtime/issues/1399:
                    // To Support Serializing XmlSchemaObject
                    throw new NotImplementedException(nameof(XmlSchemaObject));
                }

                object? o = ReflectionCreateObject(structMapping.TypeDesc.Type!)!;

                MemberMapping[] mappings = TypeScope.GetSettableMembers(structMapping);
                MemberMapping? anyText = null;
                MemberMapping? anyElement = null;
                Member? anyAttribute = null;
                Member? anyElementMember = null;
                Member? anyTextMember = null;

                bool isSequence = structMapping.HasExplicitSequence();

                if (isSequence)
                {
                    // https://github.com/dotnet/runtime/issues/1402:
                    // Currently the reflection based method treat this kind of type as normal types.
                    // But potentially we can do some optimization for types that have ordered properties.
                }

                var allMembersList = new List<Member>(mappings.Length);
                var allMemberMappingList = new List<MemberMapping>(mappings.Length);

                for (int i = 0; i < mappings.Length; i++)
                {
                    MemberMapping mapping = mappings[i];
                    var member = new Member(mapping);

                    if (mapping.Text != null)
                    {
                        anyText = mapping;
                    }

                    if (mapping.Attribute != null)
                    {
                        member.Source = Wrapper;
                        [RequiresUnreferencedCode("calls SetOrAddValueToMember")]
                        void Wrapper(object? value) { SetOrAddValueToMember(o!, value!, member.Mapping.MemberInfo!); }

                        if (mapping.Attribute.Any)
                        {
                            anyAttribute = member;
                        }
                    }

                    if (!isSequence)
                    {
                        // find anyElement if present.
                        for (int j = 0; j < mapping.Elements!.Length; j++)
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
                        structMapping.FindDeclaringMapping(mapping, out StructMapping? declaringMapping, structMapping.TypeName!);
                        throw new InvalidOperationException(SR.Format(SR.XmlSequenceHierarchy, structMapping.TypeDesc.FullName, mapping.Name, declaringMapping!.TypeDesc!.FullName, "Order"));
                    }

                    if (mapping.TypeDesc!.IsArrayLike)
                    {
                        if (member.Source == null && mapping.TypeDesc.IsArrayLike && !(mapping.Elements!.Length == 1 && mapping.Elements[0].Mapping is ArrayMapping))
                        {
                            member.Source = (item) =>
                            {
                                if (member.Collection == null)
                                {
                                    member.Collection = new CollectionMember();
                                }

                                member.Collection.Add(item);
                            };
                            member.ArraySource = member.Source;
                        }
                        else if (!mapping.TypeDesc.IsArray)
                        {

                        }
                    }

                    if (member.Source == null)
                    {
                        var pi = member.Mapping.MemberInfo as PropertyInfo;
                        if (pi != null && typeof(IList).IsAssignableFrom(pi.PropertyType)
                            && (pi.SetMethod == null || !pi.SetMethod.IsPublic))
                        {
                            member.Source = (value) =>
                            {
                                var getOnlyList = (IList)pi.GetValue(o)!;
                                if (value is IList valueList)
                                {
                                    foreach (var v in valueList)
                                    {
                                        getOnlyList.Add(v);
                                    }
                                }
                                else
                                {
                                    getOnlyList.Add(value);
                                }
                            };
                        }
                        else
                        {
                            if (member.Mapping.Xmlns != null)
                            {
                                var xmlSerializerNamespaces = new XmlSerializerNamespaces();
                                var setMemberValue = GetSetMemberValueDelegate(o!, member.Mapping.Name);
                                setMemberValue(o, xmlSerializerNamespaces);
                                member.XmlnsSource = (ns, name) =>
                                {
                                    xmlSerializerNamespaces.Add(ns, name);
                                };
                            }
                            else
                            {
                                var setterDelegate = GetSetMemberValueDelegate(o!, member.Mapping.Name);
                                member.Source = (value) => setterDelegate(o, value);
                            }
                        }
                    }

                    if (member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
                    {
                        member.CheckSpecifiedSource = Wrapper;
                        [RequiresUnreferencedCode("calls GetType on object")]
                        void Wrapper(object? _)
                        {
                            string specifiedMemberName = member.Mapping.Name + "Specified";
                            MethodInfo? specifiedMethodInfo = o!.GetType().GetMethod("set_" + specifiedMemberName);
                            if (specifiedMethodInfo != null)
                            {
                                specifiedMethodInfo.Invoke(o, new object[] { true });
                            }
                        };
                    }

                    ChoiceIdentifierAccessor? choice = mapping.ChoiceIdentifier;
                    if (choice != null && o != null)
                    {
                        member.ChoiceSource = Wrapper;
                        [RequiresUnreferencedCode("Calls SetOrAddValueToMember")]
                        void Wrapper(object elementNameObject)
                        {
                            string? elementName = elementNameObject as string;
                            foreach (var name in choice.MemberIds!)
                            {
                                if (name == elementName)
                                {
                                    object choiceValue = Enum.Parse(choice.Mapping!.TypeDesc!.Type!, name);
                                    SetOrAddValueToMember(o, choiceValue, choice.MemberInfo!);

                                    break;
                                }
                            }
                        };
                    }

                    allMemberMappingList.Add(mapping);
                    allMembersList.Add(member);

                    if (mapping == anyElement)
                    {
                        anyElementMember = member;
                    }
                    else if (mapping == anyText)
                    {
                        anyTextMember = member;
                    }
                }

                Member[] allMembers = allMembersList.ToArray();

                UnknownNodeAction unknownNodeAction = (_) => UnknownNode(o);
                WriteAttributes(allMembers, anyAttribute, unknownNodeAction, ref o);

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
                    // https://github.com/dotnet/runtime/issues/1402:
                    // Currently the reflection based method treat this kind of type as normal types.
                    // But potentially we can do some optimization for types that have ordered properties.
                }

                WriteMembers(ref o, allMembers, unknownNodeAction, unknownNodeAction, anyElementMember, anyTextMember);

                foreach (Member member in allMembers)
                {
                    if (member.Collection != null)
                    {
                        MemberInfo[] memberInfos = o!.GetType().GetMember(member.Mapping.Name);
                        MemberInfo memberInfo = memberInfos[0];
                        object? collection = null;
                        SetCollectionObjectWithCollectionMember(ref collection, member.Collection, member.Mapping.TypeDesc!.Type!);
                        var setMemberValue = GetSetMemberValueDelegate(o, memberInfo.Name);
                        setMemberValue(o, collection);
                    }
                }

                ReadEndElement();
                return o;
            }
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private bool WriteEnumAndArrayTypes(out object? o, StructMapping mapping, XmlQualifiedName xsiType, string? defaultNamespace)
        {
            foreach (var m in _mapping.Scope!.TypeMappings)
            {
                if (m is EnumMapping enumMapping)
                {
                    if (QNameEqual(xsiType, enumMapping.TypeName, enumMapping.Namespace, defaultNamespace))
                    {
                        Reader.ReadStartElement();
                        Func<object, string> functor = (state) =>
                        {
                            var reader = (ReflectionXmlSerializationReader)state;
                            return reader.CollapseWhitespace(reader.Reader.ReadString());
                        };
                        o = WriteEnumMethod(enumMapping, functor, this);
                        ReadEndElement();
                        return true;
                    }

                    continue;
                }

                if (m is ArrayMapping arrayMapping)
                {
                    if (QNameEqual(xsiType, arrayMapping.TypeName, arrayMapping.Namespace, defaultNamespace))
                    {
                        o = WriteArray(arrayMapping, false, false, defaultNamespace);
                        return true;
                    }

                    continue;
                }
            }

            o = null;
            return false;
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private bool WriteDerivedTypes(out object? o, StructMapping mapping, XmlQualifiedName xsiType, string? defaultNamespace, bool checkType, bool isNullable)
        {
            for (StructMapping? derived = mapping.DerivedMappings; derived != null; derived = derived.NextDerivedMapping)
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

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private void WriteAttributes(Member[] members, Member? anyAttribute, UnknownNodeAction elseCall, ref object? o)
        {
            Member? xmlnsMember = null;
            var attributes = new List<AttributeAccessor>();
            foreach (Member member in members)
            {
                if (member.Mapping.Xmlns != null)
                {
                    xmlnsMember = member;
                    break;
                }
            }

            while (Reader.MoveToNextAttribute())
            {
                bool memberFound = false;
                foreach (Member member in members)
                {
                    if (member.Mapping.Xmlns != null || member.Mapping.Ignore)
                    {
                        continue;
                    }

                    AttributeAccessor? attribute = member.Mapping.Attribute;

                    if (attribute == null) continue;
                    if (attribute.Any) continue;

                    attributes.Add(attribute);

                    if (attribute.IsSpecialXmlNamespace)
                    {
                        memberFound = XmlNodeEqual(Reader, attribute.Name, XmlReservedNs.NsXml);
                    }
                    else
                    {
                        memberFound = XmlNodeEqual(Reader, attribute.Name, attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : string.Empty);
                    }

                    if (memberFound)
                    {
                        WriteAttribute(member);
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
                        Debug.Assert(xmlnsMember.XmlnsSource != null, "Xmlns member's source was not set.");
                        xmlnsMember.XmlnsSource(Reader.Name.Length == 5 ? string.Empty : Reader.LocalName, Reader.Value);
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
                        var attr = (Document.ReadNode(Reader) as XmlAttribute)!;
                        ParseWsdlArrayType(attr);
                        WriteAttribute(anyAttribute, attr);
                    }
                    else
                    {
                        elseCall(o);
                    }
                }
            }
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private void WriteAttribute(Member member, object? attr = null)
        {
            AttributeAccessor attribute = member.Mapping.Attribute!;
            object? value = null;
            if (attribute.Mapping is SpecialMapping special)
            {
                if (special.TypeDesc!.Kind == TypeKind.Attribute)
                {
                    value = attr;
                }
                else if (special.TypeDesc.CanBeAttributeValue)
                {
                    // https://github.com/dotnet/runtime/issues/1398:
                    // To Support special.TypeDesc.CanBeAttributeValue == true
                    throw new NotImplementedException("special.TypeDesc.CanBeAttributeValue");
                }
                else
                    throw new InvalidOperationException(SR.XmlInternalError);
            }
            else
            {
                if (attribute.IsList)
                {
                    string listValues = Reader.Value;
                    string[] vals = listValues.Split(null);
                    Array arrayValue = Array.CreateInstance(member.Mapping.TypeDesc!.Type!.GetElementType()!, vals.Length);
                    for (int i = 0; i < vals.Length; i++)
                    {
                        arrayValue.SetValue(WritePrimitive(attribute.Mapping!, (state) => ((string[])state)[i], vals), i);
                    }

                    value = arrayValue;
                }
                else
                {
                    value = WritePrimitive(attribute.Mapping!, (state) => ((XmlReader)state).Value, Reader);
                }
            }

            member.Source!(value);

            if (member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
            {
                member.CheckSpecifiedSource?.Invoke(null);
            }
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private void SetOrAddValueToMember(object o, object value, MemberInfo memberInfo)
        {
            Type memberType = GetMemberType(memberInfo);

            if (memberType == value.GetType())
            {
                var setMemberValue = GetSetMemberValueDelegate(o, memberInfo.Name);
                setMemberValue(o, value);
            }
            else if (memberType.IsArray)
            {
                AddItemInArrayMember(o, memberInfo, memberType, value);
            }
            else
            {
                var setMemberValue = GetSetMemberValueDelegate(o, memberInfo.Name);
                setMemberValue(o, value);
            }
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        private void AddItemInArrayMember(object o, MemberInfo memberInfo, Type memberType, object item)
        {
            var currentArray = (Array?)GetMemberValue(o, memberInfo);
            int length;
            if (currentArray == null)
            {
                length = 0;
            }
            else
            {
                length = currentArray.Length;
            }

            var newArray = Array.CreateInstance(memberType.GetElementType()!, length + 1);
            if (currentArray != null)
            {
                Array.Copy(currentArray, newArray, length);
            }

            newArray.SetValue(item, length);
            var setMemberValue = GetSetMemberValueDelegate(o, memberInfo.Name);
            setMemberValue(o, newArray);
        }

        // WriteXmlNodeEqual
        private bool XmlNodeEqual(XmlReader source, string name, string? ns)
        {
            return source.LocalName == name && string.Equals(source.NamespaceURI, ns);
        }

        private bool QNameEqual(XmlQualifiedName xsiType, string? name, string? ns, string? defaultNamespace)
        {
            return xsiType.Name == name && string.Equals(xsiType.Namespace, defaultNamespace);
        }

        private void CreateUnknownNodeException(object? o)
        {
            CreateUnknownNodeException();
        }

        internal sealed class CollectionMember : List<object?>
        {
        }

        internal sealed class Member
        {
            public MemberMapping Mapping;
            public CollectionMember? Collection;
            public int FixupIndex = -1;
            public bool MultiRef;
            public Action<object?>? Source;
            public Func<object?>? GetSource;
            public Action<object>? ArraySource;
            public Action<object?>? CheckSpecifiedSource;
            public Action<object>? ChoiceSource;
            public Action<string, string>? XmlnsSource;

            public Member(MemberMapping mapping)
            {
                Mapping = mapping;
            }
        }

        internal sealed class CheckTypeSource
        {
            public string? Id { get; set; }
            public bool IsObject { get; set; }
            public Type? Type { get; set; }
            public object? RefObject { get; set; }
        }

        internal sealed class ObjectHolder
        {
            public object? Object;
        }
    }

    internal static class ReflectionXmlSerializationReaderHelper
    {
        public delegate void SetMemberValueDelegate(object? o, object? val);

        public static SetMemberValueDelegate GetSetMemberValueDelegateWithType<TObj, TParam>(MemberInfo memberInfo)
        {
            if (typeof(TObj).IsValueType)
            {
                if (memberInfo is PropertyInfo propInfo)
                {
                    return delegate (object? o, object? p)
                    {
                        propInfo.SetValue(o, p);
                    };
                }
                else if (memberInfo is FieldInfo fieldInfo)
                {
                    return delegate (object? o, object? p)
                    {
                        fieldInfo.SetValue(o, p);
                    };
                }

                throw new InvalidOperationException(SR.XmlInternalError);
            }
            else
            {
                Action<TObj, TParam>? setTypedDelegate = null;
                if (memberInfo is PropertyInfo propInfo)
                {
                    var setMethod = propInfo.GetSetMethod(true);
                    if (setMethod == null)
                    {
                        return delegate (object? o, object? p)
                        {
                            // Maintain the same failure behavior as non-cached delegate
                            propInfo.SetValue(o, p);
                        };
                    }

                    setTypedDelegate = (Action<TObj, TParam>)setMethod.CreateDelegate(typeof(Action<TObj, TParam>));
                }
                else if (memberInfo is FieldInfo fieldInfo)
                {
                    var objectParam = Expression.Parameter(typeof(TObj));
                    var valueParam = Expression.Parameter(typeof(TParam));
                    var fieldExpr = Expression.Field(objectParam, fieldInfo);
                    var assignExpr = Expression.Assign(fieldExpr, valueParam);
                    setTypedDelegate = Expression.Lambda<Action<TObj, TParam>>(assignExpr, objectParam, valueParam).Compile();
                }

                return delegate (object? o, object? p)
                {
                    setTypedDelegate!((TObj)o!, (TParam)p!);
                };
            }
        }
    }
}
