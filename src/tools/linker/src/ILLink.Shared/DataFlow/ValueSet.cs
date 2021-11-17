using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ILLink.Shared.DataFlow
{
	public readonly struct ValueSet<TValue> : IEquatable<ValueSet<TValue>>, IEnumerable<TValue>
		where TValue : notnull
	{
		public readonly HashSet<TValue>? Values;

		public ValueSet (HashSet<TValue> values) => Values = values;

		public ValueSet (TValue value) => Values = new HashSet<TValue> () { value };

		public override bool Equals (object? obj) => obj is ValueSet<TValue> other && Equals (other);

		public bool Equals (ValueSet<TValue> other)
		{
			if (Values == null)
				return other.Values == null;
			if (other.Values == null)
				return false;

			return Values.SetEquals (other.Values);
		}

		public override int GetHashCode ()
		{
			if (Values == null)
				return typeof (ValueSet<TValue>).GetHashCode ();

			int hashCode = 0;
			foreach (var item in Values)
				hashCode = HashUtils.Combine (hashCode, item);
			return hashCode;
		}

		public IEnumerator<TValue> GetEnumerator ()
		{
			return Values?.GetEnumerator () ?? Enumerable.Empty<TValue> ().GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();

		public bool Contains (TValue value) => Values?.Contains (value) ?? false;

		public override string ToString ()
		{
			StringBuilder sb = new ();
			sb.Append ("{");
			sb.Append (string.Join (",", Values?.Select (v => v.ToString ()) ?? Enumerable.Empty<string> ()));
			sb.Append ("}");
			return sb.ToString ();
		}
	}
}