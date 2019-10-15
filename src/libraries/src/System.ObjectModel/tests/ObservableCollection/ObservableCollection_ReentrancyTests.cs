// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    public partial class ObservableCollectionTests
    {
        [Fact]
        public void Reentrancy_SingleListener_DoesNotThrow()
        {
            bool handlerCalled = false;

            var collection = new ObservableCollection<int>();
            collection.CollectionChanged += (sender, e) =>
            {
                if (!handlerCalled)
                {
                    handlerCalled = true;

                    // Single listener; does not throw.
                    collection.Add(2);
                }
            };
            collection.Add(1);

            Assert.True(handlerCalled);
            Assert.Equal(2, collection.Count);
            Assert.Equal(1, collection[0]);
            Assert.Equal(2, collection[1]);
        }

        [Fact]
        public void Reentrancy_MultipleListeners_Throws()
        {
            bool handler1Called = false;
            bool handler2Called = false;

            var collection = new ObservableCollection<int>();
            collection.CollectionChanged += (sender, e) => { handler1Called = true; };
            collection.CollectionChanged += (sender, e) =>
            {
                handler2Called = true;

                // More than one listener; throws.
                Assert.Throws<InvalidOperationException>(() => collection.Add(2));
            };
            collection.Add(1);

            Assert.True(handler1Called);
            Assert.True(handler2Called);
            Assert.Equal(1, collection.Count);
            Assert.Equal(1, collection[0]);
        }

        [Fact]
        public void BlockReentrancy()
        {
            var collection = new ObservableCollectionSubclass<int>();
            Assert.NotNull(collection.BlockReentrancy());
            Assert.Same(collection.BlockReentrancy(), collection.BlockReentrancy());
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, true)]
        [InlineData(3, true)]
        public void CheckReentrancy(int listenerCount, bool shouldThrow)
        {
            var collection = new ObservableCollectionSubclass<int>();
            for (int i = 0; i < listenerCount; i++)
            {
                collection.CollectionChanged += (sender, e) => { };
            }

            collection.CheckReentrancy();
            using (collection.BlockReentrancy())
            {
                if (shouldThrow)
                {
                    Assert.Throws<InvalidOperationException>(() => collection.CheckReentrancy());
                }
                else
                {
                    collection.CheckReentrancy();
                }
            }
            collection.CheckReentrancy();
        }

        [Fact]
        public void CheckReentrancy_MultipleListeners_MultipleBlocks()
        {
            var collection = new ObservableCollectionSubclass<int>();
            collection.CollectionChanged += (sender, e) => { };
            collection.CollectionChanged += (sender, e) => { };

            collection.CheckReentrancy();

            IDisposable block1 = collection.BlockReentrancy();
            Assert.Throws<InvalidOperationException>(() => collection.CheckReentrancy());

            IDisposable block2 = collection.BlockReentrancy();
            Assert.Throws<InvalidOperationException>(() => collection.CheckReentrancy());

            block1.Dispose();
            Assert.Throws<InvalidOperationException>(() => collection.CheckReentrancy());

            block2.Dispose();
            collection.CheckReentrancy();

            collection.CheckReentrancy();
        }

        public class ObservableCollectionSubclass<T> : ObservableCollection<T>
        {
            public new IDisposable BlockReentrancy() => base.BlockReentrancy();

            public new void CheckReentrancy() => base.CheckReentrancy();
        }
    }
}
