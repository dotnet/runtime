// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Diagnostics
{
    // This struct is purposed to store a list of tags. It avoids allocating any memory till we have more than eight tags to store, then it will create an array at that time.
    // To avoid the allocations, the struct define eight fields Tag1, Tag2,...,Tag8 to store up to eight tags. If need to store more than eight tags, it will create
    // a managed array at that time.
    // The main consumer of this struct is the Metrics APIs which create a span from this struct to send it with the reported measurements.
    // As we need to have this struct work on NetFX too, we couldn't use any .NET collection as we need to create a span from such collection.
    // Instead, we use regular managed array and we expand it as needed. It is easy to create a span from such managed array without allocating more memory.

    /// <summary>
    /// Represents a list of tags that can be accessed by index. Provides methods to search, sort, and manipulate lists.
    /// </summary>
    /// <remarks>
    /// TagList can be used in the scenarios which need to optimize for memory allocations. TagList will avoid allocating any memory when using up to eight tags.
    /// Using more than eight tags will cause allocating memory to store the tags.
    /// Public static (Shared in Visual Basic) members of this type are thread safe. Any instance members are not guaranteed to be thread safe.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct TagList : IList<KeyValuePair<string, object?>>, IReadOnlyList<KeyValuePair<string, object?>>
    {
        internal KeyValuePair<string, object?> Tag1;
        internal KeyValuePair<string, object?> Tag2;
        internal KeyValuePair<string, object?> Tag3;
        internal KeyValuePair<string, object?> Tag4;
        internal KeyValuePair<string, object?> Tag5;
        internal KeyValuePair<string, object?> Tag6;
        internal KeyValuePair<string, object?> Tag7;
        internal KeyValuePair<string, object?> Tag8;
        private int _tagsCount;
        private KeyValuePair<string, object?>[]? _overflowTags;
        private const int OverflowAdditionalCapacity = 8;

        /// <summary>
        /// Initializes a new instance of the TagList structure using the specified <paramref name="tagList" />.
        /// </summary>
        /// <param name="tagList">A span of tags to initialize the list with.</param>
        public TagList(ReadOnlySpan<KeyValuePair<string, object?>> tagList) : this()
        {
            _tagsCount = tagList.Length;
            switch (_tagsCount)
            {
                case 8:
                    Tag8 = tagList[7];
                    goto case 7;

                case 7:
                    Tag7 = tagList[6];
                    goto case 6;

                case 6:
                    Tag6 = tagList[5];
                    goto case 5;

                case 5:
                    Tag5 = tagList[4];
                    goto case 4;

                case 4:
                    Tag4 = tagList[3];
                    goto case 3;

                case 3:
                    Tag3 = tagList[2];
                    goto case 2;

                case 2:
                    Tag2 = tagList[1];
                    goto case 1;

                case 1:
                    Tag1 = tagList[0];
                    break;

                case 0: return;

                default:
                    Debug.Assert(_tagsCount > 8);
                    _overflowTags = new KeyValuePair<string, object?>[_tagsCount + OverflowAdditionalCapacity]; // Add extra slots for more tags to add if needed
                    tagList.CopyTo(_overflowTags);
                    break;
            }
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
                if ((uint)index >= (uint)_tagsCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (_overflowTags is not null)
                {
                    Debug.Assert(index < _overflowTags.Length);
                    return _overflowTags[index];
                }

                Debug.Assert(index <= 7);

                return index switch
                {
                    0 => Tag1,
                    1 => Tag2,
                    2 => Tag3,
                    3 => Tag4,
                    4 => Tag5,
                    5 => Tag6,
                    6 => Tag7,
                    7 => Tag8,
                    _ => default, // we shouldn't come here anyway.
                };
            }

            set
            {
                if ((uint)index >= (uint)_tagsCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (_overflowTags is not null)
                {
                    Debug.Assert(index < _overflowTags.Length);
                    _overflowTags[index] = value;
                    return;
                }

                switch (index)
                {
                    case 0: Tag1 = value; break;
                    case 1: Tag2 = value; break;
                    case 2: Tag3 = value; break;
                    case 3: Tag4 = value; break;
                    case 4: Tag5 = value; break;
                    case 5: Tag6 = value; break;
                    case 6: Tag7 = value; break;
                    case 7: Tag8 = value; break;
                    default:
                        Debug.Assert(false);
                        break;
                }
            }
        }

        /// <summary>
        /// Adds a tag with the provided <paramref name="key" /> and <paramref name="value" /> to the list.
        /// </summary>
        /// <param name="key">The tag key.</param>
        /// <param name="value">The tag value.</param>
        public void Add(string key, object? value) => Add(new KeyValuePair<string, object?>(key, value));

        /// <summary>
        /// Adds a tag to the list.
        /// </summary>
        /// <param name="tag">Key and value pair of the tag to add to the list.</param>
        public void Add(KeyValuePair<string, object?> tag)
        {
            if (_overflowTags is not null)
            {
                if (_tagsCount == _overflowTags.Length)
                {
                    Array.Resize(ref _overflowTags, _tagsCount + OverflowAdditionalCapacity);
                }

                _overflowTags[_tagsCount++] = tag;
                return;
            }

            Debug.Assert(_tagsCount <= 8);

            switch (_tagsCount)
            {
                case 0: Tag1 = tag; break;
                case 1: Tag2 = tag; break;
                case 2: Tag3 = tag; break;
                case 3: Tag4 = tag; break;
                case 4: Tag5 = tag; break;
                case 5: Tag6 = tag; break;
                case 6: Tag7 = tag; break;
                case 7: Tag8 = tag; break;
                case 8:
                    Debug.Assert(_overflowTags is null);
                    MoveTagsToTheArray();
                    Debug.Assert(_overflowTags is not null);
                    _overflowTags[8] = tag;
                    break;
                default:
                    // We shouldn't come here.
                    Debug.Assert(_overflowTags is null);
                    return;
            }
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

            if (_overflowTags is not null)
            {
                _overflowTags.AsSpan(0, _tagsCount).CopyTo(tags);
                return;
            }

            Debug.Assert(_tagsCount <= 8);

            switch (_tagsCount)
            {
                case 0: break;
                case 8: tags[7] = Tag8; goto case 7;
                case 7: tags[6] = Tag7; goto case 6;
                case 6: tags[5] = Tag6; goto case 5;
                case 5: tags[4] = Tag5; goto case 4;
                case 4: tags[3] = Tag4; goto case 3;
                case 3: tags[2] = Tag3; goto case 2;
                case 2: tags[1] = Tag2; goto case 1;
                case 1: tags[0] = Tag1; break;
            }
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
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if ((uint)arrayIndex >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

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
            if ((uint)index > (uint)_tagsCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (index == _tagsCount)
            {
                Add(item);
                return;
            }

            if (_tagsCount == 8 && _overflowTags is null)
            {
                MoveTagsToTheArray();
                Debug.Assert(_overflowTags is not null);
            }

            if (_overflowTags is not null)
            {
                if (_tagsCount == _overflowTags.Length)
                {
                    Array.Resize(ref _overflowTags, _tagsCount + OverflowAdditionalCapacity);
                }

                for (int i = _tagsCount; i > index; i--)
                {
                    _overflowTags[i] = _overflowTags[i - 1];
                }
                _overflowTags[index] = item;
                _tagsCount++;
                return;
            }

            Debug.Assert(_tagsCount < 8 && index < 7);

            switch (index)
            {
                case 0: Tag8 = Tag7; Tag7 = Tag6; Tag6 = Tag5; Tag5 = Tag4; Tag4 = Tag3; Tag3 = Tag2; Tag2 = Tag1; Tag1 = item; break;
                case 1: Tag8 = Tag7; Tag7 = Tag6; Tag6 = Tag5; Tag5 = Tag4; Tag4 = Tag3; Tag3 = Tag2; Tag2 = item; break;
                case 2: Tag8 = Tag7; Tag7 = Tag6; Tag6 = Tag5; Tag5 = Tag4; Tag4 = Tag3; Tag3 = item; break;
                case 3: Tag8 = Tag7; Tag7 = Tag6; Tag6 = Tag5; Tag5 = Tag4; Tag4 = item; break;
                case 4: Tag8 = Tag7; Tag7 = Tag6; Tag6 = Tag5; Tag5 = item; break;
                case 5: Tag8 = Tag7; Tag7 = Tag6; Tag6 = item; break;
                case 6: Tag8 = Tag7; Tag7 = item; break;
                default:
                    Debug.Assert(false); // we shouldn't come here
                    return;
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
            if ((uint)index >= (uint)_tagsCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (_overflowTags is not null)
            {
                for (int i = index; i < _tagsCount - 1; i++)
                {
                    _overflowTags[i] = _overflowTags[i + 1];
                }

                _tagsCount--;
                return;
            }

            Debug.Assert(_tagsCount <= 8 && index <= 7);

            switch (index)
            {
                case 0: Tag1 = Tag2; goto case 1;
                case 1: Tag2 = Tag3; goto case 2;
                case 2: Tag3 = Tag4; goto case 3;
                case 3: Tag4 = Tag5; goto case 4;
                case 4: Tag5 = Tag6; goto case 5;
                case 5: Tag6 = Tag7; goto case 6;
                case 6: Tag7 = Tag8; break;
                case 7: break;
            }
            _tagsCount--;
        }

        /// <summary>
        /// Removes all elements from the <see cref="T:System.Diagnostics.TagList" />.
        /// </summary>
        public void Clear() => _tagsCount = 0;

        /// <summary>
        /// Determines whether an tag is in the <see cref="T:System.Diagnostics.TagList" />.
        /// </summary>
        /// <param name="item">The tag to locate in the <see cref="T:System.Diagnostics.TagList" />.</param>
        /// <returns><see langword="true" /> if item is found in the <see cref="T:System.Diagnostics.TagList" />; otherwise, <see langword="false" />.</returns>
        public readonly bool Contains(KeyValuePair<string, object?> item) => IndexOf(item) >= 0;

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
            if (_overflowTags is not null)
            {
                for (int i = 0; i < _tagsCount; i++)
                {
                    if (TagsEqual(_overflowTags[i], item))
                    {
                        return i;
                    }
                }
                return -1;
            }

            Debug.Assert(_tagsCount <= 8);

            switch (_tagsCount)
            {
                case 1: if (TagsEqual(Tag1, item)) { return 0; };
                        break;
                case 2: if (TagsEqual(Tag1, item)) { return 0; }
                        if (TagsEqual(Tag2, item)) { return 1; };
                        break;
                case 3: if (TagsEqual(Tag1, item)) { return 0; }
                        if (TagsEqual(Tag2, item)) { return 1; };
                        if (TagsEqual(Tag3, item)) { return 2; };
                        break;
                case 4: if (TagsEqual(Tag1, item)) { return 0; }
                        if (TagsEqual(Tag2, item)) { return 1; };
                        if (TagsEqual(Tag3, item)) { return 2; };
                        if (TagsEqual(Tag4, item)) { return 3; };
                        break;
                case 5: if (TagsEqual(Tag1, item)) { return 0; }
                        if (TagsEqual(Tag2, item)) { return 1; };
                        if (TagsEqual(Tag3, item)) { return 2; };
                        if (TagsEqual(Tag4, item)) { return 3; };
                        if (TagsEqual(Tag5, item)) { return 4; };
                        break;
                case 6: if (TagsEqual(Tag1, item)) { return 0; }
                        if (TagsEqual(Tag2, item)) { return 1; };
                        if (TagsEqual(Tag3, item)) { return 2; };
                        if (TagsEqual(Tag4, item)) { return 3; };
                        if (TagsEqual(Tag5, item)) { return 4; };
                        if (TagsEqual(Tag6, item)) { return 5; };
                        break;
                case 7: if (TagsEqual(Tag1, item)) { return 0; }
                        if (TagsEqual(Tag2, item)) { return 1; };
                        if (TagsEqual(Tag3, item)) { return 2; };
                        if (TagsEqual(Tag4, item)) { return 3; };
                        if (TagsEqual(Tag5, item)) { return 4; };
                        if (TagsEqual(Tag6, item)) { return 5; };
                        if (TagsEqual(Tag7, item)) { return 6; };
                        break;
                case 8: if (TagsEqual(Tag1, item)) { return 0; }
                        if (TagsEqual(Tag2, item)) { return 1; };
                        if (TagsEqual(Tag3, item)) { return 2; };
                        if (TagsEqual(Tag4, item)) { return 3; };
                        if (TagsEqual(Tag5, item)) { return 4; };
                        if (TagsEqual(Tag6, item)) { return 5; };
                        if (TagsEqual(Tag7, item)) { return 6; };
                        if (TagsEqual(Tag8, item)) { return 7; };
                        break;
            }

            return -1;
        }

        internal readonly KeyValuePair<string, object?>[]? Tags => _overflowTags;

        private static bool TagsEqual(KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
        {
            // Keys always of string type so using equality operator would be enough.
            if (tag1.Key != tag2.Key)
            {
                return false;
            }

            // Values are of Object type which need to call Equals method on them.
            if (tag1.Value is null)
            {
                if (tag2.Value is not null)
                {
                    return false;
                }
            }
            else
            {
                if (!tag1.Value.Equals(tag2.Value))
                {
                    return false;
                }
            }

            return true;
        }

        private void MoveTagsToTheArray()
        {
            _overflowTags = new KeyValuePair<string, object?>[16];
            _overflowTags[0] = Tag1;
            _overflowTags[1] = Tag2;
            _overflowTags[2] = Tag3;
            _overflowTags[3] = Tag4;
            _overflowTags[4] = Tag5;
            _overflowTags[5] = Tag6;
            _overflowTags[6] = Tag7;
            _overflowTags[7] = Tag8;
        }

        public struct Enumerator : IEnumerator<KeyValuePair<string, object?>>, IEnumerator
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
