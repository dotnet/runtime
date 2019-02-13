// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition.Registration.Tests
{
    public class PartBuilderInterfaceTests
    {
        public interface IFirst {}
        public interface ISecond {}
        public interface IThird {}
        public interface IFourth {}
        public interface IFifth : IFourth {}

        public class Standard : IFirst, ISecond, IThird, IFifth
        {
        }

        public class Dippy : IFirst, ISecond, IThird, IFifth, IDisposable
        {
            public void Dispose() {}
        }

        public class BareClass {}

        public class Base :  IFirst, ISecond {}

        public class Derived : Base, IThird, IFifth {}

        public class Importer
        {
            [ImportMany] public IEnumerable<IFirst>  First;
            [ImportMany] public IEnumerable<ISecond> Second;
            [ImportMany] public IEnumerable<IThird>  Third;
            [ImportMany] public IEnumerable<IFourth> Fourth;
            [ImportMany] public IEnumerable<IFifth>  Fifth;
            [Import(AllowDefault=true)] public Base         Base;
            [Import(AllowDefault=true)] public Derived      Derived;
            [Import(AllowDefault=true)] public Dippy        Dippy;
            [Import(AllowDefault=true)] public Standard     Standard;
            [Import(AllowDefault=true)] public IDisposable  Disposable;
            [Import(AllowDefault=true)] public BareClass    BareClass;
        }
 
        [Fact]
        [ActiveIssue(35144, TargetFrameworkMonikers.UapAot)]
        public void StandardExportInterfacesShouldWork()
        {
            var builder = new RegistrationBuilder();
            builder.ForTypesMatching( (t) => true ).ExportInterfaces( (iface) => iface != typeof(System.IDisposable), (iface, bldr) => bldr.AsContractType((Type)iface) );
            builder.ForTypesMatching((t) => t.GetInterfaces().Where((iface) => iface != typeof(System.IDisposable)).Count() == 0).Export();

            var types = new Type[] { typeof(Standard), typeof(Dippy), typeof(Derived), typeof(BareClass) };
            var catalog = new TypeCatalog(types, builder);

            CompositionService cs = catalog.CreateCompositionService();
            
            var importer = new Importer();
            cs.SatisfyImportsOnce(importer);
            
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
        [ActiveIssue(35144, TargetFrameworkMonikers.UapAot)]
        public void StandardExportInterfacesDefaultContractShouldWork()
        {            //Same test as above only using default export builder
            var builder = new RegistrationBuilder();
            builder.ForTypesMatching( (t) => true ).ExportInterfaces( (iface) => iface != typeof(System.IDisposable) );
            builder.ForTypesMatching((t) => t.GetInterfaces().Where((iface) => iface != typeof(System.IDisposable)).Count() == 0).Export();

            var types = new Type[] { typeof(Standard), typeof(Dippy), typeof(Derived), typeof(BareClass) };
            var catalog = new TypeCatalog(types, builder);

            CompositionService cs = catalog.CreateCompositionService();
            
            var importer = new Importer();
            cs.SatisfyImportsOnce(importer);
            
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
