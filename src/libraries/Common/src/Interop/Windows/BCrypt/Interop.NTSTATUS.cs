// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        internal enum NTSTATUS : uint
        {
            STATUS_SUCCESS = 0x0,
            STATUS_NOT_FOUND = 0xc0000225,
            STATUS_INVALID_PARAMETER = 0xc000000d,
            STATUS_NO_MEMORY = 0xc0000017,
            STATUS_AUTH_TAG_MISMATCH = 0xc000a002,
        }
    }
}
