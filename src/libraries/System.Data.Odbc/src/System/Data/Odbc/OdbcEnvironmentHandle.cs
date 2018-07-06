// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Runtime.Versioning;

namespace System.Data.Odbc
{
    internal sealed class OdbcEnvironmentHandle : OdbcHandle
    {
        internal OdbcEnvironmentHandle() : base(ODBC32.SQL_HANDLE.ENV, null)
        {
            ODBC32.RetCode retcode;

            //Set the expected driver manager version
            //
            retcode = Interop.Odbc.SQLSetEnvAttr(
                this,
                ODBC32.SQL_ATTR.ODBC_VERSION,
                ODBC32.SQL_OV_ODBC3,
                ODBC32.SQL_IS.INTEGER);
            // ignore retcode

            //Turn on connection pooling
            //Note: the env handle controls pooling.  Only those connections created under that
            //handle are pooled.  So we have to keep it alive and not create a new environment
            //for   every connection.
            //
            retcode = Interop.Odbc.SQLSetEnvAttr(
                this,
                ODBC32.SQL_ATTR.CONNECTION_POOLING,
                ODBC32.SQL_CP_ONE_PER_HENV,
                ODBC32.SQL_IS.INTEGER);

            switch (retcode)
            {
                case ODBC32.RetCode.SUCCESS:
                case ODBC32.RetCode.SUCCESS_WITH_INFO:
                    break;
                default:
                    Dispose();
                    throw ODBC.CantEnableConnectionpooling(retcode);
            }
        }
    }
}

