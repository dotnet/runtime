// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;
using System.Linq;

namespace System.ComponentModel.Composition.Factories
{
    partial class CatalogFactory
    {
        // NOTE: Do not add any more behavior to this class, as ComposablePartCatalogTests.cs 
        // uses this to verify default behavior of the base class.
        private class NoOverridesComposablePartCatalog : ComposablePartCatalog
        {
            public NoOverridesComposablePartCatalog()
            {
            }

            public override IQueryable<ComposablePartDefinition> Parts
            {
                get { return Enumerable.Empty<ComposablePartDefinition>().AsQueryable(); }
            }
        }
    }
}
