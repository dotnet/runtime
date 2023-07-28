// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.DataFlow
{
	// A lattice over dictionaries where the stored values are also from a lattice.
	public readonly struct DictionaryLattice<TKey, TValue, TValueLattice> : ILattice<DefaultValueDictionary<TKey, TValue>>
		where TKey : IEquatable<TKey>
		where TValue : IEquatable<TValue>
		where TValueLattice : ILattice<TValue>
	{
		public readonly TValueLattice ValueLattice;

		public DefaultValueDictionary<TKey, TValue> Top { get; }

		public DictionaryLattice (TValueLattice valueLattice)
		{
			ValueLattice = valueLattice;
			Top = new DefaultValueDictionary<TKey, TValue> (valueLattice.Top);
		}

		public DefaultValueDictionary<TKey, TValue> Meet (DefaultValueDictionary<TKey, TValue> left, DefaultValueDictionary<TKey, TValue> right)
		{
			var met = new DefaultValueDictionary<TKey, TValue> (left);
			foreach (var kvp in right) {
				TKey key = kvp.Key;
				TValue rightValue = kvp.Value;
				met.Set (key, ValueLattice.Meet (left.Get (key), rightValue));
			}
			return met;
		}
	}
}
