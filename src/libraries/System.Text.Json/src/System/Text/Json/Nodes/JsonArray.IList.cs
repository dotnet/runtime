﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Nodes
{
    public sealed partial class JsonArray : JsonNode, IList<JsonNode?>
    {
        /// <summary>
        ///   Gets the number of elements contained in the <see cref="JsonArray"/>.
        /// </summary>
        public int Count => List.Count;

        /// <summary>
        ///   Adds a <see cref="JsonNode"/> to the end of the <see cref="JsonArray"/>.
        /// </summary>
        /// <param name="item">
        ///   The <see cref="JsonNode"/> to be added to the end of the <see cref="JsonArray"/>.
        /// </param>
        public void Add(JsonNode? item)
        {
            item?.AssignParent(this);

            List.Add(item);
        }

        /// <summary>
        ///   Removes all elements from the <see cref="JsonArray"/>.
        /// </summary>
        public void Clear()
        {
            List<JsonNode?>? list = _list;

            if (list is null)
            {
                _jsonElement = null;
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    DetachParent(list[i]);
                }

                list.Clear();
            }
        }

        /// <summary>
        ///   Determines whether an element is in the <see cref="JsonArray"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="JsonArray"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="item"/> is found in the <see cref="JsonArray"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Contains(JsonNode? item) => List.Contains(item);

        /// <summary>
        ///   The object to locate in the <see cref="JsonArray"/>.
        /// </summary>
        /// <param name="item">The <see cref="JsonNode"/> to locate in the <see cref="JsonArray"/>.</param>
        /// <returns>
        ///  The index of item if found in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(JsonNode? item) => List.IndexOf(item);

        /// <summary>
        ///   Inserts an element into the <see cref="JsonArray"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="JsonNode"/> to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="index"/> is less than 0 or <paramref name="index"/> is greater than <see cref="Count"/>.
        /// </exception>
        public void Insert(int index, JsonNode? item)
        {
            item?.AssignParent(this);
            List.Insert(index, item);
        }

        /// <summary>
        ///   Removes the first occurrence of a specific <see cref="JsonNode"/> from the <see cref="JsonArray"/>.
        /// </summary>
        /// <param name="item">
        ///   The <see cref="JsonNode"/> to remove from the <see cref="JsonArray"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="item"/> is successfully removed; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Remove(JsonNode? item)
        {
            if (List.Remove(item))
            {
                DetachParent(item);
                return true;
            }

            return false;
        }

        /// <summary>
        ///   Removes the element at the specified index of the <see cref="JsonArray"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="index"/> is less than 0 or <paramref name="index"/> is greater than <see cref="Count"/>.
        /// </exception>
        public void RemoveAt(int index)
        {
            JsonNode? item = List[index];
            List.RemoveAt(index);
            DetachParent(item);
        }

        /// <summary>
        ///   Removes all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">The predicate that defines the conditions of the elements to remove.</param>
        /// <returns>The number of elements removed from the <see cref="JsonArray"/>.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="match"/> is <see langword="null"/>.
        /// </exception>
        public int RemoveAll(Func<JsonNode?, bool> match)
        {
            ArgumentNullException.ThrowIfNull(match);

            return List.RemoveAll(node =>
            {
                if (match(node))
                {
                    DetachParent(node);
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }

        /// <summary>
        ///   Removes a range of elements from the <see cref="JsonArray"/>.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of elements to remove.</param>
        /// <param name="count">The number of elements to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="index"/> or <paramref name="count"/> is less than 0.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in the <see cref="JsonArray"/>.
        /// </exception>
        public void RemoveRange(int index, int count)
        {
            if (index < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum(nameof(index));
            }

            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum(nameof(count));
            }

            List<JsonNode?> list = List;

            if (list.Count - index < count)
            {
                ThrowHelper.ThrowArgumentException_InvalidOffLen();
            }

            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    DetachParent(list[index + i]);
                    // There's no need to assign nulls because List<>.RemoveRange calls
                    // Array.Clear on the removed partition.
                }

                list.RemoveRange(index, count);
            }
        }

        #region Explicit interface implementation

        /// <summary>
        ///   Copies the entire <see cref="Array"/> to a compatible one-dimensional array,
        ///   starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">
        ///   The one-dimensional <see cref="Array"/> that is the destination of the elements copied
        ///   from <see cref="JsonArray"/>. The Array must have zero-based indexing.</param>
        /// <param name="index">
        ///   The zero-based index in <paramref name="array"/> at which copying begins.
        /// </param>
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
        void ICollection<JsonNode?>.CopyTo(JsonNode?[] array, int index) => List.CopyTo(array, index);

        /// <summary>
        ///   Returns an enumerator that iterates through the <see cref="JsonArray"/>.
        /// </summary>
        /// <returns>A <see cref="IEnumerator{JsonNode}"/> for the <see cref="JsonNode"/>.</returns>
        public IEnumerator<JsonNode?> GetEnumerator() => List.GetEnumerator();

        /// <summary>
        ///   Returns an enumerator that iterates through the <see cref="JsonArray"/>.
        /// </summary>
        /// <returns>
        ///   A <see cref="IEnumerator"/> for the <see cref="JsonArray"/>.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)List).GetEnumerator();

        /// <summary>
        ///   Returns <see langword="false"/>.
        /// </summary>
        bool ICollection<JsonNode?>.IsReadOnly => false;

        #endregion

        private static void DetachParent(JsonNode? item)
        {
            if (item != null)
            {
                item.Parent = null;
            }
        }
    }
}
