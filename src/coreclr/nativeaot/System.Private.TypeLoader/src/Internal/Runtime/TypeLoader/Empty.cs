// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace System.Collections.Generic
{
    //
    // Helper class to store reusable empty arrays. We cannot use the public Array.GetEmpty<T>() in the type loader because of
    // recursive dictionary lookups.
    //
    [System.Runtime.CompilerServices.ForceDictionaryLookups]
    internal static class Empty<T>
    {
        //
        // Returns a reusable empty array.
        //
        public static readonly T[] Array = new T[0];
    }
}
