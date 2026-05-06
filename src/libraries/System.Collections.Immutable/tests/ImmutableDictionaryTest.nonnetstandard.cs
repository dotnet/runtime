// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public partial class ImmutableDictionaryTest : ImmutableDictionaryTestBase
    {
        [Fact]
        public override void EmptyTest()
        {
            base.EmptyTest();
            this.EmptyTestHelperHash(Empty<int, bool>(), 5);
        }

        [Fact]
        public void EnumeratorWithHashCollisionsTest()
        {
            ImmutableDictionary<int, GenericParameterHelper> emptyMap = Empty<int, GenericParameterHelper>(new BadHasher<int>());
            this.EnumeratorTestHelper(emptyMap);
        }

        internal override BinaryTreeProxy GetRootNode<TKey, TValue>(IImmutableDictionary<TKey, TValue> dictionary)
        {
            return ((ImmutableDictionary<TKey, TValue>)dictionary).GetBinaryTreeProxy();
        }

        private void EmptyTestHelperHash<TKey, TValue>(IImmutableDictionary<TKey, TValue> empty, TKey someKey)
        {
            Assert.Same(EqualityComparer<TKey>.Default, ((ImmutableDictionary<TKey, TValue>)empty).KeyComparer);
        }
    }
}
