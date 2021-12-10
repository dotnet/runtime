// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace System.Collections.Concurrent
{
    //
    // Objects that want to be used as values in a keyed ConcurrentUnifier need to implement this interface.
    // Keyed items are values that contain their own keys and can produce them on demand.
    //
    internal interface IKeyedItem<K> where K : IEquatable<K>
    {
        //
        // This method is the keyed item's chance to do any lazy evaluation needed to produce the key quickly.
        // Concurrent unifiers are guaranteed to invoke this method at least once and wait for it
        // to complete before invoking the Key property. The unifier lock is NOT held across the call.
        //
        // PrepareKey() must be idempodent and thread-safe. It may be invoked multiple times and concurrently.
        //
        void PrepareKey();

        //
        // Produce the key. This is a high-traffic property and is called while the hash table's lock is held. Thus, it should
        // return a precomputed stored value and refrain from invoking other methods. If the keyed item wishes to
        // do lazy evaluation of the key, it should do so in the PrepareKey() method.
        //
        K Key { get; }
    }
}
