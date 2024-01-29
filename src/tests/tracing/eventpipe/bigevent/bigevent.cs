// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using Tracing.Tests.Common;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.NETCore.Client;
using Xunit;

namespace Tracing.Tests.BigEventsValidation
{

    public sealed class BigEventSource : EventSource
    {
        private static string bigString = new String('a', 100 * 1024);
        private static string smallString = new String('a', 10);

        private BigEventSource()
        {
        }

        public static BigEventSource Log = new BigEventSource();

        public void BigEvent()
        {
            WriteEvent(1, bigString);
        }

        public void SmallEvent()
        {
            WriteEvent(2, smallString);
        }
    }


    public class BigEventsValidation
    {
        [Fact]
        public static int TestEntryPoint()
        {
            // This test tries to send a big event (>100KB) and checks that the app does not crash
            // See https://github.com/dotnet/runtime/issues/50515 for the regression issue
            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("BigEventSource", EventLevel.Verbose)
            };

            return IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, providers, 1024, _Verify);
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            { "BigEventSource", -1 }
        };

        private static Action _eventGeneratingAction = () =>
        {
            // Write 10 big events
            for (int i = 0; i < 10; i++)
            {
                BigEventSource.Log.BigEvent();
            }
            // Write 10 small events
            for (int i = 0; i < 10; i++)
            {
                BigEventSource.Log.SmallEvent();
            }
        };

        private static Func<EventPipeEventSource, Func<int>> _Verify = (source) =>
        {
            bool hasSmallEvent = false;
            source.Dynamic.All += (TraceEvent data) =>
            {
                if (data.EventName == "SmallEvent")
                {
                    hasSmallEvent = true;
                }
            };
            return () => hasSmallEvent ? 100 : -1;
        };
    }
}
