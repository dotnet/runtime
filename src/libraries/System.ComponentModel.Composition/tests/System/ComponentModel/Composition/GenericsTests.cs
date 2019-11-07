// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class GenericsTests
    {
        public class Bar
        {
        }

        public class Bar2 : Bar
        {
        }

        public struct FooStruct
        { }

        public interface IFoo { }
        public interface IFoo2 : IFoo { }
        public interface IBar { }
        public interface IExport<T1, T2> { }
        public class ExportImpl<T1, T2> : IExport<T1, T2> { }

        public interface IExport<T> { }
        public interface IImport<T1, T2> { }
        public interface IImport<T> { }

        [Export(typeof(IFoo))]
        public class Foo : IFoo
        {
        }

        public interface IPartWithImport
        {
            object GetValue();
        }

        [Export(typeof(IImport<,>))]
        public class SelfImport<T1, T2> : IImport<T1, T2>
        {

        }

        [Export(typeof(IImport<>))]
        public class SelfImport<T> : IImport<T>
        {

        }

        [Export(typeof(IExport<,>))]
        public class SelfExport<T1, T2> : IExport<T1, T2>
        {

        }

        public class PropertyExport<T1, T2> : IExport<T1, T2>
        {
            [Export(typeof(IExport<,>))]
            IExport<T1, T2> Property { get { return this; } }
        }

        public class PropertyExportWithContractInferred<T1, T2> : IExport<T1, T2>
        {
            [Export]
            IExport<T1, T2> PropertyExport { get { return this; } }
        }

        [Export(typeof(IExport<,>))]
        public class SelfExportWithPropertyImport<T1, T2> : IExport<T1, T2>, IPartWithImport
        {
            [Import(typeof(IImport<,>))]
            IImport<T1, T2> Value { get; set; }

            public object GetValue()
            {
                return this.Value;
            }
        }

        public class PropertyExportWithChangedParameterOrder<T1, T2>
        {
            public PropertyExportWithChangedParameterOrder()
            {
                this.Export = new ExportImpl<T2, T1>();
            }

            [Export]
            public IExport<T2, T1> Export { get; set; }
        }

        [Export(typeof(IExport<,>))]
        public class SelfExportWithLazyPropertyImport<T1, T2> : IExport<T1, T2>, IPartWithImport
        {
            [Import]
            Lazy<IImport<T1, T2>> Value { get; set; }

            public object GetValue()
            {
                return this.Value;
            }
        }

        [Export(typeof(IExport<>))]
        public class SelfExportWithNakedLazyPropertyImport<T> : IExport<T>, IPartWithImport
        {
            [Import]
            Lazy<T> Value { get; set; }

            public object GetValue()
            {
                return this.Value;
            }
        }

        [Export(typeof(IExport<,>))]
        public class SelfExportWithExportFactoryPropertyImport<T1, T2> : IExport<T1, T2>, IPartWithImport
        {
            [Import]
            ExportFactory<IImport<T1, T2>> Value { get; set; }

            public object GetValue()
            {
                return this.Value;
            }
        }

        [Export(typeof(IExport<>))]
        public class SelfExportWithNakedExportFactoryPropertyImport<T> : IExport<T>, IPartWithImport
        {
            [Import]
            ExportFactory<T> Value { get; set; }

            public object GetValue()
            {
                return this.Value;
            }
        }

        [Export(typeof(IExport<,>))]
        public class SelfExportWithExportFactoryParameterImport<T1, T2> : IExport<T1, T2>, IPartWithImport
        {
            [ImportingConstructor]
            SelfExportWithExportFactoryParameterImport(ExportFactory<IImport<T1, T2>> value)
            {
                this.Value = value;
            }

            private ExportFactory<IImport<T1, T2>> Value { get; set; }

            public object GetValue()
            {
                return this.Value;
            }
        }

        [Export(typeof(IExport<,>))]
        public class SelfExportWithCollectionPropertyImport<T1, T2> : IExport<T1, T2>, IPartWithImport
        {
            [ImportMany]
            IEnumerable<IImport<T1, T2>> Value { get; set; }

            public object GetValue()
            {
                return this.Value;
            }
        }

        [Export(typeof(IExport<,>))]
        public class SelfExportWithLazyCollectionPropertyImport<T1, T2> : IExport<T1, T2>, IPartWithImport
        {
            [ImportMany]
            IEnumerable<Lazy<IImport<T1, T2>>> Value { get; set; }

            public object GetValue()
            {
                return this.Value;
            }
        }

        [Export(typeof(IExport<,>))]
        public class SelfExportWithParameterImport<T1, T2> : IExport<T1, T2>, IPartWithImport
        {
            [ImportingConstructor]
            SelfExportWithParameterImport(IImport<T1, T2> import)
            {
                this.Value = import;
            }

            IImport<T1, T2> Value { get; set; }

            public object GetValue()
            {
                return this.Value;
            }
        }

        [Export(typeof(IExport<,>))]
        public class SelfExportWithPropertyImportWithContractInferred<T1, T2> : IExport<T1, T2>, IPartWithImport
        {
            [Import]
            IImport<T1, T2> Value { get; set; }

            public object GetValue()
            {
                return this.Value;
            }
        }

        [Export(typeof(IExport<,>))]
        public class SelfExportWithMultipleGenericImports<T1, T2> : IExport<T1, T2>
        {
            [Import]
            public IImport<T1> Import1 { get; set; }

            [Import]
            public IImport<T2> Import2 { get; set; }

            [Import]
            public IImport<T1, T2> Import3 { get; set; }

            [Import]
            public IImport<T2, T1> Import4 { get; set; }

            [Import]
            public IFoo Import5 { get; set; }

            [Import]
            public T1 Import6 { get; set; }

        }

        [Export(typeof(IExport<IFoo, IBar>))]
        public class ExportFooBar : IExport<IFoo, IBar>
        {
        }

        public static class SingletonExportExportCount
        {
            public static int Count { get; set; }
        }

        [Export(typeof(IExport<,>))]
        public class SingletonExport<T1, T2> : IExport<T1, T2>
        {
            public SingletonExport()
            {
                SingletonExportExportCount.Count++;
            }
        }

        public class SingletonImport<T1, T2>
        {
            [Import]
            public IExport<T1, T2> Import { get; set; }
        }

        [Export(typeof(IExport<>))]
        public class PartWithTypeConstraint<T> : IExport<T> where T : IFoo
        {
        }

        [Export(typeof(IExport<>))]
        public class PartWithBaseTypeConstraint<T> : IExport<T> where T : Bar
        {
        }

        [Export(typeof(IExport<>))]
        public class PartWithRefTypeConstraint<T> : IExport<T> where T : class
        {
        }

        [Export(typeof(IExport<>))]
        public class PartWithStructTypeConstraint<T> : IExport<T> where T : struct
        {
        }

        [Export(typeof(IExport<>))]
        public class PartWithNewableTypeConstraint<T> : IExport<T> where T : new()
        {
        }

        [Export(typeof(IExport<,>))]
        public class PartWithGenericConstraint<T1, T2> : IExport<T1, T2> where T2 : IDictionary<string, T1>
        {
        }

        [Export(typeof(IExport<,>))]
        public class PartWithNakedConstraint<T1, T2> : IExport<T1, T2> where T2 : T1
        {
        }

        [Export(typeof(IExport<>))]
        public class OpenGenericPartWithClosedGenericImport<T> : IExport<T>
        {
            [Import]
            public IImport<string> ClosedImport { get; set; }
        }

        [Fact]
        public void SelfExportWithClosedGenericImportTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SelfImport<>), typeof(OpenGenericPartWithClosedGenericImport<>));
            CompositionContainer container = new CompositionContainer(catalog);

            var export = container.GetExportedValueOrDefault<IExport<object>>();
            Assert.NotNull(export);

            OpenGenericPartWithClosedGenericImport<object> impl = export as OpenGenericPartWithClosedGenericImport<object>;
            Assert.NotNull(impl);
            Assert.NotNull(impl.ClosedImport);
        }

        [Fact]
        public void SelfExportTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SelfExport<,>));
            CompositionContainer container = new CompositionContainer(catalog);

            var export = container.GetExportedValueOrDefault<IExport<IFoo, IBar>>();
            Assert.NotNull(export);

            var export2 = container.GetExportedValueOrDefault<IExport<IBar, IFoo>>();
            Assert.NotNull(export2);
        }

        [Fact]
        public void PropertyExportTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(PropertyExport<,>));
            CompositionContainer container = new CompositionContainer(catalog);

            var export = container.GetExportedValueOrDefault<IExport<IFoo, IBar>>();
            Assert.NotNull(export);

            var export2 = container.GetExportedValueOrDefault<IExport<IBar, IFoo>>();
            Assert.NotNull(export2);
        }

        [Fact]
        public void PropertyExportWithContractInferredTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(PropertyExportWithContractInferred<,>));
            CompositionContainer container = new CompositionContainer(catalog);

            var export = container.GetExportedValueOrDefault<IExport<IFoo, IBar>>();
            Assert.NotNull(export);

            var export2 = container.GetExportedValueOrDefault<IExport<IBar, IFoo>>();
            Assert.NotNull(export2);
        }

        [Fact]
        public void SelfExportWithPropertyImportTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SelfExportWithPropertyImport<,>), typeof(SelfImport<,>));
            CompositionContainer container = new CompositionContainer(catalog);

            var export = container.GetExportedValueOrDefault<IExport<IFoo, IBar>>();
            Assert.NotNull(export);

            var partWithImport = export as IPartWithImport;
            Assert.NotNull(partWithImport);
            Assert.NotNull(partWithImport.GetValue());
        }

        [Fact]
        public void SelfExportWithLazyPropertyImportTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SelfExportWithLazyPropertyImport<,>), typeof(SelfImport<,>));
            CompositionContainer container = new CompositionContainer(catalog);

            var export = container.GetExportedValueOrDefault<IExport<IFoo, IBar>>();
            Assert.NotNull(export);

            var partWithImport = export as IPartWithImport;
            Assert.NotNull(partWithImport);
            Assert.NotNull(partWithImport.GetValue());
        }

        [Fact]
        public void SelfExportWithNakedLazyPropertyImportTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SelfExportWithNakedLazyPropertyImport<>), typeof(Foo));
            CompositionContainer container = new CompositionContainer(catalog);

            var export = container.GetExportedValueOrDefault<IExport<IFoo>>();
            Assert.NotNull(export);

            var partWithImport = export as IPartWithImport;
            Assert.NotNull(partWithImport);
            Assert.NotNull(partWithImport.GetValue());
        }

        [Fact]
        public void SelfExportWithExportFactoryPropertyImportTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SelfExportWithExportFactoryPropertyImport<,>), typeof(SelfImport<,>));
            CompositionContainer container = new CompositionContainer(catalog);

            var export = container.GetExportedValueOrDefault<IExport<IFoo, IBar>>();
            Assert.NotNull(export);

            var partWithImport = export as IPartWithImport;
            Assert.NotNull(partWithImport);

            var value = partWithImport.GetValue() as ExportFactory<IImport<IFoo, IBar>>;
            Assert.NotNull(value);

            using (var efv = value.CreateExport())
            {
                Assert.NotNull(efv.Value);
            }
        }

        [Fact]
        public void SelfExportWithNakedExportFactoryPropertyImportTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SelfExportWithNakedExportFactoryPropertyImport<>), typeof(Foo));
            CompositionContainer container = new CompositionContainer(catalog);

            var export = container.GetExportedValueOrDefault<IExport<IFoo>>();
            Assert.NotNull(export);

            var partWithImport = export as IPartWithImport;
            Assert.NotNull(partWithImport);

            var value = partWithImport.GetValue() as ExportFactory<IFoo>;
            Assert.NotNull(value);

            using (var efv = value.CreateExport())
            {
                Assert.NotNull(efv.Value);
            }
        }

        [Fact]
        public void SelfExportWithExportFactoryParameterImportTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SelfExportWithExportFactoryParameterImport<,>), typeof(SelfImport<,>));
            CompositionContainer container = new CompositionContainer(catalog);

            var export = container.GetExportedValueOrDefault<IExport<IFoo, IBar>>();
            Assert.NotNull(export);

            var partWithImport = export as IPartWithImport;
            Assert.NotNull(partWithImport);

            var value = partWithImport.GetValue() as ExportFactory<IImport<IFoo, IBar>>;
            Assert.NotNull(value);

            using (var efv = value.CreateExport())
            {
                Assert.NotNull(efv.Value);
            }
        }

        [Fact]
        public void SelfExportWithCollectionPropertyImportTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SelfExportWithCollectionPropertyImport<,>), typeof(SelfImport<,>));
            CompositionContainer container = new CompositionContainer(catalog);

            var export = container.GetExportedValueOrDefault<IExport<IFoo, IBar>>();
            Assert.NotNull(export);

            var partWithImport = export as IPartWithImport;
            Assert.NotNull(partWithImport);
            Assert.NotNull(partWithImport.GetValue());
        }

        [Fact]
        public void SelfExportWithLazyCollectionPropertyImportTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SelfExportWithLazyCollectionPropertyImport<,>), typeof(SelfImport<,>));
            CompositionContainer container = new CompositionContainer(catalog);

            var export = container.GetExportedValueOrDefault<IExport<IFoo, IBar>>();
            Assert.NotNull(export);

            var partWithImport = export as IPartWithImport;
            Assert.NotNull(partWithImport);
            Assert.NotNull(partWithImport.GetValue());
        }

        [Fact]
        public void SelfExportWithPropertyImportWithContractInferredTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SelfExportWithPropertyImportWithContractInferred<,>), typeof(SelfImport<,>));
            CompositionContainer container = new CompositionContainer(catalog);

            var export = container.GetExportedValueOrDefault<IExport<IFoo, IBar>>();
            Assert.NotNull(export);

            var partWithImport = export as IPartWithImport;
            Assert.NotNull(partWithImport);
            Assert.NotNull(partWithImport.GetValue());
        }

        [Fact]
        public void SelfExportWithParameterImportTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SelfExportWithParameterImport<,>), typeof(SelfImport<,>));
            CompositionContainer container = new CompositionContainer(catalog);

            var export = container.GetExportedValueOrDefault<IExport<IFoo, IBar>>();
            Assert.NotNull(export);

            var partWithImport = export as IPartWithImport;
            Assert.NotNull(partWithImport);
            Assert.NotNull(partWithImport.GetValue());
        }

        [Fact]
        public void SelfExportWithMultipleGenericImportsTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SelfExportWithMultipleGenericImports<,>), typeof(SelfImport<,>), typeof(SelfImport<>), typeof(Foo));
            CompositionContainer container = new CompositionContainer(catalog);

            var export = container.GetExportedValueOrDefault<IExport<IFoo, IBar>>();
            Assert.NotNull(export);

            var part = export as SelfExportWithMultipleGenericImports<IFoo, IBar>;

            Assert.NotNull(part);
            Assert.NotNull(part.Import1);
            Assert.NotNull(part.Import2);
            Assert.NotNull(part.Import3);
            Assert.NotNull(part.Import4);
            Assert.NotNull(part.Import5);
            Assert.NotNull(part.Import6);
        }

        [Fact]
        public void SpecilzationMakesGeneric()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SelfExport<,>), typeof(ExportFooBar), typeof(SelfExport<IFoo, IBar>));
            CompositionContainer container = new CompositionContainer(catalog);

            // we are expecting 3 - one from the open generic, one from the closed generic and one from the specialization
            var exports = container.GetExportedValues<IExport<IFoo, IBar>>().ToArray();
            Assert.Equal(3, exports.Length);
        }

        [Fact]
        public void SingletonBehavior()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(SingletonExport<,>));
            CompositionContainer container = new CompositionContainer(catalog);

            SingletonExportExportCount.Count = 0;

            var exports = container.GetExportedValues<IExport<IFoo, IBar>>();
            Assert.Equal(1, exports.Count());
            // only one instance of the SingletonExport<,> is created
            Assert.Equal(1, SingletonExportExportCount.Count);

            exports = container.GetExportedValues<IExport<IFoo, IBar>>();
            Assert.Equal(1, exports.Count());
            // still only one instance of the SingletonExport<,> is created
            Assert.Equal(1, SingletonExportExportCount.Count);

            var import = new SingletonImport<IFoo, IBar>();
            container.SatisfyImportsOnce(import);
            // still only one instance of the SingletonExport<,> is created
            Assert.Equal(1, SingletonExportExportCount.Count);

            import = new SingletonImport<IFoo, IBar>();
            container.SatisfyImportsOnce(import);
            // still only one instance of the SingletonExport<,> is created
            Assert.Equal(1, SingletonExportExportCount.Count);

            var import2 = new SingletonImport<IBar, IFoo>();
            container.SatisfyImportsOnce(import2);
            // two instances of the SingletonExport<,> is created
            Assert.Equal(2, SingletonExportExportCount.Count);
        }

        [Fact]
        public void PartWithTypeConstraintTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(PartWithTypeConstraint<>));
            CompositionContainer container = new CompositionContainer(catalog);

            // IFoo should work
            Assert.Equal(1, container.GetExportedValues<IExport<IFoo>>().Count());

            // IFoo2 should work
            Assert.Equal(1, container.GetExportedValues<IExport<IFoo2>>().Count());

            // IBar shouldn't
            Assert.Equal(0, container.GetExportedValues<IExport<IBar>>().Count());
        }

        [Fact]
        public void PartWithBaseTypeConstraintTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(PartWithBaseTypeConstraint<>));
            CompositionContainer container = new CompositionContainer(catalog);

            // bar should work
            Assert.Equal(1, container.GetExportedValues<IExport<Bar>>().Count());

            // bar2 should work
            Assert.Equal(1, container.GetExportedValues<IExport<Bar2>>().Count());

            // IFoo shouldn't
            Assert.Equal(0, container.GetExportedValues<IExport<IFoo>>().Count());
        }

        [Fact]
        public void PartWithRefTypeConstraintTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(PartWithRefTypeConstraint<>));
            CompositionContainer container = new CompositionContainer(catalog);

            // IFoo should work
            Assert.Equal(1, container.GetExportedValues<IExport<IFoo>>().Count());

            // Bar should work
            Assert.Equal(1, container.GetExportedValues<IExport<Bar>>().Count());

            // int shouldn't
            Assert.Equal(0, container.GetExportedValues<IExport<int>>().Count());

            // FooStruct shouldn't
            Assert.Equal(0, container.GetExportedValues<IExport<FooStruct>>().Count());
        }

        [Fact]
        public void PartWithStructTypeConstraintTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(PartWithStructTypeConstraint<>));
            CompositionContainer container = new CompositionContainer(catalog);

            // int should work
            Assert.Equal(1, container.GetExportedValues<IExport<int>>().Count());

            // FooStruct should work
            Assert.Equal(1, container.GetExportedValues<IExport<FooStruct>>().Count());

            // IFoo shouldn't
            Assert.Equal(0, container.GetExportedValues<IExport<IFoo>>().Count());

            // Bar shouldn't
            Assert.Equal(0, container.GetExportedValues<IExport<Bar>>().Count());

        }

        [Fact]
        public void PartWithNewableTypeConstraintTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(PartWithNewableTypeConstraint<>));
            CompositionContainer container = new CompositionContainer(catalog);

            // int should work
            Assert.Equal(1, container.GetExportedValues<IExport<int>>().Count());

            // FooStruct should work
            Assert.Equal(1, container.GetExportedValues<IExport<FooStruct>>().Count());

            // IFoo shouldn't
            Assert.Equal(0, container.GetExportedValues<IExport<IFoo>>().Count());

            // Bar should
            Assert.Equal(1, container.GetExportedValues<IExport<Bar>>().Count());

        }

        [Fact]
        public void PartWithGenericConstraintTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(PartWithGenericConstraint<,>));
            CompositionContainer container = new CompositionContainer(catalog);

            // int, Dictionary<string, int> should work
            Assert.Equal(1, container.GetExportedValues<IExport<int, Dictionary<string, int>>>().Count());

            // int, Dictionary<string, string> should not work
            Assert.Equal(0, container.GetExportedValues<IExport<int, Dictionary<string, string>>>().Count());

            // FooStruct, FooStruct[] should work
            Assert.Equal(1, container.GetExportedValues<IExport<FooStruct, Dictionary<string, FooStruct>>>().Count());

            // FooStruct, IFoo should not
            Assert.Equal(0, container.GetExportedValues<IExport<FooStruct, IFoo>>().Count());

        }

        [Fact]
        public void PartWithNakedConstraintTest()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(PartWithNakedConstraint<,>));
            CompositionContainer container = new CompositionContainer(catalog);

            // Bar, Bar2 should work
            Assert.Equal(1, container.GetExportedValues<IExport<Bar, Bar2>>().Count());

            // Bar2, Bar should not work
            Assert.Equal(0, container.GetExportedValues<IExport<Bar2, Bar>>().Count());
        }

        [Fact]
        public void PartWithExportParametersInReverseOrder()
        {
            TypeCatalog catalog = new TypeCatalog(typeof(PropertyExportWithChangedParameterOrder<,>));
            CompositionContainer container = new CompositionContainer(catalog);

            Assert.Equal(1, container.GetExportedValues<IExport<string, int>>().Count());
        }

        public interface IA<T> { }
        public interface IB<T> { }

        [Export(typeof(IA<>)), Export(typeof(IB<>))]
        public class AB<T> : IA<T>, IB<T> { }

        [Fact]
        public void PartWithMultipleExportsOfASingleOrder_One_ShouldSucceed()
        {
            var tc = new TypeCatalog(typeof(AB<>));
            var cc = new CompositionContainer(tc);
            var ia = cc.GetExportedValue<IA<string>>();
            var ib = cc.GetExportedValue<IB<string>>();
            var ia1 = cc.GetExportedValue<IA<int>>();
            var ib1 = cc.GetExportedValue<IB<int>>();
        }

        [Export(typeof(IA<>)), Export(typeof(IBar))]
        class ANonGenericB<T> : IA<T>, IBar { }

        [Fact]
        public void NonGenericExportOnGenericPartIsNotAllowed()
        {
            var catalog = new TypeCatalog(typeof(ANonGenericB<>));
            var container = new CompositionContainer(catalog);
            Assert.Throws<ImportCardinalityMismatchException>(() =>
           {
               var b = container.GetExportedValue<IBar>();
           });

        }

        [Fact]
        public void MultipleGenericExportsFromTheSameSharedPartProvideTheSameInstance()
        {
            var catalog = new TypeCatalog(typeof(AB<>));
            var container = new CompositionContainer(catalog);
            var x = container.GetExportedValue<IA<string>>();
            var y = container.GetExportedValue<IB<string>>();
            Assert.Same(x, y);
        }

        [InheritedExport]
        interface ITest1<T>
        {
            void Execute();
        }

        class TestClass1<T> : ITest1<T>
        {
            public void Execute() { }
        }

        [InheritedExport]
        abstract class BaseClass<T>
        {
            public abstract void Execute();
        }

        class TestClass2<T> : BaseClass<T>
        {
            public override void Execute() { }
        }

        [Fact]
        public void InheritedExportOnGenericInterface()
        {
            var catalog = new TypeCatalog(typeof(TestClass1<>));
            var container = new CompositionContainer(catalog);
            var test = container.GetExportedValues<ITest1<string>>();

            Assert.True(test.Count() == 1);
        }

        [Fact]
        public void InheritedExportOnGenericBaseClass()
        {
            var catalog = new TypeCatalog(typeof(TestClass2<>));
            var container = new CompositionContainer(catalog);
            var test = container.GetExportedValues<BaseClass<string>>();

            Assert.True(test.Count() == 1);
        }
    }
}
