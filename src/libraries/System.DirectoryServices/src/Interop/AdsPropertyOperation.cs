//------------------------------------------------------------------------------
// <copyright file="AdsPropertyOperation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.DirectoryServices.Interop {

    using System;
    using System.Runtime.InteropServices;
    using System.Diagnostics;

    internal enum AdsPropertyOperation {
        
        Clear = 1,

        Update = 2,

        Append = 3,

        Delete = 4

    }

}
