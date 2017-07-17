// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.DirectoryServices.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct AdsSortKey
    {
        public IntPtr pszAttrType;
        public IntPtr pszReserved;
        public int fReverseOrder;
    }
}
