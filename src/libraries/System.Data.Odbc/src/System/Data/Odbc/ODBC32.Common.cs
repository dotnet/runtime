// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data.Odbc
{
    // Class needs to be public to support serialization with type forwarding from Desktop to Core.
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public static partial class ODBC32
    {
        // from .\public\sdk\inc\sqlext.h: and .\public\sdk\inc\sql.h
        // must be public because it is serialized by OdbcException
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
