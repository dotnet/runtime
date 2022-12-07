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
    public class CompositionBatchTests
    {
        [Fact]
        public void Constructor1_PropertiesShouldBeSetAndEmpty()
        {
            CompositionBatch batch = new CompositionBatch();

            Assert.NotNull(batch.PartsToAdd);
            Assert.Empty(batch.PartsToAdd);

            Assert.NotNull(batch.PartsToRemove);
            Assert.Empty(batch.PartsToRemove);
        }

        [Fact]
        public void Constructor2_PropertiesShouldBeSetAndMatchArguments()
        {
            ComposablePart[] partsToAdd = new ComposablePart[] { PartFactory.Create(), PartFactory.Create(), PartFactory.Create() };
            ComposablePart[] partsToRemove = new ComposablePart[] { PartFactory.Create(), PartFactory.Create(), PartFactory.Create() };

            CompositionBatch batch = new CompositionBatch(partsToAdd, partsToRemove);

            Assert.NotNull(batch.PartsToAdd);
            Assert.NotNull(batch.PartsToRemove);

            EqualityExtensions.CheckEquals(batch.PartsToAdd, partsToAdd);
            EqualityExtensions.CheckEquals(batch.PartsToRemove, partsToRemove);
        }

        [Fact]
        public void Constructor2_PartsToAddAsNull_PartsToAddShouldBeEmpty()
        {
            ComposablePart[] partsToRemove = new ComposablePart[] { PartFactory.Create(), PartFactory.Create(), PartFactory.Create() };

            var batch = new CompositionBatch(null, partsToRemove);

            Assert.Equal(0, batch.PartsToAdd.Count);
            Assert.Equal(partsToRemove.Length, batch.PartsToRemove.Count);
        }

        [Fact]
        public void Constructor2_PartsToRemoveAsNull_PartsToRemoveShouldBeEmpty()
        {
            ComposablePart[] partsToAdd = new ComposablePart[] { PartFactory.Create(), PartFactory.Create(), PartFactory.Create() };

            var batch = new CompositionBatch(partsToAdd, null);

            Assert.Equal(partsToAdd.Length, batch.PartsToAdd.Count);
            Assert.Equal(0, batch.PartsToRemove.Count);
        }

        [Fact]
        public void Constructor2_PartsToAddHasNull_ShouldThrowArgumentNullException()
        {
            ComposablePart[] partsToAdd = new ComposablePart[] { PartFactory.Create(), null, PartFactory.Create() };
            ComposablePart[] partsToRemove = new ComposablePart[] { PartFactory.Create(), PartFactory.Create(), PartFactory.Create() };

            Assert.Throws<ArgumentException>("partsToAdd", () =>
            {
                new CompositionBatch(partsToAdd, partsToRemove);
            });
        }

        [Fact]
        public void Constructor2_PartsToRemoveHasNull_ShouldThrowArgumentNullException()
        {
            ComposablePart[] partsToAdd = new ComposablePart[] { PartFactory.Create(), PartFactory.Create(), PartFactory.Create() };
            ComposablePart[] partsToRemove = new ComposablePart[] { PartFactory.Create(), null, PartFactory.Create() };

            Assert.Throws<ArgumentException>("partsToRemove", () =>
            {
                new CompositionBatch(partsToAdd, partsToRemove);
            });
        }

        [Fact]
        public void AddPart_PartIsInPartsToAdd()
        {
            CompositionBatch batch = new CompositionBatch();
            ComposablePart part = PartFactory.Create();

            batch.AddPart(part);

            Assert.Equal(1, batch.PartsToAdd.Count);
            Assert.Same(part, batch.PartsToAdd[0]);

            Assert.Empty(batch.PartsToRemove);
        }

        [Fact]
        public void AddPart_PartAsNull_ShouldThrowArgumentNullException()
        {
            CompositionBatch batch = new CompositionBatch();

            Assert.Throws<ArgumentNullException>("part", () =>
            {
                batch.AddPart(null);
            });
        }

        [Fact]
        public void RemovePart_PartIsInPartsToRemove()
        {
            CompositionBatch batch = new CompositionBatch();
            ComposablePart part = PartFactory.Create();

            batch.RemovePart(part);

            Assert.Equal(1, batch.PartsToRemove.Count);
            Assert.Same(part, batch.PartsToRemove[0]);

            Assert.Empty(batch.PartsToAdd);
        }

        [Fact]
        public void RemovePart_PartAsNull_ShouldThrowArgumentNullException()
        {
            CompositionBatch batch = new CompositionBatch();

            Assert.Throws<ArgumentNullException>("part", () =>
            {
                batch.RemovePart(null);
            });
        }

        [Fact]
        public void PartsToAdd_ShouldGetCopiedAfterAdd()
        {
            CompositionBatch batch = new CompositionBatch();
            ComposablePart part1 = PartFactory.Create();
            ComposablePart part2 = PartFactory.Create();

            batch.AddPart(part1);
            Assert.Contains(part1, batch.PartsToAdd);

            ReadOnlyCollection<ComposablePart> partsToAddBeforeCopy = batch.PartsToAdd;
            Assert.Same(partsToAddBeforeCopy, batch.PartsToAdd);

            Assert.Equal(1, partsToAddBeforeCopy.Count);
            Assert.Contains(part1, partsToAddBeforeCopy);

            batch.AddPart(part2);

            ReadOnlyCollection<ComposablePart> partsToAddAfterCopy = batch.PartsToAdd;
            Assert.Same(partsToAddAfterCopy, batch.PartsToAdd);

            Assert.Equal(2, partsToAddAfterCopy.Count);
            Assert.Contains(part1, partsToAddAfterCopy);
            Assert.Contains(part2, partsToAddAfterCopy);
            Assert.NotSame(partsToAddBeforeCopy, partsToAddAfterCopy);
        }

        [Fact]
        public void PartsToRemove_ShouldGetCopiedAfterRemove()
        {
            CompositionBatch batch = new CompositionBatch();
            ComposablePart part1 = PartFactory.Create();
            ComposablePart part2 = PartFactory.Create();

            batch.RemovePart(part1);
            Assert.Contains(part1, batch.PartsToRemove);

            ReadOnlyCollection<ComposablePart> partsToRemoveBeforeCopy = batch.PartsToRemove;
            Assert.Same(partsToRemoveBeforeCopy, batch.PartsToRemove);

            Assert.Equal(1, partsToRemoveBeforeCopy.Count);
            Assert.Contains(part1, partsToRemoveBeforeCopy);

            batch.RemovePart(part2);

            ReadOnlyCollection<ComposablePart> partsToRemoveAfterCopy = batch.PartsToRemove;
            Assert.Same(partsToRemoveAfterCopy, batch.PartsToRemove);

            Assert.Equal(2, partsToRemoveAfterCopy.Count);
            Assert.Contains(part1, partsToRemoveAfterCopy);
            Assert.Contains(part2, partsToRemoveAfterCopy);
            Assert.NotSame(partsToRemoveBeforeCopy, partsToRemoveAfterCopy);
        }

        [Fact]
        public void AddExportedValue_NullAsContractNameArgument_ShouldThrowArgumentNull()
        {
            CompositionBatch batch = new CompositionBatch();

            Assert.Throws<ArgumentNullException>("contractName", () =>
            {
                batch.AddExportedValue((string)null, "Value");
            });
        }

        [Fact]
        public void AddExportedValue_EmptyStringAsContractNameArgument_ShouldThrowArgument()
        {
            CompositionBatch batch = new CompositionBatch();

            Assert.Throws<ArgumentException>("contractName", () =>
            {
                batch.AddExportedValue("", "Value");
            });
        }

        [Fact]
        public void AddExport_NullAsExportArgument_ShouldThrowArgumentNull()
        {
            CompositionBatch batch = new CompositionBatch();

            Assert.Throws<ArgumentNullException>("export", () =>
            {
                batch.AddExport((Export)null);
            });
        }

        [Fact]
        public void AddExport_ExportWithNullExportedValueAsExportArgument_CanBeExported()
        {
            CompositionBatch batch = new CompositionBatch();
            var export = ExportFactory.Create("Contract", (object)null);

            batch.AddExport(export);
            Assert.Equal(1, batch.PartsToAdd.Count);

            var result = this.GetSingleExport(batch.PartsToAdd[0], "Contract");

            Assert.NotNull(result);
            Assert.Null(result.Value);
        }

        [Fact]
        public void AddExportedValueOfT_NullAsExportedValueArgument_CanBeExported()
        {
            CompositionBatch batch = new CompositionBatch();

            batch.AddExportedValue<string>((string)null);

            Assert.Equal(1, batch.PartsToAdd.Count);
            var result = this.GetSingleLazy<string>(batch.PartsToAdd[0]);

            Assert.NotNull(result);
            Assert.Null(result.Value);
        }

        [Fact]
        public void AddExportedValue_NullAsExportedValueArgument_CanBeExported()
        {
            CompositionBatch batch = new CompositionBatch();

            batch.AddExportedValue("Contract", (string)null);

            Assert.Equal(1, batch.PartsToAdd.Count);
            var result = this.GetSingleExport(batch.PartsToAdd[0], "Contract");

            Assert.NotNull(result);
            Assert.Null(result.Value);
        }

        [Fact]
        public void AddExport_ExportWithEmptyMetadata_IsExportedWithEmptyMetadata()
        {
            CompositionBatch batch = new CompositionBatch();
            var export = ExportFactory.Create("Contract", "Value", new Dictionary<string, object>());

            Assert.Equal(0, export.Metadata.Count);

            batch.AddExport(export);
            Assert.Equal(1, batch.PartsToAdd.Count);

            var result = this.GetSingleExport(batch.PartsToAdd[0], "Contract");

            Assert.Equal(0, result.Metadata.Count);
        }

        [Fact]
        public void AddExportedValueOfT_IsExportedWithEmptyMetadata()
        {
            CompositionBatch batch = new CompositionBatch();

            batch.AddExportedValue<string>("Value");
            Assert.Equal(1, batch.PartsToAdd.Count);

            var result = this.GetSingleLazy<string>(batch.PartsToAdd[0]);

            Assert.Equal(1, result.Metadata.Count); // contains type identity
        }

        [Fact]
        public void AddExportedValue_IsExportedWithEmptyMetadata()
        {
            CompositionBatch batch = new CompositionBatch();

            batch.AddExportedValue("Contract", "Value");
            Assert.Equal(1, batch.PartsToAdd.Count);

            var result = this.GetSingleExport(batch.PartsToAdd[0], "Contract");

            Assert.Equal(1, result.Metadata.Count); // contains type identity
        }

        [Fact]
        public void AddExport_ReturnedComposablePart_IsInAddedPartsCollection()
        {
            CompositionBatch batch = new CompositionBatch();

            var export = ExportFactory.Create("Contract", "Value");
            var part = batch.AddExport(export);
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Equal("Value", this.GetSingleExport(batch.PartsToAdd[0], "Contract").Value);
            Assert.Contains(part, batch.PartsToAdd);
        }

        [Fact]
        public void AddExportedValueOfT_ReturnedComposablePart_IsInAddedPartsCollection()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue<string>("Value");
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Equal("Value", this.GetSingleLazy<string>(batch.PartsToAdd[0]).Value);
            Assert.Contains(part, batch.PartsToAdd);
        }

        [Fact]
        public void AddExportedValue_ReturnedComposablePart_IsInAddedPartsCollection()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue("Contract", "Value");
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Equal("Value", this.GetSingleExport(batch.PartsToAdd[0], "Contract").Value);
            Assert.Contains(part, batch.PartsToAdd);
        }

        [Fact]
        public void AddExportedValueOfT_ExportAsExportedValueArgument_ShouldBeWrappedInExport()
        {
            CompositionBatch batch = new CompositionBatch();
            var export = ExportFactory.Create("Contract", "Value");

            batch.AddExportedValue<object>(export);
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Same(export, this.GetSingleLazy<object>(batch.PartsToAdd[0]).Value);
        }

        [Fact]
        public void AddExportedValue_ExportAsExportedValueArgument_ShouldBeWrappedInExport()
        {
            CompositionBatch batch = new CompositionBatch();
            var export = ExportFactory.Create("Contract", "Value");

            batch.AddExportedValue(export);
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Same(export, this.GetSingleLazy<Export>(batch.PartsToAdd[0]).Value);
        }

        [Fact]
        public void AddExport_ReturnedComposablePart_NullAsDefinitionArgumentToGetExportedValue_ShouldThrowArgumentNull()
        {
            CompositionBatch batch = new CompositionBatch();
            var export = ExportFactory.Create("Contract", "Value");

            var part = batch.AddExport(export);
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                part.GetExportedValue((ExportDefinition)null);
            });
        }

        [Fact]
        public void AddExport_ReturnedComposablePart_WrongDefinitionAsDefinitionArgumentToGetExportedValue_ShouldThrowArgument()
        {
            CompositionBatch batch = new CompositionBatch();
            var export = ExportFactory.Create("Contract", "Value");

            var part = batch.AddExport(export);
            var definition = ExportDefinitionFactory.Create();
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentException>("definition", () =>
            {
                part.GetExportedValue(definition);
            });
        }

        [Fact]
        public void AddExport_ReturnedComposablePart_NullAsDefinitionArgumentToSetImports_ShouldThrowArgumentNull()
        {
            CompositionBatch batch = new CompositionBatch();
            var export = ExportFactory.Create("Contract", "Value");

            var part = batch.AddExport(export);
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                part.SetImport((ImportDefinition)null, Enumerable.Empty<Export>());
            });
        }

        [Fact]
        public void AddExport_ReturnedComposablePart_NullAsExportsArgumentToSetImports_ShouldThrowArgumentNull()
        {
            CompositionBatch batch = new CompositionBatch();
            var export = ExportFactory.Create("Contract", "Value");

            var part = batch.AddExport(export);
            var definition = ImportDefinitionFactory.Create();
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentNullException>("exports", () =>
            {
                part.SetImport(definition, (IEnumerable<Export>)null);
            });
        }

        [Fact]
        public void AddExport_ReturnedComposablePart_ExportsArrayWithNullElementAsExportsArgumentToSetImports_ShouldThrowArgument()
        {
            CompositionBatch batch = new CompositionBatch();
            var export = ExportFactory.Create("Contract", "Value");

            var part = batch.AddExport(export);
            var definition = ImportDefinitionFactory.Create();
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentException>("exports", () =>
            {
                part.SetImport(definition, new Export[] { null });
            });
        }

        [Fact]
        public void AddExport_ReturnedComposablePart_SetImports_ShouldThrowArgument()
        {
            CompositionBatch batch = new CompositionBatch();
            var export = ExportFactory.Create("Contract", "Value");

            var part = batch.AddExport(export);
            var definition = ImportDefinitionFactory.Create();
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentException>("definition", () =>
            {
                part.SetImport(definition, Enumerable.Empty<Export>());
            });
        }

        [Fact]
        public void AddExport_ReturnedComposablePart_ContainsExportDefinitionRepresentingExport()
        {
            var metadata = new Dictionary<string, object>();
            metadata["Name"] = "Value";

            CompositionBatch batch = new CompositionBatch();
            var export = ExportFactory.Create("Contract", "Value", metadata);

            var part = batch.AddExport(export);
            Assert.Equal(1, batch.PartsToAdd.Count);

            var definition = part.ExportDefinitions.Single();

            Assert.Equal("Contract", definition.ContractName);
            Assert.Equal("Value", part.GetExportedValue(definition));
            EnumerableAssert.AreEqual(metadata, definition.Metadata);
        }

        [Fact]
        public void AddExportedValueOfT_ReturnedComposablePart_ImportDefinitionsPropertyIsEmpty()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue<string>("Value");
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Equal(0, part.ImportDefinitions.Count());
        }

        [Fact]
        public void AddExportedValueOfT_ReturnedComposablePart_MetadataPropertyIsEmpty()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue<string>("Value");
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Equal(0, part.Metadata.Count);
        }

        [Fact]
        public void AddExportedValueOfT_ReturnedComposablePart_NullAsDefinitionArgumentToGetExportedValue_ShouldThrowArgumentNull()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue<string>("Value");
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                part.GetExportedValue((ExportDefinition)null);
            });
        }

        [Fact]
        public void AddExportedValueOfT_ReturnedComposablePart_WrongDefinitionAsDefinitionArgumentToGetExportedValue_ShouldThrowArgument()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue<string>("Value");
            var definition = ExportDefinitionFactory.Create();
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentException>("definition", () =>
            {
                part.GetExportedValue(definition);
            });
        }

        [Fact]
        public void AddExportedValueOfT_ReturnedComposablePart_NullAsDefinitionArgumentToSetImports_ShouldThrowArgumentNull()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue<string>("Value");
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                part.SetImport((ImportDefinition)null, Enumerable.Empty<Export>());
            });
        }

        [Fact]
        public void AddExportedValueOfT_ReturnedComposablePart_NullAsExportsArgumentToSetImports_ShouldThrowArgumentNull()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue<string>("Value");
            var definition = ImportDefinitionFactory.Create();
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentNullException>("exports", () =>
            {
                part.SetImport(definition, (IEnumerable<Export>)null);
            });
        }

        [Fact]
        public void AddExportedValueOfT_ReturnedComposablePart_ExportsArrayWithNullElementAsExportsArgumentToSetImports_ShouldThrowArgument()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue<string>("Value");
            var definition = ImportDefinitionFactory.Create();

            Assert.Throws<ArgumentException>("exports", () =>
            {
                part.SetImport(definition, new Export[] { null });
            });
        }

        [Fact]
        public void AddExportedValueOfT_ReturnedComposablePart_SetImports_ShouldThrowArgument()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue<string>("Value");
            var definition = ImportDefinitionFactory.Create();
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentException>("definition", () =>
            {
                part.SetImport(definition, Enumerable.Empty<Export>());
            });
        }

        [Fact]
        public void AddExportedValueOfT_ReturnedComposablePart_ContainsExportDefinitionRepresentingExport()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue<string>("Value");
            Assert.Equal(1, batch.PartsToAdd.Count);

            var definition = part.ExportDefinitions.Single();

            Assert.Equal(NameForType<string>(), definition.ContractName);
            Assert.Equal("Value", part.GetExportedValue(definition));
            Assert.Equal(1, definition.Metadata.Count); // contains type identity
        }

        [Fact]
        public void AddExportedValue_ReturnedComposablePart_ImportDefinitionsPropertyIsEmpty()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue("Contract", "Value");
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Equal(0, part.ImportDefinitions.Count());
        }

        [Fact]
        public void AddExportedValue_ReturnedComposablePart_MetadataPropertyIsEmpty()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue("Contract", "Value");
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Equal(0, part.Metadata.Count);
        }

        [Fact]
        public void AddExportedValue_ReturnedComposablePart_NullAsDefinitionArgumentToGetExportedValue_ShouldThrowArgumentNull()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue("Contract", "Value");
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                part.GetExportedValue((ExportDefinition)null);
            });
        }

        [Fact]
        public void AddExportedValue_ReturnedComposablePart_WrongDefinitionAsDefinitionArgumentToGetExportedValue_ShouldThrowArgument()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue("Contract", "Value");
            var definition = ExportDefinitionFactory.Create();
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentException>("definition", () =>
            {
                part.GetExportedValue(definition);
            });
        }

        [Fact]
        public void AddExportedValue_ReturnedComposablePart_NullAsDefinitionArgumentToSetImports_ShouldThrowArgumentNull()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue("Contract", "Value");
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                part.SetImport((ImportDefinition)null, Enumerable.Empty<Export>());
            });
        }

        [Fact]
        public void AddExportedValue_ReturnedComposablePart_NullAsExportsArgumentToSetImports_ShouldThrowArgumentNull()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue("Contract", "Value");
            var definition = ImportDefinitionFactory.Create();
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentNullException>("exports", () =>
            {
                part.SetImport(definition, (IEnumerable<Export>)null);
            });
        }

        [Fact]
        public void AddExportedValue_ReturnedComposablePart_ExportsArrayWithNullElementAsExportsArgumentToSetImports_ShouldThrowArgument()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue("Contract", "Value");
            var definition = ImportDefinitionFactory.Create();
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentException>("exports", () =>
            {
                part.SetImport(definition, new Export[] { null });
            });
        }

        [Fact]
        public void AddExportedValue_ReturnedComposablePart_WrongDefinitionAsDefinitionArgumentToSetImports_ShouldThrowArgument()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue("Contract", "Value");
            var definition = ImportDefinitionFactory.Create();
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentException>("definition", () =>
            {
                part.SetImport(definition, Enumerable.Empty<Export>());
            });
        }

        [Fact]
        public void AddExportedValue_ReturnedComposablePart_ContainsExportDefinitionRepresentingExport()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddExportedValue("Contract", "Value");
            Assert.Equal(1, batch.PartsToAdd.Count);

            var definition = part.ExportDefinitions.Single();

            Assert.Equal("Contract", definition.ContractName);
            Assert.Equal("Value", part.GetExportedValue(definition));
            Assert.Equal(1, definition.Metadata.Count); // contains type identity
        }

        [Fact]
        public void AddPart_Int32ValueTypeAsAttributedPartArgument_ShouldThrowArgument()
        {
            CompositionBatch batch = new CompositionBatch();

            Assert.Throws<ArgumentException>("attributedPart", () =>
            {
                batch.AddPart((object)10);
            });
        }

        [Fact]
        public void AddPart_ReturnedComposablePart_NullAsDefinitionArgumentToGetExportedValue_ShouldThrowArgumentNull()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddPart(new Int32Importer());
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                part.GetExportedValue((ExportDefinition)null);
            });
        }

        [Fact]
        public void AddPart_ReturnedComposablePart_WrongDefinitionAsDefinitionArgumentToGetExportedValue_ShouldThrowArgument()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddPart(new Int32Importer());
            var definition = ExportDefinitionFactory.Create();
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentException>("definition", () =>
            {
                part.GetExportedValue(definition);
            });
        }

        [Fact]
        public void AddPart_ReturnedComposablePart_NullAsDefinitionArgumentToSetImports_ShouldThrowArgumentNull()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddPart(new Int32Importer());
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                part.SetImport((ImportDefinition)null, Enumerable.Empty<Export>());
            });
        }

        [Fact]
        public void AddPart_ReturnedComposablePart_NullAsExportsArgumentToSetImports_ShouldThrowArgumentNull()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddPart(new Int32Importer());
            var definition = ImportDefinitionFactory.Create();
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentNullException>("exports", () =>
            {
                part.SetImport(definition, (IEnumerable<Export>)null);
            });
        }

        [Fact]
        public void AddPart_ReturnedComposablePart_ExportsArrayWithNullElementAsExportsArgumentToSetImports_ShouldThrowArgument()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddPart(new Int32Importer());
            var definition = part.ImportDefinitions.First();
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentException>("exports", () =>
            {
                part.SetImport(definition, new Export[] { null });
            });
        }

        [Fact]
        public void AddPart_ReturnedComposablePart_WrongDefinitionAsDefinitionArgumentToSetImports_ShouldThrowArgument()
        {
            CompositionBatch batch = new CompositionBatch();

            var part = batch.AddPart(new Int32Importer());
            var definition = ImportDefinitionFactory.Create();
            Assert.Equal(1, batch.PartsToAdd.Count);

            Assert.Throws<ArgumentException>("definition", () =>
            {
                part.SetImport(definition, Enumerable.Empty<Export>());
            });
        }

        private Export GetSingleLazy<T>(ComposablePart part)
        {
            return this.GetSingleExport(part, AttributedModelServices.GetContractName(typeof(T)));
        }

        private Export GetSingleExport(ComposablePart part, string contractName)
        {
            Assert.NotNull(part);
            Assert.Equal(0, part.Metadata.Count);
            Assert.Equal(1, part.ExportDefinitions.Count());
            Assert.Equal(0, part.ImportDefinitions.Count());
            ExportDefinition exportDefinition = part.ExportDefinitions.First();
            Assert.Equal(contractName, exportDefinition.ContractName);

            part.Activate();

            return new Export(exportDefinition, () => part.GetExportedValue(exportDefinition));
        }

        private static string NameForType<T>()
        {
            return AttributedModelServices.GetContractName(typeof(T));
        }
    }

}
