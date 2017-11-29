// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Caching;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq.Expressions;
using System.UnitTesting;
using System.Reflection;

namespace System.ComponentModel.Composition
{
    internal static class ComposablePartCatalogExtensions
    {
        public static T GetCachedCatalog<T>(this T catalog)
            where T : ComposablePartCatalog
        {
            string path = FileIO.GetTemporaryFileName("cache.dll");

            ComposablePartCatalogCachingServices.CacheCatalog((ICachedComposablePartCatalog)catalog, path);
            return (T)ComposablePartCatalogCachingServices.ReadCatalogFromCache(path);
        }

        public static IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> GetExports(this ComposablePartCatalog catalog, Expression<Func<ExportDefinition, bool>> constraint)
        {
            var import = ImportDefinitionFactory.Create(constraint);
            return catalog.GetExports(import);
        }
    }
}
