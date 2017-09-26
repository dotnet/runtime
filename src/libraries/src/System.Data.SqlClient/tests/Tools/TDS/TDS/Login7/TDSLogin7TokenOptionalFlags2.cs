// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.Login7
{
    /// <summary>
    /// Flags that indicate fLanguage bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7OptionalFlags2Language : byte
    {
        Warning = 0,
        Fatal = 1
    }

    /// <summary>
    /// Flags that indicate fODBC bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7OptionalFlags2Odbc : byte
    {
        Off = 0,
        On = 1
    }

    /// <summary>
    /// Flags that indicate fUserType bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7OptionalFlags2UserType : byte
    {
        Normal = 0,
        Server = 1,
        Remote = 2,
        Replication = 3
    }

    /// <summary>
    /// Flags that indicate fIntSecurity bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7OptionalFlags2IntSecurity : byte
    {
        Off = 0,
        On = 1
    }

    /// <summary>
    /// Structure of the optional flags 1
    /// </summary>
    public class TDSLogin7TokenOptionalFlags2
    {
        /// <summary>
        /// Set if the change to initial language must succeed if the connect is to succeed
        /// </summary>
        public TDSLogin7OptionalFlags2Language Language { get; set; }

        /// <summary>
        /// Set if the client is the ODBC driver.
        /// </summary>
        public TDSLogin7OptionalFlags2Odbc Odbc { get; set; }

        /// <summary>
        /// The type of user connecting to the server
        /// </summary>
        public TDSLogin7OptionalFlags2UserType UserType { get; set; }

        /// <summary>
        /// The type of security required by the client
        /// </summary>
        public TDSLogin7OptionalFlags2IntSecurity IntegratedSecurity { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSLogin7TokenOptionalFlags2()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSLogin7TokenOptionalFlags2(byte flags)
        {
            // Parse bytes as per TDS specification, section 2.2.6.3 LOGIN 7
            Language = (TDSLogin7OptionalFlags2Language)(flags & 0x1);
            Odbc = (TDSLogin7OptionalFlags2Odbc)((flags >> 1) & 0x1);
            // Skipping deprecated fTranBoundary and fCacheConnect
            UserType = (TDSLogin7OptionalFlags2UserType)((flags >> 4) & 0x7);
            IntegratedSecurity = (TDSLogin7OptionalFlags2IntSecurity)((flags >> 7) & 0x1);
        }

        /// <summary>
        /// Assemble bits into a byte
        /// </summary>
        public byte ToByte()
        {
            return (byte)((byte)Language
                | ((byte)Odbc) << 1
                | ((byte)UserType) << 4
                | ((byte)IntegratedSecurity) << 7);
        }
    }
}
