// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface:  IEqualityComparer
**
** 
** 
**
** Purpose: A mechanism to expose a simplified infrastructure for 
**          Comparing objects in collections.
**
** 
===========================================================*/
namespace System.Collections {
    
    using System;
    // An IEqualityComparer is a mechanism to consume custom performant comparison infrastructure
    // that can be consumed by some of the common collections.
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IEqualityComparer {
        bool Equals(Object x, Object y);
        int GetHashCode(Object obj);        
    }
}
