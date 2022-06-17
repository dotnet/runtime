// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization
{
    public class XsdDataContractExporter
    {
        private ExportOptions? _options;
        private XmlSchemaSet? _schemas;
        private DataContractSet? _dataContractSet;

        public XsdDataContractExporter()
        {
        }

        public XsdDataContractExporter(XmlSchemaSet? schemas)
        {
            this._schemas = schemas;
        }

        public ExportOptions? Options
        {
            get { return _options; }
            set { _options = value; }
        }

        public XmlSchemaSet Schemas
        {
            get
            {
                XmlSchemaSet schemaSet = GetSchemaSet();
#if smolloy_add_schema_import
                SchemaImporter.CompileSchemaSet(schemaSet);
#endif
                return schemaSet;
            }
        }

        private XmlSchemaSet GetSchemaSet()
        {
            if (_schemas == null)
            {
                _schemas = new XmlSchemaSet();
                _schemas.XmlResolver = null;
            }
            return _schemas;
        }

#if smolloy_add_schema_import
        private DataContractSet DataContractSet
        {
            get
            {
                if (_dataContractSet == null)
                {
#if smolloy_add_ext_surrogate
                    _dataContractSet = new DataContractSet(Options?.GetSurrogate(), null, null);
#else
                    _dataContractSet = new DataContractSet(null, null, null);
#endif
                }
                return _dataContractSet;
            }
        }
#else
        private static DataContractSet DataContractSet
        {
            get
            {
                // On .NET Framework , we set _dataContractSet = Options.GetSurrogate());
                // But Options.GetSurrogate() is not available on NetCore because IDataContractSurrogate
                // is not in NetStandard.
                throw new PlatformNotSupportedException("NOTE TODO smolloy - add this resource back if we don't check in schema import"/*SR.PlatformNotSupported_IDataContractSurrogate*/);
            }
        }
#endif

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void Export(ICollection<Assembly> assemblies)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
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
            catch
            {
                _dataContractSet = oldValue;
                throw;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void Export(ICollection<Type> types)
        {
            ArgumentNullException.ThrowIfNull(types);

            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
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
            catch
            {
                _dataContractSet = oldValue;
                throw;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void Export(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                AddType(type);
                Export();
            }
            catch
            {
                _dataContractSet = oldValue;
                throw;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public XmlQualifiedName GetSchemaTypeName(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            type = GetSurrogatedType(type);
            DataContract dataContract = DataContract.GetDataContract(type);
            DataContractSet.EnsureTypeNotGeneric(dataContract.UnderlyingType);
            XmlDataContract? xmlDataContract = dataContract as XmlDataContract;
            if (xmlDataContract != null && xmlDataContract.IsAnonymous)
                return XmlQualifiedName.Empty;
            return dataContract.StableName;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public XmlSchemaType? GetSchemaType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            type = GetSurrogatedType(type);
            DataContract dataContract = DataContract.GetDataContract(type);
            DataContractSet.EnsureTypeNotGeneric(dataContract.UnderlyingType);
            XmlDataContract? xmlDataContract = dataContract as XmlDataContract;
            if (xmlDataContract != null && xmlDataContract.IsAnonymous)
                return xmlDataContract.XsdType;
            return null;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public XmlQualifiedName? GetRootElementName(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            type = GetSurrogatedType(type);
            DataContract dataContract = DataContract.GetDataContract(type);
            DataContractSet.EnsureTypeNotGeneric(dataContract.UnderlyingType);
            if (dataContract.HasRoot)
            {
                return new XmlQualifiedName(dataContract.TopLevelElementName!.Value, dataContract.TopLevelElementNamespace!.Value);
            }
            else
            {
                return null;
            }
        }

#if smolloy_add_ext_surrogate
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private Type GetSurrogatedType(Type type)
        {
            ISerializationSurrogateProvider? dataContractSurrogate;
            if (Options != null && (dataContractSurrogate = Options.GetSurrogate()) != null)
                type = DataContractSurrogateCaller.GetDataContractType(dataContractSurrogate, type);
            return type;
        }
#else
        private static Type GetSurrogatedType(Type type)
        {
            return type;
        }
#endif

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
#if smolloy_add_ext_surrogate || smolloy_add_schema_import
        private void CheckAndAddType(Type type)
#else
        private static void CheckAndAddType(Type type)
#endif
        {
            type = GetSurrogatedType(type);
            if (!type.ContainsGenericParameters && DataContract.IsTypeSerializable(type))
                AddType(type);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
#if smolloy_add_schema_import
        private void AddType(Type type)
#else
        private static void AddType(Type type)
#endif
        {
            DataContractSet.Add(type);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void Export()
        {
            AddKnownTypes();
            SchemaExporter schemaExporter = new SchemaExporter(GetSchemaSet(), DataContractSet);
            schemaExporter.Export();
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public bool CanExport(ICollection<Assembly> assemblies)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
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
            catch
            {
                _dataContractSet = oldValue;
                throw;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public bool CanExport(ICollection<Type> types)
        {
            ArgumentNullException.ThrowIfNull(types);

            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
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
            catch
            {
                _dataContractSet = oldValue;
                throw;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public bool CanExport(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
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
            catch
            {
                _dataContractSet = oldValue;
                throw;
            }
        }
    }
}
