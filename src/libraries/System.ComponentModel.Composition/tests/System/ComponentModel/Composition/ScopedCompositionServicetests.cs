// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ScopedCompositionServiceTests
    {
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
            public int InstanceValue;
        }

        public class ImportA
        {
            [Import]
            public ClassA classA;
        }

        [Export]
        public class FromRoot
        {
            [Import]
            public ExportFactory<ClassRequiresICompositionService> Required { get; set; }   
            
            [Import]
            public ExportFactory<ClassOptionallyImportsICompositionService> Optional { get; set; }   
        }

        [Export]
        public class ClassRequiresICompositionService
        {
            [Import(AllowDefault=false)]
            public ICompositionService CompositionService { get; set;  }
        }

        [Export]
        public class ClassOptionallyImportsICompositionService
        {
            [Import(AllowDefault=true)]
            public ICompositionService CompositionService { get; set;  }
        }

        [Fact]
        public void DontExportICompositionServiceFromRootRequiredImportShouldThrowCompositionException()
        {
            var rootCatalog = new TypeCatalog(typeof(ClassRequiresICompositionService), typeof(ClassOptionallyImportsICompositionService));
            var container = new CompositionContainer(rootCatalog);

            Assert.Throws<ImportCardinalityMismatchException>(() =>
            {
                var service = container.GetExportedValue<ClassRequiresICompositionService>();
                Assert.Null(service.CompositionService);
            });
        }

        [Fact]
        public void DontExportICompositionServiceFromRootOptionalImportShouldSucceed()
        {
            var rootCatalog = new TypeCatalog(typeof(ClassRequiresICompositionService),typeof(ClassOptionallyImportsICompositionService));
            var container = new CompositionContainer(rootCatalog);

            var  service = container.GetExportedValue<ClassOptionallyImportsICompositionService>();
            Assert.Null(service.CompositionService);
        }

        [Fact]
        public void ExportICompositionServiceFromRootRequiredImportShouldsucceed()
        {
            var rootCatalog = new TypeCatalog(typeof(ClassRequiresICompositionService),typeof(ClassOptionallyImportsICompositionService));
            var container = new CompositionContainer(rootCatalog, CompositionOptions.ExportCompositionService);

            var service = container.GetExportedValue<ClassRequiresICompositionService>();
            Assert.NotNull(service.CompositionService);
        }

        [Fact]
        public void ExportICompositionServiceFromRootOptionalImportShouldSucceed()
        {
            var rootCatalog = new TypeCatalog(typeof(ClassRequiresICompositionService),typeof(ClassOptionallyImportsICompositionService));
            var container = new CompositionContainer(rootCatalog, CompositionOptions.ExportCompositionService);

            var service = container.GetExportedValue<ClassOptionallyImportsICompositionService>();
            Assert.NotNull(service.CompositionService);
        }

        [Fact]
        public void DontExportICompositionServiceFromChildImportShouldShouldThrowCompositionException()
        {
            var rootCatalog = new TypeCatalog( typeof(FromRoot) );
            var childCatalog = new TypeCatalog( typeof(ClassRequiresICompositionService), typeof(ClassOptionallyImportsICompositionService) );
            var scope = rootCatalog.AsScope(childCatalog.AsScope());
            var container = new CompositionContainer(scope);

            Assert.Throws<ImportCardinalityMismatchException>(() =>
            {
                var fromRoot = container.GetExportedValue<FromRoot>();
                Assert.Null(fromRoot);
            });
        }

        [Fact]
        public void ExportICompositionServiceFromChildImportShouldShouldSucceed()
        {
            var childCatalog = new TypeCatalog( typeof(ClassRequiresICompositionService), typeof(ClassOptionallyImportsICompositionService) );
            var rootCatalog = new TypeCatalog( typeof(FromRoot) );
            var scope = rootCatalog.AsScope(childCatalog.AsScope());
            var container = new CompositionContainer(scope, CompositionOptions.ExportCompositionService);

            var fromRoot = container.GetExportedValue<FromRoot>();

            var requiredService = fromRoot.Required.CreateExport();
            Console.WriteLine("requiredService: {0}", requiredService.Value.CompositionService);
            Assert.NotNull(requiredService.Value.CompositionService);

            var optionalService = fromRoot.Optional.CreateExport();
            Console.WriteLine("optionalService: {0}", optionalService.Value.CompositionService);
            Assert.NotNull(optionalService.Value.CompositionService);
        }

        [Fact]
        public void ScopingEndToEndWithCompositionService_MatchingCatalogsShouldSucceed()
        {
            var c = new TypeCatalog( typeof(ClassRoot), typeof(ClassA) );
            var sd = c.AsScope( c.AsScope() );

            var container = new CompositionContainer(sd, CompositionOptions.ExportCompositionService);

            var fromRoot = container.GetExportedValue<ClassRoot>();
            var a1 = fromRoot.classA.CreateExport().Value;
            var a2 = fromRoot.classA.CreateExport().Value;
            fromRoot.localClassA.InstanceValue = 101;
            a1.InstanceValue = 202;
            a2.InstanceValue = 303;

            if (a1.InstanceValue ==  a2.InstanceValue) { throw new Exception("Incorrect sharing, a1 is shared with a2"); }

            var xroot = new ImportA();
            var x1 = new ImportA();
            var x2 = new ImportA();

            fromRoot.localClassA.CompositionService.SatisfyImportsOnce(xroot);
            a1.CompositionService.SatisfyImportsOnce(x1);
            a2.CompositionService.SatisfyImportsOnce(x2);
            Assert.Equal(xroot.classA.InstanceValue, fromRoot.localClassA.InstanceValue); 
            Assert.Equal(x1.classA.InstanceValue, a1.InstanceValue); 
            Assert.Equal(x2.classA.InstanceValue, a2.InstanceValue); 

        }

        [Fact]
        public void ScopingEndToEndWithCompositionService_PartitionedCatalogsShouldSucceed()
        {
            var c1 = new TypeCatalog( typeof(ClassRoot), typeof(ClassA) );
            var c2 = new TypeCatalog( typeof(ClassA) );
            var sd = c1.AsScope( c2.AsScope() );

            var container = new CompositionContainer(sd, CompositionOptions.ExportCompositionService);

            var fromRoot = container.GetExportedValue<ClassRoot>();
            var a1 = fromRoot.classA.CreateExport().Value;
            var a2 = fromRoot.classA.CreateExport().Value;
            fromRoot.localClassA.InstanceValue = 101;
            a1.InstanceValue = 202;
            a2.InstanceValue = 303;

            if (a1.InstanceValue ==  a2.InstanceValue) { throw new Exception("Incorrect sharing, a1 is shared with a2"); }

            var xroot = new ImportA();
            var x1 = new ImportA();
            var x2 = new ImportA();

            fromRoot.localClassA.CompositionService.SatisfyImportsOnce(xroot);
            a1.CompositionService.SatisfyImportsOnce(x1);
            a2.CompositionService.SatisfyImportsOnce(x2);
            Assert.Equal(xroot.classA.InstanceValue, fromRoot.localClassA.InstanceValue); 
            Assert.Equal(x1.classA.InstanceValue, a1.InstanceValue); 
            Assert.Equal(x2.classA.InstanceValue, a2.InstanceValue); 
        }

    }
}
