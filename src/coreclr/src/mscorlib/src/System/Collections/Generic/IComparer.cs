// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Interface:  IComparer
** 
** 
**
**
** Purpose: Interface for comparing two generic Objects.
**
** 
===========================================================*/
namespace System.Collections.Generic {
    
    using System;
    // The generic IComparer interface implements a method that compares 
    // two objects. It is used in conjunction with the Sort and 
    // BinarySearch methods on the Array, List, and SortedList classes.
    public interface IComparer<in T>
    {
        // Compares two objects. An implementation of this method must return a
        // value less than zero if x is less than y, zero if x is equal to y, or a
        // value greater than zero if x is greater than y.
        // 
        int Compare(T x, T y);
    }
}
