// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class JsonTypeInfoResolverCombineArrayTests : JsonTypeInfoResolverCombineTests
    {
        public override IJsonTypeInfoResolver Combine(params IJsonTypeInfoResolver?[] resolvers) =>
            JsonTypeInfoResolver.Combine(resolvers);

        [Fact]
        public void CombineNullArgument()
        {
            IJsonTypeInfoResolver[] resolvers = null;
            Assert.Throws<ArgumentNullException>(() => Combine(resolvers));
        }
    }

    public class JsonTypeInfoResolverCombineSpanTests : JsonTypeInfoResolverCombineTests
    {
        public override IJsonTypeInfoResolver Combine(params IJsonTypeInfoResolver?[] resolvers) =>
            JsonTypeInfoResolver.Combine((ReadOnlySpan<IJsonTypeInfoResolver>)resolvers);
    }

    public abstract class JsonTypeInfoResolverCombineTests
    {
        public abstract IJsonTypeInfoResolver Combine(params IJsonTypeInfoResolver?[] resolvers);

        [Fact]
        public void Combine_ShouldFlattenResolvers()
        {
            DefaultJsonTypeInfoResolver nonNullResolver1 = new();
            DefaultJsonTypeInfoResolver nonNullResolver2 = new();
            DefaultJsonTypeInfoResolver nonNullResolver3 = new();

            ValidateCombinations(Array.Empty<IJsonTypeInfoResolver>(), Combine());
            ValidateCombinations(Array.Empty<IJsonTypeInfoResolver>(), Combine(new IJsonTypeInfoResolver[] { null }));
            ValidateCombinations(Array.Empty<IJsonTypeInfoResolver>(), Combine(null, null));
            ValidateCombinations(new[] { nonNullResolver1 }, Combine(nonNullResolver1, null));
            ValidateCombinations(new[] { nonNullResolver1, nonNullResolver2 }, Combine(nonNullResolver1, nonNullResolver2, null));
            ValidateCombinations(new[] { nonNullResolver1, nonNullResolver2 }, Combine(nonNullResolver1, null, nonNullResolver2));
            ValidateCombinations(new[] { nonNullResolver1, nonNullResolver2, nonNullResolver3 }, Combine(Combine(Combine(nonNullResolver1), nonNullResolver2), nonNullResolver3));
            ValidateCombinations(new[] { nonNullResolver1, nonNullResolver2, nonNullResolver3 }, Combine(Combine(nonNullResolver1, null, nonNullResolver2), nonNullResolver3));

            void ValidateCombinations(IJsonTypeInfoResolver[] expectedResolvers, IJsonTypeInfoResolver combinedResolver)
            {
                if (expectedResolvers.Length == 1)
                {
                    Assert.Same(expectedResolvers[0], combinedResolver);
                }
                else
                {
                    Assert.Equal(expectedResolvers, GetAndValidateCombinedResolvers(combinedResolver));
                }
            }
        }

        [Fact]
        public void CombiningZeroResolversProducesValidResolver()
        {
            IJsonTypeInfoResolver resolver = Combine();
            Assert.NotNull(resolver);

            // calling twice to make sure we get the same answer
            Assert.Null(resolver.GetTypeInfo(null, null));
            Assert.Null(resolver.GetTypeInfo(null, null));
        }

        [Fact]
        public void CombiningSingleResolverProducesSameAnswersAsInputResolver()
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

            IJsonTypeInfoResolver combined = Combine(resolver);

            Assert.Same(t1, combined.GetTypeInfo(typeof(int), options));
            Assert.Same(t2, combined.GetTypeInfo(typeof(uint), options));
            Assert.Same(t3, combined.GetTypeInfo(typeof(string), options));
            Assert.Null(combined.GetTypeInfo(typeof(char), options));
            Assert.Null(combined.GetTypeInfo(typeof(StringBuilder), options));
        }

        [Fact]
        public void CombiningUsesAndRespectsAllResolversInOrder()
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

            IJsonTypeInfoResolver combined = Combine(r1, r2, r3);

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

        private IList<IJsonTypeInfoResolver> GetAndValidateCombinedResolvers(IJsonTypeInfoResolver resolver)
        {
            var list = (IList<IJsonTypeInfoResolver>)resolver;

            Assert.True(list.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => list.Clear());
            Assert.Throws<InvalidOperationException>(() => list.Add(new DefaultJsonTypeInfoResolver()));

            return list;
        }

    }

    public class JsonTypeInfoResolverTests
    {
        [Fact]
        public void WithAddedModifier_CallsModifierOnResolvedMetadata()
        {
            int modifierInvocationCount = 0;
            JsonSerializerOptions options = new();
            TestResolver resolver = new(JsonTypeInfo.CreateJsonTypeInfo);

            IJsonTypeInfoResolver resolverWithModifier = resolver.WithAddedModifier(_ => modifierInvocationCount++);

            Assert.NotNull(resolverWithModifier.GetTypeInfo(typeof(int), options));
            Assert.Equal(1, modifierInvocationCount);

            Assert.NotNull(resolverWithModifier.GetTypeInfo(typeof(string), options));
            Assert.Equal(2, modifierInvocationCount);

            Assert.NotNull(resolverWithModifier.GetTypeInfo(typeof(int), options));
            Assert.Equal(3, modifierInvocationCount);
        }

        [Fact]
        public void WithAddedModifier_DoesNotCallModifierOnUnResolvedMetadata()
        {
            int modifierInvocationCount = 0;
            JsonSerializerOptions options = new();
            TestResolver resolver = new((_, _) => null);

            IJsonTypeInfoResolver resolverWithModifier = resolver.WithAddedModifier(_ => modifierInvocationCount++);

            Assert.Null(resolverWithModifier.GetTypeInfo(typeof(int), options));
            Assert.Equal(0, modifierInvocationCount);

            Assert.Null(resolverWithModifier.GetTypeInfo(typeof(string), options));
            Assert.Equal(0, modifierInvocationCount);
        }

        [Fact]
        public void WithAddedModifier_CanChainMultipleModifiers()
        {
            int modifier1InvocationCount = 0;
            int modifier2InvocationCount = 0;
            JsonSerializerOptions options = new();
            TestResolver resolver = new(JsonTypeInfo.CreateJsonTypeInfo);

            IJsonTypeInfoResolver resolverWithModifier = resolver
                .WithAddedModifier(_ => modifier1InvocationCount++)
                .WithAddedModifier(_ => Assert.Equal(modifier1InvocationCount, ++modifier2InvocationCount)); // Validates order of modifier evaluation.

            Assert.NotNull(resolverWithModifier.GetTypeInfo(typeof(int), options));
            Assert.Equal(1, modifier1InvocationCount);
            Assert.Equal(1, modifier2InvocationCount);
        }

        [Fact]
        public void WithAddedModifier_ChainingDoesNotMutateIntermediateResolvers()
        {
            int modifier1InvocationCount = 0;
            int modifier2InvocationCount = 0;
            JsonSerializerOptions options = new();
            TestResolver resolver = new(JsonTypeInfo.CreateJsonTypeInfo);

            IJsonTypeInfoResolver resolverWithModifier = resolver
                .WithAddedModifier(_ => modifier1InvocationCount++);

            IJsonTypeInfoResolver resolverWithChainedModifier = resolverWithModifier
                .WithAddedModifier(_ => Assert.Equal(modifier1InvocationCount, ++modifier2InvocationCount)); // Validates order of modifier evaluation.

            Assert.NotSame(resolverWithModifier, resolverWithChainedModifier);

            Assert.NotNull(resolverWithChainedModifier.GetTypeInfo(typeof(int), options));
            Assert.Equal(1, modifier1InvocationCount);
            Assert.Equal(1, modifier2InvocationCount);

            Assert.NotNull(resolverWithModifier.GetTypeInfo(typeof(int), options));
            Assert.Equal(2, modifier1InvocationCount);
            Assert.Equal(1, modifier2InvocationCount);
        }

        [Fact]
        public void WithAddedModifier_ThrowsOnNullArguments()
        {
            TestResolver resolver = new(JsonTypeInfo.CreateJsonTypeInfo);

            Assert.Throws<ArgumentNullException>(() => ((IJsonTypeInfoResolver)null!).WithAddedModifier(_ => { }));
            Assert.Throws<ArgumentNullException>(() => resolver.WithAddedModifier(null));
        }

        [Fact]
        public void NullResolver_ReturnsObjectMetadata()
        {
            var options = new JsonSerializerOptions();
            var resolver = new NullResolver();
            Assert.Null(resolver.GetTypeInfo(typeof(object), options));

            options.TypeInfoResolver = resolver;
            Assert.IsAssignableFrom<JsonTypeInfo<object>>(options.GetTypeInfo(typeof(object)));
        }

        public sealed class NullResolver : IJsonTypeInfoResolver
        {
            public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) => null;
        }
    }
}
