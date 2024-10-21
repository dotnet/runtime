// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Concurrent
{
    //
    // Objects that want to be used as values in a keyed ConcurrentUnifier need to implement this interface.
    // Keyed items are values that contain their own keys and can produce them on demand.
    //
    internal interface IKeyedItem<K> where K : IEquatable<K>
    {
        //
        // Produce the key. This is a high-traffic property and is called while the hash table's lock is held. Thus, it should
        // return a precomputed stored value and refrain from invoking other methods.
        //
        K Key { get; }
    }
}
