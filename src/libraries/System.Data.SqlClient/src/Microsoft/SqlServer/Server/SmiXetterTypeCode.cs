// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



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
