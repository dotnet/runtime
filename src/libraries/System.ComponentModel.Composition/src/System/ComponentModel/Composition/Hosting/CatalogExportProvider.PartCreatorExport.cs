// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class CatalogExportProvider
    {
        internal sealed class PartCreatorExport : FactoryExport
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
