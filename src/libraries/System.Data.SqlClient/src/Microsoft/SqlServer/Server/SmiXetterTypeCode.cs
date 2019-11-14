// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System;

namespace Microsoft.SqlServer.Server
{
    // Types should match Getter/Setter names
    internal enum SmiXetterTypeCode
    {
        XetBoolean,
        XetByte,
        XetBytes,
        XetChars,
        XetString,
        XetInt16,
        XetInt32,
        XetInt64,
        XetSingle,
        XetDouble,
        XetSqlDecimal,
        XetDateTime,
        XetGuid,
        GetVariantMetaData,     // no set call, just get
        GetXet,
        XetTime,                // XetTime mistakenly named, does not match getter/setter method name
        XetTimeSpan = XetTime,  // prefer using XetTimeSpan instead of XetTime.  Both mean the same thing for now.
        XetDateTimeOffset,
    }
}
