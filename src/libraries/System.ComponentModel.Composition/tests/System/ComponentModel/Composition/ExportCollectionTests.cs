// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ExportCollectionTests
    {
        public class Importer
        {
            [ImportMany("Value")]
            public Collection<Lazy<object>> CollectionPlain { get; set; }

            [ImportMany("Value")]
            public Collection<Lazy<object, IDictionary<string, object>>> CollectionPlainRawMetadata { get; set; }

            [ImportMany("EmptyValue")]
            public Collection<Lazy<object>> CollectionPlainEmpty { get; set; }

            [ImportMany("EmptyValue")]
            public Collection<Lazy<object, IDictionary<string, object>>> CollectionPlainEmptyRawMetadata { get; set; }

            [ImportMany("Value")]
            public Collection<Lazy<int>> CollectionTyped { get; set; }

            [ImportMany("Value")]
            public Collection<Lazy<int, IDictionary<string, object>>> CollectionTypedRawMetadata { get; set; }

            [ImportMany("EmptyValue")]
            public Collection<Lazy<int>> CollectionTypedEmpty { get; set; }

            [ImportMany("Value")]
            public Collection<Lazy<int, ITrans_MetadataTests_CustomMetadata>> CollectionTypedMetadata { get; set; }

            [ImportMany("EmptyValue")]
            public Collection<Lazy<int, ITrans_MetadataTests_CustomMetadata>> CollectionTypedMetadataEmpty { get; set; }

            [ImportMany("Value")]
            public IEnumerable<int> ReadWriteEnumerable { get; set; }

            [ImportMany("EmptyValue")]
            public IEnumerable<int> ReadWriteEnumerableEmpty { get; set; }

            [ImportMany("Value")]
            public IEnumerable<Lazy<object>> MetadataUntypedEnumerable { get; set; }

            [ImportMany("Value")]
            public IEnumerable<Lazy<object, IDictionary<string, object>>> MetadataUntypedEnumerableRawMetadata { get; set; }

            [ImportMany("EmptyValue")]
            public IEnumerable<Lazy<object>> MetadataUntypedEnumerableEmpty { get; set; }

            [ImportMany("EmptyValue")]
            public IEnumerable<Lazy<object, IDictionary<string, object>>> MetadataUntypedEnumerableEmptyRawMetadata { get; set; }

            [ImportMany("Value")]
            public IEnumerable<Lazy<int>> MetadataTypedEnumerable { get; set; }

            [ImportMany("Value")]
            public IEnumerable<Lazy<int, IDictionary<string, object>>> MetadataTypedEnumerableRawMetadata { get; set; }

            [ImportMany("EmptyValue")]
            public IEnumerable<Lazy<int>> MetadataTypedEnumerableEmpty { get; set; }

            [ImportMany("Value")]
            public IEnumerable<Lazy<int, ITrans_MetadataTests_CustomMetadata>> MetadataFullyTypedEnumerable { get; set; }

            [ImportMany("EmptyValue")]
            public IEnumerable<Lazy<int, ITrans_MetadataTests_CustomMetadata>> MetadataFullyTypedEnumerableEmpty { get; set; }

            public void VerifyImport(params int[] expectedValues)
            {
                object[] untypedExpectedValues = expectedValues.Cast<object>().ToArray();

                ExportsAssert.AreEqual(CollectionPlain, untypedExpectedValues);
                ExportsAssert.AreEqual(CollectionPlainRawMetadata, untypedExpectedValues);
                EqualityExtensions.IsTrueForAll(CollectionPlainRawMetadata, i => true.Equals(i.Metadata["PropertyName"]));
                Assert.Empty(CollectionPlainEmpty);
                Assert.Empty(CollectionPlainEmptyRawMetadata);

                // Add a new Export to this collection to ensure that it doesn't
                // modifiy the other collections because they should each have there
                // own collection instance
                CollectionPlain.Add(ExportFactory.Create<object>("Value"));

                ExportsAssert.AreEqual(CollectionTyped, expectedValues);
                ExportsAssert.AreEqual(CollectionTypedRawMetadata, expectedValues);
                EqualityExtensions.IsTrueForAll(CollectionTypedRawMetadata, i => true.Equals(i.Metadata["PropertyName"]));
                Assert.Empty(CollectionTypedEmpty);

                ExportsAssert.AreEqual(CollectionTypedMetadata, expectedValues);
                EqualityExtensions.IsTrueForAll(CollectionTypedMetadata, i => true == i.Metadata.PropertyName);
                Assert.Empty(CollectionTypedMetadataEmpty);

                EnumerableAssert.AreEqual(ReadWriteEnumerable, expectedValues);
                Assert.Empty(ReadWriteEnumerableEmpty);

                ExportsAssert.AreEqual(MetadataUntypedEnumerable, untypedExpectedValues);
                ExportsAssert.AreEqual(MetadataUntypedEnumerableRawMetadata, untypedExpectedValues);
                EqualityExtensions.IsTrueForAll(MetadataUntypedEnumerableRawMetadata, i => true.Equals(i.Metadata["PropertyName"]));
                Assert.Empty(MetadataUntypedEnumerableEmpty);
                Assert.Empty(MetadataUntypedEnumerableEmptyRawMetadata);

                ExportsAssert.AreEqual(MetadataTypedEnumerable, expectedValues);
                ExportsAssert.AreEqual(MetadataTypedEnumerableRawMetadata, expectedValues);
                EqualityExtensions.IsTrueForAll(MetadataTypedEnumerableRawMetadata, i => true.Equals(i.Metadata["PropertyName"]));
                Assert.Empty(MetadataTypedEnumerableEmpty);

                ExportsAssert.AreEqual(MetadataFullyTypedEnumerable, expectedValues);
                EqualityExtensions.IsTrueForAll(MetadataFullyTypedEnumerable, i => true == i.Metadata.PropertyName);
                Assert.Empty(MetadataFullyTypedEnumerableEmpty);
            }
        }

        public class ExporterDefault21
        {
            public ExporterDefault21() { Value = 21; }
            public ExporterDefault21(int v) { Value = v; }

            [Export("Value")]
            [ExportMetadata("PropertyName", true)]
            public int Value { get; set; }
        }

        public class ExporterDefault42
        {
            public ExporterDefault42() { Value = 42; }
            public ExporterDefault42(int v) { Value = v; }

            [Export("Value")]
            [ExportMetadata("PropertyName", true)]
            public int Value { get; set; }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31792", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49365", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
        [Trait("Type", "Integration")]
        public void ImportCollectionsFromContainerOnly()
        {
            var container = ContainerFactory.Create();
            Importer importer = new Importer();

            CompositionBatch batch = new CompositionBatch();
            batch.AddParts(importer
                , new ExporterDefault21()
                , new ExporterDefault21(22)
                , new ExporterDefault42()
                , new ExporterDefault42(43));

            container.Compose(batch);

            importer.VerifyImport(21, 22, 42, 43);
        }

        [Fact]
        [Trait("Type", "Integration")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ImportCollectionsFromCatalogOnly()
        {
            var cat = CatalogFactory.CreateDefaultAttributed();
            var container = new CompositionContainer(cat);
            Importer importer = new Importer();

            CompositionBatch batch = new CompositionBatch();
            batch.AddParts(importer);
            container.Compose(batch);

            importer.VerifyImport(21, 42);
        }

        [Fact]
        [Trait("Type", "Integration")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ImportCollectionsFormContainerAndCatalog()
        {
            var cat = CatalogFactory.CreateDefaultAttributed();
            var container = new CompositionContainer(cat);
            Importer importer = new Importer();

            CompositionBatch batch = new CompositionBatch();
            batch.AddParts(importer
                , new ExporterDefault21(22)
                , new ExporterDefault42(43));

            container.Compose(batch);

            importer.VerifyImport(22, 43, 21, 42);
        }
    }
}
