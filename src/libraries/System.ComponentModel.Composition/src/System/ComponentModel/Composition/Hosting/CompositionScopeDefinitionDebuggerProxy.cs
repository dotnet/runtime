// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Primitives;
using Microsoft.Internal;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.Hosting
{
    // This proxy is needed to pretty up CompositionScopeDefinitionCatalog.Parts; IQueryable<T>
    // instances are not displayed in a very friendly way in the debugger.
    internal sealed class CompositionScopeDefinitionDebuggerProxy
    {
        private readonly CompositionScopeDefinition _compositionScopeDefinition;

        public CompositionScopeDefinitionDebuggerProxy(CompositionScopeDefinition compositionScopeDefinition)
        {
            Requires.NotNull(compositionScopeDefinition, nameof(compositionScopeDefinition));

            _compositionScopeDefinition = compositionScopeDefinition;
        }

        public ReadOnlyCollection<ComposablePartDefinition> Parts
        {
            get { return _compositionScopeDefinition.Parts.ToReadOnlyCollection(); }
        }

        public IEnumerable<ExportDefinition> PublicSurface
        {
            get
            {
                return _compositionScopeDefinition.PublicSurface.ToReadOnlyCollection();
            }
        }

        public IEnumerable<CompositionScopeDefinition> Children
        {
            get
            {
                return _compositionScopeDefinition.Children.ToReadOnlyCollection();
            }
        }
    }
}
