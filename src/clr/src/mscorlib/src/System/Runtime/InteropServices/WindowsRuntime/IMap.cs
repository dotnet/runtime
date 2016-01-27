// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

// Windows.Foundation.Collections.IMap`2, IMapView`2, and IKeyValuePair`2 cannot be referenced from
// managed code because they're hidden by the metadata adapter. We redeclare the interfaces manually
// to be able to talk to native WinRT objects.
namespace System.Runtime.InteropServices.WindowsRuntime
{
    [ComImport]
    [Guid("3c2925fe-8519-45c1-aa79-197b6718c1c1")]
    [WindowsRuntimeImport]
    internal interface IMap<K, V> : IIterable<IKeyValuePair<K, V>>
    {
        [Pure]
        V Lookup(K key);
        [Pure]
        uint Size { get; }
        [Pure]
        bool HasKey(K key);
        [Pure]
        IReadOnlyDictionary<K, V> GetView();  // Really an IMapView<K, V>
        bool Insert(K key, V value);
        void Remove(K key);
        void Clear();
    }

    [ComImport]
    [Guid("e480ce40-a338-4ada-adcf-272272e48cb9")]
    [WindowsRuntimeImport]
    internal interface IMapView<K, V> : IIterable<IKeyValuePair<K, V>>
    {
        [Pure]
        V Lookup(K key);
        [Pure]
        uint Size { get; }
        [Pure]
        bool HasKey(K key);
        [Pure]
        void Split(out IMapView<K, V> first, out IMapView<K, V> second);
    }

    [ComImport]
    [Guid("02b51929-c1c4-4a7e-8940-0312b5c18500")]
    [WindowsRuntimeImport]
    internal interface IKeyValuePair<K, V>
    {
        [Pure]
        K Key { get; }
        [Pure]
        V Value { get; }
    }
}
