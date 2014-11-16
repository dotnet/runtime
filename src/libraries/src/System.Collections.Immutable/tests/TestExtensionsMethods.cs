// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Validation;
using Xunit;

namespace System.Collections.Immutable.Test
{
    internal static class TestExtensionsMethods
    {
        private static readonly double GoldenRatio = (1 + Math.Sqrt(5)) / 2;

        internal static IDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(this IImmutableDictionary<TKey, TValue> dictionary)
        {
            Requires.NotNull(dictionary, "dictionary");

            return (IDictionary<TKey, TValue>)dictionary;
        }

        internal static IDictionary<TKey, TValue> ToBuilder<TKey, TValue>(this IImmutableDictionary<TKey, TValue> dictionary)
        {
            Requires.NotNull(dictionary, "dictionary");

            var hashDictionary = dictionary as ImmutableDictionary<TKey, TValue>;
            if (hashDictionary != null)
            {
                return hashDictionary.ToBuilder();
            }

            var sortedDictionary = dictionary as ImmutableSortedDictionary<TKey, TValue>;
            if (sortedDictionary != null)
            {
                return sortedDictionary.ToBuilder();
            }

            throw new NotSupportedException();
        }

        internal static void VerifyBalanced<T>(this IBinaryTree<T> node)
        {
            if (node.Count <= 2)
            {
                return;
            }

            VerifyBalanced(node.Left);
            VerifyBalanced(node.Right);

            Assert.InRange(node.Left.Height - node.Right.Height, -1, 1);
        }

        internal static void VerifyHeightIsWithinTolerance<T>(this IBinaryTree<T> node)
        {
            // http://en.wikipedia.org/wiki/AVL_tree
            double heightMustBeLessThan = Math.Log(2, GoldenRatio) * Math.Log(Math.Sqrt(5) * (node.Count + 2), 2) - 2;
            Assert.True(node.Height < heightMustBeLessThan);
        }
    }
}
