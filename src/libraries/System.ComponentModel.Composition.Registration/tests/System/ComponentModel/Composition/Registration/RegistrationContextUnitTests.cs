// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Hosting;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition.Registration.Tests
{
    public interface IFoo { }
    public class FooImplementation1 : IFoo { }
    public class FooImplementation2 : IFoo { }

    public class RegistrationBuilderUnitTests
    {
        [Fact]
        public void UndecoratedContext_ShouldFindZeroParts()
        {
            var ctx = new RegistrationBuilder();
            var catalog = new TypeCatalog(new[] { typeof(IFoo), typeof(FooImplementation1), typeof(FooImplementation2) }, ctx);
            Assert.True(catalog.Parts.Count() == 0);
        }

        [Fact]
        public void ImplementsIFooNoExport_ShouldFindZeroParts()
        {
            var ctx = new RegistrationBuilder();
            ctx.ForTypesDerivedFrom<IFoo>();
            var catalog = new TypeCatalog(new[] { typeof(IFoo), typeof(FooImplementation1), typeof(FooImplementation2) }, ctx);
            Assert.True(catalog.Parts.Count() == 0);
        }

        [Fact]
        public void ImplementsIFooWithExport_ShouldFindTwoParts()
        {
            var ctx = new RegistrationBuilder();
            ctx.ForTypesDerivedFrom<IFoo>().Export();
            var catalog = new TypeCatalog(new[] { typeof(IFoo), typeof(FooImplementation1), typeof(FooImplementation2) }, ctx);
            Assert.True(catalog.Parts.Count() == 2);
        }

        [Fact]
        public void OfTypeInterfaceNoExport_ShouldFindZeroParts()
        {
            var ctx = new RegistrationBuilder();
            ctx.ForType<IFoo>();
            var catalog = new TypeCatalog(new[] { typeof(IFoo), typeof(FooImplementation1), typeof(FooImplementation2) }, ctx);
            Assert.True(catalog.Parts.Count() == 0);
        }


        [Fact]
        public void OfTypeInterfaceWithExport_ShouldFindZeroParts()
        {
            var ctx = new RegistrationBuilder();
            ctx.ForType<IFoo>().Export();
            var catalog = new TypeCatalog(new[] { typeof(IFoo), typeof(FooImplementation1), typeof(FooImplementation2) }, ctx);
            Assert.True(catalog.Parts.Count() == 0);
        }

        [Fact]
        public void OfTypeOnePart_ShouldFindOnePart()
        {
            var ctx = new RegistrationBuilder();
            ctx.ForType<FooImplementation1>().Export();
            var catalog = new TypeCatalog(new[] { typeof(IFoo), typeof(FooImplementation1), typeof(FooImplementation2) }, ctx);
            Assert.True(catalog.Parts.Count() == 1);
        }

        [Fact]
        public void OfTypeTwoPart_ShouldFindTwoParts()
        {
            var ctx = new RegistrationBuilder();
            ctx.ForType<FooImplementation1>().Export();
            ctx.ForType<FooImplementation2>().Export();
            var catalog = new TypeCatalog(new[] { typeof(IFoo), typeof(FooImplementation1), typeof(FooImplementation2) }, ctx);
            Assert.True(catalog.Parts.Count() == 2);
        }

        [Fact]
        public void OfTypeTwoPart_ConfigurationAfterConstruction_ShouldFindTwoParts()
        {
            var ctx = new RegistrationBuilder();
            var catalog = new TypeCatalog(new[] { typeof(IFoo), typeof(FooImplementation1), typeof(FooImplementation2) }, ctx);
            ctx.ForType<FooImplementation1>().Export();
            ctx.ForType<FooImplementation2>().Export();
            Assert.True(catalog.Parts.Count() == 2);
        }

        [Fact]
        public void OfTypeTwoPart_ConfigurationAfterParts_ShouldFindZeroParts()
        {
            var ctx = new RegistrationBuilder();
            var catalog = new TypeCatalog(new[] { typeof(IFoo), typeof(FooImplementation1), typeof(FooImplementation2) }, ctx);
            Assert.True(catalog.Parts.Count() == 0);
            ctx.ForType<FooImplementation1>().Export();
            ctx.ForType<FooImplementation2>().Export();
            Assert.True(catalog.Parts.Count() == 0);
        }

        [Fact]
        public void WhereNullArgument_ShouldThrowArgumentException()
        {
            Assert.Throws<ArgumentNullException>("typeFilter", () =>
            {
                var ctx = new RegistrationBuilder();
                ctx.ForTypesMatching(null);
                var catalog = new TypeCatalog(new[] { typeof(IFoo), typeof(FooImplementation1), typeof(FooImplementation2) }, ctx);
                Assert.True(catalog.Parts.Count() == 0);
            });
        }

        [Fact]
        public void WhereIsClassAndImplementsIFooNoExport_ShouldFindZeroParts()
        {
            var ctx = new RegistrationBuilder();
            // Implements<IFoo>
            ctx.ForTypesMatching((t) => { return t.IsClass && typeof(IFoo).IsAssignableFrom(t); });
            var catalog = new TypeCatalog(new[] { typeof(IFoo), typeof(FooImplementation1), typeof(FooImplementation2) }, ctx);
            Assert.True(catalog.Parts.Count() == 0);
        }

        [Fact]
        public void WhereIsClassAndImplementsIFooWithExport_ShouldFindTwoParts()
        {
            var ctx = new RegistrationBuilder();
            // Implements<IFoo>
            ctx.ForTypesMatching((t) => { return t.IsClass && typeof(IFoo).IsAssignableFrom(t); }).Export();
            var catalog = new TypeCatalog(new[] { typeof(IFoo), typeof(FooImplementation1), typeof(FooImplementation2) }, ctx);
            Assert.True(catalog.Parts.Count() == 2);
        }

        [Fact]
        public void WhereIsTypeWithExport_ShouldFindTwoParts()
        {
            var ctx = new RegistrationBuilder();
            // Implements<FooImplementation1>
            ctx.ForTypesMatching((t) => { return t.IsAssignableFrom(typeof(FooImplementation1)); }).Export();
            // Implements<FooImplementation2>
            ctx.ForTypesMatching((t) => { return t.IsAssignableFrom(typeof(FooImplementation2)); }).Export();
            var catalog = new TypeCatalog(new[] { typeof(IFoo), typeof(FooImplementation1), typeof(FooImplementation2) }, ctx);
            Assert.True(catalog.Parts.Count() == 2);
        }
    }
}
