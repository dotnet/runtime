// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if smolloy_add_schema_import

namespace System.Runtime.Serialization
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Schema;
    using System.Diagnostics.CodeAnalysis;

    public class XsdDataContractImporter
    {
        private ImportOptions? _options;
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
            this._codeCompileUnit = codeCompileUnit;
        }

        public ImportOptions? Options
        {
            get { return _options; }
            set { _options = value; }
        }


        public CodeCompileUnit CodeCompileUnit
        {
            get
            {
                return GetCodeCompileUnit();
            }
        }

        private CodeCompileUnit GetCodeCompileUnit()
        {
            if (_codeCompileUnit == null)
                _codeCompileUnit = new CodeCompileUnit();
            return _codeCompileUnit;
        }


        private DataContractSet DataContractSet
        {
            get
            {
                if (_dataContractSet == null)
                {
                    _dataContractSet = Options == null ? new DataContractSet(null, null, null) :
                                                        new DataContractSet(Options.SerializationExtendedSurrogateProvider, Options.ReferencedTypes, Options.ReferencedCollectionTypes);
                }
                return _dataContractSet;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void Import(XmlSchemaSet schemas)
        {
            if (schemas == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            InternalImport(schemas, null, s_emptyElementArray, s_emptyTypeNameArray);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void Import(XmlSchemaSet schemas, ICollection<XmlQualifiedName> typeNames)
        {
            if (schemas == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (typeNames == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(typeNames)));

            InternalImport(schemas, typeNames, s_emptyElementArray, s_emptyTypeNameArray);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void Import(XmlSchemaSet schemas, XmlQualifiedName typeName)
        {
            if (schemas == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (typeName == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(typeName)));

            SingleTypeNameArray[0] = typeName;
            InternalImport(schemas, SingleTypeNameArray, s_emptyElementArray, s_emptyTypeNameArray);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public XmlQualifiedName? Import(XmlSchemaSet schemas, XmlSchemaElement element)
        {
            if (schemas == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (element == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(element)));

            SingleElementArray[0] = element;
            InternalImport(schemas, s_emptyTypeNameArray, SingleElementArray, SingleTypeNameArray /*filled on return*/);
            return SingleTypeNameArray[0];
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public bool CanImport(XmlSchemaSet schemas)
        {
            if (schemas == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            return InternalCanImport(schemas, null, s_emptyElementArray, s_emptyTypeNameArray);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public bool CanImport(XmlSchemaSet schemas, ICollection<XmlQualifiedName> typeNames)
        {
            if (schemas == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (typeNames == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(typeNames)));

            return InternalCanImport(schemas, typeNames, s_emptyElementArray, s_emptyTypeNameArray);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public bool CanImport(XmlSchemaSet schemas, XmlQualifiedName typeName)
        {
            if (schemas == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (typeName == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(typeName)));

            return InternalCanImport(schemas, new XmlQualifiedName[] { typeName }, s_emptyElementArray, s_emptyTypeNameArray);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public bool CanImport(XmlSchemaSet schemas, XmlSchemaElement element)
        {
            if (schemas == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(schemas)));

            if (element == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(element)));

            SingleElementArray[0] = element;
            return InternalCanImport(schemas, s_emptyTypeNameArray, SingleElementArray, SingleTypeNameArray);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public CodeTypeReference GetCodeTypeReference(XmlQualifiedName typeName)
        {
            DataContract dataContract = FindDataContract(typeName);
            CodeExporter codeExporter = new CodeExporter(DataContractSet, Options, GetCodeCompileUnit());
            return codeExporter.GetCodeTypeReference(dataContract);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public CodeTypeReference GetCodeTypeReference(XmlQualifiedName typeName, XmlSchemaElement element)
        {
            if (element == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(element)));
            if (typeName == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(typeName)));
            DataContract dataContract = FindDataContract(typeName);
            CodeExporter codeExporter = new CodeExporter(DataContractSet, Options, GetCodeCompileUnit());
            return codeExporter.GetElementTypeReference(dataContract, element.IsNillable);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal DataContract FindDataContract(XmlQualifiedName typeName)
        {
            if (typeName == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(typeName)));

            DataContract? dataContract = DataContract.GetBuiltInDataContract(typeName.Name, typeName.Namespace);
            if (dataContract == null)
            {
                dataContract = DataContractSet[typeName];
                if (dataContract == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.TypeHasNotBeenImported, typeName.Name, typeName.Namespace)));
            }
            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public ICollection<CodeTypeReference>? GetKnownTypeReferences(XmlQualifiedName typeName)
        {
            if (typeName == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(typeName)));

            DataContract? dataContract = DataContract.GetBuiltInDataContract(typeName.Name, typeName.Namespace);
            if (dataContract == null)
            {
                dataContract = DataContractSet[typeName];
                if (dataContract == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.TypeHasNotBeenImported, typeName.Name, typeName.Namespace)));
            }

            CodeExporter codeExporter = new CodeExporter(DataContractSet, Options, GetCodeCompileUnit());
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void InternalImport(XmlSchemaSet schemas, ICollection<XmlQualifiedName>? typeNames, ICollection<XmlSchemaElement> elements, XmlQualifiedName[] elementTypeNames/*filled on return*/)
        {
            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                SchemaImporter schemaImporter = new SchemaImporter(schemas, typeNames, elements, elementTypeNames/*filled on return*/, DataContractSet, ImportXmlDataType);
                schemaImporter.Import();

                CodeExporter codeExporter = new CodeExporter(DataContractSet, Options, GetCodeCompileUnit());
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
                return this.Options == null ? false : this.Options.ImportXmlType;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private bool InternalCanImport(XmlSchemaSet schemas, ICollection<XmlQualifiedName>? typeNames, ICollection<XmlSchemaElement> elements, XmlQualifiedName[] elementTypeNames)
        {
            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                SchemaImporter schemaImporter = new SchemaImporter(schemas, typeNames, elements, elementTypeNames, DataContractSet, ImportXmlDataType);
                schemaImporter.Import();
                return true;
            }
            catch (InvalidDataContractException)
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
    }
}

#endif
