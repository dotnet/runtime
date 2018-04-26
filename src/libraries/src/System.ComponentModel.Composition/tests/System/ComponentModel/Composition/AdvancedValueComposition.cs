// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public interface ITrans_ExportableTest
    {
        string Var1 { get; }
    }

    public interface ITrans_CollectionOfStrings
    {
        IEnumerable<string> Values { get; }
    }

    public class AdvancedValueComposition
    {
        [Fact]
        public void RepeatedContainerUse()
        {
            var container = ContainerFactory.Create();
            TrivialExporter e = new TrivialExporter();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(e);
            container.Compose(batch);

            batch = new CompositionBatch();
            batch.AddPart(new TrivialImporter());
            container.Compose(batch);

            Assert.True(e.done, "Initialization of importer should have set the done flag on E");
        }

        [Fact]
        public void FunctionsFieldsAndProperties()
        {
            Consumer c;
            var container = ContainerFactory.Create();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(new RealAddProvider());
            batch.AddPart(c = new Consumer());
            container.Compose(batch);

            Assert.Equal(3, c.op(c.a, c.b));
        }

        [Fact]
        public void FunctionsFieldsAndProperties2()
        {
            Consumer c;
            var container = ContainerFactory.Create();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(new SubtractProvider());
            batch.AddPart(c = new Consumer());
            container.Compose(batch);

            Assert.Equal(-1, c.op(c.a, c.b));
        }

        [Fact]
        [ActiveIssue(25498)]
        public void FunctionsFieldsAndProperties2_WithCatalog()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();

            ConsumerOfMultiple c = new ConsumerOfMultiple();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(c);
            container.Compose(batch);

            foreach (Lazy<Func<int,int,int>, IDictionary<string, object>> export in c.opInfo)
            {
                if ((string)export.Metadata["Var1"] == "add")
                {
                    Assert.Equal(3, export.Value(1, 2));
                }
                else if ((string)export.Metadata["Var1"] == "sub")
                {
                    Assert.Equal(-1, export.Value(1, 2));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void FunctionsFieldsAndProperties2_StronglyTypedMetadata()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();

            var exports = container.GetExports<Func<int, int, int>, ITrans_ExportableTest>("Add");

            foreach (var export in exports)
            {
                if (export.Metadata.Var1 == "add")
                {
                    Assert.Equal(3, export.Value(1, 2));
                }
                else if (export.Metadata.Var1 == "sub")
                {
                    Assert.Equal(-1, export.Value(1, 2));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void InAdditionToCatalogTest()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();

            IDictionary<string, object> multMetadata = new Dictionary<string, object>();
            multMetadata["Var1"] = "mult";
            multMetadata[CompositionConstants.ExportTypeIdentityMetadataName] = AttributedModelServices.GetTypeIdentity(typeof(Func<int, int, int>));
            var basicValue = ExportFactory.Create("Add", multMetadata, (() => (Func<int, int, int>)delegate (int a, int b)
            { return a * b; }));

            CompositionBatch batch = new CompositionBatch();
            batch.AddExport(basicValue);
            container.Compose(batch);

            var exports = container.GetExports<Func<int, int, int>, ITrans_ExportableTest>("Add");

            Assert.Equal(3, exports.Count());

            foreach (var export in exports)
            {
                if (export.Metadata.Var1 == "mult")
                {
                    Assert.Equal(2, export.Value(1, 2));
                }
                else if (export.Metadata.Var1 == "add")
                {
                    Assert.Equal(3, export.Value(1, 2));
                }
                else if (export.Metadata.Var1 == "sub")
                {
                    Assert.Equal(-1, export.Value(1, 2));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        [Fact]
        [ActiveIssue(25498)]
        public void CollectionMetadataPropertyTest()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();
            var export = container.GetExport<ComponentWithCollectionProperty, ITrans_CollectionOfStrings>();

            Assert.NotNull(export.Metadata);
            Assert.NotNull(export.Metadata.Values);
            Assert.Equal(export.Metadata.Values.Count(), 3);
            Assert.Equal(export.Metadata.Values.First(), "One");
            Assert.Equal(export.Metadata.Values.Skip(1).First(), "two");
            Assert.Equal(export.Metadata.Values.Skip(2).First(), "3");
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ImportExportSansNameTest()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();

            UnnamedImportAndExport unnamed = container.GetExportedValue<UnnamedImportAndExport>();
            Assert.NotNull(unnamed);
            Assert.NotNull(unnamed.ImportedValue);
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void MultipleInstantiationOfStaticCatalogItem()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();
            var unnamedVI = container.GetExport<StaticExport, object>();

            StaticExport first = unnamedVI.Value;
            StaticExport second = unnamedVI.Value;

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.True(object.ReferenceEquals(first, second), "Instances should be the same");

            var exports = container.GetExports<StaticExport, object>();

            Assert.Equal(1, exports.Count());
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void MultipleInstantiationOfNonStaticCatalogItem()
        {
            var container = ContainerFactory.CreateWithDefaultAttributedCatalog();
            var export1 = container.GetExport<NonStaticExport, object>();
            var export2 = container.GetExport<NonStaticExport, object>();

            NonStaticExport first = export1.Value;
            NonStaticExport second = export2.Value;

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.False(object.ReferenceEquals(first, second), "Instances should be different");
        }

        [Fact]
        [ActiveIssue(25498)]
        public void ImportIntoUntypedExportTest()
        {
            var container = ContainerFactory.Create();

            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue("untyped", 42);

            var u = new UntypedExportImporter();
            var rb = AttributedModelServices.CreatePart(u);

            batch.AddPart(rb);
            container.Compose(batch);

            Assert.Equal(42, u.Export.Value);

            var us = new UntypedExportsImporter();
            batch = new CompositionBatch();
            batch.AddExportedValue("untyped", 19);
            batch.RemovePart(rb);
            batch.AddPart(us);
            container.Compose(batch);

            Assert.NotNull(us.Exports);
            Assert.Equal(2, us.Exports.Count());
        }

        [Fact]
        public void ImportIntoDerivationOfExportException()
        {
            var container = ContainerFactory.Create();

            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue("derived", typeof(DerivedExport), 42);
            var d = new DerivedExportImporter();
            batch.AddPart(d);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotSetImport,
                                          ErrorId.ReflectionModel_ImportNotAssignableFromExport, RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void ImportIntoDerivationOfExportsException()
        {
            var container = ContainerFactory.Create();

            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue("derived", typeof(DerivedExport), 42);
            var d = new DerivedExportsImporter();
            batch.AddPart(d);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotSetImport,
                                          ErrorId.ReflectionModel_ImportNotAssignableFromExport, RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }
    }
}
