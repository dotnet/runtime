//------------------------------------------------------------------------------
// <copyright file="AdsSearchPreferenceInfo.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.Interop {
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct AdsSearchPreferenceInfo {
        public int /*AdsSearchPreferences*/ dwSearchPref;
        internal int pad;
        public AdsValue vValue;
        public int /*AdsStatus*/ dwStatus;
        internal int pad2;
    }

}

