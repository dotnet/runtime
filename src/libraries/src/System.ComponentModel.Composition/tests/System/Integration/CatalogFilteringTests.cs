// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class CatalogFilteringTests
    {
        [Fact]
        public void FilteredCatalog_ScopeA()
        {
            var cat = GetCatalog();
            var contA = new CompositionContainer(ScopeCatalog(cat, "A"));

            Assert.True(contA.IsPresent<ScopeAComponent1>());
            Assert.True(contA.IsPresent<ScopeAComponent2>());
            Assert.False(contA.IsPresent<ScopeBComponent>());
            Assert.False(contA.IsPresent<ScopeCComponent>());
        }

        [Fact]
        public void FilteredCatalog_ScopeB()
        {
            var cat = GetCatalog();
            var contA = new CompositionContainer(ScopeCatalog(cat, "A"));
            var contB = new CompositionContainer(ScopeCatalog(cat, "B"), contA);

            Assert.True(contB.IsPresent<ScopeAComponent1>());
            Assert.True(contB.IsPresent<ScopeAComponent2>());
            Assert.True(contB.IsPresent<ScopeBComponent>());
            Assert.False(contB.IsPresent<ScopeCComponent>());
        }

        [Fact]
        public void FilteredCatalog_ScopeC()
        {
            var cat = GetCatalog();
            var contA = new CompositionContainer(ScopeCatalog(cat, "A"));
            var contB = new CompositionContainer(ScopeCatalog(cat, "B"), contA);
            var contC = new CompositionContainer(ScopeCatalog(cat, "C"), contB);

            Assert.True(contC.IsPresent<ScopeAComponent1>());
            Assert.True(contC.IsPresent<ScopeAComponent2>());
            Assert.True(contC.IsPresent<ScopeBComponent>());
            Assert.True(contC.IsPresent<ScopeCComponent>());
        }

        [Fact]
        [ActiveIssue(812029)]
        public void FilteredCatalog_EventsFired()
        {
            var aggCatalog = CatalogFactory.CreateAggregateCatalog();
            var cat1 = CatalogFactory.CreateAttributed(typeof(ScopeAComponent1), typeof(ScopeBComponent));

            var filteredCatalog = CatalogFactory.CreateFiltered(aggCatalog, 
                partDef => partDef.Metadata.ContainsKey("Scope") &&
                                    partDef.Metadata["Scope"].ToString() == "A");

            var container = ContainerFactory.Create(filteredCatalog);

            Assert.False(container.IsPresent<ScopeAComponent1>(), "sa before add");
            Assert.False(container.IsPresent<ScopeBComponent>(), "sb before add");

            aggCatalog.Catalogs.Add(cat1);

            Assert.True(container.IsPresent<ScopeAComponent1>(), "sa after add");
            Assert.False(container.IsPresent<ScopeBComponent>(), "sb after add");

            aggCatalog.Catalogs.Remove(cat1);

            Assert.False(container.IsPresent<ScopeAComponent1>(), "sa after remove");
            Assert.False(container.IsPresent<ScopeBComponent>(), "sb after remove");
        }

        private ComposablePartCatalog GetCatalog()
        {
            return CatalogFactory.CreateAttributed(
                typeof(ScopeAComponent1),
                typeof(ScopeAComponent2),
                typeof(ScopeBComponent),
                typeof(ScopeCComponent));
        }

        private ComposablePartCatalog ScopeCatalog(ComposablePartCatalog catalog, string scope)
        {
            return CatalogFactory.CreateFiltered(catalog,
                         partDef => partDef.Metadata.ContainsKey("Scope") &&
                                    partDef.Metadata["Scope"].ToString() == scope);
        }

        [Export]
        [PartMetadata("Scope", "A")]
        public class ScopeAComponent1
        {
        }

        [Export]
        [PartMetadata("Scope", "A")]
        public class ScopeAComponent2
        {
            [Import]
            public ScopeAComponent1 ScopeA { get; set; }
        }

        [Export]
        [PartMetadata("Scope", "B")]
        public class ScopeBComponent
        {
            [Import]
            public ScopeAComponent1 ScopeA { get; set; }
        }

        [Export]
        [PartMetadata("Scope", "C")]
        public class ScopeCComponent
        {
            [Import]
            public ScopeBComponent ScopeB { get; set; }
        }
    }
}
