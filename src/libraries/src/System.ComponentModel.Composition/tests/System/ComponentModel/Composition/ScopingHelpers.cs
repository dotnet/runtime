// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;

namespace System.ComponentModel.Composition
{
    static class ScopingHelpers
    {
        public static CompositionScopeDefinition AsScope(this ComposablePartCatalog catalog, params CompositionScopeDefinition[] children)
        {
            return new CompositionScopeDefinition(catalog, children);
        }

        public static CompositionScopeDefinition AsScopeWithPublicSurface<T>(this ComposablePartCatalog catalog, params CompositionScopeDefinition[] children)
        {
            IEnumerable<ExportDefinition> definitions = catalog.Parts.SelectMany( (p) => p.ExportDefinitions.Where( (e) => e.ContractName == AttributedModelServices.GetContractName(typeof(T)) ) );
            return new CompositionScopeDefinition(catalog, children, definitions);
        }
    }
}
