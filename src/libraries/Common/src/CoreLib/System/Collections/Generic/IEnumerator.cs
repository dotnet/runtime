// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace System.Collections.Generic
{
    // Base interface for all generic enumerators, providing a simple approach
    // to iterating over a collection.
    public interface IEnumerator<out T> : IDisposable, IEnumerator
    {
        // Returns the current element of the enumeration. The returned value is
        // undefined before the first call to MoveNext and following a
        // call to MoveNext that returned false. Multiple calls to
        // GetCurrent with no intervening calls to MoveNext 
        // will return the same object.
        new T Current
        {
            get;
        }
    }
}
