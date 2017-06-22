// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.WindowsRuntime.Tests
{
    public class EventRegistrationTokenTableTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var tokenTable = new EventRegistrationTokenTable<Delegate>();
            Assert.Null(tokenTable.InvocationList);
        }

        [Fact]
        public void Ctor_NonDelegateType_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => new EventRegistrationTokenTable<string>());
        }

        [Fact]
        public void AddEventHandler_SingleInvocationList_AddsSingleDelegateToInvocationList()
        {
            EventHandler handler = new EventHandler(EventHandlerMethod1);
            var tokenTable = new EventRegistrationTokenTable<Delegate>();

            EventRegistrationToken token = tokenTable.AddEventHandler(handler);
            Assert.NotEqual(0, token.GetHashCode());
            Assert.Equal(new Delegate[] { handler }, tokenTable.InvocationList.GetInvocationList());
        }

        [Fact]
        public void AddEventHandler_MultipleInvocationList_AddsAllDelegateToInvocationLists()
        {
            EventHandler handler1 = new EventHandler(EventHandlerMethod1);
            EventHandler handler2 = new EventHandler(EventHandlerMethod2);
            EventHandler combinedHandler = (EventHandler)Delegate.Combine(handler1, handler2);
            
            var tokenTable = new EventRegistrationTokenTable<Delegate>();

            EventRegistrationToken token = tokenTable.AddEventHandler(combinedHandler);
            Assert.NotEqual(0, token.GetHashCode());
            Assert.Equal(new Delegate[] { handler1, handler2 }, tokenTable.InvocationList.GetInvocationList());
        }

        [Fact]
        public void AddEventHandler_MultipleTimes_AddsEachDelegateToInvocationList()
        {
            EventHandler handler = new EventHandler(EventHandlerMethod1);
            var tokenTable = new EventRegistrationTokenTable<Delegate>();

            EventRegistrationToken token1 = tokenTable.AddEventHandler(handler);
            Assert.NotEqual(0, token1.GetHashCode());

            EventRegistrationToken token2 = tokenTable.AddEventHandler(handler);
            Assert.NotEqual(token1.GetHashCode(), token2.GetHashCode());

            Assert.Equal(new Delegate[] { handler, handler }, tokenTable.InvocationList.GetInvocationList());
        }

        [Fact]
        public void AddEventHandler_Null_ReturnsZeroToken()
        {
            var tokenTable = new EventRegistrationTokenTable<Delegate>();
            EventRegistrationToken token = tokenTable.AddEventHandler(null);
            Assert.Equal(0, token.GetHashCode());

            // Removing this token should be a nop.
            tokenTable.RemoveEventHandler(token);
        }

        [Fact]
        public void RemoveEventHandler_Token_RemovesFromTable()
        {
            EventHandler handler = new EventHandler(EventHandlerMethod1);
            var tokenTable = new EventRegistrationTokenTable<Delegate>();
            EventRegistrationToken token = tokenTable.AddEventHandler(handler);

            tokenTable.RemoveEventHandler(token);
            Assert.Null(tokenTable.InvocationList);

            // Calls to RemoveEventHandler after removal are nops.
            tokenTable.RemoveEventHandler(token);
        }

        [Fact]
        public void RemoveEventHandler_Delegate_RemovesFromTable()
        {
            EventHandler handler1 = new EventHandler(EventHandlerMethod1);
            EventHandler handler2 = new EventHandler(EventHandlerMethod2);
            EventHandler combinedHandler = (EventHandler)Delegate.Combine(handler1, handler2);

            var tokenTable = new EventRegistrationTokenTable<Delegate>();
            tokenTable.AddEventHandler(combinedHandler);

            tokenTable.RemoveEventHandler(handler1);
            tokenTable.RemoveEventHandler(handler2);
            Assert.Equal(new Delegate[] { handler1, handler2 }, tokenTable.InvocationList.GetInvocationList());

            tokenTable.RemoveEventHandler(combinedHandler);
            Assert.Null(tokenTable.InvocationList);

            // Calls to RemoveEventHandler after removal are nops.
            tokenTable.RemoveEventHandler(combinedHandler);
        }

        [Fact]
        public void RemoveEventHandler_MultipleTimes_RemovesSingleDelegateFromTable()
        {
            EventHandler handler = new EventHandler(EventHandlerMethod1);
            var tokenTable = new EventRegistrationTokenTable<Delegate>();

            tokenTable.AddEventHandler(handler);
            tokenTable.AddEventHandler(handler);

            tokenTable.RemoveEventHandler(handler);
            Assert.Equal(new Delegate[] { handler }, tokenTable.InvocationList.GetInvocationList());
 
            tokenTable.RemoveEventHandler(handler);
            Assert.Null(tokenTable.InvocationList);
        }

        [Fact]
        public void RemoveEventHandler_NullDelegate_Nop()
        {
            var tokenTable = new EventRegistrationTokenTable<Delegate>();
            tokenTable.RemoveEventHandler(null);
        }

        [Fact]
        public void GetOrCreateEventRegistrationTokenTable_NullTable_ReturnsNewTable()
        {
            EventRegistrationTokenTable<Delegate> tokenTable = null;
            EventRegistrationTokenTable<Delegate> result = EventRegistrationTokenTable<Delegate>.GetOrCreateEventRegistrationTokenTable(ref tokenTable);
            Assert.Null(result.InvocationList);
        }

        [Fact]
        public void GetOrCreateEventRegistrationTokenTable_NonNullTable_ReturnsEventTable()
        {
            var tokenTable = new EventRegistrationTokenTable<Delegate>();
            EventRegistrationTokenTable<Delegate> result = EventRegistrationTokenTable<Delegate>.GetOrCreateEventRegistrationTokenTable(ref tokenTable);
            Assert.Same(tokenTable, result);
        }

        [Fact]
        public void GetOrCreateEventRegistrationTokenTable_NonDelegateType_ThrowsInvalidOperationException()
        {
            EventRegistrationTokenTable<string> tokenTable = null;
            Assert.Throws<InvalidOperationException>(() => EventRegistrationTokenTable<string>.GetOrCreateEventRegistrationTokenTable(ref tokenTable));
        }

        [Fact]
        public void InvocationList_SetNull_GetReturnsNull()
        {
            var tokenTable = new EventRegistrationTokenTable<Delegate>();
            tokenTable.AddEventHandler(new EventHandler(EventHandlerMethod1));

            tokenTable.InvocationList = null;
            Assert.Null(tokenTable.InvocationList);
        }

        [Fact]
        public void InvocationList_SetDelegate_GetReturnsExpected()
        {
            Delegate invocationList = new EventHandler(EventHandlerMethod1);
            var tokenTable = new EventRegistrationTokenTable<Delegate>() { InvocationList = invocationList };
            
            Assert.Equal(new Delegate[] { invocationList }, tokenTable.InvocationList.GetInvocationList());
        }

        private static void EventHandlerMethod1(object sender, EventArgs e) { }
        private static void EventHandlerMethod2(object sender, EventArgs e) { }
    }
}
