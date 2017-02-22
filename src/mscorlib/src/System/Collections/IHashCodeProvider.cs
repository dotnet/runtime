// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

using System;

namespace System.Collections
{
    // Provides a mechanism for a hash table user to override the default
    // GetHashCode() function on Objects, providing their own hash function.
    [Obsolete("Please use IEqualityComparer instead.")]
    internal interface IHashCodeProvider
    {
        // Interfaces are not serializable
        // Returns a hash code for the given object.  
        // 
        int GetHashCode(Object obj);
    }
}
