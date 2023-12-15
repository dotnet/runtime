// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.SourceGeneration.Tests
{
    public sealed partial class PropertyNameTests_Metadata : PropertyNameTests
    {
        public PropertyNameTests_Metadata()
            : base(new StringSerializerWrapper(PropertyNameTestsContext_Metadata.Default))
        {
        }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
        [JsonSerializable(typeof(Dictionary<string, OverridePropertyNameDesignTime_TestClass>))]
        [JsonSerializable(typeof(Dictionary<string, int>))]
        [JsonSerializable(typeof(int))]
        [JsonSerializable(typeof(ClassWithSpecialCharacters))]
        [JsonSerializable(typeof(ClassWithPropertyNamePermutations))]
        [JsonSerializable(typeof(ClassWithUnicodeProperty))]
        [JsonSerializable(typeof(DuplicatePropertyNameDesignTime_TestClass))]
        [JsonSerializable(typeof(EmptyPropertyName_TestClass))]
        [JsonSerializable(typeof(IntPropertyNamesDifferentByCaseOnly_TestClass))]
        [JsonSerializable(typeof(NullPropertyName_TestClass))]
        [JsonSerializable(typeof(ObjectPropertyNamesDifferentByCaseOnly_TestClass))]
        [JsonSerializable(typeof(OverridePropertyNameDesignTime_TestClass))]
        [JsonSerializable(typeof(SimpleTestClass))]
        [JsonSerializable(typeof(ClassWithIgnoredCaseInsensitiveConflict))]
        internal sealed partial class PropertyNameTestsContext_Metadata : JsonSerializerContext
        {
        }
    }

    public sealed partial class PropertyNameTests_Default : PropertyNameTests
    {
        public PropertyNameTests_Default()
            : base(new StringSerializerWrapper(PropertyNameTestsContext_Default.Default))
        {
        }

        [JsonSerializable(typeof(Dictionary<string, OverridePropertyNameDesignTime_TestClass>))]
        [JsonSerializable(typeof(Dictionary<string, int>))]
        [JsonSerializable(typeof(int))]
        [JsonSerializable(typeof(ClassWithSpecialCharacters))]
        [JsonSerializable(typeof(ClassWithPropertyNamePermutations))]
        [JsonSerializable(typeof(ClassWithUnicodeProperty))]
        [JsonSerializable(typeof(DuplicatePropertyNameDesignTime_TestClass))]
        [JsonSerializable(typeof(EmptyPropertyName_TestClass))]
        [JsonSerializable(typeof(IntPropertyNamesDifferentByCaseOnly_TestClass))]
        [JsonSerializable(typeof(NullPropertyName_TestClass))]
        [JsonSerializable(typeof(ObjectPropertyNamesDifferentByCaseOnly_TestClass))]
        [JsonSerializable(typeof(OverridePropertyNameDesignTime_TestClass))]
        [JsonSerializable(typeof(SimpleTestClass))]
        [JsonSerializable(typeof(ClassWithIgnoredCaseInsensitiveConflict))]
        internal sealed partial class PropertyNameTestsContext_Default : JsonSerializerContext
        {
        }
    }
}
