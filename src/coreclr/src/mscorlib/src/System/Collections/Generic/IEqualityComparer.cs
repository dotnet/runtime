// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// 

namespace System.Collections.Generic {
    using System;

    // The generic IEqualityComparer interface implements methods to if check two objects are equal
    // and generate Hashcode for an object.
    // It is use in Dictionary class.  
    public interface IEqualityComparer<in T>
    {
        bool Equals(T x, T y);
        int GetHashCode(T obj);                
    }
}

