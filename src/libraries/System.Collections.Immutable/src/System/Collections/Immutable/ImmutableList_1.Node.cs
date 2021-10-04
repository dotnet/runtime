// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Immutable
{
    public sealed partial class ImmutableList<T>
    {
        /// <summary>
        /// A node in the AVL tree storing this set.
        /// </summary>
        [DebuggerDisplay("{_key}")]
        internal sealed class Node : IBinaryTree<T>, IEnumerable<T>
        {
            /// <summary>
            /// The default empty node.
            /// </summary>
            internal static readonly Node EmptyNode = new Node();

            /// <summary>
            /// The key associated with this node.
            /// </summary>
            private T _key = default!; // Defaulting for EmptyNode, shouldn't be null for non empty nodes so didn't annotated with [MaybeNull]

            /// <summary>
            /// A value indicating whether this node has been frozen (made immutable).
            /// </summary>
            /// <remarks>
            /// Nodes must be frozen before ever being observed by a wrapping collection type
            /// to protect collections from further mutations.
            /// </remarks>
            private bool _frozen;

            /// <summary>
            /// The depth of the tree beneath this node.
            /// </summary>
            private byte _height; // AVL tree max height <= ~1.44 * log2(maxNodes + 2)

            /// <summary>
            /// The number of elements contained by this subtree starting at this node.
            /// </summary>
            /// <remarks>
            /// If this node would benefit from saving 4 bytes, we could have only a few nodes
            /// scattered throughout the graph actually record the count of nodes beneath them.
            /// Those without the count could query their descendants, which would often short-circuit
            /// when they hit a node that *does* include a count field.
            /// </remarks>
            private int _count;

            /// <summary>
            /// The left tree.
            /// </summary>
            private Node? _left;

            /// <summary>
            /// The right tree.
            /// </summary>
            private Node? _right;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableList{T}.Node"/> class
            /// that is pre-frozen.
            /// </summary>
            private Node()
            {
                _frozen = true; // the empty node is *always* frozen.
                Debug.Assert(this.IsEmpty);
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableList{T}.Node"/> class
            /// that is not yet frozen.
            /// </summary>
            /// <param name="key">The value stored by this node.</param>
            /// <param name="left">The left branch.</param>
            /// <param name="right">The right branch.</param>
            /// <param name="frozen">Whether this node is prefrozen.</param>
            private Node(T key, Node left, Node right, bool frozen = false)
            {
                Requires.NotNull(left, nameof(left));
                Requires.NotNull(right, nameof(right));
                Debug.Assert(!frozen || (left._frozen && right._frozen));

                _key = key;
                _left = left;
                _right = right;
                _height = ParentHeight(left, right);
                _count = ParentCount(left, right);
                _frozen = frozen;

                Debug.Assert(!this.IsEmpty);
            }

            /// <summary>
            /// Gets a value indicating whether this instance is empty.
            /// </summary>
            /// <value>
            /// <c>true</c> if this instance is empty; otherwise, <c>false</c>.
            /// </value>
            public bool IsEmpty
            {
                get
                {
                    Debug.Assert(!(_left == null ^ _right == null));
                    return _left == null;
                }
            }

            /// <summary>
            /// Gets the height of the tree beneath this node.
            /// </summary>
            public int Height => _height;

            /// <summary>
            /// Gets the left branch of this node.
            /// </summary>
            public Node? Left => _left;

            /// <summary>
            /// Gets the left branch of this node.
            /// </summary>
            IBinaryTree? IBinaryTree.Left => _left;

            /// <summary>
            /// Gets the right branch of this node.
            /// </summary>
            public Node? Right => _right;

            /// <summary>
            /// Gets the right branch of this node.
            /// </summary>
            IBinaryTree? IBinaryTree.Right => _right;

            /// <summary>
            /// Gets the left branch of this node.
            /// </summary>
            IBinaryTree<T>? IBinaryTree<T>.Left => _left;

            /// <summary>
            /// Gets the right branch of this node.
            /// </summary>
            IBinaryTree<T>? IBinaryTree<T>.Right => _right;

            /// <summary>
            /// Gets the value represented by the current node.
            /// </summary>
            public T Value => _key;

            /// <summary>
            /// Gets the number of elements contained by this subtree starting at this node.
            /// </summary>
            public int Count => _count;

            /// <summary>
            /// Gets the key.
            /// </summary>
            internal T Key => _key;

            /// <summary>
            /// Gets the element of the set at the given index.
            /// </summary>
            /// <param name="index">The 0-based index of the element in the set to return.</param>
            /// <returns>The element at the given position.</returns>
            internal T this[int index]
            {
                get
                {
                    Requires.Range(index >= 0 && index < this.Count, nameof(index));
                    Debug.Assert(_left != null && _right != null);

                    if (index < _left._count)
                    {
                        return _left[index];
                    }

                    if (index > _left._count)
                    {
                        return _right[index - _left._count - 1];
                    }

                    return _key;
                }
            }

            /// <summary>
            /// Gets a read-only reference to the element of the set at the given index.
            /// </summary>
            /// <param name="index">The 0-based index of the element in the set to return.</param>
            /// <returns>A read-only reference to the element at the given position.</returns>
            internal ref readonly T ItemRef(int index)
            {
                Requires.Range(index >= 0 && index < this.Count, nameof(index));

                return ref ItemRefUnchecked(index);
            }

            private ref readonly T ItemRefUnchecked(int index)
            {
                Debug.Assert(_left != null && _right != null);
                if (index < _left._count)
                {
                    return ref _left.ItemRefUnchecked(index);
                }

                if (index > _left._count)
                {
                    return ref _right.ItemRefUnchecked(index - _left._count - 1);
                }

                return ref _key;
            }

            #region IEnumerable<T> Members

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            public Enumerator GetEnumerator() => new Enumerator(this);

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            [ExcludeFromCodeCoverage] // internal, never called, but here for interface implementation
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.GetEnumerator();

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            [ExcludeFromCodeCoverage] // internal, never called, but here for interface implementation
            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            #endregion

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <param name="builder">The builder, if applicable.</param>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            internal Enumerator GetEnumerator(Builder builder) => new Enumerator(this, builder);

            /// <summary>
            /// Creates a node tree that contains the contents of a list.
            /// </summary>
            /// <param name="items">An indexable list with the contents that the new node tree should contain.</param>
            /// <param name="start">The starting index within <paramref name="items"/> that should be captured by the node tree.</param>
            /// <param name="length">The number of elements from <paramref name="items"/> that should be captured by the node tree.</param>
            /// <returns>The root of the created node tree.</returns>
            internal static Node NodeTreeFromList(IOrderedCollection<T> items, int start, int length)
            {
                Requires.NotNull(items, nameof(items));
                Requires.Range(start >= 0, nameof(start));
                Requires.Range(length >= 0, nameof(length));

                if (length == 0)
                {
                    return EmptyNode;
                }

                int rightCount = (length - 1) / 2;
                int leftCount = (length - 1) - rightCount;
                Node left = NodeTreeFromList(items, start, leftCount);
                Node right = NodeTreeFromList(items, start + leftCount + 1, rightCount);
                return new Node(items[start + leftCount], left, right, true);
            }

            /// <summary>
            /// Adds the specified key to the tree.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <returns>The new tree.</returns>
            internal Node Add(T key)
            {
                if (this.IsEmpty)
                {
                    return CreateLeaf(key);
                }

                Node newRight = _right!.Add(key);
                Node result = this.MutateRight(newRight);
                return result.IsBalanced ? result : result.BalanceRight();
            }

            /// <summary>
            /// Adds a value at a given index to this node.
            /// </summary>
            /// <param name="index">The location for the new value.</param>
            /// <param name="key">The value to add.</param>
            /// <returns>The new tree.</returns>
            internal Node Insert(int index, T key)
            {
                Requires.Range(index >= 0 && index <= this.Count, nameof(index));

                if (this.IsEmpty)
                {
                    return CreateLeaf(key);
                }

                if (index <= _left!._count)
                {
                    Node newLeft = _left.Insert(index, key);
                    Node result = this.MutateLeft(newLeft);
                    return result.IsBalanced ? result : result.BalanceLeft();
                }
                else
                {
                    Node newRight = _right!.Insert(index - _left._count - 1, key);
                    Node result = this.MutateRight(newRight);
                    return result.IsBalanced ? result : result.BalanceRight();
                }
            }

            /// <summary>
            /// Adds the specified keys to this tree.
            /// </summary>
            /// <param name="keys">The keys.</param>
            /// <returns>The new tree.</returns>
            internal Node AddRange(IEnumerable<T> keys)
            {
                Requires.NotNull(keys, nameof(keys));

                if (this.IsEmpty)
                {
                    return CreateRange(keys);
                }

                Node newRight = _right!.AddRange(keys);
                Node result = this.MutateRight(newRight);
                return result.BalanceMany();
            }

            /// <summary>
            /// Adds the specified keys at a given index to this tree.
            /// </summary>
            /// <param name="index">The location for the new keys.</param>
            /// <param name="keys">The keys.</param>
            /// <returns>The new tree.</returns>
            internal Node InsertRange(int index, IEnumerable<T> keys)
            {
                Requires.Range(index >= 0 && index <= this.Count, nameof(index));
                Requires.NotNull(keys, nameof(keys));

                if (this.IsEmpty)
                {
                    return CreateRange(keys);
                }

                Node result;
                if (index <= _left!._count)
                {
                    Node newLeft = _left.InsertRange(index, keys);
                    result = this.MutateLeft(newLeft);
                }
                else
                {
                    Node newRight = _right!.InsertRange(index - _left._count - 1, keys);
                    result = this.MutateRight(newRight);
                }

                return result.BalanceMany();
            }

            /// <summary>
            /// Removes a value at a given index to this node.
            /// </summary>
            /// <param name="index">The location for the new value.</param>
            /// <returns>The new tree.</returns>
            internal Node RemoveAt(int index)
            {
                Requires.Range(index >= 0 && index < this.Count, nameof(index));
                Debug.Assert(_left != null && _right != null);

                Node result = this;
                if (index == _left._count)
                {
                    // We have a match. If this is a leaf, just remove it
                    // by returning Empty.  If we have only one child,
                    // replace the node with the child.
                    if (_right.IsEmpty && _left.IsEmpty)
                    {
                        result = EmptyNode;
                    }
                    else if (_right.IsEmpty && !_left.IsEmpty)
                    {
                        result = _left;
                    }
                    else if (!_right.IsEmpty && _left.IsEmpty)
                    {
                        result = _right;
                    }
                    else
                    {
                        // We have two children. Remove the next-highest node and replace
                        // this node with it.
                        var successor = _right;
                        while (!successor._left!.IsEmpty)
                        {
                            successor = successor._left;
                        }

                        var newRight = _right.RemoveAt(0);
                        result = successor.MutateBoth(left: _left, right: newRight);
                    }
                }
                else if (index < _left._count)
                {
                    var newLeft = _left.RemoveAt(index);
                    result = this.MutateLeft(newLeft);
                }
                else
                {
                    var newRight = _right.RemoveAt(index - _left._count - 1);
                    result = this.MutateRight(newRight);
                }

                return result.IsEmpty || result.IsBalanced ? result : result.Balance();
            }

            /// <summary>
            /// Removes all the elements that match the conditions defined by the specified
            /// predicate.
            /// </summary>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the elements
            /// to remove.
            /// </param>
            /// <returns>
            /// The new node tree.
            /// </returns>
            internal Node RemoveAll(Predicate<T> match)
            {
                Requires.NotNull(match, nameof(match));

                var result = this;
                var enumerator = new Enumerator(result);
                try
                {
                    var startIndex = 0;
                    while (enumerator.MoveNext())
                    {
                        if (match(enumerator.Current))
                        {
                            result = result.RemoveAt(startIndex);
                            enumerator.Dispose();
                            enumerator = new Enumerator(result, startIndex: startIndex);
                        }
                        else
                        {
                            startIndex++;
                        }
                    }
                }
                finally
                {
                    enumerator.Dispose();
                }

                Debug.Assert(result != null);
                return result;
            }

            /// <summary>
            /// Replaces a value at a given index.
            /// </summary>
            /// <param name="index">The location for the new value.</param>
            /// <param name="value">The new value for the node.</param>
            /// <returns>The new tree.</returns>
            internal Node ReplaceAt(int index, T value)
            {
                Requires.Range(index >= 0 && index < this.Count, nameof(index));
                Debug.Assert(!this.IsEmpty);

                Node result = this;
                if (index == _left!._count)
                {
                    // We have a match.
                    result = this.MutateKey(value);
                }
                else if (index < _left._count)
                {
                    var newLeft = _left.ReplaceAt(index, value);
                    result = this.MutateLeft(newLeft);
                }
                else
                {
                    var newRight = _right!.ReplaceAt(index - _left._count - 1, value);
                    result = this.MutateRight(newRight);
                }

                return result;
            }

            /// <summary>
            /// Reverses the order of the elements in the entire <see cref="ImmutableList{T}"/>.
            /// </summary>
            /// <returns>The reversed list.</returns>
            internal Node Reverse() => this.Reverse(0, this.Count);

            /// <summary>
            /// Reverses the order of the elements in the specified range.
            /// </summary>
            /// <param name="index">The zero-based starting index of the range to reverse.</param>
            /// <param name="count">The number of elements in the range to reverse.</param>
            /// <returns>The reversed list.</returns>
            internal Node Reverse(int index, int count)
            {
                Requires.Range(index >= 0, nameof(index));
                Requires.Range(count >= 0, nameof(count));
                Requires.Range(index + count <= this.Count, nameof(index));

                Node result = this;
                int start = index;
                int end = index + count - 1;
                while (start < end)
                {
                    T a = result.ItemRef(start);
                    T b = result.ItemRef(end);
                    result = result
                        .ReplaceAt(end, a)
                        .ReplaceAt(start, b);
                    start++;
                    end--;
                }

                return result;
            }

            /// <summary>
            /// Sorts the elements in the entire <see cref="ImmutableList{T}"/> using
            /// the default comparer.
            /// </summary>
            internal Node Sort() => this.Sort(Comparer<T>.Default);

            /// <summary>
            /// Sorts the elements in the entire <see cref="ImmutableList{T}"/> using
            /// the specified <see cref="Comparison{T}"/>.
            /// </summary>
            /// <param name="comparison">
            /// The <see cref="Comparison{T}"/> to use when comparing elements.
            /// </param>
            /// <returns>The sorted list.</returns>
            internal Node Sort(Comparison<T> comparison)
            {
                Requires.NotNull(comparison, nameof(comparison));

                // PERF: Eventually this might be reimplemented in a way that does not require allocating an array.
                var array = new T[this.Count];
                this.CopyTo(array);
                Array.Sort(array, comparison);
                return NodeTreeFromList(array.AsOrderedCollection(), 0, this.Count);
            }

            /// <summary>
            /// Sorts the elements in the entire <see cref="ImmutableList{T}"/> using
            /// the specified comparer.
            /// </summary>
            /// <param name="comparer">
            /// The <see cref="IComparer{T}"/> implementation to use when comparing
            /// elements, or null to use the default comparer <see cref="Comparer{T}.Default"/>.
            /// </param>
            /// <returns>The sorted list.</returns>
            internal Node Sort(IComparer<T>? comparer) => this.Sort(0, this.Count, comparer);

            /// <summary>
            /// Sorts the elements in a range of elements in <see cref="ImmutableList{T}"/>
            /// using the specified comparer.
            /// </summary>
            /// <param name="index">
            /// The zero-based starting index of the range to sort.
            /// </param>
            /// <param name="count">
            /// The length of the range to sort.
            /// </param>
            /// <param name="comparer">
            /// The <see cref="IComparer{T}"/> implementation to use when comparing
            /// elements, or null to use the default comparer <see cref="Comparer{T}.Default"/>.
            /// </param>
            /// <returns>The sorted list.</returns>
            internal Node Sort(int index, int count, IComparer<T>? comparer)
            {
                Requires.Range(index >= 0, nameof(index));
                Requires.Range(count >= 0, nameof(count));
                Requires.Argument(index + count <= this.Count);

                // PERF: Eventually this might be reimplemented in a way that does not require allocating an array.
                var array = new T[this.Count];
                this.CopyTo(array);
                Array.Sort(array, index, count, comparer);
                return NodeTreeFromList(array.AsOrderedCollection(), 0, this.Count);
            }

            /// <summary>
            /// Searches a range of elements in the sorted <see cref="ImmutableList{T}"/>
            /// for an element using the specified comparer and returns the zero-based index
            /// of the element.
            /// </summary>
            /// <param name="index">The zero-based starting index of the range to search.</param>
            /// <param name="count"> The length of the range to search.</param>
            /// <param name="item">The object to locate. The value can be null for reference types.</param>
            /// <param name="comparer">
            /// The <see cref="IComparer{T}"/> implementation to use when comparing
            /// elements, or null to use the default comparer <see cref="Comparer{T}.Default"/>.
            /// </param>
            /// <returns>
            /// The zero-based index of item in the sorted <see cref="ImmutableList{T}"/>,
            /// if item is found; otherwise, a negative number that is the bitwise complement
            /// of the index of the next element that is larger than item or, if there is
            /// no larger element, the bitwise complement of <see cref="ImmutableList{T}.Count"/>.
            /// </returns>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="index"/> is less than 0.-or-<paramref name="count"/> is less than 0.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the <see cref="ImmutableList{T}"/>.
            /// </exception>
            /// <exception cref="InvalidOperationException">
            /// <paramref name="comparer"/> is null, and the default comparer <see cref="Comparer{T}.Default"/>
            /// cannot find an implementation of the <see cref="IComparable{T}"/> generic interface
            /// or the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.
            /// </exception>
            internal int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
            {
                Requires.Range(index >= 0, nameof(index));
                Requires.Range(count >= 0, nameof(count));
                comparer = comparer ?? Comparer<T>.Default;

                if (this.IsEmpty || count <= 0)
                {
                    return ~index;
                }

                // If this node is not within range, defer to either branch as appropriate.
                int thisNodeIndex = _left!.Count; // this is only the index within the AVL tree, treating this node as root rather than a member of a larger tree.
                if (index + count <= thisNodeIndex)
                {
                    return _left.BinarySearch(index, count, item, comparer);
                }
                else if (index > thisNodeIndex)
                {
                    int result = _right!.BinarySearch(index - thisNodeIndex - 1, count, item, comparer);
                    int offset = thisNodeIndex + 1;
                    return result < 0 ? result - offset : result + offset;
                }

                // We're definitely in the caller's designated range now.
                // Any possible match will be a descendant of this node (or this immediate one).
                // Some descendants may not be in range, but if we hit any it means no match was found,
                // and a negative response would come back from the above code to the below code.
                int compare = comparer.Compare(item, _key);
                if (compare == 0)
                {
                    return thisNodeIndex;
                }
                else if (compare > 0)
                {
                    int adjustedCount = count - (thisNodeIndex - index) - 1;
                    int result = adjustedCount < 0 ? -1 : _right!.BinarySearch(0, adjustedCount, item, comparer);
                    int offset = thisNodeIndex + 1;
                    return result < 0 ? result - offset : result + offset;
                }
                else
                {
                    if (index == thisNodeIndex)
                    {
                        // We can't go any further left.
                        return ~index;
                    }

                    int result = _left.BinarySearch(index, count, item, comparer);
                    return result;
                }
            }

            /// <summary>
            /// Searches for the specified object and returns the zero-based index of the
            /// first occurrence within the range of elements in the <see cref="ImmutableList{T}"/>
            /// that starts at the specified index and contains the specified number of elements.
            /// </summary>
            /// <param name="item">
            /// The object to locate in the <see cref="ImmutableList{T}"/>. The value
            /// can be null for reference types.
            /// </param>
            /// <param name="equalityComparer">The equality comparer to use for testing the match of two elements.</param>
            /// <returns>
            /// The zero-based index of the first occurrence of <paramref name="item"/> within the entire
            /// <see cref="ImmutableList{T}"/>, if found; otherwise, -1.
            /// </returns>
            internal int IndexOf(T item, IEqualityComparer<T>? equalityComparer) => this.IndexOf(item, 0, this.Count, equalityComparer);

            /// <summary>
            /// Searches for the specified object and returns <c>true</c> if it is found, <c>false</c> otherwise.
            /// </summary>
            /// <param name="item">
            /// The object to locate in the <see cref="ImmutableList{T}"/>. The value
            /// can be null for reference types.
            /// </param>
            /// <param name="equalityComparer">
            /// The equality comparer to use for testing the match of two elements.
            /// </param>
            /// <returns>
            /// <c>true</c> if it is found, <c>false</c> otherwise.
            /// </returns>
            internal bool Contains(T item, IEqualityComparer<T> equalityComparer) => Contains(this, item, equalityComparer);

            /// <summary>
            /// Searches for the specified object and returns the zero-based index of the
            /// first occurrence within the range of elements in the <see cref="ImmutableList{T}"/>
            /// that starts at the specified index and contains the specified number of elements.
            /// </summary>
            /// <param name="item">
            /// The object to locate in the <see cref="ImmutableList{T}"/>. The value
            /// can be null for reference types.
            /// </param>
            /// <param name="index">
            /// The zero-based starting index of the search. 0 (zero) is valid in an empty
            /// list.
            /// </param>
            /// <param name="count">
            /// The number of elements in the section to search.
            /// </param>
            /// <param name="equalityComparer">
            /// The equality comparer to use in the search.
            /// If <c>null</c>, <see cref="EqualityComparer{T}.Default"/> is used.
            /// </param>
            /// <returns>
            /// The zero-based index of the first occurrence of <paramref name="item"/> within the range of
            /// elements in the <see cref="ImmutableList{T}"/> that starts at <paramref name="index"/> and
            /// contains <paramref name="count"/> number of elements, if found; otherwise, -1.
            /// </returns>
            internal int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
            {
                Requires.Range(index >= 0, nameof(index));
                Requires.Range(count >= 0, nameof(count));
                Requires.Range(count <= this.Count, nameof(count));
                Requires.Range(index + count <= this.Count, nameof(count));

                equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
                using (var enumerator = new Enumerator(this, startIndex: index, count: count))
                {
                    while (enumerator.MoveNext())
                    {
                        if (equalityComparer.Equals(item, enumerator.Current))
                        {
                            return index;
                        }

                        index++;
                    }
                }

                return -1;
            }

            /// <summary>
            /// Searches for the specified object and returns the zero-based index of the
            /// last occurrence within the range of elements in the <see cref="ImmutableList{T}"/>
            /// that contains the specified number of elements and ends at the specified
            /// index.
            /// </summary>
            /// <param name="item">
            /// The object to locate in the <see cref="ImmutableList{T}"/>. The value
            /// can be null for reference types.
            /// </param>
            /// <param name="index">The zero-based starting index of the backward search.</param>
            /// <param name="count">The number of elements in the section to search.</param>
            /// <param name="equalityComparer">The equality comparer to use for testing the match of two elements.</param>
            /// <returns>
            /// The zero-based index of the last occurrence of <paramref name="item"/> within the range of elements
            /// in the <see cref="ImmutableList{T}"/> that contains <paramref name="count"/> number of elements
            /// and ends at <paramref name="index"/>, if found; otherwise, -1.
            /// </returns>
            internal int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
            {
                Requires.Range(index >= 0, nameof(index));
                Requires.Range(count >= 0 && count <= this.Count, nameof(count));
                Requires.Argument(index - count + 1 >= 0);

                equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
                using (var enumerator = new Enumerator(this, startIndex: index, count: count, reversed: true))
                {
                    while (enumerator.MoveNext())
                    {
                        if (equalityComparer.Equals(item, enumerator.Current))
                        {
                            return index;
                        }

                        index--;
                    }
                }

                return -1;
            }

            /// <summary>
            /// Copies the entire <see cref="ImmutableList{T}"/> to a compatible one-dimensional
            /// array, starting at the beginning of the target array.
            /// </summary>
            /// <param name="array">
            /// The one-dimensional <see cref="Array"/> that is the destination of the elements
            /// copied from <see cref="ImmutableList{T}"/>. The <see cref="Array"/> must have
            /// zero-based indexing.
            /// </param>
            internal void CopyTo(T[] array)
            {
                Requires.NotNull(array, nameof(array));
                Requires.Range(array.Length >= this.Count, nameof(array));

                int index = 0;
                foreach (var element in this)
                {
                    array[index++] = element;
                }
            }

            /// <summary>
            /// Copies the entire <see cref="ImmutableList{T}"/> to a compatible one-dimensional
            /// array, starting at the specified index of the target array.
            /// </summary>
            /// <param name="array">
            /// The one-dimensional <see cref="Array"/> that is the destination of the elements
            /// copied from <see cref="ImmutableList{T}"/>. The <see cref="Array"/> must have
            /// zero-based indexing.
            /// </param>
            /// <param name="arrayIndex">
            /// The zero-based index in <paramref name="array"/> at which copying begins.
            /// </param>
            internal void CopyTo(T[] array, int arrayIndex)
            {
                Requires.NotNull(array, nameof(array));
                Requires.Range(arrayIndex >= 0, nameof(arrayIndex));
                Requires.Range(array.Length >= arrayIndex + this.Count, nameof(arrayIndex));

                foreach (var element in this)
                {
                    array[arrayIndex++] = element;
                }
            }

            /// <summary>
            /// Copies a range of elements from the <see cref="ImmutableList{T}"/> to
            /// a compatible one-dimensional array, starting at the specified index of the
            /// target array.
            /// </summary>
            /// <param name="index">
            /// The zero-based index in the source <see cref="ImmutableList{T}"/> at
            /// which copying begins.
            /// </param>
            /// <param name="array">
            /// The one-dimensional <see cref="Array"/> that is the destination of the elements
            /// copied from <see cref="ImmutableList{T}"/>. The <see cref="Array"/> must have
            /// zero-based indexing.
            /// </param>
            /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
            /// <param name="count">The number of elements to copy.</param>
            internal void CopyTo(int index, T[] array, int arrayIndex, int count)
            {
                Requires.NotNull(array, nameof(array));
                Requires.Range(index >= 0, nameof(index));
                Requires.Range(count >= 0, nameof(count));
                Requires.Range(index + count <= this.Count, nameof(count));
                Requires.Range(arrayIndex >= 0, nameof(arrayIndex));
                Requires.Range(arrayIndex + count <= array.Length, nameof(arrayIndex));

                using (var enumerator = new Enumerator(this, startIndex: index, count: count))
                {
                    while (enumerator.MoveNext())
                    {
                        array[arrayIndex++] = enumerator.Current;
                    }
                }
            }

            /// <summary>
            /// Copies the elements of the <see cref="ICollection"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
            /// </summary>
            /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection"/>. The <see cref="Array"/> must have zero-based indexing.</param>
            /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
            internal void CopyTo(Array array, int arrayIndex)
            {
                Requires.NotNull(array, nameof(array));
                Requires.Range(arrayIndex >= 0, nameof(arrayIndex));
                Requires.Range(array.Length >= arrayIndex + this.Count, nameof(arrayIndex));

                foreach (var element in this)
                {
                    array.SetValue(element, arrayIndex++);
                }
            }

            /// <summary>
            /// Converts the elements in the current <see cref="ImmutableList{T}"/> to
            /// another type, and returns a list containing the converted elements.
            /// </summary>
            /// <param name="converter">
            /// A <see cref="Func{T, TResult}"/> delegate that converts each element from
            /// one type to another type.
            /// </param>
            /// <typeparam name="TOutput">
            /// The type of the elements of the target array.
            /// </typeparam>
            /// <returns>
            /// A node tree with the transformed list.
            /// </returns>
            internal ImmutableList<TOutput>.Node ConvertAll<TOutput>(Func<T, TOutput> converter)
            {
                var root = ImmutableList<TOutput>.Node.EmptyNode;

                if (this.IsEmpty)
                {
                    return root;
                }

                return root.AddRange(Linq.Enumerable.Select(this, converter));
            }

            /// <summary>
            /// Determines whether every element in the <see cref="ImmutableList{T}"/>
            /// matches the conditions defined by the specified predicate.
            /// </summary>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions to check against
            /// the elements.
            /// </param>
            /// <returns>
            /// true if every element in the <see cref="ImmutableList{T}"/> matches the
            /// conditions defined by the specified predicate; otherwise, false. If the list
            /// has no elements, the return value is true.
            /// </returns>
            internal bool TrueForAll(Predicate<T> match)
            {
                Requires.NotNull(match, nameof(match));

                foreach (var item in this)
                {
                    if (!match(item))
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// Determines whether the <see cref="ImmutableList{T}"/> contains elements
            /// that match the conditions defined by the specified predicate.
            /// </summary>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the elements
            /// to search for.
            /// </param>
            /// <returns>
            /// true if the <see cref="ImmutableList{T}"/> contains one or more elements
            /// that match the conditions defined by the specified predicate; otherwise,
            /// false.
            /// </returns>
            internal bool Exists(Predicate<T> match)
            {
                Requires.NotNull(match, nameof(match));

                foreach (T item in this)
                {
                    if (match(item))
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the first occurrence within the entire <see cref="ImmutableList{T}"/>.
            /// </summary>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
            /// to search for.
            /// </param>
            /// <returns>
            /// The first element that matches the conditions defined by the specified predicate,
            /// if found; otherwise, the default value for type <typeparamref name="T"/>.
            /// </returns>
            internal T? Find(Predicate<T> match)
            {
                Requires.NotNull(match, nameof(match));

                foreach (var item in this)
                {
                    if (match(item))
                    {
                        return item;
                    }
                }

                return default;
            }

            /// <summary>
            /// Retrieves all the elements that match the conditions defined by the specified
            /// predicate.
            /// </summary>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the elements
            /// to search for.
            /// </param>
            /// <returns>
            /// A <see cref="ImmutableList{T}"/> containing all the elements that match
            /// the conditions defined by the specified predicate, if found; otherwise, an
            /// empty <see cref="ImmutableList{T}"/>.
            /// </returns>
            internal ImmutableList<T> FindAll(Predicate<T> match)
            {
                Requires.NotNull(match, nameof(match));

                if (this.IsEmpty)
                {
                    return ImmutableList<T>.Empty;
                }

                List<T>? list = null;
                foreach (var item in this)
                {
                    if (match(item))
                    {
                        if (list == null)
                        {
                            list = new List<T>();
                        }

                        list.Add(item);
                    }
                }

                return list != null ?
                    ImmutableList.CreateRange(list) :
                    ImmutableList<T>.Empty;
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the first occurrence within
            /// the entire <see cref="ImmutableList{T}"/>.
            /// </summary>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
            /// to search for.
            /// </param>
            /// <returns>
            /// The zero-based index of the first occurrence of an element that matches the
            /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
            /// </returns>
            internal int FindIndex(Predicate<T> match)
            {
                Requires.NotNull(match, nameof(match));

                return this.FindIndex(0, _count, match);
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the first occurrence within
            /// the range of elements in the <see cref="ImmutableList{T}"/> that extends
            /// from the specified index to the last element.
            /// </summary>
            /// <param name="startIndex">The zero-based starting index of the search.</param>
            /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the element to search for.</param>
            /// <returns>
            /// The zero-based index of the first occurrence of an element that matches the
            /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
            /// </returns>
            internal int FindIndex(int startIndex, Predicate<T> match)
            {
                Requires.NotNull(match, nameof(match));
                Requires.Range(startIndex >= 0 && startIndex <= this.Count, nameof(startIndex));

                return this.FindIndex(startIndex, this.Count - startIndex, match);
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the first occurrence within
            /// the range of elements in the <see cref="ImmutableList{T}"/> that starts
            /// at the specified index and contains the specified number of elements.
            /// </summary>
            /// <param name="startIndex">The zero-based starting index of the search.</param>
            /// <param name="count">The number of elements in the section to search.</param>
            /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the element to search for.</param>
            /// <returns>
            /// The zero-based index of the first occurrence of an element that matches the
            /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
            /// </returns>
            internal int FindIndex(int startIndex, int count, Predicate<T> match)
            {
                Requires.NotNull(match, nameof(match));
                Requires.Range(startIndex >= 0, nameof(startIndex));
                Requires.Range(count >= 0, nameof(count));
                Requires.Range(startIndex + count <= this.Count, nameof(count));

                using (var enumerator = new Enumerator(this, startIndex: startIndex, count: count))
                {
                    int index = startIndex;
                    while (enumerator.MoveNext())
                    {
                        if (match(enumerator.Current))
                        {
                            return index;
                        }

                        index++;
                    }
                }

                return -1;
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the last occurrence within the entire <see cref="ImmutableList{T}"/>.
            /// </summary>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
            /// to search for.
            /// </param>
            /// <returns>
            /// The last element that matches the conditions defined by the specified predicate,
            /// if found; otherwise, the default value for type <typeparamref name="T"/>.
            /// </returns>
            internal T? FindLast(Predicate<T> match)
            {
                Requires.NotNull(match, nameof(match));

                using (var enumerator = new Enumerator(this, reversed: true))
                {
                    while (enumerator.MoveNext())
                    {
                        if (match(enumerator.Current))
                        {
                            return enumerator.Current;
                        }
                    }
                }

                return default;
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the last occurrence within
            /// the entire <see cref="ImmutableList{T}"/>.
            /// </summary>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
            /// to search for.
            /// </param>
            /// <returns>
            /// The zero-based index of the last occurrence of an element that matches the
            /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
            /// </returns>
            internal int FindLastIndex(Predicate<T> match)
            {
                Requires.NotNull(match, nameof(match));

                return this.IsEmpty ? -1 : this.FindLastIndex(this.Count - 1, this.Count, match);
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the last occurrence within
            /// the range of elements in the <see cref="ImmutableList{T}"/> that extends
            /// from the first element to the specified index.
            /// </summary>
            /// <param name="startIndex">The zero-based starting index of the backward search.</param>
            /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
            /// to search for.</param>
            /// <returns>
            /// The zero-based index of the last occurrence of an element that matches the
            /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
            /// </returns>
            internal int FindLastIndex(int startIndex, Predicate<T> match)
            {
                Requires.NotNull(match, nameof(match));
                Requires.Range(startIndex >= 0, nameof(startIndex));
                Requires.Range(startIndex == 0 || startIndex < this.Count, nameof(startIndex));

                return this.IsEmpty ? -1 : this.FindLastIndex(startIndex, startIndex + 1, match);
            }

            /// <summary>
            /// Searches for an element that matches the conditions defined by the specified
            /// predicate, and returns the zero-based index of the last occurrence within
            /// the range of elements in the <see cref="ImmutableList{T}"/> that contains
            /// the specified number of elements and ends at the specified index.
            /// </summary>
            /// <param name="startIndex">The zero-based starting index of the backward search.</param>
            /// <param name="count">The number of elements in the section to search.</param>
            /// <param name="match">
            /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
            /// to search for.
            /// </param>
            /// <returns>
            /// The zero-based index of the last occurrence of an element that matches the
            /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
            /// </returns>
            internal int FindLastIndex(int startIndex, int count, Predicate<T> match)
            {
                Requires.NotNull(match, nameof(match));
                Requires.Range(startIndex >= 0, nameof(startIndex));
                Requires.Range(count <= this.Count, nameof(count));
                Requires.Range(startIndex - count + 1 >= 0, nameof(startIndex));

                using (var enumerator = new Enumerator(this, startIndex: startIndex, count: count, reversed: true))
                {
                    int index = startIndex;
                    while (enumerator.MoveNext())
                    {
                        if (match(enumerator.Current))
                        {
                            return index;
                        }

                        index--;
                    }
                }

                return -1;
            }

            /// <summary>
            /// Freezes this node and all descendant nodes so that any mutations require a new instance of the nodes.
            /// </summary>
            internal void Freeze()
            {
                // If this node is frozen, all its descendants must already be frozen.
                if (!_frozen)
                {
                    _left!.Freeze();
                    _right!.Freeze();
                    _frozen = true;
                }
            }

            #region Tree balancing methods

            /// <summary>
            /// AVL rotate left operation.
            /// </summary>
            /// <returns>The rotated tree.</returns>
            private Node RotateLeft()
            {
                Debug.Assert(!this.IsEmpty);
                Debug.Assert(!_right!.IsEmpty);

                return _right.MutateLeft(this.MutateRight(_right._left!));
            }

            /// <summary>
            /// AVL rotate right operation.
            /// </summary>
            /// <returns>The rotated tree.</returns>
            private Node RotateRight()
            {
                Debug.Assert(!this.IsEmpty);
                Debug.Assert(!_left!.IsEmpty);

                return _left.MutateRight(this.MutateLeft(_left._right!));
            }

            /// <summary>
            /// AVL rotate double-left operation.
            /// </summary>
            /// <returns>The rotated tree.</returns>
            private Node DoubleLeft()
            {
                Debug.Assert(!this.IsEmpty);
                Debug.Assert(!_right!.IsEmpty);
                Debug.Assert(!_right!._left!.IsEmpty);

                // The following is an optimized version of rotating the right child right, then rotating the parent left.
                Node right = _right;
                Node rightLeft = right._left;
                return rightLeft.MutateBoth(
                    left: this.MutateRight(rightLeft._left!),
                    right: right.MutateLeft(rightLeft._right!));
            }

            /// <summary>
            /// AVL rotate double-right operation.
            /// </summary>
            /// <returns>The rotated tree.</returns>
            private Node DoubleRight()
            {
                Debug.Assert(!this.IsEmpty);
                Debug.Assert(!_left!.IsEmpty);
                Debug.Assert(!_left._right!.IsEmpty);

                // The following is an optimized version of rotating the left child left, then rotating the parent right.
                Node left = _left;
                Node leftRight = left._right;
                return leftRight.MutateBoth(
                    left: left.MutateRight(leftRight._left!),
                    right: this.MutateLeft(leftRight._right!));
            }

            /// <summary>
            /// Gets a value indicating whether this tree is in balance.
            /// </summary>
            /// <returns>
            /// 0 if the heights of both sides are the same, a positive integer if the right side is taller, or a negative integer if the left side is taller.
            /// </returns>
            private int BalanceFactor
            {
                get
                {
                    Debug.Assert(!this.IsEmpty);
                    return _right!._height - _left!._height;
                }
            }

            /// <summary>
            /// Determines whether this tree is right heavy.
            /// </summary>
            /// <returns>
            /// <c>true</c> if this tree is right heavy; otherwise, <c>false</c>.
            /// </returns>
            private bool IsRightHeavy => this.BalanceFactor >= 2;

            /// <summary>
            /// Determines whether this tree is left heavy.
            /// </summary>
            /// <returns>
            /// <c>true</c> if this tree is left heavy; otherwise, <c>false</c>.
            /// </returns>
            private bool IsLeftHeavy => this.BalanceFactor <= -2;

            /// <summary>
            /// Determines whether this tree has a balance factor of -1, 0, or 1.
            /// </summary>
            /// <returns>
            /// <c>true</c> if this tree is balanced; otherwise, <c>false</c>.
            /// </returns>
            private bool IsBalanced => unchecked((uint)(this.BalanceFactor + 1)) <= 2;

            /// <summary>
            /// Balances this tree.
            /// </summary>
            /// <returns>A balanced tree.</returns>
            private Node Balance() => this.IsLeftHeavy ? this.BalanceLeft() : this.BalanceRight();

            /// <summary>
            /// Balances the left side of this tree by rotating this tree rightwards.
            /// </summary>
            /// <returns>A balanced tree.</returns>
            private Node BalanceLeft()
            {
                Debug.Assert(!this.IsEmpty);
                Debug.Assert(this.IsLeftHeavy);

                return _left!.BalanceFactor > 0 ? this.DoubleRight() : this.RotateRight();
            }

            /// <summary>
            /// Balances the right side of this tree by rotating this tree leftwards.
            /// </summary>
            /// <returns>A balanced tree.</returns>
            private Node BalanceRight()
            {
                Debug.Assert(!this.IsEmpty);
                Debug.Assert(this.IsRightHeavy);

                return _right!.BalanceFactor < 0 ? this.DoubleLeft() : this.RotateLeft();
            }

            /// <summary>
            /// Balances this tree.  Allows for a large imbalance between left and
            /// right nodes, but assumes left and right nodes are individually balanced.
            /// </summary>
            /// <returns>A balanced tree.</returns>
            /// <remarks>
            /// If this tree is already balanced, this method does nothing.
            /// </remarks>
            private Node BalanceMany()
            {
                Node tree = this;
                Debug.Assert(tree._left != null && tree._right != null);
                while (!tree.IsBalanced)
                {
                    if (tree.IsRightHeavy)
                    {
                        tree = tree.BalanceRight();
                        Debug.Assert(tree._left != null);
                        tree.MutateLeft(tree._left.BalanceMany());
                    }
                    else
                    {
                        tree = tree.BalanceLeft();
                        Debug.Assert(tree._right != null);
                        tree.MutateRight(tree._right.BalanceMany());
                    }
                }

                return tree;
            }

            #endregion

            /// <summary>
            /// Creates a node mutation, either by mutating this node (if not yet frozen) or by creating a clone of this node
            /// with the described changes.
            /// </summary>
            /// <param name="left">The left branch of the mutated node.</param>
            /// <param name="right">The right branch of the mutated node.</param>
            /// <returns>The mutated (or created) node.</returns>
            private Node MutateBoth(Node left, Node right)
            {
                Requires.NotNull(left, nameof(left));
                Requires.NotNull(right, nameof(right));
                Debug.Assert(!this.IsEmpty);

                if (_frozen)
                {
                    return new Node(_key, left, right);
                }
                else
                {
                    _left = left;
                    _right = right;
                    _height = ParentHeight(left, right);
                    _count = ParentCount(left, right);
                    return this;
                }
            }

            /// <summary>
            /// Creates a node mutation, either by mutating this node (if not yet frozen) or by creating a clone of this node
            /// with the described changes.
            /// </summary>
            /// <param name="left">The left branch of the mutated node.</param>
            /// <returns>The mutated (or created) node.</returns>
            private Node MutateLeft(Node left)
            {
                Requires.NotNull(left, nameof(left));
                Debug.Assert(!this.IsEmpty);

                if (_frozen)
                {
                    return new Node(_key, left, _right!);
                }
                else
                {
                    _left = left;
                    _height = ParentHeight(left, _right!);
                    _count = ParentCount(left, _right!);
                    return this;
                }
            }

            /// <summary>
            /// Creates a node mutation, either by mutating this node (if not yet frozen) or by creating a clone of this node
            /// with the described changes.
            /// </summary>
            /// <param name="right">The right branch of the mutated node.</param>
            /// <returns>The mutated (or created) node.</returns>
            private Node MutateRight(Node right)
            {
                Requires.NotNull(right, nameof(right));
                Debug.Assert(!this.IsEmpty);

                if (_frozen)
                {
                    return new Node(_key, _left!, right);
                }
                else
                {
                    _right = right;
                    _height = ParentHeight(_left!, right);
                    _count = ParentCount(_left!, right);
                    return this;
                }
            }

            /// <summary>
            /// Calculates the height of the parent node from its children.
            /// </summary>
            /// <param name="left">The left child.</param>
            /// <param name="right">The right child.</param>
            /// <returns>The height of the parent node.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static byte ParentHeight(Node left, Node right) => checked((byte)(1 + Math.Max(left._height, right._height)));

            /// <summary>
            /// Calculates the number of elements in the parent node from its children.
            /// </summary>
            /// <param name="left">The left child.</param>
            /// <param name="right">The right child.</param>
            /// <returns>The number of elements in the parent node.</returns>
            private static int ParentCount(Node left, Node right) => 1 + left._count + right._count;

            /// <summary>
            /// Creates a node mutation, either by mutating this node (if not yet frozen) or by creating a clone of this node
            /// with the described changes.
            /// </summary>
            /// <param name="key">The new key for this node.</param>
            /// <returns>The mutated (or created) node.</returns>
            private Node MutateKey(T key)
            {
                Debug.Assert(!this.IsEmpty);

                if (_frozen)
                {
                    return new Node(key, _left!, _right!);
                }
                else
                {
                    _key = key;
                    return this;
                }
            }

            /// <summary>
            /// Creates a node from the specified keys.
            /// </summary>
            /// <param name="keys">The keys.</param>
            /// <returns>The root of the created node tree.</returns>
            private static Node CreateRange(IEnumerable<T> keys)
            {
                ImmutableList<T>? other;
                if (TryCastToImmutableList(keys, out other))
                {
                    return other._root;
                }

                var list = keys.AsOrderedCollection();
                return NodeTreeFromList(list, 0, list.Count);
            }

            /// <summary>
            /// Creates a leaf node.
            /// </summary>
            /// <param name="key">The leaf node's key.</param>
            /// <returns>The leaf node.</returns>
            private static Node CreateLeaf(T key) => new Node(key, left: EmptyNode, right: EmptyNode);

            /// <summary>
            /// Traverses the node tree looking for a node with the provided value. The provided node will be checked
            /// then we will recursively check it's left and right nodes.
            /// </summary>
            /// <param name="node">
            /// The node to check.
            /// </param>
            /// <param name="value">
            /// The value to check for.
            /// </param>
            /// <param name="equalityComparer">
            /// The equality comparer to use for testing the node and value.
            /// </param>
            /// <returns></returns>
            private static bool Contains(Node node, T value, IEqualityComparer<T> equalityComparer) => !node.IsEmpty && (equalityComparer.Equals(value, node._key) || Contains(node._left!, value, equalityComparer) || Contains(node._right!, value, equalityComparer));
        }
    }
}
