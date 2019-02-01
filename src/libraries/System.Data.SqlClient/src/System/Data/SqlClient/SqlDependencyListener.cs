// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.ProviderBase;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Threading;
using System.Xml;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;

// This class is the process wide dependency dispatcher.  It contains all connection listeners for the entire process and 
// receives notifications on those connections to dispatch to the corresponding AppDomain dispatcher to notify the
// appropriate dependencies.

internal class SqlDependencyProcessDispatcher : MarshalByRefObject
{
    // Class to contain/store all relevant information about a connection that waits on the SSB queue.
    private class SqlConnectionContainer
    {
        private SqlConnection _con;
        private SqlCommand _com;
        private SqlParameter _conversationGuidParam;
        private SqlParameter _timeoutParam;
        private SqlConnectionContainerHashHelper _hashHelper;
        private string _queue;
        private string _receiveQuery;
        private string _beginConversationQuery;
        private string _endConversationQuery;
        private string _concatQuery;
        private readonly int _defaultWaitforTimeout = 60000; // Waitfor(Receive) timeout (milleseconds)
        private string _escapedQueueName;
        private string _sprocName;
        private string _dialogHandle;
        private string _cachedServer;
        private string _cachedDatabase;
        private volatile bool _errorState = false;
        private volatile bool _stop = false; // Can probably simplify this slightly - one bool instead of two.
        private volatile bool _stopped = false;
        private volatile bool _serviceQueueCreated = false;
        private int _startCount = 0;     // Each container class is called once per Start() - we refCount 
                                         // to track when we can dispose.
        private Timer _retryTimer = null;
        private Dictionary<string, int> _appDomainKeyHash = null;  // AppDomainKey->Open RefCount

        // Constructor

        internal SqlConnectionContainer(SqlConnectionContainerHashHelper hashHelper, string appDomainKey, bool useDefaults)
        {
            bool setupCompleted = false;
            try
            {
                _hashHelper = hashHelper;
                string guid = null;

                // If default, queue name is not present on hashHelper at this point - so we need to 
                // generate one and complete initialization.
                if (useDefaults)
                {
                    guid = Guid.NewGuid().ToString();
                    _queue = SQL.SqlNotificationServiceDefault + "-" + guid;
                    _hashHelper.ConnectionStringBuilder.ApplicationName = _queue; // Used by cleanup sproc.
                }
                else
                {
                    _queue = _hashHelper.Queue;
                }

                // Always use ConnectionStringBuilder since in default case it is different from the 
                // connection string used in the hashHelper.
                _con = new SqlConnection(_hashHelper.ConnectionStringBuilder.ConnectionString); // Create connection and open.

                // Assert permission for this particular connection string since it differs from the user passed string
                // which we have already demanded upon.  
                SqlConnectionString connStringObj = (SqlConnectionString)_con.ConnectionOptions;

                _con.Open();

                _cachedServer = _con.DataSource;

                _escapedQueueName = SqlConnection.FixupDatabaseTransactionName(_queue); // Properly escape to prevent SQL Injection.
                _appDomainKeyHash = new Dictionary<string, int>(); // Dictionary stores the Start/Stop refcount per AppDomain for this container.
                _com = new SqlCommand()
                {
                    Connection = _con,
                    // Determine if broker is enabled on current database.
                    CommandText = "select is_broker_enabled from sys.databases where database_id=db_id()"
                };

                if (!(bool)_com.ExecuteScalar())
                {
                    throw SQL.SqlDependencyDatabaseBrokerDisabled();
                }

                _conversationGuidParam = new SqlParameter("@p1", SqlDbType.UniqueIdentifier);
                _timeoutParam = new SqlParameter("@p2", SqlDbType.Int)
                {
                    Value = 0 // Timeout set to 0 for initial sync query.
                };
                _com.Parameters.Add(_timeoutParam);

                setupCompleted = true;
                // connection with the server has been setup - from this point use TearDownAndDispose() in case of error

                // Create standard query.
                _receiveQuery = "WAITFOR(RECEIVE TOP (1) message_type_name, conversation_handle, cast(message_body AS XML) as message_body from " + _escapedQueueName + "), TIMEOUT @p2;";

                // Create queue, service, sync query, and async query on user thread to ensure proper
                // init prior to return.

                if (useDefaults)
                { // Only create if user did not specify service & database.
                    _sprocName = SqlConnection.FixupDatabaseTransactionName(SQL.SqlNotificationStoredProcedureDefault + "-" + guid);
                    CreateQueueAndService(false); // Fail if we cannot create service, queue, etc.
                }
                else
                {
                    // Continue query setup.
                    _com.CommandText = _receiveQuery;
                    _endConversationQuery = "END CONVERSATION @p1; ";
                    _concatQuery = _endConversationQuery + _receiveQuery;
                }

                IncrementStartCount(appDomainKey, out bool ignored);
                // Query synchronously once to ensure everything is working correctly.
                // We want the exception to occur on start to immediately inform caller.
                SynchronouslyQueryServiceBrokerQueue();
                _timeoutParam.Value = _defaultWaitforTimeout; // Sync successful, extend timeout to 60 seconds.
                AsynchronouslyQueryServiceBrokerQueue();
            }
            catch (Exception e)
            {
                if (!ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }

                ADP.TraceExceptionWithoutRethrow(e); // Discard failure, but trace for now.
                if (setupCompleted)
                {
                    // Be sure to drop service & queue.  This may fail if create service & queue failed.
                    // This method will not drop unless we created or service & queue ref-count is 0.
                    TearDownAndDispose();
                }
                else
                {
                    // connection has not been fully setup yet - cannot use TearDownAndDispose();
                    // we have to dispose the command and the connection to avoid connection leaks (until GC collects them).
                    if (_com != null)
                    {
                        _com.Dispose();
                        _com = null;
                    }
                    if (_con != null)
                    {
                        _con.Dispose();
                        _con = null;
                    }

                }
                throw;
            }
        }

        // Properties

        internal string Database
        {
            get
            {
                if (_cachedDatabase == null)
                {
                    _cachedDatabase = _con.Database;
                }
                return _cachedDatabase;
            }
        }

        internal SqlConnectionContainerHashHelper HashHelper => _hashHelper;

        internal bool InErrorState => _errorState;

        internal string Queue => _queue;

        internal string Server => _cachedServer;

        // Methods

        // This function is called by a ThreadPool thread as a result of an AppDomain calling 
        // SqlDependencyProcessDispatcher.QueueAppDomainUnload on AppDomain.Unload.
        internal bool AppDomainUnload(string appDomainKey)
        {
            Debug.Assert(!string.IsNullOrEmpty(appDomainKey), "Unexpected empty appDomainKey!");

            // Dictionary used to track how many times start has been called per app domain.
            // For each decrement, subtract from count, and delete if we reach 0.
            lock (_appDomainKeyHash)
            {
                if (_appDomainKeyHash.ContainsKey(appDomainKey))
                { // Do nothing if AppDomain did not call Start!
                    int value = _appDomainKeyHash[appDomainKey];
                    Debug.Assert(value > 0, "Why is value 0 or less?");

                    bool ignored = false;
                    while (value > 0)
                    {
                        Debug.Assert(!_stopped, "We should not yet be stopped!");
                        Stop(appDomainKey, out ignored); // Stop will decrement value and remove if necessary from _appDomainKeyHash.
                        value--;
                    }

                    // Stop will remove key when decremented to 0 for this AppDomain, which should now be the case.
                    Debug.Assert(0 == value, "We did not reach 0 at end of loop in AppDomainUnload!");
                    Debug.Assert(!_appDomainKeyHash.ContainsKey(appDomainKey), "Key not removed after AppDomainUnload!");
                }
            }

            return _stopped;
        }

        private void AsynchronouslyQueryServiceBrokerQueue()
        {
            AsyncCallback callback = new AsyncCallback(AsyncResultCallback);
            _com.BeginExecuteReader(callback, null, CommandBehavior.Default); // NO LOCK NEEDED
        }

        private void AsyncResultCallback(IAsyncResult asyncResult)
        {
            try
            {
                using (SqlDataReader reader = _com.EndExecuteReader(asyncResult))
                {
                    ProcessNotificationResults(reader);
                }

                // Successfull completion of query - no errors.
                if (!_stop)
                {
                    AsynchronouslyQueryServiceBrokerQueue(); // Requeue...
                }
                else
                {
                    TearDownAndDispose();
                }
            }
            catch (Exception e)
            {
                if (!ADP.IsCatchableExceptionType(e))
                {
                    // Let the waiting thread detect the error and exit (otherwise, the Stop call loops forever)
                    _errorState = true;
                    throw;
                }

                if (!_stop)
                { // Only assert if not in cancel path.
                    ADP.TraceExceptionWithoutRethrow(e); // Discard failure, but trace for now.
                }

                // Failure - likely due to cancelled command.  Check _stop state.
                if (_stop)
                {
                    TearDownAndDispose();
                }
                else
                {
                    _errorState = true;
                    Restart(null); // Error code path.  Will Invalidate based on server if 1st retry fails.
                }
            }
        }

        private void CreateQueueAndService(bool restart)
        {
            SqlCommand com = new SqlCommand()
            {
                Connection = _con
            };
            SqlTransaction trans = null;

            try
            {
                trans = _con.BeginTransaction(); // Since we cannot batch proc creation, start transaction.
                com.Transaction = trans;

                string nameLiteral = SqlServerEscapeHelper.MakeStringLiteral(_queue);

                com.CommandText =
                        "CREATE PROCEDURE " + _sprocName + " AS"
                        + " BEGIN"
                            + " BEGIN TRANSACTION;"
                            + " RECEIVE TOP(0) conversation_handle FROM " + _escapedQueueName + ";"
                            + " IF (SELECT COUNT(*) FROM " + _escapedQueueName + " WHERE message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer') > 0"
                            + " BEGIN"
                                + " if ((SELECT COUNT(*) FROM sys.services WHERE name = " + nameLiteral + ") > 0)"
                                + "   DROP SERVICE " + _escapedQueueName + ";"
                                + " if (OBJECT_ID(" + nameLiteral + ", 'SQ') IS NOT NULL)"
                                + "   DROP QUEUE " + _escapedQueueName + ";"
                                + " DROP PROCEDURE " + _sprocName + ";" // Don't need conditional because this is self
                            + " END"
                            + " COMMIT TRANSACTION;"
                        + " END";

                if (!restart)
                {
                    com.ExecuteNonQuery();
                }
                else
                { // Upon restart, be resilient to the user dropping queue, service, or procedure.
                    try
                    {
                        com.ExecuteNonQuery(); // Cannot add 'IF OBJECT_ID' to create procedure query - wrap and discard failure.
                    }
                    catch (Exception e)
                    {
                        if (!ADP.IsCatchableExceptionType(e))
                        {
                            throw;
                        }
                        ADP.TraceExceptionWithoutRethrow(e);

                        try
                        { // Since the failure will result in a rollback, rollback our object.
                            if (null != trans)
                            {
                                trans.Rollback();
                                trans = null;
                            }
                        }
                        catch (Exception f)
                        {
                            if (!ADP.IsCatchableExceptionType(f))
                            {
                                throw;
                            }
                            ADP.TraceExceptionWithoutRethrow(f); // Discard failure, but trace for now.
                        }
                    }

                    if (null == trans)
                    { // Create a new transaction for next operations.
                        trans = _con.BeginTransaction();
                        com.Transaction = trans;
                    }
                }


                com.CommandText =
                         "IF OBJECT_ID(" + nameLiteral + ", 'SQ') IS NULL"
                            + " BEGIN"
                            + " CREATE QUEUE " + _escapedQueueName + " WITH ACTIVATION (PROCEDURE_NAME=" + _sprocName + ", MAX_QUEUE_READERS=1, EXECUTE AS OWNER);"
                            + " END;"
                      + " IF (SELECT COUNT(*) FROM sys.services WHERE NAME=" + nameLiteral + ") = 0"
                            + " BEGIN"
                            + " CREATE SERVICE " + _escapedQueueName + " ON QUEUE " + _escapedQueueName + " ([http://schemas.microsoft.com/SQL/Notifications/PostQueryNotification]);"
                         + " IF (SELECT COUNT(*) FROM sys.database_principals WHERE name='sql_dependency_subscriber' AND type='R') <> 0"
                              + " BEGIN"
                              + " GRANT SEND ON SERVICE::" + _escapedQueueName + " TO sql_dependency_subscriber;"
                              + " END; "
                            + " END;"
                      + " BEGIN DIALOG @dialog_handle FROM SERVICE " + _escapedQueueName + " TO SERVICE " + nameLiteral;

                SqlParameter param = new SqlParameter()
                {
                    ParameterName = "@dialog_handle",
                    DbType = DbType.Guid,
                    Direction = ParameterDirection.Output
                };
                com.Parameters.Add(param);
                com.ExecuteNonQuery();

                // Finish setting up queries and state.  For re-start, we need to ensure we begin a new dialog above and reset
                // our queries to use the new dialogHandle.
                _dialogHandle = ((Guid)param.Value).ToString();
                _beginConversationQuery = "BEGIN CONVERSATION TIMER ('" + _dialogHandle + "') TIMEOUT = 120; " + _receiveQuery;
                _com.CommandText = _beginConversationQuery;
                _endConversationQuery = "END CONVERSATION @p1; ";
                _concatQuery = _endConversationQuery + _com.CommandText;

                trans.Commit();
                trans = null;
                _serviceQueueCreated = true;
            }
            finally
            {
                if (null != trans)
                {
                    try
                    {
                        trans.Rollback();
                        trans = null;
                    }
                    catch (Exception e)
                    {
                        if (!ADP.IsCatchableExceptionType(e))
                        {
                            throw;
                        }
                        ADP.TraceExceptionWithoutRethrow(e); // Discard failure, but trace for now.
                    }
                }
            }
        }

        internal void IncrementStartCount(string appDomainKey, out bool appDomainStart)
        {
            appDomainStart = false; // Reset out param.
            int result = Interlocked.Increment(ref _startCount); // Add to refCount.

            // Dictionary used to track how many times start has been called per app domain.
            // For each increment, add to count, and create entry if not present.
            lock (_appDomainKeyHash)
            {
                if (_appDomainKeyHash.ContainsKey(appDomainKey))
                {
                    _appDomainKeyHash[appDomainKey] = _appDomainKeyHash[appDomainKey] + 1;
                }
                else
                {
                    _appDomainKeyHash[appDomainKey] = 1;
                    appDomainStart = true;
                }
            }
        }

        private void ProcessNotificationResults(SqlDataReader reader)
        {
            Guid handle = Guid.Empty; // Conversation_handle.  Always close this!
            try
            {
                if (!_stop)
                {
                    while (reader.Read())
                    {
                        string msgType = reader.GetString(0);
                        handle = reader.GetGuid(1);

                        // Only process QueryNotification messages.
                        if (string.Equals(msgType, "http://schemas.microsoft.com/SQL/Notifications/QueryNotification", StringComparison.OrdinalIgnoreCase))
                        {
                            SqlXml payload = reader.GetSqlXml(2);
                            if (null != payload)
                            {
                                SqlNotification notification = SqlNotificationParser.ProcessMessage(payload);
                                if (null != notification)
                                {
                                    string key = notification.Key;
                                    int index = key.IndexOf(';'); // Our format is simple: "AppDomainKey;commandHash"

                                    if (index >= 0)
                                    { // Ensure ';' present.
                                        string appDomainKey = key.Substring(0, index);
                                        SqlDependencyPerAppDomainDispatcher dispatcher;
                                        lock (s_staticInstance._sqlDependencyPerAppDomainDispatchers)
                                        {
                                            dispatcher = s_staticInstance._sqlDependencyPerAppDomainDispatchers[appDomainKey];
                                        }
                                        if (null != dispatcher)
                                        {
                                            try
                                            {
                                                dispatcher.InvalidateCommandID(notification); // CROSS APP-DOMAIN CALL!
                                            }
                                            catch (Exception e)
                                            {
                                                if (!ADP.IsCatchableExceptionType(e))
                                                {
                                                    throw;
                                                }
                                                ADP.TraceExceptionWithoutRethrow(e); // Discard failure.  User event could throw exception.
                                            }
                                        }
                                        else
                                        {
                                            Debug.Fail("Received notification but do not have an associated PerAppDomainDispatcher!");
                                        }
                                    }
                                    else
                                    {
                                        Debug.Fail("Unexpected ID format received!");
                                    }
                                }
                                else
                                {
                                    Debug.Fail("Null notification returned from ProcessMessage!");
                                }
                            }
                            else
                            {
                                Debug.Fail("Null payload for QN notification type!");
                            }
                        }
                        else
                        {
                            handle = Guid.Empty;
                        }
                    }
                }
            }
            finally
            {
                // Since we do not want to make a separate round trip just for the end conversation call, we need to
                // batch it with the next command.  
                if (handle == Guid.Empty)
                { // This should only happen if failure occurred, or if non-QN format received.
                    _com.CommandText = _beginConversationQuery ?? _receiveQuery; // If we're doing the initial query, we won't have a conversation Guid to begin yet.
                    if (_com.Parameters.Count > 1)
                    { // Remove conversation param since next execute is only query.
                        _com.Parameters.Remove(_conversationGuidParam);
                    }
                    Debug.Assert(_com.Parameters.Count == 1, "Unexpected number of parameters!");
                }
                else
                {
                    _com.CommandText = _concatQuery; // END query + WAITFOR RECEIVE query.
                    _conversationGuidParam.Value = handle; // Set value for conversation handle.
                    if (_com.Parameters.Count == 1)
                    { // Add parameter if previous execute was only query.
                        _com.Parameters.Add(_conversationGuidParam);
                    }
                    Debug.Assert(_com.Parameters.Count == 2, "Unexpected number of parameters!");
                }
            }
        }

        private void Restart(object unused)
        { // Unused arg required by TimerCallback.
            try
            {
                lock (this)
                {
                    if (!_stop)
                    { // Only execute if we are still in running state.
                        try
                        {
                            _con.Close();
                        }
                        catch (Exception e)
                        {
                            if (!ADP.IsCatchableExceptionType(e))
                            {
                                throw;
                            }
                            ADP.TraceExceptionWithoutRethrow(e); // Discard close failure, if it occurs.  Only trace it.
                        }
                    }
                }

                // Rather than one long lock - take lock 3 times for shorter periods.

                lock (this)
                {
                    if (!_stop)
                    {
                        _con.Open();
                    }
                }

                lock (this)
                {
                    if (!_stop)
                    {
                        if (_serviceQueueCreated)
                        {
                            bool failure = false;

                            try
                            {
                                CreateQueueAndService(true); // Ensure service, queue, etc is present, if we created it.
                            }
                            catch (Exception e)
                            {
                                if (!ADP.IsCatchableExceptionType(e))
                                {
                                    throw;
                                }
                                ADP.TraceExceptionWithoutRethrow(e); // Discard failure, but trace for now.
                                failure = true;
                            }

                            if (failure)
                            {
                                // If we failed to re-created queue, service, sproc - invalidate!
                                s_staticInstance.Invalidate(Server,
                                                           new SqlNotification(SqlNotificationInfo.Error,
                                                                               SqlNotificationSource.Client,
                                                                               SqlNotificationType.Change,
                                                                               null));

                            }
                        }
                    }
                }

                lock (this)
                {
                    if (!_stop)
                    {
                        _timeoutParam.Value = 0; // Reset timeout to zero - we do not want to block.
                        SynchronouslyQueryServiceBrokerQueue();
                        // If the above succeeds, we are back in success case - requeue for async call.
                        _timeoutParam.Value = _defaultWaitforTimeout; // If success, reset to default for re-queue.
                        AsynchronouslyQueryServiceBrokerQueue();
                        _errorState = false;
                        Timer retryTimer = _retryTimer;
                        if (retryTimer != null)
                        {
                            _retryTimer = null;
                            retryTimer.Dispose();
                        }
                    }
                }

                if (_stop)
                {
                    TearDownAndDispose(); // Function will lock(this).
                }
            }
            catch (Exception e)
            {
                if (!ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }
                ADP.TraceExceptionWithoutRethrow(e);

                try
                {
                    // If unexpected query or connection failure, invalidate all dependencies against this server.
                    // We may over-notify if only some of the connections to a particular database were affected,
                    // but this should not be frequent enough to be a concern.
                    // NOTE - we invalidate after failure first occurs and then retry fails.  We will then continue
                    // to invalidate every time the retry fails.
                    s_staticInstance.Invalidate(Server,
                                               new SqlNotification(SqlNotificationInfo.Error,
                                                                   SqlNotificationSource.Client,
                                                                   SqlNotificationType.Change,
                                                                   null));
                }
                catch (Exception f)
                {
                    if (!ADP.IsCatchableExceptionType(f))
                    {
                        throw;
                    }
                    ADP.TraceExceptionWithoutRethrow(f); // Discard exception from Invalidate.  User events can throw.
                }

                try
                {
                    _con.Close();
                }
                catch (Exception f)
                {
                    if (!ADP.IsCatchableExceptionType(f))
                    {
                        throw;
                    }
                    ADP.TraceExceptionWithoutRethrow(f); // Discard close failure, if it occurs.  Only trace it.
                }

                if (!_stop)
                {
                    // Create a timer to callback in one minute, retrying the call to Restart().
                    _retryTimer = new Timer(new TimerCallback(Restart), null, _defaultWaitforTimeout, Timeout.Infinite);
                    // We will retry this indefinitely, until success - or Stop();
                }
            }
        }

        internal bool Stop(string appDomainKey, out bool appDomainStop)
        {
            appDomainStop = false;

            // Dictionary used to track how many times start has been called per app domain.
            // For each decrement, subtract from count, and delete if we reach 0.

            if (null != appDomainKey)
            {
                // If null, then this was called from SqlDependencyProcessDispatcher, we ignore appDomainKeyHash.
                lock (_appDomainKeyHash)
                {
                    if (_appDomainKeyHash.ContainsKey(appDomainKey))
                    { // Do nothing if AppDomain did not call Start!
                        int value = _appDomainKeyHash[appDomainKey];

                        Debug.Assert(value > 0, "Unexpected count for appDomainKey");

                        if (value > 0)
                        {
                            _appDomainKeyHash[appDomainKey] = value - 1;
                        }
                        else
                        {
                            Debug.Fail("Unexpected AppDomainKey count in Stop()");
                        }

                        if (1 == value)
                        { // Remove from dictionary if pre-decrement count was one.
                            _appDomainKeyHash.Remove(appDomainKey);
                            appDomainStop = true;
                        }
                    }
                    else
                    {
                        Debug.Fail("Unexpected state on Stop() - no AppDomainKey entry in hashtable!");
                    }
                }
            }

            Debug.Assert(_startCount > 0, "About to decrement _startCount less than 0!");
            int result = Interlocked.Decrement(ref _startCount);

            if (0 == result)
            { // If we've reached refCount 0, destroy.
                // Lock to ensure Cancel() complete prior to other thread calling TearDown.
                lock (this)
                {
                    try
                    {
                        // Race condition with executing thread - will throw if connection is closed due to failure.
                        // Rather than fighting the race condition, just call it and discard any potential failure.
                        _com.Cancel(); // Cancel the pending command.  No-op if connection closed.
                    }
                    catch (Exception e)
                    {
                        if (!ADP.IsCatchableExceptionType(e))
                        {
                            throw;
                        }
                        ADP.TraceExceptionWithoutRethrow(e); // Discard failure, if it should occur.
                    }
                    _stop = true;
                }

                // Wait until stopped and service & queue are dropped.
                Stopwatch retryStopwatch = Stopwatch.StartNew();
                while (true)
                {
                    lock (this)
                    {
                        if (_stopped)
                        {
                            break;
                        }

                        // If we are in error state (_errorState is true), force a tear down.
                        // Likewise, if we have exceeded the maximum retry period (30 seconds) waiting for cleanup, force a tear down.
                        // In rare cases during app domain unload, the async cleanup performed by AsyncResultCallback
                        // may fail to execute TearDownAndDispose, leaving this method in an infinite loop.
                        // To avoid the infinite loop, we force the cleanup here after 30 seconds.  Since we have reached 
                        // refcount of 0, either this method call or the thread running AsyncResultCallback is responsible for calling 
                        // TearDownAndDispose when transitioning to the _stopped state.  Failing to call TearDownAndDispose means we leak 
                        // the service broker objects created by this SqlDependency instance, so we make a best effort here to call 
                        // TearDownAndDispose in the maximum retry period case as well as in the _errorState case.
                        if (_errorState || retryStopwatch.Elapsed.Seconds >= 30)
                        {
                            Timer retryTimer = _retryTimer;
                            _retryTimer = null;
                            if (retryTimer != null)
                            {
                                retryTimer.Dispose(); // Dispose timer - stop retry loop!
                            }
                            TearDownAndDispose(); // Will not hit server unless connection open!
                            break;
                        }
                    }

                    // Yield the thread since the stop has not yet completed.
                    // To avoid CPU spikes while waiting, yield and wait for at least one millisecond
                    Thread.Sleep(1);
                }
            }

            Debug.Assert(0 <= _startCount, "Invalid start count state");

            return _stopped;
        }

        private void SynchronouslyQueryServiceBrokerQueue()
        {
            using (SqlDataReader reader = _com.ExecuteReader())
            {
                ProcessNotificationResults(reader);
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:ReviewSqlQueriesForSecurityVulnerabilities")]
        private void TearDownAndDispose()
        {
            lock (this)
            { // Lock to ensure Stop() (with Cancel()) complete prior to TearDown.
                try
                {
                    // Only execute if connection is still up and open.
                    if (ConnectionState.Closed != _con.State && ConnectionState.Broken != _con.State)
                    {
                        if (_com.Parameters.Count > 1)
                        { // Need to close dialog before completing.
                            // In the normal case, the "End Conversation" query is executed before a
                            // receive query and upon return we will clear the state.  However, unless
                            // a non notification query result is returned, we will not clear it.  That
                            // means a query is generally always executing with an "end conversation" on
                            // the wire.  Rather than synchronize for success of the other "end conversation", 
                            // simply re-execute.
                            try
                            {
                                _com.CommandText = _endConversationQuery;
                                _com.Parameters.Remove(_timeoutParam);
                                _com.ExecuteNonQuery();
                            }
                            catch (Exception e)
                            {
                                if (!ADP.IsCatchableExceptionType(e))
                                {
                                    throw;
                                }
                                ADP.TraceExceptionWithoutRethrow(e); // Discard failure.
                            }
                        }

                        if (_serviceQueueCreated && !_errorState)
                        {
                            /*
                                BEGIN TRANSACTION;
                                DROP SERVICE "+_escapedQueueName+";
                                DROP QUEUE "+_escapedQueueName+";
                                DROP PROCEDURE "+_sprocName+";
                                COMMIT TRANSACTION;
                            */
                            _com.CommandText = "BEGIN TRANSACTION; DROP SERVICE " + _escapedQueueName + "; DROP QUEUE " + _escapedQueueName + "; DROP PROCEDURE " + _sprocName + "; COMMIT TRANSACTION;";
                            try
                            {
                                _com.ExecuteNonQuery();
                            }
                            catch (Exception e)
                            {
                                if (!ADP.IsCatchableExceptionType(e))
                                {
                                    throw;
                                }
                                ADP.TraceExceptionWithoutRethrow(e); // Discard failure.
                            }
                        }
                    }
                }
                finally
                {
                    _stopped = true;
                    _con.Dispose(); // Close and dispose connection.
                }
            }
        }
    }

    // Private class encapsulating the notification payload parsing logic.

    private class SqlNotificationParser
    {
        [Flags]
        private enum MessageAttributes
        {
            None = 0,
            Type = 1,
            Source = 2,
            Info = 4,
            All = Type + Source + Info,
        }

        // node names in the payload
        private const string RootNode = "QueryNotification";
        private const string MessageNode = "Message";

        // attribute names (on the QueryNotification element)
        private const string InfoAttribute = "info";
        private const string SourceAttribute = "source";
        private const string TypeAttribute = "type";

        internal static SqlNotification ProcessMessage(SqlXml xmlMessage)
        {
            using (XmlReader xmlReader = xmlMessage.CreateReader())
            {
                string keyvalue = string.Empty;

                MessageAttributes messageAttributes = MessageAttributes.None;

                SqlNotificationType type = SqlNotificationType.Unknown;
                SqlNotificationInfo info = SqlNotificationInfo.Unknown;
                SqlNotificationSource source = SqlNotificationSource.Unknown;

                string key = string.Empty;

                // Move to main node, expecting "QueryNotification".
                xmlReader.Read();
                if ((XmlNodeType.Element == xmlReader.NodeType) &&
                     (RootNode == xmlReader.LocalName) &&
                     (3 <= xmlReader.AttributeCount))
                {
                    // Loop until we've processed all the attributes.
                    while ((MessageAttributes.All != messageAttributes) && (xmlReader.MoveToNextAttribute()))
                    {
                        try
                        {
                            switch (xmlReader.LocalName)
                            {
                                case TypeAttribute:
                                    try
                                    {
                                        SqlNotificationType temp = (SqlNotificationType)Enum.Parse(typeof(SqlNotificationType), xmlReader.Value, true);
                                        if (Enum.IsDefined(typeof(SqlNotificationType), temp))
                                        {
                                            type = temp;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        if (!ADP.IsCatchableExceptionType(e))
                                        {
                                            throw;
                                        }
                                        ADP.TraceExceptionWithoutRethrow(e); // Discard failure, if it should occur.
                                    }
                                    messageAttributes |= MessageAttributes.Type;
                                    break;
                                case SourceAttribute:
                                    try
                                    {
                                        SqlNotificationSource temp = (SqlNotificationSource)Enum.Parse(typeof(SqlNotificationSource), xmlReader.Value, true);
                                        if (Enum.IsDefined(typeof(SqlNotificationSource), temp))
                                        {
                                            source = temp;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        if (!ADP.IsCatchableExceptionType(e))
                                        {
                                            throw;
                                        }
                                        ADP.TraceExceptionWithoutRethrow(e); // Discard failure, if it should occur.
                                    }
                                    messageAttributes |= MessageAttributes.Source;
                                    break;
                                case InfoAttribute:
                                    try
                                    {
                                        string value = xmlReader.Value;
                                        // 3 of the server info values do not match client values - map.
                                        switch (value)
                                        {
                                            case "set options":
                                                info = SqlNotificationInfo.Options;
                                                break;
                                            case "previous invalid":
                                                info = SqlNotificationInfo.PreviousFire;
                                                break;
                                            case "query template limit":
                                                info = SqlNotificationInfo.TemplateLimit;
                                                break;
                                            default:
                                                SqlNotificationInfo temp = (SqlNotificationInfo)Enum.Parse(typeof(SqlNotificationInfo), value, true);
                                                if (Enum.IsDefined(typeof(SqlNotificationInfo), temp))
                                                {
                                                    info = temp;
                                                }
                                                break;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        if (!ADP.IsCatchableExceptionType(e))
                                        {
                                            throw;
                                        }
                                        ADP.TraceExceptionWithoutRethrow(e); // Discard failure, if it should occur.
                                    }
                                    messageAttributes |= MessageAttributes.Info;
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch (ArgumentException e)
                        {
                            ADP.TraceExceptionWithoutRethrow(e); // Discard failure, but trace.
                            return null;
                        }
                    }

                    if (MessageAttributes.All != messageAttributes)
                    {
                        return null;
                    }

                    // Proceed to the "Message" node.
                    if (!xmlReader.Read())
                    {
                        return null;
                    }

                    // Verify state after Read().
                    if ((XmlNodeType.Element != xmlReader.NodeType) || (0 != string.Compare(xmlReader.LocalName, MessageNode, StringComparison.OrdinalIgnoreCase)))
                    {
                        return null;
                    }

                    // Proceed to the Text Node.
                    if (!xmlReader.Read())
                    {
                        return null;
                    }

                    // Verify state after Read().
                    if (xmlReader.NodeType != XmlNodeType.Text)
                    {
                        return null;
                    }

                    // Create a new XmlTextReader on the Message node value.
                    using (XmlTextReader xmlMessageReader = new XmlTextReader(xmlReader.Value, XmlNodeType.Element, null))
                    {
                        // Proceed to the Text Node.
                        if (!xmlMessageReader.Read())
                        {
                            return null;
                        }

                        if (xmlMessageReader.NodeType == XmlNodeType.Text)
                        {
                            key = xmlMessageReader.Value;
                            xmlMessageReader.Close();
                        }
                        else
                        {
                            return null;
                        }
                    }

                    return new SqlNotification(info, source, type, key);
                }
                else
                {
                    return null; // failure
                }
            }
        }
    }

    // Private class encapsulating the SqlConnectionContainer hash logic.

    private class SqlConnectionContainerHashHelper
    {
        // For default, queue is computed in SqlConnectionContainer constructor, so queue will be empty and
        // connection string will not include app name based on queue.  As a result, the connection string
        // builder will always contain up to date info, but _connectionString and _queue will not.

        // As a result, we will not use _connectionStringBuilder as part of Equals or GetHashCode.

        private DbConnectionPoolIdentity _identity;
        private string _connectionString;
        private string _queue;
        private SqlConnectionStringBuilder _connectionStringBuilder; // Not to be used for comparison!

        internal SqlConnectionContainerHashHelper(DbConnectionPoolIdentity identity, string connectionString,
                                                  string queue, SqlConnectionStringBuilder connectionStringBuilder)
        {
            _identity = identity;
            _connectionString = connectionString;
            _queue = queue;
            _connectionStringBuilder = connectionStringBuilder;
        }

        // Not to be used for comparison!
        internal SqlConnectionStringBuilder ConnectionStringBuilder => _connectionStringBuilder;

        internal DbConnectionPoolIdentity Identity => _identity;

        internal string Queue => _queue;

        public override bool Equals(object value)
        {
            SqlConnectionContainerHashHelper temp = (SqlConnectionContainerHashHelper)value;

            bool result = false;

            // Ignore SqlConnectionStringBuilder, since it is present largely for debug purposes.

            if (null == temp)
            { // If passed value null - false.
                result = false;
            }
            else if (this == temp)
            { // If instances equal - true.
                result = true;
            }
            else
            {
                if ((_identity != null && temp._identity == null) || // If XOR of null identities false - false.
                     (_identity == null && temp._identity != null))
                {
                    result = false;
                }
                else if (_identity == null && temp._identity == null)
                {
                    if (temp._connectionString == _connectionString &&
                        string.Equals(temp._queue, _queue, StringComparison.OrdinalIgnoreCase))
                    {
                        result = true;
                    }
                    else
                    {
                        result = false;
                    }
                }
                else
                {
                    if (temp._identity.Equals(_identity) &&
                        temp._connectionString == _connectionString &&
                        string.Equals(temp._queue, _queue, StringComparison.OrdinalIgnoreCase))
                    {
                        result = true;
                    }
                    else
                    {
                        result = false;
                    }
                }
            }

            return result;
        }

        public override int GetHashCode()
        {
            int hashValue = 0;

            if (null != _identity)
            {
                hashValue = _identity.GetHashCode();
            }

            if (null != _queue)
            {
                hashValue = unchecked(_connectionString.GetHashCode() + _queue.GetHashCode() + hashValue);
            }
            else
            {
                hashValue = unchecked(_connectionString.GetHashCode() + hashValue);
            }

            return hashValue;
        }
    }

    // SqlDependencyProcessDispatcher static members

    private static SqlDependencyProcessDispatcher s_staticInstance = new SqlDependencyProcessDispatcher(null);

    // Dictionaries used as maps.
    private Dictionary<SqlConnectionContainerHashHelper, SqlConnectionContainer> _connectionContainers;                 // NT_ID+ConStr+Service->Container
    private Dictionary<string, SqlDependencyPerAppDomainDispatcher> _sqlDependencyPerAppDomainDispatchers; // AppDomainKey->Callback

    // Constructors

    // Private constructor - only called by public constructor for static initialization.
    private SqlDependencyProcessDispatcher(object dummyVariable)
    {
        Debug.Assert(null == s_staticInstance, "Real constructor called with static instance already created!");

        _connectionContainers = new Dictionary<SqlConnectionContainerHashHelper, SqlConnectionContainer>();
        _sqlDependencyPerAppDomainDispatchers = new Dictionary<string, SqlDependencyPerAppDomainDispatcher>();
    }

    // Constructor is only called by remoting.
    // Required to be public, even on internal class, for Remoting infrastructure.
    public SqlDependencyProcessDispatcher()
    {
        // Empty constructor and object - dummy to obtain singleton.
    }

    // Properties

    internal static SqlDependencyProcessDispatcher SingletonProcessDispatcher => s_staticInstance;

    // Various private methods

    private static SqlConnectionContainerHashHelper GetHashHelper(
        string connectionString,
        out SqlConnectionStringBuilder connectionStringBuilder,
        out DbConnectionPoolIdentity identity,
        out string user,
            string queue)
    {
        // Force certain connection string properties to be used by SqlDependencyProcessDispatcher.  
        // This logic is done here to enable us to have the complete connection string now to be used
        // for tracing as we flow through the logic.
        connectionStringBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            Pooling = false,
            Enlist = false,
            ConnectRetryCount = 0
        };
        if (null != queue)
        { // User provided!
            connectionStringBuilder.ApplicationName = queue; // ApplicationName will be set to queue name.
        }

        if (connectionStringBuilder.IntegratedSecurity)
        {
            // Use existing identity infrastructure for error cases and proper hash value.
            identity = DbConnectionPoolIdentity.GetCurrent();
            user = null;
        }
        else
        {
            identity = null;
            user = connectionStringBuilder.UserID;
        }

        return new SqlConnectionContainerHashHelper(identity, connectionStringBuilder.ConnectionString,
                                                    queue, connectionStringBuilder);
    }

    // Needed for remoting to prevent lifetime issues and default GC cleanup.
    public override object InitializeLifetimeService()
    {
        return null;
    }

    private void Invalidate(string server, SqlNotification sqlNotification)
    {
        Debug.Assert(this == s_staticInstance, "Instance method called on non _staticInstance instance!");
        lock (_sqlDependencyPerAppDomainDispatchers)
        {

            foreach (KeyValuePair<string, SqlDependencyPerAppDomainDispatcher> entry in _sqlDependencyPerAppDomainDispatchers)
            {
                SqlDependencyPerAppDomainDispatcher perAppDomainDispatcher = entry.Value;
                try
                {
                    perAppDomainDispatcher.InvalidateServer(server, sqlNotification);
                }
                catch (Exception f)
                {
                    // Since we are looping over dependency dispatchers, do not allow one Invalidate
                    // that results in a throw prevent us from invalidating all dependencies
                    // related to this server.
                    // NOTE - SqlDependencyPerAppDomainDispatcher already wraps individual dependency invalidates
                    // with try/catch, but we should be careful and do the same here.
                    if (!ADP.IsCatchableExceptionType(f))
                    {
                        throw;
                    }
                    ADP.TraceExceptionWithoutRethrow(f); // Discard failure, but trace.
                }
            }
        }
    }

    // Clean-up method initiated by other AppDomain.Unloads

    // Individual AppDomains upon AppDomain.UnloadEvent will call this method.
    internal void QueueAppDomainUnloading(string appDomainKey)
    {
        ThreadPool.QueueUserWorkItem(new WaitCallback(AppDomainUnloading), appDomainKey);
    }

    // This method is only called by queued work-items from the method above.
    private void AppDomainUnloading(object state)
    {
        string appDomainKey = (string)state;

        Debug.Assert(this == s_staticInstance, "Instance method called on non _staticInstance instance!");
        lock (_connectionContainers)
        {
            List<SqlConnectionContainerHashHelper> containersToRemove = new List<SqlConnectionContainerHashHelper>();

            foreach (KeyValuePair<SqlConnectionContainerHashHelper, SqlConnectionContainer> entry in _connectionContainers)
            {
                SqlConnectionContainer container = entry.Value;
                if (container.AppDomainUnload(appDomainKey))
                { // Perhaps wrap in try catch.
                    containersToRemove.Add(container.HashHelper);
                }
            }

            foreach (SqlConnectionContainerHashHelper hashHelper in containersToRemove)
            {
                _connectionContainers.Remove(hashHelper);
            }
        }

        lock (_sqlDependencyPerAppDomainDispatchers)
        { // Remove from global Dictionary.
            _sqlDependencyPerAppDomainDispatchers.Remove(appDomainKey);
        }
    }

    // -------------
    // Start methods
    // -------------

    internal bool StartWithDefault(
        string connectionString,
        out string server,
        out DbConnectionPoolIdentity identity,
        out string user,
        out string database,
        ref string service,
            string appDomainKey,
            SqlDependencyPerAppDomainDispatcher dispatcher,
        out bool errorOccurred,
        out bool appDomainStart)
    {
        Debug.Assert(this == s_staticInstance, "Instance method called on non _staticInstance instance!");
        return Start(
            connectionString,
            out server,
            out identity,
            out user,
            out database,
            ref service,
                appDomainKey,
                dispatcher,
            out errorOccurred,
            out appDomainStart,
                true);
    }

    internal bool Start(
        string connectionString,
        string queue,
        string appDomainKey,
        SqlDependencyPerAppDomainDispatcher dispatcher)
    {
        Debug.Assert(this == s_staticInstance, "Instance method called on non _staticInstance instance!");
        return Start(
            connectionString,
            out string dummyValue1,
            out DbConnectionPoolIdentity dummyValue3,
            out dummyValue1,
            out dummyValue1,
            ref queue,
                appDomainKey,
                dispatcher,
            out bool dummyValue2,
            out dummyValue2,
                false);
    }

    private bool Start(
        string connectionString,
        out string server,
        out DbConnectionPoolIdentity identity,
        out string user,
        out string database,
        ref string queueService,
            string appDomainKey,
            SqlDependencyPerAppDomainDispatcher dispatcher,
        out bool errorOccurred,
        out bool appDomainStart,
            bool useDefaults)
    {
        Debug.Assert(this == s_staticInstance, "Instance method called on non _staticInstance instance!");
        server = null;  // Reset out params.
        identity = null;
        user = null;
        database = null;
        errorOccurred = false;
        appDomainStart = false;

        lock (_sqlDependencyPerAppDomainDispatchers)
        {
            if (!_sqlDependencyPerAppDomainDispatchers.ContainsKey(appDomainKey))
            {
                _sqlDependencyPerAppDomainDispatchers[appDomainKey] = dispatcher;
            }
        }

        SqlConnectionContainerHashHelper hashHelper = GetHashHelper(connectionString,
                                                            out SqlConnectionStringBuilder connectionStringBuilder,
                                                            out identity,
                                                            out user,
                                                                queueService);

        bool started = false;

        SqlConnectionContainer container = null;
        lock (_connectionContainers)
        {
            if (!_connectionContainers.ContainsKey(hashHelper))
            {
                container = new SqlConnectionContainer(hashHelper, appDomainKey, useDefaults);
                _connectionContainers.Add(hashHelper, container);
                started = true;
                appDomainStart = true;
            }
            else
            {
                container = _connectionContainers[hashHelper];
                if (container.InErrorState)
                {
                    errorOccurred = true; // Set outparam errorOccurred true so we invalidate on Start().
                }
                else
                {
                    container.IncrementStartCount(appDomainKey, out appDomainStart);
                }
            }
        }

        if (useDefaults && !errorOccurred)
        { // Return server, database, and queue for use by SqlDependency.
            server = container.Server;
            database = container.Database;
            queueService = container.Queue;
        }

        return started;
    }

    // Stop methods

    internal bool Stop(
        string connectionString,
        out string server,
        out DbConnectionPoolIdentity identity,
        out string user,
        out string database,
        ref string queueService,
            string appDomainKey,
        out bool appDomainStop)
    {
         Debug.Assert(this == s_staticInstance, "Instance method called on non _staticInstance instance!");
         server = null;  // Reset out param.
         identity = null;
         user = null;
         database = null;
         appDomainStop = false;

        SqlConnectionContainerHashHelper hashHelper = GetHashHelper(connectionString,
                                                          out SqlConnectionStringBuilder connectionStringBuilder,
                                                          out identity,
                                                          out user,
                                                              queueService);

        bool stopped = false;

         lock (_connectionContainers)
         {
             if (_connectionContainers.ContainsKey(hashHelper))
             {
                 SqlConnectionContainer container = _connectionContainers[hashHelper];
                 server = container.Server;   // Return server, database, and queue info for use by calling SqlDependency.
                 database = container.Database;
                 queueService = container.Queue;

                 if (container.Stop(appDomainKey, out appDomainStop))
                 { // Stop can be blocking if refCount == 0 on container.
                     stopped = true;
                     _connectionContainers.Remove(hashHelper); // Remove from collection.
                 }
             }
         }

         return stopped;
    }
}
