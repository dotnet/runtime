// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class CatalogExportProvider
    {
        internal sealed class ScopeManager : ExportProvider
        {
            private readonly CompositionScopeDefinition _scopeDefinition;
            private readonly CatalogExportProvider _catalogExportProvider;

            public ScopeManager(CatalogExportProvider catalogExportProvider, CompositionScopeDefinition scopeDefinition)
            {
                if (catalogExportProvider == null)
                {
                    throw new ArgumentNullException(nameof(catalogExportProvider));
                }
                if (scopeDefinition == null)
                {
                    throw new ArgumentNullException(nameof(scopeDefinition));
                }

                _scopeDefinition = scopeDefinition;
                _catalogExportProvider = catalogExportProvider;
            }

            protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition? atomicComposition)
            {
                List<Export> exports = new List<Export>();

                ImportDefinition? queryImport = TranslateImport(definition);
                if (queryImport == null)
                {
                    return exports;
                }

                // go through the catalogs and see if there's anything there of interest
                foreach (CompositionScopeDefinition childCatalog in _scopeDefinition.Children)
                {
                    foreach (var partDefinitionAndExportDefinition in childCatalog.GetExportsFromPublicSurface(queryImport))
                    {
                        // We found a match in the child catalog. Now we need to check that it doesn't get rejected.
                        // if the rejetecion is enabled and atomic composition is present, we will actually have to do the work, if not - we just use what we have
                        bool isChildPartRejected = false;

                        if (_catalogExportProvider.EnsureRejection(atomicComposition))
                        {
                            using (var container = CreateChildContainer(childCatalog))
                            {
                                // We create a nested AtomicComposition() because the container will be Disposed and
                                // the RevertActions need to operate before we Dispose the child container
                                using (var localAtomicComposition = new AtomicComposition(atomicComposition))
                                {
                                    isChildPartRejected = container.CatalogExportProvider!.DetermineRejection(partDefinitionAndExportDefinition.Item1, localAtomicComposition);
                                }
                            }
                        }

                        // If the child part has not been rejected, we will add it to the result set.
                        if (!isChildPartRejected)
                        {
                            exports.Add(CreateScopeExport(childCatalog, partDefinitionAndExportDefinition.Item1, partDefinitionAndExportDefinition.Item2));
                        }
                    }
                }

                return exports;
            }

            private Export CreateScopeExport(CompositionScopeDefinition childCatalog, ComposablePartDefinition partDefinition, ExportDefinition exportDefinition)
            {
                return new ScopeFactoryExport(this, childCatalog, partDefinition, exportDefinition);
            }

            internal CompositionContainer CreateChildContainer(ComposablePartCatalog childCatalog)
            {
                return new CompositionContainer(childCatalog, _catalogExportProvider._compositionOptions, _catalogExportProvider._sourceProvider!);
            }

            private static ImportDefinition? TranslateImport(ImportDefinition definition)
            {
                IPartCreatorImportDefinition? factoryDefinition = definition as IPartCreatorImportDefinition;
                if (factoryDefinition == null)
                {
                    return null;
                }

                // Now we need to make sure that the creation policy is handled correctly
                // We will always create a new child CatalogEP to satsify the request, so from the perspecitive of the caller, the policy should
                // always be NonShared (or Any). From teh perspective of the callee, it's the otehr way around.
                ContractBasedImportDefinition productImportDefinition = factoryDefinition.ProductImportDefinition;
                ImportDefinition? result = null;

                switch (productImportDefinition.RequiredCreationPolicy)
                {
                    case CreationPolicy.NonShared:
                        {
                            // we need to recreate the import definition with the policy "Any", so that we can
                            // pull singletons from the inner CatalogEP. teh "non-sharedness" is achieved through
                            // the creation of the new EPs already.
                            result = new ContractBasedImportDefinition(
                                productImportDefinition.ContractName,
                                productImportDefinition.RequiredTypeIdentity,
                                productImportDefinition.RequiredMetadata,
                                productImportDefinition.Cardinality,
                                productImportDefinition.IsRecomposable,
                                productImportDefinition.IsPrerequisite,
                                CreationPolicy.Any,
                                productImportDefinition.Metadata);
                            break;
                        }
                    case CreationPolicy.Any:
                        {
                            // "Any" works every time
                            result = productImportDefinition;
                            break;
                        }
                }

                return result;
            }
        }
    }
}
