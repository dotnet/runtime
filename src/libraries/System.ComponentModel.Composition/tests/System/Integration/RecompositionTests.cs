// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Xunit;

namespace Tests.Integration
{
    public class RecompositionTests
    {
        public class Class_OptIn_AllowRecompositionImports
        {
            [Import("Value", AllowRecomposition = true)]
            public int Value { get; set; }
        }

        [Fact]
        public void Import_OptIn_AllowRecomposition()
        {
            var container = new CompositionContainer();
            var importer = new Class_OptIn_AllowRecompositionImports();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            var valueKey = batch.AddExportedValue("Value", 21);
            container.Compose(batch);

            // Initial compose Value should be 21
            Assert.Equal(21, importer.Value);

            // Recompose Value to be 42
            batch = new CompositionBatch();
            batch.RemovePart(valueKey);
            batch.AddExportedValue("Value", 42);
            container.Compose(batch);

            Assert.Equal(42, importer.Value);
        }

        public class Class_OptOut_AllowRecompositionImports
        {
            [Import("Value", AllowRecomposition = false)]
            public int Value { get; set; }
        }

        [Fact]
        public void Import_OptOut_AllowRecomposition()
        {
            var container = new CompositionContainer();
            var importer = new Class_OptOut_AllowRecompositionImports();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            var valueKey = batch.AddExportedValue("Value", 21);
            container.Compose(batch);

            // Initial compose Value should be 21
            Assert.Equal(21, importer.Value);

            // Reset value to ensure it doesn't get set to same value again
            importer.Value = -21;

            // Recompose Value to be 42
            batch = new CompositionBatch();
            batch.RemovePart(valueKey);
            batch.AddExportedValue("Value", 42);
            // After rejection batch failures throw ChangeRejectedException to indicate that
            // the failure did not affect the container
            Assert.Throws<ChangeRejectedException>(() =>
            {
                container.Compose(batch);
            });

            Assert.Equal(-21, importer.Value);
        }

        public class Class_Default_AllowRecompositionImports
        {
            [Import("Value")]
            public int Value { get; set; }
        }

        [Fact]
        public void Import_Default_AllowRecomposition()
        {
            var container = new CompositionContainer();
            var importer = new Class_Default_AllowRecompositionImports();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            var valueKey = batch.AddExportedValue("Value", 21);
            container.Compose(batch);

            // Initial compose Value should be 21
            Assert.Equal(21, importer.Value);

            // Reset value to ensure it doesn't get set to same value again
            importer.Value = -21;

            // Recompose Value to be 42
            batch = new CompositionBatch();
            batch.RemovePart(valueKey);
            batch.AddExportedValue("Value", 42);
            // After rejection batch failures throw ChangeRejectedException to indicate that
            // the failure did not affect the container
            Assert.Throws<ChangeRejectedException>(() =>
            {
                container.Compose(batch);
            });

            Assert.Equal(-21, importer.Value);
        }

        public class Class_BothOptInAndOptOutRecompositionImports
        {
            [Import("Value", AllowRecomposition = true)]
            public int RecomposableValue { get; set; }

            [Import("Value", AllowRecomposition = false)]
            public int NonRecomposableValue { get; set; }
        }

        [Fact]
        public void Import_BothOptInAndOptOutRecomposition()
        {
            var container = new CompositionContainer();
            var importer = new Class_BothOptInAndOptOutRecompositionImports();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            var valueKey = batch.AddExportedValue("Value", 21);
            container.Compose(batch);

            // Initial compose values should be 21
            Assert.Equal(21, importer.RecomposableValue);
            Assert.Equal(21, importer.NonRecomposableValue);

            // Reset value to ensure it doesn't get set to same value again
            importer.NonRecomposableValue = -21;
            importer.RecomposableValue = -21;

            // Recompose Value to be 42
            batch = new CompositionBatch();
            batch.RemovePart(valueKey);
            batch.AddExportedValue("Value", 42);
            // After rejection batch failures throw ChangeRejectedException to indicate that
            // the failure did not affect the container
            Assert.Throws<ChangeRejectedException>(() =>
            {
                container.Compose(batch);
            });

            Assert.Equal(-21, importer.NonRecomposableValue);
            // The batch rejection means that the recomposable value shouldn't change either
            Assert.Equal(-21, importer.RecomposableValue);
        }

        public class Class_MultipleOptInRecompositionImportsWithDifferentContracts
        {
            [Import("Value1", AllowRecomposition = true)]
            public int Value1 { get; set; }

            [Import("Value2", AllowRecomposition = true)]
            public int Value2 { get; set; }
        }

        [Fact]
        public void Import_OptInRecomposition_Multlple()
        {
            var container = new CompositionContainer();
            var importer = new Class_MultipleOptInRecompositionImportsWithDifferentContracts();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            var value1Key = batch.AddExportedValue("Value1", 21);
            var value2Key = batch.AddExportedValue("Value2", 23);
            container.Compose(batch);

            Assert.Equal(21, importer.Value1);
            Assert.Equal(23, importer.Value2);

            // Reset value to ensure it doesn't get set to same value again
            importer.Value1 = -21;
            importer.Value2 = -23;

            // Recompose Value to be 42
            batch = new CompositionBatch();
            batch.RemovePart(value1Key);
            batch.AddExportedValue("Value1", 42);
            container.Compose(batch);

            Assert.Equal(42, importer.Value1);
            Assert.Equal(-23, importer.Value2);
        }

        [PartNotDiscoverable]
        public class MyName
        {
            public MyName(string name)
            {
                this.Name = name;
            }

            [Export("Name")]
            public string Name { get; private set; }
        }

        [PartNotDiscoverable]
        public class Spouse
        {
            public Spouse(string name)
            {
                this.Name = name;
            }

            [Export("Friend")]
            [ExportMetadata("Relationship", "Wife")]
            public string Name { get; private set; }
        }

        [PartNotDiscoverable]
        public class Child
        {
            public Child(string name)
            {
                this.Name = name;
            }

            [Export("Child")]
            public string Name { get; private set; }
        }

        [PartNotDiscoverable]
        public class Job
        {
            public Job(string name)
            {
                this.Name = name;
            }

            [Export("Job")]
            public string Name { get; private set; }
        }

        [PartNotDiscoverable]
        public class Friend
        {
            public Friend(string name)
            {
                this.Name = name;
            }

            [Export("Friend")]
            public string Name { get; private set; }
        }

        public interface IRelationshipView
        {
            string Relationship { get; }
        }

        [PartNotDiscoverable]
        public class Me
        {
            [Import("Name", AllowRecomposition = true)]
            public string Name { get; set; }

            [Import("Job", AllowDefault = true, AllowRecomposition = true)]
            public string Job { get; set; }

            [ImportMany("Child")]
            public string[] Children { get; set; }

            [ImportMany("Friend")]
            public Lazy<string, IRelationshipView>[] Relatives { get; set; }

            [ImportMany("Friend", AllowRecomposition = true)]
            public string[] Friends { get; set; }
        }

        [Fact]
        public void Recomposition_IntegrationTest()
        {
            var container = new CompositionContainer();
            var batch = new CompositionBatch();

            var me = new Me();
            batch.AddPart(me);
            var namePart = batch.AddPart(new MyName("Blake"));
            batch.AddPart(new Spouse("Barbara"));
            batch.AddPart(new Friend("Steve"));
            batch.AddPart(new Friend("Joyce"));
            container.Compose(batch);
            Assert.Equal(me.Name, "Blake");
            Assert.Equal(me.Job, null);
            Assert.Equal(me.Friends.Length, 3);
            Assert.Equal(me.Relatives.Length, 1);
            Assert.Equal(me.Children.Length, 0);

            // Can only have one name
            Assert.Throws<ChangeRejectedException>(() =>
                container.ComposeParts(new MyName("Blayke")));

            batch = new CompositionBatch();
            batch.AddPart(new MyName("Blayke"));
            batch.RemovePart(namePart);
            container.Compose(batch);
            Assert.Equal(me.Name, "Blayke");

            batch = new CompositionBatch();
            var jobPart = batch.AddPart(new Job("Architect"));
            container.Compose(batch);
            Assert.Equal(me.Job, "Architect");

            batch = new CompositionBatch();
            batch.AddPart(new Job("Chimney Sweep"));
            container.Compose(batch);
            Assert.True(me.Job == null, "More than one of an optional import should result in the default value");

            batch = new CompositionBatch();
            batch.RemovePart(jobPart);
            container.Compose(batch);
            Assert.Equal(me.Job, "Chimney Sweep");

            batch = new CompositionBatch();

            // Can only have one spouse because they aren't recomposable
            Assert.Throws<ChangeRejectedException>(() =>
                container.ComposeParts(new Spouse("Cameron")));

            Assert.Equal(me.Relatives.Length, 1);

            batch = new CompositionBatch();
            batch.AddPart(new Friend("Graham"));
            container.Compose(batch);
            Assert.Equal(me.Friends.Length, 4);
            Assert.Equal(me.Relatives.Length, 1);
        }

        public class FooWithOptionalImport
        {
            private FooWithSimpleImport _optionalImport;

            [Import(AllowDefault = true, AllowRecomposition = true)]
            public FooWithSimpleImport OptionalImport
            {
                get
                {
                    return this._optionalImport;
                }
                set
                {
                    if (value != null)
                    {
                        this._optionalImport = value;

                        Assert.True(!string.IsNullOrEmpty(this._optionalImport.SimpleValue), "Value should have it's imports satisfied");
                    }
                }
            }
        }

        [Export]
        public class FooWithSimpleImport
        {
            [Import("FooSimpleImport")]
            public string SimpleValue { get; set; }
        }

        [Fact]
        public void PartsShouldHaveImportsSatisfiedBeforeBeingUsedToSatisfyRecomposableImports()
        {
            var container = new CompositionContainer();
            var fooOptional = new FooWithOptionalImport();

            container.ComposeParts(fooOptional);
            container.ComposeExportedValue<string>("FooSimpleImport", "NotNullOrEmpty");
            container.ComposeParts(new FooWithSimpleImport());

            Assert.True(!string.IsNullOrEmpty(fooOptional.OptionalImport.SimpleValue));
        }

        [Export]
        public class RootImportRecomposable
        {
            [Import(AllowDefault = true, AllowRecomposition = true)]
            public NonSharedImporter Importer { get; set; }
        }

        [Export]
        [PartCreationPolicy(CreationPolicy.NonShared)]
        public class NonSharedImporter
        {
            [Import]
            public SimpleImport Import { get; set; }
        }

        [Export]
        public class RootImporter
        {
            [Import]
            public SimpleImport Import { get; set; }
        }

        [Export]
        public class SimpleImport
        {
            public int Property { get { return 42; } }
        }

        [Fact]
        [ActiveIssue(733533)]
        public void RemoveCatalogWithNonSharedPartWithRequiredImport()
        {
            var typeCatalog = new TypeCatalog(typeof(NonSharedImporter), typeof(SimpleImport));
            var aggCatalog = new AggregateCatalog();
            var container = new CompositionContainer(aggCatalog);

            aggCatalog.Catalogs.Add(typeCatalog);
            aggCatalog.Catalogs.Add(new TypeCatalog(typeof(RootImportRecomposable)));

            var rootExport = container.GetExport<RootImportRecomposable>();
            var root = rootExport.Value;

            Assert.Equal(42, root.Importer.Import.Property);

            aggCatalog.Catalogs.Remove(typeCatalog);

            Assert.Null(root.Importer);
        }

        [Fact]
        [ActiveIssue(734123)]
        public void GetExportResultShouldBePromise()
        {
            var typeCatalog = new TypeCatalog(typeof(RootImporter), typeof(SimpleImport));
            var aggCatalog = new AggregateCatalog();
            var container = new CompositionContainer(aggCatalog);

            aggCatalog.Catalogs.Add(typeCatalog);

            var root = container.GetExport<RootImporter>();

            Assert.Throws<ChangeRejectedException>(() =>
                aggCatalog.Catalogs.Remove(typeCatalog)
            );

            var value = root.Value;
            Assert.Equal(42, value.Import.Property);
        }

        [Fact]
        // [WorkItem(789269)]
        public void TestRemovingAndReAddingMultipleDefinitionsFromCatalog()
        {
            var fixedParts = new TypeCatalog(typeof(RootMultipleImporter), typeof(ExportedService));
            var changingParts = new TypeCatalog(typeof(Exporter1), typeof(Exporter2));
            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(fixedParts);
            catalog.Catalogs.Add(changingParts);

            var container = new CompositionContainer(catalog);

            var root = container.GetExport<RootMultipleImporter>().Value;

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

        [Fact]
        [ActiveIssue(762215)]
        public void TestPartCreatorResurrection()
        {
            var container = new CompositionContainer(new TypeCatalog(typeof(NonDisposableImportsDisposable), typeof(PartImporter<NonDisposableImportsDisposable>)));
            var exports = container.GetExports<PartImporter<NonDisposableImportsDisposable>>();
            Assert.Equal(0, exports.Count());
            container.ComposeParts(new DisposablePart());
            exports = container.GetExports<PartImporter<NonDisposableImportsDisposable>>();
            Assert.Equal(1, exports.Count());
        }

        [Export]
        public class PartImporter<PartType>
        {
            [Import]
            public PartType Creator { get; set; }
        }

        [Export]
        public class NonDisposableImportsDisposable
        {
            [Import]
            public DisposablePart Part { get; set; }
        }

        [Export]
        public class Part
        {

        }

        [Export]
        [PartCreationPolicy(CreationPolicy.NonShared)]
        public class DisposablePart : Part, IDisposable
        {
            public bool Disposed { get; private set; }
            public void Dispose()
            {
                Disposed = true;
            }
        }

    }
}
