// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class FilteredCatalog
    {
        /// <summary>
        /// Creates a new instance of the <see cref="FilteredCatalog"/> that conatains all the parts from the orignal filtered catalog and all their dependecies.
        /// </summary>
        /// <returns></returns>
        public FilteredCatalog IncludeDependencies()
        {
            return IncludeDependencies(i => i.Cardinality == ImportCardinality.ExactlyOne);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FilteredCatalog"/> that conatains all the parts from the orignal filtered catalog and all their dependencies that
        /// can be reached via imports that match the specified filter.
        /// </summary>
        /// <param name="importFilter">The import filter.</param>
        /// <returns></returns>
        public FilteredCatalog IncludeDependencies(Func<ImportDefinition, bool> importFilter)
        {
            Requires.NotNull(importFilter, nameof(importFilter));
            ThrowIfDisposed();

            return Traverse(new DependenciesTraversal(this, importFilter));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FilteredCatalog"/> that conatains all the parts from the orignal filtered catalog and all their dependents.
        /// </summary>
        /// <returns></returns>
        public FilteredCatalog IncludeDependents()
        {
            return IncludeDependents(i => i.Cardinality == ImportCardinality.ExactlyOne);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FilteredCatalog"/> that conatains all the parts from the orignal filtered catalog and all their dependents that
        /// can be reached via imports that match the specified filter.
        /// </summary>
        /// <param name="importFilter">The import filter.</param>
        /// <returns></returns>
        public FilteredCatalog IncludeDependents(Func<ImportDefinition, bool> importFilter)
        {
            Requires.NotNull(importFilter, nameof(importFilter));
            ThrowIfDisposed();

            return Traverse(new DependentsTraversal(this, importFilter));
        }

        private FilteredCatalog Traverse(IComposablePartCatalogTraversal traversal)
        {
            ArgumentNullException.ThrowIfNull(traversal);

            // we make sure that the underlyiong catalog cannot change while we are doing the trasversal
            // After thaty traversal is done, the freeze is lifted, and the catalog is free to change, but the changes
            // cannot affect partitioning
            FreezeInnerCatalog();

            try
            {
                traversal.Initialize();
                var traversalClosure = GetTraversalClosure(_innerCatalog.Where(_filter), traversal);
                return new FilteredCatalog(_innerCatalog, p => traversalClosure.Contains(p));
            }
            finally
            {
                UnfreezeInnerCatalog();
            }
        }

        private static HashSet<ComposablePartDefinition> GetTraversalClosure(IEnumerable<ComposablePartDefinition> parts, IComposablePartCatalogTraversal traversal)
        {
            ArgumentNullException.ThrowIfNull(traversal);

            var traversedParts = new HashSet<ComposablePartDefinition>();
            GetTraversalClosure(parts, traversedParts, traversal);
            return traversedParts;
        }

        private static void GetTraversalClosure(IEnumerable<ComposablePartDefinition> parts, HashSet<ComposablePartDefinition> traversedParts, IComposablePartCatalogTraversal traversal)
        {
            foreach (var part in parts)
            {
                if (traversedParts.Add(part))
                {
                    if (traversal.TryTraverse(part, out IEnumerable<ComposablePartDefinition>? partsToTraverse))
                    {
                        GetTraversalClosure(partsToTraverse, traversedParts, traversal);
                    }
                }
            }
        }

        private void FreezeInnerCatalog()
        {
            if (_innerCatalog is INotifyComposablePartCatalogChanged innerNotifyCatalog)
            {
                innerNotifyCatalog.Changing += ThrowOnRecomposition;
            }
        }

        private void UnfreezeInnerCatalog()
        {
            if (_innerCatalog is INotifyComposablePartCatalogChanged innerNotifyCatalog)
            {
                innerNotifyCatalog.Changing -= ThrowOnRecomposition;
            }
        }

        private static void ThrowOnRecomposition(object? sender, ComposablePartCatalogChangeEventArgs e)
        {
            throw new ChangeRejectedException();
        }
    }
}
