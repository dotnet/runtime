//------------------------------------------------------------------------------
// <copyright file="AdsSortKey.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.Interop {

    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct AdsSortKey {
        public IntPtr pszAttrType;
        public IntPtr pszReserved;     
        public int fReverseOrder;
    }

}

