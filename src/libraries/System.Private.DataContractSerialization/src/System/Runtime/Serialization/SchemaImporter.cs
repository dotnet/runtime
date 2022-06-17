// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if smolloy_add_schema_import

namespace System.Runtime.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using System.Xml.Schema;
    using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, DataContract>;
    using SchemaObjectDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, SchemaObjectInfo>;
    using System.Diagnostics.CodeAnalysis;

    internal sealed class SchemaImporter
    {
        private DataContractSet _dataContractSet;
        private XmlSchemaSet _schemaSet;
        private ICollection<XmlQualifiedName>? _typeNames;
        private ICollection<XmlSchemaElement> _elements;
        private XmlQualifiedName[] _elementTypeNames;
        private bool _importXmlDataType;
        private SchemaObjectDictionary _schemaObjects = null!;   // Not directly referenced. Always lazy initialized by property getter.
        private List<XmlSchemaRedefine> _redefineList = null!;   // Not directly referenced. Always lazy initialized by property getter.
        private bool _needToImportKnownTypesForObject;

        private static Hashtable? s_serializationSchemaElements;

        internal SchemaImporter(XmlSchemaSet schemas, ICollection<XmlQualifiedName>? typeNames, ICollection<XmlSchemaElement> elements, XmlQualifiedName[] elementTypeNames, DataContractSet dataContractSet, bool importXmlDataType)
        {
            _dataContractSet = dataContractSet;
            _schemaSet = schemas;
            _typeNames = typeNames;
            _elements = elements;
            _elementTypeNames = elementTypeNames;
            _importXmlDataType = importXmlDataType;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void Import()
        {
            if (!_schemaSet.Contains(Globals.SerializationNamespace))
            {
                StringReader reader = new StringReader(Globals.SerializationSchema);
                XmlSchema? schema = XmlSchema.Read(new XmlTextReader(reader) { DtdProcessing = DtdProcessing.Prohibit }, null);
                if (schema == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.CouldNotReadSerializationSchema, Globals.SerializationNamespace)));
                _schemaSet.Add(schema);
            }

            try
            {
                CompileSchemaSet(_schemaSet);
            }
#pragma warning suppress 56500 // covered by FxCOP
            catch (Exception ex)
            {
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.CannotImportInvalidSchemas), ex));
            }

            if (_typeNames == null)
            {
                ICollection schemaList = _schemaSet.Schemas();
                foreach (object schemaObj in schemaList)
                {
                    if (schemaObj == null)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.CannotImportNullSchema)));

                    XmlSchema schema = (XmlSchema)schemaObj;
                    if (schema.TargetNamespace != Globals.SerializationNamespace
                        && schema.TargetNamespace != Globals.SchemaNamespace)
                    {
                        foreach (XmlSchemaObject typeObj in schema.SchemaTypes.Values)
                        {
                            ImportType((XmlSchemaType)typeObj);
                        }
                        foreach (XmlSchemaElement element in schema.Elements.Values)
                        {
                            if (element.SchemaType != null)
                                ImportAnonymousGlobalElement(element, element.QualifiedName, schema.TargetNamespace);
                        }
                    }
                }
            }
            else
            {
                foreach (XmlQualifiedName typeName in _typeNames)
                {
                    if (typeName == null)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.CannotImportNullDataContractName)));
                    ImportType(typeName);
                }

                if (_elements.Count > 0)
                {
                    int i = 0;
                    foreach (XmlSchemaElement element in _elements)
                    {
                        XmlQualifiedName typeName = element.SchemaTypeName;
                        if (typeName != null && typeName.Name.Length > 0)
                        {
                            _elementTypeNames[i++] = ImportType(typeName).StableName;
                        }
                        else
                        {
                            XmlSchema? schema = SchemaHelper.GetSchemaWithGlobalElementDeclaration(element, _schemaSet);
                            if (schema == null)
                            {
                                _elementTypeNames[i++] = ImportAnonymousElement(element, element.QualifiedName).StableName;
                            }
                            else
                            {
                                _elementTypeNames[i++] = ImportAnonymousGlobalElement(element, element.QualifiedName, schema.TargetNamespace).StableName;
                            }
                        }
                    }
                }
            }
            ImportKnownTypesForObject();
        }

        internal static void CompileSchemaSet(XmlSchemaSet schemaSet)
        {
            if (schemaSet.Contains(XmlSchema.Namespace))
                schemaSet.Compile();
            else
            {
                // Add base XSD schema with top level element named "schema"
                XmlSchema xsdSchema = new XmlSchema();
                xsdSchema.TargetNamespace = XmlSchema.Namespace;
                XmlSchemaElement element = new XmlSchemaElement();
                element.Name = Globals.SchemaLocalName;
                element.SchemaType = new XmlSchemaComplexType();
                xsdSchema.Items.Add(element);
                schemaSet.Add(xsdSchema);
                schemaSet.Compile();
            }
        }

        private SchemaObjectDictionary SchemaObjects
        {
            get
            {
                if (_schemaObjects == null)
                    _schemaObjects = CreateSchemaObjects();
                return _schemaObjects;
            }
        }

        private List<XmlSchemaRedefine> RedefineList
        {
            get
            {
                if (_redefineList == null)
                    _redefineList = CreateRedefineList();
                return _redefineList;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void ImportKnownTypes(XmlQualifiedName typeName)
        {
            SchemaObjectInfo? schemaObjectInfo;
            if (SchemaObjects.TryGetValue(typeName, out schemaObjectInfo))
            {
                List<XmlSchemaType>? knownTypes = schemaObjectInfo._knownTypes;
                if (knownTypes != null)
                {
                    foreach (XmlSchemaType knownType in knownTypes)
                        ImportType(knownType);
                }
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static bool IsObjectContract(DataContract dataContract)
        {
            Dictionary<Type, object> previousCollectionTypes = new Dictionary<Type, object>();
            while (dataContract is CollectionDataContract)
            {
                if (dataContract.OriginalUnderlyingType == null)
                {
                    dataContract = ((CollectionDataContract)dataContract).ItemContract;
                    continue;
                }

                if (!previousCollectionTypes.ContainsKey(dataContract.OriginalUnderlyingType))
                {
                    previousCollectionTypes.Add(dataContract.OriginalUnderlyingType, dataContract.OriginalUnderlyingType);
                    dataContract = ((CollectionDataContract)dataContract).ItemContract;
                }
                else
                {
                    break;
                }
            }

            return dataContract is PrimitiveDataContract && ((PrimitiveDataContract)dataContract).UnderlyingType == Globals.TypeOfObject;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void ImportKnownTypesForObject()
        {
            if (!_needToImportKnownTypesForObject)
                return;
            _needToImportKnownTypesForObject = false;
            if (_dataContractSet.KnownTypesForObject == null)
            {
                SchemaObjectInfo? schemaObjectInfo;
                if (SchemaObjects.TryGetValue(SchemaExporter.AnytypeQualifiedName, out schemaObjectInfo))
                {
                    List<XmlSchemaType>? knownTypes = schemaObjectInfo._knownTypes;
                    if (knownTypes != null)
                    {
                        DataContractDictionary knownDataContracts = new DataContractDictionary();
                        foreach (XmlSchemaType knownType in knownTypes)
                        {
                            // Expected: will throw exception if schema set contains types that are not supported
                            DataContract dataContract = ImportType(knownType);
                            if (!knownDataContracts.TryGetValue(dataContract.StableName, out _))
                            {
                                knownDataContracts.Add(dataContract.StableName, dataContract);
                            }
                        }
                        _dataContractSet.KnownTypesForObject = knownDataContracts;
                    }
                }
            }
        }

        internal SchemaObjectDictionary CreateSchemaObjects()
        {
            SchemaObjectDictionary schemaObjects = new SchemaObjectDictionary();
            ICollection schemaList = _schemaSet.Schemas();
            List<XmlSchemaType> knownTypesForObject = new List<XmlSchemaType>();
            schemaObjects.Add(SchemaExporter.AnytypeQualifiedName, new SchemaObjectInfo(null, null, null, knownTypesForObject));

            foreach (XmlSchema schema in schemaList)
            {
                if (schema.TargetNamespace != Globals.SerializationNamespace)
                {
                    foreach (XmlSchemaObject schemaObj in schema.SchemaTypes.Values)
                    {
                        XmlSchemaType? schemaType = schemaObj as XmlSchemaType;
                        if (schemaType != null)
                        {
                            knownTypesForObject.Add(schemaType);

                            XmlQualifiedName currentTypeName = new XmlQualifiedName(schemaType.Name, schema.TargetNamespace);
                            SchemaObjectInfo? schemaObjectInfo;
                            if (schemaObjects.TryGetValue(currentTypeName, out schemaObjectInfo))
                            {
                                schemaObjectInfo._type = schemaType;
                                schemaObjectInfo._schema = schema;
                            }
                            else
                            {
                                schemaObjects.Add(currentTypeName, new SchemaObjectInfo(schemaType, null, schema, null));
                            }

                            XmlQualifiedName? baseTypeName = GetBaseTypeName(schemaType);
                            if (baseTypeName != null)
                            {
                                SchemaObjectInfo? baseTypeInfo;
                                if (schemaObjects.TryGetValue(baseTypeName, out baseTypeInfo))
                                {
                                    if (baseTypeInfo._knownTypes == null)
                                    {
                                        baseTypeInfo._knownTypes = new List<XmlSchemaType>();
                                    }
                                }
                                else
                                {
                                    baseTypeInfo = new SchemaObjectInfo(null, null, null, new List<XmlSchemaType>());
                                    schemaObjects.Add(baseTypeName, baseTypeInfo);
                                }
                                baseTypeInfo._knownTypes!.Add(schemaType);  // Verified not-null above, or created not-null.
                            }
                        }
                    }
                    foreach (XmlSchemaObject schemaObj in schema.Elements.Values)
                    {
                        XmlSchemaElement? schemaElement = schemaObj as XmlSchemaElement;
                        if (schemaElement != null)
                        {
                            XmlQualifiedName currentElementName = new XmlQualifiedName(schemaElement.Name, schema.TargetNamespace);
                            SchemaObjectInfo? schemaObjectInfo;
                            if (schemaObjects.TryGetValue(currentElementName, out schemaObjectInfo))
                            {
                                schemaObjectInfo._element = schemaElement;
                                schemaObjectInfo._schema = schema;
                            }
                            else
                            {
                                schemaObjects.Add(currentElementName, new SchemaObjectInfo(null, schemaElement, schema, null));
                            }
                        }
                    }
                }
            }
            return schemaObjects;
        }

        private static XmlQualifiedName? GetBaseTypeName(XmlSchemaType type)
        {
            XmlQualifiedName? baseTypeName = null;
            if (type is XmlSchemaComplexType complexType)
            {
                if (complexType.ContentModel != null)
                {
                    if (complexType.ContentModel is XmlSchemaComplexContent complexContent)
                    {
                        if (complexContent.Content is XmlSchemaComplexContentExtension extension)
                        {
                            baseTypeName = extension.BaseTypeName;
                        }
                    }
                }
            }
            return baseTypeName;
        }

        private List<XmlSchemaRedefine> CreateRedefineList()
        {
            List<XmlSchemaRedefine> list = new List<XmlSchemaRedefine>();

            ICollection schemaList = _schemaSet.Schemas();
            foreach (object schemaObj in schemaList)
            {
                if (schemaObj is XmlSchema schema)
                {
                    foreach (XmlSchemaExternal ext in schema.Includes)
                    {
                        if (ext is XmlSchemaRedefine redefine)
                            list.Add(redefine);
                    }
                }
            }

            return list;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private DataContract ImportAnonymousGlobalElement(XmlSchemaElement element, XmlQualifiedName typeQName, string? ns)
        {
            DataContract contract = ImportAnonymousElement(element, typeQName);
            if (contract is XmlDataContract xmlDataContract)
            {
                xmlDataContract.SetTopLevelElementName(new XmlQualifiedName(element.Name, ns));
                xmlDataContract.IsTopLevelElementNullable = element.IsNillable;
            }
            return contract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private DataContract ImportAnonymousElement(XmlSchemaElement element, XmlQualifiedName typeQName)
        {
            if (SchemaHelper.GetSchemaType(SchemaObjects, typeQName) != null)
            {
                for (int i = 1;; i++)
                {
                    typeQName = new XmlQualifiedName(typeQName.Name + i.ToString(NumberFormatInfo.InvariantInfo), typeQName.Namespace);
                    if (SchemaHelper.GetSchemaType(SchemaObjects, typeQName) == null)
                        break;
                    if (i == int.MaxValue)
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.CannotComputeUniqueName, element.Name)));
                }
            }
            if (element.SchemaType == null)
                return ImportType(SchemaExporter.AnytypeQualifiedName);
            else
                return ImportType(element.SchemaType, typeQName, true/*isAnonymous*/);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private DataContract ImportType(XmlQualifiedName typeName)
        {
            DataContract? dataContract = DataContract.GetBuiltInDataContract(typeName.Name, typeName.Namespace);
            if (dataContract == null)
            {
                XmlSchemaType? type = SchemaHelper.GetSchemaType(SchemaObjects, typeName);
                if (type == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.SpecifiedTypeNotFoundInSchema, typeName.Name, typeName.Namespace)));
                dataContract = ImportType(type);
            }
            if (IsObjectContract(dataContract))
                _needToImportKnownTypesForObject = true;
            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private DataContract ImportType(XmlSchemaType type)
        {
            return ImportType(type, type.QualifiedName, false/*isAnonymous*/);
        }


        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private DataContract ImportType(XmlSchemaType type, XmlQualifiedName typeName, bool isAnonymous)
        {
            DataContract? dataContract = _dataContractSet[typeName];
            if (dataContract != null)
                return dataContract;

            InvalidDataContractException invalidContractException;
            try
            {
                foreach (XmlSchemaRedefine redefine in RedefineList)
                {
                    if (redefine.SchemaTypes[typeName] != null)
                        ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.RedefineNotSupported));
                }

                if (type is XmlSchemaSimpleType)
                {
                    XmlSchemaSimpleType simpleType = (XmlSchemaSimpleType)type;
                    XmlSchemaSimpleTypeContent? content = simpleType.Content;
                    if (content is XmlSchemaSimpleTypeUnion)
                        ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.SimpleTypeUnionNotSupported));
                    else if (content is XmlSchemaSimpleTypeList)
                        dataContract = ImportFlagsEnum(typeName, (XmlSchemaSimpleTypeList)content, simpleType.Annotation);
                    else if (content is XmlSchemaSimpleTypeRestriction)
                    {
                        XmlSchemaSimpleTypeRestriction restriction = (XmlSchemaSimpleTypeRestriction)content;
                        if (CheckIfEnum(restriction))
                        {
                            dataContract = ImportEnum(typeName, restriction, false /*isFlags*/, simpleType.Annotation);
                        }
                        else
                        {
                            dataContract = ImportSimpleTypeRestriction(typeName, restriction);
                            if (dataContract.IsBuiltInDataContract && !isAnonymous)
                            {
                                _dataContractSet.InternalAdd(typeName, dataContract);
                            }
                        }
                    }
                }
                else if (type is XmlSchemaComplexType)
                {
                    XmlSchemaComplexType complexType = (XmlSchemaComplexType)type;
                    if (complexType.ContentModel == null)
                    {
                        CheckComplexType(typeName, complexType);
                        dataContract = ImportType(typeName, complexType.Particle, complexType.Attributes, complexType.AnyAttribute, null /* baseTypeName */, complexType.Annotation);
                    }
                    else
                    {
                        XmlSchemaContentModel contentModel = complexType.ContentModel;
                        if (contentModel is XmlSchemaSimpleContent)
                            ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.SimpleContentNotSupported));
                        else if (contentModel is XmlSchemaComplexContent)
                        {
                            XmlSchemaComplexContent complexContent = (XmlSchemaComplexContent)contentModel;
                            if (complexContent.IsMixed)
                                ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.MixedContentNotSupported));

                            if (complexContent.Content is XmlSchemaComplexContentExtension)
                            {
                                XmlSchemaComplexContentExtension extension = (XmlSchemaComplexContentExtension)complexContent.Content;
                                dataContract = ImportType(typeName, extension.Particle, extension.Attributes, extension.AnyAttribute, extension.BaseTypeName, complexType.Annotation);
                            }
                            else if (complexContent.Content is XmlSchemaComplexContentRestriction)
                            {
                                XmlSchemaComplexContentRestriction restriction = (XmlSchemaComplexContentRestriction)complexContent.Content;
                                XmlQualifiedName baseTypeName = restriction.BaseTypeName;
                                if (baseTypeName == SchemaExporter.AnytypeQualifiedName)
                                    dataContract = ImportType(typeName, restriction.Particle, restriction.Attributes, restriction.AnyAttribute, null /* baseTypeName */, complexType.Annotation);
                                else
                                    ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ComplexTypeRestrictionNotSupported));
                            }
                        }
                    }
                }

                if (dataContract == null)
                    ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, string.Empty);
                if (type.QualifiedName != XmlQualifiedName.Empty)
                    ImportTopLevelElement(typeName);
                Debug.Assert(dataContract != null); // Throws above if false
                ImportDataContractExtension(type, dataContract);
                ImportGenericInfo(type, dataContract);
                ImportKnownTypes(typeName);

                return dataContract;
            }
            catch (InvalidDataContractException e)
            {
                invalidContractException = e;
            }

            // Execution gets to this point if InvalidDataContractException was thrown
            if (_importXmlDataType)
            {
                RemoveFailedContract(typeName);
                return ImportXmlDataType(typeName, type, isAnonymous);
            }
            Type? referencedType;
            if (_dataContractSet.TryGetReferencedType(typeName, dataContract, out referencedType)
                || (string.IsNullOrEmpty(type.Name) && _dataContractSet.TryGetReferencedType(ImportActualType(type.Annotation, typeName, typeName), dataContract, out referencedType)))
            {
                if (Globals.TypeOfIXmlSerializable.IsAssignableFrom(referencedType))
                {
                    RemoveFailedContract(typeName);
                    return ImportXmlDataType(typeName, type, isAnonymous);
                }
            }
            XmlDataContract? specialContract = ImportSpecialXmlDataType(type, isAnonymous);
            if (specialContract != null)
            {
                _dataContractSet.Remove(typeName);
                return specialContract;
            }

            throw invalidContractException;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void RemoveFailedContract(XmlQualifiedName typeName)
        {
            ClassDataContract? oldContract = _dataContractSet[typeName] as ClassDataContract;
            _dataContractSet.Remove(typeName);
            if (oldContract != null)
            {
                ClassDataContract? ancestorDataContract = oldContract.BaseContract;
                while (ancestorDataContract != null)
                {
                    ancestorDataContract.KnownDataContracts?.Remove(typeName);
                    ancestorDataContract = ancestorDataContract.BaseContract;
                }
                if (_dataContractSet.KnownTypesForObject != null)
                    _dataContractSet.KnownTypesForObject.Remove(typeName);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private bool CheckIfEnum(XmlSchemaSimpleTypeRestriction restriction)
        {
            foreach (XmlSchemaFacet facet in restriction.Facets)
            {
                if (facet is not XmlSchemaEnumerationFacet)
                    return false;
            }

            XmlQualifiedName expectedBase = SchemaExporter.StringQualifiedName;
            if (restriction.BaseTypeName != XmlQualifiedName.Empty)
            {
                return ((restriction.BaseTypeName == expectedBase && restriction.Facets.Count > 0) || ImportType(restriction.BaseTypeName) is EnumDataContract);
            }
            else if (restriction.BaseType != null)
            {
                DataContract baseContract = ImportType(restriction.BaseType);
                return (baseContract.StableName == expectedBase || baseContract is EnumDataContract);
            }

            return false;
        }

        private static bool CheckIfCollection(XmlSchemaSequence rootSequence)
        {
            if (rootSequence.Items == null || rootSequence.Items.Count == 0)
                return false;
            RemoveOptionalUnknownSerializationElements(rootSequence.Items);
            if (rootSequence.Items.Count != 1)
                return false;

            XmlSchemaObject o = rootSequence.Items[0];
            if (o is XmlSchemaElement localElement)
                return (localElement.MaxOccursString == Globals.OccursUnbounded || localElement.MaxOccurs > 1);

            return false;
        }

        private static bool CheckIfISerializable(XmlSchemaSequence rootSequence, XmlSchemaObjectCollection attributes)
        {
            if (rootSequence.Items == null || rootSequence.Items.Count == 0)
                return false;

            RemoveOptionalUnknownSerializationElements(rootSequence.Items);

            if (attributes == null || attributes.Count == 0)
                return false;

            return (rootSequence.Items.Count == 1 && rootSequence.Items[0] is XmlSchemaAny);
        }

        private static void RemoveOptionalUnknownSerializationElements(XmlSchemaObjectCollection items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                XmlSchemaElement? element = items[i] as XmlSchemaElement;
                if (element != null && element.RefName != null &&
                   element.RefName.Namespace == Globals.SerializationNamespace &&
                   element.MinOccurs == 0)
                {
                    if (s_serializationSchemaElements == null)
                    {
                        XmlSchema serializationSchema = XmlSchema.Read(XmlReader.Create(new StringReader(Globals.SerializationSchema)), null)!; // Source is our constant. Schema is valid.
                        s_serializationSchemaElements = new Hashtable();
                        foreach (XmlSchemaObject schemaObject in serializationSchema.Items)
                        {
                            if (schemaObject is XmlSchemaElement schemaElement)
                                if (schemaElement.Name != null)
                                    s_serializationSchemaElements.Add(schemaElement.Name, schemaElement);
                        }
                    }
                    if (!s_serializationSchemaElements.ContainsKey(element.RefName.Name))
                    {
                        items.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private DataContract? ImportType(XmlQualifiedName typeName, XmlSchemaParticle? rootParticle, XmlSchemaObjectCollection attributes, XmlSchemaAnyAttribute? anyAttribute, XmlQualifiedName? baseTypeName, XmlSchemaAnnotation? annotation)
        {
            DataContract? dataContract = null;
            bool isDerived = (baseTypeName != null);

            bool isReference;
            ImportAttributes(typeName, attributes, anyAttribute, out isReference);

            if (rootParticle == null)
                dataContract = ImportClass(typeName, new XmlSchemaSequence(), baseTypeName, annotation, isReference);
            else if (rootParticle is XmlSchemaSequence rootSequence)
            {
                if (rootSequence.MinOccurs != 1)
                    ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.RootSequenceMustBeRequired));
                if (rootSequence.MaxOccurs != 1)
                    ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.RootSequenceMaxOccursMustBe));

                if (!isDerived && CheckIfCollection(rootSequence))
                    dataContract = ImportCollection(typeName, rootSequence, attributes, annotation, isReference);
                else if (CheckIfISerializable(rootSequence, attributes))
                    dataContract = ImportISerializable(typeName, rootSequence, baseTypeName, attributes, annotation);
                else
                    dataContract = ImportClass(typeName, rootSequence, baseTypeName, annotation, isReference);
            }
            else
                ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.RootParticleMustBeSequence));
            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private ClassDataContract ImportClass(XmlQualifiedName typeName, XmlSchemaSequence rootSequence, XmlQualifiedName? baseTypeName, XmlSchemaAnnotation? annotation, bool isReference)
        {
            ClassDataContract dataContract = new ClassDataContract(Globals.TypeOfSchemaTypePlaceholder);
            dataContract.StableName = typeName;
            AddDataContract(dataContract);

            dataContract.IsValueType = IsValueType(typeName, annotation);
            dataContract.IsReference = isReference;
            if (baseTypeName != null)
            {
                ImportBaseContract(baseTypeName, dataContract);
                Debug.Assert(dataContract.BaseContract != null);    // ImportBaseContract will always set this... or throw.
                if (dataContract.BaseContract.IsISerializable)
                {
                    if (IsISerializableDerived(typeName, rootSequence))
                        dataContract.IsISerializable = true;
                    else
                        ThrowTypeCannotBeImportedException(dataContract.StableName.Name, dataContract.StableName.Namespace, SR.Format(SR.DerivedTypeNotISerializable, baseTypeName.Name, baseTypeName.Namespace));
                }
                if (dataContract.BaseContract.IsReference)
                {
                    dataContract.IsReference = true;
                }
            }

            if (!dataContract.IsISerializable)
            {
                dataContract.Members = new List<DataMember>();
                RemoveOptionalUnknownSerializationElements(rootSequence.Items);
                for (int memberIndex = 0; memberIndex < rootSequence.Items.Count; memberIndex++)
                {
                    XmlSchemaElement? element = rootSequence.Items[memberIndex] as XmlSchemaElement;
                    if (element == null)
                        ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.MustContainOnlyLocalElements));
                    ImportClassMember(element!, dataContract);
                }
            }

            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private DataContract ImportXmlDataType(XmlQualifiedName typeName, XmlSchemaType xsdType, bool isAnonymous)
        {
            DataContract? dataContract = _dataContractSet[typeName];
            if (dataContract != null)
                return dataContract;

            XmlDataContract? xmlDataContract = ImportSpecialXmlDataType(xsdType, isAnonymous);
            if (xmlDataContract != null)
                return xmlDataContract;

            xmlDataContract = new XmlDataContract(Globals.TypeOfSchemaTypePlaceholder);
            xmlDataContract.StableName = typeName;
            xmlDataContract.IsValueType = false;
            AddDataContract(xmlDataContract);
            if (xsdType != null)
            {
                ImportDataContractExtension(xsdType, xmlDataContract);
                xmlDataContract.IsValueType = IsValueType(typeName, xsdType.Annotation);
                xmlDataContract.IsTypeDefinedOnImport = true;
                xmlDataContract.XsdType = isAnonymous ? xsdType : null;
                xmlDataContract.HasRoot = !IsXmlAnyElementType(xsdType as XmlSchemaComplexType);
            }
            else
            {
                //Value type can be used by both nillable and non-nillable elements but reference type cannot be used by non nillable elements
                xmlDataContract.IsValueType = true;
                xmlDataContract.IsTypeDefinedOnImport = false;
                xmlDataContract.HasRoot = true;
            }
            if (!isAnonymous)
            {
                bool isNullable;
                xmlDataContract.SetTopLevelElementName(SchemaHelper.GetGlobalElementDeclaration(_schemaSet, typeName, out isNullable));
                xmlDataContract.IsTopLevelElementNullable = isNullable;
            }
            return xmlDataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlDataContract? ImportSpecialXmlDataType(XmlSchemaType xsdType, bool isAnonymous)
        {
            if (!isAnonymous)
                return null;
            XmlSchemaComplexType? complexType = xsdType as XmlSchemaComplexType;
            if (complexType == null)
                return null;
            if (IsXmlAnyElementType(complexType))
            {
                //check if the type is XElement
                XmlQualifiedName xlinqTypeName = new XmlQualifiedName("XElement", "http://schemas.datacontract.org/2004/07/System.Xml.Linq");
                Type? referencedType;
                if (_dataContractSet.TryGetReferencedType(xlinqTypeName, null, out referencedType)
                    && Globals.TypeOfIXmlSerializable.IsAssignableFrom(referencedType))
                {
                    XmlDataContract xmlDataContract = new XmlDataContract(referencedType);
                    AddDataContract(xmlDataContract);
                    return xmlDataContract;
                }
                //otherwise, assume XmlElement
                return (XmlDataContract?)DataContract.GetBuiltInDataContract(Globals.TypeOfXmlElement);
            }
            if (IsXmlAnyType(complexType))
                return (XmlDataContract?)DataContract.GetBuiltInDataContract(Globals.TypeOfXmlNodeArray);
            return null;
        }

        private static bool IsXmlAnyElementType(XmlSchemaComplexType? xsdType)
        {
            if (xsdType == null)
                return false;

            if (xsdType.Particle is XmlSchemaSequence sequence)
            {
                if (sequence.Items == null || sequence.Items.Count != 1)
                    return false;

                XmlSchemaAny? any = sequence.Items[0] as XmlSchemaAny;
                if (any == null || any.Namespace != null)
                    return false;

                if (xsdType.AnyAttribute != null || (xsdType.Attributes != null && xsdType.Attributes.Count > 0))
                    return false;

                return true;
            }

            return false;
        }

        private static bool IsXmlAnyType(XmlSchemaComplexType xsdType)
        {
            if (xsdType == null)
                return false;

            if (xsdType.Particle is XmlSchemaSequence sequence)
            {
                if (sequence.Items == null || sequence.Items.Count != 1)
                    return false;

                XmlSchemaAny? any = sequence.Items[0] as XmlSchemaAny;
                if (any == null || any.Namespace != null)
                    return false;

                if (any.MaxOccurs != decimal.MaxValue)
                    return false;

                if (xsdType.AnyAttribute == null || xsdType.Attributes.Count > 0)
                    return false;

                return true;
            }

            return false;
        }

        private static bool IsValueType(XmlQualifiedName typeName, XmlSchemaAnnotation? annotation)
        {
            string? isValueTypeInnerText = GetInnerText(typeName, ImportAnnotation(annotation, SchemaExporter.IsValueTypeName));
            if (isValueTypeInnerText != null)
            {
                try
                {
                    return XmlConvert.ToBoolean(isValueTypeInnerText);
                }
                catch (FormatException fe)
                {
                    ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.IsValueTypeFormattedIncorrectly, isValueTypeInnerText, fe.Message));
                }
            }
            return false;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private ClassDataContract ImportISerializable(XmlQualifiedName typeName, XmlSchemaSequence rootSequence, XmlQualifiedName? baseTypeName, XmlSchemaObjectCollection attributes, XmlSchemaAnnotation? annotation)
        {
            ClassDataContract dataContract = new ClassDataContract(Globals.TypeOfSchemaTypePlaceholder);
            dataContract.StableName = typeName;
            dataContract.IsISerializable = true;
            AddDataContract(dataContract);

            dataContract.IsValueType = IsValueType(typeName, annotation);
            if (baseTypeName == null)
                CheckISerializableBase(typeName, rootSequence, attributes);
            else
            {
                ImportBaseContract(baseTypeName, dataContract);
                Debug.Assert(dataContract.BaseContract != null);    // ImportBaseContract will always set this... or throw.
                if (!dataContract.BaseContract.IsISerializable)
                    ThrowISerializableTypeCannotBeImportedException(dataContract.StableName.Name, dataContract.StableName.Namespace, SR.Format(SR.BaseTypeNotISerializable, baseTypeName.Name, baseTypeName.Namespace));
                if (!IsISerializableDerived(typeName, rootSequence))
                    ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ISerializableDerivedContainsOneOrMoreItems));
            }

            return dataContract;
        }

        private static void CheckISerializableBase(XmlQualifiedName typeName, XmlSchemaSequence? rootSequence, XmlSchemaObjectCollection attributes)
        {
            if (rootSequence == null)
                ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ISerializableDoesNotContainAny));

            if (rootSequence.Items == null || rootSequence.Items.Count < 1)
                ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ISerializableDoesNotContainAny));
            else if (rootSequence.Items.Count > 1)
                ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ISerializableContainsMoreThanOneItems));

            XmlSchemaObject o = rootSequence.Items[0];
            if (!(o is XmlSchemaAny))
                ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ISerializableDoesNotContainAny));

            XmlSchemaAny wildcard = (XmlSchemaAny)o;
            XmlSchemaAny iSerializableWildcardElement = SchemaExporter.ISerializableWildcardElement;
            if (wildcard.MinOccurs != iSerializableWildcardElement.MinOccurs)
                ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ISerializableWildcardMinOccursMustBe, iSerializableWildcardElement.MinOccurs));
            if (wildcard.MaxOccursString != iSerializableWildcardElement.MaxOccursString)
                ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ISerializableWildcardMaxOccursMustBe, iSerializableWildcardElement.MaxOccursString));
            if (wildcard.Namespace != iSerializableWildcardElement.Namespace)
                ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ISerializableWildcardNamespaceInvalid, iSerializableWildcardElement.Namespace));
            if (wildcard.ProcessContents != iSerializableWildcardElement.ProcessContents)
                ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ISerializableWildcardProcessContentsInvalid, iSerializableWildcardElement.ProcessContents));

            XmlQualifiedName factoryTypeAttributeRefName = SchemaExporter.ISerializableFactoryTypeAttribute.RefName;
            bool containsFactoryTypeAttribute = false;
            if (attributes != null)
            {
                for (int i = 0; i < attributes.Count; i++)
                {
                    o = attributes[i];
                    if (o is XmlSchemaAttribute)
                    {
                        if (((XmlSchemaAttribute)o).RefName == factoryTypeAttributeRefName)
                        {
                            containsFactoryTypeAttribute = true;
                            break;
                        }
                    }
                }
            }
            if (!containsFactoryTypeAttribute)
                ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ISerializableMustRefFactoryTypeAttribute, factoryTypeAttributeRefName.Name, factoryTypeAttributeRefName.Namespace));
        }

        private static bool IsISerializableDerived(XmlQualifiedName typeName, XmlSchemaSequence? rootSequence)
        {
            return (rootSequence == null || rootSequence.Items == null || rootSequence.Items.Count == 0);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void ImportBaseContract(XmlQualifiedName baseTypeName, ClassDataContract dataContract)
        {
            ClassDataContract? baseContract = ImportType(baseTypeName) as ClassDataContract;
            if (baseContract == null)
                ThrowTypeCannotBeImportedException(dataContract.StableName.Name, dataContract.StableName.Namespace, SR.Format(dataContract.IsISerializable ? SR.InvalidISerializableDerivation : SR.InvalidClassDerivation, baseTypeName.Name, baseTypeName.Namespace));

            // Note: code ignores IsValueType annotation if derived type exists
            if (baseContract.IsValueType)
                baseContract.IsValueType = false;

            ClassDataContract? ancestorDataContract = baseContract;
            while (ancestorDataContract != null)
            {
                DataContractDictionary? knownDataContracts = ancestorDataContract.KnownDataContracts;
                if (knownDataContracts == null)
                {
                    knownDataContracts = new DataContractDictionary();
                    ancestorDataContract.KnownDataContracts = knownDataContracts;
                }
                knownDataContracts.Add(dataContract.StableName, dataContract);
                ancestorDataContract = ancestorDataContract.BaseContract;
            }

            dataContract.BaseContract = baseContract;
        }

        private void ImportTopLevelElement(XmlQualifiedName typeName)
        {
            XmlSchemaElement? topLevelElement = SchemaHelper.GetSchemaElement(SchemaObjects, typeName);
            // Top level element of same name is not required, but is validated if it is present
            if (topLevelElement == null)
                return;
            else
            {
                XmlQualifiedName elementTypeName = topLevelElement.SchemaTypeName;
                if (elementTypeName.IsEmpty)
                {
                    if (topLevelElement.SchemaType != null)
                        ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.AnonymousTypeNotSupported, typeName.Name, typeName.Namespace));
                    else
                        elementTypeName = SchemaExporter.AnytypeQualifiedName;
                }
                if (elementTypeName != typeName)
                    ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.TopLevelElementRepresentsDifferentType, topLevelElement.SchemaTypeName.Name, topLevelElement.SchemaTypeName.Namespace));
                CheckIfElementUsesUnsupportedConstructs(typeName, topLevelElement);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void ImportClassMember(XmlSchemaElement element, ClassDataContract dataContract)
        {
            XmlQualifiedName typeName = dataContract.StableName;

            Debug.Assert(element.Name != null);

            if (element.MinOccurs > 1)
                ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ElementMinOccursMustBe, element.Name));
            if (element.MaxOccurs != 1)
                ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ElementMaxOccursMustBe, element.Name));

            DataContract? memberTypeContract = null;
            string memberName = element.Name;
            bool memberIsRequired = (element.MinOccurs > 0);
            bool memberIsNullable = element.IsNillable;
            bool memberEmitDefaultValue;
            int memberOrder = 0;

            XmlSchemaForm elementForm = element.Form;
            if (elementForm == XmlSchemaForm.None)
            {
                XmlSchema? schema = SchemaHelper.GetSchemaWithType(SchemaObjects, _schemaSet, typeName);
                if (schema != null)
                    elementForm = schema.ElementFormDefault;
            }
            if (elementForm != XmlSchemaForm.Qualified)
                ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.FormMustBeQualified, element.Name));
            CheckIfElementUsesUnsupportedConstructs(typeName, element);

            if (element.SchemaTypeName.IsEmpty)
            {
                if (element.SchemaType != null)
                    memberTypeContract = ImportAnonymousElement(element, new XmlQualifiedName(string.Format(CultureInfo.InvariantCulture, "{0}.{1}Type", typeName.Name, element.Name), typeName.Namespace));
                else if (!element.RefName.IsEmpty)
                    ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ElementRefOnLocalElementNotSupported, element.RefName.Name, element.RefName.Namespace));
                else
                    memberTypeContract = ImportType(SchemaExporter.AnytypeQualifiedName);
            }
            else
            {
                XmlQualifiedName memberTypeName = ImportActualType(element.Annotation, element.SchemaTypeName, typeName);
                memberTypeContract = ImportType(memberTypeName);
                if (IsObjectContract(memberTypeContract))
                    _needToImportKnownTypesForObject = true;
            }
            bool? emitDefaultValueFromAnnotation = ImportEmitDefaultValue(element.Annotation, typeName);
            if (!memberTypeContract.IsValueType && !memberIsNullable)
            {
                if (emitDefaultValueFromAnnotation != null && emitDefaultValueFromAnnotation.Value)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.InvalidEmitDefaultAnnotation, memberName, typeName.Name, typeName.Namespace)));
                memberEmitDefaultValue = false;
            }
            else
                memberEmitDefaultValue = emitDefaultValueFromAnnotation != null ? emitDefaultValueFromAnnotation.Value : Globals.DefaultEmitDefaultValue;

            Debug.Assert(dataContract.Members != null); // This method is only called from ImportClass() after that method has initialized the Members collection.

            int prevMemberIndex = dataContract.Members.Count - 1;
            if (prevMemberIndex >= 0)
            {
                DataMember prevMember = dataContract.Members![prevMemberIndex];
                if (prevMember.Order > Globals.DefaultOrder)
                    memberOrder = dataContract.Members.Count;
                DataMember currentMember = new DataMember(memberTypeContract, memberName, memberIsNullable, memberIsRequired, memberEmitDefaultValue, memberOrder);
                int compare = ClassDataContract.DataMemberComparer.Singleton.Compare(prevMember, currentMember);
                if (compare == 0)
                    ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.CannotHaveDuplicateElementNames, memberName));
                else if (compare > 0)
                    memberOrder = dataContract.Members.Count;
            }
            DataMember dataMember = new DataMember(memberTypeContract, memberName, memberIsNullable, memberIsRequired, memberEmitDefaultValue, memberOrder);

#if smolloy_add_ext_surrogate
            XmlQualifiedName surrogateDataAnnotationName = SchemaExporter.SurrogateDataAnnotationName;
            _dataContractSet.SetSurrogateData(dataMember, ImportSurrogateData(ImportAnnotation(element.Annotation, surrogateDataAnnotationName), surrogateDataAnnotationName.Name, surrogateDataAnnotationName.Namespace));
#endif

            dataContract.Members.Add(dataMember);
        }

        private static bool? ImportEmitDefaultValue(XmlSchemaAnnotation? annotation, XmlQualifiedName typeName)
        {
            XmlElement? defaultValueElement = ImportAnnotation(annotation, SchemaExporter.DefaultValueAnnotation);
            if (defaultValueElement == null)
                return null;
            XmlNode? emitDefaultValueAttribute = defaultValueElement.Attributes.GetNamedItem(Globals.EmitDefaultValueAttribute);
            if (emitDefaultValueAttribute?.Value == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.AnnotationAttributeNotFound, SchemaExporter.DefaultValueAnnotation.Name, typeName.Name, typeName.Namespace, Globals.EmitDefaultValueAttribute)));
            return XmlConvert.ToBoolean(emitDefaultValueAttribute.Value);
        }

        internal static XmlQualifiedName ImportActualType(XmlSchemaAnnotation? annotation, XmlQualifiedName defaultTypeName, XmlQualifiedName typeName)
        {
            XmlElement? actualTypeElement = ImportAnnotation(annotation, SchemaExporter.ActualTypeAnnotationName);
            if (actualTypeElement == null)
                return defaultTypeName;

            XmlNode? nameAttribute = actualTypeElement.Attributes.GetNamedItem(Globals.ActualTypeNameAttribute);
            if (nameAttribute?.Value == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.AnnotationAttributeNotFound, SchemaExporter.ActualTypeAnnotationName.Name, typeName.Name, typeName.Namespace, Globals.ActualTypeNameAttribute)));
            XmlNode? nsAttribute = actualTypeElement.Attributes.GetNamedItem(Globals.ActualTypeNamespaceAttribute);
            if (nsAttribute?.Value == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.AnnotationAttributeNotFound, SchemaExporter.ActualTypeAnnotationName.Name, typeName.Name, typeName.Namespace, Globals.ActualTypeNamespaceAttribute)));
            return new XmlQualifiedName(nameAttribute.Value, nsAttribute.Value);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private CollectionDataContract ImportCollection(XmlQualifiedName typeName, XmlSchemaSequence rootSequence, XmlSchemaObjectCollection attributes, XmlSchemaAnnotation? annotation, bool isReference)
        {
            CollectionDataContract dataContract = new CollectionDataContract(Globals.TypeOfSchemaTypePlaceholder, CollectionKind.Array);
            dataContract.StableName = typeName;
            AddDataContract(dataContract);

            dataContract.IsReference = isReference;

            // CheckIfCollection has already checked if sequence contains exactly one item with maxOccurs="unbounded" or maxOccurs > 1
            XmlSchemaElement element = (XmlSchemaElement)rootSequence.Items[0];

            Debug.Assert(element.Name != null);

            dataContract.IsItemTypeNullable = element.IsNillable;
            dataContract.ItemName = element.Name;

            XmlSchemaForm elementForm = element.Form;
            if (elementForm == XmlSchemaForm.None)
            {
                XmlSchema? schema = SchemaHelper.GetSchemaWithType(SchemaObjects, _schemaSet, typeName);
                if (schema != null)
                    elementForm = schema.ElementFormDefault;
            }
            if (elementForm != XmlSchemaForm.Qualified)
                ThrowArrayTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ArrayItemFormMustBe, element.Name));
            CheckIfElementUsesUnsupportedConstructs(typeName, element);

            if (element.SchemaTypeName.IsEmpty)
            {
                if (element.SchemaType != null)
                {
                    XmlQualifiedName shortName = new XmlQualifiedName(element.Name, typeName.Namespace);
                    DataContract? contract = _dataContractSet[shortName];
                    if (contract == null)
                    {
                        dataContract.ItemContract = ImportAnonymousElement(element, shortName);
                    }
                    else
                    {
                        XmlQualifiedName fullName = new XmlQualifiedName(string.Format(CultureInfo.InvariantCulture, "{0}.{1}Type", typeName.Name, element.Name), typeName.Namespace);
                        dataContract.ItemContract = ImportAnonymousElement(element, fullName);
                    }
                }
                else if (!element.RefName.IsEmpty)
                    ThrowArrayTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.ElementRefOnLocalElementNotSupported, element.RefName.Name, element.RefName.Namespace));
                else
                    dataContract.ItemContract = ImportType(SchemaExporter.AnytypeQualifiedName);
            }
            else
            {
                dataContract.ItemContract = ImportType(element.SchemaTypeName);
            }

            if (IsDictionary(typeName, annotation))
            {
                ClassDataContract? keyValueContract = dataContract.ItemContract as ClassDataContract;
                DataMember key = null!, value = null!;  // Set in the following || conditional chain. If the chain triggers before setting these, an exception is thrown.
                if (keyValueContract == null || keyValueContract.Members == null || keyValueContract.Members.Count != 2
                    || !(key = keyValueContract.Members[0]).IsRequired || !(value = keyValueContract.Members[1]).IsRequired)
                {
                    ThrowArrayTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.InvalidKeyValueType, element.Name));
                }
                if (keyValueContract.Namespace != dataContract.Namespace)
                {
                    ThrowArrayTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.InvalidKeyValueTypeNamespace, element.Name, keyValueContract.Namespace));
                }
                keyValueContract.IsValueType = true;
                dataContract.KeyName = key.Name;
                dataContract.ValueName = value.Name;
                if (element.SchemaType != null)
                {
                    _dataContractSet.Remove(keyValueContract.StableName);

                    GenericInfo genericInfo = new GenericInfo(DataContract.GetStableName(Globals.TypeOfKeyValue), Globals.TypeOfKeyValue.FullName);
                    genericInfo.Add(GetGenericInfoForDataMember(key));
                    genericInfo.Add(GetGenericInfoForDataMember(value));
                    genericInfo.AddToLevel(0, 2);
                    dataContract.ItemContract.StableName = new XmlQualifiedName(genericInfo.GetExpandedStableName().Name, typeName.Namespace);
                }
            }

            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static GenericInfo GetGenericInfoForDataMember(DataMember dataMember)
        {
            GenericInfo genericInfo;
            if (dataMember.MemberTypeContract.IsValueType && dataMember.IsNullable)
            {
                genericInfo = new GenericInfo(DataContract.GetStableName(Globals.TypeOfNullable), Globals.TypeOfNullable.FullName);
                genericInfo.Add(new GenericInfo(dataMember.MemberTypeContract.StableName, null));
            }
            else
            {
                genericInfo = new GenericInfo(dataMember.MemberTypeContract.StableName, null);
            }

            return genericInfo;
        }

        private static bool IsDictionary(XmlQualifiedName typeName, XmlSchemaAnnotation? annotation)
        {
            string? isDictionaryInnerText = GetInnerText(typeName, ImportAnnotation(annotation, SchemaExporter.IsDictionaryAnnotationName));
            if (isDictionaryInnerText != null)
            {
                try
                {
                    return XmlConvert.ToBoolean(isDictionaryInnerText);
                }
                catch (FormatException fe)
                {
                    ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.IsDictionaryFormattedIncorrectly, isDictionaryInnerText, fe.Message));
                }
            }
            return false;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private EnumDataContract? ImportFlagsEnum(XmlQualifiedName typeName, XmlSchemaSimpleTypeList list, XmlSchemaAnnotation? annotation)
        {
            XmlSchemaSimpleType? anonymousType = list.ItemType;
            if (anonymousType == null)
                ThrowEnumTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.EnumListMustContainAnonymousType));

            XmlSchemaSimpleTypeContent? content = anonymousType.Content;
            if (content is XmlSchemaSimpleTypeUnion)
                ThrowEnumTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.EnumUnionInAnonymousTypeNotSupported));
            else if (content is XmlSchemaSimpleTypeList)
                ThrowEnumTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.EnumListInAnonymousTypeNotSupported));
            else if (content is XmlSchemaSimpleTypeRestriction)
            {
                if (content is XmlSchemaSimpleTypeRestriction restriction && CheckIfEnum(restriction))
                    return ImportEnum(typeName, restriction, true /*isFlags*/, annotation);
                else
                    ThrowEnumTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.EnumRestrictionInvalid));
            }
            return null;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private EnumDataContract ImportEnum(XmlQualifiedName typeName, XmlSchemaSimpleTypeRestriction restriction, bool isFlags, XmlSchemaAnnotation? annotation)
        {
            EnumDataContract dataContract = new EnumDataContract(Globals.TypeOfSchemaTypePlaceholder);
            dataContract.StableName = typeName;
            dataContract.BaseContractName = ImportActualType(annotation, SchemaExporter.DefaultEnumBaseTypeName, typeName);
            dataContract.IsFlags = isFlags;
            AddDataContract(dataContract);

            // CheckIfEnum has already checked if baseType of restriction is string
            dataContract.Values = new List<long>();
            dataContract.Members = new List<DataMember>();
            foreach (XmlSchemaFacet facet in restriction.Facets)
            {
                XmlSchemaEnumerationFacet? enumFacet = facet as XmlSchemaEnumerationFacet;
                if (enumFacet == null)
                    ThrowEnumTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.EnumOnlyEnumerationFacetsSupported));
                if (enumFacet.Value == null)
                    ThrowEnumTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.EnumEnumerationFacetsMustHaveValue));

                string? valueInnerText = GetInnerText(typeName, ImportAnnotation(enumFacet.Annotation, SchemaExporter.EnumerationValueAnnotationName));
                if (valueInnerText == null)
                    dataContract.Values.Add(SchemaExporter.GetDefaultEnumValue(isFlags, dataContract.Members.Count));
                else
                    dataContract.Values.Add(dataContract.GetEnumValueFromString(valueInnerText));
                DataMember dataMember = new DataMember(Globals.SchemaMemberInfoPlaceholder) { Name = enumFacet.Value };
                dataContract.Members.Add(dataMember);
            }
            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private DataContract ImportSimpleTypeRestriction(XmlQualifiedName typeName, XmlSchemaSimpleTypeRestriction restriction)
        {
            DataContract dataContract = null!;  // Always assigned by one of the ImportType()s, or exception is thrown.

            if (!restriction.BaseTypeName.IsEmpty)
                dataContract = ImportType(restriction.BaseTypeName);
            else if (restriction.BaseType != null)
                dataContract = ImportType(restriction.BaseType);
            else
                ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.SimpleTypeRestrictionDoesNotSpecifyBase));

            return dataContract;
        }

#if smolloy_add_ext_surrogate
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void ImportDataContractExtension(XmlSchemaType type, DataContract dataContract)
        {
            if (type.Annotation == null || type.Annotation.Items == null)
                return;
            foreach (XmlSchemaObject schemaObject in type.Annotation.Items)
            {
                if (schemaObject is XmlSchemaAppInfo appInfo && appInfo.Markup != null)
                {
                    foreach (XmlNode? xmlNode in appInfo.Markup)
                    {
                        XmlElement? typeElement = xmlNode as XmlElement;
                        XmlQualifiedName surrogateDataAnnotationName = SchemaExporter.SurrogateDataAnnotationName;
                        if (typeElement != null && typeElement.NamespaceURI == surrogateDataAnnotationName.Namespace && typeElement.LocalName == surrogateDataAnnotationName.Name)
                        {
                            object? surrogateData = ImportSurrogateData(typeElement, surrogateDataAnnotationName.Name, surrogateDataAnnotationName.Namespace);
                            _dataContractSet.SetSurrogateData(dataContract, surrogateData);
                        }
                    }
                }
            }
        }
#else
        private static void ImportDataContractExtension(XmlSchemaType type, DataContract dataContract) { }
#endif

        private void ImportGenericInfo(XmlSchemaType type, DataContract dataContract)
        {
            if (type.Annotation == null || type.Annotation.Items == null)
                return;
            foreach (XmlSchemaObject schemaObject in type.Annotation.Items)
            {
                if (schemaObject is XmlSchemaAppInfo appInfo && appInfo.Markup != null)
                {
                    foreach (XmlNode? xmlNode in appInfo.Markup)
                    {
                        XmlElement? typeElement = xmlNode as XmlElement;
                        if (typeElement != null && typeElement.NamespaceURI == Globals.SerializationNamespace)
                        {
                            if (typeElement.LocalName == Globals.GenericTypeLocalName)
                                dataContract.GenericInfo = ImportGenericInfo(typeElement, type);
                        }
                    }
                }
            }
        }

        private GenericInfo ImportGenericInfo(XmlElement typeElement, XmlSchemaType type)
        {
            string? name = typeElement.Attributes.GetNamedItem(Globals.GenericNameAttribute)?.Value;
            if (name == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.GenericAnnotationAttributeNotFound, type.Name, Globals.GenericNameAttribute)));
            string? ns = typeElement.Attributes.GetNamedItem(Globals.GenericNamespaceAttribute)?.Value;
            if (ns == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.GenericAnnotationAttributeNotFound, type.Name, Globals.GenericNamespaceAttribute)));
            if (typeElement.ChildNodes.Count > 0) //Generic Type
                name = DataContract.EncodeLocalName(name);

            int currentLevel = 0;
            GenericInfo genInfo = new GenericInfo(new XmlQualifiedName(name, ns), type.Name);
            foreach (XmlNode childNode in typeElement.ChildNodes)
            {
                if (childNode is XmlElement argumentElement)
                {
                    if (argumentElement.LocalName != Globals.GenericParameterLocalName ||
                        argumentElement.NamespaceURI != Globals.SerializationNamespace)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.GenericAnnotationHasInvalidElement, argumentElement.LocalName, argumentElement.NamespaceURI, type.Name)));
                    XmlNode? nestedLevelAttribute = argumentElement.Attributes.GetNamedItem(Globals.GenericParameterNestedLevelAttribute);
                    int argumentLevel = 0;
                    if (nestedLevelAttribute != null)
                    {
                        if (!int.TryParse(nestedLevelAttribute.Value, out argumentLevel))
                            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.GenericAnnotationHasInvalidAttributeValue, argumentElement.LocalName, argumentElement.NamespaceURI, type.Name, nestedLevelAttribute.Value, nestedLevelAttribute.LocalName, Globals.TypeOfInt.Name)));
                    }
                    if (argumentLevel < currentLevel)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.GenericAnnotationForNestedLevelMustBeIncreasing, argumentElement.LocalName, argumentElement.NamespaceURI, type.Name)));
                    genInfo.Add(ImportGenericInfo(argumentElement, type));
                    genInfo.AddToLevel(argumentLevel, 1);
                    currentLevel = argumentLevel;
                }
            }

            XmlNode? typeNestedLevelsAttribute = typeElement.Attributes.GetNamedItem(Globals.GenericParameterNestedLevelAttribute);
            if (typeNestedLevelsAttribute != null)
            {
                int nestedLevels;
                if (!int.TryParse(typeNestedLevelsAttribute.Value, out nestedLevels))
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.GenericAnnotationHasInvalidAttributeValue, typeElement.LocalName, typeElement.NamespaceURI, type.Name, typeNestedLevelsAttribute.Value, typeNestedLevelsAttribute.LocalName, Globals.TypeOfInt.Name)));
                if ((nestedLevels - 1) > currentLevel)
                    genInfo.AddToLevel(nestedLevels - 1, 0);
            }
            return genInfo;
        }

#if smolloy_add_ext_surrogate
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private object? ImportSurrogateData(XmlElement? typeElement, string name, string ns)
        {
            if (_dataContractSet.SerializationExtendedSurrogateProvider != null && typeElement != null)
            {
                Collection<Type> knownTypes = new Collection<Type>();
                DataContractSurrogateCaller.GetKnownCustomDataTypes(_dataContractSet.SerializationExtendedSurrogateProvider, knownTypes);
                DataContractSerializer serializer = new DataContractSerializer(Globals.TypeOfObject, name, ns, knownTypes,
                    int.MaxValue, false /*ignoreExtensionDataObject*/, true /*preserveObjectReferences*/);
                return serializer.ReadObject(new XmlNodeReader(typeElement));
            }
            return null;
        }
#endif

        private static void CheckComplexType(XmlQualifiedName typeName, XmlSchemaComplexType type)
        {
            if (type.IsAbstract)
                ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.AbstractTypeNotSupported));
            if (type.IsMixed)
                ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.MixedContentNotSupported));
        }

        private static void CheckIfElementUsesUnsupportedConstructs(XmlQualifiedName typeName, XmlSchemaElement element)
        {
            if (element.IsAbstract)
                ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.AbstractElementNotSupported, element.Name));
            if (element.DefaultValue != null)
                ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.DefaultOnElementNotSupported, element.Name));
            if (element.FixedValue != null)
                ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.FixedOnElementNotSupported, element.Name));
            if (!element.SubstitutionGroup.IsEmpty)
                ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.SubstitutionGroupOnElementNotSupported, element.Name));
        }

        private static void ImportAttributes(XmlQualifiedName typeName, XmlSchemaObjectCollection attributes, XmlSchemaAnyAttribute? anyAttribute, out bool isReference)
        {
            if (anyAttribute != null)
                ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.AnyAttributeNotSupported));

            isReference = false;
            if (attributes != null)
            {
                bool foundId = false, foundRef = false;
                for (int i = 0; i < attributes.Count; i++)
                {
                    XmlSchemaObject o = attributes[i];
                    if (o is XmlSchemaAttribute)
                    {
                        XmlSchemaAttribute attribute = (XmlSchemaAttribute)o;
                        if (attribute.Use == XmlSchemaUse.Prohibited)
                            continue;
                        if (TryCheckIfAttribute(typeName, attribute, Globals.IdQualifiedName, ref foundId))
                            continue;
                        if (TryCheckIfAttribute(typeName, attribute, Globals.RefQualifiedName, ref foundRef))
                            continue;
                        if (attribute.RefName.IsEmpty || attribute.RefName.Namespace != Globals.SerializationNamespace || attribute.Use == XmlSchemaUse.Required)
                            ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.TypeShouldNotContainAttributes, Globals.SerializationNamespace));
                    }
                }
                isReference = (foundId && foundRef);
            }
        }

        private static bool TryCheckIfAttribute(XmlQualifiedName typeName, XmlSchemaAttribute attribute, XmlQualifiedName refName, ref bool foundAttribute)
        {
            if (attribute.RefName != refName)
                return false;
            if (foundAttribute)
                ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.CannotHaveDuplicateAttributeNames, refName.Name));
            foundAttribute = true;
            return true;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void AddDataContract(DataContract dataContract)
        {
            _dataContractSet.Add(dataContract.StableName, dataContract);
        }

        private static string? GetInnerText(XmlQualifiedName typeName, XmlElement? xmlElement)
        {
            if (xmlElement != null)
            {
                XmlNode? child = xmlElement.FirstChild;
                while (child != null)
                {
                    if (child.NodeType == XmlNodeType.Element)
                        ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, SR.Format(SR.InvalidAnnotationExpectingText, xmlElement.LocalName, xmlElement.NamespaceURI, child.LocalName, child.NamespaceURI));
                    child = child.NextSibling;
                }
                return xmlElement.InnerText;
            }
            return null;
        }

        private static XmlElement? ImportAnnotation(XmlSchemaAnnotation? annotation, XmlQualifiedName annotationQualifiedName)
        {
            if (annotation != null && annotation.Items != null && annotation.Items.Count > 0 && annotation.Items[0] is XmlSchemaAppInfo)
            {
                XmlSchemaAppInfo appInfo = (XmlSchemaAppInfo)annotation.Items[0];
                XmlNode?[]? markup = appInfo.Markup;
                if (markup != null)
                {
                    for (int i = 0; i < markup.Length; i++)
                    {
                        XmlElement? annotationElement = markup[i] as XmlElement;
                        if (annotationElement != null && annotationElement.LocalName == annotationQualifiedName.Name && annotationElement.NamespaceURI == annotationQualifiedName.Namespace)
                            return annotationElement;
                    }
                }
            }
            return null;
        }

        [DoesNotReturn]
        private static void ThrowTypeCannotBeImportedException(string name, string ns, string message)
        {
            ThrowTypeCannotBeImportedException(SR.Format(SR.TypeCannotBeImported, name, ns, message));
        }

        [DoesNotReturn]
        private static void ThrowArrayTypeCannotBeImportedException(string name, string ns, string message)
        {
            ThrowTypeCannotBeImportedException(SR.Format(SR.ArrayTypeCannotBeImported, name, ns, message));
        }

        [DoesNotReturn]
        private static void ThrowEnumTypeCannotBeImportedException(string name, string ns, string message)
        {
            ThrowTypeCannotBeImportedException(SR.Format(SR.EnumTypeCannotBeImported, name, ns, message));
        }

        [DoesNotReturn]
        private static void ThrowISerializableTypeCannotBeImportedException(string name, string ns, string message)
        {
            ThrowTypeCannotBeImportedException(SR.Format(SR.ISerializableTypeCannotBeImported, name, ns, message));
        }

        [DoesNotReturn]
        private static void ThrowTypeCannotBeImportedException(string message)
        {
            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.TypeCannotBeImportedHowToFix, message)));
        }
    }

}


#endif
