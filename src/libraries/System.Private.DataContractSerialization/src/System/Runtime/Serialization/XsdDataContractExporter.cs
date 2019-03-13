// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Diagnostics;

namespace System.Runtime.Serialization
{
    public class XsdDataContractExporter
    {
        private ExportOptions _options;
        private XmlSchemaSet _schemas;
        private DataContractSet _dataContractSet;

        public XsdDataContractExporter()
        {
        }

        public XsdDataContractExporter(XmlSchemaSet schemas)
        {
            this._schemas = schemas;
        }

        public ExportOptions Options
        {
            get { return _options; }
            set { _options = value; }
        }

        public XmlSchemaSet Schemas
        {
            get
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_SchemaImporter);
            }
        }

        XmlSchemaSet GetSchemaSet()
        {
            if (_schemas == null)
            {
                _schemas = new XmlSchemaSet();
                _schemas.XmlResolver = null;
            }
            return _schemas;
        }

        DataContractSet DataContractSet
        {
            get
            {
                // On full framework , we set _dataContractSet = Options.GetSurrogate());
                // But Options.GetSurrogate() is not available on NetCore because IDataContractSurrogate
                // is not in NetStandard.
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_IDataContractSurrogate);
            }
        }

        void TraceExportBegin()
        {
        }

        void TraceExportEnd()
        {
        }

        void TraceExportError(Exception exception)
        {
        }

        public void Export(ICollection<Assembly> assemblies)
        {
            if (assemblies == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(assemblies)));

            TraceExportBegin();

            DataContractSet oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly == null)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.CannotExportNullAssembly, nameof(assemblies))));

                    Type[] types = assembly.GetTypes();
                    for (int j = 0; j < types.Length; j++)
                        CheckAndAddType(types[j]);
                }

                Export();
            }
            catch (Exception ex)
            {
                if (DiagnosticUtility.IsFatal(ex))
                {
                    throw;
                }
                _dataContractSet = oldValue;
                TraceExportError(ex);
                throw;
            }
            TraceExportEnd();
        }

        public void Export(ICollection<Type> types)
        {
            if (types == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(types)));

            TraceExportBegin();

            DataContractSet oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                foreach (Type type in types)
                {
                    if (type == null)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.CannotExportNullType, nameof(types))));
                    AddType(type);
                }

                Export();
            }
            catch (Exception ex)
            {
                if (DiagnosticUtility.IsFatal(ex))
                {
                    throw;
                }
                _dataContractSet = oldValue;
                TraceExportError(ex);
                throw;
            }
            TraceExportEnd();
        }

        public void Export(Type type)
        {
            if (type == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(type)));

            TraceExportBegin();

            DataContractSet oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                AddType(type);
                Export();
            }
            catch (Exception ex)
            {
                if (DiagnosticUtility.IsFatal(ex))
                {
                    throw;
                }
                _dataContractSet = oldValue;
                TraceExportError(ex);
                throw;
            }
            TraceExportEnd();
        }

        public XmlQualifiedName GetSchemaTypeName(Type type)
        {
            if (type == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(type)));
            type = GetSurrogatedType(type);
            DataContract dataContract = DataContract.GetDataContract(type);
            DataContractSet.EnsureTypeNotGeneric(dataContract.UnderlyingType);
            XmlDataContract xmlDataContract = dataContract as XmlDataContract;
            if (xmlDataContract != null && xmlDataContract.IsAnonymous)
                return XmlQualifiedName.Empty;
            return dataContract.StableName;
        }

        public XmlSchemaType GetSchemaType(Type type)
        {
            if (type == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(type)));
            type = GetSurrogatedType(type);
            DataContract dataContract = DataContract.GetDataContract(type);
            DataContractSet.EnsureTypeNotGeneric(dataContract.UnderlyingType);
            XmlDataContract xmlDataContract = dataContract as XmlDataContract;
            if (xmlDataContract != null && xmlDataContract.IsAnonymous)
                return xmlDataContract.XsdType;
            return null;
        }

        public XmlQualifiedName GetRootElementName(Type type)
        {
            if (type == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(type)));
            type = GetSurrogatedType(type);
            DataContract dataContract = DataContract.GetDataContract(type);
            DataContractSet.EnsureTypeNotGeneric(dataContract.UnderlyingType);
            if (dataContract.HasRoot)
            {
                return new XmlQualifiedName(dataContract.TopLevelElementName.Value, dataContract.TopLevelElementNamespace.Value);
            }
            else
            {
                return null;
            }
        }

        private Type GetSurrogatedType(Type type)
        {
#if SUPPORT_SURROGATE
            IDataContractSurrogate dataContractSurrogate;
            if (options != null && (dataContractSurrogate = Options.GetSurrogate()) != null)
                type = DataContractSurrogateCaller.GetDataContractType(dataContractSurrogate, type);
#endif
            return type;
        }

        private void CheckAndAddType(Type type)
        {
            type = GetSurrogatedType(type);
            if (!type.ContainsGenericParameters && DataContract.IsTypeSerializable(type))
                AddType(type);
        }

        private void AddType(Type type)
        {
            DataContractSet.Add(type);
        }

        private void Export()
        {
            AddKnownTypes();
            SchemaExporter schemaExporter = new SchemaExporter(GetSchemaSet(), DataContractSet);
            schemaExporter.Export();
        }

        private void AddKnownTypes()
        {
            if (Options != null)
            {
                Collection<Type> knownTypes = Options.KnownTypes;

                if (knownTypes != null)
                {
                    for (int i = 0; i < knownTypes.Count; i++)
                    {
                        Type type = knownTypes[i];
                        if (type == null)
                            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.CannotExportNullKnownType));
                        AddType(type);
                    }
                }
            }
        }

        public bool CanExport(ICollection<Assembly> assemblies)
        {
            if (assemblies == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(assemblies)));

            DataContractSet oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly == null)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.CannotExportNullAssembly, nameof(assemblies))));

                    Type[] types = assembly.GetTypes();
                    for (int j = 0; j < types.Length; j++)
                        CheckAndAddType(types[j]);
                }
                AddKnownTypes();
                return true;
            }
            catch (InvalidDataContractException)
            {
                _dataContractSet = oldValue;
                return false;
            }
            catch (Exception ex)
            {
                if (DiagnosticUtility.IsFatal(ex))
                {
                    throw;
                }
                _dataContractSet = oldValue;
                TraceExportError(ex);
                throw;
            }
        }

        public bool CanExport(ICollection<Type> types)
        {
            if (types == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(types)));

            DataContractSet oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                foreach (Type type in types)
                {
                    if (type == null)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.CannotExportNullType, nameof(types))));
                    AddType(type);
                }
                AddKnownTypes();
                return true;
            }
            catch (InvalidDataContractException)
            {
                _dataContractSet = oldValue;
                return false;
            }
            catch (Exception ex)
            {
                if (DiagnosticUtility.IsFatal(ex))
                {
                    throw;
                }
                _dataContractSet = oldValue;
                TraceExportError(ex);
                throw;
            }
        }

        public bool CanExport(Type type)
        {
            if (type == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(type)));

            DataContractSet oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                AddType(type);
                AddKnownTypes();
                return true;
            }
            catch (InvalidDataContractException)
            {
                _dataContractSet = oldValue;
                return false;
            }
            catch (Exception ex)
            {
                if (DiagnosticUtility.IsFatal(ex))
                {
                    throw;
                }
                _dataContractSet = oldValue;
                TraceExportError(ex);
                throw;
            }
        }

#if USE_REFEMIT
        //Returns warnings
        public IList<string> GenerateCode(IList<Assembly> assemblies) 
        {
            if (assemblies == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(assemblies)));
            List<string> warnings = new List<string>();
 
            DataContractSet oldValue = (dataContractSet == null) ? null : new DataContractSet(dataContractSet);
            try
            {
                for (int i=0; i < assemblies.Count; i++)
                {
                    Assembly assembly = assemblies[i];
                    if (assembly == null)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.CannotExportNullAssembly, "assemblies")));
 
                    Type[] types = assembly.GetTypes();
                    for (int j=0; j < types.Length; j++)
                    {
                        try
                        {
                            CheckAndAddType(types[j]);
                        }
                        catch (Exception ex)
                        {
                            warnings.Add("Error on exporting Type " + DataContract.GetClrTypeFullName(types[j]) + ". " + ex.Message);
                        }
                        
                    }
                }
 
                foreach (KeyValuePair<XmlQualifiedName, DataContract> pair in dataContractSet)
                {
                    DataContract dataContract = pair.Value;
                    if (dataContract is ClassDataContract)
                    {
                        try
                        {
                            XmlFormatClassWriterDelegate writerMethod = ((ClassDataContract)dataContract).XmlFormatWriterDelegate;
                            XmlFormatClassReaderDelegate readerMethod = ((ClassDataContract)dataContract).XmlFormatReaderDelegate;
                        }
                        catch (Exception ex)
                        {
                            warnings.Add("Error on exporting Type " + dataContract.UnderlyingType + ". " + ex.Message);
                        }
                    }
                    else if (dataContract is CollectionDataContract)
                    {
                        try
                        {
                            XmlFormatCollectionWriterDelegate writerMethod = ((CollectionDataContract)dataContract).XmlFormatWriterDelegate;
                            XmlFormatCollectionReaderDelegate readerMethod = ((CollectionDataContract)dataContract).XmlFormatReaderDelegate;
                        }
                        catch (Exception ex)
                        {
                            warnings.Add("Error on exporting Type " + dataContract.UnderlyingType + ". " + ex.Message);
                        }
                    }
                }
                return warnings;
            }
            catch (Exception ex)
            {
                if (DiagnosticUtility.IsFatal(ex))
                {
                    throw;
                }
                dataContractSet = oldValue;
                TraceExportError(ex);
                throw;
            }
        }
#endif

    }

}
