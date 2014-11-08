// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Validation;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner MutationResult class.
    /// </content>
    public partial class ImmutableDictionary<TKey, TValue>
    {
        /// <summary>
        /// Describes the result of a mutation on the immutable data structure.
        /// </summary>
        private struct MutationResult
        {
            /// <summary>
            /// The root node of the data structure after the mutation.
            /// </summary>
            private readonly ImmutableSortedDictionary<int, HashBucket>.Node root;

            /// <summary>
            /// The number of elements added or removed from the collection as a result of the operation (a negative number represents removed elements).
            /// </summary>
            private readonly int countAdjustment;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableDictionary&lt;TKey, TValue&gt;.MutationResult"/> struct.
            /// </summary>
            /// <param name="unchangedInput">The unchanged input.</param>
            internal MutationResult(MutationInput unchangedInput)
            {
                this.root = unchangedInput.Root;
                this.countAdjustment = 0;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableDictionary&lt;TKey, TValue&gt;.MutationResult"/> struct.
            /// </summary>
            /// <param name="root">The root.</param>
            /// <param name="countAdjustment">The count adjustment.</param>
            internal MutationResult(ImmutableSortedDictionary<int, HashBucket>.Node root, int countAdjustment)
            {
                Requires.NotNull(root, "root");
                this.root = root;
                this.countAdjustment = countAdjustment;
            }

            /// <summary>
            /// Gets the root node of the data structure after the mutation.
            /// </summary>
            internal ImmutableSortedDictionary<int, HashBucket>.Node Root
            {
                get { return this.root; }
            }

            /// <summary>
            /// Gets the number of elements added or removed from the collection as a result of the operation (a negative number represents removed elements).
            /// </summary>
            internal int CountAdjustment
            {
                get { return this.countAdjustment; }
            }

            /// <summary>
            /// Returns an immutable dictionary that captures the result of this mutation.
            /// </summary>
            /// <param name="priorMap">The prior version of the map.  Used to capture the equality comparer and previous count, when applicable.</param>
            /// <returns>The new collection.</returns>
            internal ImmutableDictionary<TKey, TValue> Finalize(ImmutableDictionary<TKey, TValue> priorMap)
            {
                Requires.NotNull(priorMap, "priorMap");
                return priorMap.Wrap(this.Root, priorMap.count + this.CountAdjustment);
            }
        }
    }
}
