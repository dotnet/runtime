// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Data.OleDb
{
    internal sealed class OleDbMetaDataFactory : DbMetaDataFactory
    { // V1.2.3300

        private struct SchemaRowsetName
        {
            internal SchemaRowsetName(string schemaName, Guid schemaRowset)
            {
                _schemaName = schemaName;
                _schemaRowset = schemaRowset;
            }
            internal readonly string _schemaName;
            internal readonly Guid _schemaRowset;
        }

        private const string _collectionName = "CollectionName";
        private const string _populationMechanism = "PopulationMechanism";
        private const string _prepareCollection = "PrepareCollection";

        private readonly SchemaRowsetName[] _schemaMapping;

        internal OleDbMetaDataFactory(Stream XMLStream,
                                    string serverVersion,
                                    string serverVersionNormalized,
                                    SchemaSupport[]? schemaSupport) :
            base(XMLStream, serverVersion, serverVersionNormalized)
        {
            // set up the colletion mane schema rowset guid mapping
            _schemaMapping = new SchemaRowsetName[] {
                 new SchemaRowsetName(DbMetaDataCollectionNames.DataTypes, OleDbSchemaGuid.Provider_Types),
                 new SchemaRowsetName(OleDbMetaDataCollectionNames.Catalogs, OleDbSchemaGuid.Catalogs),
                 new SchemaRowsetName(OleDbMetaDataCollectionNames.Collations, OleDbSchemaGuid.Collations),
                 new SchemaRowsetName(OleDbMetaDataCollectionNames.Columns, OleDbSchemaGuid.Columns),
                 new SchemaRowsetName(OleDbMetaDataCollectionNames.Indexes, OleDbSchemaGuid.Indexes),
                 new SchemaRowsetName(OleDbMetaDataCollectionNames.Procedures, OleDbSchemaGuid.Procedures),
                 new SchemaRowsetName(OleDbMetaDataCollectionNames.ProcedureColumns, OleDbSchemaGuid.Procedure_Columns),
                 new SchemaRowsetName(OleDbMetaDataCollectionNames.ProcedureParameters, OleDbSchemaGuid.Procedure_Parameters),
                 new SchemaRowsetName(OleDbMetaDataCollectionNames.Tables, OleDbSchemaGuid.Tables),
                 new SchemaRowsetName(OleDbMetaDataCollectionNames.Views, OleDbSchemaGuid.Views)};

            // verify the existance of the table in the data set
            DataTable? metaDataCollectionsTable = CollectionDataSet.Tables[DbMetaDataCollectionNames.MetaDataCollections];
            if (metaDataCollectionsTable == null)
            {
                throw ADP.UnableToBuildCollection(DbMetaDataCollectionNames.MetaDataCollections);
            }

            // copy the table filtering out any rows that don't apply to the current version of the provider
            metaDataCollectionsTable = CloneAndFilterCollection(DbMetaDataCollectionNames.MetaDataCollections, null);

            // verify the existance of the table in the data set
            DataTable? restrictionsTable = CollectionDataSet.Tables[DbMetaDataCollectionNames.Restrictions];
            if (restrictionsTable != null)
            {
                // copy the table filtering out any rows that don't apply to the current version of the provider
                restrictionsTable = CloneAndFilterCollection(DbMetaDataCollectionNames.Restrictions, null);
            }

            // need to filter out any of the collections where
            // 1) it is populated using prepare collection
            // 2) it is in the collection to schema rowset mapping above
            // 3) the provider does not support the necessary schema rowset

            DataColumn? populationMechanism = metaDataCollectionsTable.Columns[_populationMechanism];
            if ((null == populationMechanism) || (typeof(string) != populationMechanism.DataType))
            {
                throw ADP.InvalidXmlMissingColumn(DbMetaDataCollectionNames.MetaDataCollections, _populationMechanism);
            }
            DataColumn? collectionName = metaDataCollectionsTable.Columns[_collectionName];
            if ((null == collectionName) || (typeof(string) != collectionName.DataType))
            {
                throw ADP.InvalidXmlMissingColumn(DbMetaDataCollectionNames.MetaDataCollections, _collectionName);
            }
            DataColumn? restrictionCollectionName = null;
            if (restrictionsTable != null)
            {
                restrictionCollectionName = restrictionsTable.Columns[_collectionName];
                if ((null == restrictionCollectionName) || (typeof(string) != restrictionCollectionName.DataType))
                {
                    throw ADP.InvalidXmlMissingColumn(DbMetaDataCollectionNames.Restrictions, _collectionName);
                }
            }

            foreach (DataRow collection in metaDataCollectionsTable.Rows)
            {
                string? populationMechanismValue = collection[populationMechanism] as string;
                if (ADP.IsEmpty(populationMechanismValue))
                {
                    throw ADP.InvalidXmlInvalidValue(DbMetaDataCollectionNames.MetaDataCollections, _populationMechanism);
                }
                string? collectionNameValue = collection[collectionName] as string;
                if (ADP.IsEmpty(collectionNameValue))
                {
                    throw ADP.InvalidXmlInvalidValue(DbMetaDataCollectionNames.MetaDataCollections, _collectionName);
                }

                if (populationMechanismValue == _prepareCollection)
                {
                    // is the collection in the mapping
                    int mapping = -1;
                    for (int i = 0; i < _schemaMapping.Length; i++)
                    {
                        if (_schemaMapping[i]._schemaName == collectionNameValue)
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

                    // does the provider support the necessary schema rowset
                    bool isSchemaRowsetSupported = false;
                    if (schemaSupport != null)
                    {
                        for (int i = 0; i < schemaSupport.Length; i++)
                        {
                            if (_schemaMapping[mapping]._schemaRowset == schemaSupport[i]._schemaRowset)
                            {
                                isSchemaRowsetSupported = true;
                                break;
                            }
                        }
                    }
                    // if not delete the row from the table
                    if (isSchemaRowsetSupported == false)
                    {
                        // but first delete any related restrictions
                        if (restrictionsTable != null)
                        {
                            foreach (DataRow restriction in restrictionsTable.Rows)
                            {
                                string? restrictionCollectionNameValue = restriction[restrictionCollectionName!] as string;
                                if (ADP.IsEmpty(restrictionCollectionNameValue))
                                {
                                    throw ADP.InvalidXmlInvalidValue(DbMetaDataCollectionNames.Restrictions, _collectionName);
                                }
                                if (collectionNameValue == restrictionCollectionNameValue)
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

        private static string BuildRegularExpression(string invalidChars, string invalidStartingChars)
        {
            StringBuilder regularExpression = new StringBuilder("[^");
            ADP.EscapeSpecialCharacters(invalidStartingChars, regularExpression);
            regularExpression.Append("][^");
            ADP.EscapeSpecialCharacters(invalidChars, regularExpression);
            regularExpression.Append("]*");

            return regularExpression.ToString();
        }

        private DataTable GetDataSourceInformationTable(OleDbConnection connection, OleDbConnectionInternal internalConnection)
        {
            // verify that the data source information table is in the data set
            DataTable? dataSourceInformationTable = CollectionDataSet.Tables[DbMetaDataCollectionNames.DataSourceInformation];
            if (dataSourceInformationTable == null)
            {
                throw ADP.UnableToBuildCollection(DbMetaDataCollectionNames.DataSourceInformation);
            }

            // copy the table filtering out any rows that don't apply to tho current version of the prrovider
            dataSourceInformationTable = CloneAndFilterCollection(DbMetaDataCollectionNames.DataSourceInformation, null);

            // after filtering there better be just one row
            if (dataSourceInformationTable.Rows.Count != 1)
            {
                throw ADP.IncorrectNumberOfDataSourceInformationRows();
            }
            DataRow dataSourceInformation = dataSourceInformationTable.Rows[0];

            // update the identifier separator
            string? catalogSeparatorPattern = internalConnection.GetLiteralInfo(ODB.DBLITERAL_CATALOG_SEPARATOR);
            string? schemaSeparatorPattern = internalConnection.GetLiteralInfo(ODB.DBLITERAL_SCHEMA_SEPARATOR);

            if (catalogSeparatorPattern != null)
            {
                StringBuilder compositeSeparatorPattern = new StringBuilder();
                StringBuilder patternEscaped = new StringBuilder();
                ADP.EscapeSpecialCharacters(catalogSeparatorPattern, patternEscaped);
                compositeSeparatorPattern.Append(patternEscaped);
                if ((schemaSeparatorPattern != null) && (schemaSeparatorPattern != catalogSeparatorPattern))
                {
                    compositeSeparatorPattern.Append('|');
                    patternEscaped.Length = 0;
                    ADP.EscapeSpecialCharacters(schemaSeparatorPattern, patternEscaped);
                    compositeSeparatorPattern.Append(patternEscaped);
                }
                dataSourceInformation[DbMetaDataColumnNames.CompositeIdentifierSeparatorPattern] = compositeSeparatorPattern.ToString();
            }
            else if (schemaSeparatorPattern != null)
            {
                StringBuilder patternEscaped = new StringBuilder();
                ADP.EscapeSpecialCharacters(schemaSeparatorPattern, patternEscaped);
                dataSourceInformation[DbMetaDataColumnNames.CompositeIdentifierSeparatorPattern] = patternEscaped.ToString();
                ;
            }

            // update the DataSourceProductName
            object? property;
            property = connection.GetDataSourcePropertyValue(OleDbPropertySetGuid.DataSourceInfo, ODB.DBPROP_DBMSNAME);
            if (property != null)
            {
                dataSourceInformation[DbMetaDataColumnNames.DataSourceProductName] = (string)property;
            }

            // update the server version strings
            dataSourceInformation[DbMetaDataColumnNames.DataSourceProductVersion] = ServerVersion;
            dataSourceInformation[DbMetaDataColumnNames.DataSourceProductVersionNormalized] = ServerVersionNormalized;

            // values that are the same for all OLE DB Providers.
            dataSourceInformation[DbMetaDataColumnNames.ParameterMarkerFormat] = "?";
            dataSourceInformation[DbMetaDataColumnNames.ParameterMarkerPattern] = "\\?";
            dataSourceInformation[DbMetaDataColumnNames.ParameterNameMaxLength] = 0;

            property = connection.GetDataSourcePropertyValue(OleDbPropertySetGuid.DataSourceInfo, ODB.DBPROP_GROUPBY);
            GroupByBehavior groupByBehavior = GroupByBehavior.Unknown;
            if (property != null)
            {
                switch ((int)property)
                {
                    case ODB.DBPROPVAL_GB_CONTAINS_SELECT:
                        groupByBehavior = GroupByBehavior.MustContainAll;
                        break;

                    case ODB.DBPROPVAL_GB_EQUALS_SELECT:
                        groupByBehavior = GroupByBehavior.ExactMatch;
                        break;

                    case ODB.DBPROPVAL_GB_NO_RELATION:
                        groupByBehavior = GroupByBehavior.Unrelated;
                        break;

                    case ODB.DBPROPVAL_GB_NOT_SUPPORTED:
                        groupByBehavior = GroupByBehavior.NotSupported;
                        break;
                }
            }
            dataSourceInformation[DbMetaDataColumnNames.GroupByBehavior] = groupByBehavior;

            SetIdentifierCase(DbMetaDataColumnNames.IdentifierCase, ODB.DBPROP_IDENTIFIERCASE, dataSourceInformation, connection);
            SetIdentifierCase(DbMetaDataColumnNames.QuotedIdentifierCase, ODB.DBPROP_QUOTEDIDENTIFIERCASE, dataSourceInformation, connection);

            property = connection.GetDataSourcePropertyValue(OleDbPropertySetGuid.DataSourceInfo, ODB.DBPROP_ORDERBYCOLUNSINSELECT);
            if (property != null)
            {
                dataSourceInformation[DbMetaDataColumnNames.OrderByColumnsInSelect] = (bool)property;
            }

            DataTable? infoLiterals = internalConnection.BuildInfoLiterals();
            if (infoLiterals != null)
            {
                DataRow[] tableNameRow = infoLiterals.Select("Literal = " + ODB.DBLITERAL_TABLE_NAME.ToString(CultureInfo.InvariantCulture));
                if (tableNameRow != null)
                {
                    object invalidCharsObject = tableNameRow[0]["InvalidChars"];
                    if (invalidCharsObject.GetType() == typeof(string))
                    {
                        string invalidChars = (string)invalidCharsObject;
                        object invalidStartingCharsObject = tableNameRow[0]["InvalidStartingChars"];
                        string invalidStartingChars;
                        if (invalidStartingCharsObject.GetType() == typeof(string))
                        {
                            invalidStartingChars = (string)invalidStartingCharsObject;
                        }
                        else
                        {
                            invalidStartingChars = invalidChars;
                        }
                        dataSourceInformation[DbMetaDataColumnNames.IdentifierPattern] =
                                                    BuildRegularExpression(invalidChars, invalidStartingChars);
                    }
                }
            }

            // build the QuotedIdentifierPattern using the quote prefix and suffix from the provider and
            // assuming that the quote suffix is escaped via repetion (i.e " becomes "")
            string quotePrefix;
            string quoteSuffix;
            connection.GetLiteralQuotes(ADP.GetSchema, out quotePrefix, out quoteSuffix);

            if (quotePrefix != null)
            {
                // if the quote suffix is null assume that it is the same as the prefix (See OLEDB spec
                // IDBInfo::GetLiteralInfo DBLITERAL_QUOTE_SUFFIX.)
                if (quoteSuffix == null)
                {
                    quoteSuffix = quotePrefix;
                }

                // only know how to build the parttern if the suffix is 1 character
                // in all other cases just leave the field null
                if (quoteSuffix.Length == 1)
                {
                    StringBuilder scratchStringBuilder = new StringBuilder();
                    ADP.EscapeSpecialCharacters(quoteSuffix, scratchStringBuilder);
                    string escapedQuoteSuffixString = scratchStringBuilder.ToString();
                    scratchStringBuilder.Length = 0;

                    ADP.EscapeSpecialCharacters(quotePrefix, scratchStringBuilder);
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

            dataSourceInformationTable.AcceptChanges();

            return dataSourceInformationTable;
        }

        private DataTable GetDataTypesTable(OleDbConnection connection)
        {
            // verify the existance of the table in the data set
            DataTable? dataTypesTable = CollectionDataSet.Tables[DbMetaDataCollectionNames.DataTypes];
            if (dataTypesTable == null)
            {
                throw ADP.UnableToBuildCollection(DbMetaDataCollectionNames.DataTypes);
            }

            // copy the table filtering out any rows that don't apply to tho current version of the prrovider
            dataTypesTable = CloneAndFilterCollection(DbMetaDataCollectionNames.DataTypes, null);

            DataTable providerTypesTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Provider_Types, null)!;

            DataColumn?[] targetColumns = new DataColumn?[] {
                dataTypesTable.Columns[DbMetaDataColumnNames.TypeName],
                dataTypesTable.Columns[DbMetaDataColumnNames.ColumnSize],
                dataTypesTable.Columns[DbMetaDataColumnNames.CreateParameters],
                dataTypesTable.Columns[DbMetaDataColumnNames.IsAutoIncrementable],
                dataTypesTable.Columns[DbMetaDataColumnNames.IsCaseSensitive],
                dataTypesTable.Columns[DbMetaDataColumnNames.IsFixedLength],
                dataTypesTable.Columns[DbMetaDataColumnNames.IsFixedPrecisionScale],
                dataTypesTable.Columns[DbMetaDataColumnNames.IsLong],
                dataTypesTable.Columns[DbMetaDataColumnNames.IsNullable],
                dataTypesTable.Columns[DbMetaDataColumnNames.IsUnsigned],
                dataTypesTable.Columns[DbMetaDataColumnNames.MaximumScale],
                dataTypesTable.Columns[DbMetaDataColumnNames.MinimumScale],
                dataTypesTable.Columns[DbMetaDataColumnNames.LiteralPrefix],
                dataTypesTable.Columns[DbMetaDataColumnNames.LiteralSuffix],
                dataTypesTable.Columns[OleDbMetaDataColumnNames.NativeDataType]};

            DataColumn?[] sourceColumns = new DataColumn?[] {
                providerTypesTable.Columns["TYPE_NAME"],
                providerTypesTable.Columns["COLUMN_SIZE"],
                providerTypesTable.Columns["CREATE_PARAMS"],
                providerTypesTable.Columns["AUTO_UNIQUE_VALUE"],
                providerTypesTable.Columns["CASE_SENSITIVE"],
                providerTypesTable.Columns["IS_FIXEDLENGTH"],
                providerTypesTable.Columns["FIXED_PREC_SCALE"],
                providerTypesTable.Columns["IS_LONG"],
                providerTypesTable.Columns["IS_NULLABLE"],
                providerTypesTable.Columns["UNSIGNED_ATTRIBUTE"],
                providerTypesTable.Columns["MAXIMUM_SCALE"],
                providerTypesTable.Columns["MINIMUM_SCALE"],
                providerTypesTable.Columns["LITERAL_PREFIX"],
                providerTypesTable.Columns["LITERAL_SUFFIX"],
                providerTypesTable.Columns["DATA_TYPE"]};

            Debug.Assert(sourceColumns.Length == targetColumns.Length);

            DataColumn isSearchable = dataTypesTable.Columns[DbMetaDataColumnNames.IsSearchable]!;
            DataColumn isSearchableWithLike = dataTypesTable.Columns[DbMetaDataColumnNames.IsSearchableWithLike]!;
            DataColumn providerDbType = dataTypesTable.Columns[DbMetaDataColumnNames.ProviderDbType]!;
            DataColumn clrType = dataTypesTable.Columns[DbMetaDataColumnNames.DataType]!;
            DataColumn isLong = dataTypesTable.Columns[DbMetaDataColumnNames.IsLong]!;
            DataColumn isFixed = dataTypesTable.Columns[DbMetaDataColumnNames.IsFixedLength]!;
            DataColumn sourceOleDbType = providerTypesTable.Columns["DATA_TYPE"]!;

            DataColumn searchable = providerTypesTable.Columns["SEARCHABLE"]!;

            foreach (DataRow sourceRow in providerTypesTable.Rows)
            {
                DataRow newRow = dataTypesTable.NewRow();
                for (int i = 0; i < sourceColumns.Length; i++)
                {
                    if ((sourceColumns[i] is DataColumn sourceColumn) && (targetColumns[i] is DataColumn targetColumn))
                    {
                        newRow[targetColumn] = sourceRow[sourceColumn];
                    }
                }

                short nativeDataType = (short)Convert.ChangeType(sourceRow[sourceOleDbType], typeof(short), CultureInfo.InvariantCulture);
                NativeDBType nativeType = NativeDBType.FromDBType(nativeDataType, (bool)newRow[isLong], (bool)newRow[isFixed]);

                newRow[clrType] = nativeType.dataType!.FullName;
                newRow[providerDbType] = nativeType.enumOleDbType;

                // searchable has to be special cased becasue it is not an eaxct mapping
                if ((isSearchable != null) && (isSearchableWithLike != null) && (searchable != null))
                {
                    newRow[isSearchable] = DBNull.Value;
                    newRow[isSearchableWithLike] = DBNull.Value;
                    if (DBNull.Value != sourceRow[searchable])
                    {
                        long searchableValue = (long)(sourceRow[searchable]);
                        switch (searchableValue)
                        {
                            case ODB.DB_UNSEARCHABLE:
                                newRow[isSearchable] = false;
                                newRow[isSearchableWithLike] = false;
                                break;

                            case ODB.DB_LIKE_ONLY:
                                newRow[isSearchable] = false;
                                newRow[isSearchableWithLike] = true;
                                break;

                            case ODB.DB_ALL_EXCEPT_LIKE:
                                newRow[isSearchable] = true;
                                newRow[isSearchableWithLike] = false;
                                break;

                            case ODB.DB_SEARCHABLE:
                                newRow[isSearchable] = true;
                                newRow[isSearchableWithLike] = true;
                                break;
                        }
                    }
                }

                dataTypesTable.Rows.Add(newRow);
            }

            dataTypesTable.AcceptChanges();

            return dataTypesTable;

        }

        private DataTable GetReservedWordsTable(OleDbConnectionInternal internalConnection)
        {
            // verify the existance of the table in the data set
            DataTable? reservedWordsTable = CollectionDataSet.Tables[DbMetaDataCollectionNames.ReservedWords];
            if (null == reservedWordsTable)
            {
                throw ADP.UnableToBuildCollection(DbMetaDataCollectionNames.ReservedWords);
            }

            // copy the table filtering out any rows that don't apply to tho current version of the prrovider
            reservedWordsTable = CloneAndFilterCollection(DbMetaDataCollectionNames.ReservedWords, null);

            DataColumn? reservedWordColumn = reservedWordsTable.Columns[DbMetaDataColumnNames.ReservedWord];
            if (null == reservedWordColumn)
            {
                throw ADP.UnableToBuildCollection(DbMetaDataCollectionNames.ReservedWords);
            }

            if (!internalConnection.AddInfoKeywordsToTable(reservedWordsTable, reservedWordColumn))
            {
                throw ODB.IDBInfoNotSupported();
            }

            return reservedWordsTable;
        }

        protected override DataTable PrepareCollection(string collectionName, string?[]? restrictions, DbConnection connection)
        {
            OleDbConnection oleDbConnection = (OleDbConnection)connection;
            OleDbConnectionInternal oleDbInternalConnection = (OleDbConnectionInternal)(oleDbConnection.InnerConnection);
            DataTable? resultTable = null;
            if (collectionName == DbMetaDataCollectionNames.DataSourceInformation)
            {
                if (ADP.IsEmptyArray(restrictions) == false)
                {
                    throw ADP.TooManyRestrictions(DbMetaDataCollectionNames.DataSourceInformation);
                }
                resultTable = GetDataSourceInformationTable(oleDbConnection, oleDbInternalConnection);
            }
            else if (collectionName == DbMetaDataCollectionNames.DataTypes)
            {
                if (ADP.IsEmptyArray(restrictions) == false)
                {
                    throw ADP.TooManyRestrictions(DbMetaDataCollectionNames.DataTypes);
                }
                resultTable = GetDataTypesTable(oleDbConnection);
            }
            else if (collectionName == DbMetaDataCollectionNames.ReservedWords)
            {
                if (ADP.IsEmptyArray(restrictions) == false)
                {
                    throw ADP.TooManyRestrictions(DbMetaDataCollectionNames.ReservedWords);
                }
                resultTable = GetReservedWordsTable(oleDbInternalConnection);
            }
            else
            {
                for (int i = 0; i < _schemaMapping.Length; i++)
                {
                    if (_schemaMapping[i]._schemaName == collectionName)
                    {
                        // need to special case the oledb schema rowset restrictions on columns that are not
                        // string tpyes
                        object?[]? mungedRestrictions = restrictions;
                        ;
                        if (restrictions != null)
                        {
                            //verify that there are not too many restrictions
                            DataTable metaDataCollectionsTable = CollectionDataSet.Tables[DbMetaDataCollectionNames.MetaDataCollections]!;
                            int numberOfSupportedRestictions = -1;
                            // prepare colletion is called with the exact collection name so
                            // we can do an exact string comparision here
                            foreach (DataRow row in metaDataCollectionsTable.Rows)
                            {
                                string candidateCollectionName = ((string)row[DbMetaDataColumnNames.CollectionName, DataRowVersion.Current]);

                                if (collectionName == candidateCollectionName)
                                {
                                    numberOfSupportedRestictions = (int)row[DbMetaDataColumnNames.NumberOfRestrictions];
                                    if (numberOfSupportedRestictions < restrictions.Length)
                                    {
                                        throw ADP.TooManyRestrictions(collectionName);
                                    }
                                    break;
                                }
                            }

                            Debug.Assert(numberOfSupportedRestictions != -1, "PrepareCollection was called for an collection that is not supported.");

                            // the 4th restrictionon the indexes schema rowset(type) is an I2 - enum
                            const int indexRestrictionTypeSlot = 3;

                            if ((collectionName == OleDbMetaDataCollectionNames.Indexes) &&
                                (restrictions.Length >= indexRestrictionTypeSlot + 1) &&
                                (restrictions[indexRestrictionTypeSlot] != null))
                            {
                                mungedRestrictions = new object[restrictions.Length];
                                for (int j = 0; j < restrictions.Length; j++)
                                {
                                    mungedRestrictions[j] = restrictions[j];
                                }

                                ushort indexTypeValue;

                                if ((restrictions[indexRestrictionTypeSlot] == "DBPROPVAL_IT_BTREE") ||
                                    (restrictions[indexRestrictionTypeSlot] == "1"))
                                {
                                    indexTypeValue = 1;
                                }
                                else if ((restrictions[indexRestrictionTypeSlot] == "DBPROPVAL_IT_HASH") ||
                                    (restrictions[indexRestrictionTypeSlot] == "2"))
                                {
                                    indexTypeValue = 2;
                                }
                                else if ((restrictions[indexRestrictionTypeSlot] == "DBPROPVAL_IT_CONTENT") ||
                                    (restrictions[indexRestrictionTypeSlot] == "3"))
                                {
                                    indexTypeValue = 3;
                                }
                                else if ((restrictions[indexRestrictionTypeSlot] == "DBPROPVAL_IT_OTHER") ||
                                    (restrictions[indexRestrictionTypeSlot] == "4"))
                                {
                                    indexTypeValue = 4;
                                }
                                else
                                {
                                    throw ADP.InvalidRestrictionValue(collectionName, "TYPE", restrictions[indexRestrictionTypeSlot]!);
                                }

                                mungedRestrictions[indexRestrictionTypeSlot] = indexTypeValue;

                            }

                            // the 4th restrictionon the procedures schema rowset(type) is an I2 - enum
                            const int procedureRestrictionTypeSlot = 3;

                            if ((collectionName == OleDbMetaDataCollectionNames.Procedures) &&
                                (restrictions.Length >= procedureRestrictionTypeSlot + 1) &&
                                (restrictions[procedureRestrictionTypeSlot] != null))
                            {
                                mungedRestrictions = new object[restrictions.Length];
                                for (int j = 0; j < restrictions.Length; j++)
                                {
                                    mungedRestrictions[j] = restrictions[j];
                                }

                                short procedureTypeValue;

                                if ((restrictions[procedureRestrictionTypeSlot] == "DB_PT_UNKNOWN") ||
                                    (restrictions[procedureRestrictionTypeSlot] == "1"))
                                {
                                    procedureTypeValue = 1;
                                }
                                else if ((restrictions[procedureRestrictionTypeSlot] == "DB_PT_PROCEDURE") ||
                                    (restrictions[procedureRestrictionTypeSlot] == "2"))
                                {
                                    procedureTypeValue = 2;
                                }
                                else if ((restrictions[procedureRestrictionTypeSlot] == "DB_PT_FUNCTION") ||
                                    (restrictions[procedureRestrictionTypeSlot] == "3"))
                                {
                                    procedureTypeValue = 3;
                                }
                                else
                                {
                                    throw ADP.InvalidRestrictionValue(collectionName, "PROCEDURE_TYPE", restrictions[procedureRestrictionTypeSlot]!);
                                }

                                mungedRestrictions[procedureRestrictionTypeSlot] = procedureTypeValue;

                            }
                        }

                        resultTable = oleDbConnection.GetOleDbSchemaTable((System.Guid)_schemaMapping[i]._schemaRowset, mungedRestrictions);
                        break;
                    }
                }
            }

            if (resultTable == null)
            {
                throw ADP.UnableToBuildCollection(collectionName);
            }

            return resultTable;
        }

        private static void SetIdentifierCase(string columnName, int propertyID, DataRow row, OleDbConnection connection)
        {
            object? property = connection.GetDataSourcePropertyValue(OleDbPropertySetGuid.DataSourceInfo, propertyID);
            IdentifierCase identifierCase = IdentifierCase.Unknown;
            if (property != null)
            {
                int propertyValue = (int)property;
                switch (propertyValue)
                {
                    case ODB.DBPROPVAL_IC_UPPER:
                    case ODB.DBPROPVAL_IC_LOWER:
                    case ODB.DBPROPVAL_IC_MIXED:
                        identifierCase = IdentifierCase.Insensitive;
                        break;

                    case ODB.DBPROPVAL_IC_SENSITIVE:
                        identifierCase = IdentifierCase.Sensitive;
                        break;
                }
            }
            row[columnName] = identifierCase;

        }

    }
}
