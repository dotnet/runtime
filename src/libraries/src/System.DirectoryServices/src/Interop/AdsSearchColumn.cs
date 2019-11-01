// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.DirectoryServices.Interop
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
