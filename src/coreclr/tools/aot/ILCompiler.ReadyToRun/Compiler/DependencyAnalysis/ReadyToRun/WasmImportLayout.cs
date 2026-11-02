// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis.ReadyToRun;

// Ownership is collected before layout and reused when extracting the sidecar's data ranges.
internal sealed class WasmImportLayout
{
    private readonly List<Import> _imports = new();
    private readonly HashSet<Import> _primaryImports = new();
    private readonly HashSet<Import> _coldImports = new();
    private readonly HashSet<ISymbolNode> _primarySignatures = new();
    private readonly HashSet<ISymbolNode> _coldSignatures = new();

    internal IEnumerable<Import> ColdImports => _coldImports;
    internal IEnumerable<ISymbolNode> ColdSignatures => _coldSignatures;

    internal void AddImport(Import import)
    {
        _imports.Add(import);
        if (import.Table.IsEager)
            _primaryImports.Add(import);
    }

    internal void AddUse(ISymbolNode symbol, bool cold)
    {
        if (symbol is Import import)
            (cold ? _coldImports : _primaryImports).Add(import);
        else if (!cold && symbol is Signature)
            _primarySignatures.Add(symbol);
    }

    internal void Complete(Func<ISymbolNode, ISymbolNode> deduplicate)
    {
        _coldImports.ExceptWith(_primaryImports);
        HashSet<ISymbolNode> primarySignatures = new();
        foreach (ISymbolNode signature in _primarySignatures)
            primarySignatures.Add(deduplicate(signature));
        foreach (Import import in _imports)
        {
            bool cold = _coldImports.Contains(import);
            import.SetPartition(cold);
            if (!cold)
                primarySignatures.Add(deduplicate(import.Signature));
        }
        foreach (Import import in _coldImports)
        {
            ISymbolNode signature = deduplicate(import.Signature);
            if (!primarySignatures.Contains(signature))
                _coldSignatures.Add(signature);
        }
    }

    internal IReadOnlyCollection<DependencyNode> OrderSignatureBlobs(IReadOnlyCollection<DependencyNode> nodes)
    {
        List<DependencyNode> ordered = new(nodes.Count);
        List<DependencyNode> cold = new();
        foreach (DependencyNode node in nodes)
        {
            if (node is Signature signature && _coldSignatures.Contains(signature))
                cold.Add(node);
            else
                ordered.Add(node);
        }
        // Keep the existing deterministic order in each group. Only independent signature blobs move;
        // ordered tables (which assign import indices before fixup-list emission) stay in place.
        ordered.AddRange(cold);
        return ordered;
    }
}
