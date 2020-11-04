// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Composition.UnitTests.Util;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.UnitTests
{
    public class ExportMetadataDiscoveryTests : ContainerTests
    {
        [MetadataAttribute]
        public class ExportWithNameFooAttribute : ExportAttribute
        {
            public string Name { get { return "Foo"; } }
        }

        [MetadataAttribute]
        public class NameFooAttribute : Attribute
        {
            public string Name { get { return "Foo"; } }
        }

        [ExportWithNameFoo]
        public class SingleNamedExport { }

        [Export, NameFoo]
        public class NamedWithCustomMetadata { }

        [ExportWithNameFoo, ExportMetadata("Priority", 10)]
        public class NamedAndPrioritized { }

        [ExportWithNameFoo, Export, ExportMetadata("Priority", 10)]
        public class MultipleExportsOneNamedAndBothPrioritized { }

        public class Named {[DefaultValue(null)] public string Name { get; set; } }

        public class MultiValuedName { public string[] Name { get; set; } }

        public class Prioritized {[DefaultValue(0)] public int Priority { get; set; } }

        [Export,
         ExportMetadata("Name", "A"),
         ExportMetadata("Name", "B"),
         ExportMetadata("Name", "B")]
        public class MultipleNames { }

        [Fact]
        public void DiscoversMetadataSpecifiedUsingMetadataAttributeOnExportAttribute()
        {
            var cc = CreateContainer(typeof(SingleNamedExport));
            var ne = cc.GetExport<Lazy<SingleNamedExport, Named>>();
            Assert.Equal("Foo", ne.Metadata.Name);
        }

        [Fact]
        public void IfMetadataIsSpecifiedOnAnExportAttributeOtherExportsDoNotHaveIt()
        {
            var cc = CreateContainer(typeof(MultipleExportsOneNamedAndBothPrioritized));
            var ne = cc.GetExports<Lazy<MultipleExportsOneNamedAndBothPrioritized, Named>>();
            Assert.Equal(2, ne.Count());
            Assert.True(ne.Where(e => e.Metadata.Name != null).Count() == 1);
        }

        [Fact]
        public void DiscoversStandaloneExportMetadata()
        {
            var cc = CreateContainer(typeof(NamedAndPrioritized));
            var ne = cc.GetExport<Lazy<NamedAndPrioritized, Prioritized>>();
            Assert.Equal(10, ne.Metadata.Priority);
        }

        [Fact]
        public void DiscoversStandaloneExportMetadataUsingMetadataAttributes()
        {
            var cc = CreateContainer(typeof(NamedWithCustomMetadata));
            var ne = cc.GetExport<Lazy<NamedWithCustomMetadata, Named>>();
            Assert.Equal("Foo", ne.Metadata.Name);
        }

        [Fact]
        public void StandaloneExportMetadataAppliesToAllExportsOnAMember()
        {
            var cc = CreateContainer(typeof(MultipleExportsOneNamedAndBothPrioritized));
            var ne = cc.GetExports<Lazy<MultipleExportsOneNamedAndBothPrioritized, Prioritized>>();
            Assert.Equal(2, ne.Count());
            Assert.True(ne.All(e => e.Metadata.Priority == 10));
        }

        [Fact]
        public void MultiplePiecesOfMetadataAreCombinedIntoAnArray()
        {
            var cc = CreateContainer(typeof(MultipleNames));

            var withNames = cc.GetExport<Lazy<MultipleNames, MultiValuedName>>();

            AssertX.Equivalent(new[] { "A", "B", "B" }, withNames.Metadata.Name);
        }

        [Export]
        public class ConstructorImported { }

        [Export("A"), Export("B")]
        public class MultipleExportsNonDefaultConstructor
        {
            [ImportingConstructor]
            public MultipleExportsNonDefaultConstructor(ConstructorImported c) { }
        }

        [Fact]
        public void MultipleExportsCanBeRetrievedWhenANonDefaultConstructorExists()
        {
            var c = CreateContainer(typeof(ConstructorImported), typeof(MultipleExportsNonDefaultConstructor));
            c.GetExport<MultipleExportsNonDefaultConstructor>("A");
            c.GetExport<MultipleExportsNonDefaultConstructor>("B");
        }
    }
}
