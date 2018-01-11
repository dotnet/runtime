// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace System.Collections.Immutable
{
    public sealed partial class ImmutableSortedDictionary<TKey, TValue>
    {
        /// <summary>
        /// A node in the AVL tree storing this map.
        /// </summary>
        [DebuggerDisplay("{_key} = {_value}")]
        internal sealed class Node : IBinaryTree<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>
        {
            /// <summary>
            /// The default empty node.
            /// </summary>
            internal static readonly Node EmptyNode = new Node();

            /// <summary>
            /// The key associated with this node.
            /// </summary>
            private readonly TKey _key;

            /// <summary>
            /// The value associated with this node.
            /// </summary>
            private readonly TValue _value;

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
            /// The left tree.
            /// </summary>
            private Node _left;

            /// <summary>
            /// The right tree.
            /// </summary>
            private Node _right;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableSortedDictionary{TKey, TValue}.Node"/> class
            /// that is pre-frozen.
            /// </summary>
            private Node()
            {
                Contract.Ensures(this.IsEmpty);
                _frozen = true; // the empty node is *always* frozen.
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableSortedDictionary{TKey, TValue}.Node"/> class
            /// that is not yet frozen.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="value">The value.</param>
            /// <param name="left">The left.</param>
            /// <param name="right">The right.</param>
            /// <param name="frozen">Whether this node is prefrozen.</param>
            private Node(TKey key, TValue value, Node left, Node right, bool frozen = false)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(left, nameof(left));
                Requires.NotNull(right, nameof(right));
                Debug.Assert(!frozen || (left._frozen && right._frozen));
                Contract.Ensures(!this.IsEmpty);
                Contract.Ensures(_key != null);
                Contract.Ensures(_left == left);
                Contract.Ensures(_right == right);

                _key = key;
                _value = value;
                _left = left;
                _right = right;
                _height = checked((byte)(1 + Math.Max(left._height, right._height)));
                _frozen = frozen;
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
                    Contract.Ensures((_left != null && _right != null) || Contract.Result<bool>());
                    return _left == null;
                }
            }

            /// <summary>
            /// Gets the left branch of this node.
            /// </summary>
            IBinaryTree<KeyValuePair<TKey, TValue>> IBinaryTree<KeyValuePair<TKey, TValue>>.Left
            {
                get { return _left; }
            }

            /// <summary>
            /// Gets the right branch of this node.
            /// </summary>
            IBinaryTree<KeyValuePair<TKey, TValue>> IBinaryTree<KeyValuePair<TKey, TValue>>.Right
            {
                get { return _right; }
            }

            /// <summary>
            /// Gets the height of the tree beneath this node.
            /// </summary>
            public int Height { get { return _height; } }

            /// <summary>
            /// Gets the left branch of this node.
            /// </summary>
            public Node Left { get { return _left; } }

            /// <summary>
            /// Gets the left branch of this node.
            /// </summary>
            IBinaryTree IBinaryTree.Left
            {
                get { return _left; }
            }

            /// <summary>
            /// Gets the right branch of this node.
            /// </summary>
            public Node Right { get { return _right; } }

            /// <summary>
            /// Gets the right branch of this node.
            /// </summary>
            IBinaryTree IBinaryTree.Right
            {
                get { return _right; }
            }

            /// <summary>
            /// Gets the value represented by the current node.
            /// </summary>
            public KeyValuePair<TKey, TValue> Value
            {
                get { return new KeyValuePair<TKey, TValue>(_key, _value); }
            }

            /// <summary>
            /// Gets the number of elements contained by this node and below.
            /// </summary>
            int IBinaryTree.Count
            {
                get { throw new NotSupportedException(); }
            }

            /// <summary>
            /// Gets the keys.
            /// </summary>
            internal IEnumerable<TKey> Keys
            {
                get { return Linq.Enumerable.Select(this, p => p.Key); }
            }

            /// <summary>
            /// Gets the values.
            /// </summary>
            internal IEnumerable<TValue> Values
            {
                get { return Linq.Enumerable.Select(this, p => p.Value); }
            }

            #region IEnumerable<TKey> Members

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(this);
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <param name="builder">The builder, if applicable.</param>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            internal Enumerator GetEnumerator(Builder builder)
            {
                return new Enumerator(this, builder);
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            internal void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex, int dictionarySize)
            {
                Requires.NotNull(array, nameof(array));
                Requires.Range(arrayIndex >= 0, nameof(arrayIndex));
                Requires.Range(array.Length >= arrayIndex + dictionarySize, nameof(arrayIndex));

                foreach (var item in this)
                {
                    array[arrayIndex++] = item;
                }
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            internal void CopyTo(Array array, int arrayIndex, int dictionarySize)
            {
                Requires.NotNull(array, nameof(array));
                Requires.Range(arrayIndex >= 0, nameof(arrayIndex));
                Requires.Range(array.Length >= arrayIndex + dictionarySize, nameof(arrayIndex));

                foreach (var item in this)
                {
                    array.SetValue(new DictionaryEntry(item.Key, item.Value), arrayIndex++);
                }
            }

            /// <summary>
            /// Creates a node tree from an existing (mutable) collection.
            /// </summary>
            /// <param name="dictionary">The collection.</param>
            /// <returns>The root of the node tree.</returns>
            [Pure]
            internal static Node NodeTreeFromSortedDictionary(SortedDictionary<TKey, TValue> dictionary)
            {
                Requires.NotNull(dictionary, nameof(dictionary));
                Contract.Ensures(Contract.Result<Node>() != null);

                var list = dictionary.AsOrderedCollection();
                return NodeTreeFromList(list, 0, list.Count);
            }

            /// <summary>
            /// Adds the specified key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="value">The value.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="valueComparer">The value comparer.</param>
            /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
            internal Node Add(TKey key, TValue value, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer, out bool mutated)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(keyComparer, nameof(keyComparer));
                Requires.NotNull(valueComparer, nameof(valueComparer));

                bool dummy;
                return this.SetOrAdd(key, value, keyComparer, valueComparer, false, out dummy, out mutated);
            }

            /// <summary>
            /// Adds the specified key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="value">The value.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="valueComparer">The value comparer.</param>
            /// <param name="replacedExistingValue">Receives a value indicating whether an existing value was replaced.</param>
            /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
            internal Node SetItem(TKey key, TValue value, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer, out bool replacedExistingValue, out bool mutated)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(keyComparer, nameof(keyComparer));
                Requires.NotNull(valueComparer, nameof(valueComparer));

                return this.SetOrAdd(key, value, keyComparer, valueComparer, true, out replacedExistingValue, out mutated);
            }

            /// <summary>
            /// Removes the specified key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
            /// <returns>The new AVL tree.</returns>
            internal Node Remove(TKey key, IComparer<TKey> keyComparer, out bool mutated)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(keyComparer, nameof(keyComparer));

                return this.RemoveRecursive(key, keyComparer, out mutated);
            }

#if FEATURE_ITEMREFAPI
            /// <summary>
            /// Returns a read-only reference to the value associated with the provided key.
            /// </summary>
            /// <exception cref="KeyNotFoundException">If the key is not present.</exception>
            [Pure]
            internal ref readonly TValue ValueRef(TKey key, IComparer<TKey> keyComparer)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(keyComparer, nameof(keyComparer));

                var match = this.Search(key, keyComparer);
                if (match.IsEmpty)
                {
                    throw new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key.ToString()));
                }

                return ref match._value;
            }
#endif

            /// <summary>
            /// Tries to get the value.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="value">The value.</param>
            /// <returns>True if the key was found.</returns>
            [Pure]
            internal bool TryGetValue(TKey key, IComparer<TKey> keyComparer, out TValue value)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(keyComparer, nameof(keyComparer));

                var match = this.Search(key, keyComparer);
                if (match.IsEmpty)
                {
                    value = default(TValue);
                    return false;
                }
                else
                {
                    value = match._value;
                    return true;
                }
            }

            /// <summary>
            /// Searches the dictionary for a given key and returns the equal key it finds, if any.
            /// </summary>
            /// <param name="equalKey">The key to search for.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="actualKey">The key from the dictionary that the search found, or <paramref name="equalKey"/> if the search yielded no match.</param>
            /// <returns>A value indicating whether the search was successful.</returns>
            /// <remarks>
            /// This can be useful when you want to reuse a previously stored reference instead of
            /// a newly constructed one (so that more sharing of references can occur) or to look up
            /// the canonical value, or a value that has more complete data than the value you currently have,
            /// although their comparer functions indicate they are equal.
            /// </remarks>
            [Pure]
            internal bool TryGetKey(TKey equalKey, IComparer<TKey> keyComparer, out TKey actualKey)
            {
                Requires.NotNullAllowStructs(equalKey, nameof(equalKey));
                Requires.NotNull(keyComparer, nameof(keyComparer));

                var match = this.Search(equalKey, keyComparer);
                if (match.IsEmpty)
                {
                    actualKey = equalKey;
                    return false;
                }
                else
                {
                    actualKey = match._key;
                    return true;
                }
            }

            /// <summary>
            /// Determines whether the specified key contains key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <returns>
            /// <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
            /// </returns>
            [Pure]
            internal bool ContainsKey(TKey key, IComparer<TKey> keyComparer)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(keyComparer, nameof(keyComparer));
                return !this.Search(key, keyComparer).IsEmpty;
            }

            /// <summary>
            /// Determines whether the <see cref="ImmutableSortedDictionary{TKey, TValue}"/>
            /// contains an element with the specified value.
            /// </summary>
            /// <param name="value">
            /// The value to locate in the <see cref="ImmutableSortedDictionary{TKey, TValue}"/>.
            /// The value can be null for reference types.
            /// </param>
            /// <param name="valueComparer">The value comparer to use.</param>
            /// <returns>
            /// true if the <see cref="ImmutableSortedDictionary{TKey, TValue}"/> contains
            /// an element with the specified value; otherwise, false.
            /// </returns>
            [Pure]
            internal bool ContainsValue(TValue value, IEqualityComparer<TValue> valueComparer)
            {
                Requires.NotNull(valueComparer, nameof(valueComparer));
                foreach (KeyValuePair<TKey, TValue> item in this)
                {
                    if (valueComparer.Equals(value, item.Value))
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Determines whether [contains] [the specified pair].
            /// </summary>
            /// <param name="pair">The pair.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="valueComparer">The value comparer.</param>
            /// <returns>
            /// <c>true</c> if [contains] [the specified pair]; otherwise, <c>false</c>.
            /// </returns>
            [Pure]
            internal bool Contains(KeyValuePair<TKey, TValue> pair, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
            {
                Requires.NotNullAllowStructs(pair.Key, nameof(pair.Key));
                Requires.NotNull(keyComparer, nameof(keyComparer));
                Requires.NotNull(valueComparer, nameof(valueComparer));

                var matchingNode = this.Search(pair.Key, keyComparer);
                if (matchingNode.IsEmpty)
                {
                    return false;
                }

                return valueComparer.Equals(matchingNode._value, pair.Value);
            }

            /// <summary>
            /// Freezes this node and all descendant nodes so that any mutations require a new instance of the nodes.
            /// </summary>
            internal void Freeze()
            {
                // If this node is frozen, all its descendants must already be frozen.
                if (!_frozen)
                {
                    _left.Freeze();
                    _right.Freeze();
                    _frozen = true;
                }
            }

            #region Tree balancing methods

            /// <summary>
            /// AVL rotate left operation.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>The rotated tree.</returns>
            private static Node RotateLeft(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (tree._right.IsEmpty)
                {
                    return tree;
                }

                var right = tree._right;
                return right.Mutate(left: tree.Mutate(right: right._left));
            }

            /// <summary>
            /// AVL rotate right operation.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>The rotated tree.</returns>
            private static Node RotateRight(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (tree._left.IsEmpty)
                {
                    return tree;
                }

                var left = tree._left;
                return left.Mutate(right: tree.Mutate(left: left._right));
            }

            /// <summary>
            /// AVL rotate double-left operation.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>The rotated tree.</returns>
            private static Node DoubleLeft(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (tree._right.IsEmpty)
                {
                    return tree;
                }

                Node rotatedRightChild = tree.Mutate(right: RotateRight(tree._right));
                return RotateLeft(rotatedRightChild);
            }

            /// <summary>
            /// AVL rotate double-right operation.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>The rotated tree.</returns>
            private static Node DoubleRight(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (tree._left.IsEmpty)
                {
                    return tree;
                }

                Node rotatedLeftChild = tree.Mutate(left: RotateLeft(tree._left));
                return RotateRight(rotatedLeftChild);
            }

            /// <summary>
            /// Returns a value indicating whether the tree is in balance.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>0 if the tree is in balance, a positive integer if the right side is heavy, or a negative integer if the left side is heavy.</returns>
            [Pure]
            private static int Balance(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);

                return tree._right._height - tree._left._height;
            }

            /// <summary>
            /// Determines whether the specified tree is right heavy.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>
            /// <c>true</c> if [is right heavy] [the specified tree]; otherwise, <c>false</c>.
            /// </returns>
            [Pure]
            private static bool IsRightHeavy(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);
                return Balance(tree) >= 2;
            }

            /// <summary>
            /// Determines whether the specified tree is left heavy.
            /// </summary>
            [Pure]
            private static bool IsLeftHeavy(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);
                return Balance(tree) <= -2;
            }

            /// <summary>
            /// Balances the specified tree.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>A balanced tree.</returns>
            [Pure]
            private static Node MakeBalanced(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (IsRightHeavy(tree))
                {
                    return Balance(tree._right) < 0 ? DoubleLeft(tree) : RotateLeft(tree);
                }

                if (IsLeftHeavy(tree))
                {
                    return Balance(tree._left) > 0 ? DoubleRight(tree) : RotateRight(tree);
                }

                return tree;
            }

            #endregion

            /// <summary>
            /// Creates a node tree that contains the contents of a list.
            /// </summary>
            /// <param name="items">An indexable list with the contents that the new node tree should contain.</param>
            /// <param name="start">The starting index within <paramref name="items"/> that should be captured by the node tree.</param>
            /// <param name="length">The number of elements from <paramref name="items"/> that should be captured by the node tree.</param>
            /// <returns>The root of the created node tree.</returns>
            [Pure]
            private static Node NodeTreeFromList(IOrderedCollection<KeyValuePair<TKey, TValue>> items, int start, int length)
            {
                Requires.NotNull(items, nameof(items));
                Requires.Range(start >= 0, nameof(start));
                Requires.Range(length >= 0, nameof(length));
                Contract.Ensures(Contract.Result<Node>() != null);

                if (length == 0)
                {
                    return EmptyNode;
                }

                int rightCount = (length - 1) / 2;
                int leftCount = (length - 1) - rightCount;
                Node left = NodeTreeFromList(items, start, leftCount);
                Node right = NodeTreeFromList(items, start + leftCount + 1, rightCount);
                var item = items[start + leftCount];
                return new Node(item.Key, item.Value, left, right, true);
            }

            /// <summary>
            /// Adds the specified key. Callers are expected to have validated arguments.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="value">The value.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="valueComparer">The value comparer.</param>
            /// <param name="overwriteExistingValue">if <c>true</c>, an existing key=value pair will be overwritten with the new one.</param>
            /// <param name="replacedExistingValue">Receives a value indicating whether an existing value was replaced.</param>
            /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
            /// <returns>The new AVL tree.</returns>
            private Node SetOrAdd(TKey key, TValue value, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer, bool overwriteExistingValue, out bool replacedExistingValue, out bool mutated)
            {
                // Arg validation skipped in this private method because it's recursive and the tax
                // of revalidating arguments on each recursive call is significant.
                // All our callers are therefore required to have done input validation.
                replacedExistingValue = false;
                if (this.IsEmpty)
                {
                    mutated = true;
                    return new Node(key, value, this, this);
                }
                else
                {
                    Node result = this;
                    int compareResult = keyComparer.Compare(key, _key);
                    if (compareResult > 0)
                    {
                        var newRight = _right.SetOrAdd(key, value, keyComparer, valueComparer, overwriteExistingValue, out replacedExistingValue, out mutated);
                        if (mutated)
                        {
                            result = this.Mutate(right: newRight);
                        }
                    }
                    else if (compareResult < 0)
                    {
                        var newLeft = _left.SetOrAdd(key, value, keyComparer, valueComparer, overwriteExistingValue, out replacedExistingValue, out mutated);
                        if (mutated)
                        {
                            result = this.Mutate(left: newLeft);
                        }
                    }
                    else
                    {
                        if (valueComparer.Equals(_value, value))
                        {
                            mutated = false;
                            return this;
                        }
                        else if (overwriteExistingValue)
                        {
                            mutated = true;
                            replacedExistingValue = true;
                            result = new Node(key, value, _left, _right);
                        }
                        else
                        {
                            throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.DuplicateKey, key));
                        }
                    }

                    return mutated ? MakeBalanced(result) : result;
                }
            }

            /// <summary>
            /// Removes the specified key. Callers are expected to validate arguments.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
            /// <returns>The new AVL tree.</returns>
            private Node RemoveRecursive(TKey key, IComparer<TKey> keyComparer, out bool mutated)
            {
                // Skip parameter validation because it's too expensive and pointless in recursive methods.
                if (this.IsEmpty)
                {
                    mutated = false;
                    return this;
                }
                else
                {
                    Node result = this;
                    int compare = keyComparer.Compare(key, _key);
                    if (compare == 0)
                    {
                        // We have a match.
                        mutated = true;

                        // If this is a leaf, just remove it 
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
                            while (!successor._left.IsEmpty)
                            {
                                successor = successor._left;
                            }

                            bool dummyMutated;
                            var newRight = _right.Remove(successor._key, keyComparer, out dummyMutated);
                            result = successor.Mutate(left: _left, right: newRight);
                        }
                    }
                    else if (compare < 0)
                    {
                        var newLeft = _left.Remove(key, keyComparer, out mutated);
                        if (mutated)
                        {
                            result = this.Mutate(left: newLeft);
                        }
                    }
                    else
                    {
                        var newRight = _right.Remove(key, keyComparer, out mutated);
                        if (mutated)
                        {
                            result = this.Mutate(right: newRight);
                        }
                    }

                    return result.IsEmpty ? result : MakeBalanced(result);
                }
            }

            /// <summary>
            /// Creates a node mutation, either by mutating this node (if not yet frozen) or by creating a clone of this node
            /// with the described changes.
            /// </summary>
            /// <param name="left">The left branch of the mutated node.</param>
            /// <param name="right">The right branch of the mutated node.</param>
            /// <returns>The mutated (or created) node.</returns>
            private Node Mutate(Node left = null, Node right = null)
            {
                if (_frozen)
                {
                    return new Node(_key, _value, left ?? _left, right ?? _right);
                }
                else
                {
                    if (left != null)
                    {
                        _left = left;
                    }

                    if (right != null)
                    {
                        _right = right;
                    }

                    _height = checked((byte)(1 + Math.Max(_left._height, _right._height)));
                    return this;
                }
            }

            /// <summary>
            /// Searches the specified key. Callers are expected to validate arguments.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="keyComparer">The key comparer.</param>
            [Pure]
            private Node Search(TKey key, IComparer<TKey> keyComparer)
            {
                // Arg validation is too expensive for recursive methods.
                // Callers are expected to have validated parameters.
                if (this.IsEmpty)
                {
                    return this;
                }
                else
                {
                    int compare = keyComparer.Compare(key, _key);
                    if (compare == 0)
                    {
                        return this;
                    }
                    else if (compare > 0)
                    {
                        return _right.Search(key, keyComparer);
                    }
                    else
                    {
                        return _left.Search(key, keyComparer);
                    }
                }
            }
        }
    }
}
