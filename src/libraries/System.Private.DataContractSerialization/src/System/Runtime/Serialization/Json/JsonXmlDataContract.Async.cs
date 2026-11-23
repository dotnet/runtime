// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization.DataContracts;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed partial class JsonXmlDataContract
{
    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private protected override ReflectionValueReader CreateAsyncReader(JsonDictionaryFormat format) => ReadXmlValueAsync;

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async ValueTask<object?> ReadXmlValueAsync(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
    {
        string xml = (string)await reader.ReadReferencePrimitiveElementAsync<ReflectionPlan.StringValue>(context.CancellationToken).ConfigureAwait(false);
        object? value = DeserializeXmlValue(xml, reader, (XmlObjectSerializerReadContextComplexJson)context);
        context.AddNewObject(value);
        return value;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal override Task WriteJsonValueCoreAsync(XmlWriterDelegator writer, object value, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredHandle) =>
        writer.WriteStringAsync(SerializeXmlValue(value, context, declaredHandle));

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private object? DeserializeXmlValue(string xml, XmlReaderDelegator reader, XmlObjectSerializerReadContextComplexJson? context)
    {
        DataContractSerializer serializer = new DataContractSerializer(TraditionalDataContract.UnderlyingType,
            GetKnownTypesFromContext(context, context?.SerializerKnownTypeList), 1, false, false);
        using StringStream stream = new StringStream(xml, Encoding.UTF8);
        XmlDictionaryReaderQuotas? quotas = ((JsonReaderDelegator)reader).ReaderQuotas;
        return quotas is null ? serializer.ReadObject(stream) :
            serializer.ReadObject(XmlDictionaryReader.CreateTextReader(stream, quotas));
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private static string SerializeXmlValue(object value, XmlObjectSerializerWriteContextComplexJson? context, RuntimeTypeHandle declaredHandle)
    {
        DataContractSerializer serializer = new DataContractSerializer(Type.GetTypeFromHandle(declaredHandle)!,
            GetKnownTypesFromContext(context, context?.SerializerKnownTypeList), 1, false, false);
        using MemoryStream content = new MemoryStream();
        serializer.WriteObject(content, value);
        content.Position = 0;
        using StreamReader reader = new StreamReader(content);
        return reader.ReadToEnd();
    }
}
