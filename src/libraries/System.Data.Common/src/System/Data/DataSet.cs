// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data
{
    /// <summary>
    /// Represents an in-memory cache of data.
    /// </summary>
    [DefaultProperty(nameof(DataSetName))]
    [Serializable]
    [XmlSchemaProvider(nameof(GetDataSetSchema))]
    [XmlRoot(nameof(DataSet))]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class DataSet : MarshalByValueComponent, IListSource, IXmlSerializable, ISupportInitializeNotification, ISerializable
    {
        private const string KEY_XMLSCHEMA = "XmlSchema";
        private const string KEY_XMLDIFFGRAM = "XmlDiffGram";

        private DataViewManager _defaultViewManager;

        // Public Collections
        private readonly DataTableCollection _tableCollection;
        private readonly DataRelationCollection _relationCollection;
        internal PropertyCollection _extendedProperties = null;
        private string _dataSetName = "NewDataSet";
        private string _datasetPrefix = string.Empty;
        internal string _namespaceURI = string.Empty;
        private bool _enforceConstraints = true;

        // globalization stuff
        private bool _caseSensitive;
        private CultureInfo _culture;
        private bool _cultureUserSet;

        // Internal definitions
        internal bool _fInReadXml = false;
        internal bool _fInLoadDiffgram = false;
        internal bool _fTopLevelTable = false;
        internal bool _fInitInProgress = false;
        internal bool _fEnableCascading = true;
        internal bool _fIsSchemaLoading = false;
        private bool _fBoundToDocument;        // for XmlDataDocument

        internal string _mainTableName = string.Empty;

        //default remoting format is XML
        private SerializationFormat _remotingFormat = SerializationFormat.Xml;

        private object _defaultViewManagerLock = new object();

        private static int s_objectTypeCount; // Bid counter
        private readonly int _objectID = Interlocked.Increment(ref s_objectTypeCount);
        private static XmlSchemaComplexType s_schemaTypeForWSDL = null;

        internal bool _useDataSetSchemaOnly; // UseDataSetSchemaOnly  , for YUKON
        internal bool _udtIsWrapped; // if UDT is wrapped , for YUKON

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DataSet'/> class.
        /// </summary>
        public DataSet()
        {
            GC.SuppressFinalize(this);
            DataCommonEventSource.Log.Trace("<ds.DataSet.DataSet|API> {0}", ObjectID); // others will call this constr

            // Set default locale
            _tableCollection = new DataTableCollection(this);
            _relationCollection = new DataRelationCollection.DataSetRelationCollection(this);
            _culture = CultureInfo.CurrentCulture; // Set default locale
        }

        /// <summary>
        /// Initializes a new instance of a <see cref='System.Data.DataSet'/>
        /// class with the given name.
        /// </summary>
        public DataSet(string dataSetName) : this()
        {
            DataSetName = dataSetName;
        }

        [DefaultValue(SerializationFormat.Xml)]
        public SerializationFormat RemotingFormat
        {
            get { return _remotingFormat; }
            set
            {
                if (value != SerializationFormat.Binary && value != SerializationFormat.Xml)
                {
                    throw ExceptionBuilder.InvalidRemotingFormat(value);
                }
                _remotingFormat = value;
                // this property is inherited to DataTable from DataSet.So we set this value to DataTable also
                for (int i = 0; i < Tables.Count; i++)
                {
                    Tables[i].RemotingFormat = value;
                }
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual SchemaSerializationMode SchemaSerializationMode
        {
            //Typed DataSet calls into this
            get { return SchemaSerializationMode.IncludeSchema; }
            set
            {
                if (value != SchemaSerializationMode.IncludeSchema)
                {
                    throw ExceptionBuilder.CannotChangeSchemaSerializationMode();
                }
            }
        }

        // Check whether the stream is binary serialized.
        // 'static' function that consumes SerializationInfo
        protected bool IsBinarySerialized(SerializationInfo info, StreamingContext context)
        {
            // mainly for typed DS
            // our default remoting format is XML
            SerializationFormat remotingFormat = SerializationFormat.Xml;
            SerializationInfoEnumerator e = info.GetEnumerator();

            while (e.MoveNext())
            {
                if (e.Name == "DataSet.RemotingFormat")
                {
                    //DataSet.RemotingFormat does not exist in V1/V1.1 versions
                    remotingFormat = (SerializationFormat)e.Value;
                    break;
                }
            }

            return (remotingFormat == SerializationFormat.Binary);
        }

        // Should Schema be included during Serialization
        // 'static' function that consumes SerializationInfo
        protected SchemaSerializationMode DetermineSchemaSerializationMode(SerializationInfo info, StreamingContext context)
        {
            //Typed DataSet calls into this
            SchemaSerializationMode schemaSerializationMode = SchemaSerializationMode.IncludeSchema;
            SerializationInfoEnumerator e = info.GetEnumerator();

            while (e.MoveNext())
            {
                if (e.Name == "SchemaSerializationMode.DataSet")
                { //SchemaSerializationMode.DataSet does not exist in V1/V1.1 versions
                    schemaSerializationMode = (SchemaSerializationMode)e.Value;
                    break;
                }
            }
            return schemaSerializationMode;
        }

        protected SchemaSerializationMode DetermineSchemaSerializationMode(XmlReader reader)
        {
            //Typed DataSet calls into this
            SchemaSerializationMode schemaSerializationMode = SchemaSerializationMode.IncludeSchema;
            reader.MoveToContent();
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.HasAttributes)
                {
                    string attribValue = reader.GetAttribute(Keywords.MSD_SCHEMASERIALIZATIONMODE, Keywords.MSDNS);
                    if (string.Equals(attribValue, Keywords.MSD_EXCLUDESCHEMA, StringComparison.OrdinalIgnoreCase))
                    {
                        schemaSerializationMode = SchemaSerializationMode.ExcludeSchema;
                    }
                    else if (string.Equals(attribValue, Keywords.MSD_INCLUDESCHEMA, StringComparison.OrdinalIgnoreCase))
                    {
                        schemaSerializationMode = SchemaSerializationMode.IncludeSchema;
                    }
                    else if (attribValue != null)
                    {
                        // if attrib does not exist, then don't throw
                        throw ExceptionBuilder.InvalidSchemaSerializationMode(typeof(SchemaSerializationMode), attribValue);
                    }
                }
            }
            return schemaSerializationMode;
        }


        // Deserialize all the tables data of the dataset from binary/xml stream.
        // 'instance' method that consumes SerializationInfo
        protected void GetSerializationData(SerializationInfo info, StreamingContext context)
        {
            // mainly for typed DS
            SerializationFormat remotingFormat = SerializationFormat.Xml;
            SerializationInfoEnumerator e = info.GetEnumerator();

            while (e.MoveNext())
            {
                if (e.Name == "DataSet.RemotingFormat")
                { //DataSet.RemotingFormat does not exist in V1/V1.1 versions
                    remotingFormat = (SerializationFormat)e.Value;
                    break;
                }
            }

            DeserializeDataSetData(info, context, remotingFormat);
        }


        // Deserialize all the tables schema and data of the dataset from binary/xml stream.
        protected DataSet(SerializationInfo info, StreamingContext context) : this(info, context, true)
        {
        }

        protected DataSet(SerializationInfo info, StreamingContext context, bool ConstructSchema) : this()
        {
            SerializationFormat remotingFormat = SerializationFormat.Xml;
            SchemaSerializationMode schemaSerializationMode = SchemaSerializationMode.IncludeSchema;
            SerializationInfoEnumerator e = info.GetEnumerator();

            while (e.MoveNext())
            {
                switch (e.Name)
                {
                    case "DataSet.RemotingFormat": //DataSet.RemotingFormat does not exist in V1/V1.1 versions
                        remotingFormat = (SerializationFormat)e.Value;
                        break;
                    case "SchemaSerializationMode.DataSet": //SchemaSerializationMode.DataSet does not exist in V1/V1.1 versions
                        schemaSerializationMode = (SchemaSerializationMode)e.Value;
                        break;
                }
            }

            if (schemaSerializationMode == SchemaSerializationMode.ExcludeSchema)
            {
                InitializeDerivedDataSet();
            }

            // adding back this check will fix typed dataset XML remoting, but we have to fix case that 
            // a class inherits from DataSet and just relies on DataSet to deserialize (see SQL BU DT 374717)
            // to fix that case also, we need to add a flag and add it to below check so return (no-op) will be 
            // conditional (flag needs to be set in TypedDataSet
            if (remotingFormat == SerializationFormat.Xml && !ConstructSchema)
            {
                return; //For typed dataset xml remoting, this is a no-op
            }

            DeserializeDataSet(info, context, remotingFormat, schemaSerializationMode);
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            SerializationFormat remotingFormat = RemotingFormat;
            SerializeDataSet(info, context, remotingFormat);
        }

        // Deserialize all the tables data of the dataset from binary/xml stream.
        protected virtual void InitializeDerivedDataSet() { }

        // Serialize all the tables.
        private void SerializeDataSet(SerializationInfo info, StreamingContext context, SerializationFormat remotingFormat)
        {
            Debug.Assert(info != null);
            info.AddValue("DataSet.RemotingVersion", new Version(2, 0));

            // SqlHotFix 299, SerializationFormat enumeration types don't exist in V1.1 SP1
            if (SerializationFormat.Xml != remotingFormat)
            {
                info.AddValue("DataSet.RemotingFormat", remotingFormat);
            }

            // SqlHotFix 299, SchemaSerializationMode enumeration types don't exist in V1.1 SP1
            if (SchemaSerializationMode.IncludeSchema != SchemaSerializationMode)
            {
                //SkipSchemaDuringSerialization
                info.AddValue("SchemaSerializationMode.DataSet", SchemaSerializationMode);
            }

            if (remotingFormat != SerializationFormat.Xml)
            {
                if (SchemaSerializationMode == SchemaSerializationMode.IncludeSchema)
                {
                    //DataSet public state properties
                    SerializeDataSetProperties(info, context);

                    //Tables Count
                    info.AddValue("DataSet.Tables.Count", Tables.Count);

                    //Tables, Columns, Rows
                    for (int i = 0; i < Tables.Count; i++)
                    {
                        BinaryFormatter bf = new BinaryFormatter(null, new StreamingContext(context.State, false));
                        MemoryStream memStream = new MemoryStream();
                        bf.Serialize(memStream, Tables[i]);
                        memStream.Position = 0;
                        info.AddValue(string.Format(CultureInfo.InvariantCulture, "DataSet.Tables_{0}", i), memStream.GetBuffer());
                    }

                    //Constraints
                    for (int i = 0; i < Tables.Count; i++)
                    {
                        Tables[i].SerializeConstraints(info, context, i, true);
                    }

                    //Relations
                    SerializeRelations(info, context);

                    //Expression Columns
                    for (int i = 0; i < Tables.Count; i++)
                    {
                        Tables[i].SerializeExpressionColumns(info, context, i);
                    }
                }
                else
                {
                    //Serialize  DataSet public properties.
                    SerializeDataSetProperties(info, context);
                }

                //Rows
                for (int i = 0; i < Tables.Count; i++)
                {
                    Tables[i].SerializeTableData(info, context, i);
                }
            }
            else
            {
                // old behaviour
                string strSchema = GetXmlSchemaForRemoting(null);

                string strData = null;
                info.AddValue(KEY_XMLSCHEMA, strSchema);

                StringBuilder strBuilder = new StringBuilder(EstimatedXmlStringSize() * 2);
                StringWriter strWriter = new StringWriter(strBuilder, CultureInfo.InvariantCulture);
                XmlTextWriter w = new XmlTextWriter(strWriter);
                WriteXml(w, XmlWriteMode.DiffGram);
                strData = strWriter.ToString();
                info.AddValue(KEY_XMLDIFFGRAM, strData);
            }
        }

        // Deserialize all the tables - marked internal so that DataTable can call into this
        internal void DeserializeDataSet(SerializationInfo info, StreamingContext context, SerializationFormat remotingFormat, SchemaSerializationMode schemaSerializationMode)
        {
            // deserialize schema
            DeserializeDataSetSchema(info, context, remotingFormat, schemaSerializationMode);
            // deserialize data
            DeserializeDataSetData(info, context, remotingFormat);
        }

        // Deserialize schema.
        private void DeserializeDataSetSchema(SerializationInfo info, StreamingContext context, SerializationFormat remotingFormat, SchemaSerializationMode schemaSerializationMode)
        {
            if (remotingFormat != SerializationFormat.Xml)
            {
                if (schemaSerializationMode == SchemaSerializationMode.IncludeSchema)
                {
                    //DataSet public state properties
                    DeserializeDataSetProperties(info, context);

                    //Tables Count
                    int tableCount = info.GetInt32("DataSet.Tables.Count");

                    //Tables, Columns, Rows
                    for (int i = 0; i < tableCount; i++)
                    {
                        byte[] buffer = (byte[])info.GetValue(string.Format(CultureInfo.InvariantCulture, "DataSet.Tables_{0}", i), typeof(byte[]));
                        MemoryStream memStream = new MemoryStream(buffer);
                        memStream.Position = 0;
                        BinaryFormatter bf = new BinaryFormatter(null, new StreamingContext(context.State, false));
                        DataTable dt = (DataTable)bf.Deserialize(memStream);
                        Tables.Add(dt);
                    }

                    //Constraints
                    for (int i = 0; i < tableCount; i++)
                    {
                        Tables[i].DeserializeConstraints(info, context,  /* table index */i,  /* serialize all constraints */ true); //
                    }

                    //Relations
                    DeserializeRelations(info, context);

                    //Expression Columns
                    for (int i = 0; i < tableCount; i++)
                    {
                        Tables[i].DeserializeExpressionColumns(info, context, i);
                    }
                }
                else
                {
                    //DeSerialize DataSet public properties.[Locale, CaseSensitive and EnforceConstraints]
                    DeserializeDataSetProperties(info, context);
                }
            }
            else
            {
                string strSchema = (string)info.GetValue(KEY_XMLSCHEMA, typeof(string));

                if (strSchema != null)
                {
                    ReadXmlSchema(new XmlTextReader(new StringReader(strSchema)), true);
                }
            }
        }

        // Deserialize all  data.
        private void DeserializeDataSetData(SerializationInfo info, StreamingContext context, SerializationFormat remotingFormat)
        {
            if (remotingFormat != SerializationFormat.Xml)
            {
                for (int i = 0; i < Tables.Count; i++)
                {
                    Tables[i].DeserializeTableData(info, context, i);
                }
            }
            else
            {
                string strData = (string)info.GetValue(KEY_XMLDIFFGRAM, typeof(string));

                if (strData != null)
                {
                    ReadXml(new XmlTextReader(new StringReader(strData)), XmlReadMode.DiffGram);
                }
            }
        }

        // Serialize just the dataset properties
        private void SerializeDataSetProperties(SerializationInfo info, StreamingContext context)
        {
            //DataSet basic properties
            info.AddValue("DataSet.DataSetName", DataSetName);
            info.AddValue("DataSet.Namespace", Namespace);
            info.AddValue("DataSet.Prefix", Prefix);

            //DataSet runtime properties
            info.AddValue("DataSet.CaseSensitive", CaseSensitive);
            info.AddValue("DataSet.LocaleLCID", Locale.LCID);
            info.AddValue("DataSet.EnforceConstraints", EnforceConstraints);

            //ExtendedProperties
            info.AddValue("DataSet.ExtendedProperties", ExtendedProperties);
        }

        // DeSerialize dataset properties
        private void DeserializeDataSetProperties(SerializationInfo info, StreamingContext context)
        {
            //DataSet basic properties
            _dataSetName = info.GetString("DataSet.DataSetName");
            _namespaceURI = info.GetString("DataSet.Namespace");
            _datasetPrefix = info.GetString("DataSet.Prefix");

            //DataSet runtime properties
            _caseSensitive = info.GetBoolean("DataSet.CaseSensitive");
            int lcid = (int)info.GetValue("DataSet.LocaleLCID", typeof(int));
            _culture = new CultureInfo(lcid);
            _cultureUserSet = true;
            _enforceConstraints = info.GetBoolean("DataSet.EnforceConstraints");

            //ExtendedProperties
            _extendedProperties = (PropertyCollection)info.GetValue("DataSet.ExtendedProperties", typeof(PropertyCollection));
        }

        // Gets relation info from the dataset.
        // ***Schema for Serializing ArrayList of Relations***
        // Relations -> [relationName]->[parentTableIndex, parentcolumnIndexes]->[childTableIndex, childColumnIndexes]->[Nested]->[extendedProperties]
        private void SerializeRelations(SerializationInfo info, StreamingContext context)
        {
            ArrayList relationList = new ArrayList();

            foreach (DataRelation rel in Relations)
            {
                int[] parentInfo = new int[rel.ParentColumns.Length + 1];

                parentInfo[0] = Tables.IndexOf(rel.ParentTable);
                for (int j = 1; j < parentInfo.Length; j++)
                {
                    parentInfo[j] = rel.ParentColumns[j - 1].Ordinal;
                }

                int[] childInfo = new int[rel.ChildColumns.Length + 1];
                childInfo[0] = Tables.IndexOf(rel.ChildTable);
                for (int j = 1; j < childInfo.Length; j++)
                {
                    childInfo[j] = rel.ChildColumns[j - 1].Ordinal;
                }

                ArrayList list = new ArrayList();
                list.Add(rel.RelationName);
                list.Add(parentInfo);
                list.Add(childInfo);
                list.Add(rel.Nested);
                list.Add(rel._extendedProperties);

                relationList.Add(list);
            }
            info.AddValue("DataSet.Relations", relationList);
        }

        // Adds relations to the dataset.
        // ***Schema for Serializing ArrayList of Relations***
        // Relations -> [relationName]->[parentTableIndex, parentcolumnIndexes]->[childTableIndex, childColumnIndexes]->[Nested]->[extendedProperties]
        private void DeserializeRelations(SerializationInfo info, StreamingContext context)
        {
            ArrayList relationList = (ArrayList)info.GetValue("DataSet.Relations", typeof(ArrayList));

            foreach (ArrayList list in relationList)
            {
                string relationName = (string)list[0];
                int[] parentInfo = (int[])list[1];
                int[] childInfo = (int[])list[2];
                bool isNested = (bool)list[3];
                PropertyCollection extendedProperties = (PropertyCollection)list[4];

                //ParentKey Columns.
                DataColumn[] parentkeyColumns = new DataColumn[parentInfo.Length - 1];
                for (int i = 0; i < parentkeyColumns.Length; i++)
                {
                    parentkeyColumns[i] = Tables[parentInfo[0]].Columns[parentInfo[i + 1]];
                }

                //ChildKey Columns.
                DataColumn[] childkeyColumns = new DataColumn[childInfo.Length - 1];
                for (int i = 0; i < childkeyColumns.Length; i++)
                {
                    childkeyColumns[i] = Tables[childInfo[0]].Columns[childInfo[i + 1]];
                }

                //Create the Relation, without any constraints[Assumption: The constraints are added earlier than the relations]
                DataRelation rel = new DataRelation(relationName, parentkeyColumns, childkeyColumns, false);
                rel.CheckMultipleNested = false; // disable the check for multiple nested parent
                rel.Nested = isNested;
                rel._extendedProperties = extendedProperties;

                Relations.Add(rel);
                rel.CheckMultipleNested = true; // enable the check for multiple nested parent
            }
        }

        internal void FailedEnableConstraints()
        {
            EnforceConstraints = false;
            throw ExceptionBuilder.EnforceConstraint();
        }

        /// <summary>
        /// Gets or sets a value indicating whether string
        /// comparisons within <see cref='System.Data.DataTable'/>
        /// objects are case-sensitive.
        /// </summary>
        [DefaultValue(false)]
        public bool CaseSensitive
        {
            get { return _caseSensitive; }
            set
            {
                if (_caseSensitive != value)
                {
                    bool oldValue = _caseSensitive;
                    _caseSensitive = value;

                    if (!ValidateCaseConstraint())
                    {
                        _caseSensitive = oldValue;
                        throw ExceptionBuilder.CannotChangeCaseLocale();
                    }

                    foreach (DataTable table in Tables)
                    {
                        table.SetCaseSensitiveValue(value, false, true);
                    }
                }
            }
        }

        bool IListSource.ContainsListCollection => true;

        /// <summary>
        /// Gets a custom view of the data contained by the <see cref='System.Data.DataSet'/> , one
        /// that allows filtering, searching, and navigating through the custom data view.
        /// </summary>
        [Browsable(false)]
        public DataViewManager DefaultViewManager
        {
            get
            {
                if (_defaultViewManager == null)
                {
                    lock (_defaultViewManagerLock)
                    {
                        if (_defaultViewManager == null)
                        {
                            _defaultViewManager = new DataViewManager(this, true);
                        }
                    }
                }
                return _defaultViewManager;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether constraint rules are followed when
        /// attempting any update operation.
        /// </summary>
        [DefaultValue(true)]
        public bool EnforceConstraints
        {
            get { return _enforceConstraints; }
            set
            {
                long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.set_EnforceConstraints|API> {0}, {1}", ObjectID, value);
                try
                {
                    if (_enforceConstraints != value)
                    {
                        if (value)
                        {
                            EnableConstraints();
                        }
                        _enforceConstraints = value;
                    }
                }
                finally
                {
                    DataCommonEventSource.Log.ExitScope(logScopeId);
                }
            }
        }

        internal void RestoreEnforceConstraints(bool value)
        {
            _enforceConstraints = value;
        }

        internal void EnableConstraints()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.EnableConstraints|INFO> {0}", ObjectID);
            try
            {
                bool errors = false;
                for (ConstraintEnumerator constraints = new ConstraintEnumerator(this); constraints.GetNext();)
                {
                    Constraint constraint = constraints.GetConstraint();
                    errors |= constraint.IsConstraintViolated();
                }

                foreach (DataTable table in Tables)
                {
                    foreach (DataColumn column in table.Columns)
                    {
                        if (!column.AllowDBNull)
                        {
                            errors |= column.IsNotAllowDBNullViolated();
                        }
                        if (column.MaxLength >= 0)
                        {
                            errors |= column.IsMaxLengthViolated();
                        }
                    }
                }

                if (errors)
                {
                    FailedEnableConstraints();
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Gets or sets the name of this <see cref='System.Data.DataSet'/> .
        /// </summary>
        [DefaultValue("")]
        public string DataSetName
        {
            get { return _dataSetName; }
            set
            {
                DataCommonEventSource.Log.Trace("<ds.DataSet.set_DataSetName|API> {0}, '{1}'", ObjectID, value);
                if (value != _dataSetName)
                {
                    if (value == null || value.Length == 0)
                    {
                        throw ExceptionBuilder.SetDataSetNameToEmpty();
                    }

                    DataTable conflicting = Tables[value, Namespace];
                    if ((conflicting != null) && (!conflicting._fNestedInDataset))
                    {
                        throw ExceptionBuilder.SetDataSetNameConflicting(value);
                    }

                    RaisePropertyChanging(nameof(DataSetName));
                    _dataSetName = value;
                }
            }
        }

        [DefaultValue("")]
        public string Namespace
        {
            get { return _namespaceURI; }
            set
            {
                DataCommonEventSource.Log.Trace("<ds.DataSet.set_Namespace|API> {0}, '{1}'", ObjectID, value);
                if (value == null)
                {
                    value = string.Empty;
                }

                if (value != _namespaceURI)
                {
                    RaisePropertyChanging(nameof(Namespace));
                    foreach (DataTable dt in Tables)
                    {
                        if (dt._tableNamespace != null)
                        {
                            continue;
                        }

                        if ((dt.NestedParentRelations.Length == 0) ||
                            (dt.NestedParentRelations.Length == 1 && dt.NestedParentRelations[0].ChildTable == dt))
                        {
                            if (Tables.Contains(dt.TableName, value, false, true))
                            {
                                throw ExceptionBuilder.DuplicateTableName2(dt.TableName, value);
                            }
                            dt.CheckCascadingNamespaceConflict(value);
                            dt.DoRaiseNamespaceChange();
                        }
                    }
                    _namespaceURI = value;

                    if (string.IsNullOrEmpty(value))
                    {
                        _datasetPrefix = string.Empty;
                    }
                }
            }
        }

        [DefaultValue("")]
        public string Prefix
        {
            get { return _datasetPrefix; }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }

                if ((XmlConvert.DecodeName(value) == value) && (XmlConvert.EncodeName(value) != value))
                {
                    throw ExceptionBuilder.InvalidPrefix(value);
                }

                if (value != _datasetPrefix)
                {
                    RaisePropertyChanging(nameof(Prefix));
                    _datasetPrefix = value;
                }
            }
        }

        /// <summary>
        /// Gets the collection of custom user information.
        /// </summary>
        [Browsable(false)]
        public PropertyCollection ExtendedProperties => _extendedProperties ?? (_extendedProperties = new PropertyCollection());

        /// <summary>
        /// Gets a value indicating whether there are errors in any
        /// of the rows in any of the tables of this <see cref='System.Data.DataSet'/> .
        /// </summary>
        [Browsable(false)]
        public bool HasErrors
        {
            get
            {
                for (int i = 0; i < Tables.Count; i++)
                {
                    if (Tables[i].HasErrors)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        [Browsable(false)]
        public bool IsInitialized => !_fInitInProgress;

        /// <summary>
        /// Gets or sets the locale information used to compare strings within the table.
        /// </summary>
        public CultureInfo Locale
        {
            get
            {
                // used for comparing not formating/parsing
                Debug.Assert(null != _culture, "DataSet.Locale: null culture");
                return _culture;
            }
            set
            {
                long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.set_Locale|API> {0}", ObjectID);
                try
                {
                    if (value != null)
                    {
                        if (!_culture.Equals(value))
                        {
                            SetLocaleValue(value, true);
                        }
                        _cultureUserSet = true;
                    }
                }
                finally
                {
                    DataCommonEventSource.Log.ExitScope(logScopeId);
                }
            }
        }

        internal void SetLocaleValue(CultureInfo value, bool userSet)
        {
            bool flag = false;
            bool exceptionThrown = false;
            int tableCount = 0;

            CultureInfo oldLocale = _culture;
            bool oldUserSet = _cultureUserSet;

            try
            {
                _culture = value;
                _cultureUserSet = userSet;

                foreach (DataTable table in Tables)
                {
                    if (!table.ShouldSerializeLocale())
                    {
                        bool retchk = table.SetLocaleValue(value, false, false);
                    }
                }

                flag = ValidateLocaleConstraint();
                if (flag)
                {
                    flag = false;
                    foreach (DataTable table in Tables)
                    {
                        tableCount++;
                        if (!table.ShouldSerializeLocale())
                        {
                            table.SetLocaleValue(value, false, true);
                        }
                    }
                    flag = true;
                }
            }
            catch
            {
                exceptionThrown = true;
                throw;
            }
            finally
            {
                if (!flag)
                { // reset old locale if ValidationFailed or exception thrown
                    _culture = oldLocale;
                    _cultureUserSet = oldUserSet;
                    foreach (DataTable table in Tables)
                    {
                        if (!table.ShouldSerializeLocale())
                        {
                            table.SetLocaleValue(oldLocale, false, false);
                        }
                    }
                    try
                    {
                        for (int i = 0; i < tableCount; ++i)
                        {
                            if (!Tables[i].ShouldSerializeLocale())
                            {
                                Tables[i].SetLocaleValue(oldLocale, false, true);
                            }
                        }
                    }
                    catch (Exception e) when (ADP.IsCatchableExceptionType(e))
                    {
                        ADP.TraceExceptionWithoutRethrow(e);
                    }
                    if (!exceptionThrown)
                    {
                        throw ExceptionBuilder.CannotChangeCaseLocale(null);
                    }
                }
            }
        }

        internal bool ShouldSerializeLocale()
        {
            // this method is used design-time scenarios via reflection
            //   by the property grid to show the Locale property in bold or not
            //   by the code dom for persisting the Locale property or not

            // we always want the locale persisted if set by user or different the current thread
            // but that logic should by performed by the serializion code
            return _cultureUserSet;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override ISite Site
        {
            get { return base.Site; }
            set
            {
                ISite oldSite = Site;
                if (value == null && oldSite != null)
                {
                    IContainer cont = oldSite.Container;

                    if (cont != null)
                    {
                        for (int i = 0; i < Tables.Count; i++)
                        {
                            if (Tables[i].Site != null)
                            {
                                cont.Remove(Tables[i]);
                            }
                        }
                    }
                }
                base.Site = value;
            }
        }

        /// <summary>
        /// Get the collection of relations that link tables and
        /// allow navigation from parent tables to child tables.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public DataRelationCollection Relations => _relationCollection;

        /// <summary>
        /// Indicates whether <see cref='Relations'/> property should be persisted.
        /// </summary>
        protected virtual bool ShouldSerializeRelations() => true;

        /// <summary>
        /// Resets the <see cref='System.Data.DataSet.Relations'/> property to its default state.
        /// </summary>
        private void ResetRelations() => Relations.Clear();

        /// <summary>
        /// Gets the collection of tables contained in the <see cref='System.Data.DataSet'/>.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public DataTableCollection Tables => _tableCollection;

        /// <summary>
        /// Indicates whether <see cref='System.Data.DataSet.Tables'/> property should be persisted.
        /// </summary>
        protected virtual bool ShouldSerializeTables() => true;

        /// <summary>
        /// Resets the <see cref='System.Data.DataSet.Tables'/> property to its default state.
        /// </summary>
        private void ResetTables() => Tables.Clear();

        internal bool FBoundToDocument
        {
            get { return _fBoundToDocument; }
            set { _fBoundToDocument = value; }
        }

        /// <summary>
        /// Commits all the changes made to this <see cref='System.Data.DataSet'/> since it was loaded or the last
        /// time <see cref='System.Data.DataSet.AcceptChanges'/> was called.
        /// </summary>
        public void AcceptChanges()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.AcceptChanges|API> {0}", ObjectID);
            try
            {
                for (int i = 0; i < Tables.Count; i++)
                {
                    Tables[i].AcceptChanges();
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal event PropertyChangedEventHandler PropertyChanging;

        /// <summary>
        /// Occurs when attempting to merge schemas for two tables with the same name.
        /// </summary>
        public event MergeFailedEventHandler MergeFailed;

        internal event DataRowCreatedEventHandler DataRowCreated; // Internal for XmlDataDocument only
        internal event DataSetClearEventhandler ClearFunctionCalled; // Internal for XmlDataDocument only

        public event EventHandler Initialized;

        public void BeginInit()
        {
            _fInitInProgress = true;
        }

        public void EndInit()
        {
            Tables.FinishInitCollection();
            for (int i = 0; i < Tables.Count; i++)
            {
                Tables[i].Columns.FinishInitCollection();
            }

            for (int i = 0; i < Tables.Count; i++)
            {
                Tables[i].Constraints.FinishInitConstraints();
            }

            ((DataRelationCollection.DataSetRelationCollection)Relations).FinishInitRelations();

            _fInitInProgress = false;
            OnInitialized();
        }

        /// <summary>
        /// Clears the <see cref='System.Data.DataSet'/> of any data by removing all rows in all tables.
        /// </summary>
        public void Clear()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Clear|API> {0}", ObjectID);
            try
            {
                OnClearFunctionCalled(null);
                bool fEnforce = EnforceConstraints;
                EnforceConstraints = false;
                for (int i = 0; i < Tables.Count; i++)
                {
                    Tables[i].Clear();
                }
                EnforceConstraints = fEnforce;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Clones the structure of the <see cref='System.Data.DataSet'/>, including all <see cref='System.Data.DataTable'/> schemas, relations, and
        /// constraints.
        /// </summary>
        // Prevent inlining so that reflection calls are not moved to caller that may be in a different assembly that may have a different grant set.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual DataSet Clone()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Clone|API> {0}", ObjectID);
            try
            {
                DataSet ds = (DataSet)Activator.CreateInstance(GetType(), true);

                if (ds.Tables.Count > 0)  // To clean up all the schema in strong typed dataset.
                {
                    ds.Reset();
                }

                //copy some original dataset properties
                ds.DataSetName = DataSetName;
                ds.CaseSensitive = CaseSensitive;
                ds._culture = _culture;
                ds._cultureUserSet = _cultureUserSet;
                ds.EnforceConstraints = EnforceConstraints;
                ds.Namespace = Namespace;
                ds.Prefix = Prefix;
                ds.RemotingFormat = RemotingFormat;
                ds._fIsSchemaLoading = true; //delay expression evaluation

                // ...Tables...
                DataTableCollection tbls = Tables;
                for (int i = 0; i < tbls.Count; i++)
                {
                    DataTable dt = tbls[i].Clone(ds);
                    dt._tableNamespace = tbls[i].Namespace; // hardcode the namespace for a second to not mess up
                    // DataRelation cloning.
                    ds.Tables.Add(dt);
                }

                // ...Constraints...
                for (int i = 0; i < tbls.Count; i++)
                {
                    ConstraintCollection constraints = tbls[i].Constraints;
                    for (int j = 0; j < constraints.Count; j++)
                    {
                        if (constraints[j] is UniqueConstraint)
                        {
                            continue;
                        }

                        ForeignKeyConstraint foreign = constraints[j] as ForeignKeyConstraint;
                        if (foreign.Table == foreign.RelatedTable)
                        {
                            continue;// we have already added this foreign key in while cloning the datatable
                        }

                        ds.Tables[i].Constraints.Add(constraints[j].Clone(ds));
                    }
                }

                // ...Relations...
                DataRelationCollection rels = Relations;
                for (int i = 0; i < rels.Count; i++)
                {
                    DataRelation rel = rels[i].Clone(ds);
                    rel.CheckMultipleNested = false; // disable the check for multiple nested parent
                    ds.Relations.Add(rel);
                    rel.CheckMultipleNested = true; // enable the check for multiple nested parent
                }

                // ...Extended Properties...
                if (_extendedProperties != null)
                {
                    foreach (object key in _extendedProperties.Keys)
                    {
                        ds.ExtendedProperties[key] = _extendedProperties[key];
                    }
                }

                foreach (DataTable table in Tables)
                {
                    foreach (DataColumn col in table.Columns)
                    {
                        if (col.Expression.Length != 0)
                        {
                            ds.Tables[table.TableName, table.Namespace].Columns[col.ColumnName].Expression = col.Expression;
                        }
                    }
                }

                for (int i = 0; i < tbls.Count; i++)
                {
                    ds.Tables[i]._tableNamespace = tbls[i]._tableNamespace; // undo the hardcoding of the namespace
                }

                ds._fIsSchemaLoading = false; //reactivate column computations

                return ds;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Copies both the structure and data for this <see cref='System.Data.DataSet'/>.
        /// </summary>
        public DataSet Copy()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Copy|API> {0}", ObjectID);
            try
            {
                DataSet dsNew = Clone();
                bool fEnforceConstraints = dsNew.EnforceConstraints;
                dsNew.EnforceConstraints = false;
                foreach (DataTable table in Tables)
                {
                    DataTable destTable = dsNew.Tables[table.TableName, table.Namespace];

                    foreach (DataRow row in table.Rows)
                    {
                        table.CopyRow(destTable, row);
                    }
                }

                dsNew.EnforceConstraints = fEnforceConstraints;

                return dsNew;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal int EstimatedXmlStringSize()
        {
            int bytes = 100;
            for (int i = 0; i < Tables.Count; i++)
            {
                int rowBytes = (Tables[i].TableName.Length + 4) << 2;
                DataTable table = Tables[i];
                for (int j = 0; j < table.Columns.Count; j++)
                {
                    rowBytes += ((table.Columns[j].ColumnName.Length + 4) << 2);
                    rowBytes += 20;
                }
                bytes += table.Rows.Count * rowBytes;
            }

            return bytes;
        }

        /// <summary>
        /// Returns a copy of the <see cref='System.Data.DataSet'/> that contains all changes made to
        /// it since it was loaded or <see cref='System.Data.DataSet.AcceptChanges'/> was last called.
        /// </summary>
        public DataSet GetChanges() =>
            GetChanges(DataRowState.Added | DataRowState.Deleted | DataRowState.Modified);

        private struct TableChanges
        {
            private BitArray _rowChanges;

            internal TableChanges(int rowCount)
            {
                _rowChanges = new BitArray(rowCount);
                HasChanges = 0;
            }

            internal int HasChanges { get; set; }

            internal bool this[int index]
            {
                get { return _rowChanges[index]; }
                set
                {
                    Debug.Assert(value && !_rowChanges[index], "setting twice or to false");
                    _rowChanges[index] = value;
                    HasChanges++;
                }
            }
        }

        public DataSet GetChanges(DataRowState rowStates)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.GetChanges|API> {0}, rowStates={1}", ObjectID, rowStates);
            try
            {
                DataSet dsNew = null;
                bool fEnforceConstraints = false;
                if (0 != (rowStates & ~(DataRowState.Added | DataRowState.Deleted | DataRowState.Modified | DataRowState.Unchanged)))
                {
                    throw ExceptionBuilder.InvalidRowState(rowStates);
                }

                // Initialize all the individual table bitmaps.
                TableChanges[] bitMatrix = new TableChanges[Tables.Count];
                for (int i = 0; i < bitMatrix.Length; ++i)
                {
                    bitMatrix[i] = new TableChanges(Tables[i].Rows.Count);
                }

                // find all the modified rows and their parents
                MarkModifiedRows(bitMatrix, rowStates);

                // copy the changes to a cloned table
                for (int i = 0; i < bitMatrix.Length; ++i)
                {
                    Debug.Assert(0 <= bitMatrix[i].HasChanges, "negative change count");
                    if (0 < bitMatrix[i].HasChanges)
                    {
                        if (null == dsNew)
                        {
                            dsNew = Clone();
                            fEnforceConstraints = dsNew.EnforceConstraints;
                            dsNew.EnforceConstraints = false;
                        }

                        DataTable table = Tables[i];
                        DataTable destTable = dsNew.Tables[table.TableName, table.Namespace];
                        Debug.Assert(bitMatrix[i].HasChanges <= table.Rows.Count, "to many changes");

                        for (int j = 0; 0 < bitMatrix[i].HasChanges; ++j)
                        {
                            // Loop through the rows.
                            if (bitMatrix[i][j])
                            {
                                table.CopyRow(destTable, table.Rows[j]);
                                bitMatrix[i].HasChanges--;
                            }
                        }
                    }
                }

                if (null != dsNew)
                {
                    dsNew.EnforceConstraints = fEnforceConstraints;
                }
                return dsNew;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        private void MarkModifiedRows(TableChanges[] bitMatrix, DataRowState rowStates)
        {
            // for every table, every row & every relation find the modified rows and for non-deleted rows, their parents
            for (int tableIndex = 0; tableIndex < bitMatrix.Length; ++tableIndex)
            {
                DataRowCollection rows = Tables[tableIndex].Rows;
                int rowCount = rows.Count;

                for (int rowIndex = 0; rowIndex < rowCount; ++rowIndex)
                {
                    DataRow row = rows[rowIndex];
                    DataRowState rowState = row.RowState;
                    Debug.Assert(DataRowState.Added == rowState ||
                                 DataRowState.Deleted == rowState ||
                                 DataRowState.Modified == rowState ||
                                 DataRowState.Unchanged == rowState,
                                 "unexpected DataRowState");

                    // if bit not already set and row is modified
                    if ((0 != (rowStates & rowState)) && !bitMatrix[tableIndex][rowIndex])
                    {
                        bitMatrix[tableIndex][rowIndex] = true;

                        if (DataRowState.Deleted != rowState)
                        {
                            MarkRelatedRowsAsModified(bitMatrix, row);
                        }
                    }
                }
            }
        }

        private void MarkRelatedRowsAsModified(TableChanges[] bitMatrix, DataRow row)
        {
            DataRelationCollection relations = row.Table.ParentRelations;
            int relationCount = relations.Count;
            for (int relatedIndex = 0; relatedIndex < relationCount; ++relatedIndex)
            {
                DataRow[] relatedRows = row.GetParentRows(relations[relatedIndex], DataRowVersion.Current);

                foreach (DataRow relatedRow in relatedRows)
                {
                    int relatedTableIndex = Tables.IndexOf(relatedRow.Table);
                    int relatedRowIndex = relatedRow.Table.Rows.IndexOf(relatedRow);

                    if (!bitMatrix[relatedTableIndex][relatedRowIndex])
                    {
                        bitMatrix[relatedTableIndex][relatedRowIndex] = true;

                        if (DataRowState.Deleted != relatedRow.RowState)
                        {
                            // recurse into related rows
                            MarkRelatedRowsAsModified(bitMatrix, relatedRow);
                        }
                    }
                }
            }
        }

        IList IListSource.GetList() => DefaultViewManager;

        internal string GetRemotingDiffGram(DataTable table)
        {
            StringWriter strWriter = new StringWriter(CultureInfo.InvariantCulture);
            XmlTextWriter writer = new XmlTextWriter(strWriter);
            writer.Formatting = Formatting.Indented;
            if (strWriter != null)
            {
                // Create and save the updates
                new NewDiffgramGen(table, false).Save(writer, table);
            }

            return strWriter.ToString();
        }

        public string GetXml()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.GetXml|API> {0}", ObjectID);
            try
            {
                // StringBuilder strBuilder = new StringBuilder(EstimatedXmlStringSize());
                // StringWriter strWriter = new StringWriter(strBuilder);
                StringWriter strWriter = new StringWriter(CultureInfo.InvariantCulture);
                if (strWriter != null)
                {
                    XmlTextWriter w = new XmlTextWriter(strWriter);
                    w.Formatting = Formatting.Indented;
                    new XmlDataTreeWriter(this).Save(w, false);
                }
                return strWriter.ToString();
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public string GetXmlSchema()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.GetXmlSchema|API> {0}", ObjectID);
            try
            {
                StringWriter strWriter = new StringWriter(CultureInfo.InvariantCulture);
                XmlTextWriter writer = new XmlTextWriter(strWriter);
                writer.Formatting = Formatting.Indented;
                if (strWriter != null)
                {
                    (new XmlTreeGen(SchemaFormat.Public)).Save(this, writer);
                }

                return strWriter.ToString();
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal string GetXmlSchemaForRemoting(DataTable table)
        {
            StringWriter strWriter = new StringWriter(CultureInfo.InvariantCulture);
            XmlTextWriter writer = new XmlTextWriter(strWriter);
            writer.Formatting = Formatting.Indented;
            if (strWriter != null)
            {
                if (table == null)
                {
                    if (SchemaSerializationMode == SchemaSerializationMode.ExcludeSchema)
                    {
                        (new XmlTreeGen(SchemaFormat.RemotingSkipSchema)).Save(this, writer);
                    }
                    else
                    {
                        (new XmlTreeGen(SchemaFormat.Remoting)).Save(this, writer);
                    }
                }
                else
                {
                    // no skip schema support for typed datatable
                    (new XmlTreeGen(SchemaFormat.Remoting)).Save(table, writer);
                }
            }

            return strWriter.ToString();
        }


        /// <summary>
        /// Gets a value indicating whether the <see cref='System.Data.DataSet'/> has changes, including new,
        ///    deleted, or modified rows.
        /// </summary>
        public bool HasChanges() => HasChanges(DataRowState.Added | DataRowState.Deleted | DataRowState.Modified);

        /// <summary>
        /// Gets a value indicating whether the <see cref='System.Data.DataSet'/> has changes, including new,
        ///    deleted, or modified rows, filtered by <see cref='System.Data.DataRowState'/>.
        /// </summary>
        public bool HasChanges(DataRowState rowStates)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.HasChanges|API> {0}, rowStates={1}", ObjectID, (int)rowStates);
            try
            {
                const DataRowState allRowStates = DataRowState.Detached | DataRowState.Unchanged | DataRowState.Added | DataRowState.Deleted | DataRowState.Modified;

                if ((rowStates & (~allRowStates)) != 0)
                {
                    throw ExceptionBuilder.ArgumentOutOfRange("rowState");
                }

                for (int i = 0; i < Tables.Count; i++)
                {
                    DataTable table = Tables[i];

                    for (int j = 0; j < table.Rows.Count; j++)
                    {
                        DataRow row = table.Rows[j];
                        if ((row.RowState & rowStates) != 0)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Infer the XML schema from the specified <see cref='System.IO.TextReader'/> into the <see cref='System.Data.DataSet'/>.
        /// </summary>
        public void InferXmlSchema(XmlReader reader, string[] nsArray)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.InferXmlSchema|API> {0}", ObjectID);
            try
            {
                if (reader == null)
                {
                    return;
                }

                XmlDocument xdoc = new XmlDocument();
                if (reader.NodeType == XmlNodeType.Element)
                {
                    XmlNode node = xdoc.ReadNode(reader);
                    xdoc.AppendChild(node);
                }
                else
                {
                    xdoc.Load(reader);
                }

                if (xdoc.DocumentElement == null)
                {
                    return;
                }

                InferSchema(xdoc, nsArray, XmlReadMode.InferSchema);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Infer the XML schema from the specified <see cref='System.IO.TextReader'/> into the <see cref='System.Data.DataSet'/>.
        /// </summary>
        public void InferXmlSchema(Stream stream, string[] nsArray)
        {
            if (stream == null)
            {
                return;
            }

            InferXmlSchema(new XmlTextReader(stream), nsArray);
        }

        /// <summary>
        /// Infer the XML schema from the specified <see cref='System.IO.TextReader'/> into the <see cref='System.Data.DataSet'/>.
        /// </summary>
        public void InferXmlSchema(TextReader reader, string[] nsArray)
        {
            if (reader == null)
            {
                return;
            }

            InferXmlSchema(new XmlTextReader(reader), nsArray);
        }

        /// <summary>
        /// Infer the XML schema from the specified file into the <see cref='System.Data.DataSet'/>.
        /// </summary>
        public void InferXmlSchema(string fileName, string[] nsArray)
        {
            XmlTextReader xr = new XmlTextReader(fileName);
            try
            {
                InferXmlSchema(xr, nsArray);
            }
            finally
            {
                xr.Close();
            }
        }

        /// <summary>
        /// Reads the XML schema from the specified <see cref='T:System.Xml.XMLReader'/> into the <see cref='System.Data.DataSet'/>
        /// </summary>
        public void ReadXmlSchema(XmlReader reader) => ReadXmlSchema(reader, false);

        internal void ReadXmlSchema(XmlReader reader, bool denyResolving)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.ReadXmlSchema|INFO> {0}, reader, denyResolving={1}", ObjectID, denyResolving);
            try
            {
                int iCurrentDepth = -1;

                if (reader == null)
                {
                    return;
                }

                if (reader is XmlTextReader)
                {
                    ((XmlTextReader)reader).WhitespaceHandling = WhitespaceHandling.None;
                }

                XmlDocument xdoc = new XmlDocument(); // we may need this to infer the schema

                if (reader.NodeType == XmlNodeType.Element)
                {
                    iCurrentDepth = reader.Depth;
                }

                reader.MoveToContent();

                if (reader.NodeType == XmlNodeType.Element)
                {
                    // if reader points to the schema load it...
                    if (reader.LocalName == Keywords.XDR_SCHEMA && reader.NamespaceURI == Keywords.XDRNS)
                    {
                        // load XDR schema and exit
                        ReadXDRSchema(reader);
                        return;
                    }

                    if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI == Keywords.XSDNS)
                    {
                        // load XSD schema and exit
                        ReadXSDSchema(reader, denyResolving);
                        return;
                    }

                    if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI.StartsWith(Keywords.XSD_NS_START, StringComparison.Ordinal))
                    {
                        throw ExceptionBuilder.DataSetUnsupportedSchema(Keywords.XSDNS);
                    }

                    // ... otherwise backup the top node and all its attributes
                    XmlElement topNode = xdoc.CreateElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                    if (reader.HasAttributes)
                    {
                        int attrCount = reader.AttributeCount;
                        for (int i = 0; i < attrCount; i++)
                        {
                            reader.MoveToAttribute(i);
                            if (reader.NamespaceURI.Equals(Keywords.XSD_XMLNS_NS))
                            {
                                topNode.SetAttribute(reader.Name, reader.GetAttribute(i));
                            }
                            else
                            {
                                XmlAttribute attr = topNode.SetAttributeNode(reader.LocalName, reader.NamespaceURI);
                                attr.Prefix = reader.Prefix;
                                attr.Value = reader.GetAttribute(i);
                            }
                        }
                    }
                    reader.Read();

                    while (MoveToElement(reader, iCurrentDepth))
                    {
                        // if reader points to the schema load it...
                        if (reader.LocalName == Keywords.XDR_SCHEMA && reader.NamespaceURI == Keywords.XDRNS)
                        {
                            // load XDR schema and exit
                            ReadXDRSchema(reader);
                            return;
                        }

                        if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI == Keywords.XSDNS)
                        {
                            // load XSD schema and exit
                            ReadXSDSchema(reader, denyResolving);
                            return;
                        }

                        if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI.StartsWith(Keywords.XSD_NS_START, StringComparison.Ordinal))
                        {
                            throw ExceptionBuilder.DataSetUnsupportedSchema(Keywords.XSDNS);
                        }

                        XmlNode node = xdoc.ReadNode(reader);
                        topNode.AppendChild(node);
                    }

                    // read the closing tag of the current element
                    ReadEndElement(reader);

                    // if we are here no schema has been found
                    xdoc.AppendChild(topNode);

                    // so we InferSchema
                    InferSchema(xdoc, null, XmlReadMode.Auto);
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal bool MoveToElement(XmlReader reader, int depth)
        {
            while (!reader.EOF && reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.Element && reader.Depth > depth)
            {
                reader.Read();
            }
            return (reader.NodeType == XmlNodeType.Element);
        }

        private static void MoveToElement(XmlReader reader)
        {
            while (!reader.EOF && reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.Element)
            {
                reader.Read();
            }
        }
        internal void ReadEndElement(XmlReader reader)
        {
            while (reader.NodeType == XmlNodeType.Whitespace)
            {
                reader.Skip();
            }
            if (reader.NodeType == XmlNodeType.None)
            {
                reader.Skip();
            }
            else if (reader.NodeType == XmlNodeType.EndElement)
            {
                reader.ReadEndElement();
            }
        }

        internal void ReadXSDSchema(XmlReader reader, bool denyResolving)
        {
            XmlSchemaSet sSet = new XmlSchemaSet();

            int schemaFragmentCount = 1;
            //read from current schmema element
            if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI == Keywords.XSDNS)
            {
                if (reader.HasAttributes)
                {
                    string attribValue = reader.GetAttribute(Keywords.MSD_FRAGMENTCOUNT, Keywords.MSDNS); // this must not move the position
                    if (!string.IsNullOrEmpty(attribValue))
                    {
                        schemaFragmentCount = int.Parse(attribValue, null);
                    }
                }
            }

            while (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI == Keywords.XSDNS)
            {
                XmlSchema s = XmlSchema.Read(reader, null);
                sSet.Add(s);
                //read the end tag
                ReadEndElement(reader);

                if (--schemaFragmentCount > 0)
                {
                    MoveToElement(reader);
                }
                while (reader.NodeType == XmlNodeType.Whitespace)
                {
                    reader.Skip();
                }
            }
            sSet.Compile();
            XSDSchema schema = new XSDSchema();
            schema.LoadSchema(sSet, this);
        }

        internal void ReadXDRSchema(XmlReader reader)
        {
            XmlDocument xdoc = new XmlDocument(); // we may need this to infer the schema
            XmlNode schNode = xdoc.ReadNode(reader);
            xdoc.AppendChild(schNode);
            XDRSchema schema = new XDRSchema(this, false);
            DataSetName = xdoc.DocumentElement.LocalName;
            schema.LoadSchema((XmlElement)schNode, this);
        }

        /// <summary>
        /// Reads the XML schema from the specified <see cref='System.IO.Stream'/> into the
        /// <see cref='System.Data.DataSet'/>.
        /// </summary>
        public void ReadXmlSchema(Stream stream)
        {
            if (stream == null)
            {
                return;
            }

            ReadXmlSchema(new XmlTextReader(stream), false);
        }

        /// <summary>
        /// Reads the XML schema from the specified <see cref='System.IO.TextReader'/> into the <see cref='System.Data.DataSet'/>.
        /// </summary>
        public void ReadXmlSchema(TextReader reader)
        {
            if (reader == null)
            {
                return;
            }

            ReadXmlSchema(new XmlTextReader(reader), false);
        }

        /// <summary>
        /// Reads the XML schema from the specified file into the <see cref='System.Data.DataSet'/>.
        /// </summary>
        public void ReadXmlSchema(string fileName)
        {
            XmlTextReader xr = new XmlTextReader(fileName);
            try
            {
                ReadXmlSchema(xr, false);
            }
            finally
            {
                xr.Close();
            }
        }

        #region WriteXmlSchema
        /// <summary>Writes the <see cref='DataSet'/> structure as an XML schema to using the specified <see cref='Stream'/> object.</summary>
        /// <param name="stream">A <see cref='Stream'/> object used to write to a file.</param>
        public void WriteXmlSchema(Stream stream) => WriteXmlSchema(stream, SchemaFormat.Public, null);

        /// <summary>Writes the <see cref='DataSet'/> structure as an XML schema to using the specified <see cref='Stream'/> object.</summary>
        /// <param name="stream">A <see cref='Stream'/> object used to write to a file.</param>
        /// <param name="multipleTargetConverter">A delegate used to convert <see cref='Type'/> into string.</param>
        public void WriteXmlSchema(Stream stream, Converter<Type, string> multipleTargetConverter)
        {
            ADP.CheckArgumentNull(multipleTargetConverter, nameof(multipleTargetConverter));
            WriteXmlSchema(stream, SchemaFormat.Public, multipleTargetConverter);
        }

        /// <summary>Writes the <see cref='DataSet'/> structure as an XML schema to a file.</summary>
        /// <param name="fileName">The file name (including the path) to which to write.</param>
        public void WriteXmlSchema(string fileName) => WriteXmlSchema(fileName, SchemaFormat.Public, null);

        /// <summary>Writes the <see cref='DataSet'/> structure as an XML schema to a file.</summary>
        /// <param name="fileName">The file name (including the path) to which to write.</param>
        /// <param name="multipleTargetConverter">A delegate used to convert <see cref='Type'/> into string.</param>
        public void WriteXmlSchema(string fileName, Converter<Type, string> multipleTargetConverter)
        {
            ADP.CheckArgumentNull(multipleTargetConverter, nameof(multipleTargetConverter));
            WriteXmlSchema(fileName, SchemaFormat.Public, multipleTargetConverter);
        }

        /// <summary>Writes the <see cref='DataSet'/> structure as an XML schema to a <see cref='TextWriter'/> object.</summary>
        /// <param name="writer">The <see cref='TextWriter'/> object with which to write.</param>
        public void WriteXmlSchema(TextWriter writer) => WriteXmlSchema(writer, SchemaFormat.Public, null);

        /// <summary>Writes the <see cref='DataSet'/> structure as an XML schema to a <see cref='TextWriter'/> object.</summary>
        /// <param name="writer">The <see cref='TextWriter'/> object with which to write.</param>
        /// <param name="multipleTargetConverter">A delegate used to convert <see cref='Type'/> into string.</param>
        public void WriteXmlSchema(TextWriter writer, Converter<Type, string> multipleTargetConverter)
        {
            ADP.CheckArgumentNull(multipleTargetConverter, nameof(multipleTargetConverter));
            WriteXmlSchema(writer, SchemaFormat.Public, multipleTargetConverter);
        }

        /// <summary>Writes the <see cref='DataSet'/> structure as an XML schema to an <see cref='XmlWriter'/> object.</summary>
        /// <param name="writer">The <see cref='XmlWriter'/> object with which to write.</param>
        public void WriteXmlSchema(XmlWriter writer) => WriteXmlSchema(writer, SchemaFormat.Public, null);

        /// <summary>Writes the <see cref='DataSet'/> structure as an XML schema to an <see cref='XmlWriter'/> object.</summary>
        /// <param name="writer">The <see cref='XmlWriter'/> object with which to write.</param>
        /// <param name="multipleTargetConverter">A delegate used to convert <see cref='Type'/> into string.</param>
        public void WriteXmlSchema(XmlWriter writer, Converter<Type, string> multipleTargetConverter)
        {
            ADP.CheckArgumentNull(multipleTargetConverter, nameof(multipleTargetConverter));
            WriteXmlSchema(writer, SchemaFormat.Public, multipleTargetConverter);
        }

        private void WriteXmlSchema(string fileName, SchemaFormat schemaFormat, Converter<Type, string> multipleTargetConverter)
        {
            XmlTextWriter xw = new XmlTextWriter(fileName, null);
            try
            {
                xw.Formatting = Formatting.Indented;
                xw.WriteStartDocument(true);
                WriteXmlSchema(xw, schemaFormat, multipleTargetConverter);
                xw.WriteEndDocument();
            }
            finally
            {
                xw.Close();
            }
        }

        private void WriteXmlSchema(Stream stream, SchemaFormat schemaFormat, Converter<Type, string> multipleTargetConverter)
        {
            if (stream == null)
            {
                return;
            }

            XmlTextWriter w = new XmlTextWriter(stream, null);
            w.Formatting = Formatting.Indented;

            WriteXmlSchema(w, schemaFormat, multipleTargetConverter);
        }

        private void WriteXmlSchema(TextWriter writer, SchemaFormat schemaFormat, Converter<Type, string> multipleTargetConverter)
        {
            if (writer == null)
            {
                return;
            }

            XmlTextWriter w = new XmlTextWriter(writer);
            w.Formatting = Formatting.Indented;

            WriteXmlSchema(w, schemaFormat, multipleTargetConverter);
        }

        private void WriteXmlSchema(XmlWriter writer, SchemaFormat schemaFormat, Converter<Type, string> multipleTargetConverter)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.WriteXmlSchema|INFO> {0}, schemaFormat={1}", ObjectID, schemaFormat);
            try
            {
                // Generate SchemaTree and write it out
                if (writer != null)
                {
                    XmlTreeGen treeGen = null;
                    if (schemaFormat == SchemaFormat.WebService &&
                        SchemaSerializationMode == SchemaSerializationMode.ExcludeSchema &&
                        writer.WriteState == WriteState.Element)
                    {
                        treeGen = new XmlTreeGen(SchemaFormat.WebServiceSkipSchema);
                    }
                    else
                    {
                        treeGen = new XmlTreeGen(schemaFormat);
                    }

                    treeGen.Save(this, null, writer, false, multipleTargetConverter);
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }
        #endregion

        public XmlReadMode ReadXml(XmlReader reader) => ReadXml(reader, false);

        internal XmlReadMode ReadXml(XmlReader reader, bool denyResolving)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.ReadXml|INFO> {0}, denyResolving={1}", ObjectID, denyResolving);
            try
            {
                DataTable.DSRowDiffIdUsageSection rowDiffIdUsage = new DataTable.DSRowDiffIdUsageSection();
                try
                {
                    bool fDataFound = false;
                    bool fSchemaFound = false;
                    bool fDiffsFound = false;
                    bool fIsXdr = false;
                    int iCurrentDepth = -1;
                    XmlReadMode ret = XmlReadMode.Auto;
                    bool isEmptyDataSet = false;
                    bool topNodeIsProcessed = false; // we chanche topnode and there is just one case that we miss to process it
                    // it is : <elem attrib1="Attrib">txt</elem>

                    // clear the hashtable to avoid conflicts between diffgrams, SqlHotFix 782
                    rowDiffIdUsage.Prepare(this);

                    if (reader == null)
                    {
                        return ret;
                    }

                    if (Tables.Count == 0)
                    {
                        isEmptyDataSet = true;
                    }

                    if (reader is XmlTextReader)
                    {
                        ((XmlTextReader)reader).WhitespaceHandling = WhitespaceHandling.Significant;
                    }

                    XmlDocument xdoc = new XmlDocument(); // we may need this to infer the schema
                    XmlDataLoader xmlload = null;

                    reader.MoveToContent();

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        iCurrentDepth = reader.Depth;
                    }

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if ((reader.LocalName == Keywords.DIFFGRAM) && (reader.NamespaceURI == Keywords.DFFNS))
                        {
                            ReadXmlDiffgram(reader);
                            // read the closing tag of the current element
                            ReadEndElement(reader);
                            return XmlReadMode.DiffGram;
                        }

                        // if reader points to the schema load it
                        if (reader.LocalName == Keywords.XDR_SCHEMA && reader.NamespaceURI == Keywords.XDRNS)
                        {
                            // load XDR schema and exit
                            ReadXDRSchema(reader);
                            return XmlReadMode.ReadSchema; //since the top level element is a schema return
                        }

                        if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI == Keywords.XSDNS)
                        {
                            // load XSD schema and exit
                            ReadXSDSchema(reader, denyResolving);
                            return XmlReadMode.ReadSchema; //since the top level element is a schema return
                        }

                        if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI.StartsWith(Keywords.XSD_NS_START, StringComparison.Ordinal))
                        {
                            throw ExceptionBuilder.DataSetUnsupportedSchema(Keywords.XSDNS);
                        }

                        // now either the top level node is a table and we load it through dataReader...

                        // ... or backup the top node and all its attributes because we may need to InferSchema
                        XmlElement topNode = xdoc.CreateElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                        if (reader.HasAttributes)
                        {
                            int attrCount = reader.AttributeCount;
                            for (int i = 0; i < attrCount; i++)
                            {
                                reader.MoveToAttribute(i);
                                if (reader.NamespaceURI.Equals(Keywords.XSD_XMLNS_NS))
                                    topNode.SetAttribute(reader.Name, reader.GetAttribute(i));
                                else
                                {
                                    XmlAttribute attr = topNode.SetAttributeNode(reader.LocalName, reader.NamespaceURI);
                                    attr.Prefix = reader.Prefix;
                                    attr.Value = reader.GetAttribute(i);
                                }
                            }
                        }
                        reader.Read();
                        string rootNodeSimpleContent = reader.Value;

                        while (MoveToElement(reader, iCurrentDepth))
                        {
                            if ((reader.LocalName == Keywords.DIFFGRAM) && (reader.NamespaceURI == Keywords.DFFNS))
                            {
                                ReadXmlDiffgram(reader);
                                // read the closing tag of the current element
                                // YUKON FIX                            ReadEndElement(reader);
                                //                            return XmlReadMode.DiffGram;
                                ret = XmlReadMode.DiffGram; // continue reading for multiple schemas
                            }

                            // if reader points to the schema load it...


                            if (!fSchemaFound && !fDataFound && reader.LocalName == Keywords.XDR_SCHEMA && reader.NamespaceURI == Keywords.XDRNS)
                            {
                                // load XDR schema and exit
                                ReadXDRSchema(reader);
                                fSchemaFound = true;
                                fIsXdr = true;
                                continue;
                            }

                            if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI == Keywords.XSDNS)
                            {
                                // load XSD schema and exit
                                ReadXSDSchema(reader, denyResolving);
                                fSchemaFound = true;
                                continue;
                            }

                            if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI.StartsWith(Keywords.XSD_NS_START, StringComparison.Ordinal))
                            {
                                throw ExceptionBuilder.DataSetUnsupportedSchema(Keywords.XSDNS);
                            }

                            if ((reader.LocalName == Keywords.DIFFGRAM) && (reader.NamespaceURI == Keywords.DFFNS))
                            {
                                ReadXmlDiffgram(reader);
                                fDiffsFound = true;
                                ret = XmlReadMode.DiffGram;
                            }
                            else
                            {
                                // We have found data IFF the reader.NodeType == Element and reader.depth == currentDepth-1
                                // if reader.NodeType == whitespace, skip all whitespace.
                                // skip processing i.e. continue if the first non-whitespace node is not of type element.
                                while (!reader.EOF && reader.NodeType == XmlNodeType.Whitespace)
                                    reader.Read();
                                if (reader.NodeType != XmlNodeType.Element)
                                    continue;
                                // we found data here
                                fDataFound = true;

                                if (!fSchemaFound && Tables.Count == 0)
                                {
                                    XmlNode node = xdoc.ReadNode(reader);
                                    topNode.AppendChild(node);
                                }
                                else
                                {
                                    if (xmlload == null)
                                    {
                                        xmlload = new XmlDataLoader(this, fIsXdr, topNode, false);
                                    }

                                    xmlload.LoadData(reader);
                                    topNodeIsProcessed = true; // we process the topnode
                                    if (fSchemaFound)
                                    {
                                        ret = XmlReadMode.ReadSchema;
                                    }
                                    else
                                    {
                                        ret = XmlReadMode.IgnoreSchema;
                                    }
                                }
                            }
                        }
                        // read the closing tag of the current element
                        ReadEndElement(reader);
                        bool isfTopLevelTableSet = false;
                        bool tmpValue = _fTopLevelTable;
                        //While inference we ignore root elements text content
                        if (!fSchemaFound && Tables.Count == 0 && !topNode.HasChildNodes)
                        {
                            //We shoule not come add SC of root elemnt to topNode if we are not infering
                            _fTopLevelTable = true;
                            isfTopLevelTableSet = true;
                            if ((rootNodeSimpleContent != null && rootNodeSimpleContent.Length > 0))
                            {
                                topNode.InnerText = rootNodeSimpleContent;
                            }
                        }
                        if (!isEmptyDataSet)
                        {
                            if ((rootNodeSimpleContent != null && rootNodeSimpleContent.Length > 0))
                            {
                                topNode.InnerText = rootNodeSimpleContent;
                            }
                        }

                        // now top node contains the data part
                        xdoc.AppendChild(topNode);

                        if (xmlload == null)
                        {
                            xmlload = new XmlDataLoader(this, fIsXdr, topNode, false);
                        }

                        if (!isEmptyDataSet && !topNodeIsProcessed)
                        {
                            XmlElement root = xdoc.DocumentElement;
                            Debug.Assert(root.NamespaceURI != null, "root.NamespaceURI should not ne null, it should be empty string");
                            // just recognize that below given Xml represents datatable in toplevel
                            //<table attr1="foo" attr2="bar" table_Text="junk">text</table>
                            // only allow root element with simple content, if any
                            if (root.ChildNodes.Count == 0 || ((root.ChildNodes.Count == 1) && root.FirstChild.GetType() == typeof(System.Xml.XmlText)))
                            {
                                bool initfTopLevelTable = _fTopLevelTable;
                                // if root element maps to a datatable
                                // ds and dt cant have the samm name and ns at the same time, how to write to xml
                                if (DataSetName != root.Name && _namespaceURI != root.NamespaceURI &&
                                    Tables.Contains(root.Name, (root.NamespaceURI.Length == 0) ? null : root.NamespaceURI, false, true))
                                {
                                    _fTopLevelTable = true;
                                }
                                try
                                {
                                    xmlload.LoadData(xdoc);
                                }
                                finally
                                {
                                    _fTopLevelTable = initfTopLevelTable; // this is not for inference, we have schema and we were skipping
                                    // topnode where it was a datatable, We must restore the value
                                }
                            }
                        }

                        // above check and below check are orthogonal
                        // so we InferSchema
                        if (!fDiffsFound)
                        {
                            // Load Data
                            if (!fSchemaFound && Tables.Count == 0)
                            {
                                InferSchema(xdoc, null, XmlReadMode.Auto);
                                ret = XmlReadMode.InferSchema;
                                xmlload.FromInference = true;
                                try
                                {
                                    xmlload.LoadData(xdoc);
                                }
                                finally
                                {
                                    xmlload.FromInference = false;
                                }
                            }
                            //We dont need this assignement. Once we set it(where we set it during inference), it won't be changed
                            if (isfTopLevelTableSet)
                                _fTopLevelTable = tmpValue;
                        }
                    }

                    return ret;
                }
                finally
                {
                    rowDiffIdUsage.Cleanup();
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public XmlReadMode ReadXml(Stream stream)
        {
            if (stream == null)
            {
                return XmlReadMode.Auto;
            }

            XmlTextReader xr = new XmlTextReader(stream);

            // Prevent Dtd entity in dataset 
            xr.XmlResolver = null;

            return ReadXml(xr, false);
        }

        public XmlReadMode ReadXml(TextReader reader)
        {
            if (reader == null)
            {
                return XmlReadMode.Auto;
            }

            XmlTextReader xr = new XmlTextReader(reader);

            // Prevent Dtd entity in dataset 
            xr.XmlResolver = null;

            return ReadXml(xr, false);
        }

        public XmlReadMode ReadXml(string fileName)
        {
            XmlTextReader xr = new XmlTextReader(fileName);

            // Prevent Dtd entity in dataset 
            xr.XmlResolver = null;

            try
            {
                return ReadXml(xr, false);
            }
            finally
            {
                xr.Close();
            }
        }

        internal void InferSchema(XmlDocument xdoc, string[] excludedNamespaces, XmlReadMode mode)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.InferSchema|INFO> {0}, mode={1}", ObjectID, mode);
            try
            {
                string ns = xdoc.DocumentElement.NamespaceURI;
                if (null == excludedNamespaces)
                {
                    excludedNamespaces = Array.Empty<string>();
                }
                XmlNodeReader xnr = new XmlIgnoreNamespaceReader(xdoc, excludedNamespaces);
                XmlSchemaInference infer = new XmlSchemaInference();

                infer.Occurrence = XmlSchemaInference.InferenceOption.Relaxed;

                infer.TypeInference = (mode == XmlReadMode.InferTypedSchema) ?
                    XmlSchemaInference.InferenceOption.Restricted :
                    XmlSchemaInference.InferenceOption.Relaxed;

                XmlSchemaSet schemaSet = infer.InferSchema(xnr);
                schemaSet.Compile();

                XSDSchema schema = new XSDSchema();
                schema.FromInference = true;

                try
                {
                    schema.LoadSchema(schemaSet, this);
                }
                finally
                {
                    schema.FromInference = false; // this is always false if you are not calling fron inference
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        private bool IsEmpty()
        {
            foreach (DataTable table in Tables)
            {
                if (table.Rows.Count > 0)
                {
                    return false;
                }
            }
            return true;
        }

        private void ReadXmlDiffgram(XmlReader reader)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.ReadXmlDiffgram|INFO> {0}", ObjectID);
            try
            {
                int d = reader.Depth;
                bool fEnforce = EnforceConstraints;
                EnforceConstraints = false;
                DataSet newDs;
                bool isEmpty = IsEmpty();

                if (isEmpty)
                {
                    newDs = this;
                }
                else
                {
                    newDs = Clone();
                    newDs.EnforceConstraints = false;
                }

                foreach (DataTable t in newDs.Tables)
                {
                    t.Rows._nullInList = 0;
                }
                reader.MoveToContent();
                if ((reader.LocalName != Keywords.DIFFGRAM) && (reader.NamespaceURI != Keywords.DFFNS))
                {
                    return;
                }

                reader.Read();
                if (reader.NodeType == XmlNodeType.Whitespace)
                {
                    MoveToElement(reader, reader.Depth - 1 /*iCurrentDepth*/); // skip over whitespace.
                }

                newDs._fInLoadDiffgram = true;

                if (reader.Depth > d)
                {
                    if ((reader.NamespaceURI != Keywords.DFFNS) && (reader.NamespaceURI != Keywords.MSDNS))
                    {
                        //we should be inside the dataset part
                        XmlDocument xdoc = new XmlDocument();
                        XmlElement node = xdoc.CreateElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                        reader.Read();
                        if (reader.NodeType == XmlNodeType.Whitespace)
                        {
                            MoveToElement(reader, reader.Depth - 1 /*iCurrentDepth*/); // skip over whitespace.
                        }
                        if (reader.Depth - 1 > d)
                        {
                            XmlDataLoader xmlload = new XmlDataLoader(newDs, false, node, false);
                            xmlload._isDiffgram = true; // turn on the special processing
                            xmlload.LoadData(reader);
                        }
                        ReadEndElement(reader);
                        if (reader.NodeType == XmlNodeType.Whitespace)
                        {
                            MoveToElement(reader, reader.Depth - 1 /*iCurrentDepth*/); // skip over whitespace.
                        }
                    }
                    Debug.Assert(reader.NodeType != XmlNodeType.Whitespace, "Should not be on Whitespace node");

                    if (((reader.LocalName == Keywords.SQL_BEFORE) && (reader.NamespaceURI == Keywords.DFFNS)) ||
                        ((reader.LocalName == Keywords.MSD_ERRORS) && (reader.NamespaceURI == Keywords.DFFNS)))
                    {
                        //this will consume the changes and the errors part
                        XMLDiffLoader diffLoader = new XMLDiffLoader();
                        diffLoader.LoadDiffGram(newDs, reader);
                    }

                    // get to the closing diff tag
                    while (reader.Depth > d)
                    {
                        reader.Read();
                    }
                    // read the closing tag
                    ReadEndElement(reader);
                }

                foreach (DataTable t in newDs.Tables)
                {
                    if (t.Rows._nullInList > 0)
                    {
                        throw ExceptionBuilder.RowInsertMissing(t.TableName);
                    }
                }

                newDs._fInLoadDiffgram = false;

                //terrible performance!
                foreach (DataTable t in newDs.Tables)
                {
                    DataRelation[] nestedParentRelations = t.NestedParentRelations;
                    foreach (DataRelation rel in nestedParentRelations)
                    {
                        if (rel.ParentTable == t)
                        {
                            foreach (DataRow r in t.Rows)
                            {
                                foreach (DataRelation rel2 in nestedParentRelations)
                                {
                                    r.CheckForLoops(rel2);
                                }
                            }
                        }
                    }
                }

                if (!isEmpty)
                {
                    Merge(newDs);
                    if (_dataSetName == "NewDataSet")
                    {
                        _dataSetName = newDs._dataSetName;
                    }
                    newDs.EnforceConstraints = fEnforce;
                }
                EnforceConstraints = fEnforce;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// </summary>
        public XmlReadMode ReadXml(XmlReader reader, XmlReadMode mode) => ReadXml(reader, mode, false);

        internal XmlReadMode ReadXml(XmlReader reader, XmlReadMode mode, bool denyResolving)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.ReadXml|INFO> {0}, mode={1}, denyResolving={2}", ObjectID, mode, denyResolving);
            try
            {
                XmlReadMode ret = mode;

                if (reader == null)
                {
                    return ret;
                }

                if (mode == XmlReadMode.Auto)
                {
                    // nested ReadXml calls on the same DataSet must be done outside of RowDiffIdUsage scope
                    return ReadXml(reader);
                }

                DataTable.DSRowDiffIdUsageSection rowDiffIdUsage = new DataTable.DSRowDiffIdUsageSection();
                try
                {
                    bool fSchemaFound = false;
                    bool fDataFound = false;
                    bool fIsXdr = false;
                    int iCurrentDepth = -1;

                    // prepare and cleanup rowDiffId hashtable
                    rowDiffIdUsage.Prepare(this);

                    if (reader is XmlTextReader)
                    {
                        ((XmlTextReader)reader).WhitespaceHandling = WhitespaceHandling.Significant;
                    }

                    XmlDocument xdoc = new XmlDocument(); // we may need this to infer the schema

                    if ((mode != XmlReadMode.Fragment) && (reader.NodeType == XmlNodeType.Element))
                    {
                        iCurrentDepth = reader.Depth;
                    }

                    reader.MoveToContent();
                    XmlDataLoader xmlload = null;

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        XmlElement topNode = null;
                        if (mode == XmlReadMode.Fragment)
                        {
                            xdoc.AppendChild(xdoc.CreateElement("ds_sqlXmlWraPPeR"));
                            topNode = xdoc.DocumentElement;
                        }
                        else
                        {
                            //handle the top node
                            if ((reader.LocalName == Keywords.DIFFGRAM) && (reader.NamespaceURI == Keywords.DFFNS))
                            {
                                if ((mode == XmlReadMode.DiffGram) || (mode == XmlReadMode.IgnoreSchema))
                                {
                                    ReadXmlDiffgram(reader);
                                    // read the closing tag of the current element
                                    ReadEndElement(reader);
                                }
                                else
                                {
                                    reader.Skip();
                                }
                                return ret;
                            }

                            if (reader.LocalName == Keywords.XDR_SCHEMA && reader.NamespaceURI == Keywords.XDRNS)
                            {
                                // load XDR schema and exit
                                if ((mode != XmlReadMode.IgnoreSchema) && (mode != XmlReadMode.InferSchema) &&
                                    (mode != XmlReadMode.InferTypedSchema))
                                {
                                    ReadXDRSchema(reader);
                                }
                                else
                                {
                                    reader.Skip();
                                }
                                return ret; //since the top level element is a schema return
                            }

                            if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI == Keywords.XSDNS)
                            {
                                // load XSD schema and exit
                                if ((mode != XmlReadMode.IgnoreSchema) && (mode != XmlReadMode.InferSchema) &&
                                    (mode != XmlReadMode.InferTypedSchema))
                                {
                                    ReadXSDSchema(reader, denyResolving);
                                }
                                else
                                {
                                    reader.Skip();
                                }

                                return ret; //since the top level element is a schema return
                            }

                            if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI.StartsWith(Keywords.XSD_NS_START, StringComparison.Ordinal))
                            {
                                throw ExceptionBuilder.DataSetUnsupportedSchema(Keywords.XSDNS);
                            }

                            // now either the top level node is a table and we load it through dataReader...
                            // ... or backup the top node and all its attributes
                            topNode = xdoc.CreateElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                            if (reader.HasAttributes)
                            {
                                int attrCount = reader.AttributeCount;
                                for (int i = 0; i < attrCount; i++)
                                {
                                    reader.MoveToAttribute(i);
                                    if (reader.NamespaceURI.Equals(Keywords.XSD_XMLNS_NS))
                                        topNode.SetAttribute(reader.Name, reader.GetAttribute(i));
                                    else
                                    {
                                        XmlAttribute attr = topNode.SetAttributeNode(reader.LocalName, reader.NamespaceURI);
                                        attr.Prefix = reader.Prefix;
                                        attr.Value = reader.GetAttribute(i);
                                    }
                                }
                            }
                            reader.Read();
                        }

                        while (MoveToElement(reader, iCurrentDepth))
                        {
                            if (reader.LocalName == Keywords.XDR_SCHEMA && reader.NamespaceURI == Keywords.XDRNS)
                            {
                                // load XDR schema
                                if (!fSchemaFound && !fDataFound && (mode != XmlReadMode.IgnoreSchema) && (mode != XmlReadMode.InferSchema) &&
                                    (mode != XmlReadMode.InferTypedSchema))
                                {
                                    ReadXDRSchema(reader);
                                    fSchemaFound = true;
                                    fIsXdr = true;
                                }
                                else
                                {
                                    reader.Skip();
                                }
                                continue;
                            }

                            if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI == Keywords.XSDNS)
                            {
                                // load XSD schema and exit
                                if ((mode != XmlReadMode.IgnoreSchema) && (mode != XmlReadMode.InferSchema) &&
                                    (mode != XmlReadMode.InferTypedSchema))
                                {
                                    ReadXSDSchema(reader, denyResolving);
                                    fSchemaFound = true;
                                }
                                else
                                {
                                    reader.Skip();
                                }
                                continue;
                            }

                            if ((reader.LocalName == Keywords.DIFFGRAM) && (reader.NamespaceURI == Keywords.DFFNS))
                            {
                                if ((mode == XmlReadMode.DiffGram) || (mode == XmlReadMode.IgnoreSchema))
                                {
                                    ReadXmlDiffgram(reader);
                                    ret = XmlReadMode.DiffGram;
                                }
                                else
                                {
                                    reader.Skip();
                                }
                                continue;
                            }

                            if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI.StartsWith(Keywords.XSD_NS_START, StringComparison.Ordinal))
                                throw ExceptionBuilder.DataSetUnsupportedSchema(Keywords.XSDNS);

                            if (mode == XmlReadMode.DiffGram)
                            {
                                reader.Skip();
                                continue; // we do not read data in diffgram mode
                            }

                            // if we are here we found some data
                            fDataFound = true;

                            if (mode == XmlReadMode.InferSchema || mode == XmlReadMode.InferTypedSchema)
                            { //save the node in DOM until the end;
                                XmlNode node = xdoc.ReadNode(reader);
                                topNode.AppendChild(node);
                            }
                            else
                            {
                                if (xmlload == null)
                                {
                                    xmlload = new XmlDataLoader(this, fIsXdr, topNode, mode == XmlReadMode.IgnoreSchema);
                                }
                                xmlload.LoadData(reader);
                            }
                        } //end of the while

                        // read the closing tag of the current element
                        ReadEndElement(reader);

                        // now top node contains the data part
                        xdoc.AppendChild(topNode);
                        if (xmlload == null)
                            xmlload = new XmlDataLoader(this, fIsXdr, mode == XmlReadMode.IgnoreSchema);

                        if (mode == XmlReadMode.DiffGram)
                        {
                            // we already got the diffs through XmlReader interface
                            return ret;
                        }

                        // Load Data
                        if (mode == XmlReadMode.InferSchema || mode == XmlReadMode.InferTypedSchema)
                        {
                            InferSchema(xdoc, null, mode);
                            ret = XmlReadMode.InferSchema;
                            xmlload.FromInference = true;

                            try
                            {
                                xmlload.LoadData(xdoc);
                            }
                            finally
                            {
                                xmlload.FromInference = false;
                            }
                        }
                    }

                    return ret;
                }
                finally
                {
                    // prepare and cleanup rowDiffId hashtable
                    rowDiffIdUsage.Cleanup();
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public XmlReadMode ReadXml(Stream stream, XmlReadMode mode)
        {
            if (stream == null)
            {
                return XmlReadMode.Auto;
            }

            XmlTextReader reader = (mode == XmlReadMode.Fragment) ? new XmlTextReader(stream, XmlNodeType.Element, null) : new XmlTextReader(stream);
            // Prevent Dtd entity in dataset 
            reader.XmlResolver = null;
            return ReadXml(reader, mode, false);
        }

        public XmlReadMode ReadXml(TextReader reader, XmlReadMode mode)
        {
            if (reader == null)
            {
                return XmlReadMode.Auto;
            }

            XmlTextReader xmlreader = (mode == XmlReadMode.Fragment) ? new XmlTextReader(reader.ReadToEnd(), XmlNodeType.Element, null) : new XmlTextReader(reader);
            // Prevent Dtd entity in dataset 
            xmlreader.XmlResolver = null;
            return ReadXml(xmlreader, mode, false);
        }

        public XmlReadMode ReadXml(string fileName, XmlReadMode mode)
        {
            XmlTextReader xr = null;
            if (mode == XmlReadMode.Fragment)
            {
                FileStream stream = new FileStream(fileName, FileMode.Open);
                xr = new XmlTextReader(stream, XmlNodeType.Element, null);
            }
            else
            {
                xr = new XmlTextReader(fileName);
            }

            // Prevent Dtd entity in dataset             
            xr.XmlResolver = null;

            try
            {
                return ReadXml(xr, mode, false);
            }
            finally
            {
                xr.Close();
            }
        }

        public void WriteXml(Stream stream) => WriteXml(stream, XmlWriteMode.IgnoreSchema);

        public void WriteXml(TextWriter writer) => WriteXml(writer, XmlWriteMode.IgnoreSchema);

        public void WriteXml(XmlWriter writer) => WriteXml(writer, XmlWriteMode.IgnoreSchema);

        public void WriteXml(string fileName) => WriteXml(fileName, XmlWriteMode.IgnoreSchema);

        /// <summary>
        /// Writes schema and data for the DataSet.
        /// </summary>
        public void WriteXml(Stream stream, XmlWriteMode mode)
        {
            if (stream != null)
            {
                XmlTextWriter w = new XmlTextWriter(stream, null);
                w.Formatting = Formatting.Indented;

                WriteXml(w, mode);
            }
        }

        public void WriteXml(TextWriter writer, XmlWriteMode mode)
        {
            if (writer != null)
            {
                XmlTextWriter w = new XmlTextWriter(writer);
                w.Formatting = Formatting.Indented;

                WriteXml(w, mode);
            }
        }

        public void WriteXml(XmlWriter writer, XmlWriteMode mode)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.WriteXml|API> {0}, mode={1}", ObjectID, mode);
            try
            {
                // Generate SchemaTree and write it out
                if (writer != null)
                {
                    if (mode == XmlWriteMode.DiffGram)
                    {
                        // Create and save the updates
                        new NewDiffgramGen(this).Save(writer);
                    }
                    else
                    {
                        // Create and save xml data
                        new XmlDataTreeWriter(this).Save(writer, mode == XmlWriteMode.WriteSchema);
                    }
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public void WriteXml(string fileName, XmlWriteMode mode)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.WriteXml|API> {0}, fileName='{1}', mode={2}", ObjectID, fileName, (int)mode);
            XmlTextWriter xw = new XmlTextWriter(fileName, null);
            try
            {
                xw.Formatting = Formatting.Indented;
                xw.WriteStartDocument(true);
                if (xw != null)
                {
                    // Create and save the updates
                    if (mode == XmlWriteMode.DiffGram)
                    {
                        new NewDiffgramGen(this).Save(xw);
                    }
                    else
                    {
                        // Create and save xml data
                        new XmlDataTreeWriter(this).Save(xw, mode == XmlWriteMode.WriteSchema);
                    }
                }
                xw.WriteEndDocument();
            }
            finally
            {
                xw.Close();
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Gets the collection of parent relations which belong to a
        /// specified table.
        /// </summary>
        internal DataRelationCollection GetParentRelations(DataTable table) => table.ParentRelations;

        /// <summary>
        /// Merges this <see cref='System.Data.DataSet'/> into a specified <see cref='System.Data.DataSet'/>.
        /// </summary>
        public void Merge(DataSet dataSet)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Merge|API> {0}, dataSet={1}", ObjectID, (dataSet != null) ? dataSet.ObjectID : 0);
            try
            {
                Merge(dataSet, false, MissingSchemaAction.Add);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Merges this <see cref='System.Data.DataSet'/> into a specified <see cref='System.Data.DataSet'/> preserving changes according to
        /// the specified argument.
        /// </summary>
        public void Merge(DataSet dataSet, bool preserveChanges)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Merge|API> {0}, dataSet={1}, preserveChanges={2}", ObjectID, (dataSet != null) ? dataSet.ObjectID : 0, preserveChanges);
            try
            {
                Merge(dataSet, preserveChanges, MissingSchemaAction.Add);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Merges this <see cref='System.Data.DataSet'/> into a specified <see cref='System.Data.DataSet'/> preserving changes according to
        /// the specified argument, and handling an incompatible schema according to the
        /// specified argument.
        /// </summary>
        public void Merge(DataSet dataSet, bool preserveChanges, MissingSchemaAction missingSchemaAction)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Merge|API> {0}, dataSet={1}, preserveChanges={2}, missingSchemaAction={3}", ObjectID, (dataSet != null) ? dataSet.ObjectID : 0, preserveChanges, missingSchemaAction);
            try
            {
                // Argument checks
                if (dataSet == null)
                {
                    throw ExceptionBuilder.ArgumentNull(nameof(dataSet));
                }

                switch (missingSchemaAction)
                {
                    case MissingSchemaAction.Add:
                    case MissingSchemaAction.Ignore:
                    case MissingSchemaAction.Error:
                    case MissingSchemaAction.AddWithKey:
                        Merger merger = new Merger(this, preserveChanges, missingSchemaAction);
                        merger.MergeDataSet(dataSet);
                        break;
                    default:
                        throw ADP.InvalidMissingSchemaAction(missingSchemaAction);
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Merges this <see cref='System.Data.DataTable'/> into a specified <see cref='System.Data.DataTable'/>.
        /// </summary>
        public void Merge(DataTable table)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Merge|API> {0}, table={1}", ObjectID, (table != null) ? table.ObjectID : 0);
            try
            {
                Merge(table, false, MissingSchemaAction.Add);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Merges this <see cref='System.Data.DataTable'/> into a specified <see cref='System.Data.DataTable'/>. with a value to preserve changes
        /// made to the target, and a value to deal with missing schemas.
        /// </summary>
        public void Merge(DataTable table, bool preserveChanges, MissingSchemaAction missingSchemaAction)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Merge|API> {0}, table={1}, preserveChanges={2}, missingSchemaAction={3}", ObjectID, (table != null) ? table.ObjectID : 0, preserveChanges, missingSchemaAction);
            try
            {
                // Argument checks
                if (table == null)
                {
                    throw ExceptionBuilder.ArgumentNull(nameof(table));
                }

                switch (missingSchemaAction)
                {
                    case MissingSchemaAction.Add:
                    case MissingSchemaAction.Ignore:
                    case MissingSchemaAction.Error:
                    case MissingSchemaAction.AddWithKey:
                        Merger merger = new Merger(this, preserveChanges, missingSchemaAction);
                        merger.MergeTable(table);
                        break;
                    default:
                        throw ADP.InvalidMissingSchemaAction(missingSchemaAction);
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public void Merge(DataRow[] rows)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Merge|API> {0}, rows", ObjectID);
            try
            {
                Merge(rows, false, MissingSchemaAction.Add);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public void Merge(DataRow[] rows, bool preserveChanges, MissingSchemaAction missingSchemaAction)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Merge|API> {0}, preserveChanges={1}, missingSchemaAction={2}", ObjectID, preserveChanges, missingSchemaAction);
            try
            {
                // Argument checks
                if (rows == null)
                {
                    throw ExceptionBuilder.ArgumentNull(nameof(rows));
                }

                switch (missingSchemaAction)
                {
                    case MissingSchemaAction.Add:
                    case MissingSchemaAction.Ignore:
                    case MissingSchemaAction.Error:
                    case MissingSchemaAction.AddWithKey:
                        Merger merger = new Merger(this, preserveChanges, missingSchemaAction);
                        merger.MergeRows(rows);
                        break;
                    default:
                        throw ADP.InvalidMissingSchemaAction(missingSchemaAction);
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        protected virtual void OnPropertyChanging(PropertyChangedEventArgs pcevent)
        {
            PropertyChanging?.Invoke(this, pcevent);
        }

        /// <summary>
        /// Inheriting classes should override this method to handle this event.
        /// Call base.OnMergeFailed to send this event to any registered event
        /// listeners.
        /// </summary>
        internal void OnMergeFailed(MergeFailedEventArgs mfevent)
        {
            if (MergeFailed != null)
            {
                MergeFailed(this, mfevent);
            }
            else
            {
                throw ExceptionBuilder.MergeFailed(mfevent.Conflict);
            }
        }

        internal void RaiseMergeFailed(DataTable table, string conflict, MissingSchemaAction missingSchemaAction)
        {
            if (MissingSchemaAction.Error == missingSchemaAction)
            {
                throw ExceptionBuilder.MergeFailed(conflict);
            }

            OnMergeFailed(new MergeFailedEventArgs(table, conflict));
        }

        internal void OnDataRowCreated(DataRow row) => DataRowCreated?.Invoke(this, row);

        internal void OnClearFunctionCalled(DataTable table) => ClearFunctionCalled?.Invoke(this, table);

        private void OnInitialized() => Initialized?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// This method should be overridden by subclasses to restrict tables being removed.
        /// </summary>
        protected internal virtual void OnRemoveTable(DataTable table) { }

        internal void OnRemovedTable(DataTable table)
        {
            DataViewManager viewManager = _defaultViewManager;
            if (null != viewManager)
            {
                viewManager.DataViewSettings.Remove(table);
            }
        }

        /// <summary>
        /// This method should be overridden by subclasses to restrict tables being removed.
        /// </summary>
        protected virtual void OnRemoveRelation(DataRelation relation) { }

        internal void OnRemoveRelationHack(DataRelation relation) => OnRemoveRelation(relation);

        protected internal void RaisePropertyChanging(string name) => OnPropertyChanging(new PropertyChangedEventArgs(name));

        internal DataTable[] TopLevelTables() => TopLevelTables(false);

        internal DataTable[] TopLevelTables(bool forSchema)
        {
            // first let's figure out if we can represent the given dataSet as a tree using
            // the fact that all connected undirected graphs with n-1 edges are trees.
            List<DataTable> topTables = new List<DataTable>();

            if (forSchema)
            {
                // prepend the tables that are nested more than once
                for (int i = 0; i < Tables.Count; i++)
                {
                    DataTable table = Tables[i];
                    if (table.NestedParentsCount > 1 || table.SelfNested)
                    {
                        topTables.Add(table);
                    }
                }
            }
            for (int i = 0; i < Tables.Count; i++)
            {
                DataTable table = Tables[i];
                if (table.NestedParentsCount == 0 && !topTables.Contains(table))
                {
                    topTables.Add(table);
                }
            }

            return topTables.Count == 0 ?
                Array.Empty<DataTable>() :
                topTables.ToArray();
        }

        /// <summary>
        /// This method rolls back all the changes to have been made to this DataSet since
        /// it was loaded or the last time AcceptChanges was called.
        /// Any rows still in edit-mode cancel their edits.  New rows get removed.  Modified and
        /// Deleted rows return back to their original state.
        /// </summary>
        public virtual void RejectChanges()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.RejectChanges|API> {0}", ObjectID);
            try
            {
                bool fEnforce = EnforceConstraints;
                EnforceConstraints = false;
                for (int i = 0; i < Tables.Count; i++)
                {
                    Tables[i].RejectChanges();
                }
                EnforceConstraints = fEnforce;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Resets the dataSet back to it's original state.  Subclasses should override
        /// to restore back to it's original state.
        /// </summary>
        public virtual void Reset()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Reset|API> {0}", ObjectID);
            try
            {
                for (int i = 0; i < Tables.Count; i++)
                {
                    ConstraintCollection cons = Tables[i].Constraints;
                    for (int j = 0; j < cons.Count;)
                    {
                        if (cons[j] is ForeignKeyConstraint)
                        {
                            cons.Remove(cons[j]);
                        }
                        else
                        {
                            j++;
                        }
                    }
                }

                Clear();
                Relations.Clear();
                Tables.Clear();
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal bool ValidateCaseConstraint()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.ValidateCaseConstraint|INFO> {0}", ObjectID);
            try
            {
                DataRelation relation = null;
                for (int i = 0; i < Relations.Count; i++)
                {
                    relation = Relations[i];
                    if (relation.ChildTable.CaseSensitive != relation.ParentTable.CaseSensitive)
                    {
                        return false;
                    }
                }

                ForeignKeyConstraint constraint = null;
                ConstraintCollection constraints = null;
                for (int i = 0; i < Tables.Count; i++)
                {
                    constraints = Tables[i].Constraints;
                    for (int j = 0; j < constraints.Count; j++)
                    {
                        if (constraints[j] is ForeignKeyConstraint)
                        {
                            constraint = (ForeignKeyConstraint)constraints[j];
                            if (constraint.Table.CaseSensitive != constraint.RelatedTable.CaseSensitive)
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal bool ValidateLocaleConstraint()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.ValidateLocaleConstraint|INFO> {0}", ObjectID);
            try
            {
                DataRelation relation = null;
                for (int i = 0; i < Relations.Count; i++)
                {
                    relation = Relations[i];
                    if (relation.ChildTable.Locale.LCID != relation.ParentTable.Locale.LCID)
                    {
                        return false;
                    }
                }

                ForeignKeyConstraint constraint = null;
                ConstraintCollection constraints = null;
                for (int i = 0; i < Tables.Count; i++)
                {
                    constraints = Tables[i].Constraints;
                    for (int j = 0; j < constraints.Count; j++)
                    {
                        if (constraints[j] is ForeignKeyConstraint)
                        {
                            constraint = (ForeignKeyConstraint)constraints[j];
                            if (constraint.Table.Locale.LCID != constraint.RelatedTable.Locale.LCID)
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        // SDUB: may be better to rewrite this as nonrecursive?
        internal DataTable FindTable(DataTable baseTable, PropertyDescriptor[] props, int propStart)
        {
            if (props.Length < propStart + 1)
            {
                return baseTable;
            }

            PropertyDescriptor currentProp = props[propStart];

            if (baseTable == null)
            {
                // the accessor is the table name.  if we don't find it, return null.
                if (currentProp is DataTablePropertyDescriptor)
                {
                    return FindTable(((DataTablePropertyDescriptor)currentProp).Table, props, propStart + 1);
                }
                return null;
            }

            if (currentProp is DataRelationPropertyDescriptor)
            {
                return FindTable(((DataRelationPropertyDescriptor)currentProp).Relation.ChildTable, props, propStart + 1);
            }

            return null;
        }

        protected virtual void ReadXmlSerializable(XmlReader reader)
        {
            // <DataSet xsi:nil="true"> does not mean DataSet is null,but it does not have any child
            // so  dont do anything, ignore the attributes and just return empty DataSet;
            _useDataSetSchemaOnly = false;
            _udtIsWrapped = false;

            if (reader.HasAttributes)
            {
                const string xsinill = Keywords.XSI + ":" + Keywords.XSI_NIL;
                if (reader.MoveToAttribute(xsinill))
                {
                    string nilAttrib = reader.GetAttribute(xsinill);
                    if (string.Equals(nilAttrib, "true", StringComparison.Ordinal))
                    {
                        // case sensitive true comparison
                        MoveToElement(reader, 1);
                        return;
                    }
                }

                const string UseDataSetSchemaOnlyString = Keywords.MSD + ":" + Keywords.USEDATASETSCHEMAONLY;
                if (reader.MoveToAttribute(UseDataSetSchemaOnlyString))
                {
                    string useDataSetSchemaOnly = reader.GetAttribute(UseDataSetSchemaOnlyString);
                    if (string.Equals(useDataSetSchemaOnly, "true", StringComparison.Ordinal) ||
                        string.Equals(useDataSetSchemaOnly, "1", StringComparison.Ordinal))
                    {
                        _useDataSetSchemaOnly = true;
                    }
                    else if (!string.Equals(useDataSetSchemaOnly, "false", StringComparison.Ordinal) &&
                             !string.Equals(useDataSetSchemaOnly, "0", StringComparison.Ordinal))
                    {
                        throw ExceptionBuilder.InvalidAttributeValue(Keywords.USEDATASETSCHEMAONLY, useDataSetSchemaOnly);
                    }
                }

                const string udtIsWrappedString = Keywords.MSD + ":" + Keywords.UDTCOLUMNVALUEWRAPPED;
                if (reader.MoveToAttribute(udtIsWrappedString))
                {
                    string _udtIsWrappedString = reader.GetAttribute(udtIsWrappedString);
                    if (string.Equals(_udtIsWrappedString, "true", StringComparison.Ordinal) ||
                        string.Equals(_udtIsWrappedString, "1", StringComparison.Ordinal))
                    {
                        _udtIsWrapped = true;
                    }
                    else if (!string.Equals(_udtIsWrappedString, "false", StringComparison.Ordinal) &&
                             !string.Equals(_udtIsWrappedString, "0", StringComparison.Ordinal))
                    {
                        throw ExceptionBuilder.InvalidAttributeValue(Keywords.UDTCOLUMNVALUEWRAPPED, _udtIsWrappedString);
                    }
                }
            }
            ReadXml(reader, XmlReadMode.DiffGram, true);
        }

        protected virtual System.Xml.Schema.XmlSchema GetSchemaSerializable() => null;

        public static XmlSchemaComplexType GetDataSetSchema(XmlSchemaSet schemaSet)
        {
            // For performance resons we are exploiting the fact that config files content is constant 
            // for a given appdomain so we can safely cache the prepared schema complex type and reuse it
            if (s_schemaTypeForWSDL == null)
            {
                // to change the config file, appdomain needs to restart; so it seems safe to cache the schema
                XmlSchemaComplexType tempWSDL = new XmlSchemaComplexType();
                XmlSchemaSequence sequence = new XmlSchemaSequence();

                XmlSchemaAny any = new XmlSchemaAny();
                any.Namespace = XmlSchema.Namespace;
                any.MinOccurs = 0;
                any.ProcessContents = XmlSchemaContentProcessing.Lax;
                sequence.Items.Add(any);

                any = new XmlSchemaAny();
                any.Namespace = Keywords.DFFNS;
                any.MinOccurs = 0; // when recognizing WSDL - MinOccurs="0" denotes DataSet, a MinOccurs="1" for DataTable
                any.ProcessContents = XmlSchemaContentProcessing.Lax;
                sequence.Items.Add(any);
                sequence.MaxOccurs = decimal.MaxValue;

                tempWSDL.Particle = sequence;

                s_schemaTypeForWSDL = tempWSDL;
            }
            return s_schemaTypeForWSDL;
        }

        private static bool PublishLegacyWSDL() => false;

        XmlSchema IXmlSerializable.GetSchema()
        {
            if (GetType() == typeof(DataSet))
            {
                return null;
            }

            MemoryStream stream = new MemoryStream();
            // WriteXmlSchema(new XmlTextWriter(stream, null));
            XmlWriter writer = new XmlTextWriter(stream, null);
            if (writer != null)
            {
                (new XmlTreeGen(SchemaFormat.WebService)).Save(this, writer);
            }
            stream.Position = 0;
            return XmlSchema.Read(new XmlTextReader(stream), null);
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            bool fNormalization = true;
            XmlTextReader xmlTextReader = null;
            IXmlTextParser xmlTextParser = reader as IXmlTextParser;
            if (xmlTextParser != null)
            {
                fNormalization = xmlTextParser.Normalized;
                xmlTextParser.Normalized = false;
            }
            else
            {
                xmlTextReader = reader as XmlTextReader;
                if (xmlTextReader != null)
                {
                    fNormalization = xmlTextReader.Normalization;
                    xmlTextReader.Normalization = false;
                }
            }

            ReadXmlSerializable(reader);

            if (xmlTextParser != null)
            {
                xmlTextParser.Normalized = fNormalization;
            }
            else if (xmlTextReader != null)
            {
                xmlTextReader.Normalization = fNormalization;
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            WriteXmlSchema(writer, SchemaFormat.WebService, null);
            WriteXml(writer, XmlWriteMode.DiffGram);
        }

        public virtual void Load(IDataReader reader, LoadOption loadOption, FillErrorEventHandler errorHandler, params DataTable[] tables)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Load|API> reader, loadOption={0}", loadOption);
            try
            {
                foreach (DataTable dt in tables)
                {
                    ADP.CheckArgumentNull(dt, nameof(tables));
                    if (dt.DataSet != this)
                    {
                        throw ExceptionBuilder.TableNotInTheDataSet(dt.TableName);
                    }
                }

                var adapter = new LoadAdapter();
                adapter.FillLoadOption = loadOption;
                adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                if (null != errorHandler)
                {
                    adapter.FillError += errorHandler;
                }
                adapter.FillFromReader(tables, reader, 0, 0);

                if (!reader.IsClosed && !reader.NextResult())
                {
                    reader.Close();
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public void Load(IDataReader reader, LoadOption loadOption, params DataTable[] tables) =>
            Load(reader, loadOption, null, tables);

        public void Load(IDataReader reader, LoadOption loadOption, params string[] tables)
        {
            ADP.CheckArgumentNull(tables, nameof(tables));
            var dataTables = new DataTable[tables.Length];
            for (int i = 0; i < tables.Length; i++)
            {
                DataTable tempDT = Tables[tables[i]];
                if (null == tempDT)
                {
                    tempDT = new DataTable(tables[i]);
                    Tables.Add(tempDT);
                }
                dataTables[i] = tempDT;
            }
            Load(reader, loadOption, null, dataTables);
        }

        public DataTableReader CreateDataReader()
        {
            if (Tables.Count == 0)
            {
                throw ExceptionBuilder.CannotCreateDataReaderOnEmptyDataSet();
            }

            var dataTables = new DataTable[Tables.Count];
            for (int i = 0; i < Tables.Count; i++)
            {
                dataTables[i] = Tables[i];
            }
            return CreateDataReader(dataTables);
        }

        public DataTableReader CreateDataReader(params DataTable[] dataTables)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.GetDataReader|API> {0}", ObjectID);
            try
            {
                if (dataTables.Length == 0)
                {
                    throw ExceptionBuilder.DataTableReaderArgumentIsEmpty();
                }

                for (int i = 0; i < dataTables.Length; i++)
                {
                    if (dataTables[i] == null)
                    {
                        throw ExceptionBuilder.ArgumentContainsNullValue();
                    }
                }

                return new DataTableReader(dataTables);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal string MainTableName
        {
            get { return _mainTableName; }
            set { _mainTableName = value; }
        }

        internal int ObjectID => _objectID;
    }
}
