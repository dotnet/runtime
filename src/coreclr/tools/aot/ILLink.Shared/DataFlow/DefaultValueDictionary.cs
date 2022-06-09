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
	// This is a dictionary along with a default value, where every possible key either maps to
	// the default value, or another value. The default value is never explicitly stored in the dictionary,
	// and the empty dictionary (where all possible keys have the default value) is represented without
	// actually allocating a dictionary.
	public struct DefaultValueDictionary<TKey, TValue> : IEquatable<DefaultValueDictionary<TKey, TValue>>,
		IEnumerable<KeyValuePair<TKey, TValue>>
		where TKey : IEquatable<TKey>
		where TValue : IEquatable<TValue>
	{
		Dictionary<TKey, TValue>? Dictionary;

		readonly TValue DefaultValue;

		public DefaultValueDictionary (TValue defaultValue) => (Dictionary, DefaultValue) = (null, defaultValue);

		public DefaultValueDictionary (DefaultValueDictionary<TKey, TValue> other)
		{
			Dictionary = other.Dictionary == null ? null : new Dictionary<TKey, TValue> (other.Dictionary);
			DefaultValue = other.DefaultValue;
		}

		public TValue Get (TKey key) => Dictionary?.TryGetValue (key, out var value) == true ? value : DefaultValue;

		public void Set (TKey key, TValue value)
		{
			if (value.Equals (DefaultValue))
				Dictionary?.Remove (key);
			else
				(Dictionary ??= new Dictionary<TKey, TValue> ())[key] = value;
		}

		public bool Equals (DefaultValueDictionary<TKey, TValue> other)
		{
			if (!DefaultValue.Equals (other.DefaultValue))
				return false;

			if (Dictionary == null)
				return other.Dictionary == null;

			if (other.Dictionary == null)
				return false;

			if (Dictionary.Count != other.Dictionary.Count)
				return false;

			foreach (var kvp in other.Dictionary) {
				if (!Get (kvp.Key).Equals (kvp.Value))
					return false;
			}

			return true;
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator ()
		{
			return Dictionary?.GetEnumerator () ?? Enumerable.Empty<KeyValuePair<TKey, TValue>> ().GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();

		public override string ToString ()
		{
			StringBuilder sb = new ();
			sb.Append ("{");
			if (Dictionary != null) {
				foreach (var kvp in Dictionary)
					sb.Append (Environment.NewLine).Append ('\t').Append (kvp.Key.ToString ()).Append (" -> ").Append (kvp.Value.ToString ());
			}
			sb.Append (Environment.NewLine).Append ("\t_ -> ").Append (DefaultValue.ToString ());
			sb.Append (Environment.NewLine).Append ("}");
			return sb.ToString ();
		}
	}
}
