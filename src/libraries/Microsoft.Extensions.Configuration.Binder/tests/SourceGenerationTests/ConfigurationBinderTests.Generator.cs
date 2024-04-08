// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

        // These are regression tests for https://github.com/dotnet/runtime/issues/90976
        // Ensure that every emitted identifier name is unique, otherwise name clashes
        // will occur and cause compilation to fail.

        [Fact]
        public void NameClashTests_NamingPatternsThatCouldCauseClashes()
        {
            // Potential class between type with closed generic & non generic type.
            // Both types start with same substring. The generic arg type's name is
            // the same as the suffix of the non generic type's name.
            // Manifested in https://github.com/dotnet/runtime/issues/90976.

            IConfiguration configuration = TestHelpers.GetConfigurationFromJsonString(@"{""Value"":1}");

            var c1 = new Cint();
            var c2 = new C<int>();

            configuration.Bind(c1);
            configuration.Bind(c2);
            Assert.Equal(1, c1.Value);
            Assert.Equal(1, c2.Value);
        }

        internal class C<T>
        {
            public int Value { get; set; }
        }

        internal class Cint
        {
            public int Value { get; set; }
        }

        [Fact]
        public void NameClashTests_SameTypeName()
        {
            // Both types have the same name, but one is a nested type.

            IConfiguration configuration = TestHelpers.GetConfigurationFromJsonString(@"{""Value"":1}");

            var c1 = new ClassWithThisIdentifier();
            var c2 = new ClassWithThisIdentifier_Wrapper.ClassWithThisIdentifier();

            configuration.Bind(c1);
            configuration.Bind(c2);
            Assert.Equal(1, c1.Value);
            Assert.Equal(1, c2.Value);
        }

        internal class ClassWithThisIdentifier
        {
            public int Value { get; set; }
        }

        internal class ClassWithThisIdentifier_Wrapper
        {
            internal class ClassWithThisIdentifier
            {
                public int Value { get; set; }
            }
        }

        /// <summary>
        /// These are regression tests for https://github.com/dotnet/runtime/issues/90909.
        /// Ensure that we don't emit root interceptors to handle types/members that
        /// are inaccessible to the generated helpers. Tests for inaccessible transitive members
        /// are covered in the shared (reflection/src-gen) <see cref="ConfigurationBinderTests"/>,
        /// e.g. <see cref="NonPublicModeGetStillIgnoresReadonly"/>.
        /// </summary>
        /// <remarks>
        /// In these cases, binding calls will fallback to reflection, as with all cases where
        /// we can't correctly resolve the type, such as generic call patterns and boxed objects.
        /// </remarks>
        [Fact]
        public void MemberAccessibility_InaccessibleNestedTypeAsRootConfig()
        {
            IConfiguration configuration = TestHelpers.GetConfigurationFromJsonString(@"{""Value"":1}");

            // Ensure no compilation errors; types are skipped.

#pragma warning disable SYSLIB1104 // Binding logic was not generated for a binder call.
            var c1 = new InaccessibleClass_1();
            configuration.Bind(c1);
            var c2 = configuration.Get<InaccessibleClass_2>();
            var c3 = configuration.Get<InaccessibleClass_3>();

            // Generic collections.

            configuration = TestHelpers
                .GetConfigurationFromJsonString(@"{""Array"": [{""Value"":1}]}")
                .GetSection("Array");
            var c4 = configuration.Get<InaccessibleClass_1[]>();
            var c5 = configuration.Get<List<InaccessibleClass_1>>();

            // Generic types.

            Action<BinderOptions>? configureOptions = options => options.BindNonPublicProperties = true;
            string GetNestedObjectPayload(string propName) => $$"""
                {
                    "{{propName}}": {
                        "Value": 1
                    }
                }
                """;

            configuration = TestHelpers.GetConfigurationFromJsonString(GetNestedObjectPayload("item1"));
            var c6 = configuration.Get<Dictionary<string, InaccessibleClass_1>>(configureOptions);

            configuration = TestHelpers.GetConfigurationFromJsonString(GetNestedObjectPayload("protectedMember"));
            var c7 = configuration.Get<AccessibleGenericClass<InaccessibleClass_1>>(configureOptions);
            var c8 = configuration.Get<AccessibleGenericClass<InaccessibleClass_1>>(configureOptions);

            configuration = TestHelpers.GetConfigurationFromJsonString(GetNestedObjectPayload("publicMember"));
            var c9 = configuration.Get<InaccessibleGenericClass<AccessibleClass>>(configureOptions);
            var c10 = configuration.Get<InaccessibleGenericClass<InaccessibleClass_1>>(configureOptions);
#pragma warning disable SYSLIB1104

            // Reflection fallback.

            Assert.Equal(1, c1.Value);
            Assert.Equal(1, c2.Value);
            Assert.Equal(1, c3.Value);
            
            Assert.Equal(1, c4[0].Value);
            Assert.Equal(1, c5[0].Value);
            Assert.Equal(1, c6["item1"].Value);

            Assert.Equal(1, c7.GetProtectedMember.Value);
            Assert.Equal(1, c8.GetProtectedMember.Value);
            Assert.Equal(1, c9.PublicMember.Value);
            Assert.Equal(1, c10.PublicMember.Value);
        }

        private class InaccessibleClass_1()
        {
            public int Value { get; set; }
        }

        protected record InaccessibleClass_2(int Value);

        protected internal class InaccessibleClass_3
        {
            public InaccessibleClass_3(int value) => Value = value;

            public int Value { get; }
        }

        internal class AccessibleGenericClass<T>
        {
            protected T ProtectedMember { get; set; }

            public T GetProtectedMember => ProtectedMember;
        }

        private class InaccessibleGenericClass<T>
        {
            public T PublicMember { get; set; }
        }

        public class AccessibleClass()
        {
            public int Value { get; set; }
        }
    }
}
