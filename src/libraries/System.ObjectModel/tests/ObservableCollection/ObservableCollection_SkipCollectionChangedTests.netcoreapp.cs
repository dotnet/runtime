using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.ObjectModel.Tests.ObservableCollection
{
    public class ObservableCollection_SkipCollectionChangedTests
    {
        [Fact]
        public void SkipCollectionChanged_AddRange_Test()
        {
            int collectionChangedCounter = 0;
            NonNullObservableCollection<string> collection = new NonNullObservableCollection<string>();
            collection.Add("1");
            collection.Add("2");
            collection.Add("3");
            collection.CollectionChanged += (s, e) => collectionChangedCounter++;

            Assert.Throws<ArgumentNullException>(() => collection.AddRange(new string[1]));
            Assert.Equal(0, collectionChangedCounter);

            collection.Add("4");
            Assert.Equal(1, collectionChangedCounter);
        }

        [Fact]
        public void SkipCollectionChanged_InsertRange_Test()
        {
            int collectionChangedCounter = 0;
            NonNullObservableCollection<string> collection = new NonNullObservableCollection<string>();
            collection.Add("1");
            collection.Add("2");
            collection.Add("3");
            collection.CollectionChanged += (s, e) => collectionChangedCounter++;

            Assert.Throws<ArgumentNullException>(() => collection.InsertRange(0, new string[1]));
            Assert.Equal(0, collectionChangedCounter);

            collection.Add("4");
            Assert.Equal(1, collectionChangedCounter);
        }

        [Fact]
        public void SkipCollectionChanged_RemoveRange_Test()
        {
            int collectionChangedCounter = 0;
            NonNullObservableCollection<string> collection = new NonNullObservableCollection<string>();
            collection.Add("1");
            collection.Add("2");
            collection.Add("3");
            collection.CollectionChanged += (s, e) => collectionChangedCounter++;

            collection.RemoveRange(0, 2);
            Assert.Equal(1, collectionChangedCounter);

            collection.Add("1");
            Assert.Equal(2, collectionChangedCounter);
        }

        [Fact]
        public void SkipCollectionChanged_RemoveRange_NoEventsRaised_Test()
        {
            int collectionChangedCounter = 0;
            NonNullObservableCollection<string> collection = new NonNullObservableCollection<string>();
            collection.Add("1");
            collection.Add("2");
            collection.Add("3");
            collection.CollectionChanged += (s, e) => collectionChangedCounter++;

            collection.RemoveRange(0, 0);

            Assert.Equal(0, collectionChangedCounter);
        }

        [Fact]
        public void SkipCollectionChanged_ReplaceRange_Test()
        {
            int collectionChangedCounter = 0;
            NonNullObservableCollection<string> collection = new NonNullObservableCollection<string>();
            collection.Add("1");
            collection.Add("2");
            collection.Add("3");
            collection.CollectionChanged += (s, e) => collectionChangedCounter++;

            Assert.Throws<ArgumentNullException>(() => collection.ReplaceRange(0, 2, new string[1]));
            Assert.Equal(0, collectionChangedCounter);

            collection.Add("1");
            Assert.Equal(1, collectionChangedCounter);
        }

        [Fact]
        public void SkipCollectionChanged_ReplaceRange_Empty_Test()
        {
            int collectionChangedCounter = 0;
            NonNullObservableCollection<string> collection = new NonNullObservableCollection<string>();
            collection.Add("1");
            collection.Add("2");
            collection.Add("3");
            collection.CollectionChanged += (s, e) => collectionChangedCounter++;

            collection.ReplaceRange(0, 0, new string[0]);
            Assert.Equal(0, collectionChangedCounter);

            collection.Add("1");
            Assert.Equal(1, collectionChangedCounter);
        }

        public class NonNullObservableCollection<T> : ObservableCollection<T>
        {

            public NonNullObservableCollection() : base() { }
            public NonNullObservableCollection(List<T> list) : base(list) { }

            protected override void InsertItem(int index, T item)
            {
                if (item == null)
                {
                    throw new ArgumentNullException();
                }

                base.InsertItem(index, item);
            }
        }
    }
}
