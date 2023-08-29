// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Extensions.SourceGeneration.Configuration.Binder.Tests
{
    public partial class ConfigurationBinderTests : ConfigurationBinderTestsBase
    {
        /// <summary>
        /// This is a regression test for https://github.com/dotnet/runtime/issues/90851.
        /// It asserts that the configuration binding source generator properly formats
        /// binding invocation source locations that the generated interceptors replace.
        /// A location issue that's surfaced is emitting the right location of invocations
        /// that are on a different line than the containing binder type or the static
        /// extension binder class (e.g. ConfigurationBinder.Bind).
        /// </summary>
        [Fact]
        public void TestBindingInvocationsWithIrregularCSharpSyntax()
        {
            IConfiguration configuration = TestHelpers.GetConfigurationFromJsonString(@"{""Longitude"":1,""Latitude"":2}");

            // Tests a binding invocation variant that's on a separate line from source configuration.

            GeolocationRecord record = (GeolocationRecord)configuration
                .Get(typeof(GeolocationRecord), _ => { });

            Verify();

            // Tests generic binding invocation variants with irregular C# syntax, interspersed with white space.

            record = configuration.Get<
                GeolocationRecord
                >();

            record = configuration.Get
                <GeolocationRecord>();
            Verify();

            // Tests binding invocation variants that are on a separate line from source configuration.

            TestHelpers
                .GetConfigurationFromJsonString(@"{""Longitude"":3,""Latitude"":4}")
                .Bind(record);
            Verify(3, 4);

            int lat = configuration
                .GetValue<int>("Latitude");
            Assert.Equal(2, lat);

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

            // Tests binding invocation variants with static method call syntax, interspaced with whitespace.
            ConfigurationBinder
                .Bind(configuration, record);
            Verify();

            ConfigurationBinder.Bind(
                configuration, record);
            Verify();

            ConfigurationBinder.
                Bind(configuration
                , record)
                ;
            Verify();

            void Verify(int longitude = 1, int latitude = 2)
            {
                Assert.Equal(longitude, record.Longitude);
                Assert.Equal(latitude, record.Latitude);
            }
        }
    }
}
