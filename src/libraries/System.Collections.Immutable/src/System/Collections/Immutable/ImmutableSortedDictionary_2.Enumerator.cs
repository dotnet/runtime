// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Collections.Immutable
{
    public sealed partial class ImmutableSortedDictionary<TKey, TValue>
    {
        /// <summary>
        /// Enumerates the contents of a binary tree.
        /// </summary>
        /// <remarks>
        /// This struct can and should be kept in exact sync with the other binary tree enumerators:
        /// <see cref="ImmutableList{T}.Enumerator"/>, <see cref="ImmutableSortedDictionary{TKey, TValue}.Enumerator"/>, and <see cref="ImmutableSortedSet{T}.Enumerator"/>.
        ///
        /// CAUTION: when this enumerator is actually used as a valuetype (not boxed) do NOT copy it by assigning to a second variable
        /// or by passing it to another method.  When this enumerator is disposed of it returns a mutable reference type stack to a resource pool,
        /// and if the value type enumerator is copied (which can easily happen unintentionally if you pass the value around) there is a risk
        /// that a stack that has already been returned to the resource pool may still be in use by one of the enumerator copies, leading to data
        /// corruption and/or exceptions.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, ISecurePooledObjectUser
        {
            /// <summary>
            /// The builder being enumerated, if applicable.
            /// </summary>
            private readonly Builder? _builder;

            /// <summary>
            /// A unique ID for this instance of this enumerator.
            /// Used to protect pooled objects from use after they are recycled.
            /// </summary>
            private readonly int _poolUserId;

            /// <summary>
            /// The set being enumerated.
            /// </summary>
            private Node _root;

            /// <summary>
            /// The stack to use for enumerating the binary tree.
            /// </summary>
            private SecurePooledObject<Stack<RefAsValueType<Node>>>? _stack;

            /// <summary>
            /// The node currently selected.
            /// </summary>
            private Node? _current;

            /// <summary>
            /// The version of the builder (when applicable) that is being enumerated.
            /// </summary>
            private int _enumeratingBuilderVersion;

            /// <summary>
            /// Initializes an <see cref="Enumerator"/> structure.
            /// </summary>
            /// <param name="root">The root of the set to be enumerated.</param>
            /// <param name="builder">The builder, if applicable.</param>
            internal Enumerator(Node root, Builder? builder = null)
            {
                Requires.NotNull(root, nameof(root));

                _root = root;
                _builder = builder;
                _current = null;
                _enumeratingBuilderVersion = builder != null ? builder.Version : -1;
                _poolUserId = SecureObjectPool.NewId();
                _stack = null;
                if (!_root.IsEmpty)
                {
                    if (!SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator>.TryTake(this, out _stack))
                    {
                        _stack = SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator>.PrepNew(this, new Stack<RefAsValueType<Node>>(root.Height));
                    }

                    this.PushLeft(_root);
                }
            }

            /// <summary>
            /// The current element.
            /// </summary>
            public KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    this.ThrowIfDisposed();
                    if (_current != null)
                    {
                        return _current.Value;
                    }

                    throw new InvalidOperationException();
                }
            }

            /// <inheritdoc/>
            int ISecurePooledObjectUser.PoolUserId
            {
                get { return _poolUserId; }
            }

            /// <summary>
            /// The current element.
            /// </summary>
            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            /// <summary>
            /// Disposes of this enumerator and returns the stack reference to the resource pool.
            /// </summary>
            public void Dispose()
            {
                _root = null!;
                _current = null;
                if (_stack != null && _stack.TryUse(ref this, out Stack<RefAsValueType<Node>>? stack))
                {
                    stack.ClearFastWhenEmpty();
                    SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator>.TryAdd(this, _stack!);
                }

                _stack = null;
            }

            /// <summary>
            /// Advances enumeration to the next element.
            /// </summary>
            /// <returns>A value indicating whether there is another element in the enumeration.</returns>
            public bool MoveNext()
            {
                this.ThrowIfDisposed();
                this.ThrowIfChanged();

                if (_stack != null)
                {
                    var stack = _stack.Use(ref this);
                    if (stack.Count > 0)
                    {
                        Node n = stack.Pop().Value;
                        _current = n;
                        this.PushLeft(n.Right!);
                        return true;
                    }
                }

                _current = null;
                return false;
            }

            /// <summary>
            /// Restarts enumeration.
            /// </summary>
            public void Reset()
            {
                this.ThrowIfDisposed();

                _enumeratingBuilderVersion = _builder != null ? _builder.Version : -1;
                _current = null;
                if (_stack != null)
                {
                    var stack = _stack.Use(ref this);
                    stack.ClearFastWhenEmpty();
                    this.PushLeft(_root);
                }
            }

            /// <summary>
            /// Throws an <see cref="ObjectDisposedException"/> if this enumerator has been disposed.
            /// </summary>
            internal void ThrowIfDisposed()
            {
                // Since this is a struct, copies might not have been marked as disposed.
                // But the stack we share across those copies would know.
                // This trick only works when we have a non-null stack.
                // For enumerators of empty collections, there isn't any natural
                // way to know when a copy of the struct has been disposed of.

                if (_root == null || (_stack != null && !_stack.IsOwned(ref this)))
                {
                    Requires.FailObjectDisposed(this);
                }
            }

            /// <summary>
            /// Throws an exception if the underlying builder's contents have been changed since enumeration started.
            /// </summary>
            /// <exception cref="System.InvalidOperationException">Thrown if the collection has changed.</exception>
            private void ThrowIfChanged()
            {
                if (_builder != null && _builder.Version != _enumeratingBuilderVersion)
                {
                    throw new InvalidOperationException(SR.CollectionModifiedDuringEnumeration);
                }
            }

            /// <summary>
            /// Pushes this node and all its Left descendants onto the stack.
            /// </summary>
            /// <param name="node">The starting node to push onto the stack.</param>
            private void PushLeft(Node node)
            {
                Requires.NotNull(node, nameof(node));
                Debug.Assert(_stack != null);
                var stack = _stack.Use(ref this);
                while (!node.IsEmpty)
                {
                    stack.Push(new RefAsValueType<Node>(node));
                    node = node.Left!;
                }
            }
        }
    }
}
