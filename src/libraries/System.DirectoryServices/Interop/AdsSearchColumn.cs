//------------------------------------------------------------------------------
// <copyright file="AdsSearchColumn.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.Interop {

    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AdsSearchColumn {        
        public IntPtr pszAttrName;
        public int/*AdsType*/ dwADsType;
        public AdsValue *pADsValues;
        public int dwNumValues;
        public IntPtr hReserved;
    }

}

