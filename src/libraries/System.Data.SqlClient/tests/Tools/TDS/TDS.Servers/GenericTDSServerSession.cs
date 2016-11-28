// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;

using Microsoft.SqlServer.TDS.EndPoint;
using Microsoft.SqlServer.TDS.EndPoint.SSPI;
using Microsoft.SqlServer.TDS.PreLogin;
using Microsoft.SqlServer.TDS.Login7;
using Microsoft.SqlServer.TDS.SessionState;

namespace Microsoft.SqlServer.TDS.Servers
{
    /// <summary>
    /// Generic session for TDS Server
    /// </summary>
    public class GenericTDSServerSession : ITDSServerSession
    {
        /// <summary>
        /// Server that created the session
        /// </summary>
        public ITDSServer Server { get; private set; }

        /// <summary>
        /// Session identifier
        /// </summary>
        public uint SessionID { get; private set; }

        /// <summary>
        /// Size of the TDS packet
        /// </summary>
        public uint PacketSize { get; set; }

        /// <summary>
        /// User name if SQL authentication is used
        /// </summary>
        public string SQLUserID { get; set; }

        /// <summary>
        /// Context that indicates the stage of SSPI authentication
        /// </summary>
        public SSPIContext NTUserAuthenticationContext { get; set; }

        /// <summary>
        /// Database to which connection is established
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Collation
        /// </summary>
        public byte[] Collation { get; set; }

        /// <summary>
        /// TDS version of the communication
        /// </summary>
        public Version TDSVersion { get; set; }

        /// <summary>
        /// Local connection end-point information
        /// </summary>
        public TDSEndPointInfo ServerEndPointInfo { get; set; }

        /// <summary>
        /// Remote connectionend-point information
        /// </summary>
        public TDSEndPointInfo ClientEndPointInfo { get; set; }

        /// <summary>
        /// Transport encryption
        /// </summary>
        public TDSEncryptionType Encryption { get; set; }

        /// <summary>
        /// Certificate to use for encryption
        /// </summary>
        public X509Certificate EncryptionCertificate { get; set; }

        /// <summary>
        /// Nonce option sent by client
        /// </summary>
        public byte[] ClientNonce { get; set; }

        /// <summary>
        /// Nonce option sent by server
        /// </summary>
        public byte[] ServerNonce { get; set; }

        /// <summary>
        /// FedAuthRequired Response sent by server
        /// </summary>
        public TdsPreLoginFedAuthRequiredOption FedAuthRequiredPreLoginServerResponse { get; set; }

        /// <summary>
        /// Federated authentication set of libraries to be used
        /// </summary>
        public TDSFedAuthLibraryType FederatedAuthenticationLibrary { get; set; }

        /// <summary>
        /// Counter of connection reset requests for this session
        /// </summary>
        public int ConnectionResetRequestCount { get; set; }

        /// <summary>
        /// Indicates whether this session supports transport-level recovery
        /// </summary>
        public bool IsSessionRecoveryEnabled { get; set; }

        #region Session Options

        /// <summary>
        /// Controls a group of SQL Server settings that collectively specify ISO standard behavior
        /// </summary>
        public bool AnsiDefaults
        {
            get
            {
                // See ...\sql\ntdbms\include\typesystem\setopts.h, SetAnsiDefaults()
                return AnsiNullDefaultOn && AnsiNulls && AnsiPadding && AnsiWarnings && CursorCloseOnCommit && ImplicitTransactions && QuotedIdentifier;
            }
            set
            {
                // Populate ansi defaults
                AnsiNullDefaultOn = value;
                AnsiNulls = value;
                AnsiPadding = value;
                AnsiWarnings = value;
                CursorCloseOnCommit = value;
                ImplicitTransactions = value;
                QuotedIdentifier = value;
            }
        }

        /// <summary>
        /// Affects the nullability of new columns when the nullability of the column is not specified in the CREATE TABLE and ALTER TABLE statements.
        /// </summary>
        public bool AnsiNullDefaultOn { get; set; }

        /// <summary>
        /// Controls null behavior in T-SQL
        /// </summary>
        public bool AnsiNulls { get; set; }

        /// <summary>
        /// Impacts character column behavior (char, varchar, binary, and varbinary)
        /// </summary>
        public bool AnsiPadding { get; set; }

        /// <summary>
        /// Controls certain warning messages required for ansi compliance
        /// </summary>
        public bool AnsiWarnings { get; set; }

        /// <summary>
        /// Terminates a query when an overflow or divide-by-zero error occurs during query execution
        /// </summary>
        public bool ArithAbort { get; set; }

        /// <summary>
        /// Controls whether error messages are returned from overflow or divide-by-zero errors during a query
        /// </summary>
        public bool ArithIgnore { get; set; }

        /// <summary>
        /// Controls whether concatenation results are treated as null or empty string values
        /// </summary>
        public bool ConcatNullYieldsNull { get; set; }

        /// <summary>
        /// Associates up to 128 bytes of binary information with the current session or connection
        /// </summary>
        public byte[] ContextInfo { get; set; }

        /// <summary>
        /// Controls whether the server will close cursors when you commit a transaction
        /// </summary>
        public bool CursorCloseOnCommit { get; set; }

        /// <summary>
        /// Sets the first day of the week to a number from 1 through 7
        /// </summary>
        public byte DateFirst { get; set; }

        /// <summary>
        /// Sets the order of the month, day, and year date parts for interpreting date character strings
        /// </summary>
        public DateFormatType DateFormat { get; set; }

        /// <summary>
        /// Specifies the relative importance that the current session continue processing if it is deadlocked with another session.
        /// </summary>
        public int DeadlockPriority { get; set; }

        /// <summary>
        /// Sets implicit transaction mode for the connection
        /// </summary>
        public bool ImplicitTransactions { get; set; }

        /// <summary>
        /// Specifies the language environment for the session (language name from sys.syslanguages). The session language determines the datetime formats and system messages.
        /// </summary>
        public LanguageType Language { get; set; }

        /// <summary>
        /// Specifies the number of milliseconds a statement waits for a lock to be released
        /// </summary>
        public int LockTimeout { get; set; }

        /// <summary>
        /// Controls the emitting of Done w/count tokens from Transact-SQL.
        /// </summary>
        public bool NoCount { get; set; }

        /// <summary>
        /// Generates an error when a loss of precision occurs in an expression
        /// </summary>
        public bool NumericRoundAbort { get; set; }

        /// <summary>
        /// Causes SQL Server to follow the ISO rules regarding quotation mark delimiting identifiers and literal strings
        /// </summary>
        public bool QuotedIdentifier { get; set; }

        /// <summary>
        /// Specifies the size of varchar(max), nvarchar(max), varbinary(max), text, ntext, and image data returned by a SELECT statement
        /// </summary>
        public int TextSize { get; set; }

        /// <summary>
        /// Controls the locking and row versioning behavior of Transact-SQL statements issued by a connection to SQL Server.
        /// </summary>
        public TransactionIsolationLevelType TransactionIsolationLevel { get; set; }

        /// <summary>
        /// Specifies whether SQL Server automatically rolls back the current transaction when a Transact-SQL statement raises a run-time error
        /// </summary>
        public bool TransactionAbortOnError { get; set; }

        #endregion

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public GenericTDSServerSession(ITDSServer server, uint sessionID) :
            this(server, sessionID, 4096)
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public GenericTDSServerSession(ITDSServer server, uint sessionID, uint packetSize)
        {
            // Save the server
            Server = server;

            // Save session identifier
            SessionID = sessionID;

            // Save packet size
            PacketSize = packetSize;

            // By default encryption is disabled
            Encryption = TDSEncryptionType.Off;

            // First day of the week
            DateFirst = 7;

            // Transaction isolation level
            TransactionIsolationLevel = TransactionIsolationLevelType.ReadCommited;

            // Language
            Language = LanguageType.English;

            // Date format
            DateFormat = DateFormatType.MonthDayYear;

            // Default collation
            Collation = new byte[] { 0x09, 0x04, 0xD0, 0x00, 0x34 };

            // Infinite text size
            TextSize = -1;
        }

        /// <summary>
        /// Inflate the session state using options
        /// </summary>
        public virtual void Inflate(TDSSessionRecoveryData initial, TDSSessionRecoveryData current)
        {
            // Check if we have an initial state
            if (initial != null)
            {
                // Save initial values
                _InflateRecoveryData(initial);
            }

            // Check if we have a current value
            if (current != null)
            {
                // Update values with current
                _InflateRecoveryData(current);
            }
        }

        /// <summary>
        /// Serialize session state into a collection of options
        /// </summary>
        /// <returns></returns>
        public virtual IList<TDSSessionStateOption> Deflate()
        {
            // Prepare options list
            IList<TDSSessionStateOption> options = new List<TDSSessionStateOption>();

            // Create user options option
            TDSSessionStateUserOptionsOption userOptionsOption = new TDSSessionStateUserOptionsOption();

            // Transfer properties from the session onto the session state
            userOptionsOption.AnsiWarnings = AnsiWarnings;
            userOptionsOption.AnsiNulls = AnsiNulls;
            userOptionsOption.CursorCloseOnCommit = CursorCloseOnCommit;
            userOptionsOption.QuotedIdentifier = QuotedIdentifier;
            userOptionsOption.ConcatNullYieldsNull = ConcatNullYieldsNull;
            userOptionsOption.AnsiNullDefaultOn = AnsiNullDefaultOn;
            userOptionsOption.AnsiPadding = AnsiPadding;
            userOptionsOption.ArithAbort = ArithAbort;
            userOptionsOption.TransactionAbortOnError = TransactionAbortOnError;
            userOptionsOption.NoCount = NoCount;
            userOptionsOption.ArithIgnore = ArithIgnore;
            userOptionsOption.ImplicitTransactions = ImplicitTransactions;
            userOptionsOption.NumericRoundAbort = ImplicitTransactions;

            // Register option with the collection
            options.Add(userOptionsOption);

            // Create date first/date format option
            TDSSessionStateDateFirstDateFormatOption dateFirstDateFormatOption = new TDSSessionStateDateFirstDateFormatOption();

            // Transfer properties from the session onto the session state
            dateFirstDateFormatOption.DateFirst = DateFirst;
            dateFirstDateFormatOption.DateFormat = DateFormat;

            // Register option with the collection
            options.Add(dateFirstDateFormatOption);

            // Allocate deadlock priority option
            TDSSessionStateDeadlockPriorityOption deadlockPriorityOption = new TDSSessionStateDeadlockPriorityOption();

            // Transfer properties from the session onto the session state 
            deadlockPriorityOption.Value = (sbyte)DeadlockPriority;

            // Register option with the collection
            options.Add(deadlockPriorityOption);

            // Allocate lock timeout option
            TDSSessionStateLockTimeoutOption lockTimeoutOption = new TDSSessionStateLockTimeoutOption();

            // Transfer properties from the session onto the session state
            lockTimeoutOption.Value = LockTimeout;

            // Register option with the collection
            options.Add(lockTimeoutOption);

            // Allocate ISO fips option
            TDSSessionStateISOFipsOption isoFipsOption = new TDSSessionStateISOFipsOption();

            // Transfer properties from the session onto the session state 
            isoFipsOption.TransactionIsolationLevel = TransactionIsolationLevel;

            // Register option with the collection
            options.Add(isoFipsOption);

            // Allocate text size option
            TDSSessionStateTextSizeOption textSizeOption = new TDSSessionStateTextSizeOption();

            // Transfer properties from the session onto the session state 
            textSizeOption.Value = TextSize;

            // Register option with the collection
            options.Add(textSizeOption);

            // Check if context info is specified
            if (ContextInfo != null)
            {
                // Allocate context info option
                TDSSessionStateContextInfoOption contextInfoOption = new TDSSessionStateContextInfoOption();

                // Transfer properties from the session onto the session state
                contextInfoOption.Value = ContextInfo;

                // Register option with the collection
                options.Add(isoFipsOption);
            }

            return options;
        }

        /// <summary>
        /// Indlate recovery data
        /// </summary>
        private void _InflateRecoveryData(TDSSessionRecoveryData data)
        {
            // Check if database is available
            if (data.Database != null)
            {
                // Apply database
                Database = data.Database;
            }

            // Check if language is available
            if (data.Language != null)
            {
                // Apply language
                Language = LanguageString.ToEnum(data.Language);
            }

            // Check if collation is available
            if (data.Collation != null)
            {
                Collation = data.Collation;
            }

            // Traverse all session states and inflate each separately
            foreach (TDSSessionStateOption option in data.Options)
            {
                // Check on the options
                if (option is TDSSessionStateUserOptionsOption)
                {
                    // Cast to specific option
                    TDSSessionStateUserOptionsOption specificOption = option as TDSSessionStateUserOptionsOption;

                    // Transfer properties from the session state onto the session
                    AnsiWarnings = specificOption.AnsiWarnings;
                    AnsiNulls = specificOption.AnsiNulls;
                    CursorCloseOnCommit = specificOption.CursorCloseOnCommit;
                    QuotedIdentifier = specificOption.QuotedIdentifier;
                    ConcatNullYieldsNull = specificOption.ConcatNullYieldsNull;
                    AnsiNullDefaultOn = specificOption.AnsiNullDefaultOn;
                    AnsiPadding = specificOption.AnsiPadding;
                    ArithAbort = specificOption.ArithAbort;
                    TransactionAbortOnError = specificOption.TransactionAbortOnError;
                    NoCount = specificOption.NoCount;
                    ArithIgnore = specificOption.ArithIgnore;
                    ImplicitTransactions = specificOption.ImplicitTransactions;
                    NumericRoundAbort = specificOption.NumericRoundAbort;
                }
                else if (option is TDSSessionStateDateFirstDateFormatOption)
                {
                    // Cast to specific option
                    TDSSessionStateDateFirstDateFormatOption specificOption = option as TDSSessionStateDateFirstDateFormatOption;

                    // Transfer properties from the session state onto the session
                    DateFirst = specificOption.DateFirst;
                    DateFormat = specificOption.DateFormat;
                }
                else if (option is TDSSessionStateDeadlockPriorityOption)
                {
                    // Cast to specific option
                    TDSSessionStateDeadlockPriorityOption specificOption = option as TDSSessionStateDeadlockPriorityOption;

                    // Transfer properties from the session state onto the session
                    DeadlockPriority = specificOption.Value;
                }
                else if (option is TDSSessionStateLockTimeoutOption)
                {
                    // Cast to specific option
                    TDSSessionStateLockTimeoutOption specificOption = option as TDSSessionStateLockTimeoutOption;

                    // Transfer properties from the session state onto the session
                    LockTimeout = specificOption.Value;
                }
                else if (option is TDSSessionStateISOFipsOption)
                {
                    // Cast to specific option
                    TDSSessionStateISOFipsOption specificOption = option as TDSSessionStateISOFipsOption;

                    // Transfer properties from the session state onto the session
                    TransactionIsolationLevel = specificOption.TransactionIsolationLevel;
                }
                else if (option is TDSSessionStateTextSizeOption)
                {
                    // Cast to specific option
                    TDSSessionStateTextSizeOption specificOption = option as TDSSessionStateTextSizeOption;

                    // Transfer properties from the session state onto the session
                    TextSize = specificOption.Value;
                }
                else if (option is TDSSessionStateContextInfoOption)
                {
                    // Cast to specific option
                    TDSSessionStateContextInfoOption specificOption = option as TDSSessionStateContextInfoOption;

                    // Transfer properties from the session state onto the session
                    ContextInfo = specificOption.Value;
                }
            }
        }
    }
}
