// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;

namespace System.Data.OleDb
{
    public sealed class OleDbCommandBuilder : DbCommandBuilder
    {
        public OleDbCommandBuilder() : base()
        {
            GC.SuppressFinalize(this);
        }

        public OleDbCommandBuilder(OleDbDataAdapter? adapter) : this()
        {
            DataAdapter = adapter;
        }

        [DefaultValue(null)]
        public new OleDbDataAdapter? DataAdapter
        {
            get
            {
                return (base.DataAdapter as OleDbDataAdapter);
            }
            set
            {
                base.DataAdapter = value;
            }
        }

        private void OleDbRowUpdatingHandler(object sender, OleDbRowUpdatingEventArgs ruevent)
        {
            RowUpdatingHandler(ruevent);
        }

        public new OleDbCommand GetInsertCommand()
        {
            return (OleDbCommand)base.GetInsertCommand();
        }
        public new OleDbCommand GetInsertCommand(bool useColumnsForParameterNames)
        {
            return (OleDbCommand)base.GetInsertCommand(useColumnsForParameterNames);
        }

        public new OleDbCommand GetUpdateCommand()
        {
            return (OleDbCommand)base.GetUpdateCommand();
        }
        public new OleDbCommand GetUpdateCommand(bool useColumnsForParameterNames)
        {
            return (OleDbCommand)base.GetUpdateCommand(useColumnsForParameterNames);
        }

        public new OleDbCommand GetDeleteCommand()
        {
            return (OleDbCommand)base.GetDeleteCommand();
        }
        public new OleDbCommand GetDeleteCommand(bool useColumnsForParameterNames)
        {
            return (OleDbCommand)base.GetDeleteCommand(useColumnsForParameterNames);
        }

        protected override string GetParameterName(int parameterOrdinal)
        {
            return "p" + parameterOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        protected override string GetParameterName(string parameterName)
        {
            return parameterName;
        }

        protected override string GetParameterPlaceholder(int parameterOrdinal)
        {
            return "?";
        }

        protected override void ApplyParameterInfo(DbParameter parameter, DataRow datarow, StatementType statementType, bool whereClause)
        {
            OleDbParameter p = (OleDbParameter)parameter;
            object valueType = datarow[SchemaTableColumn.ProviderType];
            p.OleDbType = (OleDbType)valueType;

            object bvalue = datarow[SchemaTableColumn.NumericPrecision];
            if (DBNull.Value != bvalue)
            {
                byte bval = (byte)(short)bvalue;
                p.PrecisionInternal = ((0xff != bval) ? bval : (byte)0);
            }

            bvalue = datarow[SchemaTableColumn.NumericScale];
            if (DBNull.Value != bvalue)
            {
                byte bval = (byte)(short)bvalue;
                p.ScaleInternal = ((0xff != bval) ? bval : (byte)0);
            }
        }

        public static void DeriveParameters(OleDbCommand command)
        {
            if (null == command)
            {
                throw ADP.ArgumentNull("command");
            }
            switch (command.CommandType)
            {
                case System.Data.CommandType.Text:
                    throw ADP.DeriveParametersNotSupported(command);
                case System.Data.CommandType.StoredProcedure:
                    break;
                case System.Data.CommandType.TableDirect:
                    // CommandType.TableDirect - do nothing, parameters are not supported
                    throw ADP.DeriveParametersNotSupported(command);
                default:
                    throw ADP.InvalidCommandType(command.CommandType);
            }
            if (ADP.IsEmpty(command.CommandText))
            {
                throw ADP.CommandTextRequired(ADP.DeriveParameters);
            }
            OleDbConnection? connection = command.Connection;
            if (null == connection)
            {
                throw ADP.ConnectionRequired(ADP.DeriveParameters);
            }
            ConnectionState state = connection.State;
            if (ConnectionState.Open != state)
            {
                throw ADP.OpenConnectionRequired(ADP.DeriveParameters, state);
            }
            OleDbParameter[] list = DeriveParametersFromStoredProcedure(connection, command);

            OleDbParameterCollection parameters = command.Parameters;
            parameters.Clear();

            for (int i = 0; i < list.Length; ++i)
            {
                parameters.Add(list[i]);
            }
        }

        // known difference: when getting the parameters for a sproc, the
        //   return value gets marked as a return value but for a sql stmt
        //   the return value gets marked as an output parameter.
        private static OleDbParameter[] DeriveParametersFromStoredProcedure(OleDbConnection connection, OleDbCommand command)
        {
            OleDbParameter[] plist = Array.Empty<OleDbParameter>();

            if (connection.SupportSchemaRowset(OleDbSchemaGuid.Procedure_Parameters))
            {
                string quotePrefix, quoteSuffix;
                connection.GetLiteralQuotes(ADP.DeriveParameters, out quotePrefix, out quoteSuffix);

                object?[] parsed = MultipartIdentifier.ParseMultipartIdentifier(command.CommandText, quotePrefix, quoteSuffix, '.', 4, true, SR.OLEDB_OLEDBCommandText, false);
                if (null == parsed[3])
                {
                    throw ADP.NoStoredProcedureExists(command.CommandText);
                }

                object?[] restrictions = new object[4];
                object value;

                // Parse returns an enforced 4 part array
                // 0) Server - ignored but removal would be a run-time breaking change from V1.0
                // 1) Catalog
                // 2) Schema
                // 3) ProcedureName

                // Restrictions array which is passed to OleDb API expects:
                // 0) Catalog
                // 1) Schema
                // 2) ProcedureName
                // 3) ParameterName (leave null)

                // Copy from Parse format to OleDb API format
                Array.Copy(parsed, 1, restrictions, 0, 3);

                //if (cmdConnection.IsServer_msdaora) {
                //    restrictions[1] = Convert.ToString(cmdConnection.UserId).ToUpper();
                //}
                DataTable? table = connection.GetSchemaRowset(OleDbSchemaGuid.Procedure_Parameters, restrictions);

                if (null != table)
                {
                    DataColumnCollection columns = table.Columns;

                    DataColumn? parameterName = null;
                    DataColumn? parameterDirection = null;
                    DataColumn? dataType = null;
                    DataColumn? maxLen = null;
                    DataColumn? numericPrecision = null;
                    DataColumn? numericScale = null;
                    DataColumn? backendtype = null;

                    int index = columns.IndexOf(ODB.PARAMETER_NAME);
                    if (-1 != index)
                        parameterName = columns[index];

                    index = columns.IndexOf(ODB.PARAMETER_TYPE);
                    if (-1 != index)
                        parameterDirection = columns[index];

                    index = columns.IndexOf(ODB.DATA_TYPE);
                    if (-1 != index)
                        dataType = columns[index];

                    index = columns.IndexOf(ODB.CHARACTER_MAXIMUM_LENGTH);
                    if (-1 != index)
                        maxLen = columns[index];

                    index = columns.IndexOf(ODB.NUMERIC_PRECISION);
                    if (-1 != index)
                        numericPrecision = columns[index];

                    index = columns.IndexOf(ODB.NUMERIC_SCALE);
                    if (-1 != index)
                        numericScale = columns[index];

                    index = columns.IndexOf(ODB.TYPE_NAME);
                    if (-1 != index)
                        backendtype = columns[index];

                    DataRow[] dataRows = table.Select(null, ODB.ORDINAL_POSITION_ASC, DataViewRowState.CurrentRows);
                    plist = new OleDbParameter[dataRows.Length];
                    for (index = 0; index < dataRows.Length; ++index)
                    {
                        DataRow dataRow = dataRows[index];

                        OleDbParameter parameter = new OleDbParameter();

                        if ((null != parameterName) && !dataRow.IsNull(parameterName, DataRowVersion.Default))
                        {
                            // $CONSIDER - not trimming the @ from the beginning but to left the designer do that
                            parameter.ParameterName = Convert.ToString(dataRow[parameterName, DataRowVersion.Default], CultureInfo.InvariantCulture)!.TrimStart(new char[] { '@', ' ', ':' });
                        }
                        if ((null != parameterDirection) && !dataRow.IsNull(parameterDirection, DataRowVersion.Default))
                        {
                            short direction = Convert.ToInt16(dataRow[parameterDirection, DataRowVersion.Default], CultureInfo.InvariantCulture);
                            parameter.Direction = ConvertToParameterDirection(direction);
                        }
                        if ((null != dataType) && !dataRow.IsNull(dataType, DataRowVersion.Default))
                        {
                            // need to ping FromDBType, otherwise WChar->WChar when the user really wants VarWChar
                            short wType = Convert.ToInt16(dataRow[dataType, DataRowVersion.Default], CultureInfo.InvariantCulture);
                            parameter.OleDbType = NativeDBType.FromDBType(wType, false, false).enumOleDbType;
                        }
                        if ((null != maxLen) && !dataRow.IsNull(maxLen, DataRowVersion.Default))
                        {
                            parameter.Size = Convert.ToInt32(dataRow[maxLen, DataRowVersion.Default], CultureInfo.InvariantCulture);
                        }
                        switch (parameter.OleDbType)
                        {
                            case OleDbType.Decimal:
                            case OleDbType.Numeric:
                            case OleDbType.VarNumeric:
                                if ((null != numericPrecision) && !dataRow.IsNull(numericPrecision, DataRowVersion.Default))
                                {
                                    // @devnote: unguarded cast from Int16 to Byte
                                    parameter.PrecisionInternal = (byte)Convert.ToInt16(dataRow[numericPrecision], CultureInfo.InvariantCulture);
                                }
                                if ((null != numericScale) && !dataRow.IsNull(numericScale, DataRowVersion.Default))
                                {
                                    // @devnote: unguarded cast from Int16 to Byte
                                    parameter.ScaleInternal = (byte)Convert.ToInt16(dataRow[numericScale], CultureInfo.InvariantCulture);
                                }
                                break;
                            case OleDbType.VarBinary:
                            case OleDbType.VarChar:
                            case OleDbType.VarWChar:
                                value = dataRow[backendtype!, DataRowVersion.Default];
                                if (value is string)
                                {
                                    string backendtypename = ((string)value).ToLowerInvariant();
                                    switch (backendtypename)
                                    {
                                        case "binary":
                                            parameter.OleDbType = OleDbType.Binary;
                                            break;
                                        //case "varbinary":
                                        //    parameter.OleDbType = OleDbType.VarBinary;
                                        //    break;
                                        case "image":
                                            parameter.OleDbType = OleDbType.LongVarBinary;
                                            break;
                                        case "char":
                                            parameter.OleDbType = OleDbType.Char;
                                            break;
                                        //case "varchar":
                                        //case "varchar2":
                                        //    parameter.OleDbType = OleDbType.VarChar;
                                        //    break;
                                        case "text":
                                            parameter.OleDbType = OleDbType.LongVarChar;
                                            break;
                                        case "nchar":
                                            parameter.OleDbType = OleDbType.WChar;
                                            break;
                                        //case "nvarchar":
                                        //    parameter.OleDbType = OleDbType.VarWChar;
                                        case "ntext":
                                            parameter.OleDbType = OleDbType.LongVarWChar;
                                            break;
                                    }
                                }
                                break;
                        }
                        //if (AdapterSwitches.OleDbSql.TraceVerbose) {
                        //    ADP.Trace_Parameter("StoredProcedure", parameter);
                        //}
                        plist[index] = parameter;
                    }
                }
                if ((0 == plist.Length) && connection.SupportSchemaRowset(OleDbSchemaGuid.Procedures))
                {
                    restrictions = new object?[4] { null, null, command.CommandText, null };
                    table = connection.GetSchemaRowset(OleDbSchemaGuid.Procedures, restrictions)!;
                    if (0 == table.Rows.Count)
                    {
                        throw ADP.NoStoredProcedureExists(command.CommandText);
                    }
                }
            }
            else if (connection.SupportSchemaRowset(OleDbSchemaGuid.Procedures))
            {
                object?[] restrictions = new object?[4] { null, null, command.CommandText, null };
                DataTable table = connection.GetSchemaRowset(OleDbSchemaGuid.Procedures, restrictions)!;
                if (0 == table.Rows.Count)
                {
                    throw ADP.NoStoredProcedureExists(command.CommandText);
                }
                // we don't ever expect a procedure with 0 parameters, they should have at least a return value
                throw ODB.NoProviderSupportForSProcResetParameters(connection.Provider);
            }
            else
            {
                throw ODB.NoProviderSupportForSProcResetParameters(connection.Provider);
            }
            return plist;
        }

        private static ParameterDirection ConvertToParameterDirection(int value) =>
            value switch
            {
                ODB.DBPARAMTYPE_INPUT => System.Data.ParameterDirection.Input,
                ODB.DBPARAMTYPE_INPUTOUTPUT => System.Data.ParameterDirection.InputOutput,
                ODB.DBPARAMTYPE_OUTPUT => System.Data.ParameterDirection.Output,
                ODB.DBPARAMTYPE_RETURNVALUE => System.Data.ParameterDirection.ReturnValue,
                _ => System.Data.ParameterDirection.Input,
            };

        public override string QuoteIdentifier(string unquotedIdentifier)
        {
            return QuoteIdentifier(unquotedIdentifier, null /* use DataAdapter.SelectCommand.Connection if available */);
        }
        public string QuoteIdentifier(string unquotedIdentifier, OleDbConnection? connection)
        {
            ADP.CheckArgumentNull(unquotedIdentifier, "unquotedIdentifier");

            // if the user has specificed a prefix use the user specified  prefix and suffix
            // otherwise get them from the provider
            string quotePrefix = QuotePrefix;
            string quoteSuffix = QuoteSuffix;
            if (ADP.IsEmpty(quotePrefix) == true)
            {
                if (connection == null)
                {
                    // Use the adapter's connection if QuoteIdentifier was called from
                    // DbCommandBuilder instance (which does not have an overload that gets connection object)
                    connection = DataAdapter?.SelectCommand?.Connection;
                    if (connection == null)
                    {
                        throw ADP.QuotePrefixNotSet(ADP.QuoteIdentifier);
                    }
                }
                connection.GetLiteralQuotes(ADP.QuoteIdentifier, out quotePrefix, out quoteSuffix);
                // if the quote suffix is null assume that it is the same as the prefix (See OLEDB spec
                // IDBInfo::GetLiteralInfo DBLITERAL_QUOTE_SUFFIX.)
                if (quoteSuffix == null)
                {
                    quoteSuffix = quotePrefix;
                }
            }

            return ADP.BuildQuotedString(quotePrefix, quoteSuffix, unquotedIdentifier);
        }

        protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
        {
            Debug.Assert(adapter is OleDbDataAdapter, "!OleDbDataAdapter");
            if (adapter == base.DataAdapter)
            { // removal case
                ((OleDbDataAdapter)adapter).RowUpdating -= OleDbRowUpdatingHandler;
            }
            else
            { // adding case
                ((OleDbDataAdapter)adapter).RowUpdating += OleDbRowUpdatingHandler;
            }
        }

        public override string UnquoteIdentifier(string quotedIdentifier)
        {
            return UnquoteIdentifier(quotedIdentifier, null /* use DataAdapter.SelectCommand.Connection if available */);
        }

        public string UnquoteIdentifier(string quotedIdentifier, OleDbConnection? connection)
        {
            ADP.CheckArgumentNull(quotedIdentifier, "quotedIdentifier");

            // if the user has specificed a prefix use the user specified  prefix and suffix
            // otherwise get them from the provider
            string quotePrefix = QuotePrefix;
            string quoteSuffix = QuoteSuffix;
            if (ADP.IsEmpty(quotePrefix) == true)
            {
                if (connection == null)
                {
                    // Use the adapter's connection if UnquoteIdentifier was called from
                    // DbCommandBuilder instance (which does not have an overload that gets connection object)
                    connection = DataAdapter?.SelectCommand?.Connection;
                    if (connection == null)
                    {
                        throw ADP.QuotePrefixNotSet(ADP.UnquoteIdentifier);
                    }
                }
                connection.GetLiteralQuotes(ADP.UnquoteIdentifier, out quotePrefix, out quoteSuffix);
                // if the quote suffix is null assume that it is the same as the prefix (See OLEDB spec
                // IDBInfo::GetLiteralInfo DBLITERAL_QUOTE_SUFFIX.)
                if (quoteSuffix == null)
                {
                    quoteSuffix = quotePrefix;
                }
            }

            string? unquotedIdentifier;
            // ignoring the return value because it is acceptable for the quotedString to not be quoted in this
            // context.
            ADP.RemoveStringQuotes(quotePrefix, quoteSuffix, quotedIdentifier, out unquotedIdentifier);
            return unquotedIdentifier!;
        }
    }
}
