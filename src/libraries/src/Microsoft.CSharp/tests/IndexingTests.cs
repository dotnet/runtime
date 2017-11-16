// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
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

        [Fact]
        public void ArrayLongIndexed()
        {
            dynamic arr = new[] {1, 2, 3};
            dynamic ind = 2L;
            Assert.Equal(3, arr[ind]);
        }

        [Fact]
        public void BadArrayIndexer()
        {
            dynamic arr = new[] {1, 2, 3};
            dynamic ind = "a";
            Assert.Throws<RuntimeBinderException>(() => arr[ind]);
        }

        [Fact]
        public void BadArrayIndexerCouldHaveCast()
        {
            dynamic arr = new[] { 1, 2, 3 };
            dynamic ind = 2m;
            Assert.Throws<RuntimeBinderException>(() => arr[ind]);
        }

        // Only gives results once.
        private class ArgumentEnumerable : IEnumerable<CSharpArgumentInfo>
        {
            private int _count;

            public ArgumentEnumerable(int count)
            {
                _count = count;
            }

            public IEnumerator<CSharpArgumentInfo> GetEnumerator()
            {
                while (_count > 0)
                {
                    yield return CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null);
                    --_count;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Fact]
        public void GetIndexWithNonRepeatingArgumentInfos()
        {
            CallSiteBinder binder = Binder.GetIndex(CSharpBinderFlags.None, GetType(), new ArgumentEnumerable(2));
            CallSite<Func<CallSite, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object>>.Create(binder);
            Func<CallSite, object, object, object> targ = callSite.Target;
            object result = targ(callSite, new[] {1, 2, 3}, 1);
            Assert.Equal(2, result);
        }

        [Fact]
        public void SetIndexWithNonRepeatingArgumentInfos()
        {
            CallSiteBinder binder = Binder.SetIndex(CSharpBinderFlags.None, GetType(), new ArgumentEnumerable(3));
            CallSite<Func<CallSite, object, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object, object>>.Create(binder);
            Func<CallSite, object, object, object, object> targ = callSite.Target;
            int[] array = {1, 2, 3};
            object result = targ(callSite, array, 1, 9);
            Assert.Equal(9, result);
            Assert.Equal(9, array[1]);
        }
    }
}
