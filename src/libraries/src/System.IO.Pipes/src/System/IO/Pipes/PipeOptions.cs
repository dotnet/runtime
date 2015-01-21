// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.Pipes
{
    [Flags]
    public enum PipeOptions
    {
        None = 0x0,
        WriteThrough = unchecked((int)0x80000000),
        Asynchronous = unchecked((int)0x40000000),  // corresponds to FILE_FLAG_OVERLAPPED
    }
}
