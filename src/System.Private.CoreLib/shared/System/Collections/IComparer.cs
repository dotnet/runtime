// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Collections
{
    // The IComparer interface implements a method that compares two objects. It is
    // used in conjunction with the Sort and BinarySearch methods on
    // the Array and List classes.
    // 
    // Interfaces are not serializable
    public interface IComparer
    {
        // Compares two objects. An implementation of this method must return a
        // value less than zero if x is less than y, zero if x is equal to y, or a
        // value greater than zero if x is greater than y.
        // 
        int Compare(object? x, object? y);
    }
}
