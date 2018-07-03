// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Data.SqlClient
{
    /// <devdoc> Class of variables for the Tds connection.
    /// </devdoc>
    internal static class TdsEnums
    {
        // internal tdsparser constants


        public const string SQL_PROVIDER_NAME = Common.DbConnectionStringDefaults.ApplicationName;

        public static readonly decimal SQL_SMALL_MONEY_MIN = new decimal(-214748.3648);
        public static readonly decimal SQL_SMALL_MONEY_MAX = new decimal(214748.3647);


        // HACK!!!
        // Constant for SqlDbType.SmallVarBinary... store internal variable here instead of on
        // SqlDbType so that it is not surfaced to the user!!!  Related to dtc and the fact that
        // the TransactionManager TDS stream is the only token left that uses VarBinarys instead of
        // BigVarBinarys.
        public const SqlDbType SmallVarBinary = (SqlDbType)(SqlDbType.Variant) + 1;

        // network protocol string constants
        public const string TCP = "tcp";
        public const string NP = "np";
        public const string RPC = "rpc";
        public const string BV = "bv";
        public const string ADSP = "adsp";
        public const string SPX = "spx";
        public const string VIA = "via";
        public const string LPC = "lpc";
        public const string ADMIN = "admin";

        // network function string constants
        public const string INIT_SSPI_PACKAGE = "InitSSPIPackage";
        public const string INIT_SESSION = "InitSession";
        public const string CONNECTION_GET_SVR_USER = "ConnectionGetSvrUser";
        public const string GEN_CLIENT_CONTEXT = "GenClientContext";

        // tdsparser packet handling constants
        public const byte SOFTFLUSH = 0;
        public const byte HARDFLUSH = 1;
        public const byte IGNORE = 2;

        // header constants
        public const int HEADER_LEN = 8;
        public const int HEADER_LEN_FIELD_OFFSET = 2;
        public const int YUKON_HEADER_LEN = 12; //Yukon headers also include a MARS session id
        public const int MARS_ID_OFFSET = 8;
        public const int HEADERTYPE_QNOTIFICATION = 1;
        public const int HEADERTYPE_MARS = 2;
        public const int HEADERTYPE_TRACE = 3;

        // other various constants
        public const int SUCCEED = 1;
        public const int FAIL = 0;
        public const short TYPE_SIZE_LIMIT = 8000;
        public const int MIN_PACKET_SIZE = 512;
        // Login packet can be no greater than 4k until server sends us env-change
        // increasing packet size.
        public const int DEFAULT_LOGIN_PACKET_SIZE = 4096;
        public const int MAX_PRELOGIN_PAYLOAD_LENGTH = 1024;
        public const int MAX_PACKET_SIZE = 32768;
        public const int MAX_SERVER_USER_NAME = 256;  // obtained from luxor

        // Severity  0 - 10 indicates informational (non-error) messages
        // Severity 11 - 16 indicates errors that can be corrected by user (syntax errors, etc...)
        // Severity 17 - 19 indicates failure due to insufficient resources in the server
        //      (max locks exceeded, not enough memory, other internal server limits reached, etc..)
        // Severity 20 - 25 Severe problems with the server, connection terminated.
        public const byte MIN_ERROR_CLASS = 11;   // webdata 100667: This should actually be 11
        public const byte MAX_USER_CORRECTABLE_ERROR_CLASS = 16;
        public const byte FATAL_ERROR_CLASS = 20;

        //    Message types
        public const byte MT_SQL = 1;    // SQL command batch
        public const byte MT_LOGIN = 2;    // Login message for pre-Sphinx (before version 7.0)
        public const byte MT_RPC = 3;    // Remote procedure call
        public const byte MT_TOKENS = 4;    // Table response data stream
        public const byte MT_BINARY = 5;    // Unformatted binary response data (UNUSED)
        public const byte MT_ATTN = 6;    // Attention (break) signal
        public const byte MT_BULK = 7;    // Bulk load data
        public const byte MT_OPEN = 8;    // Set up subchannel   (UNUSED)
        public const byte MT_CLOSE = 9;    // Close subchannel   (UNUSED)
        public const byte MT_ERROR = 10;   // Protocol error detected
        public const byte MT_ACK = 11;   // Protocol acknowledgement   (UNUSED)
        public const byte MT_ECHO = 12;   // Echo data  (UNUSED)
        public const byte MT_LOGOUT = 13;   // Logout message (UNUSED)
        public const byte MT_TRANS = 14;   // Transaction Manager Interface
        public const byte MT_OLEDB = 15;   // ? (UNUSED)
        public const byte MT_LOGIN7 = 16;   // Login message for Sphinx (version 7) or later
        public const byte MT_SSPI = 17;   // SSPI message
        public const byte MT_PRELOGIN = 18;   // Pre-login handshake

        // Message status bits
        public const byte ST_EOM = 0x1; // Packet is end-of-message
        public const byte ST_AACK = 0x2; // Packet acknowledges attention (server to client)
        public const byte ST_IGNORE = 0x2; // Ignore this event (client to server)
        public const byte ST_BATCH = 0x4; // Message is part of a batch.
        public const byte ST_RESET_CONNECTION = 0x8; // Exec sp_reset_connection prior to processing message
        public const byte ST_RESET_CONNECTION_PRESERVE_TRANSACTION = 0x10;  // reset prior to processing, with preserving local tx

        // TDS control tokens
        public const byte SQLCOLFMT = 0xa1;
        public const byte SQLPROCID = 0x7c;
        public const byte SQLCOLNAME = 0xa0;
        public const byte SQLTABNAME = 0xa4;
        public const byte SQLCOLINFO = 0xa5;
        public const byte SQLALTNAME = 0xa7;
        public const byte SQLALTFMT = 0xa8;
        public const byte SQLERROR = 0xaa;
        public const byte SQLINFO = 0xab;
        public const byte SQLRETURNVALUE = 0xac;
        public const byte SQLRETURNSTATUS = 0x79;
        public const byte SQLRETURNTOK = 0xdb;
        public const byte SQLALTCONTROL = 0xaf;
        public const byte SQLROW = 0xd1;
        public const byte SQLNBCROW = 0xd2;    // same as ROW with null-bit-compression support
        public const byte SQLALTROW = 0xd3;
        public const byte SQLDONE = 0xfd;
        public const byte SQLDONEPROC = 0xfe;
        public const byte SQLDONEINPROC = 0xff;
        public const byte SQLOFFSET = 0x78;
        public const byte SQLORDER = 0xa9;
        public const byte SQLDEBUG_CMD = 0x60;
        public const byte SQLLOGINACK = 0xad;
        public const byte SQLFEATUREEXTACK = 0xae;    // TDS 7.4 - feature ack
        public const byte SQLSESSIONSTATE = 0xe4;    // TDS 7.4 - connection resiliency session state  
        public const byte SQLENVCHANGE = 0xe3;    // Environment change notification
        public const byte SQLSECLEVEL = 0xed;    // Security level token ???
        public const byte SQLROWCRC = 0x39;    // ROWCRC datastream???
        public const byte SQLCOLMETADATA = 0x81;    // Column metadata including name
        public const byte SQLALTMETADATA = 0x88;    // Alt column metadata including name
        public const byte SQLSSPI = 0xed;    // SSPI data

        // Environment change notification streams
        // TYPE on TDS ENVCHANGE token stream (from sql\ntdbms\include\odsapi.h)
        //
        public const byte ENV_DATABASE = 1;  // Database changed
        public const byte ENV_LANG = 2;  // Language changed
        public const byte ENV_CHARSET = 3;  // Character set changed
        public const byte ENV_PACKETSIZE = 4;  // Packet size changed
        public const byte ENV_LOCALEID = 5;  // Unicode data sorting locale id
        public const byte ENV_COMPFLAGS = 6;  // Unicode data sorting comparison flags
        public const byte ENV_COLLATION = 7;  // SQL Collation
        // The following are environment change tokens valid for Yukon or later.
        public const byte ENV_BEGINTRAN = 8;  // Transaction began
        public const byte ENV_COMMITTRAN = 9;  // Transaction committed
        public const byte ENV_ROLLBACKTRAN = 10; // Transaction rolled back
        public const byte ENV_ENLISTDTC = 11; // Enlisted in Distributed Transaction
        public const byte ENV_DEFECTDTC = 12; // Defected from Distributed Transaction
        public const byte ENV_LOGSHIPNODE = 13; // Realtime Log shipping primary node
        public const byte ENV_PROMOTETRANSACTION = 15; // Promote Transaction
        public const byte ENV_TRANSACTIONMANAGERADDRESS = 16; // Transaction Manager Address
        public const byte ENV_TRANSACTIONENDED = 17; // Transaction Ended
        public const byte ENV_SPRESETCONNECTIONACK = 18; // SP_Reset_Connection ack
        public const byte ENV_USERINSTANCE = 19; // User Instance
        public const byte ENV_ROUTING = 20; // Routing (ROR) information

        public enum EnvChangeType : byte
        {
            ENVCHANGE_DATABASE = ENV_DATABASE,
            ENVCHANGE_LANG = ENV_LANG,
            ENVCHANGE_CHARSET = ENV_CHARSET,
            ENVCHANGE_PACKETSIZE = ENV_PACKETSIZE,
            ENVCHANGE_LOCALEID = ENV_LOCALEID,
            ENVCHANGE_COMPFLAGS = ENV_COMPFLAGS,
            ENVCHANGE_COLLATION = ENV_COLLATION,
            ENVCHANGE_BEGINTRAN = ENV_BEGINTRAN,
            ENVCHANGE_COMMITTRAN = ENV_COMMITTRAN,
            ENVCHANGE_ROLLBACKTRAN = ENV_ROLLBACKTRAN,
            ENVCHANGE_ENLISTDTC = ENV_ENLISTDTC,
            ENVCHANGE_DEFECTDTC = ENV_DEFECTDTC,
            ENVCHANGE_LOGSHIPNODE = ENV_LOGSHIPNODE,
            ENVCHANGE_PROMOTETRANSACTION = ENV_PROMOTETRANSACTION,
            ENVCHANGE_TRANSACTIONMANAGERADDRESS = ENV_TRANSACTIONMANAGERADDRESS,
            ENVCHANGE_TRANSACTIONENDED = ENV_TRANSACTIONENDED,
            ENVCHANGE_SPRESETCONNECTIONACK = ENV_SPRESETCONNECTIONACK,
            ENVCHANGE_USERINSTANCE = ENV_USERINSTANCE,
            ENVCHANGE_ROUTING = ENV_ROUTING
        }

        // done status stream bit masks
        public const int DONE_MORE = 0x0001; // more command results coming
        public const int DONE_ERROR = 0x0002; // error in command batch
        public const int DONE_INXACT = 0x0004; // transaction in progress
        public const int DONE_PROC = 0x0008; // done from stored proc
        public const int DONE_COUNT = 0x0010; // count in done info
        public const int DONE_ATTN = 0x0020; // oob ack
        public const int DONE_INPROC = 0x0040; // like DONE_PROC except proc had error
        public const int DONE_RPCINBATCH = 0x0080; // Done from RPC in batch
        public const int DONE_SRVERROR = 0x0100; // Severe error in which resultset should be discarded
        public const int DONE_FMTSENT = 0x8000; // fmt message sent, done_inproc req'd

        // Feature Extension
        public const byte FEATUREEXT_TERMINATOR = 0xFF;
        public const byte FEATUREEXT_SRECOVERY = 0x01;
        public const byte FEATUREEXT_FEDAUTH = 0x02;
        public const byte FEATUREEXT_GLOBALTRANSACTIONS = 0x05;
        public const byte FEATUREEXT_UTF8SUPPORT = 0x0A;

        [Flags]
        public enum FeatureExtension : uint
        {
            None = 0,
            SessionRecovery = 1,
            FedAuth = 2,
            GlobalTransactions = 16,
            UTF8Support = 512,
        }

        public const uint UTF8_IN_TDSCOLLATION = 0x4000000;

        public const byte FEDAUTHLIB_LIVEID = 0X00;
        public const byte FEDAUTHLIB_SECURITYTOKEN = 0x01;
        public const byte FEDAUTHLIB_ADAL = 0x02;
        public const byte FEDAUTHLIB_RESERVED = 0X7F;

        public enum FedAuthLibrary : byte
        {
            LiveId = FEDAUTHLIB_LIVEID,
            SecurityToken = FEDAUTHLIB_SECURITYTOKEN,
            ADAL = FEDAUTHLIB_ADAL, // For later support
            Default = FEDAUTHLIB_RESERVED
        }

        //    Loginrec defines
        public const byte MAX_LOG_NAME = 30;              // TDS 4.2 login rec max name length
        public const byte MAX_PROG_NAME = 10;              // max length of loginrec program name
        public const byte SEC_COMP_LEN = 8;               // length of security compartments
        public const byte MAX_PK_LEN = 6;               // max length of TDS packet size
        public const byte MAX_NIC_SIZE = 6;               // The size of a MAC or client address
        public const byte SQLVARIANT_SIZE = 2;               // size of the fixed portion of a sql variant (type, cbPropBytes)
        public const byte VERSION_SIZE = 4;               // size of the tds version (4 unsigned bytes)
        public const int CLIENT_PROG_VER = 0x06000000;      // Client interface version       
        public const int YUKON_LOG_REC_FIXED_LEN = 0x5e;
        // misc
        public const int TEXT_TIME_STAMP_LEN = 8;
        public const int COLLATION_INFO_LEN = 4;

        /*
                public const byte INT4_LSB_HI   = 0;     // lsb is low byte (e.g. 68000)
                //    public const byte INT4_LSB_LO   = 1;     // lsb is low byte (e.g. VAX)
                public const byte INT2_LSB_HI   = 2;     // lsb is low byte (e.g. 68000)
                //    public const byte INT2_LSB_LO   = 3;     // lsb is low byte (e.g. VAX)
                public const byte FLT_IEEE_HI   = 4;     // lsb is low byte (e.g. 68000)
                public const byte CHAR_ASCII    = 6;     // ASCII character set
                public const byte TWO_I4_LSB_HI = 8;     // lsb is low byte (e.g. 68000
                //    public const byte TWO_I4_LSB_LO = 9;     // lsb is low byte (e.g. VAX)
                //    public const byte FLT_IEEE_LO   = 10;    // lsb is low byte (e.g. MSDOS)
                public const byte FLT4_IEEE_HI  = 12;    // IEEE 4-byte floating point -lsb is high byte
                //    public const byte FLT4_IEEE_LO  = 13;    // IEEE 4-byte floating point -lsb is low byte
                public const byte TWO_I2_LSB_HI = 16;    // lsb is high byte
                //    public const byte TWO_I2_LSB_LO = 17;    // lsb is low byte

                public const byte LDEFSQL     = 0;    // server sends its default
                public const byte LDEFUSER    = 0;    // regular old user
                public const byte LINTEGRATED = 8;    // integrated security login
        */

        /* Versioning scheme table:

            Client sends:
            0x70000000 -> Sphinx
            0x71000000 -> Shiloh RTM
            0x71000001 -> Shiloh SP1
            0x72xx0002 -> Yukon RTM

            Server responds:
            0x07000000 -> Sphinx     // Notice server response format is different for bwd compat
            0x07010000 -> Shiloh RTM // Notice server response format is different for bwd compat
            0x71000001 -> Shiloh SP1
            0x72xx0002 -> Yukon RTM
        */


        // Shiloh SP1 and beyond versioning scheme:

        // Majors:
        public const int YUKON_MAJOR = 0x72;     // the high-byte is sufficient to distinguish later versions
        public const int KATMAI_MAJOR = 0x73;
        public const int DENALI_MAJOR = 0x74;

        // Increments:
        public const int YUKON_INCREMENT = 0x09;
        public const int KATMAI_INCREMENT = 0x0b;
        public const int DENALI_INCREMENT = 0x00;

        // Minors:
        public const int YUKON_RTM_MINOR = 0x0002;
        public const int KATMAI_MINOR = 0x0003;
        public const int DENALI_MINOR = 0x0004;

        public const int ORDER_68000 = 1;
        public const int USE_DB_ON = 1;
        public const int INIT_DB_FATAL = 1;
        public const int SET_LANG_ON = 1;
        public const int INIT_LANG_FATAL = 1;
        public const int ODBC_ON = 1;
        public const int SSPI_ON = 1;
        public const int REPL_ON = 3;


        // send the read-only intent to the server
        public const int READONLY_INTENT_ON = 1;

        // Token masks
        public const byte SQLLenMask = 0x30;    // mask to check for length tokens
        public const byte SQLFixedLen = 0x30;    // Mask to check for fixed token
        public const byte SQLVarLen = 0x20;    // Value to check for variable length token
        public const byte SQLZeroLen = 0x10;    // Value to check for zero length token
        public const byte SQLVarCnt = 0x00;    // Value to check for variable count token

        // Token masks for COLINFO status
        public const byte SQLDifferentName = 0x20; // column name different than select list name
        public const byte SQLExpression = 0x4;     // column was result of an expression
        public const byte SQLKey = 0x8;            // column is part of the key for the table
        public const byte SQLHidden = 0x10;        // column not part of select list but added because part of key

        // Token masks for COLMETADATA flags
        //   first byte
        public const byte Nullable = 0x1;
        public const byte Identity = 0x10;
        public const byte Updatability = 0xb;   // mask off bits 3 and 4
        //   second byte
        public const byte ClrFixedLen = 0x1;    // Fixed length CLR type
        public const byte IsColumnSet = 0x4;    // Column is an XML representation of an aggregation of other columns

        // null values
        public const uint VARLONGNULL = 0xffffffff; // null value for text and image types
        public const int VARNULL = 0xffff;    // null value for character and binary types
        public const int MAXSIZE = 8000; // max size for any column
        public const byte FIXEDNULL = 0;
        public const ulong UDTNULL = 0xffffffffffffffff;

        // SQL Server Data Type Tokens.
        public const int SQLVOID = 0x1f;
        public const int SQLTEXT = 0x23;
        public const int SQLVARBINARY = 0x25;
        public const int SQLINTN = 0x26;
        public const int SQLVARCHAR = 0x27;
        public const int SQLBINARY = 0x2d;
        public const int SQLIMAGE = 0x22;
        public const int SQLCHAR = 0x2f;
        public const int SQLINT1 = 0x30;
        public const int SQLBIT = 0x32;
        public const int SQLINT2 = 0x34;
        public const int SQLINT4 = 0x38;
        public const int SQLMONEY = 0x3c;
        public const int SQLDATETIME = 0x3d;
        public const int SQLFLT8 = 0x3e;
        public const int SQLFLTN = 0x6d;
        public const int SQLMONEYN = 0x6e;
        public const int SQLDATETIMN = 0x6f;
        public const int SQLFLT4 = 0x3b;
        public const int SQLMONEY4 = 0x7a;
        public const int SQLDATETIM4 = 0x3a;
        public const int SQLDECIMALN = 0x6a;
        public const int SQLNUMERICN = 0x6c;
        public const int SQLUNIQUEID = 0x24;
        public const int SQLBIGCHAR = 0xaf;
        public const int SQLBIGVARCHAR = 0xa7;
        public const int SQLBIGBINARY = 0xad;
        public const int SQLBIGVARBINARY = 0xa5;
        public const int SQLBITN = 0x68;
        public const int SQLNCHAR = 0xef;
        public const int SQLNVARCHAR = 0xe7;
        public const int SQLNTEXT = 0x63;
        public const int SQLUDT = 0xF0;

        // aggregate operator type TDS tokens, used by compute statements:
        public const int AOPCNTB = 0x09;
        public const int AOPSTDEV = 0x30;
        public const int AOPSTDEVP = 0x31;
        public const int AOPVAR = 0x32;
        public const int AOPVARP = 0x33;

        public const int AOPCNT = 0x4b;
        public const int AOPSUM = 0x4d;
        public const int AOPAVG = 0x4f;
        public const int AOPMIN = 0x51;
        public const int AOPMAX = 0x52;
        public const int AOPANY = 0x53;
        public const int AOPNOOP = 0x56;

        // SQL Server user-defined type tokens we care about
        public const int SQLTIMESTAMP = 0x50;

        public const int MAX_NUMERIC_LEN = 0x11; // 17 bytes of data for max numeric/decimal length
        public const int DEFAULT_NUMERIC_PRECISION = 0x1D; // 29 is the default max numeric precision(Decimal.MaxValue) if not user set
        public const int SPHINX_DEFAULT_NUMERIC_PRECISION = 0x1C; // 28 is the default max numeric precision for Sphinx(Decimal.MaxValue doesn't work for sphinx)
        public const int MAX_NUMERIC_PRECISION = 0x26; // 38 is max numeric precision;
        public const byte UNKNOWN_PRECISION_SCALE = 0xff; // -1 is value for unknown precision or scale

        // The following datatypes are specific to SHILOH (version 8) and later.
        public const int SQLINT8 = 0x7f;
        public const int SQLVARIANT = 0x62;

        // The following datatypes are specific to Yukon (version 9) or later
        public const int SQLXMLTYPE = 0xf1;
        public const int XMLUNICODEBOM = 0xfeff;
        public static readonly byte[] XMLUNICODEBOMBYTES = { 0xff, 0xfe };

        // The following datatypes are specific to Katmai (version 10) or later
        public const int SQLTABLE = 0xf3;
        public const int SQLDATE = 0x28;
        public const int SQLTIME = 0x29;
        public const int SQLDATETIME2 = 0x2a;
        public const int SQLDATETIMEOFFSET = 0x2b;

        public const int DEFAULT_VARTIME_SCALE = 7;

        //Partially length prefixed datatypes constants. These apply to XMLTYPE, BIGVARCHRTYPE,
        // NVARCHARTYPE, and BIGVARBINTYPE. Valid for Yukon or later

        public const ulong SQL_PLP_NULL = 0xffffffffffffffff;        // Represents null value
        public const ulong SQL_PLP_UNKNOWNLEN = 0xfffffffffffffffe;  // Data coming in chunks, total length unknown
        public const int SQL_PLP_CHUNK_TERMINATOR = 0x00000000;     // Represents end of chunked data.
        public const ushort SQL_USHORTVARMAXLEN = 0xffff;          // Second ushort in TDS stream is this value if one of max types

        // TVPs require some new in-value control tokens:
        public const byte TVP_ROWCOUNT_ESTIMATE = 0x12;
        public const byte TVP_ROW_TOKEN = 0x01;
        public const byte TVP_END_TOKEN = 0x00;
        public const ushort TVP_NOMETADATA_TOKEN = 0xFFFF;
        public const byte TVP_ORDER_UNIQUE_TOKEN = 0x10;

        // TvpColumnMetaData flags
        public const int TVP_DEFAULT_COLUMN = 0x200;

        // TVP_ORDER_UNIQUE_TOKEN flags
        public const byte TVP_ORDERASC_FLAG = 0x1;
        public const byte TVP_ORDERDESC_FLAG = 0x2;
        public const byte TVP_UNIQUE_FLAG = 0x4;


        // RPC function names
        public const string SP_EXECUTESQL = "sp_executesql";       // used against 7.0 servers
        public const string SP_PREPEXEC = "sp_prepexec";         // used against 7.5 servers

        public const string SP_PREPARE = "sp_prepare";          // used against 7.0 servers
        public const string SP_EXECUTE = "sp_execute";
        public const string SP_UNPREPARE = "sp_unprepare";
        public const string SP_PARAMS = "sp_procedure_params_rowset";
        public const string SP_PARAMS_MANAGED = "sp_procedure_params_managed";
        public const string SP_PARAMS_MGD10 = "sp_procedure_params_100_managed";

        // RPC ProcID's
        // NOTE: It is more efficient to call these procs using ProcID's instead of names
        public const ushort RPC_PROCID_CURSOR = 1;
        public const ushort RPC_PROCID_CURSOROPEN = 2;
        public const ushort RPC_PROCID_CURSORPREPARE = 3;
        public const ushort RPC_PROCID_CURSOREXECUTE = 4;
        public const ushort RPC_PROCID_CURSORPREPEXEC = 5;
        public const ushort RPC_PROCID_CURSORUNPREPARE = 6;
        public const ushort RPC_PROCID_CURSORFETCH = 7;
        public const ushort RPC_PROCID_CURSOROPTION = 8;
        public const ushort RPC_PROCID_CURSORCLOSE = 9;
        public const ushort RPC_PROCID_EXECUTESQL = 10;
        public const ushort RPC_PROCID_PREPARE = 11;
        public const ushort RPC_PROCID_EXECUTE = 12;
        public const ushort RPC_PROCID_PREPEXEC = 13;
        public const ushort RPC_PROCID_PREPEXECRPC = 14;
        public const ushort RPC_PROCID_UNPREPARE = 15;

        // For Transactions
        public const string TRANS_BEGIN = "BEGIN TRANSACTION";
        public const string TRANS_COMMIT = "COMMIT TRANSACTION";
        public const string TRANS_ROLLBACK = "ROLLBACK TRANSACTION";
        public const string TRANS_IF_ROLLBACK = "IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION";
        public const string TRANS_SAVE = "SAVE TRANSACTION";

        // For Transactions - isolation levels
        public const string TRANS_READ_COMMITTED = "SET TRANSACTION ISOLATION LEVEL READ COMMITTED";
        public const string TRANS_READ_UNCOMMITTED = "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED";
        public const string TRANS_REPEATABLE_READ = "SET TRANSACTION ISOLATION LEVEL REPEATABLE READ";
        public const string TRANS_SERIALIZABLE = "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE";
        public const string TRANS_SNAPSHOT = "SET TRANSACTION ISOLATION LEVEL SNAPSHOT";

        // Batch RPC flags
        public const byte SHILOH_RPCBATCHFLAG = 0x80;
        public const byte YUKON_RPCBATCHFLAG = 0xFF;

        // RPC flags
        public const byte RPC_RECOMPILE = 0x1;
        public const byte RPC_NOMETADATA = 0x2;

        // RPC parameter class
        public const byte RPC_PARAM_BYREF = 0x1;
        public const byte RPC_PARAM_DEFAULT = 0x2;
        public const byte RPC_PARAM_IS_LOB_COOKIE = 0x8;

        // SQL parameter list text
        public const string PARAM_OUTPUT = "output";

        // SQL Parameter constants
        public const int MAX_PARAMETER_NAME_LENGTH = 128;

        // metadata options (added around an existing sql statement)

        // prefixes
        public const string FMTONLY_ON = " SET FMTONLY ON;";
        public const string FMTONLY_OFF = " SET FMTONLY OFF;";
        // suffixes
        public const string BROWSE_ON = " SET NO_BROWSETABLE ON;";
        public const string BROWSE_OFF = " SET NO_BROWSETABLE OFF;";

        // generic table name
        public const string TABLE = "Table";

        public const int EXEC_THRESHOLD = 0x3; // if the number of commands we execute is > than this threshold, than do prep/exec/unprep instead
        // of executesql.

        // dbnetlib error values
        public const short TIMEOUT_EXPIRED = -2;
        public const short ENCRYPTION_NOT_SUPPORTED = 20;
        // CAUTION: These are not error codes returned by SNI. This is used for backward compatibility
        // since netlib (now removed from sqlclient) returned these codes.

        // SQL error values (from sqlerrorcodes.h)
        public const int LOGON_FAILED = 18456;
        public const int PASSWORD_EXPIRED = 18488;
        public const int IMPERSONATION_FAILED = 1346;
        public const int P_TOKENTOOLONG = 103;

        // SNI\Win32 error values
        // NOTE: these are simply windows system error codes, not SNI specific
        public const uint SNI_UNINITIALIZED = unchecked((uint)-1);
        public const uint SNI_SUCCESS = 0;        // The operation completed successfully.
        public const uint SNI_ERROR = 1;          // Error
        public const uint SNI_WAIT_TIMEOUT = 258;      // The wait operation timed out.
        public const uint SNI_SUCCESS_IO_PENDING = 997;      // Overlapped I/O operation is in progress.

        // Windows Sockets Error Codes
        public const short SNI_WSAECONNRESET = 10054;    // An existing connection was forcibly closed by the remote host.

        // SNI internal errors (shouldn't overlap with Win32 / socket errors)
        public const uint SNI_QUEUE_FULL = 1048576;		 // Packet queue is full

        // SNI flags
        public const uint SNI_SSL_VALIDATE_CERTIFICATE = 1;   // This enables validation of server certificate
        public const uint SNI_SSL_USE_SCHANNEL_CACHE = 2;     // This enables schannel session cache
        public const uint SNI_SSL_IGNORE_CHANNEL_BINDINGS = 0x10; // Used with SSL Provider, sent to SNIAddProvider in case of SQL Authentication & Encrypt.

        public const string DEFAULT_ENGLISH_CODE_PAGE_STRING = "iso_1";
        public const short DEFAULT_ENGLISH_CODE_PAGE_VALUE = 1252;
        public const short CHARSET_CODE_PAGE_OFFSET = 2;
        internal const int MAX_SERVERNAME = 255;

        // Sql Statement Tokens in the DONE packet
        // (see ntdbms\ntinc\tokens.h)
        //
        internal const ushort SELECT = 0xc1;
        internal const ushort INSERT = 0xc3;
        internal const ushort DELETE = 0xc4;
        internal const ushort UPDATE = 0xc5;
        internal const ushort ABORT = 0xd2;
        internal const ushort BEGINXACT = 0xd4;
        internal const ushort ENDXACT = 0xd5;
        internal const ushort BULKINSERT = 0xf0;
        internal const ushort OPENCURSOR = 0x20;
        internal const ushort MERGE = 0x117;


        // Login data validation Rules
        //
        internal const ushort MAXLEN_HOSTNAME = 128; // the client machine name
        internal const ushort MAXLEN_USERNAME = 128; // the client user id
        internal const ushort MAXLEN_PASSWORD = 128; // the password supplied by the client
        internal const ushort MAXLEN_APPNAME = 128; // the client application name
        internal const ushort MAXLEN_SERVERNAME = 128; // the server name
        internal const ushort MAXLEN_CLIENTINTERFACE = 128; // the interface library name
        internal const ushort MAXLEN_LANGUAGE = 128; // the initial language
        internal const ushort MAXLEN_DATABASE = 128; // the initial database
        internal const ushort MAXLEN_ATTACHDBFILE = 260; // the filename for a database that is to be attached during the connection process
        internal const ushort MAXLEN_NEWPASSWORD = 128; // new password for the specified login.


        // array copied directly from tdssort.h from luxor
        public static readonly ushort[] CODE_PAGE_FROM_SORT_ID = {
            0,      /*   0 */
            0,      /*   1 */
            0,      /*   2 */
            0,      /*   3 */
            0,      /*   4 */
            0,      /*   5 */
            0,      /*   6 */
            0,      /*   7 */
            0,      /*   8 */
            0,      /*   9 */
            0,      /*  10 */
            0,      /*  11 */
            0,      /*  12 */
            0,      /*  13 */
            0,      /*  14 */
            0,      /*  15 */
            0,      /*  16 */
            0,      /*  17 */
            0,      /*  18 */
            0,      /*  19 */
            0,      /*  20 */
            0,      /*  21 */
            0,      /*  22 */
            0,      /*  23 */
            0,      /*  24 */
            0,      /*  25 */
            0,      /*  26 */
            0,      /*  27 */
            0,      /*  28 */
            0,      /*  29 */
            437,    /*  30 */
            437,    /*  31 */
            437,    /*  32 */
            437,    /*  33 */
            437,    /*  34 */
            0,      /*  35 */
            0,      /*  36 */
            0,      /*  37 */
            0,      /*  38 */
            0,      /*  39 */
            850,    /*  40 */
            850,    /*  41 */
            850,    /*  42 */
            850,    /*  43 */
            850,    /*  44 */
            0,      /*  45 */
            0,      /*  46 */
            0,      /*  47 */
            0,      /*  48 */
            850,    /*  49 */
            1252,   /*  50 */
            1252,   /*  51 */
            1252,   /*  52 */
            1252,   /*  53 */
            1252,   /*  54 */
            850,    /*  55 */
            850,    /*  56 */
            850,    /*  57 */
            850,    /*  58 */
            850,    /*  59 */
            850,    /*  60 */
            850,    /*  61 */
            0,      /*  62 */
            0,      /*  63 */
            0,      /*  64 */
            0,      /*  65 */
            0,      /*  66 */
            0,      /*  67 */
            0,      /*  68 */
            0,      /*  69 */
            0,      /*  70 */
            1252,   /*  71 */
            1252,   /*  72 */
            1252,   /*  73 */
            1252,   /*  74 */
            1252,   /*  75 */
            0,      /*  76 */
            0,      /*  77 */
            0,      /*  78 */
            0,      /*  79 */
            1250,   /*  80 */
            1250,   /*  81 */
            1250,   /*  82 */
            1250,   /*  83 */
            1250,   /*  84 */
            1250,   /*  85 */
            1250,   /*  86 */
            1250,   /*  87 */
            1250,   /*  88 */
            1250,   /*  89 */
            1250,   /*  90 */
            1250,   /*  91 */
            1250,   /*  92 */
            1250,   /*  93 */
            1250,   /*  94 */
            1250,   /*  95 */
            1250,   /*  96 */
            1250,   /*  97 */
            1250,   /*  98 */
            0,      /*  99 */
            0,      /* 100 */
            0,      /* 101 */
            0,      /* 102 */
            0,      /* 103 */
            1251,   /* 104 */
            1251,   /* 105 */
            1251,   /* 106 */
            1251,   /* 107 */
            1251,   /* 108 */
            0,      /* 109 */
            0,      /* 110 */
            0,      /* 111 */
            1253,   /* 112 */
            1253,   /* 113 */
            1253,   /* 114 */
            0,      /* 115 */
            0,      /* 116 */
            0,      /* 117 */
            0,      /* 118 */
            0,      /* 119 */
            1253,   /* 120 */
            1253,   /* 121 */
            1253,   /* 122 */
            0,      /* 123 */
            1253,   /* 124 */
            0,      /* 125 */
            0,      /* 126 */
            0,      /* 127 */
            1254,   /* 128 */
            1254,   /* 129 */
            1254,   /* 130 */
            0,      /* 131 */
            0,      /* 132 */
            0,      /* 133 */
            0,      /* 134 */
            0,      /* 135 */
            1255,   /* 136 */
            1255,   /* 137 */
            1255,   /* 138 */
            0,      /* 139 */
            0,      /* 140 */
            0,      /* 141 */
            0,      /* 142 */
            0,      /* 143 */
            1256,   /* 144 */
            1256,   /* 145 */
            1256,   /* 146 */
            0,      /* 147 */
            0,      /* 148 */
            0,      /* 149 */
            0,      /* 150 */
            0,      /* 151 */
            1257,   /* 152 */
            1257,   /* 153 */
            1257,   /* 154 */
            1257,   /* 155 */
            1257,   /* 156 */
            1257,   /* 157 */
            1257,   /* 158 */
            1257,   /* 159 */
            1257,   /* 160 */
            0,      /* 161 */
            0,      /* 162 */
            0,      /* 163 */
            0,      /* 164 */
            0,      /* 165 */
            0,      /* 166 */
            0,      /* 167 */
            0,      /* 168 */
            0,      /* 169 */
            0,      /* 170 */
            0,      /* 171 */
            0,      /* 172 */
            0,      /* 173 */
            0,      /* 174 */
            0,      /* 175 */
            0,      /* 176 */
            0,      /* 177 */
            0,      /* 178 */
            0,      /* 179 */
            0,      /* 180 */
            0,      /* 181 */
            0,      /* 182 */
            1252,   /* 183 */
            1252,   /* 184 */
            1252,   /* 185 */
            1252,   /* 186 */
            0,      /* 187 */
            0,      /* 188 */
            0,      /* 189 */
            0,      /* 190 */
            0,      /* 191 */
            932,    /* 192 */
            932,    /* 193 */
            949,    /* 194 */
            949,    /* 195 */
            950,    /* 196 */
            950,    /* 197 */
            936,    /* 198 */
            936,    /* 199 */
            932,    /* 200 */
            949,    /* 201 */
            950,    /* 202 */
            936,    /* 203 */
            874,    /* 204 */
            874,    /* 205 */
            874,    /* 206 */
            0,      /* 207 */
            0,      /* 208 */
            0,      /* 209 */
            1252,   /* 210 */
            1252,   /* 211 */
            1252,   /* 212 */
            1252,   /* 213 */
            1252,   /* 214 */
            1252,   /* 215 */
            1252,   /* 216 */
            1252,   /* 217 */
            0,      /* 218 */
            0,      /* 219 */
            0,      /* 220 */
            0,      /* 221 */
            0,      /* 222 */
            0,      /* 223 */
            0,      /* 224 */
            0,      /* 225 */
            0,      /* 226 */
            0,      /* 227 */
            0,      /* 228 */
            0,      /* 229 */
            0,      /* 230 */
            0,      /* 231 */
            0,      /* 232 */
            0,      /* 233 */
            0,      /* 234 */
            0,      /* 235 */
            0,      /* 236 */
            0,      /* 237 */
            0,      /* 238 */
            0,      /* 239 */
            0,      /* 240 */
            0,      /* 241 */
            0,      /* 242 */
            0,      /* 243 */
            0,      /* 244 */
            0,      /* 245 */
            0,      /* 246 */
            0,      /* 247 */
            0,      /* 248 */
            0,      /* 249 */
            0,      /* 250 */
            0,      /* 251 */
            0,      /* 252 */
            0,      /* 253 */
            0,      /* 254 */
            0,      /* 255 */
        };


        internal enum TransactionManagerRequestType
        {
            GetDTCAddress = 0,
            Propagate = 1,
            Begin = 5,
            Promote = 6,
            Commit = 7,
            Rollback = 8,
            Save = 9
        };

        internal enum TransactionManagerIsolationLevel
        {
            Unspecified = 0x00,
            ReadUncommitted = 0x01,
            ReadCommitted = 0x02,
            RepeatableRead = 0x03,
            Serializable = 0x04,
            Snapshot = 0x05
        }

        internal enum GenericType
        {
            MultiSet = 131,
        };

        // Date, Time, DateTime2, DateTimeOffset specific constants
        internal static readonly long[] TICKS_FROM_SCALE = {
            10000000,
            1000000,
            100000,
            10000,
            1000,
            100,
            10,
            1,
        };

        internal const int WHIDBEY_DATE_LENGTH = 10;
        internal static readonly int[] WHIDBEY_TIME_LENGTH = { 8, 10, 11, 12, 13, 14, 15, 16 };
        internal static readonly int[] WHIDBEY_DATETIME2_LENGTH = { 19, 21, 22, 23, 24, 25, 26, 27 };
        internal static readonly int[] WHIDBEY_DATETIMEOFFSET_LENGTH = { 26, 28, 29, 30, 31, 32, 33, 34 };

        // Needed for UapAot, since we cannot use Enum.GetName() on SniContext.
        // Enum.GetName() uses reflection, which is blocked on UapAot for internal types
        // like SniContext.
        internal static string GetSniContextEnumName(SniContext sniContext)
        {
            switch (sniContext)
            {
                case SniContext.Undefined:
                    return "Undefined";
                case SniContext.Snix_Connect:
                    return "Snix_Connect";
                case SniContext.Snix_PreLoginBeforeSuccessfulWrite:
                    return "Snix_PreLoginBeforeSuccessfulWrite";
                case SniContext.Snix_PreLogin:
                    return "Snix_PreLogin";
                case SniContext.Snix_LoginSspi:
                    return "Snix_LoginSspi";
                case SniContext.Snix_ProcessSspi:
                    return "Snix_ProcessSspi";
                case SniContext.Snix_Login:
                    return "Snix_Login";
                case SniContext.Snix_EnableMars:
                    return "Snix_EnableMars";
                case SniContext.Snix_AutoEnlist:
                    return "Snix_AutoEnlist";
                case SniContext.Snix_GetMarsSession:
                    return "Snix_GetMarsSession";
                case SniContext.Snix_Execute:
                    return "Snix_Execute";
                case SniContext.Snix_Read:
                    return "Snix_Read";
                case SniContext.Snix_Close:
                    return "Snix_Close";
                case SniContext.Snix_SendRows:
                    return "Snix_SendRows";
                default:
                    Debug.Fail($"Received unknown SniContext enum. Value: {sniContext}");
                    return null;
            }
        }
    }

    internal enum SniContext
    {
        Undefined = 0,
        Snix_Connect,
        Snix_PreLoginBeforeSuccessfulWrite,
        Snix_PreLogin,
        Snix_LoginSspi,
        Snix_ProcessSspi,
        Snix_Login,
        Snix_EnableMars,
        Snix_AutoEnlist,
        Snix_GetMarsSession,
        Snix_Execute,
        Snix_Read,
        Snix_Close,
        Snix_SendRows,
    }

    internal enum ParsingErrorState
    {
        Undefined = 0,
        FedAuthInfoLengthTooShortForCountOfInfoIds = 1,
        FedAuthInfoLengthTooShortForData = 2,
        FedAuthInfoFailedToReadCountOfInfoIds = 3,
        FedAuthInfoFailedToReadTokenStream = 4,
        FedAuthInfoInvalidOffset = 5,
        FedAuthInfoFailedToReadData = 6,
        FedAuthInfoDataNotUnicode = 7,
        FedAuthInfoDoesNotContainStsurlAndSpn = 8,
        FedAuthInfoNotReceived = 9,
        FedAuthNotAcknowledged = 10,
        FedAuthFeatureAckContainsExtraData = 11,
        FedAuthFeatureAckUnknownLibraryType = 12,
        UnrequestedFeatureAckReceived = 13,
        UnknownFeatureAck = 14,
        InvalidTdsTokenReceived = 15,
        SessionStateLengthTooShort = 16,
        SessionStateInvalidStatus = 17,
        CorruptedTdsStream = 18,
        ProcessSniPacketFailed = 19,
        FedAuthRequiredPreLoginResponseInvalidValue = 20,
    }
}

