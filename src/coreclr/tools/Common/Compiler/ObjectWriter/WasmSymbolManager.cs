// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections;
using System.Collections.Frozen;
using Internal.Text;
using System.Collections.Immutable;

namespace ILCompiler.ObjectWriter;

internal enum WasmIndexSpace
{
    Type,
    Function,
    Table,
    Memory,
    Global,
    Tag,
    Count,
}

internal readonly record struct WasmSymbol(
    Utf8String Name,
    WasmIndexSpace IndexSpace,
    int Index,
    bool IsImport);

internal sealed class WasmSymbolManager
{
    private readonly record struct Entry(
        Utf8String Name,
        WasmIndexSpace IndexSpace,
        int Ordinal,
        bool IsImport);

    private struct IndexSpaceArray<T>
    {
        private T[] _array;

        public IndexSpaceArray()
        {
            _array = new T[(int)WasmIndexSpace.Count];
        }

        public T this[WasmIndexSpace indexSpace]
        {
            get => _array[(int)indexSpace];
            set => _array[(int)indexSpace] = value;
        }

        public IReadOnlyList<T> Values => _array;
    }

    private readonly Dictionary<Utf8String, Entry> _entries = new();
    private IndexSpaceArray<int> _importCounts = new IndexSpaceArray<int>();
    private IndexSpaceArray<int> _definitionCounts = new IndexSpaceArray<int>();
    private IndexSpaceArray<bool> _importsFrozen = new IndexSpaceArray<bool>();

    public void AddImport(Utf8String name, WasmIndexSpace indexSpace, int? expectedIndex = null)
    {
        Debug.Assert(!_importsFrozen[indexSpace]);
        int ordinal = _importCounts[indexSpace];
        Debug.Assert(!expectedIndex.HasValue || expectedIndex.Value == ordinal);
        _entries.Add(name, new Entry(name, indexSpace, ordinal, IsImport: true));
        _importCounts[indexSpace]++;
    }

    public void AddDefinition(Utf8String name, WasmIndexSpace indexSpace)
    {
        int ordinal = _definitionCounts[indexSpace];
        _entries.Add(name, new Entry(name, indexSpace, ordinal, IsImport: false));
        _definitionCounts[indexSpace]++;
    }

    public WasmSymbol GetSymbol(Utf8String name)
    {
        return ResolveAndFreeze(_entries[name]);
    }

    public bool TryGetSymbol(Utf8String name, out WasmSymbol symbol)
    {
        if (!_entries.TryGetValue(name, out Entry entry))
        {
            symbol = default;
            return false;
        }

        symbol = ResolveAndFreeze(entry);
        return true;
    }

    public int GetImportCount() => _importCounts.Values.Sum();

    public int GetDefinitionCount(WasmIndexSpace indexSpace) =>
        _definitionCounts[indexSpace];

    private static readonly Comparer<WasmSymbol> DefaultSymbolComparer = Comparer<WasmSymbol>.Create(static (x, y) => x.Index.CompareTo(y.Index));
    public IEnumerable<WasmSymbol> GetDefinitions(WasmIndexSpace indexSpace, IComparer<WasmSymbol> comparer = null)
    {
        comparer ??= DefaultSymbolComparer;
        _importsFrozen[indexSpace] = true;
        return GetUnsortedDefinitions(indexSpace).Order(comparer);

        IEnumerable<WasmSymbol> GetUnsortedDefinitions(WasmIndexSpace indexSpace)
        {
            foreach (var entry in _entries.Values)
            {
                if (entry.IndexSpace == indexSpace)
                {
                    yield return Resolve(entry);
                }
            }
        }
    }

    private WasmSymbol ResolveAndFreeze(Entry entry)
    {
        _importsFrozen[entry.IndexSpace] = true;
        return Resolve(entry);
    }

    private WasmSymbol Resolve(Entry entry)
    {
        int index = entry.IsImport
            ? entry.Ordinal
            : _importCounts[entry.IndexSpace] + entry.Ordinal;

        return new WasmSymbol(entry.Name, entry.IndexSpace, index, entry.IsImport);
    }

    private static int GetSpaceIndex(WasmIndexSpace indexSpace)
    {
        Debug.Assert((uint)indexSpace < (uint)WasmIndexSpace.Count);
        return (int)indexSpace;
    }
}
