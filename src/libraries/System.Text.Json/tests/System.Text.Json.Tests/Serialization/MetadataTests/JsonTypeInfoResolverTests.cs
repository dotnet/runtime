// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class JsonTypeInfoResolverTests
    {
        [Fact]
        public static void GetTypeInfoNullArguments()
        {
            IJsonTypeInfoResolver[] resolvers = null;
            Assert.Throws<ArgumentNullException>(() => JsonTypeInfoResolver.Combine(resolvers));

            DefaultJsonTypeInfoResolver nonNullResolver1 = new();
            DefaultJsonTypeInfoResolver nonNullResolver2 = new();
            Assert.Throws<ArgumentNullException>(() => JsonTypeInfoResolver.Combine(null));
            Assert.Throws<ArgumentNullException>(() => JsonTypeInfoResolver.Combine(null, null));
            Assert.Throws<ArgumentNullException>(() => JsonTypeInfoResolver.Combine(nonNullResolver1, null));
            Assert.Throws<ArgumentNullException>(() => JsonTypeInfoResolver.Combine(nonNullResolver1, nonNullResolver2, null));
            Assert.Throws<ArgumentNullException>(() => JsonTypeInfoResolver.Combine(nonNullResolver1, null, nonNullResolver2));
        }

        [Fact]
        public static void CombiningZeroResolversProducesValidResolver()
        {
            IJsonTypeInfoResolver resolver = JsonTypeInfoResolver.Combine();
            Assert.NotNull(resolver);

            // calling twice to make sure we get the same answer
            Assert.Null(resolver.GetTypeInfo(null, null));
            Assert.Null(resolver.GetTypeInfo(null, null));
        }

        [Fact]
        public static void CombiningSingleResolverProducesSameAnswersAsInputResolver()
        {
            JsonSerializerOptions options = new();
            JsonTypeInfo t1 = JsonTypeInfo.CreateJsonTypeInfo(typeof(int), options);
            JsonTypeInfo t2 = JsonTypeInfo.CreateJsonTypeInfo(typeof(uint), options);
            JsonTypeInfo t3 = JsonTypeInfo.CreateJsonTypeInfo(typeof(string), options);

            // we return same instance for easier comparison
            TestResolver resolver = new((t, o) =>
            {
                Assert.Same(o, options);
                if (t == typeof(int)) return t1;
                if (t == typeof(uint)) return t2;
                if (t == typeof(string)) return t3;
                return null;
            });

            IJsonTypeInfoResolver combined = JsonTypeInfoResolver.Combine(resolver);

            Assert.Same(t1, combined.GetTypeInfo(typeof(int), options));
            Assert.Same(t2, combined.GetTypeInfo(typeof(uint), options));
            Assert.Same(t3, combined.GetTypeInfo(typeof(string), options));
            Assert.Null(combined.GetTypeInfo(typeof(char), options));
            Assert.Null(combined.GetTypeInfo(typeof(StringBuilder), options));
        }

        [Fact]
        public static void CombiningUsesAndRespectsAllResolversInOrder()
        {
            JsonSerializerOptions options = new();
            JsonTypeInfo t1 = JsonTypeInfo.CreateJsonTypeInfo(typeof(int), options);
            JsonTypeInfo t2 = JsonTypeInfo.CreateJsonTypeInfo(typeof(uint), options);
            JsonTypeInfo t3 = JsonTypeInfo.CreateJsonTypeInfo(typeof(string), options);

            int resolverId = 1;

            // we return same instance for easier comparison
            TestResolver r1 = new((t, o) =>
            {
                Assert.Equal(1, resolverId);
                Assert.Same(o, options);
                if (t == typeof(int)) return t1;
                resolverId++;
                return null;
            });

            TestResolver r2 = new((t, o) =>
            {
                Assert.Equal(2, resolverId);
                Assert.Same(o, options);
                if (t == typeof(uint)) return t2;
                resolverId++;
                return null;
            });

            TestResolver r3 = new((t, o) =>
            {
                Assert.Equal(3, resolverId);
                Assert.Same(o, options);
                if (t == typeof(string)) return t3;
                resolverId++;
                return null;
            });

            IJsonTypeInfoResolver combined = JsonTypeInfoResolver.Combine(r1, r2, r3);

            resolverId = 1;
            Assert.Same(t1, combined.GetTypeInfo(typeof(int), options));
            Assert.Equal(1, resolverId);

            resolverId = 1;
            Assert.Same(t2, combined.GetTypeInfo(typeof(uint), options));
            Assert.Equal(2, resolverId);

            resolverId = 1;
            Assert.Same(t3, combined.GetTypeInfo(typeof(string), options));
            Assert.Equal(3, resolverId);

            resolverId = 1;
            Assert.Null(combined.GetTypeInfo(typeof(char), options));
            Assert.Equal(4, resolverId);

            resolverId = 1;
            Assert.Null(combined.GetTypeInfo(typeof(StringBuilder), options));
            Assert.Equal(4, resolverId);
        }
    }
}
