// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public static class MemoryMarshal
    {
        [Intrinsic]
        public static ref T GetArrayDataReference<T>(T[] array) =>
            ref GetArrayDataReference(array);
    }
}
