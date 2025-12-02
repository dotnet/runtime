// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace Sample
{
    class ConsoleWriterEventListener : EventListener
    {
        public static ConsoleWriterEventListener Instance;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if(eventSource.Name == "WasmHello")
            {
                EnableEvents(eventSource, EventLevel.Informational);
            }
            if(eventSource.Name == "System.Runtime")
            {
                EnableEvents(eventSource, EventLevel.Informational);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            Console.WriteLine(eventData.TimeStamp + " " + eventData.EventName);
        }
    }

    public partial class Test
    {
        public static int Main(string[] args)
        {
            DisplayMeaning(42);

            WasmHelloEventSource.Instance.NewCallsCounter();
            ConsoleWriterEventListener.Instance = new ConsoleWriterEventListener();

            // SayHi();
            return 0;
        }

        [JSImport("Sample.Test.displayMeaning", "main.js")]
        internal static partial void DisplayMeaning(int meaning);

        public static int counter;

        [JSExport]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SayHi()
        {
            Console.WriteLine("Hi from C#! " + EventSource.CurrentThreadActivityId);
            WasmHelloEventSource.Instance.HelloStart(counter);
            Console.WriteLine("Wave from C#! " + EventSource.CurrentThreadActivityId);
            for(int i = 0; i < 100000; i++)
            {
                WasmHelloEventSource.Instance.CountCall();
            }
            counter++;
            SayHiCatch();
            WasmHelloEventSource.Instance.HelloStop(counter, "counter"+counter);
            Console.WriteLine("Bye from C#! " + EventSource.CurrentThreadActivityId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SayHiThrow()
        {
            throw new Exception("Hello from C#!");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SayHiCatch()
        {
            try
            {
                SayHiThrow();
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception: " + e.Message);
            }
        }

        [JSExport]
        internal static Task SayHiAsync()
        {
            WasmHelloEventSource.Instance.HelloStart(counter);
            Console.WriteLine("Hi from C#!");
            for(int i = 0; i < 100000; i++)
            {
                WasmHelloEventSource.Instance.CountCall();
            }
            counter++;
            Console.WriteLine("Hello from C#!");
            WasmHelloEventSource.Instance.HelloStop(counter, "counter"+counter);

            return Task.CompletedTask;
        }
    }



    [EventSource(Name = "WasmHello")]
    public class WasmHelloEventSource  : EventSource
    {
        public static readonly WasmHelloEventSource Instance = new ();

        private IncrementingEventCounter _calls;

        private WasmHelloEventSource ()
        {
        }

        [NonEvent]
        public void NewCallsCounter()
        {
            _calls?.Dispose();
            _calls = new ("hello-calls", this)
            {
                DisplayName = "Hello calls",
            };
        }

        [NonEvent]
        public void CountCall() {
            _calls?.Increment(1.0);
        }

        protected override void Dispose (bool disposing)
        {
            _calls?.Dispose();
            _calls = null;

            base.Dispose(disposing);
        }

        [Event(1, Message="Started Hello({0})", Level = EventLevel.Informational)]
        public void HelloStart(int n)
        {
            if (!IsEnabled())
                return;

            WriteEvent(1, n);
        }

        [Event(2, Message="Stopped Hello({0}) = {1}", Level = EventLevel.Informational)]
        public void HelloStop(int n, string result)
        {
            if (!IsEnabled())
                return;

            WriteEvent(2, n, result);
        }
    }
}
