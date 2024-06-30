// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Diagnostics
{
    /// <summary>
    /// Represents a list of tags that can be accessed by index. Provides methods to search, sort, and manipulate lists.
    /// </summary>
    /// <remarks>
    /// TagList can be used in the scenarios which need to optimize for memory allocations. TagList will avoid allocating any memory when using up to eight tags.
    /// Using more than eight tags will cause allocating memory to store the tags.
    /// Public static (Shared in Visual Basic) members of this type are thread safe. Any instance members are not guaranteed to be thread safe.
    /// </remarks>
    public struct TagList : IList<KeyValuePair<string, object?>>, IReadOnlyList<KeyValuePair<string, object?>>
    {
        private const int OverflowAdditionalCapacity = 8;

        // Up to eight tags are stored in an inline array. Once there are more items than will fit in the inline array,
        // an array is allocated to store all the items and the inline array is abandoned. Even if the size shrinks down
        // to below eight items, the array continues to be used.

        private InlineTags _tags;
        private KeyValuePair<string, object?>[]? _overflowTags;
        private int _tagsCount;

        /// <summary>
        /// Initializes a new instance of the TagList structure using the specified <paramref name="tagList" />.
        /// </summary>
        /// <param name="tagList">A span of tags to initialize the list with.</param>
        public TagList(params ReadOnlySpan<KeyValuePair<string, object?>> tagList) : this()
        {
            _tagsCount = tagList.Length;

            scoped Span<KeyValuePair<string, object?>> tags = _tagsCount <= InlineTags.Length ?
                _tags :
                _overflowTags = new KeyValuePair<string, object?>[_tagsCount + OverflowAdditionalCapacity];

            tagList.CopyTo(tags);
        }

        /// <summary>
        /// Gets the number of tags contained in the <see cref="T:System.Diagnostics.TagList" />.
        /// </summary>
        public readonly int Count => _tagsCount;

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Diagnostics.TagList" /> is read-only. This property will always return <see langword="false" />.
        /// </summary>
        public readonly bool IsReadOnly => false;

        /// <summary>
        /// Gets or sets the tags at the specified index.
        /// </summary>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> is not a valid index in the <see cref="T:System.Diagnostics.TagList" />.</exception>
        public KeyValuePair<string, object?> this[int index]
        {
            readonly get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_tagsCount, nameof(index));

                return _overflowTags is null ? _tags[index] : _overflowTags[index];
            }

            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_tagsCount, nameof(index));

                if (_overflowTags is null)
                {
                    _tags[index] = value;
                }
                else
                {
                    _overflowTags[index] = value;
                }
            }
        }

        /// <summary>
        /// Adds a tag with the provided <paramref name="key" /> and <paramref name="value" /> to the list.
        /// </summary>
        /// <param name="key">The tag key.</param>
        /// <param name="value">The tag value.</param>
        public void Add(string key, object? value) =>
            Add(new KeyValuePair<string, object?>(key, value));

        /// <summary>
        /// Adds a tag to the list.
        /// </summary>
        /// <param name="tag">Key and value pair of the tag to add to the list.</param>
        public void Add(KeyValuePair<string, object?> tag)
        {
            int count = _tagsCount;
            if (_overflowTags is null && (uint)count < InlineTags.Length)
            {
                _tags[count] = tag;
                _tagsCount++;
            }
            else
            {
                AddToOverflow(tag);
            }
        }

        /// <summary>
        /// Adds a tag to the overflow list. Slow path outlined from Add to maximize the chance for the fast path to be inlined.
        /// </summary>
        /// <param name="tag">Key and value pair of the tag to add to the list.</param>
        private void AddToOverflow(KeyValuePair<string, object?> tag)
        {
            Debug.Assert(_overflowTags is not null || _tagsCount == InlineTags.Length);

            if (_overflowTags is null)
            {
                _overflowTags = new KeyValuePair<string, object?>[InlineTags.Length + OverflowAdditionalCapacity];
                ((ReadOnlySpan<KeyValuePair<string, object?>>)_tags).CopyTo(_overflowTags);
            }
            else if (_tagsCount == _overflowTags.Length)
            {
                Array.Resize(ref _overflowTags, _tagsCount + OverflowAdditionalCapacity);
            }

            _overflowTags[_tagsCount] = tag;
            _tagsCount++;
        }

        /// <summary>
        /// Copies the contents of this  into a destination <paramref name="tags" /> span.
        /// Inserts an element into this <see cref="T:System.Diagnostics.TagList" /> at the specified index.
        /// </summary>
        /// <param name="tags">The destination <see cref="T:System.Span`1" /> object.</param>
        /// <exception cref="T:System.ArgumentException"> <paramref name="tags" /> The number of elements in the source <see cref="T:System.Diagnostics.TagList" /> is greater than the number of elements that the destination span.</exception>
        public readonly void CopyTo(Span<KeyValuePair<string, object?>> tags)
        {
            if (tags.Length < _tagsCount)
            {
                throw new ArgumentException(SR.Arg_BufferTooSmall);
            }

            Tags.CopyTo(tags);
        }

        /// <summary>
        /// Copies the entire <see cref="T:System.Diagnostics.TagList" /> to a compatible one-dimensional array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from <see cref="T:System.Diagnostics.TagList" />. The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException"> <paramref name="array" /> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"> <paramref name="arrayIndex " /> is less than 0 or greater that or equal the <paramref name="array" /> length.</exception>
        public readonly void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)arrayIndex, (uint)array.Length, nameof(arrayIndex));

            CopyTo(array.AsSpan(arrayIndex));
        }

        /// <summary>
        /// Inserts an element into the <see cref="T:System.Diagnostics.TagList" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The tag to insert.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"> <paramref name="index" /> index is less than 0 or <paramref name="index" /> is greater than <see cref="M:System.Diagnostics.TagList.Count" />.</exception>
        public void Insert(int index, KeyValuePair<string, object?> item)
        {
            if (index == _tagsCount)
            {
                Add(item);
                return;
            }

            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)_tagsCount, nameof(index));

            if (_tagsCount == InlineTags.Length && _overflowTags is null)
            {
                _overflowTags = new KeyValuePair<string, object?>[InlineTags.Length + OverflowAdditionalCapacity];
                ((ReadOnlySpan<KeyValuePair<string, object?>>)_tags).CopyTo(_overflowTags);
            }

            if (_overflowTags is not null)
            {
                if (_tagsCount == _overflowTags.Length)
                {
                    Array.Resize(ref _overflowTags, _tagsCount + OverflowAdditionalCapacity);
                }

                _overflowTags.AsSpan(index, _tagsCount - index).CopyTo(_overflowTags.AsSpan(index + 1));
                _overflowTags[index] = item;
            }
            else
            {
                Span<KeyValuePair<string, object?>> tags = _tags;
                tags.Slice(index, _tagsCount - index).CopyTo(tags.Slice(index + 1));
                tags[index] = item;
            }

            _tagsCount++;
        }

        /// <summary>
        /// Removes the element at the specified index of the <see cref="T:System.Diagnostics.TagList" />.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"> <paramref name="index" /> index is less than 0 or <paramref name="index" /> is greater than <see cref="M:System.Diagnostics.TagList.Count" />.</exception>
        public void RemoveAt(int index)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_tagsCount, nameof(index));

            Span<KeyValuePair<string, object?>> tags = _overflowTags is not null ? _overflowTags : _tags;
            tags.Slice(index + 1, _tagsCount - index - 1).CopyTo(tags.Slice(index));
            _tagsCount--;
        }

        /// <summary>
        /// Removes all elements from the <see cref="T:System.Diagnostics.TagList" />.
        /// </summary>
        public void Clear() =>
            _tagsCount = 0;

        /// <summary>
        /// Determines whether an tag is in the <see cref="T:System.Diagnostics.TagList" />.
        /// </summary>
        /// <param name="item">The tag to locate in the <see cref="T:System.Diagnostics.TagList" />.</param>
        /// <returns><see langword="true" /> if item is found in the <see cref="T:System.Diagnostics.TagList" />; otherwise, <see langword="false" />.</returns>
        public readonly bool Contains(KeyValuePair<string, object?> item) =>
            IndexOf(item) >= 0;

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Diagnostics.TagList" />.
        /// </summary>
        /// <param name="item">The tag to remove from the <see cref="T:System.Diagnostics.TagList" />.</param>
        /// <returns><see langword="true" /> if item is successfully removed; otherwise, <see langword="false" />. This method also returns <see langword="false" /> if item was not found in the <see cref="T:System.Diagnostics.TagList" />.</returns>
        public bool Remove(KeyValuePair<string, object?> item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="T:System.Diagnostics.TagList" />.
        /// </summary>
        /// <returns>Returns an enumerator that iterates through the <see cref="T:System.Diagnostics.TagList" />.</returns>
        public readonly IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => new Enumerator(in this);

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="T:System.Diagnostics.TagList" />.
        /// </summary>
        /// <returns>Returns an enumerator that iterates through the <see cref="T:System.Diagnostics.TagList" />.</returns>
        readonly IEnumerator IEnumerable.GetEnumerator() => new Enumerator(in this);

        /// <summary>
        /// Searches for the specified tag and returns the zero-based index of the first occurrence within the entire <see cref="T:System.Diagnostics.TagList" />.
        /// </summary>
        /// <param name="item">The tag to locate in the <see cref="T:System.Diagnostics.TagList" />.</param>
        public readonly int IndexOf(KeyValuePair<string, object?> item)
        {
            ReadOnlySpan<KeyValuePair<string, object?>> tags =
                _overflowTags is not null ? _overflowTags :
                _tags;

            tags = tags.Slice(0, _tagsCount);

            if (item.Value is not null)
            {
                for (int i = 0; i < tags.Length; i++)
                {
                    if (item.Key == tags[i].Key && item.Value.Equals(tags[i].Value))
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = 0; i < tags.Length; i++)
                {
                    if (item.Key == tags[i].Key && tags[i].Value is null)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        [UnscopedRef]
        internal readonly ReadOnlySpan<KeyValuePair<string, object?>> Tags =>
            _overflowTags is not null ? _overflowTags.AsSpan(0, _tagsCount) :
            ((ReadOnlySpan<KeyValuePair<string, object?>>)_tags).Slice(0, _tagsCount);

        [InlineArray(8)]
        private struct InlineTags
        {
            public const int Length = 8;
            private KeyValuePair<string, object?> _first;
        }

        public struct Enumerator : IEnumerator<KeyValuePair<string, object?>>
        {
            private TagList _tagList;
            private int _index;

            internal Enumerator(in TagList tagList)
            {
                _index = -1;
                _tagList = tagList;
            }

            public KeyValuePair<string, object?> Current => _tagList[_index];

            object IEnumerator.Current => _tagList[_index];

            public void Dispose() { _index = _tagList.Count; }

            public bool MoveNext()
            {
                _index++;
                return _index < _tagList.Count;
            }

            public void Reset() => _index = -1;
        }
    }
}
