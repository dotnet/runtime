//------------------------------------------------------------------------------
// <copyright file="AdsAuthentication.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.Interop {

    internal enum AdsAuthentication {
        ADS_SECURE_AUTHENTICATION  = 0x1,
        ADS_USE_ENCRYPTION         = 0x2,
        ADS_USE_SSL                = 0x2,
        ADS_READONLY_SERVER        = 0x4,
        ADS_PROMPT_CREDENTIALS     = 0x8,
        ADS_NO_AUTHENTICATION      = 0x10,
        ADS_FAST_BIND              = 0x20,
        ADS_USE_SIGNING            = 0x40,
        ADS_USE_SEALING            = 0x80
    }

}

