// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class CatalogExportProvider
    {
        internal class PartCreatorExport : FactoryExport
        {
            private readonly CatalogExportProvider _catalogExportProvider;

            public PartCreatorExport(CatalogExportProvider catalogExportProvider, ComposablePartDefinition partDefinition, ExportDefinition exportDefinition) : 
                base(partDefinition, exportDefinition)
            {
                _catalogExportProvider = catalogExportProvider;
            }

            public override Export CreateExportProduct()
            {
                return new NonSharedCatalogExport(_catalogExportProvider, UnderlyingPartDefinition, UnderlyingExportDefinition);
            }
        }
    }
}
