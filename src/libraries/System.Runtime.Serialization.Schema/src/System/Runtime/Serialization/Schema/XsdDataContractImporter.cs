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
    /// <summary>
    /// Allows the transformation of a set of XML schema files (.xsd) into common language runtime (CLR) types.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="XsdDataContractImporter"/> if you are creating a Web service that must interoperate with an existing
    /// Web service, or to create data contract types from XML schemas. <see cref="XsdDataContractImporter"/> will transform a
    /// set of XML schemas and create the .NET Framework types that represent the data contract in a selected programming language.
    /// To create the code, use the classes in the <see cref="System.CodeDom"/> namespace.
    ///
    /// Conversely, use the <see cref="XsdDataContractExporter"/> class when you have created a Web service that incorporates
    /// data represented by CLR types and when you need to export XML schemas for each data type to be consumed by other Web
    /// services.That is, <see cref="XsdDataContractExporter"/> transforms a set of CLR types into a set of XML schemas.
    /// </remarks>
    public sealed class XsdDataContractImporter
    {
        private CodeCompileUnit _codeCompileUnit = null!;   // Not directly referenced. Always lazy initialized by property getter.
        private DataContractSet? _dataContractSet;

        private static readonly XmlQualifiedName[] s_emptyTypeNameArray = Array.Empty<XmlQualifiedName>();
        private static readonly XmlSchemaElement[] s_emptyElementArray = Array.Empty<XmlSchemaElement>();
        private XmlQualifiedName[] _singleTypeNameArray = null!;   // Not directly referenced. Always lazy initialized by property getter.
        private XmlSchemaElement[] _singleElementArray = null!;   // Not directly referenced. Always lazy initialized by property getter.

        /// <summary>
        /// Initializes a new instance of the <see cref="XsdDataContractImporter"/> class.
        /// </summary>
        public XsdDataContractImporter()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XsdDataContractImporter"/> class with the <see cref="System.CodeDom.CodeCompileUnit"/> that will be used to generate CLR code.
        /// </summary>
        /// <param name="codeCompileUnit">The <see cref="System.CodeDom.CodeCompileUnit"/> that will be used to store the code.</param>
        public XsdDataContractImporter(CodeCompileUnit codeCompileUnit)
        {
            _codeCompileUnit = codeCompileUnit;
        }

        /// <summary>
        /// Gets or sets an <see cref="ImportOptions"/> that contains settable options for the import operation.
        /// </summary>
        public ImportOptions? Options { get; set; }

        /// <summary>
        /// Gets a <see cref="System.CodeDom.CodeCompileUnit"/> used for storing the CLR types generated.
        /// </summary>
        public CodeCompileUnit CodeCompileUnit => _codeCompileUnit ??= new CodeCompileUnit();

        private DataContractSet DataContractSet
        {
            get
            {
                if (_dataContractSet == null)
                {
                    _dataContractSet = Options == null ? new DataContractSet(null, null, null) :
                                                        new DataContractSet(Options.SurrogateProvider, Options.ReferencedTypes, Options.ReferencedCollectionTypes);
                }
                return _dataContractSet;
            }
        }

        /// <summary>
        /// Transforms the specified set of XML schemas contained in an <see cref="XmlSchemaSet"/> into a <see cref="System.CodeDom.CodeCompileUnit"/>.
        /// </summary>
        /// <param name="schemas">A <see cref="XmlSchemaSet"/> that contains the schema representations to generate CLR types for.</param>
        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public void Import(XmlSchemaSet schemas)
        {
            if (schemas == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            InternalImport(schemas, null, s_emptyElementArray, s_emptyTypeNameArray);
        }

        /// <summary>
        /// Transforms the specified set of schema types contained in an <see cref="XmlSchemaSet"/> into CLR types generated into a <see cref="System.CodeDom.CodeCompileUnit"/>.
        /// </summary>
        /// <param name="schemas">A <see cref="XmlSchemaSet"/> that contains the schema representations.</param>
        /// <param name="typeNames">A <see cref="ICollection{T}"/> (of <see cref="XmlQualifiedName"/>) that represents the set of schema types to import.</param>
        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public void Import(XmlSchemaSet schemas, ICollection<XmlQualifiedName> typeNames)
        {
            if (schemas == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (typeNames == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(typeNames)));

            InternalImport(schemas, typeNames, s_emptyElementArray, s_emptyTypeNameArray);
        }

        /// <summary>
        /// Transforms the specified XML schema type contained in an <see cref="XmlSchemaSet"/> into a <see cref="System.CodeDom.CodeCompileUnit"/>.
        /// </summary>
        /// <param name="schemas">A <see cref="XmlSchemaSet"/> that contains the schema representations.</param>
        /// <param name="typeName">A <see cref="XmlQualifiedName"/> that represents a specific schema type to import.</param>
        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public void Import(XmlSchemaSet schemas, XmlQualifiedName typeName)
        {
            if (schemas == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (typeName == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(typeName)));

            SingleTypeNameArray[0] = typeName;
            InternalImport(schemas, SingleTypeNameArray, s_emptyElementArray, s_emptyTypeNameArray);
        }

        /// <summary>
        /// Transforms the specified schema element in the set of specified XML schemas into a <see cref="System.CodeDom.CodeCompileUnit"/> and
        /// returns an <see cref="XmlQualifiedName"/> that represents the data contract name for the specified element.
        /// </summary>
        /// <param name="schemas">An <see cref="XmlSchemaSet"/> that contains the schemas to transform.</param>
        /// <param name="element">An <see cref="XmlSchemaElement"/> that represents the specific schema element to transform.</param>
        /// <returns>An <see cref="XmlQualifiedName"/> that represents the specified element.</returns>
        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
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

        /// <summary>
        /// Gets a value that indicates whether the schemas contained in an <see cref="XmlSchemaSet"/> can be transformed into a <see cref="System.CodeDom.CodeCompileUnit"/>.
        /// </summary>
        /// <param name="schemas">A <see cref="XmlSchemaSet"/> that contains the schemas to transform.</param>
        /// <returns>true if the schemas can be transformed to data contract types; otherwise, false.</returns>
        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public bool CanImport(XmlSchemaSet schemas)
        {
            if (schemas == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            return InternalCanImport(schemas, null, s_emptyElementArray, s_emptyTypeNameArray);
        }

        /// <summary>
        /// Gets a value that indicates whether the specified set of types contained in an <see cref="XmlSchemaSet"/> can be transformed into CLR types generated into a <see cref="System.CodeDom.CodeCompileUnit"/>.
        /// </summary>
        /// <param name="schemas">A <see cref="XmlSchemaSet"/> that contains the schemas to transform.</param>
        /// <param name="typeNames">An <see cref="ICollection{T}"/> of <see cref="XmlQualifiedName"/> that represents the set of schema types to import.</param>
        /// <returns>true if the schemas can be transformed; otherwise, false.</returns>
        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public bool CanImport(XmlSchemaSet schemas, ICollection<XmlQualifiedName> typeNames)
        {
            if (schemas == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (typeNames == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(typeNames)));

            return InternalCanImport(schemas, typeNames, s_emptyElementArray, s_emptyTypeNameArray);
        }

        /// <summary>
        /// Gets a value that indicates whether the schemas contained in an <see cref="XmlSchemaSet"/> can be transformed into a <see cref="System.CodeDom.CodeCompileUnit"/>.
        /// </summary>
        /// <param name="schemas">A <see cref="XmlSchemaSet"/> that contains the schema representations.</param>
        /// <param name="typeName">An <see cref="XmlQualifiedName"/> that specifies the names of the schema types that need to be imported from the <see cref="XmlSchemaSet"/>.</param>
        /// <returns>true if the schemas can be transformed to data contract types; otherwise, false.</returns>
        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public bool CanImport(XmlSchemaSet schemas, XmlQualifiedName typeName)
        {
            if (schemas == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (typeName == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(typeName)));

            return InternalCanImport(schemas, new XmlQualifiedName[] { typeName }, s_emptyElementArray, s_emptyTypeNameArray);
        }

        /// <summary>
        /// Gets a value that indicates whether a specific schema element contained in an <see cref="XmlSchemaSet"/> can be imported.
        /// </summary>
        /// <param name="schemas">An <see cref="XmlSchemaSet"/> to import.</param>
        /// <param name="element">A specific <see cref="XmlSchemaElement"/> to check in the set of schemas.</param>
        /// <returns>true if the element can be imported; otherwise, false.</returns>
        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public bool CanImport(XmlSchemaSet schemas, XmlSchemaElement element)
        {
            if (schemas == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (element == null)
                throw ExceptionUtil.ThrowHelperError(new ArgumentNullException(nameof(element)));

            SingleElementArray[0] = element;
            return InternalCanImport(schemas, s_emptyTypeNameArray, SingleElementArray, SingleTypeNameArray);
        }

        /// <summary>
        /// Returns a <see cref="CodeTypeReference"/> to the CLR type generated for the schema type with the specified <see cref="XmlQualifiedName"/>.
        /// </summary>
        /// <param name="typeName">The <see cref="XmlQualifiedName"/> that specifies the schema type to look up.</param>
        /// <returns>A <see cref="CodeTypeReference"/> reference to the CLR type generated for the schema type with the typeName specified.</returns>
        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public CodeTypeReference GetCodeTypeReference(XmlQualifiedName typeName)
        {
            DataContract dataContract = FindDataContract(typeName);
            CodeExporter codeExporter = new CodeExporter(DataContractSet, Options, CodeCompileUnit);
            return codeExporter.GetCodeTypeReference(dataContract);
        }

        /// <summary>
        /// Returns a <see cref="CodeTypeReference"/> for the specified XML qualified element and schema element.
        /// </summary>
        /// <param name="typeName">An <see cref="XmlQualifiedName"/> that specifies the XML qualified name of the schema type to look up.</param>
        /// <param name="element">An <see cref="XmlSchemaElement"/> that specifies an element in an XML schema.</param>
        /// <returns>A <see cref="CodeTypeReference"/> that represents the type that was generated for the specified schema type.</returns>
        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
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

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
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

        /// <summary>
        /// Returns a list of <see cref="CodeTypeReference"/> objects that represents the known types generated when generating code for the specified schema type.
        /// </summary>
        /// <param name="typeName">An <see cref="XmlQualifiedName"/> that represents the schema type to look up known types for.</param>
        /// <returns>A collection of type <see cref="CodeTypeReference"/>.</returns>
        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
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

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private void InternalImport(XmlSchemaSet schemas, ICollection<XmlQualifiedName>? typeNames, ICollection<XmlSchemaElement> elements, XmlQualifiedName[] elementTypeNames/*filled on return*/)
        {
            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                DataContractSet.ImportSchemaSet(schemas, typeNames, elements, elementTypeNames/*filled on return*/, ImportXmlDataType);

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

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private bool InternalCanImport(XmlSchemaSet schemas, ICollection<XmlQualifiedName>? typeNames, ICollection<XmlSchemaElement> elements, XmlQualifiedName[] elementTypeNames)
        {
            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                DataContractSet.ImportSchemaSet(schemas, typeNames, elements, elementTypeNames/*filled on return*/, ImportXmlDataType);
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
