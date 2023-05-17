// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace System.Text.Json.SourceGeneration
{
    internal sealed class KnownTypeSymbols(Compilation compilation)
    {
#pragma warning disable CA1822 // Mark members as static false positive with primary constructors.
        public Compilation Compilation => compilation!;

        public readonly INamedTypeSymbol? IListOfTType = compilation!.GetBestTypeByMetadataName(typeof(IList<>));
        public readonly INamedTypeSymbol? ICollectionOfTType = compilation!.GetBestTypeByMetadataName(typeof(ICollection<>));
        public readonly INamedTypeSymbol? IEnumerableType = compilation!.GetBestTypeByMetadataName(typeof(IEnumerable));
        public readonly INamedTypeSymbol? IEnumerableOfTType = compilation!.GetBestTypeByMetadataName(typeof(IEnumerable<>));

        public readonly INamedTypeSymbol? ListOfTType = compilation!.GetBestTypeByMetadataName(typeof(List<>));
        public readonly INamedTypeSymbol? DictionaryOfTKeyTValueType = compilation!.GetBestTypeByMetadataName(typeof(Dictionary<,>));
        public readonly INamedTypeSymbol? IAsyncEnumerableOfTType = compilation!.GetBestTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
        public readonly INamedTypeSymbol? IDictionaryOfTKeyTValueType = compilation!.GetBestTypeByMetadataName(typeof(IDictionary<,>));
        public readonly INamedTypeSymbol? IReadonlyDictionaryOfTKeyTValueType = compilation!.GetBestTypeByMetadataName(typeof(IReadOnlyDictionary<,>));
        public readonly INamedTypeSymbol? ISetOfTType = compilation!.GetBestTypeByMetadataName(typeof(ISet<>));
        public readonly INamedTypeSymbol? StackOfTType = compilation!.GetBestTypeByMetadataName(typeof(Stack<>));
        public readonly INamedTypeSymbol? QueueOfTType = compilation!.GetBestTypeByMetadataName(typeof(Queue<>));
        public readonly INamedTypeSymbol? ConcurrentStackType = compilation!.GetBestTypeByMetadataName(typeof(ConcurrentStack<>));
        public readonly INamedTypeSymbol? ConcurrentQueueType = compilation!.GetBestTypeByMetadataName(typeof(ConcurrentQueue<>));
        public readonly INamedTypeSymbol? IDictionaryType = compilation!.GetBestTypeByMetadataName(typeof(IDictionary));
        public readonly INamedTypeSymbol? IListType = compilation!.GetBestTypeByMetadataName(typeof(IList));
        public readonly INamedTypeSymbol? StackType = compilation!.GetBestTypeByMetadataName(typeof(Stack));
        public readonly INamedTypeSymbol? QueueType = compilation!.GetBestTypeByMetadataName(typeof(Queue));
        public readonly INamedTypeSymbol? KeyValuePair = compilation!.GetBestTypeByMetadataName(typeof(KeyValuePair<,>));

        public readonly INamedTypeSymbol? ImmutableArrayType = compilation!.GetBestTypeByMetadataName(typeof(ImmutableArray<>));
        public readonly INamedTypeSymbol? ImmutableListType = compilation!.GetBestTypeByMetadataName(typeof(ImmutableList<>));
        public readonly INamedTypeSymbol? IImmutableListType = compilation!.GetBestTypeByMetadataName(typeof(IImmutableList<>));
        public readonly INamedTypeSymbol? ImmutableStackType = compilation!.GetBestTypeByMetadataName(typeof(ImmutableStack<>));
        public readonly INamedTypeSymbol? IImmutableStackType = compilation!.GetBestTypeByMetadataName(typeof(IImmutableStack<>));
        public readonly INamedTypeSymbol? ImmutableQueueType = compilation!.GetBestTypeByMetadataName(typeof(ImmutableQueue<>));
        public readonly INamedTypeSymbol? IImmutableQueueType = compilation!.GetBestTypeByMetadataName(typeof(IImmutableQueue<>));
        public readonly INamedTypeSymbol? ImmutableSortedType = compilation!.GetBestTypeByMetadataName(typeof(ImmutableSortedSet<>));
        public readonly INamedTypeSymbol? ImmutableHashSetType = compilation!.GetBestTypeByMetadataName(typeof(ImmutableHashSet<>));
        public readonly INamedTypeSymbol? IImmutableSetType = compilation!.GetBestTypeByMetadataName(typeof(IImmutableSet<>));
        public readonly INamedTypeSymbol? ImmutableDictionaryType = compilation!.GetBestTypeByMetadataName(typeof(ImmutableDictionary<,>));
        public readonly INamedTypeSymbol? ImmutableSortedDictionaryType = compilation!.GetBestTypeByMetadataName(typeof(ImmutableSortedDictionary<,>));
        public readonly INamedTypeSymbol? IImmutableDictionaryType = compilation!.GetBestTypeByMetadataName(typeof(IImmutableDictionary<,>));

        public readonly INamedTypeSymbol ObjectType = compilation!.GetSpecialType(SpecialType.System_Object);
        public readonly INamedTypeSymbol StringType = compilation!.GetSpecialType(SpecialType.System_String);

        public readonly INamedTypeSymbol? DateTimeOffsetType = compilation!.GetBestTypeByMetadataName(typeof(DateTimeOffset));
        public readonly INamedTypeSymbol? TimeSpanType = compilation!.GetBestTypeByMetadataName(typeof(TimeSpan));
        public readonly INamedTypeSymbol? DateOnlyType = compilation!.GetBestTypeByMetadataName("System.DateOnly");
        public readonly INamedTypeSymbol? TimeOnlyType = compilation!.GetBestTypeByMetadataName("System.TimeOnly");
        public readonly IArrayTypeSymbol? ByteArrayType = compilation!.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Byte), rank: 1);
        public readonly INamedTypeSymbol? GuidType = compilation!.GetBestTypeByMetadataName(typeof(Guid));
        public readonly INamedTypeSymbol? UriType = compilation!.GetBestTypeByMetadataName(typeof(Uri));
        public readonly INamedTypeSymbol? VersionType = compilation!.GetBestTypeByMetadataName(typeof(Version));

        // System.Text.Json types
        public readonly INamedTypeSymbol? JsonConverterOfTType = compilation!.GetBestTypeByMetadataName("System.Text.Json.Serialization.JsonConverter`1");
        public readonly INamedTypeSymbol? JsonConverterFactoryType = compilation!.GetBestTypeByMetadataName("System.Text.Json.Serialization.JsonConverterFactory");

        public readonly INamedTypeSymbol? JsonSerializerContextType = compilation.GetBestTypeByMetadataName("System.Text.Json.Serialization.JsonSerializerContext");
        public readonly INamedTypeSymbol? JsonSerializableAttributeType = compilation.GetBestTypeByMetadataName("System.Text.Json.Serialization.JsonSerializableAttribute");

        public readonly INamedTypeSymbol? JsonDocumentType = compilation!.GetBestTypeByMetadataName("System.Text.Json.JsonDocument");
        public readonly INamedTypeSymbol? JsonElementType = compilation!.GetBestTypeByMetadataName("System.Text.Json.JsonElement");

        public readonly INamedTypeSymbol? JsonNodeType = compilation!.GetBestTypeByMetadataName("System.Text.Json.Nodes.JsonNode");
        public readonly INamedTypeSymbol? JsonValueType = compilation!.GetBestTypeByMetadataName("System.Text.Json.Nodes.JsonValue");
        public readonly INamedTypeSymbol? JsonObjectType = compilation!.GetBestTypeByMetadataName("System.Text.Json.Nodes.JsonObject");
        public readonly INamedTypeSymbol? JsonArrayType = compilation!.GetBestTypeByMetadataName("System.Text.Json.Nodes.JsonArray");

        // System.Text.Json attributes
        public readonly INamedTypeSymbol? JsonConverterAttributeType = compilation!.GetBestTypeByMetadataName("System.Text.Json.Serialization.JsonConverterAttribute");
        public readonly INamedTypeSymbol? JsonDerivedTypeAttributeType = compilation!.GetBestTypeByMetadataName("System.Text.Json.Serialization.JsonDerivedTypeAttribute");
        public readonly INamedTypeSymbol? JsonNumberHandlingAttributeType = compilation!.GetBestTypeByMetadataName("System.Text.Json.Serialization.JsonNumberHandlingAttribute");
        public readonly INamedTypeSymbol? JsonObjectCreationHandlingAttributeType = compilation!.GetBestTypeByMetadataName("System.Text.Json.Serialization.JsonObjectCreationHandlingAttribute");
        public readonly INamedTypeSymbol? JsonSourceGenerationOptionsAttributeType = compilation.GetBestTypeByMetadataName("System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute");
        public readonly INamedTypeSymbol? JsonUnmappedMemberHandlingAttributeType = compilation!.GetBestTypeByMetadataName("System.Text.Json.Serialization.JsonUnmappedMemberHandlingAttribute");

        // Unsupported types
        public readonly INamedTypeSymbol? DelegateType = compilation!.GetSpecialType(SpecialType.System_Delegate);
        public readonly INamedTypeSymbol? MemberInfoType = compilation!.GetBestTypeByMetadataName(typeof(MemberInfo));
        public readonly INamedTypeSymbol? SerializationInfoType = compilation!.GetBestTypeByMetadataName(typeof(Runtime.Serialization.SerializationInfo));
        public readonly INamedTypeSymbol? IntPtrType = compilation!.GetBestTypeByMetadataName(typeof(IntPtr));
        public readonly INamedTypeSymbol? UIntPtrType = compilation!.GetBestTypeByMetadataName(typeof(UIntPtr));
#pragma warning restore CA1822 // Mark members as static false positive with primary constructors.

        public bool IsImmutableEnumerableType(ITypeSymbol type, out string? factoryTypeFullName)
        {
            if (type is not INamedTypeSymbol { IsGenericType: true, ConstructedFrom: INamedTypeSymbol genericTypeDef })
            {
                factoryTypeFullName = null;
                return false;
            }

            SymbolEqualityComparer cmp = SymbolEqualityComparer.Default;
            if (cmp.Equals(genericTypeDef, ImmutableArrayType))
            {
                factoryTypeFullName = typeof(ImmutableArray).FullName;
                return true;
            }

            if (cmp.Equals(genericTypeDef, ImmutableListType) ||
                cmp.Equals(genericTypeDef, IImmutableListType))
            {
                factoryTypeFullName = typeof(ImmutableList).FullName;
                return true;
            }

            if (cmp.Equals(genericTypeDef, ImmutableStackType) ||
                cmp.Equals(genericTypeDef, IImmutableStackType))
            {
                factoryTypeFullName = typeof(ImmutableStack).FullName;
                return true;
            }

            if (cmp.Equals(genericTypeDef, ImmutableQueueType) ||
                cmp.Equals(genericTypeDef, IImmutableQueueType))
            {
                factoryTypeFullName = typeof(ImmutableQueue).FullName;
                return true;
            }

            if (cmp.Equals(genericTypeDef, ImmutableHashSetType) ||
                cmp.Equals(genericTypeDef, IImmutableSetType))
            {
                factoryTypeFullName = typeof(ImmutableHashSet).FullName;
                return true;
            }

            if (cmp.Equals(genericTypeDef, ImmutableSortedType))
            {
                factoryTypeFullName = typeof(ImmutableSortedSet).FullName;
                return true;
            }

            factoryTypeFullName = null;
            return false;
        }

        public bool IsImmutableDictionaryType(ITypeSymbol type, out string? factoryTypeFullName)
        {
            if (type is not INamedTypeSymbol { IsGenericType: true, ConstructedFrom: INamedTypeSymbol genericTypeDef })
            {
                factoryTypeFullName = null;
                return false;
            }

            SymbolEqualityComparer cmp = SymbolEqualityComparer.Default;

            if (cmp.Equals(genericTypeDef, ImmutableDictionaryType) ||
                cmp.Equals(genericTypeDef, IImmutableDictionaryType))
            {
                factoryTypeFullName = typeof(ImmutableDictionary).FullName;
                return true;
            }

            if (cmp.Equals(genericTypeDef, ImmutableSortedDictionaryType))
            {
                factoryTypeFullName = typeof(ImmutableSortedDictionary).FullName;
                return true;
            }

            factoryTypeFullName = null;
            return false;
        }
    }
}
