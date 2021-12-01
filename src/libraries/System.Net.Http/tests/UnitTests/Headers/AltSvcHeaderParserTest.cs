// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http.Headers;
using Xunit;

namespace System.Net.Http.Tests
{
    public class AltSvcHeaderParserTest
    {
        [Theory]
        [MemberData(nameof(SuccessfulParseData))]
        public void TryParse_Success(string value, object[] expectedServicesObj)
        {
            var expectedServices = (AltSvcHeaderValue[])expectedServicesObj;

            HttpHeaderParser parser = AltSvcHeaderParser.Parser;

            int parseIdx = 0;
            int expectedIdx = 0;

            while (parseIdx < value.Length)
            {
                Assert.True(parser.TryParseValue(value, null, ref parseIdx, out object result));
                AltSvcHeaderValue actual = Assert.IsType<AltSvcHeaderValue>(result);

                Assert.True(expectedIdx != expectedServices.Length, "More services than expected.");
                AltSvcHeaderValue expected = expectedServices[expectedIdx++];

                Assert.Equal(expected.AlpnProtocolName, actual.AlpnProtocolName);
                Assert.Equal(expected.Host, actual.Host);
                Assert.Equal(expected.Port, actual.Port);
                Assert.Equal(expected.MaxAge, actual.MaxAge);
                Assert.Equal(expected.Persist, actual.Persist);
            }

            Assert.Equal(expectedServices.Length, expectedIdx);
        }

        public static IEnumerable<object[]> SuccessfulParseData()
        {
            TimeSpan defaultAge = TimeSpan.FromTicks(AltSvcHeaderParser.DefaultMaxAgeTicks);

            // Example from RFC 7838, Section 3: change of port.
            yield return new object[]
            {
                "h2=\":8000\"", new []
                {
                    new AltSvcHeaderValue("h2", host: null, port: 8000, defaultAge, persist: false)
                }
            };

            // Example from RFC 7838, Section 3: change of host/port.
            yield return new object[]
            {
                "h2=\"new.example.org:80\"", new []
                {
                    new AltSvcHeaderValue("h2", "new.example.org", port: 80, defaultAge, persist: false)
                }
            };

            // Example from RFC 7838, Section 3: multiple services in one line.
            yield return new object[]
            {
                "h2=\"alt.example.com:8000\", h2=\":443\"", new []
                {
                    new AltSvcHeaderValue("h2", "alt.example.com", port: 8000, defaultAge, persist: false),
                    new AltSvcHeaderValue("h2", host: null, port: 443, defaultAge, persist: false)
                }
            };

            // Example from RFC 7838, Section 3.1: change of port with max age.
            yield return new object[]
            {
                "h2=\":443\"; ma=3600", new []
                {
                    new AltSvcHeaderValue("h2", host: null, port: 443, TimeSpan.FromTicks(TimeSpan.TicksPerSecond * 3600), persist: false)
                }
            };

            // Example from RFC 7838, Section 3.1: change of port with max age and persist.
            yield return new object[]
            {
                "h2=\":443\"; ma=2592000; persist=1", new []
                {
                    new AltSvcHeaderValue("h2", host: null, port: 443, TimeSpan.FromTicks(TimeSpan.TicksPerSecond * 2592000), persist: true)
                }
            };

            // "clear".
            yield return new object[]
            {
                "clear", new []
                {
                    AltSvcHeaderValue.Clear
                }
            };
        }
    }
}
