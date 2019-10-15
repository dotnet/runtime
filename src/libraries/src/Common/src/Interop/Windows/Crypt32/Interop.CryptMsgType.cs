// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum CryptMsgType : int
        {
            CMSG_DATA = 1,
            CMSG_SIGNED = 2,
            CMSG_ENVELOPED = 3,
            CMSG_SIGNED_AND_ENVELOPED = 4,
            CMSG_HASHED = 5,
            CMSG_ENCRYPTED = 6,
        }
    }
}
