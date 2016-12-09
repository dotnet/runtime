//------------------------------------------------------------------------------
// <copyright file="DereferenceAlias.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

namespace System.DirectoryServices.Protocols {     
    
    public enum DereferenceAlias {
        Never = 0,
        InSearching = 1,
        FindingBaseObject = 2,
        Always = 3
    }    
    
}
