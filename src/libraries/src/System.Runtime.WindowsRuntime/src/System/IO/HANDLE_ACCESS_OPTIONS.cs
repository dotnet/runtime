// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    [Flags]
    internal enum HANDLE_ACCESS_OPTIONS : uint
    {
        HAO_NONE = 0,
        HAO_READ_ATTRIBUTES = 0x80,
        // 0x120089
        HAO_READ = SYNCHRONIZE | READ_CONTROL | HAO_READ_ATTRIBUTES | FILE_READ_EA | FILE_READ_DATA,
        // 0x120116
        HAO_WRITE = SYNCHRONIZE | READ_CONTROL | FILE_WRITE_ATTRIBUTES | FILE_WRITE_EA | FILE_APPEND_DATA | FILE_WRITE_DATA,
        HAO_DELETE = 0x10000,

        // These are defined elsewhere, adding here for clarity
        // on what the HANDLE_ACCESS_OPTIONS represent.

        // DELETE               = 0x00010000,
        READ_CONTROL = 0x00020000,
        SYNCHRONIZE = 0x00100000,
        FILE_READ_DATA = 0x00000001,
        FILE_WRITE_DATA = 0x00000002,
        FILE_APPEND_DATA = 0x00000004,
        FILE_READ_EA = 0x00000008,
        FILE_WRITE_EA = 0x00000010,
        FILE_EXECUTE = 0x00000020,
        // FILE_READ_ATTRIBUTES = 0x00000080,
        FILE_WRITE_ATTRIBUTES = 0x00000100,
    }
}
