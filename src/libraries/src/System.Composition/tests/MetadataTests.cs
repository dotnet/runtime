// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Xunit;
using System.Composition.UnitTests.Util;

namespace System.Composition.UnitTests
{
    public class MetadataTests : ContainerTests
    {
        public class CircularM { public string Name { get; set; } }

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

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void HandlesMetadataCircularity()
        {
            var cc = CreateContainer(typeof(MetadataCircularityA), typeof(MetadataCircularityB));
            var a = cc.GetExport<MetadataCircularityA>();

            Assert.Equal(a.B.Metadata.Name, "B");
            Assert.Equal(a.B.Value.A.Metadata.Name, "A");
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        [MetadataAttribute]
        public class NameNullAttribute : Attribute
        {
            public string Name => null;
        }

        [MetadataAttribute]
        public class ExportWithNameFooAttribute : ExportAttribute
        {
            public string Name { get { return "Foo"; } }
        }

        [Export, NameNull, NameNull]
        public class NameNullTwiceExport { }

        [ExportWithNameFoo]
        public class SingleNamedExport { }

        public class Named {[DefaultValue(null)] public string Name { get; set; } }

        public class MultiNamed {[DefaultValue(null)] public IEnumerable<string> Name { get; set; } }

        [ExportWithNameFoo, Export, ExportMetadata("Priority", 10)]
        public class MultipleExportsOneNamedAndBothPrioritized { }

        [ExportWithNameFoo, ExportMetadata("Priority", 10)]
        public class NamedAndPrioritized { }

        [MetadataAttribute]
        public class NameFooAttribute : Attribute
        {
            public string Name { get { return "Foo"; } }
        }

        [Export,
         ExportMetadata("Name", "A"),
         ExportMetadata("Name", "B"),
         ExportMetadata("Name", "B")]
        public class MultipleNames { }

        [Export, NameFoo]
        public class NamedWithCustomMetadata { }

        public class MultiValuedName { public string[] Name { get; set; } }

        public class Prioritized {[DefaultValue(0)] public int Priority { get; set; } }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void MultipleMetadataAttributesWithAPropertyThatReturnsNull()
        {
            var cc = CreateContainer(typeof(NameNullTwiceExport));
            var ne = cc.GetExport<Lazy<NameNullTwiceExport, MultiNamed>>();
            Assert.Equal(new string[] { null, null }, ne.Metadata.Name);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void DiscoversMetadataSpecifiedUsingMetadataAttributeOnExportAttribute()
        {
            var cc = CreateContainer(typeof(SingleNamedExport));
            var ne = cc.GetExport<Lazy<SingleNamedExport, Named>>();
            Assert.Equal("Foo", ne.Metadata.Name);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void IfMetadataIsSpecifiedOnAnExportAttributeOtherExportsDoNotHaveIt()
        {
            var cc = CreateContainer(typeof(MultipleExportsOneNamedAndBothPrioritized));
            var ne = cc.GetExports<Lazy<MultipleExportsOneNamedAndBothPrioritized, Named>>();
            Assert.Equal(2, ne.Count());
            Assert.True(ne.Where(e => e.Metadata.Name != null).Count() == 1);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void DiscoversStandaloneExportMetadata()
        {
            var cc = CreateContainer(typeof(NamedAndPrioritized));
            var ne = cc.GetExport<Lazy<NamedAndPrioritized, Prioritized>>();
            Assert.Equal(10, ne.Metadata.Priority);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void DiscoversStandaloneExportMetadataUsingMetadataAttributes()
        {
            var cc = CreateContainer(typeof(NamedWithCustomMetadata));
            var ne = cc.GetExport<Lazy<NamedWithCustomMetadata, Named>>();
            Assert.Equal("Foo", ne.Metadata.Name);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void StandaloneExportMetadataAppliesToAllExportsOnAMember()
        {
            var cc = CreateContainer(typeof(MultipleExportsOneNamedAndBothPrioritized));
            var ne = cc.GetExports<Lazy<MultipleExportsOneNamedAndBothPrioritized, Prioritized>>();
            Assert.Equal(2, ne.Count());
            Assert.True(ne.All(e => e.Metadata.Priority == 10));
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void MultiplePiecesOfMetadataAreCombinedIntoAnArray()
        {
            var cc = CreateContainer(typeof(MultipleNames));

            var withNames = cc.GetExport<Lazy<MultipleNames, MultiValuedName>>();

            AssertX.Equivalent(new[] { "A", "B", "B" }, withNames.Metadata.Name);
        }

        [Export, ExportMetadata("Name", "Fred")]
        public class NamedFred { }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void SupportsExportMetadata()
        {
            var cc = CreateContainer(typeof(NamedFred));
            var fred = cc.GetExport<Lazy<NamedFred, Named>>();
            Assert.Equal("Fred", fred.Metadata.Name);
        }
    }
}
