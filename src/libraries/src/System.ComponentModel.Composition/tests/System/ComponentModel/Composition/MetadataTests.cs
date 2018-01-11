// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public class MetadataTests
    {
        #region Tests for metadata on exports

        public enum SimpleEnum
        {
            First
        }

        [PartNotDiscoverable]
        [Export]
        [ExportMetadata("String", "42")]
        [ExportMetadata("Int", 42)]
        [ExportMetadata("Float", 42.0f)]
        [ExportMetadata("Enum", SimpleEnum.First)]
        [ExportMetadata("Type", typeof(string))]
        [ExportMetadata("Object", 42)]
        public class SimpleMetadataExporter
        {
        }

        [PartNotDiscoverable]
        [Export]
        [ExportMetadata("String", null)] // null
        [ExportMetadata("Int", 42)]
        [ExportMetadata("Float", 42.0f)]
        [ExportMetadata("Enum", SimpleEnum.First)]
        [ExportMetadata("Type", typeof(string))]
        [ExportMetadata("Object", 42)]
        public class SimpleMetadataExporterWithNullReferenceValue
        {
        }

        [PartNotDiscoverable]
        [Export]
        [ExportMetadata("String", "42")]
        [ExportMetadata("Int", null)] //null
        [ExportMetadata("Float", 42.0f)]
        [ExportMetadata("Enum", SimpleEnum.First)]
        [ExportMetadata("Type", typeof(string))]
        [ExportMetadata("Object", 42)]
        public class SimpleMetadataExporterWithNullNonReferenceValue
        {
        }

        [PartNotDiscoverable]
        [Export]
        [ExportMetadata("String", "42")]
        [ExportMetadata("Int", "42")] // wrong type
        [ExportMetadata("Float", 42.0f)]
        [ExportMetadata("Enum", SimpleEnum.First)]
        [ExportMetadata("Type", typeof(string))]
        [ExportMetadata("Object", 42)]
        public class SimpleMetadataExporterWithTypeMismatch
        {
        }

        public interface ISimpleMetadataView
        {
            string String { get; }
            int Int { get; }
            float Float { get; }
            SimpleEnum Enum { get; }
            Type Type { get; }
            object Object { get; }
        }

        [Fact]
        public void SimpleMetadataTest()
        {
            var container = ContainerFactory.Create();
            container.ComposeParts(new SimpleMetadataExporter());

            var export = container.GetExport<SimpleMetadataExporter, ISimpleMetadataView>();

            Assert.Equal("42", export.Metadata.String);
            Assert.Equal(42, export.Metadata.Int);
            Assert.Equal(42.0f, export.Metadata.Float);
            Assert.Equal(SimpleEnum.First, export.Metadata.Enum);
            Assert.Equal(typeof(string), export.Metadata.Type);
            Assert.Equal(42, export.Metadata.Object);
        }

        [Fact]
        public void SimpleMetadataTestWithNullReferenceValue()
        {
            var container = ContainerFactory.Create();
            container.ComposeParts(new SimpleMetadataExporterWithNullReferenceValue());

            var export = container.GetExport<SimpleMetadataExporterWithNullReferenceValue, ISimpleMetadataView>();

            Assert.Equal(null, export.Metadata.String);
            Assert.Equal(42, export.Metadata.Int);
            Assert.Equal(42.0f, export.Metadata.Float);
            Assert.Equal(SimpleEnum.First, export.Metadata.Enum);
            Assert.Equal(typeof(string), export.Metadata.Type);
            Assert.Equal(42, export.Metadata.Object);
        }

        [Fact]
        public void SimpleMetadataTestWithNullNonReferenceValue()
        {
            var container = ContainerFactory.Create();
            container.ComposeParts(new SimpleMetadataExporterWithNullNonReferenceValue());

            var exports = container.GetExports<SimpleMetadataExporterWithNullNonReferenceValue, ISimpleMetadataView>();
            Assert.False(exports.Any());
        }

        [Fact]
        public void SimpleMetadataTestWithTypeMismatch()
        {
            var container = ContainerFactory.Create();
            container.ComposeParts(new SimpleMetadataExporterWithTypeMismatch());

            var exports = container.GetExports<SimpleMetadataExporterWithTypeMismatch, ISimpleMetadataView>();
            Assert.False(exports.Any());
        }

        [Fact]
        public void ValidMetadataTest()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(new MyExporterWithValidMetadata());
            container.Compose(batch);

            var typeVi = container.GetExport<MyExporterWithValidMetadata, IDictionary<string, object>>();
            var metadataFoo = typeVi.Metadata["foo"] as IList<string>;
            Assert.Equal(2, metadataFoo.Count());
            Assert.True(metadataFoo.Contains("bar1"), "The metadata collection should include value 'bar1'");
            Assert.True(metadataFoo.Contains("bar2"), "The metadata collection should include value 'bar2'");
            Assert.Equal("world", typeVi.Metadata["hello"]);
            Assert.Equal("GoodOneValue2", typeVi.Metadata["GoodOne2"]);

            var metadataAcme = typeVi.Metadata["acme"] as IList<object>;
            Assert.Equal(2, metadataAcme.Count());
            Assert.True(metadataAcme.Contains("acmebar"), "The metadata collection should include value 'bar'");
            Assert.True(metadataAcme.Contains(2.0), "The metadata collection should include value 2");

            var memberVi = container.GetExport<Func<double>, IDictionary<string, object>>("ContractForValidMetadata");
            var metadataBar = memberVi.Metadata["bar"] as IList<string>;
            Assert.Equal(2, metadataBar.Count());
            Assert.True(metadataBar.Contains("foo1"), "The metadata collection should include value 'foo1'");
            Assert.True(metadataBar.Contains("foo2"), "The metadata collection should include value 'foo2'");
            Assert.Equal("hello", memberVi.Metadata["world"]);
            Assert.Equal("GoodOneValue2", memberVi.Metadata["GoodOne2"]);

            var metadataStuff = memberVi.Metadata["stuff"] as IList<object>;
            Assert.Equal(2, metadataAcme.Count());
            Assert.True(metadataStuff.Contains("acmebar"), "The metadata collection should include value 'acmebar'");
            Assert.True(metadataStuff.Contains(2.0), "The metadata collection should include value 2");

        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ValidMetadataDiscoveredByComponentCatalogTest()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();
            ValidMetadataDiscoveredByCatalog(container);
        }

        private void ValidMetadataDiscoveredByCatalog(CompositionContainer container)
        {
            var export1 = container.GetExport<MyExporterWithValidMetadata, IDictionary<string, object>>();

            var metadataFoo = export1.Metadata["foo"] as IList<string>;
            Assert.Equal(2, metadataFoo.Count());
            Assert.True(metadataFoo.Contains("bar1"), "The metadata collection should include value 'bar1'");
            Assert.True(metadataFoo.Contains("bar2"), "The metadata collection should include value 'bar2'");
            Assert.Equal("world", export1.Metadata["hello"]);
            Assert.Equal("GoodOneValue2", export1.Metadata["GoodOne2"]);

            var metadataAcme = export1.Metadata["acme"] as IList<object>;
            Assert.Equal(2, metadataAcme.Count());
            Assert.True(metadataAcme.Contains("acmebar"), "The metadata collection should include value 'bar'");
            Assert.True(metadataAcme.Contains(2.0), "The metadata collection should include value 2");

            var export2 = container.GetExport<Func<double>, IDictionary<string, object>>("ContractForValidMetadata");
            var metadataBar = export2.Metadata["bar"] as IList<string>;
            Assert.Equal(2, metadataBar.Count());
            Assert.True(metadataBar.Contains("foo1"), "The metadata collection should include value 'foo1'");
            Assert.True(metadataBar.Contains("foo2"), "The metadata collection should include value 'foo2'");
            Assert.Equal("hello", export2.Metadata["world"]);
            Assert.Equal("GoodOneValue2", export2.Metadata["GoodOne2"]);

            var metadataStuff = export2.Metadata["stuff"] as IList<object>;
            Assert.Equal(2, metadataAcme.Count());
            Assert.True(metadataStuff.Contains("acmebar"), "The metadata collection should include value 'acmebar'");
            Assert.True(metadataStuff.Contains(2.0), "The metadata collection should include value 2");
        }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
        [MetadataAttribute]
        public class BadStrongMetadata : Attribute
        {
            public string SelfConflicted { get { return "SelfConflictedValue"; } }
        }

        [Export]
        [BadStrongMetadata]
        [ExportMetadata("InvalidCollection", "InvalidCollectionValue1")]
        [ExportMetadata("InvalidCollection", "InvalidCollectionValue2", IsMultiple = true)]
        [BadStrongMetadata]
        [ExportMetadata("RepeatedMetadata", "RepeatedMetadataValue1")]
        [ExportMetadata("RepeatedMetadata", "RepeatedMetadataValue2")]
        [ExportMetadata("GoodOne1", "GoodOneValue1")]
        [ExportMetadata("ConflictedOne1", "ConfilictedOneValue1")]
        [GoodStrongMetadata]
        [ExportMetadata("ConflictedOne2", "ConflictedOne2Value2")]
        [PartNotDiscoverable]
        public class MyExporterWithInvalidMetadata
        {
            [Export("ContractForInvalidMetadata")]
            [ExportMetadata("ConflictedOne1", "ConfilictedOneValue1")]
            [GoodStrongMetadata]
            [ExportMetadata("ConflictedOne2", "ConflictedOne2Value2")]
            [ExportMetadata("RepeatedMetadata", "RepeatedMetadataValue1")]
            [ExportMetadata("RepeatedMetadata", "RepeatedMetadataValue2")]
            [BadStrongMetadata]
            [ExportMetadata("InvalidCollection", "InvalidCollectionValue1")]
            [ExportMetadata("InvalidCollection", "InvalidCollectionValue2", IsMultiple = true)]
            [BadStrongMetadata]
            [ExportMetadata("GoodOne1", "GoodOneValue1")]
            public double DoSomething() { return 0.618; }
        }

        [Export]
        [ExportMetadata("DuplicateMetadataName", "My Name")]
        [ExportMetadata("DuplicateMetadataName", "Your Name")]
        [PartNotDiscoverable]
        public class ClassWithInvalidDuplicateMetadataOnType
        {

        }

        [Fact]
        public void InvalidDuplicateMetadataOnType_ShouldThrow()
        {
            var part = AttributedModelServices.CreatePart(new ClassWithInvalidDuplicateMetadataOnType());
            var export = part.ExportDefinitions.First();
            var ex = ExceptionAssert.Throws<InvalidOperationException>(RetryMode.DoNotRetry, () =>
            {
                var metadata = export.Metadata;
            });

            Assert.True(ex.Message.Contains("DuplicateMetadataName"));
        }

        [PartNotDiscoverable]
        public class ClassWithInvalidDuplicateMetadataOnMember
        {
            [Export]
            [ExportMetadata("DuplicateMetadataName", "My Name")]
            [ExportMetadata("DuplicateMetadataName", "Your Name")]
            public ClassWithDuplicateMetadataOnMember Member { get; set; }
        }

        [Fact]
        public void InvalidDuplicateMetadataOnMember_ShouldThrow()
        {
            var part = AttributedModelServices.CreatePart(new ClassWithInvalidDuplicateMetadataOnMember());
            var export = part.ExportDefinitions.First();

            var ex = ExceptionAssert.Throws<InvalidOperationException>(RetryMode.DoNotRetry, () =>
            {
                var metadata = export.Metadata;
            });

            Assert.True(ex.Message.Contains("DuplicateMetadataName"));
        }

        [Export]
        [ExportMetadata("DuplicateMetadataName", "My Name", IsMultiple = true)]
        [ExportMetadata("DuplicateMetadataName", "Your Name", IsMultiple = true)]
        public class ClassWithValidDuplicateMetadataOnType
        {

        }

        [Fact]
        public void ValidDuplicateMetadataOnType_ShouldDiscoverAllMetadata()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(new ClassWithValidDuplicateMetadataOnType());

            container.Compose(batch);

            var export = container.GetExport<ClassWithValidDuplicateMetadataOnType, IDictionary<string, object>>();

            var names = export.Metadata["DuplicateMetadataName"] as string[];

            Assert.Equal(2, names.Length);
        }

        public class ClassWithDuplicateMetadataOnMember
        {
            [Export]
            [ExportMetadata("DuplicateMetadataName", "My Name", IsMultiple = true)]
            [ExportMetadata("DuplicateMetadataName", "Your Name", IsMultiple = true)]
            public ClassWithDuplicateMetadataOnMember Member { get; set; }
        }

        [Fact]
        public void ValidDuplicateMetadataOnMember_ShouldDiscoverAllMetadata()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(new ClassWithDuplicateMetadataOnMember());

            container.Compose(batch);

            var export = container.GetExport<ClassWithDuplicateMetadataOnMember, IDictionary<string, object>>();

            var names = export.Metadata["DuplicateMetadataName"] as string[];

            Assert.Equal(2, names.Length);
        }

        [Export]
        [ExportMetadata(CompositionConstants.PartCreationPolicyMetadataName, "My Policy")]
        [PartNotDiscoverable]
        public class ClassWithReservedMetadataValue
        {

        }

        [Fact]
        public void InvalidMetadata_UseOfReservedName_ShouldThrow()
        {
            var part = AttributedModelServices.CreatePart(new ClassWithReservedMetadataValue());
            var export = part.ExportDefinitions.First();

            var ex = ExceptionAssert.Throws<InvalidOperationException>(RetryMode.DoNotRetry, () =>
            {
                var metadata = export.Metadata;
            });

            Assert.True(ex.Message.Contains(CompositionConstants.PartCreationPolicyMetadataName));
        }

        #endregion

        #region Tests for weakly supported metadata as part of contract

        [Fact]
        [ActiveIssue(468388)]
        public void FailureImportForNoRequiredMetadatForExportCollection()
        {
            CompositionContainer container = ContainerFactory.Create();

            MyImporterWithExportCollection importer;
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(new MyExporterWithNoMetadata());
            batch.AddPart(importer = new MyImporterWithExportCollection());

            throw new NotImplementedException();

            //var result = container.TryCompose();

            //Assert.True(result.Succeeded, "Composition should be successful because collection import is not required");
            //Assert.Equal(1, result.Issues.Count);
            //Assert.True(result.Issues[0].Description.Contains("Foo"), "The missing required metadata is 'Foo'");
        }

        [Fact]
        [ActiveIssue(472538)]
        public void FailureImportForNoRequiredMetadataThroughComponentCatalogTest()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();
            FailureImportForNoRequiredMetadataThroughCatalog(container);
        }

        private void FailureImportForNoRequiredMetadataThroughCatalog(CompositionContainer container)
        {
            throw new NotImplementedException();

            //var export1 = container.GetExport<MyImporterWithExport>();

            //export1.TryGetExportedValue().VerifyFailure(CompositionIssueId.RequiredMetadataNotFound, CompositionIssueId.CardinalityMismatch);

            //var export2 = container.GetExport<MyImporterWithExportCollection>();
            //export2.TryGetExportedValue().VerifySuccess(CompositionIssueId.RequiredMetadataNotFound);

            //container.TryGetExportedValue<MyImporterWithValue>().VerifyFailure(CompositionIssueId.RequiredMetadataNotFound, CompositionIssueId.CardinalityMismatch);
        }

        [Fact]
        [ActiveIssue(468388)]
        public void SelectiveImportBasedOnMetadataForExport()
        {
            CompositionContainer container = ContainerFactory.Create();

            MyImporterWithExportForSelectiveImport importer;
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(new MyExporterWithNoMetadata());
            batch.AddPart(new MyExporterWithMetadata());
            batch.AddPart(importer = new MyImporterWithExportForSelectiveImport());

            throw new NotImplementedException();
            //var result = container.TryCompose();

            //Assert.True(result.Succeeded, "Composition should be successfull because one of two exports meets both the contract name and metadata requirement");
            //Assert.Equal(1, result.Issues.Count);
            //Assert.True(result.Issues[0].Description.Contains("Foo"), "The missing required metadata is 'Foo'");
            //Assert.NotNull(importer.ValueInfo, "The import should really get bound");
        }

        [Fact]
        [ActiveIssue(468388)]
        public void SelectiveImportBasedOnMetadataForExportCollection()
        {
            CompositionContainer container = ContainerFactory.Create();

            MyImporterWithExportCollectionForSelectiveImport importer;
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(new MyExporterWithNoMetadata());
            batch.AddPart(new MyExporterWithMetadata());
            batch.AddPart(importer = new MyImporterWithExportCollectionForSelectiveImport());

            throw new NotImplementedException();

            //var result = container.TryCompose();

            //Assert.True(result.Succeeded, "Composition should be successfull in anyway for collection import");
            //Assert.Equal(1, result.Issues.Count);
            //Assert.True(result.Issues[0].Description.Contains("Foo"), "The missing required metadata is 'Foo'");
            //Assert.Equal(1, importer.ValueInfoCol.Count);
            //Assert.NotNull(importer.ValueInfoCol[0], "The import should really get bound");
        }

        [Fact]
        [ActiveIssue(472538)]
        public void SelectiveImportBasedOnMetadataThruoughComponentCatalogTest()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();
            SelectiveImportBasedOnMetadataThruoughCatalog(container);
        }

        private void SelectiveImportBasedOnMetadataThruoughCatalog(CompositionContainer container)
        {
            throw new NotImplementedException();

            //var export1 = container.GetExport<MyImporterWithExportForSelectiveImport>();
            //export1.TryGetExportedValue().VerifySuccess(CompositionIssueId.RequiredMetadataNotFound);

            //var export2 = container.GetExport<MyImporterWithExportCollectionForSelectiveImport>();
            //export2.TryGetExportedValue().VerifySuccess(CompositionIssueId.RequiredMetadataNotFound);
        }

        [Fact]
        public void ChildParentContainerTest1()
        {
            CompositionContainer parent = ContainerFactory.Create();
            CompositionContainer child = new CompositionContainer(parent);

            CompositionBatch childBatch = new CompositionBatch();
            CompositionBatch parentBatch = new CompositionBatch();
            parentBatch.AddPart(new MyExporterWithNoMetadata());
            childBatch.AddPart(new MyExporterWithMetadata());
            parent.Compose(parentBatch);
            child.Compose(childBatch);

            var exports = child.GetExports(CreateImportDefinition(typeof(IMyExporter), "Foo"));

            Assert.Equal(1, exports.Count());
        }

        [Fact]
        public void ChildParentContainerTest2()
        {
            CompositionContainer parent = ContainerFactory.Create();
            CompositionContainer child = new CompositionContainer(parent);

            CompositionBatch childBatch = new CompositionBatch();
            CompositionBatch parentBatch = new CompositionBatch();
            parentBatch.AddPart(new MyExporterWithMetadata());
            childBatch.AddPart(new MyExporterWithNoMetadata());
            parent.Compose(parentBatch);

            var exports = child.GetExports(CreateImportDefinition(typeof(IMyExporter), "Foo"));

            Assert.Equal(1, exports.Count());
        }

        [Fact]
        public void ChildParentContainerTest3()
        {
            CompositionContainer parent = ContainerFactory.Create();
            CompositionContainer child = new CompositionContainer(parent);

            CompositionBatch childBatch = new CompositionBatch();
            CompositionBatch parentBatch = new CompositionBatch();

            parentBatch.AddPart(new MyExporterWithMetadata());
            childBatch.AddPart(new MyExporterWithMetadata());
            parent.Compose(parentBatch);
            child.Compose(childBatch);

            var exports = child.GetExports(CreateImportDefinition(typeof(IMyExporter), "Foo"));

            Assert.Equal(2, exports.Count());
        }

        private static ImportDefinition CreateImportDefinition(Type type, string metadataKey)
        {
            return new ContractBasedImportDefinition(AttributedModelServices.GetContractName(typeof(IMyExporter)), null, new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>(metadataKey, typeof(object)) }, ImportCardinality.ZeroOrMore, true, true, CreationPolicy.Any);
        }

        #endregion

        #region Tests for strongly typed metadata as part of contract

        [Fact]
        [ActiveIssue(468388)]
        public void SelectiveImportBySTM_Export()
        {
            CompositionContainer container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();

            MyImporterWithExportStronglyTypedMetadata importer;
            batch.AddPart(new MyExporterWithNoMetadata());
            batch.AddPart(new MyExporterWithMetadata());
            batch.AddPart(importer = new MyImporterWithExportStronglyTypedMetadata());

            throw new NotImplementedException();

            //var result = container.TryCompose();

            //Assert.True(result.Succeeded, "Composition should be successful becasue one of two exports does not have required metadata");
            //Assert.Equal(1, result.Issues.Count);
            //Assert.NotNull(importer.ValueInfo, "The valid export should really get bound");
            //Assert.Equal("Bar", importer.ValueInfo.Metadata.Foo);
        }

        [Fact]
        [ActiveIssue(468388)]
        public void SelectiveImportBySTM_ExportCollection()
        {
            CompositionContainer container = ContainerFactory.Create();

            MyImporterWithExportCollectionStronglyTypedMetadata importer;
            CompositionBatch batch = new CompositionBatch();

            batch.AddPart(new MyExporterWithNoMetadata());
            batch.AddPart(new MyExporterWithMetadata());
            batch.AddPart(importer = new MyImporterWithExportCollectionStronglyTypedMetadata());

            throw new NotImplementedException();

            //var result = container.TryCompose();

            //Assert.True(result.Succeeded, "Collection import should be successful in anyway");
            //Assert.Equal(1, result.Issues.Count);
            //Assert.Equal(1, importer.ValueInfoCol.Count);
            //Assert.Equal("Bar", importer.ValueInfoCol.First().Metadata.Foo);
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void SelectiveImportBySTMThroughComponentCatalog1()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();
            SelectiveImportBySTMThroughCatalog1(container);
        }

        public void SelectiveImportBySTMThroughCatalog1(CompositionContainer container)
        {
            Assert.NotNull(container.GetExport<IMyExporter, IMetadataView>());
            var result2 = container.GetExports<IMyExporter, IMetadataView>();
        }

        [Fact]
        [ActiveIssue(468388)]
        public void SelectiveImportBySTMThroughComponentCatalog2()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();
            SelectiveImportBySTMThroughCatalog2(container);
        }

        public void SelectiveImportBySTMThroughCatalog2(CompositionContainer container)
        {
            throw new NotImplementedException();

            //var export1 = container.GetExport<MyImporterWithExportStronglyTypedMetadata>();
            //var result1 = export1.TryGetExportedValue().VerifySuccess(CompositionIssueId.RequiredMetadataNotFound);
            //Assert.NotNull(result1.Value.ValueInfo, "The valid export should really get bound");
            //Assert.Equal("Bar", result1.Value.ValueInfo.Metadata.Foo);

            //var export2 = container.GetExport<MyImporterWithExportCollectionStronglyTypedMetadata>();
            //var result2 = export2.TryGetExportedValue().VerifySuccess(CompositionIssueId.RequiredMetadataNotFound);
            //Assert.Equal(1, result2.Value.ValueInfoCol.Count);
            //Assert.Equal("Bar", result2.Value.ValueInfoCol.First().Metadata.Foo);
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void TestMultipleStronglyTypedAttributes()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();

            var export = container.GetExport<ExportMultiple, IMyOptions>();
            EnumerableAssert.AreEqual(export.Metadata.OptionNames.OrderBy(s => s), "name1", "name2", "name3");
            EnumerableAssert.AreEqual(export.Metadata.OptionValues.OrderBy(o => o.ToString()), "value1", "value2", "value3");
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void TestMultipleStronglyTypedAttributesAsIEnumerable()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();

            var export = container.GetExport<ExportMultiple, IMyOptionsAsIEnumerable>();
            EnumerableAssert.AreEqual(export.Metadata.OptionNames.OrderBy(s => s), "name1", "name2", "name3");
            EnumerableAssert.AreEqual(export.Metadata.OptionValues.OrderBy(o => o.ToString()), "value1", "value2", "value3");
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void TestMultipleStronglyTypedAttributesAsArray()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();

            var export = container.GetExport<ExportMultiple, IMyOptionsAsArray>();
            EnumerableAssert.AreEqual(export.Metadata.OptionNames.OrderBy(s => s), "name1", "name2", "name3");
            EnumerableAssert.AreEqual(export.Metadata.OptionValues.OrderBy(o => o.ToString()), "value1", "value2", "value3");
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void TestMultipleStronglyTypedAttributesWithInvalidType()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();

            // IMyOption2 actually contains all the correct properties but just the wrong types. This should cause us to not match the exports by metadata
            var exports = container.GetExports<ExportMultiple, IMyOption2>();
            Assert.Equal(0, exports.Count());
        }

        [Fact]
        public void TestOptionalMetadataValueTypeMismatch()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(OptionalFooIsInt));
            var exports = container.GetExports<OptionalFooIsInt, IMetadataView>();
            Assert.Equal(1, exports.Count());
            var export = exports.Single();
            Assert.Equal(null, export.Metadata.OptionalFoo);
        }

        #endregion

        [ExportMetadata("Name", "FromBaseType")]
        public abstract class BaseClassWithMetadataButNoExport
        {
        }

        [Export(typeof(BaseClassWithMetadataButNoExport))]
        public class DerivedClassWithExportButNoMetadata : BaseClassWithMetadataButNoExport
        {
        }

        [Fact]
        public void Metadata_BaseClassWithMetadataButNoExport()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(BaseClassWithMetadataButNoExport),
                typeof(DerivedClassWithExportButNoMetadata));

            var export = container.GetExport<BaseClassWithMetadataButNoExport, IDictionary<string, object>>();

            Assert.False(export.Metadata.ContainsKey("Name"), "Export should only contain metadata from the derived!");
        }

        [InheritedExport(typeof(BaseClassWithExportButNoMetadata))]
        public abstract class BaseClassWithExportButNoMetadata
        {
        }

        [ExportMetadata("Name", "FromDerivedType")]
        public class DerivedClassMetadataButNoExport : BaseClassWithExportButNoMetadata
        {
        }

        [Fact]
        public void Metadata_BaseClassWithExportButNoMetadata()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(BaseClassWithExportButNoMetadata),
                typeof(DerivedClassMetadataButNoExport));

            var export = container.GetExport<BaseClassWithExportButNoMetadata, IDictionary<string, object>>();

            Assert.False(export.Metadata.ContainsKey("Name"), "Export should only contain metadata from the base!");
        }

        [Export(typeof(BaseClassWithExportAndMetadata))]
        [ExportMetadata("Name", "FromBaseType")]
        public class BaseClassWithExportAndMetadata
        {
        }

        [Export(typeof(DerivedClassWithExportAndMetadata))]
        [ExportMetadata("Name", "FromDerivedType")]
        public class DerivedClassWithExportAndMetadata : BaseClassWithExportAndMetadata
        {
        }

        [Fact]
        public void Metadata_BaseAndDerivedWithExportAndMetadata()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(BaseClassWithExportAndMetadata),
                typeof(DerivedClassWithExportAndMetadata));

            var exportBase = container.GetExport<BaseClassWithExportAndMetadata, IDictionary<string, object>>();

            Assert.Equal("FromBaseType", exportBase.Metadata["Name"]);

            var exportDerived = container.GetExport<DerivedClassWithExportAndMetadata, IDictionary<string, object>>();
            Assert.Equal("FromDerivedType", exportDerived.Metadata["Name"]);
        }

        [Export]
        [ExportMetadata("Data", null, IsMultiple = true)]
        [ExportMetadata("Data", false, IsMultiple = true)]
        [ExportMetadata("Data", Int16.MaxValue, IsMultiple = true)]
        [ExportMetadata("Data", Int32.MaxValue, IsMultiple = true)]
        [ExportMetadata("Data", Int64.MaxValue, IsMultiple = true)]
        [ExportMetadata("Data", UInt16.MaxValue, IsMultiple = true)]
        [ExportMetadata("Data", UInt32.MaxValue, IsMultiple = true)]
        [ExportMetadata("Data", UInt64.MaxValue, IsMultiple = true)]
        [ExportMetadata("Data", "String", IsMultiple = true)]
        [ExportMetadata("Data", typeof(ClassWithLotsOfDifferentMetadataTypes), IsMultiple = true)]
        [ExportMetadata("Data", CreationPolicy.NonShared, IsMultiple = true)]
        [ExportMetadata("Data", new object[] { 1, 2, null }, IsMultiple = true)]
        public class ClassWithLotsOfDifferentMetadataTypes
        {
        }

        [Fact]
        public void ExportWithValidCollectionOfMetadata_ShouldDiscoverAllMetadata()
        {
            var catalog = CatalogFactory.CreateAttributed(typeof(ClassWithLotsOfDifferentMetadataTypes));

            var export = catalog.Parts.First().ExportDefinitions.First();

            var data = (object[])export.Metadata["Data"];

            Assert.Equal(12, data.Length);
        }

        [Export]
        [ExportMetadata("Data", null, IsMultiple = true)]
        [ExportMetadata("Data", 1, IsMultiple = true)]
        [ExportMetadata("Data", 2, IsMultiple = true)]
        [ExportMetadata("Data", 3, IsMultiple = true)]
        public class ClassWithIntCollectionWithNullValue
        {
        }

        [Fact]
        public void ExportWithIntCollectionPlusNullValueOfMetadata_ShouldDiscoverAllMetadata()
        {
            var catalog = CatalogFactory.CreateAttributed(typeof(ClassWithIntCollectionWithNullValue));

            var export = catalog.Parts.First().ExportDefinitions.First();

            var data = (object[])export.Metadata["Data"];

            Assert.IsNotType<int[]>(data);

            Assert.Equal(4, data.Length);
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        [MetadataAttribute]
        public class DataAttribute : Attribute
        {
            public object Object { get; set; }
        }

        [Export]
        [Data(Object = "42")]
        [Data(Object = "10")]
        public class ExportWithMultipleMetadata_ExportStringsAsObjects
        {
        }

        [Export]
        [Data(Object = "42")]
        [Data(Object = "10")]
        [Data(Object = null)]
        public class ExportWithMultipleMetadata_ExportStringsAsObjects_WithNull
        {
        }

        [Export]
        [Data(Object = 42)]
        [Data(Object = 10)]
        public class ExportWithMultipleMetadata_ExportIntsAsObjects
        {
        }

        [Export]
        [Data(Object = null)]
        [Data(Object = 42)]
        [Data(Object = 10)]
        public class ExportWithMultipleMetadata_ExportIntsAsObjects_WithNull
        {
        }

        public interface IObjectView_AsStrings
        {
            string[] Object { get; }
        }

        public interface IObjectView_AsInts
        {
            int[] Object { get; }
        }

        public interface IObjectView
        {
            object[] Object { get; }
        }

        [Fact]
        public void ExportWithMultipleMetadata_ExportStringsAsObjects_ShouldDiscoverMetadataAsStrings()
        {
            var container = ContainerFactory.Create();
            container.ComposeParts(new ExportWithMultipleMetadata_ExportStringsAsObjects());

            var export = container.GetExport<ExportWithMultipleMetadata_ExportStringsAsObjects, IObjectView_AsStrings>();
            Assert.NotNull(export);

            Assert.NotNull(export.Metadata);
            Assert.NotNull(export.Metadata.Object);
            Assert.Equal(2, export.Metadata.Object.Length);
        }

        [Fact]
        public void ExportWithMultipleMetadata_ExportStringsAsObjects_With_Null_ShouldDiscoverMetadataAsStrings()
        {
            var container = ContainerFactory.Create();
            container.ComposeParts(new ExportWithMultipleMetadata_ExportStringsAsObjects_WithNull());

            var export = container.GetExport<ExportWithMultipleMetadata_ExportStringsAsObjects_WithNull, IObjectView_AsStrings>();
            Assert.NotNull(export);

            Assert.NotNull(export.Metadata);
            Assert.NotNull(export.Metadata.Object);
            Assert.Equal(3, export.Metadata.Object.Length);
        }

        [Fact]
        public void ExportWithMultipleMetadata_ExportIntsAsObjects_ShouldDiscoverMetadataAsInts()
        {
            var container = ContainerFactory.Create();
            container.ComposeParts(new ExportWithMultipleMetadata_ExportIntsAsObjects());

            var export = container.GetExport<ExportWithMultipleMetadata_ExportIntsAsObjects, IObjectView_AsInts>();
            Assert.NotNull(export);

            Assert.NotNull(export.Metadata);
            Assert.NotNull(export.Metadata.Object);
            Assert.Equal(2, export.Metadata.Object.Length);
        }

        [Fact]
        public void ExportWithMultipleMetadata_ExportIntsAsObjects_With_Null_ShouldDiscoverMetadataAsObjects()
        {
            var container = ContainerFactory.Create();
            container.ComposeParts(new ExportWithMultipleMetadata_ExportIntsAsObjects_WithNull());

            var exports = container.GetExports<ExportWithMultipleMetadata_ExportIntsAsObjects_WithNull, IObjectView_AsInts>();
            Assert.False(exports.Any());

            var export = container.GetExport<ExportWithMultipleMetadata_ExportIntsAsObjects_WithNull, IObjectView>();

            Assert.NotNull(export.Metadata);
            Assert.NotNull(export.Metadata.Object);
            Assert.Equal(3, export.Metadata.Object.Length);
        }

        [MetadataAttribute]
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        public class OrderAttribute : Attribute
        {
            public string Before { get; set; }
            public string After { get; set; }
        }

        public interface IOrderMetadataView
        {
            string[] Before { get; }
            string[] After { get; }
        }

        [Export]
        [Order(Before = "Step3")]
        [Order(Before = "Step2")]
        public class OrderedItemBeforesOnly
        {
        }

        [Fact]
        public void ExportWithMultipleMetadata_ExportStringsAndNulls_ThroughMetadataAttributes()
        {
            var container = ContainerFactory.Create();
            container.ComposeParts(new OrderedItemBeforesOnly());

            var export = container.GetExport<OrderedItemBeforesOnly, IOrderMetadataView>();
            Assert.NotNull(export);

            Assert.NotNull(export.Metadata);

            Assert.NotNull(export.Metadata.Before);
            Assert.NotNull(export.Metadata.After);

            Assert.Equal(2, export.Metadata.Before.Length);
            Assert.Equal(2, export.Metadata.After.Length);

            Assert.NotNull(export.Metadata.Before[0]);
            Assert.NotNull(export.Metadata.Before[1]);

            Assert.Null(export.Metadata.After[0]);
            Assert.Null(export.Metadata.After[1]);
        }

        [MetadataAttribute]
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        public class DataTypeAttribute : Attribute
        {
            public Type Type { get; set; }
        }

        public interface ITypesMetadataView
        {
            Type[] Type { get; }
        }

        [Export]
        [DataType(Type = typeof(int))]
        [DataType(Type = typeof(string))]
        public class ItemWithTypeExports
        {
        }

        [Export]
        [DataType(Type = typeof(int))]
        [DataType(Type = typeof(string))]
        [DataType(Type = null)]
        public class ItemWithTypeExports_WithNulls
        {
        }

        [Export]
        [DataType(Type = null)]
        [DataType(Type = null)]
        [DataType(Type = null)]
        public class ItemWithTypeExports_WithAllNulls
        {
        }

        [Fact]
        public void ExportWithMultipleMetadata_ExportTypes()
        {
            var container = ContainerFactory.Create();
            container.ComposeParts(new ItemWithTypeExports());

            var export = container.GetExport<ItemWithTypeExports, ITypesMetadataView>();

            Assert.NotNull(export.Metadata);
            Assert.NotNull(export.Metadata.Type);
            Assert.Equal(2, export.Metadata.Type.Length);
        }

        [Fact]
        public void ExportWithMultipleMetadata_ExportTypes_WithNulls()
        {
            var container = ContainerFactory.Create();
            container.ComposeParts(new ItemWithTypeExports_WithNulls());

            var export = container.GetExport<ItemWithTypeExports_WithNulls, ITypesMetadataView>();

            Assert.NotNull(export.Metadata);
            Assert.NotNull(export.Metadata.Type);
            Assert.Equal(3, export.Metadata.Type.Length);
        }

        [Fact]
        public void ExportWithMultipleMetadata_ExportTypes_WithAllNulls()
        {
            var container = ContainerFactory.Create();
            container.ComposeParts(new ItemWithTypeExports_WithAllNulls());

            var export = container.GetExport<ItemWithTypeExports_WithAllNulls, ITypesMetadataView>();

            Assert.NotNull(export.Metadata);
            Assert.NotNull(export.Metadata.Type);
            Assert.Equal(3, export.Metadata.Type.Length);

            Assert.Null(export.Metadata.Type[0]);
            Assert.Null(export.Metadata.Type[1]);
            Assert.Null(export.Metadata.Type[2]);
        }

        [Export]
        [ExportMetadata(null, "ValueOfNullKey")]
        public class ClassWithNullMetadataKey
        {
        }

        [Fact]
        public void ExportMetadataWithNullKey_ShouldUseEmptyString()
        {
            var nullMetadataCatalog = CatalogFactory.CreateAttributed(typeof(ClassWithNullMetadataKey));
            var nullMetadataExport = nullMetadataCatalog.Parts.Single().ExportDefinitions.Single();

            Assert.True(nullMetadataExport.Metadata.ContainsKey(string.Empty));
            Assert.Equal("ValueOfNullKey", nullMetadataExport.Metadata[string.Empty]);
        }

    }

    // Tests for metadata issues on export
    [Export]
    [ExportMetadata("foo", "bar1", IsMultiple = true)]
    [ExportMetadata("foo", "bar2", IsMultiple = true)]
    [ExportMetadata("acme", "acmebar", IsMultiple = true)]
    [ExportMetadata("acme", 2.0, IsMultiple = true)]
    [ExportMetadata("hello", "world")]
    [GoodStrongMetadata]
    public class MyExporterWithValidMetadata
    {
        [Export("ContractForValidMetadata")]
        [ExportMetadata("bar", "foo1", IsMultiple = true)]
        [ExportMetadata("bar", "foo2", IsMultiple = true)]
        [ExportMetadata("stuff", "acmebar", IsMultiple = true)]
        [ExportMetadata("stuff", 2.0, IsMultiple = true)]
        [ExportMetadata("world", "hello")] // the order of the attribute should not affect the result
        [GoodStrongMetadata]
        public double DoSomething() { return 0.618; }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    [MetadataAttribute]
    public class GoodStrongMetadata : Attribute
    {
        public string GoodOne2 { get { return "GoodOneValue2"; } }
        public string ConflictedOne1 { get { return "ConflictedOneValue1"; } }
        public string ConflictedOne2 { get { return "ConflictedOneValue2"; } }
    }

    // Tests for metadata as part of contract

    public interface IMyExporter { }

    [Export]
    [Export(typeof(IMyExporter))]
    public class MyExporterWithNoMetadata : IMyExporter
    {
    }

    [Export]
    [Export(typeof(IMyExporter))]
    [ExportMetadata("Foo", "Bar")]
    public class MyExporterWithMetadata : IMyExporter
    {
    }

    public interface IMetadataFoo
    {
        string Foo { get; }
    }

    public interface IMetadataBar
    {
        string Bar { get; }
    }

    [Export]
    public class MyImporterWithExport
    {
        [Import(typeof(MyExporterWithNoMetadata))]
        public Lazy<MyExporterWithNoMetadata, IMetadataFoo> ValueInfo { get; set; }
    }

    [Export]
    public class SingleImportWithAllowDefault
    {
        [Import("Import", AllowDefault = true)]
        public Lazy<object> Import { get; set; }
    }

    [Export]
    public class SingleImport
    {
        [Import("Import")]
        public Lazy<object> Import { get; set; }
    }

    public interface IFooMetadataView
    {
        string Foo { get; }
    }

    [Export]
    public class MyImporterWithExportCollection
    {
        [ImportMany(typeof(MyExporterWithNoMetadata))]
        public IEnumerable<Lazy<MyExporterWithNoMetadata, IFooMetadataView>> ValueInfoCol { get; set; }
    }

    [Export]
    public class MyImporterWithExportForSelectiveImport
    {
        [Import]
        public Lazy<IMyExporter, IFooMetadataView> ValueInfo { get; set; }
    }

    [Export]
    public class MyImporterWithExportCollectionForSelectiveImport
    {
        [ImportMany]
        public Collection<Lazy<IMyExporter, IFooMetadataView>> ValueInfoCol { get; set; }
    }

    public interface IMetadataView
    {
        string Foo { get; }

        [System.ComponentModel.DefaultValue(null)]
        string OptionalFoo { get; }
    }

    [Export]
    [ExportMetadata("Foo", "fooValue3")]
    [ExportMetadata("OptionalFoo", 42)]
    public class OptionalFooIsInt { }

    [Export]
    public class MyImporterWithExportStronglyTypedMetadata
    {
        [Import]
        public Lazy<IMyExporter, IMetadataView> ValueInfo { get; set; }
    }

    [Export]
    public class MyImporterWithExportCollectionStronglyTypedMetadata
    {
        [ImportMany]
        public Collection<Lazy<IMyExporter, IMetadataView>> ValueInfoCol { get; set; }
    }

    public class MyExporterWithFullMetadata
    {
        [Export("MyStringContract")]
        public string String1 { get { return "String1"; } }

        [Export("MyStringContract")]
        [ExportMetadata("Foo", "fooValue")]
        public string String2 { get { return "String2"; } }

        [Export("MyStringContract")]
        [ExportMetadata("Bar", "barValue")]
        public string String3 { get { return "String3"; } }

        [Export("MyStringContract")]
        [ExportMetadata("Foo", "fooValue")]
        [ExportMetadata("Bar", "barValue")]
        public string String4 { get { return "String4"; } }
    }

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class MyOption : Attribute
    {
        public MyOption(string name, object value)
        {
            OptionNames = name;
            OptionValues = value;
        }
        public string OptionNames { get; set; }
        public object OptionValues { get; set; }
    }

    public interface IMyOptions
    {
        IList<string> OptionNames { get; }
        ICollection<string> OptionValues { get; }
    }

    public interface IMyOptionsAsIEnumerable
    {
        IEnumerable<string> OptionNames { get; }
        IEnumerable<string> OptionValues { get; }
    }

    public interface IMyOptionsAsArray
    {
        string[] OptionNames { get; }
        string[] OptionValues { get; }
    }

    [Export]
    [MyOption("name1", "value1")]
    [MyOption("name2", "value2")]
    [ExportMetadata("OptionNames", "name3", IsMultiple = true)]
    [ExportMetadata("OptionValues", "value3", IsMultiple = true)]
    public class ExportMultiple
    {
    }

    public interface IMyOption2
    {
        string OptionNames { get; }
        string OptionValues { get; }
    }
}
