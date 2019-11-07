// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.SqlServer.TDS.ColInfo
{
    /// <summary>
    /// Status of the column
    /// </summary>
    [Flags]
    public enum TDSColumnStatus : byte
    {
        /// <summary>
        /// The column was the result of an expression
        /// </summary>
        Expression = 0x04,

        /// <summary>
        /// The column is part of a key for the associated table
        /// </summary>
        Key = 0x08,

        /// <summary>
        /// The column was not requested, but was added because it was part of a key for the associated table
        /// </summary>
        Hidden = 0x10,

        /// <summary>
        /// the column name is different than the requested column name in the case of a column alias
        /// </summary>
        DifferentName = 0x20
    }
}
