// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.UnitTests
{
    public class LazyTests : ContainerTests
    {
        public interface IA { }

        [Export(typeof(IA))]
        public class A : IA { }

        [Export]
        public class BLazy
        {
            public Lazy<IA> A;

            [ImportingConstructor]
            public BLazy(Lazy<IA> ia)
            {
                A = ia;
            }
        }

        [Export, ExportMetadata("Name", "Fred")]
        public class NamedFred { }

        public class Named { public string Name { get; set; } }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void ComposesLazily()
        {
            var cc = CreateContainer(typeof(A), typeof(BLazy));
            var x = cc.GetExport<BLazy>();
            Assert.IsAssignableFrom(typeof(A), x.A.Value);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void SupportsExportMetadata()
        {
            var cc = CreateContainer(typeof(NamedFred));
            var fred = cc.GetExport<Lazy<NamedFred, Named>>();
            Assert.Equal("Fred", fred.Metadata.Name);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void ReturnsExportMetadataAsADictionary()
        {
            var cc = CreateContainer(typeof(NamedFred));
            var fred = cc.GetExport<Lazy<NamedFred, IDictionary<string, object>>>();
            Assert.Equal("Fred", fred.Metadata["Name"]);
        }

        [Export("Special", typeof(IA))]
        public class A1 : IA { }

        [Export("Special", typeof(IA))]
        public class A2 : IA { }

        [Export]
        public class AConsumer
        {
            [ImportMany("Special")]
            public Lazy<IA>[] ALazies { get; set; }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void LazyCanBeComposedWithImportManyAndNames()
        {
            var cc = CreateContainer(typeof(AConsumer), typeof(A1), typeof(A2));
            var cons = cc.GetExport<AConsumer>();
            Assert.Equal(2, cons.ALazies.Length);
        }
    }
}
