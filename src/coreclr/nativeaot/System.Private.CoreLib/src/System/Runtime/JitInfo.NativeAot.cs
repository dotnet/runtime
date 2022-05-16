// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime
{
    public static partial class JitInfo
    {
        public static long GetCompiledILBytes(bool currentThread = false) => 0;

        public static long GetCompiledMethodCount(bool currentThread = false) => 0;

        private static long GetCompilationTimeInTicks(bool currentThread = false) => 0;
    }
}
