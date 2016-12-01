// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Buffers
{
    internal sealed partial class TlsOverPerCoreLockedStacksArrayPool<T> : ArrayPool<T>
    {
        /// <summary>Get an identifier for the current thread to use to index into the stacks.</summary>
        private static int ExecutionId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return CurrentProcessorNumber; }
        }
    }
}
