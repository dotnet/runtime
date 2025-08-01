// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.DataContracts;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Runtime.Serialization
{
    internal sealed class SchemaExporter
    {
        private readonly XmlSchemaSet _schemas;
        private XmlDocument? _xmlDoc;
        private DataContractSet _dataContractSet;

        internal SchemaExporter(XmlSchemaSet schemas, DataContractSet dataContractSet)
        {
            _schemas = schemas;
            _dataContractSet = dataContractSet;
        }

        private XmlSchemaSet Schemas
        {
            get { return _schemas; }
        }

        private XmlDocument XmlDoc => _xmlDoc ??= new XmlDocument();

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void Export()
        {
            try
            {
                // Remove this if we decide to publish serialization schema at well-known location
                ExportSerializationSchema();
                foreach (KeyValuePair<XmlQualifiedName, DataContract> pair in _dataContractSet.Contracts)
                {
                    DataContract dataContract = pair.Value;
                    if (!_dataContractSet.IsContractProcessed(dataContract))
                    {
                        ExportDataContract(dataContract);
                        _dataContractSet.SetContractProcessed(dataContract);
                    }
                }
            }
            finally
            {
                _xmlDoc = null;
                _dataContractSet = null!;
            }
        }

        private void ExportSerializationSchema()
        {
            if (!Schemas.Contains(Globals.SerializationNamespace))
            {
                StringReader reader = new StringReader(Globals.SerializationSchema);
                XmlSchema? schema = XmlSchema.Read(new XmlTextReader(reader) { DtdProcessing = DtdProcessing.Prohibit }, null);
                if (schema == null)
                    throw new InvalidOperationException(SR.Format(SR.CouldNotReadSerializationSchema, Globals.SerializationNamespace));
                Schemas.Add(schema);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void ExportDataContract(DataContract dataContract)
        {
            if (dataContract.IsBuiltInDataContract)
                return;
            else if (dataContract is XmlDataContract)
                ExportXmlDataContract((XmlDataContract)dataContract);
            else
            {
                XmlSchema schema = GetSchema(dataContract.XmlName.Namespace);

                if (dataContract is ClassDataContract classDataContract)
                {
                    if (classDataContract.IsISerializable)
                        ExportISerializableDataContract(classDataContract, schema);
                    else
                        ExportClassDataContract(classDataContract, schema);
                }
                else if (dataContract is CollectionDataContract)
                    ExportCollectionDataContract((CollectionDataContract)dataContract, schema);
                else if (dataContract is EnumDataContract)
                    ExportEnumDataContract((EnumDataContract)dataContract, schema);
                ExportTopLevelElement(dataContract, schema);
                Schemas.Reprocess(schema);
            }
        }

        private XmlSchemaElement ExportTopLevelElement(DataContract dataContract, XmlSchema? schema)
        {
            if (schema == null || dataContract.XmlName.Namespace != dataContract.TopLevelElementNamespace!.Value)
                schema = GetSchema(dataContract.TopLevelElementNamespace!.Value);

            XmlSchemaElement topLevelElement = new XmlSchemaElement();
            topLevelElement.Name = dataContract.TopLevelElementName!.Value;
            SetElementType(topLevelElement, dataContract, schema);
            topLevelElement.IsNillable = true;
            schema.Items.Add(topLevelElement);
            return topLevelElement;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void ExportClassDataContract(ClassDataContract classDataContract, XmlSchema schema)
        {
            XmlSchemaComplexType type = new XmlSchemaComplexType();
            type.Name = classDataContract.XmlName.Name;
            schema.Items.Add(type);
            XmlElement? genericInfoElement = null;
            if (classDataContract.UnderlyingType.IsGenericType)
                genericInfoElement = ExportGenericInfo(classDataContract.UnderlyingType, Globals.GenericTypeLocalName, Globals.SerializationNamespace);

            XmlSchemaSequence rootSequence = new XmlSchemaSequence();
            for (int i = 0; i < classDataContract.Members!.Count; i++)
            {
                DataMember dataMember = classDataContract.Members[i];

                XmlSchemaElement element = new XmlSchemaElement();
                element.Name = dataMember.Name;
                XmlElement? actualTypeElement = null;
                DataContract memberTypeContract = _dataContractSet.GetMemberTypeDataContract(dataMember);
                if (CheckIfMemberHasConflict(dataMember))
                {
                    element.SchemaTypeName = AnytypeQualifiedName;
                    actualTypeElement = ExportActualType(memberTypeContract.XmlName);
                    SchemaHelper.AddSchemaImport(memberTypeContract.XmlName.Namespace, schema);
                }
                else
                    SetElementType(element, memberTypeContract, schema);
                SchemaHelper.AddElementForm(element, schema);
                if (dataMember.IsNullable)
                    element.IsNillable = true;
                if (!dataMember.IsRequired)
                    element.MinOccurs = 0;

                element.Annotation = GetSchemaAnnotation(actualTypeElement, ExportSurrogateData(dataMember), ExportEmitDefaultValue(dataMember));
                rootSequence.Items.Add(element);
            }

            XmlElement? isValueTypeElement = null;
            if (classDataContract.BaseClassContract != null)
            {
                XmlSchemaComplexContentExtension extension = CreateTypeContent(type, classDataContract.BaseClassContract.XmlName, schema);
                extension.Particle = rootSequence;
                if (classDataContract.IsReference && !classDataContract.BaseClassContract.IsReference)
                {
                    AddReferenceAttributes(extension.Attributes, schema);
                }
            }
            else
            {
                type.Particle = rootSequence;
                if (classDataContract.IsValueType)
                    isValueTypeElement = GetAnnotationMarkup(IsValueTypeName, XmlConvert.ToString(classDataContract.IsValueType), schema);
                if (classDataContract.IsReference)
                    AddReferenceAttributes(type.Attributes, schema);
            }
            type.Annotation = GetSchemaAnnotation(genericInfoElement, ExportSurrogateData(classDataContract), isValueTypeElement);
        }

        private static void AddReferenceAttributes(XmlSchemaObjectCollection attributes, XmlSchema schema)
        {
            SchemaHelper.AddSchemaImport(Globals.SerializationNamespace, schema);
            schema.Namespaces.Add(Globals.SerPrefixForSchema, Globals.SerializationNamespace);
            attributes.Add(IdAttribute);
            attributes.Add(RefAttribute);
        }

        private static void SetElementType(XmlSchemaElement element, DataContract dataContract, XmlSchema schema)
        {
            if (dataContract is XmlDataContract xmlDataContract && xmlDataContract.IsAnonymous)
            {
                element.SchemaType = xmlDataContract.XsdType;
            }
            else
            {
                element.SchemaTypeName = dataContract.XmlName;

                if (element.SchemaTypeName.Namespace.Equals(Globals.SerializationNamespace))
                    schema.Namespaces.Add(Globals.SerPrefixForSchema, Globals.SerializationNamespace);

                SchemaHelper.AddSchemaImport(dataContract.XmlName.Namespace, schema);
            }
        }

        private static bool CheckIfMemberHasConflict(DataMember dataMember)
        {
            if (dataMember.HasConflictingNameAndType)
                return true;

            DataMember? conflictingMember = dataMember.ConflictingMember;
            while (conflictingMember != null)
            {
                if (conflictingMember.HasConflictingNameAndType)
                    return true;
                conflictingMember = conflictingMember.ConflictingMember;
            }

            return false;
        }

        private XmlElement? ExportEmitDefaultValue(DataMember dataMember)
        {
            if (dataMember.EmitDefaultValue)
                return null;
            XmlElement defaultValueElement = XmlDoc.CreateElement(DefaultValueAnnotation.Name, DefaultValueAnnotation.Namespace);
            XmlAttribute emitDefaultValueAttribute = XmlDoc.CreateAttribute(Globals.EmitDefaultValueAttribute);
            emitDefaultValueAttribute.Value = Globals.False;
            defaultValueElement.Attributes.Append(emitDefaultValueAttribute);
            return defaultValueElement;
        }

        private XmlElement ExportActualType(XmlQualifiedName typeName)
        {
            return ExportActualType(typeName, XmlDoc);
        }

        private static XmlElement ExportActualType(XmlQualifiedName typeName, XmlDocument xmlDoc)
        {
            XmlElement actualTypeElement = xmlDoc.CreateElement(ActualTypeAnnotationName.Name, ActualTypeAnnotationName.Namespace);

            XmlAttribute nameAttribute = xmlDoc.CreateAttribute(Globals.ActualTypeNameAttribute);
            nameAttribute.Value = typeName.Name;
            actualTypeElement.Attributes.Append(nameAttribute);

            XmlAttribute nsAttribute = xmlDoc.CreateAttribute(Globals.ActualTypeNamespaceAttribute);
            nsAttribute.Value = typeName.Namespace;
            actualTypeElement.Attributes.Append(nsAttribute);

            return actualTypeElement;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlElement ExportGenericInfo(Type clrType, string elementName, string elementNs)
        {
            Type? itemType;
            int nestedCollectionLevel = 0;
            while (CollectionDataContract.IsCollection(clrType, out itemType))
            {
                if (DataContract.GetBuiltInDataContract(clrType) != null
                    || CollectionDataContract.IsCollectionDataContract(clrType))
                {
                    break;
                }
                clrType = itemType;
                nestedCollectionLevel++;
            }

            Type[]? genericArguments = null;
            List<int>? genericArgumentCounts = null;
            if (clrType.IsGenericType)
            {
                genericArguments = clrType.GetGenericArguments();
                string typeName;
                if (clrType.DeclaringType == null)
                    typeName = clrType.Name;
                else
                {
                    int nsLen = (clrType.Namespace == null) ? 0 : clrType.Namespace.Length;
                    if (nsLen > 0)
                        nsLen++; //include the . following namespace
                    typeName = DataContract.GetClrTypeFullName(clrType).Substring(nsLen).Replace('+', '.');
                }
                int iParam = typeName.IndexOf('[');
                if (iParam >= 0)
                    typeName = typeName.Substring(0, iParam);
                genericArgumentCounts = DataContract.GetDataContractNameForGenericName(typeName, null);
                clrType = clrType.GetGenericTypeDefinition();
            }
            XmlQualifiedName dcqname = DataContract.GetXmlName(clrType);
            if (nestedCollectionLevel > 0)
            {
                string collectionName = dcqname.Name;
                for (int n = 0; n < nestedCollectionLevel; n++)
                    collectionName = Globals.ArrayPrefix + collectionName;
                dcqname = new XmlQualifiedName(collectionName, DataContract.GetCollectionNamespace(dcqname.Namespace));
            }
            XmlElement typeElement = XmlDoc.CreateElement(elementName, elementNs);

            XmlAttribute nameAttribute = XmlDoc.CreateAttribute(Globals.GenericNameAttribute);
            nameAttribute.Value = genericArguments != null ? XmlConvert.DecodeName(dcqname.Name) : dcqname.Name;
            //nameAttribute.Value = dcqname.Name;
            typeElement.Attributes.Append(nameAttribute);

            XmlAttribute nsAttribute = XmlDoc.CreateAttribute(Globals.GenericNamespaceAttribute);
            nsAttribute.Value = dcqname.Namespace;
            typeElement.Attributes.Append(nsAttribute);

            if (genericArguments != null)
            {
                int argIndex = 0;
                int nestedLevel = 0;
                Debug.Assert(genericArgumentCounts != null);
                foreach (int genericArgumentCount in genericArgumentCounts)
                {
                    for (int i = 0; i < genericArgumentCount; i++, argIndex++)
                    {
                        XmlElement argumentElement = ExportGenericInfo(genericArguments[argIndex], Globals.GenericParameterLocalName, Globals.SerializationNamespace);
                        if (nestedLevel > 0)
                        {
                            XmlAttribute nestedLevelAttribute = XmlDoc.CreateAttribute(Globals.GenericParameterNestedLevelAttribute);
                            nestedLevelAttribute.Value = nestedLevel.ToString(CultureInfo.InvariantCulture);
                            argumentElement.Attributes.Append(nestedLevelAttribute);
                        }
                        typeElement.AppendChild(argumentElement);
                    }
                    nestedLevel++;
                }
                if (genericArgumentCounts[nestedLevel - 1] == 0)
                {
                    XmlAttribute typeNestedLevelsAttribute = XmlDoc.CreateAttribute(Globals.GenericParameterNestedLevelAttribute);
                    typeNestedLevelsAttribute.Value = genericArgumentCounts.Count.ToString(CultureInfo.InvariantCulture);
                    typeElement.Attributes.Append(typeNestedLevelsAttribute);
                }
            }

            return typeElement;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlElement? ExportSurrogateData(object key)
        {
            object? surrogateData = _dataContractSet.GetSurrogateData(key);
            if (surrogateData == null)
                return null;
            StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            XmlWriterSettings writerSettings = new XmlWriterSettings();
            writerSettings.OmitXmlDeclaration = true;
            XmlWriter xmlWriter = XmlWriter.Create(stringWriter, writerSettings);
            Collection<Type> knownTypes = new Collection<Type>();
            if (_dataContractSet.SerializationExtendedSurrogateProvider != null)
                DataContractSurrogateCaller.GetKnownCustomDataTypes(_dataContractSet.SerializationExtendedSurrogateProvider, knownTypes);
            DataContractSerializer serializer = new DataContractSerializer(Globals.TypeOfObject,
                SurrogateDataAnnotationName.Name, SurrogateDataAnnotationName.Namespace, knownTypes,
                ignoreExtensionDataObject: false, preserveObjectReferences: true);
            serializer.WriteObject(xmlWriter, surrogateData);
            xmlWriter.Flush();
            using var xmlReader = XmlReader.Create(new StringReader(stringWriter.ToString()));
            return (XmlElement?)XmlDoc.ReadNode(xmlReader);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void ExportCollectionDataContract(CollectionDataContract collectionDataContract, XmlSchema schema)
        {
            XmlSchemaComplexType type = new XmlSchemaComplexType();
            type.Name = collectionDataContract.XmlName.Name;
            schema.Items.Add(type);
            XmlElement? genericInfoElement = null, isDictionaryElement = null;
            if (collectionDataContract.UnderlyingType.IsGenericType && CollectionDataContract.IsCollectionDataContract(collectionDataContract.UnderlyingType))
                genericInfoElement = ExportGenericInfo(collectionDataContract.UnderlyingType, Globals.GenericTypeLocalName, Globals.SerializationNamespace);
            if (collectionDataContract.IsDictionary)
                isDictionaryElement = ExportIsDictionary();
            type.Annotation = GetSchemaAnnotation(isDictionaryElement, genericInfoElement, ExportSurrogateData(collectionDataContract));

            XmlSchemaSequence rootSequence = new XmlSchemaSequence();

            XmlSchemaElement element = new XmlSchemaElement();
            element.Name = collectionDataContract.ItemName;
            element.MinOccurs = 0;
            element.MaxOccursString = Globals.OccursUnbounded;
            if (collectionDataContract.IsDictionary)
            {
                ClassDataContract keyValueContract = (collectionDataContract.ItemContract as ClassDataContract)!;
                XmlSchemaComplexType keyValueType = new XmlSchemaComplexType();
                XmlSchemaSequence keyValueSequence = new XmlSchemaSequence();
                foreach (DataMember dataMember in keyValueContract.Members!)
                {
                    XmlSchemaElement keyValueElement = new XmlSchemaElement();
                    keyValueElement.Name = dataMember.Name;
                    SetElementType(keyValueElement, _dataContractSet.GetMemberTypeDataContract(dataMember), schema);
                    SchemaHelper.AddElementForm(keyValueElement, schema);
                    if (dataMember.IsNullable)
                        keyValueElement.IsNillable = true;
                    keyValueElement.Annotation = GetSchemaAnnotation(ExportSurrogateData(dataMember));
                    keyValueSequence.Items.Add(keyValueElement);
                }
                keyValueType.Particle = keyValueSequence;
                element.SchemaType = keyValueType;
            }
            else
            {
                if (collectionDataContract.IsItemTypeNullable)
                    element.IsNillable = true;
                DataContract itemContract = _dataContractSet.GetItemTypeDataContract(collectionDataContract);
                SetElementType(element, itemContract, schema);
            }
            SchemaHelper.AddElementForm(element, schema);
            rootSequence.Items.Add(element);

            type.Particle = rootSequence;

            if (collectionDataContract.IsReference)
                AddReferenceAttributes(type.Attributes, schema);
        }

        private XmlElement ExportIsDictionary()
        {
            XmlElement isDictionaryElement = XmlDoc.CreateElement(IsDictionaryAnnotationName.Name, IsDictionaryAnnotationName.Namespace);
            isDictionaryElement.InnerText = Globals.True;
            return isDictionaryElement;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void ExportEnumDataContract(EnumDataContract enumDataContract, XmlSchema schema)
        {
            XmlSchemaSimpleType type = new XmlSchemaSimpleType();
            type.Name = enumDataContract.XmlName.Name;
            XmlElement? actualTypeElement = (enumDataContract.BaseContractName == DefaultEnumBaseTypeName) ? null : ExportActualType(enumDataContract.BaseContractName);
            type.Annotation = GetSchemaAnnotation(actualTypeElement, ExportSurrogateData(enumDataContract));
            schema.Items.Add(type);

            XmlSchemaSimpleTypeRestriction restriction = new XmlSchemaSimpleTypeRestriction();
            restriction.BaseTypeName = StringQualifiedName;
            SchemaHelper.AddSchemaImport(enumDataContract.BaseContractName.Namespace, schema);
            if (enumDataContract.Values != null)
            {
                for (int i = 0; i < enumDataContract.Values.Count; i++)
                {
                    XmlSchemaEnumerationFacet facet = new XmlSchemaEnumerationFacet();
                    facet.Value = enumDataContract.Members[i].Name;
                    if (enumDataContract.Values[i] != GetDefaultEnumValue(enumDataContract.IsFlags, i))
                        facet.Annotation = GetSchemaAnnotation(EnumerationValueAnnotationName, enumDataContract.GetStringFromEnumValue(enumDataContract.Values[i]), schema);
                    restriction.Facets.Add(facet);
                }
            }
            if (enumDataContract.IsFlags)
            {
                XmlSchemaSimpleTypeList list = new XmlSchemaSimpleTypeList();
                XmlSchemaSimpleType anonymousType = new XmlSchemaSimpleType();
                anonymousType.Content = restriction;
                list.ItemType = anonymousType;
                type.Content = list;
            }
            else
                type.Content = restriction;
        }

        internal static long GetDefaultEnumValue(bool isFlags, int index)
        {
            return isFlags ? (long)Math.Pow(2, index) : index;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void ExportISerializableDataContract(ClassDataContract dataContract, XmlSchema schema)
        {
            XmlSchemaComplexType type = new XmlSchemaComplexType();
            type.Name = dataContract.XmlName.Name;
            schema.Items.Add(type);
            XmlElement? genericInfoElement = null;
            if (dataContract.UnderlyingType.IsGenericType)
                genericInfoElement = ExportGenericInfo(dataContract.UnderlyingType, Globals.GenericTypeLocalName, Globals.SerializationNamespace);

            XmlElement? isValueTypeElement = null;
            if (dataContract.BaseClassContract != null)
            {
                _ = CreateTypeContent(type, dataContract.BaseClassContract.XmlName, schema);
            }
            else
            {
                schema.Namespaces.Add(Globals.SerPrefixForSchema, Globals.SerializationNamespace);
                type.Particle = ISerializableSequence;
                XmlSchemaAttribute iSerializableFactoryTypeAttribute = ISerializableFactoryTypeAttribute;
                type.Attributes.Add(iSerializableFactoryTypeAttribute);
                SchemaHelper.AddSchemaImport(ISerializableFactoryTypeAttribute.RefName.Namespace, schema);
                if (dataContract.IsValueType)
                    isValueTypeElement = GetAnnotationMarkup(IsValueTypeName, XmlConvert.ToString(dataContract.IsValueType), schema);
            }
            type.Annotation = GetSchemaAnnotation(genericInfoElement, ExportSurrogateData(dataContract), isValueTypeElement);
        }

        private static XmlSchemaComplexContentExtension CreateTypeContent(XmlSchemaComplexType type, XmlQualifiedName baseTypeName, XmlSchema schema)
        {
            SchemaHelper.AddSchemaImport(baseTypeName.Namespace, schema);

            XmlSchemaComplexContentExtension extension = new XmlSchemaComplexContentExtension();
            extension.BaseTypeName = baseTypeName;
            type.ContentModel = new XmlSchemaComplexContent();
            type.ContentModel.Content = extension;

            return extension;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void ExportXmlDataContract(XmlDataContract dataContract)
        {
            XmlQualifiedName? typeQName;
            bool hasRoot;
            XmlSchemaType? xsdType;

            Type clrType = dataContract.UnderlyingType;
            if (!IsSpecialXmlType(clrType, out typeQName, out xsdType, out hasRoot))
                if (!InvokeSchemaProviderMethod(clrType, _schemas, out typeQName, out xsdType, out hasRoot))
                    InvokeGetSchemaMethod(clrType, _schemas, typeQName);

            if (hasRoot)
            {
                if (!(typeQName.Equals(dataContract.XmlName)))
                {
                    Debug.Fail("XML data contract type name does not match schema name");
                }

                XmlSchema? schema;
                if (SchemaHelper.GetSchemaElement(Schemas,
                    new XmlQualifiedName(dataContract.TopLevelElementName!.Value, dataContract.TopLevelElementNamespace!.Value),
                    out schema) == null)
                {
                    XmlSchemaElement topLevelElement = ExportTopLevelElement(dataContract, schema);
                    topLevelElement.IsNillable = dataContract.IsTopLevelElementNullable;
                    ReprocessAll(_schemas);
                }

                XmlSchemaType? anonymousType = xsdType;
                xsdType = SchemaHelper.GetSchemaType(_schemas, typeQName, out schema);
                if (anonymousType == null && xsdType == null && typeQName.Namespace != XmlSchema.Namespace)
                {
                    throw new InvalidDataContractException(SR.Format(SR.MissingSchemaType, typeQName, DataContract.GetClrTypeFullName(clrType)));
                }
                if (xsdType != null)
                {
                    xsdType.Annotation = GetSchemaAnnotation(
                                           ExportSurrogateData(dataContract),
                                           dataContract.IsValueType ?
                                             GetAnnotationMarkup(IsValueTypeName, XmlConvert.ToString(dataContract.IsValueType), schema!) :
                                             null
                                         );
                }
            }
        }

        private static void ReprocessAll(XmlSchemaSet schemas)// and remove duplicate items
        {
            Hashtable elements = new Hashtable();
            Hashtable types = new Hashtable();
            XmlSchema[] schemaArray = new XmlSchema[schemas.Count];
            schemas.CopyTo(schemaArray, 0);
            for (int i = 0; i < schemaArray.Length; i++)
            {
                XmlSchema schema = schemaArray[i];
                XmlSchemaObject[] itemArray = new XmlSchemaObject[schema.Items.Count];
                schema.Items.CopyTo(itemArray, 0);
                for (int j = 0; j < itemArray.Length; j++)
                {
                    XmlSchemaObject item = itemArray[j];
                    Hashtable items;
                    XmlQualifiedName qname;
                    if (item is XmlSchemaElement)
                    {
                        items = elements;
                        qname = new XmlQualifiedName(((XmlSchemaElement)item).Name, schema.TargetNamespace);
                    }
                    else if (item is XmlSchemaType)
                    {
                        items = types;
                        qname = new XmlQualifiedName(((XmlSchemaType)item).Name, schema.TargetNamespace);
                    }
                    else
                        continue;
                    object? otherItem = items[qname];
                    if (otherItem != null)
                    {
                        schema.Items.Remove(item);
                    }
                    else
                        items.Add(qname, item);
                }
                schemas.Reprocess(schema);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static void GetXmlTypeInfo(Type type, out XmlQualifiedName xmlName, out XmlSchemaType? xsdType, out bool hasRoot)
        {
            if (IsSpecialXmlType(type, out xmlName!, out xsdType, out hasRoot))
                return;
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.XmlResolver = null;
            InvokeSchemaProviderMethod(type, schemas, out xmlName, out xsdType, out hasRoot);
            if (string.IsNullOrEmpty(xmlName.Name))
                throw new InvalidDataContractException(SR.Format(SR.InvalidXmlDataContractName, DataContract.GetClrTypeFullName(type)));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static bool InvokeSchemaProviderMethod(Type clrType, XmlSchemaSet schemas, out XmlQualifiedName xmlName, out XmlSchemaType? xsdType, out bool hasRoot)
        {
            xsdType = null;
            hasRoot = true;
            object[] attrs = clrType.GetCustomAttributes(Globals.TypeOfXmlSchemaProviderAttribute, false);
            if (attrs == null || attrs.Length == 0)
            {
                xmlName = DataContract.GetDefaultXmlName(clrType);
                return false;
            }

            XmlSchemaProviderAttribute provider = (XmlSchemaProviderAttribute)attrs[0];
            if (provider.IsAny)
            {
                xsdType = CreateAnyElementType();
                hasRoot = false;
            }
            string? methodName = provider.MethodName;
            if (string.IsNullOrEmpty(methodName))
            {
                if (!provider.IsAny)
                    throw new InvalidDataContractException(SR.Format(SR.InvalidGetSchemaMethod, DataContract.GetClrTypeFullName(clrType)));
                xmlName = DataContract.GetDefaultXmlName(clrType);
            }
            else
            {
                MethodInfo? getMethod = clrType.GetMethod(methodName,  /*BindingFlags.DeclaredOnly |*/ BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, new Type[] { typeof(XmlSchemaSet) });
                if (getMethod == null)
                    throw new InvalidDataContractException(SR.Format(SR.MissingGetSchemaMethod, DataContract.GetClrTypeFullName(clrType), methodName));

                if (!(Globals.TypeOfXmlQualifiedName.IsAssignableFrom(getMethod.ReturnType)) && !(Globals.TypeOfXmlSchemaType.IsAssignableFrom(getMethod.ReturnType)))
                    throw new InvalidDataContractException(SR.Format(SR.InvalidReturnTypeOnGetSchemaMethod, DataContract.GetClrTypeFullName(clrType), methodName, DataContract.GetClrTypeFullName(getMethod.ReturnType), DataContract.GetClrTypeFullName(Globals.TypeOfXmlQualifiedName), typeof(XmlSchemaType)));

                object? typeInfo = getMethod.Invoke(null, new object[] { schemas });

                if (provider.IsAny)
                {
                    if (typeInfo != null)
                        throw new InvalidDataContractException(SR.Format(SR.InvalidNonNullReturnValueByIsAny, DataContract.GetClrTypeFullName(clrType), methodName));
                    xmlName = DataContract.GetDefaultXmlName(clrType);
                }
                else if (typeInfo == null)
                {
                    xsdType = CreateAnyElementType();
                    hasRoot = false;
                    xmlName = DataContract.GetDefaultXmlName(clrType);
                }
                else
                {
                    if (typeInfo is XmlSchemaType providerXsdType)
                    {
                        string? typeName = providerXsdType.Name;
                        string? typeNs = null;
                        if (string.IsNullOrEmpty(typeName))
                        {
                            DataContract.GetDefaultXmlName(DataContract.GetClrTypeFullName(clrType), out typeName, out typeNs);
                            xmlName = new XmlQualifiedName(typeName, typeNs);
                            providerXsdType.Annotation = GetSchemaAnnotation(ExportActualType(xmlName, new XmlDocument()));
                            xsdType = providerXsdType;
                        }
                        else
                        {
                            foreach (XmlSchema schema in schemas.Schemas())
                            {
                                foreach (XmlSchemaObject schemaItem in schema.Items)
                                {
                                    if ((object)schemaItem == (object)providerXsdType)
                                    {
                                        typeNs = schema.TargetNamespace ?? string.Empty;
                                        break;
                                    }
                                }
                                if (typeNs != null)
                                    break;
                            }
                            if (typeNs == null)
                                throw new InvalidDataContractException(SR.Format(SR.MissingSchemaType, typeName, DataContract.GetClrTypeFullName(clrType)));
                            xmlName = new XmlQualifiedName(typeName, typeNs);
                        }
                    }
                    else
                        xmlName = (XmlQualifiedName)typeInfo;
                }
            }
            return true;
        }

        private static void InvokeGetSchemaMethod(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            Type clrType,
            XmlSchemaSet schemas, XmlQualifiedName xmlName)
        {
            IXmlSerializable ixmlSerializable = (IXmlSerializable)Activator.CreateInstance(clrType)!;
            XmlSchema? schema = ixmlSerializable.GetSchema();
            if (schema == null)
            {
                AddDefaultDatasetType(schemas, xmlName.Name, xmlName.Namespace);
            }
            else
            {
                if (string.IsNullOrEmpty(schema.Id))
                    throw new InvalidDataContractException(SR.Format(SR.InvalidReturnSchemaOnGetSchemaMethod, DataContract.GetClrTypeFullName(clrType)));
                AddDefaultTypedDatasetType(schemas, schema, xmlName.Name, xmlName.Namespace);
            }
        }

        internal static void AddDefaultXmlType(XmlSchemaSet schemas, string localName, string ns)
        {
            XmlSchemaComplexType defaultXmlType = CreateAnyType();
            defaultXmlType.Name = localName;
            XmlSchema schema = SchemaHelper.GetSchema(ns, schemas);
            schema.Items.Add(defaultXmlType);
            schemas.Reprocess(schema);
        }

        private static XmlSchemaComplexType CreateAnyType()
        {
            XmlSchemaComplexType anyType = new XmlSchemaComplexType();
            anyType.IsMixed = true;
            anyType.Particle = new XmlSchemaSequence();
            XmlSchemaAny any = new XmlSchemaAny();
            any.MinOccurs = 0;
            any.MaxOccurs = decimal.MaxValue;
            any.ProcessContents = XmlSchemaContentProcessing.Lax;
            ((XmlSchemaSequence)anyType.Particle).Items.Add(any);
            anyType.AnyAttribute = new XmlSchemaAnyAttribute();
            return anyType;
        }

        private static XmlSchemaComplexType CreateAnyElementType()
        {
            XmlSchemaComplexType anyElementType = new XmlSchemaComplexType();
            anyElementType.IsMixed = false;
            anyElementType.Particle = new XmlSchemaSequence();
            XmlSchemaAny any = new XmlSchemaAny();
            any.MinOccurs = 0;
            any.ProcessContents = XmlSchemaContentProcessing.Lax;
            ((XmlSchemaSequence)anyElementType.Particle).Items.Add(any);
            return anyElementType;
        }

        internal static bool IsSpecialXmlType(Type type, [NotNullWhen(true)] out XmlQualifiedName? typeName, [NotNullWhen(true)] out XmlSchemaType? xsdType, out bool hasRoot)
        {
            xsdType = null;
            hasRoot = true;
            if (type == Globals.TypeOfXmlElement || type == Globals.TypeOfXmlNodeArray)
            {
                string? name;
                if (type == Globals.TypeOfXmlElement)
                {
                    xsdType = CreateAnyElementType();
                    name = "XmlElement";
                    hasRoot = false;
                }
                else
                {
                    xsdType = CreateAnyType();
                    name = "ArrayOfXmlNode";
                    hasRoot = true;
                }
                typeName = new XmlQualifiedName(name, DataContract.GetDefaultXmlNamespace(type));
                return true;
            }
            typeName = null;
            return false;
        }

        private static void AddDefaultDatasetType(XmlSchemaSet schemas, string localName, string ns)
        {
            XmlSchemaComplexType type = new XmlSchemaComplexType();
            type.Name = localName;
            type.Particle = new XmlSchemaSequence();
            XmlSchemaElement schemaRefElement = new XmlSchemaElement();
            schemaRefElement.RefName = new XmlQualifiedName(Globals.SchemaLocalName, XmlSchema.Namespace);
            ((XmlSchemaSequence)type.Particle).Items.Add(schemaRefElement);
            XmlSchemaAny any = new XmlSchemaAny();
            ((XmlSchemaSequence)type.Particle).Items.Add(any);
            XmlSchema schema = SchemaHelper.GetSchema(ns, schemas);
            schema.Items.Add(type);
            schemas.Reprocess(schema);
        }

        private static void AddDefaultTypedDatasetType(XmlSchemaSet schemas, XmlSchema datasetSchema, string localName, string ns)
        {
            XmlSchemaComplexType type = new XmlSchemaComplexType();
            type.Name = localName;
            type.Particle = new XmlSchemaSequence();
            XmlSchemaAny any = new XmlSchemaAny();
            any.Namespace = datasetSchema.TargetNamespace ?? string.Empty;
            ((XmlSchemaSequence)type.Particle).Items.Add(any);
            schemas.Add(datasetSchema);
            XmlSchema schema = SchemaHelper.GetSchema(ns, schemas);
            schema.Items.Add(type);
            schemas.Reprocess(datasetSchema);
            schemas.Reprocess(schema);
        }

        private XmlSchemaAnnotation GetSchemaAnnotation(XmlQualifiedName annotationQualifiedName, string innerText, XmlSchema schema)
        {
            XmlSchemaAnnotation annotation = new XmlSchemaAnnotation();
            XmlSchemaAppInfo appInfo = new XmlSchemaAppInfo();
            XmlElement annotationElement = GetAnnotationMarkup(annotationQualifiedName, innerText, schema);
            appInfo.Markup = new XmlNode[1] { annotationElement };
            annotation.Items.Add(appInfo);
            return annotation;
        }

        private static XmlSchemaAnnotation? GetSchemaAnnotation(params XmlNode?[]? nodes)
        {
            if (nodes == null || nodes.Length == 0)
                return null;
            bool hasAnnotation = false;
            for (int i = 0; i < nodes.Length; i++)
                if (nodes[i] != null)
                {
                    hasAnnotation = true;
                    break;
                }
            if (!hasAnnotation)
                return null;

            XmlSchemaAnnotation annotation = new XmlSchemaAnnotation();
            XmlSchemaAppInfo appInfo = new XmlSchemaAppInfo();
            annotation.Items.Add(appInfo);
            appInfo.Markup = nodes;
            return annotation;
        }

        private XmlElement GetAnnotationMarkup(XmlQualifiedName annotationQualifiedName, string innerText, XmlSchema schema)
        {
            XmlElement annotationElement = XmlDoc.CreateElement(annotationQualifiedName.Name, annotationQualifiedName.Namespace);
            SchemaHelper.AddSchemaImport(annotationQualifiedName.Namespace, schema);
            annotationElement.InnerText = innerText;
            return annotationElement;
        }

        private XmlSchema GetSchema(string ns)
        {
            return SchemaHelper.GetSchema(ns, Schemas);
        }

        // Property is not stored in a local because XmlSchemaSequence is mutable.
        // The schema export process should not expose objects that may be modified later.
        internal static XmlSchemaSequence ISerializableSequence
        {
            get
            {
                XmlSchemaSequence iSerializableSequence = new XmlSchemaSequence();
                iSerializableSequence.Items.Add(ISerializableWildcardElement);
                return iSerializableSequence;
            }
        }

        // Property is not stored in a local because XmlSchemaAny is mutable.
        // The schema export process should not expose objects that may be modified later.
        internal static XmlSchemaAny ISerializableWildcardElement
        {
            get
            {
                XmlSchemaAny iSerializableWildcardElement = new XmlSchemaAny();
                iSerializableWildcardElement.MinOccurs = 0;
                iSerializableWildcardElement.MaxOccursString = Globals.OccursUnbounded;
                iSerializableWildcardElement.Namespace = "##local";
                iSerializableWildcardElement.ProcessContents = XmlSchemaContentProcessing.Skip;
                return iSerializableWildcardElement;
            }
        }

        internal static XmlQualifiedName AnytypeQualifiedName => field ??= new XmlQualifiedName(Globals.AnyTypeLocalName, Globals.SchemaNamespace);
        internal static XmlQualifiedName StringQualifiedName => field ??= new XmlQualifiedName(Globals.StringLocalName, Globals.SchemaNamespace);
        internal static XmlQualifiedName DefaultEnumBaseTypeName => field ??= new XmlQualifiedName(Globals.IntLocalName, Globals.SchemaNamespace);
        internal static XmlQualifiedName EnumerationValueAnnotationName => field ??= new XmlQualifiedName(Globals.EnumerationValueLocalName, Globals.SerializationNamespace);
        internal static XmlQualifiedName SurrogateDataAnnotationName => field ??= new XmlQualifiedName(Globals.SurrogateDataLocalName, Globals.SerializationNamespace);
        internal static XmlQualifiedName DefaultValueAnnotation => field ??= new XmlQualifiedName(Globals.DefaultValueLocalName, Globals.SerializationNamespace);
        internal static XmlQualifiedName ActualTypeAnnotationName => field ??= new XmlQualifiedName(Globals.ActualTypeLocalName, Globals.SerializationNamespace);
        internal static XmlQualifiedName IsDictionaryAnnotationName => field ??= new XmlQualifiedName(Globals.IsDictionaryLocalName, Globals.SerializationNamespace);
        internal static XmlQualifiedName IsValueTypeName => field ??= new XmlQualifiedName(Globals.IsValueTypeLocalName, Globals.SerializationNamespace);

        // Property is not stored in a local because XmlSchemaAttribute is mutable.
        // The schema export process should not expose objects that may be modified later.
        internal static XmlSchemaAttribute ISerializableFactoryTypeAttribute
        {
            get
            {
                XmlSchemaAttribute iSerializableFactoryTypeAttribute = new XmlSchemaAttribute();
                iSerializableFactoryTypeAttribute.RefName = new XmlQualifiedName(Globals.ISerializableFactoryTypeLocalName, Globals.SerializationNamespace);
                return iSerializableFactoryTypeAttribute;
            }
        }

        // Property is not stored in a local because XmlSchemaAttribute is mutable.
        // The schema export process should not expose objects that may be modified later.
        internal static XmlSchemaAttribute RefAttribute
        {
            get
            {
                XmlSchemaAttribute refAttribute = new XmlSchemaAttribute();
                refAttribute.RefName = Globals.RefQualifiedName;
                return refAttribute;
            }
        }

        // Property is not stored in a local because XmlSchemaAttribute is mutable.
        // The schema export process should not expose objects that may be modified later.
        internal static XmlSchemaAttribute IdAttribute
        {
            get
            {
                XmlSchemaAttribute idAttribute = new XmlSchemaAttribute();
                idAttribute.RefName = Globals.IdQualifiedName;
                return idAttribute;
            }
        }
    }
}
