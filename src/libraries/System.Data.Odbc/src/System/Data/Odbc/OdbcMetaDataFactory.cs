// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace System.Data.Odbc
{
    internal sealed class OdbcMetaDataFactory : DbMetaDataFactory
    {
        private readonly struct SchemaFunctionName
        {
            internal SchemaFunctionName(string schemaName, ODBC32.SQL_API odbcFunction)
            {
                _schemaName = schemaName;
                _odbcFunction = odbcFunction;
            }
            internal readonly string _schemaName;
            internal readonly ODBC32.SQL_API _odbcFunction;
        }

        private const string _collectionName = "CollectionName";
        private const string _populationMechanism = "PopulationMechanism";
        private const string _prepareCollection = "PrepareCollection";

        private readonly SchemaFunctionName[] _schemaMapping;

        internal static readonly char[] KeywordSeparatorChar = new char[1] { ',' };


        internal OdbcMetaDataFactory(Stream XMLStream,
                                   string serverVersion,
                                   string serverVersionNormalized,
                                   OdbcConnection connection) :
            base(XMLStream, serverVersion, serverVersionNormalized)
        {
            // set up the colletion name ODBC function mapping guid mapping
            _schemaMapping = new SchemaFunctionName[] {
                new SchemaFunctionName(DbMetaDataCollectionNames.DataTypes, ODBC32.SQL_API.SQLGETTYPEINFO),
                new SchemaFunctionName(OdbcMetaDataCollectionNames.Columns, ODBC32.SQL_API.SQLCOLUMNS),
                new SchemaFunctionName(OdbcMetaDataCollectionNames.Indexes, ODBC32.SQL_API.SQLSTATISTICS),
                new SchemaFunctionName(OdbcMetaDataCollectionNames.Procedures, ODBC32.SQL_API.SQLPROCEDURES),
                new SchemaFunctionName(OdbcMetaDataCollectionNames.ProcedureColumns, ODBC32.SQL_API.SQLPROCEDURECOLUMNS),
                new SchemaFunctionName(OdbcMetaDataCollectionNames.ProcedureParameters, ODBC32.SQL_API.SQLPROCEDURECOLUMNS),
                new SchemaFunctionName(OdbcMetaDataCollectionNames.Tables, ODBC32.SQL_API.SQLTABLES),
                new SchemaFunctionName(OdbcMetaDataCollectionNames.Views, ODBC32.SQL_API.SQLTABLES)};

            // verify the existence of the table in the data set
            DataTable? metaDataCollectionsTable = CollectionDataSet.Tables[DbMetaDataCollectionNames.MetaDataCollections];
            if (metaDataCollectionsTable == null)
            {
                throw ADP.UnableToBuildCollection(DbMetaDataCollectionNames.MetaDataCollections);
            }

            // copy the table filtering out any rows that don't apply to the current version of the provider
            metaDataCollectionsTable = CloneAndFilterCollection(DbMetaDataCollectionNames.MetaDataCollections, null);

            // verify the existence of the table in the data set
            DataTable? restrictionsTable = CollectionDataSet.Tables[DbMetaDataCollectionNames.Restrictions];
            if (restrictionsTable != null)
            {
                // copy the table filtering out any rows that don't apply to the current version of the provider
                restrictionsTable = CloneAndFilterCollection(DbMetaDataCollectionNames.Restrictions, null);
            }

            // need to filter out any of the collections where
            // 1) it is populated using prepare collection
            // 2) it is in the collection to odbc function mapping above
            // 3) the provider does not support the necessary odbc function


            DataColumn populationMechanism = metaDataCollectionsTable.Columns[_populationMechanism]!;
            DataColumn collectionName = metaDataCollectionsTable.Columns[_collectionName]!;
            DataColumn? restrictionCollectionName = null;
            if (restrictionsTable != null)
            {
                restrictionCollectionName = restrictionsTable.Columns[_collectionName];
            }
            Debug.Assert(restrictionCollectionName != null);

            foreach (DataRow collection in metaDataCollectionsTable.Rows)
            {
                if ((string)collection[populationMechanism] == _prepareCollection)
                {
                    // is the collection in the mapping
                    int mapping = -1;
                    for (int i = 0; i < _schemaMapping.Length; i++)
                    {
                        if (_schemaMapping[i]._schemaName == (string)collection[collectionName])
                        {
                            mapping = i;
                            break;
                        }
                    }
                    // no go on to the next collection
                    if (mapping == -1)
                    {
                        continue;
                    }

                    // does the provider support the necessary odbc function
                    // if not delete the row from the table
                    if (connection.SQLGetFunctions(_schemaMapping[mapping]._odbcFunction) == false)
                    {
                        // but first delete any related restrictions
                        if (restrictionsTable != null)
                        {
                            foreach (DataRow restriction in restrictionsTable.Rows)
                            {
                                if ((string)collection[collectionName] == (string)restriction[restrictionCollectionName])
                                {
                                    restriction.Delete();
                                }
                            }
                            restrictionsTable.AcceptChanges();
                        }
                        collection.Delete();
                    }
                }
            }

            // replace the original table with the updated one
            metaDataCollectionsTable.AcceptChanges();
            CollectionDataSet.Tables.Remove(CollectionDataSet.Tables[DbMetaDataCollectionNames.MetaDataCollections]!);
            CollectionDataSet.Tables.Add(metaDataCollectionsTable);

            if (restrictionsTable != null)
            {
                CollectionDataSet.Tables.Remove(CollectionDataSet.Tables[DbMetaDataCollectionNames.Restrictions]!);
                CollectionDataSet.Tables.Add(restrictionsTable);
            }
        }

        private static object BooleanFromODBC(object odbcSource)
        {
            if (odbcSource != DBNull.Value)
            {
                //convert to Int32 before doing the comparison
                //some odbc drivers report the odbcSource value as unsigned, in which case we will
                //have upgraded the type to Int32, and thus can't cast directly to short
                if (Convert.ToInt32(odbcSource, null) == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return DBNull.Value;
        }

        private static OdbcCommand GetCommand(OdbcConnection connection)
        {
            OdbcCommand command = connection.CreateCommand();

            // You need to make sure you pick up the transaction from the connection,
            // or odd things can happen...
            command.Transaction = connection.LocalTransaction;
            return command;
        }

        private static DataTable DataTableFromDataReader(OdbcDataReader reader, string tableName)
        {
            // set up the column structure of the data table from the reader
            object[] values;
            DataTable resultTable = NewDataTableFromReader(reader, out values, tableName);

            // populate the data table from the data reader
            while (reader.Read())
            {
                reader.GetValues(values);
                resultTable.Rows.Add(values);
            }
            return resultTable;
        }

        private static void DataTableFromDataReaderDataTypes(DataTable dataTypesTable, OdbcDataReader dataReader, OdbcConnection connection)
        {
            DataTable? schemaTable;
            //

            // Build a DataTable from the reader
            schemaTable = dataReader.GetSchemaTable();

            // vstfdevdiv:479715 Handle cases where reader is empty
            if (null == schemaTable)
            {
                throw ADP.OdbcNoTypesFromProvider();
            }

            object[] getTypeInfoValues = new object[schemaTable.Rows.Count];
            DataRow dataTypesRow;

            DataColumn typeNameColumn = dataTypesTable.Columns[DbMetaDataColumnNames.TypeName]!;
            DataColumn providerDbTypeColumn = dataTypesTable.Columns[DbMetaDataColumnNames.ProviderDbType]!;
            DataColumn columnSizeColumn = dataTypesTable.Columns[DbMetaDataColumnNames.ColumnSize]!;
            DataColumn createParametersColumn = dataTypesTable.Columns[DbMetaDataColumnNames.CreateParameters]!;
            DataColumn dataTypeColumn = dataTypesTable.Columns[DbMetaDataColumnNames.DataType]!;
            DataColumn isAutoIncermentableColumn = dataTypesTable.Columns[DbMetaDataColumnNames.IsAutoIncrementable]!;
            DataColumn isCaseSensitiveColumn = dataTypesTable.Columns[DbMetaDataColumnNames.IsCaseSensitive]!;
            DataColumn isFixedLengthColumn = dataTypesTable.Columns[DbMetaDataColumnNames.IsFixedLength]!;
            DataColumn isFixedPrecisionScaleColumn = dataTypesTable.Columns[DbMetaDataColumnNames.IsFixedPrecisionScale]!;
            DataColumn isLongColumn = dataTypesTable.Columns[DbMetaDataColumnNames.IsLong]!;
            DataColumn isNullableColumn = dataTypesTable.Columns[DbMetaDataColumnNames.IsNullable]!;
            DataColumn isSearchableColumn = dataTypesTable.Columns[DbMetaDataColumnNames.IsSearchable]!;
            DataColumn isSearchableWithLikeColumn = dataTypesTable.Columns[DbMetaDataColumnNames.IsSearchableWithLike]!;
            DataColumn isUnsignedColumn = dataTypesTable.Columns[DbMetaDataColumnNames.IsUnsigned]!;
            DataColumn maximumScaleColumn = dataTypesTable.Columns[DbMetaDataColumnNames.MaximumScale]!;
            DataColumn minimumScaleColumn = dataTypesTable.Columns[DbMetaDataColumnNames.MinimumScale]!;
            DataColumn literalPrefixColumn = dataTypesTable.Columns[DbMetaDataColumnNames.LiteralPrefix]!;
            DataColumn literalSuffixColumn = dataTypesTable.Columns[DbMetaDataColumnNames.LiteralSuffix]!;
            DataColumn SQLTypeNameColumn = dataTypesTable.Columns[OdbcMetaDataColumnNames.SQLType]!;


            const int indexTYPE_NAME = 0;
            const int indexDATA_TYPE = 1;
            const int indexCOLUMN_SIZE = 2;
            const int indexCREATE_PARAMS = 5;
            const int indexAUTO_UNIQUE_VALUE = 11;
            const int indexCASE_SENSITIVE = 7;
            const int indexFIXED_PREC_SCALE = 10;
            const int indexNULLABLE = 6;
            const int indexSEARCHABLE = 8;
            const int indexUNSIGNED_ATTRIBUTE = 9;
            const int indexMAXIMUM_SCALE = 14;
            const int indexMINIMUM_SCALE = 13;
            const int indexLITERAL_PREFIX = 3;
            const int indexLITERAL_SUFFIX = 4;

            const int SQL_DATE_V2 = 9;
            const int SQL_TIME_V2 = 10;

            TypeMap? typeMap;


            while (dataReader.Read())
            {
                dataReader.GetValues(getTypeInfoValues);
                dataTypesRow = dataTypesTable.NewRow();

                ODBC32.SQL_TYPE sqlType;

                dataTypesRow[typeNameColumn] = getTypeInfoValues[indexTYPE_NAME];
                dataTypesRow[SQLTypeNameColumn] = getTypeInfoValues[indexDATA_TYPE];

                sqlType = (ODBC32.SQL_TYPE)(int)Convert.ChangeType(getTypeInfoValues[indexDATA_TYPE], typeof(int), null);
                // if the driver is pre version 3 and it returned the v2 SQL_DATE or SQL_TIME types they need
                // to be mapped to their v3 equivalent
                if (connection.IsV3Driver == false)
                {
                    if ((int)sqlType == SQL_DATE_V2)
                    {
                        sqlType = ODBC32.SQL_TYPE.TYPE_DATE;
                    }
                    else if ((int)sqlType == SQL_TIME_V2)
                    {
                        sqlType = ODBC32.SQL_TYPE.TYPE_TIME;
                    }
                }
                try
                {
                    typeMap = TypeMap.FromSqlType(sqlType);
                }
                // FromSqlType will throw an argument exception if it does not recognize the SqlType.
                // This is not an error since the GetTypeInfo DATA_TYPE may be a SQL data type or a driver specific
                // type. If there is no TypeMap for the type its not an error but it will degrade our level of
                // understanding of/ support for the type.
                catch (ArgumentException)
                {
                    typeMap = null;
                }

                // if we have a type map we can determine the dbType and the CLR type if not leave them null
                if (typeMap != null)
                {
                    dataTypesRow[providerDbTypeColumn] = typeMap._odbcType;
                    dataTypesRow[dataTypeColumn] = typeMap._type.FullName;
                    // setting isLong and isFixedLength only if we have a type map because for provider
                    // specific types we have no idea what the types attributes are if GetTypeInfo did not
                    // tell us
                    switch (sqlType)
                    {
                        case ODBC32.SQL_TYPE.LONGVARCHAR:
                        case ODBC32.SQL_TYPE.WLONGVARCHAR:
                        case ODBC32.SQL_TYPE.LONGVARBINARY:
                        case ODBC32.SQL_TYPE.SS_XML:
                            dataTypesRow[isLongColumn] = true;
                            dataTypesRow[isFixedLengthColumn] = false;
                            break;

                        case ODBC32.SQL_TYPE.VARCHAR:
                        case ODBC32.SQL_TYPE.WVARCHAR:
                        case ODBC32.SQL_TYPE.VARBINARY:
                            dataTypesRow[isLongColumn] = false;
                            dataTypesRow[isFixedLengthColumn] = false;
                            break;

                        case ODBC32.SQL_TYPE.CHAR:
                        case ODBC32.SQL_TYPE.WCHAR:
                        case ODBC32.SQL_TYPE.DECIMAL:
                        case ODBC32.SQL_TYPE.NUMERIC:
                        case ODBC32.SQL_TYPE.SMALLINT:
                        case ODBC32.SQL_TYPE.INTEGER:
                        case ODBC32.SQL_TYPE.REAL:
                        case ODBC32.SQL_TYPE.FLOAT:
                        case ODBC32.SQL_TYPE.DOUBLE:
                        case ODBC32.SQL_TYPE.BIT:
                        case ODBC32.SQL_TYPE.TINYINT:
                        case ODBC32.SQL_TYPE.BIGINT:
                        case ODBC32.SQL_TYPE.TYPE_DATE:
                        case ODBC32.SQL_TYPE.TYPE_TIME:
                        case ODBC32.SQL_TYPE.TIMESTAMP:
                        case ODBC32.SQL_TYPE.TYPE_TIMESTAMP:
                        case ODBC32.SQL_TYPE.GUID:
                        case ODBC32.SQL_TYPE.SS_VARIANT:
                        case ODBC32.SQL_TYPE.SS_UTCDATETIME:
                        case ODBC32.SQL_TYPE.SS_TIME_EX:
                        case ODBC32.SQL_TYPE.BINARY:
                            dataTypesRow[isLongColumn] = false;
                            dataTypesRow[isFixedLengthColumn] = true;
                            break;

                        case ODBC32.SQL_TYPE.SS_UDT:
                        default:
                            // for User defined types don't know if its long or if it is
                            // variable length or not so leave the fields null
                            break;
                    }
                }



                dataTypesRow[columnSizeColumn] = getTypeInfoValues[indexCOLUMN_SIZE];
                dataTypesRow[createParametersColumn] = getTypeInfoValues[indexCREATE_PARAMS];

                if ((getTypeInfoValues[indexAUTO_UNIQUE_VALUE] == DBNull.Value) ||
                    (Convert.ToInt16(getTypeInfoValues[indexAUTO_UNIQUE_VALUE], null) == 0))
                {
                    dataTypesRow[isAutoIncermentableColumn] = false;
                }
                else
                {
                    dataTypesRow[isAutoIncermentableColumn] = true;
                }

                dataTypesRow[isCaseSensitiveColumn] = BooleanFromODBC(getTypeInfoValues[indexCASE_SENSITIVE]);
                dataTypesRow[isFixedPrecisionScaleColumn] = BooleanFromODBC(getTypeInfoValues[indexFIXED_PREC_SCALE]);

                if (getTypeInfoValues[indexNULLABLE] != DBNull.Value)
                {
                    //Use Convert.ToInt16 instead of direct cast to short because the value will be Int32 in some cases
                    switch ((ODBC32.SQL_NULLABILITY)Convert.ToInt16(getTypeInfoValues[indexNULLABLE], null))
                    {
                        case ODBC32.SQL_NULLABILITY.NO_NULLS:
                            dataTypesRow[isNullableColumn] = false;
                            break;

                        case ODBC32.SQL_NULLABILITY.NULLABLE:
                            dataTypesRow[isNullableColumn] = true;
                            break;

                        case ODBC32.SQL_NULLABILITY.UNKNOWN:
                            dataTypesRow[isNullableColumn] = DBNull.Value;
                            break;
                    }
                }

                if (DBNull.Value != getTypeInfoValues[indexSEARCHABLE])
                {
                    //Use Convert.ToInt16 instead of direct cast to short because the value will be Int32 in some cases
                    short searchableValue = Convert.ToInt16(getTypeInfoValues[indexSEARCHABLE], null);
                    switch (searchableValue)
                    {
                        case (short)ODBC32.SQL_SEARCHABLE.UNSEARCHABLE:
                            dataTypesRow[isSearchableColumn] = false;
                            dataTypesRow[isSearchableWithLikeColumn] = false;
                            break;

                        case (short)ODBC32.SQL_SEARCHABLE.LIKE_ONLY:
                            dataTypesRow[isSearchableColumn] = false;
                            dataTypesRow[isSearchableWithLikeColumn] = true;
                            break;

                        case (short)ODBC32.SQL_SEARCHABLE.ALL_EXCEPT_LIKE:
                            dataTypesRow[isSearchableColumn] = true;
                            dataTypesRow[isSearchableWithLikeColumn] = false;
                            break;

                        case (short)ODBC32.SQL_SEARCHABLE.SEARCHABLE:
                            dataTypesRow[isSearchableColumn] = true;
                            dataTypesRow[isSearchableWithLikeColumn] = true;
                            break;
                    }
                }

                dataTypesRow[isUnsignedColumn] = BooleanFromODBC(getTypeInfoValues[indexUNSIGNED_ATTRIBUTE]);

                //For assignment to the DataSet, don't cast the data types -- let the DataSet take care of any conversion
                if (getTypeInfoValues[indexMAXIMUM_SCALE] != DBNull.Value)
                {
                    dataTypesRow[maximumScaleColumn] = getTypeInfoValues[indexMAXIMUM_SCALE];
                }

                if (getTypeInfoValues[indexMINIMUM_SCALE] != DBNull.Value)
                {
                    dataTypesRow[minimumScaleColumn] = getTypeInfoValues[indexMINIMUM_SCALE];
                }

                if (getTypeInfoValues[indexLITERAL_PREFIX] != DBNull.Value)
                {
                    dataTypesRow[literalPrefixColumn] = getTypeInfoValues[indexLITERAL_PREFIX];
                }

                if (getTypeInfoValues[indexLITERAL_SUFFIX] != DBNull.Value)
                {
                    dataTypesRow[literalSuffixColumn] = getTypeInfoValues[indexLITERAL_SUFFIX];
                }

                dataTypesTable.Rows.Add(dataTypesRow);
            }
        }

        private static DataTable DataTableFromDataReaderIndex(OdbcDataReader reader,
                                                       string tableName,
                                                       string? restrictionIndexName)
        {
            // set up the column structure of the data table from the reader
            object[] values;
            DataTable resultTable = NewDataTableFromReader(reader, out values, tableName);

            // populate the data table from the data reader
            int positionOfType = 6;
            int positionOfIndexName = 5;
            while (reader.Read())
            {
                reader.GetValues(values);
                if (IncludeIndexRow(values[positionOfIndexName],
                                    restrictionIndexName,
                                    Convert.ToInt16(values[positionOfType], null)))
                {
                    resultTable.Rows.Add(values);
                }
            }
            return resultTable;
        }

        private static DataTable DataTableFromDataReaderProcedureColumns(OdbcDataReader reader, string tableName, bool isColumn)
        {
            // set up the column structure of the data table from the reader
            object[] values;
            DataTable resultTable = NewDataTableFromReader(reader, out values, tableName);

            // populate the data table from the data reader
            int positionOfColumnType = 4;
            while (reader.Read())
            {
                reader.GetValues(values);
                // the column type should always be short but need to check just in case
                if (values[positionOfColumnType].GetType() == typeof(short))
                {
                    if ((((short)values[positionOfColumnType] == ODBC32.SQL_RESULT_COL) && isColumn) ||
                        (((short)values[positionOfColumnType] != ODBC32.SQL_RESULT_COL) && !isColumn))
                    {
                        resultTable.Rows.Add(values);
                    }
                }
            }
            return resultTable;
        }

        private static DataTable DataTableFromDataReaderProcedures(OdbcDataReader reader, string tableName, short procedureType)
        {
            // Build a DataTable from the reader

            // set up the column structure of the data table from the reader
            object[] values;
            DataTable resultTable = NewDataTableFromReader(reader, out values, tableName);

            // populate the data table from the data reader
            int positionOfProcedureType = 7;
            while (reader.Read())
            {
                reader.GetValues(values);
                // the column type should always be short but need to check just in case its null
                if (values[positionOfProcedureType].GetType() == typeof(short))
                {
                    if ((short)values[positionOfProcedureType] == procedureType)
                    {
                        resultTable.Rows.Add(values);
                    }
                }
            }
            return resultTable;
        }

        private static void FillOutRestrictions(int restrictionsCount, string?[]? restrictions, object?[] allRestrictions, string collectionName)
        {
            Debug.Assert(allRestrictions.Length >= restrictionsCount);

            int i = 0;

            // if we have restrictions put them in the restrictions array
            if (restrictions != null)
            {
                if (restrictions.Length > restrictionsCount)
                {
                    throw ADP.TooManyRestrictions(collectionName);
                }

                for (i = 0; i < restrictions.Length; i++)
                {
                    if (restrictions[i] != null)
                    {
                        allRestrictions[i] = restrictions[i];
                    }
                }
            }

            // initialize the rest to no restrictions
            for (; i < restrictionsCount; i++)
            {
                allRestrictions[i] = null;
            }
        }


        private static DataTable GetColumnsCollection(string?[]? restrictions, OdbcConnection connection)
        {
            OdbcCommand? command = null;
            OdbcDataReader? dataReader = null;
            DataTable? resultTable = null;
            const int columnsRestrictionsCount = 4;

            try
            {
                command = GetCommand(connection);
                string[] allRestrictions = new string[columnsRestrictionsCount];
                FillOutRestrictions(columnsRestrictionsCount, restrictions, allRestrictions, OdbcMetaDataCollectionNames.Columns);

                dataReader = command.ExecuteReaderFromSQLMethod(allRestrictions, ODBC32.SQL_API.SQLCOLUMNS);

                resultTable = DataTableFromDataReader(dataReader, OdbcMetaDataCollectionNames.Columns);
            }

            finally
            {
                dataReader?.Dispose();;
                command?.Dispose();;
            }
            return resultTable;
        }


        private DataTable GetDataSourceInformationCollection(string?[]? restrictions,
                                                             OdbcConnection connection)
        {
            if (ADP.IsEmptyArray(restrictions) == false)
            {
                throw ADP.TooManyRestrictions(DbMetaDataCollectionNames.DataSourceInformation);
            }

            // verify that the data source information table is in the data set
            DataTable? dataSourceInformationTable = CollectionDataSet.Tables[DbMetaDataCollectionNames.DataSourceInformation];
            if (dataSourceInformationTable == null)
            {
                throw ADP.UnableToBuildCollection(DbMetaDataCollectionNames.DataSourceInformation);
            }

            // copy the table filtering out any rows that don't apply to the current version of the provider
            dataSourceInformationTable = CloneAndFilterCollection(DbMetaDataCollectionNames.DataSourceInformation, null);

            // after filtering there better be just one row
            if (dataSourceInformationTable.Rows.Count != 1)
            {
                throw ADP.IncorrectNumberOfDataSourceInformationRows();
            }
            DataRow dataSourceInformation = dataSourceInformationTable.Rows[0];

            string? stringValue;
            short int16Value;
            int int32Value;
            ODBC32.SQLRETURN retcode;

            // update the catalog separator
            stringValue = connection.GetInfoStringUnhandled(ODBC32.SQL_INFO.CATALOG_NAME_SEPARATOR);
            if (!string.IsNullOrEmpty(stringValue))
            {
                StringBuilder patternEscaped = new StringBuilder();
                ADP.EscapeSpecialCharacters(stringValue, patternEscaped);
                dataSourceInformation[DbMetaDataColumnNames.CompositeIdentifierSeparatorPattern] = patternEscaped.ToString();
            }

            // get the DBMS Name
            stringValue = connection.GetInfoStringUnhandled(ODBC32.SQL_INFO.DBMS_NAME);
            if (stringValue != null)
            {
                dataSourceInformation[DbMetaDataColumnNames.DataSourceProductName] = stringValue;
            }


            // update the server version strings
            dataSourceInformation[DbMetaDataColumnNames.DataSourceProductVersion] = ServerVersion;
            dataSourceInformation[DbMetaDataColumnNames.DataSourceProductVersionNormalized] = ServerVersionNormalized;


            // values that are the same for all ODBC drivers. See bug 105333
            dataSourceInformation[DbMetaDataColumnNames.ParameterMarkerFormat] = "?";
            dataSourceInformation[DbMetaDataColumnNames.ParameterMarkerPattern] = "\\?";
            dataSourceInformation[DbMetaDataColumnNames.ParameterNameMaxLength] = 0;

            // determine the supportedJoinOperators
            // leave the column null if the GetInfo fails. There is no explicit value for
            // unknown.
            if (connection.IsV3Driver)
            {
                retcode = connection.GetInfoInt32Unhandled(ODBC32.SQL_INFO.SQL_OJ_CAPABILITIES_30, out int32Value);
            }
            else
            {
                retcode = connection.GetInfoInt32Unhandled(ODBC32.SQL_INFO.SQL_OJ_CAPABILITIES_20, out int32Value);
            }

            if ((retcode == ODBC32.SQLRETURN.SUCCESS) || (retcode == ODBC32.SQLRETURN.SUCCESS_WITH_INFO))
            {
                Common.SupportedJoinOperators supportedJoinOperators = Common.SupportedJoinOperators.None;
                if ((int32Value & (int)ODBC32.SQL_OJ_CAPABILITIES.LEFT) != 0)
                {
                    supportedJoinOperators |= Common.SupportedJoinOperators.LeftOuter;
                }
                if ((int32Value & (int)ODBC32.SQL_OJ_CAPABILITIES.RIGHT) != 0)
                {
                    supportedJoinOperators |= Common.SupportedJoinOperators.RightOuter;
                }
                if ((int32Value & (int)ODBC32.SQL_OJ_CAPABILITIES.FULL) != 0)
                {
                    supportedJoinOperators |= Common.SupportedJoinOperators.FullOuter;
                }
                if ((int32Value & (int)ODBC32.SQL_OJ_CAPABILITIES.INNER) != 0)
                {
                    supportedJoinOperators |= Common.SupportedJoinOperators.Inner;
                }

                dataSourceInformation[DbMetaDataColumnNames.SupportedJoinOperators] = supportedJoinOperators;
            }

            // determine the GroupByBehavior
            retcode = connection.GetInfoInt16Unhandled(ODBC32.SQL_INFO.GROUP_BY, out int16Value);
            Common.GroupByBehavior groupByBehavior = Common.GroupByBehavior.Unknown;

            if ((retcode == ODBC32.SQLRETURN.SUCCESS) || (retcode == ODBC32.SQLRETURN.SUCCESS_WITH_INFO))
            {
                switch (int16Value)
                {
                    case (short)ODBC32.SQL_GROUP_BY.NOT_SUPPORTED:
                        groupByBehavior = Common.GroupByBehavior.NotSupported;
                        break;

                    case (short)ODBC32.SQL_GROUP_BY.GROUP_BY_EQUALS_SELECT:
                        groupByBehavior = Common.GroupByBehavior.ExactMatch;
                        break;

                    case (short)ODBC32.SQL_GROUP_BY.GROUP_BY_CONTAINS_SELECT:
                        groupByBehavior = Common.GroupByBehavior.MustContainAll;
                        break;

                    case (short)ODBC32.SQL_GROUP_BY.NO_RELATION:
                        groupByBehavior = Common.GroupByBehavior.Unrelated;
                        break;
                        /* COLLATE is new in ODBC 3.0 and GroupByBehavior does not have a value for it.
                                            case ODBC32.SQL_GROUP_BY.COLLATE:
                                                groupByBehavior = Common.GroupByBehavior.Unknown;
                                                break;
                        */
                }
            }

            dataSourceInformation[DbMetaDataColumnNames.GroupByBehavior] = groupByBehavior;

            // determine the identifier case
            retcode = connection.GetInfoInt16Unhandled(ODBC32.SQL_INFO.IDENTIFIER_CASE, out int16Value);
            Common.IdentifierCase identifierCase = Common.IdentifierCase.Unknown;

            if ((retcode == ODBC32.SQLRETURN.SUCCESS) || (retcode == ODBC32.SQLRETURN.SUCCESS_WITH_INFO))
            {
                switch (int16Value)
                {
                    case (short)ODBC32.SQL_IDENTIFIER_CASE.SENSITIVE:
                        identifierCase = Common.IdentifierCase.Sensitive;
                        break;

                    case (short)ODBC32.SQL_IDENTIFIER_CASE.UPPER:
                    case (short)ODBC32.SQL_IDENTIFIER_CASE.LOWER:
                    case (short)ODBC32.SQL_IDENTIFIER_CASE.MIXED:
                        identifierCase = Common.IdentifierCase.Insensitive;
                        break;
                }
            }
            dataSourceInformation[DbMetaDataColumnNames.IdentifierCase] = identifierCase;

            // OrderByColumnsInSelect
            stringValue = connection.GetInfoStringUnhandled(ODBC32.SQL_INFO.ORDER_BY_COLUMNS_IN_SELECT);
            if (stringValue != null)
            {
                if (stringValue == "Y")
                {
                    dataSourceInformation[DbMetaDataColumnNames.OrderByColumnsInSelect] = true;
                }
                else if (stringValue == "N")
                {
                    dataSourceInformation[DbMetaDataColumnNames.OrderByColumnsInSelect] = false;
                }
            }

            // build the QuotedIdentifierPattern using the quote prefix and suffix from the provider and
            // assuming that the quote suffix is escaped via repetition (i.e " becomes "")
            stringValue = connection.QuoteChar(ADP.GetSchema);

            if (stringValue != null)
            {
                // by spec a blank identifier quote char indicates that the provider does not suppport
                // quoted identifiers
                if (stringValue != " ")
                {
                    // only know how to build the pattern if the quote characters is 1 character
                    // in all other cases just leave the field null
                    if (stringValue.Length == 1)
                    {
                        StringBuilder scratchStringBuilder = new StringBuilder();
                        ADP.EscapeSpecialCharacters(stringValue, scratchStringBuilder);
                        string escapedQuoteSuffixString = scratchStringBuilder.ToString();
                        scratchStringBuilder.Length = 0;

                        ADP.EscapeSpecialCharacters(stringValue, scratchStringBuilder);
                        scratchStringBuilder.Append("(([^");
                        scratchStringBuilder.Append(escapedQuoteSuffixString);
                        scratchStringBuilder.Append("]|");
                        scratchStringBuilder.Append(escapedQuoteSuffixString);
                        scratchStringBuilder.Append(escapedQuoteSuffixString);
                        scratchStringBuilder.Append(")*)");
                        scratchStringBuilder.Append(escapedQuoteSuffixString);
                        dataSourceInformation[DbMetaDataColumnNames.QuotedIdentifierPattern] = scratchStringBuilder.ToString();
                    }
                }
            }

            // determine the quoted identifier case
            retcode = connection.GetInfoInt16Unhandled(ODBC32.SQL_INFO.QUOTED_IDENTIFIER_CASE, out int16Value);
            Common.IdentifierCase quotedIdentifierCase = Common.IdentifierCase.Unknown;

            if ((retcode == ODBC32.SQLRETURN.SUCCESS) || (retcode == ODBC32.SQLRETURN.SUCCESS_WITH_INFO))
            {
                switch (int16Value)
                {
                    case (short)ODBC32.SQL_IDENTIFIER_CASE.SENSITIVE:
                        quotedIdentifierCase = Common.IdentifierCase.Sensitive;
                        break;

                    case (short)ODBC32.SQL_IDENTIFIER_CASE.UPPER:
                    case (short)ODBC32.SQL_IDENTIFIER_CASE.LOWER:
                    case (short)ODBC32.SQL_IDENTIFIER_CASE.MIXED:
                        quotedIdentifierCase = Common.IdentifierCase.Insensitive;
                        break;
                }
            }
            dataSourceInformation[DbMetaDataColumnNames.QuotedIdentifierCase] = quotedIdentifierCase;

            dataSourceInformationTable.AcceptChanges();

            return dataSourceInformationTable;
        }


        private DataTable GetDataTypesCollection(string?[]? restrictions, OdbcConnection connection)
        {
            if (ADP.IsEmptyArray(restrictions) == false)
            {
                throw ADP.TooManyRestrictions(DbMetaDataCollectionNames.DataTypes);
            }



            // verify the existence of the table in the data set
            DataTable? dataTypesTable = CollectionDataSet.Tables[DbMetaDataCollectionNames.DataTypes];
            if (dataTypesTable == null)
            {
                throw ADP.UnableToBuildCollection(DbMetaDataCollectionNames.DataTypes);
            }

            // copy the data table it
            dataTypesTable = CloneAndFilterCollection(DbMetaDataCollectionNames.DataTypes, null);

            OdbcCommand? command = null;
            OdbcDataReader? dataReader = null;
            object[] allArguments = new object[1];
            allArguments[0] = ODBC32.SQL_ALL_TYPES;

            try
            {
                command = GetCommand(connection);


                dataReader = command.ExecuteReaderFromSQLMethod(allArguments, ODBC32.SQL_API.SQLGETTYPEINFO);

                DataTableFromDataReaderDataTypes(dataTypesTable, dataReader, connection);
            }

            finally
            {
                dataReader?.Dispose();;
                command?.Dispose();;
            }
            dataTypesTable.AcceptChanges();
            return dataTypesTable;
        }

        private static DataTable GetIndexCollection(string?[]? restrictions, OdbcConnection connection)
        {
            OdbcCommand? command = null;
            OdbcDataReader? dataReader = null;
            DataTable? resultTable = null;
            const int nativeRestrictionsCount = 5;
            const int indexRestrictionsCount = 4;
            const int indexOfTableName = 2;
            const int indexOfIndexName = 3;

            try
            {
                command = GetCommand(connection);
                object[] allRestrictions = new object[nativeRestrictionsCount];
                FillOutRestrictions(indexRestrictionsCount, restrictions, allRestrictions, OdbcMetaDataCollectionNames.Indexes);

                if (allRestrictions[indexOfTableName] == null)
                {
                    throw ODBC.GetSchemaRestrictionRequired();
                }

                allRestrictions[3] = (short)ODBC32.SQL_INDEX.ALL;
                allRestrictions[4] = (short)ODBC32.SQL_STATISTICS_RESERVED.ENSURE;

                dataReader = command.ExecuteReaderFromSQLMethod(allRestrictions, ODBC32.SQL_API.SQLSTATISTICS);

                string? indexName = null;
                if (restrictions != null)
                {
                    if (restrictions.Length >= indexOfIndexName + 1)
                    {
                        indexName = restrictions[indexOfIndexName];
                    }
                }

                resultTable = DataTableFromDataReaderIndex(dataReader,
                                                           OdbcMetaDataCollectionNames.Indexes,
                                                           indexName);
            }

            finally
            {
                dataReader?.Dispose();;
                command?.Dispose();;
            }
            return resultTable;
        }

        private static DataTable GetProcedureColumnsCollection(string?[]? restrictions, OdbcConnection connection, bool isColumns)
        {
            OdbcCommand? command = null;
            OdbcDataReader? dataReader = null;
            DataTable? resultTable = null;
            const int procedureColumnsRestrictionsCount = 4;

            try
            {
                command = GetCommand(connection);
                string[] allRestrictions = new string[procedureColumnsRestrictionsCount];
                FillOutRestrictions(procedureColumnsRestrictionsCount, restrictions, allRestrictions, OdbcMetaDataCollectionNames.Columns);

                dataReader = command.ExecuteReaderFromSQLMethod(allRestrictions, ODBC32.SQL_API.SQLPROCEDURECOLUMNS);

                string collectionName;
                if (isColumns)
                {
                    collectionName = OdbcMetaDataCollectionNames.ProcedureColumns;
                }
                else
                {
                    collectionName = OdbcMetaDataCollectionNames.ProcedureParameters;
                }
                resultTable = DataTableFromDataReaderProcedureColumns(dataReader,
                                                                      collectionName,
                                                                      isColumns);
            }

            finally
            {
                dataReader?.Dispose();;
                command?.Dispose();;
            }
            return resultTable;
        }

        private static DataTable GetProceduresCollection(string?[]? restrictions, OdbcConnection connection)
        {
            OdbcCommand? command = null;
            OdbcDataReader? dataReader = null;
            DataTable? resultTable = null;
            const int columnsRestrictionsCount = 4;
            const int indexOfProcedureType = 3;

            try
            {
                command = GetCommand(connection);
                string?[] allRestrictions = new string[columnsRestrictionsCount];
                FillOutRestrictions(columnsRestrictionsCount, restrictions, allRestrictions, OdbcMetaDataCollectionNames.Procedures);


                dataReader = command.ExecuteReaderFromSQLMethod(allRestrictions, ODBC32.SQL_API.SQLPROCEDURES);

                if (allRestrictions[indexOfProcedureType] == null)
                {
                    resultTable = DataTableFromDataReader(dataReader, OdbcMetaDataCollectionNames.Procedures);
                }
                else
                {
                    short procedureType;
                    if ((restrictions![indexOfProcedureType] == "SQL_PT_UNKNOWN") ||
                            (restrictions[indexOfProcedureType] == "0" /*ODBC32.SQL_PROCEDURETYPE.UNKNOWN*/))
                    {
                        procedureType = (short)ODBC32.SQL_PROCEDURETYPE.UNKNOWN;
                    }
                    else if ((restrictions[indexOfProcedureType] == "SQL_PT_PROCEDURE") ||
                             (restrictions[indexOfProcedureType] == "1" /*ODBC32.SQL_PROCEDURETYPE.PROCEDURE*/))
                    {
                        procedureType = (short)ODBC32.SQL_PROCEDURETYPE.PROCEDURE;
                    }
                    else if ((restrictions[indexOfProcedureType] == "SQL_PT_FUNCTION") ||
                             (restrictions[indexOfProcedureType] == "2" /*ODBC32.SQL_PROCEDURETYPE.FUNCTION*/))
                    {
                        procedureType = (short)ODBC32.SQL_PROCEDURETYPE.FUNCTION;
                    }
                    else
                    {
                        throw ADP.InvalidRestrictionValue(OdbcMetaDataCollectionNames.Procedures, "PROCEDURE_TYPE", restrictions[indexOfProcedureType]);
                    }

                    resultTable = DataTableFromDataReaderProcedures(dataReader, OdbcMetaDataCollectionNames.Procedures, procedureType);
                }
            }

            finally
            {
                dataReader?.Dispose();;
                command?.Dispose();;
            }
            return resultTable;
        }

        private DataTable GetReservedWordsCollection(string?[]? restrictions, OdbcConnection connection)
        {
            if (ADP.IsEmptyArray(restrictions) == false)
            {
                throw ADP.TooManyRestrictions(DbMetaDataCollectionNames.ReservedWords);
            }

            // verify the existence of the table in the data set
            DataTable? reservedWordsTable = CollectionDataSet.Tables[DbMetaDataCollectionNames.ReservedWords];
            if (reservedWordsTable == null)
            {
                throw ADP.UnableToBuildCollection(DbMetaDataCollectionNames.ReservedWords);
            }

            // copy the table filtering out any rows that don't apply to tho current version of the prrovider
            reservedWordsTable = CloneAndFilterCollection(DbMetaDataCollectionNames.ReservedWords, null);

            DataColumn? reservedWordColumn = reservedWordsTable.Columns[DbMetaDataColumnNames.ReservedWord];
            if (reservedWordColumn == null)
            {
                throw ADP.UnableToBuildCollection(DbMetaDataCollectionNames.ReservedWords);
            }

            string? keywords = connection.GetInfoStringUnhandled(ODBC32.SQL_INFO.KEYWORDS);

            if (null != keywords)
            {
                string[] values = keywords.Split(KeywordSeparatorChar);
                for (int i = 0; i < values.Length; ++i)
                {
                    DataRow row = reservedWordsTable.NewRow();
                    row[reservedWordColumn] = values[i];

                    reservedWordsTable.Rows.Add(row);
                    row.AcceptChanges();
                }
            }

            return reservedWordsTable;
        }

        private static DataTable GetTablesCollection(string?[]? restrictions, OdbcConnection connection, bool isTables)
        {
            OdbcCommand? command = null;
            OdbcDataReader? dataReader = null;
            DataTable? resultTable = null;
            const int tablesRestrictionsCount = 3;
            const string includedTableTypesTables = "TABLE,SYSTEM TABLE";
            const string includedTableTypesViews = "VIEW";
            string includedTableTypes;
            string dataTableName;

            try
            {
                //command = (OdbcCommand) connection.CreateCommand();
                command = GetCommand(connection);
                string[] allArguments = new string[tablesRestrictionsCount + 1];
                if (isTables)
                {
                    includedTableTypes = includedTableTypesTables;
                    dataTableName = OdbcMetaDataCollectionNames.Tables;
                }
                else
                {
                    includedTableTypes = includedTableTypesViews;
                    dataTableName = OdbcMetaDataCollectionNames.Views;
                }
                FillOutRestrictions(tablesRestrictionsCount, restrictions, allArguments, dataTableName);

                allArguments[tablesRestrictionsCount] = includedTableTypes;

                dataReader = command.ExecuteReaderFromSQLMethod(allArguments, ODBC32.SQL_API.SQLTABLES);

                resultTable = DataTableFromDataReader(dataReader, dataTableName);
            }

            finally
            {
                dataReader?.Dispose();;
                command?.Dispose();;
            }
            return resultTable;
        }

        private static bool IncludeIndexRow(object rowIndexName,
                                        string? restrictionIndexName,
                                        short rowIndexType)
        {
            // never include table statictics rows
            if (rowIndexType == (short)ODBC32.SQL_STATISTICSTYPE.TABLE_STAT)
            {
                return false;
            }

            if ((restrictionIndexName != null) && (restrictionIndexName != (string)rowIndexName))
            {
                return false;
            }

            return true;
        }

        private static DataTable NewDataTableFromReader(OdbcDataReader reader, out object[] values, string tableName)
        {
            DataTable resultTable = new DataTable(tableName);
            resultTable.Locale = System.Globalization.CultureInfo.InvariantCulture;
            DataTable schemaTable = reader.GetSchemaTable()!;
            foreach (DataRow row in schemaTable.Rows)
            {
                resultTable.Columns.Add(row["ColumnName"] as string, (Type)row["DataType"]);
            }

            values = new object[resultTable.Columns.Count];
            return resultTable;
        }

        protected override DataTable PrepareCollection(string collectionName, string?[]? restrictions, DbConnection connection)
        {
            DataTable? resultTable = null;
            OdbcConnection odbcConnection = (OdbcConnection)connection;

            if (collectionName == OdbcMetaDataCollectionNames.Tables)
            {
                resultTable = GetTablesCollection(restrictions, odbcConnection, true);
            }
            else if (collectionName == OdbcMetaDataCollectionNames.Views)
            {
                resultTable = GetTablesCollection(restrictions, odbcConnection, false);
            }
            else if (collectionName == OdbcMetaDataCollectionNames.Columns)
            {
                resultTable = GetColumnsCollection(restrictions, odbcConnection);
            }
            else if (collectionName == OdbcMetaDataCollectionNames.Procedures)
            {
                resultTable = GetProceduresCollection(restrictions, odbcConnection);
            }
            else if (collectionName == OdbcMetaDataCollectionNames.ProcedureColumns)
            {
                resultTable = GetProcedureColumnsCollection(restrictions, odbcConnection, true);
            }
            else if (collectionName == OdbcMetaDataCollectionNames.ProcedureParameters)
            {
                resultTable = GetProcedureColumnsCollection(restrictions, odbcConnection, false);
            }
            else if (collectionName == OdbcMetaDataCollectionNames.Indexes)
            {
                resultTable = GetIndexCollection(restrictions, odbcConnection);
            }
            else if (collectionName == DbMetaDataCollectionNames.DataTypes)
            {
                resultTable = GetDataTypesCollection(restrictions, odbcConnection);
            }
            else if (collectionName == DbMetaDataCollectionNames.DataSourceInformation)
            {
                resultTable = GetDataSourceInformationCollection(restrictions, odbcConnection);
            }
            else if (collectionName == DbMetaDataCollectionNames.ReservedWords)
            {
                resultTable = GetReservedWordsCollection(restrictions, odbcConnection);
            }

            if (resultTable == null)
            {
                throw ADP.UnableToBuildCollection(collectionName);
            }

            return resultTable;
        }
    }
}
