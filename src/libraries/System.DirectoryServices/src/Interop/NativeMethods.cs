//------------------------------------------------------------------------------
// <copyright file="NativeMethods.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.Interop {
    using System.Runtime.InteropServices;
    using System;
    using System.Security.Permissions;
    using System.Collections;
    using System.IO;
    using System.Text;

    [
    System.Runtime.InteropServices.ComVisible(false)   
    ]
    internal class NativeMethods {                                        
               
        public  enum AuthenticationModes {
            SecureAuthentication  = 0x1,
            UseEncryption         = 0x2,
            UseSSL                = 0x2,
            ReadonlyServer        = 0x4,
            // PromptCredentials     = 0x8,   // Deprecated by ADSI
            NoAuthentication      = 0x10,
            FastBind              = 0x20,
            UseSigning            = 0x40,
            UseSealing            = 0x80,
            UseDelegation = 0x100,
            UseServerBinding = 0x200    
        }                                                         
    }
}

