// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Intrinsics.X86
{
    public abstract partial class X86Base
    {
        private static unsafe void __cpuidex(int* cpuInfo, int functionId, int subFunctionId) => RuntimeImports.RhCpuIdEx(cpuInfo, functionId, subFunctionId);
    }
}
