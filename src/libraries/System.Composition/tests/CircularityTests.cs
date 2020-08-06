// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Composition.UnitTests.Util;
using System.Composition.Runtime;
using Xunit;

namespace System.Composition.UnitTests
{
    public class CircularityTests : ContainerTests
    {
        public interface IA { }

        [Export, Shared]
        public class BLazy
        {
            public Lazy<IA> A;

            [ImportingConstructor]
            public BLazy(Lazy<IA> ia)
            {
                A = ia;
            }
        }

        [Export(typeof(IA))]
        public class ACircular : IA
        {
            public BLazy B;

            [ImportingConstructor]
            public ACircular(BLazy b)
            {
                B = b;
            }
        }

        [Export, Shared]
        public class PropertyPropertyA
        {
            [Import]
            public PropertyPropertyB B { get; set; }
        }

        [Export]
        public class PropertyPropertyB
        {
            [Import]
            public PropertyPropertyA A { get; set; }
        }

        [Export]
        public class ConstructorPropertyA
        {
            [Import]
            public ConstructorPropertyB B { get; set; }
        }

        [Export, Shared]
        public class ConstructorPropertyB
        {
            [ImportingConstructor]
            public ConstructorPropertyB(ConstructorPropertyA a)
            {
                A = a;
            }

            public ConstructorPropertyA A { get; private set; }
        }

        public class CircularM
        {
            public string Name { get; set; }
        }

        [Export, ExportMetadata("Name", "A")]
        public class MetadataCircularityA
        {
            [Import]
            public Lazy<MetadataCircularityB, CircularM> B { get; set; }
        }

        [Export, ExportMetadata("Name", "B"), Shared]
        public class MetadataCircularityB
        {
            [Import]
            public Lazy<MetadataCircularityA, CircularM> A { get; set; }
        }

        [Export, Shared]
        public class NonPrereqSelfDependency
        {
            [Import]
            public NonPrereqSelfDependency Self { get; set; }
        }

        [Export]
        public class PrDepA
        {
            [ImportingConstructor]
            public PrDepA(PrDepB b) { }
        }

        [Export]
        public class PrDepB
        {
            [ImportingConstructor]
            public PrDepB(PrDepA a) { }
        }

        [Fact]
        public void CanHandleDefinitionCircularity()
        {
            var cc = CreateContainer(typeof(ACircular), typeof(BLazy));
            var x = cc.GetExport<BLazy>();
            Assert.IsAssignableFrom<ACircular>(x.A.Value);
            Assert.IsAssignableFrom<BLazy>(((ACircular)x.A.Value).B);
        }

        [Fact]
        public void CanHandleDefinitionCircularity2()
        {
            var cc = CreateContainer(typeof(ACircular), typeof(BLazy));
            var x = cc.GetExport<IA>();
            Assert.IsAssignableFrom<BLazy>(((ACircular)((ACircular)x).B.A.Value).B);
        }

        [Fact]
        public void HandlesPropertyPropertyCircularity()
        {
            var cc = CreateContainer(typeof(PropertyPropertyA), typeof(PropertyPropertyB));
            var a = cc.GetExport<PropertyPropertyA>();
            Assert.Same(a.B.A, a);
        }

        [Fact]
        public void HandlesPropertyPropertyCircularityReversed()
        {
            var cc = CreateContainer(typeof(PropertyPropertyA), typeof(PropertyPropertyB));
            var b = cc.GetExport<PropertyPropertyB>();
            Assert.Same(b.A.B, b.A.B.A.B);
        }

        [Fact]
        public void HandlesConstructorPropertyCircularity()
        {
            var cc = CreateContainer(typeof(ConstructorPropertyA), typeof(ConstructorPropertyB));
            var a = cc.GetExport<ConstructorPropertyA>();
            Assert.Same(a.B.A.B.A, a.B.A);
        }

        [Fact]
        public void HandlesConstructorPropertyCircularityReversed()
        {
            var cc = CreateContainer(typeof(ConstructorPropertyA), typeof(ConstructorPropertyB));
            var b = cc.GetExport<ConstructorPropertyB>();
            Assert.Same(b, b.A.B);
        }

        [Fact]
        public void HandlesMetadataCircularity()
        {
            var cc = CreateContainer(typeof(MetadataCircularityA), typeof(MetadataCircularityB));
            var a = cc.GetExport<MetadataCircularityA>();

            Assert.Equal("B", a.B.Metadata.Name);
            Assert.Equal("A", a.B.Value.A.Metadata.Name);
        }

        [Fact]
        public void SharedPartCanHaveNonPrereqDependencyOnSelf()
        {
            var cc = CreateContainer(typeof(NonPrereqSelfDependency));
            var npsd = cc.GetExport<NonPrereqSelfDependency>();
            Assert.Same(npsd, npsd.Self);
        }

        [Fact]
        public void PrerequisiteCircularitiesAreDetected()
        {
            var cc = CreateContainer(typeof(PrDepA), typeof(PrDepB));

            var x = Assert.Throws<CompositionFailedException>(() =>
            {
                cc.GetExport<PrDepA>();
            });
        }
    }
}
