//------------------------------------------------------------------------------
// <copyright file="DirectoryOperation" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

namespace System.DirectoryServices.Protocols {
    using System;    
    using System.Xml;

    public abstract class DirectoryOperation {
        internal string directoryRequestID = null;        
        
        protected DirectoryOperation() {}        
    }
}
