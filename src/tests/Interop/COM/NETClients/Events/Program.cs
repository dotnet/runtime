// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NetClient
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using TestLibrary;
    using Server.Contract;
    using Server.Contract.Servers;
    using Server.Contract.Events;

    class Program
    {
        static void Validate_BasicCOMEvent()
        {
            Console.WriteLine($"{nameof(Validate_BasicCOMEvent)}...");

            var eventTesting = (EventTesting)new EventTestingClass();

            // Verify event handler subscription

            // Add event
            eventTesting.OnEvent += OnEventEventHandler;

            bool eventFired = false;
            string message = string.Empty;
            eventTesting.FireEvent();

            Assert.IsTrue(eventFired, "Event didn't fire");
            Assert.AreEqual(nameof(EventTesting.FireEvent), message, "Event message is incorrect");

            // Remove event
            eventTesting.OnEvent -= OnEventEventHandler;

            // Verify event handler removed

            eventFired = false;
            eventTesting.FireEvent();

            Assert.IsFalse(eventFired, "Event shouldn't fire");

            void OnEventEventHandler(string msg)
            {
                eventFired = true;
                message = msg;
            }
        }

        // The ComAwareEventInfo is used by the compiler when PIAs
        // containing COM Events are embedded.
        static void Validate_COMEventViaComAwareEventInfo()
        {
            Console.WriteLine($"{nameof(Validate_COMEventViaComAwareEventInfo)}...");

            var eventTesting = (EventTesting)new EventTestingClass();

            // Verify event handler subscription

            // Add event
            var comAwareEventInfo = new ComAwareEventInfo(typeof(TestingEvents_Event), nameof(TestingEvents_Event.OnEvent));
            var handler = new TestingEvents_OnEventEventHandler(OnEventEventHandler);
            comAwareEventInfo.AddEventHandler(eventTesting, handler);

            bool eventFired = false;
            string message = string.Empty;
            eventTesting.FireEvent();

            Assert.IsTrue(eventFired, "Event didn't fire");
            Assert.AreEqual(nameof(EventTesting.FireEvent), message, "Event message is incorrect");

            comAwareEventInfo.RemoveEventHandler(eventTesting, handler);

            // Verify event handler removed

            eventFired = false;
            eventTesting.FireEvent();

            Assert.IsFalse(eventFired, "Event shouldn't fire");

            void OnEventEventHandler(string msg)
            {
                eventFired = true;
                message = msg;
            }
        }

        static int Main(string[] doNotUse)
        {
            // RegFree COM is not supported on Windows Nano
            if (Utilities.IsWindowsNanoServer)
            {
                return 100;
            }

            try
            {
                Validate_BasicCOMEvent();
                Validate_COMEventViaComAwareEventInfo();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return 100;
        }
    }
}
