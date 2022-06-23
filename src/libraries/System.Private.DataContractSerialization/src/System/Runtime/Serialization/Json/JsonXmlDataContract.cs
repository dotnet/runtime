// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal sealed class JsonXmlDataContract : JsonDataContract
    {
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public JsonXmlDataContract(XmlDataContract traditionalXmlDataContract)
            : base(traditionalXmlDataContract)
        {
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson? context)
        {
            string xmlContent = jsonReader.ReadElementContentAsString();

            DataContractSerializer dataContractSerializer = new DataContractSerializer(TraditionalDataContract.UnderlyingType,
                GetKnownTypesFromContext(context, (context == null) ? null : context.SerializerKnownTypeList), 1, false, false); //  maxItemsInObjectGraph //  ignoreExtensionDataObject //  preserveObjectReferences

            MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContent));
            object? xmlValue;
            XmlDictionaryReaderQuotas? quotas = ((JsonReaderDelegator)jsonReader).ReaderQuotas;
            if (quotas == null)
            {
                xmlValue = dataContractSerializer.ReadObject(memoryStream);
            }
            else
            {
                xmlValue = dataContractSerializer.ReadObject(XmlDictionaryReader.CreateTextReader(memoryStream, quotas));
            }
            if (context != null)
            {
                context.AddNewObject(xmlValue);
            }
            return xmlValue;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteJsonValueCore(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson? context, RuntimeTypeHandle declaredTypeHandle)
        {
            DataContractSerializer dataContractSerializer = new DataContractSerializer(Type.GetTypeFromHandle(declaredTypeHandle)!,
                GetKnownTypesFromContext(context, (context == null) ? null : context.SerializerKnownTypeList), 1, false, false); //  maxItemsInObjectGraph //  ignoreExtensionDataObject //  preserveObjectReferences

            MemoryStream memoryStream = new MemoryStream();
            dataContractSerializer.WriteObject(memoryStream, obj);
            memoryStream.Position = 0;
            string serialized = new StreamReader(memoryStream).ReadToEnd();
            jsonWriter.WriteString(serialized);
        }

        private static List<Type> GetKnownTypesFromContext(XmlObjectSerializerContext? context, IList<Type>? serializerKnownTypeList)
        {
            List<Type> knownTypesList = new List<Type>();
            if (context != null)
            {
                List<XmlQualifiedName> stableNames = new List<XmlQualifiedName>();
                Dictionary<XmlQualifiedName, DataContract>[] entries = context.scopedKnownTypes.dataContractDictionaries;
                if (entries != null)
                {
                    for (int i = 0; i < entries.Length; i++)
                    {
                        Dictionary<XmlQualifiedName, DataContract> entry = entries[i];
                        if (entry != null)
                        {
                            foreach (KeyValuePair<XmlQualifiedName, DataContract> pair in entry)
                            {
                                if (!stableNames.Contains(pair.Key))
                                {
                                    stableNames.Add(pair.Key);
                                    knownTypesList.Add(pair.Value.UnderlyingType);
                                }
                            }
                        }
                    }
                }
                if (serializerKnownTypeList != null)
                {
                    knownTypesList.AddRange(serializerKnownTypeList);
                }
            }
            return knownTypesList;
        }
    }
}
