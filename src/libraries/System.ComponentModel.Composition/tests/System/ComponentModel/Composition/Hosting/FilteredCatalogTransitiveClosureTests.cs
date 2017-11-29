// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition.Hosting
{
    public class FilteredCatalogTransitiveClosureTests
    {
        public interface IContract1 { }
        public interface IContract2 { }
        public interface IContract3 { }
        public interface IOther { }

        [Fact]
        public void IncludeDependentsSimpleChain()
        {
            var catalog = CreateCatalog(
                typeof(Exporter1),
                typeof(Exporter2),
                typeof(Exporter2Import1),
                typeof(Exporter3Import2),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract1>());
            Assert.Equal(1, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependents();
            Assert.Equal(3, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependentsExportFactory()
        {
            var catalog = CreateCatalog(
                typeof(Exporter1),
                typeof(Exporter2Import1AsExportFactory),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract1>());
            Assert.Equal(1, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependents();
            Assert.Equal(2, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependentsChainWithCycles()
        {
            var catalog = CreateCatalog(
                typeof(Exporter1),
                typeof(Exporter2),
                typeof(Exporter2Import1),
                typeof(Exporter1Import2),
                typeof(Exporter3Import2),
                typeof(Exporter2Import3),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract1>());
            Assert.Equal(2, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependents();
            Assert.Equal(5, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependentsSimpleChainOptional()
        {
            var catalog = CreateCatalog(
                typeof(Exporter2OptionalImport1),
                typeof(Exporter1),
                typeof(Exporter2),
                typeof(Exporter2Import1),
                typeof(Exporter3Import2),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract1>());
            Assert.Equal(1, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependents();
            Assert.Equal(3, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependentsChainWithCyclesOptional()
        {
            var catalog = CreateCatalog(
                typeof(Exporter2OptionalImport1),
                typeof(Exporter1),
                typeof(Exporter2),
                typeof(Exporter2Import1),
                typeof(Exporter1Import2),
                typeof(Exporter3Import2),
                typeof(Exporter2Import3),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract1>());
            Assert.Equal(2, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependents();
            Assert.Equal(5, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependentsSimpleChainOptionalOnly()
        {
            var catalog = CreateCatalog(
                typeof(Exporter2OptionalImport1),
                typeof(Exporter1),
                typeof(Exporter2),
                typeof(Exporter2Import1),
                typeof(Exporter3Import2),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract1>());
            Assert.Equal(1, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependents(i => i.Cardinality == ImportCardinality.ZeroOrOne);
            Assert.Equal(2, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependentsChainWithCyclesOptionalOnly()
        {
            var catalog = CreateCatalog(
                typeof(Exporter2OptionalImport1),
                typeof(Exporter1),
                typeof(Exporter2),
                typeof(Exporter2Import1),
                typeof(Exporter1Import2),
                typeof(Exporter3Import2),
                typeof(Exporter2Import3),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract1>());
            Assert.Equal(2, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependents(i => i.Cardinality == ImportCardinality.ZeroOrOne);
            Assert.Equal(3, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependentsOpenGenericToOpenGeneric()
        {
            var catalog = CreateCatalog(
                typeof(OpenGenericExporter<,>),
                typeof(OpenGenericImporter<,>),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Imports<IContract2>());
            Assert.Equal(1, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependents();
            Assert.Equal(2, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependentsOpenGenericToOpenGenericReverse()
        {
            var catalog = CreateCatalog(
                typeof(OpenGenericExporter<,>),
                typeof(OpenGenericImporterReverseOrder<,>),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Imports<IContract2>());
            Assert.Equal(1, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependents();
            Assert.Equal(2, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependentsOpenGenericToClosedGeneric()
        {
            var catalog = CreateCatalog(
                typeof(OpenGenericExporter<,>),
                typeof(ClosedGenericImporter),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Imports<IContract2>());
            Assert.Equal(1, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependents();
            Assert.Equal(2, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependentsClosedToClosed()
        {
            var catalog = CreateCatalog(
                typeof(SpecificGenericExporter),
                typeof(ClosedGenericImporter),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Imports<IContract2>());
            Assert.Equal(1, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependents();
            Assert.Equal(2, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependenciesSimpleChain()
        {
            var catalog = CreateCatalog(
                typeof(Exporter1),
                typeof(Exporter2),
                typeof(Exporter2Import1),
                typeof(Exporter3Import2),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract2>());
            Assert.Equal(2, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependencies();
            Assert.Equal(3, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependenciesExportFactory()
        {
            var catalog = CreateCatalog(
                typeof(Exporter1),
                typeof(Exporter2Import1AsExportFactory),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract2>());
            Assert.Equal(1, filteredCatalog.Parts.Count());

            var dependenciesCatalog = filteredCatalog.IncludeDependencies();
            Assert.Equal(2, dependenciesCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependenciesChainWithCycles()
        {
            var catalog = CreateCatalog(
                typeof(Exporter1),
                typeof(Exporter2),
                typeof(Exporter2Import1),
                typeof(Exporter1Import2),
                typeof(Exporter3Import2),
                typeof(Exporter2Import3),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract2>());
            Assert.Equal(3, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependents();
            Assert.Equal(5, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependenciesSimpleChainOptional()
        {
            var catalog = CreateCatalog(
                typeof(Exporter3OptionalImport2),
                typeof(Exporter1),
                typeof(Exporter2),
                typeof(Exporter3),
                typeof(Exporter2Import1),
                typeof(Exporter3Import2),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract2>());
            Assert.Equal(2, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependencies();
            Assert.Equal(3, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependenciesChainWithCyclesOptional()
        {
            var catalog = CreateCatalog(
                typeof(Exporter1OptionalImport3),
                typeof(Exporter1),
                typeof(Exporter2),
                typeof(Exporter3),
                typeof(Exporter2Import1),
                typeof(Exporter1Import2),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract2>());
            Assert.Equal(2, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependencies();
            Assert.Equal(5, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependenciesSimpleChainOptionalOnly()
        {
            var catalog = CreateCatalog(
                typeof(Exporter2OptionalImport3),
                typeof(Exporter1),
                typeof(Exporter2),
                typeof(Exporter3),
                typeof(Exporter2Import1),
                typeof(Exporter1Import2),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract2>());
            Assert.Equal(3, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependencies(i => i.Cardinality == ImportCardinality.ZeroOrOne);
            Assert.Equal(4, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependenciesChainWithCyclesOptionalOnly()
        {
            var catalog = CreateCatalog(
                typeof(Exporter3OptionalImport2),
                typeof(Exporter2OptionalImport3),
                typeof(Exporter1),
                typeof(Exporter2),
                typeof(Exporter3),
                typeof(Exporter1Import2),
                typeof(IOther));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract2>());
            Assert.Equal(2, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependencies(i => i.Cardinality == ImportCardinality.ZeroOrOne);
            Assert.Equal(4, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependciesOpenGenericToOpenGeneric()
        {
            var catalog = CreateCatalog(
                typeof(OpenGenericExporter<,>),
                typeof(OpenGenericImporter<,>),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract3>());
            Assert.Equal(1, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependencies();
            Assert.Equal(2, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependenciesOpenGenericToOpenGenericReverse()
        {
            var catalog = CreateCatalog(
                typeof(OpenGenericExporter<,>),
                typeof(OpenGenericImporterReverseOrder<,>),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract3>());
            Assert.Equal(1, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependencies();
            Assert.Equal(2, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependenciesOpenGenericToClosedGeneric()
        {
            var catalog = CreateCatalog(
                typeof(OpenGenericExporter<,>),
                typeof(ClosedGenericImporter),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract1>());
            Assert.Equal(1, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependencies();
            Assert.Equal(2, dependentsCatalog.Parts.Count());
        }

        [Fact]
        public void IncludeDependenciesClosedToClosed()
        {
            var catalog = CreateCatalog(
                typeof(SpecificGenericExporter),
                typeof(ClosedGenericImporter),
                typeof(Other));
            var filteredCatalog = catalog.Filter(p => p.Exports<IContract1>());
            Assert.Equal(1, filteredCatalog.Parts.Count());

            var dependentsCatalog = filteredCatalog.IncludeDependencies();
            Assert.Equal(2, dependentsCatalog.Parts.Count());
        }

        [Export(typeof(IOther))]
        public class Other : IOther
        {
            [Import]
            public IOther Import { get; set; }
        }

        [Export(typeof(IContract1))]
        public class Exporter1 : IContract1
        {
        }

        [Export(typeof(IContract2))]
        public class Exporter2 : IContract2
        {
        }

        [Export(typeof(IContract3))]
        public class Exporter3 : IContract3
        {
        }

        [Export(typeof(IContract2))]
        public class Exporter2Import1 : IContract2
        {
            [Import]
            public IContract1 Import { get; set; }
        }

        [Export(typeof(IContract2))]
        public class Exporter2Import1AsExportFactory : IContract2
        {
            [Import]
            public ExportFactory<IContract1> Import { get; set; }
        }

        [Export(typeof(IContract2))]
        public class Exporter2OptionalImport1 : IContract2
        {
            [Import(AllowDefault = true)]
            public IContract1 Import { get; set; }
        }

        [Export(typeof(IContract1))]
        public class Exporter1OptionalImport3 : IContract1
        {
            [Import(AllowDefault = true)]
            public IContract3 Import { get; set; }
        }

        [Export(typeof(IContract3))]
        public class Exporter3OptionalImport2 : IContract3
        {
            [Import(AllowDefault = true)]
            public IContract2 Import { get; set; }
        }

        [Export(typeof(IContract3))]
        public class Exporter3OptionalImport1 : IContract3
        {
            [Import(AllowDefault = true)]
            public IContract1 Import { get; set; }
        }

        [Export(typeof(IContract2))]
        public class Exporter2OptionalImport3 : IContract2
        {
            [Import(AllowDefault = true)]
            public IContract3 Import { get; set; }
        }

        [Export(typeof(IContract1))]
        public class Exporter1Import2 : IContract1
        {
            [Import]
            public IContract2 Import { get; set; }
        }

        [Export(typeof(IContract3))]
        public class Exporter3Import2 : IContract3
        {
            [Import]
            public IContract2 Import { get; set; }
        }

        [Export(typeof(IContract2))]
        public class Exporter2Import3 : IContract2
        {
            [Import]
            public IContract3 Import { get; set; }
        }

        public interface IContract<T1, T2> { }

        [Export(typeof(IContract<,>))]
        public class OpenGenericExporter<T1, T2> : IContract<T1, T2>
        {
            [Import]
            public IContract2 Import { get; set; }
        }

        [Export(typeof(IContract3))]
        public class OpenGenericImporter<T1, T2> : IContract3
        {
            [Import]
            IContract<T1, T2> Import { get; set; }
        }

        [Export(typeof(IContract3))]
        public class OpenGenericImporterReverseOrder<T1, T2> : IContract3
        {
            [Import]
            IContract<T2, T1> Import { get; set; }
        }

        [Export(typeof(IContract1))]
        public class ClosedGenericImporter : IContract1
        {
            [Import]
            IContract<string, string> Import { get; set; }
        }

        [Export(typeof(IContract<string, string>))]
        public class SpecificGenericExporter : IContract<string, string>
        {
            [Import]
            public IContract2 Import { get; set; }
        }

        public ComposablePartCatalog CreateCatalog(params Type[] types)
        {
            return new TypeCatalog(types);
        }
    }
}
