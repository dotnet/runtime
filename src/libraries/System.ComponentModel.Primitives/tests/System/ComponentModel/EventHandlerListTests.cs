// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class EventHandlerListTests
    {
        [Fact]
        public void AddHandler_RemoveHandler_Roundtrips()
        {
            var list = new EventHandlerList();

            Action action1 = () => Assert.Fail();
            Action action2 = () => Assert.Fail();

            // Add the first delegate to the first entry.
            list.AddHandler("key1", action1);
            Assert.Same(action1, list["key1"]);
            Assert.Null(list["key2"]);

            // Add the second delegate to the second entry.
            list.AddHandler("key2", action2);
            Assert.Same(action1, list["key1"]);
            Assert.Same(action2, list["key2"]);

            // Then remove the first delegate.
            list.RemoveHandler("key1", action1);
            Assert.Null(list["key1"]);
            Assert.Same(action2, list["key2"]);

            // And remove the second delegate.
            list.RemoveHandler("key2", action2);
            Assert.Null(list["key1"]);
            Assert.Null(list["key2"]);
        }

        [Fact]
        public void AddHandler_SameKey_CombinesDelegates()
        {
            var list = new EventHandlerList();

            // Create two delegates that will increase total by different amounts.
            int total = 0;
            Action a1 = () => total += 1;
            Action a2 = () => total += 2;

            // Add both delegates for the same key and make sure we get them both out of the indexer.
            list.AddHandler("key1", a1);
            list.AddHandler("key1", a2);
            list["key1"].DynamicInvoke();
            Assert.Equal(3, total);

            // Remove the first delegate and make sure the second delegate can still be retrieved.
            list.RemoveHandler("key1", a1);
            list["key1"].DynamicInvoke();
            Assert.Equal(5, total);

            // Remove a delegate that was never in the list; nop.
            list.RemoveHandler("key1", new Action(() => { }));
            list["key1"].DynamicInvoke();
            Assert.Equal(7, total);

            // Then remove the second delegate.
            list.RemoveHandler("key1", a2);
            Assert.Null(list["key1"]);
        }

        [Fact]
        public void AddHandlers_ValidList_AddsDelegates()
        {
            var list1 = new EventHandlerList();
            var list2 = new EventHandlerList();

            int total = 0;
            Action a1 = () => total += 1;
            Action a2 = () => total += 2;

            // Add the delegates to separate keys in the first list
            list1.AddHandler("key1", a1);
            list1.AddHandler("key2", a2);

            // Then add the first list to the second
            list2.AddHandlers(list1);

            // And make sure they contain the same entries
            Assert.Same(list1["key1"], list2["key1"]);
            Assert.Same(list1["key2"], list2["key2"]);
        }

        [Fact]
        public void AddHandlers_NullList_ThrowsArgumentNullException()
        {
            var list = new EventHandlerList();
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>(() => list.AddHandlers(null));
        }

        [Fact]
        public void Dispose_ClearsList()
        {
            var list = new EventHandlerList();

            // Create two different delegate instances.
            Action action1 = () => Assert.Fail();
            Action action2 = () => Assert.Fail();

            // The list is still usable after disposal.
            for (int i = 0; i < 2; i++)
            {
                // Add the delegates
                list.AddHandler("key1", action1);
                list.AddHandler("key2", action2);
                Assert.Same(action1, list["key1"]);
                Assert.Same(action2, list["key2"]);

                // Dispose to clear the list.
                list.Dispose();
                Assert.Null(list["key1"]);
                Assert.Null(list["key2"]);
            }
        }

        [Fact]
        public void Setter_AddsOrOverwrites()
        {
            var list = new EventHandlerList();

            int total = 0;
            Action a1 = () => total += 1;
            Action a2 = () => total += 2;

            list["key1"] = a1;
            Assert.Same(a1, list["key1"]);

            list["key2"] = a2;
            Assert.Same(a2, list["key2"]);

            list["key2"] = a1;
            Assert.Same(a1, list["key1"]);
        }

        [Fact]
        public void RemoveHandler_EmptyList_Nop()
        {
            var list = new EventHandlerList();
            list.RemoveHandler("key1", new Action(() => { }));
            list.RemoveHandler("key1", null);
            list.RemoveHandler(null, null);
        }

        [Fact]
        public void Indexer_SetNullKey_Nop()
        {
            var list = new EventHandlerList();

            Action action = () => { };
            list[null] = action;
            Assert.Same(action, list[null]);
        }

        [Fact]
        public void Indexer_SetNullValue_Nop()
        {
            var list = new EventHandlerList();

            list["key1"] = null;
            Assert.Null(list["key1"]);
        }

        [Fact]
        public void Indexer_GetNoSuchKey_ReturnsNull()
        {
            var list = new EventHandlerList();
            Assert.Null(list["key"]);
            Assert.Null(list[null]);
        }
    }
}
