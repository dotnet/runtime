// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;
using Microsoft.Internal;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.Hosting
{
    internal sealed class AssemblyCatalogDebuggerProxy
    {
        private readonly AssemblyCatalog _catalog;

        public AssemblyCatalogDebuggerProxy(AssemblyCatalog catalog)
        {
            Requires.NotNull(catalog, nameof(catalog));

            _catalog = catalog;
        }

        public Assembly Assembly
        {
            get { return _catalog.Assembly; }
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
