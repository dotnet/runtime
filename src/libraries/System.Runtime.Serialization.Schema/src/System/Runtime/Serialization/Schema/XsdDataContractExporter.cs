// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using ExceptionUtil = System.Runtime.Serialization.Schema.DiagnosticUtility.ExceptionUtility;

namespace System.Runtime.Serialization.Schema
{
    public sealed class XsdDataContractExporter
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
                DataContractSet.CompileSchemaSet(schemaSet);
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

        private DataContractSet DataContractSet
        {
            get
            {
                if (_dataContractSet == null)
                {
                    _dataContractSet = new DataContractSet(Options?.SurrogateProvider, null, null);
                }
                return _dataContractSet;
            }
        }

        private static void EnsureTypeNotGeneric(Type type)
        {
            if (type.ContainsGenericParameters)
                throw ExceptionUtil.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.GenericTypeNotExportable, type)));
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public void Export(ICollection<Assembly> assemblies)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly == null)
                        throw ExceptionUtil.ThrowHelperError(new ArgumentException(SR.Format(SR.CannotExportNullAssembly, nameof(assemblies))));

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

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public void Export(ICollection<Type> types)
        {
            ArgumentNullException.ThrowIfNull(types);

            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                foreach (Type type in types)
                {
                    if (type == null)
                        throw ExceptionUtil.ThrowHelperError(new ArgumentException(SR.Format(SR.CannotExportNullType, nameof(types))));
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

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
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

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public XmlQualifiedName GetSchemaTypeName(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            type = GetSurrogatedType(type);
            DataContract dataContract = DataContract.GetDataContract(type);
            EnsureTypeNotGeneric(dataContract.UnderlyingType);
            if (dataContract is XmlDataContract xmlDataContract && xmlDataContract.IsAnonymous)
                return XmlQualifiedName.Empty;
            return dataContract.StableName;
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public XmlSchemaType? GetSchemaType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            type = GetSurrogatedType(type);
            DataContract dataContract = DataContract.GetDataContract(type);
            EnsureTypeNotGeneric(dataContract.UnderlyingType);
            if (dataContract is XmlDataContract xmlDataContract && xmlDataContract.IsAnonymous)
                return xmlDataContract.XsdType;
            return null;
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public XmlQualifiedName? GetRootElementName(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            type = GetSurrogatedType(type);
            DataContract dataContract = DataContract.GetDataContract(type);
            EnsureTypeNotGeneric(dataContract.UnderlyingType);
            if (dataContract.HasRoot)
            {
                return new XmlQualifiedName(dataContract.TopLevelElementName!.Value, dataContract.TopLevelElementNamespace!.Value);
            }
            else
            {
                return null;
            }
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private Type GetSurrogatedType(Type type)
        {
            ISerializationSurrogateProvider? surrogate = Options?.SurrogateProvider;
            if (surrogate != null)
                type = DataContract.GetSurrogateType(surrogate, type);
            return type;
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private void CheckAndAddType(Type type)
        {
            type = GetSurrogatedType(type);
            if (!type.ContainsGenericParameters && DataContract.IsTypeSerializable(type))
                AddType(type);
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private void AddType(Type type)
        {
            DataContractSet.Add(type);
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private void Export()
        {
            AddKnownTypes();
            DataContractSet.ExportSchemaSet(GetSchemaSet());
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
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
                            throw ExceptionUtil.ThrowHelperError(new ArgumentException(SR.CannotExportNullKnownType));
                        AddType(type);
                    }
                }
            }
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public bool CanExport(ICollection<Assembly> assemblies)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly == null)
                        throw ExceptionUtil.ThrowHelperError(new ArgumentException(SR.Format(SR.CannotExportNullAssembly, nameof(assemblies))));

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

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public bool CanExport(ICollection<Type> types)
        {
            ArgumentNullException.ThrowIfNull(types);

            DataContractSet? oldValue = (_dataContractSet == null) ? null : new DataContractSet(_dataContractSet);
            try
            {
                foreach (Type type in types)
                {
                    if (type == null)
                        throw ExceptionUtil.ThrowHelperError(new ArgumentException(SR.Format(SR.CannotExportNullType, nameof(types))));
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

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
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
