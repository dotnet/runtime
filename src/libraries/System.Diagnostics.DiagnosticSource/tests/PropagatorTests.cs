// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Linq;
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
                Assert.NotNull(DistributedContextPropagator.Current);

                //
                // Default Propagator
                //

                Assert.Same(DistributedContextPropagator.CreateDefaultPropagator(), DistributedContextPropagator.Current);

                TestDefaultPropagatorUsingW3CActivity(
                                DistributedContextPropagator.Current,
                                "Legacy1=true",
                                new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("     LegacyKey1     ", "    LegacyValue1    ") });

                TestDefaultPropagatorUsingHierarchicalActivity(
                                DistributedContextPropagator.Current,
                                "Legacy2=true",
                                new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("LegacyKey2", "LegacyValue2") });

                TestFields(DistributedContextPropagator.Current);

                //
                // NoOutput Propagator
                //

                DistributedContextPropagator.Current = DistributedContextPropagator.CreateNoOutputPropagator();
                Assert.NotNull(DistributedContextPropagator.Current);
                TestNoOutputPropagatorUsingHierarchicalActivity(
                                DistributedContextPropagator.Current,
                                "ActivityState=1",
                                new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("B1", "V1"), new KeyValuePair<string, string>(" B2 ", " V2 ")});

                TestNoOutputPropagatorUsingHierarchicalActivity(
                                DistributedContextPropagator.Current,
                                "ActivityState=2",
                                null);

                TestNoOutputPropagatorUsingW3CActivity(
                                DistributedContextPropagator.Current,
                                "ActivityState=1",
                                new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>(" B3 ", " V3"), new KeyValuePair<string, string>(" B4 ", " V4 "), new KeyValuePair<string, string>("B5", "V5")});

                TestNoOutputPropagatorUsingW3CActivity(
                                DistributedContextPropagator.Current,
                                "ActivityState=2",
                                null);

                TestFields(DistributedContextPropagator.Current);

                //
                // Pass Through Propagator
                //

                DistributedContextPropagator.Current = DistributedContextPropagator.CreatePassThroughPropagator();
                Assert.NotNull(DistributedContextPropagator.Current);
                TestPassThroughPropagatorUsingHierarchicalActivityWithParentChain(
                                DistributedContextPropagator.Current,
                                "PassThrough=true",
                                new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("PassThroughKey1", "PassThroughValue1"), new KeyValuePair<string, string>("PassThroughKey2", "PassThroughValue2")});

                TestPassThroughPropagatorUsingHierarchicalActivityWithParentId(
                                DistributedContextPropagator.Current,
                                "PassThrough1=true",
                                new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("PassThroughKey3", "PassThroughValue3"), new KeyValuePair<string, string>(" PassThroughKey4 ", " PassThroughValue4 ")});

                TestPassThroughPropagatorUsingW3CActivity(
                                DistributedContextPropagator.Current,
                                "PassThrough2=1",
                                new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("     PassThroughKey4     ", "    PassThroughValue4    ") });

                TestPassThroughPropagatorWithNullCurrent(DistributedContextPropagator.Current);

                TestFields(DistributedContextPropagator.Current);

                //
                // Test Current
                //

                Assert.Throws<ArgumentNullException>(() => DistributedContextPropagator.Current = null);

            }).Dispose();
        }

        private void TestDefaultPropagatorUsingW3CActivity(DistributedContextPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
        {
            using Activity a = CreateW3CActivity("LegacyW3C1", "LegacyW3CState=1", baggage);
            using Activity b = CreateW3CActivity("LegacyW3C2", "LegacyW3CState=2", baggage);

            Assert.NotSame(Activity.Current, a);

            TestDefaultPropagatorUsing(a, propagator, state, baggage);

            Assert.Same(Activity.Current, b);

            TestDefaultPropagatorUsing(Activity.Current, propagator, state, baggage);
        }

        private void TestDefaultPropagatorUsingHierarchicalActivity(DistributedContextPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
        {
            using Activity a = CreateHierarchicalActivity("LegacyHierarchical1", null, "LegacyHierarchicalState=1", baggage);
            using Activity b = CreateHierarchicalActivity("LegacyHierarchical2", null, "LegacyHierarchicalState=2", baggage);

            Assert.NotSame(Activity.Current, a);

            TestDefaultPropagatorUsing(a, propagator, state, baggage);

            Assert.Same(Activity.Current, b);

            TestDefaultPropagatorUsing(Activity.Current, propagator, state, baggage);
        }

        private void TestDefaultPropagatorUsing(Activity a, DistributedContextPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
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

                Assert.Fail($"Encountered wrong header name '{fieldName}'");
            });

            TestDefaultExtraction(propagator, a);
            TestBaggageExtraction(propagator, a);
        }

        private void TestNoOutputPropagatorUsingHierarchicalActivity(DistributedContextPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
        {
            using Activity a = CreateHierarchicalActivity("NoOutputHierarchical", null, state, baggage);

            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                Assert.Fail($"Not expected to have the setter callback be called in the NoOutput propgator.");
            });

            TestDefaultExtraction(propagator, a);

            TestBaggageExtraction(propagator, a);
        }

        private void TestNoOutputPropagatorUsingW3CActivity(DistributedContextPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
        {
            using Activity a = CreateW3CActivity("NoOutputW3C", state, baggage);

            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                Assert.Fail($"Not expected to have the setter callback be called in the NoOutput propgator.");
            });

            TestDefaultExtraction(propagator, a);

            TestBaggageExtraction(propagator, a);
        }

        private void TestPassThroughPropagatorUsingHierarchicalActivityWithParentChain(DistributedContextPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
        {
            using Activity a = CreateHierarchicalActivity("PassThrough", null, state, baggage);
            using Activity b = CreateHierarchicalActivity("PassThroughChild1", null, state + "1", new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("Child1Key", "Child1Value") } );
            using Activity c = CreateHierarchicalActivity("PassThroughChild2", null, state + "2", new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("Child2Key", "Child2Value") } );

            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                if (fieldName == TraceParent)
                {
                    Assert.Fail($"Unexpected to inject a TraceParent with Hierarchical Activity.");
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

                Assert.Fail($"Encountered wrong header name '{fieldName}'");
            });

            TestDefaultExtraction(propagator, a);
            TestDefaultExtraction(propagator, b);
            TestDefaultExtraction(propagator, c);

            TestBaggageExtraction(propagator, a);
            TestBaggageExtraction(propagator, b);
            TestBaggageExtraction(propagator, c);
        }

        private void TestPassThroughPropagatorUsingHierarchicalActivityWithParentId(DistributedContextPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
        {
            using Activity a = CreateHierarchicalActivity("PassThrough", "Parent1", state, baggage);
            using Activity b = CreateHierarchicalActivity("PassThroughChild1", "Parent2", state + "1", new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("Child1Key", "Child1Value") } );
            using Activity c = CreateHierarchicalActivity("PassThroughChild2", "Parent3", state + "2", new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("Child2Key", "Child2Value") } );

            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                if (fieldName == TraceParent)
                {
                    Assert.Fail($"Unexpected to inject a TraceParent with Hierarchical Activity.");
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

                Assert.Fail($"Encountered wrong header name '{fieldName}'");
            });

            TestDefaultExtraction(propagator, a);
            TestDefaultExtraction(propagator, b);
            TestDefaultExtraction(propagator, c);

            TestBaggageExtraction(propagator, a);
            TestBaggageExtraction(propagator, b);
            TestBaggageExtraction(propagator, c);
        }

        private void TestPassThroughPropagatorUsingW3CActivity(DistributedContextPropagator propagator, string state, IEnumerable<KeyValuePair<string, string>> baggage)
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

                Assert.Fail($"Encountered wrong header name '{fieldName}'");
            });

            TestDefaultExtraction(propagator, a);
            TestBaggageExtraction(propagator, a);
        }

        private void TestPassThroughPropagatorWithNullCurrent(DistributedContextPropagator propagator)
        {
            Activity.Current = null;

            propagator.Inject(null, null, (object carrier, string fieldName, string value) =>
            {
                Assert.Fail($"PassThroughPropagator shouldn't inject anything if the Activity.Current is null");
            });

            using Activity a = CreateW3CActivity("PassThroughNotNull", "", null);

            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                if (fieldName == TraceParent)
                {
                    Assert.Equal(a.Id, value);
                    return;
                }

                Assert.Fail($"Encountered wrong header name '{fieldName}'");
            });
        }

        private void TestDefaultExtraction(DistributedContextPropagator propagator, Activity a)
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
                        Assert.Fail($"Not expected to get RequestId as we expect the request handled using TraceParenet.");
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

                Assert.Fail($"Encountered wrong header name '{fieldName}'");
            }, out string? traceId, out string? traceState);

            Assert.Equal(a.Id, traceId);
            Assert.Equal(a.TraceStateString, traceState);
        }

        private void TestBaggageExtraction(DistributedContextPropagator propagator, Activity a)
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

                Assert.Fail($"Encountered wrong header name '{fieldName}'");
            });

            Assert.Equal(GetFormattedBaggage(a.Baggage, false, true), GetFormattedBaggage(b, true));
        }

        private void TestFields(DistributedContextPropagator propagator)
        {
            Assert.True(propagator.Fields.Contains(TraceParent));
            Assert.True(propagator.Fields.Contains(RequestId));
            Assert.True(propagator.Fields.Contains(TraceState));
            Assert.True(propagator.Fields.Contains(Baggage));
            Assert.True(propagator.Fields.Contains(CorrelationContext));
        }

        internal static string GetFormattedBaggage(IEnumerable<KeyValuePair<string, string?>>? b, bool flipOrder = false, bool trimSpaces = false)
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

                formattedBaggage += (formattedBaggage.Length > 0 ? ", " : "") + WebUtility.UrlEncode(key) + "=" + WebUtility.UrlEncode(value);
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

        internal static IEnumerable<KeyValuePair<string, string>>? ParseBaggage(string baggageString)
        {
            if (baggageString is null)
            {
                return null;
            }

            List<KeyValuePair<string, string>> list = new();
            string [] parts = baggageString.Split(',');

            foreach (string part in parts)
            {
                string [] baggageItem = part.Split('=');

                if (baggageItem.Length != 2)
                {
                    return null; // Invalid format
                }

                list.Add(new KeyValuePair<string, string>(WebUtility.UrlDecode(baggageItem[0]).Trim(), WebUtility.UrlDecode(baggageItem[1]).Trim()));
            }

            return list;
        }

        [Fact]
        public void TestBuiltInPropagatorsAreCached()
        {
            Assert.Same(DistributedContextPropagator.CreateDefaultPropagator(), DistributedContextPropagator.CreateDefaultPropagator());
            Assert.Same(DistributedContextPropagator.CreateNoOutputPropagator(), DistributedContextPropagator.CreateNoOutputPropagator());
            Assert.Same(DistributedContextPropagator.CreatePassThroughPropagator(), DistributedContextPropagator.CreatePassThroughPropagator());
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestCustomPropagator()
        {
            RemoteExecutor.Invoke(() => {

                DistributedContextPropagator.Current = new CustomPropagator();
                using Activity a = CreateW3CActivity("CustomW3C1", "CustomW3CState=1", new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>(" CustomKey1 ", "    CustomValue1  ") });

                string traceParent   = "x-" + a.Id ;
                string traceState    = "x-" + a.TraceStateString;
                string baggageString = "x=y, " + GetFormattedBaggage(a.Baggage);

                DistributedContextPropagator.Current.Inject(a, null, (object carrier, string fieldName, string value) =>
                {
                    if (fieldName == CustomPropagator.XTraceParent)
                    {
                        Assert.Equal(traceParent, value);
                        return;
                    }

                    if (fieldName == CustomPropagator.XTraceState)
                    {
                        Assert.Equal(traceState, value);
                        return;
                    }

                    if (fieldName == CustomPropagator.XBaggage)
                    {
                        Assert.Equal(baggageString, value);
                        return;
                    }

                    Assert.Fail($"Encountered wrong header name '{fieldName}' in the Custom Propagator");
                });

                DistributedContextPropagator.Current.ExtractTraceIdAndState(null, (object carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
                {
                    fieldValues = null;
                    fieldValue = null;

                    if (fieldName == CustomPropagator.XTraceParent)
                    {
                        fieldValue = traceParent;
                        return;
                    }

                    if (fieldName == CustomPropagator.XTraceState)
                    {
                        fieldValue = traceState;
                        return;
                    }

                    Assert.Fail($"Encountered wrong header name '{fieldName}' in the Custom propagator");
                }, out string? traceId, out string? state);

                Assert.Equal(traceParent, traceId);
                Assert.Equal(traceState, state);

                IEnumerable<KeyValuePair<string, string?>>? b = DistributedContextPropagator.Current.ExtractBaggage(null, (object carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
                {
                    Assert.Null(carrier);
                    fieldValue = null;
                    fieldValues = null;

                    if (fieldName == CustomPropagator.XBaggage)
                    {
                        fieldValue = baggageString;
                        return;
                    }

                    Assert.Fail($"Encountered wrong header name '{fieldName}' in custom propagator");
                });

                Assert.Equal(2, b.Count());
                Assert.Equal(new KeyValuePair<string, string>("x", "y"), b.ElementAt(0));
                Assert.Equal(new KeyValuePair<string, string>("CustomKey1", "CustomValue1"), b.ElementAt(1));

            }).Dispose();
        }

        internal class CustomPropagator : DistributedContextPropagator
        {
            internal const string XTraceParent = "x-traceparent";
            internal const string XTraceState = "x-tracestate";
            internal const string XBaggage = "x-baggage";

            public override IReadOnlyCollection<string> Fields { get; } = new[] { XTraceParent, XTraceState, XBaggage};

            public override void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter)
            {
                if (activity is null || carrier is null)
                {
                    return;
                }

                setter(carrier, XTraceParent, "x-" + activity.Id);

                if (!string.IsNullOrEmpty(activity.TraceStateString))
                {
                    setter(carrier, XTraceState, "x-" + activity.TraceStateString);
                }

                if (activity.Baggage.Count() > 0)
                {
                    setter(carrier, XBaggage, "x=y, " + PropagatorTests.GetFormattedBaggage(activity.Baggage));
                }
            }

            public override void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState)
            {
                if (getter is null)
                {
                    traceId = null;
                    traceState = null;
                    return;
                }

                getter(carrier, XTraceParent, out traceId, out _);
                getter(carrier, XTraceState, out traceState, out _);
            }

            public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object? carrier, PropagatorGetterCallback? getter)
            {
                if (getter is null)
                {
                    return null;
                }

                getter(carrier, XBaggage, out string? theBaggage, out _);

                return PropagatorTests.ParseBaggage(theBaggage);
            }
        }
    }
}
