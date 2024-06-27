// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.SourceGeneration.Tests
{
    public partial class MetadataTests_SourceGen() : MetadataTests(new StringSerializerWrapper(Context.Default))
    {
        [JsonSerializable(typeof(int))]
        [JsonSerializable(typeof(int?))]
        [JsonSerializable(typeof(string))]
        [JsonSerializable(typeof(string[]))]
        [JsonSerializable(typeof(List<string>))]
        [JsonSerializable(typeof(IList<string>))]
        [JsonSerializable(typeof(ImmutableArray<string>))]
        [JsonSerializable(typeof(DerivedList<int>))]
        [JsonSerializable(typeof(DerivedListWithCustomConverter))]
        [JsonSerializable(typeof(Dictionary<Guid, int>))]
        [JsonSerializable(typeof(IReadOnlyDictionary<Guid, int>))]
        [JsonSerializable(typeof(ImmutableDictionary<Guid, int>))]
        [JsonSerializable(typeof(DerivedDictionary<int>))]
        [JsonSerializable(typeof(DerivedDictionaryWithCustomConverter))]
        [JsonSerializable(typeof(ArrayList))]
        [JsonSerializable(typeof(Hashtable))]
        [JsonSerializable(typeof(ClassWithoutCtor))]
        [JsonSerializable(typeof(IInterfaceWithProperties))]
        [JsonSerializable(typeof(ClassWithDefaultCtor))]
        [JsonSerializable(typeof(StructWithDefaultCtor))]
        [JsonSerializable(typeof(StructWithDefaultCtor?))]
        [JsonSerializable(typeof(ClassWithParameterizedCtor))]
        [JsonSerializable(typeof(StructWithParameterizedCtor))]
        [JsonSerializable(typeof(ClassWithRequiredMember))]
        [JsonSerializable(typeof(ClassWithInitOnlyProperty))]
        [JsonSerializable(typeof(ClassWithMultipleConstructors))]
        [JsonSerializable(typeof(DerivedClassWithShadowingProperties))]
        [JsonSerializable(typeof(IDerivedInterface))]
        [JsonSerializable(typeof(ClassWithRequiredAndOptionalConstructorParameters))]
        partial class Context : JsonSerializerContext;
    }
}
