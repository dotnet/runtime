// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.UnitTesting;
using Xunit;

namespace Tests.Integration
{
    public class LifetimeTests
    {
        [Export]
        public class AnyPartSimple
        {

        }

        [Export]
        public class AnyPartDisposable : IDisposable
        {
            public bool IsDisposed { get; set; }
            public void Dispose()
            {
                Assert.False(IsDisposed);
                IsDisposed = true;
            }
        }

        [Export]
        public class AnyPartRecomposable
        {
            [Import("Value", AllowRecomposition = true)]
            public int Value { get; set; }
        }

        [Export]
        public class AnyPartDisposableRecomposable : IDisposable
        {
            [Import("Value", AllowRecomposition = true)]
            public int Value { get; set; }

            public bool IsDisposed { get; set; }
            public void Dispose()
            {
                Assert.False(IsDisposed);
                IsDisposed = true;
            }
        }

        [Fact]
        public void PartAddedViaAddExportedValue_ShouldNotBeDisposedWithContainer()
        {
            var container = new CompositionContainer();
            var disposablePart = new AnyPartDisposable();
            var batch = new CompositionBatch();
            batch.AddPart(batch);
            container.Compose(batch);

            container.Dispose();
            Assert.False(disposablePart.IsDisposed);
        }

        [Fact]
        public void PartAddedTwice_AppearsTwice()
        {
            //  You probably shouldn't be adding a part to the container twice, but it's not something we're going to check for and throw an exception on
            var container = new CompositionContainer();
            var disposable = new AnyPartDisposable();
            var part = AttributedModelServices.CreatePart(disposable);
            var batch = new CompositionBatch();
            batch.AddPart(part);
            container.Compose(batch);

            batch = new CompositionBatch();
            batch.AddPart(part);
            container.Compose(batch);

            var exports = container.GetExports<AnyPartDisposable>();
            Assert.Equal(2, exports.Count());

            container.Dispose();
        }

        [Fact]
        public void AnyPart_Simple_ShouldNotBeCollected()
        {
            var catalog = new TypeCatalog(typeof(AnyPartSimple));
            var container = new CompositionContainer(catalog);

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesNotExpectedToBeCollected(
                container.GetExportedValue<AnyPartSimple>());

            refTracker.CollectAndAssert();

            GC.KeepAlive(container);
        }

        [Fact]
        public void AnyPart_Disposable_ShouldNotBeCollected()
        {
            var catalog = new TypeCatalog(typeof(AnyPartDisposable));
            var container = new CompositionContainer(catalog);

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesNotExpectedToBeCollected(
                container.GetExportedValue<AnyPartDisposable>());

            GC.KeepAlive(container);
        }

        [Fact]
        public void AnyPart_Disposable_ShouldBeDisposedWithContainer()
        {
            var catalog = new TypeCatalog(typeof(AnyPartDisposable));
            var container = new CompositionContainer(catalog);

            var exportedValue = container.GetExportedValue<AnyPartDisposable>();

            Assert.False(exportedValue.IsDisposed);

            container.Dispose();

            Assert.True(exportedValue.IsDisposed, "AnyPart should be disposed with the container!");
        }

        [Fact]
        public void AnyPart_RecomposabeImport_ShouldNotBeCollected()
        {
            var catalog = new TypeCatalog(typeof(AnyPartRecomposable));
            var container = new CompositionContainer(catalog);

            // Setup dependency
            CompositionBatch batch = new CompositionBatch();
            var valueKey = batch.AddExportedValue("Value", 21);
            container.Compose(batch);
            batch = null;

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesNotExpectedToBeCollected(
                container.GetExportedValue<AnyPartRecomposable>());
            refTracker.CollectAndAssert();

            // Lets make sure recomposition doesn't blow anything up here.
            batch = new CompositionBatch();
            batch.RemovePart(valueKey);
            batch.AddExportedValue("Value", 42);
            container.Compose(batch);
            batch = null;

            var exportedValue = (AnyPartRecomposable)refTracker.ReferencesNotExpectedToBeCollected[0].Target;
            Assert.Equal(42, exportedValue.Value);

            GC.KeepAlive(container);
        }

        [Fact]
        public void AnyPart_DisposableRecomposabeImport_ShouldNotBeCollected()
        {
            var catalog = new TypeCatalog(typeof(AnyPartDisposableRecomposable));
            var container = new CompositionContainer(catalog);

            // Setup dependency
            CompositionBatch batch = new CompositionBatch();
            var valueKey = batch.AddExportedValue("Value", 21);
            container.Compose(batch);
            batch = null;

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesNotExpectedToBeCollected(
                container.GetExportedValue<AnyPartDisposableRecomposable>());

            refTracker.CollectAndAssert();

            // Lets make sure recomposition doesn't blow anything up here.
            batch = new CompositionBatch();
            batch.RemovePart(valueKey);
            batch.AddExportedValue("Value", 42);
            container.Compose(batch);
            batch = null;

            var exportedValue = (AnyPartDisposableRecomposable)refTracker.ReferencesNotExpectedToBeCollected[0].Target;
            Assert.Equal(42, exportedValue.Value);

            GC.KeepAlive(container);

            container.Dispose();

            Assert.True(exportedValue.IsDisposed, "Any parts should be disposed with the container!");
        }

        [Export]
        [PartCreationPolicy(CreationPolicy.Shared)]
        public class SharedPartSimple
        {

        }

        [Export]
        [PartCreationPolicy(CreationPolicy.Shared)]
        public class SharedPartDisposable : IDisposable
        {
            public bool IsDisposed { get; set; }
            public void Dispose()
            {
                Assert.False(IsDisposed);
                IsDisposed = true;
            }
        }

        [Export]
        [PartCreationPolicy(CreationPolicy.Shared)]
        public class SharedPartRecomposable
        {
            [Import("Value", AllowRecomposition = true)]
            public int Value { get; set; }
        }

        [Export]
        [PartCreationPolicy(CreationPolicy.Shared)]
        public class SharedPartDisposableRecomposable : IDisposable
        {
            [Import("Value", AllowRecomposition = true)]
            public int Value { get; set; }

            public bool IsDisposed { get; set; }
            public void Dispose()
            {
                Assert.False(IsDisposed);
                IsDisposed = true;
            }
        }

        [Fact]
        public void SharedPart_Simple_ShouldNotBeCollected()
        {
            var catalog = new TypeCatalog(typeof(SharedPartSimple));
            var container = new CompositionContainer(catalog);

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesNotExpectedToBeCollected(
                container.GetExportedValue<SharedPartSimple>());

            refTracker.CollectAndAssert();

            GC.KeepAlive(container);
        }

        [Fact]
        public void SharedPart_Disposable_ShouldNotBeCollected()
        {
            var catalog = new TypeCatalog(typeof(SharedPartDisposable));
            var container = new CompositionContainer(catalog);

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesNotExpectedToBeCollected(
                container.GetExportedValue<SharedPartDisposable>());

            refTracker.CollectAndAssert();

            GC.KeepAlive(container);
        }

        [Fact]
        public void SharedPart_Disposable_ShouldBeDisposedWithContainer()
        {
            var catalog = new TypeCatalog(typeof(SharedPartDisposable));
            var container = new CompositionContainer(catalog);

            var export = container.GetExportedValue<SharedPartDisposable>();

            Assert.False(export.IsDisposed);

            container.Dispose();

            Assert.True(export.IsDisposed, "SharedPart should be disposed with the container!");
        }

        [Fact]
        public void SharedPart_RecomposabeImport_ShouldNotBeCollected()
        {
            var catalog = new TypeCatalog(typeof(SharedPartRecomposable));
            var container = new CompositionContainer(catalog);

            // Setup dependency
            CompositionBatch batch = new CompositionBatch();
            var valueKey = batch.AddExportedValue("Value", 21);
            container.Compose(batch);
            batch = null;

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesNotExpectedToBeCollected(
                container.GetExportedValue<SharedPartRecomposable>());

            refTracker.CollectAndAssert();

            // Lets make sure recomposition doesn't blow anything up here.
            batch = new CompositionBatch();
            batch.RemovePart(valueKey);
            batch.AddExportedValue("Value", 42);
            container.Compose(batch);
            batch = null;

            var exportedValue = (SharedPartRecomposable)refTracker.ReferencesNotExpectedToBeCollected[0].Target;
            Assert.Equal(42, exportedValue.Value);

            GC.KeepAlive(container);
        }

        [Fact]
        public void SharedPart_DisposableRecomposabeImport_ShouldNotBeCollected()
        {
            var catalog = new TypeCatalog(typeof(SharedPartDisposableRecomposable));
            var container = new CompositionContainer(catalog);

            // Setup dependency
            CompositionBatch batch = new CompositionBatch();
            var valueKey = batch.AddExportedValue("Value", 21);
            container.Compose(batch);
            batch = null;

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesNotExpectedToBeCollected(
                container.GetExportedValue<SharedPartDisposableRecomposable>());

            refTracker.CollectAndAssert();

            // Lets make sure recomposition doesn't blow anything up here.
            batch = new CompositionBatch();
            batch.RemovePart(valueKey);
            batch.AddExportedValue("Value", 42);
            container.Compose(batch);
            batch = null;

            var exportedValue = (SharedPartDisposableRecomposable)refTracker.ReferencesNotExpectedToBeCollected[0].Target;
            Assert.Equal(42, exportedValue.Value);

            container.Dispose();

            Assert.True(exportedValue.IsDisposed, "Any parts should be disposed with the container!");
        }

        [Export]
        [PartCreationPolicy(CreationPolicy.NonShared)]
        public class NonSharedPartSimple
        {

        }

        [Export]
        [PartCreationPolicy(CreationPolicy.NonShared)]
        public class NonSharedPartRecomposable
        {
            [Import("Value", AllowRecomposition = true)]
            public int Value { get; set; }
        }

        [Export]
        [PartCreationPolicy(CreationPolicy.NonShared)]
        public class NonSharedPartDisposable : IDisposable
        {
            public bool IsDisposed { get; set; }
            public void Dispose()
            {
                Assert.False(IsDisposed);
                IsDisposed = true;
            }
        }

        [Export]
        [PartCreationPolicy(CreationPolicy.NonShared)]
        public class NonSharedPartDisposableRecomposable : IDisposable
        {
            private int _value;

            [Import("Value", AllowRecomposition = true)]
            public int Value
            {
                get
                {
                    if (this.IsDisposed)
                        throw new ObjectDisposedException(this.GetType().Name);
                    return this._value;
                }
                set
                {
                    if (this.IsDisposed)
                        throw new ObjectDisposedException(this.GetType().Name);
                    this._value = value;
                }
            }

            public bool IsDisposed { get; set; }
            public void Dispose()
            {
                Assert.False(IsDisposed);
                IsDisposed = true;
            }
        }

        [Fact]
        public void NonSharedPart_Disposable_ShouldNotBeCollected()
        {
            var catalog = new TypeCatalog(typeof(NonSharedPartDisposable));
            var container = new CompositionContainer(catalog);

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesNotExpectedToBeCollected(
                container.GetExportedValue<NonSharedPartDisposable>());

            refTracker.CollectAndAssert();

            GC.KeepAlive(container);
        }

        [Fact]
        public void NonSharedPart_Disposable_ShouldBeDisposedWithContainer()
        {
            var catalog = new TypeCatalog(typeof(NonSharedPartDisposable));
            var container = new CompositionContainer(catalog);

            var export = container.GetExportedValue<NonSharedPartDisposable>();

            Assert.False(export.IsDisposed);

            container.Dispose();

            Assert.True(export.IsDisposed, "NonSharedParts should be disposed with the container!");
        }

        [Fact]
        public void NonSharedPart_RecomposableImport_WithReference_ShouldNotBeCollected()
        {
            var catalog = new TypeCatalog(typeof(NonSharedPartRecomposable));
            var container = new CompositionContainer(catalog);

            // Setup dependency
            CompositionBatch batch = new CompositionBatch();
            var valueKey = batch.AddExportedValue("Value", 21);
            container.Compose(batch);
            batch = null;

            var exportedValue = container.GetExportedValue<NonSharedPartRecomposable>();

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesNotExpectedToBeCollected(exportedValue);

            refTracker.CollectAndAssert();

            // Recompose should work because we are still holding a reference to the exported value.
            batch = new CompositionBatch();
            batch.RemovePart(valueKey);
            batch.AddExportedValue("Value", 42);
            container.Compose(batch);
            batch = null;

            Assert.Equal(42, exportedValue.Value);

            GC.KeepAlive(container);
        }

        [Fact]
        public void NonSharedPart_DisposableRecomposabeImport_NoReference_ShouldNotBeCollected()
        {
            var catalog = new TypeCatalog(typeof(NonSharedPartDisposableRecomposable));
            var container = new CompositionContainer(catalog);

            // Setup dependency
            CompositionBatch batch = new CompositionBatch();
            var valueKey = batch.AddExportedValue("Value", 21);
            container.Compose(batch);
            batch = null;

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesNotExpectedToBeCollected(
                container.GetExportedValue<NonSharedPartDisposableRecomposable>());

            refTracker.CollectAndAssert();

            // Recompose just to ensure we don't blow up, even though we don't expect anything to happen.
            batch = new CompositionBatch();
            batch.RemovePart(valueKey);
            batch.AddExportedValue("Value", 42);
            container.Compose(batch);
            batch = null;

            var exportedValue = (NonSharedPartDisposableRecomposable)refTracker.ReferencesNotExpectedToBeCollected[0].Target;
            Assert.Equal(42, exportedValue.Value);

            GC.KeepAlive(container);
        }

        [Export]
        public class SharedState
        {
            public static int instanceNumber = 0;
            public SharedState()
            {
                MyInstanceNumber = instanceNumber++;
            }

            public int MyInstanceNumber { get; private set; }
        }

        [PartCreationPolicy(CreationPolicy.NonShared)]
        public class NonSharedState
        {
            [Import(AllowRecomposition = true)]
            public SharedState State { set { ExportState = value; } }

            [Export("SharedFromNonShared")]
            public SharedState ExportState { get; private set; }
        }

        [Fact]
        public void NonSharedPart_TwoRecomposablePartsSameExportedValue()
        {
            // This test is primarily used to ensure that we allow for multiple parts to be associated
            // with the same exported value.
            var catalog = new TypeCatalog(typeof(SharedState), typeof(NonSharedState));
            var container = new CompositionContainer(catalog);

            var export1 = container.GetExportedValue<SharedState>("SharedFromNonShared");
            var export2 = container.GetExportedValue<SharedState>("SharedFromNonShared");

            // Same exported value that comes from two different recomposable part instances.
            Assert.Equal(export1.MyInstanceNumber, export2.MyInstanceNumber);
        }

        [Export]
        [PartCreationPolicy(CreationPolicy.NonShared)]
        public class SharedImporter
        {
            [Import(RequiredCreationPolicy = CreationPolicy.Shared)]
            public AnyPartSimple AnyPartSimple { get; set; }

            [Import(RequiredCreationPolicy = CreationPolicy.Shared)]
            public AnyPartDisposable AnyPartDisposable { get; set; }

            [Import(RequiredCreationPolicy = CreationPolicy.Shared)]
            public AnyPartRecomposable AnyPartRecomposable { get; set; }

            [Import(RequiredCreationPolicy = CreationPolicy.Shared)]
            public AnyPartDisposableRecomposable AnyPartDisposableRecomposable { get; set; }
        }

        [Export]
        [PartCreationPolicy(CreationPolicy.NonShared)]
        public class NonSharedImporter
        {
            [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
            public AnyPartSimple AnyPartSimple { get; set; }

            [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
            public AnyPartDisposable AnyPartDisposable { get; set; }

            [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
            public AnyPartRecomposable AnyPartRecomposable { get; set; }

            [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
            public AnyPartDisposableRecomposable AnyPartDisposableRecomposable { get; set; }
        }

        private static CompositionContainer GetContainer()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(LifetimeTests).GetNestedTypes(BindingFlags.Public));
            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue("Value", 21);
            container.Compose(batch);
            return container;
        }

        [Fact]
        public void GetReleaseExport_SharedRoot_ShouldNotDisposeChain()
        {
            var container = GetContainer();

            var export = container.GetExport<SharedImporter, IDictionary<string, object>>();
            var exportedValue = export.Value;

            container.ReleaseExport(export);

            Assert.False(exportedValue.AnyPartDisposable.IsDisposed);
            Assert.False(exportedValue.AnyPartDisposableRecomposable.IsDisposed);
        }

        [Fact]
        public void AddRemovePart_SharedRoot_ShouldNotDisposeChain()
        {
            var container = GetContainer();

            var exportedValue = new SharedImporter();

            CompositionBatch batch = new CompositionBatch();
            var part = batch.AddPart(exportedValue);
            container.Compose(batch);

            batch = new CompositionBatch();
            batch.RemovePart(part);
            container.Compose(batch);

            Assert.False(exportedValue.AnyPartDisposable.IsDisposed);
            Assert.False(exportedValue.AnyPartDisposableRecomposable.IsDisposed);
        }

        [Fact]
        public void ContainerDispose_SharedRoot_ShouldDisposeChain()
        {
            var container = GetContainer();

            var export = container.GetExport<SharedImporter>();
            var exportedValue = export.Value;

            container.Dispose();

            Assert.True(exportedValue.AnyPartDisposable.IsDisposed);
            Assert.True(exportedValue.AnyPartDisposableRecomposable.IsDisposed);
        }

        [Fact]
        public void GetReleaseExport_NonSharedRoot_ShouldDisposeChain()
        {
            var container = GetContainer();

            var exports = new List<Lazy<NonSharedImporter>>();
            var exportedValues = new List<NonSharedImporter>();

            // Executing this 100 times to help uncover any GC bugs
            for (int i = 0; i < 100; i++)
            {
                var export = container.GetExport<NonSharedImporter>();
                var exportedValue = export.Value;

                exports.Add(export);
                exportedValues.Add(exportedValue);
            }

            for (int i = 0; i < 100; i++)
            {
                var export = exports[i];
                var exportedValue = exportedValues[i];

                container.ReleaseExport(export);

                Assert.True(exportedValue.AnyPartDisposable.IsDisposed);
                Assert.True(exportedValue.AnyPartDisposableRecomposable.IsDisposed);
            }
        }

        public void GetReleaseExport_NonSharedRoot_ShouldDisposeChain_WithMetadata()
        {
            var container = GetContainer();

            var exports = new List<Lazy<NonSharedImporter, IDictionary<string, object>>>();
            var exportedValues = new List<NonSharedImporter>();

            // Executing this 100 times to help uncover any GC bugs
            for (int i = 0; i < 100; i++)
            {
                var export = container.GetExport<NonSharedImporter, IDictionary<string, object>>();
                var exportedValue = export.Value;

                exports.Add(export);
                exportedValues.Add(exportedValue);
            }

            for (int i = 0; i < 100; i++)
            {
                var export = exports[i];
                var exportedValue = exportedValues[i];

                container.ReleaseExport(export);

                Assert.True(exportedValue.AnyPartDisposable.IsDisposed);
                Assert.True(exportedValue.AnyPartDisposableRecomposable.IsDisposed);
            }
        }

        [Fact]
        public void ReleaseExports_ShouldDispose_NonSharedParts()
        {
            var container = GetContainer();

            var export1 = container.GetExport<NonSharedImporter>();
            var exportedValue1 = export1.Value;

            var export2 = container.GetExport<NonSharedImporter>();
            var exportedValue2 = export2.Value;

            container.ReleaseExports(new[] { export1, export2 });

            Assert.True(exportedValue1.AnyPartDisposable.IsDisposed);
            Assert.True(exportedValue1.AnyPartDisposableRecomposable.IsDisposed);

            Assert.True(exportedValue2.AnyPartDisposable.IsDisposed);
            Assert.True(exportedValue2.AnyPartDisposableRecomposable.IsDisposed);
        }

        [Fact]
        public void AddRemovePart_NonSharedRoot_ShouldDisposeChain()
        {
            var container = GetContainer();

            var exportedValue = new NonSharedImporter();

            CompositionBatch batch = new CompositionBatch();
            var part = batch.AddPart(exportedValue);
            container.Compose(batch);

            batch = new CompositionBatch();
            batch.RemovePart(part);
            container.Compose(batch);

            Assert.True(exportedValue.AnyPartDisposable.IsDisposed);
            Assert.True(exportedValue.AnyPartDisposableRecomposable.IsDisposed);
        }

        [Fact]
        public void ContainerDispose_NonSharedRoot_ShouldNotDisposeChain()
        {
            var container = GetContainer();

            var export = container.GetExport<NonSharedImporter>();
            var exportedValue = export.Value;

            container.Dispose();

            Assert.True(exportedValue.AnyPartDisposable.IsDisposed);
            Assert.True(exportedValue.AnyPartDisposableRecomposable.IsDisposed);
        }

        [Fact]
        public void GetReleaseExport_NonSharedPart_ShouldNotRecomposeAfterRelease()
        {
            var catalog = new TypeCatalog(typeof(NonSharedPartRecomposable));
            var container = new CompositionContainer(catalog);

            // Setup dependency
            CompositionBatch batch = new CompositionBatch();
            var valueKey = batch.AddExportedValue("Value", 21);
            container.Compose(batch);

            var export = container.GetExport<NonSharedPartRecomposable>();
            var exportedValue = export.Value;

            Assert.Equal(21, exportedValue.Value);

            container.ReleaseExport(export);

            // Recompose just to ensure we don't blow up, even though we don't expect anything to happen.
            batch = new CompositionBatch();
            batch.RemovePart(valueKey);
            batch.AddExportedValue("Value", 42);
            container.Compose(batch);

            Assert.Equal(21, exportedValue.Value);
        }

        [Fact]
        public void GetExportManualDisposeThenRecompose_NonSharedDisposableRecomposablePart_ShouldThrowComposition()
        {
            var catalog = new TypeCatalog(typeof(NonSharedPartDisposableRecomposable));
            var container = new CompositionContainer(catalog);

            // Setup dependency
            CompositionBatch batch = new CompositionBatch();
            var valueKey = batch.AddExportedValue("Value", 21);
            container.Compose(batch);

            var export = container.GetExport<NonSharedPartDisposableRecomposable>();
            var exportedValue = export.Value;

            Assert.Equal(21, exportedValue.Value);

            exportedValue.Dispose();

            // Recompose should cause a ObjectDisposedException.
            batch = new CompositionBatch();
            batch.RemovePart(valueKey);
            batch.AddExportedValue("Value", 42);

            CompositionAssert.ThrowsError(
                              ErrorId.ImportEngine_PartCannotActivate,         // Cannot activate part because
                              ErrorId.ReflectionModel_ImportThrewException,         // Import threw an exception
                              RetryMode.DoNotRetry,
                              () =>
            {
                container.Compose(batch);
            });
        }

        [Export]
        public class MyImporter
        {
            [Import(AllowDefault = true, AllowRecomposition = true, RequiredCreationPolicy = CreationPolicy.NonShared)]
            public AnyPartDisposable AnyPartDisposable { get; set; }
        }

        [Fact]
        public void RecomposeCausesOldImportedValuesToBeDisposed()
        {
            var cat = new AggregateCatalog();
            var cat1 = new TypeCatalog(typeof(AnyPartDisposable));

            cat.Catalogs.Add(new TypeCatalog(typeof(MyImporter)));
            cat.Catalogs.Add(cat1);

            var container = new CompositionContainer(cat);

            var importer = container.GetExportedValue<MyImporter>();

            var anyPart = importer.AnyPartDisposable;

            Assert.False(anyPart.IsDisposed);
            Assert.IsType<AnyPartDisposable>(anyPart);

            // Remove the instance of MyClass1
            cat.Catalogs.Remove(cat1);

            Assert.Null(importer.AnyPartDisposable);
            Assert.True(anyPart.IsDisposed);
        }

        private static CompositionContainer CreateParentChildContainerWithNonSharedImporter()
        {
            var parentCat = CatalogFactory.CreateAttributed(typeof(AnyPartDisposable),
                                      typeof(AnyPartDisposableRecomposable),
                                      typeof(AnyPartRecomposable),
                                      typeof(AnyPartSimple));
            var parent = new CompositionContainer(parentCat);
            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue("Value", 21);
            parent.Compose(batch);

            var childCat = CatalogFactory.CreateAttributed(typeof(NonSharedImporter));
            var child = new CompositionContainer(childCat, parent);

            return child;
        }

        [Fact]
        public void ChildContainerGetReleaseExport_NonSharedRoot_ShouldDisposeChain()
        {
            var child = CreateParentChildContainerWithNonSharedImporter();

            var export = child.GetExport<NonSharedImporter>();
            var exportedValue = export.Value;

            child.ReleaseExport(export);

            Assert.True(exportedValue.AnyPartDisposable.IsDisposed);
            Assert.True(exportedValue.AnyPartDisposableRecomposable.IsDisposed);
        }

        [Fact]
        public void ChildContainerAddRemovePart_NonSharedRoot_ShouldDisposeChain()
        {
            var child = CreateParentChildContainerWithNonSharedImporter();

            var exportedValue = new NonSharedImporter();

            CompositionBatch batch = new CompositionBatch();
            var part = batch.AddPart(exportedValue);
            child.Compose(batch);

            batch = new CompositionBatch();
            batch.RemovePart(part);
            child.Compose(batch);

            Assert.True(exportedValue.AnyPartDisposable.IsDisposed);
            Assert.True(exportedValue.AnyPartDisposableRecomposable.IsDisposed);
        }

        [Fact]
        public void ChildContainerAddRemovePart_NonSharedRoot_ShouldNotDisposeChain()
        {
            var child = CreateParentChildContainerWithNonSharedImporter();

            var exportedValue = child.GetExportedValue<NonSharedImporter>();

            child.Dispose();

            Assert.False(exportedValue.AnyPartDisposable.IsDisposed);
            Assert.False(exportedValue.AnyPartDisposableRecomposable.IsDisposed);
        }

        [Fact]
        [ActiveIssue(25498)]
        public void NonSharedPart_Simple_ShouldBeCollected()
        {
            var catalog = new TypeCatalog(typeof(NonSharedPartSimple));
            var container = new CompositionContainer(catalog);

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesExpectedToBeCollected(
                container.GetExportedValue<NonSharedPartSimple>());

            refTracker.CollectAndAssert();

            GC.KeepAlive(container);
        }

        [Fact]
        [ActiveIssue(25498)]
        public void ContainerDispose_SharedPart_ShouldCollectWholeObjectChain()
        {
            // Test only works properly with while using the real ConditionalWeakTable
            var container = GetContainer();

            var export = container.GetExport<SharedImporter>();
            var exportedValue = export.Value;

            container.Dispose();

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesExpectedToBeCollected(
                exportedValue,
                exportedValue.AnyPartDisposable,
                exportedValue.AnyPartDisposableRecomposable,
                exportedValue.AnyPartRecomposable,
                exportedValue.AnyPartSimple);

            export = null;
            exportedValue = null;

            refTracker.CollectAndAssert();

            GC.KeepAlive(container);
        }

        [Fact]
        [ActiveIssue(25498)]
        public void AddRemovePart_SharedPart_ShouldCollectOnlyRoot()
        {
            var container = GetContainer();

            var exportedValue = new SharedImporter();

            CompositionBatch batch = new CompositionBatch();
            var part = batch.AddPart(exportedValue);
            container.Compose(batch);
            batch = null;

            batch = new CompositionBatch();
            batch.RemovePart(part);
            container.Compose(batch);
            batch = null;

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesExpectedToBeCollected(
             exportedValue);

            refTracker.AddReferencesNotExpectedToBeCollected(
                exportedValue.AnyPartDisposable,
                exportedValue.AnyPartDisposableRecomposable,
                exportedValue.AnyPartRecomposable,
                exportedValue.AnyPartSimple);

            part = null;
            exportedValue = null;

            refTracker.CollectAndAssert();

            GC.KeepAlive(container);
        }

        [Fact]
        [ActiveIssue(25498)]
        public void AddRemovePart_NonSharedPart_ShouldCollectWholeObjectChain()
        {
            var container = GetContainer();

            var exportedValue = new NonSharedImporter();

            CompositionBatch batch = new CompositionBatch();
            var part = batch.AddPart(exportedValue);
            container.Compose(batch);
            batch = null;

            batch = new CompositionBatch();
            batch.RemovePart(part);
            container.Compose(batch);
            batch = null;

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesExpectedToBeCollected(
                exportedValue,
                exportedValue.AnyPartDisposable,
                exportedValue.AnyPartDisposableRecomposable,
                exportedValue.AnyPartRecomposable,
                exportedValue.AnyPartSimple);

            part = null;
            exportedValue = null;

            refTracker.CollectAndAssert();

            GC.KeepAlive(container);
        }

        [Fact]
        [ActiveIssue(25498)]
        public void ContainerDispose_NonSharedPart_ShouldCollectWholeObjectChain()
        {
            // Test only works properly with while using the real ConditionalWeakTable
            var container = GetContainer();

            var export = container.GetExport<NonSharedImporter>();
            var exportedValue = export.Value;

            container.Dispose();

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesExpectedToBeCollected(
                exportedValue,
                exportedValue.AnyPartDisposable,
                exportedValue.AnyPartDisposableRecomposable,
                exportedValue.AnyPartRecomposable,
                exportedValue.AnyPartSimple);

            export = null;
            exportedValue = null;

            refTracker.CollectAndAssert();

            GC.KeepAlive(container);
        }

        [Fact]
        [ActiveIssue(25498)]
        public void NonSharedImporter_ReleaseReference_ShouldCollectWholeChain()
        {
            var container = GetContainer();

            var export = container.GetExport<NonSharedImporter>();
            var exportedValue = export.Value;

            var refTracker = new ReferenceTracker();

            // Non-Disposable references in the chain should be GC'ed
            refTracker.AddReferencesExpectedToBeCollected(
                exportedValue,
                exportedValue.AnyPartRecomposable,
                exportedValue.AnyPartSimple);

            // Disposable references in the chain should NOT be GC'ed
            refTracker.AddReferencesNotExpectedToBeCollected(
                exportedValue.AnyPartDisposable,
                exportedValue.AnyPartDisposableRecomposable);

            export = null;
            exportedValue = null;

            refTracker.CollectAndAssert();

            GC.KeepAlive(container);
        }

        [Fact]
        [ActiveIssue(25498)]
        public void ChildContainerDispose_NonSharedPart_ShouldOnlyCleanupChildAndSimpleNonShared()
        {
            var child = CreateParentChildContainerWithNonSharedImporter();

            var exportedValue = child.GetExportedValue<NonSharedImporter>();

            child.Dispose();

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesExpectedToBeCollected(
                exportedValue,                // object in child
                exportedValue.AnyPartSimple,  // No reference parent so collected.
                exportedValue.AnyPartRecomposable);

            // These are in the parent and will not be cleaned out
            refTracker.AddReferencesNotExpectedToBeCollected(
                exportedValue.AnyPartDisposable,
                exportedValue.AnyPartDisposableRecomposable);

            exportedValue = null;

            refTracker.CollectAndAssert();

            GC.KeepAlive(child);
        }

        [Fact]
        [ActiveIssue(25498)]
        public void ChildContainerGetReleaseExport_NonSharedPart_ShouldCollectWholeObjectChain()
        {
            var child = CreateParentChildContainerWithNonSharedImporter();

            var export = child.GetExport<NonSharedImporter>();
            var exportedValue = export.Value;

            child.ReleaseExport(export);

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesExpectedToBeCollected(
                exportedValue,
                exportedValue.AnyPartDisposable,
                exportedValue.AnyPartDisposableRecomposable,
                exportedValue.AnyPartRecomposable,
                exportedValue.AnyPartSimple);

            export = null;
            exportedValue = null;

            refTracker.CollectAndAssert();

            GC.KeepAlive(child);
        }

        [Fact]
        [ActiveIssue(25498)]
        public void NonSharedPart_RecomposableImport_NoReference_ShouldBeCollected()
        {
            var catalog = new TypeCatalog(typeof(NonSharedPartRecomposable));
            var container = new CompositionContainer(catalog);

            // Setup dependency
            CompositionBatch batch = new CompositionBatch();
            var valueKey = batch.AddExportedValue("Value", 21);
            container.Compose(batch);
            batch = null;

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesExpectedToBeCollected(
                container.GetExportedValue<NonSharedPartRecomposable>());

            refTracker.CollectAndAssert();

            // Recompose just to ensure we don't blow up, even though we don't expect anything to happen.
            batch = new CompositionBatch();
            batch.RemovePart(valueKey);
            batch.AddExportedValue("Value", 42);
            container.Compose(batch);
            batch = null;

            GC.KeepAlive(container);
        }

        [Fact]
        [ActiveIssue(25498)]
        public void ChildContainerAddRemovePart_NonSharedPart_ShouldCollectWholeObjectChain()
        {
            var child = CreateParentChildContainerWithNonSharedImporter();

            var exportedValue = new NonSharedImporter();

            CompositionBatch batch = new CompositionBatch();
            var part = batch.AddPart(exportedValue);
            child.Compose(batch);
            batch = null;

            batch = new CompositionBatch();
            batch.RemovePart(part);
            child.Compose(batch);
            batch = null;

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesExpectedToBeCollected(
                exportedValue,
                exportedValue.AnyPartDisposable,
                exportedValue.AnyPartDisposableRecomposable,
                exportedValue.AnyPartRecomposable,
                exportedValue.AnyPartSimple);

            part = null;
            exportedValue = null;

            refTracker.CollectAndAssert();

            GC.KeepAlive(child);
        }

        [Fact]
        [ActiveIssue(25498)]
        public void GetReleaseExport_SharedPart_ShouldCollectOnlyRoot()
        {
            var container = GetContainer();

            var export = container.GetExport<SharedImporter>();
            var exportedValue = export.Value;

            container.ReleaseExport(export);

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesExpectedToBeCollected(
                exportedValue);

            refTracker.AddReferencesNotExpectedToBeCollected(
                exportedValue.AnyPartDisposable,
                exportedValue.AnyPartDisposableRecomposable,
                exportedValue.AnyPartRecomposable,
                exportedValue.AnyPartSimple);

            export = null;
            exportedValue = null;

            refTracker.CollectAndAssert();

            GC.KeepAlive(container);
        }

        [Fact]
        [ActiveIssue(25498)]
        public void GetReleaseExport_NonSharedPart_ShouldCollectWholeObjectChain()
        {
            var container = GetContainer();

            var export = container.GetExport<NonSharedImporter>();
            var exportedValue = export.Value;

            container.ReleaseExport(export);

            var refTracker = new ReferenceTracker();

            refTracker.AddReferencesExpectedToBeCollected(
                exportedValue,
                exportedValue.AnyPartDisposable,
                exportedValue.AnyPartDisposableRecomposable,
                exportedValue.AnyPartRecomposable,
                exportedValue.AnyPartSimple);

            export = null;
            exportedValue = null;

            refTracker.CollectAndAssert();

            GC.KeepAlive(container);
        }

        [Fact]
        public void ReleaseExports_ShouldWorkWithExportCollection()
        {
            var container = GetContainer();
            var exports = container.GetExports<NonSharedImporter>();

            Assert.True(exports.Count() > 0);

            var exportedValues = exports.Select(export => export.Value).ToList();

            container.ReleaseExports(exports);

            foreach (var obj in exportedValues)
            {
                Assert.True(obj.AnyPartDisposable.IsDisposed);
                Assert.True(obj.AnyPartDisposableRecomposable.IsDisposed);
            }
        }
    }
}
