// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A node in the AVL tree storing key/value pairs with Int32 keys.
    /// </summary>
    /// <remarks>
    /// This is a trimmed down version of <see cref="ImmutableSortedDictionary{TKey, TValue}.Node"/>
    /// with <c>TKey</c> fixed to be <see cref="int"/>.  This avoids multiple interface-based dispatches while examining
    /// each node in the tree during a lookup: an interface call to the comparer's <see cref="IComparer{T}.Compare"/> method,
    /// and then an interface call to <see cref="int"/>'s <see cref="IComparable{T}.CompareTo"/> method as part of
    /// the comparer's <see cref="IComparer{T}.Compare"/> implementation.
    /// </remarks>
    [DebuggerDisplay("{_key} = {_value}")]
    internal sealed partial class SortedInt32KeyNode<TValue> : IBinaryTree
    {
        /// <summary>
        /// The default empty node.
        /// </summary>
        internal static readonly SortedInt32KeyNode<TValue> EmptyNode = new SortedInt32KeyNode<TValue>();

        /// <summary>
        /// The Int32 key associated with this node.
        /// </summary>
        private readonly int _key;

        /// <summary>
        /// The value associated with this node.
        /// </summary>
        private readonly TValue? _value;

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
        private byte _height; // AVL tree height <= ~1.44 * log2(numNodes + 2)

        /// <summary>
        /// The left tree.
        /// </summary>
        private SortedInt32KeyNode<TValue>? _left;

        /// <summary>
        /// The right tree.
        /// </summary>
        private SortedInt32KeyNode<TValue>? _right;

        /// <summary>
        /// Initializes a new instance of the <see cref="SortedInt32KeyNode{TValue}"/> class that is pre-frozen.
        /// </summary>
        private SortedInt32KeyNode()
        {
            _frozen = true; // the empty node is *always* frozen.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortedInt32KeyNode{TValue}"/> class that is not yet frozen.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="frozen">Whether this node is prefrozen.</param>
        private SortedInt32KeyNode(int key, TValue value, SortedInt32KeyNode<TValue> left, SortedInt32KeyNode<TValue> right, bool frozen = false)
        {
            Requires.NotNull(left, nameof(left));
            Requires.NotNull(right, nameof(right));
            Debug.Assert(!frozen || (left._frozen && right._frozen));

            _key = key;
            _value = value;
            _left = left;
            _right = right;
            _frozen = frozen;

            _height = checked((byte)(1 + Math.Max(left._height, right._height)));
        }

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty { get { return _left == null; } }

        /// <summary>
        /// Gets the height of the tree beneath this node.
        /// </summary>
        public int Height { get { return _height; } }

        /// <summary>
        /// Gets the left branch of this node.
        /// </summary>
        public SortedInt32KeyNode<TValue>? Left { get { return _left; } }

        /// <summary>
        /// Gets the right branch of this node.
        /// </summary>
        public SortedInt32KeyNode<TValue>? Right { get { return _right; } }

        /// <summary>
        /// Gets the left branch of this node.
        /// </summary>
        IBinaryTree? IBinaryTree.Left { get { return _left; } }

        /// <summary>
        /// Gets the right branch of this node.
        /// </summary>
        IBinaryTree? IBinaryTree.Right { get { return _right; } }

        /// <summary>
        /// Gets the number of elements contained by this node and below.
        /// </summary>
        int IBinaryTree.Count
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets the value represented by the current node.
        /// </summary>
        public KeyValuePair<int, TValue> Value
        {
            get { return new KeyValuePair<int, TValue>(_key, _value!); }
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        internal IEnumerable<TValue> Values
        {
            get
            {
                foreach (var pair in this)
                {
                    yield return pair.Value;
                }
            }
        }

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
        /// Adds the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="valueComparer">The value comparer.</param>
        /// <param name="replacedExistingValue">Receives a value indicating whether an existing value was replaced.</param>
        /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
        internal SortedInt32KeyNode<TValue> SetItem(int key, TValue value, IEqualityComparer<TValue> valueComparer, out bool replacedExistingValue, out bool mutated)
        {
            Requires.NotNull(valueComparer, nameof(valueComparer));

            return this.SetOrAdd(key, value, valueComparer, true, out replacedExistingValue, out mutated);
        }

        /// <summary>
        /// Removes the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
        /// <returns>The new AVL tree.</returns>
        internal SortedInt32KeyNode<TValue> Remove(int key, out bool mutated)
        {
            return this.RemoveRecursive(key, out mutated);
        }

        /// <summary>
        /// Gets the value or default.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value.</returns>
        internal TValue? GetValueOrDefault(int key)
        {
            SortedInt32KeyNode<TValue> node = this;
            while (true)
            {
                if (node.IsEmpty)
                {
                    return default;
                }

                if (key == node._key)
                {
                    return node._value!;
                }

                if (key > node._key)
                {
                    node = node._right!;
                }
                else
                {
                    node = node._left!;
                }
            }
        }

        /// <summary>
        /// Tries to get the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>True if the key was found.</returns>
        internal bool TryGetValue(int key, [MaybeNullWhen(false)] out TValue value)
        {
            SortedInt32KeyNode<TValue> node = this;
            while (true)
            {
                if (node.IsEmpty)
                {
                    value = default;
                    return false;
                }

                if (key == node._key)
                {
                    value = node._value!;
                    return true;
                }

                if (key > node._key)
                {
                    node = node._right!;
                }
                else
                {
                    node = node._left!;
                }
            }
        }

        /// <summary>
        /// Freezes this node and all descendant nodes so that any mutations require a new instance of the nodes.
        /// </summary>
        internal void Freeze(Action<KeyValuePair<int, TValue>>? freezeAction = null)
        {
            // If this node is frozen, all its descendants must already be frozen.
            if (!_frozen)
            {
                freezeAction?.Invoke(new KeyValuePair<int, TValue>(_key, _value!));

                _left!.Freeze(freezeAction);
                _right!.Freeze(freezeAction);
                _frozen = true;
            }
        }

        /// <summary>
        /// AVL rotate left operation.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <returns>The rotated tree.</returns>
        private static SortedInt32KeyNode<TValue> RotateLeft(SortedInt32KeyNode<TValue> tree)
        {
            Requires.NotNull(tree, nameof(tree));
            Debug.Assert(!tree.IsEmpty);

            if (tree._right!.IsEmpty)
            {
                return tree;
            }

            var right = tree._right;
            return right.Mutate(left: tree.Mutate(right: right._left!));
        }

        /// <summary>
        /// AVL rotate right operation.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <returns>The rotated tree.</returns>
        private static SortedInt32KeyNode<TValue> RotateRight(SortedInt32KeyNode<TValue> tree)
        {
            Requires.NotNull(tree, nameof(tree));
            Debug.Assert(!tree.IsEmpty);

            if (tree._left!.IsEmpty)
            {
                return tree;
            }

            var left = tree._left;
            return left.Mutate(right: tree.Mutate(left: left._right!));
        }

        /// <summary>
        /// AVL rotate double-left operation.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <returns>The rotated tree.</returns>
        private static SortedInt32KeyNode<TValue> DoubleLeft(SortedInt32KeyNode<TValue> tree)
        {
            Requires.NotNull(tree, nameof(tree));
            Debug.Assert(!tree.IsEmpty);

            if (tree._right!.IsEmpty)
            {
                return tree;
            }

            SortedInt32KeyNode<TValue> rotatedRightChild = tree.Mutate(right: RotateRight(tree._right));
            return RotateLeft(rotatedRightChild);
        }

        /// <summary>
        /// AVL rotate double-right operation.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <returns>The rotated tree.</returns>
        private static SortedInt32KeyNode<TValue> DoubleRight(SortedInt32KeyNode<TValue> tree)
        {
            Requires.NotNull(tree, nameof(tree));
            Debug.Assert(!tree.IsEmpty);

            if (tree._left!.IsEmpty)
            {
                return tree;
            }

            SortedInt32KeyNode<TValue> rotatedLeftChild = tree.Mutate(left: RotateLeft(tree._left));
            return RotateRight(rotatedLeftChild);
        }

        /// <summary>
        /// Returns a value indicating whether the tree is in balance.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <returns>0 if the tree is in balance, a positive integer if the right side is heavy, or a negative integer if the left side is heavy.</returns>
        private static int Balance(SortedInt32KeyNode<TValue> tree)
        {
            Requires.NotNull(tree, nameof(tree));
            Debug.Assert(!tree.IsEmpty);

            return tree._right!._height - tree._left!._height;
        }

        /// <summary>
        /// Determines whether the specified tree is right heavy.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <returns>
        /// <c>true</c> if [is right heavy] [the specified tree]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsRightHeavy(SortedInt32KeyNode<TValue> tree)
        {
            Requires.NotNull(tree, nameof(tree));
            Debug.Assert(!tree.IsEmpty);
            return Balance(tree) >= 2;
        }

        /// <summary>
        /// Determines whether the specified tree is left heavy.
        /// </summary>
        private static bool IsLeftHeavy(SortedInt32KeyNode<TValue> tree)
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
        private static SortedInt32KeyNode<TValue> MakeBalanced(SortedInt32KeyNode<TValue> tree)
        {
            Requires.NotNull(tree, nameof(tree));
            Debug.Assert(!tree.IsEmpty);

            if (IsRightHeavy(tree))
            {
                return Balance(tree._right!) < 0 ? DoubleLeft(tree) : RotateLeft(tree);
            }

            if (IsLeftHeavy(tree))
            {
                return Balance(tree._left!) > 0 ? DoubleRight(tree) : RotateRight(tree);
            }

            return tree;
        }

        /// <summary>
        /// Adds the specified key. Callers are expected to have validated arguments.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="valueComparer">The value comparer.</param>
        /// <param name="overwriteExistingValue">if <c>true</c>, an existing key=value pair will be overwritten with the new one.</param>
        /// <param name="replacedExistingValue">Receives a value indicating whether an existing value was replaced.</param>
        /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
        /// <returns>The new AVL tree.</returns>
        private SortedInt32KeyNode<TValue> SetOrAdd(int key, TValue value, IEqualityComparer<TValue> valueComparer, bool overwriteExistingValue, out bool replacedExistingValue, out bool mutated)
        {
            // Arg validation skipped in this private method because it's recursive and the tax
            // of revalidating arguments on each recursive call is significant.
            // All our callers are therefore required to have done input validation.
            replacedExistingValue = false;
            if (this.IsEmpty)
            {
                mutated = true;
                return new SortedInt32KeyNode<TValue>(key, value, this, this);
            }
            else
            {
                SortedInt32KeyNode<TValue> result = this;
                if (key > _key)
                {
                    var newRight = _right!.SetOrAdd(key, value, valueComparer, overwriteExistingValue, out replacedExistingValue, out mutated);
                    if (mutated)
                    {
                        result = this.Mutate(right: newRight);
                    }
                }
                else if (key < _key)
                {
                    var newLeft = _left!.SetOrAdd(key, value, valueComparer, overwriteExistingValue, out replacedExistingValue, out mutated);
                    if (mutated)
                    {
                        result = this.Mutate(left: newLeft);
                    }
                }
                else
                {
                    if (valueComparer.Equals(_value!, value))
                    {
                        mutated = false;
                        return this;
                    }
                    else if (overwriteExistingValue)
                    {
                        mutated = true;
                        replacedExistingValue = true;
                        result = new SortedInt32KeyNode<TValue>(key, value, _left!, _right!);
                    }
                    else
                    {
                        throw new ArgumentException(SR.Format(SR.DuplicateKey, key));
                    }
                }

                return mutated ? MakeBalanced(result) : result;
            }
        }

        /// <summary>
        /// Removes the specified key. Callers are expected to validate arguments.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
        /// <returns>The new AVL tree.</returns>
        private SortedInt32KeyNode<TValue> RemoveRecursive(int key, out bool mutated)
        {
            if (this.IsEmpty)
            {
                mutated = false;
                return this;
            }
            else
            {
                Debug.Assert(_right != null && _left != null);
                SortedInt32KeyNode<TValue> result = this;
                if (key == _key)
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
                        while (!successor._left!.IsEmpty)
                        {
                            successor = successor._left;
                        }

                        bool dummyMutated;
                        var newRight = _right.Remove(successor._key, out dummyMutated);
                        result = successor.Mutate(left: _left, right: newRight);
                    }
                }
                else if (key < _key)
                {
                    var newLeft = _left.Remove(key, out mutated);
                    if (mutated)
                    {
                        result = this.Mutate(left: newLeft);
                    }
                }
                else
                {
                    var newRight = _right.Remove(key, out mutated);
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
        private SortedInt32KeyNode<TValue> Mutate(SortedInt32KeyNode<TValue>? left = null, SortedInt32KeyNode<TValue>? right = null)
        {
            Debug.Assert(_right != null && _left != null);
            if (_frozen)
            {
                return new SortedInt32KeyNode<TValue>(_key, _value!, left ?? _left, right ?? _right);
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
    }
}
