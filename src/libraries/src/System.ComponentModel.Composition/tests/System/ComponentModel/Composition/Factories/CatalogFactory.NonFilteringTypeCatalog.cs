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
        private class NonFilteringTypeCatalog : ComposablePartCatalog
        {
            private readonly List<ComposablePartDefinition> _definitions;

            public NonFilteringTypeCatalog(params Type[] types)
            {
                this._definitions = new List<ComposablePartDefinition>();
                foreach (Type type in types)
                {
                    this._definitions.Add(AttributedModelServices.CreatePartDefinition(type, null));
                }
            }

            public override IQueryable<ComposablePartDefinition> Parts
            {
                get { return this._definitions.AsQueryable(); }
            }
        }
    }
}
