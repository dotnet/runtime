// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Tracing.UserEvents.Tests.Common;
using Microsoft.Diagnostics.Tracing;

namespace Tracing.UserEvents.Tests.Activity
{
    public static class Activity
    {
        public static void ActivityTracee()
        {
            ActivityTraceeAsync().GetAwaiter().GetResult();
        }

        private static async Task ActivityTraceeAsync()
        {
            Task requestA = ProcessRequest("RequestA");
            Task requestB = ProcessRequest("RequestB");

            await Task.WhenAll(requestA, requestB);
        }

        private static async Task ProcessRequest(string requestName)
        {
            ActivityEventSource.Log.WorkStart(requestName);

            Task query1 = Query("Query1 for " + requestName);
            Task query2 = Query("Query2 for " + requestName);

            await Task.WhenAll(query1, query2);

            ActivityEventSource.Log.WorkStop();
        }

        private static async Task Query(string query)
        {
            ActivityEventSource.Log.QueryStart(query);
            await Task.Delay(50);
            ActivityEventSource.Log.DebugMessage("processing " + query);
            await Task.Delay(50);
            ActivityEventSource.Log.QueryStop();
        }

        private static readonly Func<EventPipeEventSource, bool> s_traceValidator = source =>
        {
            Guid firstWorkActivityId = Guid.Empty;
            Guid secondWorkActivityId = Guid.Empty;

            Guid firstWorkQuery1ActivityId = Guid.Empty;
            Guid firstWorkQuery2ActivityId = Guid.Empty;
            Guid secondWorkQuery1ActivityId = Guid.Empty;
            Guid secondWorkQuery2ActivityId = Guid.Empty;

            Guid firstWorkQuery1RelatedActivityId = Guid.Empty;
            Guid firstWorkQuery2RelatedActivityId = Guid.Empty;
            Guid secondWorkQuery1RelatedActivityId = Guid.Empty;
            Guid secondWorkQuery2RelatedActivityId = Guid.Empty;

            source.Dynamic.All += e =>
            {
                if (!string.Equals(e.ProviderName, "DemoActivityIDs", StringComparison.Ordinal))
                {
                    return;
                }

                if (e.EventName is null)
                {
                    return;
                }

                if (e.EventName.Equals("Work/Start", StringComparison.OrdinalIgnoreCase))
                {
                    string requestName = e.PayloadByName("requestName") as string ?? string.Empty;

                    if (string.Equals(requestName, "RequestA", StringComparison.Ordinal))
                    {
                        firstWorkActivityId = e.ActivityID;
                    }
                    else if (string.Equals(requestName, "RequestB", StringComparison.Ordinal))
                    {
                        secondWorkActivityId = e.ActivityID;
                    }
                }
                else if (e.EventName.Equals("Query/Start", StringComparison.OrdinalIgnoreCase))
                {
                    string queryText = e.PayloadByName("query") as string ?? string.Empty;

                    if (string.Equals(queryText, "Query1 for RequestA", StringComparison.Ordinal))
                    {
                        firstWorkQuery1ActivityId = e.ActivityID;
                        firstWorkQuery1RelatedActivityId = e.RelatedActivityID;
                    }
                    else if (string.Equals(queryText, "Query2 for RequestA", StringComparison.Ordinal))
                    {
                        firstWorkQuery2ActivityId = e.ActivityID;
                        firstWorkQuery2RelatedActivityId = e.RelatedActivityID;
                    }
                    else if (string.Equals(queryText, "Query1 for RequestB", StringComparison.Ordinal))
                    {
                        secondWorkQuery1ActivityId = e.ActivityID;
                        secondWorkQuery1RelatedActivityId = e.RelatedActivityID;
                    }
                    else if (string.Equals(queryText, "Query2 for RequestB", StringComparison.Ordinal))
                    {
                        secondWorkQuery2ActivityId = e.ActivityID;
                        secondWorkQuery2RelatedActivityId = e.RelatedActivityID;
                    }
                }
            };

            source.Process();

            if (firstWorkActivityId == Guid.Empty || secondWorkActivityId == Guid.Empty)
            {
                Console.Error.WriteLine("The trace did not contain two WorkStart events with ActivityIds for RequestA and RequestB.");
                return false;
            }

            if (firstWorkQuery1ActivityId == Guid.Empty || firstWorkQuery2ActivityId == Guid.Empty ||
                secondWorkQuery1ActivityId == Guid.Empty || secondWorkQuery2ActivityId == Guid.Empty)
            {
                Console.Error.WriteLine("The trace did not contain all expected QueryStart events with ActivityIds for both requests.");
                return false;
            }

            if (firstWorkQuery1RelatedActivityId == Guid.Empty || firstWorkQuery2RelatedActivityId == Guid.Empty ||
                secondWorkQuery1RelatedActivityId == Guid.Empty || secondWorkQuery2RelatedActivityId == Guid.Empty)
            {
                Console.Error.WriteLine("The trace did not contain RelatedActivityIds on all QueryStart events.");
                return false;
            }

            if (firstWorkQuery1RelatedActivityId != firstWorkActivityId ||
                firstWorkQuery2RelatedActivityId != firstWorkActivityId ||
                secondWorkQuery1RelatedActivityId != secondWorkActivityId ||
                secondWorkQuery2RelatedActivityId != secondWorkActivityId)
            {
                Console.Error.WriteLine("QueryStart RelatedActivityIds did not match their corresponding WorkStart ActivityIds.");
                return false;
            }

            return true;
        };

        public static int Main(string[] args)
        {
            return UserEventsTestRunner.Run(
                args,
                "activity",
                ActivityTracee,
                s_traceValidator);
        }
    }

    [EventSource(Name = "DemoActivityIDs")]
    internal sealed class ActivityEventSource : EventSource
    {
        public static readonly ActivityEventSource Log = new ActivityEventSource();

        private ActivityEventSource() {}

        [Event(1)]
        public void WorkStart(string requestName) => WriteEvent(1, requestName);

        [Event(2)]
        public void WorkStop() => WriteEvent(2);

        [Event(3)]
        public void DebugMessage(string message) => WriteEvent(3, message);

        [Event(4)]
        public void QueryStart(string query) => WriteEvent(4, query);

        [Event(5)]
        public void QueryStop() => WriteEvent(5);
    }
}
