// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace System.Data.ProviderBase
{
    internal class DbMetaDataFactory
    { // V1.2.3300

        private DataSet _metaDataCollectionsDataSet;
        private string _normalizedServerVersion;
        private string _serverVersionString;
        // well known column names
        private const string _collectionName = "CollectionName";
        private const string _populationMechanism = "PopulationMechanism";
        private const string _populationString = "PopulationString";
        private const string _maximumVersion = "MaximumVersion";
        private const string _minimumVersion = "MinimumVersion";
        private const string _dataSourceProductVersionNormalized = "DataSourceProductVersionNormalized";
        private const string _dataSourceProductVersion = "DataSourceProductVersion";
        private const string _restrictionNumber = "RestrictionNumber";
        private const string _numberOfRestrictions = "NumberOfRestrictions";
        private const string _restrictionName = "RestrictionName";
        private const string _parameterName = "ParameterName";

        // population mechanisms
        private const string _dataTable = "DataTable";
        private const string _sqlCommand = "SQLCommand";
        private const string _prepareCollection = "PrepareCollection";

        public DbMetaDataFactory(Stream xmlStream, string serverVersion, string normalizedServerVersion)
        {
            ADP.CheckArgumentNull(xmlStream, "xmlStream");
            ADP.CheckArgumentNull(serverVersion, "serverVersion");
            ADP.CheckArgumentNull(normalizedServerVersion, "normalizedServerVersion");

            _metaDataCollectionsDataSet = new DataSet { Locale = CultureInfo.InvariantCulture };
            _metaDataCollectionsDataSet.ReadXml(xmlStream);

            _serverVersionString = serverVersion;
            _normalizedServerVersion = normalizedServerVersion;
        }

        protected DataSet CollectionDataSet
        {
            get
            {
                return _metaDataCollectionsDataSet;
            }
        }

        protected string ServerVersion
        {
            get
            {
                return _serverVersionString;
            }
        }

        protected string ServerVersionNormalized
        {
            get
            {
                return _normalizedServerVersion;
            }
        }

        protected DataTable CloneAndFilterCollection(string collectionName, string[]? hiddenColumnNames)
        {
            DataTable? sourceTable;
            DataTable destinationTable;
            DataColumn[] filteredSourceColumns;
            DataColumnCollection destinationColumns;
            DataRow newRow;

            sourceTable = _metaDataCollectionsDataSet.Tables[collectionName];

            if ((sourceTable == null) || (collectionName != sourceTable.TableName))
            {
                throw ADP.DataTableDoesNotExist(collectionName);
            }

            destinationTable = new DataTable(collectionName);
            destinationTable.Locale = CultureInfo.InvariantCulture;
            destinationColumns = destinationTable.Columns;

            filteredSourceColumns = FilterColumns(sourceTable, hiddenColumnNames, destinationColumns);

            foreach (DataRow row in sourceTable.Rows)
            {
                if (SupportedByCurrentVersion(row) == true)
                {
                    newRow = destinationTable.NewRow();
                    for (int i = 0; i < destinationColumns.Count; i++)
                    {
                        newRow[destinationColumns[i]] = row[filteredSourceColumns[i], DataRowVersion.Current];
                    }
                    destinationTable.Rows.Add(newRow);
                    newRow.AcceptChanges();
                }
            }

            return destinationTable;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _normalizedServerVersion = null!;
                _serverVersionString = null!;
                _metaDataCollectionsDataSet.Dispose();
            }
        }

        private DataTable ExecuteCommand(DataRow requestedCollectionRow, string?[]? restrictions, DbConnection connection)
        {
            DataTable metaDataCollectionsTable = _metaDataCollectionsDataSet.Tables[DbMetaDataCollectionNames.MetaDataCollections]!;
            DataColumn populationStringColumn = metaDataCollectionsTable.Columns[_populationString]!;
            DataColumn numberOfRestrictionsColumn = metaDataCollectionsTable.Columns[_numberOfRestrictions]!;
            DataColumn collectionNameColumn = metaDataCollectionsTable.Columns[_collectionName]!;
            //DataColumn  restrictionNameColumn = metaDataCollectionsTable.Columns[_restrictionName];

            Debug.Assert(requestedCollectionRow != null);
            string sqlCommand = (requestedCollectionRow[populationStringColumn, DataRowVersion.Current] as string)!;
            int numberOfRestrictions = (int)requestedCollectionRow[numberOfRestrictionsColumn, DataRowVersion.Current];
            string collectionName = (requestedCollectionRow[collectionNameColumn, DataRowVersion.Current] as string)!;

            if ((restrictions != null) && (restrictions.Length > numberOfRestrictions))
            {
                throw ADP.TooManyRestrictions(collectionName);
            }

            DbCommand? command = connection.CreateCommand();
            command.CommandText = sqlCommand;
            command.CommandTimeout = System.Math.Max(command.CommandTimeout, 180);

            DataTable? resultTable = null;
            for (int i = 0; i < numberOfRestrictions; i++)
            {
                DbParameter restrictionParameter = command.CreateParameter();

                if ((restrictions != null) && (restrictions.Length > i) && (restrictions[i] != null))
                {
                    restrictionParameter.Value = restrictions[i];
                }
                else
                {
                    // This is where we have to assign null to the value of the parameter.
                    restrictionParameter.Value = DBNull.Value;

                }

                restrictionParameter.ParameterName = GetParameterName(collectionName, i + 1);
                restrictionParameter.Direction = ParameterDirection.Input;
                command.Parameters.Add(restrictionParameter);
            }

            DbDataReader? reader = null;
            try
            {
                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception e)
                {
                    if (!ADP.IsCatchableExceptionType(e))
                    {
                        throw;
                    }
                    throw ADP.QueryFailed(collectionName, e);
                }

                // TODO: Consider using the DataAdapter.Fill

                // Build a DataTable from the reader
                resultTable = new DataTable(collectionName);
                resultTable.Locale = CultureInfo.InvariantCulture;

                DataTable? schemaTable = reader.GetSchemaTable();
                foreach (DataRow row in schemaTable!.Rows)
                {
                    resultTable.Columns.Add(row["ColumnName"] as string, (Type)row["DataType"]);
                }
                object[] values = new object[resultTable.Columns.Count];
                while (reader.Read())
                {
                    reader.GetValues(values);
                    resultTable.Rows.Add(values);
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                    reader = null;
                }
            }

            return resultTable;
        }

        private DataColumn[] FilterColumns(DataTable sourceTable, string[]? hiddenColumnNames, DataColumnCollection destinationColumns)
        {
            DataColumn newDestinationColumn;
            int currentColumn;
            DataColumn[]? filteredSourceColumns = null;

            int columnCount = 0;
            foreach (DataColumn sourceColumn in sourceTable.Columns)
            {
                if (IncludeThisColumn(sourceColumn, hiddenColumnNames) == true)
                {
                    columnCount++;
                }
            }

            if (columnCount == 0)
            {
                throw ADP.NoColumns();
            }

            currentColumn = 0;
            filteredSourceColumns = new DataColumn[columnCount];

            foreach (DataColumn sourceColumn in sourceTable.Columns)
            {
                if (IncludeThisColumn(sourceColumn, hiddenColumnNames) == true)
                {
                    newDestinationColumn = new DataColumn(sourceColumn.ColumnName, sourceColumn.DataType);
                    destinationColumns.Add(newDestinationColumn);
                    filteredSourceColumns[currentColumn] = sourceColumn;
                    currentColumn++;
                }
            }
            return filteredSourceColumns;
        }

        internal DataRow FindMetaDataCollectionRow(string collectionName)
        {
            bool versionFailure;
            bool haveExactMatch;
            bool haveMultipleInexactMatches;
            string? candidateCollectionName;

            DataTable? metaDataCollectionsTable = _metaDataCollectionsDataSet.Tables[DbMetaDataCollectionNames.MetaDataCollections];
            if (metaDataCollectionsTable == null)
            {
                throw ADP.InvalidXml();
            }

            DataColumn? collectionNameColumn = metaDataCollectionsTable.Columns[DbMetaDataColumnNames.CollectionName];

            if ((null == collectionNameColumn) || (typeof(string) != collectionNameColumn.DataType))
            {
                throw ADP.InvalidXmlMissingColumn(DbMetaDataCollectionNames.MetaDataCollections, DbMetaDataColumnNames.CollectionName);
            }

            DataRow? requestedCollectionRow = null;
            string? exactCollectionName = null;

            // find the requested collection
            versionFailure = false;
            haveExactMatch = false;
            haveMultipleInexactMatches = false;

            foreach (DataRow row in metaDataCollectionsTable.Rows)
            {
                candidateCollectionName = row[collectionNameColumn, DataRowVersion.Current] as string;
                if (ADP.IsEmpty(candidateCollectionName))
                {
                    throw ADP.InvalidXmlInvalidValue(DbMetaDataCollectionNames.MetaDataCollections, DbMetaDataColumnNames.CollectionName);
                }

                if (ADP.CompareInsensitiveInvariant(candidateCollectionName, collectionName))
                {
                    if (SupportedByCurrentVersion(row) == false)
                    {
                        versionFailure = true;
                    }
                    else
                    {
                        if (collectionName == candidateCollectionName)
                        {
                            if (haveExactMatch == true)
                            {
                                throw ADP.CollectionNameIsNotUnique(collectionName);
                            }
                            requestedCollectionRow = row;
                            exactCollectionName = candidateCollectionName;
                            haveExactMatch = true;
                        }
                        else
                        {
                            // have an inexact match - ok only if it is the only one
                            if (exactCollectionName != null)
                            {
                                // can't fail here becasue we may still find an exact match
                                haveMultipleInexactMatches = true;
                            }
                            requestedCollectionRow = row;
                            exactCollectionName = candidateCollectionName;
                        }
                    }
                }
            }

            if (requestedCollectionRow == null)
            {
                if (versionFailure == false)
                {
                    throw ADP.UndefinedCollection(collectionName);
                }
                else
                {
                    throw ADP.UnsupportedVersion(collectionName);
                }
            }

            if ((haveExactMatch == false) && (haveMultipleInexactMatches == true))
            {
                throw ADP.AmbigousCollectionName(collectionName);
            }

            return requestedCollectionRow;

        }

        private void FixUpVersion(DataTable dataSourceInfoTable)
        {
            Debug.Assert(dataSourceInfoTable.TableName == DbMetaDataCollectionNames.DataSourceInformation);
            DataColumn? versionColumn = dataSourceInfoTable.Columns[_dataSourceProductVersion];
            DataColumn? normalizedVersionColumn = dataSourceInfoTable.Columns[_dataSourceProductVersionNormalized];

            if ((versionColumn == null) || (normalizedVersionColumn == null))
            {
                throw ADP.MissingDataSourceInformationColumn();
            }

            if (dataSourceInfoTable.Rows.Count != 1)
            {
                throw ADP.IncorrectNumberOfDataSourceInformationRows();
            }

            DataRow dataSourceInfoRow = dataSourceInfoTable.Rows[0];

            dataSourceInfoRow[versionColumn] = _serverVersionString;
            dataSourceInfoRow[normalizedVersionColumn] = _normalizedServerVersion;
            dataSourceInfoRow.AcceptChanges();
        }

        private string GetParameterName(string neededCollectionName, int neededRestrictionNumber)
        {
            DataTable? restrictionsTable = null;
            DataColumnCollection? restrictionColumns = null;
            DataColumn? collectionName = null;
            DataColumn? parameterName = null;
            DataColumn? restrictionName = null;
            DataColumn? restrictionNumber = null;
            ;
            string? result = null;

            restrictionsTable = _metaDataCollectionsDataSet.Tables[DbMetaDataCollectionNames.Restrictions];
            if (restrictionsTable != null)
            {
                restrictionColumns = restrictionsTable.Columns;
                if (restrictionColumns != null)
                {
                    collectionName = restrictionColumns[_collectionName];
                    parameterName = restrictionColumns[_parameterName];
                    restrictionName = restrictionColumns[_restrictionName];
                    restrictionNumber = restrictionColumns[_restrictionNumber];
                }
            }

            if ((parameterName == null) || (collectionName == null) || (restrictionName == null) || (restrictionNumber == null))
            {
                throw ADP.MissingRestrictionColumn();
            }

            foreach (DataRow restriction in restrictionsTable!.Rows)
            {
                if (((string)restriction[collectionName] == neededCollectionName) &&
                    ((int)restriction[restrictionNumber] == neededRestrictionNumber) &&
                    (SupportedByCurrentVersion(restriction)))
                {
                    result = (string)restriction[parameterName];
                    break;
                }
            }

            if (result == null)
            {
                throw ADP.MissingRestrictionRow();
            }

            return result;

        }

        public virtual DataTable GetSchema(DbConnection connection, string collectionName, string?[]? restrictions)
        {
            Debug.Assert(_metaDataCollectionsDataSet != null);

            DataTable metaDataCollectionsTable = _metaDataCollectionsDataSet.Tables[DbMetaDataCollectionNames.MetaDataCollections]!;
            DataColumn populationMechanismColumn = metaDataCollectionsTable.Columns[_populationMechanism]!;
            DataColumn collectionNameColumn = metaDataCollectionsTable.Columns[DbMetaDataColumnNames.CollectionName]!;
            DataRow? requestedCollectionRow = null;
            DataTable? requestedSchema = null;
            string[]? hiddenColumns;
            string? exactCollectionName = null;

            requestedCollectionRow = FindMetaDataCollectionRow(collectionName);
            exactCollectionName = (requestedCollectionRow[collectionNameColumn, DataRowVersion.Current] as string)!;

            if (ADP.IsEmptyArray(restrictions) == false)
            {
                for (int i = 0; i < restrictions!.Length; i++)
                {
                    if ((restrictions[i]?.Length > 4096))
                    {
                        // use a non-specific error because no new beta 2 error messages are allowed
                        // TODO: will add a more descriptive error in RTM
                        throw ADP.NotSupported();
                    }
                }
            }

            string populationMechanism = (requestedCollectionRow[populationMechanismColumn, DataRowVersion.Current] as string)!;
            switch (populationMechanism)
            {
                case _dataTable:
                    if (exactCollectionName == DbMetaDataCollectionNames.MetaDataCollections)
                    {
                        hiddenColumns = new string[2];
                        hiddenColumns[0] = _populationMechanism;
                        hiddenColumns[1] = _populationString;
                    }
                    else
                    {
                        hiddenColumns = null;
                    }
                    // none of the datatable collections support restrictions
                    if (ADP.IsEmptyArray(restrictions) == false)
                    {
                        throw ADP.TooManyRestrictions(exactCollectionName);
                    }

                    requestedSchema = CloneAndFilterCollection(exactCollectionName, hiddenColumns);

                    // TODO: Consider an alternate method that doesn't involve special casing -- perhaps _prepareCollection

                    // for the data source infomation table we need to fix up the version columns at run time
                    // since the version is determined at run time
                    if (exactCollectionName == DbMetaDataCollectionNames.DataSourceInformation)
                    {
                        FixUpVersion(requestedSchema);
                    }
                    break;

                case _sqlCommand:
                    requestedSchema = ExecuteCommand(requestedCollectionRow, restrictions, connection);
                    break;

                case _prepareCollection:
                    requestedSchema = PrepareCollection(exactCollectionName, restrictions, connection);
                    break;

                default:
                    throw ADP.UndefinedPopulationMechanism(populationMechanism);
            }

            return requestedSchema;
        }

        private bool IncludeThisColumn(DataColumn sourceColumn, string[]? hiddenColumnNames)
        {
            bool result = true;
            string sourceColumnName = sourceColumn.ColumnName;

            switch (sourceColumnName)
            {
                case _minimumVersion:
                case _maximumVersion:
                    result = false;
                    break;

                default:
                    if (hiddenColumnNames == null)
                    {
                        break;
                    }
                    for (int i = 0; i < hiddenColumnNames.Length; i++)
                    {
                        if (hiddenColumnNames[i] == sourceColumnName)
                        {
                            result = false;
                            break;
                        }
                    }
                    break;
            }

            return result;
        }

        protected virtual DataTable PrepareCollection(string collectionName, string?[]? restrictions, DbConnection connection)
        {
            throw ADP.NotSupported();
        }

        private bool SupportedByCurrentVersion(DataRow requestedCollectionRow)
        {
            bool result = true;
            DataColumnCollection tableColumns = requestedCollectionRow.Table.Columns;
            DataColumn? versionColumn;
            object version;

            // check the minimum version first
            versionColumn = tableColumns[_minimumVersion];
            if (versionColumn != null)
            {
                version = requestedCollectionRow[versionColumn];
                if (version != null)
                {
                    if (version != DBNull.Value)
                    {
                        if (0 > string.Compare(_normalizedServerVersion, (string)version, StringComparison.OrdinalIgnoreCase))
                        {
                            result = false;
                        }
                    }
                }
            }

            // if the minmum version was ok what about the maximum version
            if (result == true)
            {
                versionColumn = tableColumns[_maximumVersion];
                if (versionColumn != null)
                {
                    version = requestedCollectionRow[versionColumn];
                    if (version != null)
                    {
                        if (version != DBNull.Value)
                        {
                            if (0 < string.Compare(_normalizedServerVersion, (string)version, StringComparison.OrdinalIgnoreCase))
                            {
                                result = false;
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
