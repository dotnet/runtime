// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner <see cref="ImmutableHashSet{T}.MutationResult"/> class.
    /// </content>
    public partial class ImmutableHashSet<T>
    {
        /// <summary>
        /// Interpretations for a <see cref="MutationResult.Count"/> member.
        /// </summary>
        private enum CountType
        {
            /// <summary>
            /// The <see cref="MutationResult.Count"/> member describes an adjustment to the previous count of the collection.
            /// </summary>
            Adjustment,

            /// <summary>
            /// The <see cref="MutationResult.Count"/> member describes the actual count of the collection.
            /// </summary>
            FinalValue,
        }

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
            /// Either the number of elements added or removed from the collection as a result of the operation (a negative number represents removed elements),
            /// or the total number of elements in the collection after the mutation.  The appropriate interpretation of this value is indicated by the 
            /// <see cref="_countType"/> field.
            /// </summary>
            private readonly int _count;

            /// <summary>
            /// Whether to consider the <see cref="_count"/> field to be a count adjustment or total count.
            /// </summary>
            private readonly CountType _countType;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableHashSet{T}.MutationResult"/> struct.
            /// </summary>
            /// <param name="root">The root node of the result.</param>
            /// <param name="count">The total element count or a count adjustment.</param>
            /// <param name="countType">The appropriate interpretation for the <paramref name="count"/> parameter.</param>
            internal MutationResult(SortedInt32KeyNode<HashBucket> root, int count, CountType countType = ImmutableHashSet<T>.CountType.Adjustment)
            {
                Requires.NotNull(root, nameof(root));
                _root = root;
                _count = count;
                _countType = countType;
            }

            /// <summary>
            /// Gets the root node of the data structure after the mutation.
            /// </summary>
            internal SortedInt32KeyNode<HashBucket> Root
            {
                get { return _root; }
            }

            /// <summary>
            /// Gets either the number of elements added or removed from the collection as a result of the operation (a negative number represents removed elements),
            /// or the total number of elements in the collection after the mutation.  The appropriate interpretation of this value is indicated by the 
            /// <see cref="CountType"/> property.
            /// </summary>
            internal int Count
            {
                get { return _count; }
            }

            /// <summary>
            /// Gets the appropriate interpretation for the <see cref="Count"/> property; whether to be a count adjustment or total count.
            /// </summary>
            internal CountType CountType
            {
                get { return _countType; }
            }

            /// <summary>
            /// Returns an immutable hash set that captures the result of this mutation.
            /// </summary>
            /// <param name="priorSet">The prior version of the set.  Used to capture the equality comparer and previous count, when applicable.</param>
            /// <returns>The new collection.</returns>
            internal ImmutableHashSet<T> Finalize(ImmutableHashSet<T> priorSet)
            {
                Requires.NotNull(priorSet, nameof(priorSet));
                int count = this.Count;
                if (this.CountType == ImmutableHashSet<T>.CountType.Adjustment)
                {
                    count += priorSet._count;
                }

                return priorSet.Wrap(this.Root, count);
            }
        }
    }
}
