// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;

namespace System.ComponentModel.Composition.Factories
{
    partial class CatalogFactory
    {
        private class DerivedComposablePartCatalog : ComposablePartCatalog
        {
            private readonly IEnumerable<ComposablePartDefinition> _definitions;

            public DerivedComposablePartCatalog(IEnumerable<ComposablePartDefinition> definitions)
            {
                _definitions = definitions;
            }

            public override IQueryable<ComposablePartDefinition> Parts
            {
                get { return _definitions.AsQueryable(); }
            }
        }
    }
}
