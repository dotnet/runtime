// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Extensions.SourceGeneration.Configuration.Binder.Tests
{
    public partial class ConfigurationBinderTests : ConfigurationBinderTestsBase
    {
        // These are regression tests for https://github.com/dotnet/runtime/issues/90851
        // Source Generator Interceptors rely on identifying an accurate invocation
        // source location (line and character positions). These tests cover newline
        // and whitespace scenarios to ensure the interceptors get wired up correctly.

        [Fact]
        public void TestBindingInvocationsWithNewlines_GetMethodTypeArg()
        {
            IConfiguration configuration = TestHelpers.GetConfigurationFromJsonString(@"{""Longitude"":1,""Latitude"":2}");

            // Newline between the configuration instance and the binding invocation (with the dot on the first line)
            GeolocationRecord record1 = (GeolocationRecord)configuration.
                Get(typeof(GeolocationRecord), _ => { });

            AssertRecordIsBound(record1, 1, 2);

            // Newline between the configuration instance and the binding invocation (with the dot on the second line)
            GeolocationRecord record2 = (GeolocationRecord)configuration
                .Get(typeof(GeolocationRecord), _ => { });

            AssertRecordIsBound(record2, 1, 2);

            // Newlines between the instance, the invocation, and the arguments
            GeolocationRecord record3 = (GeolocationRecord)configuration
                .Get(
                    typeof(GeolocationRecord),
                    _ => { }
                );

            AssertRecordIsBound(record3, 1, 2);

            // Newlines before and after the instance (with the dot on the first line)
            GeolocationRecord record4 = (GeolocationRecord)
                configuration.
                Get(typeof(GeolocationRecord), _ => { });

            AssertRecordIsBound(record4, 1, 2);

            // Newlines before and after the instance (with the dot on the second line)
            GeolocationRecord record5 = (GeolocationRecord)
                            configuration
                            .Get(typeof(GeolocationRecord), _ => { });

            AssertRecordIsBound(record5, 1, 2);

            // Newlines in every place possible
            GeolocationRecord
                record6
                =
                (
                    GeolocationRecord
                )
                configuration
                .
                Get
                (
                    typeof
                    (
                        GeolocationRecord
                    )
                    ,
                    _
                    =>
                    {
                    }
                )
                ;

            AssertRecordIsBound(record6, 1, 2);
        }

        [Fact]
        public void TestBindingInvocationsWithNewlines_GetMethodGeneric()
        {
            IConfiguration configuration = TestHelpers.GetConfigurationFromJsonString(@"{""Longitude"":1,""Latitude"":2}");

            // Newline between the invocation method name and the generic type argument
            GeolocationRecord record1 = configuration.Get
                <GeolocationRecord>();

            AssertRecordIsBound(record1, 1, 2);

            // Newlines on either side of the generic type argument
            GeolocationRecord record2 = configuration.Get<
                GeolocationRecord
                >();

            AssertRecordIsBound(record2, 1, 2);

            // Newlines in every place possible
            GeolocationRecord
                record3
                =
                configuration
                .
                Get
                <
                GeolocationRecord
                >
                ()
                ;

            AssertRecordIsBound(record3, 1, 2);
        }

        [Fact]
        public void TestBindingInvocationsWithNewlines_BindExtensionMethod()
        {
            // Newline between the configuration instance and the extension method invocation
            GeolocationRecord record1 = new GeolocationRecord();
            TestHelpers.GetConfigurationFromJsonString(@"{""Longitude"":1,""Latitude"":2}")
                .Bind(record1);

            AssertRecordIsBound(record1, 1, 2);

            // Newlines between the method that returns the instance and the extension method invocation
            GeolocationRecord record2 = new GeolocationRecord();
            TestHelpers
                .GetConfigurationFromJsonString(@"{""Longitude"":1,""Latitude"":2}")
                .Bind(record2);

            AssertRecordIsBound(record2, 1, 2);

            // Newlines within the argument to the method returning the configuration and around the extension method argument
            GeolocationRecord record3 = new GeolocationRecord();
            TestHelpers
                .GetConfigurationFromJsonString(@"{""Longitude"":1,
                    ""Latitude"":2}
                    ")
                .Bind(
                    record3
                );

            AssertRecordIsBound(record3, 1, 2);

            // Newlines in every place possible
            GeolocationRecord record4 = new GeolocationRecord();
            TestHelpers
                .
                GetConfigurationFromJsonString
                (
                    @"{""Longitude"":1, ""Latitude"":2}"
                )
                .
                Bind
                (
                    record4
                )
                ;

            AssertRecordIsBound(record4, 1, 2);
        }

        [Fact]
        public void TestBindingInvocationsWithNewlines_BindStaticMethod()
        {
            IConfiguration configuration = TestHelpers.GetConfigurationFromJsonString(@"{""Longitude"":1,""Latitude"":2}");

            // Newline between the class and the static method invocation (with the dot on the first line)
            GeolocationRecord record1 = new GeolocationRecord();
            ConfigurationBinder.
                Bind(configuration, record1);

            // Newline between the class and the static method invocation (with the dot on the second line)
            GeolocationRecord record2 = new GeolocationRecord();
            ConfigurationBinder
                .Bind(configuration, record2);

            AssertRecordIsBound(record2, 1, 2);

            // Newline before the arguments
            GeolocationRecord record3 = new GeolocationRecord();
            ConfigurationBinder.Bind(
                configuration, record3);

            AssertRecordIsBound(record3, 1, 2);

            // Newlines in every place possible
            GeolocationRecord record4 = new GeolocationRecord();
            ConfigurationBinder
                .
                Bind
                (
                    configuration
                    ,
                    record4
                )
                ;

            AssertRecordIsBound(record4, 1, 2);
        }

        [Fact]
        public void TestBindingInvocationsWithNewlines_GetValueMethod()
        {
            IConfiguration configuration = TestHelpers.GetConfigurationFromJsonString(@"{""Longitude"":1,""Latitude"":2}");

            // Newline between the configuration instance and the binding invocation (with the dot on the first line)
            int lat1 = configuration.
                GetValue<int>("Latitude");

            Assert.Equal(2, lat1);

            // Newline between the configuration instance and the binding invocation (with the dot on the second line)
            int lat2 = configuration
                .GetValue<int>("Latitude");

            Assert.Equal(2, lat2);

            // Newlines in every place possible
            long
                lat3
                =
                configuration
                .
                GetValue
                <
                    int
                >
                (
                    "Latitude"
                )
                ;
            Assert.Equal(2, lat3);

            // Newlines and pragmas wrapped around the generic type argument
            long lat4 = configuration.GetValue<
#if DEBUG
                int
#else
                long
#endif
                >("Latitude");

            Assert.Equal(2, lat4);
        }

        private static void AssertRecordIsBound(GeolocationRecord record, int longitude, int latitude)
        {
            Assert.Equal((longitude, latitude), (record.Longitude, record.Latitude));
        }
    }
}
