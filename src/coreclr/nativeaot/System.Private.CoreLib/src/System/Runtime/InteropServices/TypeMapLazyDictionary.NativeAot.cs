// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

using Internal.NativeFormat;
using Internal.Reflection.Core.Execution;
using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.TypeLoader;

namespace System.Runtime.InteropServices
{
    internal static class TypeMapLazyDictionary
    {
        // Flavors of a type map group: which values the entries of the group carry.
        // The same values are used as the state of the group in the image (0 is an invalid group).
        private const uint TypesFlavor = 1;
        private const uint ValuesFlavor = 2;

        // Kinds of the data of an entry for a typed value flavor (see ITypeMapValue<TValue>). The data of an entry is
        // (index << ValueKindBits) | kind, see TypeMapValueSupport in ILC. Any other kind (2) means that the mapped type does not implement the interface.
        private const int ValueKindBits = 2;
        private const uint ValueKindMask = (1u << ValueKindBits) - 1;
        private const uint ValueKindData = 0;
        private const uint ValueKindGetter = 1;

        private readonly struct ValueFlavor(RuntimeTypeHandle valueType, IntPtr dataTable)
        {
            public RuntimeTypeHandle ValueType { get; } = valueType;
            public IntPtr DataTable { get; } = dataTable;
        }

        private sealed class GroupTable(NativeHashtable table, ExternalReferencesTable externalReferences, uint flavors, ValueFlavor[]? valueFlavors)
        {
            public NativeHashtable Table { get; } = table;
            public ExternalReferencesTable ExternalReferences { get; } = externalReferences;
            public uint Flavors { get; } = flavors;
            public ValueFlavor[]? ValueFlavors { get; } = valueFlavors;
        }

        private static readonly ConcurrentDictionary<RuntimeTypeHandle, GroupTable> s_externalGroups = new();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, GroupTable> s_proxyGroups = new();

        // Finds the group in the image and returns the parser positioned after the state of the group.
        private static NativeParser FindGroupState(ReadyToRunSectionType sectionType, RuntimeTypeHandle typeMapGroupHandle, out ExternalReferencesTable externalReferences, out uint flavors)
        {
            foreach (TypeManagerHandle module in RuntimeAugments.GetLoadedModules())
            {
                if (!TryGetNativeReaderForBlob(module, sectionType, out NativeReader typeMapReader))
                {
                    continue;
                }
                NativeParser typeMapParser = new NativeParser(typeMapReader, 0);
                NativeHashtable typeMapTable = new NativeHashtable(typeMapParser);

                externalReferences = default;
                externalReferences.InitializeCommonFixupsTable(module);

                var lookup = typeMapTable.Lookup(typeMapGroupHandle.GetHashCode());
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    RuntimeTypeHandle foundTypeMapGroup = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    if (!foundTypeMapGroup.Equals(typeMapGroupHandle))
                    {
                        continue;
                    }
                    flavors = entryParser.GetUnsigned();
                    if (flavors == 0)
                    {
                        unsafe
                        {
                            delegate*<void> exceptionStub = (delegate*<void>)externalReferences.GetFunctionPointerFromIndex(entryParser.GetUnsigned());
                            exceptionStub();
                            Debug.Fail("Expected exception stub to throw an exception.");
                        }
                    }

                    return entryParser;
                }
            }

            throw CreateMissingMetadataException(typeMapGroupHandle);
        }

        // The group is only identified by its handle: a type of a map that starts at an owner type doesn't have to be (and is not) visible to reflection.
        private static Exception CreateMissingMetadataException(RuntimeTypeHandle typeMapGroupHandle)
            => ReflectionCoreExecution.ExecutionEnvironment.CreateMissingMetadataException(Type.GetTypeFromHandle(typeMapGroupHandle)!);

        // The state of a group with typed value flavors is followed by the count of the flavors, then (value type, data table + 1) per flavor.
        private static GroupTable FindGroup(ReadyToRunSectionType sectionType, RuntimeTypeHandle typeMapGroup)
        {
            NativeParser entryParser = FindGroupState(sectionType, typeMapGroup, out ExternalReferencesTable externalReferences, out uint flavors);

            ValueFlavor[]? valueFlavors = null;
            if ((flavors & ValuesFlavor) != 0)
            {
                valueFlavors = new ValueFlavor[entryParser.GetUnsigned()];
                for (int i = 0; i < valueFlavors.Length; i++)
                {
                    RuntimeTypeHandle valueType = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    uint dataTable = entryParser.GetUnsigned();
                    valueFlavors[i] = new ValueFlavor(valueType, dataTable == 0 ? IntPtr.Zero : externalReferences.GetIntPtrFromIndex(dataTable - 1));
                }
            }

            return new GroupTable(new NativeHashtable(entryParser), externalReferences, flavors, valueFlavors);
        }

        // The readers of the type valued maps don't need the descriptors of the typed value flavors: they skip them.
        private static NativeHashtable FindTypesTable(ReadyToRunSectionType sectionType, RuntimeTypeHandle typeMapGroup, out ExternalReferencesTable externalReferences)
        {
            NativeParser entryParser = FindGroupState(sectionType, typeMapGroup, out externalReferences, out uint flavors);

            // Type valued mappings are only available if some call site asked for them. A group that is only used with
            // TryGetValue does not carry any type values.
            RequireFlavor(flavors, typeMapGroup, TypesFlavor);

            if ((flavors & ValuesFlavor) != 0)
            {
                uint count = entryParser.GetUnsigned();
                for (uint i = 0; i < count * 2; i++)
                {
                    entryParser.SkipInteger();
                }
            }

            return new NativeHashtable(entryParser);
        }

        private static GroupTable FindCachedGroupWithFlavor(ConcurrentDictionary<RuntimeTypeHandle, GroupTable> cache, ReadyToRunSectionType sectionType, RuntimeTypeHandle typeMapGroup, uint flavor)
        {
            GroupTable group = cache.GetOrAdd(typeMapGroup, static (_, state) => FindGroup(state.SectionType, state.TypeMapGroup), (SectionType: sectionType, TypeMapGroup: typeMapGroup));
            RequireFlavor(group.Flavors, typeMapGroup, flavor);
            return group;
        }

        private static void RequireFlavor(uint groupFlavors, RuntimeTypeHandle typeMapGroup, uint flavor)
        {
            if ((groupFlavors & flavor) == 0)
            {
                throw CreateMissingMetadataException(typeMapGroup);
            }
        }

        public static bool TryGetValue<TValue>(RuntimeType typeMapGroup, string key, [MaybeNullWhen(false)] out TValue value)
            => TryGetValue(typeMapGroup.TypeHandle, key, out value);

        public static bool TryGetValue<TValue>(RuntimeType typeMapGroup, Type key, [MaybeNullWhen(false)] out TValue value)
            => TryGetValue(typeMapGroup.TypeHandle, key, out value);

        private static bool TryGetValue<TValue>(RuntimeTypeHandle typeMapGroup, string key, [MaybeNullWhen(false)] out TValue value)
        {
            GroupTable group = FindCachedGroupWithFlavor(s_externalGroups, ReadyToRunSectionType.ExternalTypeMaps, typeMapGroup, ValuesFlavor);
            int flavor = FindValueFlavor<TValue>(group, typeMapGroup);

            var lookup = group.Table.Lookup(TypeHashingAlgorithms.ComputeNameHashCode(key));
            NativeParser entryParser;
            while (!(entryParser = lookup.GetNext()).IsNull)
            {
                if (entryParser.StringEquals(key))
                {
                    entryParser.SkipString();
                    return TryReadValue(group, flavor, ref entryParser, out value);
                }
            }

            value = default;
            return false;
        }

        private static bool TryGetValue<TValue>(RuntimeTypeHandle typeMapGroup, Type key, [MaybeNullWhen(false)] out TValue value)
        {
            GroupTable group = FindCachedGroupWithFlavor(s_proxyGroups, ReadyToRunSectionType.ProxyTypeMaps, typeMapGroup, ValuesFlavor);
            int flavor = FindValueFlavor<TValue>(group, typeMapGroup);

            RuntimeTypeHandle handle = key.TypeHandle;
            if (!handle.IsNull)
            {
                var lookup = group.Table.Lookup(handle.GetHashCode());
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    RuntimeTypeHandle foundHandle = group.ExternalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    if (foundHandle.Equals(handle))
                    {
                        return TryReadValue(group, flavor, ref entryParser, out value);
                    }
                }
            }

            value = default;
            return false;
        }

        // The maps that start at an owner type are tables of the image of their own, identified by TypeMapOwnerScope<TTypeMapGroup, TTypeMapGroupOwner> instead of the group.
        // The handle of the scope is used directly (a typeof of it would make the group and the owner visible to reflection).
        public static unsafe bool TryGetValue<TTypeMapGroup, TTypeMapGroupOwner, TValue>(string key, [MaybeNullWhen(false)] out TValue value)
            => TryGetValue(new RuntimeTypeHandle(MethodTable.Of<TypeMapOwnerScope<TTypeMapGroup, TTypeMapGroupOwner>>()), key, out value);

        public static unsafe bool TryGetValue<TTypeMapGroup, TTypeMapGroupOwner, TValue>(Type key, [MaybeNullWhen(false)] out TValue value)
            => TryGetValue(new RuntimeTypeHandle(MethodTable.Of<TypeMapOwnerScope<TTypeMapGroup, TTypeMapGroupOwner>>()), key, out value);

        // A typed value flavor is only in the image if some call site asked for it.
        private static int FindValueFlavor<TValue>(GroupTable group, RuntimeTypeHandle typeMapGroup)
        {
            RuntimeTypeHandle valueType = typeof(TValue).TypeHandle;
            ValueFlavor[] valueFlavors = group.ValueFlavors!;
            for (int i = 0; i < valueFlavors.Length; i++)
            {
                if (valueFlavors[i].ValueType.Equals(valueType))
                {
                    return i;
                }
            }

            throw CreateMissingMetadataException(typeMapGroup);
        }

        // The parser is positioned after the key of an entry: [type value] then one data per typed value flavor.
        private static unsafe bool TryReadValue<TValue>(GroupTable group, int flavor, ref NativeParser entryParser, [MaybeNullWhen(false)] out TValue value)
        {
            if ((group.Flavors & TypesFlavor) != 0)
            {
                entryParser.SkipInteger();
            }

            for (int i = 0; i < flavor; i++)
            {
                entryParser.SkipInteger();
            }

            uint data = entryParser.GetUnsigned();
            switch (data & ValueKindMask)
            {
                case ValueKindData:
                    // The data is a constant TValue: a struct, or a reference to a frozen object (or null). References in it are to frozen objects, so a plain copy is GC safe.
                    value = Unsafe.ReadUnaligned<TValue>((byte*)group.ValueFlavors![flavor].DataTable + (nuint)(data >> ValueKindBits) * (nuint)Unsafe.SizeOf<TValue>());
                    return true;

                case ValueKindGetter:
                    value = ((delegate*<TValue>)group.ExternalReferences.GetFunctionPointerFromIndex(data >> ValueKindBits))();
                    return true;

                default:
                    value = default;
                    return false;
            }
        }

        public static IReadOnlyDictionary<string, Type> CreateExternalTypeMap(RuntimeType typeMapGroup)
        {
            NativeHashtable table = FindTypesTable(ReadyToRunSectionType.ExternalTypeMaps, typeMapGroup.TypeHandle, out ExternalReferencesTable externalReferences);
            return new ExternalTypeMapDictionary(table, externalReferences);
        }

        public static IReadOnlyDictionary<Type, Type> CreateProxyTypeMap(RuntimeType typeMapGroup)
        {
            NativeHashtable table = FindTypesTable(ReadyToRunSectionType.ProxyTypeMaps, typeMapGroup.TypeHandle, out ExternalReferencesTable externalReferences);
            return new AssociatedTypeMapDictionary(table, externalReferences);
        }

        private static unsafe bool TryGetNativeReaderForBlob(TypeManagerHandle module, ReadyToRunSectionType sectionType, out NativeReader reader)
        {
            byte* pBlob = (byte*)RuntimeImports.RhGetModuleSection(module, sectionType, out int length);

            if (pBlob != null)
            {
                reader = new NativeReader(pBlob, (uint)length);
                return true;
            }

            reader = default;
            return false;
        }

        private abstract class TypeMapDictionaryBase<TKey> : IReadOnlyDictionary<TKey, Type>
        {
            public abstract Type this[TKey key] { get; }
            public abstract bool TryGetValue(TKey key, [MaybeNullWhen(false)] out Type value);
            // Not supported to avoid exposing TypeMap entries in a manner that
            // would violate invariants the Trimmer is attempting to enforce.
            public IEnumerable<TKey> Keys => throw new NotSupportedException();
            public IEnumerable<Type> Values => throw new NotSupportedException();
            public int Count => throw new NotSupportedException();
            public bool ContainsKey(TKey key) => throw new NotSupportedException();
            public IEnumerator<KeyValuePair<TKey, Type>> GetEnumerator() => throw new NotSupportedException();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ExternalTypeMapDictionary(NativeHashtable table, ExternalReferencesTable externalReferences) : TypeMapDictionaryBase<string>
        {
            public override Type this[string key]
            {
                get
                {
                    if (!TryGetValue(key, out Type? value))
                    {
                        ThrowHelper.ThrowKeyNotFoundException(key);
                    }
                    return value;
                }
            }

            public override  bool TryGetValue(string key, [MaybeNullWhen(false)] out Type value)
            {
                var lookup = table.Lookup(TypeHashingAlgorithms.ComputeNameHashCode(key));
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    if (entryParser.StringEquals(key))
                    {
                        entryParser.SkipString();
                        RuntimeTypeHandle typeHandle = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                        value = Type.GetTypeFromHandle(typeHandle)!;
                        return true;
                    }
                }
                value = null;
                return false;
            }
        }

        private sealed class AssociatedTypeMapDictionary(NativeHashtable table, ExternalReferencesTable externalReferences) : TypeMapDictionaryBase<Type>
        {
            public override Type this[Type key]
            {
                get
                {
                    if (!TryGetValue(key, out Type? value))
                    {
                        ThrowHelper.ThrowKeyNotFoundException(key);
                    }
                    return value;
                }
            }

            public override bool TryGetValue(Type key, [MaybeNullWhen(false)] out Type value)
            {
                RuntimeTypeHandle handle = key.TypeHandle;
                if (handle.IsNull)
                {
                    value = null;
                    return false;
                }

                var lookup = table.Lookup(handle.GetHashCode());
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    RuntimeTypeHandle foundHandle = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    if (foundHandle.Equals(handle))
                    {
                        RuntimeTypeHandle targetHandle = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                        value = Type.GetTypeFromHandle(targetHandle)!;
                        return true;
                    }
                }
                value = null;
                return false;
            }
        }
    }
}
