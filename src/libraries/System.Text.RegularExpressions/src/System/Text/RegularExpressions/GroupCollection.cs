// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Returns the set of captured groups in a single match. The collection is immutable (read-only) and
    /// has no public constructor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="GroupCollection" /> class is a zero-based collection class that consists of one or
    /// more <see cref="Group" /> objects that provide information about captured groups in a regular
    /// expression match. The collection is immutable (read-only) and has no public constructor. A
    /// <see cref="GroupCollection" /> object is returned by the <see cref="Match.Groups" /> property.
    /// </para>
    /// <para>
    /// The collection contains one or more <see cref="Group" /> objects. If the match is successful, the
    /// first element in the collection contains the <see cref="Group" /> object that corresponds to the
    /// entire match. Each subsequent element represents a captured group, if the regular expression
    /// includes capturing groups. Matches from numbered (unnamed) capturing groups appear in numeric
    /// order before matches from named capturing groups. If the match is unsuccessful, the collection
    /// contains a single <see cref="Group" /> object whose <see cref="Group.Success" /> property is
    /// <see langword="false" /> and whose <see cref="Capture.Value" /> property equals
    /// <see cref="string.Empty" />.
    /// </para>
    /// <para>
    /// To iterate through the members of the collection, you should use the collection iteration
    /// construct provided by your language (such as <c>foreach</c> in C#) instead of retrieving the
    /// enumerator that is returned by the <see cref="GetEnumerator" /> method.
    /// </para>
    /// </remarks>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(CollectionDebuggerProxy<Group>))]
    public class GroupCollection : IList<Group>, IReadOnlyList<Group>, IList, IReadOnlyDictionary<string, Group>
    {
        private readonly Match _match;
        private readonly Hashtable? _captureMap;

        /// <summary>Cache of Group objects fed to the user.</summary>
        private Group[]? _groups;

        internal GroupCollection(Match match, Hashtable? caps)
        {
            _match = match;
            _captureMap = caps;
        }

        internal void Reset() => _groups = null;

        /// <summary>Gets a value that indicates whether the collection is read only.</summary>
        /// <value><see langword="true" /> in all cases.</value>
        public bool IsReadOnly => true;

        /// <summary>Returns the number of groups in the collection.</summary>
        /// <value>The number of groups in the collection.</value>
        /// <remarks>
        /// The <see cref="GroupCollection" /> object always has at least one member. If a match is
        /// unsuccessful, the <see cref="Match.Groups" /> property returns a
        /// <see cref="GroupCollection" /> object that contains a single member.
        /// </remarks>
        public int Count => _match._matchcount.Length;

        /// <summary>Enables access to a member of the collection by integer index.</summary>
        /// <param name="groupnum">The zero-based index of the collection member to be retrieved.</param>
        /// <value>The member of the collection specified by <paramref name="groupnum" />.</value>
        /// <remarks>
        /// <para>
        /// This property is the indexer of the <see cref="GroupCollection" />
        /// class. It allows you to enumerate the members of the collection by using a <c>foreach</c>
        /// statement.
        /// </para>
        /// <para>
        /// You can also use this property to retrieve individual captured groups by their index number.
        /// You can determine the number of items in the collection by retrieving the value of the
        /// <see cref="Count" /> property. Valid values for the <paramref name="groupnum" /> parameter
        /// range from 0 to one less than the number of items in the collection.
        /// </para>
        /// <para>
        /// If <paramref name="groupnum" /> is not the index of a member of the collection, or if
        /// <paramref name="groupnum" /> is the index of a capturing group that has not been matched in
        /// the input string, the method returns a <see cref="Group" /> object whose
        /// <see cref="Group.Success" /> property is <see langword="false" /> and whose
        /// <c>Group.Value</c> property is <see cref="string.Empty" />.
        /// </para>
        /// </remarks>
        public Group this[int groupnum] => GetGroup(groupnum);

        /// <summary>Enables access to a member of the collection by string index.</summary>
        /// <param name="groupname">The name of a capturing group.</param>
        /// <value>The member of the collection specified by <paramref name="groupname" />.</value>
        /// <remarks>
        /// <para>
        /// <paramref name="groupname" /> can be either the name of a capturing group that is defined by
        /// the <c>(?&lt;name&gt;)</c> element in a regular expression, or the string representation of
        /// the number of a capturing group.
        /// </para>
        /// <para>
        /// If <paramref name="groupname" /> is not the name of a capturing group in the collection, or
        /// if <paramref name="groupname" /> is the name of a capturing group that has not been matched in
        /// the input string, the method returns a <see cref="Group" /> object whose
        /// <see cref="Group.Success" /> property is <see langword="false" /> and whose
        /// <c>Group.Value</c> property is <see cref="string.Empty" />.
        /// </para>
        /// </remarks>
        public Group this[string groupname] => _match._regex is null ?
            Group.s_emptyGroup :
            GetGroup(_match._regex.GroupNumberFromName(groupname));

        /// <summary>Provides an enumerator that iterates through the collection.</summary>
        /// <returns>
        /// An enumerator that contains all <see cref="Group" /> objects in the
        /// <see cref="GroupCollection" />.
        /// </returns>
        /// <remarks>
        /// Instead of calling the <see cref="GetEnumerator" /> method to retrieve an enumerator that lets
        /// you iterate through the <see cref="Group" /> objects in the collection, you should use the
        /// collection iteration construct provided by your programming language (such as <c>foreach</c>
        /// in C#).
        /// </remarks>
        public IEnumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<Group> IEnumerable<Group>.GetEnumerator() => new Enumerator(this);

        private Group GetGroup(int groupnum)
        {
            if (_captureMap != null)
            {
                if (_captureMap.TryGetValue(groupnum, out int groupNumImpl))
                {
                    return GetGroupImpl(groupNumImpl);
                }
            }
            else if ((uint)groupnum < _match._matchcount.Length)
            {
                return GetGroupImpl(groupnum);
            }

            return Group.s_emptyGroup;
        }

        /// <summary>
        /// Caches the group objects
        /// </summary>
        private Group GetGroupImpl(int groupnum)
        {
            if (groupnum == 0)
            {
                return _match;
            }

            // Construct all the Group objects the first time GetGroup is called
            if (_groups is null)
            {
                _groups = new Group[_match._matchcount.Length - 1];
                for (int i = 0; i < _groups.Length; i++)
                {
                    string groupname = _match._regex!.GroupNameFromNumber(i + 1);
                    _groups[i] = new Group(_match.Text, _match._matches[i + 1], _match._matchcount[i + 1], groupname);
                }
            }

            return _groups[groupnum - 1];
        }

        /// <summary>
        /// Gets a value that indicates whether access to the <see cref="GroupCollection" /> is
        /// synchronized (thread-safe).
        /// </summary>
        /// <value><see langword="false" /> in all cases.</value>
        public bool IsSynchronized => false;

        /// <summary>
        /// Gets an object that can be used to synchronize access to the
        /// <see cref="GroupCollection" />.
        /// </summary>
        /// <value>
        /// A copy of the <see cref="Match" /> object to synchronize.
        /// </value>
        public object SyncRoot => _match;

        /// <summary>
        /// Copies all the elements of the collection to the given array beginning at the given index.
        /// </summary>
        /// <param name="array">The array the collection is to be copied into.</param>
        /// <param name="arrayIndex">The position in the destination array where copying is to begin.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="array" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="IndexOutOfRangeException">
        /// <paramref name="arrayIndex" /> is outside the bounds of <paramref name="array" />.
        /// -or-
        /// <paramref name="arrayIndex" /> plus <see cref="Count" /> is outside the bounds of
        /// <paramref name="array" />.
        /// </exception>
        public void CopyTo(Array array, int arrayIndex)
        {
            if (array is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            for (int i = arrayIndex, j = 0; j < Count; i++, j++)
            {
                array.SetValue(this[j], i);
            }
        }

        /// <summary>
        /// Copies the elements of the group collection to a <see cref="Group" /> array, starting at a
        /// particular array index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional array that is the destination of the elements copied from the group
        /// collection. The array must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        /// The zero-based index in <paramref name="array" /> at which copying begins.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="array" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="arrayIndex" /> is less than zero.
        /// -or-
        /// <paramref name="arrayIndex" /> is greater than the length of <paramref name="array" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The length of <paramref name="array" /> minus <paramref name="arrayIndex" /> is less than the
        /// group collection count.
        /// </exception>
        public void CopyTo(Group[] array, int arrayIndex)
        {
            if (array is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);

            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            }

            for (int i = arrayIndex, j = 0; j < Count; i++, j++)
            {
                array[i] = this[j];
            }
        }

        int IList<Group>.IndexOf(Group item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (EqualityComparer<Group>.Default.Equals(this[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        void IList<Group>.Insert(int index, Group item) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        void IList<Group>.RemoveAt(int index) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        Group IList<Group>.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void ICollection<Group>.Add(Group item) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        void ICollection<Group>.Clear() =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        bool ICollection<Group>.Contains(Group item) =>
            ((IList<Group>)this).IndexOf(item) >= 0;

        bool ICollection<Group>.Remove(Group item) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        int IList.Add(object? value) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        void IList.Clear() =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        bool IList.Contains(object? value) =>
            value is Group other && ((ICollection<Group>)this).Contains(other);

        int IList.IndexOf(object? value) =>
            value is Group other ? ((IList<Group>)this).IndexOf(other) : -1;

        void IList.Insert(int index, object? value) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        bool IList.IsFixedSize => true;

        void IList.Remove(object? value) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        void IList.RemoveAt(int index) =>
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

        object? IList.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        IEnumerator<KeyValuePair<string, Group>> IEnumerable<KeyValuePair<string, Group>>.GetEnumerator() =>
            new Enumerator(this);

        /// <summary>
        /// Attempts to retrieve a group identified by the provided name key, if it exists in the group
        /// collection.
        /// </summary>
        /// <param name="key">A string with the group name key to look for.</param>
        /// <param name="value">
        /// When the method returns, the group whose name is <paramref name="key" />, if it is found;
        /// otherwise, <see langword="null" /> if not found.
        /// </param>
        /// <returns>
        /// <see langword="true" /> if a group identified by the provided name key exists;
        /// <see langword="false" /> otherwise.
        /// </returns>
        public bool TryGetValue(string key, [NotNullWhen(true)] out Group? value)
        {
            Group group = this[key];
            if (group == Group.s_emptyGroup)
            {
                value = null;
                return false;
            }

            value = group;
            return true;
        }

        /// <summary>
        /// Determines whether the group collection contains a captured group identified by the specified
        /// name.
        /// </summary>
        /// <param name="key">A string with the name of the captured group to locate.</param>
        /// <returns>
        /// <see langword="true" /> if the group collection contains a captured group identified by
        /// <paramref name="key" />; <see langword="false" /> otherwise.
        /// </returns>
        public bool ContainsKey(string key) => _match._regex!.GroupNumberFromName(key) >= 0;

        /// <summary>Gets a string enumeration that contains the name keys of the group collection.</summary>
        /// <value>The name keys of the group collection.</value>
        public IEnumerable<string> Keys
        {
            get
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return GetGroup(i).Name;
                }
            }
        }

        /// <summary>Gets a group enumeration with all the groups in the group collection.</summary>
        /// <value>A group enumeration.</value>
        public IEnumerable<Group> Values
        {
            get
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return GetGroup(i);
                }
            }
        }

        private sealed class Enumerator : IEnumerator<Group>, IEnumerator<KeyValuePair<string, Group>>
        {
            private readonly GroupCollection _collection;
            private int _index;

            internal Enumerator(GroupCollection collection)
            {
                Debug.Assert(collection != null, "collection cannot be null.");

                _collection = collection;
                _index = -1;
            }

            public bool MoveNext()
            {
                int size = _collection.Count;

                if (_index >= size)
                {
                    return false;
                }

                _index++;
                return _index < size;
            }

            public Group Current
            {
                get
                {
                    if (_index < 0 || _index >= _collection.Count)
                    {
                        throw new InvalidOperationException(SR.EnumNotStarted);
                    }

                    return _collection[_index];
                }
            }

            KeyValuePair<string, Group> IEnumerator<KeyValuePair<string, Group>>.Current
            {
                get
                {
                    if ((uint)_index >= _collection.Count)
                    {
                        throw new InvalidOperationException(SR.EnumNotStarted);
                    }

                    Group value = _collection[_index];
                    return new KeyValuePair<string, Group>(value.Name, value);

                }
            }

            object IEnumerator.Current => Current;

            void IEnumerator.Reset() => _index = -1;

            void IDisposable.Dispose() { }
        }
    }
}
