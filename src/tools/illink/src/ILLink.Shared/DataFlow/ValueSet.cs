// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.DataFlow
{
	public readonly struct ValueSet<TValue> : IEquatable<ValueSet<TValue>>, IEnumerable<TValue>, IDeepCopyValue<ValueSet<TValue>>
		where TValue : notnull
	{
		// Since we're going to do lot of type checks for this class a lot, it is much more efficient
		// if the class is sealed (as then the runtime can do a simple method table pointer comparison)
		private sealed class EnumerableValues : HashSet<TValue>
		{
			public EnumerableValues (IEnumerable<TValue> values) : base (values) { }

			public override int GetHashCode ()
			{
				int hashCode = 0;
				foreach (var item in this)
					hashCode = HashUtils.Combine (hashCode, item);
				return hashCode;
			}

			public bool Equals (EnumerableValues other)
			{
				// Unfortunately if some of the values are ArrayValues then they can mutate
				// after being added to the set, in which case their "hashing" is broken
				// The set will self-heal on every Meet since we recreate the HashSet
				// but equality is not guaranteed in the interim state.
				// So to workaround this for now, iterate over both sets and check
				// that the item can be found in the other set.
				foreach (TValue item in this)
					if (!other.Contains (item))
						return false;

				foreach (TValue item in other)
					if (!Contains (item))
						return false;

				return true;
			}

			public bool Equals (TValue other)
			{
				// As described above, it's possible to end up with a hashset which has multiple
				// values which are equal (due to mutability). So we can't rely on item count.
				bool found = false;
				foreach (TValue item in this) {
					if (!item.Equals (other))
						return false;
					found = true;
				}

				return found;
			}
		}

		public struct Enumerator : IEnumerator<TValue>, IDisposable, IEnumerator
		{
			private readonly object? _value;
			private int _state;  // 0 before beginning, 1 at item, 2 after end
			private readonly IEnumerator<TValue>? _enumerator;

			internal Enumerator (object? values)
			{
				_state = 0;
				if (values is EnumerableValues valuesSet) {
					_enumerator = valuesSet.GetEnumerator ();
					_value = null;
				} else {
					_enumerator = null;
					_value = values;
				}
			}

			public TValue Current => _enumerator is not null
				? _enumerator.Current
				: (_state == 1 ? (TValue) _value! : default!);

			object? IEnumerator.Current => Current;

			public void Dispose ()
			{
			}

			public bool MoveNext ()
			{
				if (_enumerator is not null)
					return _enumerator.MoveNext ();

				if (_value is null)
					return false;

				if (_state > 1)
					return false;

				_state++;
				return _state == 1;
			}

			public void Reset ()
			{
				if (_enumerator is not null)
					_enumerator.Reset ();
				else
					_state = 0;
			}
		}

		// This stores the values. By far the most common case will be either no values, or a single value.
		// Cases where there are multiple values stored are relatively very rare.
		//   null - no values (empty set)
		//   TValue - single value itself
		//   EnumerableValues typed object - multiple values, stored in the hashset
		private readonly object? _values;

		public ValueSet (TValue value) => _values = value;

		public ValueSet (IEnumerable<TValue> values) => _values = new EnumerableValues (values);

		private ValueSet (EnumerableValues values) => _values = values;

		public static implicit operator ValueSet<TValue> (TValue value) => new (value);

		public override bool Equals (object? obj) => obj is ValueSet<TValue> other && Equals (other);

		public bool Equals (ValueSet<TValue> other)
		{
			if (_values == null)
				return other._values == null;
			if (other._values == null)
				return false;

			if (_values is EnumerableValues enumerableValues) {
				if (other._values is EnumerableValues otherValuesSet) {
					return enumerableValues.Equals (otherValuesSet);
				} else
					return enumerableValues.Equals ((TValue) other._values);
			} else {
				if (other._values is EnumerableValues otherEnumerableValues) {
					return otherEnumerableValues.Equals ((TValue) _values);
				}

				return EqualityComparer<TValue>.Default.Equals ((TValue) _values, (TValue) other._values);
			}
		}

		public static bool operator == (ValueSet<TValue> left, ValueSet<TValue> right) => left.Equals (right);
		public static bool operator != (ValueSet<TValue> left, ValueSet<TValue> right) => !(left == right);

		public override int GetHashCode ()
		{
			if (_values == null)
				return typeof (ValueSet<TValue>).GetHashCode ();

			if (_values is EnumerableValues enumerableValues)
				return enumerableValues.GetHashCode ();

			return _values.GetHashCode ();
		}

		public Enumerator GetEnumerator () => new (_values);

		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator () => GetEnumerator ();

		IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();

		public bool Contains (TValue value) => _values is null
			? false
			: _values is EnumerableValues valuesSet
				? valuesSet.Contains (value)
				: EqualityComparer<TValue>.Default.Equals (value, (TValue) _values);

		internal static ValueSet<TValue> Meet (ValueSet<TValue> left, ValueSet<TValue> right)
		{
			if (left._values == null)
				return right.DeepCopy ();
			if (right._values == null)
				return left.DeepCopy ();

			if (left._values is not EnumerableValues && right.Contains ((TValue) left._values))
				return right.DeepCopy ();

			if (right._values is not EnumerableValues && left.Contains ((TValue) right._values))
				return left.DeepCopy ();

			var values = new EnumerableValues (left.DeepCopy ());
			values.UnionWith (right.DeepCopy ());
			return new ValueSet<TValue> (values);
		}

		public bool IsEmpty () => _values == null;

		public override string ToString ()
		{
			StringBuilder sb = new ();
			sb.Append ('{');
			sb.Append (string.Join (",", this.Select (v => v.ToString ())));
			sb.Append ('}');
			return sb.ToString ();
		}

		// Meet should copy the values, but most SingleValues are immutable.
		// Clone returns `this` if there are no mutable SingleValues (SingleValues that implement IDeepCopyValue), otherwise creates a new ValueSet with copies of the copiable Values
		public ValueSet<TValue> DeepCopy ()
		{
			if (_values is null)
				return this;

			// Optimize for the most common case with only a single value
			if (_values is not EnumerableValues) {
				if (_values is IDeepCopyValue<TValue> copyValue)
					return new ValueSet<TValue> (copyValue.DeepCopy ());
				else
					return this;
			}

			return new ValueSet<TValue> (this.Select (value => value is IDeepCopyValue<TValue> copyValue ? copyValue.DeepCopy () : value));
		}
	}
}
