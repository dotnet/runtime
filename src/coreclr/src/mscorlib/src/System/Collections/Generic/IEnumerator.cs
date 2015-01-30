// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Interface:  IEnumerator
** 
** 
**
**
** Purpose: Base interface for all generic enumerators.
**
** 
===========================================================*/
namespace System.Collections.Generic {    
    using System;
    using System.Runtime.InteropServices;

    // Base interface for all generic enumerators, providing a simple approach
    // to iterating over a collection.
    public interface IEnumerator<out T> : IDisposable, IEnumerator
    {    
        // Returns the current element of the enumeration. The returned value is
        // undefined before the first call to MoveNext and following a
        // call to MoveNext that returned false. Multiple calls to
        // GetCurrent with no intervening calls to MoveNext 
        // will return the same object.
        // 
        /// <include file='doc\IEnumerator.uex' path='docs/doc[@for="IEnumerator.Current"]/*' />
        new T Current {
            get; 
        }
    }
}
