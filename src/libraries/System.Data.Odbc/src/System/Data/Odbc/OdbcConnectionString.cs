// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Security;

namespace System.Data.Odbc
{
    internal sealed class OdbcConnectionString : DbConnectionOptions
    {
        // instances of this class are intended to be immutable, i.e readonly
        // used by pooling classes so it is much easier to verify correctness
        // when not worried about the class being modified during execution

        private readonly string? _expandedConnectionString;

        internal OdbcConnectionString(string connectionString, bool validate) : base(connectionString, null, true)
        {
            if (!validate)
            {
                string? filename = null;
                int position = 0;
                _expandedConnectionString = ExpandDataDirectories(ref filename, ref position);
            }
            if (validate || (null == _expandedConnectionString))
            {
                // do not check string length if it was expanded because the final result may be shorter than the original
                if ((null != connectionString) && (ODBC32.MAX_CONNECTION_STRING_LENGTH < connectionString.Length))
                { // MDAC 83536
                    throw ODBC.ConnectionStringTooLong();
                }
            }
        }
    }
}
