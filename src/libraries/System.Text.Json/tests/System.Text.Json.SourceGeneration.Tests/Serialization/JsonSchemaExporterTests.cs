// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Nodes;
using System.Text.Json.Schema.Tests;
using System.Text.Json.Serialization;

namespace System.Text.Json.SourceGeneration.Tests
{
    public sealed partial class JsonSchemaExporterTests_SourceGen()
        : JsonSchemaExporterTests(new StringSerializerWrapper(TestTypesContext.Default))
    {
        [JsonSerializable(typeof(object))]
        [JsonSerializable(typeof(bool))]
        [JsonSerializable(typeof(byte))]
        [JsonSerializable(typeof(ushort))]
        [JsonSerializable(typeof(uint))]
        [JsonSerializable(typeof(ulong))]
        [JsonSerializable(typeof(sbyte))]
        [JsonSerializable(typeof(short))]
        [JsonSerializable(typeof(int))]
        [JsonSerializable(typeof(long))]
        [JsonSerializable(typeof(float))]
        [JsonSerializable(typeof(double))]
        [JsonSerializable(typeof(decimal))]
#if NETCOREAPP
        [JsonSerializable(typeof(UInt128))]
        [JsonSerializable(typeof(Int128))]
        [JsonSerializable(typeof(Half))]
#endif
        [JsonSerializable(typeof(string))]
        [JsonSerializable(typeof(char))]
        [JsonSerializable(typeof(byte[]))]
        [JsonSerializable(typeof(Memory<byte>))]
        [JsonSerializable(typeof(ReadOnlyMemory<byte>))]
        [JsonSerializable(typeof(DateTime))]
        [JsonSerializable(typeof(DateTimeOffset))]
        [JsonSerializable(typeof(TimeSpan))]
#if NETCOREAPP
        [JsonSerializable(typeof(DateOnly))]
        [JsonSerializable(typeof(TimeOnly))]
#endif
        [JsonSerializable(typeof(Guid))]
        [JsonSerializable(typeof(Uri))]
        [JsonSerializable(typeof(Version))]
        [JsonSerializable(typeof(JsonDocument))]
        [JsonSerializable(typeof(JsonElement))]
        [JsonSerializable(typeof(JsonNode))]
        [JsonSerializable(typeof(JsonValue))]
        [JsonSerializable(typeof(JsonObject))]
        [JsonSerializable(typeof(JsonArray))]
        // Unsupported types
        [JsonSerializable(typeof(Type))]
        [JsonSerializable(typeof(MethodInfo))]
        [JsonSerializable(typeof(UIntPtr))]
        [JsonSerializable(typeof(MemberInfo))]
        [JsonSerializable(typeof(SerializationInfo))]
        [JsonSerializable(typeof(Func<int, int>))]
        // Enum types
        [JsonSerializable(typeof(IntEnum))]
        [JsonSerializable(typeof(StringEnum))]
        [JsonSerializable(typeof(FlagsStringEnum))]
        // Nullable<T> types
        [JsonSerializable(typeof(bool?))]
        [JsonSerializable(typeof(int?))]
        [JsonSerializable(typeof(double?))]
        [JsonSerializable(typeof(Guid?))]
        [JsonSerializable(typeof(JsonElement?))]
        [JsonSerializable(typeof(IntEnum?))]
        [JsonSerializable(typeof(StringEnum?))]
        [JsonSerializable(typeof(SimpleRecordStruct?))]
        // User-defined POCOs
        [JsonSerializable(typeof(SimplePoco))]
        [JsonSerializable(typeof(SimpleRecord))]
        [JsonSerializable(typeof(SimpleRecordStruct))]
        [JsonSerializable(typeof(RecordWithOptionalParameters))]
        [JsonSerializable(typeof(PocoWithRequiredMembers))]
        [JsonSerializable(typeof(PocoWithIgnoredMembers))]
        [JsonSerializable(typeof(PocoWithCustomNaming))]
        [JsonSerializable(typeof(PocoWithCustomNumberHandling))]
        [JsonSerializable(typeof(PocoWithCustomNumberHandlingOnProperties))]
        [JsonSerializable(typeof(PocoWithRecursiveMembers))]
        [JsonSerializable(typeof(PocoWithRecursiveCollectionElement))]
        [JsonSerializable(typeof(PocoWithRecursiveDictionaryValue))]
        [JsonSerializable(typeof(PocoWithDescription))]
        [JsonSerializable(typeof(PocoWithCustomConverter))]
        [JsonSerializable(typeof(PocoWithCustomPropertyConverter))]
        [JsonSerializable(typeof(PocoWithEnums))]
        [JsonSerializable(typeof(PocoWithStructFollowedByNullableStruct))]
        [JsonSerializable(typeof(PocoWithNullableStructFollowedByStruct))]
        [JsonSerializable(typeof(PocoWithExtensionDataProperty))]
        [JsonSerializable(typeof(PocoDisallowingUnmappedMembers))]
        [JsonSerializable(typeof(PocoWithNullableAnnotationAttributes))]
        [JsonSerializable(typeof(PocoWithNullableAnnotationAttributesOnConstructorParams))]
        [JsonSerializable(typeof(PocoWithNullableConstructorParameter))]
        [JsonSerializable(typeof(PocoWithOptionalConstructorParams))]
        [JsonSerializable(typeof(GenericPocoWithNullableConstructorParameter<string>))]
        [JsonSerializable(typeof(PocoWithPolymorphism))]
        [JsonSerializable(typeof(DiscriminatedUnion))]
        [JsonSerializable(typeof(NonAbstractClassWithSingleDerivedType))]
        [JsonSerializable(typeof(PocoCombiningPolymorphicTypeAndDerivedTypes))]
        [JsonSerializable(typeof(ClassWithComponentModelAttributes))]
        [JsonSerializable(typeof(ClassWithJsonPointerEscapablePropertyNames))]
        // Collection types
        [JsonSerializable(typeof(int[]))]
        [JsonSerializable(typeof(List<bool>))]
        [JsonSerializable(typeof(HashSet<string>))]
        [JsonSerializable(typeof(Queue<double>))]
        [JsonSerializable(typeof(Stack<char>))]
        [JsonSerializable(typeof(ImmutableArray<int>))]
        [JsonSerializable(typeof(ImmutableList<string>))]
        [JsonSerializable(typeof(ImmutableQueue<bool>))]
        [JsonSerializable(typeof(object[]))]
        [JsonSerializable(typeof(System.Collections.ArrayList))]
        [JsonSerializable(typeof(Dictionary<string, int>))]
        [JsonSerializable(typeof(SortedDictionary<int, string>))]
        [JsonSerializable(typeof(Dictionary<string, SimplePoco>))]
        [JsonSerializable(typeof(Dictionary<string, object>))]
        [JsonSerializable(typeof(Hashtable))]
        [JsonSerializable(typeof(StructDictionary<string, int>))]
        public partial class TestTypesContext : JsonSerializerContext;
    }
}
