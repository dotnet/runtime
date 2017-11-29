// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class InitializationScopeTests
    {
        [Fact]
        public void SingleContainerSimpleCompose()
        {
            var container = ContainerFactory.Create();
            ImportingComposablePart importPart;
            CompositionBatch batch = new CompositionBatch();

            batch.AddExportedValue("value1", "Hello");
            batch.AddExportedValue("value2", "World");
            batch.AddPart(importPart = PartFactory.CreateImporter("value1", "value2"));
            container.Compose(batch);

            Assert.Equal(2, importPart.ImportSatisfiedCount);
            Assert.Equal("Hello", importPart.GetImport("value1"));
            Assert.Equal("World", importPart.GetImport("value2"));
        }

        [Fact]
        public void ParentedContainerSimpleCompose()
        {
            var container = ContainerFactory.Create();
            var importPart = PartFactory.CreateImporter("value1", "value2");

            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue("value1", "Parent");

            var childContainer = new CompositionContainer(container);
            CompositionBatch childBatch = new CompositionBatch();
            childBatch.AddExportedValue("value2", "Child");
            childBatch.AddPart(importPart);

            Assert.Equal(0, importPart.ImportSatisfiedCount);

            container.Compose(batch);
            childContainer.Compose(childBatch);

            Assert.Equal(2, importPart.ImportSatisfiedCount);
            Assert.Equal("Parent", importPart.GetImport("value1"));
            Assert.Equal("Child", importPart.GetImport("value2"));
        }

        [Fact]
        public void SingleContainerPartReplacement()
        {
            var container = ContainerFactory.Create();
            var importPart = PartFactory.CreateImporter(true, "value1", "value2");

            CompositionBatch batch = new CompositionBatch();
            var export1Key = batch.AddExportedValue("value1", "Hello");
            batch.AddExportedValue("value2", "World");
            batch.AddPart(importPart);
            container.Compose(batch);

            Assert.Equal(2, importPart.ImportSatisfiedCount);
            Assert.Equal("Hello", importPart.GetImport("value1"));
            Assert.Equal("World", importPart.GetImport("value2"));

            importPart.ResetImportSatisfiedCount();

            batch = new CompositionBatch();
            batch.RemovePart(export1Key);
            batch.AddExportedValue("value1", "Goodbye");
            container.Compose(batch);

            Assert.Equal(1, importPart.ImportSatisfiedCount);
            Assert.Equal("Goodbye", importPart.GetImport("value1"));
            Assert.Equal("World", importPart.GetImport("value2"));
        }

        [Fact]
        public void ParentedContainerPartReplacement()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            var importPart = PartFactory.CreateImporter(true, "value1", "value2");
            var exportKey = batch.AddExportedValue("value1", "Parent");

            var childContainer = new CompositionContainer(container);
            CompositionBatch childBatch = new CompositionBatch();
            childBatch.AddExportedValue("value2", "Child");
            childBatch.AddPart(importPart);

            Assert.Equal(0, importPart.ImportSatisfiedCount);
            container.Compose(batch);
            childContainer.Compose(childBatch);

            Assert.Equal(2, importPart.ImportSatisfiedCount);
            Assert.Equal("Parent", importPart.GetImport("value1"));
            Assert.Equal("Child", importPart.GetImport("value2"));

            importPart.ResetImportSatisfiedCount();
            batch = new CompositionBatch();
            batch.RemovePart(exportKey);
            batch.AddExportedValue("value1", "New Parent");
            container.Compose(batch);

            Assert.Equal(1, importPart.ImportSatisfiedCount);
            Assert.Equal("New Parent", importPart.GetImport("value1"));
            Assert.Equal("Child", importPart.GetImport("value2"));
        }

        [Fact]
        public void SelectiveRecompose()
        {
            var container = ContainerFactory.Create();
            var stableImporter = PartFactory.CreateImporter("stable");
            var dynamicImporter = PartFactory.CreateImporter("dynamic", true);
            CompositionBatch batch = new CompositionBatch();

            batch.AddPart(stableImporter);
            batch.AddPart(dynamicImporter);
            var exportKey = batch.AddExportedValue("dynamic", 1);
            batch.AddExportedValue("stable", 42);
            container.Compose(batch);

            Assert.Equal(1, stableImporter.ImportSatisfiedCount);
            Assert.Equal(stableImporter.GetImport("stable"), 42);
            Assert.Equal(1, dynamicImporter.ImportSatisfiedCount);
            Assert.Equal(dynamicImporter.GetImport("dynamic"), 1);

            batch = new CompositionBatch();
            stableImporter.ResetImportSatisfiedCount();
            dynamicImporter.ResetImportSatisfiedCount();
            batch.RemovePart(exportKey);
            batch.AddExportedValue("dynamic", 2);
            container.Compose(batch);

            Assert.Equal(0, stableImporter.ImportSatisfiedCount);
            Assert.Equal(stableImporter.GetImport("stable"), 42);
            Assert.Equal(1, dynamicImporter.ImportSatisfiedCount);
            Assert.Equal(dynamicImporter.GetImport("dynamic"), 2);
        }
    }
}
