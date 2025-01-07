// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace System.Text.Json.SourceGeneration
{
    internal sealed class KnownTypeSymbols
    {
        public KnownTypeSymbols(Compilation compilation)
            => Compilation = compilation;

        public Compilation Compilation { get; }

        // Caches a set of types with built-in converter support. Populated by the Parser class.
        public HashSet<ITypeSymbol>? BuiltInSupportTypes { get; set; }

        public INamedTypeSymbol? IListOfTType => GetOrResolveType(typeof(IList<>), ref _IListOfTType);
        private Option<INamedTypeSymbol?> _IListOfTType;

        public INamedTypeSymbol? ICollectionOfTType => GetOrResolveType(typeof(ICollection<>), ref _ICollectionOfTType);
        private Option<INamedTypeSymbol?> _ICollectionOfTType;

        public INamedTypeSymbol? IEnumerableType => GetOrResolveType(typeof(IEnumerable), ref _IEnumerableType);
        private Option<INamedTypeSymbol?> _IEnumerableType;

        public INamedTypeSymbol? IEnumerableOfTType => GetOrResolveType(typeof(IEnumerable<>), ref _IEnumerableOfTType);
        private Option<INamedTypeSymbol?> _IEnumerableOfTType;

        public INamedTypeSymbol? ListOfTType => GetOrResolveType(typeof(List<>), ref _ListOfTType);
        private Option<INamedTypeSymbol?> _ListOfTType;

        public INamedTypeSymbol? DictionaryOfTKeyTValueType => GetOrResolveType(typeof(Dictionary<,>), ref _DictionaryOfTKeyTValueType);
        private Option<INamedTypeSymbol?> _DictionaryOfTKeyTValueType;

        public INamedTypeSymbol? IAsyncEnumerableOfTType => GetOrResolveType("System.Collections.Generic.IAsyncEnumerable`1", ref _AsyncEnumerableOfTType);
        private Option<INamedTypeSymbol?> _AsyncEnumerableOfTType;

        public INamedTypeSymbol? IDictionaryOfTKeyTValueType => GetOrResolveType(typeof(IDictionary<,>), ref _IDictionaryOfTKeyTValueType);
        private Option<INamedTypeSymbol?> _IDictionaryOfTKeyTValueType;

        public INamedTypeSymbol? IReadonlyDictionaryOfTKeyTValueType => GetOrResolveType(typeof(IReadOnlyDictionary<,>), ref _IReadonlyDictionaryOfTKeyTValueType);
        private Option<INamedTypeSymbol?> _IReadonlyDictionaryOfTKeyTValueType;

        public INamedTypeSymbol? ISetOfTType => GetOrResolveType(typeof(ISet<>), ref _ISetOfTType);
        private Option<INamedTypeSymbol?> _ISetOfTType;

        public INamedTypeSymbol? StackOfTType => GetOrResolveType(typeof(Stack<>), ref _StackOfTType);
        private Option<INamedTypeSymbol?> _StackOfTType;

        public INamedTypeSymbol? QueueOfTType => GetOrResolveType(typeof(Queue<>), ref _QueueOfTType);
        private Option<INamedTypeSymbol?> _QueueOfTType;

        public INamedTypeSymbol? ConcurrentStackType => GetOrResolveType(typeof(ConcurrentStack<>), ref _ConcurrentStackType);
        private Option<INamedTypeSymbol?> _ConcurrentStackType;

        public INamedTypeSymbol? ConcurrentQueueType => GetOrResolveType(typeof(ConcurrentQueue<>), ref _ConcurrentQueueType);
        private Option<INamedTypeSymbol?> _ConcurrentQueueType;

        public INamedTypeSymbol? IDictionaryType => GetOrResolveType(typeof(IDictionary), ref _IDictionaryType);
        private Option<INamedTypeSymbol?> _IDictionaryType;

        public INamedTypeSymbol? IListType => GetOrResolveType(typeof(IList), ref _IListType);
        private Option<INamedTypeSymbol?> _IListType;

        public INamedTypeSymbol? StackType => GetOrResolveType(typeof(Stack), ref _StackType);
        private Option<INamedTypeSymbol?> _StackType;

        public INamedTypeSymbol? QueueType => GetOrResolveType(typeof(Queue), ref _QueueType);
        private Option<INamedTypeSymbol?> _QueueType;

        public INamedTypeSymbol? KeyValuePair => GetOrResolveType(typeof(KeyValuePair<,>), ref _KeyValuePair);
        private Option<INamedTypeSymbol?> _KeyValuePair;

        public INamedTypeSymbol? ImmutableArrayType => GetOrResolveType(typeof(ImmutableArray<>), ref _ImmutableArrayType);
        private Option<INamedTypeSymbol?> _ImmutableArrayType;

        public INamedTypeSymbol? ImmutableListType => GetOrResolveType(typeof(ImmutableList<>), ref _ImmutableListType);
        private Option<INamedTypeSymbol?> _ImmutableListType;

        public INamedTypeSymbol? IImmutableListType => GetOrResolveType(typeof(IImmutableList<>), ref _IImmutableListType);
        private Option<INamedTypeSymbol?> _IImmutableListType;

        public INamedTypeSymbol? ImmutableStackType => GetOrResolveType(typeof(ImmutableStack<>), ref _ImmutableStackType);
        private Option<INamedTypeSymbol?> _ImmutableStackType;

        public INamedTypeSymbol? IImmutableStackType => GetOrResolveType(typeof(IImmutableStack<>), ref _IImmutableStackType);
        private Option<INamedTypeSymbol?> _IImmutableStackType;

        public INamedTypeSymbol? ImmutableQueueType => GetOrResolveType(typeof(ImmutableQueue<>), ref _ImmutableQueueType);
        private Option<INamedTypeSymbol?> _ImmutableQueueType;

        public INamedTypeSymbol? IImmutableQueueType => GetOrResolveType(typeof(IImmutableQueue<>), ref _IImmutableQueueType);
        private Option<INamedTypeSymbol?> _IImmutableQueueType;

        public INamedTypeSymbol? ImmutableSortedType => GetOrResolveType(typeof(ImmutableSortedSet<>), ref _ImmutableSortedType);
        private Option<INamedTypeSymbol?> _ImmutableSortedType;

        public INamedTypeSymbol? ImmutableHashSetType => GetOrResolveType(typeof(ImmutableHashSet<>), ref _ImmutableHashSetType);
        private Option<INamedTypeSymbol?> _ImmutableHashSetType;

        public INamedTypeSymbol? IImmutableSetType => GetOrResolveType(typeof(IImmutableSet<>), ref _IImmutableSetType);
        private Option<INamedTypeSymbol?> _IImmutableSetType;

        public INamedTypeSymbol? ImmutableDictionaryType => GetOrResolveType(typeof(ImmutableDictionary<,>), ref _ImmutableDictionaryType);
        private Option<INamedTypeSymbol?> _ImmutableDictionaryType;

        public INamedTypeSymbol? ImmutableSortedDictionaryType => GetOrResolveType(typeof(ImmutableSortedDictionary<,>), ref _ImmutableSortedDictionaryType);
        private Option<INamedTypeSymbol?> _ImmutableSortedDictionaryType;

        public INamedTypeSymbol? IImmutableDictionaryType => GetOrResolveType(typeof(IImmutableDictionary<,>), ref _IImmutableDictionaryType);
        private Option<INamedTypeSymbol?> _IImmutableDictionaryType;

        public INamedTypeSymbol? KeyedCollectionType => GetOrResolveType(typeof(KeyedCollection<,>), ref _KeyedCollectionType);
        private Option<INamedTypeSymbol?> _KeyedCollectionType;

        public INamedTypeSymbol ObjectType => _ObjectType ??= Compilation.GetSpecialType(SpecialType.System_Object);
        private INamedTypeSymbol? _ObjectType;

        public INamedTypeSymbol StringType => _StringType ??= Compilation.GetSpecialType(SpecialType.System_String);
        private INamedTypeSymbol? _StringType;

        public INamedTypeSymbol? DateTimeOffsetType => GetOrResolveType(typeof(DateTimeOffset), ref _DateTimeOffsetType);
        private Option<INamedTypeSymbol?> _DateTimeOffsetType;

        public INamedTypeSymbol? TimeSpanType => GetOrResolveType(typeof(TimeSpan), ref _TimeSpanType);
        private Option<INamedTypeSymbol?> _TimeSpanType;

        public INamedTypeSymbol? DateOnlyType => GetOrResolveType("System.DateOnly", ref _DateOnlyType);
        private Option<INamedTypeSymbol?> _DateOnlyType;

        public INamedTypeSymbol? TimeOnlyType => GetOrResolveType("System.TimeOnly", ref _TimeOnlyType);
        private Option<INamedTypeSymbol?> _TimeOnlyType;

        public INamedTypeSymbol? Int128Type => GetOrResolveType("System.Int128", ref _Int128Type);
        private Option<INamedTypeSymbol?> _Int128Type;

        public INamedTypeSymbol? UInt128Type => GetOrResolveType("System.UInt128", ref _UInt128Type);
        private Option<INamedTypeSymbol?> _UInt128Type;

        public INamedTypeSymbol? HalfType => GetOrResolveType("System.Half", ref _HalfType);
        private Option<INamedTypeSymbol?> _HalfType;

        public IArrayTypeSymbol? ByteArrayType => _ByteArrayType.HasValue
            ? _ByteArrayType.Value
            : (_ByteArrayType = new(Compilation.CreateArrayTypeSymbol(Compilation.GetSpecialType(SpecialType.System_Byte), rank: 1))).Value;

        private Option<IArrayTypeSymbol?> _ByteArrayType;

        public INamedTypeSymbol? MemoryByteType => _MemoryByteType.HasValue
            ? _MemoryByteType.Value
            : (_MemoryByteType = new(MemoryType?.Construct(Compilation.GetSpecialType(SpecialType.System_Byte)))).Value;

        private Option<INamedTypeSymbol?> _MemoryByteType;

        public INamedTypeSymbol? ReadOnlyMemoryByteType => _ReadOnlyMemoryByteType.HasValue
            ? _ReadOnlyMemoryByteType.Value
            : (_ReadOnlyMemoryByteType = new(ReadOnlyMemoryType?.Construct(Compilation.GetSpecialType(SpecialType.System_Byte)))).Value;

        private Option<INamedTypeSymbol?> _ReadOnlyMemoryByteType;

        public INamedTypeSymbol? GuidType => GetOrResolveType(typeof(Guid), ref _GuidType);
        private Option<INamedTypeSymbol?> _GuidType;

        public INamedTypeSymbol? UriType => GetOrResolveType(typeof(Uri), ref _UriType);
        private Option<INamedTypeSymbol?> _UriType;

        public INamedTypeSymbol? VersionType => GetOrResolveType(typeof(Version), ref _VersionType);
        private Option<INamedTypeSymbol?> _VersionType;

        // System.Text.Json types
        public INamedTypeSymbol? JsonConverterType => GetOrResolveType("System.Text.Json.Serialization.JsonConverter", ref _JsonConverterType);
        private Option<INamedTypeSymbol?> _JsonConverterType;

        public INamedTypeSymbol? JsonSerializerContextType => GetOrResolveType("System.Text.Json.Serialization.JsonSerializerContext", ref _JsonSerializerContextType);
        private Option<INamedTypeSymbol?> _JsonSerializerContextType;

        public INamedTypeSymbol? JsonSerializableAttributeType => GetOrResolveType("System.Text.Json.Serialization.JsonSerializableAttribute", ref _JsonSerializableAttributeType);
        private Option<INamedTypeSymbol?> _JsonSerializableAttributeType;

        public INamedTypeSymbol? JsonDocumentType => GetOrResolveType("System.Text.Json.JsonDocument", ref _JsonDocumentType);
        private Option<INamedTypeSymbol?> _JsonDocumentType;

        public INamedTypeSymbol? JsonElementType => GetOrResolveType("System.Text.Json.JsonElement", ref _JsonElementType);
        private Option<INamedTypeSymbol?> _JsonElementType;

        public INamedTypeSymbol? JsonNodeType => GetOrResolveType("System.Text.Json.Nodes.JsonNode", ref _JsonNodeType);
        private Option<INamedTypeSymbol?> _JsonNodeType;

        public INamedTypeSymbol? JsonValueType => GetOrResolveType("System.Text.Json.Nodes.JsonValue", ref _JsonValueType);
        private Option<INamedTypeSymbol?> _JsonValueType;

        public INamedTypeSymbol? JsonObjectType => GetOrResolveType("System.Text.Json.Nodes.JsonObject", ref _JsonObjectType);
        private Option<INamedTypeSymbol?> _JsonObjectType;

        public INamedTypeSymbol? JsonArrayType => GetOrResolveType("System.Text.Json.Nodes.JsonArray", ref _JsonArrayType);
        private Option<INamedTypeSymbol?> _JsonArrayType;

        // System.Text.Json attributes
        public INamedTypeSymbol? JsonConverterAttributeType => GetOrResolveType("System.Text.Json.Serialization.JsonConverterAttribute", ref _JsonConverterAttributeType);
        private Option<INamedTypeSymbol?> _JsonConverterAttributeType;

        public INamedTypeSymbol? JsonDerivedTypeAttributeType => GetOrResolveType("System.Text.Json.Serialization.JsonDerivedTypeAttribute", ref _JsonDerivedTypeAttributeType);
        private Option<INamedTypeSymbol?> _JsonDerivedTypeAttributeType;

        public INamedTypeSymbol? JsonNumberHandlingAttributeType => GetOrResolveType("System.Text.Json.Serialization.JsonNumberHandlingAttribute", ref _JsonNumberHandlingAttributeType);
        private Option<INamedTypeSymbol?> _JsonNumberHandlingAttributeType;

        public INamedTypeSymbol? JsonObjectCreationHandlingAttributeType => GetOrResolveType("System.Text.Json.Serialization.JsonObjectCreationHandlingAttribute", ref _JsonObjectCreationHandlingAttributeType);
        private Option<INamedTypeSymbol?> _JsonObjectCreationHandlingAttributeType;

        public INamedTypeSymbol? JsonSourceGenerationOptionsAttributeType => GetOrResolveType("System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute", ref _JsonSourceGenerationOptionsAttributeType);
        private Option<INamedTypeSymbol?> _JsonSourceGenerationOptionsAttributeType;

        public INamedTypeSymbol? JsonUnmappedMemberHandlingAttributeType => GetOrResolveType("System.Text.Json.Serialization.JsonUnmappedMemberHandlingAttribute", ref _JsonUnmappedMemberHandlingAttributeType);
        private Option<INamedTypeSymbol?> _JsonUnmappedMemberHandlingAttributeType;

        public INamedTypeSymbol? JsonConstructorAttributeType => GetOrResolveType("System.Text.Json.Serialization.JsonConstructorAttribute", ref _JsonConstructorAttributeType);
        private Option<INamedTypeSymbol?> _JsonConstructorAttributeType;

        public INamedTypeSymbol? SetsRequiredMembersAttributeType => GetOrResolveType("System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute", ref _SetsRequiredMembersAttributeType);
        private Option<INamedTypeSymbol?> _SetsRequiredMembersAttributeType;

        public INamedTypeSymbol? JsonStringEnumConverterType => GetOrResolveType("System.Text.Json.Serialization.JsonStringEnumConverter", ref _JsonStringEnumConverterType);
        private Option<INamedTypeSymbol?> _JsonStringEnumConverterType;

        public INamedTypeSymbol? JsonStringEnumConverterOfTType => GetOrResolveType("System.Text.Json.Serialization.JsonStringEnumConverter`1", ref _JsonStringEnumConverterOfTType);
        private Option<INamedTypeSymbol?> _JsonStringEnumConverterOfTType;

        public INamedTypeSymbol? IJsonOnSerializingType => GetOrResolveType(JsonConstants.IJsonOnSerializingFullName, ref _IJsonOnSerializingType);
        private Option<INamedTypeSymbol?> _IJsonOnSerializingType;

        public INamedTypeSymbol? IJsonOnSerializedType => GetOrResolveType(JsonConstants.IJsonOnSerializedFullName, ref _IJsonOnSerializedType);
        private Option<INamedTypeSymbol?> _IJsonOnSerializedType;

        // Unsupported types
        public INamedTypeSymbol? DelegateType => _DelegateType ??= Compilation.GetSpecialType(SpecialType.System_Delegate);
        private INamedTypeSymbol? _DelegateType;

        public INamedTypeSymbol? MemberInfoType => GetOrResolveType(typeof(MemberInfo), ref _MemberInfoType);
        private Option<INamedTypeSymbol?> _MemberInfoType;

        public INamedTypeSymbol? SerializationInfoType => GetOrResolveType(typeof(Runtime.Serialization.SerializationInfo), ref _SerializationInfoType);
        private Option<INamedTypeSymbol?> _SerializationInfoType;

        public INamedTypeSymbol? IntPtrType => GetOrResolveType(typeof(IntPtr), ref _IntPtrType);
        private Option<INamedTypeSymbol?> _IntPtrType;

        public INamedTypeSymbol? UIntPtrType => GetOrResolveType(typeof(UIntPtr), ref _UIntPtrType);
        private Option<INamedTypeSymbol?> _UIntPtrType;

        public INamedTypeSymbol? MemoryType => GetOrResolveType(typeof(Memory<>), ref _MemoryType);
        private Option<INamedTypeSymbol?> _MemoryType;

        public INamedTypeSymbol? ReadOnlyMemoryType => GetOrResolveType(typeof(ReadOnlyMemory<>), ref _ReadOnlyMemoryType);
        private Option<INamedTypeSymbol?> _ReadOnlyMemoryType;

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

        private INamedTypeSymbol? GetOrResolveType(Type type, ref Option<INamedTypeSymbol?> field)
            => GetOrResolveType(type.FullName!, ref field);

        private INamedTypeSymbol? GetOrResolveType(string fullyQualifiedName, ref Option<INamedTypeSymbol?> field)
        {
            if (field.HasValue)
            {
                return field.Value;
            }

            INamedTypeSymbol? type = Compilation.GetBestTypeByMetadataName(fullyQualifiedName);
            field = new(type);
            return type;
        }

        private readonly struct Option<T>
        {
            public readonly bool HasValue;
            public readonly T Value;

            public Option(T value)
            {
                HasValue = true;
                Value = value;
            }
        }
    }
}
