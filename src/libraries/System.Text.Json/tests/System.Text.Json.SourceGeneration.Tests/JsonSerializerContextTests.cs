// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public static partial class JsonSerializerContextTests
    {
        [Fact]
        public static void VariousNestingAndVisibilityLevelsAreSupported()
        {
            Assert.NotNull(PublicContext.Default);
            Assert.NotNull(NestedContext.Default);
            Assert.NotNull(NestedPublicContext.Default);
            Assert.NotNull(NestedPublicContext.NestedProtectedInternalClass.Default);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void Converters_AndTypeInfoCreator_NotRooted_WhenMetadataNotPresent()
        {
            RemoteExecutor.Invoke(
                new Action(() =>
                {
                    object[] objArr = new object[] { new MyStruct() };

                    // Metadata not generated for MyStruct without JsonSerializableAttribute.
                    NotSupportedException ex = Assert.Throws<NotSupportedException>(
                        () => JsonSerializer.Serialize(objArr, MetadataContext.Default.ObjectArray));
                    string exAsStr = ex.ToString();
                    Assert.Contains(typeof(MyStruct).ToString(), exAsStr);
                    Assert.Contains("JsonSerializerOptions", exAsStr);

                    // This test uses reflection to:
                    // - Access JsonSerializerOptions.s_defaultSimpleConverters
                    // - Access JsonSerializerOptions.s_defaultFactoryConverters
                    // - Access JsonSerializerOptions._typeInfoCreationFunc
                    //
                    // If any of them changes, this test will need to be kept in sync.

                    // Confirm built-in converters not set.
                    AssertFieldNull("s_defaultSimpleConverters", optionsInstance: null);
                    AssertFieldNull("s_defaultFactoryConverters", optionsInstance: null);

                    // Confirm type info dynamic creator not set.
                    AssertFieldNull("_typeInfoCreationFunc", MetadataContext.Default.Options);

                    static void AssertFieldNull(string fieldName, JsonSerializerOptions? optionsInstance)
                    {
                        BindingFlags bindingFlags = BindingFlags.NonPublic | (optionsInstance == null ? BindingFlags.Static : BindingFlags.Instance);
                        FieldInfo fieldInfo = typeof(JsonSerializerOptions).GetField(fieldName, bindingFlags);
                        Assert.NotNull(fieldInfo);
                        Assert.Null(fieldInfo.GetValue(optionsInstance));
                    }
                }),
                new RemoteInvokeOptions() { ExpectedExitCode = 0 }).Dispose();
        }

        [JsonSerializable(typeof(JsonMessage))]
        internal partial class NestedContext : JsonSerializerContext { }

        [JsonSerializable(typeof(JsonMessage))]
        public partial class NestedPublicContext : JsonSerializerContext
        {
            [JsonSerializable(typeof(JsonMessage))]
            protected internal partial class NestedProtectedInternalClass : JsonSerializerContext { }
        }
    }
}
