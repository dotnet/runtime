// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition.Registration.Tests
{
    public class ExportBuilderUnitTests
    {
        public interface IFoo {}

        public class CFoo : IFoo {}
        public class FooImporter
        {
            [Import]
            public IFoo fooImporter { get; set; }
        }

        public class CFoo1 : IFoo  {}
        public class CFoo2 : IFoo  {}
        public class CFoo3 : IFoo  {}
        public class CFoo4 : IFoo  {}

        public class FooImporterBase
        {
            public IFoo ImportedFoo { get; set; }
            public IEnumerable<IFoo> IFoos { get; set; }
        }

        public class FooImporter1 : FooImporterBase 
        {
            public FooImporter1 () {}
            public FooImporter1(IFoo fooImporter)
            {
                ImportedFoo = fooImporter; 
            }
        }

        public class FooImporter2 : FooImporterBase
        {
            public FooImporter2 () {}
            public FooImporter2(IFoo fooImporter)
            {
                ImportedFoo = fooImporter; 
            }
        }

        public class FooImporter3 : FooImporterBase
        {
            public FooImporter3 () {}
            public FooImporter3(IFoo fooImporter)
            {
                ImportedFoo = fooImporter; 
            }
        }

        public class FooImporter4 : FooImporterBase
        {
            public FooImporter4 () {}
            public FooImporter4(IFoo fooImporter)
            {
                ImportedFoo = fooImporter; 
            }
        }

        [Fact]
        public void ExportInterfaceWithTypeOf1()
        {
            var ctx = new RegistrationBuilder();
            ctx.ForType<CFoo>().Export<IFoo>();
            var catalog = new TypeCatalog(new[] { typeof(CFoo) }, ctx); 
            Assert.True(catalog.Parts.Count() != 0);
            
            var container = new CompositionContainer(catalog, CompositionOptions.DisableSilentRejection);
            var importer = new FooImporter();
            container.SatisfyImportsOnce(importer);

            Assert.True(importer.fooImporter != null, "fooImporter not set!");
        }

        [Fact]
        public void ExportInterfaceWithTypeOf2()
        {
            var ctx = new RegistrationBuilder();
            ctx.ForType(typeof(CFoo)).Export( (c) => c.AsContractType(typeof(IFoo)));
            var catalog = new TypeCatalog(new[] { typeof(CFoo) }, ctx);
            Assert.True(catalog.Parts.Count() != 0);
            
            var container = new CompositionContainer(catalog, CompositionOptions.DisableSilentRejection);
            var importer = new FooImporter();
            container.SatisfyImportsOnce(importer);

            Assert.True(importer.fooImporter != null, "fooImporter not set!");
        }

        [Fact]
        public void ExportInheritedInterfaceWithImplements1()
        {
            var ctx = new RegistrationBuilder();
            ctx.ForTypesDerivedFrom<IFoo>().Export( (c) => c.Inherited().AsContractType(typeof(IFoo)));
            var catalog = new TypeCatalog(new[] { typeof(CFoo) }, ctx); 
            Assert.True(catalog.Parts.Count() != 0);
            
            var container = new CompositionContainer(catalog, CompositionOptions.DisableSilentRejection);
            var importer = new FooImporter();
            container.SatisfyImportsOnce(importer);

            Assert.True(importer.fooImporter != null, "fooImporter not set!");
        }

        [Fact]
        public void ExportInheritedInterfaceWithImplements2()
        {
            var ctx = new RegistrationBuilder();
            ctx.ForTypesDerivedFrom(typeof(IFoo)).Export( (c) => c.Inherited().AsContractType(typeof(IFoo)));
            var catalog = new TypeCatalog(new[] { typeof(CFoo) }, ctx); 
            Assert.True(catalog.Parts.Count() != 0);
            
            var container = new CompositionContainer(catalog, CompositionOptions.DisableSilentRejection);
            var importer = new FooImporter();
            container.SatisfyImportsOnce(importer);

            Assert.True(importer.fooImporter != null, "fooImporter not set!");
        }
    }
}
