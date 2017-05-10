// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
