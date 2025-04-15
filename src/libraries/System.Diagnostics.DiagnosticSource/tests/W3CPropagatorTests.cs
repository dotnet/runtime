// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class W3CPropagatorTests
    {
        private static DistributedContextPropagator s_w3cPropagator = DistributedContextPropagator.CreateDefaultPropagator();

        [Fact]
        public void TestW3CPropagatorBasics()
        {
            Assert.NotNull(s_w3cPropagator);
            Assert.Equal(s_w3cPropagator, DistributedContextPropagator.CreateDefaultPropagator());
            Assert.Equal(s_w3cPropagator, DistributedContextPropagator.Current);
            Assert.Equal(new[] { PropagatorTests.TraceParent, PropagatorTests.TraceState, PropagatorTests.Baggage, PropagatorTests.CorrelationContext }, s_w3cPropagator.Fields);
        }

        public static IEnumerable<object[]> W3CTestData()
        {
            // TraceState, Expected TraceState, input baggage, expected baggage string, expected baggage

            // Simple case
            yield return new object[] { "state=1", "state=1", new[] { new KeyValuePair<string, string>("b1", "v1") }, "b1 = v1", new[] { new KeyValuePair<string, string>("b1", "v1") } };

            // Invalid trace state
            yield return new object[] { "PassThroughW3CState=1", null, null, null, null }; // trace state key has to be lowercase

            // Invalid trace state
            yield return new object[] { "1start=1", null, null, null, null }; // trace state key has to start with lowercase

            // Tabs is not allowed in trace state values. use only the valid entry
            yield return new object[] { "start=1, end=\t1", "start=1", null, null, null }; // trace state key has to start with lowercase

            // multiple trace states
            yield return new object[] { "start=1, end=1", "start=1, end=1", null, null, null }; // trace state key has to start with lowercase

            // trace state longer than the max limit
            yield return new object[] { $"{new string('a', 255)}=1", null, null, null, null }; // trace state length max is 256

            // trace state equal the max
            yield return new object[] { $"{new string('a', 254)}=1", $"{new string('a', 254)}=1", null, null, null }; // trace state length max is 256

            // Invalid baggage key.
            yield return new object[] { null, null, new[]
                                                    {
                                                        new KeyValuePair<string, string>("b 1", "v1"),    // Space is not allowed
                                                        new KeyValuePair<string, string>("b=2", "v2"),    // '=' is not allowed
                                                        new KeyValuePair<string, string>("b\t3", "v3"),   // Tab is not allowed
                                                        new KeyValuePair<string, string>("b(4", "v4"),    // '(' is not allowed
                                                        new KeyValuePair<string, string>("b:5", "v5"),    // ':' is not allowed
                                                        new KeyValuePair<string, string>("b;6", "v6"),    // ';' is not allowed
                                                        new KeyValuePair<string, string>("b/7", "v7"),    // '/' is not allowed
                                                        new KeyValuePair<string, string>("b\\8", "v8"),   // '\' is not allowed
                                                        new KeyValuePair<string, string>("b,9", "v9"),    // ',' is not allowed
                                                        new KeyValuePair<string, string>("b<10", "v10"),  // '<' is not allowed
                                                        new KeyValuePair<string, string>("b>11", "v11"),  // '>' is not allowed
                                                        new KeyValuePair<string, string>("b?12", "v12"),  // '?' is not allowed
                                                        new KeyValuePair<string, string>("b@13", "v13"),  // '@' is not allowed
                                                        new KeyValuePair<string, string>("b[14", "v14"),  // '[' is not allowed
                                                        new KeyValuePair<string, string>("b]15", "v15"),  // ']' is not allowed
                                                        new KeyValuePair<string, string>("b{16", "v16"),  // '{' is not allowed
                                                        new KeyValuePair<string, string>("b}17", "v17"),  // '}' is not allowed
                                                    },
                                        null, null };

            // Mixed valid and invalid baggage entries
            yield return new object[] { null, null, new[]
                                                    {
                                                        new KeyValuePair<string, string>("b 1", "v1"), // invalid key, entry should be ignored
                                                        new KeyValuePair<string, string>("b2", "v2")   // valid entry should be encoded/decoded
                                                    },
                                                    "b2 = v2",
                                                    new[] { new KeyValuePair<string, string>("b2", "v2") } };

            // Baggage Value containing non % escaped characters
            // baggage-octet =  %x21 / %x23-2B / %x2D-3A / %x3C-5B / %x5D-7E (Exclusing '%')
            yield return new object[] { null, null, new[] { new KeyValuePair<string, string>("key", "!#$&'()*+-/059:<@>[~]^_amxBNW") },
                                                    "key = !#$&'()*+-/059:<@>[~]^_amxBNW",
                                                    new[] { new KeyValuePair<string, string>("key", "!#$&'()*+-/059:<@>[~]^_amxBNW") } };

            // Baggage Value containing `%` and space that should get escaped
            yield return new object[] { null, null, new[] { new KeyValuePair<string, string>("key", "% \"") },
                                                    "key = %25%20%22",
                                                    new[] { new KeyValuePair<string, string>("key", "% \"") } };

            // Baggage Value containing non ascii characters that should get escaped.
            // 'à' encode to 2 bytes C3 A0. `€` encode to 3 bytes E2 82 AC. `😀` encode to 4 bytes F0 9F 98 80
            yield return new object[] { null, null, new[] { new KeyValuePair<string, string>("key", "à€😀") },
                                                    "key = %C3%A0%E2%82%AC%F0%9F%98%80",
                                                    new[] { new KeyValuePair<string, string>("key", "à€😀") } };

            // Baggage Value containing invalid UTF-16 sequence. Two high surrogate characters.
            yield return new object[] { null, null, new[] { new KeyValuePair<string, string>("key", "\uD800\uD800") },
                                                    "key = %EF%BF%BD%EF%BF%BD",
                                                    new[] { new KeyValuePair<string, string>("key", "\uFFFD\uFFFD") } };
        }

        [Theory]
        [MemberData(nameof(W3CTestData))]
        public void W3CPropagatorValidationTests(string traceState, string? expectedTraceState, IEnumerable<KeyValuePair<string, string>> baggage,
                                        string expectedBaggageString, IEnumerable<KeyValuePair<string, string>> expectedBaggage)
        {
            using Activity a = PropagatorTests.CreateW3CActivity("W3CTest", traceState, baggage);

            s_w3cPropagator.Inject(a, null, (object carrier, string fieldName, string value) =>
            {
                if (fieldName == PropagatorTests.TraceParent)
                {
                    Assert.Equal(a.Id, value);
                    return;
                }

                if (fieldName == PropagatorTests.TraceState)
                {
                    Assert.Equal(expectedTraceState, value);
                    return;
                }

                if (fieldName == PropagatorTests.Baggage)
                {
                    Assert.Equal(expectedBaggageString, value);
                    return;
                }

                Assert.Fail($"Encountered wrong header name '{fieldName}' in the W3C Propagator");
            });

            s_w3cPropagator.ExtractTraceIdAndState(null, (object carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
            {
                fieldValues = null;
                fieldValue = null;

                if (fieldName == PropagatorTests.TraceParent)
                {
                    fieldValue = Activity.Current.Id;
                    return;
                }

                if (fieldName == PropagatorTests.TraceState)
                {
                    fieldValue = Activity.Current.TraceStateString;
                    return;
                }

                Assert.Fail($"Encountered wrong header name '{fieldName}' in the W3C propagator");
            }, out string? traceId, out string? state);

            Assert.Equal(Activity.Current.Id, traceId);
            Assert.Equal(expectedTraceState, state);

            IEnumerable<KeyValuePair<string, string?>>? extractedBaggage = s_w3cPropagator.ExtractBaggage(null, (object carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
            {
                Assert.Null(carrier);
                fieldValue = null;
                fieldValues = null;

                if (fieldName == PropagatorTests.Baggage || fieldName == PropagatorTests.CorrelationContext)
                {
                    fieldValue = expectedBaggageString;
                    return;
                }

                Assert.Fail($"Encountered wrong header name '{fieldName}' in W3C propagator");
            });

            Assert.Equal(expectedBaggage, extractedBaggage);

            // Keep the activity alive till the end of the test
            Assert.Equal("W3CTest", a.OperationName);
        }

        [Fact]
        public void TestExtractingBaggageWithCorrelationContextHeader()
        {
            IEnumerable<KeyValuePair<string, string?>>? extractedBaggage = s_w3cPropagator.ExtractBaggage(null, (object carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
            {
                Assert.Null(carrier);
                fieldValue = null;
                fieldValues = null;

                if (fieldName == PropagatorTests.Baggage)
                {
                    return;
                }

                if (fieldName == PropagatorTests.CorrelationContext)
                {
                    fieldValue = "key1=value1,key2=value2,key3=value3";
                    return;
                }

                Assert.Fail($"Encountered wrong header name '{fieldName}' in W3C baggage propagatoration");
            });

            Assert.Equal(new[] { new KeyValuePair<string, string?>("key1", "value1"), new KeyValuePair<string, string?>("key2", "value2"), new KeyValuePair<string, string?>("key3", "value3") }, extractedBaggage);
        }

        //
        // Tests ported from https://github.com/w3c/baggage/blob/main/test/test_baggage.py
        //

        public static IEnumerable<object[]> W3CDecodeData()
        {
            // input to decode, decoded baggage entries

            // Double equals
            yield return new object[] { "key==value", new[] { new KeyValuePair<string, string?>("key", "=value") },  "key = =value" };

            yield return new object[] { "SomeKey=SomeValue,SomeKey2=SomeValue2", new[] { new KeyValuePair<string, string?>("SomeKey", "SomeValue"), new KeyValuePair<string, string?>("SomeKey2", "SomeValue2") }, "SomeKey2 = SomeValue2, SomeKey = SomeValue" };

            yield return new object[] { "SomeKey \t = \t SomeValue \t , \t SomeKey2 \t = \t SomeValue2 \t", new[] { new KeyValuePair<string, string?>("SomeKey", "SomeValue"), new KeyValuePair<string, string?>("SomeKey2", "SomeValue2") }, "SomeKey2 = SomeValue2, SomeKey = SomeValue" };

            yield return new object[] { "SomeKey=SomeValue=equals", new[] { new KeyValuePair<string, string?>("SomeKey", "SomeValue=equals") }, "SomeKey = SomeValue=equals" };

            yield return new object[] { "SomeKey=%09%20%22%27%3B%3Dasdf%21%40%23%24%25%5E%26%2A%28%29", new[] { new KeyValuePair<string, string?>("SomeKey", "\t \"\';=asdf!@#$%^&*()") }, "SomeKey = %22'%3B=asdf!@#$%25^&*()" };

            yield return new object[] { "SomeKey \t = \t \t \"\';=asdf!@#$%^&*()\t", null, null }; // '%' should be escaped make the value invalid

            yield return new object[] { "SomeKey \t = \t \t \"\';=asdf!@#$%25^&*()\t", new[] { new KeyValuePair<string, string?>("SomeKey", "\"\';=asdf!@#$%^&*()") }, "SomeKey = %22'%3B=asdf!@#$%25^&*()" };
        }

        [Theory]
        [MemberData(nameof(W3CDecodeData))]
        public void TestDecoding(string input, IEnumerable<KeyValuePair<string, string?>> expected, string encodedBack)
        {
            IEnumerable<KeyValuePair<string, string?>>? baggage = DecodeBaggage(input);
            Assert.Equal(expected, baggage);
            Assert.Equal(encodedBack, baggage is null ? null : EncodeBaggage(baggage));
        }

        [Theory]
        [InlineData("invalid")] // Missing equals sign
        [InlineData("=value")] // Empty key
        [InlineData("key=")] // Empty value (allowed)
        public void TestInvalidBaggage(string input)
        {
            IEnumerable<KeyValuePair<string, string>>? baggage = DecodeBaggage(input);
            Assert.Null(baggage);
        }

        [Fact]
        public void TestBaggagePropagationLimits()
        {
            const string CommaSpace = ", ";
            //
            // Test MaxBaggageEntriesToEmit
            //

            const int MaxBaggageEntriesToEmit = 64; // the max limit is 64 entries
            List<KeyValuePair<string, string>> baggageEntries = new List<KeyValuePair<string, string>>();
            string expectedBaggageString = string.Empty;
            for (int i = 0; i < MaxBaggageEntriesToEmit + 1; i++)
            {
                if (i < MaxBaggageEntriesToEmit)
                {
                    expectedBaggageString += $"key{MaxBaggageEntriesToEmit - i} = value{MaxBaggageEntriesToEmit - i}";
                    if (i < MaxBaggageEntriesToEmit - 1)
                    {
                        expectedBaggageString += CommaSpace;
                    }
                }

                baggageEntries.Add(new KeyValuePair<string, string>($"key{i}", $"value{i}"));
            }

            string encodedValue = EncodeBaggage(baggageEntries);
            Assert.Equal(expectedBaggageString, encodedValue);

            IEnumerable<KeyValuePair<string, string?>>? decodedBaggage = DecodeBaggage(encodedValue);
            Assert.Equal(MaxBaggageEntriesToEmit, decodedBaggage.Count());

            //
            // Test MaxBaggageEncodedLength
            //

            const int MaxBaggageEncodedLength = 8192; // the max limit is 8192 characters
            baggageEntries.Clear();
            expectedBaggageString = string.Empty;
            int length = 0;

            // long string to create baggage entries less than 64 entries to test the max length
            const string longString = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            int entryCount = 0;

            for (int i = 0; ; i++)
            {
                string entry = $"{longString}{i} = {longString}{i}";

                if (length + entry.Length + CommaSpace.Length <= MaxBaggageEncodedLength)
                {
                    expectedBaggageString += i == 0 ? entry : CommaSpace + entry;
                    entryCount++;
                }

                baggageEntries.Insert(0, new KeyValuePair<string, string>($"{longString}{i}", $"{longString}{i}"));
                length += i == 0 ? entry.Length : CommaSpace.Length + entry.Length;
                if (length > MaxBaggageEncodedLength)
                {
                    // Now we have exceeded the limit, so we stop adding more entries.
                    break;
                }
            }

            encodedValue = EncodeBaggage(baggageEntries);
            Assert.Equal(expectedBaggageString, encodedValue);

            decodedBaggage = DecodeBaggage(encodedValue);
            Assert.Equal(entryCount, decodedBaggage.Count());
        }

        private static string? EncodeBaggage(IEnumerable<KeyValuePair<string, string>> baggageEntries)
        {
            Activity? current = Activity.Current;
            Activity.Current = null;

            Activity activity = new Activity("W3CBaggageEncoding");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();

            try
            {
                foreach (var entry in baggageEntries)
                {
                    activity.AddBaggage(entry.Key, entry.Value);
                }

                string? encodedValue = null;

                W3CPropagatorTests.s_w3cPropagator.Inject(activity, null, (object carrier, string fieldName, string value) =>
                {
                    if (fieldName == PropagatorTests.Baggage)
                    {
                        encodedValue = value;
                    }
                });

                return encodedValue;
            }
            finally
            {
                activity.Stop();
                // Restore the current activity
                Activity.Current = current;
            }
        }

        private static IEnumerable<KeyValuePair<string, string?>> DecodeBaggage(string value)
        {
            return W3CPropagatorTests.s_w3cPropagator.ExtractBaggage(null,
                (object carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
                {
                    Assert.Null(carrier);
                    fieldValue = null;
                    fieldValues = null;

                    if (fieldName == PropagatorTests.Baggage)
                    {
                        fieldValue = value;
                    }
                });
        }
    }
}

