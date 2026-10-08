// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Dynamic
{
    using System;
    using System.Runtime.CompilerServices;
    using Xunit;

    internal class EventTest
    {
        private dynamic obj;
        private Random rand;
        private readonly WeakReference _rcwReference;

        public EventTest(int seed = 123)
        {
            Type t = Type.GetTypeFromCLSID(Guid.Parse(ServerGuids.EventTest));
            object rcw = Activator.CreateInstance(t);
            obj = rcw;
            _rcwReference = new WeakReference(rcw);
            rand = new Random(seed);
        }

        public static void Run()
        {
            Console.WriteLine($"Running {nameof(EventTest)}");

            WeakReference rcwReference = RunCore();

            // RunCore removed every handler it added, so each event source's sink has already
            // been unadvised and is kept in the RCW's sink container only so it can be reused.
            // Collecting the RCW finalizes that container, which must not unadvise those sinks
            // a second time.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(rcwReference.IsAlive);
        }

        // The RCW is created and used entirely within this method so that no local in Run can
        // extend its lifetime past the collection there, regardless of the JIT's lifetime and
        // codegen behavior in debug and release builds.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference RunCore()
        {
            var test = new EventTest();
            test.FireEvent();
            test.DynamicEventHandler();
            test.MultipleHandlers();
            test.MultipleSources();
            return test._rcwReference;
        }

        private void FireEvent()
        {
            var h = new EventTestHandler();
            int expected = rand.Next();

            // Add handler
            obj.OnEvent += h.Handler;

            // Fire event
            obj.FireEvent(expected);
            h.Validate(true, expected);

            // Remove handler
            obj.OnEvent -= h.Handler;
            h.Reset();

            // Fire event
            expected = rand.Next();
            obj.FireEvent(expected);
            h.Validate(false);

            // Re-add handler
            obj.OnEvent += h.Handler;
            h.Reset();

            // Fire event
            expected = rand.Next();
            obj.FireEvent(expected);
            h.Validate(true, expected);

            obj.OnEvent -= h.Handler;
        }

        private void DynamicEventHandler()
        {
            dynamic h = new DynamicEventTestHandler();
            int expected = rand.Next();

            // Add handler
            obj.OnEvent += h;

            // Fire event
            obj.FireEvent(expected);
            h.Handler.Validate(true, expected);

            // Remove handler
            obj.OnEvent -= h;
            h.Handler.Reset();

            // Fire event
            expected = rand.Next();
            obj.FireEvent(expected);
            h.Handler.Validate(false);

            // Re-add handler
            obj.OnEvent += h;
            h.Handler.Reset();

            // Fire event
            expected = rand.Next();
            obj.FireEvent(expected);
            h.Handler.Validate(true, expected);

            obj.OnEvent -= h;
        }

        private void MultipleHandlers()
        {
            var h1 = new EventTestHandler();
            var h2 = new EventTestHandler();
            dynamic dh1 = new DynamicEventTestHandler();
            dynamic dh2 = new DynamicEventTestHandler();
            int expected = rand.Next();

            // Add handlers
            obj.OnEvent += h1.Handler;
            obj.OnEvent += h2.Handler;
            obj.OnEvent += dh1;
            obj.OnEvent += dh2;

            // Fire event
            obj.FireEvent(expected);
            h1.Validate(true, expected);
            h2.Validate(true, expected);
            dh1.Handler.Validate(true, expected);
            dh2.Handler.Validate(true, expected);

            // Remove first handler
            obj.OnEvent -= h1.Handler;
            obj.OnEvent -= dh1;
            h1.Reset();
            h2.Reset();
            dh1.Handler.Reset();
            dh2.Handler.Reset();

            // Fire event
            expected = rand.Next();
            obj.FireEvent(expected);
            h1.Validate(false);
            h2.Validate(true, expected);
            dh1.Handler.Validate(false);
            dh2.Handler.Validate(true, expected);

            // Remove second handler
            obj.OnEvent -= h2.Handler;
            obj.OnEvent -= dh2;
            h1.Reset();
            h2.Reset();
            dh1.Handler.Reset();
            dh2.Handler.Reset();

            // Fire event
            expected = rand.Next();
            obj.FireEvent(expected);
            h1.Validate(false);
            h2.Validate(false);
            dh1.Handler.Validate(false);
            dh2.Handler.Validate(false);
        }

        private void MultipleSources()
        {
            var h = new EventTestHandler();
            int expected = rand.Next();
            string expectedMessage = expected.ToString();

            // Add handler
            obj.OnEvent += h.Handler;
            obj.OnEventMessage += h.MessageHandler;

            // Fire event
            obj.FireEvent(expected);
            obj.FireEventMessage(expectedMessage);
            h.Validate(true, expected);
            h.ValidateMessage(true, expectedMessage);

            // Remove handler for first event source
            obj.OnEvent -= h.Handler;
            h.Reset();

            // Fire event
            expected = rand.Next();
            expectedMessage = expected.ToString();
            obj.FireEvent(expected);
            obj.FireEventMessage(expectedMessage);
            h.Validate(false);
            h.ValidateMessage(true, expectedMessage);

            // Remove handler for second event source
            obj.OnEventMessage -= h.MessageHandler;
            h.Reset();

            // Fire event
            expected = rand.Next();
            expectedMessage = expected.ToString();
            obj.FireEvent(expected);
            obj.FireEventMessage(expectedMessage);
            h.Validate(false);
            h.ValidateMessage(false);

            // Re-add handler
            obj.OnEvent += h.Handler;
            obj.OnEventMessage += h.MessageHandler;
            h.Reset();

            // Fire event
            expected = rand.Next();
            expectedMessage = expected.ToString();
            obj.FireEvent(expected);
            obj.FireEventMessage(expectedMessage);
            h.Validate(true, expected);
            h.ValidateMessage(true, expectedMessage);

            obj.OnEvent -= h.Handler;
            obj.OnEventMessage -= h.MessageHandler;
        }

        private class DynamicEventTestHandler : System.Dynamic.DynamicObject
        {
            private EventTestHandler _handler = new EventTestHandler();

            public EventTestHandler Handler => _handler;

            public override bool TryInvoke(System.Dynamic.InvokeBinder binder, object[] args, out object result)
            {
                result = null;
                if (args.Length != 1 || !(args[0] is int))
                    return false;

                _handler.Handler((int)args[0]);
                return true;
            }
        }

        private delegate void OnEventDelegate(int id);
        private delegate void OnEventMessageDelegate(string message);
        private class EventTestHandler
        {
            private const int InvalidId = -1;

            private bool _eventReceived = false;
            private int _id = InvalidId;

            private bool _eventMessageReceived = false;
            private string _message = string.Empty;

            public OnEventDelegate Handler { get; private set; }
            public OnEventMessageDelegate MessageHandler { get; private set; }

            public EventTestHandler()
            {
                Handler = new OnEventDelegate(OnEvent);
                MessageHandler = new OnEventMessageDelegate(OnEventMessage);
            }

            private void OnEvent(int id)
            {
                _eventReceived = true;
                _id = id;
            }

            private void OnEventMessage(string message)
            {
                _eventMessageReceived = true;
                _message = message;
            }

            public void Reset()
            {
                _eventReceived = false;
                _id = InvalidId;
                _eventMessageReceived = false;
                _message = string.Empty;
            }

            public void Validate(bool called, int id = InvalidId)
            {
                Assert.Equal(called, _eventReceived);
                Assert.Equal(id, _id);
            }

            public void ValidateMessage(bool called, string message = "")
            {
                Assert.Equal(called, _eventMessageReceived);
                Assert.Equal(message, _message);
            }
        }
    }
}
