// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

using Microsoft.SqlServer.TDS.ColMetadata;
using Microsoft.SqlServer.TDS.Done;
using Microsoft.SqlServer.TDS.EndPoint;
using Microsoft.SqlServer.TDS.Row;
using Microsoft.SqlServer.TDS.SQLBatch;
using Microsoft.SqlServer.TDS.Info;

namespace Microsoft.SqlServer.TDS.Servers
{
    /// <summary>
    /// Class that pretends to be a full featured relational engine and returns predefined responses to well-known queries that Topology framework issues
    /// </summary>
    public class QueryEngine
    {
        /// <summary>
        /// Log to which send TDS conversation
        /// </summary>
        public TextWriter Log { get; set; }

        /// <summary>
        /// Server configuration
        /// </summary>
        public TDSServerArguments ServerArguments { get; private set; }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public QueryEngine(TDSServerArguments arguments)
        {
            ServerArguments = arguments;
        }

        /// <summary>
        /// Execute the query and produce a response
        /// </summary>
        public TDSMessageCollection ExecuteBatch(ITDSServerSession session, TDSMessage request)
        {
            // Get the batch from the tokens
            TDSSQLBatchToken batchRequest = request[0] as TDSSQLBatchToken;

            // Log request
            TDSUtilities.Log(Log, "Request", batchRequest);

            // Increase the counter of connection reset requests if the message contains the proper flags
            if (request.IsResetConnectionRequestSet || request.IsResetConnectionSkipTransactionRequestSet)
            {
                session.ConnectionResetRequestCount++;
            }

            // Prepare response message
            return CreateQueryResponse(session, batchRequest);
        }

        /// <summary>
        /// Create a response for the query
        /// </summary>
        /// <param name="session"></param>
        /// <param name="batchRequest"></param>
        /// <returns></returns>
        protected virtual TDSMessageCollection CreateQueryResponse(ITDSServerSession session, TDSSQLBatchToken batchRequest)
        {
            TDSMessage responseMessage = new TDSMessage(TDSMessageType.Response);

            // Prepare query text
            string lowerBatchText = batchRequest.Text.ToLowerInvariant();

            // Check query
            if (lowerBatchText.Contains("serverproperty('servername')"))  // SELECT convert(nvarchar(256), ServerProperty('ServerName'))
            {
                // Delegate to server name query
                responseMessage = _PrepareServerNameResponse(session);
            }
            if (lowerBatchText.Contains("serverproperty('machinename')"))  // SELECT convert(nvarchar(256), ServerProperty('MachineName'))
            {
                // Delegate to server name query
                responseMessage = _PrepareMachineNameResponse(session);
            }
            else if (lowerBatchText.Contains("serverproperty('instancename')"))  // SELECT convert(nvarchar(256), ServerProperty('InstanceName'))
            {
                // Delegate to instance name query
                responseMessage = _PrepareInstanceNameResponse(session);
            }
            else if (lowerBatchText.Contains("serverproperty('ishadrenabled')"))  // SELECT convert(bit, ServerProperty('IsHADREnabled')) 
            {
                // Delegate to HADRon query
                responseMessage = _PrepareIsHADRResponse(session);
            }
            else if (lowerBatchText.Contains("serverproperty('engineedition')"))  // SELECT convert(int, ServerProperty('EngineEdition'))
            {
                // Delegate to Azure query
                responseMessage = _PrepareIsAzure(session);
            }
            else if (lowerBatchText.Contains("serverproperty('islocaldb')"))  // SELECT convert(bit, ServerProperty('IsLocalDB')) 
            {
                // Delegate to Local DB query
                responseMessage = _PrepareIsLocalDB(session);
            }
            else if (lowerBatchText.Contains("serverproperty('istestsqlserver')"))  // SELECT convert(bit, ServerProperty('IsTestSQLServer')) 
            {
                // Delegate to test SQL Server query response
                responseMessage = _PrepareIsTestSQLServerResponse(session);
            }
            else if (lowerBatchText.Contains("serverproperty('testsqlserverclass')"))  // SELECT convert(nvarchar(256), ServerProperty('TestSQLServerClass')) 
            {
                // Delegate to test SQL Server class response
                responseMessage = _PrepareTestSQLServerClassResponse(session);
            }
            else if (lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("login_name")
                && lowerBatchText.Contains("nt_domain")
                && lowerBatchText.Contains("nt_user_name")
                && lowerBatchText.Contains("session_id"))  // SELECT login_name, nt_domain, nt_user_name FROM sys.dm_exec_sessions WHERE session_id = @@spid
            {
                // Delegate to session user query
                responseMessage = _PrepareSessionUserResponse(session);
            }
            else if (lowerBatchText.Contains("sp_oledb_ro_usrname"))  // exec [sys].sp_oledb_ro_usrname
            {
                // Delegate to OLE DB query
                responseMessage = _PrepareOleDbReadOnlyUserName(session);
            }
            else if (lowerBatchText.Contains("physical_net_transport")
                && lowerBatchText.Contains("local_net_address")
                && lowerBatchText.Contains("local_tcp_port"))  // SELECT convert(nvarchar(40), ConnectionProperty('physical_net_transport')), convert(varchar(48), ConnectionProperty('local_net_address')), convert(int, ConnectionProperty('local_tcp_port'))
            {
                // Delegate to connection information query
                responseMessage = _PrepareConnectionInfoResponse(session);
            }
            else if (lowerBatchText.Contains("dm_exec_connections")
                && lowerBatchText.Contains("encrypt_option")
                && lowerBatchText.Contains("session_id"))  // SELECT TOP (1) [encrypt_option] FROM [sys].[dm_exec_connections] WHERE [session_id] = @@SPID
            {
                // Delegate to encryption information query
                responseMessage = _PrepareEncryptionInfoResponse(session);
            }
            else if (lowerBatchText.Contains("select 1"))  // SELECT 1
            {
                // Delegate to server ping response
                responseMessage = _PreparePingResponse(session);
            }
            else if (lowerBatchText.Contains("name")
                && lowerBatchText.Contains("state")
                && lowerBatchText.Contains("databases")
                && lowerBatchText.Contains("db_name"))  // SELECT [name], [state] FROM [sys].[databases] WHERE [name] = db_name()
            {
                // Delegate to current database response
                responseMessage = _PrepareDatabaseResponse(session);
            }
            else if (lowerBatchText.Contains("dbcc")
                && lowerBatchText.Contains("tracestatus"))   // dbcc tracestatus()
            {
                // Delegate to current configuration response
                responseMessage = _PrepareConfigurationResponse(session);
            }
            else if (lowerBatchText.Contains("connectionproperty('sp_reset_connection_count')"))  // select CONNECTIONPROPERTY('sp_reset_connection_count')
            {
                // Delegate to reset connection request response
                responseMessage = _PrepareConnectionResetRequestCountResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("convert")
                && lowerBatchText.Contains("nvarchar(128)")
                && lowerBatchText.Contains("@@spid"))  // SELECT convert(nvarchar(128), @@SPID)
            {
                // Delegate to reset connection request response
                responseMessage = _PrepareSPIDResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("auth_scheme")
                && lowerBatchText.Contains("dm_exec_connections")) //select top (1) [auth_scheme] from [sys].[dm_exec_connections] where [session_id] = @@spid
            {
                // Delegate to reset connection request response
                responseMessage = _PrepareAuthSchemeResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("ansi_defaults")
                && lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("session_id")
                && lowerBatchText.Contains("@@spid"))  // SELECT TOP (1) [ansi_defaults] FROM [sys].[dm_exec_sessions] WHERE [session_id] = @@SPID
            {
                // Delegate session property query
                responseMessage = _PrepareAnsiDefaultsResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("ansi_null_dflt_on")
                && lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("session_id")
                && lowerBatchText.Contains("@@spid"))  // SELECT TOP (1) [ansi_null_dflt_on] FROM [sys].[dm_exec_sessions] WHERE [session_id] = @@SPID
            {
                // Delegate session property query
                responseMessage = _PrepareAnsiNullDefaultOnResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("ansi_nulls")
                && lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("session_id")
                && lowerBatchText.Contains("@@spid"))  // SELECT TOP (1) [ansi_nulls] FROM [sys].[dm_exec_sessions] WHERE [session_id] = @@SPID
            {
                // Delegate session property query
                responseMessage = _PrepareAnsiNullsResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("ansi_padding")
                && lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("session_id")
                && lowerBatchText.Contains("@@spid"))  // SELECT TOP (1) [ansi_padding] FROM [sys].[dm_exec_sessions] WHERE [session_id] = @@SPID
            {
                // Delegate session property query
                responseMessage = _PrepareAnsiPaddingResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("ansi_warnings")
                && lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("session_id")
                && lowerBatchText.Contains("@@spid"))  // SELECT TOP (1) [ansi_warnings] FROM [sys].[dm_exec_sessions] WHERE [session_id] = @@SPID
            {
                // Delegate session property query
                responseMessage = _PrepareAnsiWarningsResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("arithabort")
                && lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("session_id")
                && lowerBatchText.Contains("@@spid"))  // SELECT TOP (1) [arithabort] FROM [sys].[dm_exec_sessions] WHERE [session_id] = @@SPID
            {
                // Delegate session property query
                responseMessage = _PrepareArithAbortResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("concat_null_yields_null")
                && lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("session_id")
                && lowerBatchText.Contains("@@spid"))  // SELECT TOP (1) [concat_null_yields_null] FROM [sys].[dm_exec_sessions] WHERE [session_id] = @@SPID
            {
                // Delegate session property query
                responseMessage = _PrepareConcatNullYieldsNullResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("date_first")
                && lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("session_id")
                && lowerBatchText.Contains("@@spid"))  // SELECT TOP (1) [date_first] FROM [sys].[dm_exec_sessions] WHERE [session_id] = @@SPID
            {
                // Delegate session property query
                responseMessage = _PrepareDateFirstResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("date_format")
                && lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("session_id")
                && lowerBatchText.Contains("@@spid"))  // SELECT TOP (1) [date_format] FROM [sys].[dm_exec_sessions] WHERE [session_id] = @@SPID
            {
                // Delegate session property query
                responseMessage = _PrepareDateFormatResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("deadlock_priority")
                && lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("session_id")
                && lowerBatchText.Contains("@@spid"))  // SELECT TOP (1) [deadlock_priority] FROM [sys].[dm_exec_sessions] WHERE [session_id] = @@SPID
            {
                // Delegate session property query
                responseMessage = _PrepareDeadlockPriorityResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("language")
                && lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("session_id")
                && lowerBatchText.Contains("@@spid"))  // SELECT TOP (1) [language] FROM [sys].[dm_exec_sessions] WHERE [session_id] = @@SPID
            {
                // Delegate session property query
                responseMessage = _PrepareLanguageResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("lock_timeout")
                && lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("session_id")
                && lowerBatchText.Contains("@@spid"))  // SELECT TOP (1) [lock_timeout] FROM [sys].[dm_exec_sessions] WHERE [session_id] = @@SPID
            {
                // Delegate session property query
                responseMessage = _PrepareLockTimeoutResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("quoted_identifier")
                && lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("session_id")
                && lowerBatchText.Contains("@@spid"))  // SELECT TOP (1) [quoted_identifier] FROM [sys].[dm_exec_sessions] WHERE [session_id] = @@SPID
            {
                // Delegate session property query
                responseMessage = _PrepareQuotedIdentifierResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("text_size")
                && lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("session_id")
                && lowerBatchText.Contains("@@spid"))  // SELECT TOP (1) [text_size] FROM [sys].[dm_exec_sessions] WHERE [session_id] = @@SPID
            {
                // Delegate session property query
                responseMessage = _PrepareTextSizeResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("transaction_isolation_level")
                && lowerBatchText.Contains("dm_exec_sessions")
                && lowerBatchText.Contains("session_id")
                && lowerBatchText.Contains("@@spid"))  // SELECT TOP (1) [transaction_isolation_level] FROM [sys].[dm_exec_sessions] WHERE [session_id] = @@SPID
            {
                // Delegate session property query
                responseMessage = _PrepareTransactionIsolationLevelResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("@@options"))  // SELECT @@OPTIONS
            {
                // Delegate session property query
                responseMessage = _PrepareOptionsResponse(session);
            }
            else if (lowerBatchText.Contains("select")
                && lowerBatchText.Contains("context_info()"))  // SELECT CONTEXT_INFO() 
            {
                // Delegate session property query
                responseMessage = _PrepareContextInfoResponse(session);
            }
            else
            {
                // Create an info token that contains the query received
                TDSInfoToken infoToken = new TDSInfoToken(2012, 2, 0, lowerBatchText);

                // Log response
                TDSUtilities.Log(Log, "Response", infoToken);

                // Serialize DONE token into the response packet
                responseMessage.Add(infoToken);

                // Create an info token to let client know that we don't recognize this query
                infoToken = new TDSInfoToken(2012, 2, 0, "Received query is not recognized by the query engine. Please ask a very specific question.");

                // Log response
                TDSUtilities.Log(Log, "Response", infoToken);

                // Serialize DONE token into the response packet
                responseMessage.Add(infoToken);

                // Create generic DONE token
                TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final);

                // Log response
                TDSUtilities.Log(Log, "Response", doneToken);

                // Serialize DONE token into the response packet
                responseMessage.Add(doneToken);
            }

            // Response collection will contain only one message
            return new TDSMessageCollection(responseMessage);
        }


        /// <summary>
        /// Handle attention from the client
        /// </summary>
        public TDSMessageCollection ExecuteAttention(ITDSServerSession session, TDSMessage request)
        {
            // Create attention DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Attention);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Return a single message with DONE token only
            return new TDSMessageCollection(new TDSMessage(TDSMessageType.Response, doneToken));
        }

        /// <summary>
        /// Prepare response for server name query
        /// </summary>
        private TDSMessage _PrepareServerNameResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start first column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.NVarChar;
            column.DataTypeSpecific = new TDSShilohVarCharColumnSpecific(256, new TDSColumnDataCollation(13632521, 52));
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Add row
            rowToken.Data.Add(ServerArguments.ServerName);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for machine name query
        /// </summary>
        private TDSMessage _PrepareMachineNameResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start first column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.NVarChar;
            column.DataTypeSpecific = new TDSShilohVarCharColumnSpecific(256, new TDSColumnDataCollation(13632521, 52));
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Add row
            rowToken.Data.Add(Environment.MachineName.ToUpper());

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for server instance name query
        /// </summary>
        private TDSMessage _PrepareInstanceNameResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start first column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.NVarChar;
            column.DataTypeSpecific = new TDSShilohVarCharColumnSpecific(256, new TDSColumnDataCollation(13632521, 52));
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Start with server name
            string value = ServerArguments.ServerName;

            // Check if server name contains a slash
            if (value.Contains("\\"))
            {
                // Take everything after the slash
                value = value.Substring(value.IndexOf('\\') + 1);
            }
            else
            {
                // Instance is unnamed
                value = null;
            }

            // Add row
            rowToken.Data.Add(value);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for user nane query that OLE DB stack dispatches upon connection
        /// </summary>
        private TDSMessage _PrepareOleDbReadOnlyUserName(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start first column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.BigVarChar;
            column.DataTypeSpecific = new TDSShilohVarCharColumnSpecific(1, new TDSColumnDataCollation(13632521, 52));
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Start second column
            column = new TDSColumnData();
            column.DataType = TDSDataType.NVarChar;
            column.DataTypeSpecific = new TDSShilohVarCharColumnSpecific(128, new TDSColumnDataCollation(13632521, 52));
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Add row
            rowToken.Data.Add("N");
            rowToken.Data.Add("dbo");

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for query whether this is a HADRon instance
        /// </summary>
        private TDSMessage _PrepareIsHADRResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.BitN;
            column.DataTypeSpecific = (byte)1;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Flags.IsComputed = true;
            column.Flags.IsNullable = true;  // TODO: Must be nullable, otherwise something is wrong with SqlClient

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Add a data row that indicates that this is not a HADRon server
            rowToken.Data.Add(false);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for query whether this is a SQL Azure instance
        /// </summary>
        private TDSMessage _PrepareIsAzure(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.IntN;
            column.DataTypeSpecific = (byte)4;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Flags.IsComputed = true;
            column.Flags.IsNullable = true;  // TODO: Must be nullable, otherwise something is wrong with SqlClient

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Per https://docs.microsoft.com/en-us/sql/t-sql/functions/serverproperty-transact-sql
            // 4 = Express (This is returned for Express, Express with Advanced Services, and Windows Embedded SQL.)
            rowToken.Data.Add(4);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for query whether this is a Local DB instance
        /// </summary>
        private TDSMessage _PrepareIsLocalDB(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.BitN;
            column.DataTypeSpecific = (byte)1;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Flags.IsComputed = true;
            column.Flags.IsNullable = true;  // TODO: Must be nullable, otherwise something is wrong with SqlClient

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Add a data row that indicates that this is not a Local DB server
            rowToken.Data.Add(false);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for query whether this is a test SQL Server instance
        /// </summary>
        private TDSMessage _PrepareIsTestSQLServerResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.BitN;
            column.DataTypeSpecific = (byte)1;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Flags.IsComputed = true;
            column.Flags.IsNullable = true;  // TODO: Must be nullable, otherwise something is wrong with SqlClient

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Add a data row that indicates that this is a test SQL Server
            rowToken.Data.Add(true);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for query of class of the SQL Server instance
        /// </summary>
        private TDSMessage _PrepareTestSQLServerClassResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start first column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.NVarChar;
            column.DataTypeSpecific = new TDSShilohVarCharColumnSpecific(256, new TDSColumnDataCollation(13632521, 52));
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Get the server class
            rowToken.Data.Add(session.Server.GetType().FullName);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for session user name query
        /// </summary>
        private TDSMessage _PrepareSessionUserResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Prepare column type-specific data
            TDSShilohVarCharColumnSpecific nVarChar = new TDSShilohVarCharColumnSpecific(256, new TDSColumnDataCollation(13632521, 52));

            // Prepare the first column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.NVarChar;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.Unknown;
            column.Flags.IsNullable = true;
            column.DataTypeSpecific = nVarChar;
            column.Name = "nt_user_name";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Prepare the second column
            column = new TDSColumnData();
            column.DataType = TDSDataType.NVarChar;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.Unknown;
            column.Flags.IsNullable = true;
            column.DataTypeSpecific = nVarChar;
            column.Name = "nt_domain";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Prepare the third column
            column = new TDSColumnData();
            column.DataType = TDSDataType.NVarChar;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.Unknown;
            column.DataTypeSpecific = nVarChar;
            column.Name = "login_name";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Check user type
            if (session.SQLUserID != null)
            {
                // Add row
                rowToken.Data.Add(null);  // nt_user_name
                rowToken.Data.Add(null);  // nt_domain
                rowToken.Data.Add(session.SQLUserID);  // login_name
            }
            else if (session.NTUserAuthenticationContext != null)
            {
                // Get user identifier
                string userID = session.NTUserAuthenticationContext.GetRemoteIdentity().Name;

                // Look for traditional separator for form "<domain>\<user>"
                int indexOfSeparator = userID.IndexOf('\\');

                string domain = null;
                string user = null;

                // Parse domain and user out of the entry
                if (indexOfSeparator != -1)
                {
                    // Extract parts
                    domain = userID.Substring(0, indexOfSeparator);
                    user = userID.Substring(indexOfSeparator + 1);
                }
                else
                {
                    // Look for a different type of separator for form "<user>@<domain>"
                    indexOfSeparator = userID.IndexOf('@');

                    // Check if found
                    if (indexOfSeparator != -1)
                    {
                        // Extract parts
                        domain = userID.Substring(indexOfSeparator + 1);
                        user = userID.Substring(0, indexOfSeparator);
                    }
                    else
                    {
                        // We don't recognize this user so don't parse it
                        domain = null;
                        user = userID;
                    }
                }

                // Add row
                rowToken.Data.Add(user);  // nt_user_name
                rowToken.Data.Add(domain);  // nt_domain
                rowToken.Data.Add(userID);  // login_name
            }
            else
            {
                // We don't have a user, which is very strange since we're in query engine already
                rowToken.Data.Add(null);  // nt_user_name
                rowToken.Data.Add(null);  // nt_domain
                rowToken.Data.Add(null);  // login_name
            }

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response to the query about connection end-point
        /// </summary>
        private TDSMessage _PrepareConnectionInfoResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start the first column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.NVarChar;
            column.DataTypeSpecific = new TDSShilohVarCharColumnSpecific(40, new TDSColumnDataCollation(13632521, 52));
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "net_transport";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Start the second column
            column = new TDSColumnData();
            column.DataType = TDSDataType.BigVarChar;
            column.DataTypeSpecific = new TDSShilohVarCharColumnSpecific(48, new TDSColumnDataCollation(13632521, 52));
            column.Flags.IsNullable = true;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "local_net_address";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Start the third column
            column = new TDSColumnData();
            column.DataType = TDSDataType.IntN;
            column.DataTypeSpecific = (byte)4;
            column.Flags.IsNullable = true;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "local_tcp_port";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Add row
            rowToken.Data.Add(session.ServerEndPointInfo.Transport.ToString());
            rowToken.Data.Add(session.ServerEndPointInfo.Address.ToString());
            rowToken.Data.Add(session.ServerEndPointInfo.Port);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response to the query about connection encryption
        /// </summary>
        private TDSMessage _PrepareEncryptionInfoResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start the first column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.NVarChar;
            column.DataTypeSpecific = new TDSShilohVarCharColumnSpecific(40, new TDSColumnDataCollation(13632521, 52));
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "encrypt_option";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Check if encryption is enabled
            if (session.Encryption == TDSEncryptionType.Full)
            {
                // Add row
                rowToken.Data.Add("TRUE");
            }
            else
            {
                // Add row
                rowToken.Data.Add("FALSE");
            }

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response to server ping
        /// </summary>
        private TDSMessage _PreparePingResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start the first column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.IntN;
            column.DataTypeSpecific = (byte)4;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Flags.IsNullable = true;  // TODO: Must be nullable, otherwise something is wrong with SqlClient
            column.Flags.IsComputed = true;

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Add row
            rowToken.Data.Add((int)1);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare current database response
        /// </summary>
        private TDSMessage _PrepareDatabaseResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start the first column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.NVarChar;
            column.DataTypeSpecific = new TDSShilohVarCharColumnSpecific(256, new TDSColumnDataCollation(13632521, 52));
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "name";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Start the second column
            column = new TDSColumnData();
            column.DataType = TDSDataType.IntN;
            column.DataTypeSpecific = (byte)1;
            column.Flags.IsNullable = true;
            column.Name = "state";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Add row
            rowToken.Data.Add(session.Database);
            rowToken.Data.Add((byte)0);  // Online

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare configuration response
        /// </summary>
        private TDSMessage _PrepareConfigurationResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start the first column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.IntN;
            column.DataTypeSpecific = (byte)2;
            column.Flags.IsNullable = true;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "TraceFlag";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Start the second column
            column = new TDSColumnData();
            column.DataType = TDSDataType.IntN;
            column.DataTypeSpecific = (byte)2;
            column.Flags.IsNullable = true;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "Status";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Start the third column
            column = new TDSColumnData();
            column.DataType = TDSDataType.IntN;
            column.DataTypeSpecific = (byte)2;
            column.Flags.IsNullable = true;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "Global";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Start the fourth column
            column = new TDSColumnData();
            column.DataType = TDSDataType.IntN;
            column.DataTypeSpecific = (byte)2;
            column.Flags.IsNullable = true;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "Session";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 0);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, doneToken);
        }

        /// <summary>
        /// Prepare response to connection reset request count
        /// </summary>
        private TDSMessage _PrepareConnectionResetRequestCountResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start the second column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.IntN;
            column.DataTypeSpecific = (byte)4;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Flags.IsComputed = true;
            column.Flags.IsNullable = true;  // TODO: Must be nullable, otherwise something is wrong with SqlClient
            column.Name = "ConnectionResetRequestCount";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Add row data            
            rowToken.Data.Add(session.ConnectionResetRequestCount);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response to connection reset request count
        /// </summary>
        private TDSMessage _PrepareSPIDResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start the first column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.NVarChar;
            column.DataTypeSpecific = new TDSShilohVarCharColumnSpecific(128, new TDSColumnDataCollation(13632521, 52));
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Flags.IsNullable = true;
            column.Flags.IsComputed = true;

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Add row
            rowToken.Data.Add(session.SessionID.ToString());

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response to connection reset request count
        /// </summary>
        private TDSMessage _PrepareAuthSchemeResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start the first column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.NVarChar;
            column.DataTypeSpecific = new TDSShilohVarCharColumnSpecific(40, new TDSColumnDataCollation(13632521, 52));
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "auth_scheme";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Check which authentication method are we using
            // @TODO add Federated Authentication once VSTS 1072394 is resolved
            if (session.SQLUserID != null)
            {
                // Add row
                rowToken.Data.Add("SQL");
            }
            else
            {
                // Add row
                rowToken.Data.Add("NTML");
            }

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for ANSI defaults
        /// </summary>
        private TDSMessage _PrepareAnsiDefaultsResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.Bit;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "ansi_defaults";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Read the value from the session
            rowToken.Data.Add((session as GenericTDSServerSession).AnsiDefaults);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for ANSI null default on
        /// </summary>
        private TDSMessage _PrepareAnsiNullDefaultOnResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.Bit;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "ansi_null_dflt_on";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Read the value from the session
            rowToken.Data.Add((session as GenericTDSServerSession).AnsiNullDefaultOn);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for ANSI nulls
        /// </summary>
        private TDSMessage _PrepareAnsiNullsResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.Bit;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "ansi_nulls";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Read the value from the session
            rowToken.Data.Add((session as GenericTDSServerSession).AnsiNulls);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for ANSI padding
        /// </summary>
        private TDSMessage _PrepareAnsiPaddingResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.Bit;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "ansi_padding";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Read the value from the session
            rowToken.Data.Add((session as GenericTDSServerSession).AnsiPadding);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for ANSI warnings
        /// </summary>
        private TDSMessage _PrepareAnsiWarningsResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.Bit;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "ansi_warnings";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Read the value from the session
            rowToken.Data.Add((session as GenericTDSServerSession).AnsiWarnings);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for arithmetic abort
        /// </summary>
        private TDSMessage _PrepareArithAbortResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.Bit;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "arithabort";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Read the value from the session
            rowToken.Data.Add((session as GenericTDSServerSession).ArithAbort);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for concatenation of nulls yields null
        /// </summary>
        private TDSMessage _PrepareConcatNullYieldsNullResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.Bit;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "concat_null_yields_null";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Read the value from the session
            rowToken.Data.Add((session as GenericTDSServerSession).ConcatNullYieldsNull);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for date first
        /// </summary>
        private TDSMessage _PrepareDateFirstResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.Int2;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "date_first";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Read the value from the session
            rowToken.Data.Add((short)(session as GenericTDSServerSession).DateFirst);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for date format
        /// </summary>
        private TDSMessage _PrepareDateFormatResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.NVarChar;
            column.DataTypeSpecific = new TDSShilohVarCharColumnSpecific(6, new TDSColumnDataCollation(13632521, 52));
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Flags.IsNullable = true;
            column.Name = "date_format";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Generate a date format string
            rowToken.Data.Add(DateFormatString.ToString((session as GenericTDSServerSession).DateFormat));

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for deadlock priority
        /// </summary>
        private TDSMessage _PrepareDeadlockPriorityResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.Int4;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "deadlock_priority";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Serialize the value from the session
            rowToken.Data.Add((session as GenericTDSServerSession).DeadlockPriority);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for language
        /// </summary>
        private TDSMessage _PrepareLanguageResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.NVarChar;
            column.DataTypeSpecific = new TDSShilohVarCharColumnSpecific(256, new TDSColumnDataCollation(13632521, 52));
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Flags.IsNullable = true;
            column.Name = "language";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Generate a date format string
            rowToken.Data.Add(LanguageString.ToString((session as GenericTDSServerSession).Language));

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for lock timeout
        /// </summary>
        private TDSMessage _PrepareLockTimeoutResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.Int4;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "lock_timeout";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Serialize the value from the session
            rowToken.Data.Add((session as GenericTDSServerSession).LockTimeout);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for quoted identifier
        /// </summary>
        private TDSMessage _PrepareQuotedIdentifierResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.Bit;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "quoted_identifier";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Read the value from the session
            rowToken.Data.Add((session as GenericTDSServerSession).QuotedIdentifier);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for text size
        /// </summary>
        private TDSMessage _PrepareTextSizeResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.Int4;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "text_size";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Read the value from the session
            rowToken.Data.Add((session as GenericTDSServerSession).TextSize);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for transaction isolation level
        /// </summary>
        private TDSMessage _PrepareTransactionIsolationLevelResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.Int2;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Name = "transaction_isolation_level";

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Read the value from the session
            rowToken.Data.Add((short)(session as GenericTDSServerSession).TransactionIsolationLevel);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for options
        /// </summary>
        private TDSMessage _PrepareOptionsResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.Int4;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;
            column.Flags.IsComputed = true;

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Convert to generic session
            GenericTDSServerSession genericSession = session as GenericTDSServerSession;

            // Serialize the options into the bit mask
            int options = 0;

            // Check transaction abort on error
            if (genericSession.TransactionAbortOnError)
            {
                options |= 0x4000;
            }

            // Check numeric round abort
            if (genericSession.NumericRoundAbort)
            {
                options |= 0x2000;
            }

            // Check concatenation of nulls yields null
            if (genericSession.ConcatNullYieldsNull)
            {
                options |= 0x1000;
            }

            // Check ansi null default OFF
            if (!genericSession.AnsiNullDefaultOn)
            {
                options |= 0x800;
            }

            // Check ansi null default ON
            if (genericSession.AnsiNullDefaultOn)
            {
                options |= 0x400;
            }

            // Check no count
            if (genericSession.NoCount)
            {
                options |= 0x200;
            }

            // Check quoted identifier
            if (genericSession.QuotedIdentifier)
            {
                options |= 0x100;
            }

            // Check arithmetic ignore
            if (genericSession.ArithIgnore)
            {
                options |= 0x80;
            }

            // Check arithmetic abort
            if (genericSession.ArithAbort)
            {
                options |= 0x40;
            }

            // Check ansi nulls
            if (genericSession.AnsiNulls)
            {
                options |= 0x20;
            }

            // Check ansi padding
            if (genericSession.AnsiPadding)
            {
                options |= 0x10;
            }

            // Check ansi warnings
            if (genericSession.AnsiWarnings)
            {
                options |= 0x8;
            }

            // Check cursor close on commit
            if (genericSession.CursorCloseOnCommit)
            {
                options |= 0x4;
            }

            // Check implicit transactions
            if (genericSession.ImplicitTransactions)
            {
                options |= 0x2;
            }

            // Read the value from the session
            rowToken.Data.Add(options);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }

        /// <summary>
        /// Prepare response for context info
        /// </summary>
        private TDSMessage _PrepareContextInfoResponse(ITDSServerSession session)
        {
            // Prepare result metadata
            TDSColMetadataToken metadataToken = new TDSColMetadataToken();

            // Start a new column
            TDSColumnData column = new TDSColumnData();
            column.DataType = TDSDataType.BigVarBinary;
            column.DataTypeSpecific = (ushort)128;
            column.Flags.Updatable = TDSColumnDataUpdatableFlag.ReadOnly;

            // Add a column to the response
            metadataToken.Columns.Add(column);

            // Log response
            TDSUtilities.Log(Log, "Response", metadataToken);

            // Prepare result data
            TDSRowToken rowToken = new TDSRowToken(metadataToken);

            // Prepare context info container
            byte[] contextInfo = null;

            // Check if session has a context info
            if ((session as GenericTDSServerSession).ContextInfo != null)
            {
                // Allocate a container
                contextInfo = new byte[128];

                // Copy context info into the container
                Array.Copy((session as GenericTDSServerSession).ContextInfo, contextInfo, (session as GenericTDSServerSession).ContextInfo.Length);
            }

            // Set context info
            rowToken.Data.Add(contextInfo);

            // Log response
            TDSUtilities.Log(Log, "Response", rowToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);

            // Log response
            TDSUtilities.Log(Log, "Response", doneToken);

            // Serialize tokens into the message
            return new TDSMessage(TDSMessageType.Response, metadataToken, rowToken, doneToken);
        }
    }
}
