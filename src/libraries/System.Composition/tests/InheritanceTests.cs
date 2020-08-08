// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Composition.UnitTests;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.Lightweight.UnitTests
{
    public class InheritanceTests : ContainerTests
    {
        [Export]
        public class Base
        {
            public Base() { Property = "foo"; }

            [Export]
            public string Property { get; set; }
        }

        public class Derived : Base { }

        [Fact]
        public void ClassExportsAreNotInherited()
        {
            var cc = CreateContainer(typeof(Derived));
            Base export;
            Assert.False(cc.TryGetExport(out export));
        }

        [Fact]
        public void PropertyExportsAreNotInherited()
        {
            var cc = CreateContainer(typeof(Derived));
            string export;
            Assert.False(cc.TryGetExport(out export));
        }

        [Export]
        public class ExportingDerived : Base { }

        [Fact]
        public void ExportsAtTheClassLevelAreAppliedIgnoringBaseExports()
        {
            var cc = CreateContainer(typeof(ExportingDerived));
            Base baseExport;
            Assert.False(cc.TryGetExport(out baseExport));
            ExportingDerived derivedExport;
            Assert.True(cc.TryGetExport(out derivedExport));
        }

        public class Exporter
        {
            public Exporter()
            {
                A = "a"; B = "b";
            }

            [Export("a")]
            public string A { get; set; }

            [Export("b")]
            public string B { get; set; }
        }

        public class BaseImporter
        {
            [Import("a")]
            public virtual string Imported { get; set; }
        }

        [Export]
        public class OverridingImporter : BaseImporter
        {
            [Import("b")]
            public override string Imported { get; set; }
        }

        [Export]
        public class NonOverridingImporter : BaseImporter { }

        [Fact]
        public void ImportsOnOverriddenPropertiesOverrideImportsOnTheBase()
        {
            var c = CreateContainer(typeof(Exporter), typeof(OverridingImporter), typeof(NonOverridingImporter));
            var bi = c.GetExport<NonOverridingImporter>();
            var di = c.GetExport<OverridingImporter>();
            Assert.Equal("a", bi.Imported);
            Assert.Equal("b", di.Imported);
        }

        [Fact]
        public void LooseImportsOnDerivedPropertiesOverrideImportsOnTheBase()
        {
            var c = CreateContainer(typeof(Exporter));
            var bi = new BaseImporter();
            c.SatisfyImports(bi);
            var di = new OverridingImporter();
            c.SatisfyImports(di);
            Assert.Equal("a", bi.Imported);
            Assert.Equal("b", di.Imported);
        }

        [Fact]
        public void ImportsOnBaseAreInherited()
        {
            var c = CreateContainer(typeof(Exporter), typeof(NonOverridingImporter));
            var di = c.GetExport<NonOverridingImporter>();
            c.SatisfyImports(di);
            Assert.Equal("a", di.Imported);
        }

        [Fact]
        public void LooseImportsOnBaseAreInherited()
        {
            var c = CreateContainer(typeof(Exporter));
            var di = new NonOverridingImporter();
            c.SatisfyImports(di);
            Assert.Equal("a", di.Imported);
        }

        [Export, PartNotDiscoverable]
        public class NotDiscoverableBase { }

        [Export]
        public class DiscoverableDerived : NotDiscoverableBase { }

        [Fact]
        public void PartNotDiscoverableAttributeIsNotInherited()
        {
            var c = CreateContainer(typeof(DiscoverableDerived));
            DiscoverableDerived derived;
            Assert.True(c.TryGetExport(out derived));
        }

        [Shared]
        public class SharedBase { }

        [Export]
        public class SharedDerived : SharedBase { }

        [Fact]
        public void PartMetadataIsNotInherited()
        {
            var c = CreateContainer(typeof(SharedDerived));
            var ns = c.GetExport<SharedDerived>();
            var ns2 = c.GetExport<SharedDerived>();
            Assert.NotSame(ns, ns2);
        }

        public class HasImportsSatisfied
        {
            public bool ImportsSatisfied { get; set; }

            [OnImportsSatisfied]
            public void Done() { ImportsSatisfied = true; }
        }

        [Export]
        public class InheritsImportsSatisfied : HasImportsSatisfied { }

        [Fact]
        public void OnImportsSatisfiedAttributeIsInherited()
        {
            var c = CreateContainer(typeof(InheritsImportsSatisfied));
            var x = c.GetExport<InheritsImportsSatisfied>();
            Assert.True(x.ImportsSatisfied);
        }

        public interface IHandler { }

        public class HandlerMetadata { public string HandledMessage { get; set; } }

        [Export(typeof(IHandler)), ExportMetadata("HandledMessage", "A")]
        public class AHandler : IHandler { }

        [Export(typeof(IHandler)), ExportMetadata("HandledMessage", "B")]
        public class ABHandler : AHandler { }

        [Fact]
        public void MetadataIsOnlyDrawnFromTheTypeToWhichItIsApplied()
        {
            var c = CreateContainer(typeof(ABHandler));
            var handlers = c.GetExports<Lazy<IHandler, HandlerMetadata>>().ToArray();
            Assert.Equal(1, handlers.Length);
            Assert.Contains(handlers, h => h.Metadata.HandledMessage == "B");
        }

        public class BaseVirtualExporter
        {
            [Export("a")]
            public virtual string Exported { get; set; }
        }

        public class DerivedOverrideExporter : BaseVirtualExporter
        {
            [Export("b")]
            public override string Exported { get; set; }
        }

        [Fact]
        public void ExportsOnOverridePropertiesOverrideExportsOnTheBase()
        {
            var c = CreateContainer(typeof(DerivedOverrideExporter));
            string value;
            Assert.False(c.TryGetExport("a", out value));
            Assert.True(c.TryGetExport("b", out value));
        }
    }
}
