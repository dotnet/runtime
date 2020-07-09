// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data.Odbc
{
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
}
