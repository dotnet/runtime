// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;

namespace System.Runtime
{
    class BypassReadyToRunAttribute : Attribute
    {
    }
}
class DynamicPgoSmokeTest
{
    sealed class JitEventListener : EventListener
    {
        private const int JIT_KEYWORD = 0x0000010;
        private const int MethodLoadVerboseEvent = 143;

        private const int MethodNamespaceIndex = 6;
        private const int MethodNameIndex = 7;
        private const int MethodSignatureIndex = 8;

        private Action<string,string,string> _onMethodJittedEvent;

        public JitEventListener(Action<string,string,string> onMethodJittedEvent)
        {
            _onMethodJittedEvent = onMethodJittedEvent;
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // look for .NET JIT events
            if (eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime"))
            {
                EnableEvents(
                    eventSource, 
                    EventLevel.Verbose, 
                    (EventKeywords) (JIT_KEYWORD)
                    );
            }
        }

        private static void VerifyEventDataShape(EventWrittenEventArgs eventData, int index, string expectedPayloadName)
        {
            if (eventData.PayloadNames[index] != expectedPayloadName)
                throw new Exception($"Unexpected payload name of {eventData.PayloadNames[index]} expected {expectedPayloadName}");
        }

        // from https://blogs.msdn.microsoft.com/dotnet/2018/12/04/announcing-net-core-2-2/
        // Called whenever an event is written.
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventId == 143)
            {
                VerifyEventDataShape(eventData, MethodNamespaceIndex, "MethodNamespace");
                VerifyEventDataShape(eventData, MethodNameIndex, "MethodName");
                VerifyEventDataShape(eventData, MethodSignatureIndex, "MethodSignature");
                _onMethodJittedEvent(eventData.Payload[MethodNamespaceIndex].ToString(), eventData.Payload[MethodNameIndex].ToString(), eventData.Payload[MethodSignatureIndex].ToString());
            }
        }
    }

    static int t = 0;
    static int s = 0;
    static volatile int countBarMethodJitted = 0;

    private static void DetectBarMethodJitted(string methodNamespace, string methodName, string methodSignature)
    {
        if (methodNamespace == "DynamicPgoSmokeTest" && methodName == "Bar")
        {
            countBarMethodJitted++;
        }
    }

    [BypassReadyToRunAttribute]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Bar(int i, bool b = false) 
    {
        if (b)
        {
            s++;
        }


        if ((i % 3) == 0) t++;
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static int Main()
    {
        JitEventListener jitListener = new JitEventListener(DetectBarMethodJitted);

        for (int jitLoop = 0; jitLoop < 10 && (countBarMethodJitted < 2); jitLoop++)
        {
            // Loop for up to 10 seconds looking for the bar method to be jitted twice (once in Tier0 and the second in Tier1)
            for (int i = 0; i < 3_000; i++)
            {
                Bar(i);
            }

            Thread.Sleep(1000);
            Console.WriteLine(".");
        }

        // After making sure that the method was recompiled, ensure that the method still works
        for (int i = 0; i < 15_000; i++)
        {
            Bar(i);
        }

        return countBarMethodJitted >= 2 ? 100 : 1;
    }
}
 