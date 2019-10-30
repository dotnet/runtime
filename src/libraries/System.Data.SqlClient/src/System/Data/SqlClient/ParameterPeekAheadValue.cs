// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.SqlServer.Server;

namespace System.Data.SqlClient
{
    // simple storage to contain objects that must be generated prior to sending data, but
    //  that we cannot re-generate at the time of sending the data.  The entire purpose is
    //  to avoid long, complicated parameter lists that take every possible set of values.
    //  Instead, a single peekahead object is passed in, encapsulating whatever sets are needed.
    //
    //  Example:
    //      When processing IEnumerable<SqlDataRecord>, we need to obtain the enumerator and
    //      the first record during metadata generation (metadata is stored in the first record),
    //      but to properly stream the value, we can't ask the IEnumerable for these objects again
    //      when it's time to send the actual values.

    internal class ParameterPeekAheadValue
    {
        // Peekahead for IEnumerable<SqlDataRecord>
        internal IEnumerator<SqlDataRecord> Enumerator;
        internal SqlDataRecord FirstRecord;
    }
}
