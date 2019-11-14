// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition.Hosting
{
    public class CompositionScopeDefinitionTests
    {
        public interface IFooContract
        {
        }

        [Export(typeof(IFooContract))]
        public class FooImpl : IFooContract
        {
            [ImportMany]
            public IEnumerable<IFooContract> Foo { get; set; }
        }

        [Export(typeof(IFooContract))]
        public class FooImpl2 : IFooContract
        {
        }

        [Export(typeof(IFooContract))]
        public class FooImpl3 : IFooContract
        {
        }

        [Export("One", typeof(IFooContract))]
        [Export("Two", typeof(IFooContract))]
        [Export("Three", typeof(IFooContract))]
        public class FooImpl4 : IFooContract
        {
        }

        public class TestCatalog : ComposablePartCatalog, INotifyComposablePartCatalogChanged
        {
            private Func<IQueryable<ComposablePartDefinition>> _partFunc;
            private Func<IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>>> _exportsFunc;
            public TestCatalog(Func<IQueryable<ComposablePartDefinition>> partFunc, Func<IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>>> exportsFunc)
            {
                this._partFunc = partFunc;
                this._exportsFunc = exportsFunc;
            }

            public override IQueryable<ComposablePartDefinition> Parts
            {
                get { return this._partFunc.Invoke(); }
            }

            public override IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> GetExports(ImportDefinition definition)
            {
                return this._exportsFunc.Invoke();
            }

            public event EventHandler<ComposablePartCatalogChangeEventArgs> Changed;

            public event EventHandler<ComposablePartCatalogChangeEventArgs> Changing;

            public void OnChanged(ComposablePartCatalogChangeEventArgs e)
            {
                if (this.Changed != null)
                {
                    this.Changed.Invoke(this, e);
                }
            }

            public void OnChanging(ComposablePartCatalogChangeEventArgs e)
            {
                if (this.Changing != null)
                {
                    this.Changing.Invoke(this, e);
                }
            }
        }

        [Fact]
        public void Constructor()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(FooImpl));
            TypeCatalog catalog1 = new TypeCatalog(typeof(FooImpl2));
            TypeCatalog catalog2 = new TypeCatalog(typeof(FooImpl3));

            CompositionScopeDefinition scope1 = new CompositionScopeDefinition(catalog1, Enumerable.Empty<CompositionScopeDefinition>());
            CompositionScopeDefinition scope2 = new CompositionScopeDefinition(catalog1, Enumerable.Empty<CompositionScopeDefinition>());
            CompositionScopeDefinition scope = new CompositionScopeDefinition(catalog, new CompositionScopeDefinition[] { scope1, scope2 });

            Assert.NotNull(scope);
            Assert.NotNull(scope.Parts);
            Assert.Equal(1, scope.Parts.Count());
            Assert.Equal(2, scope.Children.Count());
            Assert.Same(scope1, scope.Children.ToArray()[0]);
            Assert.Same(scope2, scope.Children.ToArray()[1]);
        }

        [Fact]
        public void Constructor_NullChildren()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(FooImpl));
            CompositionScopeDefinition scope = new CompositionScopeDefinition(catalog, null);

            Assert.NotNull(scope.Children);
            Assert.Equal(0, scope.Children.Count());
        }

        [Fact]
        public void Constructor_NullCatalog_ShowThrowNullArgument()
        {
            var ex = ExceptionAssert.Throws<ArgumentNullException>(RetryMode.DoNotRetry, () =>
            {
                CompositionScopeDefinition scope = new CompositionScopeDefinition(null, Enumerable.Empty<CompositionScopeDefinition>());
            });
        }

        [Fact]
        public void Parts_DelegateToCatalog()
        {
            var parts = new TypeCatalog(typeof(FooImpl), typeof(FooImpl2), typeof(FooImpl2)).Parts;
            var exports = parts.Select(p => Tuple.Create(p, p.ExportDefinitions.First()));
            TestCatalog catalog = new TestCatalog(
                () => parts,
                () => exports);

            CompositionScopeDefinition scope = new CompositionScopeDefinition(catalog, null);
            EqualityExtensions.CheckEquals(parts, scope.Parts);
        }

        [Fact]
        public void Constructor_PublicSurface()
        {
            var catalog = new TypeCatalog(typeof(FooImpl), typeof(FooImpl2), typeof(FooImpl2));
            var exports = catalog.Parts.Select(p => p.ExportDefinitions.First());
            CompositionScopeDefinition scope = new CompositionScopeDefinition(catalog, null, exports);
            Assert.Equal(catalog.Parts.Count(), scope.Parts.Count());
            Assert.Equal(exports.Count(), scope.PublicSurface.Count());
        }

        [Fact]
        public void Constructor_PublicSurface_MultipleExportsPerPart()
        {
            var catalog = new TypeCatalog(typeof(FooImpl4));
            var exports = catalog.Parts.SelectMany(p => p.ExportDefinitions);
            CompositionScopeDefinition scope = new CompositionScopeDefinition(catalog, null, exports);
            Assert.Equal(3, scope.PublicSurface.Count());
        }

        [Fact]
        public void GetExports_DelegateToCatalog()
        {
            var parts = new TypeCatalog(typeof(FooImpl), typeof(FooImpl2), typeof(FooImpl2)).Parts;
            var exports = parts.Select(p => Tuple.Create(p, p.ExportDefinitions.First()));
            var import = parts.SelectMany(p => p.ImportDefinitions).First();
            TestCatalog catalog = new TestCatalog(
                () => parts,
                () => exports);

            CompositionScopeDefinition scope = new CompositionScopeDefinition(catalog, null);
            Assert.Same(exports, scope.GetExports(import));
        }

        [Fact]
        public void Notifications()
        {
            var parts = new TypeCatalog(typeof(FooImpl), typeof(FooImpl2), typeof(FooImpl2)).Parts;
            var exports = parts.Select(p => Tuple.Create(p, p.ExportDefinitions.First()));
            TestCatalog catalog = new TestCatalog(
                () => parts,
                () => exports);

            CompositionScopeDefinition scope = new CompositionScopeDefinition(catalog, null);

            ComposablePartCatalogChangeEventArgs args = new ComposablePartCatalogChangeEventArgs(Enumerable.Empty<ComposablePartDefinition>(), Enumerable.Empty<ComposablePartDefinition>(), null);

            bool changedFired = false;
            scope.Changed += new EventHandler<ComposablePartCatalogChangeEventArgs>((o, e) =>
                {
                    Assert.Same(args, e);
                    Assert.Same(scope, o);
                    changedFired = true;
                });

            bool changingFired = false;
            scope.Changing += new EventHandler<ComposablePartCatalogChangeEventArgs>((o, e) =>
            {
                Assert.Same(args, e);
                Assert.Same(scope, o);
                changingFired = true;
            });

            catalog.OnChanged(args);
            Assert.True(changedFired);

            catalog.OnChanging(args);
            Assert.True(changingFired);

            changedFired = false;
            changingFired = false;

            scope.Dispose();

            catalog.OnChanged(args);
            catalog.OnChanging(args);

            Assert.False(changedFired);
            Assert.False(changingFired);

        }

        [Fact]
        public void Dispose()
        {
            var parts = new TypeCatalog(typeof(FooImpl), typeof(FooImpl2), typeof(FooImpl2)).Parts;
            var exports = parts.Select(p => Tuple.Create(p, p.ExportDefinitions.First()));
            TestCatalog catalog = new TestCatalog(
                () => parts,
                () => exports);
            var import = parts.SelectMany(p => p.ImportDefinitions).First();

            CompositionScopeDefinition scope = new CompositionScopeDefinition(catalog, null);

            scope.Dispose();
            var ex = ExceptionAssert.Throws<ObjectDisposedException>(RetryMode.DoNotRetry, () =>
            {
                var ps = scope.Parts;
            });

            ex = ExceptionAssert.Throws<ObjectDisposedException>(RetryMode.DoNotRetry, () =>
            {
                var es = scope.GetExports(import);
            });

            scope.Dispose();
            ex = ExceptionAssert.Throws<ObjectDisposedException>(RetryMode.DoNotRetry, () =>
            {
                var ps = scope.Parts;
            });

            ex = ExceptionAssert.Throws<ObjectDisposedException>(RetryMode.DoNotRetry, () =>
            {
                var es = scope.GetExports(import);
            });

        }

        [Fact]
        public void SimpleComposition()
        {
            var catalog = new TypeCatalog(typeof(FooImpl), typeof(FooImpl2), typeof(FooImpl2));
            var scope = new CompositionScopeDefinition(catalog, null);
            var container = new CompositionContainer(scope);

            var foos = container.GetExportedValues<IFooContract>();
            Assert.Equal(3, foos.Count());
        }
    }
}
