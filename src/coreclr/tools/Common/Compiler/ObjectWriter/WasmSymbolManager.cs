// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Internal.Text;

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

    private readonly Dictionary<Utf8String, Entry> _entries = new();
    private readonly int[] _importCounts = new int[(int)WasmIndexSpace.Count];
    private readonly int[] _definitionCounts = new int[(int)WasmIndexSpace.Count];
    private readonly bool[] _importsFrozen = new bool[(int)WasmIndexSpace.Count];

    public int Count => _entries.Count;

    public void AddImport(Utf8String name, WasmIndexSpace indexSpace, int? expectedIndex = null)
    {
        int spaceIndex = GetSpaceIndex(indexSpace);
        if (_importsFrozen[spaceIndex])
        {
            throw new InvalidOperationException(
                $"Cannot add import '{name}' after an index in the {indexSpace} index space was observed.");
        }

        if (_entries.ContainsKey(name))
        {
            throw new InvalidOperationException($"WASM symbol '{name}' is already registered.");
        }

        int ordinal = _importCounts[spaceIndex];
        if (expectedIndex.HasValue && expectedIndex.Value != ordinal)
        {
            throw new InvalidOperationException(
                $"Import '{name}' was assigned {indexSpace} index {ordinal}, but index {expectedIndex.Value} was expected.");
        }

        _entries.Add(name, new Entry(name, indexSpace, ordinal, IsImport: true));
        _importCounts[spaceIndex]++;
    }

    public void AddDefinition(Utf8String name, WasmIndexSpace indexSpace)
    {
        int spaceIndex = GetSpaceIndex(indexSpace);
        if (_entries.TryGetValue(name, out Entry existing))
        {
            if (existing.IndexSpace != indexSpace || existing.IsImport)
            {
                throw new InvalidOperationException(
                    $"WASM symbol '{name}' was already registered in the {existing.IndexSpace} index space.");
            }

            return;
        }

        int ordinal = _definitionCounts[spaceIndex]++;
        _entries.Add(name, new Entry(name, indexSpace, ordinal, IsImport: false));
    }

    public WasmSymbol GetSymbol(Utf8String name)
    {
        if (!_entries.TryGetValue(name, out Entry entry))
        {
            throw new KeyNotFoundException($"No WASM index was registered for symbol '{name}'.");
        }

        return ResolveAndFreeze(entry);
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

    public int GetImportCount(WasmIndexSpace indexSpace) =>
        _importCounts[GetSpaceIndex(indexSpace)];

    public int GetImportCount() => _importCounts.Sum();

    public int GetDefinitionCount(WasmIndexSpace indexSpace) =>
        _definitionCounts[GetSpaceIndex(indexSpace)];

    public IReadOnlyList<WasmSymbol> GetDefinitions(WasmIndexSpace indexSpace)
    {
        int spaceIndex = GetSpaceIndex(indexSpace);
        _importsFrozen[spaceIndex] = true;

        var symbols = new List<WasmSymbol>(_definitionCounts[spaceIndex]);
        foreach (Entry entry in _entries.Values)
        {
            if (!entry.IsImport && entry.IndexSpace == indexSpace)
            {
                symbols.Add(Resolve(entry));
            }
        }

        symbols.Sort(static (left, right) => left.Index.CompareTo(right.Index));
        return symbols;
    }

    public IReadOnlyList<WasmSymbol> GetSymbols()
    {
        var symbols = new List<WasmSymbol>(_entries.Count);
        foreach (Entry entry in _entries.Values)
        {
            _importsFrozen[(int)entry.IndexSpace] = true;
            symbols.Add(Resolve(entry));
        }

        return symbols;
    }

    private WasmSymbol ResolveAndFreeze(Entry entry)
    {
        _importsFrozen[(int)entry.IndexSpace] = true;
        return Resolve(entry);
    }

    private WasmSymbol Resolve(Entry entry)
    {
        int index = entry.IsImport
            ? entry.Ordinal
            : _importCounts[(int)entry.IndexSpace] + entry.Ordinal;

        return new WasmSymbol(entry.Name, entry.IndexSpace, index, entry.IsImport);
    }

    private static int GetSpaceIndex(WasmIndexSpace indexSpace)
    {
        if ((uint)indexSpace >= (uint)WasmIndexSpace.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(indexSpace));
        }

        return (int)indexSpace;
    }
}
