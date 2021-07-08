// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Collections.Generic;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class PropagatorTests
    {
        internal const string TraceParent        = "traceparent";
        internal const string RequestId          = "Request-Id";
        internal const string TraceState         = "tracestate";
        internal const string Baggage            = "baggage";
        internal const string CorrelationContext = "Correlation-Context";

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestAllPropagators()
        {
            RemoteExecutor.Invoke(() => {
                Assert.NotNull(TextMapPropagator.Current);

                //
                // Default Propagator
                //

                Assert.Same(TextMapPropagator.CreateDefaultPropagator(), TextMapPropagator.Current);

                TestLegacyPropagatorUsingW3CActivity(
                                TextMapPropagator.Current,
                                "Legacy1=true",
                                new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("     LegacyKey1     ", "    LegacyValue1    ") });

                TestLegacyPropagatorUsingHierarchicalActivity(
                                TextMapPropagator.Current,
                                "Legacy2=true",
                                new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("LegacyKey2", "LegacyValue2") });

                //
                // NoOutput Propagator
                //

                TextMapPropagator.Current = TextMapPropagator.CreateNoOutputPropagator();
                Assert.NotNull(TextMapPropagator.Current);
                TestNoOutputPropagatorUsingHierarchicalActivity(
                                TextMapPropagator.Current,
                                "ActivityState=1",
                                new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("B1", "V1"), new KeyValuePair<string, string>(" B2 ", " V2 ")});

                TestNoOutputPropagatorUsingHierarchicalActivity(
                                TextMapPropagator.Current,
                                "ActivityState=2",
                                null);

                TestNoOutputPropagatorUsingW3CActivity(
                                TextMapPropagator.Current,
                                "ActivityState=1",
                                new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>(" B3 ", " V3"), new KeyValuePair<string, string>(" B4 ", " V4 "), new KeyValuePair<string, string>("B5", "V5")});

                TestNoOutputPropagatorUsingW3CActivity(
                                TextMapPropagator.Current,
                                "ActivityState=2",
                                null);

                //
                // Pass Through Propagator
                //

                TextMapPropagator.Current = TextMapPropagator.CreatePassThroughPropagator();
                Assert.NotNull(TextMapPropagator.Current);
                TestPassThroughPropagatorUsingHierarchicalActivityWithParentChain(
                                TextMapPropagator.Current,
                                "PassThrough=true",
                                new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("PassThroughKey1", "PassThroughValue1"), new KeyValuePair<string, string>("PassThroughKey2", "PassThroughValue2")});

                TestPassThroughPropagatorUsingHierarchicalActivityWithParentId(
                                TextMapPropagator.Current,
                                "PassThrough1=true",
                                new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("PassThroughKey3", "PassThroughValue3"), new KeyValuePair<string, string>(" PassThroughKey4 ", " PassThroughValue4 ")});

                TestPassThroughPropagatorUsingW3CActivity(
                                TextMapPropagator.Current,
                                "PassThrough2=1",
                                new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("     PassThroughKey4     ", "    PassThroughValue4    ") });

            }).Dispose();
        }

        private void TestLegacyPropagatorUsingW3CActivity(TextMapPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
        {
            using Activity a = CreateW3CActivity("LegacyW3C1", "LegacyW3CState=1", baggage);
            using Activity b = CreateW3CActivity("LegacyW3C2", "LegacyW3CState=2", baggage);

            Assert.NotSame(Activity.Current, a);

            TestLegacyPropagatorUsing(a, propagator, state, baggage);

            Assert.Same(Activity.Current, b);

            TestLegacyPropagatorUsing(Activity.Current, propagator, state, baggage);
        }

        private void TestLegacyPropagatorUsingHierarchicalActivity(TextMapPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
        {
            using Activity a = CreateHierarchicalActivity("LegacyHierarchical1", null, "LegacyHierarchicalState=1", baggage);
            using Activity b = CreateHierarchicalActivity("LegacyHierarchical2", null, "LegacyHierarchicalState=2", baggage);

            Assert.NotSame(Activity.Current, a);

            TestLegacyPropagatorUsing(a, propagator, state, baggage);

            Assert.Same(Activity.Current, b);

            TestLegacyPropagatorUsing(Activity.Current, propagator, state, baggage);
        }

        private void TestLegacyPropagatorUsing(Activity a, TextMapPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
        {
            // Test with non-current
            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                if (fieldName == TraceParent && a.IdFormat == ActivityIdFormat.W3C)
                {
                    Assert.Equal(a.Id, value);
                    return;
                }

                if (fieldName == RequestId && a.IdFormat != ActivityIdFormat.W3C)
                {
                    Assert.Equal(a.Id, value);
                    return;
                }

                if (fieldName == TraceState)
                {
                    Assert.Equal(a.TraceStateString, value);
                    return;
                }

                if (fieldName == CorrelationContext)
                {
                    Assert.Equal(GetFormattedBaggage(a.Baggage), value);
                    return;
                }

                Assert.False(true, $"Encountered wrong header name '{fieldName}'");
            });

            TestDefaultExtraction(propagator, a);
            TestBaggageExtraction(propagator, a);
        }

        private void TestNoOutputPropagatorUsingHierarchicalActivity(TextMapPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
        {
            using Activity a = CreateHierarchicalActivity("NoOutputHierarchical", null, state, baggage);

            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                Assert.False(true, $"Not expected to have the setter callback be called in the NoOutput propgator.");
            });

            TestDefaultExtraction(propagator, a);

            TestBaggageExtraction(propagator, a);
        }

        private void TestNoOutputPropagatorUsingW3CActivity(TextMapPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
        {
            using Activity a = CreateW3CActivity("NoOutputW3C", state, baggage);

            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                Assert.False(true, $"Not expected to have the setter callback be called in the NoOutput propgator.");
            });

            TestDefaultExtraction(propagator, a);

            TestBaggageExtraction(propagator, a);
        }

        private void TestPassThroughPropagatorUsingHierarchicalActivityWithParentChain(TextMapPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
        {
            using Activity a = CreateHierarchicalActivity("PassThrough", null, state, baggage);
            using Activity b = CreateHierarchicalActivity("PassThroughChild1", null, state + "1", new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("Child1Key", "Child1Value") } );
            using Activity c = CreateHierarchicalActivity("PassThroughChild2", null, state + "2", new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("Child2Key", "Child2Value") } );

            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                if (fieldName == TraceParent)
                {
                    Assert.False(true, $"Unexpected to inject a TraceParent with Hierarchical Activity.");
                    return;
                }

                if (fieldName == RequestId)
                {
                    Assert.Equal(a.Id, value);
                    return;
                }

                if (fieldName == TraceState)
                {
                    Assert.Equal(a.TraceStateString, value);
                    return;
                }

                if (fieldName == CorrelationContext)
                {
                    Assert.Equal(GetFormattedBaggage(a.Baggage), value);
                    return;
                }

                Assert.False(true, $"Encountered wrong header name '{fieldName}'");
            });

            TestDefaultExtraction(propagator, a);
            TestDefaultExtraction(propagator, b);
            TestDefaultExtraction(propagator, c);

            TestBaggageExtraction(propagator, a);
            TestBaggageExtraction(propagator, b);
            TestBaggageExtraction(propagator, c);
        }

        private void TestPassThroughPropagatorUsingHierarchicalActivityWithParentId(TextMapPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
        {
            using Activity a = CreateHierarchicalActivity("PassThrough", "Parent1", state, baggage);
            using Activity b = CreateHierarchicalActivity("PassThroughChild1", "Parent2", state + "1", new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("Child1Key", "Child1Value") } );
            using Activity c = CreateHierarchicalActivity("PassThroughChild2", "Parent3", state + "2", new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("Child2Key", "Child2Value") } );

            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                if (fieldName == TraceParent)
                {
                    Assert.False(true, $"Unexpected to inject a TraceParent with Hierarchical Activity.");
                    return;
                }

                if (fieldName == RequestId)
                {
                    Assert.Equal(c.ParentId, value);
                    return;
                }

                if (fieldName == TraceState)
                {
                    Assert.Equal(c.TraceStateString, value);
                    return;
                }

                if (fieldName == CorrelationContext)
                {
                    Assert.Equal(GetFormattedBaggage(c.Baggage), value);
                    return;
                }

                Assert.False(true, $"Encountered wrong header name '{fieldName}'");
            });

            TestDefaultExtraction(propagator, a);
            TestDefaultExtraction(propagator, b);
            TestDefaultExtraction(propagator, c);

            TestBaggageExtraction(propagator, a);
            TestBaggageExtraction(propagator, b);
            TestBaggageExtraction(propagator, c);
        }

        private void TestPassThroughPropagatorUsingW3CActivity(TextMapPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
        {
            using Activity a = CreateW3CActivity("PassThroughW3C", "PassThroughW3CState=1", baggage);

            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                if (fieldName == TraceParent)
                {
                    Assert.Equal(a.Id, value);
                    return;
                }

                if (fieldName == TraceState)
                {
                    Assert.Equal(a.TraceStateString, value);
                    return;
                }

                if (fieldName == CorrelationContext)
                {
                    Assert.Equal(GetFormattedBaggage(a.Baggage), value);
                    return;
                }

                Assert.False(true, $"Encountered wrong header name '{fieldName}'");
            });

            TestDefaultExtraction(propagator, a);
            TestBaggageExtraction(propagator, a);
        }

        private void TestDefaultExtraction(TextMapPropagator propagator, Activity a)
        {
            bool traceParentEncountered = false;

            propagator.ExtractTraceIdAndState(null, (object carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
            {
                Assert.Null(carrier);
                fieldValues = null;
                fieldValue = null;

                if (fieldName == TraceParent)
                {
                    if (a.IdFormat == ActivityIdFormat.W3C)
                    {
                        fieldValue = a.Id;
                    }
                    else
                    {
                        traceParentEncountered = true;
                    }
                    return;
                }

                if (fieldName == RequestId)
                {
                    if (a.IdFormat == ActivityIdFormat.W3C)
                    {
                        Assert.True(false, $"Not expected to get RequestId as we expect the request handled using TraceParenet.");
                    }
                    else
                    {
                        Assert.True(traceParentEncountered, $"Expected to get TraceParent request before getting RequestId.");
                        fieldValue = a.Id;
                    }

                    return;
                }

                if (fieldName == TraceState)
                {
                    fieldValue = a.TraceStateString;
                    return;
                }

                Assert.False(true, $"Encountered wrong header name '{fieldName}'");
            }, out string? traceId, out string? traceState);

            Assert.Equal(a.Id, traceId);
            Assert.Equal(a.TraceStateString, traceState);
        }

        private void TestBaggageExtraction(TextMapPropagator propagator, Activity a)
        {
            bool baggageEncountered = false;

            IEnumerable<KeyValuePair<string, string?>>? b = propagator.ExtractBaggage(null, (object carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
            {
                Assert.Null(carrier);
                fieldValue = null;
                fieldValues = null;

                if (fieldName == Baggage)
                {
                    if (a.IdFormat == ActivityIdFormat.W3C)
                    {
                        fieldValue = GetFormattedBaggage(a.Baggage);
                    }
                    else
                    {
                        baggageEncountered = true;
                    }

                    return;
                }

                if (fieldName == CorrelationContext && a.IdFormat != ActivityIdFormat.W3C)
                {
                    Assert.True(baggageEncountered, $"Expected to get Baggage request before getting Correlation-Context.");
                    fieldValue = GetFormattedBaggage(a.Baggage);
                    return;
                }

                Assert.False(true, $"Encountered wrong header name '{fieldName}'");
            });

            Assert.Equal(GetFormattedBaggage(a.Baggage, false, true), GetFormattedBaggage(b, true));
        }

        private static string GetFormattedBaggage(IEnumerable<KeyValuePair<string, string?>>? b, bool flipOrder = false, bool trimSpaces = false)
        {
            string formattedBaggage = "";

            if (b is null)
            {
                return formattedBaggage;
            }
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>(b);

            int startIndex = flipOrder ? list.Count - 1 : 0;
            int exitIndex = flipOrder ? -1 : list.Count;
            int step = flipOrder ? -1 : 1;

            for (int i = startIndex; i != exitIndex; i += step)
            {
                string key = trimSpaces ? list[i].Key.Trim() : list[i].Key;
                string value = trimSpaces ? list[i].Value.Trim() : list[i].Value;

                formattedBaggage += (formattedBaggage.Length > 0 ? "," : "") + WebUtility.UrlEncode(key) + "=" + WebUtility.UrlEncode(value);
            }

            return formattedBaggage;
        }

        private Activity CreateHierarchicalActivity(string name, string parentId, string state, IEnumerable<KeyValuePair<string, string?>>? baggage)
        {
            Activity a = new Activity(name);
            a.SetIdFormat(ActivityIdFormat.Hierarchical);

            if (baggage is not null)
            {
                foreach (KeyValuePair<string, string> kvp in baggage)
                {
                    a.SetBaggage(kvp.Key, kvp.Value);
                }
            }

            a.TraceStateString = state;

            if (parentId is not null)
            {
                a.SetParentId(parentId);
            }
            a.Start();

            return a;
        }

        private Activity CreateW3CActivity(string name, string state, IEnumerable<KeyValuePair<string, string?>>? baggage)
        {
            Activity a = new Activity(name);
            a.SetIdFormat(ActivityIdFormat.W3C);

            if (baggage is not null)
            {
                foreach (KeyValuePair<string, string> kvp in baggage)
                {
                    a.SetBaggage(kvp.Key, kvp.Value);
                }
            }

            a.TraceStateString = state;
            a.Start();

            return a;
        }
    }
}
