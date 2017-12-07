// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;
using System.Threading;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class CatalogExportProvider
    {
        internal class ScopeFactoryExport : FactoryExport
        {
            private readonly ScopeManager _scopeManager;
            private readonly CompositionScopeDefinition _catalog;

            internal ScopeFactoryExport(ScopeManager scopeManager, CompositionScopeDefinition catalog, ComposablePartDefinition partDefinition, ExportDefinition exportDefinition) :
                base(partDefinition, exportDefinition)
            {
                _scopeManager = scopeManager;
                _catalog = catalog;
            }

            public override Export CreateExportProduct()
            {
                return new ScopeCatalogExport(this);
            }

            private sealed class ScopeCatalogExport : Export, IDisposable
            {
                private readonly ScopeFactoryExport _scopeFactoryExport;
                private CompositionContainer _childContainer;
                private Export _export;
                private readonly object _lock = new object();

                public ScopeCatalogExport(ScopeFactoryExport scopeFactoryExport)
                {
                    _scopeFactoryExport = scopeFactoryExport;
                }

                public override ExportDefinition Definition
                {
                    get
                    {
                        return _scopeFactoryExport.UnderlyingExportDefinition;
                    }
                }

                protected override object GetExportedValueCore()
                {
                    if (_export == null)
                    {
                        var childContainer = _scopeFactoryExport._scopeManager.CreateChildContainer(_scopeFactoryExport._catalog);

                        var export = childContainer.CatalogExportProvider.CreateExport(_scopeFactoryExport.UnderlyingPartDefinition, _scopeFactoryExport.UnderlyingExportDefinition, false, CreationPolicy.Any);
                        lock (_lock)
                        {
                            if (_export == null)
                            {
                                _childContainer = childContainer;
                                Thread.MemoryBarrier();
                                _export = export;

                                childContainer = null;
                                export = null;
                            }
                        }
                        if (childContainer != null)
                        {
                            childContainer.Dispose();
                        }
                    }

                    return _export.Value;
                }

                public void Dispose()
                {
                    CompositionContainer childContainer = null;
                    Export export = null;

                    if (_export != null)
                    {
                        lock (_lock)
                        {
                            export = _export;
                            childContainer = _childContainer;

                            _childContainer = null;
                            Thread.MemoryBarrier();
                            _export = null;
                        }
                    }

                    if(childContainer != null)
                    {
                        childContainer.Dispose();
                    }
                }
            }
        }
    }
}
