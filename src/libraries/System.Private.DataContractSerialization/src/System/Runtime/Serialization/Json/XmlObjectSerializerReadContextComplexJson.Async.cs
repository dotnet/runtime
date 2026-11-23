// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization.DataContracts;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed partial class XmlObjectSerializerReadContextComplexJson
{
    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    protected override ValueTask<object?> ReadDataContractValueAsync(DataContract contract, XmlReaderDelegator reader) =>
        JsonDataContract.GetJsonDataContract(contract).ReadJsonValueAsync(reader, this, CancellationToken);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal async ValueTask<int> GetJsonMemberIndexAsync(XmlReaderDelegator reader, XmlDictionaryString[] names, int memberIndex, ExtensionDataObject? extensionData)
    {
        int count = names.Length;
        if (count != 0)
        {
            await reader.MoveToContentAsync(CancellationToken).ConfigureAwait(false);
            for (int i = 0, index = (memberIndex + 1) % count; i < count; i++, index = (index + 1) % count)
            {
                if (reader.IsLocalName(names[index]) && reader.IsNamespaceUri(XmlDictionaryString.Empty))
                {
                    return index;
                }
            }
            if (TryGetJsonLocalName(reader, out string? name))
            {
                for (int i = 0, index = (memberIndex + 1) % count; i < count; i++, index = (index + 1) % count)
                {
                    if (names[index].Value == name)
                    {
                        return index;
                    }
                }
            }
        }

        await HandleMemberNotFoundAsync(reader, extensionData, memberIndex).ConfigureAwait(false);
        return count;
    }

    protected override async ValueTask<IDataNode> ReadPrimitiveExtensionDataValueAsync(XmlReaderDelegator reader, string? name, string? ns)
    {
        IDataNode node = _extensionDataValueType switch
        {
            null or JsonGlobals.stringString => new DataNode<string>(await reader.ReadContentAsStringAsync(CancellationToken).ConfigureAwait(false)),
            JsonGlobals.booleanString => new DataNode<bool>((bool)await reader.ReadContentAsAnyTypeAsync(typeof(bool), CancellationToken).ConfigureAwait(false)),
            JsonGlobals.numberString => CreateNumericalPrimitiveExtensionDataValue(await reader.ReadContentAsStringAsync(CancellationToken).ConfigureAwait(false)),
            _ => throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.JsonUnexpectedAttributeValue, _extensionDataValueType))
        };
        await reader.ReadEndElementAsync(CancellationToken).ConfigureAwait(false);
        return node;
    }
}
