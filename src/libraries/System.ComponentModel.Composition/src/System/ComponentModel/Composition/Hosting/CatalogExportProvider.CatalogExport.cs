// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class CatalogExportProvider
    {
        private class CatalogExport : Export
        {
            protected readonly CatalogExportProvider _catalogExportProvider;
            protected readonly ComposablePartDefinition _partDefinition;
            protected readonly ExportDefinition _definition;

            public CatalogExport(CatalogExportProvider catalogExportProvider,
                ComposablePartDefinition partDefinition, ExportDefinition definition)
            {
                _catalogExportProvider = catalogExportProvider;
                _partDefinition = partDefinition;
                _definition = definition;
            }

            public override ExportDefinition Definition
            {
                get
                {
                    return _definition;
                }
            }

            protected virtual bool IsSharedPart
            {
                get
                {
                    return true;
                }
            }

            protected CatalogPart GetPartCore()
            {
                return _catalogExportProvider.GetComposablePart(_partDefinition, IsSharedPart);
            }

            protected void DisposePartCore(CatalogPart part, object value)
            {
                _catalogExportProvider.DisposePart(value, part, null);
            }

            protected virtual CatalogPart GetPart()
            {
                return GetPartCore();
            }

            protected override object GetExportedValueCore()
            {
                return _catalogExportProvider.GetExportedValue(GetPart(), _definition, IsSharedPart);
            }

            [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
            public static CatalogExport CreateExport(CatalogExportProvider catalogExportProvider,
                ComposablePartDefinition partDefinition, ExportDefinition definition, CreationPolicy importCreationPolicy)
            {
                CreationPolicy partPolicy = partDefinition.Metadata.GetValue<CreationPolicy>(CompositionConstants.PartCreationPolicyMetadataName);
                bool isSharedPart = ShouldUseSharedPart(partPolicy, importCreationPolicy);

                if (isSharedPart)
                {
                    return new CatalogExport(catalogExportProvider, partDefinition, definition);
                }
                else
                {
                    return new NonSharedCatalogExport(catalogExportProvider, partDefinition, definition);
                }
            }

            private static bool ShouldUseSharedPart(CreationPolicy partPolicy, CreationPolicy importPolicy)
            {
                // Matrix that details which policy to use for a given part to satisfy a given import.
                //                   Part.Any   Part.Shared  Part.NonShared
                // Import.Any        Shared     Shared       NonShared
                // Import.Shared     Shared     Shared       N/A
                // Import.NonShared  NonShared  N/A          NonShared

                switch (partPolicy)
                {
                    case CreationPolicy.Any:
                        {
                            if (importPolicy == CreationPolicy.Any ||
                                importPolicy == CreationPolicy.Shared)
                            {
                                return true;
                            }
                            return false;
                        }

                    case CreationPolicy.NonShared:
                        {
                            Assumes.IsTrue(importPolicy != CreationPolicy.Shared);
                            return false;
                        }

                    default:
                        {
                            Assumes.IsTrue(partPolicy == CreationPolicy.Shared);
                            Assumes.IsTrue(importPolicy != CreationPolicy.NonShared);
                            return true;
                        }
                }
            }
        }

        private sealed class NonSharedCatalogExport : CatalogExport, IDisposable
        {
            private CatalogPart _part;
            private readonly object _lock = new object();

            public NonSharedCatalogExport(CatalogExportProvider catalogExportProvider,
                ComposablePartDefinition partDefinition, ExportDefinition definition)
                : base(catalogExportProvider, partDefinition, definition)
            {
            }

            protected override CatalogPart GetPart()
            {
                // we need to ensure that the part gets created only once, as the export contract requires that the same value be returned on subsequent calls
                if (_part == null)
                {
                    CatalogPart part = GetPartCore();

                    lock (_lock)
                    {
                        if (_part == null)
                        {
                            Thread.MemoryBarrier();
                            _part = part;
                            part = null;
                        }
                    }

                    if (part != null)
                    {
                        DisposePartCore(part, null);
                    }
                }

                return _part;
            }

            protected override bool IsSharedPart
            {
                get
                {
                    return false;
                }
            }

            void IDisposable.Dispose()
            {
                if (_part != null)
                {
                    DisposePartCore(_part, Value);
                    _part = null;
                }
            }
        }
    }
}
