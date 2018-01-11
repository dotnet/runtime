// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class CompositionContainerCollectionTests
    {
        public class SupportedImportCollectionAssignments<T>
        {
            // Fields
            [ImportMany("Value")]
            public IEnumerable<T> IEnumerableOfTField;

            [ImportMany("Value")]
            public IEnumerable<object> IEnumerableOfObjectField;

            [ImportMany("Value")]
            public T[] ArrayOfTField;

            [ImportMany("Value")]
            public object[] ArrayOfObjectField;

            [ImportMany("Value")]
            public IEnumerable<T> IEnumerableOfTProperty { get; set; }

            [ImportMany("Value")]
            public IEnumerable<object> IEnumerableOfObjectProperty { get; set; }

            [ImportMany("Value")]
            public T[] ArrayOfTProperty { get; set; }

            [ImportMany("Value")]
            public object[] ArrayOfObjectProperty { get; set; }
            
            public void VerifyImports(params T[] expectedValues)
            {
                // Fields
                EnumerableAssert.AreEqual(IEnumerableOfTField, expectedValues);
                EnumerableAssert.AreEqual(IEnumerableOfObjectField, expectedValues.Cast<object>());
                EnumerableAssert.AreEqual(ArrayOfTField, expectedValues);
                EnumerableAssert.AreEqual(ArrayOfObjectField, expectedValues.Cast<object>());

                // Properties
                EnumerableAssert.AreEqual(IEnumerableOfTProperty, expectedValues);
                EnumerableAssert.AreEqual(IEnumerableOfObjectProperty, expectedValues.Cast<object>());
                EnumerableAssert.AreEqual(ArrayOfTProperty, expectedValues);
                EnumerableAssert.AreEqual(ArrayOfObjectProperty, expectedValues.Cast<object>());
            }
        }

        [Fact]
        public void ValidateSupportedImportCollectionAssignments()
        {
            var container = ContainerFactory.Create();
            var batch = new CompositionBatch();

            var importer = new SupportedImportCollectionAssignments<int>();

            batch.AddPart(importer);
            batch.AddExportedValue("Value", 21);
            batch.AddExportedValue("Value", 32);
            batch.AddExportedValue("Value", 43);

            container.Compose(batch);

            importer.VerifyImports(21, 32, 43);
        }

        public class SupportedImportCollectionMutation<T>
        {
            public SupportedImportCollectionMutation()
            {
                ICollectionOfTReadOnlyField = new List<T>();
                ListOfTReadOnlyField = new List<T>();
                CollectionOfTField = new Collection<T>();
                CollectionOfTReadOnlyField = new Collection<T>();

                _iCollectionOfTReadOnlyProperty = new List<T>();
                _listOfTReadOnlyProperty = new List<T>();
                CollectionOfTProperty = new Collection<T>();
                _collectionOfTReadOnlyProperty = new Collection<T>();
                
                ObservableCollectionOfTReadOnlyField = new ObservableCollection<T>();
                _observableCollectionOfTReadOnlyProperty = new ObservableCollection<T>();
            }

            [ImportMany("Value")]
            public readonly ICollection<T> ICollectionOfTReadOnlyField;

            [ImportMany("Value")]
            public List<T> ListOfTField;

            [ImportMany("Value")]
            public readonly List<T> ListOfTReadOnlyField;

            [ImportMany("Value")]
            public Collection<T> CollectionOfTField;
            
            [ImportMany("Value")]
            public Collection<object> CollectionOfObjectField;

            [ImportMany("Value")]
            public readonly Collection<T> CollectionOfTReadOnlyField;

            [ImportMany("Value")]
            public ICollection<T> ICollectionOfTReadOnlyProperty { get { return _iCollectionOfTReadOnlyProperty; } }
            private ICollection<T> _iCollectionOfTReadOnlyProperty;

            [ImportMany("Value")]
            public List<T> ListOfTProperty { get; set; }

            [ImportMany("Value")]
            public List<T> ListOfTReadOnlyProperty { get { return _listOfTReadOnlyProperty; } }
            private readonly List<T> _listOfTReadOnlyProperty;

            [ImportMany("Value")]
            public Collection<T> CollectionOfTProperty { get; set; }

            [ImportMany("Value")]
            public Collection<T> CollectionOfTReadOnlyProperty { get { return _collectionOfTReadOnlyProperty; } }
            private readonly Collection<T> _collectionOfTReadOnlyProperty;
            
            [ImportMany("Value")]
            public ObservableCollection<T> ObservableCollectionOfTField;

            [ImportMany("Value")]
            public readonly ObservableCollection<T> ObservableCollectionOfTReadOnlyField;

            [ImportMany("Value")]
            public ObservableCollection<T> ObservableCollectionOfTProperty { get; set; }

            [ImportMany("Value")]
            public ObservableCollection<T> ObservableCollectionOfTReadOnlyProperty { get { return _observableCollectionOfTReadOnlyProperty; } }
            private readonly ObservableCollection<T> _observableCollectionOfTReadOnlyProperty;

            public void VerifyImports(params T[] expectedValues)
            {
                EnumerableAssert.AreEqual(ICollectionOfTReadOnlyField, expectedValues);
                EnumerableAssert.AreEqual(ListOfTField, expectedValues);
                EnumerableAssert.AreEqual(ListOfTReadOnlyField, expectedValues);
                EnumerableAssert.AreEqual(CollectionOfTField, expectedValues);
                EnumerableAssert.AreEqual(CollectionOfTReadOnlyField, expectedValues);

                EnumerableAssert.AreEqual(ICollectionOfTReadOnlyProperty, expectedValues);
                EnumerableAssert.AreEqual(ListOfTProperty, expectedValues);
                EnumerableAssert.AreEqual(ListOfTReadOnlyProperty, expectedValues);
                EnumerableAssert.AreEqual(CollectionOfTProperty, expectedValues);
                EnumerableAssert.AreEqual(CollectionOfTReadOnlyProperty, expectedValues);

                EnumerableAssert.AreEqual(ObservableCollectionOfTField, expectedValues);
                EnumerableAssert.AreEqual(ObservableCollectionOfTReadOnlyField, expectedValues);
                EnumerableAssert.AreEqual(ObservableCollectionOfTProperty, expectedValues);
                EnumerableAssert.AreEqual(ObservableCollectionOfTReadOnlyProperty, expectedValues);
            }
        }

        [Fact]
        public void ValidateSupportedImportCollectionMutation()
        {
            var container = ContainerFactory.Create();
            var batch = new CompositionBatch();

            var importer = new SupportedImportCollectionMutation<int>();

            batch.AddPart(importer);
            batch.AddExportedValue("Value", 21);
            batch.AddExportedValue("Value", 32);
            batch.AddExportedValue("Value", 43);

            container.Compose(batch);

            importer.VerifyImports(21, 32, 43);
        }

        public class ImportCollectionNullValue
        {
            [ImportMany("Value")]
            public List<int> NullValue { get; set; }
        }

        public class NamelessImporter
        {
            [ImportMany]
            public int[] ReadWriteIList { get; set; }

            [ImportMany]
            public Collection<Lazy<int>> ReadWriteMetadata { get; set; }
        }

        public class NamelessExporter
        {
            public NamelessExporter(int value)
            {
                Value = value;
            }

            [Export]
            public int Value { get; set; }
        }

        [Fact]
        public void ImportCollectionsNameless()
        {
            // Verifing that the contract name gets the correct value
            var container = ContainerFactory.Create();
            NamelessImporter importer = new NamelessImporter();
            NamelessExporter exporter42 = new NamelessExporter(42);
            NamelessExporter exporter0 = new NamelessExporter(0);

            CompositionBatch batch = new CompositionBatch();
            batch.AddParts(importer, exporter42, exporter0);
            container.Compose(batch);

            EnumerableAssert.AreEqual(importer.ReadWriteIList, 42, 0);
        }

        public class InvalidImporterReadOnlyEnumerable
        {
            IEnumerable<int> readOnlyEnumerable = new List<int>();

            [ImportMany("Value")]
            public IEnumerable<int> ReadOnlyEnumerable
            {
                get
                {
                    return readOnlyEnumerable;
                }
            }
        }

        [Fact]
        public void ImportCollectionsExceptionReadOnlyEnumerable()
        {
            ExpectedErrorOnPartActivate(new InvalidImporterReadOnlyEnumerable(),
                ErrorId.ReflectionModel_ImportCollectionNotWritable);  
        }

        public class ImporterWriteOnlyExportCollection
        {
            [ImportMany("Value")]
            public Collection<Lazy<int>> WriteOnlyExportCollection
            {
                set { PublicExportCollection = value; }
            }

            public Collection<Lazy<int>> PublicExportCollection { get; set; }
        }

        [Fact]
        public void ImportCollections_WriteOnlyExportCollection()
        {
            var container = ContainerFactory.Create();
            var batch = new CompositionBatch();

            var importer = new ImporterWriteOnlyExportCollection();

            List<int> values = new List<int>() { 21, 32, 43 };

            batch.AddPart(importer);
            values.ForEach(v => batch.AddExportedValue("Value", v));

            container.Compose(batch);

            EnumerableAssert.AreEqual(values, importer.PublicExportCollection.Select(export => export.Value));            
        }

        public class ImporterWriteOnlyIEnumerableOfT
        {
            [ImportMany("Value")]
            public IEnumerable<int> WriteOnlyIEnumerable
            {
                set { PublicIEnumerable = value; }
            }

            public IEnumerable<int> PublicIEnumerable { get; set; }
        }

        [Fact]
        public void ImportCollections_WriteOnlyIEnumerableOfT()
        {
            var container = ContainerFactory.Create();
            var batch = new CompositionBatch();

            var importer = new ImporterWriteOnlyIEnumerableOfT();

            List<int> values = new List<int>() { 21, 32, 43 };

            batch.AddPart(importer);
            values.ForEach(v => batch.AddExportedValue("Value", v));

            container.Compose(batch);

            EnumerableAssert.AreEqual(values, importer.PublicIEnumerable);
        }

        public class ImporterWriteOnlyArray
        {
            [ImportMany("Value")]
            public int[] WriteOnlyArray
            {
                set { PublicArray = value; }
            }

            public int[] PublicArray { get; set; }
        }

        [Fact]
        public void ImportCollections_WriteOnlyArray()
        {
            var container = ContainerFactory.Create();
            var batch = new CompositionBatch();

            var importer = new ImporterWriteOnlyArray();

            List<int> values = new List<int>() { 21, 32, 43 };

            batch.AddPart(importer);
            values.ForEach(v => batch.AddExportedValue("Value", v));

            container.Compose(batch);

            EnumerableAssert.AreEqual(values, importer.PublicArray);
        }

        public class InvalidImporterNonCollection
        {
            [Import("Value")]
            public int Value { get; set; }
        }

        [Fact]
        public void ImportCollectionsExceptionNonCollection()
        {
            ExpectedChangeRejectedErrorOnSetImport(new InvalidImporterNonCollection(),
                ErrorId.ImportEngine_ImportCardinalityMismatch);
        }

        public class InvalidImporterNonAssignableCollection
        {
            [ImportMany("Value", typeof(int))]
            public IEnumerable<string> StringCollection { get; set; }
        }

        [Fact]
        public void ImportCollectionsExceptionNonAssignableCollection()
        {
            ExpectedErrorOnSetImport(new InvalidImporterNonAssignableCollection(),
                ErrorId.ReflectionModel_ImportNotAssignableFromExport);  
        }

        public class InvalidImporterNullReadOnlyICollection
        {
            ICollection<int> readOnlyICollection = null;

            [ImportMany("Value")]
            public ICollection<int> Values { get { return readOnlyICollection; } }
        }

        [Fact]
        public void ImportCollectionsExceptionNullReadOnlyICollection()
        {
            ExpectedErrorOnPartActivate(new InvalidImporterNullReadOnlyICollection(),
                ErrorId.ReflectionModel_ImportCollectionNull);   
        }

        public class ImporterWeakIEnumerable
        {
            public ImporterWeakIEnumerable()
            {
                ReadWriteEnumerable = new IntCollection();
            }

            [ImportMany("Value")]
            public IntCollection ReadWriteEnumerable { get; set; }

            public class IntCollection : IEnumerable
            {
                List<int> ints = new List<int>();
                public void Add(int item) { ints.Add(item); }
                public void Clear() { ints.Clear(); }
                public bool Remove(int item) { return ints.Remove(item); }
                public IEnumerator GetEnumerator() { return ints.GetEnumerator(); }
            }
        }

        [Fact]
        public void ImportCollectionsExceptionWeakCollectionNotSupportingICollectionOfT()
        {
            ExpectedErrorOnPartActivate(new ImporterWeakIEnumerable(),
                ErrorId.ReflectionModel_ImportCollectionNotWritable);
        }

        public class ImporterThrowsOnGetting
        {
            [ImportMany("Value")]
            public List<int> Value
            {
                get
                {
                    throw new NotSupportedException();
                }
            }
        }

        [Fact]
        public void ImportCollectionsExceptionGettingValue()
        {
            var container = ContainerFactory.Create();
            ImporterThrowsOnGetting importer = new ImporterThrowsOnGetting();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(importer);
            batch.AddExportedValue("Value", 42);
            batch.AddExportedValue("Value", 0);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotActivate,
                                          ErrorId.ReflectionModel_ImportCollectionGetThrewException, RetryMode.DoNotRetry, () =>
                                          {
                                              container.Compose(batch);
                                          });
        }

        public class CustomCollectionThrowsDuringConstruction : Collection<int>
        {
            public CustomCollectionThrowsDuringConstruction()
            {
                throw new NotSupportedException();
            }
        }

        public class ImportCustomCollectionThrowsDuringConstruction
        {
            public ImportCustomCollectionThrowsDuringConstruction()
            {
            }

            [ImportMany("Value")]
            public CustomCollectionThrowsDuringConstruction Values { get; set; }
        }

        [Fact]
        public void ImportCollections_ImportTypeThrowsOnConstruction()
        {
            ExpectedErrorOnPartActivate(new ImportCustomCollectionThrowsDuringConstruction(),
                ErrorId.ReflectionModel_ImportCollectionConstructionThrewException);
        }

        public class CustomCollectionThrowsDuringClear : Collection<int>
        {
            protected override void ClearItems()
            {
                throw new NotSupportedException();
            }
        }

        public class ImportCustomCollectionThrowsDuringClear
        {
            public ImportCustomCollectionThrowsDuringClear()
            {
            }

            [ImportMany("Value")]
            public CustomCollectionThrowsDuringClear Values { get; set; }
        }

        [Fact]
        public void ImportCollections_ImportTypeThrowsOnClear()
        {
            ExpectedErrorOnPartActivate(new ImportCustomCollectionThrowsDuringClear(),
                ErrorId.ReflectionModel_ImportCollectionClearThrewException);
        }

        public class CustomCollectionThrowsDuringAdd : Collection<int>
        {
            protected override void InsertItem(int index, int item)
            {
                throw new NotSupportedException();
            }
        }

        public class ImportCustomCollectionThrowsDuringAdd
        {
            public ImportCustomCollectionThrowsDuringAdd()
            {
            }

            [ImportMany("Value")]
            public CustomCollectionThrowsDuringAdd Values { get; set; }
        }

        [Fact]
        public void ImportCollections_ImportTypeThrowsOnAdd()
        {
            ExpectedErrorOnPartActivate(new ImportCustomCollectionThrowsDuringAdd(),
                ErrorId.ReflectionModel_ImportCollectionAddThrewException);
        }

        public class CustomCollectionThrowsDuringIsReadOnly : ICollection<int>
        {
            void ICollection<int>.Add(int item)
            {
                throw new NotImplementedException();
            }

            void ICollection<int>.Clear()
            {
                throw new NotImplementedException();
            }

            bool ICollection<int>.Contains(int item)
            {
                throw new NotImplementedException();
            }

            void ICollection<int>.CopyTo(int[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            int ICollection<int>.Count
            {
                get { throw new NotImplementedException(); }
            }

            bool ICollection<int>.IsReadOnly
            {
                get { throw new NotSupportedException(); }
            }

            bool ICollection<int>.Remove(int item)
            {
                throw new NotImplementedException();
            }

            IEnumerator<int> IEnumerable<int>.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        public class ImportCustomCollectionThrowsDuringIsReadOnly
        {
            public ImportCustomCollectionThrowsDuringIsReadOnly()
            {
                Values = new CustomCollectionThrowsDuringIsReadOnly();
            }

            [ImportMany("Value")]
            public CustomCollectionThrowsDuringIsReadOnly Values { get; set; }
        }

        [Fact]
        public void ImportCollections_ImportTypeThrowsOnIsReadOnly()
        {
            ExpectedErrorOnPartActivate(new ImportCustomCollectionThrowsDuringIsReadOnly(),
                ErrorId.ReflectionModel_ImportCollectionIsReadOnlyThrewException);
        }

        public class CollectionTypeWithNoIList<T> : ICollection<T>
        {
            private int _count = 0;
            public CollectionTypeWithNoIList()
            {
                
            }

            public void Add(T item)
            {
                // Do Nothing
                this._count++;
            }

            public void Clear()
            {
                // Do Nothings
            }

            public bool Contains(T item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public int Count
            {
                get { return this._count; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public bool Remove(T item)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<T> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        public class ImportCollectionWithNoIList
        {
            [ImportMany("Value")]
            public CollectionTypeWithNoIList<int> Values { get; set; }
        }

        [Fact]
        public void ImportCollections_NoIList_ShouldWorkFine()
        {
            var container = ContainerFactory.Create();
            var batch = new CompositionBatch();
            var importer = new ImportCollectionWithNoIList();
            batch.AddPart(importer);
            batch.AddExportedValue("Value", 42);
            batch.AddExportedValue("Value", 0);

            container.Compose(batch);

            Assert.Equal(2, importer.Values.Count);
        }

        public class CollectionWithMultipleInterfaces :  ICollection<int>, ICollection<string>
        {
            public CollectionWithMultipleInterfaces()
            {

            }

            #region ICollection<int> Members

            void ICollection<int>.Add(int item)
            {
                throw new NotImplementedException();
            }

            void ICollection<int>.Clear()
            {
                throw new NotImplementedException();
            }

            bool ICollection<int>.Contains(int item)
            {
                throw new NotImplementedException();
            }

            void ICollection<int>.CopyTo(int[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            int ICollection<int>.Count
            {
                get { throw new NotImplementedException(); }
            }

            bool ICollection<int>.IsReadOnly
            {
                get { throw new NotImplementedException(); }
            }

            bool ICollection<int>.Remove(int item)
            {
                throw new NotImplementedException();
            }

            IEnumerator<int> IEnumerable<int>.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            #endregion

            #region ICollection<string> Members

            void ICollection<string>.Add(string item)
            {
                throw new NotImplementedException();
            }

            void ICollection<string>.Clear()
            {
                throw new NotImplementedException();
            }

            bool ICollection<string>.Contains(string item)
            {
                throw new NotImplementedException();
            }

            void ICollection<string>.CopyTo(string[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            int ICollection<string>.Count
            {
                get { throw new NotImplementedException(); }
            }

            bool ICollection<string>.IsReadOnly
            {
                get { throw new NotImplementedException(); }
            }

            bool ICollection<string>.Remove(string item)
            {
                throw new NotImplementedException();
            }

            IEnumerator<string> IEnumerable<string>.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        public class ImportCollectionWithMultipleInterfaces
        {
            [ImportMany("Value")]
            public CollectionWithMultipleInterfaces Values { get; set; }
        }
        
        [Fact]
        public void ImportCollections_MultipleICollections_ShouldCauseNotWriteable()
        {
            ExpectedErrorOnPartActivate(new ImportCollectionWithMultipleInterfaces(),
                ErrorId.ReflectionModel_ImportCollectionNotWritable);
        }

        public class ImportManyNonCollectionTypeString
        {
            [ImportMany("Value")]
            public string Foo { get; set; }
        }

        [Fact]
        public void ImportManyOnNonCollectionTypeString_ShouldCauseNotWritable()
        {
            ExpectedErrorOnPartActivate(new ImportManyNonCollectionTypeString(),
                ErrorId.ReflectionModel_ImportCollectionNotWritable);
        }

        public class ImportManyNonCollectionTypeObject
        {
            [ImportMany("Value")]
            public object Foo { get; set; }
        }

        [Fact]
        public void ImportManyOnNonCollectionTypeObject_ShouldCauseNotWritable()
        {
            ExpectedErrorOnPartActivate(new ImportManyNonCollectionTypeObject(),
                ErrorId.ReflectionModel_ImportCollectionNotWritable);
        }

        public class ExportADictionaryObject
        {
            [Export]
            public IDictionary<string, object> MyDictionary
            {
                get
                {
                    var dictionary = new Dictionary<string, object>();
                    dictionary.Add("a", 42);
                    dictionary.Add("b", "c");
                    return dictionary;
                }
            }
        }

        public class ImportADictionaryObject
        {
            [Import]
            public IDictionary<string, object> MyDictionary { get; set; }
        }

        [Fact]
        public void ImportDictionaryAsSingleObject()
        {
            var container = ContainerFactory.Create();
            var batch = new CompositionBatch();
            var importer = new ImportADictionaryObject();
            var exporter = new ExportADictionaryObject();

            batch.AddPart(importer);
            batch.AddPart(exporter);
            container.Compose(batch);

            Assert.Equal(2, importer.MyDictionary.Count);
        }

        public class ExportACollectionObject
        {
            [Export]
            public Collection<string> MyCollection
            {
                get
                {
                    var collection = new Collection<string>();
                    collection.Add("a");
                    collection.Add("b");
                    return collection;
                }
            }
        }

        public class ImportACollectionObject
        {
            [Import]
            public Collection<string> MyCollection { get; set; }
        }

        [Fact]
        public void ImportCollectionAsSingleObject()
        {
            var container = ContainerFactory.Create();
            var batch = new CompositionBatch();
            var importer = new ImportACollectionObject();
            var exporter = new ExportACollectionObject();

            batch.AddPart(importer);
            batch.AddPart(exporter);
            container.Compose(batch);

            Assert.Equal(2, importer.MyCollection.Count);
        }

        public void ExpectedErrorOnPartActivate(object importer, ErrorId expectedErrorId)
        {
            var container = ContainerFactory.Create();
            var batch = new CompositionBatch();
            batch.AddPart(importer);
            batch.AddExportedValue("Value", 42);
            batch.AddExportedValue("Value", 0);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotActivate,
              expectedErrorId, RetryMode.DoNotRetry, () =>
              {
                  container.Compose(batch);
              });
        }

        public void ExpectedErrorOnSetImport(object importer, ErrorId expectedErrorId)
        {
            var container = ContainerFactory.Create();
            var batch = new CompositionBatch();
            batch.AddPart(importer);
            batch.AddExportedValue("Value", 42);
            batch.AddExportedValue("Value", 0);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotSetImport,
              expectedErrorId, RetryMode.DoNotRetry, () =>
              {
                  container.Compose(batch);
              });
        }

        public void ExpectedChangeRejectedErrorOnSetImport(object importer, ErrorId expectedErrorId)
        {
            var container = ContainerFactory.Create();
            var batch = new CompositionBatch();
            batch.AddPart(importer);
            batch.AddExportedValue("Value", 42);
            batch.AddExportedValue("Value", 0);

            CompositionAssert.ThrowsChangeRejectedError(ErrorId.ImportEngine_PartCannotSetImport,
              expectedErrorId, RetryMode.DoNotRetry, () =>
              {
                  container.Compose(batch);
              });
        }
    }
}
