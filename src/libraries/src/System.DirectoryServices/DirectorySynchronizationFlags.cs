//------------------------------------------------------------------------------
// <copyright file="DirectorySynchronizationFlags.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
 
namespace System.DirectoryServices {
    [Flags]
    public enum DirectorySynchronizationOptions :long{
        None = 0,
        ObjectSecurity = 0x1,
        ParentsFirst = 0x0800,
    	PublicDataOnly = 0x2000,
    	IncrementalValues = 0x80000000
    	
    }
}
