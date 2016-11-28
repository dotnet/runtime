// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.Login7
{
    /// <summary>
    /// Flags that indicate fSQLType bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7TypeFlagsSQL : byte
    {
        Default = 0,
        SQL = 1
    }

    /// <summary>
    /// Flags that indicate fOLEDB bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7TypeFlagsOleDb : byte
    {
        Off = 0,
        On = 1
    }

    /// <summary>
    /// Flags that indicate fReadOnlyIntent bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7TypeFlagsReadOnlyIntent : byte
    {
        ReadWrite = 0,
        ReadOnly = 1
    }


    /// <summary>
    /// Structure of the flags 3
    /// </summary>
    public class TDSLogin7TokenTypeFlags
    {
        /// <summary>
        /// The type of SQL the client sends to the server
        /// </summary>
        public TDSLogin7TypeFlagsSQL SQL { get; set; }

        /// <summary>
        /// Set if the client is the OLEDB driver
        /// </summary>
        public TDSLogin7TypeFlagsOleDb OleDb { get; set; }

        /// <summary>
        /// Application intent
        /// </summary>
        public TDSLogin7TypeFlagsReadOnlyIntent ReadOnlyIntent { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSLogin7TokenTypeFlags()
        {
        }

        /// <summary>
        /// Initialization construcgtor
        /// </summary>
        public TDSLogin7TokenTypeFlags(byte flags)
        {
            // Parse bytes as per TDS specification, section 2.2.6.3 LOGIN 7
            SQL = (TDSLogin7TypeFlagsSQL)(flags & 0xF);
            OleDb = (TDSLogin7TypeFlagsOleDb)((flags >> 4) & 0x1);
            ReadOnlyIntent = (TDSLogin7TypeFlagsReadOnlyIntent)((flags >> 5) & 0x1);
        }

        /// <summary>
        /// Assemble bits into a byte
        /// </summary>
        public byte ToByte()
        {
            return (byte)((((byte)SQL) & 0xF)
                | (((byte)OleDb) & 0x1) << 4
                | (((byte)ReadOnlyIntent) & 0x1) << 5);
        }
    }
}
