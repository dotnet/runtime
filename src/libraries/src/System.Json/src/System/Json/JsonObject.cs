// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using JsonPair = System.Collections.Generic.KeyValuePair<string, System.Json.JsonValue>;
using JsonPairEnumerable = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Json.JsonValue>>;

namespace System.Json
{
    public class JsonObject : JsonValue, IDictionary<string, JsonValue>, ICollection<JsonPair>
	{
		// Use SortedDictionary to make result of ToString() deterministic
		SortedDictionary<string, JsonValue> map;

		public JsonObject (params JsonPair [] items)
		{
			map = new SortedDictionary<string, JsonValue> (StringComparer.Ordinal);

			if (items != null)
				AddRange (items);
		}

		public JsonObject (JsonPairEnumerable items)
		{
            if (items == null)
                throw new ArgumentNullException(nameof(items));

			map = new SortedDictionary<string, JsonValue> (StringComparer.Ordinal);
			AddRange (items);
		}

		public override int Count {
			get { return map.Count; }
		}

		public IEnumerator<JsonPair> GetEnumerator ()
		{
			return map.GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return map.GetEnumerator ();
		}

		public override sealed JsonValue this [string key] {
			get { return map [key]; }
			set { map [key] = value; }
		}

		public override JsonType JsonType {
			get { return JsonType.Object; }
		}

		public ICollection<string> Keys {
			get { return map.Keys; }
		}

		public ICollection<JsonValue> Values {
			get { return map.Values; }
		}

		public void Add (string key, JsonValue value)
		{
			if (key == null)
				throw new ArgumentNullException ("key");

			map.Add (key, value);
		}

		public void Add (JsonPair pair)
		{
			Add (pair.Key, pair.Value);
		}

		public void AddRange (JsonPairEnumerable items)
		{
			if (items == null)
				throw new ArgumentNullException ("items");

			foreach (var pair in items)
				map.Add (pair.Key, pair.Value);
		}

		public void AddRange (params JsonPair [] items)
		{
			AddRange ((JsonPairEnumerable) items);
		}

		public void Clear ()
		{
			map.Clear ();
		}

		bool ICollection<JsonPair>.Contains (JsonPair item)
		{
			return (map as ICollection<JsonPair>).Contains (item);
		}

		bool ICollection<JsonPair>.Remove (JsonPair item)
		{
			return (map as ICollection<JsonPair>).Remove (item);
		}

		public override bool ContainsKey (string key)
		{
			if (key == null)
				throw new ArgumentNullException ("key");

			return map.ContainsKey (key);
		}

		public void CopyTo (JsonPair [] array, int arrayIndex)
		{
			(map as ICollection<JsonPair>).CopyTo (array, arrayIndex);
		}

		public bool Remove (string key)
		{
			if (key == null)
				throw new ArgumentNullException ("key");

			return map.Remove (key);
		}

		bool ICollection<JsonPair>.IsReadOnly {
			get { return false; }
		}

		public override void Save (Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException ("stream");
			stream.WriteByte ((byte) '{');
			foreach (JsonPair pair in map) {
				stream.WriteByte ((byte) '"');
				byte [] bytes = Encoding.UTF8.GetBytes (EscapeString (pair.Key));
				stream.Write (bytes, 0, bytes.Length);
				stream.WriteByte ((byte) '"');
				stream.WriteByte ((byte) ',');
				stream.WriteByte ((byte) ' ');
				if (pair.Value == null) {
					stream.WriteByte ((byte) 'n');
					stream.WriteByte ((byte) 'u');
					stream.WriteByte ((byte) 'l');
					stream.WriteByte ((byte) 'l');
				} else
					pair.Value.Save (stream);
			}
			stream.WriteByte ((byte) '}');
		}

		public bool TryGetValue (string key, out JsonValue value)
		{
			return map.TryGetValue (key, out value);
		}
	}
}
