// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Data;
using System.IO;
using System.Data.ProviderBase;
using System.Data.Common;
using System.Text;

namespace System.Data.SqlClient
{
    internal sealed class SqlMetaDataFactory : DbMetaDataFactory
    {

        private const string _serverVersionNormalized90 = "09.00.0000";
        private const string _serverVersionNormalized90782 = "09.00.0782";
        private const string _serverVersionNormalized10 = "10.00.0000";


        public SqlMetaDataFactory(Stream XMLStream,
                                    string serverVersion,
                                    string serverVersionNormalized) :
                base(XMLStream, serverVersion, serverVersionNormalized) { }

        private void addUDTsToDataTypesTable(DataTable dataTypesTable, SqlConnection connection, String ServerVersion)
        {
            const string sqlCommand =
                "select " +
                    "assemblies.name, " +
                    "types.assembly_class, " +
                    "ASSEMBLYPROPERTY(assemblies.name, 'VersionMajor') as version_major, " +
                    "ASSEMBLYPROPERTY(assemblies.name, 'VersionMinor') as version_minor, " +
                    "ASSEMBLYPROPERTY(assemblies.name, 'VersionBuild') as version_build, " +
                    "ASSEMBLYPROPERTY(assemblies.name, 'VersionRevision') as version_revision, " +
                    "ASSEMBLYPROPERTY(assemblies.name, 'CultureInfo') as culture_info, " +
                    "ASSEMBLYPROPERTY(assemblies.name, 'PublicKey') as public_key, " +
                    "is_nullable, " +
                    "is_fixed_length, " +
                    "max_length " +
                "from sys.assemblies as assemblies  join sys.assembly_types as types " +
                "on assemblies.assembly_id = types.assembly_id ";

            // pre 9.0/Yukon servers do not have UDTs
            if (0 > string.Compare(ServerVersion, _serverVersionNormalized90, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Execute the SELECT statement
            SqlCommand command = connection.CreateCommand();
            command.CommandText = sqlCommand;
            DataRow newRow = null;
            DataColumn providerDbtype = dataTypesTable.Columns[DbMetaDataColumnNames.ProviderDbType];
            DataColumn columnSize = dataTypesTable.Columns[DbMetaDataColumnNames.ColumnSize];
            DataColumn isFixedLength = dataTypesTable.Columns[DbMetaDataColumnNames.IsFixedLength];
            DataColumn isSearchable = dataTypesTable.Columns[DbMetaDataColumnNames.IsSearchable];
            DataColumn isLiteralSupported = dataTypesTable.Columns[DbMetaDataColumnNames.IsLiteralSupported];
            DataColumn typeName = dataTypesTable.Columns[DbMetaDataColumnNames.TypeName];
            DataColumn isNullable = dataTypesTable.Columns[DbMetaDataColumnNames.IsNullable];

            if ((providerDbtype == null) ||
                (columnSize == null) ||
                (isFixedLength == null) ||
                (isSearchable == null) ||
                (isLiteralSupported == null) ||
                (typeName == null) ||
                (isNullable == null))
            {
                throw ADP.InvalidXml();
            }

            const int columnSizeIndex = 10;
            const int isFixedLengthIndex = 9;
            const int isNullableIndex = 8;
            const int assemblyNameIndex = 0;
            const int assemblyClassIndex = 1;
            const int versionMajorIndex = 2;
            const int versionMinorIndex = 3;
            const int versionBuildIndex = 4;
            const int versionRevisionIndex = 5;
            const int cultureInfoIndex = 6;
            const int publicKeyIndex = 7;


            using (IDataReader reader = command.ExecuteReader())
            {

                object[] values = new object[11];
                while (reader.Read())
                {

                    reader.GetValues(values);
                    newRow = dataTypesTable.NewRow();

                    newRow[providerDbtype] = SqlDbType.Udt;

                    if (values[columnSizeIndex] != DBNull.Value)
                    {
                        newRow[columnSize] = values[columnSizeIndex];
                    }

                    if (values[isFixedLengthIndex] != DBNull.Value)
                    {
                        newRow[isFixedLength] = values[isFixedLengthIndex];
                    }

                    newRow[isSearchable] = true;
                    newRow[isLiteralSupported] = false;
                    if (values[isNullableIndex] != DBNull.Value)
                    {
                        newRow[isNullable] = values[isNullableIndex];
                    }

                    if ((values[assemblyNameIndex] != DBNull.Value) &&
                        (values[assemblyClassIndex] != DBNull.Value) &&
                        (values[versionMajorIndex] != DBNull.Value) &&
                        (values[versionMinorIndex] != DBNull.Value) &&
                        (values[versionBuildIndex] != DBNull.Value) &&
                        (values[versionRevisionIndex] != DBNull.Value))
                    {

                        StringBuilder nameString = new StringBuilder();
                        nameString.Append(values[assemblyClassIndex].ToString());
                        nameString.Append(", ");
                        nameString.Append(values[assemblyNameIndex].ToString());
                        nameString.Append(", Version=");

                        nameString.Append(values[versionMajorIndex].ToString());
                        nameString.Append(".");
                        nameString.Append(values[versionMinorIndex].ToString());
                        nameString.Append(".");
                        nameString.Append(values[versionBuildIndex].ToString());
                        nameString.Append(".");
                        nameString.Append(values[versionRevisionIndex].ToString());

                        if (values[cultureInfoIndex] != DBNull.Value)
                        {
                            nameString.Append(", Culture=");
                            nameString.Append(values[cultureInfoIndex].ToString());
                        }

                        if (values[publicKeyIndex] != DBNull.Value)
                        {

                            nameString.Append(", PublicKeyToken=");

                            StringBuilder resultString = new StringBuilder();
                            Byte[] byteArrayValue = (Byte[])values[publicKeyIndex];
                            foreach (byte b in byteArrayValue)
                            {
                                resultString.Append(String.Format((IFormatProvider)null, "{0,-2:x2}", b));
                            }
                            nameString.Append(resultString.ToString());
                        }

                        newRow[typeName] = nameString.ToString();
                        dataTypesTable.Rows.Add(newRow);
                        newRow.AcceptChanges();
                    } // if assembly name

                }//end while
            } // end using
        }

        private void AddTVPsToDataTypesTable(DataTable dataTypesTable, SqlConnection connection, String ServerVersion)
        {

            const string sqlCommand =
                "select " +
                    "name, " +
                    "is_nullable, " +
                    "max_length " +
                "from sys.types " +
                "where is_table_type = 1";

            // TODO: update this check once the server upgrades major version number!!!
            // pre 9.0/Yukon servers do not have Table types
            if (0 > string.Compare(ServerVersion, _serverVersionNormalized10, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Execute the SELECT statement
            SqlCommand command = connection.CreateCommand();
            command.CommandText = sqlCommand;
            DataRow newRow = null;
            DataColumn providerDbtype = dataTypesTable.Columns[DbMetaDataColumnNames.ProviderDbType];
            DataColumn columnSize = dataTypesTable.Columns[DbMetaDataColumnNames.ColumnSize];
            DataColumn isSearchable = dataTypesTable.Columns[DbMetaDataColumnNames.IsSearchable];
            DataColumn isLiteralSupported = dataTypesTable.Columns[DbMetaDataColumnNames.IsLiteralSupported];
            DataColumn typeName = dataTypesTable.Columns[DbMetaDataColumnNames.TypeName];
            DataColumn isNullable = dataTypesTable.Columns[DbMetaDataColumnNames.IsNullable];

            if ((providerDbtype == null) ||
                (columnSize == null) ||
                (isSearchable == null) ||
                (isLiteralSupported == null) ||
                (typeName == null) ||
                (isNullable == null))
            {
                throw ADP.InvalidXml();
            }

            const int columnSizeIndex = 2;
            const int isNullableIndex = 1;
            const int typeNameIndex = 0;

            using (IDataReader reader = command.ExecuteReader())
            {

                object[] values = new object[11];
                while (reader.Read())
                {

                    reader.GetValues(values);
                    newRow = dataTypesTable.NewRow();

                    newRow[providerDbtype] = SqlDbType.Structured;

                    if (values[columnSizeIndex] != DBNull.Value)
                    {
                        newRow[columnSize] = values[columnSizeIndex];
                    }

                    newRow[isSearchable] = false;
                    newRow[isLiteralSupported] = false;
                    if (values[isNullableIndex] != DBNull.Value)
                    {
                        newRow[isNullable] = values[isNullableIndex];
                    }

                    if (values[typeNameIndex] != DBNull.Value)
                    {
                        newRow[typeName] = values[typeNameIndex];
                        dataTypesTable.Rows.Add(newRow);
                        newRow.AcceptChanges();
                    } // if type name
                }//end while
            } // end using
        }

        private DataTable GetDataTypesTable(SqlConnection connection)
        {
            // verify the existance of the table in the data set
            DataTable dataTypesTable = CollectionDataSet.Tables[DbMetaDataCollectionNames.DataTypes];
            if (dataTypesTable == null)
            {
                throw ADP.UnableToBuildCollection(DbMetaDataCollectionNames.DataTypes);
            }

            // copy the table filtering out any rows that don't apply to tho current version of the prrovider
            dataTypesTable = CloneAndFilterCollection(DbMetaDataCollectionNames.DataTypes, null);

            addUDTsToDataTypesTable(dataTypesTable, connection, ServerVersionNormalized);
            AddTVPsToDataTypesTable(dataTypesTable, connection, ServerVersionNormalized);

            dataTypesTable.AcceptChanges();
            return dataTypesTable;
        }

        protected override DataTable PrepareCollection(String collectionName, String[] restrictions, DbConnection connection)
        {
            SqlConnection sqlConnection = (SqlConnection)connection;
            DataTable resultTable = null;

            if (collectionName == DbMetaDataCollectionNames.DataTypes)
            {
                if (ADP.IsEmptyArray(restrictions) == false)
                {
                    throw ADP.TooManyRestrictions(DbMetaDataCollectionNames.DataTypes);
                }
                resultTable = GetDataTypesTable(sqlConnection);
            }

            if (resultTable == null)
            {
                throw ADP.UnableToBuildCollection(collectionName);
            }

            return resultTable;

        }



    }
}