// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Linq;
using System;

/// <summary>
/// Tests that using an EventSource to get the manifest works without method references in a trimmed application.
/// EventSource has DynamicallyAccessedMembersAttribute applied at class level which means derived types keeps members
/// </summary>
internal class Program
{
    internal class EventSourceTest : EventSource
    {
        public void EventSourceTest_Method_0() => WriteEvent(1); 

        public void EventSourceTest_Method_1(int value) => WriteEvent(2, value); 
        void EventSourceTest_Method_2(string name) => WriteEvent(3, name); 

        [NonEvent]
        public void EventSourceTest_Method_3(int num1, int num2) => WriteEvent(4, num1, num2); 
        [NonEvent]
        void EventSourceTest_Method_4(){}

        [Event(500)]
        public void EventSourceTest_Method_5(byte[] bytes) => WriteEvent(500, bytes); 
        [Event(1500)]
        protected virtual void EventSourceTest_Method_6(long value) => WriteEvent(1500, value); 

        [Event(2500)]
        int EventSourceTest_Method_7() => 5; 
    }

    [UnconditionalSuppressMessage ("ReflectionAnalysis", "IL2118",
        Justification = "DAM on EventSource.GenerateManifest references compiler-generated local function GetTrimSafeTraceLoggingEventTypes " +
                        "which calls a constructor that requires unreferenced code. EventSource will not access this local function.")]
    public static int Main()
    {
        string manifest = EventSource.GenerateManifest(typeof(EventSourceTest), null);
        // we are going to avoid as much as possible program constructs that could give the trimmer reasons to keep members
        const string baseMethodName = "EventSourceTest_Method_";
        int[] exclusions = { 3, 4, 8 };
        for (int i = 0; i <= 8; i++)
        {
            string methodName = $"{baseMethodName}{i}";
            // We expect the methodName to be in the manifest unless the prefix, i, is in exclusions ([NonEvent] or non-existing methods)
            bool methodExists = manifest.Contains(methodName);
            bool shouldMethodExist = !exclusions.Contains(i);
            if (methodExists != shouldMethodExist)
            {
                return -1;
            }
        }

        return 100;
    }
}
