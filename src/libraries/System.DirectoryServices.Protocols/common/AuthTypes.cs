//------------------------------------------------------------------------------
// <copyright file="AuthTypes.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

namespace System.DirectoryServices.Protocols {
    using System;           
    
    public enum AuthType {
        Anonymous = 0,
        Basic = 1,
        Negotiate = 2,
        Ntlm = 3,
        Digest = 4,
        Sicily = 5,
        Dpa = 6,
        Msn = 7,
        External = 8,
        Kerberos = 9
        
    }

    public enum PartialResultProcessing
    {
        NoPartialResultSupport,
        ReturnPartialResults,
        ReturnPartialResultsAndNotifyCallback
    }

}
