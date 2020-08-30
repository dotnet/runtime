// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner <see cref="ImmutableDictionary{TKey, TValue}.MutationResult"/> class.
    /// </content>
    public partial class ImmutableDictionary<TKey, TValue>
    {
        /// <summary>
        /// Describes the result of a mutation on the immutable data structure.
        /// </summary>
        private readonly struct MutationResult
        {
            /// <summary>
            /// The root node of the data structure after the mutation.
            /// </summary>
            private readonly SortedInt32KeyNode<HashBucket> _root;

            /// <summary>
            /// The number of elements added or removed from the collection as a result of the operation (a negative number represents removed elements).
            /// </summary>
            private readonly int _countAdjustment;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableDictionary{TKey, TValue}.MutationResult"/> struct.
            /// </summary>
            /// <param name="unchangedInput">The unchanged input.</param>
            internal MutationResult(MutationInput unchangedInput)
            {
                _root = unchangedInput.Root;
                _countAdjustment = 0;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableDictionary{TKey, TValue}.MutationResult"/> struct.
            /// </summary>
            /// <param name="root">The root.</param>
            /// <param name="countAdjustment">The count adjustment.</param>
            internal MutationResult(SortedInt32KeyNode<HashBucket> root, int countAdjustment)
            {
                Requires.NotNull(root, nameof(root));
                _root = root;
                _countAdjustment = countAdjustment;
            }

            /// <summary>
            /// Gets the root node of the data structure after the mutation.
            /// </summary>
            internal SortedInt32KeyNode<HashBucket> Root
            {
                get { return _root; }
            }

            /// <summary>
            /// Gets the number of elements added or removed from the collection as a result of the operation (a negative number represents removed elements).
            /// </summary>
            internal int CountAdjustment
            {
                get { return _countAdjustment; }
            }

            /// <summary>
            /// Returns an immutable dictionary that captures the result of this mutation.
            /// </summary>
            /// <param name="priorMap">The prior version of the map.  Used to capture the equality comparer and previous count, when applicable.</param>
            /// <returns>The new collection.</returns>
            internal ImmutableDictionary<TKey, TValue> Finalize(ImmutableDictionary<TKey, TValue> priorMap)
            {
                Requires.NotNull(priorMap, nameof(priorMap));
                return priorMap.Wrap(this.Root, priorMap._count + this.CountAdjustment);
            }
        }
    }
}
