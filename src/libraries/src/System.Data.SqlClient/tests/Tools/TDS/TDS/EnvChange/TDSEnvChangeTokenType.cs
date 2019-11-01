// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.EnvChange
{
    /// <summary>
    /// Type of TDS EnvChagne token
    /// </summary>
    public enum TDSEnvChangeTokenType : byte
    {
        Database = 1,
        Language = 2,
        CharacterSet = 3,
        PacketSize = 4,
        UnicodeSort = 5,
        UnicodeFlags = 6,
        SQLCollation = 7,
        BeginTransaction = 8,
        CommitTransaction = 9,
        RollbackTransaction = 10,
        EnlistDTCTransaction = 11,
        DefectTransaction = 12,
        RealTimeLogShipping = 13,
        PromoteTransaction = 15,
        TransactionManagerAddress = 16,
        TransactionEnded = 17,
        ResetConnectionAcknowledgement = 18,
        UserInstance = 19,
        Routing = 20
    }
}
