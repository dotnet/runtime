// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Interface: IHashCodeProvider
** 
** 
**
**
** Purpose: A bunch of strings.
**
** 
===========================================================*/
namespace System.Collections {
    
    using System;
    // Provides a mechanism for a hash table user to override the default
    // GetHashCode() function on Objects, providing their own hash function.
    [Obsolete("Please use IEqualityComparer instead.")]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IHashCodeProvider 
    {
        // Interfaces are not serializable
        // Returns a hash code for the given object.  
        // 
        int GetHashCode (Object obj);
    }
}
