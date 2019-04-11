// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Security;
using System.Text;
using System.Threading;
using Microsoft.SqlServer.Server;

namespace System.Data.SqlClient
{
    internal enum CallbackType
    {
        Read = 0,
        Write = 1
    }

    internal enum EncryptionOptions
    {
        OFF,
        ON,
        NOT_SUP,
        REQ,
        LOGIN
    }

    internal enum PreLoginHandshakeStatus
    {
        Successful,
        InstanceFailure
    }

    internal enum PreLoginOptions
    {
        VERSION,
        ENCRYPT,
        INSTANCE,
        THREADID,
        MARS,
        TRACEID,
        FEDAUTHREQUIRED,
        NUMOPT,
        LASTOPT = 255
    }

    internal enum RunBehavior
    {
        UntilDone = 1, // 0001 binary
        ReturnImmediately = 2, // 0010 binary
        Clean = 5, // 0101 binary - Clean AND UntilDone
        Attention = 13  // 1101 binary - Clean AND UntilDone AND Attention
    }

    internal enum TdsParserState
    {
        Closed,
        OpenNotLoggedIn,
        OpenLoggedIn,
        Broken,
    }

    /// <summary>
    /// Struct encapsulating the data to be sent to the server as part of Federated Authentication Feature Extension.
    /// </summary>
    internal struct FederatedAuthenticationFeatureExtensionData
    {
        internal TdsEnums.FedAuthLibrary libraryType;
        internal bool fedAuthRequiredPreLoginResponse;
        internal byte[] accessToken;
    }

    internal sealed class SqlCollation
    {
        // First 20 bits of info field represent the lcid, bits 21-25 are compare options
        private const uint IgnoreCase = 1 << 20; // bit 21 - IgnoreCase
        private const uint IgnoreNonSpace = 1 << 21; // bit 22 - IgnoreNonSpace / IgnoreAccent
        private const uint IgnoreWidth = 1 << 22; // bit 23 - IgnoreWidth
        private const uint IgnoreKanaType = 1 << 23; // bit 24 - IgnoreKanaType
        private const uint BinarySort = 1 << 24; // bit 25 - BinarySort

        internal const uint MaskLcid = 0xfffff;
        private const int LcidVersionBitOffset = 28;
        private const uint MaskLcidVersion = unchecked((uint)(0xf << LcidVersionBitOffset));
        private const uint MaskCompareOpt = IgnoreCase | IgnoreNonSpace | IgnoreWidth | IgnoreKanaType | BinarySort;

        internal uint info;
        internal byte sortId;

        private static int FirstSupportedCollationVersion(int lcid)
        {
            // NOTE: switch-case works ~3 times faster in this case than search with Dictionary
            switch (lcid)
            {
                case 1044: return 2; // Norwegian_100_BIN
                case 1047: return 2; // Romansh_100_BIN
                case 1056: return 2; // Urdu_100_BIN
                case 1065: return 2; // Persian_100_BIN
                case 1068: return 2; // Azeri_Latin_100_BIN
                case 1070: return 2; // Upper_Sorbian_100_BIN
                case 1071: return 1; // Macedonian_FYROM_90_BIN
                case 1081: return 1; // Indic_General_90_BIN
                case 1082: return 2; // Maltese_100_BIN
                case 1083: return 2; // Sami_Norway_100_BIN
                case 1087: return 1; // Kazakh_90_BIN
                case 1090: return 2; // Turkmen_100_BIN
                case 1091: return 1; // Uzbek_Latin_90_BIN
                case 1092: return 1; // Tatar_90_BIN
                case 1093: return 2; // Bengali_100_BIN
                case 1101: return 2; // Assamese_100_BIN
                case 1105: return 2; // Tibetan_100_BIN
                case 1106: return 2; // Welsh_100_BIN
                case 1107: return 2; // Khmer_100_BIN
                case 1108: return 2; // Lao_100_BIN
                case 1114: return 1; // Syriac_90_BIN
                case 1121: return 2; // Nepali_100_BIN
                case 1122: return 2; // Frisian_100_BIN
                case 1123: return 2; // Pashto_100_BIN
                case 1125: return 1; // Divehi_90_BIN
                case 1133: return 2; // Bashkir_100_BIN
                case 1146: return 2; // Mapudungan_100_BIN
                case 1148: return 2; // Mohawk_100_BIN
                case 1150: return 2; // Breton_100_BIN
                case 1152: return 2; // Uighur_100_BIN
                case 1153: return 2; // Maori_100_BIN
                case 1155: return 2; // Corsican_100_BIN
                case 1157: return 2; // Yakut_100_BIN
                case 1164: return 2; // Dari_100_BIN
                case 2074: return 2; // Serbian_Latin_100_BIN
                case 2092: return 2; // Azeri_Cyrillic_100_BIN
                case 2107: return 2; // Sami_Sweden_Finland_100_BIN
                case 2143: return 2; // Tamazight_100_BIN
                case 3076: return 1; // Chinese_Hong_Kong_Stroke_90_BIN
                case 3098: return 2; // Serbian_Cyrillic_100_BIN
                case 5124: return 2; // Chinese_Traditional_Pinyin_100_BIN
                case 5146: return 2; // Bosnian_Latin_100_BIN
                case 8218: return 2; // Bosnian_Cyrillic_100_BIN

                default: return 0;   // other LCIDs have collation with version 0
            }
        }

        internal int LCID
        {
            // First 20 bits of info field represent the lcid
            get
            {
                return unchecked((int)(info & MaskLcid));
            }
            set
            {
                int lcid = value & (int)MaskLcid;
                Debug.Assert(lcid == value, "invalid set_LCID value");

                // Some new Katmai LCIDs do not have collation with version = 0
                // since user has no way to specify collation version, we set the first (minimal) supported version for these collations
                int versionBits = FirstSupportedCollationVersion(lcid) << LcidVersionBitOffset;
                Debug.Assert((versionBits & MaskLcidVersion) == versionBits, "invalid version returned by FirstSupportedCollationVersion");

                // combine the current compare options with the new locale ID and its first supported version
                info = (info & MaskCompareOpt) | unchecked((uint)lcid) | unchecked((uint)versionBits);
            }
        }

        internal SqlCompareOptions SqlCompareOptions
        {
            get
            {
                SqlCompareOptions options = SqlCompareOptions.None;
                if (0 != (info & IgnoreCase))
                    options |= SqlCompareOptions.IgnoreCase;
                if (0 != (info & IgnoreNonSpace))
                    options |= SqlCompareOptions.IgnoreNonSpace;
                if (0 != (info & IgnoreWidth))
                    options |= SqlCompareOptions.IgnoreWidth;
                if (0 != (info & IgnoreKanaType))
                    options |= SqlCompareOptions.IgnoreKanaType;
                if (0 != (info & BinarySort))
                    options |= SqlCompareOptions.BinarySort;
                return options;
            }
            set
            {
                Debug.Assert((value & SqlTypeWorkarounds.SqlStringValidSqlCompareOptionMask) == value, "invalid set_SqlCompareOptions value");
                uint tmp = 0;
                if (0 != (value & SqlCompareOptions.IgnoreCase))
                    tmp |= IgnoreCase;
                if (0 != (value & SqlCompareOptions.IgnoreNonSpace))
                    tmp |= IgnoreNonSpace;
                if (0 != (value & SqlCompareOptions.IgnoreWidth))
                    tmp |= IgnoreWidth;
                if (0 != (value & SqlCompareOptions.IgnoreKanaType))
                    tmp |= IgnoreKanaType;
                if (0 != (value & SqlCompareOptions.BinarySort))
                    tmp |= BinarySort;
                info = (info & MaskLcid) | tmp;
            }
        }


        internal static bool AreSame(SqlCollation a, SqlCollation b)
        {
            if (a == null || b == null)
            {
                return a == b;
            }
            else
            {
                return a.info == b.info && a.sortId == b.sortId;
            }
        }
    }

    internal class RoutingInfo
    {
        internal byte Protocol { get; private set; }
        internal ushort Port { get; private set; }
        internal string ServerName { get; private set; }

        internal RoutingInfo(byte protocol, ushort port, string servername)
        {
            Protocol = protocol;
            Port = port;
            ServerName = servername;
        }
    }

    internal sealed class SqlEnvChange
    {
        internal byte type;
        internal byte oldLength;
        internal int newLength; // 7206 TDS changes makes this length an int
        internal int length;
        internal string newValue;
        internal string oldValue;
        /// <summary>
        /// contains binary data, before using this field check newBinRented to see if you can take the field array or whether you should allocate and copy
        /// </summary>
        internal byte[] newBinValue;
        /// <summary>
        /// contains binary data, before using this field check newBinRented to see if you can take the field array or whether you should allocate and copy
        /// </summary>
        internal byte[] oldBinValue;
        internal long newLongValue;
        internal long oldLongValue;
        internal SqlCollation newCollation;
        internal SqlCollation oldCollation;
        internal RoutingInfo newRoutingInfo;
        internal bool newBinRented;
        internal bool oldBinRented;

        internal SqlEnvChange Next;

        internal void Clear()
        {
            type = 0;
            oldLength = 0;
            newLength = 0;
            length = 0;
            newValue = null;
            oldValue = null;
            if (newBinValue != null)
            {
                Array.Clear(newBinValue, 0, newBinValue.Length);
                if (newBinRented)
                {
                    ArrayPool<byte>.Shared.Return(newBinValue);
                }

                newBinValue = null;
            }
            if (oldBinValue != null)
            {
                Array.Clear(oldBinValue, 0, oldBinValue.Length);
                if (oldBinRented)
                {
                    ArrayPool<byte>.Shared.Return(oldBinValue);
                }
                oldBinValue = null;
            }
            newBinRented = false;
            oldBinRented = false;
            newLongValue = 0;
            oldLongValue = 0;
            newCollation = null;
            oldCollation = null;
            newRoutingInfo = null;
            Next = null;
        }
    }

    internal sealed class SqlLogin
    {
        internal int timeout;                                                       // login timeout
        internal bool userInstance = false;                                   // user instance
        internal string hostName = "";                                      // client machine name
        internal string userName = "";                                      // user id
        internal string password = "";                                      // password
        internal string applicationName = "";                                      // application name
        internal string serverName = "";                                      // server name
        internal string language = "";                                      // initial language
        internal string database = "";                                      // initial database
        internal string attachDBFilename = "";                                      // DB filename to be attached
        internal bool useReplication = false;                                   // user login for replication
        internal string newPassword = "";                                   // new password for reset password
        internal bool useSSPI = false;                                   // use integrated security
        internal int packetSize = SqlConnectionString.DEFAULT.Packet_Size; // packet size
        internal bool readOnlyIntent = false;                                   // read-only intent
        internal SqlCredential credential;                                      // user id and password in SecureString
        internal SecureString newSecurePassword;
    }

    internal sealed class SqlLoginAck
    {
        internal byte majorVersion;
        internal byte minorVersion;
        internal short buildNum;
        internal uint tdsVersion;
    }

    internal sealed class _SqlMetaData : SqlMetaDataPriv
    {
        [Flags]
        private enum _SqlMetadataFlags : int
        {
            None = 0,

            Updatable = 1 << 0,
            UpdateableUnknown = 1 << 1,
            IsDifferentName = 1 << 2,
            IsKey = 1 << 3,
            IsHidden = 1 << 4,
            IsExpression = 1 << 5,
            IsIdentity = 1 << 6,
            IsColumnSet = 1 << 7,

            IsReadOnlyMask = (Updatable | UpdateableUnknown) // two bit field (0 is read only, 1 is updatable, 2 is updatability unknown)
        }

        internal string column;
        internal string baseColumn;
        internal MultiPartTableName multiPartTableName;
        internal readonly int ordinal;
        internal byte tableNum;
        internal byte op;        // for altrow-columns only
        internal ushort operand; // for altrow-columns only
        private _SqlMetadataFlags flags;


        internal _SqlMetaData(int ordinal) : base()
        {
            this.ordinal = ordinal;
        }

        internal string serverName
        {
            get
            {
                return multiPartTableName.ServerName;
            }
        }
        internal string catalogName
        {
            get
            {
                return multiPartTableName.CatalogName;
            }
        }
        internal string schemaName
        {
            get
            {
                return multiPartTableName.SchemaName;
            }
        }
        internal string tableName
        {
            get
            {
                return multiPartTableName.TableName;
            }
        }


        public byte Updatability
        {
            get => (byte)(flags & _SqlMetadataFlags.IsReadOnlyMask);
            set => flags = (_SqlMetadataFlags)((value & 0x3) | ((int)flags & ~0x03));
        }

        public bool IsReadOnly
        {
            get => flags.HasFlag(_SqlMetadataFlags.IsReadOnlyMask);
        }

        public bool IsDifferentName
        {
            get => flags.HasFlag(_SqlMetadataFlags.IsDifferentName);
            set => Set(_SqlMetadataFlags.IsDifferentName, value);
        }

        public bool IsKey
        {
            get => flags.HasFlag(_SqlMetadataFlags.IsKey);
            set => Set(_SqlMetadataFlags.IsKey, value);
        }

        public bool IsHidden
        {
            get => flags.HasFlag(_SqlMetadataFlags.IsHidden);
            set => Set(_SqlMetadataFlags.IsHidden, value);
        }

        public bool IsExpression
        {
            get => flags.HasFlag(_SqlMetadataFlags.IsExpression);
            set => Set(_SqlMetadataFlags.IsExpression, value);
        }

        public bool IsIdentity
        {
            get => flags.HasFlag(_SqlMetadataFlags.IsIdentity);
            set => Set(_SqlMetadataFlags.IsIdentity, value);
        }

        public bool IsColumnSet
        {
            get => flags.HasFlag(_SqlMetadataFlags.IsColumnSet);
            set => Set(_SqlMetadataFlags.IsColumnSet, value);
        }

        private void Set(_SqlMetadataFlags flag, bool value)
        {
            flags = value ? flags | flag : flags & ~flag;
        }

        internal bool IsNewKatmaiDateTimeType
        {
            get
            {
                return SqlDbType.Date == type || SqlDbType.Time == type || SqlDbType.DateTime2 == type || SqlDbType.DateTimeOffset == type;
            }
        }

        internal bool IsLargeUdt
        {
            get
            {
                return type == SqlDbType.Udt && length == int.MaxValue;
            }
        }

        public object Clone()
        {
            _SqlMetaData result = new _SqlMetaData(ordinal);
            result.CopyFrom(this);
            result.column = column;
            result.baseColumn = baseColumn;
            result.multiPartTableName = multiPartTableName;
            result.tableNum = tableNum;
            result.flags = flags;
            result.op = op;
            result.operand = operand;
            return result;
        }
    }

    internal sealed class _SqlMetaDataSet
    {
        internal ushort id;             // for altrow-columns only
        internal int[] indexMap;
        internal int visibleColumns;
        internal DataTable schemaTable;
        private readonly _SqlMetaData[] _metaDataArray;
        internal ReadOnlyCollection<DbColumn> dbColumnSchema;

        internal _SqlMetaDataSet(int count)
        {
            _metaDataArray = new _SqlMetaData[count];
            for (int i = 0; i < _metaDataArray.Length; ++i)
            {
                _metaDataArray[i] = new _SqlMetaData(i);
            }
        }

        private _SqlMetaDataSet(_SqlMetaDataSet original)
        {
            this.id = original.id;
            // although indexMap is not immutable, in practice it is initialized once and then passed around
            this.indexMap = original.indexMap;
            this.visibleColumns = original.visibleColumns;
            this.dbColumnSchema = original.dbColumnSchema;
            if (original._metaDataArray == null)
            {
                _metaDataArray = null;
            }
            else
            {
                _metaDataArray = new _SqlMetaData[original._metaDataArray.Length];
                for (int idx = 0; idx < _metaDataArray.Length; idx++)
                {
                    _metaDataArray[idx] = (_SqlMetaData)original._metaDataArray[idx].Clone();
                }
            }
        }

        internal int Length
        {
            get
            {
                return _metaDataArray.Length;
            }
        }

        internal _SqlMetaData this[int index]
        {
            get
            {
                return _metaDataArray[index];
            }
            set
            {
                Debug.Assert(null == value, "used only by SqlBulkCopy");
                _metaDataArray[index] = value;
            }
        }

        public object Clone()
        {
            return new _SqlMetaDataSet(this);
        }
    }

    internal sealed class _SqlMetaDataSetCollection
    {
        private readonly List<_SqlMetaDataSet> _altMetaDataSetArray;
        internal _SqlMetaDataSet metaDataSet;

        internal _SqlMetaDataSetCollection()
        {
            _altMetaDataSetArray = new List<_SqlMetaDataSet>();
        }

        internal void SetAltMetaData(_SqlMetaDataSet altMetaDataSet)
        {
            // If altmetadata with same id is found, override it rather than adding a new one
            int newId = altMetaDataSet.id;
            for (int i = 0; i < _altMetaDataSetArray.Count; i++)
            {
                if (_altMetaDataSetArray[i].id == newId)
                {
                    // override the existing metadata with the same id
                    _altMetaDataSetArray[i] = altMetaDataSet;
                    return;
                }
            }

            // if we did not find metadata to override, add as new
            _altMetaDataSetArray.Add(altMetaDataSet);
        }

        internal _SqlMetaDataSet GetAltMetaData(int id)
        {
            foreach (_SqlMetaDataSet altMetaDataSet in _altMetaDataSetArray)
            {
                if (altMetaDataSet.id == id)
                {
                    return altMetaDataSet;
                }
            }
            Debug.Fail("Can't match up altMetaDataSet with given id");
            return null;
        }

        public object Clone()
        {
            _SqlMetaDataSetCollection result = new _SqlMetaDataSetCollection();
            result.metaDataSet = metaDataSet == null ? null : (_SqlMetaDataSet)metaDataSet.Clone();
            foreach (_SqlMetaDataSet set in _altMetaDataSetArray)
            {
                result._altMetaDataSetArray.Add((_SqlMetaDataSet)set.Clone());
            }
            return result;
        }
    }

    internal class SqlMetaDataPriv
    {
        [Flags]
        private enum SqlMetaDataPrivFlags : byte
        {
            None = 0,
            IsNullable = 1 << 1,
            IsMultiValued = 1 << 2
        }

        internal SqlDbType type;    // SqlDbType enum value
        internal byte tdsType; // underlying tds type
        internal byte precision = TdsEnums.UNKNOWN_PRECISION_SCALE; // give default of unknown (-1)
        internal byte scale = TdsEnums.UNKNOWN_PRECISION_SCALE; // give default of unknown (-1)
        private SqlMetaDataPrivFlags flags;
        internal int length;
        internal SqlCollation collation;
        internal int codePage;
        internal Encoding encoding;
        internal MetaType metaType; // cached metaType
        public SqlMetaDataUdt udt;
        public SqlMetaDataXmlSchemaCollection xmlSchemaCollection;

        internal SqlMetaDataPriv()
        {
        }

        public bool IsNullable
        {
            get => flags.HasFlag(SqlMetaDataPrivFlags.IsNullable);
            set => Set(SqlMetaDataPrivFlags.IsNullable, value);
        }

        public bool IsMultiValued
        {
            get => flags.HasFlag(SqlMetaDataPrivFlags.IsMultiValued);
            set => Set(SqlMetaDataPrivFlags.IsMultiValued, value);
        }

        private void Set(SqlMetaDataPrivFlags flag, bool value)
        {
            flags = value ? flags | flag : flags & ~flag;
        }

        internal virtual void CopyFrom(SqlMetaDataPriv original)
        {
            this.type = original.type;
            this.tdsType = original.tdsType;
            this.precision = original.precision;
            this.scale = original.scale;
            this.length = original.length;
            this.collation = original.collation;
            this.codePage = original.codePage;
            this.encoding = original.encoding;
            this.metaType = original.metaType;
            this.flags = original.flags;

            if (original.udt != null)
            {
                udt = new SqlMetaDataUdt();
                udt.CopyFrom(original.udt);
            }

            if (original.xmlSchemaCollection != null)
            {
                xmlSchemaCollection = new SqlMetaDataXmlSchemaCollection();
                xmlSchemaCollection.CopyFrom(original.xmlSchemaCollection);
            }
        }
    }

    internal sealed class SqlMetaDataXmlSchemaCollection
    {
        internal string Database;
        internal string OwningSchema;
        internal string Name;

        public void CopyFrom(SqlMetaDataXmlSchemaCollection original)
        {
            if (original != null)
            {
                Database = original.Database;
                OwningSchema = original.OwningSchema;
                Name = original.Name;
            }
        }
    }

    internal sealed class SqlMetaDataUdt
    {
        internal Type Type;
        internal string DatabaseName;
        internal string SchemaName;
        internal string TypeName;
        internal string AssemblyQualifiedName;

        public void CopyFrom(SqlMetaDataUdt original)
        {
            if (original != null)
            {
                Type = original.Type;
                DatabaseName = original.DatabaseName;
                SchemaName = original.SchemaName;
                TypeName = original.TypeName;
                AssemblyQualifiedName = original.AssemblyQualifiedName;
            }
        }
    }

    internal sealed class _SqlRPC
    {
        internal string rpcName;
        internal ushort ProcID;       // Used instead of name
        internal ushort options;

        internal SqlParameter[] systemParams;
        internal byte[] systemParamOptions;
        internal int systemParamCount;

        internal SqlParameterCollection userParams;
        internal long[] userParamMap;
        internal int userParamCount;

        internal int? recordsAffected;
        internal int cumulativeRecordsAffected;

        internal int errorsIndexStart;
        internal int errorsIndexEnd;
        internal SqlErrorCollection errors;

        internal int warningsIndexStart;
        internal int warningsIndexEnd;
        internal SqlErrorCollection warnings;

        internal SqlParameter GetParameterByIndex(int index, out byte options)
        {
            options = 0;
            SqlParameter retval = null;
            if (index < systemParamCount)
            {
                retval = systemParams[index];
                options = systemParamOptions[index];
            }
            else
            {
                long data = userParamMap[index - systemParamCount];
                int paramIndex = (int)(data & int.MaxValue);
                options = (byte)((data >> 32) & 0xFF);
                retval = userParams[paramIndex];
            }
            return retval;
        }
    }

    internal sealed class SqlReturnValue : SqlMetaDataPriv
    {
        internal string parameter;
        internal readonly SqlBuffer value;

        internal SqlReturnValue() : base()
        {
            value = new SqlBuffer();
        }
    }

    internal struct MultiPartTableName
    {
        private string _multipartName;
        private string _serverName;
        private string _catalogName;
        private string _schemaName;
        private string _tableName;

        internal MultiPartTableName(string[] parts)
        {
            _multipartName = null;
            _serverName = parts[0];
            _catalogName = parts[1];
            _schemaName = parts[2];
            _tableName = parts[3];
        }

        internal MultiPartTableName(string multipartName)
        {
            _multipartName = multipartName;
            _serverName = null;
            _catalogName = null;
            _schemaName = null;
            _tableName = null;
        }

        internal string ServerName
        {
            get
            {
                ParseMultipartName();
                return _serverName;
            }
            set { _serverName = value; }
        }
        internal string CatalogName
        {
            get
            {
                ParseMultipartName();
                return _catalogName;
            }
            set { _catalogName = value; }
        }
        internal string SchemaName
        {
            get
            {
                ParseMultipartName();
                return _schemaName;
            }
            set { _schemaName = value; }
        }
        internal string TableName
        {
            get
            {
                ParseMultipartName();
                return _tableName;
            }
            set { _tableName = value; }
        }

        private void ParseMultipartName()
        {
            if (null != _multipartName)
            {
                string[] parts = MultipartIdentifier.ParseMultipartIdentifier(_multipartName, "[\"", "]\"", SR.SQL_TDSParserTableName, false);
                _serverName = parts[0];
                _catalogName = parts[1];
                _schemaName = parts[2];
                _tableName = parts[3];
                _multipartName = null;
            }
        }

        internal static readonly MultiPartTableName Null = new MultiPartTableName(new string[] { null, null, null, null });
    }
}
