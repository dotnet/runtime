// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ComposablePartCatalogExportProviderTests
    {
        [Fact]
        public void Constructor_NullAsCatalogArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("catalog", () =>
            {
                new CatalogExportProvider((ComposablePartCatalog)null);
            });
        }

        [Fact]
        public void CompositionOptionsInvalidValueCatalogExportProvider()
        {
            Assert.Throws<ArgumentOutOfRangeException>("compositionOptions",
                () => new CatalogExportProvider(new TypeCatalog(), (CompositionOptions)0x0400));
        }

        [Fact]
        public void CompositionOptionsInvalidValueComposablePartExportProvider()
        {
            Assert.Throws<ArgumentOutOfRangeException>("compositionOptions",
                () => new ComposablePartExportProvider((CompositionOptions)0x0400));
        }

        [Fact]
        public void Constructor_ValueAsCatalogArgument_ShouldSetCatalogPropertyToEmpty()
        {
            var expectations = Expectations.GetCatalogs();

            foreach (var e in expectations)
            {
                var provider = new CatalogExportProvider(e);

                Assert.Same(e, provider.Catalog);
            }
        }

        [Fact]
        public void Catalog_WhenDisposed_ShouldThrowObjectDisposed()
        {
            var provider = CreateCatalogExportProvider();
            provider.Dispose();

            ExceptionAssert.ThrowsDisposed(provider, () =>
            {
                var catalog = provider.Catalog;
            });
        }

        [Fact]
        public void SourceProvider_NullAsValueArgument_ShouldThrowArgumentNull()
        {
            var provider = CreateCatalogExportProvider();

            Assert.Throws<ArgumentNullException>("value", () =>
            {
                provider.SourceProvider = null;
            });
        }

        [Fact]
        public void GetExports_WhenRejectedDefinitionRequiredImportIsAdded_ShouldBeResurrected()
        {
            var part = PartFactory.CreateImporterExporter("Export", "Import");

            var provider = CreateCatalogExportProvider(part);
            var sourceProvider = ExportProviderFactory.CreateRecomposable();
            provider.SourceProvider = sourceProvider;

            var exports = provider.GetExports<object>("Export");

            Assert.Empty(exports);

            // Resurrect the definition
            sourceProvider.AddExport("Import", new object());

            exports = provider.GetExports<object>("Export");

            Assert.Equal(1, exports.Count());
        }

        [Fact]
        public void GetExports_WhenMultipleRejectedDefinitionsRequiredImportsAreAdded_ShouldBeResurrected()
        {
            var part1 = PartFactory.CreateImporterExporter("Export", "Import");
            var part2 = PartFactory.CreateImporterExporter("Export", "Import");

            var provider = CreateCatalogExportProvider(part1, part2);
            var sourceProvider = ExportProviderFactory.CreateRecomposable();
            provider.SourceProvider = sourceProvider;

            var exports = provider.GetExports<object>("Export");

            Assert.Empty(exports);

            // Resurrect both definitions
            sourceProvider.AddExport("Import", new object());

            exports = provider.GetExports<object>("Export");

            Assert.Equal(2, exports.Count());
        }

        [Fact]
        [ActiveIssue(743740)]
        public void GetExports_AfterResurrectedDefinitionHasBeenRemovedAndReaddedToCatalog_ShouldNotBeTreatedAsRejected()
        {
            var definition1 = PartDefinitionFactory.Create(PartFactory.CreateImporterExporter("Export", "Import"));
            var definition2 = PartDefinitionFactory.Create(PartFactory.CreateImporterExporter("Export", "Import"));
            var catalog = CatalogFactory.CreateMutable(definition1, definition2);

            var provider = CreateCatalogExportProvider(catalog);
            var sourceProvider = ExportProviderFactory.CreateRecomposable();
            provider.SourceProvider = sourceProvider;

            var exports = provider.GetExports<object>("Export");

            Assert.Empty(exports);

            // Resurrect both definitions
            sourceProvider.AddExport("Import", new object());

            exports = provider.GetExports<object>("Export");

            Assert.Equal(2, exports.Count());

            catalog.RemoveDefinition(definition1);

            exports = provider.GetExports<object>("Export");
            Assert.Equal(1, exports.Count());

            catalog.AddDefinition(definition1);

            exports = provider.GetExports<object>("Export");

            Assert.Equal(2, exports.Count());
        }

        [Fact]
        [Trait("Type", "Integration")]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void BasicTest()
        {
            var catalog = CatalogFactory.CreateDefaultAttributed();
            var catalogExportProvider = new CatalogExportProvider(catalog);
            catalogExportProvider.SourceProvider = catalogExportProvider;
            var testName = AttributedModelServices.GetContractName(typeof(CatalogComponentTest));
            var testNameNonComponent = AttributedModelServices.GetContractName(typeof(CatalogComponentTestNonComponentPart));
            var testInterfaceName = AttributedModelServices.GetContractName(typeof(ICatalogComponentTest));

            Assert.Equal(1, catalogExportProvider.GetExports(ImportFromContract(testName)).Count());
            Assert.Equal(0, catalogExportProvider.GetExports(ImportFromContract(testNameNonComponent)).Count());

            var exports = catalogExportProvider.GetExports(ImportFromContract(testInterfaceName));
            Assert.Equal(2, exports.Count());

            foreach (var i in exports)
                Assert.NotNull(i.Value);

        }

        [Fact]
        [Trait("Type", "Integration")]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void BasicTestWithRequiredMetadata_NoTypeConstraint()
        {
            var catalog = CatalogFactory.CreateDefaultAttributed();
            var catalogExportProvider = new CatalogExportProvider(catalog);
            catalogExportProvider.SourceProvider = catalogExportProvider;

            Assert.Equal(0, catalogExportProvider.GetExports(ImportFromContractAndMetadata("MyExporterWithNoFoo", new string[] { "Foo" }, new Type[] { typeof(object) })).Count());

            Assert.Equal(1, catalogExportProvider.GetExports(ImportFromContractAndMetadata("MyExporterWithFoo", new string[] { "Foo" }, new Type[] { typeof(object) })).Count());
            Assert.Equal(0, catalogExportProvider.GetExports(ImportFromContractAndMetadata("MyExporterWithFoo", new string[] { "Foo", "Bar" }, new Type[] { typeof(object), typeof(object) })).Count());

            Assert.Equal(0, catalogExportProvider.GetExports(ImportFromContractAndMetadata("MyExporterWithNoFoo", new string[] { "Foo" }, new Type[] { typeof(object) })).Count());
            Assert.Equal(1, catalogExportProvider.GetExports(ImportFromContractAndMetadata("MyExporterWithFoo", new string[] { "Foo" }, new Type[] { typeof(object) })).Count());
        }

        [Fact]
        [Trait("Type", "Integration")]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void BasicTestWithRequiredMetadata_TypeConstraint()
        {
            var catalog = CatalogFactory.CreateDefaultAttributed();
            var catalogExportProvider = new CatalogExportProvider(catalog);
            catalogExportProvider.SourceProvider = catalogExportProvider;

            Assert.Equal(0, catalogExportProvider.GetExports(ImportFromContractAndMetadata("MyExporterWithNoFoo", new string[] { "Foo" }, new Type[] { typeof(string) })).Count());

            Assert.Equal(1, catalogExportProvider.GetExports(ImportFromContractAndMetadata("MyExporterWithFoo", new string[] { "Foo" }, new Type[] { typeof(string) })).Count());
            Assert.Equal(0, catalogExportProvider.GetExports(ImportFromContractAndMetadata("MyExporterWithFoo", new string[] { "Foo", "Bar" }, new Type[] { typeof(string), typeof(string) })).Count());

            Assert.Equal(0, catalogExportProvider.GetExports(ImportFromContractAndMetadata("MyExporterWithNoFoo", new string[] { "Foo" }, new Type[] { typeof(string) })).Count());
            Assert.Equal(1, catalogExportProvider.GetExports(ImportFromContractAndMetadata("MyExporterWithFoo", new string[] { "Foo" }, new Type[] { typeof(string) })).Count());
        }

        [Fact]
        [Trait("Type", "Integration")]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void BasicTestWithRequiredMetadata_WrongTypeConstraint()
        {
            var catalog = CatalogFactory.CreateDefaultAttributed();
            var catalogExportProvider = new CatalogExportProvider(catalog);
            catalogExportProvider.SourceProvider = catalogExportProvider;

            Assert.Equal(0, catalogExportProvider.GetExports(ImportFromContractAndMetadata("MyExporterWithNoFoo", new string[] { "Foo" }, new Type[] { typeof(int) })).Count());

            Assert.Equal(0, catalogExportProvider.GetExports(ImportFromContractAndMetadata("MyExporterWithFoo", new string[] { "Foo" }, new Type[] { typeof(int) })).Count());
            Assert.Equal(0, catalogExportProvider.GetExports(ImportFromContractAndMetadata("MyExporterWithFoo", new string[] { "Foo", "Bar" }, new Type[] { typeof(int), typeof(int) })).Count());

            Assert.Equal(0, catalogExportProvider.GetExports(ImportFromContractAndMetadata("MyExporterWithNoFoo", new string[] { "Foo" }, new Type[] { typeof(int) })).Count());
            Assert.Equal(0, catalogExportProvider.GetExports(ImportFromContractAndMetadata("MyExporterWithFoo", new string[] { "Foo" }, new Type[] { typeof(int) })).Count());
        }

        [Fact]
        [Trait("Type", "Integration")]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ComponentCatalogResolverGetStaticExport()
        {
            var catalog = CatalogFactory.CreateDefaultAttributed();
            var catalogExportProvider = new CatalogExportProvider(catalog);
            catalogExportProvider.SourceProvider = catalogExportProvider;

            var exports = catalogExportProvider.GetExports(ImportFromContract("StaticString"));
            Assert.Equal(1, exports.Count());
            Assert.Equal("StaticString", exports.First().Value);
        }

        [Fact]
        [Trait("Type", "Integration")]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ComponentCatalogResolverComponentCatalogExportReference()
        {
            var catalog = CatalogFactory.CreateDefaultAttributed();
            var catalogExportProvider = new CatalogExportProvider(catalog);
            catalogExportProvider.SourceProvider = catalogExportProvider;

            var exports = catalogExportProvider.GetExports(ImportFromContract(AttributedModelServices.GetContractName(typeof(MyExporterWithValidMetadata))));

            Assert.Equal(1, exports.Count());

            var export = exports.First();
            Assert.Equal("world", export.Metadata["hello"]);

            Assert.IsType<MyExporterWithValidMetadata>(export.Value);
        }

        [Fact]
        [Trait("Type", "Integration")]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ValueTypeFromCatalog()
        {
            var catalog = CatalogFactory.CreateDefaultAttributed();
            var container = new CompositionContainer(catalog);
            int singletonResult = container.GetExportedValue<int>("{AssemblyCatalogResolver}SingletonValueType");
            Assert.Equal(17, singletonResult);
            int factoryResult = container.GetExportedValue<int>("{AssemblyCatalogResolver}FactoryValueType");
            Assert.Equal(18, factoryResult);
        }

        [Export]
        [PartCreationPolicy(CreationPolicy.Any)]
        public class CreationPolicyAny
        {

        }

        [Fact]
        public void CreationPolicyAny_MultipleCallsReturnSameInstance()
        {
            var catalog = CatalogFactory.CreateAttributed(typeof(CreationPolicyAny));
            var provider = new CatalogExportProvider(catalog);
            provider.SourceProvider = ContainerFactory.Create();

            var export = provider.GetExportedValue<CreationPolicyAny>();

            for (int i = 0; i < 5; i++) // 5 is arbitrarily chosen
            {
                var export1 = provider.GetExportedValue<CreationPolicyAny>();

                Assert.Equal(export, export1);
            }
        }

        [Export]
        [PartCreationPolicy(CreationPolicy.Shared)]
        public class CreationPolicyShared
        {

        }

        [Fact]
        public void CreationPolicyShared_MultipleCallsReturnSameInstance()
        {
            var catalog = CatalogFactory.CreateAttributed(typeof(CreationPolicyShared));
            var provider = new CatalogExportProvider(catalog);
            provider.SourceProvider = ContainerFactory.Create();

            var export = provider.GetExportedValue<CreationPolicyShared>();

            for (int i = 0; i < 5; i++) // 5 is arbitrarily chosen
            {
                var export1 = provider.GetExportedValue<CreationPolicyShared>();

                Assert.Equal(export, export1);
            }
        }

        [Export]
        [PartCreationPolicy(CreationPolicy.NonShared)]
        public class CreationPolicyNonShared
        {

        }

        [Fact]
        [ActiveIssue(25498)]
        public void CreationPolicyNonShared_MultipleCallsReturnsDifferentInstances()
        {
            var catalog = CatalogFactory.CreateAttributed(typeof(CreationPolicyNonShared));
            var provider = new CatalogExportProvider(catalog);
            provider.SourceProvider = ContainerFactory.Create();

            List<CreationPolicyNonShared> list = new List<CreationPolicyNonShared>();
            var export = provider.GetExportedValue<CreationPolicyNonShared>();
            list.Add(export);

            for (int i = 0; i < 5; i++) // 5 is arbitrarily chosen
            {
                export = provider.GetExportedValue<CreationPolicyNonShared>();

                Assert.DoesNotContain(export, list);
                Assert.Contains(export, list);
                list.Add(export);
            }
        }

        [Fact]
        [ActiveIssue(684514)]
        public void GetExports_NoSourceProvider_ShouldThrowInvalidOperation()
        {
            var catalog = CatalogFactory.CreateAttributed();
            var provider = new CatalogExportProvider(catalog);

            ExceptionAssert.Throws<InvalidOperationException>(() =>
                provider.GetExports(ImportFromContract("Foo")));
        }

        [Fact]
        [Trait("Type", "Integration")]
        [ActiveIssue(561310)]
        public void Recomposition_PartDefWithRecomposableImportIsRemoved_ExportsMatchingImportChanged_ShouldNotBeRecomposed()
        {
            string dependencyContractName = "dependency";
            var exportValue = new object();

            var exporterPart = PartFactory.CreateExporter(dependencyContractName, exportValue);
            var importerPart = PartFactory.CreateImporter(dependencyContractName, true);

            var exporterCatalog = CatalogFactory.Create(exporterPart);
            var importerCatalog = CatalogFactory.Create(importerPart);

            var aggregateCatalog = CatalogFactory.CreateAggregateCatalog(importerCatalog, exporterCatalog);

            var provider = new CatalogExportProvider(aggregateCatalog);
            provider.SourceProvider = provider;

            var exports = provider.GetExports(importerPart.ImportDefinitions.Single());
            Assert.Equal(exportValue, importerPart.Value);

            aggregateCatalog.Catalogs.Remove(importerCatalog);
            aggregateCatalog.Catalogs.Remove(exporterCatalog);

            Assert.Equal(exportValue, importerPart.Value);
        }

        [Fact]
        [Trait("Type", "Integration")]
        [ActiveIssue(561310)]
        public void Recomposition_PartDefWithNonRecomposableImportIsRemoved_ExportsMatchingImportChanged_ShouldNotBeRejected()
        {
            string dependencyContractName = "dependency";
            var exportValue = new object();

            var exporterPart = PartFactory.CreateExporter(dependencyContractName, exportValue);
            var importerPart = PartFactory.CreateImporter(dependencyContractName, false);

            var exporterCatalog = CatalogFactory.Create(exporterPart);
            var importerCatalog = CatalogFactory.Create(importerPart);

            var aggregateCatalog = CatalogFactory.CreateAggregateCatalog(importerCatalog, exporterCatalog);

            var provider = new CatalogExportProvider(aggregateCatalog);
            provider.SourceProvider = provider;

            var exports = provider.GetExports(importerPart.ImportDefinitions.Single());
            Assert.Equal(exportValue, importerPart.Value);

            aggregateCatalog.Catalogs.Remove(importerCatalog);
            aggregateCatalog.Catalogs.Remove(exporterCatalog);

            Assert.Equal(exportValue, importerPart.Value);
        }

        [Fact]
        [ActiveIssue(25498)]
        public void CanBeCollectedAfterDispose()
        {
            AggregateExportProvider sourceExportProvider = new AggregateExportProvider();
            var catalog = new AggregateCatalog(CatalogFactory.CreateDefaultAttributed());
            var catalogExportProvider = new CatalogExportProvider(catalog);
            catalogExportProvider.SourceProvider = sourceExportProvider;

            WeakReference weakCatalogExportProvider = new WeakReference(catalogExportProvider);
            catalogExportProvider.Dispose();
            catalogExportProvider = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.False(weakCatalogExportProvider.IsAlive);

            GC.KeepAlive(sourceExportProvider);
            GC.KeepAlive(catalog);
        }

        [Fact]
        public void RemovingAndReAddingMultipleDefinitionsFromCatalog()
        {
            var fixedParts = new TypeCatalog(typeof(RootMultipleImporter), typeof(ExportedService));
            var changingParts = new TypeCatalog(typeof(Exporter1), typeof(Exporter2));
            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(fixedParts);
            catalog.Catalogs.Add(changingParts);
            var catalogExportProvider = new CatalogExportProvider(catalog);
            catalogExportProvider.SourceProvider = catalogExportProvider;

            var root = catalogExportProvider.GetExport<RootMultipleImporter>().Value;
            Assert.Equal(2, root.Imports.Length);

            catalog.Catalogs.Remove(changingParts);
            Assert.Equal(0, root.Imports.Length);

            catalog.Catalogs.Add(changingParts);
            Assert.Equal(2, root.Imports.Length);
        }

        [Export]
        public class RootMultipleImporter
        {
            [ImportMany(AllowRecomposition = true)]
            public IExportedInterface[] Imports { get; set; }
        }

        public interface IExportedInterface
        {
        }

        [Export(typeof(IExportedInterface))]
        public class Exporter1 : IExportedInterface
        {
            [Import]
            public ExportedService Service { get; set; }
        }

        [Export(typeof(IExportedInterface))]
        public class Exporter2 : IExportedInterface
        {
            [Import]
            public ExportedService Service { get; set; }
        }

        [Export]
        public class ExportedService
        {
        }

        private static ImportDefinition ImportFromContract(string contractName)
        {
            return ImportDefinitionFactory.CreateDefault(contractName,

                                                     ImportCardinality.ZeroOrMore,
                                                     false,
                                                     false);
        }

        private static ImportDefinition ImportFromContractAndMetadata(string contractName, string[] metadataKeys, Type[] metadataValues)
        {
            Assert.Equal(metadataKeys.Length, metadataValues.Length);
            Dictionary<string, Type> requiredMetadata = new Dictionary<string, Type>();
            for (int i = 0; i < metadataKeys.Length; i++)
            {
                requiredMetadata.Add(metadataKeys[i], metadataValues[i]);
            }

            return new ContractBasedImportDefinition(contractName,
                                                     (string)null,
                                                     requiredMetadata,
                                                     ImportCardinality.ZeroOrMore,
                                                     false,
                                                     false,
                                                     CreationPolicy.Any);
        }

        private static CatalogExportProvider CreateCatalogExportProvider()
        {
            return CreateCatalogExportProvider(CatalogFactory.Create());
        }

        private static CatalogExportProvider CreateCatalogExportProvider(params ComposablePartDefinition[] definitions)
        {
            return CreateCatalogExportProvider(CatalogFactory.Create(definitions));
        }

        private static CatalogExportProvider CreateCatalogExportProvider(params ComposablePart[] parts)
        {
            return CreateCatalogExportProvider(CatalogFactory.Create(parts));
        }

        private static CatalogExportProvider CreateCatalogExportProvider(ComposablePartCatalog catalog)
        {
            return new CatalogExportProvider(catalog);
        }
    }
}
