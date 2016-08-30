// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class EventHandlerListTests
    {
        [Fact]
        public void AddHandler_Getter_RemoveHandler_Getter_Roundtrips()
        {
            var list = new EventHandlerList();

            // Create two different delegate instances
            Action a1 = () => Assert.True(false);
            Action a2 = () => Assert.False(true);
            Assert.NotSame(a1, a2);

            // Neither entry in the list has a delegate
            Assert.Null(list["key1"]);
            Assert.Null(list["key2"]);

            // Add the first delegate to the first entry
            list.AddHandler("key1", a1);
            Assert.Same(a1, list["key1"]);
            Assert.Null(list["key2"]);

            // Add the second delegate to the second entry
            list.AddHandler("key2", a2);
            Assert.Same(a1, list["key1"]);
            Assert.Same(a2, list["key2"]);

            // Then remove the first delegate
            list.RemoveHandler("key1", a1);
            Assert.Null(list["key1"]);
            Assert.Same(a2, list["key2"]);

            // And remove the second delegate
            list.RemoveHandler("key2", a2);
            Assert.Null(list["key1"]);
            Assert.Null(list["key2"]);
        }

        [Fact]
        public void AddHandler_MultipleInSameKey_Getter_CombinedDelegates()
        {
            var list = new EventHandlerList();

            // Create two delegates that will increase total by different amounts
            int total = 0;
            Action a1 = () => total += 1;
            Action a2 = () => total += 2;

            // Add both delegates for the same key and make sure we get them both out of the indexer
            list.AddHandler("key1", a1);
            list.AddHandler("key1", a2);
            list["key1"].DynamicInvoke();
            Assert.Equal(3, total);

            // Remove the first delegate and make sure the second delegate can still be retrieved
            list.RemoveHandler("key1", a1);
            list["key1"].DynamicInvoke();
            Assert.Equal(5, total);

            // Remove a delegate that was never in the list; nop
            list.RemoveHandler("key1", new Action(() => { }));
            list["key1"].DynamicInvoke();
            Assert.Equal(7, total);

            // Then remove the second delegate
            list.RemoveHandler("key1", a2);
            Assert.Null(list["key1"]);
        }

        [Fact]
        public void AddHandlers_Gettable()
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
        public void Dispose_ClearsList()
        {
            var list = new EventHandlerList();

            // Create two different delegate instances
            Action a1 = () => Assert.True(false);
            Action a2 = () => Assert.False(true);
            Assert.NotSame(a1, a2);

            // Neither entry in the list has a delegate
            Assert.Null(list["key1"]);
            Assert.Null(list["key2"]);

            for (int i = 0; i < 2; i++)
            {
                // Add the delegates
                list.AddHandler("key1", a1);
                list.AddHandler("key2", a2);
                Assert.Same(a1, list["key1"]);
                Assert.Same(a2, list["key2"]);

                // Dispose to clear the list
                list.Dispose();
                Assert.Null(list["key1"]);
                Assert.Null(list["key2"]);

                // List is still usable, though, so loop around to do it again
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
            list.RemoveHandler("key1", new Action(() => { })); // no error
        }

        [Fact]
        public void NullKey_Valid()
        {
            var list = new EventHandlerList();

            int total = 0;
            Action a1 = () => total += 1;

            list[null] = a1;
            Assert.Same(a1, list[null]);
        }

        [Fact]
        public void NullValue_Nop()
        {
            var list = new EventHandlerList();

            list["key1"] = null;
            Assert.Null(list["key1"]);
        }
    }
}
