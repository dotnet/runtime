// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http.Headers
{
    [Flags]
    internal enum HttpHeaderType : byte
    {
        General = 0b1,
        Request = 0b10,
        Response = 0b100,
        Content = 0b1000,
        Custom = 0b10000,
        NonTrailing = 0b100000,

        All = 0b111111,
        None = 0
    }
}
