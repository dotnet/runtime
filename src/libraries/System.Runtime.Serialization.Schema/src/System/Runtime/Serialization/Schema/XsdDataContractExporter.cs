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
                // TODO smolloy - what to do. Looks like this particular method can live easily anywhere we want. But...
                // the home of SchemaImporter is yet to be determined. If it lives externally here, then use it like this.
                // If it lives internally... which is probably preferred if possible... then we will need to duplicate this
                // method out here, or find a way to call into it over there.
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
                    // TODO smolloy - This used to pass the surrogate to DCSet in 4.8. We can't do that here. We need to pair
                    // the two somehow. For now, just pass nulls and see where down the line we need to resurface the surrogate provider
                    // so we can best figure how to pair these.
                    // _dataContractSet = new DataContractSet(Options?.GetSurrogate(), null, null);
                    _dataContractSet = new DataContractSet(null, null, null);
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
            XmlDataContract? xmlDataContract = dataContract as XmlDataContract;
            if (xmlDataContract != null && xmlDataContract.IsAnonymous)
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
            XmlDataContract? xmlDataContract = dataContract as XmlDataContract;
            if (xmlDataContract != null && xmlDataContract.IsAnonymous)
                return dataContract.XsdType;
            return null;
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        public XmlQualifiedName? GetRootElementName(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            type = GetSurrogatedType(type);
            DataContract dataContract = DataContract.GetDataContract(type);
            EnsureTypeNotGeneric(dataContract.UnderlyingType);
            //if (dataContract.HasRoot)
            //{
            //    return new XmlQualifiedName(dataContract.TopLevelElementName!.Value, dataContract.TopLevelElementNamespace!.Value);
            //}
            // TODO smolloy - the above is now done with the new API below.
            if (dataContract.GetRootElementName(out XmlQualifiedName? root))
            {
                return root;
            }
            else
            {
                return null;
            }
        }

        [RequiresUnreferencedCode(Globals.SerializerTrimmerWarning)]
        private Type GetSurrogatedType(Type type)
        {
            // TODO smolloy - This is the DCS surrogate, not the extended one. Hmmmmm... guess inheritence is still the way to go.
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
            // TODO smolloy - done with the new API above. Keeps SchemaExporter internal.
            //SchemaExporter schemaExporter = new SchemaExporter(GetSchemaSet(), DataContractSet);
            //schemaExporter.Export();
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
