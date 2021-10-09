// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class FilteredCatalog
    {
        internal sealed class DependenciesTraversal : IComposablePartCatalogTraversal
        {
            private readonly IEnumerable<ComposablePartDefinition> _parts;
            private readonly Func<ImportDefinition, bool> _importFilter;
            private Dictionary<string, List<ComposablePartDefinition>>? _exportersIndex;

            public DependenciesTraversal(FilteredCatalog catalog, Func<ImportDefinition, bool> importFilter)
            {
                if (catalog == null)
                {
                    throw new ArgumentNullException(nameof(catalog));
                }

                if (importFilter == null)
                {
                    throw new ArgumentNullException(nameof(importFilter));
                }
                _parts = catalog._innerCatalog;
                _importFilter = importFilter;
            }

            public void Initialize()
            {
                BuildExportersIndex();
            }

            private void BuildExportersIndex()
            {
                _exportersIndex = new Dictionary<string, List<ComposablePartDefinition>>();
                foreach (ComposablePartDefinition part in _parts)
                {
                    foreach (var export in part.ExportDefinitions)
                    {
                        AddToExportersIndex(export.ContractName, part);
                    }
                }
            }

            private void AddToExportersIndex(string contractName, ComposablePartDefinition part)
            {
                if (!_exportersIndex!.TryGetValue(contractName, out List<ComposablePartDefinition>? parts))
                {
                    parts = new List<ComposablePartDefinition>();
                    _exportersIndex.Add(contractName, parts);
                }
                parts.Add(part);
            }

            public bool TryTraverse(ComposablePartDefinition part, [NotNullWhen(true)] out IEnumerable<ComposablePartDefinition>? reachableParts)
            {
                reachableParts = null;
                List<ComposablePartDefinition>? reachablePartList = null;

                // Go through all part imports
                foreach (ImportDefinition import in part.ImportDefinitions.Where(_importFilter))
                {
                    // Find all parts that we know will import each export
                    List<ComposablePartDefinition>? candidateReachableParts = null;
                    Debug.Assert(_exportersIndex != null);
                    foreach (var contractName in import.GetCandidateContractNames(part))
                    {
                        if (_exportersIndex.TryGetValue(contractName, out candidateReachableParts))
                        {
                            // find if they actually match
                            foreach (var candidateReachablePart in candidateReachableParts)
                            {
                                foreach (ExportDefinition export in candidateReachablePart.ExportDefinitions)
                                {
                                    if (import.IsImportDependentOnPart(candidateReachablePart, export, part.IsGeneric() != candidateReachablePart.IsGeneric()))
                                    {
                                        if (reachablePartList == null)
                                        {
                                            reachablePartList = new List<ComposablePartDefinition>();
                                        }
                                        reachablePartList.Add(candidateReachablePart);
                                    }
                                }
                            }
                        }
                    }
                }

                reachableParts = reachablePartList;
                return (reachableParts != null);
            }
        }
    }
}
