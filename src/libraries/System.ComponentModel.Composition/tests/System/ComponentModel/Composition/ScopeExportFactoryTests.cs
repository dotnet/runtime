// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ScopeExportFactoryTests
    {
        public interface IFooContract
        {
        }

        public interface IFooMetadata
        {
            string Name { get; }
        }

        public interface IBarContract
        {
            IFooContract CreateFoo();
        }

        public interface IBlahContract
        {
        }

        [Export(typeof(IFooContract))]
        [ExportMetadata("Name", "Foo")]
        public class FooImpl : IFooContract
        {
        }

        [Export(typeof(IFooContract))]
        [ExportMetadata("Name", "Foo")]
        public class Foo2Impl : IFooContract
        {
        }

        [Export(typeof(IFooContract))]
        public class Foo3Impl : IFooContract
        {
            [Import]
            public IBlahContract Blah { get; set; }
        }

        [Export(typeof(IFooContract))]
        public class Foo4Impl : IFooContract
        {
            [Import]
            public ExportFactory<IFooContract> Blah { get; set; }
        }

        [Export(typeof(IBlahContract))]
        public class BlahImpl : IBlahContract
        {
            [Import]
            public IBlahContract Blah { get; set; }
        }

        [Export(typeof(IBarContract))]
        public class BarImpl : IBarContract
        {
            [Import]
            public ExportFactory<IFooContract> FooFactory { get; set; }

            public IFooContract CreateFoo()
            {
                var efv = this.FooFactory.CreateExport();
                var value = efv.Value;
                efv.Dispose();
                return value;
            }
        }

        [Export(typeof(IBarContract))]
        public class BarWithMany : IBarContract
        {
            [ImportMany]
            public ExportFactory<IFooContract>[] FooFactories { get; set; }

            public IFooContract CreateFoo()
            {
                var efv = this.FooFactories[0].CreateExport();
                var value = efv.Value;
                efv.Dispose();
                return value;
            }
        }

        [Export(typeof(IBarContract))]
        public class BarImplWithMetadata : IBarContract
        {
            [Import]
            public ExportFactory<IFooContract, IFooMetadata> FooFactory { get; set; }

            public IFooContract CreateFoo()
            {
                Assert.Equal("Foo", this.FooFactory.Metadata.Name);
                var efv = this.FooFactory.CreateExport();
                var value = efv.Value;
                efv.Dispose();
                return value;
            }
        }

        [Fact]
        public void SimpleChain()
        {
            var parentCatalog = new TypeCatalog(typeof(BarImpl));
            var childCatalog = new TypeCatalog(typeof(FooImpl));

            var scope = parentCatalog.AsScope(childCatalog.AsScope());
            var container = new CompositionContainer(scope);

            var bar = container.GetExportedValue<IBarContract>();
            Assert.NotNull(bar);

            var foo1 = bar.CreateFoo();
            Assert.NotNull(foo1);
            Assert.True(foo1 is FooImpl);

            var foo2 = bar.CreateFoo();
            Assert.NotNull(foo1);
            Assert.True(foo2 is FooImpl);

            Assert.NotEqual(foo1, foo2);
        }

        [Fact]
        public void SimpleChainWithTwoChildren()
        {
            var parentCatalog = new TypeCatalog(typeof(BarWithMany));
            var childCatalog1 = new TypeCatalog(typeof(FooImpl));
            var childCatalog2 = new TypeCatalog(typeof(Foo2Impl));

            var scope = parentCatalog.AsScope(childCatalog1.AsScope(), childCatalog2.AsScope());
            var container = new CompositionContainer(scope);

            var bar = container.GetExportedValue<IBarContract>() as BarWithMany;
            Assert.NotNull(bar);

            Assert.Equal(2, bar.FooFactories.Length);

            IFooContract foo1 = null;
            using (var efFoo1 = bar.FooFactories[0].CreateExport())
            {
                foo1 = efFoo1.Value;
            }

            IFooContract foo2 = null;
            using (var efFoo2 = bar.FooFactories[1].CreateExport())
            {
                foo2 = efFoo2.Value;
            }

            Assert.True(((foo1 is FooImpl) && (foo2 is Foo2Impl)) || ((foo2 is FooImpl) && (foo1 is Foo2Impl)));
        }

        [Fact]
        public void SimpleChainWithMetadata()
        {
            var parentCatalog = new TypeCatalog(typeof(BarImplWithMetadata));
            var childCatalog = new TypeCatalog(typeof(FooImpl));

            var scope = parentCatalog.AsScope(childCatalog.AsScope());
            var container = new CompositionContainer(scope);

            var bar = container.GetExportedValue<IBarContract>();
            Assert.NotNull(bar);

            var foo1 = bar.CreateFoo();
            Assert.NotNull(foo1);
            Assert.True(foo1 is FooImpl);

            var foo2 = bar.CreateFoo();
            Assert.NotNull(foo1);
            Assert.True(foo2 is FooImpl);

            Assert.NotEqual(foo1, foo2);
        }

        [Fact]
        public void SimpleChainWithLowerLoop()
        {
            var parentCatalog = new TypeCatalog(typeof(BarImpl));
            var childCatalog = new TypeCatalog(typeof(Foo3Impl), typeof(BlahImpl));

            var scope = parentCatalog.AsScope(childCatalog.AsScope());
            var container = new CompositionContainer(scope);

            var bar = container.GetExportedValue<IBarContract>();
            Assert.NotNull(bar);

            var foo1 = bar.CreateFoo();
            Assert.NotNull(foo1);
            Assert.True(foo1 is Foo3Impl);

            var foo2 = bar.CreateFoo();
            Assert.NotNull(foo1);
            Assert.True(foo2 is Foo3Impl);

            Assert.NotEqual(foo1, foo2);
        }

        [Fact]
        public void SimpleChainWithCrossLoop()
        {
            var parentCatalog = new TypeCatalog(typeof(BarImpl));
            var childCatalog = new TypeCatalog(typeof(Foo4Impl));

            var scope = parentCatalog.AsScope(childCatalog.AsScope());
            var container = new CompositionContainer(scope);

            var bar = container.GetExportedValue<IBarContract>();
            Assert.NotNull(bar);

            var foo1 = bar.CreateFoo();
            Assert.NotNull(foo1);
            Assert.True(foo1 is Foo4Impl);

            var foo2 = bar.CreateFoo();
            Assert.NotNull(foo1);
            Assert.True(foo2 is Foo4Impl);

            Assert.NotEqual(foo1, foo2);
        }

        [Fact]
        public void SimpleChainWithLowerLoopRejection()
        {
            var parentCatalog = new TypeCatalog(typeof(BarImpl));
            var childCatalog = new TypeCatalog(typeof(Foo3Impl));

            var scope = parentCatalog.AsScope(childCatalog.AsScope());
            var container = new CompositionContainer(scope);

            var bar = container.GetExportedValueOrDefault<IBarContract>();
            Assert.Null(bar);
        }

        [Fact]
        public void ExportFactoryCausesRejectionBasedOnContract()
        {
            var parentCatalog = new TypeCatalog(typeof(BarImpl));
            var childCatalog = new TypeCatalog(typeof(BarImpl));

            var scope = parentCatalog.AsScope(childCatalog.AsScope());
            var container = new CompositionContainer(scope);

            var bar = container.GetExportedValueOrDefault<IBarContract>();
            Assert.Null(bar);
        }

        [Fact]
        public void ExportFactoryCausesRejectionBasedOnCardinality()
        {
            var parentCatalog = new TypeCatalog(typeof(BarImpl));
            var childCatalog = new TypeCatalog(typeof(FooImpl), typeof(Foo2Impl));

            var scope = parentCatalog.AsScope(childCatalog.AsScope());
            var container = new CompositionContainer(scope);

            var bar = container.GetExportedValueOrDefault<IBarContract>();
            Assert.Null(bar);
        }
    }

    public class ScopeExportFactoryWithPublicSurface
    {
        [Export] public class ClassA { }

        [Export] public class ClassB { }

        [Export] public class ClassC { }

        [Export]
        public class ClassRoot
        {
            [ImportAttribute]
            public ExportFactory<ClassA> classA;

            [ImportAttribute]
            public ExportFactory<ClassB> classB;

            [ImportAttribute]
            public ExportFactory<ClassB> classC;
        }

        [Fact]
        public void FilteredScopeFactoryOfTM_ShouldSucceed()
        {
            var c1 = new TypeCatalog(typeof(ClassRoot), typeof(ClassA));
            var c2 = new TypeCatalog(typeof(ClassA), typeof(ClassB), typeof(ClassC));
            var c3 = new TypeCatalog(typeof(ClassA), typeof(ClassB), typeof(ClassC));
            var c4 = new TypeCatalog(typeof(ClassA), typeof(ClassB), typeof(ClassC));
            var sd = c1.AsScope(c2.AsScopeWithPublicSurface<ClassA>(),
                                c3.AsScopeWithPublicSurface<ClassB>(),
                                c4.AsScopeWithPublicSurface<ClassC>());

            var container = new CompositionContainer(sd);

            var fromRoot = container.GetExportedValue<ClassRoot>();
            var a = fromRoot.classA.CreateExport().Value;
            var b = fromRoot.classB.CreateExport().Value;
            var c = fromRoot.classC.CreateExport().Value;

        }
    }

    public class ScopeFactoryAutoResolveFromAncestorScope
    {
        [Export] public class Root { }

        [Export] public class Child { }

        [Export]
        public class ClassRoot
        {
            [ImportAttribute]
            public ExportFactory<ClassA> classA;

            [ImportAttribute]
            public ClassA localClassA;
        }

        [Export]
        public class ClassA
        {
            [Import]
            public ICompositionService CompositionService;

            [ImportAttribute]
            public Root classRoot;

            public int InstanceValue;
        }

        public class ImportA
        {
            [Import]
            public ClassA classA;
        }

        [Fact]
        public void ScopeFactoryAutoResolveFromAncestorScopeShouldSucceed()
        {
            var c1 = new TypeCatalog(typeof(ClassRoot), typeof(ClassA), typeof(Root));
            var c2 = new TypeCatalog(typeof(ClassRoot), typeof(ClassA), typeof(Child));
            var sd = c1.AsScope(c2.AsScope());

            var container = new CompositionContainer(sd, CompositionOptions.ExportCompositionService);

            var fromRoot = container.GetExportedValue<ClassRoot>();
            var a1 = fromRoot.classA.CreateExport().Value;
            var a2 = fromRoot.classA.CreateExport().Value;
            fromRoot.localClassA.InstanceValue = 101;
            a1.InstanceValue = 202;
            a2.InstanceValue = 303;

            Assert.NotEqual(a1.InstanceValue, a2.InstanceValue);
            Assert.NotNull(fromRoot.localClassA.classRoot);
            Assert.NotNull(a1.classRoot);
            Assert.NotNull(a2.classRoot);
        }
    }

    public class DeeplyNestedCatalog
    {
        [Export]
        public class ClassRoot
        {
            [ImportAttribute]
            public ExportFactory<ClassA> classA;
        }

        [Export]
        public class ClassA
        {
            [ImportAttribute]
            public ExportFactory<ClassB> classB;
        }

        [Export]
        public class ClassB
        {
            [ImportAttribute]
            public ExportFactory<ClassC> classC;

            public int InstanceValue;
        }

        [Export]
        public class ClassC
        {
            [ImportAttribute]
            public ExportFactory<ClassD> classD;
        }

        [Export]
        public class ClassD
        {
        }

        [Fact]
        public void DeeplyNestedCatalogPartitionedCatalog_ShouldWork()
        {
            var cat1 = new TypeCatalog(typeof(ClassRoot));
            var cat2 = new TypeCatalog(typeof(ClassA));
            var cat3 = new TypeCatalog(typeof(ClassB));
            var cat4 = new TypeCatalog(typeof(ClassC));
            var cat5 = new TypeCatalog(typeof(ClassD));
            var sd = cat1.AsScope(cat2.AsScope(cat3.AsScope(cat4.AsScope(cat5.AsScope()))));

            var container = new CompositionContainer(sd);

            var fromRoot = container.GetExportedValue<ClassRoot>();

            var a1 = fromRoot.classA.CreateExport().Value;
            var b1 = a1.classB.CreateExport().Value;
            var c1 = b1.classC.CreateExport().Value;
            var d1 = c1.classD.CreateExport().Value;
        }

        [Fact]
        public void DeeplyNestedCatalogOverlappedCatalog_ShouldWork()
        {
            var cat1 = new TypeCatalog(typeof(ClassRoot), typeof(ClassA), typeof(ClassB), typeof(ClassC), typeof(ClassD));
            var cat2 = cat1;
            var cat3 = cat1;
            var cat4 = cat1;
            var cat5 = cat1;
            var sd = cat1.AsScope(cat2.AsScope(cat3.AsScope(cat4.AsScope(cat5.AsScope()))));

            var container = new CompositionContainer(sd);

            var fromRoot = container.GetExportedValue<ClassRoot>();

            var a1 = fromRoot.classA.CreateExport().Value;
            var b1 = a1.classB.CreateExport().Value;
            var c1 = b1.classC.CreateExport().Value;
            var d1 = c1.classD.CreateExport().Value;
        }
    }

    public class LocalSharedNonLocalInSameContainer
    {
        [Export]
        public class ClassRoot
        {
            [ImportAttribute]
            public ExportFactory<ClassA> classA;

            [ImportAttribute]
            public ClassXXXX xxxx;
        }

        [Export]
        public class ClassA
        {
            [ImportAttribute]
            public ExportFactory<ClassB> classB;

            [ImportAttribute]
            public ClassXXXX xxxx;
        }

        [Export]
        public class ClassB
        {
            [ImportAttribute]
            public ExportFactory<ClassC> classC;

            [ImportAttribute]
            public ClassXXXX xxxx;
        }

        [Export]
        public class ClassC
        {
            [ImportAttribute(Source = ImportSource.NonLocal)]
            public ClassXXXX xxxx;

            [Import]
            public ClassD classD;
        }

        [Export]
        public class ClassD
        {
            [ImportAttribute]
            public ClassXXXX xxxx;
        }

        [Export]
        public class ClassXXXX
        {
            public int InstanceValue;
        }

        [Fact]
        public void LocalSharedNonLocalInSameContainer_ShouldSucceed()
        {
            var cat1 = new TypeCatalog(typeof(ClassRoot), typeof(ClassXXXX));
            var cat2 = new TypeCatalog(typeof(ClassA));
            var cat3 = new TypeCatalog(typeof(ClassB));
            var cat4 = new TypeCatalog(typeof(ClassC), typeof(ClassD), typeof(ClassXXXX));
            var sd = cat1.AsScope(cat2.AsScope(cat3.AsScope(cat4.AsScope())));

            var container = new CompositionContainer(sd);

            var fromRoot = container.GetExportedValue<ClassRoot>();
            var a1 = fromRoot.classA.CreateExport().Value;
            fromRoot.xxxx.InstanceValue = 16;
            var b1 = a1.classB.CreateExport().Value;
            var c1 = b1.classC.CreateExport().Value;

            Assert.Equal(16, fromRoot.xxxx.InstanceValue);
            Assert.Equal(16, a1.xxxx.InstanceValue);
            Assert.Equal(16, b1.xxxx.InstanceValue);
            Assert.Equal(16, c1.xxxx.InstanceValue);
            Assert.Equal(0, c1.classD.xxxx.InstanceValue);

            c1.xxxx.InstanceValue = 8;

            Assert.Equal(8, fromRoot.xxxx.InstanceValue);
            Assert.Equal(8, a1.xxxx.InstanceValue);
            Assert.Equal(8, b1.xxxx.InstanceValue);
            Assert.Equal(8, c1.xxxx.InstanceValue);
            Assert.Equal(0, c1.classD.xxxx.InstanceValue);

            c1.classD.xxxx.InstanceValue = 2;
            Assert.Equal(8, fromRoot.xxxx.InstanceValue);
            Assert.Equal(8, a1.xxxx.InstanceValue);
            Assert.Equal(8, b1.xxxx.InstanceValue);
            Assert.Equal(8, c1.xxxx.InstanceValue);
            Assert.Equal(2, c1.classD.xxxx.InstanceValue);

        }
    }

    public class ScopeBridgingAdaptersConstructorInjection
    {
        [Export]
        public class ClassRoot
        {
            [ImportAttribute]
            public ExportFactory<ClassC> classC;

            [ImportAttribute]
            public ClassXXXX xxxx;
        }

        [Export]
        public class ClassC
        {
            [ImportingConstructor]
            public ClassC([ImportAttribute(RequiredCreationPolicy = CreationPolicy.NonShared, Source = ImportSource.NonLocal)]ClassXXXX xxxx)
            {
                this.xxxx = xxxx;
            }

            [Export]
            public ClassXXXX xxxx;

            [Import]
            public ClassD classD;
        }

        [Export]
        public class ClassD
        {
            [Import]
            public ClassXXXX xxxx;
        }

        [Export]
        public class ClassXXXX
        {
            public int InstanceValue;
        }

        [Fact]
        public void ScopeBridgingAdapters_ShouldSucceed()
        {
            var cat1 = new TypeCatalog(typeof(ClassRoot), typeof(ClassXXXX));
            var cat2 = new TypeCatalog(typeof(ClassC), typeof(ClassD));
            var sd = cat1.AsScope(cat2.AsScope());
            var container = new CompositionContainer(sd);

            var fromRoot = container.GetExportedValue<ClassRoot>();
            var c1 = fromRoot.classC.CreateExport().Value;
            var c2 = fromRoot.classC.CreateExport().Value;
            var c3 = fromRoot.classC.CreateExport().Value;
            var c4 = fromRoot.classC.CreateExport().Value;
            var c5 = fromRoot.classC.CreateExport().Value;

            Assert.Equal(0, fromRoot.xxxx.InstanceValue);
            Assert.Equal(0, c1.xxxx.InstanceValue);
            Assert.Equal(0, c1.classD.xxxx.InstanceValue);
            Assert.Equal(0, c2.xxxx.InstanceValue);
            Assert.Equal(0, c2.classD.xxxx.InstanceValue);
            Assert.Equal(0, c3.xxxx.InstanceValue);
            Assert.Equal(0, c3.classD.xxxx.InstanceValue);
            Assert.Equal(0, c4.xxxx.InstanceValue);
            Assert.Equal(0, c4.classD.xxxx.InstanceValue);
            Assert.Equal(0, c5.xxxx.InstanceValue);
            Assert.Equal(0, c5.classD.xxxx.InstanceValue);

            c1.xxxx.InstanceValue = 1;
            c2.xxxx.InstanceValue = 2;
            c3.xxxx.InstanceValue = 3;
            c4.xxxx.InstanceValue = 4;
            c5.xxxx.InstanceValue = 5;

            Assert.Equal(0, fromRoot.xxxx.InstanceValue);
            Assert.Equal(1, c1.xxxx.InstanceValue);
            Assert.Equal(1, c1.classD.xxxx.InstanceValue);
            Assert.Equal(2, c2.xxxx.InstanceValue);
            Assert.Equal(2, c2.classD.xxxx.InstanceValue);
            Assert.Equal(3, c3.xxxx.InstanceValue);
            Assert.Equal(3, c3.classD.xxxx.InstanceValue);
            Assert.Equal(4, c4.xxxx.InstanceValue);
            Assert.Equal(4, c4.classD.xxxx.InstanceValue);
            Assert.Equal(5, c5.xxxx.InstanceValue);
            Assert.Equal(5, c5.classD.xxxx.InstanceValue);
        }
    }

    public class ScopeBridgingAdaptersImportExportProperty
    {
        [Export]
        public class ClassRoot
        {
            [ImportAttribute]
            public ExportFactory<ClassC> classC;

            [ImportAttribute]
            public ClassXXXX xxxx;
        }

        [Export]
        public class ClassC
        {
            [Export]
            [ImportAttribute(RequiredCreationPolicy = CreationPolicy.NonShared, Source = ImportSource.NonLocal)]
            public ClassXXXX xxxx;

            [Import]
            public ClassD classD;
        }

        [Export]
        public class ClassD
        {
            [Import]
            public ClassXXXX xxxx;
        }

        [Export]
        public class ClassXXXX
        {
            public int InstanceValue;
        }

        [Fact]
        public void ScopeBridgingAdaptersImportExportProperty_ShouldSucceed()
        {
            var cat1 = new TypeCatalog(typeof(ClassRoot), typeof(ClassXXXX));
            var cat2 = new TypeCatalog(typeof(ClassC), typeof(ClassD));
            var sd = cat1.AsScope(cat2.AsScope());
            var container = new CompositionContainer(sd);

            var fromRoot = container.GetExportedValue<ClassRoot>();
            var c1 = fromRoot.classC.CreateExport().Value;
            var c2 = fromRoot.classC.CreateExport().Value;
            var c3 = fromRoot.classC.CreateExport().Value;
            var c4 = fromRoot.classC.CreateExport().Value;
            var c5 = fromRoot.classC.CreateExport().Value;

            Assert.Equal(0, fromRoot.xxxx.InstanceValue);
            Assert.Equal(0, c1.xxxx.InstanceValue);
            Assert.Equal(0, c1.classD.xxxx.InstanceValue);
            Assert.Equal(0, c2.xxxx.InstanceValue);
            Assert.Equal(0, c2.classD.xxxx.InstanceValue);
            Assert.Equal(0, c3.xxxx.InstanceValue);
            Assert.Equal(0, c3.classD.xxxx.InstanceValue);
            Assert.Equal(0, c4.xxxx.InstanceValue);
            Assert.Equal(0, c4.classD.xxxx.InstanceValue);
            Assert.Equal(0, c5.xxxx.InstanceValue);
            Assert.Equal(0, c5.classD.xxxx.InstanceValue);

            c1.xxxx.InstanceValue = 1;
            c2.xxxx.InstanceValue = 2;
            c3.xxxx.InstanceValue = 3;
            c4.xxxx.InstanceValue = 4;
            c5.xxxx.InstanceValue = 5;

            Assert.Equal(0, fromRoot.xxxx.InstanceValue);
            Assert.Equal(1, c1.xxxx.InstanceValue);
            Assert.Equal(1, c1.classD.xxxx.InstanceValue);
            Assert.Equal(2, c2.xxxx.InstanceValue);
            Assert.Equal(2, c2.classD.xxxx.InstanceValue);
            Assert.Equal(3, c3.xxxx.InstanceValue);
            Assert.Equal(3, c3.classD.xxxx.InstanceValue);
            Assert.Equal(4, c4.xxxx.InstanceValue);
            Assert.Equal(4, c4.classD.xxxx.InstanceValue);
            Assert.Equal(5, c5.xxxx.InstanceValue);
            Assert.Equal(5, c5.classD.xxxx.InstanceValue);
        }
    }

    public class SelfExportFromExportFactory
    {
        [Export]
        public class ClassRoot
        {
            [ImportAttribute]
            public ExportFactory<ClassA> classA;
        }

        [Export]
        public class ClassA
        {
            [ImportAttribute] public ClassB classB;
            [ImportAttribute] public ClassC classC;
            [ImportAttribute] public ClassD classD;

            public int InstanceValue;
        }

        [Export]
        public class ClassB
        {
            [ImportAttribute] public ClassA classA;
        }

        [Export]
        public class ClassC
        {
            [ImportAttribute] public ClassA classA;
        }

        [Export]
        public class ClassD
        {
            [ImportAttribute] public ClassA classA;
        }

        [Fact]
        public void SelfExportFromExportFactory_ShouldSucceed()
        {
            var cat1 = new TypeCatalog(typeof(ClassRoot));
            var cat2 = new TypeCatalog(typeof(ClassA), typeof(ClassB), typeof(ClassC), typeof(ClassD));
            var sd = cat1.AsScope(cat2.AsScope());

            var container = new CompositionContainer(sd);

            var fromRoot = container.GetExportedValue<ClassRoot>();
            var a1 = fromRoot.classA.CreateExport().Value;

            a1.InstanceValue = 8;

            Assert.Equal(8, a1.classB.classA.InstanceValue);
            Assert.Equal(8, a1.classC.classA.InstanceValue);
            Assert.Equal(8, a1.classD.classA.InstanceValue);
        }
    }
}
