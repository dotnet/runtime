// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Linq.Expressions;

namespace System.ComponentModel.Composition
{
    internal static class ComposablePartCatalogExtensions
    {
        public static IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> GetExports(this ComposablePartCatalog catalog, Expression<Func<ExportDefinition, bool>> constraint)
        {
            var import = ImportDefinitionFactory.Create(constraint);
            return catalog.GetExports(import);
        }

        public static Tuple<ComposablePartDefinition, ExportDefinition>[] GetExports<T>(this ComposablePartCatalog catalog)
        {
            return catalog.GetExports(ImportDefinitionFactory.Create(typeof(T), ImportCardinality.ZeroOrMore)).ToArray();
        }

        public static Tuple<ComposablePartDefinition, ExportDefinition> GetExport<T>(this ComposablePartCatalog catalog)
        {
            return catalog.GetExports(ImportDefinitionFactory.Create(typeof(T), ImportCardinality.ExactlyOne)).Single();
        }
    }
}
