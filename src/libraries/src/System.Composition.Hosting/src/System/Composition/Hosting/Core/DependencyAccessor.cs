// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace System.Composition.Hosting.Core
{
    /// <summary>
    /// Allows <see cref="ExportDescriptorProvider"/>s to locate the dependencies they require.
    /// </summary>
    public abstract class DependencyAccessor
    {
        /// <summary>
        /// Get all definitions for a specified <see cref="CompositionContract"/>.
        /// </summary>
        /// <param name="exportKey">The export key the definitions must supply.</param>
        /// <returns>The available promises for that export key.</returns>
        protected abstract IEnumerable<ExportDescriptorPromise> GetPromises(CompositionContract exportKey);

        /// <summary>
        /// Resolve dependencies on all implementations of a contract.
        /// </summary>
        /// <param name="site">A tag describing the dependency site.</param>
        /// <param name="contract">The contract required by the site.</param>
        /// <param name="isPrerequisite">True if the dependency must be satisfied before corresponding exports can be retrieved; otherwise, false.</param>
        /// <returns>Dependencies for all implementations of the contact.</returns>
        public IEnumerable<CompositionDependency> ResolveDependencies(object site, CompositionContract contract, bool isPrerequisite)
        {
            var all = GetPromises(contract).ToArray();
            var result = new CompositionDependency[all.Length];
            for (var i = 0; i < all.Length; ++i)
                result[i] = CompositionDependency.Satisfied(contract, all[i], isPrerequisite, site);
            return result;
        }

        /// <summary>
        /// Resolve a required dependency on exactly one implementation of a contract.
        /// </summary>
        /// <param name="site">A tag describing the dependency site.</param>
        /// <param name="contract">The contract required by the site.</param>
        /// <param name="isPrerequisite">True if the dependency must be satisfied before corresponding exports can be retrieved; otherwise, false.</param>
        /// <returns>The dependency.</returns>
        public CompositionDependency ResolveRequiredDependency(object site, CompositionContract contract, bool isPrerequisite)
        {
            CompositionDependency result;
            if (!TryResolveOptionalDependency(site, contract, isPrerequisite, out result))
                return CompositionDependency.Missing(contract, site);

            return result;
        }

        /// <summary>
        /// Resolve an optional dependency on exactly one implementation of a contract.
        /// </summary>
        /// <param name="site">A tag describing the dependency site.</param>
        /// <param name="contract">The contract required by the site.</param>
        /// <param name="isPrerequisite">True if the dependency must be satisfied before corresponding exports can be retrieved; otherwise, false.</param>
        /// <param name="dependency">The dependency, or null.</param>
        /// <returns>True if the dependency could be resolved; otherwise, false.</returns>
        public bool TryResolveOptionalDependency(object site, CompositionContract contract, bool isPrerequisite, out CompositionDependency dependency)
        {
            var all = GetPromises(contract).ToArray();
            if (all.Length == 0)
            {
                dependency = null;
                return false;
            }

            if (all.Length != 1)
            {
                dependency = CompositionDependency.Oversupplied(contract, all, site);
                return true;
            }

            dependency = CompositionDependency.Satisfied(contract, all[0], isPrerequisite, site);
            return true;
        }
    }
}
