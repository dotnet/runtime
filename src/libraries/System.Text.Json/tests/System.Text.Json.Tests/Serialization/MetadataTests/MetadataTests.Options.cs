// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace System.Text.Json.Tests.Serialization
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

            // Options can be binded only once.
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

            // Those options are overwritten when context is binded via options.AddContext<TContext>();
            JsonSerializerOptions options = new();
            options.AddContext<MyJsonContextThatSetsOptionsInParameterlessCtor>(); // No error.
            FieldInfo contextField = typeof(JsonSerializerOptions).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(contextField);
            Assert.Same(options, ((JsonSerializerContext)contextField.GetValue(options)).Options);
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

        private class MyJsonContext : JsonSerializerContext
        {
            public MyJsonContext() : base(null, null) { }

            public MyJsonContext(JsonSerializerOptions options) : base(options, null) { }

            public override JsonTypeInfo? GetTypeInfo(Type type) => throw new NotImplementedException();
        }

        private class MyJsonContextThatSetsOptionsInParameterlessCtor : JsonSerializerContext
        {
            public MyJsonContextThatSetsOptionsInParameterlessCtor() : base(new JsonSerializerOptions(), null) { }
            public override JsonTypeInfo? GetTypeInfo(Type type) => throw new NotImplementedException();
        }
    }
}
