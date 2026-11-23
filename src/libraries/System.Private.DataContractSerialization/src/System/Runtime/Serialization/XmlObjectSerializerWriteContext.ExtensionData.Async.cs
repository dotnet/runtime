// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization.DataContracts;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization;

internal partial class XmlObjectSerializerWriteContext
{
    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal async Task WriteExtensionDataAsync(XmlWriterDelegator writer, ExtensionDataObject? extensionData, int memberIndex)
    {
        if (IgnoreExtensionDataObject || extensionData?.Members is not IList<ExtensionDataMember> members)
        {
            return;
        }
        foreach (ExtensionDataMember member in members)
        {
            if (member.MemberIndex == memberIndex)
            {
                await WriteExtensionDataMemberAsync(writer, member).ConfigureAwait(false);
            }
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task WriteExtensionDataMemberAsync(XmlWriterDelegator writer, ExtensionDataMember member)
    {
        await writer.WriteStartElementAsync(null, member.Name, member.Namespace).ConfigureAwait(false);
        await WriteExtensionDataValueAsync(writer, member.Value).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal virtual Task WriteExtensionDataTypeInfoAsync(XmlWriterDelegator writer, IDataNode node) =>
        node.DataContractName is not null ? WriteTypeInfoAsync(writer, node.DataContractName, node.DataContractNamespace) : Task.CompletedTask;

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal async Task WriteExtensionDataValueAsync(XmlWriterDelegator writer, IDataNode? node)
    {
        IncrementItemCount(1);
        if (node is null)
        {
            await WriteNullAsync(writer).ConfigureAwait(false);
            return;
        }
        object referenceValue = node.Value ?? node;
        if (node.PreservesReferences && await OnHandleReferenceAsync(writer, referenceValue, true).ConfigureAwait(false))
        {
            return;
        }
        try
        {
            Type type = node.DataType;
            if (type == typeof(ClassDataNode))
            {
                await WriteExtensionClassDataAsync(writer, (ClassDataNode)node).ConfigureAwait(false);
            }
            else if (type == typeof(CollectionDataNode))
            {
                await WriteExtensionCollectionDataAsync(writer, (CollectionDataNode)node).ConfigureAwait(false);
            }
            else if (type == typeof(XmlDataNode))
            {
                await WriteExtensionXmlDataAsync(writer, (XmlDataNode)node).ConfigureAwait(false);
            }
            else if (type == typeof(ISerializableDataNode))
            {
                await WriteExtensionISerializableDataAsync(writer, (ISerializableDataNode)node).ConfigureAwait(false);
            }
            else
            {
                await WriteExtensionDataTypeInfoAsync(writer, node).ConfigureAwait(false);
                if (type == typeof(object))
                {
                    if (node.Value is object value)
                    {
                        await InternalSerializeAsync(writer, value, false, false, -1, value.GetType().TypeHandle).ConfigureAwait(false);
                    }
                }
                else
                {
                    await writer.WriteExtensionDataAsync(node).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            if (node.PreservesReferences)
            {
                OnEndHandleReference(writer, referenceValue, true);
            }
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async ValueTask<bool> TryWriteDeserializedExtensionDataAsync(XmlWriterDelegator writer, IDataNode node)
    {
        if (node.Value is not object value)
        {
            return false;
        }
        Type declaredType = node.DataContractName is null ? value.GetType() : typeof(object);
        await InternalSerializeAsync(writer, value, false, false, -1, declaredType.TypeHandle).ConfigureAwait(false);
        return true;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task WriteExtensionClassDataAsync(XmlWriterDelegator writer, ClassDataNode node)
    {
        if (await TryWriteDeserializedExtensionDataAsync(writer, node).ConfigureAwait(false))
        {
            return;
        }
        await WriteExtensionDataTypeInfoAsync(writer, node).ConfigureAwait(false);
        if (node.Members is IList<ExtensionDataMember> members)
        {
            foreach (ExtensionDataMember member in members)
            {
                await WriteExtensionDataMemberAsync(writer, member).ConfigureAwait(false);
            }
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task WriteExtensionCollectionDataAsync(XmlWriterDelegator writer, CollectionDataNode node)
    {
        if (await TryWriteDeserializedExtensionDataAsync(writer, node).ConfigureAwait(false))
        {
            return;
        }
        await WriteExtensionDataTypeInfoAsync(writer, node).ConfigureAwait(false);
        await WriteArraySizeAsync(writer, node.Size).ConfigureAwait(false);
        if (node.Items is IList<IDataNode?> items)
        {
            foreach (IDataNode? item in items)
            {
                await writer.WriteStartElementAsync(null, node.ItemName!, node.ItemNamespace).ConfigureAwait(false);
                await WriteExtensionDataValueAsync(writer, item).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task WriteExtensionISerializableDataAsync(XmlWriterDelegator writer, ISerializableDataNode node)
    {
        if (await TryWriteDeserializedExtensionDataAsync(writer, node).ConfigureAwait(false))
        {
            return;
        }
        await WriteExtensionDataTypeInfoAsync(writer, node).ConfigureAwait(false);
        if (node.FactoryTypeName is not null)
        {
            await writer.WriteAttributeQualifiedNameAsync(Globals.SerPrefix, DictionaryGlobals.ISerializableFactoryTypeLocalName,
                DictionaryGlobals.SerializationNamespace, node.FactoryTypeName, node.FactoryTypeNamespace).ConfigureAwait(false);
        }
        if (node.Members is IList<ISerializableDataMember> members)
        {
            foreach (ISerializableDataMember member in members)
            {
                await writer.WriteStartElementAsync(null, member.Name, string.Empty).ConfigureAwait(false);
                await WriteExtensionDataValueAsync(writer, member.Value).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task WriteExtensionXmlDataAsync(XmlWriterDelegator writer, XmlDataNode node)
    {
        if (await TryWriteDeserializedExtensionDataAsync(writer, node).ConfigureAwait(false))
        {
            return;
        }
        if (node.XmlAttributes is IList<XmlAttribute> attributes)
        {
            foreach (XmlAttribute attribute in attributes)
            {
                await AsyncXmlSerializableServices.WriteNodeAsync(writer, attribute, CancellationToken).ConfigureAwait(false);
            }
        }
        await WriteExtensionDataTypeInfoAsync(writer, node).ConfigureAwait(false);
        if (node.XmlChildNodes is IList<XmlNode> children)
        {
            foreach (XmlNode child in children)
            {
                await AsyncXmlSerializableServices.WriteNodeAsync(writer, child, CancellationToken).ConfigureAwait(false);
            }
        }
    }
}
