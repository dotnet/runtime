// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Collections.Generic;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class PropagatorTests
    {
        private string ToString(ActivityContext context)
        {
            Span<char> traceParent = stackalloc char[55];
            traceParent[0]  = '0';
            traceParent[1]  = '0';
            traceParent[2]  = '-';
            traceParent[35] = '-';
            traceParent[52] = '-';
            CopyStringToSpan(context.TraceId.ToHexString(), traceParent.Slice(3, 32));
            CopyStringToSpan(context.SpanId.ToHexString(),  traceParent.Slice(36, 16));
            traceParent[53] = '0';
            traceParent[54] = (context.TraceFlags & ActivityTraceFlags.Recorded) != 0 ? '1' : '0';
            return traceParent.ToString();
        }

        private string GetFormattedBaggage(Activity a)
        {
            string formattedBaggage = "";
            IEnumerator<KeyValuePair<string, string>> enumerator = a.Baggage.GetEnumerator();
            while (enumerator.MoveNext())
            {
                formattedBaggage += (formattedBaggage.Length > 0 ? "," : "") + enumerator.Current.Key + "=" + enumerator.Current.Value;
            }

            return formattedBaggage;
        }

        private static void CopyStringToSpan(string s, Span<char> span)
        {
            Debug.Assert(s is not null);
            Debug.Assert(s.Length == span.Length);

            for (int i = 0; i < s.Length; i++)
            {
                span[i] = s[i];
            }
        }

        [Fact]
        public void DifferentTests()
        {
            TextMapPropagator propagator = TextMapPropagator.Default;

            Assert.NotNull(propagator);

            Activity a = new Activity("SomeActivity");
            a.SetIdFormat(ActivityIdFormat.Hierarchical);
            a.SetBaggage("B1", "v1");
            a.TraceStateString = "traceState";
            a.SetParentId("Hierarchical");
            a.Start();

            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                Assert.Null(carrier);
                if (fieldName == "traceparent" || fieldName == "Request-Id")
                {
                    Assert.Equal(a.Id, value);
                }
                else if (fieldName == "tracestate")
                {
                    Assert.Equal(a.TraceStateString, value);
                }
                else if (fieldName == "Correlation-Context")
                {

                    string formattedBaggage = GetFormattedBaggage(a);
                    Assert.Equal(formattedBaggage, value);
                }
                else
                {
                    Assert.False(true, $"{fieldName} Unexpected Field Name");
                }
            });

            propagator.Extract(null, (object carrier, string fieldName, out string? value) =>
            {
                Assert.Null(carrier);
                if (fieldName == "traceparent")
                {
                    value = null;
                }
                else if (fieldName == "Request-Id")
                {
                    value = a.Id;
                }
                else if (fieldName == "tracestate")
                {
                    value = a.TraceStateString;
                }
                else
                {
                    value = null;
                    Assert.False(true, $"{fieldName} Unexpected Field Name");
                }

                return true;
            },
            out string? id, out string? state);

            Assert.Equal(a.Id, id);
            Assert.Equal(a.TraceStateString, state);

            propagator.Extract(null, (object carrier, string fieldName, out string? value) =>
            {
                Assert.Null(carrier);
                if (fieldName == "baggage" || fieldName == "Correlation-Context")
                {
                    value = GetFormattedBaggage(a);
                }
                else
                {
                    value = null;
                    Assert.False(true, $"{fieldName} Unexpected Field Name");
                }
                return true;
            } , out IEnumerable<KeyValuePair<string, string?>>? baggage);

            List<KeyValuePair<string, string?>> list = new List<KeyValuePair<string, string?>>(baggage);
            Assert.Equal(1, list.Count);
            Assert.Equal(new KeyValuePair<string, string>("B1", "v1"), list[0]);

            propagator.Extract(null, (object carrier, string fieldName, out string? value) =>
            {
                Assert.Null(carrier);
                if (fieldName == "baggage" || fieldName == "Correlation-Context")
                {
                    value = $"{WebUtility.UrlEncode(" k1 ")} = {WebUtility.UrlEncode(" v1 ")},     {WebUtility.UrlEncode(" k2 ")} = {WebUtility.UrlEncode(" v2 ")}";
                }
                else
                {
                    value = null;
                    Assert.False(true, $"{fieldName} Unexpected Field Name");
                }
                return true;
            } , out baggage);

            list = new List<KeyValuePair<string, string?>>(baggage);
            Assert.Equal(2, list.Count);
            Assert.Equal(new KeyValuePair<string, string>("k2", "v2"), list[0]);
            Assert.Equal(new KeyValuePair<string, string>("k1", "v1"), list[1]);

            propagator = TextMapPropagator.CreateW3CPropagator();
            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                Assert.Null(carrier);
                if (fieldName == "traceparent")
                {
                    Assert.False(true, $"{fieldName} cannot be used with activity ");
                }
                else if (fieldName == "tracestate")
                {
                    Assert.Equal(a.TraceStateString, value);
                }
                else if (fieldName == "baggage")
                {
                    string formattedBaggage = GetFormattedBaggage(a);
                    Assert.Equal(formattedBaggage, value);
                }
                else
                {
                    Assert.False(true, $"{fieldName} Unexpected Field Name Hierarchical activity Id");
                }
            });

            a = new Activity("W3CActivity");
            a.SetIdFormat(ActivityIdFormat.W3C);
            a.SetBaggage("B2", "v2");
            a.TraceStateString = "W3CtraceState";
            a.Start();

            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                Assert.Null(carrier);
                if (fieldName == "traceparent")
                {
                    Assert.Equal(a.Id, value);
                }
                else if (fieldName == "tracestate")
                {
                    Assert.Equal(a.TraceStateString, value);
                }
                else if (fieldName == "baggage")
                {
                    string formattedBaggage = GetFormattedBaggage(a);
                    Assert.Equal(formattedBaggage, value);
                }
                else
                {
                    Assert.False(true, $"{fieldName} Unexpected Field Name Hierarchical activity Id");
                }
            });

            propagator.Extract(null, (object carrier, string fieldName, out string? value) =>
            {
                Assert.Null(carrier);
                if (fieldName == "traceparent")
                {
                    value = a.Id;
                }
                else if (fieldName == "tracestate")
                {
                    value = a.TraceStateString;
                }
                else
                {
                    value = null;
                    Assert.False(true, $"{fieldName} Unexpected Field Name");
                }
                return true;
            }, out id, out state);

            Assert.Equal(a.Id, id);
            Assert.Equal(a.TraceStateString, state);

            propagator.Extract(null, (object carrier, string fieldName, out string? value) =>
            {
                Assert.Null(carrier);
                if (fieldName == "traceparent")
                {
                    value = a.Id;
                }
                else if (fieldName == "tracestate")
                {
                    value = a.TraceStateString;
                }
                else
                {
                    value = null;
                    Assert.False(true, $"{fieldName} Unexpected Field Name");
                }
                return true;
            }, out ActivityContext context);

            Span<char> traceParent = stackalloc char[55];
            traceParent[0]  = '0';
            traceParent[1]  = '0';
            traceParent[2]  = '-';
            traceParent[35] = '-';
            traceParent[52] = '-';
            CopyStringToSpan(context.TraceId.ToHexString(), traceParent.Slice(3, 32));
            CopyStringToSpan(context.SpanId.ToHexString(),  traceParent.Slice(36, 16));
            traceParent[53] = '0';
            traceParent[54] = a.Recorded ? '1' : '0';

            Assert.Equal(a.Id, traceParent.ToString());
            Assert.Equal(a.TraceStateString, context.TraceState);

            propagator.Extract(null, (object carrier, string fieldName, out string? value) =>
            {
                Assert.Null(carrier);
                if (fieldName == "baggage")
                {
                    value = GetFormattedBaggage(a);
                }
                else
                {
                    value = null;
                    Assert.False(true, $"{fieldName} Unexpected Field Name");
                }
                return true;
            }, out baggage);

            list = new List<KeyValuePair<string, string?>>(baggage);
            Assert.Equal(2, list.Count);
            Assert.Equal(new KeyValuePair<string, string>("B1", "v1"), list[0]);
            Assert.Equal(new KeyValuePair<string, string>("B2", "v2"), list[1]);

            //
            // Test Inject Suppresssion propagator
            //
            propagator = TextMapPropagator.CreateOutputSuppressionPropagator();
            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                Assert.False(true, $"Not expected to have the seeter callback be called.");
            });

            // Extract should continue work
            propagator.Extract(null, (object carrier, string fieldName, out string? value) =>
            {
                Assert.Null(carrier);
                if (fieldName == "baggage")
                {
                    value = GetFormattedBaggage(a);
                }
                else
                {
                    value = null;
                    Assert.False(true, $"{fieldName} Unexpected Field Name");
                }
                return true;
            }, out baggage);

            list = new List<KeyValuePair<string, string?>>(baggage);
            Assert.Equal(2, list.Count);
            Assert.Equal(new KeyValuePair<string, string>("B1", "v1"), list[0]);
            Assert.Equal(new KeyValuePair<string, string>("B2", "v2"), list[1]);

            //
            // Test PassThroughPropagator
            //
            propagator = TextMapPropagator.CreatePassThroughPropagator();

            Activity.Current = null;

            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                Assert.False(true, $"Activity.Current is null. Extract shouldn't be called");
            });

            using ActivitySource source = new ActivitySource("PropagatorTests");
            using ActivityListener listener = new ActivityListener();
            listener.ShouldListenTo = (activitySource) => object.ReferenceEquals(source, activitySource);
            listener.SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllData;
            listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData;
            ActivitySource.AddActivityListener(listener);

            ActivityContext parentContext = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded, "states");
            a = source.StartActivity("a", ActivityKind.Client, parentContext);
            a.AddBaggage("B1", "v1");

            propagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                if (fieldName == "traceparent")
                {
                    Assert.Equal(ToString(parentContext), value);
                }
                else if (fieldName == "tracestate")
                {
                    Assert.Equal(parentContext.TraceState, value);
                }
                else if (fieldName == "Correlation-Context")
                {
                    Assert.Equal(GetFormattedBaggage(a), value);
                }
                else
                {
                    Assert.False(true, $"{fieldName} Unexpected Field Name");
                }
            });

            Activity b = source.StartActivity("b");
            b.AddBaggage("B2", "v2");

            propagator.Inject(b, null, (object carrier, string fieldName, string value) =>
            {
                if (fieldName == "traceparent")
                {
                    Assert.Equal(ToString(parentContext), value);
                }
                else if (fieldName == "tracestate")
                {
                    Assert.Equal(parentContext.TraceState, value);
                }
                else if (fieldName == "Correlation-Context")
                {
                    Assert.Equal(GetFormattedBaggage(a), value);
                }
                else
                {
                    Assert.False(true, $"{fieldName} Unexpected Field Name");
                }
            });

            Activity.Current = null;
            a = new Activity("SomeActivity");
            a.SetIdFormat(ActivityIdFormat.Hierarchical);
            a.SetBaggage("B1H", "v1H");
            a.TraceStateString = "traceStateH";
            a.SetParentId("HierarchicalH");
            a.Start();

            propagator.Inject(b, null, (object carrier, string fieldName, string value) =>
            {
                if (fieldName == "Request-Id")
                {
                    Assert.Equal(a.ParentId, value);
                }
                else if (fieldName == "tracestate")
                {
                    Assert.Equal(a.TraceStateString, value);
                }
                else if (fieldName == "Correlation-Context")
                {
                    Assert.Equal(GetFormattedBaggage(a), value);
                }
                else
                {
                    Assert.False(true, $"{fieldName} Unexpected Field Name");
                }
            });

            b = new Activity("SomeActivityb");
            b.SetIdFormat(ActivityIdFormat.Hierarchical);
            b.SetBaggage("B2H", "v2H");
            b.TraceStateString = "traceStateW";
            b.Start();

            string s1 = Activity.Current.Parent?.OperationName ?? "null";

            propagator.Inject(b, null, (object carrier, string fieldName, string value) =>
            {
                if (fieldName == "Request-Id")
                {
                    Assert.Equal(a.ParentId, value);
                }
                else if (fieldName == "tracestate")
                {
                    Assert.Equal(a.TraceStateString, value);
                }
                else if (fieldName == "Correlation-Context")
                {
                    Assert.Equal(GetFormattedBaggage(a), value);
                }
                else
                {
                    Assert.False(true, $"{fieldName} Unexpected Field Name with value {value}");
                }
            });
        }
    }
}
