// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Data
{
    /// <summary>
    /// Represents one table of in-memory data.
    /// </summary>
    [ToolboxItem(false)]
    [DesignTimeVisible(false)]
    [DefaultProperty(nameof(TableName))]
    [DefaultEvent(nameof(RowChanging))]
    [Editor("Microsoft.VSDesigner.Data.Design.DataTableEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [XmlSchemaProvider(nameof(GetDataTableSchema))]
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
    public class DataTable : MarshalByValueComponent, IListSource, ISupportInitializeNotification, ISerializable, IXmlSerializable
    {
        private DataSet? _dataSet;
        private DataView? _defaultView;

        /// <summary>
        /// Monotonically increasing number representing the order <see cref="DataRow"/> have been added to <see cref="DataRowCollection"/>.
        /// </summary>
        /// <remarks>This limits <see cref="DataRowCollection.Add(DataRow)"/> to <see cref="int.MaxValue"/> operations.</remarks>
        internal long _nextRowID;
        internal readonly DataRowCollection _rowCollection;

        // columns
        internal readonly DataColumnCollection _columnCollection;

        // constraints
        private readonly ConstraintCollection _constraintCollection;

        //SimpleContent implementation
        private int _elementColumnCount;

        // relations
        internal DataRelationCollection? _parentRelationsCollection;
        internal DataRelationCollection? _childRelationsCollection;

        // RecordManager
        internal readonly RecordManager _recordManager;

        // index mgmt
        internal readonly List<Index> _indexes;

        private List<Index>? _shadowIndexes;
        private int _shadowCount;

        // props
        internal PropertyCollection? _extendedProperties;
        private string _tableName = string.Empty;
        internal string? _tableNamespace;
        private string _tablePrefix = string.Empty;
        internal DataExpression? _displayExpression;
        internal bool _fNestedInDataset = true;

        // globalization stuff
        private CultureInfo _culture;
        private bool _cultureUserSet;
        private CompareInfo? _compareInfo;
        private CompareOptions _compareFlags = CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth;
        private IFormatProvider? _formatProvider;
        private StringComparer? _hashCodeProvider;
        private bool _caseSensitive;
        private bool _caseSensitiveUserSet;

        // XML properties
        internal string? _encodedTableName;           // For XmlDataDocument only
        internal DataColumn? _xmlText;            // text values of a complex xml element
        internal DataColumn? _colUnique;
        internal decimal _minOccurs = 1;    // default = 1
        internal decimal _maxOccurs = 1;    // default = 1
        internal bool _repeatableElement;
        private object? _typeName;

        // primary key info
        internal UniqueConstraint? _primaryKey;
        internal IndexField[] _primaryIndex = Array.Empty<IndexField>();
        private DataColumn[]? _delayedSetPrimaryKey;

        // Loading Schema and/or Data related optimization
        private Index? _loadIndex;
        private Index? _loadIndexwithOriginalAdded;
        private Index? _loadIndexwithCurrentDeleted;
        private int _suspendIndexEvents;

        private bool _savedEnforceConstraints;
        private bool _inDataLoad;
        private bool _initialLoad;
        private bool _enforceConstraints = true;
        internal bool _suspendEnforceConstraints;

        protected internal bool fInitInProgress;
        internal bool _fInLoadDiffgram;

        private byte _isTypedDataTable; // 0 == unknown, 1 = yes, 2 = No
        private DataRow[]? _emptyDataRowArray;

        // Property Descriptor Cache for DataBinding
        private PropertyDescriptorCollection? _propertyDescriptorCollectionCache;

        // Cache for relation that has this table as nested child table.
        private DataRelation[] _nestedParentRelations = Array.Empty<DataRelation>();

        // Dependent column list for expression evaluation
        internal List<DataColumn>? _dependentColumns;

        // events
        private bool _mergingData;
        private DataRowChangeEventHandler? _onRowChangedDelegate;
        private DataRowChangeEventHandler? _onRowChangingDelegate;
        private DataRowChangeEventHandler? _onRowDeletingDelegate;
        private DataRowChangeEventHandler? _onRowDeletedDelegate;
        private DataColumnChangeEventHandler? _onColumnChangedDelegate;
        private DataColumnChangeEventHandler? _onColumnChangingDelegate;
        private DataTableClearEventHandler? _onTableClearingDelegate;
        private DataTableClearEventHandler? _onTableClearedDelegate;
        private DataTableNewRowEventHandler? _onTableNewRowDelegate;
        private PropertyChangedEventHandler? _onPropertyChangingDelegate;

        private EventHandler? _onInitialized;

        // misc
        private readonly DataRowBuilder _rowBuilder;
        private const string KEY_XMLSCHEMA = "XmlSchema";
        private const string KEY_XMLDIFFGRAM = "XmlDiffGram";

        internal readonly List<DataView> _delayedViews = new List<DataView>();
        private readonly List<DataViewListener> _dataViewListeners = new List<DataViewListener>();

        internal Hashtable? _rowDiffId;
        internal readonly ReaderWriterLockSlim _indexesLock = new ReaderWriterLockSlim();
        internal int _ukColumnPositionForInference = -1;

        // default remoting format is Xml
        private SerializationFormat _remotingFormat = SerializationFormat.Xml;

        private static int s_objectTypeCount; // Bid counter
        private readonly int _objectID = System.Threading.Interlocked.Increment(ref s_objectTypeCount);

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DataTable'/> class with no arguments.
        /// </summary>
        public DataTable()
        {
            GC.SuppressFinalize(this);
            DataCommonEventSource.Log.Trace("<ds.DataTable.DataTable|API> {0}", ObjectID);
            _nextRowID = 1;
            _recordManager = new RecordManager(this);

            _culture = CultureInfo.CurrentCulture;
            _columnCollection = new DataColumnCollection(this);
            _constraintCollection = new ConstraintCollection(this);
            _rowCollection = new DataRowCollection(this);
            _indexes = new List<Index>();

            _rowBuilder = new DataRowBuilder(this, -1);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DataTable'/> class with the specified table
        ///    name.
        /// </summary>
        public DataTable(string? tableName) : this()
        {
            _tableName = tableName ?? "";
        }

        public DataTable(string? tableName, string? tableNamespace) : this(tableName)
        {
            Namespace = tableNamespace;
        }

        // Deserialize the table from binary/xml stream.
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
            Justification = "CreateInstance's use of GetType uses only the parameterless constructor. Warnings are about serialization related constructors.")]
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        protected DataTable(SerializationInfo info, StreamingContext context) : this()
        {
            bool isSingleTable = context.Context != null ? Convert.ToBoolean(context.Context, CultureInfo.InvariantCulture) : true;
            SerializationFormat remotingFormat = SerializationFormat.Xml;
            SerializationInfoEnumerator e = info.GetEnumerator();
            while (e.MoveNext())
            {
                switch (e.Name)
                {
                    case "DataTable.RemotingFormat": //DataTable.RemotingFormat does not exist in V1/V1.1 versions
                        remotingFormat = (SerializationFormat)e.Value!;
                        break;
                }
            }

            if (remotingFormat == SerializationFormat.Binary &&
                !LocalAppContextSwitches.AllowUnsafeSerializationFormatBinary)
            {
                throw ExceptionBuilder.SerializationFormatBinaryNotSupported();
            }

            DeserializeDataTable(info, context, isSingleTable, remotingFormat);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Binary serialization is unsafe in general and is planned to be obsoleted. We do not want to mark interface or ctors of this class as unsafe as that would show many unnecessary warnings elsewhere.")]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            SerializationFormat remotingFormat = RemotingFormat;
            bool isSingleTable = context.Context != null ? Convert.ToBoolean(context.Context, CultureInfo.InvariantCulture) : true;
            SerializeDataTable(info, context, isSingleTable, remotingFormat);
        }

        // Serialize the table schema and data.
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void SerializeDataTable(SerializationInfo info, StreamingContext context, bool isSingleTable, SerializationFormat remotingFormat)
        {
            info.AddValue("DataTable.RemotingVersion", new Version(2, 0));

            // SqlHotFix 299, SerializationFormat enumeration types don't exist in V1.1 SP1
            if (SerializationFormat.Xml != remotingFormat)
            {
                info.AddValue("DataTable.RemotingFormat", remotingFormat);
            }

            if (remotingFormat != SerializationFormat.Xml)
            {
                //Binary
                SerializeTableSchema(info, context, isSingleTable);
                if (isSingleTable)
                {
                    SerializeTableData(info, context, 0);
                }
            }
            else
            {
                //XML/V1.0/V1.1
                string tempDSNamespace = string.Empty;
                bool fCreatedDataSet = false;

                if (_dataSet == null)
                {
                    DataSet ds = new DataSet("tmpDataSet");
                    // if user set values on DataTable, it isn't necessary
                    // to set them on the DataSet because they won't be inherited
                    // but it is simpler to set them in both places

                    // if user did not set values on DataTable, it is required
                    // to set them on the DataSet so the table will inherit
                    // the value already on the Datatable
                    ds.SetLocaleValue(_culture, _cultureUserSet);
                    ds.CaseSensitive = CaseSensitive;
                    ds._namespaceURI = Namespace;
                    Debug.Assert(ds.RemotingFormat == SerializationFormat.Xml, "RemotingFormat must be SerializationFormat.Xml");
                    ds.Tables.Add(this);
                    fCreatedDataSet = true;
                }
                else
                {
                    tempDSNamespace = _dataSet.Namespace;
                    _dataSet._namespaceURI = Namespace;
                }

                info.AddValue(KEY_XMLSCHEMA, _dataSet!.GetXmlSchemaForRemoting(this));
                info.AddValue(KEY_XMLDIFFGRAM, DataSet.GetRemotingDiffGram(this));

                if (fCreatedDataSet)
                {
                    _dataSet.Tables.Remove(this);
                }
                else
                {
                    _dataSet._namespaceURI = tempDSNamespace;
                }
            }
        }

        // Deserialize the table schema and data.
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void DeserializeDataTable(SerializationInfo info, StreamingContext context, bool isSingleTable, SerializationFormat remotingFormat)
        {
            if (remotingFormat != SerializationFormat.Xml)
            {
                //Binary
                DeserializeTableSchema(info, context, isSingleTable);
                if (isSingleTable)
                {
                    DeserializeTableData(info, context, 0);
                    ResetIndexes();
                }
            }
            else
            {
                //XML/V1.0/V1.1
                string? strSchema = (string?)info.GetValue(KEY_XMLSCHEMA, typeof(string));
                string? strData = (string?)info.GetValue(KEY_XMLDIFFGRAM, typeof(string));

                if (strSchema != null)
                {
                    DataSet ds = new DataSet();
                    ds.ReadXmlSchema(new XmlTextReader(new StringReader(strSchema)));

                    Debug.Assert(ds.Tables.Count == 1, "There should be exactly 1 table here");
                    DataTable table = ds.Tables[0];
                    table.CloneTo(this, null, false);
                    //this is to avoid the cascading rules in the namespace
                    Namespace = table.Namespace;

                    if (strData != null)
                    {
                        ds.Tables.Remove(ds.Tables[0]);
                        ds.Tables.Add(this);
                        ds.ReadXml(new XmlTextReader(new StringReader(strData)), XmlReadMode.DiffGram);
                        ds.Tables.Remove(this);
                    }
                }
            }
        }

        // Serialize the columns
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void SerializeTableSchema(SerializationInfo info, StreamingContext context, bool isSingleTable)
        {
            //DataTable basic  properties
            info.AddValue("DataTable.TableName", TableName);
            info.AddValue("DataTable.Namespace", Namespace);
            info.AddValue("DataTable.Prefix", Prefix);
            info.AddValue("DataTable.CaseSensitive", _caseSensitive);
            info.AddValue("DataTable.caseSensitiveAmbient", !_caseSensitiveUserSet);
            info.AddValue("DataTable.LocaleLCID", Locale.LCID);
            info.AddValue("DataTable.MinimumCapacity", _recordManager.MinimumCapacity);

            //DataTable state internal properties
            info.AddValue("DataTable.NestedInDataSet", _fNestedInDataset);
            info.AddValue("DataTable.TypeName", TypeName.ToString());
            info.AddValue("DataTable.RepeatableElement", _repeatableElement);

            //ExtendedProperties
            info.AddValue("DataTable.ExtendedProperties", ExtendedProperties);

            //Columns
            info.AddValue("DataTable.Columns.Count", Columns.Count);

            //Check for closure of expression in case of single table.
            if (isSingleTable)
            {
                List<DataTable> list = new List<DataTable>();
                list.Add(this);
                if (!CheckForClosureOnExpressionTables(list))
                {
                    throw ExceptionBuilder.CanNotRemoteDataTable();
                }
            }

            IFormatProvider formatProvider = CultureInfo.InvariantCulture;
            for (int i = 0; i < Columns.Count; i++)
            {
                //DataColumn basic properties
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.ColumnName", i), Columns[i].ColumnName);
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.Namespace", i), Columns[i]._columnUri);
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.Prefix", i), Columns[i].Prefix);
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.ColumnMapping", i), Columns[i].ColumnMapping);
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.AllowDBNull", i), Columns[i].AllowDBNull);
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.AutoIncrement", i), Columns[i].AutoIncrement);
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.AutoIncrementStep", i), Columns[i].AutoIncrementStep);
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.AutoIncrementSeed", i), Columns[i].AutoIncrementSeed);
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.Caption", i), Columns[i].Caption);
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.DefaultValue", i), Columns[i].DefaultValue);
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.ReadOnly", i), Columns[i].ReadOnly);
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.MaxLength", i), Columns[i].MaxLength);
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.DataType_AssemblyQualifiedName", i), Columns[i].DataType.AssemblyQualifiedName);
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.XmlDataType", i), Columns[i].XmlDataType);
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.SimpleType", i), Columns[i].SimpleType);

                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.DateTimeMode", i), Columns[i].DateTimeMode);

                //DataColumn internal state properties
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.AutoIncrementCurrent", i), Columns[i].AutoIncrementCurrent);

                //Expression
                if (isSingleTable)
                {
                    info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.Expression", i), Columns[i].Expression);
                }

                //ExtendedProperties
                info.AddValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.ExtendedProperties", i), Columns[i]._extendedProperties);
            }

            //Constraints
            if (isSingleTable)
            {
                SerializeConstraints(info, context, 0, false);
            }
        }

        // Deserialize all the Columns
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void DeserializeTableSchema(SerializationInfo info, StreamingContext context, bool isSingleTable)
        {
            //DataTable basic properties
            _tableName = info.GetString("DataTable.TableName")!;
            _tableNamespace = info.GetString("DataTable.Namespace");
            _tablePrefix = info.GetString("DataTable.Prefix")!;

            bool caseSensitive = info.GetBoolean("DataTable.CaseSensitive");
            SetCaseSensitiveValue(caseSensitive, true, false);
            _caseSensitiveUserSet = !info.GetBoolean("DataTable.caseSensitiveAmbient");

            int lcid = (int)info.GetValue("DataTable.LocaleLCID", typeof(int))!;
            CultureInfo culture = new CultureInfo(lcid);
            SetLocaleValue(culture, true, false);
            _cultureUserSet = true;

            MinimumCapacity = info.GetInt32("DataTable.MinimumCapacity");

            //DataTable state internal properties
            _fNestedInDataset = info.GetBoolean("DataTable.NestedInDataSet");
            string tName = info.GetString("DataTable.TypeName")!;
            _typeName = new XmlQualifiedName(tName);
            _repeatableElement = info.GetBoolean("DataTable.RepeatableElement");

            //ExtendedProperties
            _extendedProperties = (PropertyCollection?)info.GetValue("DataTable.ExtendedProperties", typeof(PropertyCollection));

            //Columns
            int colCount = info.GetInt32("DataTable.Columns.Count");
            string?[] expressions = new string?[colCount];
            Debug.Assert(Columns.Count == 0, "There is column in Table");

            IFormatProvider formatProvider = CultureInfo.InvariantCulture;
            for (int i = 0; i < colCount; i++)
            {
                DataColumn dc = new DataColumn();

                //DataColumn public state properties
                dc.ColumnName = info.GetString(string.Format(formatProvider, "DataTable.DataColumn_{0}.ColumnName", i));
                dc._columnUri = info.GetString(string.Format(formatProvider, "DataTable.DataColumn_{0}.Namespace", i));
                dc.Prefix = info.GetString(string.Format(formatProvider, "DataTable.DataColumn_{0}.Prefix", i));

                string typeName = (string)info.GetValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.DataType_AssemblyQualifiedName", i), typeof(string))!;
                dc.DataType = Type.GetType(typeName, throwOnError: true);
                dc.XmlDataType = (string?)info.GetValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.XmlDataType", i), typeof(string));
                dc.SimpleType = (SimpleType?)info.GetValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.SimpleType", i), typeof(SimpleType));

                dc.ColumnMapping = (MappingType)info.GetValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.ColumnMapping", i), typeof(MappingType))!;
                dc.DateTimeMode = (DataSetDateTime)info.GetValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.DateTimeMode", i), typeof(DataSetDateTime))!;

                dc.AllowDBNull = info.GetBoolean(string.Format(formatProvider, "DataTable.DataColumn_{0}.AllowDBNull", i));
                dc.AutoIncrement = info.GetBoolean(string.Format(formatProvider, "DataTable.DataColumn_{0}.AutoIncrement", i));
                dc.AutoIncrementStep = info.GetInt64(string.Format(formatProvider, "DataTable.DataColumn_{0}.AutoIncrementStep", i));
                dc.AutoIncrementSeed = info.GetInt64(string.Format(formatProvider, "DataTable.DataColumn_{0}.AutoIncrementSeed", i));
                dc.Caption = info.GetString(string.Format(formatProvider, "DataTable.DataColumn_{0}.Caption", i));
                dc.DefaultValue = info.GetValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.DefaultValue", i), typeof(object));
                dc.ReadOnly = info.GetBoolean(string.Format(formatProvider, "DataTable.DataColumn_{0}.ReadOnly", i));
                dc.MaxLength = info.GetInt32(string.Format(formatProvider, "DataTable.DataColumn_{0}.MaxLength", i));

                //DataColumn internal state properties
                dc.AutoIncrementCurrent = info.GetValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.AutoIncrementCurrent", i), typeof(object))!;

                //Expression
                if (isSingleTable)
                {
                    expressions[i] = info.GetString(string.Format(formatProvider, "DataTable.DataColumn_{0}.Expression", i));
                }

                //ExtendedProperties
                dc._extendedProperties = (PropertyCollection?)info.GetValue(string.Format(formatProvider, "DataTable.DataColumn_{0}.ExtendedProperties", i), typeof(PropertyCollection));
                Columns.Add(dc);
            }
            if (isSingleTable)
            {
                for (int i = 0; i < colCount; i++)
                {
                    if (expressions[i] != null)
                    {
                        Columns[i].Expression = expressions[i];
                    }
                }
            }

            //Constraints
            if (isSingleTable)
            {
                DeserializeConstraints(info, context, /*table index */ 0, /* serialize all constraints */false); // since single table, send table index as 0, meanwhile passing
                // false for 'allConstraints' means, handle all the constraint related to the table
            }
        }

        // Serialize constraints available on the table - note this function is marked internal because it is called by the DataSet deserializer.
        // ***Schema for Serializing ArrayList of Constraints***
        // Unique Constraint - ["U"]->[constraintName]->[columnIndexes]->[IsPrimaryKey]->[extendedProperties]
        // Foriegn Key Constraint - ["F"]->[constraintName]->[parentTableIndex, parentcolumnIndexes]->[childTableIndex, childColumnIndexes]->[AcceptRejectRule, UpdateRule, DeleteRule]->[extendedProperties]
        internal void SerializeConstraints(SerializationInfo info, StreamingContext context, int serIndex, bool allConstraints)
        {
            if (allConstraints)
            {
                Debug.Assert(DataSet != null);
            }

            ArrayList constraintList = new ArrayList();

            for (int i = 0; i < Constraints.Count; i++)
            {
                Constraint c = Constraints[i];

                UniqueConstraint? uc = c as UniqueConstraint;
                if (uc != null)
                {
                    int[] colInfo = new int[uc.Columns.Length];
                    for (int j = 0; j < colInfo.Length; j++)
                    {
                        colInfo[j] = uc.Columns[j].Ordinal;
                    }

                    ArrayList list = new ArrayList();
                    list.Add("U");
                    list.Add(uc.ConstraintName);
                    list.Add(colInfo);
                    list.Add(uc.IsPrimaryKey);
                    list.Add(uc.ExtendedProperties);

                    constraintList.Add(list);
                }
                else
                {
                    ForeignKeyConstraint? fk = c as ForeignKeyConstraint;
                    Debug.Assert(fk != null);
                    bool shouldSerialize = (allConstraints == true) || (fk.Table == this && fk.RelatedTable == this);

                    if (shouldSerialize)
                    {
                        int[] parentInfo = new int[fk.RelatedColumns.Length + 1];
                        parentInfo[0] = allConstraints ? DataSet!.Tables.IndexOf(fk.RelatedTable) : 0;
                        for (int j = 1; j < parentInfo.Length; j++)
                        {
                            parentInfo[j] = fk.RelatedColumns[j - 1].Ordinal;
                        }

                        int[] childInfo = new int[fk.Columns.Length + 1];
                        childInfo[0] = allConstraints ? DataSet!.Tables.IndexOf(fk.Table) : 0;   //Since the constraint is on the current table, this is the child table.
                        for (int j = 1; j < childInfo.Length; j++)
                        {
                            childInfo[j] = fk.Columns[j - 1].Ordinal;
                        }

                        ArrayList list = new ArrayList();
                        list.Add("F");
                        list.Add(fk.ConstraintName);
                        list.Add(parentInfo);
                        list.Add(childInfo);
                        list.Add(new int[] { (int)fk.AcceptRejectRule, (int)fk.UpdateRule, (int)fk.DeleteRule });
                        list.Add(fk.ExtendedProperties);

                        constraintList.Add(list);
                    }
                }
            }
            info.AddValue(string.Format(CultureInfo.InvariantCulture, "DataTable_{0}.Constraints", serIndex), constraintList);
        }

        // Deserialize the constraints on the table.
        // ***Schema for Serializing ArrayList of Constraints***
        // Unique Constraint - ["U"]->[constraintName]->[columnIndexes]->[IsPrimaryKey]->[extendedProperties]
        // Foriegn Key Constraint - ["F"]->[constraintName]->[parentTableIndex, parentcolumnIndexes]->[childTableIndex, childColumnIndexes]->[AcceptRejectRule, UpdateRule, DeleteRule]->[extendedProperties]
        internal void DeserializeConstraints(SerializationInfo info, StreamingContext context, int serIndex, bool allConstraints)
        {
            ArrayList constraintList = (ArrayList)info.GetValue(string.Format(CultureInfo.InvariantCulture, "DataTable_{0}.Constraints", serIndex), typeof(ArrayList))!;

            foreach (ArrayList list in constraintList)
            {
                string con = (string)list[0]!;

                if (con.Equals("U"))
                {
                    //Unique Constraints
                    string constraintName = (string)list[1]!;

                    int[] keyColumnIndexes = (int[])list[2]!;
                    bool isPrimaryKey = (bool)list[3]!;
                    PropertyCollection? extendedProperties = (PropertyCollection?)list[4];

                    DataColumn[] keyColumns = new DataColumn[keyColumnIndexes.Length];
                    for (int i = 0; i < keyColumnIndexes.Length; i++)
                    {
                        keyColumns[i] = Columns[keyColumnIndexes[i]];
                    }

                    //Create the constraint.
                    UniqueConstraint uc = new UniqueConstraint(constraintName, keyColumns, isPrimaryKey);
                    uc._extendedProperties = extendedProperties;

                    //Add the unique constraint and it will in turn set the primary keys also if needed.
                    Constraints.Add(uc);
                }
                else
                {
                    //ForeignKeyConstraints
                    Debug.Assert(con.Equals("F"));

                    string constraintName = (string)list[1]!;
                    int[] parentInfo = (int[])list[2]!;
                    int[] childInfo = (int[])list[3]!;
                    int[] rules = (int[])list[4]!;
                    PropertyCollection? extendedProperties = (PropertyCollection?)list[5];

                    //ParentKey Columns.
                    DataTable parentTable = (allConstraints == false) ? this : DataSet!.Tables[parentInfo[0]];
                    DataColumn[] parentkeyColumns = new DataColumn[parentInfo.Length - 1];
                    for (int i = 0; i < parentkeyColumns.Length; i++)
                    {
                        parentkeyColumns[i] = parentTable.Columns[parentInfo[i + 1]];
                    }

                    //ChildKey Columns.
                    DataTable childTable = (allConstraints == false) ? this : DataSet!.Tables[childInfo[0]];
                    DataColumn[] childkeyColumns = new DataColumn[childInfo.Length - 1];
                    for (int i = 0; i < childkeyColumns.Length; i++)
                    {
                        childkeyColumns[i] = childTable.Columns[childInfo[i + 1]];
                    }

                    //Create the Constraint.
                    ForeignKeyConstraint fk = new ForeignKeyConstraint(constraintName, parentkeyColumns, childkeyColumns);
                    fk.AcceptRejectRule = (AcceptRejectRule)rules[0];
                    fk.UpdateRule = (Rule)rules[1];
                    fk.DeleteRule = (Rule)rules[2];
                    fk._extendedProperties = extendedProperties;

                    //Add just the foreign key constraint without creating unique constraint.
                    Constraints.Add(fk, false);
                }
            }
        }

        // Serialize the expressions on the table - Marked internal so that DataSet deserializer can call into this
        internal void SerializeExpressionColumns(SerializationInfo info, StreamingContext context, int serIndex)
        {
            int colCount = Columns.Count;
            for (int i = 0; i < colCount; i++)
            {
                info.AddValue(string.Format(CultureInfo.InvariantCulture, "DataTable_{0}.DataColumn_{1}.Expression", serIndex, i), Columns[i].Expression);
            }
        }

        // Deserialize the expressions on the table - Marked internal so that DataSet deserializer can call into this
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void DeserializeExpressionColumns(SerializationInfo info, StreamingContext context, int serIndex)
        {
            int colCount = Columns.Count;
            for (int i = 0; i < colCount; i++)
            {
                string expr = info.GetString(string.Format(CultureInfo.InvariantCulture, "DataTable_{0}.DataColumn_{1}.Expression", serIndex, i))!;
                if (0 != expr.Length)
                {
                    Columns[i].Expression = expr;
                }
            }
        }

        // Serialize all the Rows.
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void SerializeTableData(SerializationInfo info, StreamingContext context, int serIndex)
        {
            //Cache all the column count, row count
            int colCount = Columns.Count;
            int rowCount = Rows.Count;
            int modifiedRowCount = 0;
            int editRowCount = 0;

            //Compute row states and assign the bits accordingly - 00[Unchanged], 01[Added], 10[Modifed], 11[Deleted]
            BitArray rowStates = new BitArray(rowCount * 3, false); //All bit flags are set to false on initialization of the BitArray.
            for (int i = 0; i < rowCount; i++)
            {
                int bitIndex = i * 3;
                DataRow row = Rows[i];
                DataRowState rowState = row.RowState;
                switch (rowState)
                {
                    case DataRowState.Unchanged:
                        //rowStates[bitIndex] = false;
                        //rowStates[bitIndex + 1] = false;
                        break;
                    case DataRowState.Added:
                        //rowStates[bitIndex] = false;
                        rowStates[bitIndex + 1] = true;
                        break;
                    case DataRowState.Modified:
                        rowStates[bitIndex] = true;
                        //rowStates[bitIndex + 1] = false;
                        modifiedRowCount++;
                        break;
                    case DataRowState.Deleted:
                        rowStates[bitIndex] = true;
                        rowStates[bitIndex + 1] = true;
                        break;
                    default:
                        throw ExceptionBuilder.InvalidRowState(rowState);
                }
                if (-1 != row._tempRecord)
                {
                    rowStates[bitIndex + 2] = true;
                    editRowCount++;
                }
            }

            //Compute the actual storage records that need to be created.
            int recordCount = rowCount + modifiedRowCount + editRowCount;

            //Create column storages.
            ArrayList storeList = new ArrayList();
            ArrayList nullbitList = new ArrayList();
            if (recordCount > 0)
            {
                //Create the storage only if have records.
                for (int i = 0; i < colCount; i++)
                {
                    object store = Columns[i].GetEmptyColumnStore(recordCount);
                    storeList.Add(store);
                    BitArray nullbits = new BitArray(recordCount);
                    nullbitList.Add(nullbits);
                }
            }

            //Copy values into column storages
            int recordsConsumed = 0;
            Hashtable rowErrors = new Hashtable();
            Hashtable colErrors = new Hashtable();
            for (int i = 0; i < rowCount; i++)
            {
                int recordsPerRow = Rows[i].CopyValuesIntoStore(storeList, nullbitList, recordsConsumed);
                GetRowAndColumnErrors(i, rowErrors, colErrors);
                recordsConsumed += recordsPerRow;
            }

            IFormatProvider formatProvider = CultureInfo.InvariantCulture;
            //Serialize all the computed values.
            info.AddValue(string.Format(formatProvider, "DataTable_{0}.Rows.Count", serIndex), rowCount);
            info.AddValue(string.Format(formatProvider, "DataTable_{0}.Records.Count", serIndex), recordCount);
            info.AddValue(string.Format(formatProvider, "DataTable_{0}.RowStates", serIndex), rowStates);
            info.AddValue(string.Format(formatProvider, "DataTable_{0}.Records", serIndex), storeList);
            info.AddValue(string.Format(formatProvider, "DataTable_{0}.NullBits", serIndex), nullbitList);
            info.AddValue(string.Format(formatProvider, "DataTable_{0}.RowErrors", serIndex), rowErrors);
            info.AddValue(string.Format(formatProvider, "DataTable_{0}.ColumnErrors", serIndex), colErrors);
        }

        // Deserialize all the Rows.
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void DeserializeTableData(SerializationInfo info, StreamingContext context, int serIndex)
        {
            bool enforceConstraintsOrg = _enforceConstraints;
            bool inDataLoadOrg = _inDataLoad;

            try
            {
                _enforceConstraints = false;
                _inDataLoad = true;
                IFormatProvider formatProvider = CultureInfo.InvariantCulture;
                int rowCount = info.GetInt32(string.Format(formatProvider, "DataTable_{0}.Rows.Count", serIndex));
                int recordCount = info.GetInt32(string.Format(formatProvider, "DataTable_{0}.Records.Count", serIndex));
                BitArray rowStates = (BitArray)info.GetValue(string.Format(formatProvider, "DataTable_{0}.RowStates", serIndex), typeof(BitArray))!;
                ArrayList storeList = (ArrayList)info.GetValue(string.Format(formatProvider, "DataTable_{0}.Records", serIndex), typeof(ArrayList))!;
                ArrayList nullbitList = (ArrayList)info.GetValue(string.Format(formatProvider, "DataTable_{0}.NullBits", serIndex), typeof(ArrayList))!;
                Hashtable rowErrors = (Hashtable)info.GetValue(string.Format(formatProvider, "DataTable_{0}.RowErrors", serIndex), typeof(Hashtable))!;
                rowErrors.OnDeserialization(this); //OnDeSerialization must be called since the hashtable gets deserialized after the whole graph gets deserialized
                Hashtable colErrors = (Hashtable)info.GetValue(string.Format(formatProvider, "DataTable_{0}.ColumnErrors", serIndex), typeof(Hashtable))!;
                colErrors.OnDeserialization(this); //OnDeSerialization must be called since the hashtable gets deserialized after the whole graph gets deserialized

                if (recordCount <= 0)
                {
                    //No need for deserialization of the storage and errors if there are no records.
                    return;
                }

                //Point the record manager storage to the deserialized values.
                for (int i = 0; i < Columns.Count; i++)
                {
                    Columns[i].SetStorage(storeList[i]!, (BitArray)nullbitList[i]!);
                }

                //Create rows and set the records appropriately.
                int recordIndex = 0;
                DataRow[] rowArr = new DataRow[recordCount];
                for (int i = 0; i < rowCount; i++)
                {
                    //Create a new row which sets old and new records to -1.
                    DataRow row = NewEmptyRow();
                    rowArr[recordIndex] = row;
                    int bitIndex = i * 3;
                    switch (ConvertToRowState(rowStates, bitIndex))
                    {
                        case DataRowState.Unchanged:
                            row._oldRecord = recordIndex;
                            row._newRecord = recordIndex;
                            recordIndex += 1;
                            break;
                        case DataRowState.Added:
                            row._oldRecord = -1;
                            row._newRecord = recordIndex;
                            recordIndex += 1;
                            break;
                        case DataRowState.Modified:
                            row._oldRecord = recordIndex;
                            row._newRecord = recordIndex + 1;
                            rowArr[recordIndex + 1] = row;
                            recordIndex += 2;
                            break;
                        case DataRowState.Deleted:
                            row._oldRecord = recordIndex;
                            row._newRecord = -1;
                            recordIndex += 1;
                            break;
                    }
                    if (rowStates[bitIndex + 2])
                    {
                        row._tempRecord = recordIndex;
                        rowArr[recordIndex] = row;
                        recordIndex += 1;
                    }
                    else
                    {
                        row._tempRecord = -1;
                    }
                    Rows.ArrayAdd(row);
                    row.rowID = _nextRowID;
                    _nextRowID++;
                    ConvertToRowError(i, rowErrors, colErrors);
                }
                _recordManager.SetRowCache(rowArr);
                ResetIndexes();
            }
            finally
            {
                _enforceConstraints = enforceConstraintsOrg;
                _inDataLoad = inDataLoadOrg;
            }
        }

        // Constructs the RowState from the two bits in the bitarray.
        private static DataRowState ConvertToRowState(BitArray bitStates, int bitIndex)
        {
            Debug.Assert(bitStates != null);
            Debug.Assert(bitStates.Length > bitIndex);

            bool b1 = bitStates[bitIndex];
            bool b2 = bitStates[bitIndex + 1];

            if (!b1 && !b2)
            {
                return DataRowState.Unchanged;
            }
            else if (!b1 && b2)
            {
                return DataRowState.Added;
            }
            else if (b1 && !b2)
            {
                return DataRowState.Modified;
            }
            else if (b1 && b2)
            {
                return DataRowState.Deleted;
            }
            else
            {
                throw ExceptionBuilder.InvalidRowBitPattern();
            }
        }

        // Get the error on the row and columns - Marked internal so that DataSet deserializer can call into this
        internal void GetRowAndColumnErrors(int rowIndex, Hashtable rowErrors, Hashtable colErrors)
        {
            Debug.Assert(Rows.Count > rowIndex);
            Debug.Assert(rowErrors != null);
            Debug.Assert(colErrors != null);

            DataRow row = Rows[rowIndex];

            if (row.HasErrors)
            {
                rowErrors.Add(rowIndex, row.RowError);
                DataColumn[] dcArr = row.GetColumnsInError();
                if (dcArr.Length > 0)
                {
                    int[] columnsInError = new int[dcArr.Length];
                    string[] columnErrors = new string[dcArr.Length];
                    for (int i = 0; i < dcArr.Length; i++)
                    {
                        columnsInError[i] = dcArr[i].Ordinal;
                        columnErrors[i] = row.GetColumnError(dcArr[i]);
                    }
                    ArrayList list = new ArrayList();
                    list.Add(columnsInError);
                    list.Add(columnErrors);
                    colErrors.Add(rowIndex, list);
                }
            }
        }

        // Set the row and columns in error..
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void ConvertToRowError(int rowIndex, Hashtable rowErrors, Hashtable colErrors)
        {
            Debug.Assert(Rows.Count > rowIndex);
            Debug.Assert(rowErrors != null);
            Debug.Assert(colErrors != null);

            DataRow row = Rows[rowIndex];

            if (rowErrors.ContainsKey(rowIndex))
            {
                row.RowError = (string)rowErrors[rowIndex]!;
            }
            if (colErrors.ContainsKey(rowIndex))
            {
                ArrayList list = (ArrayList)colErrors[rowIndex]!;
                int[] columnsInError = (int[])list[0]!;
                string[] columnErrors = (string[])list[1]!;
                Debug.Assert(columnsInError.Length == columnErrors.Length);
                for (int i = 0; i < columnsInError.Length; i++)
                {
                    row.SetColumnError(columnsInError[i], columnErrors[i]);
                }
            }
        }

        /// <summary>
        /// Indicates whether string comparisons within the table are case-sensitive.
        /// </summary>
        public bool CaseSensitive
        {
            get { return _caseSensitive; }
            set
            {
                if (_caseSensitive != value)
                {
                    bool oldValue = _caseSensitive;
                    bool oldUserSet = _caseSensitiveUserSet;
                    _caseSensitive = value;
                    _caseSensitiveUserSet = true;

                    if (DataSet != null && !DataSet.ValidateCaseConstraint())
                    {
                        _caseSensitive = oldValue;
                        _caseSensitiveUserSet = oldUserSet;
                        throw ExceptionBuilder.CannotChangeCaseLocale();
                    }
                    SetCaseSensitiveValue(value, true, true);
                }
                _caseSensitiveUserSet = true;
            }
        }

        internal bool AreIndexEventsSuspended => 0 < _suspendIndexEvents;

        internal void RestoreIndexEvents(bool forceReset)
        {
            DataCommonEventSource.Log.Trace("<ds.DataTable.RestoreIndexEvents|Info> {0}, {1}", ObjectID, _suspendIndexEvents);
            if (0 < _suspendIndexEvents)
            {
                _suspendIndexEvents--;
                if (0 == _suspendIndexEvents)
                {
                    Exception? first = null;
                    SetShadowIndexes();
                    try
                    {
                        // the length of shadowIndexes will not change
                        // but the array instance may change during
                        // events during Index.Reset
                        int numIndexes = _shadowIndexes!.Count;
                        for (int i = 0; i < numIndexes; i++)
                        {
                            Index ndx = _shadowIndexes[i]; // shadowindexes may change, see ShadowIndexCopy()
                            try
                            {
                                if (forceReset || ndx.HasRemoteAggregate)
                                {
                                    ndx.Reset(); // resets & fires
                                }
                                else
                                {
                                    ndx.FireResetEvent(); // fire the Reset event we were firing
                                }
                            }
                            catch (Exception e) when (ADP.IsCatchableExceptionType(e))
                            {
                                ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                                if (null == first)
                                {
                                    first = e;
                                }
                            }
                        }
                        if (null != first)
                        {
                            throw first;
                        }
                    }
                    finally
                    {
                        RestoreShadowIndexes();
                    }
                }
            }
        }

        internal void SuspendIndexEvents()
        {
            DataCommonEventSource.Log.Trace("<ds.DataTable.SuspendIndexEvents|Info> {0}, {1}", ObjectID, _suspendIndexEvents);
            _suspendIndexEvents++;
        }

        [Browsable(false)]
        public bool IsInitialized => !fInitInProgress;

        private bool IsTypedDataTable
        {
            get
            {
                switch (_isTypedDataTable)
                {
                    case 0:
                        _isTypedDataTable = (byte)((GetType() != typeof(DataTable)) ? 1 : 2);
                        return (1 == _isTypedDataTable);
                    case 1:
                        return true;
                    default:
                        return false;
                }
            }
        }

        internal bool SetCaseSensitiveValue(bool isCaseSensitive, bool userSet, bool resetIndexes)
        {
            if (userSet || (!_caseSensitiveUserSet && (_caseSensitive != isCaseSensitive)))
            {
                _caseSensitive = isCaseSensitive;
                if (isCaseSensitive)
                {
                    _compareFlags = CompareOptions.None;
                }
                else
                {
                    _compareFlags = CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth;
                }
                if (resetIndexes)
                {
                    ResetIndexes();
                    foreach (Constraint constraint in Constraints)
                    {
                        constraint.CheckConstraint();
                    }
                }
                return true;
            }
            return false;
        }


        private void ResetCaseSensitive()
        {
            // this method is used design-time scenarios via reflection
            //   by the property grid context menu to show the Reset option or not
            SetCaseSensitiveValue((null != _dataSet) && _dataSet.CaseSensitive, true, true);
            _caseSensitiveUserSet = false;
        }

        internal bool ShouldSerializeCaseSensitive()
        {
            // this method is used design-time scenarios via reflection
            //   by the property grid to show the CaseSensitive property in bold or not
            //   by the code dom for persisting the CaseSensitive property or not
            return _caseSensitiveUserSet;
        }

        internal bool SelfNested
        {
            get
            {
                // Is this correct? if ((top[i].nestedParentRelation!= null) && (top[i].nestedParentRelation.ParentTable == top[i]))
                foreach (DataRelation rel in ParentRelations)
                {
                    if (rel.Nested && rel.ParentTable == this)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)] // don't have debugger view expand this
        internal List<Index> LiveIndexes
        {
            get
            {
                if (!AreIndexEventsSuspended)
                {
                    for (int i = _indexes.Count - 1; 0 <= i; --i)
                    {
                        Index index = _indexes[i];
                        if (index.RefCount <= 1)
                        {
                            index.RemoveRef();
                        }
                    }
                }
                return _indexes;
            }
        }

        [DefaultValue(SerializationFormat.Xml)]
        public SerializationFormat RemotingFormat
        {
            get { return _remotingFormat; }
            set
            {
                switch (value)
                {
                    case SerializationFormat.Xml:
                        break;

                    case SerializationFormat.Binary:
                        if (LocalAppContextSwitches.AllowUnsafeSerializationFormatBinary)
                        {
                            break;
                        }
                        throw ExceptionBuilder.SerializationFormatBinaryNotSupported();

                    default:
                        throw ExceptionBuilder.InvalidRemotingFormat(value);
                }

                // table can not have different format than its dataset, unless it is stand alone datatable
                if (DataSet != null && value != DataSet.RemotingFormat)
                {
                    throw ExceptionBuilder.CanNotSetRemotingFormat();
                }
                _remotingFormat = value;
            }
        }

        // used to keep temporary state of unique Key posiotion to be added for inference only
        internal int UKColumnPositionForInference
        {
            get { return _ukColumnPositionForInference; }
            set { _ukColumnPositionForInference = value; }
        }

        /// <summary>
        /// Gets the collection of child relations for this <see cref='System.Data.DataTable'/>.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataRelationCollection ChildRelations =>
            _childRelationsCollection ??= new DataRelationCollection.DataTableRelationCollection(this, false);

        /// <summary>
        /// Gets the collection of columns that belong to this table.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public DataColumnCollection Columns => _columnCollection;

        private void ResetColumns()
        {
            // this method is used design-time scenarios via reflection
            //   by the property grid context menu to show the Reset option or not
            Columns.Clear();
        }

        private CompareInfo CompareInfo => _compareInfo ??= Locale.CompareInfo;

        /// <summary>
        /// Gets the collection of constraints maintained by this table.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ConstraintCollection Constraints => _constraintCollection;

        /// <summary>
        /// Resets the <see cref='System.Data.DataTable.Constraints'/> property to its default state.
        /// </summary>
        private void ResetConstraints() => Constraints.Clear();

        /// <summary>
        /// Gets the <see cref='System.Data.DataSet'/> that this table belongs to.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public DataSet? DataSet => _dataSet;

        /// <summary>
        /// Internal method for setting the DataSet pointer.
        /// </summary>
        internal void SetDataSet(DataSet? dataSet)
        {
            if (_dataSet != dataSet)
            {
                _dataSet = dataSet;

                if (DataSet != null)
                {
                    _defaultView = null;
                }
                //Set the remoting format variable directly
                if (dataSet != null)
                {
                    _remotingFormat = dataSet.RemotingFormat;
                }
            }
        }

        /// <summary>
        /// Gets a customized view of the table which may include a
        /// filtered view, or a cursor position.
        /// </summary>
        [Browsable(false)]
        public DataView DefaultView
        {
            get
            {
                DataView? view = _defaultView;
                if (null == view)
                {
                    if (null != _dataSet)
                    {
                        view = _dataSet.DefaultViewManager.CreateDataView(this);
                    }
                    else
                    {
                        view = new DataView(this, true);
                        view.SetIndex2("", DataViewRowState.CurrentRows, null, true);
                    }

                    view = Interlocked.CompareExchange(ref _defaultView, view, null);
                    if (null == view)
                    {
                        view = _defaultView;
                    }
                }
                return view;
            }
        }

        /// <summary>
        /// Gets or sets the expression that will return a value used to represent
        /// this table in UI.
        /// </summary>
        [DefaultValue("")]
        [AllowNull]
        public string DisplayExpression
        {
            get { return DisplayExpressionInternal; }
            [RequiresUnreferencedCode("Members from types used in the expressions may be trimmed if not referenced directly.")]
            set
            {
                _displayExpression = !string.IsNullOrEmpty(value) ?
                    new DataExpression(this, value) :
                    null;
            }
        }
        internal string DisplayExpressionInternal => _displayExpression != null ? _displayExpression.Expression : string.Empty;

        internal bool EnforceConstraints
        {
            get
            {
                if (SuspendEnforceConstraints)
                {
                    return false;
                }
                if (_dataSet != null)
                {
                    return _dataSet.EnforceConstraints;
                }

                return _enforceConstraints;
            }
            set
            {
                if (_dataSet == null && _enforceConstraints != value)
                {
                    if (value)
                    {
                        EnableConstraints();
                    }

                    _enforceConstraints = value;
                }
            }
        }

        internal bool SuspendEnforceConstraints
        {
            get { return _suspendEnforceConstraints; }
            set { _suspendEnforceConstraints = value; }
        }

        internal void EnableConstraints()
        {
            bool errors = false;
            foreach (Constraint constr in Constraints)
            {
                if (constr is UniqueConstraint)
                {
                    errors |= constr.IsConstraintViolated();
                }
            }

            foreach (DataColumn column in Columns)
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

            if (errors)
            {
                EnforceConstraints = false;
                throw ExceptionBuilder.EnforceConstraint();
            }
        }

        /// <summary>
        /// Gets the collection of customized user information.
        /// </summary>
        [Browsable(false)]
        public PropertyCollection ExtendedProperties => _extendedProperties ??= new PropertyCollection();

        internal IFormatProvider FormatProvider
        {
            get
            {
                // used for Formating/Parsing
                // https://docs.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.isneutralculture
                if (null == _formatProvider)
                {
                    CultureInfo culture = Locale;
                    if (culture.IsNeutralCulture)
                    {
                        culture = CultureInfo.InvariantCulture;
                    }
                    _formatProvider = culture;
                }
                return _formatProvider;
            }
        }

        /// <summary>
        /// Gets a value indicating whether there are errors in any of the rows in any of
        /// the tables of the <see cref='System.Data.DataSet'/> to which the table belongs.
        /// </summary>
        [Browsable(false)]
        public bool HasErrors
        {
            get
            {
                for (int i = 0; i < Rows.Count; i++)
                {
                    if (Rows[i].HasErrors)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the locale information used to compare strings within the table.
        /// Also used for locale sensitive, case,kana,width insensitive column name lookups
        /// Also used for converting values to and from string
        /// </summary>
        public CultureInfo Locale
        {
            get
            {
                // used for Comparing not Formatting/Parsing
                Debug.Assert(null != _culture, "null culture");
                return _culture;
            }
            set
            {
                long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.set_Locale|API> {0}", ObjectID);
                try
                {
                    bool userSet = true;
                    if (null == value)
                    {
                        // reset Locale to inherit from DataSet
                        userSet = false;
                        value = (null != _dataSet) ? _dataSet.Locale : _culture;
                    }
                    if (_culture != value && !_culture.Equals(value))
                    {
                        bool flag = false;
                        bool exceptionThrown = false;
                        CultureInfo oldLocale = _culture;
                        bool oldUserSet = _cultureUserSet;
                        try
                        {
                            _cultureUserSet = true;
                            SetLocaleValue(value, true, false);
                            if ((null == DataSet) || DataSet.ValidateLocaleConstraint())
                            {
                                flag = false;
                                SetLocaleValue(value, true, true);
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
                            {
                                // reset old locale if ValidationFailed or exception thrown
                                try
                                {
                                    SetLocaleValue(oldLocale, true, true);
                                }
                                catch (Exception e) when (ADP.IsCatchableExceptionType(e))
                                {
                                    // failed to reset all indexes for all constraints
                                    ADP.TraceExceptionWithoutRethrow(e);
                                }
                                _cultureUserSet = oldUserSet;
                                if (!exceptionThrown)
                                {
                                    throw ExceptionBuilder.CannotChangeCaseLocale(null);
                                }
                            }
                        }
                        SetLocaleValue(value, true, true);
                    }
                    _cultureUserSet = userSet;
                }
                finally
                {
                    DataCommonEventSource.Log.ExitScope(logScopeId);
                }
            }
        }

        internal bool SetLocaleValue(CultureInfo culture, bool userSet, bool resetIndexes)
        {
            Debug.Assert(null != culture, "SetLocaleValue: no locale");
            if (userSet || resetIndexes || (!_cultureUserSet && !_culture.Equals(culture)))
            {
                _culture = culture;
                _compareInfo = null;
                _formatProvider = null;
                _hashCodeProvider = null;

                foreach (DataColumn column in Columns)
                {
                    column._hashCode = GetSpecialHashCode(column.ColumnName);
                }
                if (resetIndexes)
                {
                    ResetIndexes();
                    foreach (Constraint constraint in Constraints)
                    {
                        constraint.CheckConstraint();
                    }
                }
                return true;
            }
            return false;
        }

        internal bool ShouldSerializeLocale()
        {
            // this method is used design-time scenarios via reflection
            //   by the property grid to show the Locale property in bold or not
            //   by the code dom for persisting the Locale property or not

            // we always want the locale persisted if set by user or different the current thread if standalone table
            // but that logic should by performed by the serializion code
            return _cultureUserSet;
        }

        /// <summary>
        /// Gets or sets the initial starting size for this table.
        /// </summary>
        [DefaultValue(50)]
        public int MinimumCapacity
        {
            get { return _recordManager.MinimumCapacity; }
            set
            {
                if (value != _recordManager.MinimumCapacity)
                {
                    _recordManager.MinimumCapacity = value;
                }
            }
        }

        internal int RecordCapacity => _recordManager.RecordCapacity;

        internal int ElementColumnCount
        {
            get { return _elementColumnCount; }
            set
            {
                if ((value > 0) && (_xmlText != null))
                {
                    throw ExceptionBuilder.TableCannotAddToSimpleContent();
                }
                else
                {
                    _elementColumnCount = value;
                }
            }
        }

        /// <summary>
        /// Gets the collection of parent relations for this <see cref='System.Data.DataTable'/>.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataRelationCollection ParentRelations => _parentRelationsCollection ??= new DataRelationCollection.DataTableRelationCollection(this, true);

        internal bool MergingData
        {
            get { return _mergingData; }
            set { _mergingData = value; }
        }

        internal DataRelation[] NestedParentRelations
        {
            get
            {
#if DEBUG
                DataRelation[] nRel = FindNestedParentRelations();
                Debug.Assert(nRel.Length == _nestedParentRelations.Length, "nestedParent cache is broken");
                for (int i = 0; i < nRel.Length; i++)
                {
                    Debug.Assert(null != nRel[i], "null relation");
                    Debug.Assert(null != _nestedParentRelations[i], "null relation");
                    Debug.Assert(nRel[i] == _nestedParentRelations[i], "unequal relations");
                }
#endif
                return _nestedParentRelations;
            }
        }

        internal void CacheNestedParent()
        {
            _nestedParentRelations = FindNestedParentRelations();
        }

        private DataRelation[] FindNestedParentRelations()
        {
            List<DataRelation>? nestedParents = null;
            foreach (DataRelation relation in ParentRelations)
            {
                if (relation.Nested)
                {
                    if (null == nestedParents)
                    {
                        nestedParents = new List<DataRelation>();
                    }
                    nestedParents.Add(relation);
                }
            }

            return (null == nestedParents) || (nestedParents.Count == 0) ?
                Array.Empty<DataRelation>() :
                nestedParents.ToArray();
        }

        internal int NestedParentsCount
        {
            get
            {
                int count = 0;
                foreach (DataRelation relation in ParentRelations)
                {
                    if (relation.Nested)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Gets or sets an array of columns that function as primary keys for the data table.
        /// </summary>
        [Editor("Microsoft.VSDesigner.Data.Design.PrimaryKeyEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [TypeConverter(typeof(PrimaryKeyTypeConverter))]
        [AllowNull]
        public DataColumn[] PrimaryKey
        {
            get
            {
                UniqueConstraint? primayKeyConstraint = _primaryKey;
                if (null != primayKeyConstraint)
                {
                    Debug.Assert(2 <= primayKeyConstraint.ConstraintIndex.RefCount, "bad primaryKey index RefCount");
                    return primayKeyConstraint.Key.ToArray();
                }
                return Array.Empty<DataColumn>();
            }
            set
            {
                UniqueConstraint? key = null;
                UniqueConstraint? existingKey;

                // Loading with persisted property
                if (fInitInProgress && value != null)
                {
                    _delayedSetPrimaryKey = value;
                    return;
                }

                if ((value != null) && (value.Length != 0))
                {
                    int count = 0;
                    for (int i = 0; i < value.Length; i++)
                    {
                        if (value[i] != null)
                        {
                            count++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (count != 0)
                    {
                        DataColumn[] newValue = value;
                        if (count != value.Length)
                        {
                            newValue = new DataColumn[count];
                            for (int i = 0; i < count; i++)
                            {
                                newValue[i] = value[i];
                            }
                        }
                        key = new UniqueConstraint(newValue);
                        if (key.Table != this)
                            throw ExceptionBuilder.TableForeignPrimaryKey();
                    }
                }

                if (key == _primaryKey || (key != null && key.Equals(_primaryKey)))
                {
                    return;
                }

                // Use an existing UniqueConstraint that matches if one exists
                if ((existingKey = (UniqueConstraint?)Constraints.FindConstraint(key)) != null)
                {
                    key!.ColumnsReference.CopyTo(existingKey.Key.ColumnsReference, 0);
                    key = existingKey;
                }

                UniqueConstraint? oldKey = _primaryKey;
                _primaryKey = null;
                if (oldKey != null)
                {
                    oldKey.ConstraintIndex.RemoveRef();

                    // if PrimaryKey is removed, reset LoadDataRow indexes
                    if (null != _loadIndex)
                    {
                        _loadIndex.RemoveRef();
                        _loadIndex = null;
                    }
                    if (null != _loadIndexwithOriginalAdded)
                    {
                        _loadIndexwithOriginalAdded.RemoveRef();
                        _loadIndexwithOriginalAdded = null;
                    }
                    if (null != _loadIndexwithCurrentDeleted)
                    {
                        _loadIndexwithCurrentDeleted.RemoveRef();
                        _loadIndexwithCurrentDeleted = null;
                    }
                    Constraints.Remove(oldKey);
                }

                // Add the key if there isnt an existing matching key in collection
                if (key != null && existingKey == null)
                {
                    Constraints.Add(key);
                }

                _primaryKey = key;

                Debug.Assert(_primaryKey == null || Constraints.FindConstraint(_primaryKey) == _primaryKey, "PrimaryKey is not in ConstraintCollection");
                _primaryIndex = (key != null) ? key.Key.GetIndexDesc() : Array.Empty<IndexField>();

                if (_primaryKey != null)
                {
                    // must set index for DataView.Sort before setting AllowDBNull which can fail
                    key!.ConstraintIndex.AddRef();

                    for (int i = 0; i < key.ColumnsReference.Length; i++)
                    {
                        key.ColumnsReference[i].AllowDBNull = false;
                    }
                }
            }
        }

        /// <summary>
        /// Indicates whether the <see cref='System.Data.DataTable.PrimaryKey'/> property should be persisted.
        /// </summary>
        private bool ShouldSerializePrimaryKey() => _primaryKey != null;

        /// <summary>
        /// Resets the <see cref='System.Data.DataTable.PrimaryKey'/> property to its default state.
        /// </summary>
        private void ResetPrimaryKey()
        {
            PrimaryKey = null;
        }

        /// <summary>
        /// Gets the collection of rows that belong to this table.
        /// </summary>
        [Browsable(false)]
        public DataRowCollection Rows => _rowCollection;

        /// <summary>
        /// Gets or sets the name of the table.
        /// </summary>
        [RefreshProperties(RefreshProperties.All)]
        [DefaultValue("")]
        [AllowNull]
        public string TableName
        {
            get { return _tableName; }
            set
            {
                long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.set_TableName|API> {0}, value='{1}'", ObjectID, value);
                try
                {
                    if (value == null)
                    {
                        value = string.Empty;
                    }
                    CultureInfo currentLocale = Locale;
                    if (string.Compare(_tableName, value, true, currentLocale) != 0)
                    {
                        if (_dataSet != null)
                        {
                            if (value.Length == 0)
                            {
                                throw ExceptionBuilder.NoTableName();
                            }
                            if ((0 == string.Compare(value, _dataSet.DataSetName, true, _dataSet.Locale)) && !_fNestedInDataset)
                            {
                                throw ExceptionBuilder.DatasetConflictingName(_dataSet.DataSetName);
                            }

                            DataRelation[] nestedRelations = NestedParentRelations;
                            if (nestedRelations.Length == 0)
                            {
                                _dataSet.Tables.RegisterName(value, Namespace);
                            }
                            else
                            {
                                foreach (DataRelation rel in nestedRelations)
                                {
                                    if (!rel.ParentTable.Columns.CanRegisterName(value))
                                    {
                                        throw ExceptionBuilder.CannotAddDuplicate2(value);
                                    }
                                }
                                // if it cannot register the following line will throw exception
                                _dataSet.Tables.RegisterName(value, Namespace);

                                foreach (DataRelation rel in nestedRelations)
                                {
                                    rel.ParentTable.Columns.RegisterColumnName(value, null);
                                    rel.ParentTable.Columns.UnregisterName(TableName);
                                }
                            }

                            if (_tableName.Length != 0)
                            {
                                _dataSet.Tables.UnregisterName(_tableName);
                            }
                        }
                        RaisePropertyChanging(nameof(TableName));
                        _tableName = value;
                        _encodedTableName = null;
                    }
                    else if (string.Compare(_tableName, value, false, currentLocale) != 0)
                    {
                        RaisePropertyChanging(nameof(TableName));
                        _tableName = value;
                        _encodedTableName = null;
                    }
                }
                finally
                {
                    DataCommonEventSource.Log.ExitScope(logScopeId);
                }
            }
        }

        internal string EncodedTableName
        {
            get
            {
                string? encodedTblName = _encodedTableName;
                if (null == encodedTblName)
                {
                    encodedTblName = XmlConvert.EncodeLocalName(TableName);
                    _encodedTableName = encodedTblName;
                }
                return encodedTblName;
            }
        }

        private string GetInheritedNamespace(List<DataTable> visitedTables)
        {
            // if there is nested relation: ie: this table is nested child of another table and
            // if it is not self nested, return parent tables NS: Meanwhile make sure
            DataRelation[] nestedRelations = NestedParentRelations;
            if (nestedRelations.Length > 0)
            {
                for (int i = 0; i < nestedRelations.Length; i++)
                {
                    DataRelation rel = nestedRelations[i];
                    if (rel.ParentTable._tableNamespace != null)
                    {
                        return rel.ParentTable._tableNamespace; // if parent table has a non-null NS, return it
                    }
                }
                // Assumption, in hierarchy of multiple nested relation, a child table with no NS, has DataRelation
                // only and only with parent DataTable witin the same namespace
                int j = 0;
                while (j < nestedRelations.Length && ((nestedRelations[j].ParentTable == this) || (visitedTables.Contains(nestedRelations[j].ParentTable))))
                {
                    j++;
                }
                if (j < nestedRelations.Length)
                {
                    DataTable parentTable = nestedRelations[j].ParentTable;
                    if (!visitedTables.Contains(parentTable))
                        visitedTables.Add(parentTable);
                    return parentTable.GetInheritedNamespace(visitedTables); // this is the same as return parentTable.Namespace
                }
            } // dont put else

            if (DataSet != null)
            {
                // if it cant return from parent tables, return NS from dataset, if exists
                return DataSet.Namespace;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the namespace for the <see cref='System.Data.DataTable'/>.
        /// </summary>
        [AllowNull]
        public string Namespace
        {
            get { return _tableNamespace ?? GetInheritedNamespace(new List<DataTable>()); }
            set
            {
                long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.set_Namespace|API> {0}, value='{1}'", ObjectID, value);
                try
                {
                    if (value != _tableNamespace)
                    {
                        if (_dataSet != null)
                        {
                            string realNamespace = value ?? GetInheritedNamespace(new List<DataTable>());
                            if (realNamespace != Namespace)
                            {
                                // do this extra check only if the namespace is really going to change
                                // inheritance-wise.
                                if (_dataSet.Tables.Contains(TableName, realNamespace, true, true))
                                    throw ExceptionBuilder.DuplicateTableName2(TableName, realNamespace);

                                CheckCascadingNamespaceConflict(realNamespace);
                            }
                        }
                        CheckNamespaceValidityForNestedRelations(value);
                        DoRaiseNamespaceChange();
                    }
                    _tableNamespace = value;
                }
                finally
                {
                    DataCommonEventSource.Log.ExitScope(logScopeId);
                }
            }
        }
        internal bool IsNamespaceInherited() => null == _tableNamespace;

        internal void CheckCascadingNamespaceConflict(string realNamespace)
        {
            foreach (DataRelation rel in ChildRelations)
            {
                if ((rel.Nested) && (rel.ChildTable != this) && (rel.ChildTable._tableNamespace == null))
                {
                    DataTable childTable = rel.ChildTable;
                    if (_dataSet!.Tables.Contains(childTable.TableName, realNamespace, false, true))
                        throw ExceptionBuilder.DuplicateTableName2(TableName, realNamespace);

                    childTable.CheckCascadingNamespaceConflict(realNamespace);
                }
            }
        }

        internal void CheckNamespaceValidityForNestedRelations(string? realNamespace)
        {
            foreach (DataRelation rel in ChildRelations)
            {
                if (rel.Nested)
                {
                    if (realNamespace != null)
                    {
                        rel.ChildTable.CheckNamespaceValidityForNestedParentRelations(realNamespace, this);
                    }
                    else
                    {
                        rel.ChildTable.CheckNamespaceValidityForNestedParentRelations(GetInheritedNamespace(new List<DataTable>()), this);
                    }
                }
            }

            if (realNamespace == null)
            {
                // this will affect this table if it has parent relations
                CheckNamespaceValidityForNestedParentRelations(GetInheritedNamespace(new List<DataTable>()), this);
            }
        }
        internal void CheckNamespaceValidityForNestedParentRelations(string ns, DataTable parentTable)
        {
            foreach (DataRelation rel in ParentRelations)
            {
                if (rel.Nested)
                {
                    if (rel.ParentTable != parentTable && rel.ParentTable.Namespace != ns)
                    {
                        throw ExceptionBuilder.InValidNestedRelation(TableName);
                    }
                }
            }
        }

        internal void DoRaiseNamespaceChange()
        {
            RaisePropertyChanging(nameof(Namespace));
            // raise column Namespace change

            foreach (DataColumn col in Columns)
            {
                if (col._columnUri == null)
                {
                    col.RaisePropertyChanging(nameof(Namespace));
                }
            }

            foreach (DataRelation rel in ChildRelations)
            {
                if ((rel.Nested) && (rel.ChildTable != this))
                {
                    rel.ChildTable.DoRaiseNamespaceChange();
                }
            }
        }

        /// <summary>
        /// Indicates whether the <see cref='System.Data.DataTable.Namespace'/> property should be persisted.
        /// </summary>
        private bool ShouldSerializeNamespace() => _tableNamespace != null;

        /// <summary>
        /// Resets the <see cref='System.Data.DataTable.Namespace'/> property to its default state.
        /// </summary>
        private void ResetNamespace()
        {
            Namespace = null;
        }

        public virtual void BeginInit()
        {
            fInitInProgress = true;
        }

        public virtual void EndInit()
        {
            if (_dataSet == null || !_dataSet._fInitInProgress)
            {
                Columns.FinishInitCollection();
                Constraints.FinishInitConstraints();
                foreach (DataColumn dc in Columns)
                {
                    if (dc.Computed)
                    {
                        dc.CopyExpressionFrom(dc);
                    }
                }
            }

            fInitInProgress = false; // It is must that we set off this flag after calling FinishInitxxx();
            if (_delayedSetPrimaryKey != null)
            {
                PrimaryKey = _delayedSetPrimaryKey;
                _delayedSetPrimaryKey = null;
            }

            if (_delayedViews.Count > 0)
            {
                foreach (DataView dv in _delayedViews)
                {
                    dv.EndInit();
                }
                _delayedViews.Clear();
            }

            OnInitialized();
        }

        [DefaultValue("")]
        [AllowNull]
        public string Prefix
        {
            get { return _tablePrefix; }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                DataCommonEventSource.Log.Trace("<ds.DataTable.set_Prefix|API> {0}, value='{1}'", ObjectID, value);
                if ((XmlConvert.DecodeName(value) == value) && (XmlConvert.EncodeName(value) != value))
                {
                    throw ExceptionBuilder.InvalidPrefix(value);
                }

                _tablePrefix = value;
            }
        }

        internal DataColumn? XmlText
        {
            get { return _xmlText; }
            set
            {
                if (_xmlText != value)
                {
                    if (_xmlText != null)
                    {
                        if (value != null)
                        {
                            throw ExceptionBuilder.MultipleTextOnlyColumns();
                        }
                        Columns.Remove(_xmlText);
                    }
                    else
                    {
                        Debug.Assert(value != null, "Value shoud not be null ??");
                        Debug.Assert(value.ColumnMapping == MappingType.SimpleContent, "should be text node here");
                        if (value != Columns[value.ColumnName])
                        {
                            Columns.Add(value);
                        }
                    }
                    _xmlText = value;
                }
            }
        }

        internal decimal MaxOccurs
        {
            get { return _maxOccurs; }
            set { _maxOccurs = value; }
        }

        internal decimal MinOccurs
        {
            get { return _minOccurs; }
            set { _minOccurs = value; }
        }

        internal static void SetKeyValues(DataKey key, object[] keyValues, int record)
        {
            for (int i = 0; i < keyValues.Length; i++)
            {
                key.ColumnsReference[i][record] = keyValues[i];
            }
        }

        internal DataRow? FindByIndex(Index ndx, object[] key)
        {
            Range range = ndx.FindRecords(key);
            return range.IsNull ? null : _recordManager[ndx.GetRecord(range.Min)];
        }

        internal DataRow? FindMergeTarget(DataRow row, DataKey key, Index ndx)
        {
            DataRow? targetRow = null;

            // Primary key match
            if (key.HasValue)
            {
                Debug.Assert(ndx != null);
                int findRecord = (row._oldRecord == -1) ? row._newRecord : row._oldRecord;
                object[] values = key.GetKeyValues(findRecord);
                targetRow = FindByIndex(ndx, values);
            }
            return targetRow;
        }

        private void SetMergeRecords(DataRow row, int newRecord, int oldRecord, DataRowAction action)
        {
            if (newRecord != -1)
            {
                SetNewRecord(row, newRecord, action, true, true);
                SetOldRecord(row, oldRecord);
            }
            else
            {
                SetOldRecord(row, oldRecord);
                if (row._newRecord != -1)
                {
                    Debug.Assert(action == DataRowAction.Delete, "Unexpected SetNewRecord action in merge function.");
                    SetNewRecord(row, newRecord, action, true, true);
                }
            }
        }

        internal DataRow MergeRow(DataRow row, DataRow? targetRow, bool preserveChanges, Index? idxSearch)
        {
            if (targetRow == null)
            {
                targetRow = NewEmptyRow();
                targetRow._oldRecord = _recordManager.ImportRecord(row.Table, row._oldRecord);
                targetRow._newRecord = targetRow._oldRecord;
                if (row._oldRecord != row._newRecord)
                {
                    targetRow._newRecord = _recordManager.ImportRecord(row.Table, row._newRecord);
                }
                InsertRow(targetRow, -1);
            }
            else
            {
                // Record Manager corruption during Merge when target row in edit state
                // the newRecord would be freed and overwrite tempRecord (which became the newRecord)
                // this would leave the DataRow referencing a freed record and leaking memory for the now lost record
                int proposedRecord = targetRow._tempRecord; // by saving off the tempRecord, EndEdit won't free newRecord
                targetRow._tempRecord = -1;
                try
                {
                    DataRowState saveRowState = targetRow.RowState;
                    int saveIdxRecord = (saveRowState == DataRowState.Added) ? targetRow._newRecord : targetRow._oldRecord;
                    int newRecord;
                    int oldRecord;
                    if (targetRow.RowState == DataRowState.Unchanged && row.RowState == DataRowState.Unchanged)
                    {
                        // unchanged row merging with unchanged row
                        oldRecord = targetRow._oldRecord;
                        newRecord = (preserveChanges) ? _recordManager.CopyRecord(this, oldRecord, -1) : targetRow._newRecord;
                        oldRecord = _recordManager.CopyRecord(row.Table, row._oldRecord, targetRow._oldRecord);
                        SetMergeRecords(targetRow, newRecord, oldRecord, DataRowAction.Change);
                    }
                    else if (row._newRecord == -1)
                    {
                        // Incoming row is deleted
                        oldRecord = targetRow._oldRecord;
                        if (preserveChanges)
                        {
                            newRecord = (targetRow.RowState == DataRowState.Unchanged) ? _recordManager.CopyRecord(this, oldRecord, -1) : targetRow._newRecord;
                        }
                        else
                            newRecord = -1;
                        oldRecord = _recordManager.CopyRecord(row.Table, row._oldRecord, oldRecord);

                        // Change index record, need to update index
                        if (saveIdxRecord != ((saveRowState == DataRowState.Added) ? newRecord : oldRecord))
                        {
                            SetMergeRecords(targetRow, newRecord, oldRecord, (newRecord == -1) ? DataRowAction.Delete : DataRowAction.Change);
                            idxSearch!.Reset();
                            saveIdxRecord = ((saveRowState == DataRowState.Added) ? newRecord : oldRecord);
                        }
                        else
                        {
                            SetMergeRecords(targetRow, newRecord, oldRecord, (newRecord == -1) ? DataRowAction.Delete : DataRowAction.Change);
                        }
                    }
                    else
                    {
                        // incoming row is added, modified or unchanged (targetRow is not unchanged)
                        oldRecord = targetRow._oldRecord;
                        newRecord = targetRow._newRecord;
                        if (targetRow.RowState == DataRowState.Unchanged)
                        {
                            newRecord = _recordManager.CopyRecord(this, oldRecord, -1);
                        }
                        oldRecord = _recordManager.CopyRecord(row.Table, row._oldRecord, oldRecord);

                        if (!preserveChanges)
                        {
                            newRecord = _recordManager.CopyRecord(row.Table, row._newRecord, newRecord);
                        }
                        SetMergeRecords(targetRow, newRecord, oldRecord, DataRowAction.Change);
                    }

                    if (saveRowState == DataRowState.Added && targetRow._oldRecord != -1)
                    {
                        idxSearch!.Reset();
                    }

                    Debug.Assert(saveIdxRecord == ((saveRowState == DataRowState.Added) ? targetRow._newRecord : targetRow._oldRecord), "oops, you change index record without noticing it");
                }
                finally
                {
                    targetRow._tempRecord = proposedRecord;
                }
            }

            // Merge all errors
            if (row.HasErrors)
            {
                if (targetRow.RowError.Length == 0)
                {
                    targetRow.RowError = row.RowError;
                }
                else
                {
                    targetRow.RowError += " ]:[ " + row.RowError;
                }
                DataColumn[] cols = row.GetColumnsInError();

                for (int i = 0; i < cols.Length; i++)
                {
                    DataColumn col = targetRow.Table.Columns[cols[i].ColumnName]!;
                    targetRow.SetColumnError(col, row.GetColumnError(cols[i]));
                }
            }
            else
            {
                if (!preserveChanges)
                {
                    targetRow.ClearErrors();
                }
            }

            return targetRow;
        }

        /// <summary>
        /// Commits all the changes made to this table since the last time <see cref='System.Data.DataTable.AcceptChanges'/> was called.
        /// </summary>
        public void AcceptChanges()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.AcceptChanges|API> {0}", ObjectID);
            try
            {
                DataRow[] oldRows = new DataRow[Rows.Count];
                Rows.CopyTo(oldRows, 0);

                // delay updating of indexes until after all
                // AcceptChange calls have been completed
                SuspendIndexEvents();
                try
                {
                    for (int i = 0; i < oldRows.Length; ++i)
                    {
                        if (oldRows[i].rowID != -1)
                        {
                            oldRows[i].AcceptChanges();
                        }
                    }
                }
                finally
                {
                    RestoreIndexEvents(false);
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        // Prevent inlining so that reflection calls are not moved to caller that may be in a different assembly that may have a different grant set.
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected virtual DataTable CreateInstance() => (DataTable)Activator.CreateInstance(GetType(), true)!;

        public virtual DataTable Clone() => Clone(null);

        internal DataTable Clone(DataSet? cloneDS)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.Clone|INFO> {0}, cloneDS={1}", ObjectID, (cloneDS != null) ? cloneDS.ObjectID : 0);
            try
            {
                DataTable clone = CreateInstance();
                if (clone.Columns.Count > 0) // To clean up all the schema in strong typed dataset.
                {
                    clone.Reset();
                }
                return CloneTo(clone, cloneDS, false);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        private static DataTable IncrementalCloneTo(DataTable sourceTable, DataTable targetTable)
        {
            foreach (DataColumn dc in sourceTable.Columns)
            {
                if (targetTable.Columns[dc.ColumnName] == null)
                {
                    targetTable.Columns.Add(dc.Clone());
                }
            }

            return targetTable;
        }

        private DataTable CloneHierarchy(DataTable sourceTable, DataSet ds, Hashtable? visitedMap)
        {
            if (visitedMap == null)
            {
                visitedMap = new Hashtable();
            }
            if (visitedMap.Contains(sourceTable))
            {
                return ((DataTable)visitedMap[sourceTable]!);
            }

            DataTable? destinationTable = ds.Tables[sourceTable.TableName, sourceTable.Namespace];

            if ((destinationTable != null && destinationTable.Columns.Count > 0))
            {
                destinationTable = IncrementalCloneTo(sourceTable, destinationTable);
                // get extra columns from source into destination , increamental read
            }
            else
            {
                if (destinationTable == null)
                {
                    destinationTable = new DataTable();
                    // fxcop: new DataTable values for CaseSensitive, Locale, Namespace will come from CloneTo
                    ds.Tables.Add(destinationTable);
                }
                destinationTable = sourceTable.CloneTo(destinationTable, ds, true);
            }
            visitedMap[sourceTable] = destinationTable;

            // start cloning relation
            foreach (DataRelation r in sourceTable.ChildRelations)
            {
                CloneHierarchy(r.ChildTable, ds, visitedMap);
            }

            return destinationTable;
        }

        private DataTable CloneTo(DataTable clone, DataSet? cloneDS, bool skipExpressionColumns)
        {
            // we do clone datatables while we do readxmlschema, so we do not want to clone columnexpressions if we call this from ReadXmlSchema
            // it will cause exception to be thrown in cae expression refers to a table that is not in hirerachy or not created yet
            Debug.Assert(clone != null, "The table passed in has to be newly created empty DataTable.");

            // set All properties
            clone._tableName = _tableName;

            clone._tableNamespace = _tableNamespace;
            clone._tablePrefix = _tablePrefix;
            clone._fNestedInDataset = _fNestedInDataset;

            clone._culture = _culture;
            clone._cultureUserSet = _cultureUserSet;
            clone._compareInfo = _compareInfo;
            clone._compareFlags = _compareFlags;
            clone._formatProvider = _formatProvider;
            clone._hashCodeProvider = _hashCodeProvider;
            clone._caseSensitive = _caseSensitive;
            clone._caseSensitiveUserSet = _caseSensitiveUserSet;

            clone._displayExpression = _displayExpression;
            clone._typeName = _typeName; //enzol
            clone._repeatableElement = _repeatableElement; //enzol
            clone.MinimumCapacity = MinimumCapacity;
            clone.RemotingFormat = RemotingFormat;

            // add all columns
            DataColumnCollection clmns = Columns;
            for (int i = 0; i < clmns.Count; i++)
            {
                clone.Columns.Add(clmns[i].Clone());
            }

            // add all expressions if Clone is invoked only on DataTable otherwise DataSet.Clone will assign expressions after creating all relationships.
            if (!skipExpressionColumns && cloneDS == null)
            {
                for (int i = 0; i < clmns.Count; i++)
                {
                    clone.Columns[clmns[i].ColumnName]!.CopyExpressionFrom(clmns[i]);
                }
            }

            // Create PrimaryKey
            DataColumn[] pkey = PrimaryKey;
            if (pkey.Length > 0)
            {
                DataColumn[] key = new DataColumn[pkey.Length];
                for (int i = 0; i < pkey.Length; i++)
                {
                    key[i] = clone.Columns[pkey[i].Ordinal];
                }
                clone.PrimaryKey = key;
            }

            // now clone all unique constraints
            // Rename first
            for (int j = 0; j < Constraints.Count; j++)
            {
                ForeignKeyConstraint? foreign = Constraints[j] as ForeignKeyConstraint;
                UniqueConstraint? unique = Constraints[j] as UniqueConstraint;
                if (foreign != null)
                {
                    if (foreign.Table == foreign.RelatedTable)
                    {
                        if (foreign.Clone(clone) is ForeignKeyConstraint clonedConstraint &&
                            clone.Constraints.FindConstraint(clonedConstraint) is Constraint oldConstraint)
                        {
                            oldConstraint.ConstraintName = Constraints[j].ConstraintName;
                        }
                    }
                }
                else if (unique != null)
                {
                    if (unique.Clone(clone) is UniqueConstraint clonedConstraint &&
                        clone.Constraints.FindConstraint(clonedConstraint) is Constraint oldConstraint)
                    {
                        oldConstraint.ConstraintName = Constraints[j].ConstraintName;
                        foreach (object key in clonedConstraint.ExtendedProperties.Keys)
                        {
                            oldConstraint.ExtendedProperties[key] = clonedConstraint.ExtendedProperties[key];
                        }
                    }
                }
            }

            // then add
            for (int j = 0; j < Constraints.Count; j++)
            {
                if (!clone.Constraints.Contains(Constraints[j].ConstraintName, true))
                {
                    ForeignKeyConstraint? foreign = Constraints[j] as ForeignKeyConstraint;
                    UniqueConstraint? unique = Constraints[j] as UniqueConstraint;
                    if (foreign != null)
                    {
                        if (foreign.Table == foreign.RelatedTable &&
                            foreign.Clone(clone) is ForeignKeyConstraint newforeign)
                        {
                            // we cant make sure that we receive a cloned FKC,since it depends if table and relatedtable be the same
                            clone.Constraints.Add(newforeign);
                        }
                    }
                    else if (unique != null)
                    {
                        clone.Constraints.Add(unique.Clone(clone)!);
                    }
                }
            }

            // ...Extended Properties...

            if (_extendedProperties != null)
            {
                foreach (object key in _extendedProperties.Keys)
                {
                    clone.ExtendedProperties[key] = _extendedProperties[key];
                }
            }

            return clone;
        }

        public DataTable Copy()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.Copy|API> {0}", ObjectID);
            try
            {
                DataTable destTable = Clone();

                foreach (DataRow row in Rows)
                {
                    CopyRow(destTable, row);
                }

                return destTable;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Occurs when a value has been submitted for this column.
        /// </summary>
        public event DataColumnChangeEventHandler? ColumnChanging
        {
            add
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.add_ColumnChanging|API> {0}", ObjectID);
                _onColumnChangingDelegate += value;
            }
            remove
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.remove_ColumnChanging|API> {0}", ObjectID);
                _onColumnChangingDelegate -= value;
            }
        }

        public event DataColumnChangeEventHandler? ColumnChanged
        {
            add
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.add_ColumnChanged|API> {0}", ObjectID);
                _onColumnChangedDelegate += value;
            }
            remove
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.remove_ColumnChanged|API> {0}", ObjectID);
                _onColumnChangedDelegate -= value;
            }
        }

        public event EventHandler? Initialized
        {
            add { _onInitialized += value; }
            remove { _onInitialized -= value; }
        }

        internal event PropertyChangedEventHandler PropertyChanging
        {
            add
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.add_PropertyChanging|INFO> {0}", ObjectID);
                _onPropertyChangingDelegate += value;
            }
            remove
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.remove_PropertyChanging|INFO> {0}", ObjectID);
                _onPropertyChangingDelegate -= value;
            }
        }

        /// <summary>
        /// Occurs after a row in the table has been successfully edited.
        /// </summary>
        public event DataRowChangeEventHandler? RowChanged
        {
            add
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.add_RowChanged|API> {0}", ObjectID);
                _onRowChangedDelegate += value;
            }
            remove
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.remove_RowChanged|API> {0}", ObjectID);
                _onRowChangedDelegate -= value;
            }
        }

        /// <summary>
        /// Occurs when the <see cref='System.Data.DataRow'/> is changing.
        /// </summary>
        public event DataRowChangeEventHandler? RowChanging
        {
            add
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.add_RowChanging|API> {0}", ObjectID);
                _onRowChangingDelegate += value;
            }
            remove
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.remove_RowChanging|API> {0}", ObjectID);
                _onRowChangingDelegate -= value;
            }
        }

        /// <summary>
        /// Occurs before a row in the table is about to be deleted.
        /// </summary>
        public event DataRowChangeEventHandler? RowDeleting
        {
            add
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.add_RowDeleting|API> {0}", ObjectID);
                _onRowDeletingDelegate += value;
            }
            remove
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.remove_RowDeleting|API> {0}", ObjectID);
                _onRowDeletingDelegate -= value;
            }
        }

        /// <summary>
        /// Occurs after a row in the table has been deleted.
        /// </summary>
        public event DataRowChangeEventHandler? RowDeleted
        {
            add
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.add_RowDeleted|API> {0}", ObjectID);
                _onRowDeletedDelegate += value;
            }
            remove
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.remove_RowDeleted|API> {0}", ObjectID);
                _onRowDeletedDelegate -= value;
            }
        }

        public event DataTableClearEventHandler? TableClearing
        {
            add
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.add_TableClearing|API> {0}", ObjectID);
                _onTableClearingDelegate += value;
            }
            remove
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.remove_TableClearing|API> {0}", ObjectID);
                _onTableClearingDelegate -= value;
            }
        }

        public event DataTableClearEventHandler? TableCleared
        {
            add
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.add_TableCleared|API> {0}", ObjectID);
                _onTableClearedDelegate += value;
            }
            remove
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.remove_TableCleared|API> {0}", ObjectID);
                _onTableClearedDelegate -= value;
            }
        }

        public event DataTableNewRowEventHandler? TableNewRow
        {
            add
            {
                _onTableNewRowDelegate += value;
            }
            remove
            {
                _onTableNewRowDelegate -= value;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override ISite? Site
        {
            get { return base.Site; }
            set
            {
                ISite? oldSite = Site;
                if (value == null && oldSite != null)
                {
                    IContainer? cont = oldSite.Container;

                    if (cont != null)
                    {
                        for (int i = 0; i < Columns.Count; i++)
                        {
                            if (Columns[i].Site != null)
                            {
                                cont.Remove(Columns[i]);
                            }
                        }
                    }
                }
                base.Site = value;
            }
        }

        internal DataRow AddRecords(int oldRecord, int newRecord)
        {
            DataRow row;
            if (oldRecord == -1 && newRecord == -1)
            {
                row = NewRow(-1);
                AddRow(row);
            }
            else
            {
                row = NewEmptyRow();
                row._oldRecord = oldRecord;
                row._newRecord = newRecord;
                InsertRow(row, -1);
            }
            return row;
        }

        internal void AddRow(DataRow row) => AddRow(row, -1);

        internal void AddRow(DataRow row, int proposedID) => InsertRow(row, proposedID, -1);

        internal void InsertRow(DataRow row, int proposedID, int pos) => InsertRow(row, proposedID, pos, fireEvent: true);

        internal void InsertRow(DataRow row, long proposedID, int pos, bool fireEvent)
        {
            Exception? deferredException;

            if (row == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(row));
            }
            if (row.Table != this)
            {
                throw ExceptionBuilder.RowAlreadyInOtherCollection();
            }
            if (row.rowID != -1)
            {
                throw ExceptionBuilder.RowAlreadyInTheCollection();
            }
            row.BeginEdit(); // ensure something's there.

            int record = row._tempRecord;
            row._tempRecord = -1;

            if (proposedID == -1)
            {
                proposedID = _nextRowID;
            }

            bool rollbackOnException;
            if (rollbackOnException = (_nextRowID <= proposedID))
            {
                _nextRowID = checked(proposedID + 1);
            }

            try
            {
                try
                {
                    row.rowID = proposedID;
                    // this method may cause DataView.OnListChanged in which another row may be added
                    SetNewRecordWorker(row, record, DataRowAction.Add, false, false, pos, fireEvent, out deferredException); // now we do add the row to collection before OnRowChanged (RaiseRowChanged)
                }
                catch
                {
                    if (rollbackOnException && (_nextRowID == proposedID + 1))
                    {
                        _nextRowID = proposedID;
                    }
                    row.rowID = -1;
                    row._tempRecord = record;
                    throw;
                }

                // since expression evaluation occurred in SetNewRecordWorker, there may have been a problem that
                // was deferred to this point.  If so, throw now since row has already been added.
                if (deferredException != null)
                    throw deferredException;

                if (EnforceConstraints)
                {
                    // if we are evaluating expression, we need to validate constraints
                    int columnCount = _columnCollection.Count;
                    for (int i = 0; i < columnCount; ++i)
                    {
                        DataColumn column = _columnCollection[i];
                        if (column.Computed)
                        {
                            column.CheckColumnConstraint(row, DataRowAction.Add);
                        }
                    }
                }
            }
            finally
            {
                row.ResetLastChangedColumn(); // if expression is evaluated while adding, before  return, we want to clear it
            }
        }

        internal static void CheckNotModifying(DataRow row)
        {
            if (row._tempRecord != -1)
            {
                row.EndEdit();
            }
        }

        /// <summary>
        /// Clears the table of all data.
        /// </summary>
        public void Clear() => Clear(true);

        internal void Clear(bool clearAll)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.Clear|INFO> {0}, clearAll={1}", ObjectID, clearAll);
            try
            {
                Debug.Assert(null == _rowDiffId, "wasn't previously cleared");
                _rowDiffId = null;

                if (_dataSet != null)
                    _dataSet.OnClearFunctionCalled(this);
                bool shouldFireClearEvents = (Rows.Count != 0); // if Rows is already empty, this is noop

                DataTableClearEventArgs? e = null;
                if (shouldFireClearEvents)
                {
                    e = new DataTableClearEventArgs(this);
                    OnTableClearing(e);
                }

                if (_dataSet != null && _dataSet.EnforceConstraints)
                {
                    for (ParentForeignKeyConstraintEnumerator constraints = new ParentForeignKeyConstraintEnumerator(_dataSet, this); constraints.GetNext();)
                    {
                        ForeignKeyConstraint constraint = constraints.GetForeignKeyConstraint();
                        constraint.CheckCanClearParentTable(this);
                    }
                }

                _recordManager.Clear(clearAll);

                // this improves performance by iterating over rows instead of computing by index
                foreach (DataRow row in Rows)
                {
                    row._oldRecord = -1;
                    row._newRecord = -1;
                    row._tempRecord = -1;
                    row.rowID = -1;
                    row.RBTreeNodeId = 0;
                }
                Rows.ArrayClear();

                ResetIndexes();

                if (shouldFireClearEvents)
                {
                    OnTableCleared(e!);
                }

                foreach (DataColumn column in Columns)
                {
                    EvaluateDependentExpressions(column);
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal void CascadeAll(DataRow row, DataRowAction action)
        {
            if (_dataSet != null && _dataSet._fEnableCascading)
            {
                for (ParentForeignKeyConstraintEnumerator constraints = new ParentForeignKeyConstraintEnumerator(_dataSet, this); constraints.GetNext();)
                {
                    constraints.GetForeignKeyConstraint().CheckCascade(row, action);
                }
            }
        }

        internal void CommitRow(DataRow row)
        {
            // Fire Changing event
            DataRowChangeEventArgs? drcevent = OnRowChanging(null, row, DataRowAction.Commit);

            if (!_inDataLoad)
            {
                CascadeAll(row, DataRowAction.Commit);
            }

            SetOldRecord(row, row._newRecord);

            OnRowChanged(drcevent, row, DataRowAction.Commit);
        }

        internal int Compare(string s1, string s2) => Compare(s1, s2, null);

        internal int Compare(string s1, string s2, CompareInfo? comparer)
        {
            object obj1 = s1;
            object obj2 = s2;
            if (obj1 == obj2)
            {
                return 0;
            }
            if (obj1 == null)
            {
                return -1;
            }
            if (obj2 == null)
            {
                return 1;
            }

            int leng1 = s1.Length;
            int leng2 = s2.Length;

            for (; leng1 > 0; leng1--)
            {
                if (s1[leng1 - 1] != 0x20 && s1[leng1 - 1] != 0x3000) // 0x3000 is Ideographic Whitespace
                {
                    break;
                }
            }
            for (; leng2 > 0; leng2--)
            {
                if (s2[leng2 - 1] != 0x20 && s2[leng2 - 1] != 0x3000)
                {
                    break;
                }
            }

            return (comparer ?? CompareInfo).Compare(s1, 0, leng1, s2, 0, leng2, _compareFlags);
        }

        internal int IndexOf(string s1, string s2) => CompareInfo.IndexOf(s1, s2, _compareFlags);

        internal bool IsSuffix(string s1, string s2) => CompareInfo.IsSuffix(s1, s2, _compareFlags);

        /// <summary>
        /// Computes the given expression on the current rows that pass the filter criteria.
        /// </summary>
        [RequiresUnreferencedCode("Members of types used in the filter or expression might be trimmed.")]
        public object Compute(string? expression, string? filter)
        {
            DataRow[] rows = Select(filter, "", DataViewRowState.CurrentRows);
            DataExpression expr = new DataExpression(this, expression);
            return expr.Evaluate(rows);
        }

        bool IListSource.ContainsListCollection => false;

        internal static void CopyRow(DataTable table, DataRow row)
        {
            int oldRecord = -1, newRecord = -1;

            if (row == null)
            {
                return;
            }

            if (row._oldRecord != -1)
            {
                oldRecord = table._recordManager.ImportRecord(row.Table, row._oldRecord);
            }
            if (row._newRecord != -1)
            {
                if (row._newRecord != row._oldRecord)
                {
                    newRecord = table._recordManager.ImportRecord(row.Table, row._newRecord);
                }
                else
                {
                    newRecord = oldRecord;
                }
            }

            DataRow targetRow = table.AddRecords(oldRecord, newRecord);

            if (row.HasErrors)
            {
                targetRow.RowError = row.RowError;

                DataColumn[] cols = row.GetColumnsInError();

                for (int i = 0; i < cols.Length; i++)
                {
                    DataColumn col = targetRow.Table.Columns[cols[i].ColumnName]!;
                    targetRow.SetColumnError(col, row.GetColumnError(cols[i]));
                }
            }
        }

        internal void DeleteRow(DataRow row)
        {
            if (row._newRecord == -1)
            {
                throw ExceptionBuilder.RowAlreadyDeleted();
            }

            // Store.PrepareForDelete(row);
            SetNewRecord(row, -1, DataRowAction.Delete, false, true);
        }

        private void CheckPrimaryKey()
        {
            if (_primaryKey == null) throw ExceptionBuilder.TableMissingPrimaryKey();
        }

        internal DataRow? FindByPrimaryKey(object?[] values)
        {
            CheckPrimaryKey();
            return FindRow(_primaryKey!.Key, values);
        }

        internal DataRow? FindByPrimaryKey(object? value)
        {
            CheckPrimaryKey();
            return FindRow(_primaryKey!.Key, value);
        }

        private DataRow? FindRow(DataKey key, object?[] values)
        {
            Index index = GetIndex(NewIndexDesc(key));
            Range range = index.FindRecords(values);
            if (range.IsNull)
            {
                return null;
            }
            return _recordManager[index.GetRecord(range.Min)];
        }

        private DataRow? FindRow(DataKey key, object? value)
        {
            Index index = GetIndex(NewIndexDesc(key));
            Range range = index.FindRecords(value);
            if (range.IsNull)
            {
                return null;
            }
            return _recordManager[index.GetRecord(range.Min)];
        }

        internal static string FormatSortString(IndexField[] indexDesc)
        {
            var builder = new StringBuilder();
            foreach (IndexField field in indexDesc)
            {
                if (0 < builder.Length)
                {
                    builder.Append(", ");
                }
                builder.Append(field.Column.ColumnName);
                if (field.IsDescending)
                {
                    builder.Append(" DESC");
                }
            }
            return builder.ToString();
        }

        internal void FreeRecord(ref int record)
        {
            _recordManager.FreeRecord(ref record);
        }

        public DataTable? GetChanges()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.GetChanges|API> {0}", ObjectID);
            try
            {
                DataTable dtChanges = Clone();
                DataRow? row = null;

                for (int i = 0; i < Rows.Count; i++)
                {
                    row = Rows[i];
                    if (row._oldRecord != row._newRecord)
                    {
                        dtChanges.ImportRow(row);
                    }
                }

                if (dtChanges.Rows.Count == 0)
                {
                    return null;
                }

                return dtChanges;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public DataTable? GetChanges(DataRowState rowStates)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.GetChanges|API> {0}, rowStates={1}", ObjectID, rowStates);
            try
            {
                DataTable dtChanges = Clone();
                DataRow? row = null;

                // check that rowStates is valid DataRowState
                Debug.Assert(Enum.GetUnderlyingType(typeof(DataRowState)) == typeof(int), "Invalid DataRowState type");

                for (int i = 0; i < Rows.Count; i++)
                {
                    row = Rows[i];
                    if ((row.RowState & rowStates) != 0)
                    {
                        dtChanges.ImportRow(row);
                    }
                }

                if (dtChanges.Rows.Count == 0)
                {
                    return null;
                }

                return dtChanges;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Returns an array of <see cref='System.Data.DataRow'/> objects that contain errors.
        /// </summary>
        public DataRow[] GetErrors()
        {
            List<DataRow> errorList = new List<DataRow>();

            for (int i = 0; i < Rows.Count; i++)
            {
                DataRow row = Rows[i];
                if (row.HasErrors)
                {
                    errorList.Add(row);
                }
            }

            DataRow[] temp = NewRowArray(errorList.Count);
            errorList.CopyTo(temp);
            return temp;
        }

        internal Index GetIndex(IndexField[] indexDesc) =>
            GetIndex(indexDesc, DataViewRowState.CurrentRows, null);

        internal Index GetIndex(string sort, DataViewRowState recordStates, IFilter? rowFilter) =>
            GetIndex(ParseSortString(sort), recordStates, rowFilter);

        internal Index GetIndex(IndexField[] indexDesc, DataViewRowState recordStates, IFilter? rowFilter)
        {
            _indexesLock.EnterUpgradeableReadLock();
            try
            {
                for (int i = 0; i < _indexes.Count; i++)
                {
                    Index index = _indexes[i];
                    if (index != null)
                    {
                        if (index.Equal(indexDesc, recordStates, rowFilter))
                        {
                            return index;
                        }
                    }
                }
            }
            finally
            {
                _indexesLock.ExitUpgradeableReadLock();
            }
            Index ndx = new Index(this, indexDesc, recordStates, rowFilter);
            ndx.AddRef();
            return ndx;
        }

        IList IListSource.GetList() => DefaultView;

        internal List<DataViewListener> GetListeners() => _dataViewListeners;

        // We need a HashCodeProvider for Case, Kana and Width insensitive
        internal int GetSpecialHashCode(string name)
        {
            int i;
            for (i = 0; (i < name.Length) && (0x3000 > name[i]); ++i) ;

            if (name.Length == i)
            {
                if (null == _hashCodeProvider)
                {
                    // it should use the CaseSensitive property, but V1 shipped this way
                    _hashCodeProvider = StringComparer.Create(Locale, true);
                }
                return _hashCodeProvider.GetHashCode(name);
            }
            else
            {
                return 0;
            }
        }

        public void ImportRow(DataRow? row)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.ImportRow|API> {0}", ObjectID);
            try
            {
                int oldRecord = -1, newRecord = -1;

                if (row == null)
                {
                    return;
                }

                if (row._oldRecord != -1)
                {
                    oldRecord = _recordManager.ImportRecord(row.Table, row._oldRecord);
                }
                if (row._newRecord != -1)
                {  // row not deleted
                    if (row.RowState != DataRowState.Unchanged)
                    { // not unchanged, it means Added or modified
                        newRecord = _recordManager.ImportRecord(row.Table, row._newRecord);
                    }
                    else
                    {
                        newRecord = oldRecord;
                    }
                }

                if (oldRecord != -1 || newRecord != -1)
                {
                    DataRow targetRow = AddRecords(oldRecord, newRecord);

                    if (row.HasErrors)
                    {
                        targetRow.RowError = row.RowError;

                        DataColumn[] cols = row.GetColumnsInError();

                        for (int i = 0; i < cols.Length; i++)
                        {
                            DataColumn col = targetRow.Table.Columns[cols[i].ColumnName]!;
                            targetRow.SetColumnError(col, row.GetColumnError(cols[i]));
                        }
                    }
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal void InsertRow(DataRow row, long proposedID)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.InsertRow|INFO> {0}, row={1}", ObjectID, row._objectID);
            try
            {
                if (row.Table != this)
                {
                    throw ExceptionBuilder.RowAlreadyInOtherCollection();
                }
                if (row.rowID != -1)
                {
                    throw ExceptionBuilder.RowAlreadyInTheCollection();
                }
                if (row._oldRecord == -1 && row._newRecord == -1)
                {
                    throw ExceptionBuilder.RowEmpty();
                }

                if (proposedID == -1)
                {
                    proposedID = _nextRowID;
                }

                row.rowID = proposedID;
                if (_nextRowID <= proposedID)
                {
                    _nextRowID = checked(proposedID + 1);
                }

                DataRowChangeEventArgs? drcevent = null;

                if (row._newRecord != -1)
                {
                    row._tempRecord = row._newRecord;
                    row._newRecord = -1;

                    try
                    {
                        drcevent = RaiseRowChanging(null, row, DataRowAction.Add, true);
                    }
                    catch
                    {
                        row._tempRecord = -1;
                        throw;
                    }

                    row._newRecord = row._tempRecord;
                    row._tempRecord = -1;
                }

                if (row._oldRecord != -1)
                {
                    _recordManager[row._oldRecord] = row;
                }

                if (row._newRecord != -1)
                {
                    _recordManager[row._newRecord] = row;
                }

                Rows.ArrayAdd(row);

                if (row.RowState == DataRowState.Unchanged)
                {
                    //  how about row.oldRecord == row.newRecord both == -1
                    RecordStateChanged(row._oldRecord, DataViewRowState.None, DataViewRowState.Unchanged);
                }
                else
                {
                    RecordStateChanged(row._oldRecord, DataViewRowState.None, row.GetRecordState(row._oldRecord),
                                       row._newRecord, DataViewRowState.None, row.GetRecordState(row._newRecord));
                }

                if (_dependentColumns != null && _dependentColumns.Count > 0)
                {
                    EvaluateExpressions(row, DataRowAction.Add, null);
                }

                RaiseRowChanged(drcevent, row, DataRowAction.Add);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        private static IndexField[] NewIndexDesc(DataKey key)
        {
            Debug.Assert(key.HasValue);
            IndexField[] indexDesc = key.GetIndexDesc();
            IndexField[] newIndexDesc = new IndexField[indexDesc.Length];
            Array.Copy(indexDesc, newIndexDesc, indexDesc.Length);
            return newIndexDesc;
        }

        internal int NewRecord() => NewRecord(-1);

        internal int NewUninitializedRecord()
        {
            return _recordManager.NewRecordBase();
        }

        internal int NewRecordFromArray(object?[] value)
        {
            int colCount = _columnCollection.Count; // Perf: use the readonly columnCollection field directly
            if (colCount < value.Length)
            {
                throw ExceptionBuilder.ValueArrayLength();
            }
            int record = _recordManager.NewRecordBase();
            try
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] is object v)
                    {
                        _columnCollection[i][record] = v;
                    }
                    else
                    {
                        _columnCollection[i].Init(record);  // Increase AutoIncrementCurrent
                    }
                }
                for (int i = value.Length; i < colCount; i++)
                {
                    _columnCollection[i].Init(record);
                }
                return record;
            }
            catch (Exception e) when (ADP.IsCatchableOrSecurityExceptionType(e))
            {
                FreeRecord(ref record);
                throw;
            }
        }

        internal int NewRecord(int sourceRecord)
        {
            int record = _recordManager.NewRecordBase();

            int count = _columnCollection.Count;
            if (-1 == sourceRecord)
            {
                for (int i = 0; i < count; ++i)
                {
                    _columnCollection[i].Init(record);
                }
            }
            else
            {
                for (int i = 0; i < count; ++i)
                {
                    _columnCollection[i].Copy(sourceRecord, record);
                }
            }
            return record;
        }

        internal DataRow NewEmptyRow()
        {
            _rowBuilder._record = -1;
            DataRow dr = NewRowFromBuilder(_rowBuilder);
            _dataSet?.OnDataRowCreated(dr);
            return dr;
        }

        private DataRow NewUninitializedRow() => NewRow(NewUninitializedRecord());

        /// <summary>
        /// Creates a new <see cref='System.Data.DataRow'/>
        /// with the same schema as the table.
        /// </summary>
        public DataRow NewRow()
        {
            DataRow dr = NewRow(-1);
            NewRowCreated(dr); // this is the only API we want this event to be fired
            return dr;
        }

        // Only initialize DataRelation mapping columns (approximately hidden columns)
        internal DataRow CreateEmptyRow()
        {
            DataRow row = NewUninitializedRow();

            foreach (DataColumn c in Columns)
            {
                if (!XmlToDatasetMap.IsMappedColumn(c))
                {
                    if (!c.AutoIncrement)
                    {
                        if (c.AllowDBNull)
                        {
                            row[c] = DBNull.Value;
                        }
                        else if (c.DefaultValue != null)
                        {
                            row[c] = c.DefaultValue;
                        }
                    }
                    else
                    {
                        c.Init(row._tempRecord);
                    }
                }
            }
            return row;
        }

        private void NewRowCreated(DataRow row)
        {
            if (null != _onTableNewRowDelegate)
            {
                DataTableNewRowEventArgs eventArg = new DataTableNewRowEventArgs(row);
                OnTableNewRow(eventArg);
            }
        }

        internal DataRow NewRow(int record)
        {
            if (-1 == record)
            {
                record = NewRecord(-1);
            }

            _rowBuilder._record = record;
            DataRow row = NewRowFromBuilder(_rowBuilder);
            _recordManager[record] = row;

            _dataSet?.OnDataRowCreated(row);

            return row;
        }

        // This is what a subclassed dataSet overrides to create a new row.
        protected virtual DataRow NewRowFromBuilder(DataRowBuilder builder) => new DataRow(builder);

        /// <summary>
        /// Gets the row type.
        /// </summary>
        protected virtual Type GetRowType() => typeof(DataRow);

        // Prevent inlining so that reflection calls are not moved to caller that may be in a different assembly that may have a different grant set.
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected internal DataRow[] NewRowArray(int size)
        {
            if (IsTypedDataTable)
            {
                if (0 == size)
                {
                    if (null == _emptyDataRowArray)
                    {
                        _emptyDataRowArray = (DataRow[])Array.CreateInstance(GetRowType(), 0);
                    }
                    return _emptyDataRowArray;
                }
                return (DataRow[])Array.CreateInstance(GetRowType(), size);
            }
            else
            {
                return ((0 == size) ? Array.Empty<DataRow>() : new DataRow[size]);
            }
        }

        internal bool NeedColumnChangeEvents =>
            (IsTypedDataTable || (null != _onColumnChangingDelegate) || (null != _onColumnChangedDelegate));

        protected internal virtual void OnColumnChanging(DataColumnChangeEventArgs e)
        {
            // intentionally allow exceptions to bubble up.  We haven't committed anything yet.
            Debug.Assert(e != null, "e should not be null");
            if (_onColumnChangingDelegate != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.OnColumnChanging|INFO> {0}", ObjectID);
                _onColumnChangingDelegate(this, e);
            }
        }

        protected internal virtual void OnColumnChanged(DataColumnChangeEventArgs e)
        {
            Debug.Assert(e != null, "e should not be null");
            if (_onColumnChangedDelegate != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.OnColumnChanged|INFO> {0}", ObjectID);
                _onColumnChangedDelegate(this, e);
            }
        }

        protected virtual void OnPropertyChanging(PropertyChangedEventArgs pcevent)
        {
            if (_onPropertyChangingDelegate != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.OnPropertyChanging|INFO> {0}", ObjectID);
                _onPropertyChangingDelegate(this, pcevent);
            }
        }

        internal void OnRemoveColumnInternal(DataColumn column) => OnRemoveColumn(column);

        /// <summary>
        /// Notifies the <see cref='System.Data.DataTable'/> that a <see cref='System.Data.DataColumn'/> is
        /// being removed.
        /// </summary>
        protected virtual void OnRemoveColumn(DataColumn column) { }

        private DataRowChangeEventArgs? OnRowChanged(DataRowChangeEventArgs? args, DataRow eRow, DataRowAction eAction)
        {
            if ((null != _onRowChangedDelegate) || IsTypedDataTable)
            {
                if (null == args)
                {
                    args = new DataRowChangeEventArgs(eRow, eAction);
                }
                OnRowChanged(args);
            }
            return args;
        }

        private DataRowChangeEventArgs? OnRowChanging(DataRowChangeEventArgs? args, DataRow eRow, DataRowAction eAction)
        {
            if ((null != _onRowChangingDelegate) || IsTypedDataTable)
            {
                if (null == args)
                {
                    args = new DataRowChangeEventArgs(eRow, eAction);
                }
                OnRowChanging(args);
            }
            return args;
        }

        /// <summary>
        /// Raises the <see cref='System.Data.DataTable.RowChanged'/> event.
        /// </summary>
        protected virtual void OnRowChanged(DataRowChangeEventArgs e)
        {
            Debug.Assert((null != e) && ((null != _onRowChangedDelegate) || IsTypedDataTable), "OnRowChanged arguments");
            if (_onRowChangedDelegate != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.OnRowChanged|INFO> {0}", ObjectID);
                _onRowChangedDelegate(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref='System.Data.DataTable.RowChanging'/> event.
        /// </summary>
        protected virtual void OnRowChanging(DataRowChangeEventArgs e)
        {
            Debug.Assert((null != e) && ((null != _onRowChangingDelegate) || IsTypedDataTable), "OnRowChanging arguments");
            if (_onRowChangingDelegate != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.OnRowChanging|INFO> {0}", ObjectID);
                _onRowChangingDelegate(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref='System.Data.DataTable.OnRowDeleting'/> event.
        /// </summary>
        protected virtual void OnRowDeleting(DataRowChangeEventArgs e)
        {
            Debug.Assert((null != e) && ((null != _onRowDeletingDelegate) || IsTypedDataTable), "OnRowDeleting arguments");
            if (_onRowDeletingDelegate != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.OnRowDeleting|INFO> {0}", ObjectID);
                _onRowDeletingDelegate(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref='System.Data.DataTable.OnRowDeleted'/> event.
        /// </summary>
        protected virtual void OnRowDeleted(DataRowChangeEventArgs e)
        {
            Debug.Assert((null != e) && ((null != _onRowDeletedDelegate) || IsTypedDataTable), "OnRowDeleted arguments");
            if (_onRowDeletedDelegate != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.OnRowDeleted|INFO> {0}", ObjectID);
                _onRowDeletedDelegate(this, e);
            }
        }

        protected virtual void OnTableCleared(DataTableClearEventArgs e)
        {
            if (_onTableClearedDelegate != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.OnTableCleared|INFO> {0}", ObjectID);
                _onTableClearedDelegate(this, e);
            }
        }

        protected virtual void OnTableClearing(DataTableClearEventArgs e)
        {
            if (_onTableClearingDelegate != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.OnTableClearing|INFO> {0}", ObjectID);
                _onTableClearingDelegate(this, e);
            }
        }

        protected virtual void OnTableNewRow(DataTableNewRowEventArgs e)
        {
            if (_onTableNewRowDelegate != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.OnTableNewRow|INFO> {0}", ObjectID);
                _onTableNewRowDelegate(this, e);
            }
        }

        private void OnInitialized()
        {
            if (_onInitialized != null)
            {
                DataCommonEventSource.Log.Trace("<ds.DataTable.OnInitialized|INFO> {0}", ObjectID);
                _onInitialized(this, EventArgs.Empty);
            }
        }

        internal IndexField[] ParseSortString(string? sortString)
        {
            IndexField[] indexDesc = Array.Empty<IndexField>();
            if ((null != sortString) && (0 < sortString.Length))
            {
                string[] split = sortString.Split(',', StringSplitOptions.TrimEntries);
                indexDesc = new IndexField[split.Length];

                for (int i = 0; i < split.Length; i++)
                {
                    string current = split[i];

                    // handle ASC and DESC.
                    int length = current.Length;
                    bool descending = false;
                    if (length >= 5 && string.Compare(current, length - 4, " ASC", 0, 4, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        current = current.AsSpan(0, length - 4).Trim().ToString();
                    }
                    else if (length >= 6 && string.Compare(current, length - 5, " DESC", 0, 5, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        descending = true;
                        current = current.AsSpan(0, length - 5).Trim().ToString();
                    }

                    // handle brackets.
                    if (current.StartsWith('['))
                    {
                        if (current.EndsWith(']'))
                        {
                            current = current.Substring(1, current.Length - 2);
                        }
                        else
                        {
                            throw ExceptionBuilder.InvalidSortString(split[i]);
                        }
                    }

                    // find the column.
                    DataColumn? column = Columns[current];
                    if (column == null)
                    {
                        throw ExceptionBuilder.ColumnOutOfRange(current);
                    }
                    indexDesc[i] = new IndexField(column, descending);
                }
            }
            return indexDesc;
        }

        internal void RaisePropertyChanging(string name)
        {
            OnPropertyChanging(new PropertyChangedEventArgs(name));
        }

        // Notify all indexes that record changed.
        // Only called when Error was changed.
        internal void RecordChanged(int record)
        {
            Debug.Assert(record != -1, "Record number must be given");
            SetShadowIndexes(); // how about new assert?
            try
            {
                int numIndexes = _shadowIndexes!.Count;
                for (int i = 0; i < numIndexes; i++)
                {
                    Index ndx = _shadowIndexes[i]; // shadowindexes may change, see ShadowIndexCopy()
                    if (0 < ndx.RefCount)
                    {
                        ndx.RecordChanged(record);
                    }
                }
            }
            finally
            {
                RestoreShadowIndexes();
            }
        }

        // for each index in liveindexes invok RecordChanged
        // oldIndex and newIndex keeps  position of record before delete and after insert in each index in order
        // LiveIndexes[n-m] will have its information in oldIndex[n-m] and  newIndex[n-m]
        internal void RecordChanged(int[] oldIndex, int[] newIndex)
        {
            SetShadowIndexes();
            Debug.Assert(oldIndex.Length == newIndex.Length, "Size oldIndexes and newIndexes should be the same");
            Debug.Assert(oldIndex.Length == _shadowIndexes!.Count, "Size of OldIndexes should be the same as size of Live indexes");
            try
            {
                int numIndexes = _shadowIndexes.Count;
                for (int i = 0; i < numIndexes; i++)
                {
                    Index ndx = _shadowIndexes[i]; // shadowindexes may change, see ShadowIndexCopy()
                    if (0 < ndx.RefCount)
                    {
                        ndx.RecordChanged(oldIndex[i], newIndex[i]);
                    }
                }
            }
            finally
            {
                RestoreShadowIndexes();
            }
        }

        internal void RecordStateChanged(int record, DataViewRowState oldState, DataViewRowState newState)
        {
            SetShadowIndexes();
            try
            {
                int numIndexes = _shadowIndexes!.Count;
                for (int i = 0; i < numIndexes; i++)
                {
                    Index ndx = _shadowIndexes[i]; // shadowindexes may change, see ShadowIndexCopy()
                    if (0 < ndx.RefCount)
                    {
                        ndx.RecordStateChanged(record, oldState, newState);
                    }
                }
            }
            finally
            {
                RestoreShadowIndexes();
            }
            // System.Data.XML.Store.Store.OnROMChanged(record, oldState, newState);
        }

        internal void RecordStateChanged(int record1, DataViewRowState oldState1, DataViewRowState newState1,
                                         int record2, DataViewRowState oldState2, DataViewRowState newState2)
        {
            SetShadowIndexes();
            try
            {
                int numIndexes = _shadowIndexes!.Count;
                for (int i = 0; i < numIndexes; i++)
                {
                    Index ndx = _shadowIndexes[i]; // shadowindexes may change, see ShadowIndexCopy()
                    if (0 < ndx.RefCount)
                    {
                        if (record1 != -1 && record2 != -1)
                        {
                            ndx.RecordStateChanged(record1, oldState1, newState1, record2, oldState2, newState2);
                        }
                        else if (record1 != -1)
                        {
                            ndx.RecordStateChanged(record1, oldState1, newState1);
                        }
                        else if (record2 != -1)
                        {
                            ndx.RecordStateChanged(record2, oldState2, newState2);
                        }
                    }
                }
            }
            finally
            {
                RestoreShadowIndexes();
            }
            // System.Data.XML.Store.Store.OnROMChanged(record1, oldState1, newState1, record2, oldState2, newState2);
        }


        // RemoveRecordFromIndexes removes the given record (using row and version) from all indexes and it  stores and returns the position of deleted
        // record from each index
        // IT SHOULD NOT CAUSE ANY EVENT TO BE FIRED
        internal int[] RemoveRecordFromIndexes(DataRow row, DataRowVersion version)
        {
            int indexCount = LiveIndexes.Count;
            int[] positionIndexes = new int[indexCount];

            int recordNo = row.GetRecordFromVersion(version);
            DataViewRowState states = row.GetRecordState(recordNo);

            while (--indexCount >= 0)
            {
                if (row.HasVersion(version) && ((states & _indexes[indexCount].RecordStates) != DataViewRowState.None))
                {
                    int index = _indexes[indexCount].GetIndex(recordNo);
                    if (index > -1)
                    {
                        positionIndexes[indexCount] = index;
                        _indexes[indexCount].DeleteRecordFromIndex(index); // this will delete the record from index and MUSt not fire event
                    }
                    else
                    {
                        positionIndexes[indexCount] = -1; // this means record was not in index
                    }
                }
                else
                {
                    positionIndexes[indexCount] = -1; // this means record was not in index
                }
            }
            return positionIndexes;
        }

        // InsertRecordToIndexes inserts the given record (using row and version) to all indexes and it  stores and returns the position of inserted
        // record to each index
        // IT SHOULD NOT CAUSE ANY EVENT TO BE FIRED
        internal int[] InsertRecordToIndexes(DataRow row, DataRowVersion version)
        {
            int indexCount = LiveIndexes.Count;
            int[] positionIndexes = new int[indexCount];

            int recordNo = row.GetRecordFromVersion(version);
            DataViewRowState states = row.GetRecordState(recordNo);

            while (--indexCount >= 0)
            {
                if (row.HasVersion(version))
                {
                    if ((states & _indexes[indexCount].RecordStates) != DataViewRowState.None)
                    {
                        positionIndexes[indexCount] = _indexes[indexCount].InsertRecordToIndex(recordNo);
                    }
                    else
                    {
                        positionIndexes[indexCount] = -1;
                    }
                }
            }
            return positionIndexes;
        }

        internal static void SilentlySetValue(DataRow dr, DataColumn dc, DataRowVersion version, object newValue)
        {
            // get record for version
            int record = dr.GetRecordFromVersion(version);

            bool equalValues;
            if (DataStorage.IsTypeCustomType(dc.DataType) && newValue != dc[record])
            {
                // if UDT storage, need to check if reference changed.
                equalValues = false;
            }
            else
            {
                equalValues = dc.CompareValueTo(record, newValue, true);
            }

            // if expression has changed
            if (!equalValues)
            {
                int[] oldIndex = dr.Table.RemoveRecordFromIndexes(dr, version); // conditional, if it exists it will try to remove with no event fired
                dc.SetValue(record, newValue);
                int[] newIndex = dr.Table.InsertRecordToIndexes(dr, version); // conditional, it will insert if it qualifies, no event will be fired
                if (dr.HasVersion(version))
                {
                    if (version != DataRowVersion.Original)
                    {
                        dr.Table.RecordChanged(oldIndex, newIndex);
                    }
                    if (dc._dependentColumns != null)
                    {
                        dc.Table!.EvaluateDependentExpressions(dc._dependentColumns, dr, version, null);
                    }
                }
            }
            dr.ResetLastChangedColumn();
        }

        /// <summary>
        /// Rolls back all changes that have been made to the table
        /// since it was loaded, or the last time <see cref='System.Data.DataTable.AcceptChanges'/> was called.
        /// </summary>
        public void RejectChanges()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.RejectChanges|API> {0}", ObjectID);
            try
            {
                DataRow[] oldRows = new DataRow[Rows.Count];
                Rows.CopyTo(oldRows, 0);

                for (int i = 0; i < oldRows.Length; i++)
                {
                    RollbackRow(oldRows[i]);
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal void RemoveRow(DataRow row, bool check)
        {
            if (row.rowID == -1)
            {
                throw ExceptionBuilder.RowAlreadyRemoved();
            }

            if (check && _dataSet != null)
            {
                for (ParentForeignKeyConstraintEnumerator constraints = new ParentForeignKeyConstraintEnumerator(_dataSet, this); constraints.GetNext();)
                {
                    constraints.GetForeignKeyConstraint().CheckCanRemoveParentRow(row);
                }
            }

            int oldRecord = row._oldRecord;
            int newRecord = row._newRecord;

            DataViewRowState oldRecordStatePre = row.GetRecordState(oldRecord);
            DataViewRowState newRecordStatePre = row.GetRecordState(newRecord);

            row._oldRecord = -1;
            row._newRecord = -1;

            if (oldRecord == newRecord)
            {
                oldRecord = -1;
            }

            RecordStateChanged(oldRecord, oldRecordStatePre, DataViewRowState.None, newRecord, newRecordStatePre, DataViewRowState.None);

            FreeRecord(ref oldRecord);
            FreeRecord(ref newRecord);

            row.rowID = -1;
            Rows.ArrayRemove(row);
        }

        // Resets the table back to its original state.
        public virtual void Reset()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.Reset|API> {0}", ObjectID);
            try
            {
                Clear();
                ResetConstraints();

                DataRelationCollection dr = ParentRelations;
                int count = dr.Count;
                while (count > 0)
                {
                    count--;
                    dr.RemoveAt(count);
                }

                dr = ChildRelations;
                count = dr.Count;
                while (count > 0)
                {
                    count--;
                    dr.RemoveAt(count);
                }

                Columns.Clear();
                _indexes.Clear();
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal void ResetIndexes() => ResetInternalIndexes(null);

        internal void ResetInternalIndexes(DataColumn? column)
        {
            Debug.Assert(null != _indexes, "unexpected null indexes");
            SetShadowIndexes();
            try
            {
                // the length of shadowIndexes will not change
                // but the array instance may change during
                // events during Index.Reset
                int numIndexes = _shadowIndexes!.Count;
                for (int i = 0; i < numIndexes; i++)
                {
                    Index ndx = _shadowIndexes[i]; // shadowindexes may change, see ShadowIndexCopy()
                    if (0 < ndx.RefCount)
                    {
                        if (null == column)
                        {
                            ndx.Reset();
                        }
                        else
                        {
                            bool found = false;
                            foreach (IndexField field in ndx._indexFields)
                            {
                                if (ReferenceEquals(column, field.Column))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                            {
                                ndx.Reset();
                            }
                        }
                    }
                }
            }
            finally
            {
                RestoreShadowIndexes();
            }
        }

        internal void RollbackRow(DataRow row)
        {
            row.CancelEdit();
            SetNewRecord(row, row._oldRecord, DataRowAction.Rollback, false, true);
        }

        private DataRowChangeEventArgs? RaiseRowChanged(DataRowChangeEventArgs? args, DataRow eRow, DataRowAction eAction)
        {
            try
            {
                if (UpdatingCurrent(eRow, eAction) && (IsTypedDataTable || (null != _onRowChangedDelegate)))
                {
                    args = OnRowChanged(args, eRow, eAction);
                }
                // check if we deleting good row
                else if (DataRowAction.Delete == eAction && eRow._newRecord == -1 && (IsTypedDataTable || (null != _onRowDeletedDelegate)))
                {
                    if (null == args)
                    {
                        args = new DataRowChangeEventArgs(eRow, eAction);
                    }
                    OnRowDeleted(args);
                }
            }
            catch (Exception f) when (ADP.IsCatchableExceptionType(f))
            {
                ExceptionBuilder.TraceExceptionWithoutRethrow(f); // ignore the exception
            }
            return args;
        }

        private DataRowChangeEventArgs? RaiseRowChanging(DataRowChangeEventArgs? args, DataRow eRow, DataRowAction eAction)
        {
            if (UpdatingCurrent(eRow, eAction) && (IsTypedDataTable || (null != _onRowChangingDelegate)))
            {
                eRow._inChangingEvent = true;

                // don't catch
                try
                {
                    args = OnRowChanging(args, eRow, eAction);
                }
                finally
                {
                    eRow._inChangingEvent = false;
                }
            }
            // check if we deleting good row
            else if (DataRowAction.Delete == eAction && eRow._newRecord != -1 && (IsTypedDataTable || (null != _onRowDeletingDelegate)))
            {
                eRow._inDeletingEvent = true;
                // don't catch
                try
                {
                    if (null == args)
                    {
                        args = new DataRowChangeEventArgs(eRow, eAction);
                    }
                    OnRowDeleting(args);
                }
                finally
                {
                    eRow._inDeletingEvent = false;
                }
            }
            return args;
        }

        private DataRowChangeEventArgs? RaiseRowChanging(DataRowChangeEventArgs? args, DataRow eRow, DataRowAction eAction, bool fireEvent)
        {
            // check all constraints
            if (EnforceConstraints)
            {
                int columnCount = _columnCollection.Count;
                for (int i = 0; i < columnCount; ++i)
                {
                    DataColumn column = _columnCollection[i];
                    if (!column.Computed || eAction != DataRowAction.Add)
                    {
                        column.CheckColumnConstraint(eRow, eAction);
                    }
                }

                int constraintCount = _constraintCollection.Count;
                for (int i = 0; i < constraintCount; ++i)
                {
                    _constraintCollection[i].CheckConstraint(eRow, eAction);
                }
            }

            if (fireEvent)
            {
                args = RaiseRowChanging(args, eRow, eAction);
            }

            if (!_inDataLoad)
            {
                // cascade things...
                if (!MergingData && eAction != DataRowAction.Nothing && eAction != DataRowAction.ChangeOriginal)
                {
                    CascadeAll(eRow, eAction);
                }
            }
            return args;
        }

        /// <summary>
        /// Returns an array of all <see cref='System.Data.DataRow'/> objects.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Filter expression is empty therefore this is safe.")]
        public DataRow[] Select()
        {
            DataCommonEventSource.Log.Trace("<ds.DataTable.Select|API> {0}", ObjectID);
            return new Select(this, "", "", DataViewRowState.CurrentRows).SelectRows();
        }

        /// <summary>
        /// Returns an array of all <see cref='System.Data.DataRow'/> objects that match the filter criteria in order of
        /// primary key (or lacking one, order of addition.)
        /// </summary>
        [RequiresUnreferencedCode(Data.Select.RequiresUnreferencedCodeMessage)]
        public DataRow[] Select(string? filterExpression)
        {
            DataCommonEventSource.Log.Trace("<ds.DataTable.Select|API> {0}, filterExpression='{1}'", ObjectID, filterExpression);
            return new Select(this, filterExpression, "", DataViewRowState.CurrentRows).SelectRows();
        }

        /// <summary>
        /// Returns an array of all <see cref='System.Data.DataRow'/> objects that match the filter criteria, in the
        /// specified sort order.
        /// </summary>
        [RequiresUnreferencedCode(Data.Select.RequiresUnreferencedCodeMessage)]
        public DataRow[] Select(string? filterExpression, string? sort)
        {
            DataCommonEventSource.Log.Trace("<ds.DataTable.Select|API> {0}, filterExpression='{1}', sort='{2}'", ObjectID, filterExpression, sort);
            return new Select(this, filterExpression, sort, DataViewRowState.CurrentRows).SelectRows();
        }

        /// <summary>
        /// Returns an array of all <see cref='System.Data.DataRow'/> objects that match the filter in the order of the
        /// sort, that match the specified state.
        /// </summary>
        [RequiresUnreferencedCode(Data.Select.RequiresUnreferencedCodeMessage)]
        public DataRow[] Select(string? filterExpression, string? sort, DataViewRowState recordStates)
        {
            DataCommonEventSource.Log.Trace("<ds.DataTable.Select|API> {0}, filterExpression='{1}', sort='{2}', recordStates={3}", ObjectID, filterExpression, sort, recordStates);
            return new Select(this, filterExpression, sort, recordStates).SelectRows();
        }

        internal void SetNewRecord(DataRow row, int proposedRecord, DataRowAction action = DataRowAction.Change, bool isInMerge = false, bool fireEvent = true, bool suppressEnsurePropertyChanged = false)
        {
            Exception? deferredException;
            SetNewRecordWorker(row, proposedRecord, action, isInMerge, suppressEnsurePropertyChanged, -1, fireEvent, out deferredException); // we are going to call below overload from insert
            if (deferredException != null)
            {
                throw deferredException;
            }
        }

        private void SetNewRecordWorker(DataRow row, int proposedRecord, DataRowAction action, bool isInMerge, bool suppressEnsurePropertyChanged,
            int position, bool fireEvent, out Exception? deferredException)
        {
            // this is the event workhorse... it will throw the changing/changed events
            // and update the indexes. Used by change, add, delete, revert.

            // order of execution is as follows
            //
            // 1) set temp record
            // 2) Check constraints for non-expression columns
            // 3) Raise RowChanging/RowDeleting with temp record
            // 4) set the new record in storage
            // 5) Update indexes with recordStateChanges - this will fire ListChanged & PropertyChanged events on associated views
            // 6) Evaluate all Expressions (exceptions are deferred)- this will fire ListChanged & PropertyChanged events on associated views
            // 7) Raise RowChanged/ RowDeleted
            // 8) Check constraints for expression columns

            Debug.Assert(row != null, "Row can't be null.");
            deferredException = null;

            if (row._tempRecord != proposedRecord)
            {
                // $HACK: for performance reasons, EndUpdate calls SetNewRecord with tempRecord == proposedRecord
                if (!_inDataLoad)
                {
                    row.CheckInTable();
                    CheckNotModifying(row);
                }
                if (proposedRecord == row._newRecord)
                {
                    if (isInMerge)
                    {
                        Debug.Assert(fireEvent, "SetNewRecord is called with wrong parameter");
                        RaiseRowChanged(null, row, action);
                    }
                    return;
                }

                Debug.Assert(!row._inChangingEvent, "How can this row be in an infinite loop?");

                row._tempRecord = proposedRecord;
            }
            DataRowChangeEventArgs? drcevent = null;

            try
            {
                row._action = action;
                drcevent = RaiseRowChanging(null, row, action, fireEvent);
            }
            catch
            {
                row._tempRecord = -1;
                throw;
            }
            finally
            {
                row._action = DataRowAction.Nothing;
            }

            row._tempRecord = -1;

            int currentRecord = row._newRecord;

            // if we're deleting, then the oldRecord value will change, so need to track that if it's distinct from the newRecord.
            int secondRecord = (proposedRecord != -1 ?
                                proposedRecord :
                                (row.RowState != DataRowState.Unchanged ?
                                 row._oldRecord :
                                 -1));

            if (action == DataRowAction.Add)
            {
                //if we come here from insert we do insert the row to collection
                if (position == -1)
                {
                    Rows.ArrayAdd(row);
                }
                else
                {
                    Rows.ArrayInsert(row, position);
                }
            }

            List<DataRow>? cachedRows = null;
            if ((action == DataRowAction.Delete || action == DataRowAction.Change) &&
                _dependentColumns != null && _dependentColumns.Count > 0)
            {
                // if there are expression columns, need to cache related rows for deletes and updates (key changes)
                // before indexes are modified.
                cachedRows = new List<DataRow>();
                for (int j = 0; j < ParentRelations.Count; j++)
                {
                    DataRelation relation = ParentRelations[j];
                    if (relation.ChildTable != row.Table)
                    {
                        continue;
                    }
                    cachedRows.InsertRange(cachedRows.Count, row.GetParentRows(relation));
                }

                for (int j = 0; j < ChildRelations.Count; j++)
                {
                    DataRelation relation = ChildRelations[j];
                    if (relation.ParentTable != row.Table)
                    {
                        continue;
                    }
                    cachedRows.InsertRange(cachedRows.Count, row.GetChildRows(relation));
                }
            }

            // if the newRecord is changing, the propertychanged event should be allowed to triggered for ListChangedType.Changed or .Moved
            // unless the specific condition is known that no data has changed, like DataRow.SetModified()
            if (!suppressEnsurePropertyChanged && !row.HasPropertyChanged && (row._newRecord != proposedRecord)
                && (-1 != proposedRecord)
                && (-1 != row._newRecord))
            {
                // DataRow will believe multiple edits occurred and
                // DataView.ListChanged event w/ ListChangedType.ItemChanged will raise DataRowView.PropertyChanged event and
                // PropertyChangedEventArgs.PropertyName will now be empty string so
                // WPF will refresh the entire row
                row.LastChangedColumn = null;
                row.LastChangedColumn = null;
            }

            // Check whether we need to update indexes
            if (LiveIndexes.Count != 0)
            {
                if ((-1 == currentRecord) && (-1 != proposedRecord) && (-1 != row._oldRecord) && (proposedRecord != row._oldRecord))
                {
                    // the transition from DataRowState.Deleted -> DataRowState.Modified
                    // with same orginal record but new current record
                    // needs to raise an ItemChanged or ItemMoved instead of ItemAdded in the ListChanged event.
                    // for indexes/views listening for both DataViewRowState.Deleted | DataViewRowState.ModifiedCurrent
                    currentRecord = row._oldRecord;
                }

                DataViewRowState currentRecordStatePre = row.GetRecordState(currentRecord);
                DataViewRowState secondRecordStatePre = row.GetRecordState(secondRecord);

                row._newRecord = proposedRecord;
                if (proposedRecord != -1)
                    _recordManager[proposedRecord] = row;

                DataViewRowState currentRecordStatePost = row.GetRecordState(currentRecord);
                DataViewRowState secondRecordStatePost = row.GetRecordState(secondRecord);

                // may raise DataView.ListChanged event
                RecordStateChanged(currentRecord, currentRecordStatePre, currentRecordStatePost,
                    secondRecord, secondRecordStatePre, secondRecordStatePost);
            }
            else
            {
                row._newRecord = proposedRecord;
                if (proposedRecord != -1)
                    _recordManager[proposedRecord] = row;
            }

            // reset the last changed column here, after all
            // DataViews have raised their DataRowView.PropertyChanged event
            row.ResetLastChangedColumn();

            // free the 'currentRecord' only after all the indexes have been updated.
            // Corruption! { if (currentRecord != row.oldRecord) { FreeRecord(ref currentRecord); } }
            // RecordStateChanged raises ListChanged event at which time user may do work
            if (-1 != currentRecord)
            {
                if (currentRecord != row._oldRecord)
                {
                    if ((currentRecord != row._tempRecord) &&   // Delete, AcceptChanges, BeginEdit
                        (currentRecord != row._newRecord) &&    // RejectChanges & SetAdded
                        (row == _recordManager[currentRecord])) // AcceptChanges, NewRow
                    {
                        FreeRecord(ref currentRecord);
                    }
                }
            }

            if (row.RowState == DataRowState.Detached && row.rowID != -1)
            {
                RemoveRow(row, false);
            }

            if (_dependentColumns != null && _dependentColumns.Count > 0)
            {
                try
                {
                    EvaluateExpressions(row, action, cachedRows);
                }
                catch (Exception exc)
                {
                    // For DataRows being added, throwing of exception from expression evaluation is
                    // deferred until after the row has been completely added.
                    if (action != DataRowAction.Add)
                    {
                        throw;
                    }
                    else
                    {
                        deferredException = exc;
                    }
                }
            }

            try
            {
                if (fireEvent)
                {
                    RaiseRowChanged(drcevent, row, action);
                }
            }
            catch (Exception e) when (ADP.IsCatchableExceptionType(e))
            {
                ExceptionBuilder.TraceExceptionWithoutRethrow(e); // ignore the exception
            }
        }

        // this is the event workhorse... it will throw the changing/changed events
        // and update the indexes.
        internal void SetOldRecord(DataRow row, int proposedRecord)
        {
            if (!_inDataLoad)
            {
                row.CheckInTable();
                CheckNotModifying(row);
            }

            if (proposedRecord == row._oldRecord)
            {
                return;
            }

            int originalRecord = row._oldRecord; // cache old record after potential RowChanging event
            try
            {
                // Check whether we need to update indexes
                if (LiveIndexes.Count != 0)
                {
                    if ((-1 == originalRecord) && (-1 != proposedRecord) && (-1 != row._newRecord) && (proposedRecord != row._newRecord))
                    {
                        // the transition from DataRowState.Added -> DataRowState.Modified
                        // with same current record but new original record
                        // needs to raise an ItemChanged or ItemMoved instead of ItemAdded in the ListChanged event.
                        // for indexes/views listening for both DataViewRowState.Added | DataViewRowState.ModifiedOriginal
                        originalRecord = row._newRecord;
                    }

                    DataViewRowState originalRecordStatePre = row.GetRecordState(originalRecord);
                    DataViewRowState proposedRecordStatePre = row.GetRecordState(proposedRecord);

                    row._oldRecord = proposedRecord;
                    if (proposedRecord != -1)
                    {
                        _recordManager[proposedRecord] = row;
                    }

                    DataViewRowState originalRecordStatePost = row.GetRecordState(originalRecord);
                    DataViewRowState proposedRecordStatePost = row.GetRecordState(proposedRecord);

                    RecordStateChanged(originalRecord, originalRecordStatePre, originalRecordStatePost,
                                       proposedRecord, proposedRecordStatePre, proposedRecordStatePost);
                }
                else
                {
                    row._oldRecord = proposedRecord;
                    if (proposedRecord != -1)
                    {
                        _recordManager[proposedRecord] = row;
                    }
                }
            }
            finally
            {
                if ((originalRecord != -1) && (originalRecord != row._tempRecord) &&
                    (originalRecord != row._oldRecord) && (originalRecord != row._newRecord))
                {
                    FreeRecord(ref originalRecord);
                }
                // else during an event 'row.AcceptChanges(); row.BeginEdit(); row.EndEdit();'

                if (row.RowState == DataRowState.Detached && row.rowID != -1)
                {
                    RemoveRow(row, false);
                }
            }
        }

        private void RestoreShadowIndexes()
        {
            Debug.Assert(1 <= _shadowCount, "unexpected negative shadow count");
            _shadowCount--;
            if (0 == _shadowCount)
            {
                _shadowIndexes = null;
            }
        }

        private void SetShadowIndexes()
        {
            if (null == _shadowIndexes)
            {
                Debug.Assert(0 == _shadowCount, "unexpected count");
                _shadowIndexes = LiveIndexes;
                _shadowCount = 1;
            }
            else
            {
                Debug.Assert(1 <= _shadowCount, "unexpected negative shadow count");
                _shadowCount++;
            }
        }

        internal void ShadowIndexCopy()
        {
            if (_shadowIndexes == _indexes)
            {
                Debug.Assert(0 < _indexes.Count, "unexpected");
                _shadowIndexes = new List<Index>(_indexes);
            }
        }

        /// <summary>
        /// Returns the <see cref='System.Data.DataTable.TableName'/> and <see cref='System.Data.DataTable.DisplayExpression'/>, if there is one as a concatenated string.
        /// </summary>
        public override string ToString() => _displayExpression == null ?
            TableName :
            TableName + " + " + DisplayExpressionInternal;

        public void BeginLoadData()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.BeginLoadData|API> {0}", ObjectID);
            try
            {
                if (_inDataLoad)
                {
                    return;
                }

                _inDataLoad = true;
                Debug.Assert(null == _loadIndex, "loadIndex should already be null");
                _loadIndex = null;

                // LoadDataRow may have been called before BeginLoadData and already
                // initialized loadIndexwithOriginalAdded & loadIndexwithCurrentDeleted

                _initialLoad = (Rows.Count == 0);
                if (_initialLoad)
                {
                    SuspendIndexEvents();
                }
                else
                {
                    if (_primaryKey != null)
                    {
                        _loadIndex = _primaryKey.Key.GetSortIndex(DataViewRowState.OriginalRows);
                    }
                    if (_loadIndex != null)
                    {
                        _loadIndex.AddRef();
                    }
                }

                if (DataSet != null)
                {
                    _savedEnforceConstraints = DataSet.EnforceConstraints;
                    DataSet.EnforceConstraints = false;
                }
                else
                {
                    EnforceConstraints = false;
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public void EndLoadData()
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.EndLoadData|API> {0}", ObjectID);
            try
            {
                if (!_inDataLoad)
                {
                    return;
                }

                if (_loadIndex != null)
                {
                    _loadIndex.RemoveRef();
                }
                if (_loadIndexwithOriginalAdded != null)
                {
                    _loadIndexwithOriginalAdded.RemoveRef();
                }
                if (_loadIndexwithCurrentDeleted != null)
                {
                    _loadIndexwithCurrentDeleted.RemoveRef();
                }

                _loadIndex = null;
                _loadIndexwithOriginalAdded = null;
                _loadIndexwithCurrentDeleted = null;

                _inDataLoad = false;

                RestoreIndexEvents(false);

                if (DataSet != null)
                {
                    DataSet.EnforceConstraints = _savedEnforceConstraints;
                }
                else
                {
                    EnforceConstraints = true;
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Finds and updates a specific row. If no matching
        /// row is found, a new row is created using the given values.
        /// </summary>
        public DataRow LoadDataRow(object?[] values, bool fAcceptChanges)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.LoadDataRow|API> {0}, fAcceptChanges={1}", ObjectID, fAcceptChanges);
            try
            {
                DataRow row;
                if (_inDataLoad)
                {
                    int record = NewRecordFromArray(values);
                    if (_loadIndex != null)
                    {
                        // not expecting LiveIndexes to clear the index we use between calls to LoadDataRow
                        Debug.Assert(2 <= _loadIndex.RefCount, "bad loadIndex.RefCount");

                        int result = _loadIndex.FindRecord(record);
                        if (result != -1)
                        {
                            int resultRecord = _loadIndex.GetRecord(result);
                            row = _recordManager[resultRecord]!;
                            Debug.Assert(row != null, "Row can't be null for index record");
                            row.CancelEdit();
                            if (row.RowState == DataRowState.Deleted)
                            {
                                SetNewRecord(row, row._oldRecord, DataRowAction.Rollback, false, true);
                            }
                            SetNewRecord(row, record, DataRowAction.Change, false, true);
                            if (fAcceptChanges)
                            {
                                row.AcceptChanges();
                            }
                            return row;
                        }
                    }
                    row = NewRow(record);
                    AddRow(row);
                    if (fAcceptChanges)
                    {
                        row.AcceptChanges();
                    }
                    return row;
                }
                else
                {
                    // In case, BeginDataLoad is not called yet
                    row = UpdatingAdd(values);
                    if (fAcceptChanges)
                    {
                        row.AcceptChanges();
                    }
                    return row;
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        /// <summary>
        /// Finds and updates a specific row. If no matching row is found, a new row is created using the given values.
        /// </summary>
        public DataRow LoadDataRow(object?[] values, LoadOption loadOption)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.LoadDataRow|API> {0}, loadOption={1}", ObjectID, loadOption);
            try
            {
                Index? indextoUse = null;
                if (_primaryKey != null)
                {
                    if (loadOption == LoadOption.Upsert)
                    {
                        // CurrentVersion, and Deleted
                        if (_loadIndexwithCurrentDeleted == null)
                        {
                            _loadIndexwithCurrentDeleted = _primaryKey.Key.GetSortIndex(DataViewRowState.CurrentRows | DataViewRowState.Deleted);
                            Debug.Assert(_loadIndexwithCurrentDeleted != null, "loadIndexwithCurrentDeleted should not be null");
                            if (_loadIndexwithCurrentDeleted != null)
                            {
                                _loadIndexwithCurrentDeleted.AddRef();
                            }
                        }
                        indextoUse = _loadIndexwithCurrentDeleted;
                    }
                    else
                    {
                        // CurrentVersion, and Deleted : OverwriteRow, PreserveCurrentValues
                        if (_loadIndexwithOriginalAdded == null)
                        {
                            _loadIndexwithOriginalAdded = _primaryKey.Key.GetSortIndex(DataViewRowState.OriginalRows | DataViewRowState.Added);
                            Debug.Assert(_loadIndexwithOriginalAdded != null, "loadIndexwithOriginalAdded should not be null");
                            if (_loadIndexwithOriginalAdded != null)
                            {
                                _loadIndexwithOriginalAdded.AddRef();
                            }
                        }
                        indextoUse = _loadIndexwithOriginalAdded;
                    }
                    // not expecting LiveIndexes to clear the index we use between calls to LoadDataRow
                    Debug.Assert(2 <= indextoUse!.RefCount, "bad indextoUse.RefCount");
                }
                if (_inDataLoad && !AreIndexEventsSuspended)
                {
                    // we do not want to fire any listchanged in new Load/Fill
                    SuspendIndexEvents(); // so suspend events here(not suspended == table already has some rows initially)
                }

                DataRow dataRow = LoadRow(values, loadOption, indextoUse); // if indextoUse == null, it means we dont have PK,
                                                                           // so LoadRow will take care of just adding the row to end

                return dataRow;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal DataRow UpdatingAdd(object?[] values)
        {
            Index? index = null;
            if (_primaryKey != null)
            {
                index = _primaryKey.Key.GetSortIndex(DataViewRowState.OriginalRows);
            }

            if (index != null)
            {
                int record = NewRecordFromArray(values);
                int result = index.FindRecord(record);
                if (result != -1)
                {
                    int resultRecord = index.GetRecord(result);
                    DataRow row = _recordManager[resultRecord]!;
                    Debug.Assert(row != null, "Row can't be null for index record");
                    row.RejectChanges();
                    SetNewRecord(row, record);
                    return row;
                }
                DataRow row2 = NewRow(record);
                Rows.Add(row2);
                return row2;
            }

            return Rows.Add(values);
        }

        internal static bool UpdatingCurrent(DataRow row, DataRowAction action)
        {
            return (action == DataRowAction.Add || action == DataRowAction.Change ||
                   action == DataRowAction.Rollback || action == DataRowAction.ChangeOriginal ||
                   action == DataRowAction.ChangeCurrentAndOriginal);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "DataColumn with null expression and int data type is safe.")]
        internal DataColumn AddUniqueKey(int position)
        {
            if (_colUnique != null)
                return _colUnique;

            // check to see if we can use already existent PrimaryKey
            DataColumn[] pkey = PrimaryKey;
            if (pkey.Length == 1)
            {
                // We have one-column primary key, so we can use it in our heirarchical relation
                return pkey[0];
            }

            // add Unique, but not primaryKey to the table

            string keyName = XMLSchema.GenUniqueColumnName(TableName + "_Id", this);
            DataColumn key = new DataColumn(keyName, typeof(int), null, MappingType.Hidden);
            key.Prefix = _tablePrefix;
            key.AutoIncrement = true;
            key.AllowDBNull = false;
            key.Unique = true;

            if (position == -1)
            {
                Columns.Add(key);
            }
            else
            { // we do have a problem and Imy idea is it is bug. Ask Enzo while Code review. Why we do not set ordinal when we call AddAt?
                for (int i = Columns.Count - 1; i >= position; i--)
                {
                    Columns[i].SetOrdinalInternal(i + 1);
                }
                Columns.AddAt(position, key);
                key.SetOrdinalInternal(position);
            }

            if (pkey.Length == 0)
            {
                PrimaryKey = new DataColumn[] { key };
            }

            _colUnique = key;
            return _colUnique;
        }

        internal DataColumn AddUniqueKey() => AddUniqueKey(-1);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Expression is null and potential problem with data type has already been reported when constructing parentKey")]
        internal DataColumn AddForeignKey(DataColumn parentKey)
        {
            Debug.Assert(parentKey != null, "AddForeignKey: Invalid paramter.. related primary key is null");

            string keyName = XMLSchema.GenUniqueColumnName(parentKey.ColumnName, this);
            DataColumn foreignKey = new DataColumn(keyName, parentKey.DataType, null, MappingType.Hidden);
            Columns.Add(foreignKey);

            return foreignKey;
        }

        internal void UpdatePropertyDescriptorCollectionCache()
        {
            _propertyDescriptorCollectionCache = null;
        }

        /// <summary>
        /// Retrieves an array of properties that the given component instance
        /// provides.  This may differ from the set of properties the class
        /// provides.  If the component is sited, the site may add or remove
        /// additional properties.  The returned array of properties will be
        /// filtered by the given set of attributes.
        /// </summary>
        internal PropertyDescriptorCollection GetPropertyDescriptorCollection(Attribute[]? attributes)
        {
            if (_propertyDescriptorCollectionCache == null)
            {
                int columnsCount = Columns.Count;
                int relationsCount = ChildRelations.Count;
                PropertyDescriptor[] props = new PropertyDescriptor[columnsCount + relationsCount];
                {
                    for (int i = 0; i < columnsCount; i++)
                    {
                        props[i] = new DataColumnPropertyDescriptor(Columns[i]);
                    }
                    for (int i = 0; i < relationsCount; i++)
                    {
                        props[columnsCount + i] = new DataRelationPropertyDescriptor(ChildRelations[i]);
                    }
                }
                _propertyDescriptorCollectionCache = new PropertyDescriptorCollection(props);
            }
            return _propertyDescriptorCollectionCache;
        }

        internal XmlQualifiedName TypeName
        {
            get { return ((_typeName == null) ? XmlQualifiedName.Empty : (XmlQualifiedName)_typeName); }
            set { _typeName = value; }
        }

        public void Merge(DataTable table) =>
            Merge(table, false, MissingSchemaAction.Add);

        public void Merge(DataTable table, bool preserveChanges) =>
            Merge(table, preserveChanges, MissingSchemaAction.Add);

        public void Merge(DataTable table, bool preserveChanges, MissingSchemaAction missingSchemaAction)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.Merge|API> {0}, table={1}, preserveChanges={2}, missingSchemaAction={3}", ObjectID, (table != null) ? table.ObjectID : 0, preserveChanges, missingSchemaAction);
            try
            {
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

        [RequiresUnreferencedCode("Members from types used in the expression column to be trimmed if not referenced directly.")]
        public void Load(IDataReader reader) => Load(reader, LoadOption.PreserveChanges, null);

        [RequiresUnreferencedCode("Using LoadOption may cause members from types used in the expression column to be trimmed if not referenced directly.")]
        public void Load(IDataReader reader, LoadOption loadOption) => Load(reader, loadOption, null);

        [RequiresUnreferencedCode("Using LoadOption may cause members from types used in the expression column to be trimmed if not referenced directly.")]
        public virtual void Load(IDataReader reader, LoadOption loadOption, FillErrorEventHandler? errorHandler)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.Load|API> {0}, loadOption={1}", ObjectID, loadOption);
            try
            {
                if (PrimaryKey.Length == 0)
                {
                    DataTableReader? dtReader = reader as DataTableReader;
                    if (dtReader != null && dtReader.CurrentDataTable == this)
                    {
                        return; // if not return, it will go to infinite loop
                    }
                }
                Common.LoadAdapter adapter = new Common.LoadAdapter();
                adapter.FillLoadOption = loadOption;
                adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                if (null != errorHandler)
                {
                    adapter.FillError += errorHandler;
                }
                adapter.FillFromReader(new DataTable[] { this }, reader, 0, 0);

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

        private DataRow LoadRow(object?[] values, LoadOption loadOption, Index? searchIndex)
        {
            int recordNo;
            DataRow? dataRow = null;

            if (searchIndex != null)
            {
                int[] primaryKeyIndex = Array.Empty<int>();
                if (_primaryKey != null)
                {
                    // we do check above for PK, but in case if someone else gives us some index unrelated to PK
                    primaryKeyIndex = new int[_primaryKey.ColumnsReference.Length];
                    for (int i = 0; i < _primaryKey.ColumnsReference.Length; i++)
                    {
                        primaryKeyIndex[i] = _primaryKey.ColumnsReference[i].Ordinal;
                    }
                }

                object[] keys = new object[primaryKeyIndex.Length];
                for (int i = 0; i < primaryKeyIndex.Length; i++)
                {
                    // TODO: There may be null values in the key
                    keys[i] = values[primaryKeyIndex[i]]!;
                }

                Range result = searchIndex.FindRecords(keys);

                if (!result.IsNull)
                {
                    Debug.Assert(result.Max >= result.Min);
                    int deletedRowUpsertCount = 0;
                    for (int i = result.Min; i <= result.Max; i++)
                    {
                        int resultRecord = searchIndex.GetRecord(i);
                        dataRow = _recordManager[resultRecord]!;
                        recordNo = NewRecordFromArray(values);

                        // values array is being reused by DataAdapter, do not modify the values array
                        for (int count = 0; count < values.Length; count++)
                        {
                            if (null == values[count])
                            {
                                _columnCollection[count].Copy(resultRecord, recordNo);
                            }
                        }
                        for (int count = values.Length; count < _columnCollection.Count; count++)
                        {
                            _columnCollection[count].Copy(resultRecord, recordNo); // if there are missing values
                        }

                        if (loadOption != LoadOption.Upsert || dataRow.RowState != DataRowState.Deleted)
                        {
                            SetDataRowWithLoadOption(dataRow, recordNo, loadOption, true);
                        }
                        else
                        {
                            deletedRowUpsertCount++;
                        }
                    }
                    if (0 == deletedRowUpsertCount)
                    {
                        return dataRow!;
                    }
                }
            }

            recordNo = NewRecordFromArray(values);
            dataRow = NewRow(recordNo);

            // fire rowChanging event here
            DataRowAction action;
            DataRowChangeEventArgs? drcevent = null;
            switch (loadOption)
            {
                case LoadOption.OverwriteChanges:
                case LoadOption.PreserveChanges:
                    action = DataRowAction.ChangeCurrentAndOriginal;
                    break;
                case LoadOption.Upsert:
                    action = DataRowAction.Add;
                    break;
                default:
                    throw ExceptionBuilder.ArgumentOutOfRange(nameof(LoadOption));
            }

            drcevent = RaiseRowChanging(null, dataRow, action);

            InsertRow(dataRow, -1, -1, false);
            switch (loadOption)
            {
                case LoadOption.OverwriteChanges:
                case LoadOption.PreserveChanges:
                    SetOldRecord(dataRow, recordNo);
                    break;
                case LoadOption.Upsert:
                    break;
                default:
                    throw ExceptionBuilder.ArgumentOutOfRange(nameof(LoadOption));
            }
            RaiseRowChanged(drcevent, dataRow, action);

            return dataRow;
        }

        private void SetDataRowWithLoadOption(DataRow dataRow, int recordNo, LoadOption loadOption, bool checkReadOnly)
        {
            bool hasError = false;
            if (checkReadOnly)
            {
                foreach (DataColumn dc in Columns)
                {
                    if (dc.ReadOnly && !dc.Computed)
                    {
                        switch (loadOption)
                        {
                            case LoadOption.OverwriteChanges:
                                if ((dataRow[dc, DataRowVersion.Current] != dc[recordNo]) || (dataRow[dc, DataRowVersion.Original] != dc[recordNo]))
                                    hasError = true;
                                break;
                            case LoadOption.Upsert:
                                if (dataRow[dc, DataRowVersion.Current] != dc[recordNo])
                                    hasError = true;
                                break;
                            case LoadOption.PreserveChanges:
                                if (dataRow[dc, DataRowVersion.Original] != dc[recordNo])
                                    hasError = true;
                                break;
                        }
                    }
                }
            } // No Event should be fired  in SenNewRecord and SetOldRecord
            // fire rowChanging event here

            DataRowChangeEventArgs? drcevent = null;
            DataRowAction action = DataRowAction.Nothing;
            int cacheTempRecord = dataRow._tempRecord;
            dataRow._tempRecord = recordNo;

            switch (loadOption)
            {
                case LoadOption.OverwriteChanges:
                    action = DataRowAction.ChangeCurrentAndOriginal;
                    break;
                case LoadOption.Upsert:
                    switch (dataRow.RowState)
                    {
                        case DataRowState.Unchanged:
                            // let see if the incomming value has the same values as existing row, so compare records
                            foreach (DataColumn dc in dataRow.Table.Columns)
                            {
                                if (0 != dc.Compare(dataRow._newRecord, recordNo))
                                {
                                    action = DataRowAction.Change;
                                    break;
                                }
                            }
                            break;
                        case DataRowState.Deleted:
                            Debug.Fail("LoadOption.Upsert with deleted row, should not be here");
                            break;
                        default:
                            action = DataRowAction.Change;
                            break;
                    }
                    break;
                case LoadOption.PreserveChanges:
                    action = dataRow.RowState switch
                    {
                        DataRowState.Unchanged => DataRowAction.ChangeCurrentAndOriginal,
                        _ => DataRowAction.ChangeOriginal,
                    };
                    break;
                default:
                    throw ExceptionBuilder.ArgumentOutOfRange(nameof(LoadOption));
            }

            try
            {
                drcevent = RaiseRowChanging(null, dataRow, action);
                if (action == DataRowAction.Nothing)
                { // RaiseRowChanging does not fire for DataRowAction.Nothing
                    dataRow._inChangingEvent = true;
                    try
                    {
                        drcevent = OnRowChanging(drcevent, dataRow, action);
                    }
                    finally
                    {
                        dataRow._inChangingEvent = false;
                    }
                }
            }
            finally
            {
                Debug.Assert(dataRow._tempRecord == recordNo, "tempRecord has been changed in event handler");
                if (DataRowState.Detached == dataRow.RowState)
                {
                    // 'row.Table.Remove(row);'
                    if (-1 != cacheTempRecord)
                    {
                        FreeRecord(ref cacheTempRecord);
                    }
                }
                else
                {
                    if (dataRow._tempRecord != recordNo)
                    {
                        // 'row.EndEdit(); row.BeginEdit(); '
                        if (-1 != cacheTempRecord)
                        {
                            FreeRecord(ref cacheTempRecord);
                        }
                        if (-1 != recordNo)
                        {
                            FreeRecord(ref recordNo);
                        }
                        recordNo = dataRow._tempRecord;
                    }
                    else
                    {
                        dataRow._tempRecord = cacheTempRecord;
                    }
                }
            }
            if (dataRow._tempRecord != -1)
            {
                dataRow.CancelEdit();
            }

            switch (loadOption)
            {
                case LoadOption.OverwriteChanges:
                    SetNewRecord(dataRow, recordNo, DataRowAction.Change, false, false);
                    SetOldRecord(dataRow, recordNo);
                    break;
                case LoadOption.Upsert:
                    if (dataRow.RowState == DataRowState.Unchanged)
                    {
                        SetNewRecord(dataRow, recordNo, DataRowAction.Change, false, false);
                        if (!dataRow.HasChanges())
                        {
                            SetOldRecord(dataRow, recordNo);
                        }
                    }
                    else
                    {
                        if (dataRow.RowState == DataRowState.Deleted)
                            dataRow.RejectChanges();
                        SetNewRecord(dataRow, recordNo, DataRowAction.Change, false, false);
                    }
                    break;
                case LoadOption.PreserveChanges:
                    if (dataRow.RowState == DataRowState.Unchanged)
                    {
                        // if ListChanged event deletes dataRow
                        SetOldRecord(dataRow, recordNo); // do not fire event
                        SetNewRecord(dataRow, recordNo, DataRowAction.Change, false, false);
                    }
                    else
                    {
                        // if modified/ added / deleted we want this operation to fire event (just for LoadOption.PreserveCurrentValues)
                        SetOldRecord(dataRow, recordNo);
                    }
                    break;
                default:
                    throw ExceptionBuilder.ArgumentOutOfRange(nameof(LoadOption));
            }

            if (hasError)
            {
                string error = SR.Load_ReadOnlyDataModified;
                if (dataRow.RowError.Length == 0)
                {
                    dataRow.RowError = error;
                }
                else
                {
                    dataRow.RowError += " ]:[ " + error;
                }

                foreach (DataColumn dc in Columns)
                {
                    if (dc.ReadOnly && !dc.Computed)
                    {
                        dataRow.SetColumnError(dc, error);
                    }
                }
            }

            drcevent = RaiseRowChanged(drcevent, dataRow, action);
            if (action == DataRowAction.Nothing)
            {
                // RaiseRowChanged does not fire for DataRowAction.Nothing
                dataRow._inChangingEvent = true;
                try
                {
                    OnRowChanged(drcevent, dataRow, action);
                }
                finally
                {
                    dataRow._inChangingEvent = false;
                }
            }
        }

        public DataTableReader CreateDataReader() => new DataTableReader(this);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(Stream? stream) => WriteXml(stream, XmlWriteMode.IgnoreSchema, false);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(Stream? stream, bool writeHierarchy) => WriteXml(stream, XmlWriteMode.IgnoreSchema, writeHierarchy);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(TextWriter? writer) => WriteXml(writer, XmlWriteMode.IgnoreSchema, false);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(TextWriter? writer, bool writeHierarchy) => WriteXml(writer, XmlWriteMode.IgnoreSchema, writeHierarchy);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(XmlWriter? writer) => WriteXml(writer, XmlWriteMode.IgnoreSchema, false);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(XmlWriter? writer, bool writeHierarchy) => WriteXml(writer, XmlWriteMode.IgnoreSchema, writeHierarchy);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(string fileName) => WriteXml(fileName, XmlWriteMode.IgnoreSchema, false);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(string fileName, bool writeHierarchy) => WriteXml(fileName, XmlWriteMode.IgnoreSchema, writeHierarchy);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(Stream? stream, XmlWriteMode mode) => WriteXml(stream, mode, false);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(Stream? stream, XmlWriteMode mode, bool writeHierarchy)
        {
            if (stream != null)
            {
                XmlTextWriter w = new XmlTextWriter(stream, null);
                w.Formatting = Formatting.Indented;

                WriteXml(w, mode, writeHierarchy);
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(TextWriter? writer, XmlWriteMode mode)
        {
            WriteXml(writer, mode, false);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(TextWriter? writer, XmlWriteMode mode, bool writeHierarchy)
        {
            if (writer != null)
            {
                XmlTextWriter w = new XmlTextWriter(writer);
                w.Formatting = Formatting.Indented;

                WriteXml(w, mode, writeHierarchy);
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(XmlWriter? writer, XmlWriteMode mode)
        {
            WriteXml(writer, mode, false);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(XmlWriter? writer, XmlWriteMode mode, bool writeHierarchy)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.WriteXml|API> {0}, mode={1}", ObjectID, mode);
            try
            {
                if (_tableName.Length == 0)
                {
                    throw ExceptionBuilder.CanNotSerializeDataTableWithEmptyName();
                }
                // Generate SchemaTree and write it out
                if (writer != null)
                {
                    if (mode == XmlWriteMode.DiffGram)
                    {
                        // Create and save the updates
                        new NewDiffgramGen(this, writeHierarchy).Save(writer, this);
                    }
                    else
                    {
                        // Create and save xml data
                        if (mode == XmlWriteMode.WriteSchema)
                        {
                            DataSet? ds = null;
                            string? tablenamespace = _tableNamespace;
                            if (null == DataSet)
                            {
                                ds = new DataSet();

                                // if user set values on DataTable, it isn't necessary
                                // to set them on the DataSet because they won't be inherited
                                // but it is simpler to set them in both places

                                // if user did not set values on DataTable, it is required
                                // to set them on the DataSet so the table will inherit
                                // the value already on the Datatable
                                ds.SetLocaleValue(_culture, _cultureUserSet);
                                ds.CaseSensitive = CaseSensitive;
                                ds.Namespace = Namespace;
                                ds.RemotingFormat = RemotingFormat;
                                ds.Tables.Add(this);
                            }

                            if (writer != null)
                            {
                                XmlDataTreeWriter xmldataWriter = new XmlDataTreeWriter(this, writeHierarchy);
                                xmldataWriter.Save(writer, /*mode == XmlWriteMode.WriteSchema*/true);
                            }
                            if (null != ds)
                            {
                                ds.Tables.Remove(this);
                                _tableNamespace = tablenamespace;
                            }
                        }
                        else
                        {
                            XmlDataTreeWriter xmldataWriter = new XmlDataTreeWriter(this, writeHierarchy);
                            xmldataWriter.Save(writer, /*mode == XmlWriteMode.WriteSchema*/ false);
                        }
                    }
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(string fileName, XmlWriteMode mode) => WriteXml(fileName, mode, false);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXml(string fileName, XmlWriteMode mode, bool writeHierarchy)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.WriteXml|API> {0}, fileName='{1}', mode={2}", ObjectID, fileName, mode);
            try
            {
                using (XmlTextWriter xw = new XmlTextWriter(fileName, null))
                {
                    xw.Formatting = Formatting.Indented;
                    xw.WriteStartDocument(true);

                    WriteXml(xw, mode, writeHierarchy);

                    xw.WriteEndDocument();
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXmlSchema(Stream? stream) => WriteXmlSchema(stream, false);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXmlSchema(Stream? stream, bool writeHierarchy)
        {
            if (stream == null)
            {
                return;
            }

            XmlTextWriter w = new XmlTextWriter(stream, null);
            w.Formatting = Formatting.Indented;

            WriteXmlSchema(w, writeHierarchy);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXmlSchema(TextWriter? writer) => WriteXmlSchema(writer, false);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXmlSchema(TextWriter? writer, bool writeHierarchy)
        {
            if (writer == null)
            {
                return;
            }

            XmlTextWriter w = new XmlTextWriter(writer);
            w.Formatting = Formatting.Indented;

            WriteXmlSchema(w, writeHierarchy);
        }

        private bool CheckForClosureOnExpressions(DataTable dt, bool writeHierarchy)
        {
            List<DataTable> tableList = new List<DataTable>();
            tableList.Add(dt);
            if (writeHierarchy)
            {
                CreateTableList(dt, tableList);
            }
            return CheckForClosureOnExpressionTables(tableList);
        }

        private static bool CheckForClosureOnExpressionTables(List<DataTable> tableList)
        {
            Debug.Assert(tableList != null, "tableList shouldnot be null");

            foreach (DataTable datatable in tableList)
            {
                foreach (DataColumn dc in datatable.Columns)
                {
                    if (dc.Expression.Length != 0)
                    {
                        DataColumn[] dependency = dc.DataExpression!.GetDependency();
                        for (int j = 0; j < dependency.Length; j++)
                        {
                            if (!(tableList.Contains(dependency[j].Table!)))
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXmlSchema(XmlWriter? writer) => WriteXmlSchema(writer, false);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXmlSchema(XmlWriter? writer, bool writeHierarchy)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.WriteXmlSchema|API> {0}", ObjectID);
            try
            {
                if (_tableName.Length == 0)
                {
                    throw ExceptionBuilder.CanNotSerializeDataTableWithEmptyName();
                }

                if (!CheckForClosureOnExpressions(this, writeHierarchy))
                {
                    throw ExceptionBuilder.CanNotSerializeDataTableHierarchy();
                }

                DataSet? ds = null;
                string? tablenamespace = _tableNamespace; //SQL BU Defect Tracking 286968

                // Generate SchemaTree and write it out
                if (null == DataSet)
                {
                    ds = new DataSet();
                    // if user set values on DataTable, it isn't necessary
                    // to set them on the DataSet because they won't be inherited
                    // but it is simpler to set them in both places

                    // if user did not set values on DataTable, it is required
                    // to set them on the DataSet so the table will inherit
                    // the value already on the Datatable
                    ds.SetLocaleValue(_culture, _cultureUserSet);
                    ds.CaseSensitive = CaseSensitive;
                    ds.Namespace = Namespace;
                    ds.RemotingFormat = RemotingFormat;
                    ds.Tables.Add(this);
                }

                if (writer != null)
                {
                    XmlTreeGen treeGen = new XmlTreeGen(SchemaFormat.Public);
                    treeGen.Save(null, this, writer, writeHierarchy);
                }
                if (null != ds)
                {
                    ds.Tables.Remove(this);
                    _tableNamespace = tablenamespace;
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXmlSchema(string fileName) => WriteXmlSchema(fileName, false);

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void WriteXmlSchema(string fileName, bool writeHierarchy)
        {
            XmlTextWriter xw = new XmlTextWriter(fileName, null);
            try
            {
                xw.Formatting = Formatting.Indented;
                xw.WriteStartDocument(true);
                WriteXmlSchema(xw, writeHierarchy);
                xw.WriteEndDocument();
            }
            finally
            {
                xw.Close();
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public XmlReadMode ReadXml(Stream? stream)
        {
            if (stream == null)
            {
                return XmlReadMode.Auto;
            }

            XmlTextReader xr = new XmlTextReader(stream);

            // Prevent Dtd entity in DataTable
            xr.XmlResolver = null;

            return ReadXml(xr, false);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public XmlReadMode ReadXml(TextReader? reader)
        {
            if (reader == null)
            {
                return XmlReadMode.Auto;
            }

            XmlTextReader xr = new XmlTextReader(reader);

            // Prevent Dtd entity in DataTable
            xr.XmlResolver = null;

            return ReadXml(xr, false);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public XmlReadMode ReadXml(string fileName)
        {
            XmlTextReader xr = new XmlTextReader(fileName);

            // Prevent Dtd entity in DataTable
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

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public XmlReadMode ReadXml(XmlReader? reader) => ReadXml(reader, false);

        private void RestoreConstraint(bool originalEnforceConstraint)
        {
            if (DataSet != null)
            {
                DataSet.EnforceConstraints = originalEnforceConstraint;
            }
            else
            {
                EnforceConstraints = originalEnforceConstraint;
            }
        }

        private bool IsEmptyXml(XmlReader reader)
        {
            if (reader.IsEmptyElement)
            {
                if (reader.AttributeCount == 0 || (reader.LocalName == Keywords.DIFFGRAM && reader.NamespaceURI == Keywords.DFFNS))
                {
                    return true;
                }
                if (reader.AttributeCount == 1)
                {
                    reader.MoveToAttribute(0);
                    if ((Namespace == reader.Value) &&
                        (Prefix == reader.LocalName) &&
                        (reader.Prefix == Keywords.XMLNS) &&
                        (reader.NamespaceURI == Keywords.XSD_XMLNS_NS))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal XmlReadMode ReadXml(XmlReader? reader, bool denyResolving)
        {
            IDisposable? restrictedScope = null;
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.ReadXml|INFO> {0}, denyResolving={1}", ObjectID, denyResolving);
            try
            {
                restrictedScope = TypeLimiter.EnterRestrictedScope(this);
                RowDiffIdUsageSection rowDiffIdUsage = default;
                try
                {
                    bool fDataFound = false;
                    bool fSchemaFound = false;
                    bool fDiffsFound = false;
                    bool fIsXdr = false;
                    int iCurrentDepth = -1;
                    XmlReadMode ret = XmlReadMode.Auto;

                    // clear the hashtable to avoid conflicts between diffgrams, SqlHotFix 782
                    rowDiffIdUsage.Prepare(this);

                    if (reader == null)
                    {
                        return ret;
                    }

                    bool originalEnforceConstraint = false;
                    if (DataSet != null)
                    {
                        originalEnforceConstraint = DataSet.EnforceConstraints;
                        DataSet.EnforceConstraints = false;
                    }
                    else
                    {
                        originalEnforceConstraint = EnforceConstraints;
                        EnforceConstraints = false;
                    }

                    if (reader is XmlTextReader)
                    {
                        ((XmlTextReader)reader).WhitespaceHandling = WhitespaceHandling.Significant;
                    }

                    XmlDocument xdoc = new XmlDocument(); // we may need this to infer the schema
                    XmlDataLoader? xmlload = null;

                    reader.MoveToContent();
                    if (Columns.Count == 0)
                    {
                        if (IsEmptyXml(reader))
                        {
                            reader.Read();
                            return ret;
                        }
                    }

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        iCurrentDepth = reader.Depth;

                        if ((reader.LocalName == Keywords.DIFFGRAM) && (reader.NamespaceURI == Keywords.DFFNS))
                        {
                            if (Columns.Count == 0)
                            {
                                if (reader.IsEmptyElement)
                                {
                                    reader.Read();
                                    return XmlReadMode.DiffGram;
                                }
                                throw ExceptionBuilder.DataTableInferenceNotSupported();
                            }
                            ReadXmlDiffgram(reader);
                            // read the closing tag of the current element
                            ReadEndElement(reader);

                            RestoreConstraint(originalEnforceConstraint);
                            return XmlReadMode.DiffGram;
                        }

                        // if reader points to the schema load it
                        if (reader.LocalName == Keywords.XDR_SCHEMA && reader.NamespaceURI == Keywords.XDRNS)
                        {
                            // load XDR schema and exit
                            ReadXDRSchema(reader);

                            RestoreConstraint(originalEnforceConstraint);
                            return XmlReadMode.ReadSchema; //since the top level element is a schema return
                        }

                        if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI == Keywords.XSDNS)
                        {
                            // load XSD schema and exit
                            ReadXmlSchema(reader, denyResolving);
                            RestoreConstraint(originalEnforceConstraint);
                            return XmlReadMode.ReadSchema; //since the top level element is a schema return
                        }

                        if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI.StartsWith(Keywords.XSD_NS_START, StringComparison.Ordinal))
                        {
                            if (DataSet != null)
                            { // we should not throw for constraint, we already will throw for unsupported schema, so restore enforce cost, but not via property
                                DataSet.RestoreEnforceConstraints(originalEnforceConstraint);
                            }
                            else
                            {
                                _enforceConstraints = originalEnforceConstraint;
                            }

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
                            if ((reader.LocalName == Keywords.DIFFGRAM) && (reader.NamespaceURI == Keywords.DFFNS))
                            {
                                ReadXmlDiffgram(reader);
                                // read the closing tag of the current element
                                ReadEndElement(reader);
                                RestoreConstraint(originalEnforceConstraint);
                                return XmlReadMode.DiffGram;
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
                                ReadXmlSchema(reader, denyResolving);
                                fSchemaFound = true;
                                continue;
                            }

                            if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI.StartsWith(Keywords.XSD_NS_START, StringComparison.Ordinal))
                            {
                                if (DataSet != null)
                                {
                                    // we should not throw for constraint, we already will throw for unsupported schema, so restore enforce cost, but not via property
                                    DataSet.RestoreEnforceConstraints(originalEnforceConstraint);
                                }
                                else
                                {
                                    _enforceConstraints = originalEnforceConstraint;
                                }
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
                                // we found data here
                                fDataFound = true;

                                if (!fSchemaFound && Columns.Count == 0)
                                {
                                    XmlNode node = xdoc.ReadNode(reader)!;
                                    topNode.AppendChild(node);
                                }
                                else
                                {
                                    if (xmlload == null)
                                    {
                                        xmlload = new XmlDataLoader(this, fIsXdr, topNode, false);
                                    }
                                    xmlload.LoadData(reader);
                                    ret = fSchemaFound ? XmlReadMode.ReadSchema : XmlReadMode.IgnoreSchema;
                                }
                            }
                        }
                        // read the closing tag of the current element
                        ReadEndElement(reader);

                        // now top node contains the data part
                        xdoc.AppendChild(topNode);

                        if (!fSchemaFound && Columns.Count == 0)
                        {
                            if (IsEmptyXml(reader))
                            {
                                reader.Read();
                                return ret;
                            }
                            throw ExceptionBuilder.DataTableInferenceNotSupported();
                        }

                        if (xmlload == null)
                            xmlload = new XmlDataLoader(this, fIsXdr, false);

                        // so we InferSchema
                        if (!fDiffsFound)
                        {
                            // we need to add support for inference here
                        }
                    }
                    RestoreConstraint(originalEnforceConstraint);
                    return ret;
                }
                finally
                {
                    rowDiffIdUsage.Cleanup();
                }
            }
            finally
            {
                restrictedScope?.Dispose();
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal XmlReadMode ReadXml(XmlReader? reader, XmlReadMode mode, bool denyResolving)
        {
            IDisposable? restrictedScope = null;
            RowDiffIdUsageSection rowDiffIdUsage = default;
            try
            {
                restrictedScope = TypeLimiter.EnterRestrictedScope(this);
                bool fSchemaFound = false;
                bool fDataFound = false;
                bool fIsXdr = false;
                int iCurrentDepth = -1;
                XmlReadMode ret = mode;

                // prepare and cleanup rowDiffId hashtable
                rowDiffIdUsage.Prepare(this);

                if (reader == null)
                {
                    return ret;
                }

                bool originalEnforceConstraint = false;
                if (DataSet != null)
                {
                    originalEnforceConstraint = DataSet.EnforceConstraints;
                    DataSet.EnforceConstraints = false;
                }
                else
                {
                    originalEnforceConstraint = EnforceConstraints;
                    EnforceConstraints = false;
                }

                if (reader is XmlTextReader)
                    ((XmlTextReader)reader).WhitespaceHandling = WhitespaceHandling.Significant;

                XmlDocument xdoc = new XmlDocument(); // we may need this to infer the schema

                if ((mode != XmlReadMode.Fragment) && (reader.NodeType == XmlNodeType.Element))
                {
                    iCurrentDepth = reader.Depth;
                }

                reader.MoveToContent();
                if (Columns.Count == 0)
                {
                    if (IsEmptyXml(reader))
                    {
                        reader.Read();
                        return ret;
                    }
                }

                XmlDataLoader? xmlload = null;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    XmlElement? topNode = null;
                    if (mode == XmlReadMode.Fragment)
                    {
                        xdoc.AppendChild(xdoc.CreateElement("ds_sqlXmlWraPPeR"));
                        topNode = xdoc.DocumentElement!;
                    }
                    else
                    {
                        //handle the top node
                        if ((reader.LocalName == Keywords.DIFFGRAM) && (reader.NamespaceURI == Keywords.DFFNS))
                        {
                            if ((mode == XmlReadMode.DiffGram) || (mode == XmlReadMode.IgnoreSchema))
                            {
                                if (Columns.Count == 0)
                                {
                                    if (reader.IsEmptyElement)
                                    {
                                        reader.Read();
                                        return XmlReadMode.DiffGram;
                                    }
                                    throw ExceptionBuilder.DataTableInferenceNotSupported();
                                }
                                ReadXmlDiffgram(reader);
                                // read the closing tag of the current element
                                ReadEndElement(reader);
                            }
                            else
                            {
                                reader.Skip();
                            }
                            RestoreConstraint(originalEnforceConstraint);
                            return ret;
                        }

                        if (reader.LocalName == Keywords.XDR_SCHEMA && reader.NamespaceURI == Keywords.XDRNS)
                        {
                            // load XDR schema and exit
                            if ((mode != XmlReadMode.IgnoreSchema) && (mode != XmlReadMode.InferSchema))
                            {
                                ReadXDRSchema(reader);
                            }
                            else
                            {
                                reader.Skip();
                            }
                            RestoreConstraint(originalEnforceConstraint);
                            return ret; //since the top level element is a schema return
                        }

                        if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI == Keywords.XSDNS)
                        {
                            // load XSD schema and exit
                            if ((mode != XmlReadMode.IgnoreSchema) && (mode != XmlReadMode.InferSchema))
                            {
                                ReadXmlSchema(reader, denyResolving);
                            }
                            else
                            {
                                reader.Skip();
                            }

                            RestoreConstraint(originalEnforceConstraint);
                            return ret; //since the top level element is a schema return
                        }

                        if (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI.StartsWith(Keywords.XSD_NS_START, StringComparison.Ordinal))
                        {
                            if (DataSet != null)
                            {
                                // we should not throw for constraint, we already will throw for unsupported schema, so restore enforce cost, but not via property
                                DataSet.RestoreEnforceConstraints(originalEnforceConstraint);
                            }
                            else
                            {
                                _enforceConstraints = originalEnforceConstraint;
                            }
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
                    }

                    while (MoveToElement(reader, iCurrentDepth))
                    {
                        if (reader.LocalName == Keywords.XDR_SCHEMA && reader.NamespaceURI == Keywords.XDRNS)
                        {
                            // load XDR schema
                            if (!fSchemaFound && !fDataFound && (mode != XmlReadMode.IgnoreSchema) && (mode != XmlReadMode.InferSchema))
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
                            if ((mode != XmlReadMode.IgnoreSchema) && (mode != XmlReadMode.InferSchema))
                            {
                                ReadXmlSchema(reader, denyResolving);
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
                                if (Columns.Count == 0)
                                {
                                    if (reader.IsEmptyElement)
                                    {
                                        reader.Read();
                                        return XmlReadMode.DiffGram;
                                    }
                                    throw ExceptionBuilder.DataTableInferenceNotSupported();
                                }
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
                        {
                            if (DataSet != null)
                            {
                                // we should not throw for constraint, we already will throw for unsupported schema, so restore enforce cost, but not via property
                                DataSet.RestoreEnforceConstraints(originalEnforceConstraint);
                            }
                            else
                            {
                                _enforceConstraints = originalEnforceConstraint;
                            }
                            throw ExceptionBuilder.DataSetUnsupportedSchema(Keywords.XSDNS);
                        }

                        if (mode == XmlReadMode.DiffGram)
                        {
                            reader.Skip();
                            continue; // we do not read data in diffgram mode
                        }

                        // if we are here we found some data
                        fDataFound = true;

                        if (mode == XmlReadMode.InferSchema)
                        {
                            //save the node in DOM until the end;
                            XmlNode node = xdoc.ReadNode(reader)!;
                            topNode.AppendChild(node);
                        }
                        else
                        {
                            if (Columns.Count == 0)
                            {
                                throw ExceptionBuilder.DataTableInferenceNotSupported();
                            }
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
                    {
                        xmlload = new XmlDataLoader(this, fIsXdr, mode == XmlReadMode.IgnoreSchema);
                    }

                    if (mode == XmlReadMode.DiffGram)
                    {
                        // we already got the diffs through XmlReader interface
                        RestoreConstraint(originalEnforceConstraint);
                        return ret;
                    }

                    // Load Data
                    if (mode == XmlReadMode.InferSchema)
                    {
                        if (Columns.Count == 0)
                        {
                            throw ExceptionBuilder.DataTableInferenceNotSupported();
                        }
                    }
                }
                RestoreConstraint(originalEnforceConstraint);

                return ret;
            }
            finally
            {
                restrictedScope?.Dispose();
                // prepare and cleanup rowDiffId hashtable
                rowDiffIdUsage.Cleanup();
            }
        }

        internal static void ReadEndElement(XmlReader reader)
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
        internal static void ReadXDRSchema(XmlReader reader)
        {
            XmlDocument xdoc = new XmlDocument(); // we may need this to infer the schema
            xdoc.ReadNode(reader);
            //consume and ignore it - No support
        }

        internal static bool MoveToElement(XmlReader reader, int depth)
        {
            while (!reader.EOF && reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.Element && reader.Depth > depth)
            {
                reader.Read();
            }
            return (reader.NodeType == XmlNodeType.Element);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void ReadXmlDiffgram(XmlReader reader)
        {
            // fill correctly
            int d = reader.Depth;
            bool fEnforce = EnforceConstraints;
            EnforceConstraints = false;
            DataTable newDt;
            bool isEmpty;

            if (Rows.Count == 0)
            {
                isEmpty = true;
                newDt = this;
            }
            else
            {
                isEmpty = false;
                newDt = Clone();
                newDt.EnforceConstraints = false;
            }

            newDt.Rows._nullInList = 0;

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

            newDt._fInLoadDiffgram = true;

            if (reader.Depth > d)
            {
                if ((reader.NamespaceURI != Keywords.DFFNS) && (reader.NamespaceURI != Keywords.MSDNS))
                {
                    //we should be inside the dataset part
                    XmlDocument xdoc = new XmlDocument();
                    XmlElement node = xdoc.CreateElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                    reader.Read();
                    if (reader.Depth - 1 > d)
                    {
                        XmlDataLoader xmlload = new XmlDataLoader(newDt, false, node, false);
                        xmlload._isDiffgram = true; // turn on the special processing
                        xmlload.LoadData(reader);
                    }
                    ReadEndElement(reader);
                }

                if (((reader.LocalName == Keywords.SQL_BEFORE) && (reader.NamespaceURI == Keywords.DFFNS)) ||
                    ((reader.LocalName == Keywords.MSD_ERRORS) && (reader.NamespaceURI == Keywords.DFFNS)))
                {
                    //this will consume the changes and the errors part
                    XMLDiffLoader diffLoader = new XMLDiffLoader();
                    diffLoader.LoadDiffGram(newDt, reader);
                }

                // get to the closing diff tag
                while (reader.Depth > d)
                {
                    reader.Read();
                }

                // read the closing tag
                ReadEndElement(reader);
            }

            if (newDt.Rows._nullInList > 0)
            {
                throw ExceptionBuilder.RowInsertMissing(newDt.TableName);
            }

            newDt._fInLoadDiffgram = false;
            List<DataTable> tableList = new List<DataTable>();
            tableList.Add(this);
            CreateTableList(this, tableList);

            // this is terrible, optimize it
            for (int i = 0; i < tableList.Count; i++)
            {
                DataRelation[] relations = tableList[i].NestedParentRelations;
                foreach (DataRelation rel in relations)
                {
                    if (rel != null && rel.ParentTable == tableList[i])
                    {
                        foreach (DataRow r in tableList[i].Rows)
                        {
                            foreach (DataRelation rel2 in relations)
                            {
                                r.CheckForLoops(rel2);
                            }
                        }
                    }
                }
            }

            if (!isEmpty)
            {
                Merge(newDt);
            }
            EnforceConstraints = fEnforce;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void ReadXSDSchema(XmlReader reader, bool denyResolving)
        {
            XmlSchemaSet sSet = new XmlSchemaSet();
            while (reader.LocalName == Keywords.XSD_SCHEMA && reader.NamespaceURI == Keywords.XSDNS)
            {
                XmlSchema s = XmlSchema.Read(reader, null)!;
                sSet.Add(s);
                //read the end tag
                ReadEndElement(reader);
            }
            sSet.Compile();

            XSDSchema schema = new XSDSchema();
            schema.LoadSchema(sSet, this);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void ReadXmlSchema(Stream? stream)
        {
            if (stream == null)
            {
                return;
            }

            ReadXmlSchema(new XmlTextReader(stream), false);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void ReadXmlSchema(TextReader? reader)
        {
            if (reader == null)
            {
                return;
            }

            ReadXmlSchema(new XmlTextReader(reader), false);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
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

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public void ReadXmlSchema(XmlReader? reader)
        {
            ReadXmlSchema(reader, false);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void ReadXmlSchema(XmlReader? reader, bool denyResolving)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTable.ReadXmlSchema|INFO> {0}, denyResolving={1}", ObjectID, denyResolving);
            try
            {
                DataSet ds = new DataSet();
                SerializationFormat cachedRemotingFormat = RemotingFormat;
                // fxcop: ReadXmlSchema will provide the CaseSensitive, Locale, Namespace information
                ds.ReadXmlSchema(reader, denyResolving);

                string CurrentTableFullName = ds.MainTableName;

                if (string.IsNullOrEmpty(_tableName) && string.IsNullOrEmpty(CurrentTableFullName))
                {
                    return;
                }

                DataTable? currentTable = null;

                if (!string.IsNullOrEmpty(_tableName))
                {
                    if (!string.IsNullOrEmpty(Namespace))
                    {
                        currentTable = ds.Tables[_tableName, Namespace];
                    }
                    else
                    {
                        int tableIndex = ds.Tables.InternalIndexOf(_tableName);
                        if (tableIndex > -1)
                        {
                            currentTable = ds.Tables[tableIndex];
                        }
                    }
                }
                else
                {
                    string CurrentTableNamespace = string.Empty;
                    int nsSeparator = CurrentTableFullName.IndexOf(':');
                    if (nsSeparator > -1)
                    {
                        CurrentTableNamespace = CurrentTableFullName.Substring(0, nsSeparator);
                    }
                    string CurrentTableName = CurrentTableFullName.Substring(nsSeparator + 1);

                    currentTable = ds.Tables[CurrentTableName, CurrentTableNamespace];
                }

                if (currentTable == null)
                {
                    string qTableName = string.Empty;
                    if (!string.IsNullOrEmpty(_tableName))
                    {
                        qTableName = (Namespace.Length > 0) ? (Namespace + ":" + _tableName) : _tableName;
                    }
                    else
                    {
                        qTableName = CurrentTableFullName;
                    }
                    throw ExceptionBuilder.TableNotFound(qTableName);
                }

                currentTable._remotingFormat = cachedRemotingFormat;

                List<DataTable> tableList = new List<DataTable>();
                tableList.Add(currentTable);
                CreateTableList(currentTable, tableList);
                List<DataRelation> relationList = new List<DataRelation>();
                CreateRelationList(tableList, relationList);

                if (relationList.Count == 0)
                {
                    if (Columns.Count == 0)
                    {
                        DataTable tempTable = currentTable;
                        tempTable.CloneTo(this, null, false); // we may have issue Amir

                        if (DataSet == null && _tableNamespace == null)
                        {
                            // for standalone table, clone won't get these correctly, since they may come with inheritance
                            _tableNamespace = tempTable.Namespace;
                        }
                    }
                    return;
                }
                else
                {
                    if (string.IsNullOrEmpty(TableName))
                    {
                        TableName = currentTable.TableName;
                        if (!string.IsNullOrEmpty(currentTable.Namespace))
                        {
                            Namespace = currentTable.Namespace;
                        }
                    }
                    if (DataSet == null)
                    {
                        DataSet dataset = new DataSet(ds.DataSetName);

                        // if user set values on DataTable, it isn't necessary
                        // to set them on the DataSet because they won't be inherited
                        // but it is simpler to set them in both places

                        // if user did not set values on DataTable, it is required
                        // to set them on the DataSet so the table will inherit
                        // the value already on the Datatable
                        dataset.SetLocaleValue(ds.Locale, ds.ShouldSerializeLocale());
                        dataset.CaseSensitive = ds.CaseSensitive;
                        dataset.Namespace = ds.Namespace;
                        dataset._mainTableName = ds._mainTableName;
                        dataset.RemotingFormat = ds.RemotingFormat;

                        dataset.Tables.Add(this);
                    }

                    Debug.Assert(DataSet != null);

                    CloneHierarchy(currentTable, DataSet, null);

                    foreach (DataTable tempTable in tableList)
                    {
                        DataTable destinationTable = DataSet.Tables[tempTable._tableName, tempTable.Namespace]!;
                        DataTable sourceTable = ds.Tables[tempTable._tableName, tempTable.Namespace]!;
                        foreach (Constraint tempConstrain in sourceTable.Constraints)
                        {
                            ForeignKeyConstraint? fkc = tempConstrain as ForeignKeyConstraint;  // we have already cloned the UKC when cloning the datatable
                            if (fkc != null)
                            {
                                if (fkc.Table != fkc.RelatedTable)
                                {
                                    if (tableList.Contains(fkc.Table!) && tableList.Contains(fkc.RelatedTable))
                                    {
                                        ForeignKeyConstraint newFKC = (ForeignKeyConstraint)fkc.Clone(destinationTable.DataSet!)!;
                                        if (!destinationTable.Constraints.Contains(newFKC.ConstraintName))
                                        {
                                            destinationTable.Constraints.Add(newFKC); // we know that the dest table is already in the table
                                        }
                                    }
                                }
                            }
                        }
                    }

                    foreach (DataRelation rel in relationList)
                    {
                        if (!DataSet.Relations.Contains(rel.RelationName))
                        {
                            DataSet.Relations.Add(rel.Clone(DataSet));
                        }
                    }

                    bool hasExternaldependency = false;

                    foreach (DataTable tempTable in tableList)
                    {
                        foreach (DataColumn dc in tempTable.Columns)
                        {
                            hasExternaldependency = false;
                            if (dc.Expression.Length != 0)
                            {
                                DataColumn[] dependency = dc.DataExpression!.GetDependency();
                                for (int j = 0; j < dependency.Length; j++)
                                {
                                    if (!tableList.Contains(dependency[j].Table!))
                                    {
                                        hasExternaldependency = true;
                                        break;
                                    }
                                }
                            }
                            if (!hasExternaldependency)
                            {
                                DataSet.Tables[tempTable.TableName, tempTable.Namespace]!.Columns[dc.ColumnName]!.Expression = dc.Expression;
                            }
                        }
                        hasExternaldependency = false;
                    }
                }
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        private void CreateTableList(DataTable currentTable, List<DataTable> tableList)
        {
            foreach (DataRelation r in currentTable.ChildRelations)
            {
                if (!tableList.Contains(r.ChildTable))
                {
                    tableList.Add(r.ChildTable);
                    CreateTableList(r.ChildTable, tableList);
                }
            }
        }
        private static void CreateRelationList(List<DataTable> tableList, List<DataRelation> relationList)
        {
            foreach (DataTable table in tableList)
            {
                foreach (DataRelation r in table.ChildRelations)
                {
                    if (tableList.Contains(r.ChildTable) && tableList.Contains(r.ParentTable))
                    {
                        relationList.Add(r);
                    }
                }
            }
        }

        public static XmlSchemaComplexType GetDataTableSchema(XmlSchemaSet? schemaSet)
        {
            XmlSchemaComplexType type = new XmlSchemaComplexType();
            XmlSchemaSequence sequence = new XmlSchemaSequence();
            XmlSchemaAny any = new XmlSchemaAny();
            any.Namespace = XmlSchema.Namespace;
            any.MinOccurs = 0;
            any.MaxOccurs = decimal.MaxValue;
            any.ProcessContents = XmlSchemaContentProcessing.Lax;
            sequence.Items.Add(any);

            any = new XmlSchemaAny();
            any.Namespace = Keywords.DFFNS;
            any.MinOccurs = 1; // when recognizing WSDL - MinOccurs="0" denotes DataSet, a MinOccurs="1" for DataTable
            any.ProcessContents = XmlSchemaContentProcessing.Lax;
            sequence.Items.Add(any);

            type.Particle = sequence;

            return type;
        }

#pragma warning disable IL2026 // suppressed in ILLink.Suppressions.LibraryBuild.xml
        XmlSchema? IXmlSerializable.GetSchema() => GetXmlSchema();
#pragma warning restore IL2026

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2046:UnrecognizedReflectionPattern",
            Justification = "https://github.com/mono/linker/issues/1187 Trimmer thinks this implements IXmlSerializable.GetSchema() and warns about not matching attributes.")]
        protected virtual XmlSchema? GetSchema()
        {
            if (GetType() == typeof(DataTable))
            {
                return null;
            }
            MemoryStream stream = new MemoryStream();

            XmlWriter writer = new XmlTextWriter(stream, null);
            if (writer != null)
            {
                (new XmlTreeGen(SchemaFormat.WebService)).Save(this, writer);
            }
            stream.Position = 0;
            return XmlSchema.Read(new XmlTextReader(stream), null);
        }

        [RequiresUnreferencedCode("DataTable.GetSchema uses TypeDescriptor and XmlSerialization underneath which are not trimming safe. Members from serialized types may be trimmed if not referenced directly.")]
        private XmlSchema? GetXmlSchema()
        {
            return GetSchema();
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            IXmlTextParser? textReader = reader as IXmlTextParser;
            bool fNormalization = true;
            if (textReader != null)
            {
                fNormalization = textReader.Normalized;
                textReader.Normalized = false;
            }
#pragma warning disable IL2026 // suppressed in ILLink.Suppressions.LibraryBuild.xml
            ReadXmlSerializableInternal(reader);
#pragma warning restore IL2026

            if (textReader != null)
            {
                textReader.Normalized = fNormalization;
            }
        }

        [RequiresUnreferencedCode("DataTable.ReadXml uses XmlSerialization underneath which is not trimming safe. Members from serialized types may be trimmed if not referenced directly.")]
        private void ReadXmlSerializableInternal(XmlReader reader)
        {
            ReadXmlSerializable(reader);
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
#pragma warning disable IL2026 // suppressed in ILLink.Suppressions.LibraryBuild.xml
            WriteXmlInternal(writer);
#pragma warning restore IL2026
        }

        [RequiresUnreferencedCode("DataTable.WriteXml uses XmlSerialization underneath which is not trimming safe. Members from serialized types may be trimmed if not referenced directly.")]
        private void WriteXmlInternal(XmlWriter writer)
        {
            WriteXmlSchema(writer, false);
            WriteXml(writer, XmlWriteMode.DiffGram, false);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        protected virtual void ReadXmlSerializable(XmlReader? reader) => ReadXml(reader, XmlReadMode.DiffGram, true);

        // RowDiffIdUsageSection & DSRowDiffIdUsageSection Usage:
        //
        //        DataTable.[DS]RowDiffIdUsageSection rowDiffIdUsage = new DataTable.[DS]RowDiffIdUsageSection();
        //        try {
        //            rowDiffIdUsage.Prepare(DataTable or DataSet, depending on type);
        //
        //            // code that requires RowDiffId usage
        //
        //        }
        //        finally {
        //            rowDiffIdUsage.Cleanup();
        //        }
        //
        // Nested calls are allowed on different tables. For example, user can register to row change events and trigger
        // ReadXml on different table/ds). But, if user code will try to call ReadXml on table that is already in the scope,
        // this code will assert since nested calls on same table are unsupported.
        internal struct RowDiffIdUsageSection
        {
#if DEBUG
            // This list contains tables currently used in diffgram processing, not including new tables that might be added later during.
            // if diffgram processing is not started, this value must be null. when it starts, relevant method should call Prepare.
            // Notes:
            // * in case of ReadXml on empty DataSet, this list can be initialized as empty (so empty list != null).
            // * only one scope is allowed on single thread, either for datatable or dataset
            // * assert is triggered if same table is added to this list twice
            //
            // do not allocate TLS data in RETAIL bits!
            [ThreadStatic]
            internal static List<DataTable>? t_usedTables;
#endif //DEBUG

            private DataTable _targetTable;

            internal void Prepare(DataTable table)
            {
                Debug.Assert(_targetTable == null, "do not reuse this section");
                Debug.Assert(table != null);
                Debug.Assert(table._rowDiffId == null, "rowDiffId wasn't previously cleared");
#if DEBUG
                Debug.Assert(t_usedTables == null || !t_usedTables.Contains(table),
                    "Nested call with same table can cause data corruption!");
#endif

#if DEBUG
                if (t_usedTables == null)
                    t_usedTables = new List<DataTable>();
                t_usedTables.Add(table);
#endif
                _targetTable = table;
                table._rowDiffId = null;
            }

            [Conditional("DEBUG")]
            internal void Cleanup()
            {
                // cannot assume target table was set
                if (_targetTable != null)
                {
#if DEBUG
                    Debug.Assert(t_usedTables != null && t_usedTables.Contains(_targetTable), "missing Prepare before Cleanup");
                    if (t_usedTables != null)
                    {
                        t_usedTables.Remove(_targetTable);
                        if (t_usedTables.Count == 0)
                            t_usedTables = null;
                    }
#endif
                    _targetTable._rowDiffId = null;
                }
            }

            [Conditional("DEBUG")]
            internal static void Assert(string message)
            {
#if DEBUG
                // this code asserts scope was created, but it does not assert that the table was included in it
                // note that in case of DataSet, new tables might be added to the list in which case they won't appear in t_usedTables.
                Debug.Assert(t_usedTables != null, message);
#endif
            }
        }

        internal struct DSRowDiffIdUsageSection
        {
            private DataSet _targetDS;

            internal void Prepare(DataSet ds)
            {
                Debug.Assert(_targetDS == null, "do not reuse this section");
                Debug.Assert(ds != null);

                _targetDS = ds;
#if DEBUG
                // initialize list of tables out of current tables
                // note: it might remain empty (still initialization is needed for assert to operate)
                if (RowDiffIdUsageSection.t_usedTables == null)
                    RowDiffIdUsageSection.t_usedTables = new List<DataTable>();
#endif
                for (int tableIndex = 0; tableIndex < ds.Tables.Count; ++tableIndex)
                {
                    DataTable table = ds.Tables[tableIndex];
#if DEBUG
                    Debug.Assert(!RowDiffIdUsageSection.t_usedTables.Contains(table), "Nested call with same table can cause data corruption!");
                    RowDiffIdUsageSection.t_usedTables.Add(table);
#endif
                    Debug.Assert(table._rowDiffId == null, "rowDiffId wasn't previously cleared");
                    table._rowDiffId = null;
                }
            }

            [Conditional("DEBUG")]
            internal void Cleanup()
            {
                // cannot assume target was set
                if (_targetDS != null)
                {
#if DEBUG
                    Debug.Assert(RowDiffIdUsageSection.t_usedTables != null, "missing Prepare before Cleanup");
#endif

                    for (int tableIndex = 0; tableIndex < _targetDS.Tables.Count; ++tableIndex)
                    {
                        DataTable table = _targetDS.Tables[tableIndex];
#if DEBUG
                        // cannot assert that table exists in the usedTables - new tables might be
                        // created during diffgram processing in DataSet.ReadXml.
                        if (RowDiffIdUsageSection.t_usedTables != null)
                            RowDiffIdUsageSection.t_usedTables.Remove(table);
#endif
                        table._rowDiffId = null;
                    }
#if DEBUG
                    if (RowDiffIdUsageSection.t_usedTables != null && RowDiffIdUsageSection.t_usedTables.Count == 0)
                        RowDiffIdUsageSection.t_usedTables = null; // out-of-scope
#endif
                }
            }
        }

        internal Hashtable RowDiffId
        {
            get
            {
                // assert scope has been created either with RowDiffIdUsageSection.Prepare or DSRowDiffIdUsageSection.Prepare
                RowDiffIdUsageSection.Assert("missing call to RowDiffIdUsageSection.Prepare or DSRowDiffIdUsageSection.Prepare");

                if (_rowDiffId == null)
                {
                    _rowDiffId = new Hashtable();
                }
                return _rowDiffId;
            }
        }

        internal int ObjectID => _objectID;

        internal void AddDependentColumn(DataColumn expressionColumn)
        {
            if (_dependentColumns == null)
            {
                _dependentColumns = new List<DataColumn>();
            }

            if (!_dependentColumns.Contains(expressionColumn))
            {
                // only remember unique columns but expect non-unique columns to be added
                _dependentColumns.Add(expressionColumn);
            }
        }

        internal void RemoveDependentColumn(DataColumn expressionColumn)
        {
            if (_dependentColumns != null && _dependentColumns.Contains(expressionColumn))
            {
                _dependentColumns.Remove(expressionColumn);
            }
        }

        internal void EvaluateExpressions()
        {
            // evaluates all expressions for all rows in table
            // this improves performance by only computing expressions when they are present
            // and iterating over the rows instead of computing their position multiple times
            if ((null != _dependentColumns) && (0 < _dependentColumns.Count))
            {
                foreach (DataRow row in Rows)
                {
                    // only evaluate original values if different from current.
                    if (row._oldRecord != -1 && row._oldRecord != row._newRecord)
                    {
                        EvaluateDependentExpressions(_dependentColumns, row, DataRowVersion.Original, null);
                    }
                    if (row._newRecord != -1)
                    {
                        EvaluateDependentExpressions(_dependentColumns, row, DataRowVersion.Current, null);
                    }
                    if (row._tempRecord != -1)
                    {
                        EvaluateDependentExpressions(_dependentColumns, row, DataRowVersion.Proposed, null);
                    }
                }
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This is unsafe only when columns have associated expression. All ways to add such expression are marked unsafe.")]
        internal void EvaluateExpressions(DataRow row, DataRowAction action, List<DataRow>? cachedRows)
        {
            // evaluate all expressions for specified row
            if (action == DataRowAction.Add ||
                action == DataRowAction.Change ||
                (action == DataRowAction.Rollback && (row._oldRecord != -1 || row._newRecord != -1)))
            {
                // only evaluate original values if different from current.
                if (row._oldRecord != -1 && row._oldRecord != row._newRecord)
                {
                    EvaluateDependentExpressions(_dependentColumns, row, DataRowVersion.Original, cachedRows);
                }
                if (row._newRecord != -1)
                {
                    EvaluateDependentExpressions(_dependentColumns, row, DataRowVersion.Current, cachedRows);
                }
                if (row._tempRecord != -1)
                {
                    EvaluateDependentExpressions(_dependentColumns, row, DataRowVersion.Proposed, cachedRows);
                }
                return;
            }
            else if ((action == DataRowAction.Delete || (action == DataRowAction.Rollback && row._oldRecord == -1 && row._newRecord == -1)) && _dependentColumns != null)
            {
                foreach (DataColumn col in _dependentColumns)
                {
                    if (col.DataExpression != null && col.DataExpression.HasLocalAggregate() && col.Table == this)
                    {
                        for (int j = 0; j < Rows.Count; j++)
                        {
                            DataRow tableRow = Rows[j];

                            if (tableRow._oldRecord != -1 && tableRow._oldRecord != tableRow._newRecord)
                            {
                                EvaluateDependentExpressions(_dependentColumns, tableRow, DataRowVersion.Original, null);
                            }
                        }
                        for (int j = 0; j < Rows.Count; j++)
                        {
                            DataRow tableRow = Rows[j];

                            if (tableRow._tempRecord != -1)
                            {
                                EvaluateDependentExpressions(_dependentColumns, tableRow, DataRowVersion.Proposed, null);
                            }
                        }
                        // Order is important here - we need to update proposed before current
                        // Oherwise rows that are in edit state will get ListChanged/PropertyChanged event before default value is changed
                        // It is also the reason why we are not doping it in the single loop: EvaluateDependentExpression can update the
                        // whole table, if it happens, current for all but first row is updated before proposed value
                        for (int j = 0; j < Rows.Count; j++)
                        {
                            DataRow tableRow = Rows[j];

                            if (tableRow._newRecord != -1)
                            {
                                EvaluateDependentExpressions(_dependentColumns, tableRow, DataRowVersion.Current, null);
                            }
                        }
                        break;
                    }
                }

                if (cachedRows != null)
                {
                    foreach (DataRow relatedRow in cachedRows)
                    {
                        if (relatedRow._oldRecord != -1 && relatedRow._oldRecord != relatedRow._newRecord)
                        {
                            relatedRow.Table.EvaluateDependentExpressions(relatedRow.Table._dependentColumns, relatedRow, DataRowVersion.Original, null);
                        }
                        if (relatedRow._newRecord != -1)
                        {
                            relatedRow.Table.EvaluateDependentExpressions(relatedRow.Table._dependentColumns, relatedRow, DataRowVersion.Current, null);
                        }
                        if (relatedRow._tempRecord != -1)
                        {
                            relatedRow.Table.EvaluateDependentExpressions(relatedRow.Table._dependentColumns, relatedRow, DataRowVersion.Proposed, null);
                        }
                    }
                }
            }
        }

        internal void EvaluateExpressions(DataColumn column)
        {
            Debug.Assert(column._table != null);
            Debug.Assert(column.Computed, "Only computed columns should be re-evaluated.");

            // evaluates all rows for expression from specified column
            int count = column._table.Rows.Count;
            if (column.DataExpression!.IsTableAggregate() && count > 0)
            {
                // this value is a constant across the table.
                object aggCurrent = column.DataExpression!.Evaluate();
                for (int j = 0; j < count; j++)
                {
                    DataRow row = column._table.Rows[j];
                    // only evaluate original values if different from current.
                    if (row._oldRecord != -1 && row._oldRecord != row._newRecord)
                    {
                        column[row._oldRecord] = aggCurrent;
                    }
                    if (row._newRecord != -1)
                    {
                        column[row._newRecord] = aggCurrent;
                    }
                    if (row._tempRecord != -1)
                    {
                        column[row._tempRecord] = aggCurrent;
                    }
                }
            }
            else
            {
                for (int j = 0; j < count; j++)
                {
                    DataRow row = column._table.Rows[j];

                    if (row._oldRecord != -1 && row._oldRecord != row._newRecord)
                    {
                        column[row._oldRecord] = column.DataExpression.Evaluate(row, DataRowVersion.Original);
                    }
                    if (row._newRecord != -1)
                    {
                        column[row._newRecord] = column.DataExpression.Evaluate(row, DataRowVersion.Current);
                    }
                    if (row._tempRecord != -1)
                    {
                        column[row._tempRecord] = column.DataExpression.Evaluate(row, DataRowVersion.Proposed);
                    }
                }
            }

            column._table.ResetInternalIndexes(column);
            EvaluateDependentExpressions(column);
        }

        internal void EvaluateDependentExpressions(DataColumn column)
        {
            // DataTable.Clear(), DataRowCollection.Clear() & DataColumn.set_Expression
            if (column._dependentColumns != null)
            {
                foreach (DataColumn dc in column._dependentColumns)
                {
                    if ((dc._table != null) && !ReferenceEquals(column, dc))
                    {
                        EvaluateExpressions(dc);
                    }
                }
            }
        }

        internal void EvaluateDependentExpressions(List<DataColumn>? columns, DataRow row, DataRowVersion version, List<DataRow>? cachedRows)
        {
            if (columns == null)
            {
                return;
            }

            //Expression evaluation is done first over same table expressions.
            int count = columns.Count;
            for (int i = 0; i < count; i++)
            {
                if (columns[i].Table == this)
                {
                    // if this column is in my table
                    DataColumn dc = columns[i];
                    if (dc.DataExpression != null && dc.DataExpression.HasLocalAggregate())
                    {
                        // if column expression references a local Table aggregate we need to recalc it for the each row in the local table
                        DataRowVersion expressionVersion = (version == DataRowVersion.Proposed) ? DataRowVersion.Default : version;
                        bool isConst = dc.DataExpression.IsTableAggregate(); //is expression constant for entire table?
                        object? newValue = null;
                        if (isConst)
                        {
                            //if new value, just compute once
                            newValue = dc.DataExpression.Evaluate(row, expressionVersion);
                        }
                        for (int j = 0; j < Rows.Count; j++)
                        {
                            //evaluate for all rows in the table
                            DataRow dr = Rows[j];
                            if (dr.RowState == DataRowState.Deleted)
                            {
                                continue;
                            }
                            else if (expressionVersion == DataRowVersion.Original && (dr._oldRecord == -1 || dr._oldRecord == dr._newRecord))
                            {
                                continue;
                            }

                            if (!isConst)
                            {
                                newValue = dc.DataExpression.Evaluate(dr, expressionVersion);
                            }
                            SilentlySetValue(dr, dc, expressionVersion, newValue!);
                        }
                    }
                    else
                    {
                        if (row.RowState == DataRowState.Deleted)
                        {
                            continue;
                        }
                        else if (version == DataRowVersion.Original && (row._oldRecord == -1 || row._oldRecord == row._newRecord))
                        {
                            continue;
                        }
                        SilentlySetValue(row, dc, version, dc.DataExpression == null ? dc.DefaultValue : dc.DataExpression.Evaluate(row, version));
                    }
                }
            }
            // now do expression evaluation for expression columns other tables.
            count = columns.Count;
            for (int i = 0; i < count; i++)
            {
                DataColumn dc = columns[i];
                // if this column is NOT in my table or it is in the table and is not a local aggregate (self refs)
                if (dc.Table != this || (dc.DataExpression != null && !dc.DataExpression.HasLocalAggregate()))
                {
                    DataRowVersion foreignVer = (version == DataRowVersion.Proposed) ? DataRowVersion.Default : version;

                    // first - evaluate expressions for cachedRows (deletes & updates)
                    if (cachedRows != null)
                    {
                        foreach (DataRow cachedRow in cachedRows)
                        {
                            if (cachedRow.Table != dc.Table)
                            {
                                continue;
                            }

                            // don't update original version if child row doesn't have an oldRecord.
                            if (foreignVer == DataRowVersion.Original && cachedRow._newRecord == cachedRow._oldRecord)
                            {
                                continue;
                            }

                            if (cachedRow != null && ((cachedRow.RowState != DataRowState.Deleted) && (version != DataRowVersion.Original || cachedRow._oldRecord != -1)))
                            {
                                // if deleted GetRecordFromVersion will throw
                                // TODO: Possible bug, dc.DataExpression may be null
                                object newValue = dc.DataExpression!.Evaluate(cachedRow, foreignVer);
                                SilentlySetValue(cachedRow, dc, foreignVer, newValue);
                            }
                        }
                    }

                    // next check parent relations
                    for (int j = 0; j < ParentRelations.Count; j++)
                    {
                        DataRelation relation = ParentRelations[j];
                        if (relation.ParentTable != dc.Table)
                        {
                            continue;
                        }

                        foreach (DataRow parentRow in row.GetParentRows(relation, version))
                        {
                            if (cachedRows != null && cachedRows.Contains(parentRow))
                            {
                                continue;
                            }

                            // don't update original version if child row doesn't have an oldRecord.
                            if (foreignVer == DataRowVersion.Original && parentRow._newRecord == parentRow._oldRecord)
                            {
                                continue;
                            }

                            if (parentRow != null && ((parentRow.RowState != DataRowState.Deleted) && (version != DataRowVersion.Original || parentRow._oldRecord != -1)))
                            {
                                // if deleted GetRecordFromVersion will throw
                                // TODO: Possible bug, dc.DataExpression may be null
                                object newValue = dc.DataExpression!.Evaluate(parentRow, foreignVer);
                                SilentlySetValue(parentRow, dc, foreignVer, newValue);
                            }
                        }
                    }

                    // next check child relations
                    for (int j = 0; j < ChildRelations.Count; j++)
                    {
                        DataRelation relation = ChildRelations[j];
                        if (relation.ChildTable != dc.Table)
                        {
                            continue;
                        }

                        foreach (DataRow childRow in row.GetChildRows(relation, version))
                        {
                            // don't update original version if child row doesn't have an oldRecord.
                            if (cachedRows != null && cachedRows.Contains(childRow))
                            {
                                continue;
                            }

                            if (foreignVer == DataRowVersion.Original && childRow._newRecord == childRow._oldRecord)
                            {
                                continue;
                            }

                            if (childRow != null && ((childRow.RowState != DataRowState.Deleted) && (version != DataRowVersion.Original || childRow._oldRecord != -1)))
                            {
                                // if deleted GetRecordFromVersion will throw
                                // TODO: Possible bug, dc.DataExpression may be null
                                object newValue = dc.DataExpression!.Evaluate(childRow, foreignVer);
                                SilentlySetValue(childRow, dc, foreignVer, newValue);
                            }
                        }
                    }
                }
            }
        }
    }
}
