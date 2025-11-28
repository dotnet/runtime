// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        internal static class OSStatus
        {
            public const int NoErr = 0;
            public const int ReadErr = -19;
            public const int WritErr = -20;
            public const int EOFErr = -39;
            public const int SecUserCanceled = -128;
            public const int ErrSSLWouldBlock = -9803;
        }
    }
}
