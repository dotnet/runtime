// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Activeds
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct AdsSearchColumn
        {
            public IntPtr pszAttrName;
            public int/*AdsType*/ dwADsType;
            public AdsValue* pADsValues;
            public int dwNumValues;
            public IntPtr hReserved;
        }
    }
}
