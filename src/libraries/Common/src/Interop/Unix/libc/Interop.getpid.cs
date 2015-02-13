// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

using pid_t = System.Int32;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc)]
        internal static extern pid_t getpid();
    }
}
