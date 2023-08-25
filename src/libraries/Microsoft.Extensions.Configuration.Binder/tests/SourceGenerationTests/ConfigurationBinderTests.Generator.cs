// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Extensions.SourceGeneration.Configuration.Binder.Tests
{
    public partial class ConfigurationBinderTests : ConfigurationBinderTestsBase
    {
        [Fact]
        public void GeneratorHandlesInvocationsOnNewline()
        {
            IConfiguration configuration = TestHelpers.GetConfigurationFromJsonString(@"{""Longitude"":1,""Latitude"":2}");

            GeolocationRecord record = configuration.Get<
                GeolocationRecord
                >();
            Verify();

            record = (GeolocationRecord)configuration
                .Get(typeof(GeolocationRecord), _ => { });
            Verify();

            TestHelpers
                .GetConfigurationFromJsonString(@"{""Longitude"":3,""Latitude"":4}")
                .Bind(record);
            Verify(3, 4);

            int lat = configuration
                .GetValue<int>("Latitude");
            Assert.Equal(2, lat);

            record = configuration.Get
                <GeolocationRecord>();
            Verify();

            record = (GeolocationRecord)configuration
                .Get(
                typeof(GeolocationRecord), _ =>
                { });
            Verify();

            TestHelpers
                .GetConfigurationFromJsonString(@"{""Longitude"":3,
""Latitude"":4}
")
                .Bind(
                record
                );
            Verify(3, 4);

            long latLong = configuration
                .GetValue<
#if DEBUG
                int
#else
                long
#endif
                >
                ("Latitude")
                ;
            Assert.Equal(2, lat);

            record = (GeolocationRecord)configuration.
                Get(typeof(GeolocationRecord), _ => { });
            Verify();

            record = (GeolocationRecord)
                            configuration.
                            Get(typeof(GeolocationRecord), _ => { });
            Verify();

            record = (GeolocationRecord)
                            configuration
                            .Get(typeof(GeolocationRecord), _ => { });
            Verify();

            void Verify(int longitude = 1, int latitude = 2)
            {
                Assert.Equal(longitude, record.Longitude);
                Assert.Equal(latitude, record.Latitude);
            }
        }
    }
}
