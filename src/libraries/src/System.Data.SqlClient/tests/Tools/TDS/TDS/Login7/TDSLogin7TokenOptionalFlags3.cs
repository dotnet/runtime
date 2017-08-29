// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.Login7
{
    /// <summary>
    /// Flags that indicate fChangePassword bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7OptionalFlags3ChangePassword : byte
    {
        No = 0,
        Yes = 1
    }

    /// <summary>
    /// Flags that indicate fUnknownCollationHandling bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7OptionalFlags3Collation : byte
    {
        Must = 0,
        May = 1
    }

    /// <summary>
    /// Structure of the optional flags 1
    /// </summary>
    public class TDSLogin7TokenOptionalFlags3
    {
        /// <summary>
        /// Password change request
        /// </summary>
        public TDSLogin7OptionalFlags3ChangePassword ChangePassword { get; set; }

        /// <summary>
        /// Client is requesting separate process to be spawned as user instance
        /// </summary>
        public bool IsUserInstance { get; set; }

        /// <summary>
        /// Send Yukon binary XML.
        /// </summary>
        public bool SendYukonBinaryXML { get; set; }

        /// <summary>
        /// Unknown collection handling
        /// </summary>
        public TDSLogin7OptionalFlags3Collation UnknownCollation { get; set; }

        /// <summary>
        /// Feature extension flag.
        /// </summary>
        public bool ExtensionFlag { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSLogin7TokenOptionalFlags3()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSLogin7TokenOptionalFlags3(byte flags)
        {
            // Parse bytes as per TDS specification, section 2.2.6.3 LOGIN 7
            ChangePassword = (TDSLogin7OptionalFlags3ChangePassword)(flags & 0x1);
            IsUserInstance = ((flags >> 1) & 0x1) != 0;
            SendYukonBinaryXML = ((flags >> 2) & 0x1) != 0;
            UnknownCollation = (TDSLogin7OptionalFlags3Collation)((flags >> 3) & 0x1);
            ExtensionFlag = ((flags >> 4) & 0x1) != 0;
        }

        /// <summary>
        /// Assemble bits into a byte
        /// </summary>
        public byte ToByte()
        {
            return (byte)((byte)ChangePassword
                | ((byte)(IsUserInstance ? 1 : 0)) << 1
                | ((byte)(SendYukonBinaryXML ? 1 : 0)) << 2
                | ((byte)UnknownCollation) << 3
                | ((byte)(ExtensionFlag ? 1 : 0)) << 4);
        }
    }
}
