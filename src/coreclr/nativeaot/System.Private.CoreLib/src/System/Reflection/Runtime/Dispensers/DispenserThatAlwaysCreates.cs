// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Reflection.Runtime.Dispensers
{
    //
    // This dispenser always creates things anew.
    //
    internal sealed class DispenserThatAlwaysCreates<K, V> : Dispenser<K, V>
        where K : IEquatable<K>
        where V : class
    {
        public DispenserThatAlwaysCreates(Func<K, V> factory)
        {
            _factory = factory;
        }

        public sealed override V GetOrAdd(K key)
        {
            return _factory(key);
        }

        private readonly Func<K, V> _factory;
    }
}
