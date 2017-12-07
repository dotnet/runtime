// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    public static class CatalogExtensions
    {
        /// <summary>
        /// Creates a <see cref="CompositionService"/>.
        /// </summary>
        /// <param name="catalog">The catalog.</param>
        /// <returns>The newly created <see cref="CompositionService"/> 
        public static CompositionService CreateCompositionService(this ComposablePartCatalog composablePartCatalog)
        {
            Requires.NotNull(composablePartCatalog, nameof(composablePartCatalog));

            return new CompositionService(composablePartCatalog);
        }
    }
}
