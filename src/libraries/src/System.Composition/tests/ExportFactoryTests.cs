// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public class ExportFactoryTests : ContainerTests
    {
        private static class Boundaries
        {
            public const string DataConsistency = "DataConsistency";
            public const string UserIdentity = "UserIdentity";
        }

        [Shared, Export]
        public class SharedUnbounded { }

        [Shared(Boundaries.DataConsistency), Export]
        public class SharedBoundedByDC { }

        [Export]
        public class DataConsistencyBoundaryProvider
        {
            [Import, SharingBoundary(Boundaries.DataConsistency)]
            public ExportFactory<CompositionContext> SharingScopeFactory { get; set; }
        }

        [Export]
        public class SharedPartConsumer
        {
            public SharedBoundedByDC Sc1, Sc2;

            [ImportingConstructor]
            public SharedPartConsumer(SharedBoundedByDC sc1, SharedBoundedByDC sc2)
            {
                Sc1 = sc1;
                Sc2 = sc2;
            }
        }

        public interface IA { }

        [Export(typeof(IA))]
        public class A : IA, IDisposable
        {
            public bool IsDisposed;

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        [Shared, Export]
        public class GloballySharedWithDependency
        {
            public IA A;

            [ImportingConstructor]
            public GloballySharedWithDependency(IA a)
            {
                A = a;
            }
        }

        [Export]
        public class UseExportFactory
        {
            [Import]
            public ExportFactory<IA> AFactory { get; set; }
        }

        [Export]
        public class DisposesFactoryProduct : IDisposable
        {
            private readonly ExportFactory<IA> _factory;

            [ImportingConstructor]
            public DisposesFactoryProduct(ExportFactory<IA> factory)
            {
                _factory = factory;
            }

            public Export<IA> Product { get; set; }

            public void CreateProduct()
            {
                Product = _factory.CreateExport();
            }

            public void Dispose()
            {
                Product.Dispose();
            }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void SharedPartsAreSharedBetweenAllScopes()
        {
            var cc = CreateContainer(typeof(SharedUnbounded), typeof(DataConsistencyBoundaryProvider));
            var bp = cc.GetExport<DataConsistencyBoundaryProvider>().SharingScopeFactory;
            var x = bp.CreateExport().Value.GetExport<SharedUnbounded>();
            var y = bp.CreateExport().Value.GetExport<SharedUnbounded>();
            Assert.Same(x, y);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/20656", TargetFrameworkMonikers.UapAot)]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void TheSameSharedInstanceIsReusedWithinItsSharingBoundary()
        {
            var cc = CreateContainer(typeof(SharedBoundedByDC), typeof(SharedPartConsumer), typeof(DataConsistencyBoundaryProvider));
            var sf = cc.GetExport<DataConsistencyBoundaryProvider>().SharingScopeFactory;
            var s = sf.CreateExport();
            var s2 = sf.CreateExport();
            var x = s.Value.GetExport<SharedPartConsumer>();
            var y = s2.Value.GetExport<SharedPartConsumer>();
            Assert.Same(x.Sc1, x.Sc2);
            Assert.NotSame(x.Sc1, y.Sc1);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void NonSharedInstancesCreatedByAnExportFactoryAreControlledByTheirExportLifetimeContext()
        {
            var cc = CreateContainer(typeof(A), typeof(UseExportFactory));
            var bef = cc.GetExport<UseExportFactory>();
            var a = bef.AFactory.CreateExport();
            Assert.IsAssignableFrom(typeof(A), a.Value);
            Assert.False(((A)a.Value).IsDisposed);
            a.Dispose();
            Assert.True(((A)a.Value).IsDisposed);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void DependenciesOfSharedPartsAreResolvedInTheGlobalScope()
        {
            var cc = new ContainerConfiguration()
                .WithParts(typeof(GloballySharedWithDependency), typeof(A), typeof(DataConsistencyBoundaryProvider))
                .CreateContainer();
            var s = cc.GetExport<DataConsistencyBoundaryProvider>().SharingScopeFactory.CreateExport();
            var g = s.Value.GetExport<GloballySharedWithDependency>();
            s.Dispose();
            var a = (A)g.A;
            Assert.False(a.IsDisposed);
            cc.Dispose();
            Assert.True(a.IsDisposed);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void WhenABoundaryIsPresentBoundedPartsCannotBeCreatedOutsideIt()
        {
            var container = CreateContainer(typeof(DataConsistencyBoundaryProvider), typeof(SharedBoundedByDC));
            var x = Assert.Throws<CompositionFailedException>(() => container.GetExport<SharedBoundedByDC>());
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void TheProductOfAnExportFactoryCanBeDisposedDuringDisposalOfTheParent()
        {
            var container = new ContainerConfiguration()
                .WithPart<DisposesFactoryProduct>()
                .WithPart<A>()
                .CreateContainer();

            var dfp = container.GetExport<DisposesFactoryProduct>();
            dfp.CreateProduct();

            var a = dfp.Product.Value as A;

            container.Dispose();

            Assert.True(a.IsDisposed);
        }

        [Export("Special", typeof(IA))]
        public class A1 : IA { }

        [Export("Special", typeof(IA))]
        public class A2 : IA { }

        [Export]
        public class AConsumer
        {
            [ImportMany("Special")]
            public ExportFactory<IA>[] AFactories { get; set; }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void ExportFactoryCanBeComposedWithImportManyAndNames()
        {
            var cc = CreateContainer(typeof(AConsumer), typeof(A1), typeof(A2));
            var cons = cc.GetExport<AConsumer>();
            Assert.Equal(2, cons.AFactories.Length);
        }

        [Export]
        public class Disposable : IDisposable
        {
            public bool IsDisposed { get; set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        [Export]
        public class HasDisposableDependency
        {
            [Import]
            public Disposable Dependency { get; set; }
        }

        [Export]
        public class HasFactory
        {
            [Import]
            public ExportFactory<HasDisposableDependency> Factory { get; set; }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void WhenReleasingAnExportFromAnExportFactoryItsNonSharedDependenciesAreDisposed()
        {
            var cc = CreateContainer(typeof(Disposable), typeof(HasDisposableDependency), typeof(HasFactory));
            var hf = cc.GetExport<HasFactory>();
            var hddx = hf.Factory.CreateExport();
            var hdd = hddx.Value;
            hddx.Dispose();
            Assert.True(hdd.Dependency.IsDisposed);
        }
    }
}
