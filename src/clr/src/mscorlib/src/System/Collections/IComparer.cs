// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Interface:  IComparer
** 
** 
**
**
** Purpose: Interface for comparing two Objects.
**
** 
===========================================================*/
namespace System.Collections {
    
    using System;
    // The IComparer interface implements a method that compares two objects. It is
    // used in conjunction with the Sort and BinarySearch methods on
    // the Array and List classes.
    // 
    // Interfaces are not serializable
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IComparer {
        // Compares two objects. An implementation of this method must return a
        // value less than zero if x is less than y, zero if x is equal to y, or a
        // value greater than zero if x is greater than y.
        // 
        int Compare(Object x, Object y);
    }
}
