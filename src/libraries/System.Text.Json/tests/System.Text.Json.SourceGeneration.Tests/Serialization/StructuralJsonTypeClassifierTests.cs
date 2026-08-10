// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.SourceGeneration.Tests
{
    public sealed partial class StructuralJsonTypeClassifierTests_Metadata : StructuralJsonTypeClassifierTests
    {
        public StructuralJsonTypeClassifierTests_Metadata()
            : base(new StringSerializerWrapper(StructuralJsonTypeClassifierTestsContext_Metadata.Default))
        {
        }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
        [JsonSerializable(typeof(ArrayOrDictionaryUnion))]
        [JsonSerializable(typeof(Batch<StatusReading>))]
        [JsonSerializable(typeof(Batch<TemperatureReading>))]
        [JsonSerializable(typeof(BatchUnion))]
        [JsonSerializable(typeof(Cat))]
        [JsonSerializable(typeof(Circle))]
        [JsonSerializable(typeof(DateOrDateTimeOffsetUnion))]
        [JsonSerializable(typeof(Dictionary<string, int>))]
        [JsonSerializable(typeof(Dictionary<string, string>))]
        [JsonSerializable(typeof(DictionaryUnion))]
        [JsonSerializable(typeof(Dog))]
        [JsonSerializable(typeof(IdenticalCat))]
        [JsonSerializable(typeof(IdenticalDog))]
        [JsonSerializable(typeof(IdenticalPetUnion))]
        [JsonSerializable(typeof(List<int>))]
        [JsonSerializable(typeof(List<string>))]
        [JsonSerializable(typeof(ListUnion))]
        [JsonSerializable(typeof(NumberHandlingUnion))]
        [JsonSerializable(typeof(NumericUnion))]
        [JsonSerializable(typeof(ObjectOrDictionaryUnion))]
        [JsonSerializable(typeof(OtherRenamedPropertyCase))]
        [JsonSerializable(typeof(PetUnion))]
        [JsonSerializable(typeof(Point))]
        [JsonSerializable(typeof(Rectangle))]
        [JsonSerializable(typeof(RenamedPropertyCase))]
        [JsonSerializable(typeof(RenamedPropertyUnion))]
        [JsonSerializable(typeof(Shape))]
        [JsonSerializable(typeof(StatusReading))]
        [JsonSerializable(typeof(TemporalUnion))]
        [JsonSerializable(typeof(TemperatureReading))]
        internal sealed partial class StructuralJsonTypeClassifierTestsContext_Metadata : JsonSerializerContext
        {
        }
    }

    public sealed partial class StructuralJsonTypeClassifierTests_Default : StructuralJsonTypeClassifierTests
    {
        public StructuralJsonTypeClassifierTests_Default()
            : base(new StringSerializerWrapper(StructuralJsonTypeClassifierTestsContext_Default.Default))
        {
        }

        [JsonSerializable(typeof(ArrayOrDictionaryUnion))]
        [JsonSerializable(typeof(Batch<StatusReading>))]
        [JsonSerializable(typeof(Batch<TemperatureReading>))]
        [JsonSerializable(typeof(BatchUnion))]
        [JsonSerializable(typeof(Cat))]
        [JsonSerializable(typeof(Circle))]
        [JsonSerializable(typeof(DateOrDateTimeOffsetUnion))]
        [JsonSerializable(typeof(Dictionary<string, int>))]
        [JsonSerializable(typeof(Dictionary<string, string>))]
        [JsonSerializable(typeof(DictionaryUnion))]
        [JsonSerializable(typeof(Dog))]
        [JsonSerializable(typeof(IdenticalCat))]
        [JsonSerializable(typeof(IdenticalDog))]
        [JsonSerializable(typeof(IdenticalPetUnion))]
        [JsonSerializable(typeof(List<int>))]
        [JsonSerializable(typeof(List<string>))]
        [JsonSerializable(typeof(ListUnion))]
        [JsonSerializable(typeof(NumberHandlingUnion))]
        [JsonSerializable(typeof(NumericUnion))]
        [JsonSerializable(typeof(ObjectOrDictionaryUnion))]
        [JsonSerializable(typeof(OtherRenamedPropertyCase))]
        [JsonSerializable(typeof(PetUnion))]
        [JsonSerializable(typeof(Point))]
        [JsonSerializable(typeof(Rectangle))]
        [JsonSerializable(typeof(RenamedPropertyCase))]
        [JsonSerializable(typeof(RenamedPropertyUnion))]
        [JsonSerializable(typeof(Shape))]
        [JsonSerializable(typeof(StatusReading))]
        [JsonSerializable(typeof(TemporalUnion))]
        [JsonSerializable(typeof(TemperatureReading))]
        internal sealed partial class StructuralJsonTypeClassifierTestsContext_Default : JsonSerializerContext
        {
        }
    }
}
