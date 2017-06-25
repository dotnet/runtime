// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition.Convention;
using System.Composition.Hosting;
using System.Composition.TypedParts;
using System.Composition.TypedParts.ActivationFeatures;
using System.Composition.TypedParts.Util;
using System.Linq;
using System.Reflection;

namespace System.Composition
{
    /// <summary>
    /// Adds methods to <see cref="CompositionContext"/> that are specific to the
    /// typed part model.
    /// </summary>
    public static class CompositionContextExtensions
    {
        private static readonly DirectAttributeContext s_directAttributeContext = new DirectAttributeContext();

        /// <summary>
        /// Set public properties decorated with the <see cref="ImportAttribute"/>.
        /// </summary>
        /// <remarks>Uses reflection, is slow - caching would help here.</remarks>
        /// <param name="objectWithLooseImports">An object with decorated with import attributes.</param>
        /// <param name="compositionContext">Export provider that will supply imported values.</param>
        public static void SatisfyImports(this CompositionContext compositionContext, object objectWithLooseImports)
        {
            SatisfyImportsInternal(compositionContext, objectWithLooseImports, s_directAttributeContext);
        }

        /// <summary>
        /// Set public properties decorated with the <see cref="ImportAttribute"/>.
        /// </summary>
        /// <remarks>Uses reflection, is slow - caching would help here.</remarks>
        /// <param name="conventions">Conventions to apply when satisfying loose imports.</param>
        /// <param name="objectWithLooseImports">An object with decorated with import attributes.</param>
        /// <param name="compositionContext">Export provider that will supply imported values.</param>
        public static void SatisfyImports(this CompositionContext compositionContext, object objectWithLooseImports, AttributedModelProvider conventions)
        {
            SatisfyImportsInternal(compositionContext, objectWithLooseImports, conventions);
        }

        private static void SatisfyImportsInternal(this CompositionContext exportProvider, object objectWithLooseImports, AttributedModelProvider conventions)
        {
            if (exportProvider == null) throw new ArgumentNullException(nameof(exportProvider));
            if (objectWithLooseImports == null) throw new ArgumentNullException(nameof(objectWithLooseImports));
            if (conventions == null) throw new ArgumentNullException(nameof(conventions));

            var objType = objectWithLooseImports.GetType();

            foreach (var pi in objType.GetRuntimeProperties())
            {
                ImportInfo importInfo;
                var site = new PropertyImportSite(pi);
                if (ContractHelpers.TryGetExplicitImportInfo(pi.PropertyType, conventions.GetDeclaredAttributes(pi.DeclaringType, pi), site, out importInfo))
                {
                    object value;
                    if (exportProvider.TryGetExport(importInfo.Contract, out value))
                    {
                        pi.SetValue(objectWithLooseImports, value);
                    }
                    else if (!importInfo.AllowDefault)
                    {
                        throw new CompositionFailedException(SR.Format(
                            SR.CompositionContextExtensions_MissingDependency, pi.Name, objectWithLooseImports));
                    }
                }
            }

            var importsSatisfiedMethods = objectWithLooseImports.GetType().GetRuntimeMethods().Where(m =>
                m.CustomAttributes.Any(ca => ca.AttributeType == typeof(OnImportsSatisfiedAttribute)));

            foreach (var ois in importsSatisfiedMethods)
                ois.Invoke(objectWithLooseImports, null);
        }
    }
}
