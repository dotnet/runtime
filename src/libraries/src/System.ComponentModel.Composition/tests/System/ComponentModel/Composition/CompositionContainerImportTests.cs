// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Runtime.InteropServices;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class CompositionContainerImportTests
    {
        // Exporting collectin values is not supported
        [Fact]
        public void ImportValues()
        {
            var container = ContainerFactory.Create();
            Importer importer = new Importer();
            Exporter exporter42 = new Exporter(42);

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            batch.AddPart(exporter42);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotSetImport,
                                          ErrorId.ReflectionModel_ImportNotAssignableFromExport, RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void ImportSingle()
        {
            var container = ContainerFactory.Create();
            var importer = new Int32Importer();
            var exporter = new Int32Exporter(42);

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            batch.AddPart(exporter);
            container.Compose(batch);

            Assert.Equal(42, importer.Value);

        }

        [Fact]
        public void ImportSingleFromInternal()
        {
            var container = ContainerFactory.Create();
            var importer = new Int32Importer();
            var exporter = new Int32ExporterInternal(42);

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            batch.AddPart(exporter);
            container.Compose(batch);

            Assert.Equal(42, importer.Value);
        }

        [Fact]
        public void ImportSingleToInternal()
        {
            var container = ContainerFactory.Create();
            var importer = new Int32ImporterInternal();
            var exporter = new Int32Exporter(42);

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            batch.AddPart(exporter);
            container.Compose(batch);

            Assert.Equal(42, importer.Value);
        }

        [Fact]
        public void ImportSingleIntoCollection()
        {
            var container = ContainerFactory.Create();
            var importer = new Int32CollectionImporter();
            var exporter = new Int32Exporter(42);

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            batch.AddPart(exporter);
            container.Compose(batch);

            EnumerableAssert.AreEqual(importer.Values, 42);
        }

        [Fact]
        public void ImportValuesNameless()
        {
            var container = ContainerFactory.Create();
            ImporterNameless importer;
            ExporterNameless exporter42 = new ExporterNameless(42);

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer = new ImporterNameless());
            batch.AddPart(exporter42);
            container.Compose(batch);

            Assert.Equal(42, importer.ValueReadWrite);
            Assert.Equal(42, importer.MetadataReadWrite.Value);
        }

        [Fact]
        public void ImportValueExceptionMissing()
        {
            var container = ContainerFactory.Create();
            Importer importer;

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer = new Importer());

            CompositionAssert.ThrowsChangeRejectedError(ErrorId.ImportEngine_PartCannotSetImport,
                                          ErrorId.ImportEngine_ImportCardinalityMismatch,
                                          RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void ImportValueExceptionMultiple()
        {
            var container = ContainerFactory.Create();
            Importer importer = new Importer();
            Exporter exporter42 = new Exporter(42);
            Exporter exporter6 = new Exporter(6);

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            batch.AddPart(exporter42);
            batch.AddPart(exporter6);

            CompositionAssert.ThrowsChangeRejectedError(ErrorId.ImportEngine_PartCannotSetImport,
                RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void ImportValueExceptionSetterException()
        {
            var container = ContainerFactory.Create();
            ImporterInvalidSetterException importer = new ImporterInvalidSetterException();
            Exporter exporter42 = new Exporter(42);

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            batch.AddPart(exporter42);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotActivate,
                                          ErrorId.ReflectionModel_ImportThrewException,
                                          RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }

        [Fact]
        public void ImportValueExceptionLazily()
        {
            var catalog = new AssemblyCatalog(typeof(ImportImporterInvalidSetterExceptionLazily).Assembly);
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(ImportImporterInvalidSetterExceptionLazily), typeof(ImporterInvalidSetterException));
            var invalidLazy = container.GetExportedValue<ImportImporterInvalidSetterExceptionLazily>();

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue,
                                          ErrorId.ImportEngine_PartCannotActivate,
                                          ErrorId.ReflectionModel_ImportThrewException, RetryMode.DoNotRetry, () =>
            {
                var value = invalidLazy.Value.Value;
            });
        }
        
        [ConditionalFact(Helpers.ComImportAvailable)]
        [PlatformSpecific(TestPlatforms.Windows)]
        [ActiveIssue(25498)]
        public void ImportValueComComponent()
        {
            CTaskScheduler scheduler = new CTaskScheduler();

            try
            {
                var container = ContainerFactory.Create();
                var importer = new ImportComComponent();

                CompositionBatch batch = new CompositionBatch();
                batch.AddParts(importer);
                batch.AddExportedValue<ITaskScheduler>("TaskScheduler", (ITaskScheduler)scheduler);

                container.Compose(batch);

                Assert.Equal<object>(scheduler, importer.TaskScheduler);
            }
            finally
            {
                Marshal.ReleaseComObject(scheduler);
            }
        }

        [ConditionalFact(Helpers.ComImportAvailable)]
        [PlatformSpecific(TestPlatforms.Windows)]
        [ActiveIssue(25498)]
        public void DelayImportValueComComponent()
        {
            CTaskScheduler scheduler = new CTaskScheduler();

            try
            {
                var container = ContainerFactory.Create();
                var importer = new DelayImportComComponent();

                CompositionBatch batch = new CompositionBatch();
                batch.AddParts(importer);
                batch.AddExportedValue<ITaskScheduler>("TaskScheduler", (ITaskScheduler)scheduler);

                container.Compose(batch);

                Assert.Equal<object>(scheduler, importer.TaskScheduler.Value);
            }
            finally
            {
                Marshal.ReleaseComObject(scheduler);
            }
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void OptionalImportsOfValueTypesAreBoundToDefaultWhenNotSatisfied()
        {
            var container = ContainerFactory.Create();
            var importer = new OptionalImport();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            container.Compose(batch);

            Assert.Equal(1, importer.ValueTypeSetCount);
            Assert.Equal(0, importer.ValueType);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void OptionalImportsOfNullableValueTypesAreBoundToDefaultWhenNotSatisfied()
        {
            var container = ContainerFactory.Create();
            var importer = new OptionalImport();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            container.Compose(batch);

            Assert.Equal(1, importer.NullableValueTypeSetCount);
            Assert.Null(importer.NullableValueType);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void OptionalImportsOfReferenceTypesAreBoundToDefaultWhenNotSatisfied()
        {
            var container = ContainerFactory.Create();
            var importer = new OptionalImport();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            container.Compose(batch);

            Assert.Equal(1, importer.ReferenceTypeSetCount);
            Assert.Null(importer.ReferenceType);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void OptionalImportsOfExportValueTypesAreBoundToDefaultWhenNotSatisfied()
        {
            var container = ContainerFactory.Create();
            var importer = new OptionalExport();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            container.Compose(batch);

            Assert.Equal(1, importer.ValueTypeSetCount);
            Assert.Null(importer.ValueType);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void OptionalImportsOfExportNullableValueTypesAreBoundToDefaultWhenNotSatisfied()
        {
            var container = ContainerFactory.Create();
            var importer = new OptionalExport();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            container.Compose(batch);

            Assert.Equal(1, importer.NullableValueTypeSetCount);
            Assert.Null(importer.NullableValueType);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void OptionalImportsOfExportReferenceTypesAreBoundToDefaultWhenNotSatisfied()
        {
            var container = ContainerFactory.Create();
            var importer = new OptionalExport();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            container.Compose(batch);

            Assert.Equal(1, importer.ReferenceTypeSetCount);
            Assert.Null(importer.ReferenceType);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void OptionalImportsOfValueTypesAreReboundToDefaultWhenExportIsRemoved()
        {
            var container = ContainerFactory.Create();
            var importer = new OptionalImport();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            var key = batch.AddExportedValue("ValueType", 10);

            container.Compose(batch);

            Assert.Equal(1, importer.ValueTypeSetCount);
            Assert.Equal(10, importer.ValueType);

            batch = new CompositionBatch();
            batch.RemovePart(key);
            container.Compose(batch);

            Assert.Equal(2, importer.ValueTypeSetCount);
            Assert.Equal(0, importer.ValueType);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void OptionalImportsOfNullableValueTypesAreReboundToDefaultWhenExportIsRemoved()
        {
            var container = ContainerFactory.Create();
            var importer = new OptionalImport();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            var key = batch.AddExportedValue<int?>("NullableValueType", 10);

            container.Compose(batch);
            Assert.Equal(1, importer.NullableValueTypeSetCount);
            Assert.Equal(10, importer.NullableValueType);

            batch = new CompositionBatch();
            batch.RemovePart(key);
            container.Compose(batch);

            Assert.Equal(2, importer.NullableValueTypeSetCount);
            Assert.Null(importer.NullableValueType);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void OptionalImportsOfReferenceTypesAreReboundToDefaultWhenExportIsRemoved()
        {
            var container = ContainerFactory.Create();
            var importer = new OptionalImport();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            var key = batch.AddExportedValue("ReferenceType", "Bar");

            container.Compose(batch);
            Assert.Equal(1, importer.ReferenceTypeSetCount);
            Assert.Equal("Bar", importer.ReferenceType);

            batch = new CompositionBatch();
            batch.RemovePart(key);
            container.Compose(batch);

            Assert.Equal(2, importer.ReferenceTypeSetCount);
            Assert.Null(importer.ReferenceType);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void OptionalImportsOfExportValueTypesAreReboundToDefaultWhenExportIsRemoved()
        {
            var container = ContainerFactory.Create();
            var importer = new OptionalExport();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            var key = batch.AddExportedValue("ValueType", 10);

            container.Compose(batch);

            Assert.Equal(1, importer.ValueTypeSetCount);
            Assert.Equal(10, importer.ValueType.Value);

            batch = new CompositionBatch();
            batch.RemovePart(key);
            container.Compose(batch);

            Assert.Equal(2, importer.ValueTypeSetCount);
            Assert.Null(importer.ValueType);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void OptionalImportsOfExportNullableValueTypesAreReboundToDefaultWhenExportIsRemoved()
        {
            var container = ContainerFactory.Create();
            var importer = new OptionalExport();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            var key = batch.AddExportedValue<int?>("NullableValueType", 10);

            container.Compose(batch);
            Assert.Equal(1, importer.NullableValueTypeSetCount);
            Assert.Equal(10, importer.NullableValueType.Value);

            batch = new CompositionBatch();
            batch.RemovePart(key);
            container.Compose(batch);

            Assert.Equal(2, importer.NullableValueTypeSetCount);
            Assert.Null(importer.NullableValueType);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public void OptionalImportsOfExportReferenceTypesAreReboundToDefaultWhenExportIsRemoved()
        {
            var container = ContainerFactory.Create();
            var importer = new OptionalExport();

            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            var key = batch.AddExportedValue("ReferenceType", "Bar");

            container.Compose(batch);
            Assert.Equal(1, importer.ReferenceTypeSetCount);
            Assert.Equal("Bar", importer.ReferenceType.Value);

            batch = new CompositionBatch();
            batch.RemovePart(key);
            container.Compose(batch);

            Assert.Equal(2, importer.ReferenceTypeSetCount);
            Assert.Null(importer.ReferenceType);
        }

        public class OptionalImport
        {
            public int ValueTypeSetCount;
            public int NullableValueTypeSetCount;
            public int ReferenceTypeSetCount;

            private int _valueType;
            private int? _nullableValueType;
            private string _referenceType;

            [Import("ValueType", AllowDefault = true, AllowRecomposition = true)]
            public int ValueType
            {
                get { return _valueType; }
                set
                {
                    ValueTypeSetCount++;
                    _valueType = value;
                }
            }

            [Import("NullableValueType", AllowDefault = true, AllowRecomposition = true)]
            public int? NullableValueType
            {
                get { return _nullableValueType; }
                set
                {
                    NullableValueTypeSetCount++;
                    _nullableValueType = value;
                }
            }

            [Import("ReferenceType", AllowDefault = true, AllowRecomposition = true)]
            public string ReferenceType
            {
                get { return _referenceType; }
                set
                {
                    ReferenceTypeSetCount++;
                    _referenceType = value;
                }
            }
        }

        public class OptionalExport
        {
            public int ValueTypeSetCount;
            public int NullableValueTypeSetCount;
            public int ReferenceTypeSetCount;

            private Lazy<int> _valueType;
            private Lazy<int?> _nullableValueType;
            private Lazy<string> _referenceType;

            [Import("ValueType", AllowDefault = true, AllowRecomposition = true)]
            public Lazy<int> ValueType
            {
                get { return _valueType; }
                set
                {
                    ValueTypeSetCount++;
                    _valueType = value;
                }
            }

            [Import("NullableValueType", AllowDefault = true, AllowRecomposition = true)]
            public Lazy<int?> NullableValueType
            {
                get { return _nullableValueType; }
                set
                {
                    NullableValueTypeSetCount++;
                    _nullableValueType = value;
                }
            }

            [Import("ReferenceType", AllowDefault = true, AllowRecomposition = true)]
            public Lazy<string> ReferenceType
            {
                get { return _referenceType; }
                set
                {
                    ReferenceTypeSetCount++;
                    _referenceType = value;
                }
            }
        }

        private class DelayDuckImporter
        {
            [Import("Duck")]
            public Lazy<IDuck> Duck
            {
                get;
                set;
            }
        }

        private class DuckImporter
        {
            [Import("Duck")]
            public IDuck Duck
            {
                get;
                set;
            }
        }

        public class QuackLikeADuck
        {
            public virtual string Quack()
            {
                return "Quack";
            }
        }

        public interface IDuck
        {
            string Quack();
        }
        
        [ComImport]
        [Guid("148BD52A-A2AB-11CE-B11F-00AA00530503")]
        private class CTaskScheduler
        {   // This interface doesn't implement 
            // ITaskScheduler deliberately
        }

        [Guid("148BD527-A2AB-11CE-B11F-00AA00530503")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskScheduler
        {
            void FakeMethod();
        }

        private class ImportComComponent
        {
            [Import("TaskScheduler")]
            public ITaskScheduler TaskScheduler
            {
                get;
                set;
            }
        }

        private class DelayImportComComponent
        {
            [Import("TaskScheduler")]
            public Lazy<ITaskScheduler> TaskScheduler
            {
                get;
                set;
            }
        }
        
        public class Importer
        {
            public Importer()
            {
            }

            [Import("Value")]
            public int ValueReadWrite { get; set; }

            [ImportMany("Value")]
            public IList<int> SingleValueCollectionReadWrite { get; set; }

            [Import("EmptyValue", AllowDefault = true)]
            public int ValueEmptyOptional { get; set; }

            [ImportMany("CollectionValue", typeof(IList<int>))]
            public IList<int> ValueCollection { get; set; }

        }

        public class ImporterNameless
        {

            public ImporterNameless()
            {
            }

            [Import]
            public int ValueReadWrite { get; set; }

            [Import]
            public Lazy<int> MetadataReadWrite { get; set; }

        }

        public class ImporterInvalidWrongType
        {
            [Import("Value")]
            public DateTime ValueReadWrite { get; set; }
        }

        [Export]
        public class ImporterInvalidSetterException
        {
            [ImportMany("Value")]
            public IEnumerable<int> ValueReadWrite { get { return null; } set { throw new InvalidOperationException(); } }
        }

        [Export]
        public class ImportImporterInvalidSetterExceptionLazily
        {
            [Import]
            public Lazy<ImporterInvalidSetterException> Value { get; set; }
        }

        [PartNotDiscoverable]
        public class Exporter
        {
            List<int> collectionValue = new List<int>();

            public Exporter(int value)
            {
                Value = value;
            }

            [Export("Value")]
            public int Value { get; set; }

            [Export("CollectionValue")]
            public IList<int> CollectionValue { get { return collectionValue; } }

        }

        public class ExporterNameless
        {

            public ExporterNameless(int value)
            {
                Value = value;
            }

            [Export]
            public int Value { get; set; }

        }

        public class ExportsString
        {
            [Export]
            public string ExportedString = "Test";
        }

        public class ExportsInvalidListOfExportOfString
        {
            [Export(typeof(List<Lazy<string>>))]
            public string ExportedString = "Test";
        }

        public class ExportsValidListOfExportOfString
        {
            [Export(typeof(List<Lazy<string>>))]
            public List<Lazy<string>> ExportedString = new List<Lazy<string>>();
        }

        [Export]
        public class ImportsListOfExportOfString
        {
            [Import(AllowDefault = true)]
            public List<Lazy<string>> ExportedList { get; set; }
        }

        [Fact]
        public void ImportListOfExportWithOnlySingleElementsAvailable_ShouldNotFindExport()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(ExportsString), typeof(ImportsListOfExportOfString));
            var importer = container.GetExportedValue<ImportsListOfExportOfString>();
            Assert.Null(importer.ExportedList);

            var part = AttributedModelServices.CreatePartDefinition(typeof(ImportsListOfExportOfString), null);
            var contract = AttributedModelServices.GetContractName(typeof(List<Lazy<string>>));
            Assert.Equal(contract, ((ContractBasedImportDefinition)part.ImportDefinitions.First()).ContractName);
        }

        [Fact]
        public void ImportListOfExportWithInvalidCollectionAvailable_ShouldThrowMismatch()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(ExportsInvalidListOfExportOfString), typeof(ImportsListOfExportOfString));

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
                container.GetExportedValue<ImportsListOfExportOfString>());
        }

        [Fact]
        public void ImportListOfExportWithValidCollectionAvailable_ShouldSatisfyImport()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(ExportsValidListOfExportOfString), typeof(ImportsListOfExportOfString));
            var importer = container.GetExportedValue<ImportsListOfExportOfString>();
            Assert.Equal(0, importer.ExportedList.Count);
        }
    }
}
