// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class CatalogExportProvider : ExportProvider, IDisposable
    {
        private sealed class CatalogChangeProxy : ComposablePartCatalog
        {
            private readonly ComposablePartCatalog _originalCatalog;
            private readonly List<ComposablePartDefinition> _addedParts;
            private readonly HashSet<ComposablePartDefinition> _removedParts;

            public CatalogChangeProxy(ComposablePartCatalog originalCatalog,
                IEnumerable<ComposablePartDefinition> addedParts,
                IEnumerable<ComposablePartDefinition> removedParts)
            {
                _originalCatalog = originalCatalog;
                _addedParts = new List<ComposablePartDefinition>(addedParts);
                _removedParts = new HashSet<ComposablePartDefinition>(removedParts);
            }

            public override IEnumerator<ComposablePartDefinition> GetEnumerator()
            {
                return _originalCatalog.Concat(_addedParts).Except(_removedParts).GetEnumerator();
            }

            public override IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> GetExports(
                ImportDefinition definition)
            {
                Requires.NotNull(definition, nameof(definition));

                var originalExports = _originalCatalog.GetExports(definition);
                var trimmedExports = originalExports.Where(partAndExport =>
                    !_removedParts.Contains(partAndExport.Item1));

                var addedExports = new List<Tuple<ComposablePartDefinition, ExportDefinition>>();
                foreach (var part in _addedParts)
                {
                    foreach (var export in part.ExportDefinitions)
                    {
                        if (definition.IsConstraintSatisfiedBy(export))
                        {
                            addedExports.Add(new Tuple<ComposablePartDefinition, ExportDefinition>(part, export));
                        }
                    }
                }
                return trimmedExports.Concat(addedExports);
            }
        }
    }
}
