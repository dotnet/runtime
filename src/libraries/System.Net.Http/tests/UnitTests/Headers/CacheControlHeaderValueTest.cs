// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Http.Tests
{
    public class CacheControlHeaderValueTest
    {
        [Fact]
        public void Properties_SetAndGetAllProperties_SetValueReturnedInGetter()
        {
            CacheControlHeaderValue cacheControl = new CacheControlHeaderValue();

            // Bool properties
            cacheControl.NoCache = true;
            Assert.True(cacheControl.NoCache);
            cacheControl.NoStore = true;
            Assert.True(cacheControl.NoStore);
            cacheControl.MaxStale = true;
            Assert.True(cacheControl.MaxStale);
            cacheControl.NoTransform = true;
            Assert.True(cacheControl.NoTransform);
            cacheControl.OnlyIfCached = true;
            Assert.True(cacheControl.OnlyIfCached);
            cacheControl.Public = true;
            Assert.True(cacheControl.Public);
            cacheControl.Private = true;
            Assert.True(cacheControl.Private);
            cacheControl.MustRevalidate = true;
            Assert.True(cacheControl.MustRevalidate);
            cacheControl.ProxyRevalidate = true;
            Assert.True(cacheControl.ProxyRevalidate);

            // TimeSpan properties
            TimeSpan timeSpan = new TimeSpan(1, 2, 3);
            cacheControl.MaxAge = timeSpan;
            Assert.Equal(timeSpan, cacheControl.MaxAge);
            cacheControl.SharedMaxAge = timeSpan;
            Assert.Equal(timeSpan, cacheControl.SharedMaxAge);
            cacheControl.MaxStaleLimit = timeSpan;
            Assert.Equal(timeSpan, cacheControl.MaxStaleLimit);
            cacheControl.MinFresh = timeSpan;
            Assert.Equal(timeSpan, cacheControl.MinFresh);

            // String collection properties
            Assert.NotNull(cacheControl.NoCacheHeaders);
            AssertExtensions.Throws<ArgumentException>("item", () => { cacheControl.NoCacheHeaders.Add(null); });
            Assert.Throws<FormatException>(() => { cacheControl.NoCacheHeaders.Add("invalid token"); });
            cacheControl.NoCacheHeaders.Add("token");
            Assert.Equal(1, cacheControl.NoCacheHeaders.Count);
            Assert.Equal("token", cacheControl.NoCacheHeaders.First());

            Assert.NotNull(cacheControl.PrivateHeaders);
            AssertExtensions.Throws<ArgumentException>("item", () => { cacheControl.PrivateHeaders.Add(null); });
            Assert.Throws<FormatException>(() => { cacheControl.PrivateHeaders.Add("invalid token"); });
            cacheControl.PrivateHeaders.Add("token");
            Assert.Equal(1, cacheControl.PrivateHeaders.Count);
            Assert.Equal("token", cacheControl.PrivateHeaders.First());

            // NameValueHeaderValue collection property
            Assert.NotNull(cacheControl.Extensions);
            Assert.Throws<ArgumentNullException>(() => { cacheControl.Extensions.Add(null); });
            cacheControl.Extensions.Add(new NameValueHeaderValue("name", "value"));
            Assert.Equal(1, cacheControl.Extensions.Count);
            Assert.Equal(new NameValueHeaderValue("name", "value"), cacheControl.Extensions.First());
        }

        [Fact]
        public void ToString_UseRequestDirectiveValues_AllSerializedCorrectly()
        {
            CacheControlHeaderValue cacheControl = new CacheControlHeaderValue();
            Assert.Equal("", cacheControl.ToString());

            // Note that we allow all combinations of all properties even though the RFC specifies rules what value
            // can be used together.
            // Also for property pairs (bool property + collection property) like 'NoCache' and 'NoCacheHeaders' the
            // caller needs to set the bool property in order for the collection to be populated as string.

            // Cache Request Directive sample
            cacheControl.NoStore = true;
            Assert.Equal("no-store", cacheControl.ToString());
            cacheControl.NoCache = true;
            Assert.Equal("no-store, no-cache", cacheControl.ToString());
            cacheControl.MaxAge = new TimeSpan(0, 1, 10);
            Assert.Equal("no-store, no-cache, max-age=70", cacheControl.ToString());
            cacheControl.MaxStale = true;
            Assert.Equal("no-store, no-cache, max-age=70, max-stale", cacheControl.ToString());
            cacheControl.MaxStaleLimit = new TimeSpan(0, 2, 5);
            Assert.Equal("no-store, no-cache, max-age=70, max-stale=125", cacheControl.ToString());
            cacheControl.MinFresh = new TimeSpan(0, 3, 0);
            Assert.Equal("no-store, no-cache, max-age=70, max-stale=125, min-fresh=180", cacheControl.ToString());

            cacheControl = new CacheControlHeaderValue();
            cacheControl.NoTransform = true;
            Assert.Equal("no-transform", cacheControl.ToString());
            cacheControl.OnlyIfCached = true;
            Assert.Equal("no-transform, only-if-cached", cacheControl.ToString());
            cacheControl.Extensions.Add(new NameValueHeaderValue("custom"));
            cacheControl.Extensions.Add(new NameValueHeaderValue("customName", "customValue"));
            Assert.Equal("no-transform, only-if-cached, custom, customName=customValue", cacheControl.ToString());

            cacheControl = new CacheControlHeaderValue();
            cacheControl.Extensions.Add(new NameValueHeaderValue("custom"));
            Assert.Equal("custom", cacheControl.ToString());
        }

        [Fact]
        public void ToString_UseResponseDirectiveValues_AllSerializedCorrectly()
        {
            CacheControlHeaderValue cacheControl = new CacheControlHeaderValue();
            Assert.Equal("", cacheControl.ToString());

            cacheControl.NoCache = true;
            Assert.Equal("no-cache", cacheControl.ToString());
            cacheControl.NoCacheHeaders.Add("token1");
            Assert.Equal("no-cache=\"token1\"", cacheControl.ToString());
            cacheControl.Public = true;
            Assert.Equal("public, no-cache=\"token1\"", cacheControl.ToString());

            cacheControl = new CacheControlHeaderValue();
            cacheControl.Private = true;
            Assert.Equal("private", cacheControl.ToString());
            cacheControl.PrivateHeaders.Add("token2");
            cacheControl.PrivateHeaders.Add("token3");
            Assert.Equal("private=\"token2, token3\"", cacheControl.ToString());
            cacheControl.MustRevalidate = true;
            Assert.Equal("must-revalidate, private=\"token2, token3\"", cacheControl.ToString());
            cacheControl.ProxyRevalidate = true;
            Assert.Equal("must-revalidate, proxy-revalidate, private=\"token2, token3\"", cacheControl.ToString());
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ToString_NegativeValues_UsesMinusSignRegardlessOfCurrentCulture()
        {
            RemoteExecutor.Invoke(() =>
            {
                var cacheControl = new CacheControlHeaderValue()
                {
                    MaxAge = new TimeSpan(0, 0, -1),
                    MaxStale = true,
                    MaxStaleLimit = new TimeSpan(0, 0, -2),
                    MinFresh = new TimeSpan(0, 0, -3),
                    SharedMaxAge = new TimeSpan(0, 0, -4)
                };

                var ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                ci.NumberFormat.NegativeSign = "n";
                CultureInfo.CurrentCulture = ci;

                Assert.Equal("max-age=-1, s-maxage=-4, max-stale=-2, min-fresh=-3", cacheControl.ToString());
            }).Dispose();
        }

        [Fact]
        public void GetHashCode_CompareValuesWithBoolFieldsSet_MatchExpectation()
        {
            // Verify that different bool fields return different hash values.
            CacheControlHeaderValue[] values = new CacheControlHeaderValue[9];

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = new CacheControlHeaderValue();
            }

            values[0].ProxyRevalidate = true;
            values[1].NoCache = true;
            values[2].NoStore = true;
            values[3].MaxStale = true;
            values[4].NoTransform = true;
            values[5].OnlyIfCached = true;
            values[6].Public = true;
            values[7].Private = true;
            values[8].MustRevalidate = true;

            // Only one bool field set. All hash codes should differ
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    if (i != j)
                    {
                        CompareHashCodes(values[i], values[j], false);
                    }
                }
            }

            // Validate that two instances with the same bool fields set are equal.
            values[0].NoCache = true;
            CompareHashCodes(values[0], values[1], false);
            values[1].ProxyRevalidate = true;
            CompareHashCodes(values[0], values[1], true);
        }

        [Fact]
        public void GetHashCode_CompareValuesWithTimeSpanFieldsSet_MatchExpectation()
        {
            // Verify that different timespan fields return different hash values.
            CacheControlHeaderValue[] values = new CacheControlHeaderValue[4];

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = new CacheControlHeaderValue();
            }

            values[0].MaxAge = new TimeSpan(0, 1, 1);
            values[1].MaxStaleLimit = new TimeSpan(0, 1, 1);
            values[2].MinFresh = new TimeSpan(0, 1, 1);
            values[3].SharedMaxAge = new TimeSpan(0, 1, 1);

            // Only one timespan field set. All hash codes should differ
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    if (i != j)
                    {
                        CompareHashCodes(values[i], values[j], false);
                    }
                }
            }

            values[0].MaxStaleLimit = new TimeSpan(0, 1, 2);
            CompareHashCodes(values[0], values[1], false);

            values[1].MaxAge = new TimeSpan(0, 1, 1);
            values[1].MaxStaleLimit = new TimeSpan(0, 1, 2);
            CompareHashCodes(values[0], values[1], true);
        }

        [Fact]
        public void GetHashCode_CompareCollectionFieldsSet_MatchExpectation()
        {
            CacheControlHeaderValue cacheControl1 = new CacheControlHeaderValue();
            CacheControlHeaderValue cacheControl2 = new CacheControlHeaderValue();
            CacheControlHeaderValue cacheControl3 = new CacheControlHeaderValue();
            CacheControlHeaderValue cacheControl4 = new CacheControlHeaderValue();
            CacheControlHeaderValue cacheControl5 = new CacheControlHeaderValue();

            cacheControl1.NoCache = true;
            cacheControl1.NoCacheHeaders.Add("token2");

            cacheControl2.NoCache = true;
            cacheControl2.NoCacheHeaders.Add("token1");
            cacheControl2.NoCacheHeaders.Add("token2");

            CompareHashCodes(cacheControl1, cacheControl2, false);

            cacheControl1.NoCacheHeaders.Add("token1");
            CompareHashCodes(cacheControl1, cacheControl2, true);

            // Since NoCache and Private generate different hash codes, even if NoCacheHeaders and PrivateHeaders
            // have the same values, the hash code will be different.
            cacheControl3.Private = true;
            cacheControl3.PrivateHeaders.Add("token2");
            CompareHashCodes(cacheControl1, cacheControl3, false);


            cacheControl4.Extensions.Add(new NameValueHeaderValue("custom"));
            CompareHashCodes(cacheControl1, cacheControl4, false);

            cacheControl5.Extensions.Add(new NameValueHeaderValue("customN", "customV"));
            cacheControl5.Extensions.Add(new NameValueHeaderValue("custom"));
            CompareHashCodes(cacheControl4, cacheControl5, false);

            cacheControl4.Extensions.Add(new NameValueHeaderValue("customN", "customV"));
            CompareHashCodes(cacheControl4, cacheControl5, true);
        }

        [Fact]
        public void Equals_CompareValuesWithBoolFieldsSet_MatchExpectation()
        {
            // Verify that different bool fields return different hash values.
            CacheControlHeaderValue[] values = new CacheControlHeaderValue[9];

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = new CacheControlHeaderValue();
            }

            values[0].ProxyRevalidate = true;
            values[1].NoCache = true;
            values[2].NoStore = true;
            values[3].MaxStale = true;
            values[4].NoTransform = true;
            values[5].OnlyIfCached = true;
            values[6].Public = true;
            values[7].Private = true;
            values[8].MustRevalidate = true;

            // Only one bool field set. All hash codes should differ
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    if (i != j)
                    {
                        CompareValues(values[i], values[j], false);
                    }
                }
            }

            // Validate that two instances with the same bool fields set are equal.
            values[0].NoCache = true;
            CompareValues(values[0], values[1], false);
            values[1].ProxyRevalidate = true;
            CompareValues(values[0], values[1], true);
        }

        [Fact]
        public void Equals_CompareValuesWithTimeSpanFieldsSet_MatchExpectation()
        {
            // Verify that different timespan fields return different hash values.
            CacheControlHeaderValue[] values = new CacheControlHeaderValue[4];

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = new CacheControlHeaderValue();
            }

            values[0].MaxAge = new TimeSpan(0, 1, 1);
            values[1].MaxStaleLimit = new TimeSpan(0, 1, 1);
            values[2].MinFresh = new TimeSpan(0, 1, 1);
            values[3].SharedMaxAge = new TimeSpan(0, 1, 1);

            // Only one timespan field set. All hash codes should differ
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    if (i != j)
                    {
                        CompareValues(values[i], values[j], false);
                    }
                }
            }

            values[0].MaxStaleLimit = new TimeSpan(0, 1, 2);
            CompareValues(values[0], values[1], false);

            values[1].MaxAge = new TimeSpan(0, 1, 1);
            values[1].MaxStaleLimit = new TimeSpan(0, 1, 2);
            CompareValues(values[0], values[1], true);

            CacheControlHeaderValue value1 = new CacheControlHeaderValue();
            value1.MaxStale = true;
            CacheControlHeaderValue value2 = new CacheControlHeaderValue();
            value2.MaxStale = true;
            CompareValues(value1, value2, true);

            value1.MaxStaleLimit = new TimeSpan(1, 2, 3);
            CompareValues(value1, value2, false);

            value2.MaxStaleLimit = new TimeSpan(2, 3, 4);
            CompareValues(value1, value2, false);

            value1.MaxStaleLimit = new TimeSpan(2, 3, 4);
            CompareValues(value1, value2, true);
        }

        [Fact]
        public void Equals_CompareCollectionFieldsSet_MatchExpectation()
        {
            CacheControlHeaderValue cacheControl1 = new CacheControlHeaderValue();
            CacheControlHeaderValue cacheControl2 = new CacheControlHeaderValue();
            CacheControlHeaderValue cacheControl3 = new CacheControlHeaderValue();
            CacheControlHeaderValue cacheControl4 = new CacheControlHeaderValue();
            CacheControlHeaderValue cacheControl5 = new CacheControlHeaderValue();
            CacheControlHeaderValue cacheControl6 = new CacheControlHeaderValue();

            cacheControl1.NoCache = true;
            cacheControl1.NoCacheHeaders.Add("token2");

            Assert.False(cacheControl1.Equals(null), "Compare with 'null'");

            cacheControl2.NoCache = true;
            cacheControl2.NoCacheHeaders.Add("token1");
            cacheControl2.NoCacheHeaders.Add("token2");

            CompareValues(cacheControl1, cacheControl2, false);

            cacheControl1.NoCacheHeaders.Add("token1");
            CompareValues(cacheControl1, cacheControl2, true);

            // Since NoCache and Private generate different hash codes, even if NoCacheHeaders and PrivateHeaders
            // have the same values, the hash code will be different.
            cacheControl3.Private = true;
            cacheControl3.PrivateHeaders.Add("token2");
            CompareValues(cacheControl1, cacheControl3, false);

            cacheControl4.Private = true;
            cacheControl4.PrivateHeaders.Add("token3");
            CompareValues(cacheControl3, cacheControl4, false);

            cacheControl5.Extensions.Add(new NameValueHeaderValue("custom"));
            CompareValues(cacheControl1, cacheControl5, false);

            cacheControl6.Extensions.Add(new NameValueHeaderValue("customN", "customV"));
            cacheControl6.Extensions.Add(new NameValueHeaderValue("custom"));
            CompareValues(cacheControl5, cacheControl6, false);

            cacheControl5.Extensions.Add(new NameValueHeaderValue("customN", "customV"));
            CompareValues(cacheControl5, cacheControl6, true);
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            CacheControlHeaderValue source = new CacheControlHeaderValue();
            source.Extensions.Add(new NameValueHeaderValue("custom"));
            source.Extensions.Add(new NameValueHeaderValue("customN", "customV"));
            source.MaxAge = new TimeSpan(1, 1, 1);
            source.MaxStale = true;
            source.MaxStaleLimit = new TimeSpan(1, 1, 2);
            source.MinFresh = new TimeSpan(1, 1, 3);
            source.MustRevalidate = true;
            source.NoCache = true;
            source.NoCacheHeaders.Add("token1");
            source.NoStore = true;
            source.NoTransform = true;
            source.OnlyIfCached = true;
            source.Private = true;
            source.PrivateHeaders.Add("token2");
            source.ProxyRevalidate = true;
            source.Public = true;
            source.SharedMaxAge = new TimeSpan(1, 1, 4);
            CacheControlHeaderValue clone = (CacheControlHeaderValue)((ICloneable)source).Clone();

            Assert.Equal(source, clone);
        }

        [Fact]
        public void GetCacheControlLength_DifferentValidScenariosAndNoExistingCacheControl_AllReturnNonZero()
        {
            CacheControlHeaderValue expected = new CacheControlHeaderValue();
            expected.NoCache = true;
            CheckGetCacheControlLength("X , , no-cache ,,", 1, null, 16, expected);

            expected = new CacheControlHeaderValue();
            expected.NoCache = true;
            expected.NoCacheHeaders.Add("token1");
            expected.NoCacheHeaders.Add("token2");
            CheckGetCacheControlLength("no-cache=\"token1, token2\"", 0, null, 25, expected);

            expected = new CacheControlHeaderValue();
            expected.NoStore = true;
            expected.MaxAge = new TimeSpan(0, 0, 125);
            expected.MaxStale = true;
            CheckGetCacheControlLength("X no-store , max-age = 125, max-stale,", 1, null, 37, expected);

            expected = new CacheControlHeaderValue();
            expected.MinFresh = new TimeSpan(0, 0, 123);
            expected.NoTransform = true;
            expected.OnlyIfCached = true;
            expected.Extensions.Add(new NameValueHeaderValue("custom"));
            CheckGetCacheControlLength("min-fresh=123, no-transform, only-if-cached, custom", 0, null, 51, expected);

            expected = new CacheControlHeaderValue();
            expected.Public = true;
            expected.Private = true;
            expected.PrivateHeaders.Add("PLACEHOLDER");
            expected.MustRevalidate = true;
            expected.ProxyRevalidate = true;
            expected.Extensions.Add(new NameValueHeaderValue("c", "d"));
            expected.Extensions.Add(new NameValueHeaderValue("a", "b"));
            CheckGetCacheControlLength(",public, , private=\"PLACEHOLDER\", must-revalidate, c=d, proxy-revalidate, a=b", 0,
                null, 77, expected);

            expected = new CacheControlHeaderValue();
            expected.Private = true;
            expected.SharedMaxAge = new TimeSpan(0, 0, 1234567890);
            expected.MaxAge = new TimeSpan(0, 0, 987654321);
            CheckGetCacheControlLength("s-maxage=1234567890, private, max-age = 987654321,", 0, null, 50, expected);
        }

        [Fact]
        public void GetCacheControlLength_DifferentValidScenariosAndExistingCacheControl_AllReturnNonZero()
        {
            CacheControlHeaderValue storeValue = new CacheControlHeaderValue();
            storeValue.NoStore = true;
            CacheControlHeaderValue expected = new CacheControlHeaderValue();
            expected.NoCache = true;
            expected.NoStore = true;
            CheckGetCacheControlLength("X no-cache", 1, storeValue, 9, expected);

            storeValue = new CacheControlHeaderValue();
            storeValue.Private = true;
            storeValue.PrivateHeaders.Add("token1");
            storeValue.NoCache = true;
            expected.NoCacheHeaders.Add("token1");
            expected.NoCacheHeaders.Clear(); // just make sure we have an assigned (empty) collection.
            expected = new CacheControlHeaderValue();
            expected.Private = true;
            expected.PrivateHeaders.Add("token1");
            expected.PrivateHeaders.Add("PLACEHOLDER");
            expected.NoCache = true;
            expected.NoCacheHeaders.Add("token1");
            expected.NoCacheHeaders.Add("token2");
            CheckGetCacheControlLength("private=\"PLACEHOLDER\", no-cache=\"token1, , token2,\"", 0, storeValue, 51,
                expected);

            storeValue = new CacheControlHeaderValue();
            storeValue.Extensions.Add(new NameValueHeaderValue("x", "y"));
            storeValue.NoTransform = true;
            storeValue.OnlyIfCached = true;
            expected = new CacheControlHeaderValue();
            expected.Public = true;
            expected.Private = true;
            expected.PrivateHeaders.Add("PLACEHOLDER");
            expected.MustRevalidate = true;
            expected.ProxyRevalidate = true;
            expected.NoTransform = true;
            expected.OnlyIfCached = true;
            expected.Extensions.Add(new NameValueHeaderValue("a", "\"b\""));
            expected.Extensions.Add(new NameValueHeaderValue("c", "d"));
            expected.Extensions.Add(new NameValueHeaderValue("x", "y")); // from store result
            CheckGetCacheControlLength(",public, , private=\"PLACEHOLDER\", must-revalidate, c=d, proxy-revalidate, a=\"b\"",
                0, storeValue, 79, expected);

            storeValue = new CacheControlHeaderValue();
            storeValue.MaxStale = true;
            storeValue.MinFresh = new TimeSpan(1, 2, 3);
            expected = new CacheControlHeaderValue();
            expected.MaxStale = true;
            expected.MaxStaleLimit = new TimeSpan(0, 0, 5);
            expected.MinFresh = new TimeSpan(0, 0, 10); // note that the last header value overwrites existing ones
            CheckGetCacheControlLength("  ,,max-stale=5,,min-fresh = 10,,", 0, storeValue, 33, expected);

            storeValue = new CacheControlHeaderValue();
            storeValue.SharedMaxAge = new TimeSpan(1, 2, 3);
            storeValue.NoTransform = true;
            expected = new CacheControlHeaderValue();
            expected.SharedMaxAge = new TimeSpan(1, 2, 3);
            expected.NoTransform = true;
        }

        [Fact]
        public void GetCacheControlLength_DifferentInvalidScenarios_AllReturnZero()
        {
            // Token-only values
            CheckInvalidCacheControlLength("no-store=15", 0);
            CheckInvalidCacheControlLength("no-store=", 0);

            CheckInvalidCacheControlLength("no-transform=a", 0);
            CheckInvalidCacheControlLength("no-transform=", 0);

            CheckInvalidCacheControlLength("only-if-cached=\"x\"", 0);
            CheckInvalidCacheControlLength("only-if-cached=", 0);

            CheckInvalidCacheControlLength("public=\"x\"", 0);
            CheckInvalidCacheControlLength("public=", 0);

            CheckInvalidCacheControlLength("must-revalidate=\"1\"", 0);
            CheckInvalidCacheControlLength("must-revalidate=", 0);

            CheckInvalidCacheControlLength("proxy-revalidate=x", 0);
            CheckInvalidCacheControlLength("proxy-revalidate=", 0);

            // Token with optional field-name list
            CheckInvalidCacheControlLength("no-cache=", 0);
            CheckInvalidCacheControlLength("no-cache=token", 0);
            CheckInvalidCacheControlLength("no-cache=\"token", 0);
            CheckInvalidCacheControlLength("no-cache=\"\"", 0); // at least one token expected as value

            CheckInvalidCacheControlLength("private=", 0);
            CheckInvalidCacheControlLength("private=token", 0);
            CheckInvalidCacheControlLength("private=\"token", 0);
            CheckInvalidCacheControlLength("private=\",\"", 0); // at least one token expected as value
            CheckInvalidCacheControlLength("private=\"=\"", 0);

            // Token with delta-seconds value
            CheckInvalidCacheControlLength("max-age", 0);
            CheckInvalidCacheControlLength("max-age=", 0);
            CheckInvalidCacheControlLength("max-age=a", 0);
            CheckInvalidCacheControlLength("max-age=\"1\"", 0);
            CheckInvalidCacheControlLength("max-age=1.5", 0);

            CheckInvalidCacheControlLength("max-stale=", 0);
            CheckInvalidCacheControlLength("max-stale=a", 0);
            CheckInvalidCacheControlLength("max-stale=\"1\"", 0);
            CheckInvalidCacheControlLength("max-stale=1.5", 0);

            CheckInvalidCacheControlLength("min-fresh", 0);
            CheckInvalidCacheControlLength("min-fresh=", 0);
            CheckInvalidCacheControlLength("min-fresh=a", 0);
            CheckInvalidCacheControlLength("min-fresh=\"1\"", 0);
            CheckInvalidCacheControlLength("min-fresh=1.5", 0);

            CheckInvalidCacheControlLength("s-maxage", 0);
            CheckInvalidCacheControlLength("s-maxage=", 0);
            CheckInvalidCacheControlLength("s-maxage=a", 0);
            CheckInvalidCacheControlLength("s-maxage=\"1\"", 0);
            CheckInvalidCacheControlLength("s-maxage=1.5", 0);

            // Invalid Extension values
            CheckInvalidCacheControlLength("custom=", 0);
            CheckInvalidCacheControlLength("custom value", 0);

            CheckInvalidCacheControlLength(null, 0);
            CheckInvalidCacheControlLength("", 0);
            CheckInvalidCacheControlLength("", 1);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            // Just verify parser is implemented correctly. Don't try to test syntax parsed by CacheControlHeaderValue.
            CacheControlHeaderValue expected = new CacheControlHeaderValue();
            expected.NoStore = true;
            expected.MinFresh = new TimeSpan(0, 2, 3);
            CheckValidParse(" , no-store, min-fresh=123", expected);

            expected = new CacheControlHeaderValue();
            expected.MaxStale = true;
            expected.NoCache = true;
            expected.NoCacheHeaders.Add("t");
            CheckValidParse("max-stale, no-cache=\"t\", ,,", expected);
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse("no-cache,=", 0);
            CheckInvalidParse("max-age=123x", 0);
            CheckInvalidParse("=no-cache", 0);
            CheckInvalidParse("no-cache no-store", 0);
            CheckInvalidParse("invalid =", 0);
            CheckInvalidParse("\u4F1A", 0);
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(",")]
        [InlineData(",,")]
        [InlineData(" , , ")]
        public void CacheControlHeaderValue_EmptyValue_Parsed(string value)
        {
            Assert.NotNull(CacheControlHeaderValue.Parse(value));

            Assert.True(CacheControlHeaderValue.TryParse(value, out CacheControlHeaderValue headerValue));
            Assert.NotNull(headerValue);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            // Just verify parser is implemented correctly. Don't try to test syntax parsed by CacheControlHeaderValue.
            CacheControlHeaderValue expected = new CacheControlHeaderValue();
            expected.NoStore = true;
            expected.MinFresh = new TimeSpan(0, 2, 3);
            CheckValidTryParse(" , no-store, min-fresh=123", expected);

            expected = new CacheControlHeaderValue();
            expected.MaxStale = true;
            expected.NoCache = true;
            expected.NoCacheHeaders.Add("t");
            CheckValidTryParse("max-stale, no-cache=\"t\", ,,", expected);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidTryParse("no-cache,=", 0);
            CheckInvalidTryParse("max-age=123x", 0);
            CheckInvalidTryParse("=no-cache", 0);
            CheckInvalidTryParse("no-cache no-store", 0);
            CheckInvalidTryParse("invalid =", 0);
            CheckInvalidTryParse("\u4F1A", 0);
        }

        [Fact]
        public void TryParseAndAddRawHeaderValue_AddEmptyAfterValid_NoEmptyValuesAdded()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://microsoft.com");

            request.Headers.TryAddWithoutValidation(KnownHeaders.CacheControl.Descriptor, "min-fresh=123");
            request.Headers.TryAddWithoutValidation(KnownHeaders.CacheControl.Descriptor, string.Empty);

            Assert.True(request.Headers.TryGetValues(KnownHeaders.CacheControl.Descriptor, out IEnumerable<string>? values));
            Assert.Equal("min-fresh=123", Assert.Single(values));
        }

        #region Helper methods

        private void CompareHashCodes(CacheControlHeaderValue x, CacheControlHeaderValue y, bool areEqual)
        {
            if (areEqual)
            {
                Assert.Equal(x.GetHashCode(), y.GetHashCode());
            }
            else
            {
                Assert.NotEqual(x.GetHashCode(), y.GetHashCode());
            }
        }

        private void CompareValues(CacheControlHeaderValue x, CacheControlHeaderValue y, bool areEqual)
        {
            Assert.Equal(areEqual, x.Equals(y));
            Assert.Equal(areEqual, y.Equals(x));
        }

        private static void CheckGetCacheControlLength(string input, int startIndex, CacheControlHeaderValue storeValue,
            int expectedLength, CacheControlHeaderValue expectedResult)
        {
            CacheControlHeaderValue result = null;
            Assert.Equal(expectedLength, CacheControlHeaderValue.GetCacheControlLength(input, startIndex, storeValue, out result));

            if (storeValue == null)
            {
                Assert.Equal(expectedResult, result);
            }
            else
            {
                // If we provide a 'storeValue', then that instance will be updated and result will be 'null'
                Assert.Null(result);
                Assert.Equal(expectedResult, storeValue);
            }
        }

        private static void CheckInvalidCacheControlLength(string input, int startIndex)
        {
            CacheControlHeaderValue result = null;
            Assert.Equal(0, CacheControlHeaderValue.GetCacheControlLength(input, startIndex, null, out result));
            Assert.Null(result);
        }

        private void CheckValidParse(string input, CacheControlHeaderValue expectedResult)
        {
            CacheControlHeaderValue result = CacheControlHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParse(string input, int startIndex)
        {
            Assert.Throws<FormatException>(() => { CacheControlHeaderValue.Parse(input); });
        }

        private void CheckValidTryParse(string input, CacheControlHeaderValue expectedResult)
        {
            CacheControlHeaderValue result = null;
            Assert.True(CacheControlHeaderValue.TryParse(input, out result));
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidTryParse(string input, int startIndex)
        {
            CacheControlHeaderValue result = null;
            Assert.False(CacheControlHeaderValue.TryParse(input, out result));
            Assert.Null(result);
        }
        #endregion
    }
}
