// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization.DataContracts;
using System.Text;
using System.Xml;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContracts.DataContract>;

namespace System.Runtime.Serialization.Json
{
    internal sealed partial class JsonXmlDataContract : JsonDataContract
    {
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public JsonXmlDataContract(XmlDataContract traditionalXmlDataContract)
            : base(traditionalXmlDataContract)
        {
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson? context)
        {
            string xmlContent = jsonReader.ReadElementContentAsString();
            object? xmlValue = DeserializeXmlValue(xmlContent, jsonReader, context);
            context?.AddNewObject(xmlValue);
            return xmlValue;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteJsonValueCore(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson? context, RuntimeTypeHandle declaredTypeHandle)
        {
            jsonWriter.WriteString(SerializeXmlValue(obj, context, declaredTypeHandle));
        }

        private static List<Type> GetKnownTypesFromContext(XmlObjectSerializerContext? context, IList<Type>? serializerKnownTypeList)
        {
            List<Type> knownTypesList = new List<Type>();
            if (context != null)
            {
                List<XmlQualifiedName> xmlNames = new List<XmlQualifiedName>();
                DataContractDictionary[] entries = context.scopedKnownTypes.dataContractDictionaries;
                if (entries != null)
                {
                    for (int i = 0; i < entries.Length; i++)
                    {
                        DataContractDictionary entry = entries[i];
                        if (entry != null)
                        {
                            foreach (KeyValuePair<XmlQualifiedName, DataContract> pair in entry)
                            {
                                if (!xmlNames.Contains(pair.Key))
                                {
                                    xmlNames.Add(pair.Key);
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
