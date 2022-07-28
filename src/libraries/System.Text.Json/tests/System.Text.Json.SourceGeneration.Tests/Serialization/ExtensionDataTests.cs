//Licensed to the .NET Foundation under one or more agreements.
//The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.SourceGeneration.Tests
{
    public sealed partial class ExtensionDataTests_Metadata : ExtensionDataTests
    {
        public ExtensionDataTests_Metadata()
            : base(new StringSerializerWrapper(ExtensionDataTestsContext_Metadata.Default, (options) => new ExtensionDataTestsContext_Metadata(options)))
        {
        }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
        [JsonSerializable(typeof(ClassWithEmptyPropertyNameAndExtensionProperty))]
        [JsonSerializable(typeof(EmptyClassWithExtensionProperty))]
        [JsonSerializable(typeof(ClassWithExtensionProperty))]
        [JsonSerializable(typeof(ClassWithExtensionField))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyAsObject))]
        [JsonSerializable(typeof(ClassWithIgnoredData))]
        [JsonSerializable(typeof(Dictionary<string, object>))]
        [JsonSerializable(typeof(Dictionary<string, JsonElement>))]
        [JsonSerializable(typeof(CustomOverflowDictionary<object>))]
        [JsonSerializable(typeof(CustomOverflowDictionary<JsonElement>))]
        [JsonSerializable(typeof(ExtensionDataTests))]
        [JsonSerializable(typeof(DictionaryOverflowConverter))]
        [JsonSerializable(typeof(JsonElementOverflowConverter))]
        [JsonSerializable(typeof(CustomObjectDictionaryOverflowConverter))]
        [JsonSerializable(typeof(CustomJsonElementDictionaryOverflowConverter))]
        [JsonSerializable(typeof(ClassWithExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(ClassWithJsonElementExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(ClassWithCustomElementExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(ClassWithCustomJsonElementExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(Dictionary<string, object>))]
        [JsonSerializable(typeof(Dictionary<string, JsonElement>))]
        [JsonSerializable(typeof(CustomOverflowDictionary<object>))]
        [JsonSerializable(typeof(CustomOverflowDictionary<JsonElement>))]
        [JsonSerializable(typeof(ClassWithExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(ClassWithJsonElementExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(ClassWithCustomElementExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(ClassWithCustomJsonElementExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyAlreadyInstantiated))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyAsObjectAndNameProperty))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyAsJsonObject))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyAsJsonElement))]
        [JsonSerializable(typeof(ClassWithReference))]
        [JsonSerializable(typeof(ParentClassWithObject))]
        [JsonSerializable(typeof(ParentClassWithJsonElement))]
        [JsonSerializable(typeof(ClassWithMultipleDictionaries))]
        [JsonSerializable(typeof(ClassWithEscapedProperty))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyAsImmutable))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyAsImmutableJsonElement))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyPrivateConstructor))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyPrivateConstructorJsonElement))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyNoGenericParameters))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyOneGenericParameter))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyThreeGenericParameters))]
        [JsonSerializable(typeof(JsonElement))]
        [JsonSerializable(typeof(ClassWithExtensionData<JsonObject>))]
        [JsonSerializable(typeof(int))]
        [JsonSerializable(typeof(DummyObj))]
        [JsonSerializable(typeof(DummyStruct))]
        internal sealed partial class ExtensionDataTestsContext_Metadata : JsonSerializerContext
        {
        }
    }

    public sealed partial class ExtensionDataTests_Default : ExtensionDataTests
    {
        public ExtensionDataTests_Default()
            : base(new StringSerializerWrapper(ExtensionDataTestsContext_Default.Default, (options) => new ExtensionDataTestsContext_Default(options)))
        {
        }

        [JsonSerializable(typeof(ClassWithEmptyPropertyNameAndExtensionProperty))]
        [JsonSerializable(typeof(EmptyClassWithExtensionProperty))]
        [JsonSerializable(typeof(ClassWithExtensionProperty))]
        [JsonSerializable(typeof(ClassWithExtensionField))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyAsObject))]
        [JsonSerializable(typeof(ClassWithIgnoredData))]
        [JsonSerializable(typeof(Dictionary<string, object>))]
        [JsonSerializable(typeof(Dictionary<string, JsonElement>))]
        [JsonSerializable(typeof(CustomOverflowDictionary<object>))]
        [JsonSerializable(typeof(CustomOverflowDictionary<JsonElement>))]
        [JsonSerializable(typeof(ExtensionDataTests))]
        [JsonSerializable(typeof(DictionaryOverflowConverter))]
        [JsonSerializable(typeof(JsonElementOverflowConverter))]
        [JsonSerializable(typeof(CustomObjectDictionaryOverflowConverter))]
        [JsonSerializable(typeof(CustomJsonElementDictionaryOverflowConverter))]
        [JsonSerializable(typeof(ClassWithExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(ClassWithJsonElementExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(ClassWithCustomElementExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(ClassWithCustomJsonElementExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(Dictionary<string, object>))]
        [JsonSerializable(typeof(Dictionary<string, JsonElement>))]
        [JsonSerializable(typeof(CustomOverflowDictionary<object>))]
        [JsonSerializable(typeof(CustomOverflowDictionary<JsonElement>))]
        [JsonSerializable(typeof(ClassWithExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(ClassWithJsonElementExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(ClassWithCustomElementExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(ClassWithCustomJsonElementExtensionDataWithAttributedConverter))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyAlreadyInstantiated))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyAsObjectAndNameProperty))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyAsJsonObject))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyAsJsonElement))]
        [JsonSerializable(typeof(ClassWithReference))]
        [JsonSerializable(typeof(ParentClassWithObject))]
        [JsonSerializable(typeof(ParentClassWithJsonElement))]
        [JsonSerializable(typeof(ClassWithMultipleDictionaries))]
        [JsonSerializable(typeof(ClassWithEscapedProperty))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyAsImmutable))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyAsImmutableJsonElement))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyPrivateConstructor))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyPrivateConstructorJsonElement))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyNoGenericParameters))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyOneGenericParameter))]
        [JsonSerializable(typeof(ClassWithExtensionPropertyThreeGenericParameters))]
        [JsonSerializable(typeof(JsonElement))]
        [JsonSerializable(typeof(ClassWithExtensionData<JsonObject>))]
        [JsonSerializable(typeof(int))]
        [JsonSerializable(typeof(DummyObj))]
        [JsonSerializable(typeof(DummyStruct))]
        internal sealed partial class ExtensionDataTestsContext_Default : JsonSerializerContext
        {
        }
    }
}
