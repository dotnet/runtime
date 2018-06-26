// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class FilteringCollection<T, M> : AdaptingCollection<T, M>
    {
        public FilteringCollection(Func<Lazy<T, M>, bool> filter)
            : base(e => e.Where(filter))
        {
        }
    }

    public class OrderingCollection<T, M> : AdaptingCollection<T, M>
    {
        public OrderingCollection(Func<Lazy<T, M>, object> keySelector)
            : this(keySelector, false)
        {
        }

        public OrderingCollection(Func<Lazy<T, M>, object> keySelector, bool descending)
            : base(e => descending ? e.OrderByDescending(keySelector) : e.OrderBy(keySelector))
        {
        }
    }

    public class AdaptingCollection<T> : AdaptingCollection<T, IDictionary<string, object>>
    {
        public AdaptingCollection(Func<IEnumerable<Lazy<T, IDictionary<string, object>>>,
                                       IEnumerable<Lazy<T, IDictionary<string, object>>>> adaptor)
            : base(adaptor)
        {
        }
    }

    public class AdaptingCollection<T, M> : ICollection<Lazy<T, M>>, INotifyCollectionChanged
    {
        private readonly List<Lazy<T, M>> _allItems = new List<Lazy<T, M>>();
        private readonly Func<IEnumerable<Lazy<T, M>>, IEnumerable<Lazy<T, M>>> _adaptor = null;
        private List<Lazy<T, M>> _adaptedItems = null;

        public AdaptingCollection() : this(null)
        {
        }

        public AdaptingCollection(Func<IEnumerable<Lazy<T, M>>, IEnumerable<Lazy<T, M>>> adaptor)
        {
            this._adaptor = adaptor;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void ReapplyAdaptor()
        {
            if (this._adaptedItems != null)
            {
                this._adaptedItems = null;
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        protected virtual IEnumerable<Lazy<T, M>> Adapt(IEnumerable<Lazy<T, M>> collection)
        {
            if (this._adaptor != null)
            {
                return this._adaptor.Invoke(collection);
            }

            return collection;
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler collectionChanged = this.CollectionChanged;

            if (collectionChanged != null)
            {
                collectionChanged.Invoke(this, e);
            }
        }

        private List<Lazy<T, M>> AdaptedItems
        {
            get
            {
                if (this._adaptedItems == null)
                {
                    this._adaptedItems = Adapt(this._allItems).ToList();
                }

                return this._adaptedItems;
            }
        }

        #region ICollection Implementation
        // Accessors work directly against adapted collection
        public bool Contains(Lazy<T, M> item)
        {
            return this.AdaptedItems.Contains(item);
        }

        public void CopyTo(Lazy<T, M>[] array, int arrayIndex)
        {
            this.AdaptedItems.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.AdaptedItems.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public IEnumerator<Lazy<T, M>> GetEnumerator()
        {
            return this.AdaptedItems.GetEnumerator();
        }

        Collections.IEnumerator Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        // Mutation methods work against complete collection
        // and then force a reset of the adapted collection
        public void Add(Lazy<T, M> item)
        {
            this._allItems.Add(item);
            ReapplyAdaptor();
        }

        public void Clear()
        {
            this._allItems.Clear();
            ReapplyAdaptor();
        }

        public bool Remove(Lazy<T, M> item)
        {
            bool removed = this._allItems.Remove(item);
            ReapplyAdaptor();
            return removed;
        }
        #endregion
    }

    public class AdaptingCollectionTests
    {
        public interface IContract { }
        public interface INetworkAwareMetadata
        {
            [DefaultValue(false)]
            bool RequiresOnline { get; }
        }

        [Export(typeof(IContract))]
        [ExportMetadata("RequiresOnline", true)]
        public class NetworkExport : IContract { }

        [Export(typeof(IContract))]
        public class NonNetworkExport : IContract { }

        public class FilterExports
        {
            public FilterExports()
            {
                this.OnlineOnly = new AdaptingCollection<IContract, INetworkAwareMetadata>(e =>
                    e.Where(p => p.Metadata.RequiresOnline));

                this.OnlineOnly2 = new FilteringCollection<IContract, INetworkAwareMetadata>(p => p.Metadata.RequiresOnline);
            }

            [ImportMany]
            public AdaptingCollection<IContract, INetworkAwareMetadata> OnlineOnly { get; set; }

            [ImportMany]
            public FilteringCollection<IContract, INetworkAwareMetadata> OnlineOnly2 { get; set; }
        }

        [Fact]
        public void TestFilteringImports()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(NetworkExport), typeof(NonNetworkExport));

            var filterExports = new FilterExports();
            container.ComposeParts(filterExports);

            Assert.Equal(1, filterExports.OnlineOnly.Count);
            Assert.Equal(1, filterExports.OnlineOnly2.Count);
        }

        public interface IOrderMetadata
        {
            [DefaultValue(int.MaxValue)]
            int Order { get; }
        }

        [Export(typeof(IContract))]
        [ExportMetadata("Order", 2)]
        public class BExport : IContract { }

        [Export(typeof(IContract))]
        [ExportMetadata("Order", 1)]
        public class AExport : IContract { }

        [Export(typeof(IContract))]
        public class CExport : IContract { }

        public class OrderExportsByMetadata
        {
            public OrderExportsByMetadata()
            {
                this.OrderedItems = new AdaptingCollection<IContract, IOrderMetadata>(e =>
                    e.OrderBy(p => p.Metadata.Order));

                this.OrderedItems2 = new OrderingCollection<IContract, IOrderMetadata>(p => p.Metadata.Order);
            }

            [ImportMany]
            public AdaptingCollection<IContract, IOrderMetadata> OrderedItems { get; set; }

            [ImportMany]
            public OrderingCollection<IContract, IOrderMetadata> OrderedItems2 { get; set; }
        }

        [Fact]
        public void TestOrderingImportsByMetadata()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(BExport), typeof(AExport), typeof(CExport));
            var orderExports = new OrderExportsByMetadata();

            container.ComposeParts(orderExports);

            Assert.IsType<AExport>(orderExports.OrderedItems.ElementAt(0).Value);
            Assert.IsType<BExport>(orderExports.OrderedItems.ElementAt(1).Value);
            Assert.IsType<CExport>(orderExports.OrderedItems.ElementAt(2).Value);

            Assert.IsType<AExport>(orderExports.OrderedItems2.ElementAt(0).Value);
            Assert.IsType<BExport>(orderExports.OrderedItems2.ElementAt(1).Value);
            Assert.IsType<CExport>(orderExports.OrderedItems2.ElementAt(2).Value);
        }

        public class OrderExportsByName
        {
            public OrderExportsByName(bool descending)
            {
                if (descending)
                {
                    this.OrderedItems = new AdaptingCollection<IContract>(e =>
                        e.OrderByDescending(p => p.Value.GetType().FullName));
                }
                else
                {
                    this.OrderedItems = new AdaptingCollection<IContract>(e =>
                        e.OrderBy(p => p.Value.GetType().FullName));
                }
            }

            [ImportMany]
            public AdaptingCollection<IContract> OrderedItems { get; set; }
        }

        [Fact]
        public void TestOrderingImportsByTypeName()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(BExport), typeof(AExport), typeof(CExport));
            var orderExports = new OrderExportsByName(false);

            container.ComposeParts(orderExports);

            Assert.IsType<AExport>(orderExports.OrderedItems.ElementAt(0).Value);
            Assert.IsType<BExport>(orderExports.OrderedItems.ElementAt(1).Value);
            Assert.IsType<CExport>(orderExports.OrderedItems.ElementAt(2).Value);

            orderExports = new OrderExportsByName(true);

            container.ComposeParts(orderExports);

            Assert.IsType<CExport>(orderExports.OrderedItems.ElementAt(0).Value);
            Assert.IsType<BExport>(orderExports.OrderedItems.ElementAt(1).Value);
            Assert.IsType<AExport>(orderExports.OrderedItems.ElementAt(2).Value);
        }

        public interface IDynamicFilteredMetadata
        {
            bool Dynamic { get; }
        }

        [Export(typeof(IContract))]
        [ExportMetadata("Dynamic", true)]
        public class Dynamic1 : IContract { }

        [Export(typeof(IContract))]
        [ExportMetadata("Dynamic", true)]
        public class Dynamic2 : IContract { }

        [Export(typeof(IContract))]
        [ExportMetadata("Dynamic", false)]
        public class NonDynamic1 : IContract { }

        public class DynamicFilteredCollection<T, M> : AdaptingCollection<T, M> where M : IDynamicFilteredMetadata
        {
            public DynamicFilteredCollection()
            {
            }

            private bool _includeDynamic = false;
            public bool IncludeDynamic
            {
                get { return this._includeDynamic; }
                set
                {
                    if (this._includeDynamic != value)
                    {
                        this.ReapplyAdaptor();
                    }

                    this._includeDynamic = value;
                }
            }

            protected override IEnumerable<Lazy<T, M>> Adapt(IEnumerable<Lazy<T, M>> collection)
            {
                return collection.Where(p => !p.Metadata.Dynamic || IncludeDynamic);
            }
        }

        public class DynamicExports
        {
            [ImportMany]
            public DynamicFilteredCollection<IContract, IDynamicFilteredMetadata> DynamicCollection { get; set; }
        }

        [Fact]
        [ActiveIssue(25498, TargetFrameworkMonikers.UapAot)]
        public void TestDyamicallyFilteringImports()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(Dynamic1), typeof(Dynamic2), typeof(NonDynamic1));
            var dynamicExports = new DynamicExports();

            container.ComposeParts(dynamicExports);

            Assert.Equal(1, dynamicExports.DynamicCollection.Count);

            dynamicExports.DynamicCollection.IncludeDynamic = true;

            Assert.Equal(3, dynamicExports.DynamicCollection.Count);
        }

        public class DynamicExportsNoSubType
        {
            public DynamicExportsNoSubType()
            {
                this.DynamicCollection = new AdaptingCollection<IContract, IDynamicFilteredMetadata>(e =>
                    e.Where(p => !p.Metadata.Dynamic || this.IncludeDynamic));
            }

            private bool _includeDynamic = false;
            public bool IncludeDynamic
            {
                get { return this._includeDynamic; }
                set
                {
                    if (this._includeDynamic != value)
                    {
                        this.DynamicCollection.ReapplyAdaptor();
                    }

                    this._includeDynamic = value;
                }
            }

            [ImportMany]
            public AdaptingCollection<IContract, IDynamicFilteredMetadata> DynamicCollection { get; set; }
        }

        [Fact]
        public void TestDyamicallyFilteringNoSubTypeImports()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(typeof(Dynamic1), typeof(Dynamic2), typeof(NonDynamic1));
            var dynamicExports = new DynamicExportsNoSubType();

            container.ComposeParts(dynamicExports);

            Assert.Equal(1, dynamicExports.DynamicCollection.Count);

            dynamicExports.IncludeDynamic = true;

            Assert.Equal(3, dynamicExports.DynamicCollection.Count);
        }
    }
}
