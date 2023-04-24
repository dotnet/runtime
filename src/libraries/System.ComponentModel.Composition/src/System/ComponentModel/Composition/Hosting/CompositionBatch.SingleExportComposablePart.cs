// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class CompositionBatch
    {
        // Represents a part that exports a single export
        private sealed class SingleExportComposablePart : ComposablePart
        {
            private readonly Export _export;

            public SingleExportComposablePart(Export export)
            {
                ArgumentNullException.ThrowIfNull(export);

                _export = export;
            }

            public override IDictionary<string, object?> Metadata
            {
                get { return MetadataServices.EmptyMetadata; }
            }

            public override IEnumerable<ExportDefinition> ExportDefinitions
            {
                get { return new ExportDefinition[] { _export.Definition }; }
            }

            public override IEnumerable<ImportDefinition> ImportDefinitions
            {
                get { return Enumerable.Empty<ImportDefinition>(); }
            }

            public override object? GetExportedValue(ExportDefinition definition)
            {
                Requires.NotNull(definition, nameof(definition));

                if (definition != _export.Definition)
                {
                    throw ExceptionBuilder.CreateExportDefinitionNotOnThisComposablePart(nameof(definition));
                }

                return _export.Value;
            }

            public override void SetImport(ImportDefinition definition, IEnumerable<Export> exports)
            {
                Requires.NotNull(definition, nameof(definition));
                Requires.NotNullOrNullElements(exports, nameof(exports));

                throw ExceptionBuilder.CreateImportDefinitionNotOnThisComposablePart(nameof(definition));
            }
        }
    }
}
