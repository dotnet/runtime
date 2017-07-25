// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class IndexingTests
    {
        private class AllTheIntegers
        {
            [IndexerName("Integers")]
            public int this[int x] => x;
        }

        [DefaultMember("Indexer")]
        private class DefaultDoesNotExist
        {
        }

        private interface IA
        {
            [IndexerName("NumberString")]
            string this[int key] { get; }
        }

        private interface IB : IA
        {
        }

        private interface IC : IB
        {
        }

        private class Implementation : IC
        {
            public string this[int key] => key.ToString();
        }

        [Fact]
        public void CustomIndexerName()
        {
            dynamic d = new AllTheIntegers();
            int answer = d[42];
            Assert.Equal(42, answer);
        }

        [Fact]
        public void CustomIndexerNameDynamicArgument()
        {
            AllTheIntegers all = new AllTheIntegers();
            dynamic d = 3;
            int answer = all[d];
            Assert.Equal(3, answer);
        }

        [Fact]
        public void TargetClassClaimsNonExistentMemberIsIndexer()
        {
            dynamic d = new DefaultDoesNotExist();
            var ex = Assert.Throws<RuntimeBinderException>(() => d[23]);
            Assert.Contains("[]", ex.Message); // true of the correct message in all translations.
            Assert.Contains(typeof(DefaultDoesNotExist).FullName.Replace('+', '.'), ex.Message);
        }

        [Fact]
        public void DeepInheritingIndexingInterface()
        {
            IC ifaceTyped = new Implementation();
            dynamic d = 23;
            string answer = ifaceTyped[d];
            Assert.Equal("23", answer);
        }
    }
}
