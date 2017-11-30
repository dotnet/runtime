// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class CompositionServiceExportFactoryTests
    {
        public interface IFoo 
        {
            void DoWork();
            Child FooChild { get; set; }
        }
    
        [Export(typeof(IFoo))]
        class Foo1 : IFoo
        {
            public void DoWork()
            {
                Console.WriteLine("HelloWorld : {0}", FooChild.FooValue);
            }
    
            [Import("FooChild")]
            public Child FooChild { get; set; }
        }
    
        [Export("FooChild")]
        public class Child
        {
            public int FooValue { get; set; }
        }
    
        [Export]
        public class App
        {
            [Import]
            public ExportFactory<IFoo> FooFactory { get; set; }
        }

        [Fact]
        [Description("Verifies CompositionServices.SatisfyImportsOne with Scoped ExportFactories")]
        public void ComposeAppInNewScopeChildrenInRoot_ShouldSucceed()
        {
            var childCatalog = new CompositionScopeDefinition(new TypeCatalog(typeof(Foo1)), new CompositionScopeDefinition[] { });
            var rootCatalog = new CompositionScopeDefinition(new TypeCatalog(typeof(Child)), new CompositionScopeDefinition[] { childCatalog });

            var cs = rootCatalog.CreateCompositionService();
            var app = new App();

            cs.SatisfyImportsOnce(app);

            var e1 = app.FooFactory.CreateExport();
            var e2 = app.FooFactory.CreateExport();
            var e3 = app.FooFactory.CreateExport();
            e1.Value.FooChild.FooValue = 10;
            e2.Value.FooChild.FooValue = 20;
            e3.Value.FooChild.FooValue = 30;

            Assert.Equal<int>(e1.Value.FooChild.FooValue, 30);
            Assert.Equal<int>(e2.Value.FooChild.FooValue, 30);
            Assert.Equal<int>(e3.Value.FooChild.FooValue, 30);
        }

        [Fact]
        [Description("Verifies CompositionServices.SatisfyImportsOne with Scoped ExportFactories")]
        public void ComposeAppInNewScopeChildrenInScope_ShouldSucceed()
        {
            var childCatalog = new CompositionScopeDefinition(new TypeCatalog(typeof(Foo1), typeof(Child)), new CompositionScopeDefinition[] { });
            var rootCatalog = new CompositionScopeDefinition(new TypeCatalog(), new CompositionScopeDefinition[] { childCatalog });

            var cs = rootCatalog.CreateCompositionService();
            var app = new App();

            cs.SatisfyImportsOnce(app);

            var e1 = app.FooFactory.CreateExport();
            var e2 = app.FooFactory.CreateExport();
            var e3 = app.FooFactory.CreateExport();
            e1.Value.FooChild.FooValue = 10;
            e2.Value.FooChild.FooValue = 20;
            e3.Value.FooChild.FooValue = 30;

            Assert.Equal<int>(e1.Value.FooChild.FooValue, 10);
            Assert.Equal<int>(e2.Value.FooChild.FooValue, 20);
            Assert.Equal<int>(e3.Value.FooChild.FooValue, 30);
        }

        [Fact]
        [Description("Verifies CompositionServices.SatisfyImportsOne with Scoped ExportFactories")]
        public void ComposeAppInNewScopeChildrenInBoth_ShouldSucceed()
        {
            var childCatalog = new CompositionScopeDefinition(new TypeCatalog(typeof(Foo1), typeof(Child)), new CompositionScopeDefinition[] { });
            var rootCatalog = new CompositionScopeDefinition(new TypeCatalog(typeof(Child)), new CompositionScopeDefinition[] { childCatalog });

            var cs = rootCatalog.CreateCompositionService();
            var app = new App();

            cs.SatisfyImportsOnce(app);

            var e1 = app.FooFactory.CreateExport();
            var e2 = app.FooFactory.CreateExport();
            var e3 = app.FooFactory.CreateExport();
            e1.Value.FooChild.FooValue = 10;
            e2.Value.FooChild.FooValue = 20;
            e3.Value.FooChild.FooValue = 30;

            Assert.Equal<int>(e1.Value.FooChild.FooValue, 10);
            Assert.Equal<int>(e2.Value.FooChild.FooValue, 20);
            Assert.Equal<int>(e3.Value.FooChild.FooValue, 30);
        }

        [Fact]
        [Description("Verifies CompositionServices.SatisfyImportsOne with NonScoped ExportFactories")]
        public void ComposeAppInRootScope_ShouldSucceed()
        {
            var catalog = new TypeCatalog(typeof(Foo1), typeof(Child));
 
            var cs = catalog.CreateCompositionService();
            var app = new App();

            cs.SatisfyImportsOnce(app);

            var e1 = app.FooFactory.CreateExport();
            var e2 = app.FooFactory.CreateExport();
            var e3 = app.FooFactory.CreateExport();
            e1.Value.FooChild.FooValue = 10;
            e2.Value.FooChild.FooValue = 20;
            e3.Value.FooChild.FooValue = 30;

            Assert.Equal<int>(e1.Value.FooChild.FooValue, 30);
            Assert.Equal<int>(e2.Value.FooChild.FooValue, 30);
            Assert.Equal<int>(e3.Value.FooChild.FooValue, 30);
        }
    }
}
