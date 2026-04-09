// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Tests.Integration
{
    public class ExportFactoryTests
    {
        public interface IId
        {
            int Id { get; }
        }

        public interface IIdTypeMetadata
        {
            string IdType { get; }
            string ExportTypeIdentity { get; }
        }

        [Export(typeof(IId))]
        [ExportMetadata("IdType", "PositiveIncrement")]
        public class UniqueExport : IId, IDisposable
        {
            private static int lastId = 0;

            public UniqueExport()
            {
                Id = lastId++;
            }

            public int Id { get; private set; }

            public void Dispose()
            {
                Id = -1;
            }
        }

        [Export]
        public class ExportFactoryImporter
        {
            [ImportingConstructor]
            public ExportFactoryImporter(
                ExportFactory<IId> idCreatorTCtor,
                ExportFactory<IId, IIdTypeMetadata> idCreatorTMCtor)
            {
                this._idCreatorTCtor = idCreatorTCtor;
                this._idCreatorTMCtor = idCreatorTMCtor;
            }

            private ExportFactory<IId> _idCreatorTCtor;
            private ExportFactory<IId, IIdTypeMetadata> _idCreatorTMCtor;

            [Import(typeof(IId))]
            public ExportFactory<IId> _idCreatorTField = null; // public so these can work on SL

            [Import]
            public ExportFactory<IId, IIdTypeMetadata> _idCreatorTMField = null; // public so these can work on SL

            [Import]
            public ExportFactory<IId> IdCreatorTProperty { get; set; }

            [Import(typeof(IId))]
            public ExportFactory<IId, IIdTypeMetadata> IdCreatorTMProperty { get; set; }

            [ImportMany]
            public ExportFactory<IId>[] IdCreatorsTProperty { get; set; }

            [ImportMany]
            public ExportFactory<IId, IIdTypeMetadata>[] IdCreatorsTMProperty { get; set; }

            public void AssertValid()
            {
                var ids = new int[]
                {
                    VerifyExportFactory(this._idCreatorTCtor),
                    VerifyExportFactory(this._idCreatorTMCtor),
                    VerifyExportFactory(this._idCreatorTField),
                    VerifyExportFactory(this._idCreatorTMField),
                    VerifyExportFactory(this.IdCreatorTProperty),
                    VerifyExportFactory(this.IdCreatorTMProperty),
                    VerifyExportFactory(this.IdCreatorsTProperty[0]),
                    VerifyExportFactory(this.IdCreatorsTMProperty[0])
                };

                Assert.Equal(1, this.IdCreatorsTProperty.Length);
                Assert.Equal(1, this.IdCreatorsTMProperty.Length);

                Assert.Equal(ids.Count(), new HashSet<int>(ids).Count());
            }

            private int VerifyExportFactory(ExportFactory<IId> creator)
            {
                var val1 = creator.CreateExport();
                var val2 = creator.CreateExport();

                Assert.NotEqual(val1.Value, val2.Value);
                Assert.NotEqual(val1.Value.Id, val2.Value.Id);

                Assert.True(val1.Value.Id >= 0, "Id should be positive");

                val1.Dispose();

                Assert.True(val1.Value.Id < 0, "Disposal of the value should set the id to negative");

                return creator.CreateExport().Value.Id;
            }

            private int VerifyExportFactory(ExportFactory<IId, IIdTypeMetadata> creator)
            {
                var val = VerifyExportFactory((ExportFactory<IId>)creator);

                Assert.Equal("PositiveIncrement", creator.Metadata.IdType);
                Assert.Equal(AttributedModelServices.GetTypeIdentity(typeof(ComposablePartDefinition)), creator.Metadata.ExportTypeIdentity);

                return val;
            }
        }

        [Fact]
        public void ExportFactoryStandardImports_ShouldWorkProperly()
        {
            var container = CreateWithAttributedCatalog(typeof(UniqueExport), typeof(ExportFactoryImporter));
            var partCreatorImporter = container.GetExportedValue<ExportFactoryImporter>();

            partCreatorImporter.AssertValid();
        }

        [Export]
        public class Foo : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                this.IsDisposed = true;
            }
        }

        [Export]
        public class SimpleExportFactoryImporter
        {
            [Import]
            public ExportFactory<Foo> FooFactory { get; set; }
        }

        [Fact]
        public void ExportFactoryOfT_RecompositionSingle_ShouldBlockChanges()
        {
            var aggCat = new AggregateCatalog();
            var typeCat = new TypeCatalog(typeof(Foo));
            aggCat.Catalogs.Add(new TypeCatalog(typeof(SimpleExportFactoryImporter)));
            aggCat.Catalogs.Add(typeCat);

            var container = new CompositionContainer(aggCat);

            var fooFactory = container.GetExportedValue<SimpleExportFactoryImporter>();

            Assert.Throws<ChangeRejectedException>(() =>
                aggCat.Catalogs.Remove(typeCat));

            Assert.Throws<ChangeRejectedException>(() =>
                aggCat.Catalogs.Add(new TypeCatalog(typeof(Foo))));
        }

        [Export]
        public class ManyExportFactoryImporter
        {
            [ImportMany(AllowRecomposition = true)]
            public ExportFactory<Foo>[] FooFactories { get; set; }
        }

        [Fact]
        public void FactoryOfT_RecompositionImportMany_ShouldSucceed()
        {
            var aggCat = new AggregateCatalog();
            var typeCat = new TypeCatalog(typeof(Foo));
            aggCat.Catalogs.Add(new TypeCatalog(typeof(ManyExportFactoryImporter)));
            aggCat.Catalogs.Add(typeCat);

            var container = new CompositionContainer(aggCat);

            var fooFactories = container.GetExportedValue<ManyExportFactoryImporter>();

            Assert.Equal(1, fooFactories.FooFactories.Length);

            aggCat.Catalogs.Add(new TypeCatalog(typeof(Foo)));

            Assert.Equal(2, fooFactories.FooFactories.Length);
        }

        public class ExportFactoryExplicitCP
        {
            [Import(RequiredCreationPolicy = CreationPolicy.Any)]
            public ExportFactory<Foo> FooCreatorAny { get; set; }

            [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
            public ExportFactory<Foo> FooCreatorNonShared { get; set; }

            [Import(RequiredCreationPolicy = CreationPolicy.Shared)]
            public ExportFactory<Foo> FooCreatorShared { get; set; }

            [ImportMany(RequiredCreationPolicy = CreationPolicy.Any)]
            public ExportFactory<Foo>[] FooCreatorManyAny { get; set; }

            [ImportMany(RequiredCreationPolicy = CreationPolicy.NonShared)]
            public ExportFactory<Foo>[] FooCreatorManyNonShared { get; set; }

            [ImportMany(RequiredCreationPolicy = CreationPolicy.Shared)]
            public ExportFactory<Foo>[] FooCreatorManyShared { get; set; }
        }

        [Fact]
        public void ExportFactory_ExplicitCreationPolicy_CPShouldBeIgnored()
        {
            var container = CreateWithAttributedCatalog(typeof(Foo));

            var part = new ExportFactoryExplicitCP();

            container.SatisfyImportsOnce(part);

            // specifying the required creation policy explicit on the import
            // of a ExportFactory will be ignored because the ExportFactory requires
            // the part it wraps to be either Any or NonShared to work properly.
            Assert.NotNull(part.FooCreatorAny);
            Assert.NotNull(part.FooCreatorNonShared);
            Assert.NotNull(part.FooCreatorShared);

            Assert.Equal(1, part.FooCreatorManyAny.Length);
            Assert.Equal(1, part.FooCreatorManyNonShared.Length);
            Assert.Equal(1, part.FooCreatorManyShared.Length);
        }

        public class ExportFactoryImportRequiredMetadata
        {
            [ImportMany]
            public ExportFactory<Foo>[] FooCreator { get; set; }

            [ImportMany]
            public ExportFactory<Foo, IIdTypeMetadata>[] FooCreatorWithMetadata { get; set; }
        }

        [Fact]
        public void ExportFactory_ImportRequiredMetadata_MissingMetadataShouldCauseImportToBeExcluded()
        {
            var container = CreateWithAttributedCatalog(typeof(Foo));

            var part = new ExportFactoryImportRequiredMetadata();

            container.SatisfyImportsOnce(part);

            Assert.Equal(1, part.FooCreator.Length);
            Assert.Equal(0, part.FooCreatorWithMetadata.Length);
        }

        [Export(typeof(Foo))]
        [PartCreationPolicy(CreationPolicy.Shared)]
        public class SharedFoo : Foo
        {
        }

        [Fact]
        public void ExportFactory_ImportShouldNotImportSharedPart()
        {
            var container = CreateWithAttributedCatalog(typeof(SharedFoo));

            var foo = container.GetExportedValue<Foo>();
            Assert.NotNull(foo);

            var part = new ExportFactoryImportRequiredMetadata();

            container.SatisfyImportsOnce(part);

            Assert.Equal(0, part.FooCreator.Length);
        }

        [Fact]
        public void ExportFactory_QueryContainerDirectly_ShouldWork()
        {
            var container = CreateWithAttributedCatalog(typeof(Foo));

            var importDef = ReflectionModelServicesEx.CreateImportDefinition(
                new LazyMemberInfo(MemberTypes.Field, () => new MemberInfo[] { typeof(ExportFactoryTests) }), // Give it a bogus member
                AttributedModelServices.GetContractName(typeof(Foo)),
                AttributedModelServices.GetTypeIdentity(typeof(Foo)),
                Enumerable.Empty<KeyValuePair<string, Type>>(),
                ImportCardinality.ZeroOrMore,
                true,
                CreationPolicy.Any,
                true, // isExportFactory
                null);

            var exports = container.GetExports(importDef);

            var partCreator = exports.Single();

            // Manually walk the steps of using a raw part creator which is modeled as a PartDefinition with
            // a single ExportDefinition.
            var partDef = (ComposablePartDefinition)partCreator.Value;
            var part = partDef.CreatePart();
            var foo = (Foo)part.GetExportedValue(partDef.ExportDefinitions.Single());

            Assert.NotNull(foo);

            var foo1 = (Foo)part.GetExportedValue(partDef.ExportDefinitions.Single());
            Assert.Equal(foo, foo1);

            // creating a new part should result in getting a new exported value
            var part2 = partDef.CreatePart();
            var foo2 = (Foo)part2.GetExportedValue(partDef.ExportDefinitions.Single());

            Assert.NotEqual(foo, foo2);

            // Disposing of part should cause foo to be disposed
            ((IDisposable)part).Dispose();
            Assert.True(foo.IsDisposed);
        }

        [Export]
        public class PartImporter<PartType>
        {
            [Import]
            public ExportFactory<PartType> Creator { get; set; }
        }

        [Export]
        public class SimpleExport
        {
        }

        [Fact]
        public void ExportFactory_SimpleRejectionResurrection_ShouldWork()
        {
            var importTypeCat = new TypeCatalog(typeof(PartImporter<SimpleExport>));
            var aggCatalog = new AggregateCatalog(importTypeCat);
            var container = new CompositionContainer(aggCatalog);
            var exports = container.GetExports<PartImporter<SimpleExport>>();
            Assert.Equal(0, exports.Count());

            aggCatalog.Catalogs.Add(new TypeCatalog(typeof(SimpleExport)));

            exports = container.GetExports<PartImporter<SimpleExport>>();
            Assert.Equal(1, exports.Count());
        }

        private static CompositionContainer CreateWithAttributedCatalog(params Type[] types)
        {
            var catalog = new TypeCatalog(types);
            return new CompositionContainer(catalog);
        }

        [Export]
        class Apple { }

        [Export]
        class Tree : IDisposable
        {
            private List<ExportLifetimeContext<Apple>> grownApples = new List<ExportLifetimeContext<Apple>>();

            [Import]
            private ExportFactory<Apple> AppleFactory { get; set; }
            internal Apple GrowApple()
            {
                var apple = this.AppleFactory.CreateExport();
                this.grownApples.Add(apple);
                return apple.Value;
            }

            internal void DisposeApples()
            {
                foreach (var apple in this.grownApples)
                {
                    apple.Dispose();
                }
                this.grownApples.Clear();
            }

            public void Dispose()
            {
                this.DisposeApples();
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ExportFactory_SimpleDispose()
        {
            var catalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            var container = new CompositionContainer(catalog);
            var tree = container.GetExportedValue<Tree>();
            var apple = tree.GrowApple();
            container.Dispose();
        }

    }
}
