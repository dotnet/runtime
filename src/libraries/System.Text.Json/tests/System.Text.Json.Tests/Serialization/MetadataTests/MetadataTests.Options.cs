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
            JsonSerializerOptions options = context.Options; // New options instance created and bound at this point.
            Assert.NotNull(options);

            // Pass options.
            options = new JsonSerializerOptions();
            context = new MyJsonContext(options); // Provided options are bound at this point.
            Assert.Same(options, context.Options);
        }

        [Fact]
        public void AddContext()
        {
            JsonSerializerOptions options = new();
            options.AddContext<MyJsonContext>();

            // Options can be bound only once.
            CauseInvalidOperationException(() => options.AddContext<MyJsonContext>());
            CauseInvalidOperationException(() => options.AddContext<MyJsonContextThatSetsOptionsInParameterlessCtor>());
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
            FieldInfo optionsField = typeof(JsonSerializerContext).GetField("_options", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(optionsField);
            Assert.NotNull((JsonSerializerOptions)optionsField.GetValue(context));

            // Those options are overwritten when context is bound via options.AddContext<TContext>();
            JsonSerializerOptions options = new();
            options.AddContext<MyJsonContextThatSetsOptionsInParameterlessCtor>(); // No error.
            FieldInfo resolverField = typeof(JsonSerializerOptions).GetField("_typeInfoResolver", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(resolverField);
            Assert.Same(options, ((JsonSerializerContext)resolverField.GetValue(options)).Options);
        }

        [Fact]
        public void AlreadyBindedOptions()
        {
            // Bind the options.
            JsonSerializerOptions options = new();
            options.AddContext<MyJsonContext>();

            // Attempt to bind the instance again.
            Assert.Throws<InvalidOperationException>(() => new MyJsonContext(options));
        }

        [Fact]
        public void OptionsImmutableAfterBinding()
        {
            // Bind via AddContext
            JsonSerializerOptions options = new();
            options.PropertyNameCaseInsensitive = true;
            options.AddContext<MyJsonContext>();
            CauseInvalidOperationException(() => options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

            // Bind via context ctor
            options = new JsonSerializerOptions();
            MyJsonContext context = new MyJsonContext(options);
            Assert.Same(options, context.Options);
            CauseInvalidOperationException(() => options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
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
    }
}
