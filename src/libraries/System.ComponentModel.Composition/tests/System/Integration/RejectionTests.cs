// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.UnitTesting;
using Xunit;

namespace Tests.Integration
{
    public class RejectionTests
    {
        public interface IExtension
        {
            int Id { get; set; }
        }

        [Export]
        public class MyImporter
        {
            [ImportMany(AllowRecomposition = true)]
            public IExtension[] Extensions { get; set; }
        }

        [Export(typeof(IExtension))]
        public class Extension1 : IExtension
        {
            [Import("IExtension.IdValue")]
            public int Id { get; set; }
        }

        [Export(typeof(IExtension))]
        public class Extension2 : IExtension
        {
            [Import("IExtension.IdValue2")]
            public int Id { get; set; }
        }

        [Fact]
        public void Rejection_ExtensionLightUp_AddedViaBatch()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(MyImporter),
                typeof(Extension1),
                typeof(Extension2));

            var importer = container.GetExportedValue<MyImporter>();

            Assert.Equal(0, importer.Extensions.Length);

            container.ComposeExportedValue<int>("IExtension.IdValue", 10);

            Assert.Equal(1, importer.Extensions.Length);
            Assert.Equal(10, importer.Extensions[0].Id);

            container.ComposeExportedValue<int>("IExtension.IdValue2", 20);

            Assert.Equal(2, importer.Extensions.Length);
            Assert.Equal(10, importer.Extensions[0].Id);
            Assert.Equal(20, importer.Extensions[1].Id);
        }

        public class ExtensionValues
        {
            [Export("IExtension.IdValue")]
            public int Value = 10;

            [Export("IExtension.IdValue2")]
            public int Value2 = 20;
        }

        [Fact]
        public void Rejection_ExtensionLightUp_AddedViaCatalog()
        {
            var ext1Cat = CatalogFactory.CreateAttributed(typeof(Extension1));
            var ext2Cat = CatalogFactory.CreateAttributed(typeof(Extension2));
            var hostCat = CatalogFactory.CreateAttributed(typeof(MyImporter));
            var valueCat = CatalogFactory.CreateAttributed(typeof(ExtensionValues));

            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(hostCat);

            var container = ContainerFactory.Create(catalog);

            var importer = container.GetExportedValue<MyImporter>();

            Assert.Equal(0, importer.Extensions.Length);

            catalog.Catalogs.Add(ext1Cat);

            Assert.Equal(0, importer.Extensions.Length);

            catalog.Catalogs.Add(ext2Cat);

            Assert.Equal(0, importer.Extensions.Length);

            catalog.Catalogs.Add(valueCat);

            Assert.Equal(2, importer.Extensions.Length);
            Assert.Equal(10, importer.Extensions[0].Id);
            Assert.Equal(20, importer.Extensions[1].Id);
        }

        public interface IMissing { }
        public interface ISingle { }
        public interface IMultiple { }
        public interface IConditional { }
        public class SingleImpl : ISingle { }
        public class MultipleImpl : IMultiple { }

        public class NoImportPart
        {
            public NoImportPart()
            {
                SingleExport = new SingleImpl();
                MultipleExport1 = new MultipleImpl();
                MultipleExport2 = new MultipleImpl();
            }

            [Export]
            public ISingle SingleExport { private set; get; }

            [Export]
            public IMultiple MultipleExport1 { private set; get; }

            [Export]
            public IMultiple MultipleExport2 { private set; get; }
        }

        [Export]
        public class Needy
        {
            public Needy() { }

            [Import]
            public ISingle SingleImport { get; set; }
        }

        [Fact]
        public void Rejection_Resurrection()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(Needy));

            var exports1 = container.GetExportedValues<Needy>();

            Assert.Equal(0, exports1.Count());

            container.ComposeParts(new NoImportPart());

            var exports2 = container.GetExportedValues<Needy>();
            Assert.Equal(1, exports2.Count());
        }

        [Fact]
        public void Rejection_BatchSatisfiesBatch()
        {
            var container = ContainerFactory.Create();
            var needy = new Needy();
            container.ComposeParts(needy, new NoImportPart());
            Assert.IsType<SingleImpl>(needy.SingleImport);
        }

        [Fact]
        public void Rejection_BatchSatisfiesBatchReversed()
        {
            var container = ContainerFactory.Create();
            var needy = new Needy();
            container.ComposeParts(new NoImportPart(), needy);
            Assert.IsType<SingleImpl>(needy.SingleImport);
        }

        [Fact]
        public void Rejection_CatalogSatisfiesBatch()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(NoImportPart));
            var needy = new Needy();
            container.ComposeParts(needy);
            Assert.IsType<SingleImpl>(needy.SingleImport);
        }

        [Fact]
        public void Rejection_TransitiveDependenciesSatisfied()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(Needy), typeof(NoImportPart));
            var needy = container.GetExportedValue<Needy>();
            Assert.NotNull(needy);
            Assert.IsType<SingleImpl>(needy.SingleImport);
        }

        [Fact]
        public void Rejection_TransitiveDependenciesUnsatisfied_ShouldThrowCardinalityMismatch()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(Needy), typeof(MissingImportPart));

            ExceptionAssert.Throws<ImportCardinalityMismatchException>(() =>
                container.GetExportedValue<Needy>());
        }

        public class MissingImportPart : NoImportPart
        {
            [Import]
            public IMissing MissingImport { set; get; }
        }

        [Fact]
        public void Rejection_BatchRevert()
        {
            var container = ContainerFactory.Create();

            ExceptionAssert.Throws<ChangeRejectedException>(() =>
                container.ComposeParts(new MissingImportPart()));
        }

        [Fact]
        public void Rejection_DefendPromisesOnceMade()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(Needy));

            var addBatch = new CompositionBatch();
            var removeBatch = new CompositionBatch();
            var addedPart = addBatch.AddPart(new NoImportPart());
            removeBatch.RemovePart(addedPart);

            // Add then remove should be fine as long as exports aren't used yet.
            container.Compose(addBatch);
            container.Compose(removeBatch);

            // Add the dependencies
            container.Compose(addBatch);

            // Retrieve needy which uses an export from addedPart
            var export = container.GetExportedValue<Needy>();

            // Should not be able to remove the addedPart because someone depends on it.
            ExceptionAssert.Throws<ChangeRejectedException>(() =>
                container.Compose(removeBatch));
        }

        [Fact]
        public void Rejection_DefendPromisesLazily()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(Needy));

            // Add the missing dependency for Needy
            container.ComposeParts(new NoImportPart());

            // This change should succeed since the component "Needy" hasn't been fully composed
            // and one way of satisfying its needs is as good ask another
            var export = container.GetExport<Needy>();

            // Cannot add another import because it would break existing promised compositions
            ExceptionAssert.Throws<ChangeRejectedException>(() =>
                container.ComposeParts(new NoImportPart()));

            // Instansitate the object
            var needy = export.Value;

            // Cannot add another import because it would break existing compositions
            ExceptionAssert.Throws<ChangeRejectedException>(() =>
                container.ComposeParts(new NoImportPart()));
        }

        [Fact]
        public void Rejection_SwitchPromiseFromManualToCatalog()
        {
            // This test shows how the priority list in the AggregateCatalog can actually play with
            // the rejection work. Until the actual object is actually pulled on and satisfied the
            // promise can be moved around even for not-recomposable imports but once the object is
            // pulled on it is fixed from that point on.

            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(Needy), typeof(NoImportPart));

            // Add the missing dependency for Needy
            container.ComposeParts(new NoImportPart());

            // This change should succeed since the component "Needy" hasn't been fully composed
            // and one way of satisfying its needs is as good as another
            var export = container.GetExport<Needy>();

            // Adding more exports doesn't fail because we push the promise to use the NoImportPart from the catalog
            // using the priorities from the AggregateExportProvider
            container.ComposeParts(new NoImportPart());

            // Instansitate the object
            var needy = export.Value;

            // Cannot add another import because it would break existing compositions
            ExceptionAssert.Throws<ChangeRejectedException>(() =>
                container.ComposeParts(new NoImportPart()));
        }

        public interface ILoopA { }
        public interface ILoopB { }

        [Export(typeof(ILoopA))]
        public class LoopA1 : ILoopA
        {
            [Import]
            public ILoopB LoopB { set; get; }
        }

        [Export(typeof(ILoopA))]
        public class LoopA2 : ILoopA
        {
            [Import]
            public ILoopB LoopB { set; get; }
        }

        [Export(typeof(ILoopB))]
        public class LoopB1 : ILoopB
        {
            [Import]
            public ILoopA LoopA { set; get; }
        }

        [Export(typeof(ILoopB))]
        public class LoopB2 : ILoopB
        {
            [Import]
            public ILoopA LoopA { set; get; }
        }

        // This is an interesting situation.  There are several possible self-consistent outcomes:
        // - All parts involved in the loop are rejected
        // - A consistent subset are not rejected (exactly one of LoopA1/LoopA2 and one of LoopB1/LoopB2
        //
        // Both have desirable and undesirable characteristics.  The first case is non-discriminatory but
        // rejects more parts than are necessary, the second minimizes rejection but must choose a subset
        // on somewhat arbitrary grounds.
        [Fact]
        public void Rejection_TheClemensLoop()
        {
            var catalog = new TypeCatalog(new Type[] { typeof(LoopA1), typeof(LoopA2), typeof(LoopB1), typeof(LoopB2) });
            var container = new CompositionContainer(catalog);
            var exportsA = container.GetExportedValues<ILoopA>();
            var exportsB = container.GetExportedValues<ILoopB>();

            // These assertions would prove solution one
            Assert.Equal(0, exportsA.Count());
            Assert.Equal(0, exportsB.Count());

            // These assertions would prove solution two
            //Assert.Equal(1, exportsA.Count);
            //Assert.Equal(1, exportsB.Count);
        }

        public interface IWorkItem
        {
            string Id { get; set; }
        }

        [Export]
        public class AllWorkItems
        {
            [ImportMany(AllowRecomposition = true)]
            public Lazy<IWorkItem>[] WorkItems { get; set; }
        }

        [Export(typeof(IWorkItem))]
        public class WorkItem : IWorkItem
        {
            [Import("WorkItem.Id", AllowRecomposition = true)]
            public string Id { get; set; }
        }

        public class Ids
        {
            [Export("WorkItem.Id")]
            public string Id = "MyId";

        }

        [Fact]
        public void AppliedStateNotCompleteedYet()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(AllWorkItems));

            container.ComposeExportedValue<string>("WorkItem.Id", "A");

            var workItems = container.GetExportedValue<AllWorkItems>();

            Assert.Equal(0, workItems.WorkItems.Length);

            container.ComposeParts(new WorkItem());

            Assert.Equal(1, workItems.WorkItems.Length);
            Assert.Equal("A", workItems.WorkItems[0].Value.Id);
        }

        [Export]
        public class ClassWithMissingImport
        {
            [Import]
            private string _importNotFound = null;

            public ClassWithMissingImport()
            {
                if (_importNotFound != null)
                    throw new ArgumentException();
            }
        }

        [Fact]
        public void AppliedStateStored_ShouldRevertStateOnFailure()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(AllWorkItems), typeof(WorkItem), typeof(Ids));

            var workItems = container.GetExportedValue<AllWorkItems>();

            Assert.Equal(1, workItems.WorkItems.Length);

            var batch = new CompositionBatch();

            batch.AddExportedValue("WorkItem.Id", "B");
            batch.AddPart(new ClassWithMissingImport());

            ExceptionAssert.Throws<ChangeRejectedException>(() =>
                container.Compose(batch));

            Assert.Equal("MyId", workItems.WorkItems[0].Value.Id);
        }

        [Export]
        public class OptionalImporter
        {
            [Import(AllowDefault = true)]
            public ClassWithMissingImport Import { get; set; }
        }

        [Fact]
        public void OptionalImportWithMissingDependency_ShouldRejectAndComposeFine()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(OptionalImporter), typeof(ClassWithMissingImport));

            var importer = container.GetExportedValue<OptionalImporter>();

            Assert.Null(importer.Import);
        }

        [Export]
        public class PartA
        {
            [Import(AllowDefault = true, AllowRecomposition = true)]
            public PartB ImportB { get; set; }
        }

        [Export]
        public class PartB
        {
            [Import]
            public PartC ImportC { get; set; }
        }

        [Export]
        public class PartC
        {
            [Import]
            public PartB ImportB { get; set; }
        }

        [Export]
        public class PartA2
        {
            [Import(AllowDefault = true, AllowRecomposition = true)]
            public PartB ImportB { get; set; }

            [Import(AllowDefault = true, AllowRecomposition = true)]
            public PartC ImportC { get; set; }
        }
    }
}
