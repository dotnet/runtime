// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Data.Common;
using System.Data.Sql;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Data.SqlClient
{
    public sealed class SqlCommandBuilder : DbCommandBuilder
    {
        public SqlCommandBuilder() : base()
        {
            GC.SuppressFinalize(this);
            base.QuotePrefix = "["; // initialize base with defaults
            base.QuoteSuffix = "]";
        }

        public SqlCommandBuilder(SqlDataAdapter adapter) : this()
        {
            DataAdapter = adapter;
        }

        /// <devnote>SqlServer only supports CatalogLocation.Start</devnote>
        public override CatalogLocation CatalogLocation
        {
            get
            {
                return CatalogLocation.Start;
            }
            set
            {
                if (CatalogLocation.Start != value)
                {
                    throw ADP.SingleValuedProperty(nameof(CatalogLocation), nameof(CatalogLocation.Start));
                }
            }
        }

        /// <devnote>SqlServer only supports '.'</devnote>
        public override string CatalogSeparator
        {
            get
            {
                return ".";
            }
            set
            {
                if ("." != value)
                {
                    throw ADP.SingleValuedProperty(nameof(CatalogSeparator), ".");
                }
            }
        }

        new public SqlDataAdapter DataAdapter
        {
            get
            {
                return (SqlDataAdapter)base.DataAdapter;
            }
            set
            {
                base.DataAdapter = value;
            }
        }

        /// <devnote>SqlServer only supports '.'</devnote>
        public override string QuotePrefix
        {
            get
            {
                return base.QuotePrefix;
            }
            set
            {
                if (("[" != value) && ("\"" != value))
                {
                    throw ADP.DoubleValuedProperty(nameof(QuotePrefix), "[", "\"");
                }
                base.QuotePrefix = value;
            }
        }

        public override string QuoteSuffix
        {
            get
            {
                return base.QuoteSuffix;
            }
            set
            {
                if (("]" != value) && ("\"" != value))
                {
                    throw ADP.DoubleValuedProperty(nameof(QuoteSuffix), "]", "\"");
                }
                base.QuoteSuffix = value;
            }
        }

        public override string SchemaSeparator
        {
            get
            {
                return ".";
            }
            set
            {
                if ("." != value)
                {
                    throw ADP.SingleValuedProperty(nameof(SchemaSeparator), ".");
                }
            }
        }

        private void SqlRowUpdatingHandler(object sender, SqlRowUpdatingEventArgs ruevent)
        {
            base.RowUpdatingHandler(ruevent);
        }

        new public SqlCommand GetInsertCommand()
            => (SqlCommand)base.GetInsertCommand();

        new public SqlCommand GetInsertCommand(bool useColumnsForParameterNames)
            => (SqlCommand)base.GetInsertCommand(useColumnsForParameterNames);

        new public SqlCommand GetUpdateCommand()
            => (SqlCommand)base.GetUpdateCommand();

        new public SqlCommand GetUpdateCommand(bool useColumnsForParameterNames)
            => (SqlCommand)base.GetUpdateCommand(useColumnsForParameterNames);

        new public SqlCommand GetDeleteCommand()
            => (SqlCommand)base.GetDeleteCommand();

        new public SqlCommand GetDeleteCommand(bool useColumnsForParameterNames)
            => (SqlCommand)base.GetDeleteCommand(useColumnsForParameterNames);

        protected override void ApplyParameterInfo(DbParameter parameter, DataRow datarow, StatementType statementType, bool whereClause)
        {
            SqlParameter p = (SqlParameter)parameter;
            object valueType = datarow[SchemaTableColumn.ProviderType];
            p.SqlDbType = (SqlDbType)valueType;
            p.Offset = 0;

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

        protected override string GetParameterName(int parameterOrdinal)
            => ("@p" + parameterOrdinal.ToString(CultureInfo.InvariantCulture));

        protected override string GetParameterName(string parameterName)
            => ("@" + parameterName);

        protected override string GetParameterPlaceholder(int parameterOrdinal)
            => ("@p" + parameterOrdinal.ToString(CultureInfo.InvariantCulture));

        private void ConsistentQuoteDelimiters(string quotePrefix, string quoteSuffix)
        {
            Debug.Assert(quotePrefix == "\"" || quotePrefix == "[");
            if ((("\"" == quotePrefix) && ("\"" != quoteSuffix)) ||
                (("[" == quotePrefix) && ("]" != quoteSuffix)))
            {
                throw ADP.InvalidPrefixSuffix();
            }
        }

        public static void DeriveParameters(SqlCommand command)
        {
            if (null == command)
            {
                throw ADP.ArgumentNull(nameof(command));
            }

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                command.DeriveParameters();
            }
            catch (OutOfMemoryException e)
            {
                command?.Connection?.Abort(e);
                throw;
            }
            catch (StackOverflowException e)
            {
                command?.Connection?.Abort(e);
                throw;
            }
            catch (ThreadAbortException e)
            {
                command?.Connection?.Abort(e);
                throw;
            }
        }

        protected override DataTable GetSchemaTable(DbCommand srcCommand)
        {
            SqlCommand sqlCommand = srcCommand as SqlCommand;
            SqlNotificationRequest notificationRequest = sqlCommand.Notification;

            sqlCommand.Notification = null;

            try
            {
                using (SqlDataReader dataReader = sqlCommand.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo))
                {
                    return dataReader.GetSchemaTable();
                }
            }
            finally
            {
                sqlCommand.Notification = notificationRequest;
            }

        }

        protected override DbCommand InitializeCommand(DbCommand command)
            => (SqlCommand)base.InitializeCommand(command);

        public override string QuoteIdentifier(string unquotedIdentifier)
        {
            ADP.CheckArgumentNull(unquotedIdentifier, nameof(unquotedIdentifier));
            string quoteSuffixLocal = QuoteSuffix;
            string quotePrefixLocal = QuotePrefix;
            ConsistentQuoteDelimiters(quotePrefixLocal, quoteSuffixLocal);
            return ADP.BuildQuotedString(quotePrefixLocal, quoteSuffixLocal, unquotedIdentifier);
        }

        protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
        {
            Debug.Assert(adapter is SqlDataAdapter, "Adapter is not a SqlDataAdapter.");
            if (adapter == base.DataAdapter)
            { // removal case
                ((SqlDataAdapter)adapter).RowUpdating -= SqlRowUpdatingHandler;
            }
            else
            { // adding case
                ((SqlDataAdapter)adapter).RowUpdating += SqlRowUpdatingHandler;
            }
        }

        public override string UnquoteIdentifier(string quotedIdentifier)
        {
            ADP.CheckArgumentNull(quotedIdentifier, nameof(quotedIdentifier));
            string unquotedIdentifier;
            string quoteSuffixLocal = QuoteSuffix;
            string quotePrefixLocal = QuotePrefix;
            ConsistentQuoteDelimiters(quotePrefixLocal, quoteSuffixLocal);
            // ignoring the return value becasue an unquoted source string is OK here
            ADP.RemoveStringQuotes(quotePrefixLocal, quoteSuffixLocal, quotedIdentifier, out unquotedIdentifier);
            return unquotedIdentifier;
        }
    }
}
