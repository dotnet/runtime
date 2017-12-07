// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ImportEngineTests
    {
        [Fact]
        public void PreviewImports_Successful_NoAtomicComposition_ShouldBlockChanges()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value");
            var importer = PartFactory.CreateImporter(import);

            exportProvider.AddExport("Value", 21);

            engine.PreviewImports(importer, null);

            Assert.Throws<ChangeRejectedException>(() =>
                exportProvider.AddExport("Value", 22));

            Assert.Throws<ChangeRejectedException>(() =>
                exportProvider.RemoveExport("Value"));

            GC.KeepAlive(importer);
        }

        [Fact]
        public void PreviewImports_Unsuccessful_NoAtomicComposition_ShouldNotBlockChanges()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value");
            var importer = PartFactory.CreateImporter(import);

            Assert.Throws<CompositionException>(() =>
                engine.PreviewImports(importer, null));

            exportProvider.AddExport("Value", 22);
            exportProvider.AddExport("Value", 23);
            exportProvider.RemoveExport("Value");

            GC.KeepAlive(importer);
        }

        [Fact]
        public void PreviewImports_Successful_AtomicComposition_Completeted_ShouldBlockChanges()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value");
            var importer = PartFactory.CreateImporter(import);

            exportProvider.AddExport("Value", 21);

            using (var atomicComposition = new AtomicComposition())
            {
                engine.PreviewImports(importer, atomicComposition);
                atomicComposition.Complete();
            }

            Assert.Throws<ChangeRejectedException>(() =>
                exportProvider.AddExport("Value", 22));

            Assert.Throws<ChangeRejectedException>(() =>
                exportProvider.RemoveExport("Value"));

            GC.KeepAlive(importer);
        }

        [Fact]
        public void PreviewImports_Successful_AtomicComposition_RolledBack_ShouldNotBlockChanges()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value");
            var importer = PartFactory.CreateImporter(import);

            exportProvider.AddExport("Value", 21);

            using (var atomicComposition = new AtomicComposition())
            {
                engine.PreviewImports(importer, atomicComposition);

                // Let atomicComposition get disposed thus rolledback
            }

            exportProvider.AddExport("Value", 22);
            exportProvider.RemoveExport("Value");

            GC.KeepAlive(importer);
        }

        [Fact]
        public void PreviewImports_Unsuccessful_AtomicComposition_ShouldNotBlockChanges()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value");
            var importer = PartFactory.CreateImporter(import);

            using (var atomicComposition = new AtomicComposition())
            {
                Assert.Throws<ChangeRejectedException>(() =>
                    engine.PreviewImports(importer, atomicComposition));
            }

            exportProvider.AddExport("Value", 22);
            exportProvider.AddExport("Value", 23);
            exportProvider.RemoveExport("Value");

            GC.KeepAlive(importer);
        }

        [Fact]
        [ActiveIssue(25498, TargetFrameworkMonikers.UapAot)]
        public void PreviewImports_ReleaseImports_ShouldNotBlockChanges()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value");
            var importer = PartFactory.CreateImporter(import);

            exportProvider.AddExport("Value", 21);

            engine.PreviewImports(importer, null);

            Assert.Throws<ChangeRejectedException>(() =>
                exportProvider.AddExport("Value", 22));

            Assert.Throws<ChangeRejectedException>(() =>
                exportProvider.RemoveExport("Value"));

            engine.ReleaseImports(importer, null);

            exportProvider.AddExport("Value", 22);
            exportProvider.RemoveExport("Value");

            GC.KeepAlive(importer);
        }

        [Fact]
        public void PreviewImports_MissingOptionalImport_ShouldSucceed()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value", ImportCardinality.ZeroOrOne);
            var importer = PartFactory.CreateImporter(import);

            engine.PreviewImports(importer, null);

            GC.KeepAlive(importer);
        }

        [Fact]
        public void PreviewImports_ZeroCollectionImport_ShouldSucceed()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value", ImportCardinality.ZeroOrMore);
            var importer = PartFactory.CreateImporter(import);

            engine.PreviewImports(importer, null);

            GC.KeepAlive(importer);
        }

        [Fact]
        public void PreviewImports_MissingOptionalImport_NonRecomposable_ShouldNotBlockChanges()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value", ImportCardinality.ZeroOrOne, false, false);
            var importer = PartFactory.CreateImporter(import);

            engine.PreviewImports(importer, null);

            exportProvider.AddExport("Value", 21);
            exportProvider.AddExport("Value", 22);
            exportProvider.RemoveExport("Value");

            GC.KeepAlive(importer);
        }

        [Fact]
        public void PreviewImports_ZeroCollectionImport_NonRecomposable_ShouldNotBlockChanges()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value", ImportCardinality.ZeroOrMore, false, false);
            var importer = PartFactory.CreateImporter(import);

            engine.PreviewImports(importer, null);

            exportProvider.AddExport("Value", 21);
            exportProvider.AddExport("Value", 22);
            exportProvider.RemoveExport("Value");

            GC.KeepAlive(importer);
        }

        [Fact]
        public void SatisfyImports_NonRecomposable_ValueShouldNotChange()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            exportProvider.AddExport("Value", 21);

            var import = ImportDefinitionFactory.Create("Value", false);
            var importer = PartFactory.CreateImporter(import);

            engine.SatisfyImports(importer);

            Assert.Equal(21, importer.GetImport(import));

            // After rejection batch failures throw ChangeRejectedException to indicate that
            // the failure did not affect the container
            Assert.Throws<ChangeRejectedException>(() =>
                exportProvider.ReplaceExportValue("Value", 42));

            Assert.Equal(21, importer.GetImport(import));

            GC.KeepAlive(importer);
        }

        [Fact]
        public void SatisfyImports_Recomposable_ValueShouldChange()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            exportProvider.AddExport("Value", 21);

            var import = ImportDefinitionFactory.Create("Value", true);
            var importer = PartFactory.CreateImporter(import);

            engine.SatisfyImports(importer);

            Assert.Equal(21, importer.GetImport(import));

            exportProvider.ReplaceExportValue("Value", 42);

            Assert.Equal(42, importer.GetImport(import));

            GC.KeepAlive(importer);
        }

        [Fact]
        public void SatisfyImports_NonRecomposable_Prerequisite_ValueShouldNotChange()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value", false, true);
            var importer = PartFactory.CreateImporter(import);

            exportProvider.AddExport("Value", 21);

            engine.SatisfyImports(importer);

            Assert.Equal(21, importer.GetImport(import));

            Assert.Throws<ChangeRejectedException>(() =>
                exportProvider.ReplaceExportValue("Value", 42));

            Assert.Equal(21, importer.GetImport(import));

            GC.KeepAlive(importer);
        }

        [Fact]
        public void SatisfyImports_Recomposable_Prerequisite_ValueShouldChange()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value", true, true);
            var importer = PartFactory.CreateImporter(import);

            exportProvider.AddExport("Value", 21);

            engine.SatisfyImports(importer);

            Assert.Equal(21, importer.GetImport(import));

            exportProvider.ReplaceExportValue("Value", 42);

            Assert.Equal(42, importer.GetImport(import));

            GC.KeepAlive(importer);
        }

        [Fact]
        public void SatisfyImports_OneRecomposable_OneNotRecomposable()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import1 = ImportDefinitionFactory.Create("Value", true);
            var import2 = ImportDefinitionFactory.Create("Value", false);
            var importer = PartFactory.CreateImporter(import1, import2);

            exportProvider.AddExport("Value", 21);

            engine.SatisfyImports(importer);

            // Initial compose values should be 21
            Assert.Equal(21, importer.GetImport(import1));
            Assert.Equal(21, importer.GetImport(import2));

            // Reset value to ensure it doesn't get set to same value again
            importer.ResetImport(import1);
            importer.ResetImport(import2);

            Assert.Throws<ChangeRejectedException>(() =>
                exportProvider.ReplaceExportValue("Value", 42));

            Assert.Equal(null, importer.GetImport(import1));
            Assert.Equal(null, importer.GetImport(import2));

            GC.KeepAlive(importer);
        }

        [Fact]
        public void SatisfyImports_TwoRecomposables_SingleExportValueChanged()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import1 = ImportDefinitionFactory.Create("Value1", true);
            var import2 = ImportDefinitionFactory.Create("Value2", true);
            var importer = PartFactory.CreateImporter(import1, import2);

            exportProvider.AddExport("Value1", 21);
            exportProvider.AddExport("Value2", 23);

            engine.SatisfyImports(importer);

            Assert.Equal(21, importer.GetImport(import1));
            Assert.Equal(23, importer.GetImport(import2));

            importer.ResetImport(import1);
            importer.ResetImport(import2);

            // Only change Value1 
            exportProvider.ReplaceExportValue("Value1", 42);

            Assert.Equal(42, importer.GetImport(import1));

            Assert.Equal(null, importer.GetImport(import2));

            GC.KeepAlive(importer);
        }

        [Fact]
        public void SatisfyImports_Recomposable_Unregister_ValueShouldChangeOnce()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            exportProvider.AddExport("Value", 21);

            var import = ImportDefinitionFactory.Create("Value", true);
            var importer = PartFactory.CreateImporter(import);

            engine.SatisfyImports(importer);

            Assert.Equal(21, importer.GetImport(import));

            exportProvider.ReplaceExportValue("Value", 42);

            Assert.Equal(42, importer.GetImport(import));

            engine.ReleaseImports(importer, null);

            exportProvider.ReplaceExportValue("Value", 666);

            Assert.Equal(42, importer.GetImport(import));

            GC.KeepAlive(importer);
        }

        [Fact]
        public void SatisfyImports_MissingOptionalImport_NonRecomposable_ShouldNotBlockChanges()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value", ImportCardinality.ZeroOrOne, false, false);
            var importer = PartFactory.CreateImporter(import);

            exportProvider.AddExport("Value", 20);

            engine.SatisfyImports(importer);

            Assert.Throws<ChangeRejectedException>(() =>
                exportProvider.AddExport("Value", 21));

            Assert.Throws<ChangeRejectedException>(() =>
                exportProvider.RemoveExport("Value"));

            GC.KeepAlive(importer);
        }

        [Fact]
        public void SatisfyImports_ZeroCollectionImport_NonRecomposable_ShouldNotBlockChanges()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value", ImportCardinality.ZeroOrMore, false, false);
            var importer = PartFactory.CreateImporter(import);

            exportProvider.AddExport("Value", 20);

            engine.SatisfyImports(importer);

            Assert.Throws<ChangeRejectedException>(() =>
                exportProvider.AddExport("Value", 21));

            Assert.Throws<ChangeRejectedException>(() =>
                exportProvider.RemoveExport("Value"));

            GC.KeepAlive(importer);
        }

        [Fact]
        public void SatisfyImports_MissingOptionalImport_Recomposable_ShouldNotBlockChanges()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value", ImportCardinality.ZeroOrOne, true, false);
            var importer = PartFactory.CreateImporter(import);

            exportProvider.AddExport("Value", 20);

            engine.SatisfyImports(importer);

            exportProvider.AddExport("Value", 21);
            exportProvider.RemoveExport("Value");

            GC.KeepAlive(importer);
        }

        [Fact]
        public void SatisfyImports_ZeroCollectionImport_Recomposable_ShouldNotBlockChanges()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value", ImportCardinality.ZeroOrMore, true, false);
            var importer = PartFactory.CreateImporter(import);

            exportProvider.AddExport("Value", 20);

            engine.SatisfyImports(importer);

            exportProvider.AddExport("Value", 21);
            exportProvider.RemoveExport("Value");

            GC.KeepAlive(importer);
        }

        [Fact]
        public void SatisfyImportsOnce_Recomposable_ValueShouldNotChange_NoRecompositionRequested()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            exportProvider.AddExport("Value", 21);

            var import = ImportDefinitionFactory.Create("Value", true);
            var importer = PartFactory.CreateImporter(import);

            engine.SatisfyImportsOnce(importer);

            Assert.Equal(21, importer.GetImport(import));

            exportProvider.ReplaceExportValue("Value", 42);

            Assert.Equal(21, importer.GetImport(import));

            GC.KeepAlive(importer);
        }

        [Fact]
        public void SatisifyImportsOnce_Recomposable_ValueShouldNotChange_NoRecompositionRequested_ViaNonArgumentSignature()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            exportProvider.AddExport("Value", 21);

            var import = ImportDefinitionFactory.Create("Value", true);
            var importer = PartFactory.CreateImporter(import);

            engine.SatisfyImportsOnce(importer);

            Assert.Equal(21, importer.GetImport(import));

            exportProvider.ReplaceExportValue("Value", 42);

            Assert.Equal(21, importer.GetImport(import));

            GC.KeepAlive(importer);
        }

        [Fact]
        public void SatisfyImportsOnce_Successful_ShouldNotBlockChanges()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value");
            var importer = PartFactory.CreateImporter(import);

            exportProvider.AddExport("Value", 21);

            engine.SatisfyImportsOnce(importer);

            exportProvider.AddExport("Value", 22);
            exportProvider.RemoveExport("Value");

            GC.KeepAlive(importer);
        }

        [Fact]
        public void SatisfyImportsOnce_Unsuccessful_ShouldNotBlockChanges()
        {
            var exportProvider = ExportProviderFactory.CreateRecomposable();
            var engine = new ImportEngine(exportProvider);

            var import = ImportDefinitionFactory.Create("Value");
            var importer = PartFactory.CreateImporter(import);

            Assert.Throws<CompositionException>(() =>
                engine.SatisfyImportsOnce(importer));

            exportProvider.AddExport("Value", 22);
            exportProvider.AddExport("Value", 23);
            exportProvider.RemoveExport("Value");

            GC.KeepAlive(importer);
        }
    }
}
