// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Schema;

using ExceptionUtil = System.Runtime.Serialization.Schema.DiagnosticUtility.ExceptionUtility;

namespace System.Runtime.Serialization.Schema
{
    public class XsdDataContractImporter
    {
        private CodeCompileUnit _codeCompileUnit = null!;   // Not directly referenced. Always lazy initialized by property getter.
        private DataContractSet? _dataContractSet;

        private static readonly XmlQualifiedName[] s_emptyTypeNameArray = Array.Empty<XmlQualifiedName>();
        private static readonly XmlSchemaElement[] s_emptyElementArray = Array.Empty<XmlSchemaElement>();
        private XmlQualifiedName[] _singleTypeNameArray = null!;   // Not directly referenced. Always lazy initialized by property getter.
        private XmlSchemaElement[] _singleElementArray = null!;   // Not directly referenced. Always lazy initialized by property getter.

        public XsdDataContractImporter()
        {
        }

        public XsdDataContractImporter(CodeCompileUnit codeCompileUnit)
        {
            _codeCompileUnit = codeCompileUnit;
        }

        public ImportOptions? Options { get; set; }

        public CodeCompileUnit CodeCompileUnit => _codeCompileUnit ??= new CodeCompileUnit();

        private DataContractSet DataContractSet
        {
            get
            {
                if (_dataContractSet == null)
                {
                    _dataContractSet = Options == null ? new DataContractSet(null, null, null) :
                                                        new DataContractSet(Options.ExtendedSurrogateProvider, Options.ReferencedTypes, Options.ReferencedCollectionTypes);
                }
                return _dataContractSet;
            }
        }

        [RequiresUnreferencedCode("Placeholder Warning")]
        public void Import(XmlSchemaSet schemas)
        {
            if (schemas == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            InternalImport(schemas, null, s_emptyElementArray, s_emptyTypeNameArray);
        }

        [RequiresUnreferencedCode("Placeholder Warning")]
        public void Import(XmlSchemaSet schemas, ICollection<XmlQualifiedName> typeNames)
        {
            if (schemas == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (typeNames == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(typeNames)));

            InternalImport(schemas, typeNames, s_emptyElementArray, s_emptyTypeNameArray);
        }

        [RequiresUnreferencedCode("Placeholder Warning")]
        public void Import(XmlSchemaSet schemas, XmlQualifiedName typeName)
        {
            if (schemas == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (typeName == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(typeName)));

            SingleTypeNameArray[0] = typeName;
            InternalImport(schemas, SingleTypeNameArray, s_emptyElementArray, s_emptyTypeNameArray);
        }

        [RequiresUnreferencedCode("Placeholder Warning")]
        public XmlQualifiedName? Import(XmlSchemaSet schemas, XmlSchemaElement element)
        {
            if (schemas == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (element == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(element)));

            SingleElementArray[0] = element;
            InternalImport(schemas, s_emptyTypeNameArray, SingleElementArray, SingleTypeNameArray /*filled on return*/);
            return SingleTypeNameArray[0];
        }

        [RequiresUnreferencedCode("Placeholder Warning")]
        public bool CanImport(XmlSchemaSet schemas)
        {
            if (schemas == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            return InternalCanImport(schemas, null, s_emptyElementArray, s_emptyTypeNameArray);
        }

        [RequiresUnreferencedCode("Placeholder Warning")]
        public bool CanImport(XmlSchemaSet schemas, ICollection<XmlQualifiedName> typeNames)
        {
            if (schemas == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (typeNames == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(typeNames)));

            return InternalCanImport(schemas, typeNames, s_emptyElementArray, s_emptyTypeNameArray);
        }

        [RequiresUnreferencedCode("Placeholder Warning")]
        public bool CanImport(XmlSchemaSet schemas, XmlQualifiedName typeName)
        {
            if (schemas == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (typeName == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(typeName)));

            return InternalCanImport(schemas, new XmlQualifiedName[] { typeName }, s_emptyElementArray, s_emptyTypeNameArray);
        }

        [RequiresUnreferencedCode("Placeholder Warning")]
        public bool CanImport(XmlSchemaSet schemas, XmlSchemaElement element)
        {
            if (schemas == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (element == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(element)));

            SingleElementArray[0] = element;
            return InternalCanImport(schemas, s_emptyTypeNameArray, SingleElementArray, SingleTypeNameArray);
        }

        [RequiresUnreferencedCode("Placeholder Warning")]
        public CodeTypeReference GetCodeTypeReference(XmlQualifiedName typeName)
        {
            DataContract dataContract = FindDataContract(typeName);
            CodeExporter codeExporter = new CodeExporter(DataContractSet, Options, CodeCompileUnit);
            return codeExporter.GetCodeTypeReference(dataContract);
        }

        [RequiresUnreferencedCode("Placeholder Warning")]
        public CodeTypeReference GetCodeTypeReference(XmlQualifiedName typeName, XmlSchemaElement element)
        {
            if (element == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(element)));
            if (typeName == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(typeName)));
            DataContract dataContract = FindDataContract(typeName);
            CodeExporter codeExporter = new CodeExporter(DataContractSet, Options, CodeCompileUnit);
            return codeExporter.GetElementTypeReference(dataContract, element.IsNillable);
        }

        [RequiresUnreferencedCode("Placeholder Warning")]
        internal DataContract FindDataContract(XmlQualifiedName typeName)
        {
            if (typeName == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(typeName)));

            DataContract? dataContract = DataContract.GetBuiltInDataContract(typeName.Name, typeName.Namespace);
            if (dataContract == null)
            {
                dataContract = DataContractSet.GetDataContract(typeName);
                if (dataContract == null)
                    throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(SR.Format(SR.TypeHasNotBeenImported, typeName.Name, typeName.Namespace)));
            }
            return dataContract;
        }

        [RequiresUnreferencedCode("Placeholder Warning")]
        public ICollection<CodeTypeReference>? GetKnownTypeReferences(XmlQualifiedName typeName)
        {
            if (typeName == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(typeName)));

            DataContract? dataContract = DataContract.GetBuiltInDataContract(typeName.Name, typeName.Namespace);
            if (dataContract == null)
            {
                dataContract = DataContractSet.GetDataContract(typeName);
                if (dataContract == null)
                    throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(SR.Format(SR.TypeHasNotBeenImported, typeName.Name, typeName.Namespace)));
            }

            CodeExporter codeExporter = new CodeExporter(DataContractSet, Options, CodeCompileUnit);
            return codeExporter.GetKnownTypeReferences(dataContract);
        }

        private XmlQualifiedName[] SingleTypeNameArray
        {
            get
            {
                if (_singleTypeNameArray == null)
                    _singleTypeNameArray = new XmlQualifiedName[1];
                return _singleTypeNameArray;
            }
        }

        private XmlSchemaElement[] SingleElementArray
        {
            get
            {
                if (_singleElementArray == null)
                    _singleElementArray = new XmlSchemaElement[1];
                return _singleElementArray;
            }
        }

        [RequiresUnreferencedCode("Placeholder Warning")]
        private void InternalImport(XmlSchemaSet schemas, ICollection<XmlQualifiedName>? typeNames, ICollection<XmlSchemaElement> elements, XmlQualifiedName[] elementTypeNames/*filled on return*/)
        {
            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                DataContractSet.ImportSchemaSet(schemas, typeNames, elements, elementTypeNames/*filled on return*/, ImportXmlDataType);
                // TODO smolloy - The above used to be done as below... but to keep SchemaImporter internal it is now done as above.
                //SchemaImporter schemaImporter = new SchemaImporter(schemas, typeNames, elements, elementTypeNames/*filled on return*/, DataContractSet, ImportXmlDataType);
                //schemaImporter.Import();

                CodeExporter codeExporter = new CodeExporter(DataContractSet, Options, CodeCompileUnit);
                codeExporter.Export();
            }
            catch
            {
                _dataContractSet = oldValue;
                throw;
            }
        }

        private bool ImportXmlDataType
        {
            get
            {
                return Options == null ? false : Options.ImportXmlType;
            }
        }

        [RequiresUnreferencedCode("Placeholder Warning")]
        private bool InternalCanImport(XmlSchemaSet schemas, ICollection<XmlQualifiedName>? typeNames, ICollection<XmlSchemaElement> elements, XmlQualifiedName[] elementTypeNames)
        {
            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                DataContractSet.ImportSchemaSet(schemas, typeNames, elements, elementTypeNames/*filled on return*/, ImportXmlDataType);
                // TODO smolloy - The above used to be done as below... but to keep SchemaImporter internal it is now done as above.
                //SchemaImporter schemaImporter = new SchemaImporter(schemas, typeNames, elements, elementTypeNames, DataContractSet, ImportXmlDataType);
                //schemaImporter.Import();
                return true;
            }
            catch (ArgumentException)
            {
                _dataContractSet = oldValue;
                return false;
            }
            catch
            {
                _dataContractSet = oldValue;
                throw;
            }
        }

        private static XmlQualifiedName? s_actualTypeAnnotationName;
        internal static XmlQualifiedName ActualTypeAnnotationName => s_actualTypeAnnotationName ??= new XmlQualifiedName(Globals.ActualTypeLocalName, Globals.SerializationNamespace);

        internal static XmlQualifiedName ImportActualType(XmlSchemaAnnotation? annotation, XmlQualifiedName defaultTypeName, XmlQualifiedName typeName)
        {
            XmlElement? actualTypeElement = ImportAnnotation(annotation, ActualTypeAnnotationName);
            if (actualTypeElement == null)
                return defaultTypeName;

            XmlNode? nameAttribute = actualTypeElement.Attributes.GetNamedItem(Globals.ActualTypeNameAttribute);
            if (nameAttribute?.Value == null)
                throw ExceptionUtil.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.AnnotationAttributeNotFound, ActualTypeAnnotationName.Name, typeName.Name, typeName.Namespace, Globals.ActualTypeNameAttribute)));
            XmlNode? nsAttribute = actualTypeElement.Attributes.GetNamedItem(Globals.ActualTypeNamespaceAttribute);
            if (nsAttribute?.Value == null)
                throw ExceptionUtil.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.AnnotationAttributeNotFound, ActualTypeAnnotationName.Name, typeName.Name, typeName.Namespace, Globals.ActualTypeNamespaceAttribute)));
            return new XmlQualifiedName(nameAttribute.Value, nsAttribute.Value);
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
    }
}
