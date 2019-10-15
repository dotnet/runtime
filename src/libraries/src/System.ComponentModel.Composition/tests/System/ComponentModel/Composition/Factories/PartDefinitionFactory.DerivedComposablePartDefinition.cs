// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;

namespace System.ComponentModel.Composition.Factories
{
    partial class PartDefinitionFactory
    {
        private class DerivedComposablePartDefinition : ComposablePartDefinition
        {
            private readonly Func<ComposablePart> _partCreator;
            private readonly IDictionary<string, object> _metadata;
            private IEnumerable<ImportDefinition> _importDefinitions;
            private IEnumerable<ExportDefinition> _exportDefinitions;
            private readonly Func<IEnumerable<ImportDefinition>> _importsCreator;
            private readonly Func<IEnumerable<ExportDefinition>> _exportsCreator;

            public DerivedComposablePartDefinition(
                            IDictionary<string, object> metadata,
                            Func<ComposablePart> partCreator,
                            Func<IEnumerable<ImportDefinition>> importsCreator,
                            Func<IEnumerable<ExportDefinition>> exportsCreator)
            {
                this._metadata = metadata.AsReadOnly();
                this._partCreator = partCreator;
                this._importsCreator = importsCreator;
                this._exportsCreator = exportsCreator;
            }

            public override IDictionary<string, object> Metadata
            {
                get { return this._metadata; }
            }

            public override IEnumerable<ExportDefinition> ExportDefinitions
            {
                get
                {
                    if (this._exportDefinitions == null)
                    {
                        this._exportDefinitions = this._exportsCreator.Invoke() ?? Enumerable.Empty<ExportDefinition>();
                    }
                    return this._exportDefinitions;
                }
            }

            public override IEnumerable<ImportDefinition> ImportDefinitions
            {
                get
                {
                    if (this._importDefinitions == null)
                    {
                        this._importDefinitions = this._importsCreator.Invoke() ?? Enumerable.Empty<ImportDefinition>();
                    }
                    return this._importDefinitions;
                }
            }

            public override ComposablePart CreatePart()
            {
                return this._partCreator();
            }
        }
    }
}
