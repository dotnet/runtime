// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [Flags]
        internal enum PipeFlags
        {
            O_CLOEXEC = 0x0010,
        }

        /// <summary>
        /// The index into the array filled by <see cref="pipe"/> which represents the read end of the pipe.
        /// </summary>
        internal const int ReadEndOfPipe = 0;

        /// <summary>
        /// The index into the array filled by <see cref="pipe"/> which represents the read end of the pipe.
        /// </summary>
        internal const int WriteEndOfPipe = 1;

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Pipe", SetLastError = true)]
        internal static extern unsafe int Pipe(int* pipefd, PipeFlags flags = 0); // pipefd is an array of two ints
    }
}
