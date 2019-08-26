// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json
{
    /// <summary>
    ///  Represents a mutable JSON object.
    /// </summary>
    public sealed class JsonObject : JsonNode, IEnumerable<KeyValuePair<string, JsonNode>>
    {
        internal readonly Dictionary<string, JsonNode> _dictionary;
        private readonly DuplicatePropertyNameHandling _duplicatePropertyNameHandling;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonObject"/> class representing the empty object.
        /// </summary>
        /// <param name="duplicatePropertyNameHandling">Specifies the way of handling duplicate property names.</param>
        /// <exception cref="ArgumentException">
        ///   Provided manner of handling duplicates does not exist.
        /// </exception>
        public JsonObject(DuplicatePropertyNameHandling duplicatePropertyNameHandling = DuplicatePropertyNameHandling.Replace)
        {
            if ((uint)duplicatePropertyNameHandling > (uint)DuplicatePropertyNameHandling.Error)
            {
                throw new ArgumentOutOfRangeException(SR.InvalidDuplicatePropertyNameHandling);
            }

            _dictionary = new Dictionary<string, JsonNode>();
            _duplicatePropertyNameHandling = duplicatePropertyNameHandling;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonObject"/> class representing provided set of JSON properties.
        /// </summary>
        /// <param name="jsonProperties">>Properties to represent as a JSON object.</param>
        /// <param name="duplicatePropertyNameHandling">Specifies the way of handling duplicate property names.</param>
        /// <exception cref="ArgumentException">
        ///   Provided collection contains duplicates if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        public JsonObject(
            IEnumerable<KeyValuePair<string, JsonNode>> jsonProperties,
            DuplicatePropertyNameHandling duplicatePropertyNameHandling = DuplicatePropertyNameHandling.Replace)
            : this(duplicatePropertyNameHandling)
            => AddRange(jsonProperties);

        /// <summary>
        ///   Gets or sets the value of the specified property.
        /// </summary>
        /// <param name="propertyName">The property name of the value to get or set.</param>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        public JsonNode this[string propertyName]
        {
            get => propertyName != null ? GetPropertyValue(propertyName) : throw new ArgumentNullException(nameof(propertyName));
            set
            {
                if (propertyName == null)
                    throw new ArgumentNullException(nameof(propertyName));

                _dictionary[propertyName] = value;
            }
        }

        /// <summary>
        ///   Adds the specified property to the JSON object.
        /// </summary>
        /// <param name="jsonProperty">The property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        public void Add(KeyValuePair<string, JsonNode> jsonProperty) => Add(jsonProperty.Key, jsonProperty.Value);

        /// <summary>
        ///   Adds the specified <see cref="JsonNode"/> property to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue">Value of the property to add.</param>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <remarks>
        ///   Null property value is allowed and represents a null JSON node.
        /// </remarks>
        public void Add(string propertyName, JsonNode propertyValue)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (_dictionary.ContainsKey(propertyName))
            {
                switch (_duplicatePropertyNameHandling)
                {
                    case DuplicatePropertyNameHandling.Ignore:
                        return;
                    case DuplicatePropertyNameHandling.Error:
                        throw new ArgumentException(SR.Format(SR.JsonObjectDuplicateKey, propertyName));
                }

                Debug.Assert(_duplicatePropertyNameHandling == DuplicatePropertyNameHandling.Replace);
            }

            _dictionary[propertyName] = propertyValue;
        }

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonString"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="string"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided value or property name is null.
        /// </exception>
        public void Add(string propertyName, string propertyValue) => Add(propertyName, new JsonString(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonString"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="ReadOnlySpan{Char}"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        public void Add(string propertyName, ReadOnlySpan<char> propertyValue) => Add(propertyName, new JsonString(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonString"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="Guid"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        public void Add(string propertyName, Guid propertyValue) => Add(propertyName, new JsonString(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonString"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="DateTime"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        public void Add(string propertyName, DateTime propertyValue) => Add(propertyName, new JsonString(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonString"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="DateTimeOffset"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        public void Add(string propertyName, DateTimeOffset propertyValue) => Add(propertyName, new JsonString(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonBoolean"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="string"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        public void Add(string propertyName, bool propertyValue) => Add(propertyName, new JsonBoolean(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonNumber"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="byte"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        public void Add(string propertyName, byte propertyValue) => Add(propertyName, new JsonNumber(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonNumber"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="short"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        public void Add(string propertyName, short propertyValue) => Add(propertyName, new JsonNumber(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonNumber"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="int"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        public void Add(string propertyName, int propertyValue) => Add(propertyName, new JsonNumber(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonNumber"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="long"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        public void Add(string propertyName, long propertyValue) => Add(propertyName, new JsonNumber(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonNumber"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="float"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   Provided value is not in a legal JSON number format.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        public void Add(string propertyName, float propertyValue) => Add(propertyName, new JsonNumber(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonNumber"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="double"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   Provided value is not in a legal JSON number format.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        public void Add(string propertyName, double propertyValue) => Add(propertyName, new JsonNumber(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonNumber"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="sbyte"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        [CLSCompliant(false)]
        public void Add(string propertyName, sbyte propertyValue) => Add(propertyName, new JsonNumber(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonNumber"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="ushort"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        [CLSCompliant(false)]
        public void Add(string propertyName, ushort propertyValue) => Add(propertyName, new JsonNumber(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonNumber"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="uint"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        [CLSCompliant(false)]
        public void Add(string propertyName, uint propertyValue) => Add(propertyName, new JsonNumber(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonNumber"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="ulong"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        [CLSCompliant(false)]
        public void Add(string propertyName, ulong propertyValue) => Add(propertyName, new JsonNumber(propertyValue));

        /// <summary>
        ///   Adds the specified property as a <see cref="JsonNumber"/> to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to add.</param>
        /// <param name="propertyValue"><see cref="decimal"/> value of the property to add.</param>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        public void Add(string propertyName, decimal propertyValue) => Add(propertyName, new JsonNumber(propertyValue));

        /// <summary>
        ///   Adds the properties from the specified collection to the JSON object.
        /// </summary>
        /// <param name="jsonProperties">Properties to add.</param>
        /// <exception cref="ArgumentException">
        ///   Provided collection contains duplicates if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Some of property names are null.
        /// </exception>
        public void AddRange(IEnumerable<KeyValuePair<string, JsonNode>> jsonProperties)
        {
            foreach (KeyValuePair<string, JsonNode> property in jsonProperties)
            {
                Add(property);
            }
        }

        /// <summary>
        ///   Adds the property values from the specified collection as a <see cref="JsonArray"/> property to the JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the <see cref="JsonArray"/> property to add.</param>
        /// <param name="propertyValues">Properties to add.</param>
        /// <exception cref="ArgumentException">
        ///   Provided collection contains duplicates if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Some of property names are null.
        /// </exception>
        public void Add(string propertyName, IEnumerable<JsonNode> propertyValues)
        {
            var jsonArray = new JsonArray();
            foreach (JsonNode value in propertyValues)
            {
                jsonArray.Add(value);
            }
            Add(propertyName, (JsonNode)jsonArray);
        }

        /// <summary>
        ///   Removes the property with the specified name.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns>
        ///   <see langword="true"/> if the property is successfully found in a JSON object and removed,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        public bool Remove(string propertyName) => propertyName != null ? _dictionary.Remove(propertyName) : throw new ArgumentNullException(nameof(propertyName));

        /// <summary>
        ///   Determines whether a property is in a JSON object.
        /// </summary>
        /// <param name="propertyName">Name of the property to check.</param>
        /// <returns>
        ///   <see langword="true"/> if the property is successfully found in a JSON object,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   Provided property name is null.
        /// </exception>
        public bool ContainsProperty(string propertyName) => propertyName != null ? _dictionary.ContainsKey(propertyName) : throw new ArgumentNullException(nameof(propertyName));

        /// <summary>
        ///   Returns the value of a property with the specified name.
        /// </summary>
        /// <param name="propertyName">Name of the property to return.</param>
        /// <returns>Value of the property with the specified name.</returns>
        /// <exception cref="KeyNotFoundException">
        ///   Property with specified name is not found in JSON object.
        /// </exception>
        public JsonNode GetPropertyValue(string propertyName)
        {
            if (!TryGetPropertyValue(propertyName, out JsonNode jsonNode))
            {
                throw new KeyNotFoundException(SR.Format(SR.PropertyNotFound, propertyName));
            }

            return jsonNode;
        }

        /// <summary>
        ///   Returns the value of a property with the specified name.
        /// </summary>
        /// <param name="propertyName">Name of the property to return.</param>
        /// <param name="jsonNode">Value of the property with specified name.</param>
        /// <returns>
        ///  <see langword="true"/> if property with specified name was found;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        /// <remarks>
        ///   When returns <see langword="false"/>, the value of <paramref name="jsonNode"/> is meaningless.
        ///   Null <paramref name="jsonNode"/> doesn't mean the property value was "null" unless <see langword="true"/> is returned.
        /// </remarks>
        public bool TryGetPropertyValue(string propertyName, out JsonNode jsonNode) => _dictionary.TryGetValue(propertyName, out jsonNode);

        /// <summary>
        ///   Returns the JSON object value of a property with the specified name.
        /// </summary>
        /// <param name="propertyName">Name of the property to return.</param>
        /// <returns>JSON objectvalue of a property with the specified name.</returns>
        /// <exception cref="KeyNotFoundException">
        ///   Property with specified name is not found in JSON object.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   Property with specified name is not a JSON object.
        /// </exception>
        public JsonObject GetJsonObjectPropertyValue(string propertyName)
        {
            if (GetPropertyValue(propertyName) is JsonObject jsonObject)
            {
                return jsonObject;
            }

            throw new ArgumentException(SR.Format(SR.PropertyTypeMismatch, propertyName));
        }

        /// <summary>
        ///   Returns the JSON object value of a property with the specified name.
        /// </summary>
        /// <param name="propertyName">Name of the property to return.</param>
        /// <param name="jsonObject">JSON object value of the property with specified name.</param>
        /// <returns>
        ///  <see langword="true"/> if JSON object property with specified name was found;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        public bool TryGetJsonObjectPropertyValue(string propertyName, out JsonObject jsonObject)
        {
            if (TryGetPropertyValue(propertyName, out JsonNode jsonNode))
            {
                jsonObject = jsonNode as JsonObject;
                return jsonObject != null;
            }

            jsonObject = null;
            return false;
        }

        /// <summary>
        ///   Returns the JSON array value of a property with the specified name.
        /// </summary>
        /// <param name="propertyName">Name of the property to return.</param>
        /// <returns>JSON objectvalue of a property with the specified name.</returns>
        /// <exception cref="KeyNotFoundException">
        ///   Property with specified name is not found in JSON array.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   Property with specified name is not a JSON array.
        /// </exception>
        public JsonArray GetJsonArrayPropertyValue(string propertyName)
        {
            if (GetPropertyValue(propertyName) is JsonArray jsonArray)
            {
                return jsonArray;
            }

            throw new ArgumentException(SR.Format(SR.PropertyTypeMismatch, propertyName));
        }

        /// <summary>
        ///   Returns the JSON array value of a property with the specified name.
        /// </summary>
        /// <param name="propertyName">Name of the property to return.</param>
        /// <param name="jsonArray">JSON array value of the property with specified name.</param>
        /// <returns>
        ///  <see langword="true"/> if JSON array property with specified name was found;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        public bool TryGetJsonArrayPropertyValue(string propertyName, out JsonArray jsonArray)
        {
            if (TryGetPropertyValue(propertyName, out JsonNode jsonNode))
            {
                jsonArray = jsonNode as JsonArray;
                return jsonArray != null;
            }

            jsonArray = null;
            return false;
        }

        /// <summary>
        ///   A collection containing the property names of JSON object.
        /// </summary>
        public ICollection<string> PropertyNames => _dictionary.Keys;

        /// <summary>
        ///  A collection containing the property values of JSON object.
        /// </summary>
        public ICollection<JsonNode> PropertyValues => _dictionary.Values;

        /// <summary>
        ///   Returns an enumerator that iterates through the JSON object properties.
        /// </summary>
        /// <returns>An enumerator structure for the <see cref="JsonObject"/>.</returns>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        ///   Returns an enumerator that iterates through the JSON object properties.
        /// </summary>
        /// <returns>An enumerator structure for the JSON object.</returns>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        IEnumerator<KeyValuePair<string, JsonNode>> IEnumerable<KeyValuePair<string, JsonNode>>.GetEnumerator() => new JsonObjectEnumerator(this);

        /// <summary>
        ///   Returns an enumerator that iterates through the JSON object properties.
        /// </summary>
        /// <returns>An enumerator structure for the JSON object.</returns>
        /// <exception cref="ArgumentException">
        ///   Property name to set already exists if handling duplicates is set to <see cref="DuplicatePropertyNameHandling.Error"/>.
        /// </exception>
        public JsonObjectEnumerator GetEnumerator() => new JsonObjectEnumerator(this);

        /// <summary>
        ///   Creates a new JSON object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new JSON object that is a copy of this instance.</returns>
        public override JsonNode Clone()
        {
            var jsonObject = new JsonObject(_duplicatePropertyNameHandling);

            foreach (KeyValuePair<string, JsonNode> property in _dictionary)
            {
                jsonObject.Add(property.Key, property.Value.Clone());
            }

            return jsonObject;
        }

        /// <summary>
        ///   Returns <see cref="JsonValueKind.Object"/>
        /// </summary>
        public override JsonValueKind ValueKind { get => JsonValueKind.Object; }
    }
}
