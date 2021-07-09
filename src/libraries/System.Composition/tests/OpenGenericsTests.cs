// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Composition.Runtime;
using System.Composition.UnitTests.Util;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.UnitTests
{
    public class OpenGenericsTests : ContainerTests
    {
        private interface IRepository<T> { }

        [Export(typeof(IRepository<>))]
        private class BasicRepository<T> : IRepository<T> { }

        private class RepositoryProperty<T>
        {
            private readonly IRepository<T> _repository = new BasicRepository<T>();

            [Export(typeof(IRepository<>))]
            public IRepository<T> Repository { get { return _repository; } }
        }

        [Shared]
        private class TwoGenericExports<T>
        {
            private readonly BasicRepository<T> _repository = new BasicRepository<T>();

            [Export(typeof(IRepository<>))]
            public IRepository<T> Repository { get { return _repository; } }

            [Export(typeof(BasicRepository<>))]
            public BasicRepository<T> Repository2 { get { return _repository; } }
        }

        private interface IFirst<T> { }
        private interface ISecond<T> { }

        [Export(typeof(IFirst<>)), Export(typeof(ISecond<>))]
        private class FirstAndSecond<T> : IFirst<T>, ISecond<T> { }

        [Export(typeof(IRepository<>))]
        private class RepositoryWithKey<T, TKey> : IRepository<T> { }

        private interface IRepository { }

        [Export(typeof(IRepository))]
        private class RepositoryWithNonGenericExport<T> : IRepository { }

        [Export]
        private class ExportSelf<T> { }

        private class SomeGenericType<T> { }

        [Export(typeof(SomeGenericType<>))]
        private class ExportsBase<T> : SomeGenericType<T> { }

        [Fact]
        public void CanExportBasicOpenGeneric()
        {
            var cc = CreateContainer(typeof(BasicRepository<>));
            var r = cc.GetExport<IRepository<string>>();
            Assert.IsAssignableFrom<BasicRepository<string>>(r);
        }

        [Fact]
        public void OpenGenericProvidesMultipleInstantiations()
        {
            var cc = CreateContainer(typeof(BasicRepository<>));
            var r = cc.GetExport<IRepository<string>>();
            var r2 = cc.GetExport<IRepository<int>>();
            Assert.IsAssignableFrom<BasicRepository<int>>(r2);
        }

        [Fact]
        public void CanExportOpenGenericProperty()
        {
            var cc = CreateContainer(typeof(RepositoryProperty<>));
            var r = cc.GetExport<IRepository<string>>();
            Assert.IsAssignableFrom<BasicRepository<string>>(r);
        }

        [Fact]
        public void ASharedOpenGenericWithTwoExportsIsProvidedByASingleInstance()
        {
            var cc = CreateContainer(typeof(TwoGenericExports<>));
            var r = cc.GetExport<IRepository<string>>();
            var r2 = cc.GetExport<BasicRepository<string>>();

            Assert.NotNull(r);
            Assert.Same(r, r2);
        }

        [Fact]
        public void APartWithMultipleGenericExportsIsOnlyDiscoveredOnce()
        {
            var cc = CreateContainer(typeof(BasicRepository<>), typeof(TwoGenericExports<>));

            // Provided by TwoGenericExports
            var r1 = cc.GetExport<BasicRepository<string>>();

            // Provided by both
            var others = cc.GetExports<IRepository<string>>();
            Assert.Equal(2, others.Count());
        }

        [Fact]
        public void MultipleGenericExportsCanBeSpecifiedAtTheClassLevel()
        {
            var cc = CreateContainer(typeof(FirstAndSecond<>));
            var first = cc.GetExport<IFirst<string>>();
            Assert.IsAssignableFrom<FirstAndSecond<string>>(first);
        }

        // In future, the set of allowable generic type mappings will be expanded (see
        // ignored tests above).
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50919", TestPlatforms.Android)]
        public void TypesWithMismatchedGenericParameterListsAreDetectedDuringDiscovery()
        {
            var x = Assert.Throws<CompositionFailedException>(() => CreateContainer(typeof(RepositoryWithKey<,>)));
            Assert.Equal("Exported contract 'IRepository`1' of open generic part 'RepositoryWithKey`2' does not match the generic arguments of the class.", x.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50919", TestPlatforms.Android)]
        public void TypesWithNonGenericExportsAreDetectedDuringDiscovery()
        {
            var x = Assert.Throws<CompositionFailedException>(() => CreateContainer(typeof(RepositoryWithNonGenericExport<>)));
            Assert.Equal("Open generic part 'RepositoryWithNonGenericExport`1' cannot export non-generic contract 'IRepository'.", x.Message);
        }

        [Fact]
        public void OpenGenericsCanExportSelf()
        {
            var cc = CreateContainer(typeof(ExportSelf<>));
            var es = cc.GetExport<ExportSelf<string>>();
            Assert.IsAssignableFrom<ExportSelf<string>>(es);
        }

        [Fact]
        public void OpenGenericsCanExportBase()
        {
            var cc = CreateContainer(typeof(ExportsBase<>));
            var es = cc.GetExport<SomeGenericType<string>>();
            Assert.IsAssignableFrom<ExportsBase<string>>(es);
        }
    }
}
