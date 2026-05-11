// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;

namespace VarArgsPInvokeLib
{
    public static class VarArgsWrapper
    {
        [DllImport("VarargsNative", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TestVarArgs(StringBuilder builder, nint bufferSize, string formatString, __arglist);
    }
}
