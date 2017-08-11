// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.WindowsRuntime.Tests
{
    public class WindowsRuntimeMarshalTests
    {
        [Fact]
        public void AddEventHandler_RemoveMethodHasTarget_Success()
        {
            bool addMethodCalled = false;
            bool removeMethodCalled = false;
            Delegate handler = new EventHandler(EventHandlerMethod1);

            Func<Delegate, EventRegistrationToken> addMethod = eventHandler =>
            {
                Assert.False(addMethodCalled);
                addMethodCalled = true;
                Assert.Same(handler, eventHandler);

                return new EventRegistrationToken();
            };

            WindowsRuntimeMarshal.AddEventHandler(addMethod, token => removeMethodCalled = true, handler);
            Assert.True(addMethodCalled);
            Assert.False(removeMethodCalled);
        }

        [Fact]
        public void AddEventHandler_RemoveMethodHasNoTarget_Success()
        {
            bool removeMethodCalled = false;
            ExpectedHandler = new EventHandler(EventHandlerMethod1);

            WindowsRuntimeMarshal.AddEventHandler(AddMethod, token => removeMethodCalled = true, ExpectedHandler);
            Assert.True(AddMethodCalled);
            Assert.False(removeMethodCalled);
        }

        private static bool AddMethodCalled { get; set; }
        private static Delegate ExpectedHandler { get; set; }

        private static EventRegistrationToken AddMethod(Delegate eventHandler)
        {
            Assert.False(AddMethodCalled);
            AddMethodCalled = true;
            Assert.Same(ExpectedHandler, eventHandler);

            return new EventRegistrationToken();
        }

        [Fact]
        public void AddEventHandler_NullHandler_Nop()
        {
            bool addMethodCalled = false;
            bool removeMethodCalled = false;
            WindowsRuntimeMarshal.AddEventHandler<Delegate>(eventHandler => {
                addMethodCalled = true;
                return new EventRegistrationToken();
            }, token => removeMethodCalled = true, null);

            Assert.False(addMethodCalled);
            Assert.False(removeMethodCalled);
        }

        [Fact]
        public void AddEventHandler_NullAddMethod_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("addMethod", () => WindowsRuntimeMarshal.AddEventHandler(null, token => { }, new EventHandler(EventHandlerMethod1)));
        }

        [Fact]
        public void AddEventHandler_NullRemoveMethod_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("removeMethod", () => WindowsRuntimeMarshal.AddEventHandler(eventHandler => new EventRegistrationToken(), null, new EventHandler(EventHandlerMethod1)));
        }

        [Fact]
        public void RemoveEventHandler_RemoveMethodHasTarget_Success()
        {
            bool removeMethodCalled = false;
            Delegate handler = new EventHandler(EventHandlerMethod1);

            var tokenTable = new EventRegistrationTokenTable<Delegate>();
            EventRegistrationToken addToken = tokenTable.AddEventHandler(handler);

            Action<EventRegistrationToken> removeMethod = token =>
            {
                Assert.False(removeMethodCalled);
                removeMethodCalled = true;
                Assert.Equal(addToken, token);
            };
            WindowsRuntimeMarshal.AddEventHandler(eventHandler => addToken, removeMethod, handler);

            // Removing with the same handler but with a different method is a nop.
            WindowsRuntimeMarshal.RemoveEventHandler(token => removeMethodCalled = true, handler);
            Assert.False(removeMethodCalled);

            WindowsRuntimeMarshal.RemoveEventHandler(DifferentRemoveMethod, handler);
            Assert.False(removeMethodCalled);

            // Removing with a different handler but with the same method is a nop.
            WindowsRuntimeMarshal.RemoveEventHandler(removeMethod, new EventHandler(EventHandlerMethod2));
            Assert.False(removeMethodCalled);

            // Removing the same handler and the same method works.
            WindowsRuntimeMarshal.RemoveEventHandler(removeMethod, handler);
            Assert.True(removeMethodCalled);
        }

        [Fact]
        public void RemoveEventHandler_RemoveMethodHasNoTarget_Success()
        {
            Delegate handler = new EventHandler(EventHandlerMethod1);

            var tokenTable = new EventRegistrationTokenTable<Delegate>();
            ExpectedRemoveToken = tokenTable.AddEventHandler(handler);

            Action<EventRegistrationToken> removeMethod = RemoveMethod;
            WindowsRuntimeMarshal.AddEventHandler(eventHandler => ExpectedRemoveToken, removeMethod, handler);

            // Removing with the same handler but with a different method is a nop.
            WindowsRuntimeMarshal.RemoveEventHandler(token => RemoveMethodCalled = true, handler);
            Assert.False(RemoveMethodCalled);

            WindowsRuntimeMarshal.RemoveEventHandler(DifferentRemoveMethod, handler);
            Assert.False(RemoveMethodCalled);

            // Removing with a different handler but with the same method is a nop.
            WindowsRuntimeMarshal.RemoveEventHandler(removeMethod, new EventHandler(EventHandlerMethod2));
            Assert.False(RemoveMethodCalled);

            // Removing the same handler and the same method works.
            WindowsRuntimeMarshal.RemoveEventHandler(removeMethod, handler);
            Assert.True(RemoveMethodCalled);
        }

        private static EventRegistrationToken ExpectedRemoveToken { get; set; }
        private static bool RemoveMethodCalled { get; set; }

        private static void RemoveMethod(EventRegistrationToken token)
        {
            Assert.False(RemoveMethodCalled);
            RemoveMethodCalled = true;
            Assert.Equal(ExpectedRemoveToken, token);
        }

        private static void DifferentRemoveMethod(EventRegistrationToken token) => RemoveMethodCalled = true;

        [Fact]
        public void RemoveEventHandler_NullHandler_Nop()
        {
            bool removeMethodCalled = false;
            Delegate handler = new EventHandler(EventHandlerMethod1);

            var tokenTable = new EventRegistrationTokenTable<Delegate>();
            EventRegistrationToken addToken = tokenTable.AddEventHandler(handler);

            Action<EventRegistrationToken> removeMethod = token => removeMethodCalled = true;
            WindowsRuntimeMarshal.AddEventHandler(eventHandler => addToken, removeMethod, handler);

            WindowsRuntimeMarshal.RemoveEventHandler<Delegate>(removeMethod, null);
            Assert.False(removeMethodCalled);
        }

        [Fact]
        public void RemoveEventHandler_NullRemoveMethod_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("removeMethod", () => WindowsRuntimeMarshal.RemoveEventHandler<Delegate>(null, new EventHandler(EventHandlerMethod1)));
        }

        [Fact]
        public void RemoveAllEventHandlers_RemoveMethodHasTarget_Success()
        {
            Delegate handler = new EventHandler(EventHandlerMethod1);

            var tokenTable = new EventRegistrationTokenTable<Delegate>();
            EventRegistrationToken token1 = tokenTable.AddEventHandler(handler);
            EventRegistrationToken token2 = tokenTable.AddEventHandler(handler);
            EventRegistrationToken token3 = tokenTable.AddEventHandler(handler);

            var removedTokens = new List<EventRegistrationToken>();
            Action<EventRegistrationToken> removeMethod = token => removedTokens.Add(token);

            WindowsRuntimeMarshal.AddEventHandler(eventHandler => token1, removeMethod, handler);
            WindowsRuntimeMarshal.AddEventHandler(eventHandler => token2, removeMethod, handler);

            bool removeMethodWithTargetCalled = false;
            WindowsRuntimeMarshal.AddEventHandler(eventHandler => token3, token => removeMethodWithTargetCalled = false, handler);

            // Removing with the same handler but with a different method is a nop.
            WindowsRuntimeMarshal.RemoveAllEventHandlers(token => RemoveMethodCalled = true);
            Assert.Empty(removedTokens);
            Assert.False(DifferentRemoveAllMethodCalled);
            Assert.False(removeMethodWithTargetCalled);

            WindowsRuntimeMarshal.RemoveAllEventHandlers(DifferentRemoveAllMethod);
            Assert.Empty(removedTokens);
            Assert.False(DifferentRemoveAllMethodCalled);
            Assert.False(removeMethodWithTargetCalled);

            // Removing the same handler and the same method works.
            WindowsRuntimeMarshal.RemoveAllEventHandlers(removeMethod);
            Assert.Equal(new EventRegistrationToken[] { token1, token2 }, removedTokens);
            Assert.False(DifferentRemoveAllMethodCalled);
            Assert.False(removeMethodWithTargetCalled);
        }

        [Fact]
        public void RemoveAllEventHandlers_RemoveMethodHasNoTarget_Success()
        {
            Delegate handler = new EventHandler(EventHandlerMethod1);

            var tokenTable = new EventRegistrationTokenTable<Delegate>();
            EventRegistrationToken token1 = tokenTable.AddEventHandler(handler);
            EventRegistrationToken token2 = tokenTable.AddEventHandler(handler);
            EventRegistrationToken token3 = tokenTable.AddEventHandler(handler);

            Action<EventRegistrationToken> removeMethod = RemoveAllMethod;
            WindowsRuntimeMarshal.AddEventHandler(eventHandler => token1, removeMethod, handler);
            WindowsRuntimeMarshal.AddEventHandler(eventHandler => token2, removeMethod, handler);

            bool removeMethodWithTargetCalled = false;
            WindowsRuntimeMarshal.AddEventHandler(eventHandler => token3, token => removeMethodWithTargetCalled = false, handler);

            // Removing with the same handler but with a different method is a nop.
            WindowsRuntimeMarshal.RemoveAllEventHandlers(token => RemoveMethodCalled = true);
            Assert.Empty(RemoveAllTokens);
            Assert.False(DifferentRemoveAllMethodCalled);
            Assert.False(removeMethodWithTargetCalled);

            WindowsRuntimeMarshal.RemoveAllEventHandlers(DifferentRemoveAllMethod);
            Assert.Empty(RemoveAllTokens);
            Assert.False(DifferentRemoveAllMethodCalled);
            Assert.False(removeMethodWithTargetCalled);

            // Removing the same handler and the same method works.
            WindowsRuntimeMarshal.RemoveAllEventHandlers(removeMethod);
            Assert.Equal(new EventRegistrationToken[] { token1, token2 }, RemoveAllTokens);
            Assert.False(DifferentRemoveAllMethodCalled);
            Assert.False(removeMethodWithTargetCalled);
        }

        private static List<EventRegistrationToken> RemoveAllTokens { get; } = new List<EventRegistrationToken>();
        private static void RemoveAllMethod(EventRegistrationToken token) => RemoveAllTokens.Add(token);

        private static bool DifferentRemoveAllMethodCalled { get; set; }
        private static void DifferentRemoveAllMethod(EventRegistrationToken token) => DifferentRemoveAllMethodCalled = true;

        [Fact]
        public void RemoveAllEventHandlers_NullRemoveMethod_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("removeMethod", () => WindowsRuntimeMarshal.RemoveAllEventHandlers(null));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWinRTSupported))]
        [InlineData("")]
        [InlineData("HString")]
        public void StringToHString_PtrToHString_ReturnsExpected(string s)
        {
            IntPtr ptr = WindowsRuntimeMarshal.StringToHString(s);
            try
            {
                if (s.Length == 0)
                {
                    Assert.Equal(IntPtr.Zero, ptr);
                }
                else
                {
                    Assert.NotEqual(IntPtr.Zero, ptr);
                }
                Assert.Equal(s, WindowsRuntimeMarshal.PtrToStringHString(ptr));
            }
            finally
            {
                WindowsRuntimeMarshal.FreeHString(ptr);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWinRTSupported))]
        public void StringToHString_NullString_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => WindowsRuntimeMarshal.StringToHString(null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWinRTSupported))]
        public void StringToHString_WinRTNotSupported_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => WindowsRuntimeMarshal.StringToHString(null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWinRTSupported))]
        public void PtrToStringHString_WinRTNotSupported_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => WindowsRuntimeMarshal.PtrToStringHString(IntPtr.Zero));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWinRTSupported))]
        public void FreeHString_WinRTNotSupported_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => WindowsRuntimeMarshal.FreeHString(IntPtr.Zero));
        }

        [Fact]
        public void GetActivationFactory_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => WindowsRuntimeMarshal.GetActivationFactory(null));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "No reliable way to check if a type is WinRT in AOT")]
        public void GetActivationFactory_NotExportedType_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => WindowsRuntimeMarshal.GetActivationFactory(typeof(int)));
        }

        private static void EventHandlerMethod1(object sender, EventArgs e) { }
        private static void EventHandlerMethod2(object sender, EventArgs e) { }
    }
}
