// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.Tests.Common
{
    public struct Variant
    {
#pragma warning disable 0649
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public IntPtr bstrVal;
        public IntPtr pRecInfo;
#pragma warning restore 0649
    }
}
