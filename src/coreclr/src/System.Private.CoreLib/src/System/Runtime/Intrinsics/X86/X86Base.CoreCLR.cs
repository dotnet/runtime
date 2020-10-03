// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Intrinsics.X86
{
    public abstract partial class X86Base
    {
        [DllImport(RuntimeHelpers.QCall)]
        private static extern unsafe void __cpuidex(int* cpuInfo, int functionId, int subFunctionId);
    }
}
