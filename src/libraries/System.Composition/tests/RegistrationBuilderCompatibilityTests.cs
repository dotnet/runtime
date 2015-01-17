// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Composition.Convention;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Composition.UnitTests.Util;
using Xunit;

namespace System.Composition.UnitTests
{
    public class ConventionBuilderCompatibilityTests
    {
        public class Base
        {
            public object Prop { get; set; }
        }

        [Export]
        public class Derived : Base
        {
            new public string Prop { get; set; }
        }

        [Fact]
        public void WhenConventionsAreInUseDuplicatePropertyNamesDoNotBreakDiscovery()
        {
            var rb = new ConventionBuilder();
            var c = new ContainerConfiguration()
                .WithPart(typeof(Derived), rb)
                .CreateContainer();
        }

        public interface IRepository<T> { }

        public class EFRepository<T> : IRepository<T> { }


        [Fact]
        public void ConventionBuilderExportsOpenGenerics()
        {
            var rb = new ConventionBuilder();

            rb.ForTypesDerivedFrom(typeof(IRepository<>))
                .Export(eb => eb.AsContractType(typeof(IRepository<>)));

            var c = new ContainerConfiguration()
                .WithPart(typeof(EFRepository<>), rb)
                .CreateContainer();

            var r = c.GetExport<IRepository<string>>();
        }

        public class Imported { }

        public class BaseWithImport
        {
            public virtual Imported Imported { get; set; }
        }

        public class DerivedFromBaseWithImport : BaseWithImport
        {
        }

        [Fact]
        public void ConventionsCanApplyImportsToInheritedProperties()
        {
            var conventions = new ConventionBuilder();
            conventions.ForType<Imported>().Export();
            conventions.ForType<DerivedFromBaseWithImport>()
                .ImportProperty(b => b.Imported)
                .Export();

            var container = new ContainerConfiguration()
                .WithDefaultConventions(conventions)
                .WithParts(typeof(Imported), typeof(DerivedFromBaseWithImport))
                .CreateContainer();

            var dfb = container.GetExport<DerivedFromBaseWithImport>();
            Assert.IsAssignableFrom(typeof(Imported), dfb.Imported);
        }

        public class BaseWithExport
        {
            public string Exported { get { return "A"; } }
        }

        public class DerivedFromBaseWithExport : BaseWithExport
        {
        }

        [Fact]
        public void ConventionsCanApplyExportsToInheritedProperties()
        {
            var conventions = new ConventionBuilder();
            conventions.ForType<DerivedFromBaseWithExport>()
                .ExportProperty(b => b.Exported);

            var container = new ContainerConfiguration()
                .WithDefaultConventions(conventions)
                .WithParts(typeof(DerivedFromBaseWithExport))
                .CreateContainer();

            var s = container.GetExport<string>();
            Assert.Equal("A", s);
        }

        public class BaseWithExport2
        {
            [Export]
            public virtual string Exported { get { return "A"; } }
        }

        public class DerivedFromBaseWithExport2 : BaseWithExport
        {
        }

        [Fact]
        public void ConventionsCanApplyExportsToInheritedPropertiesWithoutInterferingWithBase()
        {
            var conventions = new ConventionBuilder();
            conventions.ForType<DerivedFromBaseWithExport2>()
                .ExportProperty(b => b.Exported);

            var container = new ContainerConfiguration()
                .WithDefaultConventions(conventions)
                .WithParts(typeof(BaseWithExport2))
                .WithParts(typeof(DerivedFromBaseWithExport2))
                .CreateContainer();

            var s = container.GetExports<string>();
            Assert.Equal(2, s.Count());
        }

        [Export]
        public class BaseWithDeclaredExports
        {
            public BaseWithDeclaredExports() { Property = "foo"; }

            [Export]
            public string Property { get; set; }
        }

        public class DerivedFromBaseWithDeclaredExports : BaseWithDeclaredExports { }

        [Fact]
        public void InThePresenceOfConventionsClassExportsAreNotInherited()
        {
            var cc = new ContainerConfiguration()
                .WithPart<DerivedFromBaseWithDeclaredExports>(new ConventionBuilder())
                .CreateContainer();

            BaseWithDeclaredExports export;
            Assert.False(cc.TryGetExport(out export));
        }

        [Fact]
        public void InThePresenceOfConventionsPropertyExportsAreNotInherited()
        {
            var cc = new ContainerConfiguration()
                .WithPart<DerivedFromBaseWithDeclaredExports>(new ConventionBuilder())
                .CreateContainer();

            string export;
            Assert.False(cc.TryGetExport(out export));
        }

        public class CustomExport : ExportAttribute { }

        [CustomExport]
        public class BaseWithCustomExport { }

        public class DerivedFromBaseWithCustomExport : BaseWithCustomExport { }

        [Fact]
        public void CustomAttributesDoNotBecomeInheritedInThePresenceOfConventions()
        {
            var cc = new ContainerConfiguration()
                .WithPart<DerivedFromBaseWithCustomExport>(new ConventionBuilder())
                .CreateContainer();

            BaseWithCustomExport bce;
            Assert.False(cc.TryGetExport(out bce));
        }
    }
}
