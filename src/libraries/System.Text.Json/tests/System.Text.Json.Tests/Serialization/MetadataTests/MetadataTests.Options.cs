// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class MetadataTests
    {
        [Fact]
        public void JsonSerializerContextCtor()
        {
            // Pass no options.
            MyJsonContext context = new();
            JsonSerializerOptions options = context.Options; // New options instance created and binded at this point.
            Assert.NotNull(options);

            // Pass options.
            options = new JsonSerializerOptions();
            context = new MyJsonContext(options); // Provided options are binded at this point.
            Assert.Same(options, context.Options);
        }

        [Fact]
        public void AddContext()
        {
            JsonSerializerOptions options = new();
            options.AddContext<MyJsonContext>();
            Assert.IsType<MyJsonContext>(options.TypeInfoResolver);
        }

        [Fact]
        public void AddContext_SupportsMultipleContexts()
        {
            JsonSerializerOptions options = new();
            options.AddContext<SingleTypeContext<int>>();
            options.AddContext<SingleTypeContext<string>>();

            Assert.NotNull(options.GetTypeInfo(typeof(int)));
            Assert.NotNull(options.GetTypeInfo(typeof(string)));
            Assert.Throws<NotSupportedException>(() => options.GetTypeInfo(typeof(bool)));
        }

        [Fact]
        public void AddContext_AppendsToExistingResolver()
        {
            JsonSerializerOptions options = new();
            options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
            options.AddContext<MyJsonContext>(); // this context always throws

            // should always consult the default resolver, never falling back to the throwing resolver.
            options.GetTypeInfo(typeof(int));
        }

        private static void CauseInvalidOperationException(Action action)
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(action);
            string exAsStr = ex.ToString();
            Assert.Contains("JsonSerializerOptions", exAsStr);
            Assert.Contains("JsonSerializerContext", exAsStr);
        }

        [Fact]
        public void AddContextOverwritesOptionsForFreshContext()
        {
            // Context binds with options when instantiated with parameterless ctor.
            MyJsonContextThatSetsOptionsInParameterlessCtor context = new();
            Assert.NotNull(context.Options);
            Assert.Same(context, context.Options.TypeInfoResolver);

            // Those options are overwritten when context is binded via options.AddContext<TContext>();
            JsonSerializerOptions options = new();
            Assert.Null(options.TypeInfoResolver);
            options.AddContext<MyJsonContextThatSetsOptionsInParameterlessCtor>(); // No error.
            Assert.NotNull(options.TypeInfoResolver);
            Assert.NotSame(options, ((JsonSerializerContext)options.TypeInfoResolver).Options);
        }

        [Fact]
        public void AlreadyBindedOptions()
        {
            // Bind the options.
            JsonSerializerOptions options = new();
            options.AddContext<MyJsonContext>();
            Assert.False(options.IsReadOnly);

            // Pass the options to a context constructor
            _ = new MyJsonContext(options);
            Assert.True(options.IsReadOnly);
        }

        [Fact]
        public void OptionsMutableAfterBinding()
        {
            // Bind via AddContext
            JsonSerializerOptions options = new();
            options.PropertyNameCaseInsensitive = true;
            options.AddContext<MyJsonContext>();
            Assert.False(options.IsReadOnly);

            // Bind via context ctor
            options = new JsonSerializerOptions();
            MyJsonContext context = new MyJsonContext(options);
            Assert.True(options.IsReadOnly);
        }

        [Fact]
        public void PassingImmutableOptionsThrowsException()
        {
            JsonSerializerOptions defaultOptions = JsonSerializerOptions.Default;
            Assert.Throws<InvalidOperationException>(() => new MyJsonContext(defaultOptions));
        }

        [Fact]
        public void PassingWrongOptionsInstanceToResolverThrowsException()
        {
            JsonSerializerOptions defaultOptions = JsonSerializerOptions.Default;
            JsonSerializerOptions contextOptions = new();
            IJsonTypeInfoResolver context = new EmptyContext(contextOptions);

            Assert.IsAssignableFrom<JsonTypeInfo<int>>(context.GetTypeInfo(typeof(int), contextOptions));
            Assert.IsAssignableFrom<JsonTypeInfo<int>>(context.GetTypeInfo(typeof(int), null));
            Assert.Throws<InvalidOperationException>(() => context.GetTypeInfo(typeof(int), defaultOptions));
        }

        private class MyJsonContext : JsonSerializerContext
        {
            public MyJsonContext() : base(null) { }

            public MyJsonContext(JsonSerializerOptions options) : base(options) { }

            public override JsonTypeInfo? GetTypeInfo(Type type) => throw new NotImplementedException();

            protected override JsonSerializerOptions? GeneratedSerializerOptions => null;
        }

        private class MyJsonContextThatSetsOptionsInParameterlessCtor : JsonSerializerContext
        {
            public MyJsonContextThatSetsOptionsInParameterlessCtor() : base(new JsonSerializerOptions()) { }
            public override JsonTypeInfo? GetTypeInfo(Type type) => throw new NotImplementedException();
            protected override JsonSerializerOptions? GeneratedSerializerOptions => null;
        }

        private class EmptyContext : JsonSerializerContext
        {
            public EmptyContext(JsonSerializerOptions options) : base(options) { }
            protected override JsonSerializerOptions? GeneratedSerializerOptions => null;
            public override JsonTypeInfo? GetTypeInfo(Type type) => JsonTypeInfo.CreateJsonTypeInfo(type, Options);
        }

        private class SingleTypeContext<T> : JsonSerializerContext, IJsonTypeInfoResolver
        {
            public SingleTypeContext() : base(null) { }
            protected override JsonSerializerOptions? GeneratedSerializerOptions => null;
            public override JsonTypeInfo? GetTypeInfo(Type type) => GetTypeInfo(type, Options);
            public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) => type == typeof(T) ? JsonTypeInfo.CreateJsonTypeInfo(type, options) : null;
        }
    }
}
