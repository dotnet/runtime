// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// Types of the tokens in data buffer of the packet
    /// </summary>
    public enum TDSTokenType
    {
        AlternativeMetadata = 088,
        AlternativeRow = 0xD3,
        ColumnMetadata = 0x81,
        ColumnInfo = 0xA5,
        Done = 0xFD,
        DoneProcedure = 0xFE,
        DoneInProc = 0xFF,
        EnvironmentChange = 0xE3,
        Error = 0xAA,
        FeatureExtAck = 0xAE,
        FedAuthInfo = 0xEE,
        Info = 0xAB,
        LoginAcknowledgement = 0xAD,
        NBCRow = 0xD2,
        Offset = 0x78,
        Order = 0xA9,
        ReturnStatus = 0x79,
        ReturnValue = 0xAC,
        Row = 0xD1,
        SSPI = 0xED,
        TableName = 0xA4,
        SessionState = 0xE4,
    }
}
