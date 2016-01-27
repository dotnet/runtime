// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner <see cref="ImmutableHashSet{T}.Enumerator"/> class.
    /// </content>
    public partial class ImmutableHashSet<T>
    {
        /// <summary>
        /// Enumerates the contents of the collection in an allocation-free manner.
        /// </summary>
        public struct Enumerator : IEnumerator<T>, IStrongEnumerator<T>
        {
            /// <summary>
            /// The builder being enumerated, if applicable.
            /// </summary>
            private readonly Builder _builder;

            /// <summary>
            /// The enumerator over the sorted dictionary whose keys are hash values.
            /// </summary>
            private SortedInt32KeyNode<HashBucket>.Enumerator _mapEnumerator;

            /// <summary>
            /// The enumerator in use within an individual HashBucket.
            /// </summary>
            private HashBucket.Enumerator _bucketEnumerator;

            /// <summary>
            /// The version of the builder (when applicable) that is being enumerated.
            /// </summary>
            private int _enumeratingBuilderVersion;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableHashSet{T}.Enumerator"/> struct.
            /// </summary>
            /// <param name="root">The root.</param>
            /// <param name="builder">The builder, if applicable.</param>
            internal Enumerator(SortedInt32KeyNode<HashBucket> root, Builder builder = null)
            {
                _builder = builder;
                _mapEnumerator = new SortedInt32KeyNode<HashBucket>.Enumerator(root);
                _bucketEnumerator = default(HashBucket.Enumerator);
                _enumeratingBuilderVersion = builder != null ? builder.Version : -1;
            }

            /// <summary>
            /// Gets the current element.
            /// </summary>
            public T Current
            {
                get
                {
                    _mapEnumerator.ThrowIfDisposed();
                    return _bucketEnumerator.Current;
                }
            }

            /// <summary>
            /// Gets the current element.
            /// </summary>
            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            public bool MoveNext()
            {
                this.ThrowIfChanged();

                if (_bucketEnumerator.MoveNext())
                {
                    return true;
                }

                if (_mapEnumerator.MoveNext())
                {
                    _bucketEnumerator = new HashBucket.Enumerator(_mapEnumerator.Current.Value);
                    return _bucketEnumerator.MoveNext();
                }

                return false;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            public void Reset()
            {
                _enumeratingBuilderVersion = _builder != null ? _builder.Version : -1;
                _mapEnumerator.Reset();

                // Resetting the bucket enumerator is pointless because we'll start on a new bucket later anyway.
                _bucketEnumerator.Dispose();
                _bucketEnumerator = default(HashBucket.Enumerator);
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                _mapEnumerator.Dispose();
                _bucketEnumerator.Dispose();
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
        }
    }
}
