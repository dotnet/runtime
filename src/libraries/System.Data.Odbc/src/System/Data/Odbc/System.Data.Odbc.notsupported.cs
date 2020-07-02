// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.Odbc
{
    // We need this class in the implementation broswer assembly for building the system.data facade.
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public static class ODBC32
    {
        [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
        public enum RETCODE : int
        {
            SUCCESS = 0,
            SUCCESS_WITH_INFO = 1,
            ERROR = -1,
            INVALID_HANDLE = -2,
            NO_DATA = 100,
        }
    }

    // GenApi doesnot produce an in built alias for system.decimal.
    public sealed partial class OdbcDataReader : System.Data.Common.DbDataReader
    {
        public override decimal GetDecimal(int i) { throw new System.PlatformNotSupportedException(System.SR.Odbc_PlatformNotSupported); }
    }
}
