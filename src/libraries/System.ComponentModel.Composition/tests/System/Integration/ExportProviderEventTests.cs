// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ExportProviderEventTests
    {
        [Fact]
        public void BatchAdd_ShouldFireEvents()
        {
            var container = ContainerFactory.Create();
            var eventListener = new ExportProviderListener(container, container);

            var batch = new CompositionBatch();
            batch.AddExportedValue<object>("MyExport", new object());
            eventListener.VerifyCompose(batch);
        }

        [Fact]
        public void BatchRemove_ShouldFireEvents()
        {
            var container = ContainerFactory.Create();
            var batch = new CompositionBatch();
            var exportPart = batch.AddExportedValue<object>("MyExport", new object());
            container.Compose(batch);

            var eventListener = new ExportProviderListener(container, container);

            batch = new CompositionBatch();
            batch.RemovePart(exportPart);
            eventListener.VerifyCompose(batch);
        }

        [Fact]
        public void BatchAddRemove_ShouldFireEvents()
        {
            var container = ContainerFactory.Create();
            var batch = new CompositionBatch();
            var exportPart = batch.AddExportedValue<object>("MyExport", new object());
            container.Compose(batch);

            var eventListener = new ExportProviderListener(container, container);

            batch = new CompositionBatch();
            batch.RemovePart(exportPart);
            batch.AddExportedValue<object>("MyExport2", new object());
            eventListener.VerifyCompose(batch);
        }

        [Fact]
        public void BatchMultipleAdds_ShouldFireEvents()
        {
            var container = ContainerFactory.Create();
            var eventListener = new ExportProviderListener(container, container);

            var batch = new CompositionBatch();
            batch.AddExportedValue<object>("MyExport", new object());
            batch.AddExportedValue<object>("MyExport2", new object());
            batch.AddExportedValue<object>("MyExport3", new object());
            eventListener.VerifyCompose(batch);
        }

        [Fact]
        public void BatchNestedContainerAdds_ShouldFireEvents()
        {
            var parentContainer = ContainerFactory.Create();
            var container = ContainerFactory.Create(parentContainer);
            var eventListener = new ExportProviderListener(parentContainer, container);

            var batch = new CompositionBatch();
            batch.AddExportedValue<object>("MyExport", new object());
            eventListener.VerifyCompose(batch);
        }

        [Export]
        public class SampleCatalogExport { }

        [Fact]
        public void CatalogAdd_ShouldFireEvents()
        {
            var catalog = new TypeCatalog(typeof(SampleCatalogExport));
            var aggCat = new AggregateCatalog();
            var container = ContainerFactory.Create(aggCat);
            var eventListener = new ExportProviderListener(container, container);

            eventListener.VerifyCatalogAdd(() => aggCat.Catalogs.Add(catalog), typeof(SampleCatalogExport));
        }

        [Fact]
        public void CatalogRemove_ShouldFireEvents()
        {
            var catalog = new TypeCatalog(typeof(SampleCatalogExport));
            var aggCat = new AggregateCatalog();
            var container = ContainerFactory.Create(aggCat);

            aggCat.Catalogs.Add(catalog);
            var eventListener = new ExportProviderListener(container, container);

            eventListener.VerifyCatalogRemove(() => aggCat.Catalogs.Remove(catalog), typeof(SampleCatalogExport));
        }

        [Export]
        public class SampleCatalogExport2 { }

        [Fact]
        [ActiveIssue(812029)]
        public void CatalogMultipleAdds_ShouldFireEvents()
        {
            var catalog = new TypeCatalog(typeof(SampleCatalogExport));
            var aggCat = new AggregateCatalog();
            var container = ContainerFactory.Create(aggCat);
            var eventListener = new ExportProviderListener(container, container);

            var otherAggCat = new AggregateCatalog(new TypeCatalog(typeof(SampleCatalogExport)), new TypeCatalog(typeof(SampleCatalogExport2)));

            eventListener.VerifyCatalogAdd(() => aggCat.Catalogs.Add(otherAggCat), typeof(SampleCatalogExport), typeof(SampleCatalogExport2));
        }

        [Fact]
        public void CatalogNestedContainerAdds_ShouldFireEvents()
        {
            var catalog = new TypeCatalog(typeof(SampleCatalogExport));
            var aggCat = new AggregateCatalog();
            var parentContainer = ContainerFactory.Create(aggCat);
            var container = ContainerFactory.Create(parentContainer);
            var eventListener = new ExportProviderListener(parentContainer, container);

            eventListener.VerifyCatalogAdd(() => aggCat.Catalogs.Add(catalog), typeof(SampleCatalogExport));
        }

        public class ExportProviderListener
        {
            private CompositionContainer _container;
            private ExportProvider _watchedProvider;
            private string[] _expectedAdds;
            private string[] _expectedRemoves;
            private int _changedEventCount;
            private int _changingEventCount;

            public ExportProviderListener(CompositionContainer container, ExportProvider watchExportProvider)
            {
                watchExportProvider.ExportsChanged += OnExportsChanged;
                watchExportProvider.ExportsChanging += OnExportsChanging;
                this._watchedProvider = watchExportProvider;
                this._container = container;
            }

            public void VerifyCompose(CompositionBatch batch)
            {
                this._expectedAdds = GetContractNames(batch.PartsToAdd);
                this._expectedRemoves = GetContractNames(batch.PartsToRemove);

                this._container.Compose(batch);

                Assert.True(this._changingEventCount == 1);
                Assert.True(this._changedEventCount == 1);

                ResetState();
            }

            public void VerifyCatalogAdd(Action doAdd, params Type[] expectedTypesAdded)
            {
                this._expectedAdds = GetContractNames(expectedTypesAdded);

                doAdd();

                Assert.True(this._changingEventCount == 1);
                Assert.True(this._changedEventCount == 1);

                ResetState();
            }

            public void VerifyCatalogRemove(Action doRemove, params Type[] expectedTypesRemoved)
            {
                this._expectedRemoves = GetContractNames(expectedTypesRemoved);

                doRemove();

                Assert.True(this._changingEventCount == 1);
                Assert.True(this._changedEventCount == 1);

                ResetState();
            }

            public void OnExportsChanging(object sender, ExportsChangeEventArgs args)
            {
                Assert.True(this._expectedAdds != null || this._expectedRemoves != null);

                if (this._expectedAdds == null)
                {
                    Assert.Empty(args.AddedExports);
                }
                else
                {
                    Assert.All(_expectedAdds, add =>
                    {
                        Assert.False(this._container.IsPresent(add));
                    });
                }

                if (this._expectedRemoves == null)
                {
                    Assert.Empty(args.RemovedExports);
                }
                else
                {
                    Assert.All(_expectedRemoves, remove =>
                    {
                        Assert.True(this._container.IsPresent(remove));
                    });
                }

                this._changingEventCount++;
            }

            public void OnExportsChanged(object sender, ExportsChangeEventArgs args)
            {
                Assert.True(this._expectedAdds != null || this._expectedRemoves != null);

                if (this._expectedAdds == null)
                {
                    Assert.Empty(args.AddedExports);
                }
                else
                {
                    Assert.All(_expectedAdds, add =>
                    {
                        Assert.True(this._container.IsPresent(add));
                    });
                }

                if (this._expectedRemoves == null)
                {
                    Assert.Empty(args.RemovedExports);
                }
                else
                {
                    Assert.All(_expectedRemoves, remove =>
                    {
                        Assert.False(this._container.IsPresent(remove));
                    });
                }

                Assert.Null(args.AtomicComposition);

                this._changedEventCount++;
            }

            private void ResetState()
            {
                this._expectedAdds = null;
                this._expectedRemoves = null;
                this._changedEventCount = 0;
                this._changingEventCount = 0;
            }

            private static string[] GetContractNames(IEnumerable<ExportDefinition> definitions)
            {
                return definitions.Select(e => e.ContractName).ToArray();
            }

            private static string[] GetContractNames(IEnumerable<ComposablePart> parts)
            {
                return GetContractNames(parts.SelectMany(p => p.ExportDefinitions));
            }

            private static string[] GetContractNames(IEnumerable<Type> types)
            {
                return GetContractNames(types.Select(t => AttributedModelServices.CreatePartDefinition(t, null)).SelectMany(p => p.ExportDefinitions));
            }
        }
    }
}
