// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.ColMetadata
{
    /// <summary>
    /// Indicates type of updatability of the column
    /// </summary>
    public enum TDSColumnDataUpdatableFlag : byte
    {
        ReadOnly = 0,
        ReadWrite = 1,
        Unknown = 2
    }
}
