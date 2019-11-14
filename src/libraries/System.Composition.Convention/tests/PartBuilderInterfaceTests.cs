// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Composition.Convention.Tests
{
    public class PartBuilderInterfaceTests
    {
        public interface IFirst { }
        public interface ISecond { }
        public interface IThird { }
        public interface IFourth { }
        public interface IFifth : IFourth { }

        public class Standard : IFirst, ISecond, IThird, IFifth
        {
        }

        public class Dippy : IFirst, ISecond, IThird, IFifth, IDisposable
        {
            public void Dispose() { }
        }

        public class BareClass { }

        public class Base : IFirst, ISecond { }

        public class Derived : Base, IThird, IFifth { }

        public class Importer
        {
            [ImportMany]
            public IEnumerable<IFirst> First { get; set; }
            [ImportMany]
            public IEnumerable<ISecond> Second { get; set; }
            [ImportMany]
            public IEnumerable<IThird> Third { get; set; }
            [ImportMany]
            public IEnumerable<IFourth> Fourth { get; set; }
            [ImportMany]
            public IEnumerable<IFifth> Fifth { get; set; }

            [Import(AllowDefault = true)]
            public Base Base { get; set; }
            [Import(AllowDefault = true)]
            public Derived Derived { get; set; }
            [Import(AllowDefault = true)]
            public Dippy Dippy { get; set; }
            [Import(AllowDefault = true)]
            public Standard Standard { get; set; }
            [Import(AllowDefault = true)]
            public IDisposable Disposable { get; set; }
            [Import(AllowDefault = true)]
            public BareClass BareClass { get; set; }
        }

        [Fact]
        public void StandardExportInterfacesShouldWork()
        {
            // Export all interfaces except IDisposable, Export contracts on types without interfaces. except for disposable types
            var builder = new ConventionBuilder();
            builder.ForTypesMatching((t) => true).ExportInterfaces();
            builder.ForTypesMatching((t) => t.GetTypeInfo().ImplementedInterfaces.Where((iface) => iface != typeof(System.IDisposable)).Count() == 0).Export();

            CompositionHost container = new ContainerConfiguration()
                .WithPart<Standard>(builder)
                .WithPart<Dippy>(builder)
                .WithPart<Derived>(builder)
                .WithPart<BareClass>(builder)
                .CreateContainer();

            var importer = new Importer();
            container.SatisfyImports(importer);

            Assert.NotNull(importer.First);
            Assert.True(importer.First.Count() == 3);
            Assert.NotNull(importer.Second);
            Assert.True(importer.Second.Count() == 3);
            Assert.NotNull(importer.Third);
            Assert.True(importer.Third.Count() == 3);
            Assert.NotNull(importer.Fourth);
            Assert.True(importer.Fourth.Count() == 3);
            Assert.NotNull(importer.Fifth);
            Assert.True(importer.Fifth.Count() == 3);

            Assert.Null(importer.Base);
            Assert.Null(importer.Derived);
            Assert.Null(importer.Dippy);
            Assert.Null(importer.Standard);
            Assert.Null(importer.Disposable);
            Assert.NotNull(importer.BareClass);
        }


        [Fact]
        public void StandardExportInterfacesInterfaceFilterDefaultContractShouldWork()
        {
            //Same test as above only using default export builder
            var builder = new ConventionBuilder();
            builder.ForTypesMatching((t) => true).ExportInterfaces((iface) => iface != typeof(System.IDisposable));
            builder.ForTypesMatching((t) => t.GetTypeInfo().ImplementedInterfaces.Where((iface) => iface != typeof(System.IDisposable)).Count() == 0).Export();

            CompositionHost container = new ContainerConfiguration()
                .WithPart<Standard>(builder)
                .WithPart<Dippy>(builder)
                .WithPart<Derived>(builder)
                .WithPart<BareClass>(builder)
                .CreateContainer();

            var importer = new Importer();
            container.SatisfyImports(importer);

            Assert.NotNull(importer.First);
            Assert.True(importer.First.Count() == 3);
            Assert.NotNull(importer.Second);
            Assert.True(importer.Second.Count() == 3);
            Assert.NotNull(importer.Third);
            Assert.True(importer.Third.Count() == 3);
            Assert.NotNull(importer.Fourth);
            Assert.True(importer.Fourth.Count() == 3);
            Assert.NotNull(importer.Fifth);
            Assert.True(importer.Fifth.Count() == 3);

            Assert.Null(importer.Base);
            Assert.Null(importer.Derived);
            Assert.Null(importer.Dippy);
            Assert.Null(importer.Standard);
            Assert.Null(importer.Disposable);
            Assert.NotNull(importer.BareClass);
        }

        [Fact]
        public void StandardExportInterfacesInterfaceFilterConfiguredContractShouldWork()
        {
            //Same test as above only using default export builder
            var builder = new ConventionBuilder();
            builder.ForTypesMatching((t) => true).ExportInterfaces((iface) => iface != typeof(System.IDisposable), (iface, bldr) => bldr.AsContractType((Type)iface));
            builder.ForTypesMatching((t) => t.GetTypeInfo().ImplementedInterfaces.Where((iface) => iface != typeof(System.IDisposable)).Count() == 0).Export();

            CompositionHost container = new ContainerConfiguration()
                .WithPart<Standard>(builder)
                .WithPart<Dippy>(builder)
                .WithPart<Derived>(builder)
                .WithPart<BareClass>(builder)
                .CreateContainer();

            var importer = new Importer();
            container.SatisfyImports(importer);

            Assert.NotNull(importer.First);
            Assert.True(importer.First.Count() == 3);
            Assert.NotNull(importer.Second);
            Assert.True(importer.Second.Count() == 3);
            Assert.NotNull(importer.Third);
            Assert.True(importer.Third.Count() == 3);
            Assert.NotNull(importer.Fourth);
            Assert.True(importer.Fourth.Count() == 3);
            Assert.NotNull(importer.Fifth);
            Assert.True(importer.Fifth.Count() == 3);

            Assert.Null(importer.Base);
            Assert.Null(importer.Derived);
            Assert.Null(importer.Dippy);
            Assert.Null(importer.Standard);
            Assert.Null(importer.Disposable);
            Assert.NotNull(importer.BareClass);
        }
    }
}
