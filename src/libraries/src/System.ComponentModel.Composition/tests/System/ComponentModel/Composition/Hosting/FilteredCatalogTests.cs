// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition.Hosting
{
    public class FilteredCatalogTests
    {
        [Fact]
        public void Constructor_ThrowsOnNullCatalog()
        {
            Assert.Throws<ArgumentNullException>("catalog", () =>
            {
                new FilteredCatalog(null, p => true);
            });
        }

        [Fact]
        public void Constructor_ThrowsOnNullFilter()
        {
            Assert.Throws<ArgumentNullException>("filter", () =>
            {
                new FilteredCatalog(CreateCatalog(), null);
            });
        }

        [Fact]
        public void Parts_Throws_WhenDisposed()
        {
            var originalCatalog = this.CreateCatalog();
            FilteredCatalog catalog = new FilteredCatalog(originalCatalog, p => p.Exports<IContract1>());
            catalog.Dispose();

            ExceptionAssert.ThrowsDisposed(catalog, () =>
            {
                var p = catalog.Parts;
            });
        }

        [Fact]
        public void Parts()
        {
            var originalCatalog = this.CreateCatalog();
            using (FilteredCatalog catalog = new FilteredCatalog(originalCatalog, p => p.Exports<IContract1>()))
            {
                var parts = catalog.Parts;
                Assert.Equal(2, parts.Count());
            }
        }

        [Fact]
        public void GetExports_Throws_WhenDisposed()
        {
            var originalCatalog = this.CreateCatalog();
            FilteredCatalog catalog = new FilteredCatalog(originalCatalog, p => p.Exports<IContract1>());
            catalog.Dispose();

            ExceptionAssert.ThrowsDisposed(catalog, () =>
            {
                var p = catalog.GetExports<IContract1>();
            });
        }

        [Fact]
        public void GetExports()
        {
            var originalCatalog = this.CreateCatalog();
            using (FilteredCatalog catalog = new FilteredCatalog(originalCatalog, p => p.Exports<IContract1>()))
            {
                var parts1 = catalog.GetExports<IContract1>();
                Assert.Equal(2, parts1.Count());

                var parts2 = catalog.GetExports<IContract2>();
                Assert.Equal(0, parts2.Count());
            }
        }

        [Fact]
        public void GetExportsWithGenerics()
        {
            var originalCatalog = new TypeCatalog(typeof(GenericExporter<,>), typeof(Exporter11), typeof(Exporter22));
            using (FilteredCatalog catalog = new FilteredCatalog(originalCatalog, p => p.Exports<IContract1>()).IncludeDependents())
            {
                var parts1 = catalog.GetExports<IContract1>();
                Assert.Equal(1, parts1.Count());

                using (var container = new CompositionContainer(catalog))
                {
                    var results = container.GetExports<IGenericContract<string, string>>();
                    Assert.Equal(1, results.Count());
                }
            }
        }

        [Fact]
        public void Complement_Throws_WhenDisposed()
        {
            var originalCatalog = this.CreateCatalog();
            FilteredCatalog catalog = new FilteredCatalog(originalCatalog, p => p.Exports<IContract1>());
            catalog.Dispose();

            ExceptionAssert.ThrowsDisposed(catalog, () =>
            {
                var c = catalog.Complement;
            });
        }

        [Fact]
        public void Complement()
        {
            var originalCatalog = this.CreateCatalog();
            using (FilteredCatalog catalog = new FilteredCatalog(originalCatalog, p => p.Exports<IContract1>()))
            {
                var c = catalog.Complement;
                Assert.NotNull(c);

                var parts1 = c.GetExports<IContract2>();
                Assert.Equal(2, parts1.Count());

                var parts2 = c.GetExports<IContract1>();
                Assert.Equal(0, parts2.Count());
            }
        }

        [Fact]
        public void Complement_Repeatable_Read()
        {
            var originalCatalog = this.CreateCatalog();
            using (FilteredCatalog catalog = new FilteredCatalog(originalCatalog, p => p.Exports<IContract1>()))
            {
                var c1 = catalog.Complement;
                var c2 = catalog.Complement;

                Assert.Same(c1, c2);
            }
        }

        [Fact]
        public void Complement_ComplementOfComplement()
        {
            var originalCatalog = this.CreateCatalog();
            using (FilteredCatalog catalog = new FilteredCatalog(originalCatalog, p => p.Exports<IContract1>()))
            {
                var c1 = catalog.Complement;
                var c2 = c1.Complement;

                Assert.Same(catalog, c2);
            }
        }

        [Fact]
        public void FilteredNotifications()
        {
            var catalog1 = CreateSubCatalog1();
            var catalog2 = CreateSubCatalog2();
            var catalog = new AggregateCatalog();

            var filter1 = catalog.Filter(p => p.Exports<IContract1>());
            var filter2 = catalog.Filter(p => p.Exports<IContract2>());

            bool filter1Ing = false;
            bool filter1Ed = false;
            bool filter2Ing = false;
            bool filter2Ed = false;

            ComposablePartCatalogChangeEventArgs edArgs = null;
            ComposablePartCatalogChangeEventArgs ingArgs = null;

            filter1.Changing += (object s, ComposablePartCatalogChangeEventArgs a) =>
            {
                Assert.Same(filter1, s);
                Assert.False(filter1Ing);
                Assert.False(filter1Ed);
                Assert.Null(ingArgs);
                Assert.Null(edArgs);

                filter1Ing = true;
                ingArgs = a;
            };

            filter1.Changed += (object s, ComposablePartCatalogChangeEventArgs a) =>
            {
                Assert.True(filter1Ing);
                Assert.False(filter1Ed);
                Assert.NotNull(ingArgs);
                Assert.Null(edArgs);

                filter1Ed = true;
                edArgs = a;
                EqualityExtensions.CheckEquals(ingArgs.AddedDefinitions, edArgs.AddedDefinitions);
                EqualityExtensions.CheckEquals(ingArgs.RemovedDefinitions, edArgs.RemovedDefinitions);
            };

            filter2.Changing += (object s, ComposablePartCatalogChangeEventArgs a) =>
            {
                Assert.Same(filter2, s);
                Assert.False(filter2Ing);
                Assert.False(filter2Ed);
                Assert.Null(ingArgs);
                Assert.Null(edArgs);

                filter2Ing = true;
                ingArgs = a;
            };

            filter2.Changed += (object s, ComposablePartCatalogChangeEventArgs a) =>
            {
                Assert.True(filter2Ing);
                Assert.False(filter2Ed);
                Assert.NotNull(ingArgs);
                Assert.Null(edArgs);

                filter2Ed = true;
                edArgs = a;
                EqualityExtensions.CheckEquals(ingArgs.AddedDefinitions, edArgs.AddedDefinitions);
                EqualityExtensions.CheckEquals(ingArgs.RemovedDefinitions, edArgs.RemovedDefinitions);
            };

            //at first everything is empty

            // add the first one
            filter1Ing = false;
            filter1Ed = false;
            filter2Ing = false;
            filter2Ed = false;
            ingArgs = null;
            edArgs = null;

            catalog.Catalogs.Add(catalog1);
            Assert.True(filter1Ing);
            Assert.True(filter1Ed);
            Assert.False(filter2Ing);
            Assert.False(filter2Ed);

            Assert.Equal(edArgs.AddedDefinitions.Count(), 2);
            Assert.Equal(edArgs.RemovedDefinitions.Count(), 0);
            Assert.Equal(0, filter2.Parts.Count());
            Assert.Equal(2, filter1.Parts.Count());

            EqualityExtensions.CheckEquals(ingArgs.AddedDefinitions, catalog1.Parts);
            EqualityExtensions.CheckEquals(edArgs.AddedDefinitions, catalog1.Parts);

            // add the second one
            filter1Ing = false;
            filter1Ed = false;
            filter2Ing = false;
            filter2Ed = false;
            ingArgs = null;
            edArgs = null;

            catalog.Catalogs.Add(catalog2);
            Assert.True(filter2Ing);
            Assert.True(filter2Ed);
            Assert.False(filter1Ing);
            Assert.False(filter1Ed);

            Assert.Equal(edArgs.AddedDefinitions.Count(), 2);
            Assert.Equal(edArgs.RemovedDefinitions.Count(), 0);
            Assert.Equal(2, filter2.Parts.Count());
            Assert.Equal(2, filter1.Parts.Count());

            EqualityExtensions.CheckEquals(ingArgs.AddedDefinitions, catalog2.Parts);
            EqualityExtensions.CheckEquals(edArgs.AddedDefinitions, catalog2.Parts);

            // remove the second one
            filter1Ing = false;
            filter1Ed = false;
            filter2Ing = false;
            filter2Ed = false;
            ingArgs = null;
            edArgs = null;

            catalog.Catalogs.Remove(catalog2);
            Assert.True(filter2Ing);
            Assert.True(filter2Ed);
            Assert.False(filter1Ing);
            Assert.False(filter1Ed);

            Assert.Equal(edArgs.AddedDefinitions.Count(), 0);
            Assert.Equal(edArgs.RemovedDefinitions.Count(), 2);
            Assert.Equal(0, filter2.Parts.Count());
            Assert.Equal(2, filter1.Parts.Count());

            EqualityExtensions.CheckEquals(ingArgs.RemovedDefinitions, catalog2.Parts);
            EqualityExtensions.CheckEquals(edArgs.RemovedDefinitions, catalog2.Parts);

            // remove the first one
            filter1Ing = false;
            filter1Ed = false;
            filter2Ing = false;
            filter2Ed = false;
            ingArgs = null;
            edArgs = null;

            catalog.Catalogs.Remove(catalog1);
            Assert.True(filter1Ing);
            Assert.True(filter1Ed);
            Assert.False(filter2Ing);
            Assert.False(filter2Ed);

            Assert.Equal(edArgs.AddedDefinitions.Count(), 0);
            Assert.Equal(edArgs.RemovedDefinitions.Count(), 2);
            Assert.Equal(0, filter2.Parts.Count());
            Assert.Equal(0, filter1.Parts.Count());

            EqualityExtensions.CheckEquals(ingArgs.RemovedDefinitions, catalog1.Parts);
            EqualityExtensions.CheckEquals(edArgs.RemovedDefinitions, catalog1.Parts);
        }

        [Fact]
        public void NoNotificationsAfterDispose()
        {
            var catalog1 = CreateSubCatalog1();
            var catalog2 = CreateSubCatalog2();
            var catalog = new AggregateCatalog(catalog1, catalog2);

            var filter1 = catalog.Filter(p => p.Exports<IContract1>());

            filter1.Changing += (s, e) =>
            {
                throw new NotImplementedException();
            };

            filter1.Changed += (s, e) =>
            {
                throw new NotImplementedException();
            };

            filter1.Dispose();

            Assert.True(catalog.Catalogs.Remove(catalog1));
            Assert.True(catalog.Catalogs.Remove(catalog2));
        }

        [Fact]
        public void DoubleDispose()
        {
            var catalog1 = CreateSubCatalog1();
            var catalog2 = CreateSubCatalog2();
            var catalog = new AggregateCatalog(catalog1, catalog2);

            var filter1 = catalog.Filter(p => p.Exports<IContract1>());

            filter1.Changing += (s, e) =>
            {
                throw new NotImplementedException();
            };

            filter1.Changed += (s, e) =>
            {
                throw new NotImplementedException();
            };

            filter1.Dispose();
            filter1.Dispose();

            Assert.True(catalog.Catalogs.Remove(catalog1));
            Assert.True(catalog.Catalogs.Remove(catalog2));
        }

        private ComposablePartCatalog CreateCatalog()
        {
            return new TypeCatalog(
                typeof(Exporter11),
                typeof(Exporter12),
                typeof(Exporter21),
                typeof(Exporter22));

        }

        private ComposablePartCatalog CreateSubCatalog1()
        {
            return new TypeCatalog(
                typeof(Exporter11),
                typeof(Exporter12));
        }

        private ComposablePartCatalog CreateSubCatalog2()
        {
            return new TypeCatalog(
                typeof(Exporter21),
                typeof(Exporter22));
        }

        public interface IContract1 { }
        public interface IContract2 { }
        public interface IGenericContract<T1, T2> { }

        [Export(typeof(IGenericContract<,>))]
        public class GenericExporter<T1, T2> : IGenericContract<T1, T2>
        {
            [Import]
            IContract1 Import { get; set; }
        }

        [Export(typeof(IContract1))]
        public class Exporter11 : IContract1
        {
        }

        [Export(typeof(IContract1))]
        public class Exporter12 : IContract1
        {
        }

        [Export(typeof(IContract2))]
        public class Exporter21 : IContract2
        {
        }

        [Export(typeof(IContract2))]
        public class Exporter22 : IContract2
        {
        }

    }
}
