// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Node
{
    public partial class JsonObject : IDictionary<string, JsonNode?>
    {
        /// <summary>
        ///   Adds an element with the provided property name and value to the <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="propertyName">The property name of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="propertyName"/>is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   An element with the same property name already exists in the <see cref="JsonObject"/>.
        /// </exception>
        public void Add(string propertyName, JsonNode? value)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            value?.AssignParent(this);
            Dictionary.Add(propertyName, value);
            _lastKey = propertyName;
            _lastValue = value;
        }

        /// <summary>
        ///   Adds the specified property to the <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="property">
        ///   The KeyValuePair structure representing the property name and value to add to the <see cref="JsonObject"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   The property name of <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        public void Add(KeyValuePair<string, JsonNode?> property)
        {
            if (property.Key == null)
            {
                ThrowHelper.ThrowArgumentNullException_ValueCannotBeNull("propertyName");
            }

            JsonNode? value = property.Value;
            value?.AssignParent(this);
            Dictionary.Add(property);
            _lastKey = property.Key;
            _lastValue = value;
        }

        /// <summary>
        ///   Removes all elements from the <see cref="JsonObject"/>.
        /// </summary>
        public void Clear()
        {
            foreach (JsonNode? node in Dictionary.Values)
            {
                DetachParent(node);
            }

            Dictionary.Clear();
        }

        /// <summary>
        ///   Determines whether the <see cref="JsonObject"/> contains an element with the specified property name.
        /// </summary>
        /// <param name="propertyName">The property name to locate in the <see cref="JsonObject"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="JsonObject"/> contains an element with the specified property name; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        public bool ContainsKey(string propertyName)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            return Dictionary.ContainsKey(propertyName);
        }

        /// <summary>
        ///   Gets the number of elements contained in <see cref="JsonObject"/>.
        /// </summary>
        public int Count => Dictionary.Count;

        /// <summary>
        ///   Removes the element with the specified property name from the <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="propertyName">The property name of the element to remove.</param>
        /// <returns>
        ///   <see langword="true"/> if the element is successfully removed; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        public bool Remove(string propertyName)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (!Dictionary.TryGetValue(propertyName, out JsonNode? item))
            {
                return false;
            }

            bool success = Dictionary.Remove(propertyName);
            Debug.Assert(success);
            DetachParent(item);
            return true;
        }

        #region Explicit interface implementation
        /// <summary>
        ///   Determines whether the <see cref="JsonObject"/> contains a specific property name and value.
        /// </summary>
        /// <param name="item">The element to locate in the <see cref="JsonObject"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="JsonObject"/> contains an element with the property name; otherwise, <see langword="false"/>.
        /// </returns>
        bool ICollection<KeyValuePair<string, JsonNode?>>.Contains(KeyValuePair<string, JsonNode?> item) => Dictionary.Contains(item);

        /// <summary>
        ///   Copies the elements of the <see cref="JsonObject"/> to an array of type KeyValuePair starting at the specified array index.
        /// </summary>
        /// <param name="array">
        ///   The one-dimensional Array that is the destination of the elements copied from <see cref="JsonObject"/>.
        /// </param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="array"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="index"/> is less than 0.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The number of elements in the source ICollection is greater than the available space from <paramref name="index"/>
        ///   to the end of the destination <paramref name="array"/>.
        /// </exception>
        void ICollection<KeyValuePair<string, JsonNode?>>.CopyTo(KeyValuePair<string, JsonNode?>[] array, int index) =>
            Dictionary.CopyTo(array, index);

        /// <summary>
        ///   Returns an enumerator that iterates through the <see cref="JsonObject"/>.
        /// </summary>
        /// <returns>
        ///   An enumerator that iterates through the <see cref="JsonObject"/>.
        /// </returns>
        public IEnumerator<KeyValuePair<string, JsonNode?>> GetEnumerator() => Dictionary.GetEnumerator();

        /// <summary>
        ///   Removes a key and value from the <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="item">
        ///   The KeyValuePair structure representing the property name and value to remove from the <see cref="JsonObject"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the element is successfully removed; otherwise, <see langword="false"/>.
        /// </returns>
        bool ICollection<KeyValuePair<string, JsonNode?>>.Remove(KeyValuePair<string, JsonNode?> item)
        {
            if (Dictionary.Remove(item))
            {
                JsonNode? node = item.Value;
                DetachParent(node);
                return true;
            }

            return false;
        }

        /// <summary>
        ///   Gets a collection containing the property names in the <see cref="JsonObject"/>.
        /// </summary>
        ICollection<string> IDictionary<string, JsonNode?>.Keys => Dictionary.Keys;

        /// <summary>
        ///   Gets a collection containing the property values in the <see cref="JsonObject"/>.
        /// </summary>
        ICollection<JsonNode?> IDictionary<string, JsonNode?>.Values => Dictionary.Values;

        /// <summary>
        ///   Gets the value associated with the specified property name.
        /// </summary>
        /// <param name="propertyName">The property name of the value to get.</param>
        /// <param name="jsonNode">
        ///   When this method returns, contains the value associated with the specified property name, if the property name is found;
        ///   otherwise, <see langword="null"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="JsonObject"/> contains an element with the specified property name; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        bool IDictionary<string, JsonNode?>.TryGetValue(string propertyName, [NotNullWhen(true)] out JsonNode? jsonNode)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            return Dictionary.TryGetValue(propertyName, out jsonNode);
        }

        /// <summary>
        ///   Returns <see langword="false"/>.
        /// </summary>
        bool ICollection<KeyValuePair<string, JsonNode?>>.IsReadOnly => false;

        /// <summary>
        ///   Returns an enumerator that iterates through the <see cref="JsonObject"/>.
        /// </summary>
        /// <returns>
        ///   An enumerator that iterates through the <see cref="JsonObject"/>.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Dictionary).GetEnumerator();

        #endregion

        private void DetachParent(JsonNode? item)
        {
            if (item != null)
            {
                item.Parent = null;
            }

            // Prevent previous child from being returned from these cached variables.
            _lastKey = null;
            _lastValue = null;
        }
    }
}
