// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.SourceGeneration.Tests
{
    public sealed partial class Ieee754NumericConverterTests_Metadata : Ieee754NumericConverterTests
    {
        public Ieee754NumericConverterTests_Metadata()
            : base(new StringSerializerWrapper(Ieee754NumericConverterTestsContext_Metadata.Default))
        {
        }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
        [JsonSerializable(typeof(object))]
        [JsonSerializable(typeof(JsonNode))]
        [JsonSerializable(typeof(JsonObject))]
        [JsonSerializable(typeof(List<object>))]
        [JsonSerializable(typeof(Dictionary<string, object>))]
        [JsonSerializable(typeof(GenericPoco<object>))]
        [JsonSerializable(typeof(BFloat16))]
        [JsonSerializable(typeof(List<BFloat16>))]
        [JsonSerializable(typeof(Dictionary<string, BFloat16>))]
        [JsonSerializable(typeof(Dictionary<BFloat16, int>))]
        [JsonSerializable(typeof(GenericPoco<BFloat16>))]
        [JsonSerializable(typeof(Decimal32))]
        [JsonSerializable(typeof(List<Decimal32>))]
        [JsonSerializable(typeof(Dictionary<string, Decimal32>))]
        [JsonSerializable(typeof(Dictionary<Decimal32, int>))]
        [JsonSerializable(typeof(GenericPoco<Decimal32>))]
        [JsonSerializable(typeof(Decimal64))]
        [JsonSerializable(typeof(List<Decimal64>))]
        [JsonSerializable(typeof(Dictionary<string, Decimal64>))]
        [JsonSerializable(typeof(Dictionary<Decimal64, int>))]
        [JsonSerializable(typeof(GenericPoco<Decimal64>))]
        [JsonSerializable(typeof(Decimal128))]
        [JsonSerializable(typeof(List<Decimal128>))]
        [JsonSerializable(typeof(Dictionary<string, Decimal128>))]
        [JsonSerializable(typeof(Dictionary<Decimal128, int>))]
        [JsonSerializable(typeof(GenericPoco<Decimal128>))]
        [JsonSerializable(typeof(Ieee754Poco))]
        internal sealed partial class Ieee754NumericConverterTestsContext_Metadata : JsonSerializerContext
        {
        }
    }

    public sealed partial class Ieee754NumericConverterTests_Default : Ieee754NumericConverterTests
    {
        public Ieee754NumericConverterTests_Default()
            : base(new StringSerializerWrapper(Ieee754NumericConverterTestsContext_Default.Default))
        {
        }

        [JsonSerializable(typeof(object))]
        [JsonSerializable(typeof(JsonNode))]
        [JsonSerializable(typeof(JsonObject))]
        [JsonSerializable(typeof(List<object>))]
        [JsonSerializable(typeof(Dictionary<string, object>))]
        [JsonSerializable(typeof(GenericPoco<object>))]
        [JsonSerializable(typeof(BFloat16))]
        [JsonSerializable(typeof(List<BFloat16>))]
        [JsonSerializable(typeof(Dictionary<string, BFloat16>))]
        [JsonSerializable(typeof(Dictionary<BFloat16, int>))]
        [JsonSerializable(typeof(GenericPoco<BFloat16>))]
        [JsonSerializable(typeof(Decimal32))]
        [JsonSerializable(typeof(List<Decimal32>))]
        [JsonSerializable(typeof(Dictionary<string, Decimal32>))]
        [JsonSerializable(typeof(Dictionary<Decimal32, int>))]
        [JsonSerializable(typeof(GenericPoco<Decimal32>))]
        [JsonSerializable(typeof(Decimal64))]
        [JsonSerializable(typeof(List<Decimal64>))]
        [JsonSerializable(typeof(Dictionary<string, Decimal64>))]
        [JsonSerializable(typeof(Dictionary<Decimal64, int>))]
        [JsonSerializable(typeof(GenericPoco<Decimal64>))]
        [JsonSerializable(typeof(Decimal128))]
        [JsonSerializable(typeof(List<Decimal128>))]
        [JsonSerializable(typeof(Dictionary<string, Decimal128>))]
        [JsonSerializable(typeof(Dictionary<Decimal128, int>))]
        [JsonSerializable(typeof(GenericPoco<Decimal128>))]
        [JsonSerializable(typeof(Ieee754Poco))]
        internal sealed partial class Ieee754NumericConverterTestsContext_Default : JsonSerializerContext
        {
        }
    }
}
