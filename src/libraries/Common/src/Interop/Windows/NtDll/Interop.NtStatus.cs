// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal class StatusOptions
    {
        // Error codes from ntstatus.h
        internal const uint STATUS_SUCCESS = 0x00000000;
        internal const uint STATUS_SOME_NOT_MAPPED = 0x00000107;
        internal const uint STATUS_NO_MORE_FILES = 0x80000006;
        internal const uint STATUS_INVALID_PARAMETER = 0xC000000D;
        internal const uint STATUS_NO_MEMORY = 0xC0000017;
        internal const uint STATUS_OBJECT_NAME_NOT_FOUND = 0xC0000034;
        internal const uint STATUS_NONE_MAPPED = 0xC0000073;
        internal const uint STATUS_INSUFFICIENT_RESOURCES = 0xC000009A;
        internal const uint STATUS_ACCESS_DENIED = 0xC0000022;
        internal const uint STATUS_ACCOUNT_RESTRICTION = 0xc000006e;
    }
}
