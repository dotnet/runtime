// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.Login7
{
    /// <summary>
    /// Flags that indicate fByteOrder bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7OptionalFlags1Order : byte
    {
        OrderX86 = 0,
        Order68000 = 1
    }

    /// <summary>
    /// Flags that indicate fChar bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7OptionalFlags1Char : byte
    {
        Ascii = 0,
        Ebddic = 1
    }

    /// <summary>
    /// Flags that indicate fFloat bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7OptionalFlags1Float : byte
    {
        IEEE754 = 0,
        VAX = 1,
        ND5000 = 2
    }

    /// <summary>
    /// Flags that indicate fDumpLoad bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7OptionalFlags1DumpLoad : byte
    {
        On = 0,
        Off = 1
    }

    /// <summary>
    /// Flags that indicate fUseDB bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7OptionalFlags1UseDB : byte
    {
        On = 0,
        Off = 1
    }

    /// <summary>
    /// Flags that indicate fDatabase bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7OptionalFlags1Database : byte
    {
        Warning = 0,
        Fatal = 1
    }

    /// <summary>
    /// Flags that indicate fSetLang bit of LOGIN 7 options
    /// </summary>
    public enum TDSLogin7OptionalFlags1Language : byte
    {
        Off = 0,
        On = 1
    }

    /// <summary>
    /// Structure of the optional flags 1
    /// </summary>
    public class TDSLogin7TokenOptionalFlags1
    {
        /// <summary>
        /// Order
        /// </summary>
        public TDSLogin7OptionalFlags1Order Order { get; set; }

        /// <summary>
        /// Character set
        /// </summary>
        public TDSLogin7OptionalFlags1Char CharacterSet { get; set; }

        /// <summary>
        /// Floating point standard
        /// </summary>
        public TDSLogin7OptionalFlags1Float FloatingPoint { get; set; }

        /// <summary>
        /// Dump load
        /// </summary>
        public TDSLogin7OptionalFlags1DumpLoad DumpLoad { get; set; }

        /// <summary>
        /// Use database notifications
        /// </summary>
        public TDSLogin7OptionalFlags1UseDB UseDB { get; set; }

        /// <summary>
        /// Database
        /// </summary>
        public TDSLogin7OptionalFlags1Database Database { get; set; }

        /// <summary>
        /// Language
        /// </summary>
        public TDSLogin7OptionalFlags1Language Language { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSLogin7TokenOptionalFlags1()
        {
        }

        /// <summary>
        /// Initialization construcgtor
        /// </summary>
        public TDSLogin7TokenOptionalFlags1(byte flags)
        {
            // Parse bytes as per TDS specification, section 2.2.6.3 LOGIN 7
            Order = (TDSLogin7OptionalFlags1Order)(flags & 0x1);
            CharacterSet = (TDSLogin7OptionalFlags1Char)((flags >> 1) & 0x1);
            FloatingPoint = (TDSLogin7OptionalFlags1Float)((flags >> 2) & 0x3);
            DumpLoad = (TDSLogin7OptionalFlags1DumpLoad)((flags >> 4) & 0x1);
            UseDB = (TDSLogin7OptionalFlags1UseDB)((flags >> 5) & 0x1);
            Database = (TDSLogin7OptionalFlags1Database)((flags >> 6) & 0x1);
            Language = (TDSLogin7OptionalFlags1Language)((flags >> 7) & 0x1);
        }

        /// <summary>
        /// Assemble bits into a byte
        /// </summary>
        public byte ToByte()
        {
            return (byte)((byte)Order
                | ((byte)CharacterSet) << 1
                | ((byte)FloatingPoint) << 2
                | ((byte)DumpLoad) << 4
                | ((byte)UseDB) << 5
                | ((byte)Database) << 6
                | ((byte)Language) << 7);
        }
    }
}
