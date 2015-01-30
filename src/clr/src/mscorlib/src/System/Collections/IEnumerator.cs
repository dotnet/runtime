// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Interface:  IEnumerator
** 
** 
**
**
** Purpose: Base interface for all enumerators.
**
** 
===========================================================*/
namespace System.Collections {    
    using System;
    using System.Runtime.InteropServices;

    // Base interface for all enumerators, providing a simple approach
    // to iterating over a collection.
    [Guid("496B0ABF-CDEE-11d3-88E8-00902754C43A")]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IEnumerator
    {
        // Interfaces are not serializable
        // Advances the enumerator to the next element of the enumeration and
        // returns a boolean indicating whether an element is available. Upon
        // creation, an enumerator is conceptually positioned before the first
        // element of the enumeration, and the first call to MoveNext 
        // brings the first element of the enumeration into view.
        // 
        bool MoveNext();
    
        // Returns the current element of the enumeration. The returned value is
        // undefined before the first call to MoveNext and following a
        // call to MoveNext that returned false. Multiple calls to
        // GetCurrent with no intervening calls to MoveNext 
        // will return the same object.
        // 
        Object Current {
            get; 
        }
    
        // Resets the enumerator to the beginning of the enumeration, starting over.
        // The preferred behavior for Reset is to return the exact same enumeration.
        // This means if you modify the underlying collection then call Reset, your
        // IEnumerator will be invalid, just as it would have been if you had called
        // MoveNext or Current.
        //
        void Reset();
    }
}
