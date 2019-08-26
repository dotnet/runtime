// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json
{
    /// <summary>
    ///   Represents a JSON array.
    /// </summary>
    public sealed class JsonArray : JsonNode, IList<JsonNode>, IReadOnlyList<JsonNode>
    {
        internal readonly List<JsonNode> _list;
        internal int _version;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class representing the empty array.
        /// </summary>
        public JsonArray()
        {
            _list = new List<JsonNode>();
            _version = 0;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class representing the specified collection.
        /// </summary>
        /// <param name="values">Collection to represent.</param>
        public JsonArray(IEnumerable<JsonNode> values)
        {
            _list = new List<JsonNode>(values);
            _version = 0;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class representing the specified collection of <see cref="string"/>s.
        /// </summary>
        /// <param name="values">Collection to represent.</param>
        /// <exception cref="ArgumentNullException">
        ///   Some of provided values are null.
        /// </exception>
        public JsonArray(IEnumerable<string> values) : this()
        {
            foreach (string value in values)
            {
                if (value == null)
                {
                    _list.Add(null);
                }

                _list.Add(new JsonString(value));
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class representing the specified collection of <see cref="byte"/>s.
        /// </summary>
        /// <param name="values">Collection to represent.</param>
        public JsonArray(IEnumerable<bool> values) : this()
        {
            foreach (bool value in values)
            {
                _list.Add(new JsonBoolean(value));
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class representing the specified collection of <see cref="byte"/>s.
        /// </summary>
        /// <param name="values">Collection to represent.</param>
        public JsonArray(IEnumerable<byte> values) : this()
        {
            foreach (byte value in values)
            {
                _list.Add(new JsonNumber(value));
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class representing the specified collection of <see cref="short"/>s.
        /// </summary>
        /// <param name="values">Collection to represent.</param>
        public JsonArray(IEnumerable<short> values) : this()
        {
            foreach (short value in values)
            {
                _list.Add(new JsonNumber(value));
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class representing the specified collection of <see cref="int"/>s.
        /// </summary>
        /// <param name="values">Collection to represent.</param>
        public JsonArray(IEnumerable<int> values) : this()
        {
            foreach (int value in values)
            {
                _list.Add(new JsonNumber(value));
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class representing the specified collection of <see cref="long"/>s.
        /// </summary>
        /// <param name="values">Collection to represent.</param>
        public JsonArray(IEnumerable<long> values) : this()
        {
            foreach (long value in values)
            {
                _list.Add(new JsonNumber(value));
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class representing the specified collection of <see cref="float"/>s.
        /// </summary>
        /// <param name="values">Collection to represent.</param>
        /// <exception cref="ArgumentException">
        ///   Some of provided values are not in a legal JSON number format.
        /// </exception>
        public JsonArray(IEnumerable<float> values) : this()
        {
            foreach (float value in values)
            {
                _list.Add(new JsonNumber(value));
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class representing the specified collection of <see cref="double"/>s.
        /// </summary>
        /// <param name="values">Collection to represent.</param>
        /// <exception cref="ArgumentException">
        ///   Some of provided values are not in a legal JSON number format.
        /// </exception>
        public JsonArray(IEnumerable<double> values) : this()
        {
            foreach (double value in values)
            {
                _list.Add(new JsonNumber(value));
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class representing the specified collection of <see cref="sbyte"/>s.
        /// </summary>
        /// <param name="values">Collection to represent.</param>
        [CLSCompliant(false)]
        public JsonArray(IEnumerable<sbyte> values) : this()
        {
            foreach (sbyte value in values)
            {
                _list.Add(new JsonNumber(value));
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class representing the specified collection of <see cref="ushort"/>s.
        /// </summary>
        /// <param name="values">Collection to represent.</param>
        [CLSCompliant(false)]
        public JsonArray(IEnumerable<ushort> values) : this()
        {
            foreach (ushort value in values)
            {
                _list.Add(new JsonNumber(value));
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class representing the specified collection of <see cref="uint"/>s.
        /// </summary>
        /// <param name="values">Collection to represent.</param>
        [CLSCompliant(false)]
        public JsonArray(IEnumerable<uint> values) : this()
        {
            foreach (uint value in values)
            {
                _list.Add(new JsonNumber(value));
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class representing the specified collection of <see cref="ulong"/>s.
        /// </summary>
        /// <param name="values">Collection to represent.</param>
        [CLSCompliant(false)]
        public JsonArray(IEnumerable<ulong> values) : this()
        {
            foreach (ulong value in values)
            {
                _list.Add(new JsonNumber(value));
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class representing the specified collection of <see cref="decimal"/>s.
        /// </summary>
        /// <param name="values">Collection to represent.</param>
        public JsonArray(IEnumerable<decimal> values) : this()
        {
            foreach (decimal value in values)
            {
                _list.Add(new JsonNumber(value));
            }
        }

        /// <summary>
        ///   Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="idx">The zero-based index of the element to get or set.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   Index is less than 0.
        /// </exception>
        public JsonNode this[int idx]
        {
            get => _list[idx];
            set
            {
                _list[idx] = value;
                _version++;
            }
        }

        /// <summary>
        ///   Adds the specified <see cref="JsonNode"/> value as the last item in this collection.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <remarks>Null value is allowed and represents the JSON null value.</remarks>
        public void Add(JsonNode value)
        {
            _list.Add(value);
            _version++;
        }

        /// <summary>
        ///   Adds the specified value as a <see cref="JsonString"/> as the last item in this collection.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <remarks>Null value is allowed and represents the JSON null value.</remarks>
        public void Add(string value)
        {
            if (value == null)
            {
                Add((JsonNode)null);
            }
            else
            {
                Add(new JsonString(value));
            }
        }

        /// <summary>
        ///   Adds the specified value as a <see cref="JsonBoolean"/> as the last item in this collection.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void Add(bool value) => Add(new JsonBoolean(value));

        /// <summary>
        ///   Adds the specified value as a <see cref="JsonNumber"/> as the last item in this collection.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void Add(byte value) => Add(new JsonNumber(value));

        /// <summary>
        ///   Adds the specified value as a <see cref="JsonNumber"/> as the last item in this collection.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void Add(short value) => Add(new JsonNumber(value));

        /// <summary>
        ///   Adds the specified value as a <see cref="JsonNumber"/> as the last item in this collection.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void Add(int value) => Add(new JsonNumber(value));

        /// <summary>
        ///   Adds the specified value as a <see cref="JsonNumber"/> as the last item in this collection.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void Add(long value) => Add(new JsonNumber(value));

        /// <summary>
        ///   Adds the specified value as a <see cref="JsonNumber"/> as the last item in this collection.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <exception cref="ArgumentException">
        ///   Provided value is not in a legal JSON number format.
        /// </exception>
        public void Add(float value) => Add(new JsonNumber(value));

        /// <summary>
        ///   Adds the specified value as a <see cref="JsonNumber"/> as the last item in this collection.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <exception cref="ArgumentException">
        ///   Provided value is not in a legal JSON number format.
        /// </exception>
        public void Add(double value) => Add(new JsonNumber(value));

        /// <summary>
        ///   Adds the specified value as a <see cref="JsonNumber"/> as the last item in this collection.
        /// </summary>
        /// <param name="value">The value to add.</param>
        [CLSCompliant(false)]
        public void Add(sbyte value) => Add(new JsonNumber(value));

        /// <summary>
        ///   Adds the specified value as a <see cref="JsonNumber"/> as the last item in this collection.
        /// </summary>
        /// <param name="value">The value to add.</param>
        [CLSCompliant(false)]
        public void Add(ushort value) => Add(new JsonNumber(value));

        /// <summary>
        ///   Adds the specified value as a <see cref="JsonNumber"/> as the last item in this collection.
        /// </summary>
        /// <param name="value">The value to add.</param>
        [CLSCompliant(false)]
        public void Add(uint value) => Add(new JsonNumber(value));

        /// <summary>
        ///   Adds the specified value as a <see cref="JsonNumber"/> as the last item in this collection.
        /// </summary>
        /// <param name="value">The value to add.</param>
        [CLSCompliant(false)]
        public void Add(ulong value) => Add(new JsonNumber(value));

        /// <summary>
        ///   Adds the specified value as a <see cref="JsonNumber"/> as the last item in this collection.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void Add(decimal value) => Add(new JsonNumber(value));

        /// <summary>
        ///   Inserts the specified item at the specified index of the JSON array.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to add.</param>
        /// <remarks>The <paramref name="item"/> parameter may be <see langword="null" />, which represents the JSON null value.</remarks>
        public void Insert(int index, JsonNode item)
        {
            _list.Insert(index, item);
            _version++;
        }

        /// <summary>
        ///   Inserts the specified item as a <see cref="JsonString"/> at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to add.</param>
        public void Insert(int index, string item) => Insert(index, new JsonString(item));

        /// <summary>
        ///   Inserts the specified item as a <see cref="JsonBoolean"/> at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to add.</param>
        public void Insert(int index, bool item) => Insert(index, new JsonBoolean(item));

        /// <summary>
        ///   Inserts the specified item as a <see cref="JsonNumber"/> at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to add.</param>
        public void Insert(int index, byte item) => Insert(index, new JsonNumber(item));

        /// <summary>
        ///   Inserts the specified item as a <see cref="JsonNumber"/> at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to add.</param>
        public void Insert(int index, short item) => Insert(index, new JsonNumber(item));

        /// <summary>
        ///   Inserts the specified item as a <see cref="JsonNumber"/> at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to add.</param>
        public void Insert(int index, int item) => Insert(index, new JsonNumber(item));

        /// <summary>
        ///   Inserts the specified item as a <see cref="JsonNumber"/> at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to add.</param>
        public void Insert(int index, long item) => Insert(index, new JsonNumber(item));

        /// <summary>
        ///   Inserts the specified item as a <see cref="JsonNumber"/> at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to add.</param>
        /// <exception cref="ArgumentException">
        ///   Provided value is not in a legal JSON number format.
        /// </exception>
        public void Insert(int index, float item) => Insert(index, new JsonNumber(item));

        /// <summary>
        ///   Inserts the specified item as a <see cref="JsonNumber"/> at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to add.</param>
        /// <exception cref="ArgumentException">
        ///   Provided value is not in a legal JSON number format.
        /// </exception>
        public void Insert(int index, double item) => Insert(index, new JsonNumber(item));

        /// <summary>
        ///   Inserts the specified item as a <see cref="JsonNumber"/> at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to add.</param>
        [CLSCompliant(false)]
        public void Insert(int index, sbyte item) => Insert(index, new JsonNumber(item));

        /// <summary>
        ///   Inserts the specified item as a <see cref="JsonNumber"/> at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to add.</param>
        [CLSCompliant(false)]
        public void Insert(int index, ushort item) => Insert(index, new JsonNumber(item));

        /// <summary>
        ///   Inserts the specified item as a <see cref="JsonNumber"/> at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to add.</param>
        [CLSCompliant(false)]
        public void Insert(int index, uint item) => Insert(index, new JsonNumber(item));

        /// <summary>
        ///   Inserts the specified item as a <see cref="JsonNumber"/> at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to add.</param>
        [CLSCompliant(false)]
        public void Insert(int index, ulong item) => Insert(index, new JsonNumber(item));

        /// <summary>
        ///   Inserts the specified item as a <see cref="JsonNumber"/> at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to add.</param>
        public void Insert(int index, decimal item) => Insert(index, new JsonNumber(item));

        /// <summary>
        ///   Determines whether a specified <see cref="JsonNode"/> element is in a collection.
        /// </summary>
        /// <param name="value">Value to check.</param>
        /// <returns>
        ///   <see langword="true"/> if the value is successfully found in a collection,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public bool Contains(JsonNode value) => _list.Contains(value);

        /// <summary>
        ///   Gets the number of elements contained in the collection.
        /// </summary>
        public int Count => _list.Count;

        /// <summary>
        ///   Gets a value indicating whether the collection is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        ///   Returns the zero-based index of the first occurrence of a specified item in the collection.
        /// </summary>
        /// <param name="item">Item to find.</param>
        /// <returns>The zero-based starting index of the search. 0 (zero) is valid in an empty collection.</returns>
        public int IndexOf(JsonNode item) => _list.IndexOf(item);

        /// <summary>
        ///   Returns the zero-based index of the last occurrence of a specified item in the collection.
        /// </summary>
        /// <param name="item">Item to find.</param>
        /// <returns>The zero-based starting index of the search. 0 (zero) is valid in an empty collection.</returns>
        public int LastIndexOf(JsonNode item) => _list.LastIndexOf(item);

        /// <summary>
        ///   Removes all elements from the JSON array.
        /// </summary>
        public void Clear()
        {
            _list.Clear();
            _version++;
        }

        /// <summary>
        ///   Removes the first occurrence of a specific object from the collection.
        /// </summary>
        /// <param name="item">
        ///   The object to remove from the collection. The value can be null and it represents null collection.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the item is successfully found in a collection and removed,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public bool Remove(JsonNode item)
        {
            _version++;
            return _list.Remove(item);
        }

        /// <summary>
        ///   Removes all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">
        ///   Thepredicate delegate that defines the conditions of the elements to remove.
        /// </param>
        /// <returns>The number of elements removed from the collection.</returns>
        public int RemoveAll(Predicate<JsonNode> match)
        {
            _version++;
            return _list.RemoveAll(match);
        }
        /// <summary>
        ///   Removes the item at the specified index of the collection.
        /// </summary>
        /// <param name="index">
        ///   The zero-based index of the element to remove.
        /// </param>
        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
            _version++;
        }

        /// <summary>
        ///   Copies the collection or a portion of it to an array.
        /// </summary>
        /// <param name="array">
        ///   The one-dimensional array that is the destination of the elements copied from collection.
        ///   The array must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        void ICollection<JsonNode>.CopyTo(JsonNode[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        /// <summary>
        ///   Returns an enumerator that iterates through the collection values.
        /// </summary>
        /// <returns>An enumerator structure for the <see cref="JsonArray"/>.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        ///   Returns an enumerator that iterates through the collection values.
        /// </summary>
        /// <returns>An enumerator structure for the <see cref="JsonArray"/>.</returns>
        IEnumerator<JsonNode> IEnumerable<JsonNode>.GetEnumerator() => new JsonArrayEnumerator(this);

        /// <summary>
        ///   Returns an enumerator that iterates through the collection values.
        /// </summary>
        /// <returns>An enumerator structure for the <see cref="JsonArray"/>.</returns>
        public JsonArrayEnumerator GetEnumerator() => new JsonArrayEnumerator(this);

        /// <summary>
        ///   Creates a new collection that is a copy of the current instance.
        /// </summary>
        /// <returns>A new collection that is a copy of this instance.</returns>
        public override JsonNode Clone()
        {
            var jsonArray = new JsonArray();

            foreach (JsonNode jsonNode in _list)
            {
                jsonArray.Add(jsonNode.Clone());
            }

            return jsonArray;
        }

        /// <summary>
        ///   Returns <see cref="JsonValueKind.Array"/>
        /// </summary>
        public override JsonValueKind ValueKind { get => JsonValueKind.Array;}
    }
}
