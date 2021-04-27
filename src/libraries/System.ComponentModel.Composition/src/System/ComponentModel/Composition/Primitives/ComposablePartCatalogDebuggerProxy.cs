// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Microsoft.Internal;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.Primitives
{
    // This proxy is needed to pretty up ComposablePartCatalog.Parts; IQueryable<T>
    // instances are not displayed in a very friendly way in the debugger.
    internal sealed class ComposablePartCatalogDebuggerProxy
    {
        private readonly ComposablePartCatalog _catalog;

        public ComposablePartCatalogDebuggerProxy(ComposablePartCatalog catalog)
        {
            Requires.NotNull(catalog, nameof(catalog));

            _catalog = catalog;
        }

        public ReadOnlyCollection<ComposablePartDefinition> Parts
        {
            // NOTE: This shouldn't be cached, so that on every query of
            // the current value of the underlying catalog is respected.
            // We use ReadOnlyCollection as arrays do not have the
            // appropriate debugger display attributes applied to them.
            get { return _catalog.Parts.ToReadOnlyCollection(); }
        }
    }
}
