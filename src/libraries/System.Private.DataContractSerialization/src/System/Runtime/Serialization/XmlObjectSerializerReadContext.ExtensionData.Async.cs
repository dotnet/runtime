// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization.DataContracts;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization;

internal partial class XmlObjectSerializerReadContext
{
    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async ValueTask<ExtensionDataMember> ReadExtensionDataMemberAsync(XmlReaderDelegator reader, int memberIndex)
    {
        ExtensionDataMember member = new ExtensionDataMember(reader.LocalName, reader.NamespaceURI) { MemberIndex = memberIndex };
        member.Value = reader.UnderlyingExtensionDataReader is not null
            ? reader.UnderlyingExtensionDataReader.GetCurrentNode()
            : await ReadExtensionDataValueAsync(reader).ConfigureAwait(false);
        return member;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal async ValueTask<IDataNode?> ReadExtensionDataValueAsync(XmlReaderDelegator reader)
    {
        ReadAttributes(reader);
        IncrementItemCount(1);
        if (attributes.Ref != Globals.NewObjectId)
        {
            string reference = attributes.Ref;
            await reader.SkipAsync(CancellationToken).ConfigureAwait(false);
            object value = GetExistingObjectOrExtensionData(reference);
            IDataNode node = value is IDataNode dataNode ? dataNode : new DataNode<object>(value);
            node.Id = reference;
            return node;
        }
        if (attributes.XsiNil)
        {
            await reader.SkipAsync(CancellationToken).ConfigureAwait(false);
            return null;
        }

        string? name = attributes.XsiTypeName;
        string? ns = attributes.XsiTypeNamespace;
        if (IsReadingCollectionExtensionData(reader))
        {
            await ReadRequiredAsync(reader).ConfigureAwait(false);
            return await ReadUnknownCollectionDataAsync(reader, name, ns).ConfigureAwait(false);
        }
        if (attributes.FactoryTypeName is not null)
        {
            await ReadRequiredAsync(reader).ConfigureAwait(false);
            return await ReadUnknownISerializableDataAsync(reader, name, ns).ConfigureAwait(false);
        }
        if (IsReadingClassExtensionData(reader))
        {
            await ReadRequiredAsync(reader).ConfigureAwait(false);
            return await ReadUnknownClassDataAsync(reader, name, ns).ConfigureAwait(false);
        }

        DataContract? contract = ResolveDataContractFromTypeName();
        if (contract is null)
        {
            return await ReadUnrecognizedExtensionDataValueAsync(reader, name, ns).ConfigureAwait(false);
        }
        if (contract is XmlDataContract)
        {
            return await ReadUnknownXmlDataAsync(reader, name, ns).ConfigureAwait(false);
        }
        if (contract.IsISerializable)
        {
            await ReadRequiredAsync(reader).ConfigureAwait(false);
            return await ReadUnknownISerializableDataAsync(reader, name, ns).ConfigureAwait(false);
        }
        if (contract is PrimitiveDataContract)
        {
            if (attributes.Id == Globals.NewObjectId)
            {
                await ReadRequiredAsync(reader).ConfigureAwait(false);
                await reader.MoveToContentAsync(CancellationToken).ConfigureAwait(false);
                IDataNode node = await ReadUnknownPrimitiveDataAsync(reader, contract.UnderlyingType, name, ns).ConfigureAwait(false);
                await reader.ReadEndElementAsync(CancellationToken).ConfigureAwait(false);
                return node;
            }
            object value = await reader.ReadElementContentAsAnyTypeAsync(contract.UnderlyingType, CancellationToken).ConfigureAwait(false);
            IDataNode boxed = new DataNode<object>(value);
            InitializeExtensionDataNode(boxed, name, ns);
            return boxed;
        }
        if (contract is EnumDataContract enumContract)
        {
            string lexical = (string)await reader.ReadReferencePrimitiveElementAsync<ReflectionPlan.StringValue>(CancellationToken).ConfigureAwait(false);
            IDataNode node = new DataNode<object>(AsyncDataContractOperations.ParseEnum(enumContract, lexical));
            InitializeExtensionDataNode(node, name, ns);
            return node;
        }
        if (contract is ClassDataContract)
        {
            await ReadRequiredAsync(reader).ConfigureAwait(false);
            return await ReadUnknownClassDataAsync(reader, name, ns).ConfigureAwait(false);
        }
        if (contract is CollectionDataContract)
        {
            await ReadRequiredAsync(reader).ConfigureAwait(false);
            return await ReadUnknownCollectionDataAsync(reader, name, ns).ConfigureAwait(false);
        }

        throw new InvalidDataContractException(SR.Format(SR.UnexpectedContractType, DataContract.GetClrTypeFullName(contract.GetType()), DataContract.GetClrTypeFullName(contract.UnderlyingType)));
    }

    private async ValueTask ReadRequiredAsync(XmlReaderDelegator reader)
    {
        if (!await reader.ReadAsync(CancellationToken).ConfigureAwait(false))
        {
            throw XmlObjectSerializer.CreateSerializationException(SR.UnexpectedEndOfFile);
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async ValueTask<IDataNode> ReadUnrecognizedExtensionDataValueAsync(XmlReaderDelegator reader, string? name, string? ns)
    {
        Debug.Assert(attributes is not null);
        StartReadExtensionDataValue(reader);
        if (attributes.UnrecognizedAttributesFound)
        {
            return await ReadUnknownXmlDataAsync(reader, name, ns).ConfigureAwait(false);
        }

        IDictionary<string, string>? namespaces = reader.GetNamespacesInScope(XmlNamespaceScope.ExcludeXml);
        await ReadRequiredAsync(reader).ConfigureAwait(false);
        await reader.MoveToContentAsync(CancellationToken).ConfigureAwait(false);
        switch (reader.NodeType)
        {
            case XmlNodeType.Text:
            case XmlNodeType.CDATA:
                return await ReadPrimitiveExtensionDataValueAsync(reader, name, ns).ConfigureAwait(false);
            case XmlNodeType.Element:
                return reader.NamespaceURI.StartsWith(Globals.DataContractXsdBaseNamespace, StringComparison.Ordinal)
                    ? await ReadUnknownClassDataAsync(reader, name, ns).ConfigureAwait(false)
                    : await ReadAndResolveUnknownXmlDataAsync(reader, namespaces, name, ns).ConfigureAwait(false);
            case XmlNodeType.EndElement:
                IDataNode node = await ReadUnknownPrimitiveDataAsync(reader, typeof(object), name, ns).ConfigureAwait(false);
                await reader.ReadEndElementAsync(CancellationToken).ConfigureAwait(false);
                node.IsFinalValue = false;
                return node;
            default:
                throw CreateUnexpectedStateException(XmlNodeType.Element, reader);
        }
    }

    protected virtual async ValueTask<IDataNode> ReadPrimitiveExtensionDataValueAsync(XmlReaderDelegator reader, string? name, string? ns)
    {
        if (reader.ValueType == typeof(string))
        {
            IDataNode node = new DataNode<object>(await reader.ReadContentAsStringAsync(CancellationToken).ConfigureAwait(false));
            InitializeExtensionDataNode(node, name, ns);
            node.IsFinalValue = false;
            await reader.ReadEndElementAsync(CancellationToken).ConfigureAwait(false);
            return node;
        }

        IDataNode value = await ReadUnknownPrimitiveDataAsync(reader, reader.ValueType, name, ns).ConfigureAwait(false);
        await reader.ReadEndElementAsync(CancellationToken).ConfigureAwait(false);
        return value;
    }

    private async ValueTask<IDataNode> ReadUnknownPrimitiveDataAsync(XmlReaderDelegator reader, Type type, string? name, string? ns)
    {
        IDataNode node = await reader.ReadExtensionDataAsync(type, CancellationToken).ConfigureAwait(false);
        InitializeExtensionDataNode(node, name, ns);
        return node;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async ValueTask<ClassDataNode> ReadUnknownClassDataAsync(XmlReaderDelegator reader, string? name, string? ns)
    {
        ClassDataNode node = new ClassDataNode();
        InitializeExtensionDataNode(node, name, ns);
        int memberIndex = 0;
        XmlNodeType nodeType;
        while ((nodeType = await reader.MoveToContentAsync(CancellationToken).ConfigureAwait(false)) != XmlNodeType.EndElement)
        {
            if (nodeType != XmlNodeType.Element)
            {
                throw CreateUnexpectedStateException(XmlNodeType.Element, reader);
            }
            node.Members ??= new List<ExtensionDataMember>();
            node.Members.Add(await ReadExtensionDataMemberAsync(reader, memberIndex++).ConfigureAwait(false));
        }
        await reader.ReadEndElementAsync(CancellationToken).ConfigureAwait(false);
        return node;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async ValueTask<CollectionDataNode> ReadUnknownCollectionDataAsync(XmlReaderDelegator reader, string? name, string? ns)
    {
        Debug.Assert(attributes is not null);
        CollectionDataNode node = new CollectionDataNode();
        InitializeExtensionDataNode(node, name, ns);
        int size = attributes.ArraySZSize;
        XmlNodeType nodeType;
        while ((nodeType = await reader.MoveToContentAsync(CancellationToken).ConfigureAwait(false)) != XmlNodeType.EndElement)
        {
            if (nodeType != XmlNodeType.Element)
            {
                throw CreateUnexpectedStateException(XmlNodeType.Element, reader);
            }
            node.ItemName ??= reader.LocalName;
            node.ItemNamespace ??= reader.NamespaceURI;
            if (reader.IsLocalName(node.ItemName) && reader.IsNamespaceURI(node.ItemNamespace))
            {
                node.Items ??= new List<IDataNode?>();
                node.Items.Add(await ReadExtensionDataValueAsync(reader).ConfigureAwait(false));
            }
            else
            {
                await SkipUnknownElementAsync(reader).ConfigureAwait(false);
            }
        }
        await reader.ReadEndElementAsync(CancellationToken).ConfigureAwait(false);
        int count = node.Items?.Count ?? 0;
        if (size >= 0 && size != count)
        {
            throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ArraySizeAttributeIncorrect, size, count));
        }
        node.Size = size >= 0 ? size : count;
        return node;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async ValueTask<ISerializableDataNode> ReadUnknownISerializableDataAsync(XmlReaderDelegator reader, string? name, string? ns)
    {
        Debug.Assert(attributes is not null);
        ISerializableDataNode node = new ISerializableDataNode();
        InitializeExtensionDataNode(node, name, ns);
        node.FactoryTypeName = attributes.FactoryTypeName;
        node.FactoryTypeNamespace = attributes.FactoryTypeNamespace;
        XmlNodeType nodeType;
        while ((nodeType = await reader.MoveToContentAsync(CancellationToken).ConfigureAwait(false)) != XmlNodeType.EndElement)
        {
            if (nodeType != XmlNodeType.Element)
            {
                throw CreateUnexpectedStateException(XmlNodeType.Element, reader);
            }
            if (reader.NamespaceURI.Length != 0)
            {
                await SkipUnknownElementAsync(reader).ConfigureAwait(false);
                continue;
            }
            ISerializableDataMember member = new ISerializableDataMember(reader.LocalName);
            member.Value = await ReadExtensionDataValueAsync(reader).ConfigureAwait(false);
            node.Members ??= new List<ISerializableDataMember>();
            node.Members.Add(member);
        }
        await reader.ReadEndElementAsync(CancellationToken).ConfigureAwait(false);
        return node;
    }

    private async ValueTask<XmlDataNode> ReadUnknownXmlDataAsync(XmlReaderDelegator reader, string? name, string? ns)
    {
        XmlDataNode node = new XmlDataNode { OwnerDocument = Document };
        InitializeExtensionDataNode(node, name, ns);
        if (reader.NodeType == XmlNodeType.EndElement)
        {
            return node;
        }
        List<XmlAttribute>? attributes = null;
        List<XmlNode>? children = null;
        XmlNodeType nodeType = await reader.MoveToContentAsync(CancellationToken).ConfigureAwait(false);
        if (nodeType is not (XmlNodeType.Text or XmlNodeType.CDATA))
        {
            while (reader.MoveToNextAttribute())
            {
                string attributeNamespace = reader.NamespaceURI;
                if (attributeNamespace != Globals.SerializationNamespace && attributeNamespace != Globals.SchemaInstanceNamespace)
                {
                    XmlAttribute attribute = Document.CreateAttribute(reader.UnderlyingReader.Prefix, reader.LocalName, attributeNamespace);
                    attribute.Value = reader.Value;
                    attributes ??= new List<XmlAttribute>();
                    attributes.Add(attribute);
                }
            }
            reader.MoveToElement();
            await ReadRequiredAsync(reader).ConfigureAwait(false);
        }
        while (await reader.MoveToContentAsync(CancellationToken).ConfigureAwait(false) != XmlNodeType.EndElement)
        {
            if (reader.EOF)
            {
                throw XmlObjectSerializer.CreateSerializationException(SR.UnexpectedEndOfFile);
            }
            children ??= new List<XmlNode>();
            children.Add(await AsyncXmlSerializableServices.ReadNodeAsync(reader, Document, CancellationToken).ConfigureAwait(false));
        }
        await reader.ReadEndElementAsync(CancellationToken).ConfigureAwait(false);
        node.XmlAttributes = attributes;
        node.XmlChildNodes = children;
        return node;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async ValueTask<IDataNode> ReadAndResolveUnknownXmlDataAsync(XmlReaderDelegator reader, IDictionary<string, string>? namespaces, string? name, string? ns)
    {
        bool serializable = true;
        bool collection = true;
        bool classData = true;
        string? itemName = null;
        string? itemNamespace = null;
        List<XmlNode> children = new List<XmlNode>();
        List<XmlAttribute>? attributes = null;
        if (namespaces is not null)
        {
            attributes = new List<XmlAttribute>();
            foreach (KeyValuePair<string, string> pair in namespaces)
            {
                attributes.Add(AddNamespaceDeclaration(pair.Key, pair.Value));
            }
        }
        XmlNodeType nodeType;
        while ((nodeType = reader.NodeType) != XmlNodeType.EndElement)
        {
            if (nodeType == XmlNodeType.Element)
            {
                string childNamespace = reader.NamespaceURI;
                string childName = reader.LocalName;
                serializable &= childNamespace.Length == 0;
                if (collection)
                {
                    if (itemName is null)
                    {
                        itemName = childName;
                        itemNamespace = childNamespace;
                    }
                    else
                    {
                        collection = itemName == childName && itemNamespace == childNamespace;
                    }
                }
            }
            else if (reader.EOF)
            {
                throw XmlObjectSerializer.CreateSerializationException(SR.UnexpectedEndOfFile);
            }
            else if (IsContentNode(nodeType))
            {
                classData = serializable = collection = false;
            }

            _attributesInXmlData ??= new Attributes();
            _attributesInXmlData.Read(reader);
            string? xsiPrefix = _attributesInXmlData.XsiTypePrefix;
            string? xsiNamespace = _attributesInXmlData.XsiTypeNamespace;
            string? factoryPrefix = _attributesInXmlData.FactoryTypePrefix;
            string? factoryNamespace = _attributesInXmlData.FactoryTypeNamespace;
            bool xsiType = _attributesInXmlData.XsiTypeName is not null;
            bool factory = _attributesInXmlData.FactoryTypeName is not null;
            XmlNode child = await AsyncXmlSerializableServices.ReadNodeAsync(reader, Document, CancellationToken).ConfigureAwait(false);
            children.Add(child);
            if (namespaces is null)
            {
                if (xsiType)
                {
                    child.Attributes!.Append(AddNamespaceDeclaration(xsiPrefix, xsiNamespace));
                }
                if (factory)
                {
                    child.Attributes!.Append(AddNamespaceDeclaration(factoryPrefix, factoryNamespace));
                }
            }
        }
        await reader.ReadEndElementAsync(CancellationToken).ConfigureAwait(false);
        if (itemName is not null && collection)
        {
            return await ReadUnknownCollectionDataAsync(CreateReaderOverChildNodes(attributes, children), name, ns).ConfigureAwait(false);
        }
        if (serializable)
        {
            return await ReadUnknownISerializableDataAsync(CreateReaderOverChildNodes(attributes, children), name, ns).ConfigureAwait(false);
        }
        if (classData)
        {
            return await ReadUnknownClassDataAsync(CreateReaderOverChildNodes(attributes, children), name, ns).ConfigureAwait(false);
        }

        XmlDataNode node = new XmlDataNode { OwnerDocument = Document, XmlChildNodes = children, XmlAttributes = attributes };
        InitializeExtensionDataNode(node, name, ns);
        return node;
    }
}
