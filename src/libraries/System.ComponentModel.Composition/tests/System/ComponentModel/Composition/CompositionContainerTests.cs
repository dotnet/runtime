// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Linq.Expressions;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class CompositionContainerTests
    {
        [Fact]
        public void Constructor2_ArrayWithNullElementAsProvidersArgument_ShouldThrowArgumentException()
        {
            Assert.Throws<ArgumentException>("providers", () =>
            {
                new CompositionContainer(new ExportProvider[] { null });
            });
        }

        [Fact]
        public void Constructor3_ArrayWithNullElementAsProvidersArgument_ShouldThrowArgumentException()
        {
            var catalog = CatalogFactory.Create();

            Assert.Throws<ArgumentException>("providers", () =>
            {
                new CompositionContainer(catalog, new ExportProvider[] { null });
            });
        }

        [Fact]
        public void Constructor2_ArrayAsProvidersArgument_ShouldNotAllowModificationAfterConstruction()
        {
            var providers = new ExportProvider[] { ExportProviderFactory.Create() };
            var container = new CompositionContainer(providers);

            providers[0] = null;

            Assert.NotNull(container.Providers[0]);
        }

        [Fact]
        public void Constructor3_ArrayAsProvidersArgument_ShouldNotAllowModificationAfterConstruction()
        {
            var providers = new ExportProvider[] { ExportProviderFactory.Create() };
            var container = new CompositionContainer(CatalogFactory.Create(), providers);

            providers[0] = null;

            Assert.NotNull(container.Providers[0]);
        }

        [Fact]
        public void Constructor1_ShouldSetProvidersPropertyToEmptyCollection()
        {
            var container = new CompositionContainer();

            Assert.Empty(container.Providers);
        }

        [Fact]
        public void Constructor2_EmptyArrayAsProvidersArgument_ShouldSetProvidersPropertyToEmpty()
        {
            var container = new CompositionContainer(new ExportProvider[0]);

            Assert.Empty(container.Providers);
        }

        [Fact]
        public void Constructor3_EmptyArrayAsProvidersArgument_ShouldSetProvidersPropertyToEmpty()
        {
            var container = new CompositionContainer(CatalogFactory.Create(), new ExportProvider[0]);

            Assert.Empty(container.Providers);
        }

        [Fact]
        public void Constructor1_ShouldSetCatalogPropertyToNull()
        {
            var container = new CompositionContainer();

            Assert.Null(container.Catalog);
        }

        [Fact]
        public void Constructor2_ShouldSetCatalogPropertyToNull()
        {
            var container = new CompositionContainer(new ExportProvider[0]);

            Assert.Null(container.Catalog);
        }

        [Fact]
        public void Constructor3_NullAsCatalogArgument_ShouldSetCatalogPropertyToNull()
        {
            var container = new CompositionContainer((ComposablePartCatalog)null, new ExportProvider[0]);

            Assert.Null(container.Catalog);
        }

        [Fact]
        public void Constructor3_ValueAsCatalogArgument_ShouldSetCatalogProperty()
        {
            var expectations = Expectations.GetCatalogs();

            foreach (var e in expectations)
            {
                var container = new CompositionContainer(e, new ExportProvider[0]);

                Assert.Same(e, container.Catalog);
            }
        }

        [Fact]
        public void Catalog_WhenDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                var catalog = container.Catalog;
            });
        }

        [Fact]
        public void Providers_WhenDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                var providers = container.Providers;
            });
        }

        [Fact]
        [ActiveIssue(579990)]  // NullReferenceException
        public void ExportsChanged_Add_WhenDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.ExportsChanged += (o, s) => { };
            });
        }

        [Fact]
        [ActiveIssue(579990)] // NullReferenceException
        public void ExportsChanged_Remove_WhenDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.ExportsChanged -= (o, s) => { };
            });
        }

        [Fact]
        public void AddPart1_ImportOnlyPart_ShouldNotGetGarbageCollected()
        {
            var container = CreateCompositionContainer();

            var import = PartFactory.CreateImporter("Value", ImportCardinality.ZeroOrMore);

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(import);
            container.Compose(batch);

            var weakRef = new WeakReference(import);
            import = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.NotNull(weakRef.Target);

            GC.KeepAlive(container);
        }

        [Fact]
        public void Compose_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            CompositionBatch batch = new CompositionBatch();
            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void GetExportOfT1_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExport<string>();
            });
        }

        [Fact]
        public void GetExportOfT2_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExport<string>("Contract");
            });
        }

        [Fact]
        public void GetExportOfTTMetadataView1_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExport<string, object>();
            });
        }

        [Fact]
        public void GetExportOfTTMetadataView2_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExport<string, object>("Contract");
            });
        }

        [Fact]
        public void GetExports1_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            var definition = ImportDefinitionFactory.Create();
            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExports(definition);
            });
        }

        [Fact]
        public void GetExports2_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExports(typeof(string), typeof(object), "Contract");
            });
        }

        [Fact]
        public void GetExportsOfT1_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExports<string>();
            });
        }

        [Fact]
        public void GetExportsOfT2_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExports<string>("Contract");
            });
        }

        [Fact]
        public void GetExportsOfTTMetadataView1_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExports<string, object>();
            });
        }

        [Fact]
        public void GetExportsOfTTMetadataView2_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExports<string, object>("Contract");
            });
        }

        [Fact]
        public void GetExportedValueOfT1_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExportedValue<string>();
            });
        }

        [Fact]
        public void GetExportedValueOfT2_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExportedValue<string>("Contract");
            });
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT1_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExportedValueOrDefault<string>();
            });
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT2_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExportedValueOrDefault<string>("Contract");
            });
        }

        [Fact]
        public void GetExportedValuesOfT1_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExportedValues<string>();
            });
        }

        [Fact]
        public void GetExportedValuesOfT2_WhenContainerDisposed_ShouldThrowObjectDisposed()
        {
            var container = CreateCompositionContainer();
            container.Dispose();

            ExceptionAssert.ThrowsDisposed(container, () =>
            {
                container.GetExportedValues<string>("Contract");
            });
        }

        [Fact]
        public void GetExports1_NullAsImportDefinitionArgument_ShouldThrowArgumentNull()
        {
            var container = CreateCompositionContainer();

            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                container.GetExports((ImportDefinition)null);
            });
        }

        [Fact]
        public void GetExports2_NullAsTypeArgument_ShouldThrowArgumentNull()
        {
            var container = CreateCompositionContainer();

            Assert.Throws<ArgumentNullException>("type", () =>
            {
                container.GetExports((Type)null, typeof(string), "ContractName");
            });
        }

        [Fact]
        public void GetExportOfT1_AskingForContractThatDoesNotExist_ShouldThrowCardinalityMismatch()
        {
            var container = CreateCompositionContainer();

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExport<string>();
            });
        }

        [Fact]
        public void GetExportOfT2_AskingForContractThatDoesNotExist_ShouldThrowCardinalityMismatch()
        {
            var container = CreateCompositionContainer();

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExport<string>("Contract");
            });
        }

        [Fact]
        public void GetExportOfTTMetadataView1_AskingForContractThatDoesNotExist_ShouldThrowCardinalityMismatch()
        {
            var container = CreateCompositionContainer();

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExport<string, object>();
            });
        }

        [Fact]
        public void GetExportOfTTMetadataView2_AskingForContractThatDoesNotExist_ShouldThrowCardinalityMismatch()
        {
            var container = CreateCompositionContainer();

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExport<string, object>("Contract");
            });
        }

        [Fact]
        public void GetExports1_DefinitionAskingForExactlyOneContractThatDoesNotExist_ShouldThrowCardinalityMismatch()
        {
            var container = CreateCompositionContainer();

            var definition = ImportDefinitionFactory.Create("Contract", ImportCardinality.ExactlyOne);

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExports(definition);
            });
        }

        [Fact]
        public void GetExports1_DefinitionAskingForExactlyZeroOrOneContractThatDoesNotExist_ShouldReturnEmpty()
        {
            var container = CreateCompositionContainer();

            var definition = ImportDefinitionFactory.Create("Contract", ImportCardinality.ZeroOrOne);

            var exports = container.GetExports(definition);

            Assert.Empty(exports);
        }

        [Fact]
        public void GetExports1_DefinitionAskingForExactlyZeroOrMoreContractThatDoesNotExist_ShouldReturnEmpty()
        {
            var container = CreateCompositionContainer();

            var definition = ImportDefinitionFactory.Create("Contract", ImportCardinality.ZeroOrMore);

            var exports = container.GetExports(definition);

            Assert.Empty(exports);
        }

        [Fact]
        public void GetExports2_AskingForContractThatDoesNotExist_ShouldReturnNoExports()
        {
            var container = CreateCompositionContainer();

            var exports = container.GetExports(typeof(string), (Type)null, "Contract");

            Assert.Empty(exports);
        }

        [Fact]
        public void GetExportsOfT1_AskingForContractThatDoesNotExist_ShouldReturnEmpty()
        {
            var container = CreateCompositionContainer();

            var exports = container.GetExports<string>();

            Assert.Empty(exports);
        }

        [Fact]
        public void GetExportsOfT2_AskingForContractThatDoesNotExist_ShouldReturnEmpty()
        {
            var container = CreateCompositionContainer();

            var exports = container.GetExports<string>("Contract");

            Assert.Empty(exports);
        }

        [Fact]
        public void GetExportsOfTTMetadataView1_AskingForContractThatDoesNotExist_ShouldReturnEmpty()
        {
            var container = CreateCompositionContainer();

            var exports = container.GetExports<string, object>();

            Assert.Empty(exports);
        }

        [Fact]
        public void GetExportsOfTTMetadataView2_AskingForContractThatDoesNotExist_ShouldReturnEmpty()
        {
            var container = CreateCompositionContainer();

            var exports = container.GetExports<string, object>("Contract");

            Assert.Empty(exports);
        }

        [Fact]
        public void GetExportedValueOfT1_AskingForContractThatDoesNotExist_ShouldThrowCardinalityMismatch()
        {
            var container = CreateCompositionContainer();

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExportedValue<string>();
            });
        }

        [Fact]
        public void GetExportedValueOfT2_AskingForContractThatDoesNotExist_ShouldThrowCardinalityMismatch()
        {
            var container = CreateCompositionContainer();

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExportedValue<string>("Contract");
            });
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT1_AskingForStringContractThatDoesNotExist_ShouldReturnNull()
        {
            var container = CreateCompositionContainer();

            var exportedValue = container.GetExportedValueOrDefault<string>();

            Assert.Null(exportedValue);
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT2_AskingForStringContractThatDoesNotExist_ShouldReturnNull()
        {
            var container = CreateCompositionContainer();

            var exportedValue = container.GetExportedValueOrDefault<string>("Contract");

            Assert.Null(exportedValue);
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT1_AskingForInt32ContractThatDoesNotExist_ShouldReturnZero()
        {
            var container = CreateCompositionContainer();

            var exportedValue = container.GetExportedValueOrDefault<int>();

            Assert.Equal(0, exportedValue);
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT2_AskingForInt32ContractThatDoesNotExist_ShouldReturnZero()
        {
            var container = CreateCompositionContainer();

            var exportedValue = container.GetExportedValueOrDefault<int>("Contract");

            Assert.Equal(0, exportedValue);
        }

        [Fact]
        public void GetExportedValuesOfT1_AskingForContractThatDoesNotExist_ShouldReturnEmpty()
        {
            var container = CreateCompositionContainer();

            var exports = container.GetExportedValues<string>();

            Assert.Empty(exports);
        }

        [Fact]
        public void GetExportedValuesOfT2_AskingForContractThatDoesNotExist_ShouldReturnEmpty()
        {
            var container = CreateCompositionContainer();

            var exports = container.GetExports<string>("Contract");

            Assert.Empty(exports);
        }

        [Fact]
        public void GetExportOfT1_AskingForContractWithOneExport_ShouldReturnExport()
        {
            var container = ContainerFactory.Create(new MicroExport(ContractFromType(typeof(string)), "Value"));

            var export = container.GetExport<string>();

            Assert.Equal("Value", export.Value);
        }

        [Fact]
        public void GetExportOfT2_AskingForContractWithOneExport_ShouldReturnExport()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value"));

            var export = container.GetExport<string>("Contract");

            Assert.Equal("Value", export.Value);
        }

        [Fact]
        public void GetExportOfTTMetadataView1_AskingForContractWithOneExport_ShouldReturnExport()
        {
            var container = ContainerFactory.Create(new MicroExport(ContractFromType(typeof(string)), "Value"));

            var export = container.GetExport<string, object>();

            Assert.Equal("Value", export.Value);
        }

        [Fact]
        public void GetExportOfTTMetadataView2_AskingForContractWithOneExport_ShouldReturnExport()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value"));

            var export = container.GetExport<string, object>("Contract");

            Assert.Equal("Value", export.Value);
        }

        [Fact]
        public void GetExports1_AskingForExactlyOneContractWithOneExport_ShouldReturnExport()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value"));

            var definition = ImportDefinitionFactory.Create("Contract", ImportCardinality.ExactlyOne);

            var exports = container.GetExports(definition);

            ExportsAssert.AreEqual(exports, "Value");
        }

        [Fact]
        public void GetExports1_AskingForZeroOrOneContractWithOneExport_ShouldReturnExport()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value"));

            var definition = ImportDefinitionFactory.Create("Contract", ImportCardinality.ZeroOrOne);

            var exports = container.GetExports(definition);

            ExportsAssert.AreEqual(exports, "Value");
        }

        [Fact]
        public void GetExports1_AskingForZeroOrMoreContractWithOneExport_ShouldReturnExport()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value"));

            var definition = ImportDefinitionFactory.Create("Contract", ImportCardinality.ZeroOrMore);

            var exports = container.GetExports(definition);

            ExportsAssert.AreEqual(exports, "Value");
        }

        [Fact]
        public void GetExports2_AskingForContractWithOneExport_ShouldReturnOneExport()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value"));

            var exports = container.GetExports(typeof(string), (Type)null, "Contract");

            ExportsAssert.AreEqual(exports, "Value");
        }

        [Fact]
        public void GetExportsOfT1_AskingForContractWithOneExport_ShouldReturnOneExport()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), "Value"));

            var exports = container.GetExports<string>();

            ExportsAssert.AreEqual(exports, "Value");
        }

        [Fact]
        public void GetExportsOfT2_AskingForContractWithOneExport_ShouldReturnOneExport()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value"));

            var exports = container.GetExports<string>("Contract");

            ExportsAssert.AreEqual(exports, "Value");
        }

        [Fact]
        public void GetExportsOfTTMetadataView1_AskingForContractWithOneExport_ShouldReturnOneExport()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), "Value"));

            var exports = container.GetExports<string, object>();

            ExportsAssert.AreEqual(exports, "Value");
        }

        [Fact]
        public void GetExportsOfTTMetadataView2_AskingForContractWithOneExport_ShouldReturnOneExport()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value"));

            var exports = container.GetExports<string, object>("Contract");

            ExportsAssert.AreEqual(exports, "Value");
        }

        [Fact]
        public void GetExportedValueOfT1_AskingForContractWithOneExport_ShouldReturnExport()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), "Value"));

            var exportedValue = container.GetExportedValue<string>();

            Assert.Equal("Value", exportedValue);
        }

        [Fact]
        public void GetExportedValueOfT2_AskingForContractWithOneExport_ShouldReturnExport()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value"));

            var exportedValue = container.GetExportedValue<string>("Contract");

            Assert.Equal("Value", exportedValue);
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT1_AskingForContractWithOneExport_ShouldReturnExport()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), "Value"));

            var exportedValue = container.GetExportedValueOrDefault<string>();

            Assert.Equal("Value", exportedValue);
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT2_AskingForContractWithOneExport_ShouldReturnExport()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value"));

            var exportedValue = container.GetExportedValueOrDefault<string>("Contract");

            Assert.Equal("Value", exportedValue);
        }

        [Fact]
        public void GetExportedValuesOfT1_AskingForContractWithOneExport_ShouldReturnOneExport()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), "Value"));

            var exportedValues = container.GetExportedValues<string>();

            EnumerableAssert.AreEqual(exportedValues, "Value");
        }

        [Fact]
        public void GetExportedValuesOfT2_AskingForContractWithOneExport_ShouldReturnOneExport()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value"));

            var exportedValues = container.GetExportedValues<string>("Contract");

            EnumerableAssert.AreEqual(exportedValues, "Value");
        }

        [Fact]
        public void GetExportOfT1_AskingForContractWithMultipleExports_ShouldThrowCardinalityMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport(ContractFromType(typeof(string)), "Value1", "Value2"));

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
             {
                 container.GetExport<string>();
             });
        }

        [Fact]
        public void GetExportOfT2_AskingForContractWithMultipleExports_ShouldThrowCardinalityMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value1", "Value2"));

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
             {
                 container.GetExport<string>("Contract");
             });
        }

        [Fact]
        public void GetExportOfTTMetadataView1_AskingForContractWithMultipleExports_ShouldThrowCardinalityMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport(ContractFromType(typeof(string)), "Value1", "Value2"));

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExport<string, object>();
            });
        }

        [Fact]
        public void GetExportOfTTMetadataView2_AskingForContractWithMultipleExports_ShouldThrowCardinalityMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value1", "Value2"));

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExport<string, object>("Contract");
            });
        }

        [Fact]
        public void GetExports1_AskingForExactlyOneContractWithMultipleExports_ShouldThrowCardinalityMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value1", "Value2"));

            var definition = ImportDefinitionFactory.Create("Contract", ImportCardinality.ExactlyOne);

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExports(definition);
            });
        }

        [Fact]
        public void GetExports1_AskingForZeroOrOneContractWithMultipleExports_ShouldReturnZero()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value1", "Value2"));

            var definition = ImportDefinitionFactory.Create("Contract", ImportCardinality.ZeroOrOne);

            Assert.Equal(0, container.GetExports(definition).Count());
        }

        [Fact]
        public void GetExports1_AskingForZeroOrMoreContractWithMultipleExports_ShouldReturnMultipleExports()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value1", "Value2"));

            var definition = ImportDefinitionFactory.Create("Contract", ImportCardinality.ZeroOrMore);

            var exports = container.GetExports(definition);

            ExportsAssert.AreEqual(exports, "Value1", "Value2");
        }

        [Fact]
        public void GetExports2_AskingForContractWithMultipleExports_ShouldReturnMultipleExports()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value1", "Value2"));

            var exports = container.GetExports(typeof(string), (Type)null, "Contract");

            ExportsAssert.AreEqual(exports, "Value1", "Value2");
        }

        [Fact]
        public void GetExportsOfT1_AskingForContractWithMultipleExports_ShouldReturnMultipleExports()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), "Value1", "Value2"));

            var exports = container.GetExports<string>();

            ExportsAssert.AreEqual(exports, "Value1", "Value2");
        }

        [Fact]
        public void GetExportsOfT2_AskingForContractWithMultipleExports_ShouldReturnMultipleExports()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value1", "Value2"));

            var exports = container.GetExports<string>("Contract");

            ExportsAssert.AreEqual(exports, "Value1", "Value2");
        }

        [Fact]
        public void GetExportsOfTTMetadataView1_AskingForContractWithMultipleExports_ShouldReturnMultipleExports()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), "Value1", "Value2"));

            var exports = container.GetExports<string, object>();

            ExportsAssert.AreEqual(exports, "Value1", "Value2");
        }

        [Fact]
        public void GetExportsOfTTMetadataView2_AskingForContractWithMultipleExports_ShouldReturnMultipleExports()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value1", "Value2"));

            var exports = container.GetExports<string, object>("Contract");

            ExportsAssert.AreEqual(exports, "Value1", "Value2");
        }

        [Fact]
        public void GetExportedValueOfT1_AskingForContractWithMultipleExports_ShouldThrowCardinalityMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), "Value1", "Value2"));

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExportedValue<string>();
            });
        }

        [Fact]
        public void GetExportedValueOfT2_AskingForContractWithMultipleExports_ShouldThrowCardinalityMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value1", "Value2"));

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExportedValue<string>("Contract");
            });
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT1_AskingForContractWithMultipleExports_ShouldReturnZero()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), "Value1", "Value2"));

            Assert.Null(container.GetExportedValueOrDefault<string>());
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT2_AskingForContractWithMultipleExports_ShouldReturnZero()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", "Value1", "Value2"));

            Assert.Null(container.GetExportedValueOrDefault<string>("Contract"));
        }

        [Fact]
        public void GetExportedValuesOfT1_AskingForContractWithMultipleExports_ShouldReturnMultipleExports()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), "Value1", "Value2"));

            var exportedValues = container.GetExportedValues<string>();

            EnumerableAssert.AreEqual(exportedValues, "Value1", "Value2");
        }

        [Fact]
        public void GetExportedValuesOfT2_AskingForContractWithMultipleExports_ShouldReturnMultipleExports()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", typeof(string), "Value1", "Value2"));

            var exportedValues = container.GetExportedValues<string>("Contract");

            EnumerableAssert.AreEqual(exportedValues, "Value1", "Value2");
        }

        [Fact]
        public void GetExports1_AskingForExactlyOneAndAll_ShouldThrowCardinalityMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract1", "Value1", "Value2", "Value3"),
                                                    new MicroExport("Contract2", "Value4", "Value5", "Value6"));

            var definition = ImportDefinitionFactory.Create(import => true, ImportCardinality.ExactlyOne);

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExports(definition);
            });
        }

        [Fact]
        public void GetExports1_AskingForZeroOrOneAndAll_ShouldReturnZero()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract1", "Value1", "Value2", "Value3"),
                                                    new MicroExport("Contract2", "Value4", "Value5", "Value6"));

            var definition = ImportDefinitionFactory.Create(import => true, ImportCardinality.ZeroOrOne);

            Assert.Equal(0, container.GetExports(definition).Count());
        }

        [Fact]
        public void GetExports1_AskingForZeroOrMoreAndAll_ShouldReturnAll()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract1", "Value1", "Value2", "Value3"),
                                                    new MicroExport("Contract2", "Value4", "Value5", "Value6"));

            var definition = ImportDefinitionFactory.Create(import => true, ImportCardinality.ZeroOrMore);

            var exports = container.GetExports(definition);

            ExportsAssert.AreEqual(exports, "Value1", "Value2", "Value3",
                                            "Value4", "Value5", "Value6");
        }

        [Fact]
        public void GetExportOfT1_StringAsTTypeArgumentAskingForContractWithOneObjectExport_ShouldThrowContractMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), new object()));

            var export = container.GetExport<string>();

            ExceptionAssert.Throws<CompositionContractMismatchException>(() =>
            {
                var value = export.Value;
            });
        }

        [Fact]
        public void GetExportOfT2_StringAsTTypeArgumentAskingForContractWithOneObjectExport_ShouldThrowContractMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", typeof(string), new object()));

            var export = container.GetExport<string>("Contract");

            ExceptionAssert.Throws<CompositionContractMismatchException>(() =>
            {
                var value = export.Value;
            });
        }

        [Fact]
        public void GetExportOfTTMetadataView1_StringAsTTypeArgumentAskingForContractWithOneObjectExport_ShouldThrowContractMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), new object()));

            var export = container.GetExport<string, object>();

            ExceptionAssert.Throws<CompositionContractMismatchException>(() =>
            {
                var value = export.Value;
            });
        }

        [Fact]
        public void GetExportOfTTMetadataView2_StringAsTTypeArgumentAskingForContractWithOneObjectExport_ShouldThrowContractMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", typeof(string), new object()));

            var export = container.GetExport<string, object>("Contract");

            ExceptionAssert.Throws<CompositionContractMismatchException>(() =>
            {
                var value = export.Value;
            });
        }

        [Fact]
        public void GetExports2_StringAsTTypeArgumentAskingForContractWithOneObjectExport_ShouldThrowContractMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", typeof(string), new object()));

            var exports = container.GetExports(typeof(string), (Type)null, "Contract");

            Assert.Equal(1, exports.Count());

            var export = exports.ElementAt(0);

            ExceptionAssert.Throws<CompositionContractMismatchException>(() =>
            {
                var value = export.Value;
            });
        }

        [Fact]
        public void GetExportsOfT1_StringAsTTypeArgumentAskingForContractWithOneObjectExport_ShouldThrowContractMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), new object()));

            var exports = container.GetExports<string>();

            Assert.Equal(1, exports.Count());

            var export = exports.ElementAt(0);

            ExceptionAssert.Throws<CompositionContractMismatchException>(() =>
            {
                var value = export.Value;
            });
        }

        [Fact]
        public void GetExportsOfT2_StringAsTTypeArgumentAskingForContractWithOneObjectExport_ShouldThrowContractMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", typeof(string), new object()));

            var exports = container.GetExports<string>("Contract");

            Assert.Equal(1, exports.Count());

            var export = exports.ElementAt(0);

            ExceptionAssert.Throws<CompositionContractMismatchException>(() =>
            {
                var value = export.Value;
            });
        }

        [Fact]
        public void GetExportsOfTTMetadataView1_StringAsTTypeArgumentAskingForContractWithOneObjectExport_ShouldThrowContractMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), new object()));

            var exports = container.GetExports<string, object>();

            Assert.Equal(1, exports.Count());

            var export = exports.ElementAt(0);

            ExceptionAssert.Throws<CompositionContractMismatchException>(() =>
            {
                var value = export.Value;
            });
        }

        [Fact]
        public void GetExportsOfTTMetadataView2_StringAsTTypeArgumentAskingForContractWithOneObjectExport_ShouldThrowContractMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", typeof(string), new object()));

            var exports = container.GetExports<string, object>("Contract");

            Assert.Equal(1, exports.Count());

            var export = exports.ElementAt(0);

            ExceptionAssert.Throws<CompositionContractMismatchException>(() =>
            {
                var value = export.Value;
            });
        }

        [Fact]
        public void GetExportedValueOfT1_StringAsTTypeArgumentAskingForContractWithObjectExport_ShouldThrowContractMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), new object()));

            ExceptionAssert.Throws<CompositionContractMismatchException>(() =>
            {
                container.GetExportedValue<string>();
            });
        }

        [Fact]
        public void GetExportedValueOfT2_StringAsTTypeArgumentAskingForContractWithObjectExport_ShouldThrowContractMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", typeof(string), new object()));

            ExceptionAssert.Throws<CompositionContractMismatchException>(() =>
            {
                container.GetExportedValue<string>("Contract");
            });
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT1_StringAsTTypeArgumentAskingForContractWithObjectExport_ShouldThrowContractMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), new object()));

            ExceptionAssert.Throws<CompositionContractMismatchException>(() =>
            {
                container.GetExportedValueOrDefault<string>();
            });
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT2_StringAsTTypeArgumentAskingForContractWithObjectExport_ShouldThrowContractMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", typeof(string), new object()));

            ExceptionAssert.Throws<CompositionContractMismatchException>(() =>
            {
                container.GetExportedValueOrDefault<string>("Contract");
            });
        }

        [Fact]
        public void GetExportedValuesOfT1_StringAsTTypeArgumentAskingForContractWithOneObjectExport_ShouldThrowContractMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport(typeof(string), new object()));

            ExceptionAssert.Throws<CompositionContractMismatchException>(() =>
            {
                container.GetExportedValues<string>();
            });
        }

        [Fact]
        public void GetExportedValuesOfT2_StringAsTTypeArgumentAskingForContractWithOneObjectExport_ShouldThrowContractMismatch()
        {
            var container = ContainerFactory.Create(new MicroExport("Contract", typeof(string), new object()));

            ExceptionAssert.Throws<CompositionContractMismatchException>(() =>
            {
                container.GetExportedValues<string>("Contract");
            });
        }

        [Fact]
        public void GetExportOfT1_AskingForContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport(typeof(string), "Parent"));
            var child = ContainerFactory.Create(parent);

            var export = child.GetExport<string>();

            Assert.Equal("Parent", export.Value);
        }

        [Fact]
        public void GetExportOfT2_AskingForContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent);

            var export = child.GetExport<string>("Contract");

            Assert.Equal("Parent", export.Value);
        }

        [Fact]
        public void GetExportOfTTMetadataView1_AskingForContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport(typeof(string), "Parent"));
            var child = ContainerFactory.Create(parent);

            var export = child.GetExport<string, object>();

            Assert.Equal("Parent", export.Value);
        }

        [Fact]
        public void GetExportOfTTMetadataView2_AskingForContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent);

            var export = child.GetExport<string, object>("Contract");

            Assert.Equal("Parent", export.Value);
        }

        [Fact]
        public void GetExports1_AskingForExactlyOneContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent);

            var definition = ImportDefinitionFactory.Create("Contract", ImportCardinality.ExactlyOne);

            var exports = child.GetExports(definition);

            ExportsAssert.AreEqual(exports, "Parent");
        }

        [Fact]
        public void GetExports1_AskingForZeroOrOneContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent);

            var definition = ImportDefinitionFactory.Create("Contract", ImportCardinality.ZeroOrOne);

            var exports = child.GetExports(definition);

            ExportsAssert.AreEqual(exports, "Parent");
        }

        [Fact]
        public void GetExports1_AskingForZeroOrMoreContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent);

            var definition = ImportDefinitionFactory.Create("Contract", ImportCardinality.ZeroOrMore);

            var exports = child.GetExports(definition);

            ExportsAssert.AreEqual(exports, "Parent");
        }

        [Fact]
        public void GetExports2_AskingForContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent);

            var exports = child.GetExports(typeof(string), (Type)null, "Contract");

            ExportsAssert.AreEqual(exports, "Parent");
        }

        [Fact]
        public void GetExportsOfT1_AskingForContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport(typeof(string), "Parent"));
            var child = ContainerFactory.Create(parent);

            var exports = child.GetExports<string>();

            ExportsAssert.AreEqual(exports, "Parent");
        }

        [Fact]
        public void GetExportsOfT2_AskingForContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent);

            var exports = child.GetExports<string>("Contract");

            ExportsAssert.AreEqual(exports, "Parent");
        }

        [Fact]
        public void GetExportsOfTTMetadataView1_AskingForContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport(typeof(string), "Parent"));
            var child = ContainerFactory.Create(parent);

            var exports = child.GetExports<string, object>();

            ExportsAssert.AreEqual(exports, "Parent");
        }

        [Fact]
        public void GetExportsOfTTMetadataView2_AskingForContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent);

            var exports = child.GetExports<string, object>("Contract");

            ExportsAssert.AreEqual(exports, "Parent");
        }

        [Fact]
        public void GetExportedValueOfT1_AskingForContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport(typeof(string), "Parent"));
            var child = ContainerFactory.Create(parent);

            var exportedValue = child.GetExportedValue<string>();

            Assert.Equal("Parent", exportedValue);
        }

        [Fact]
        public void GetExportedValueOfT2_AskingForContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent);

            var exportedValue = child.GetExportedValue<string>("Contract");

            Assert.Equal("Parent", exportedValue);
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT1_AskingForContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport(typeof(string), "Parent"));
            var child = ContainerFactory.Create(parent);

            var exportedValue = child.GetExportedValueOrDefault<string>();

            Assert.Equal("Parent", exportedValue);
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT2_AskingForContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent);

            var exportedValue = child.GetExportedValueOrDefault<string>("Contract");

            Assert.Equal("Parent", exportedValue);
        }

        [Fact]
        public void GetExportedValuesOfT1_AskingForContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport(typeof(string), "Parent"));
            var child = ContainerFactory.Create(parent);

            var exportedValues = child.GetExportedValues<string>();

            EnumerableAssert.AreEqual(exportedValues, "Parent");
        }

        [Fact]
        public void GetExportedValuesOfT2_AskingForContractFromChildWithExportInParentContainer_ShouldReturnExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent);

            var exportedValues = child.GetExportedValues<string>("Contract");

            EnumerableAssert.AreEqual(exportedValues, "Parent");
        }

        [Fact]
        public void GetExportOfT1_AskingForContractWithExportInBothParentAndChildContainers_ShouldReturnChildExport()
        {
            var parent = ContainerFactory.Create(new MicroExport(typeof(string), "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport(typeof(string), "Child"));

            var export = child.GetExport<string>();

            Assert.Equal("Child", export.Value);
        }

        [Fact]
        public void GetExportOfT2_AskingForContractWithExportInBothParentAndChildContainers_ShouldReturnChildExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport("Contract", "Child"));

            var export = child.GetExport<string>("Contract");

            Assert.Equal("Child", export.Value);
        }

        [Fact]
        public void GetExportOfTTMetadataView1_AskingForContractWithExportInBothParentAndChildContainers_ShouldReturnChildExport()
        {
            var parent = ContainerFactory.Create(new MicroExport(typeof(string), "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport(typeof(string), "Child"));

            var export = child.GetExport<string, object>();

            Assert.Equal("Child", export.Value);
        }

        [Fact]
        public void GetExportOfTTMetadataView2_AskingForContractWithExportInBothParentAndChildContainers_ShouldReturnChildExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport("Contract", "Child"));

            var export = child.GetExport<string, object>("Contract");

            Assert.Equal("Child", export.Value);
        }

        [Fact]
        public void GetExports1_AskingForExactlyOneContractWithExportInBothParentAndChildContainers_ShouldReturnChildExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport("Contract", "Child"));

            var definition = ImportDefinitionFactory.Create("Contract", ImportCardinality.ExactlyOne);

            var exports = child.GetExports(definition);

            ExportsAssert.AreEqual(exports, "Child");
        }

        [Fact]
        public void GetExports1_AskingForZeroOrOneContractWithExportInBothParentAndChildContainers_ShouldReturnChildExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport("Contract", "Child"));

            var definition = ImportDefinitionFactory.Create("Contract", ImportCardinality.ZeroOrOne);

            var exports = child.GetExports(definition);

            ExportsAssert.AreEqual(exports, "Child");
        }

        [Fact]
        public void GetExports1_AskingForZeroOrMoreContractWithExportInBothParentAndChildContainers_ShouldReturnBothExports()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport("Contract", "Child"));

            var definition = ImportDefinitionFactory.Create("Contract", ImportCardinality.ZeroOrMore);

            var exports = child.GetExports(definition);

            ExportsAssert.AreEqual(exports, "Child", "Parent");
        }

        [Fact]
        public void GetExports2_AskingForContractWithExportInBothParentAndChildContainers_ShouldReturnBothExports()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport("Contract", "Child"));

            var exports = child.GetExports(typeof(string), (Type)null, "Contract");

            ExportsAssert.AreEqual(exports, "Child", "Parent");
        }

        [Fact]
        public void GetExportsOfT1_AskingForContractWithExportInBothParentAndChildContainers_ShouldReturnBothExports()
        {
            var parent = ContainerFactory.Create(new MicroExport(typeof(string), "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport(typeof(string), "Child"));

            var exports = child.GetExports<string>();

            ExportsAssert.AreEqual(exports, "Child", "Parent");
        }

        [Fact]
        public void GetExportsOfT2_AskingForContractWithExportInBothParentAndChildContainers_ShouldReturnBothExports()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport("Contract", "Child"));

            var exports = child.GetExports<string>("Contract");

            ExportsAssert.AreEqual(exports, "Child", "Parent");
        }

        [Fact]
        public void GetExportsOfTTMetadataView1_AskingForContractWithExportInBothParentAndChildContainers_ShouldReturnBothExports()
        {
            var parent = ContainerFactory.Create(new MicroExport(typeof(string), "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport(typeof(string), "Child"));

            var exports = child.GetExports<string, object>();

            ExportsAssert.AreEqual(exports, "Child", "Parent");
        }

        [Fact]
        public void GetExportsOfTTMetadataView2_AskingForContractWithExportInBothParentAndChildContainers_ShouldReturnBothExports()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport("Contract", "Child"));

            var exports = child.GetExports<string, object>("Contract");

            ExportsAssert.AreEqual(exports, "Child", "Parent");
        }

        [Fact]
        public void GetExportedValueOfT1_AskingForContractWithExportInBothParentAndChildContainers_ShouldReturnChildExport()
        {
            var parent = ContainerFactory.Create(new MicroExport(typeof(string), "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport(typeof(string), "Child"));

            var exportedValue = child.GetExportedValue<string>();

            Assert.Equal("Child", exportedValue);
        }

        [Fact]
        public void GetExportedValueOfT2_AskingForContractWithExportInBothParentAndChildContainers_ShouldReturnChildExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport("Contract", "Child"));

            var exportedValue = child.GetExportedValue<string>("Contract");

            Assert.Equal("Child", exportedValue);
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT1_AskingForContractWithExportInBothParentAndChildContainers_ShouldReturnChildExport()
        {
            var parent = ContainerFactory.Create(new MicroExport(typeof(string), "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport(typeof(string), "Child"));

            var exportedValue = child.GetExportedValueOrDefault<string>();

            Assert.Equal("Child", exportedValue);
        }

        [Fact]
        public void GetExportedValueOrDefaultOfT2_AskingForContractWithExportInBothParentAndChildContainers_ShouldReturnChildExport()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport("Contract", "Child"));

            var exportedValue = child.GetExportedValueOrDefault<string>("Contract");

            Assert.Equal("Child", exportedValue);
        }

        [Fact]
        public void GetExportedValuesOfT1_AskingForContractWithExportInBothParentAndChildContainers_ShouldReturnBothExports()
        {
            var parent = ContainerFactory.Create(new MicroExport(typeof(string), "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport(typeof(string), "Child"));

            var exportedValues = child.GetExportedValues<string>();

            EnumerableAssert.AreEqual(exportedValues, "Child", "Parent");
        }

        [Fact]
        public void GetExportedValuesOfT2_AskingForContractWithExportInBothParentAndChildContainers_ShouldReturnBothExports()
        {
            var parent = ContainerFactory.Create(new MicroExport("Contract", "Parent"));
            var child = ContainerFactory.Create(parent, new MicroExport("Contract", "Child"));

            var exportedValues = child.GetExportedValues<string>("Contract");

            EnumerableAssert.AreEqual(exportedValues, "Child", "Parent");
        }

        [Fact]
        public void GetExportOfTTMetadataView1_TypeAsMetadataViewTypeArgument_IsUsedAsMetadataConstraint()
        {
            var metadata = new Dictionary<string, object>();
            metadata.Add("Metadata1", "MetadataValue1");
            metadata.Add("Metadata2", "MetadataValue2");
            metadata.Add("Metadata3", "MetadataValue3");

            var container = ContainerFactory.Create(new MicroExport("Another", metadata, "Value1"),
                                                    new MicroExport(typeof(string), metadata, "Value1"),
                                                    new MicroExport(typeof(string), "Value2"));

            var export = container.GetExport<string, IMetadataView>();
            var metadataExport = (Lazy<string, IMetadataView>)export;

            Assert.Equal("Value1", metadataExport.Value);
            Assert.Equal("MetadataValue1", metadataExport.Metadata.Metadata1);
            Assert.Equal("MetadataValue2", metadataExport.Metadata.Metadata2);
            Assert.Equal("MetadataValue3", metadataExport.Metadata.Metadata3);
        }

        [Fact]
        public void GetExportOfTTMetadataView2_TypeAsMetadataViewTypeArgument_IsUsedAsMetadataConstraint()
        {
            var metadata = new Dictionary<string, object>();
            metadata.Add("Metadata1", "MetadataValue1");
            metadata.Add("Metadata2", "MetadataValue2");
            metadata.Add("Metadata3", "MetadataValue3");

            var container = ContainerFactory.Create(new MicroExport("Another", metadata, "Value1"),
                                                                     new MicroExport("Contract", metadata, "Value1"),
                                                                     new MicroExport("Contract", "Value2"));

            var export = container.GetExport<string, IMetadataView>("Contract");
            var metadataExport = (Lazy<string, IMetadataView>)export;

            Assert.Equal("Value1", metadataExport.Value);
            Assert.Equal("MetadataValue1", metadataExport.Metadata.Metadata1);
            Assert.Equal("MetadataValue2", metadataExport.Metadata.Metadata2);
            Assert.Equal("MetadataValue3", metadataExport.Metadata.Metadata3);
        }

        [Fact]
        public void GetExports1_TypeAsMetadataViewTypeArgument_IsUsedAsMetadataConstraint()
        {
            var metadata = new Dictionary<string, object>();
            metadata.Add("Metadata1", "MetadataValue1");
            metadata.Add("Metadata2", "MetadataValue2");
            metadata.Add("Metadata3", "MetadataValue3");

            var container = ContainerFactory.Create(new MicroExport("Another", metadata, "Value1"),
                                                                     new MicroExport("Contract", metadata, "Value1"),
                                                                     new MicroExport("Contract", "Value2"));

            var definition = ImportDefinitionFactory.Create(
                "Contract",
                new Dictionary<string, Type> { { "Metadata1", typeof(object) }, { "Metadata2", typeof(object) }, { "Metadata3", typeof(object) } }
                );

            var exports = container.GetExports(definition);

            Assert.Equal(1, exports.Count());

            var export = exports.First();

            Assert.Equal("Value1", export.Value);
            EnumerableAssert.AreEqual(metadata, export.Metadata);
        }

        [Fact]
        public void GetExports2_TypeAsMetadataViewTypeArgument_IsUsedAsMetadataConstraint()
        {
            var metadata = new Dictionary<string, object>();
            metadata.Add("Metadata1", "MetadataValue1");
            metadata.Add("Metadata2", "MetadataValue2");
            metadata.Add("Metadata3", "MetadataValue3");

            var container = ContainerFactory.Create(new MicroExport("Another", metadata, "Value1"),
                                                                     new MicroExport("Contract", metadata, "Value1"),
                                                                     new MicroExport("Contract", "Value2"));

            var exports = container.GetExports(typeof(string), typeof(IMetadataView), "Contract");

            Assert.Equal(1, exports.Count());

            var export = exports.First();
            IMetadataView exportMetadata = export.Metadata as IMetadataView;

            Assert.Equal("Value1", export.Value);
            Assert.NotNull(exportMetadata);

            Assert.Equal("MetadataValue1", exportMetadata.Metadata1);
            Assert.Equal("MetadataValue2", exportMetadata.Metadata2);
            Assert.Equal("MetadataValue3", exportMetadata.Metadata3);
        }

        [Fact]
        public void GetExportsOfTTMetadataView1_TypeAsMetadataViewTypeArgument_IsUsedAsMetadataConstraint()
        {
            var metadata = new Dictionary<string, object>();
            metadata.Add("Metadata1", "MetadataValue1");
            metadata.Add("Metadata2", "MetadataValue2");
            metadata.Add("Metadata3", "MetadataValue3");

            var container = ContainerFactory.Create(new MicroExport("Another", metadata, "Value1"),
                                                                     new MicroExport(typeof(string), metadata, "Value1"),
                                                                     new MicroExport(typeof(string), "Value2"));

            var exports = container.GetExports<string, IMetadataView>();

            Assert.Equal(1, exports.Count());

            var export = (Lazy<string, IMetadataView>)exports.First();

            Assert.Equal("Value1", export.Value);
            Assert.Equal("MetadataValue1", export.Metadata.Metadata1);
            Assert.Equal("MetadataValue2", export.Metadata.Metadata2);
            Assert.Equal("MetadataValue3", export.Metadata.Metadata3);
        }

        [Fact]
        public void GetExportsOfTTMetadataView2_TypeAsMetadataViewTypeArgument_IsUsedAsMetadataConstraint()
        {
            var metadata = new Dictionary<string, object>();
            metadata.Add("Metadata1", "MetadataValue1");
            metadata.Add("Metadata2", "MetadataValue2");
            metadata.Add("Metadata3", "MetadataValue3");

            var container = ContainerFactory.Create(new MicroExport("Another", metadata, "Value1"),
                                                                     new MicroExport("Contract", metadata, "Value1"),
                                                                     new MicroExport("Contract", "Value2"));

            var exports = container.GetExports<string, IMetadataView>("Contract");

            Assert.Equal(1, exports.Count());

            var export = (Lazy<string, IMetadataView>)exports.First();

            Assert.Equal("Value1", export.Value);
            Assert.Equal("MetadataValue1", export.Metadata.Metadata1);
            Assert.Equal("MetadataValue2", export.Metadata.Metadata2);
            Assert.Equal("MetadataValue3", export.Metadata.Metadata3);
        }

        [Fact]
        public void GetExports1_AskingForExactlyOneAndAllWhenContainerEmpty_ShouldThrowCardinalityMismatch()
        {
            var container = CreateCompositionContainer();

            var definition = ImportDefinitionFactory.Create(export => true, ImportCardinality.ExactlyOne);

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExports(definition);
            });
        }

        [Fact]
        public void GetExports1_AskingForZeroOrOneAndAllWhenContainerEmpty_ShouldReturnEmpty()
        {
            var container = CreateCompositionContainer();

            var definition = ImportDefinitionFactory.Create(export => true, ImportCardinality.ZeroOrOne);

            var exports = container.GetExports(definition);

            Assert.Empty(exports);
        }

        [Fact]
        public void GetExports1_AskingForExactlyOneAndAllWhenContainerEmpty_ShouldReturnEmpty()
        {
            var container = CreateCompositionContainer();

            var definition = ImportDefinitionFactory.Create(export => true, ImportCardinality.ZeroOrMore);

            var exports = container.GetExports(definition);

            Assert.Empty(exports);
        }

        [Fact]
        public void RemovePart_PartNotInContainerAsPartArgument_ShouldNotCauseImportsToBeRebound()
        {
            const string contractName = "Contract";

            var exporter = PartFactory.CreateExporter(new MicroExport(contractName, 1));
            var importer = PartFactory.CreateImporter(contractName);
            var container = ContainerFactory.Create(exporter, importer);

            Assert.Equal(1, importer.Value);
            Assert.Equal(1, importer.ImportSatisfiedCount);

            var doesNotExistInContainer = PartFactory.CreateExporter(new MicroExport(contractName, 2));

            CompositionBatch batch = new CompositionBatch();
            batch.RemovePart(doesNotExistInContainer);
            container.Compose(batch);

            Assert.Equal(1, importer.ImportSatisfiedCount);
        }

        [Fact]
        public void RemovePart_PartInContainerQueueAsPartArgument_ShouldNotLeavePartInContainer()
        {
            const string contractName = "Contract";

            var exporter = PartFactory.CreateExporter(new MicroExport(contractName, 1));
            var importer = PartFactory.CreateImporter(true, contractName);
            var container = ContainerFactory.Create(exporter, importer);

            CompositionBatch batch = new CompositionBatch();
            batch.RemovePart(exporter);
            container.Compose(batch);

            Assert.Null(importer.Value);
            Assert.Equal(2, importer.ImportSatisfiedCount);
        }

        [Fact]
        public void RemovePart_PartAlreadyRemovedAsPartArgument_ShouldNotThrow()
        {
            var exporter = PartFactory.CreateExporter(new MicroExport("Contract", 1));
            var container = ContainerFactory.Create(exporter);

            Assert.Equal(1, container.GetExportedValue<int>("Contract"));

            CompositionBatch batch = new CompositionBatch();
            batch.RemovePart(exporter);
            container.Compose(batch);

            Assert.False(container.IsPresent("Contract"));

            batch = new CompositionBatch();
            batch.RemovePart(exporter);
            container.Compose(batch);

            Assert.False(container.IsPresent("Contract"));
        }

        [Fact]
        public void TryComposeSimple()
        {
            var container = CreateCompositionContainer();
            Int32Importer importer = new Int32Importer();

            CompositionBatch batch = new CompositionBatch();
            batch.AddParts(importer, new Int32Exporter(42));
            container.Compose(batch);

            Assert.Equal(42, importer.Value);
        }

        [Fact]
        public void TryComposeSimpleFail()
        {
            var container = CreateCompositionContainer();
            Int32Importer importer = new Int32Importer();

            CompositionBatch batch = new CompositionBatch();
            batch.AddParts(importer);

            CompositionAssert.ThrowsChangeRejectedError(ErrorId.ImportEngine_PartCannotSetImport, ErrorId.ImportEngine_ImportCardinalityMismatch, RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });

            Assert.Equal(0, importer.Value);
        }

        [Fact]
        public void ComposeDisposableChildContainer()
        {
            var outerContainer = CreateCompositionContainer();
            Int32Importer outerImporter = new Int32Importer();

            CompositionBatch outerBatch = new CompositionBatch();
            var key = outerBatch.AddExportedValue("Value", 42);
            outerBatch.AddPart(outerImporter);
            outerContainer.Compose(outerBatch);
            Assert.Equal(42, outerImporter.Value);

            Int32Importer innerImporter = new Int32Importer();
            var innerContainer = new CompositionContainer(outerContainer);
            CompositionBatch innerBatch = new CompositionBatch();
            innerBatch.AddPart(innerImporter);

            innerContainer.Compose(innerBatch);
            Assert.Equal(42, innerImporter.Value);
            Assert.Equal(42, outerImporter.Value);

            outerBatch = new CompositionBatch();
            outerBatch.RemovePart(key);
            key = outerBatch.AddExportedValue("Value", -5);
            outerContainer.Compose(outerBatch);
            Assert.Equal(-5, innerImporter.Value);
            Assert.Equal(-5, outerImporter.Value);

            innerContainer.Dispose();
            outerBatch = new CompositionBatch();
            outerBatch.RemovePart(key);
            key = outerBatch.AddExportedValue("Value", 500);
            outerContainer.Compose(outerBatch);
            Assert.Equal(500, outerImporter.Value);
            Assert.Equal(-5, innerImporter.Value);
        }

        [Fact]
        public void RemoveValueTest()
        {
            var container = CreateCompositionContainer();
            CompositionBatch batch = new CompositionBatch();

            var key = batch.AddExportedValue("foo", "hello");
            container.Compose(batch);
            var result = container.GetExportedValue<string>("foo");
            Assert.Equal("hello", result);

            batch = new CompositionBatch();
            batch.RemovePart(key);
            container.Compose(batch);

            Assert.False(container.IsPresent("foo"));

            batch = new CompositionBatch();
            batch.RemovePart(key);        // Remove should be idempotent
            container.Compose(batch);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void OptionalImportsOfValueTypeBoundToDefaultValueShouldNotAffectAvailableValues()
        {
            var container = CreateCompositionContainer();
            var importer = new OptionalImporter();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            container.Compose(batch);

            Assert.Equal(0, importer.ValueType);

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExportedValue<int>("ValueType");
            });
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void OptionalImportsOfNullableValueTypeBoundToDefaultValueShouldNotAffectAvailableValues()
        {
            var container = CreateCompositionContainer();
            var importer = new OptionalImporter();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            container.Compose(batch);

            Assert.Null(importer.NullableValueType);

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExportedValue<int>("NullableValueType");
            });
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void OptionalImportsOfReferenceTypeBoundToDefaultValueShouldNotAffectAvailableValues()
        {
            var container = CreateCompositionContainer();
            var importer = new OptionalImporter();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            container.Compose(batch);

            Assert.Null(importer.ReferenceType);

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExportedValue<int>("ReferenceType");
            });
        }

        [Fact]
        public void ExportsChanged_ExportNothing_ShouldNotFireExportsChanged()
        {
            var container = CreateCompositionContainer();

            container.ExportsChanged += (sender, args) =>
            {
                throw new NotImplementedException();
            };

            CompositionBatch batch = new CompositionBatch();
            container.Compose(batch);
        }

        [Fact]
        public void ExportsChanged_ExportAdded_ShouldFireExportsChanged()
        {
            var container = CreateCompositionContainer();
            IEnumerable<string> changedNames = null;

            container.ExportsChanged += (sender, args) =>
            {
                Assert.Same(container, sender);
                Assert.Null(changedNames);
                Assert.NotNull(args.AddedExports);
                Assert.NotNull(args.RemovedExports);
                Assert.NotNull(args.ChangedContractNames);
                changedNames = args.ChangedContractNames;
            };

            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue("MyExport", new object());
            container.Compose(batch);

            EnumerableAssert.AreEqual(changedNames, "MyExport");
        }

        [Fact]
        public void ExportsChanged_ExportRemoved_ShouldFireExportsChanged()
        {
            var container = CreateCompositionContainer();
            IEnumerable<string> changedNames = null;

            CompositionBatch batch = new CompositionBatch();
            var part = batch.AddExportedValue("MyExport", new object());
            container.Compose(batch);

            container.ExportsChanged += (sender, args) =>
            {
                Assert.Same(container, sender);
                Assert.Null(changedNames);
                Assert.NotNull(args.AddedExports);
                Assert.NotNull(args.RemovedExports);
                Assert.NotNull(args.ChangedContractNames);
                changedNames = args.ChangedContractNames;
            };

            batch = new CompositionBatch();
            batch.RemovePart(part);
            container.Compose(batch);

            EnumerableAssert.AreEqual(changedNames, "MyExport");
        }

        [Fact]
        public void ExportsChanged_ExportAddAnother_ShouldFireExportsChanged()
        {
            var container = CreateCompositionContainer();
            IEnumerable<string> changedNames = null;

            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue("MyExport", new object());
            container.Compose(batch);

            container.ExportsChanged += (sender, args) =>
            {
                Assert.Same(container, sender);
                Assert.Null(changedNames);
                Assert.NotNull(args.AddedExports);
                Assert.NotNull(args.RemovedExports);
                Assert.NotNull(args.ChangedContractNames);
                changedNames = args.ChangedContractNames;
            };

            batch = new CompositionBatch();
            // Adding another should cause an update.
            batch.AddExportedValue("MyExport", new object());
            container.Compose(batch);

            EnumerableAssert.AreEqual(changedNames, "MyExport");
        }

        [Fact]
        public void ExportsChanged_AddExportOnParent_ShouldFireExportsChangedOnBoth()
        {
            var parent = CreateCompositionContainer();
            var child = new CompositionContainer(parent);

            IEnumerable<string> parentNames = null;
            parent.ExportsChanged += (sender, args) =>
            {
                Assert.Same(parent, sender);
                parentNames = args.ChangedContractNames;
            };

            IEnumerable<string> childNames = null;
            child.ExportsChanged += (sender, args) =>
            {
                Assert.Same(child, sender);
                childNames = args.ChangedContractNames;
            };

            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue("MyExport", new object());
            parent.Compose(batch);

            EnumerableAssert.AreEqual(parentNames, "MyExport");
            EnumerableAssert.AreEqual(childNames, "MyExport");
        }

        [Fact]
        public void ExportsChanged_AddExportOnChild_ShouldFireExportsChangedOnChildOnly()
        {
            var parent = CreateCompositionContainer();
            var child = new CompositionContainer(parent);

            parent.ExportsChanged += (sender, args) =>
            {
                throw new NotImplementedException();
            };

            IEnumerable<string> childNames = null;
            child.ExportsChanged += (sender, args) =>
            {
                Assert.Same(child, sender);
                childNames = args.ChangedContractNames;
            };

            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue("MyExport2", new object());
            child.Compose(batch);

            EnumerableAssert.AreEqual(childNames, "MyExport2");
        }

        [Fact]
        public void ExportsChanged_FromAggregateCatalog_ShouldFireExportsChangedOnce()
        {

            var cat = new AggregateCatalog();
            var container = new CompositionContainer(cat);
            IEnumerable<string> changedNames = null;

            container.ExportsChanged += (sender, args) =>
            {
                Assert.Same(container, sender);
                Assert.Null(changedNames);
                Assert.NotNull(args.AddedExports);
                Assert.NotNull(args.RemovedExports);
                Assert.NotNull(args.ChangedContractNames);
                changedNames = args.ChangedContractNames;
            };

            var typeCatalog = new TypeCatalog(typeof(SimpleExporter));
            cat.Catalogs.Add(typeCatalog);

            Assert.NotNull(changedNames);
        }

        [Fact]
        public void Dispose_BeforeCompose_CanBeCallMultipleTimes()
        {
            var container = ContainerFactory.Create(PartFactory.Create(), PartFactory.Create());
            container.Dispose();
            container.Dispose();
            container.Dispose();
        }

        [Fact]
        public void Dispose_AfterCompose_CanBeCallMultipleTimes()
        {
            var container = ContainerFactory.Create(PartFactory.Create(), PartFactory.Create());
            container.Dispose();
            container.Dispose();
            container.Dispose();
        }

        [Fact]
        public void Dispose_CallsGCSuppressFinalize()
        {
            bool finalizerCalled = false;

            var container = ContainerFactory.CreateDisposable(disposing =>
            {
                if (!disposing)
                {
                    finalizerCalled = true;
                }

            });

            container.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(finalizerCalled);
        }

        [Fact]
        public void Dispose_CallsDisposeBoolWithTrue()
        {
            var container = ContainerFactory.CreateDisposable(disposing =>
            {
                Assert.True(disposing);
            });

            container.Dispose();
        }

        [Fact]
        public void Dispose_CallsDisposeBoolOnce()
        {
            int disposeCount = 0;

            var container = ContainerFactory.CreateDisposable(disposing =>
            {
                disposeCount++;
            });

            container.Dispose();

            Assert.Equal(1, disposeCount);
        }

        [Fact]
        public void Dispose_ContainerAsExportedValue_CanBeDisposed()
        {
            using (var container = CreateCompositionContainer())
            {
                CompositionBatch batch = new CompositionBatch();
                batch.AddExportedValue<ICompositionService>(container);
                container.Compose(batch);
            }
        }

        [Fact]
        public void Dispose_ContainerAsPart_CanBeDisposed()
        {   // Tests that when we re-enter CompositionContainer.Dispose, that we don't
            // stack overflow.

            using (var container = CreateCompositionContainer())
            {
                var part = PartFactory.CreateExporter(new MicroExport(typeof(ICompositionService), container));
                CompositionBatch batch = new CompositionBatch();
                batch.AddPart(part);
                container.Compose(batch);

                Assert.Same(container, container.GetExportedValue<ICompositionService>());
            }
        }

        [Fact]
        public void ICompositionService_ShouldNotBeImplicitlyExported()
        {
            var container = CreateCompositionContainer();

            Assert.False(container.IsPresent<ICompositionService>());
        }

        [Fact]
        public void CompositionContainer_ShouldNotBeImplicitlyExported()
        {
            var container = CreateCompositionContainer();

            Assert.False(container.IsPresent<CompositionContainer>());
        }

        [Fact]
        public void ICompositionService_ShouldNotBeImplicitlyImported()
        {
            var importer = PartFactory.CreateImporter<ICompositionService>();
            var container = ContainerFactory.Create(importer);

            Assert.Null(importer.Value);
        }

        [Fact]
        public void CompositionContainer_ShouldNotBeImplicitlyImported()
        {
            var importer = PartFactory.CreateImporter<CompositionContainer>();
            var container = ContainerFactory.Create(importer);

            Assert.Null(importer.Value);
        }

        [Fact]
        public void ICompositionService_CanBeExported()
        {
            var container = CreateCompositionContainer();
            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue<ICompositionService>(container);
            container.Compose(batch);

            Assert.Same(container, container.GetExportedValue<ICompositionService>());
        }

        [Fact]
        public void CompositionContainer_CanBeExported()
        {
            var container = CreateCompositionContainer();
            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue<CompositionContainer>(container);
            container.Compose(batch);

            Assert.Same(container, container.GetExportedValue<CompositionContainer>());
        }

        [Fact]
        public void ReleaseExport_Null_ShouldThrowArugmentNull()
        {
            var container = CreateCompositionContainer();

            Assert.Throws<ArgumentNullException>("export",
                () => container.ReleaseExport(null));
        }

        [Fact]
        public void ReleaseExports_Null_ShouldThrowArgumentNull()
        {
            var container = CreateCompositionContainer();

            Assert.Throws<ArgumentNullException>("exports",
                () => container.ReleaseExports(null));
        }

        [Fact]
        public void ReleaseExports_ElementNull_ShouldThrowArgument()
        {
            var container = CreateCompositionContainer();

            Assert.Throws<ArgumentException>("exports",
                () => container.ReleaseExports(new Export[] { null }));
        }

        public class OptionalImporter
        {
            [Import("ValueType", AllowDefault = true)]
            public int ValueType
            {
                get;
                set;
            }

            [Import("NullableValueType", AllowDefault = true)]
            public int? NullableValueType
            {
                get;
                set;
            }

            [Import("ReferenceType", AllowDefault = true)]
            public string ReferenceType
            {
                get;
                set;
            }
        }

        public class ExportSimpleIntWithException
        {
            [Export("SimpleInt")]
            public int SimpleInt { get { throw new NotImplementedException(); } }
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // Actual:   typeof(System.Reflection.ReflectionTypeLoadException)
        public void TryGetValueWithCatalogVerifyExecptionDuringGet()
        {
            var cat = CatalogFactory.CreateDefaultAttributed();
            var container = new CompositionContainer(cat);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<int>("SimpleInt");
            });
        }

        [Fact]
        public void TryGetExportedValueWhileLockedForNotify()
        {
            var container = CreateCompositionContainer();
            CompositionBatch batch = new CompositionBatch();
            batch.AddParts(new CallbackImportNotify(delegate
            {
                container.GetExportedValueOrDefault<int>();
            }));

            container.Compose(batch);
        }

        [Fact]
        public void RawExportTests()
        {
            var container = CreateCompositionContainer();

            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue("foo", 1);
            container.Compose(batch);

            Lazy<int> export = container.GetExport<int>("foo");

            Assert.Equal(1, export.Value);
        }

        [Fact]
        [ActiveIssue(468388)]
        public void ContainerXGetXTest()
        {
            CompositionContainer container = CreateCompositionContainer();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(new MyExporterWithNoFoo());
            container.Compose(batch);
            ContainerXGetExportBoundValue(container);
        }

        [Fact]
        [ActiveIssue(468388)]
        public void ContainerXGetXByComponentCatalogTest()
        {
            CompositionContainer container = ContainerFactory.CreateWithDefaultAttributedCatalog();
            ContainerXGetExportBoundValue(container);
        }

        private void ContainerXGetExportBoundValue(CompositionContainer container)
        {
            throw new NotImplementedException();
        }

        [Export("MyExporterWithNoFoo")]
        public class MyExporterWithNoFoo
        {
        }

        [Export("MyExporterWithFoo")]
        [ExportMetadata("Foo", "Foo value")]
        public class MyExporterWithFoo
        {
        }

        [Export("MyExporterWithFooBar")]
        [ExportMetadata("Foo", "Foo value")]
        [ExportMetadata("Bar", "Bar value")]
        public class MyExporterWithFooBar
        {
        }

        // Silverlight doesn't support strongly typed metadata
        [Fact]
        public void ConverterExportTests()
        {
            var container = CreateCompositionContainer();
            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue("foo", 1);
            container.Compose(batch);

            var export = container.GetExport<int, IDictionary<string, object>>("foo");
            Assert.Equal(1, export.Value);
            Assert.NotNull(export.Metadata);
        }

        [Fact]
        public void RemoveFromWrongContainerTest()
        {
            CompositionContainer d1 = CreateCompositionContainer();
            CompositionContainer d2 = CreateCompositionContainer();

            CompositionBatch batch1 = new CompositionBatch();
            var valueKey = batch1.AddExportedValue("a", 1);
            d1.Compose(batch1);

            CompositionBatch batch2 = new CompositionBatch();
            batch2.RemovePart(valueKey);
            // removing entry from wrong container, shoudl be a no-op
            d2.Compose(batch2);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void AddPartSimple()
        {
            var container = CreateCompositionContainer();
            var importer = new Int32Importer();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            batch.AddPart(new Int32Exporter(42));
            container.Compose(batch);

            Assert.Equal(42, importer.Value);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void AddPart()
        {
            var container = CreateCompositionContainer();
            Int32Importer importer = new Int32Importer();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(AttributedModelServices.CreatePart(importer));
            batch.AddPart(new Int32Exporter(42));
            container.Compose(batch);

            Assert.Equal(42, importer.Value);
        }

        [Fact]
        public void ComposeReentrantChildContainerDisposed()
        {
            var container = CreateCompositionContainer();
            Int32Importer outerImporter = new Int32Importer();
            Int32Importer innerImporter = new Int32Importer();
            Int32Exporter exporter = new Int32Exporter(42);
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(exporter);
            container.Compose(batch);
            CallbackExecuteCodeDuringCompose callback = new CallbackExecuteCodeDuringCompose(() =>
            {
                using (CompositionContainer innerContainer = new CompositionContainer(container))
                {
                    CompositionBatch nestedBatch = new CompositionBatch();
                    nestedBatch.AddPart(innerImporter);
                    innerContainer.Compose(nestedBatch);
                }
                Assert.Equal(42, innerImporter.Value);
            });

            batch = new CompositionBatch();
            batch.AddParts(outerImporter, callback);
            container.Compose(batch);

            Assert.Equal(42, outerImporter.Value);
            Assert.Equal(42, innerImporter.Value);
        }

        [Fact]
        public void ComposeSimple()
        {
            var container = CreateCompositionContainer();
            Int32Importer importer = new Int32Importer();

            CompositionBatch batch = new CompositionBatch();
            batch.AddParts(importer, new Int32Exporter(42));
            container.Compose(batch);

            Assert.Equal(42, importer.Value);
        }

        [Fact]
        public void ComposeSimpleFail()
        {
            var container = CreateCompositionContainer();
            Int32Importer importer = new Int32Importer();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);

            CompositionAssert.ThrowsChangeRejectedError(ErrorId.ImportEngine_PartCannotSetImport,          // Cannot set Int32Importer.Value because
                                          ErrorId.ImportEngine_ImportCardinalityMismatch,    // No exports are present that match contract
                                          RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void ExceptionDuringNotify()
        {
            var container = CreateCompositionContainer();
            CompositionBatch batch = new CompositionBatch();
            batch.AddParts(new CallbackImportNotify(delegate
            {
                throw new InvalidOperationException();
            }));

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotActivate,              // Cannot activate CallbackImportNotify because
                                          RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void NeutralComposeWhileNotified()
        {
            var container = CreateCompositionContainer();
            CompositionBatch batch = new CompositionBatch();
            batch.AddParts(new CallbackImportNotify(delegate
            {
                // Is this really a supported scenario?
                container.Compose(batch);
            }));

            container.Compose(batch);
        }

        public class PartWithReentrantCompose : ComposablePart
        {
            private CompositionContainer _container;

            public PartWithReentrantCompose(CompositionContainer container)
            {
                this._container = container;
            }

            public override IEnumerable<ExportDefinition> ExportDefinitions
            {
                get
                {
                    this._container.ComposeExportedValue<string>("ExportedString");
                    return Enumerable.Empty<ExportDefinition>();
                }
            }

            public override IEnumerable<ImportDefinition> ImportDefinitions
            {
                get
                {
                    return Enumerable.Empty<ImportDefinition>();
                }
            }

            public override object GetExportedValue(ExportDefinition definition)
            {
                throw new NotImplementedException();
            }

            public override void SetImport(ImportDefinition definition, IEnumerable<Export> exports)
            {
                throw new NotImplementedException();
            }
        }

        [Export]
        public class SimpleExporter
        {

        }

        [Fact]
        public void ThreadSafeCompositionContainer()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SimpleExporter));

            CompositionContainer container = new CompositionContainer(catalog, true);
            Int32Importer importer = new Int32Importer();

            CompositionBatch batch = new CompositionBatch();
            batch.AddParts(importer, new Int32Exporter(42));
            container.Compose(batch);

            Assert.NotNull(container.GetExportedValue<SimpleExporter>());
            Assert.Equal(42, importer.Value);

            container.Dispose();

        }

        [Fact]
        public void ThreadSafeCompositionOptionsCompositionContainer()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SimpleExporter));

            CompositionContainer container = new CompositionContainer(catalog, CompositionOptions.IsThreadSafe);
            Int32Importer importer = new Int32Importer();

            CompositionBatch batch = new CompositionBatch();
            batch.AddParts(importer, new Int32Exporter(42));
            container.Compose(batch);

            Assert.NotNull(container.GetExportedValue<SimpleExporter>());
            Assert.Equal(42, importer.Value);

            container.Dispose();
        }

        [Fact]
        public void DisableSilentRejectionCompositionOptionsCompositionContainer()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SimpleExporter));

            CompositionContainer container = new CompositionContainer(catalog, CompositionOptions.DisableSilentRejection);
            Int32Importer importer = new Int32Importer();

            CompositionBatch batch = new CompositionBatch();
            batch.AddParts(importer, new Int32Exporter(42));
            container.Compose(batch);

            Assert.NotNull(container.GetExportedValue<SimpleExporter>());
            Assert.Equal(42, importer.Value);

            container.Dispose();
        }

        [Fact]
        public void DisableSilentRejectionThreadSafeCompositionOptionsCompositionContainer()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SimpleExporter));

            CompositionContainer container = new CompositionContainer(catalog, CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe);
            Int32Importer importer = new Int32Importer();

            CompositionBatch batch = new CompositionBatch();
            batch.AddParts(importer, new Int32Exporter(42));
            container.Compose(batch);

            Assert.NotNull(container.GetExportedValue<SimpleExporter>());
            Assert.Equal(42, importer.Value);

            container.Dispose();
        }

        [Fact]
        public void CompositionOptionsInvalidValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>("compositionOptions",
                () => new CompositionContainer((CompositionOptions)0x0400));
        }

        [Fact]
        public void ReentrantencyDisabledWhileComposing()
        {
            var container = CreateCompositionContainer();
            CompositionBatch batch = new CompositionBatch();

            batch.AddPart(new PartWithReentrantCompose(container));

            ExceptionAssert.Throws<InvalidOperationException>(() =>
                container.Compose(batch));
        }
        private static Expression<Func<ExportDefinition, bool>> ConstraintFromContract(string contractName)
        {
            return ConstraintFactory.Create(contractName);
        }

        private static string ContractFromType(Type type)
        {
            return AttributedModelServices.GetContractName(type);
        }

        private static CompositionContainer CreateCompositionContainer()
        {
            return new CompositionContainer();
        }

        public interface IMetadataView
        {
            string Metadata1
            {
                get;
            }

            string Metadata2
            {
                get;
            }

            string Metadata3
            {
                get;
            }
        }

        [Fact]
        public void ComposeExportedValueOfT_NullStringAsExportedValueArgument_VerifyCanPullOnValue()
        {
            var container = CreateCompositionContainer();

            var expectation = (string)null;
            container.ComposeExportedValue<string>(expectation);
            var actualValue = container.GetExportedValue<string>();

            Assert.Equal(expectation, actualValue);
        }

        [Fact]
        public void ComposeExportedValueOfT_StringAsExportedValueArgument_VerifyCanPullOnValue()
        {
            var expectations = new List<string>();
            expectations.Add((string)null);
            expectations.Add(string.Empty);
            expectations.Add("Value");

            foreach (var expectation in expectations)
            {
                var container = CreateCompositionContainer();
                container.ComposeExportedValue<string>(expectation);
                var actualValue = container.GetExportedValue<string>();

                Assert.Equal(expectation, actualValue);
            }
        }

        [Fact]
        public void ComposeExportedValueOfT_StringAsIEnumerableOfCharAsExportedValueArgument_VerifyCanPullOnValue()
        {
            var expectations = new List<string>();
            expectations.Add((string)null);
            expectations.Add(string.Empty);
            expectations.Add("Value");

            foreach (var expectation in expectations)
            {
                var container = CreateCompositionContainer();
                container.ComposeExportedValue<IEnumerable<char>>(expectation);
                var actualValue = container.GetExportedValue<IEnumerable<char>>();

                Assert.Equal(expectation, actualValue);
            }
        }

        [Fact]
        public void ComposeExportedValueOfT_ObjectAsExportedValueArgument_VerifyCanPullOnValue()
        {
            var expectations = new List<object>();
            expectations.Add((string)null);
            expectations.Add(string.Empty);
            expectations.Add("Value");
            expectations.Add(42);
            expectations.Add(new object());

            foreach (var expectation in expectations)
            {
                var container = CreateCompositionContainer();
                container.ComposeExportedValue<object>(expectation);
                var actualValue = container.GetExportedValue<object>();

                Assert.Equal(expectation, actualValue);
            }
        }

        [Fact]
        public void ComposeExportedValueOfT_ExportedValue_ExportedUnderDefaultContractName()
        {
            string expectedContractName = AttributedModelServices.GetContractName(typeof(string));
            var container = CreateCompositionContainer();
            container.ComposeExportedValue<string>("Value");

            var importDefinition = new ImportDefinition(ed => true, null, ImportCardinality.ZeroOrMore, false, false);
            var exports = container.GetExports(importDefinition);
            Assert.Equal(1, exports.Count());
            Assert.Equal(expectedContractName, exports.Single().Definition.ContractName);
        }

        [Fact]
        public void ComposeExportedValueOfT_ExportedValue_ExportContainsEmptyMetadata()
        {
            var container = CreateCompositionContainer();
            container.ComposeExportedValue<string>("Value");

            var importDefinition = new ImportDefinition(ed => true, null, ImportCardinality.ZeroOrMore, false, false);
            var exports = container.GetExports(importDefinition);
            Assert.Equal(1, exports.Count());
            Assert.Equal(1, exports.Single().Metadata.Count);  // contains type identity
        }

        [Fact]
        public void ComposeExportedValueOfT_ExportedValue_LazyContainsEmptyMetadata()
        {
            var container = CreateCompositionContainer();
            container.ComposeExportedValue<string>("Value");

            var lazy = container.GetExport<string, IDictionary<string, object>>();
            Assert.Equal(1, lazy.Metadata.Count);  // contains type identity
        }

        [Fact]
        public void ComposeExportedValueOfT_ExportedValue_ImportsAreNotDiscovered()
        {
            var container = CreateCompositionContainer();
            var importer = new PartWithRequiredImport();

            container.ComposeExportedValue<object>(importer);

            var importDefinition = new ImportDefinition(ed => true, null, ImportCardinality.ZeroOrMore, false, false);
            var exports = container.GetExports(importDefinition);
            Assert.Equal(1, exports.Count());  // we only get one if the import was not discovered since the import is not satisfied
        }

        [Fact]
        public void ComposeExportedValueOfT_NullAsContractName_ThrowsArgumentNullException()
        {
            var container = CreateCompositionContainer();
            Assert.Throws<ArgumentNullException>("contractName", () =>
                container.ComposeExportedValue<string>((string)null, "Value"));
        }

        [Fact]
        public void ComposeExportedValueOfT_EmptyStringAsContractName_ThrowsArgumentException()
        {
            var container = CreateCompositionContainer();
            Assert.Throws<ArgumentException>("contractName", () =>
                container.ComposeExportedValue<string>(string.Empty, "Value"));
        }

        [Fact]
        public void ComposeExportedValueOfT_ValidContractName_ValidExportedValue_VerifyCanPullOnValue()
        {
            var expectations = new List<Tuple<string, string>>();
            expectations.Add(new Tuple<string, string>(" ", (string)null));
            expectations.Add(new Tuple<string, string>(" ", string.Empty));
            expectations.Add(new Tuple<string, string>(" ", "Value"));
            expectations.Add(new Tuple<string, string>("ContractName", (string)null));
            expectations.Add(new Tuple<string, string>("ContractName", string.Empty));
            expectations.Add(new Tuple<string, string>("ContractName", "Value"));

            foreach (var expectation in expectations)
            {
                var container = CreateCompositionContainer();
                container.ComposeExportedValue<string>(expectation.Item1, expectation.Item2);
                var actualValue = container.GetExportedValue<string>(expectation.Item1);

                Assert.Equal(expectation.Item2, actualValue);

                ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
                    container.GetExportedValue<string>());
            }
        }

        [Fact]
        public void ComposeExportedValueOfT_ValidContractName_ExportedValue_ExportedUnderSpecifiedContractName()
        {
            string expectedContractName = "ContractName";
            var container = CreateCompositionContainer();
            container.ComposeExportedValue<string>(expectedContractName, "Value");

            var importDefinition = new ImportDefinition(ed => true, null, ImportCardinality.ZeroOrMore, false, false);
            var exports = container.GetExports(importDefinition);
            Assert.Equal(1, exports.Count());
            Assert.Equal(expectedContractName, exports.Single().Definition.ContractName);
        }

        [Fact]
        [ActiveIssue(812029)]
        public void ComposeExportedValueOfT_ValidContractName_ExportedValue_ExportContainsEmptyMetadata()
        {
            string expectedContractName = "ContractName";
            var container = CreateCompositionContainer();
            container.ComposeExportedValue<string>(expectedContractName, "Value");

            var importDefinition = new ImportDefinition(ed => ed.ContractName == expectedContractName, null, ImportCardinality.ZeroOrMore, false, false);
            var exports = container.GetExports(importDefinition);
            Assert.Equal(1, exports.Count());
            Assert.Equal(1, exports.Single().Metadata.Count); // contains type identity
        }

        [Fact]
        public void ComposeExportedValueOfT_ValidContractName_ExportedValue_ImportsAreNotDiscovered()
        {
            var container = CreateCompositionContainer();
            var importer = new PartWithRequiredImport();

            container.ComposeExportedValue<object>("ContractName", importer);

            var importDefinition = new ImportDefinition(ed => true, null, ImportCardinality.ZeroOrMore, false, false);
            var exports = container.GetExports(importDefinition);
            Assert.Equal(1, exports.Count());  // we only get one if the import was not discovered since the import is not satisfied
        }

        [Fact]
        public void TestExportedValueCachesNullValue()
        {
            var container = ContainerFactory.Create();
            var exporter = new ExportsMutableProperty();
            exporter.Property = null;
            container.ComposeParts(exporter);
            Assert.Null(container.GetExportedValue<string>("Property"));
            exporter.Property = "Value1";
            // Exported value should have been cached and so it shouldn't change
            Assert.Null(container.GetExportedValue<string>("Property"));
        }

        [Fact]
        public void TestExportedValueUsingWhereClause_ExportSuccessful()
        {
            CompositionContainer container = new CompositionContainer(new TypeCatalog(typeof(MefCollection<,>)));
            IMefCollection<DerivedClass, BaseClass> actualValue = container.GetExportedValue<IMefCollection<DerivedClass, BaseClass>>("UsingWhereClause");
            Assert.NotNull(actualValue);
            Assert.IsType<MefCollection<DerivedClass, BaseClass>>(actualValue);
        }

        public interface IMefCollection { }
        public interface IMefCollection<TC, TP> : IList<TC>, IMefCollection where TC : TP { }
        public class BaseClass { }
        public class DerivedClass : BaseClass { }

        [Export("UsingWhereClause", typeof(IMefCollection<,>))]
        public class MefCollection<TC, TP> : ObservableCollection<TC>, IMefCollection<TC, TP> where TC : TP { }

        public class ExportsMutableProperty
        {
            [Export("Property")]
            public string Property { get; set; }
        }

        public class PartWithRequiredImport
        {
            [Import]
            public object Import { get; set; }
        }
    }
}
