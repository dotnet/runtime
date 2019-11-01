// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class IsEventTests
    {
        public class TypeWithEvents
        {
            public event Action<int> Event;

            public void Raise(int eventArg) => Event?.Invoke(eventArg);

            public int NonEventField;

            public int NonEventProperty { get; set; }
        }

        private class PrivateTypeWithEvent
        {
#pragma warning disable 67
            public event Action<int> Event;
#pragma warning restore 67
        }

        [Fact]
        public void BindToEvent()
        {
            dynamic d = new TypeWithEvents();
            int output = 0;
            d.Event += (Action<int>)(i =>
            {
                output = i;
            });
            d.Raise(49);
            Assert.Equal(49, output);
        }

        [Fact]
        public void RemoveHandler()
        {
            TypeWithEvents with = new TypeWithEvents();
            int output = 0;
            Action<int> setOutput = i =>
            {
                output = i;
            };
            with.Event += setOutput;
            with.Raise(1);
            dynamic d = with;
            d.Event -= setOutput;
            with.Raise(2);
            Assert.Equal(1, output);
            d.Raise(3);
            Assert.Equal(1, output);
        }

        [Fact]
        public void BindToNonExistent()
        {
            dynamic d = new TypeWithEvents();
            Action<int> handler = i =>
            {
            };
            Assert.Throws<RuntimeBinderException>(() => d.NothingHere += handler);
        }

        [Fact]
        public void BindToNonEvents()
        {
            dynamic d = new TypeWithEvents();
            Action<int> handler = i =>
            {
            };
            Assert.Throws<RuntimeBinderException>(() => d.NonEventField += handler);
            Assert.Throws<RuntimeBinderException>(() => d.NonEventProperty += handler);
            Assert.Throws<RuntimeBinderException>(() => d.Raise += handler);
        }

        [Fact]
        public void NullEventObject()
        {
            CallSiteBinder binder = Binder.IsEvent(CSharpBinderFlags.None, "Event", GetType());
            CallSite<Func<CallSite, object, bool>> callSite = CallSite<Func<CallSite, object, bool>>.Create(binder);
            Func<CallSite, object, bool> target = callSite.Target;
            Assert.Throws<RuntimeBinderException>(() => target(callSite, null));
        }

        [Fact]
        public void NullContextAnswerTrue()
        {
            CallSiteBinder binder = Binder.IsEvent(CSharpBinderFlags.None, "Event", null);
            CallSite<Func<CallSite, object, bool>> callSite = CallSite<Func<CallSite, object, bool>>.Create(binder);
            Func<CallSite, object, bool> target = callSite.Target;
            Assert.True(target(callSite, new TypeWithEvents()));
        }

        [Fact]
        public void NullContextAnswerFalse()
        {
            CallSiteBinder binder = Binder.IsEvent(CSharpBinderFlags.None, "Nah", null);
            CallSite<Func<CallSite, object, bool>> callSite = CallSite<Func<CallSite, object, bool>>.Create(binder);
            Func<CallSite, object, bool> target = callSite.Target;
            Assert.False(target(callSite, new TypeWithEvents()));
        }

        [Fact]
        public void ContextWithVisibilityOnPrivateType()
        {
            CallSiteBinder binder = Binder.IsEvent(CSharpBinderFlags.None, "Event", GetType());
            CallSite<Func<CallSite, object, bool>> callSite = CallSite<Func<CallSite, object, bool>>.Create(binder);
            Func<CallSite, object, bool> target = callSite.Target;
            Assert.True(target(callSite, new PrivateTypeWithEvent()));
        }

        [Fact]
        public void NullContextAndPrivateType()
        {
            CallSiteBinder binder = Binder.IsEvent(CSharpBinderFlags.None, "Event", null);
            CallSite<Func<CallSite, object, bool>> callSite = CallSite<Func<CallSite, object, bool>>.Create(binder);
            Func<CallSite, object, bool> target = callSite.Target;
            Assert.False(target(callSite, new PrivateTypeWithEvent()));
        }
    }
}
