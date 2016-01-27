// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition;
using System.Composition.Convention;
using System.Composition.Convention.UnitTests;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Composition.Convention.UnitTests
{
    public class ConventionBuilderTests
    {
        private interface IFoo { }
        private class FooImpl : IFoo
        {
            public string P1 { get; set; }
            public string P2 { get; set; }
            public IEnumerable<IFoo> P3 { get; set; }
        }
        private class FooImplWithConstructors : IFoo
        {
            public FooImplWithConstructors() { }
            public FooImplWithConstructors(IEnumerable<IFoo> ids) { }
            public FooImplWithConstructors(int id, string name) { }
        }

        [Fact]
        public void MapType_ShouldReturnProjectedAttributesForType()
        {
            var builder = new ConventionBuilder();

            builder.
                ForTypesDerivedFrom<IFoo>().
                Export<IFoo>();

            var fooImplAttributes = builder.GetDeclaredAttributes(typeof(FooImpl), typeof(FooImpl).GetTypeInfo());
            var fooImplWithConstructorsAttributes = builder.GetDeclaredAttributes(typeof(FooImplWithConstructors), typeof(FooImplWithConstructors).GetTypeInfo());

            var exports = new List<object>();

            exports.AddRange(fooImplAttributes);
            exports.AddRange(fooImplWithConstructorsAttributes);
            Assert.Equal(2, exports.Count);

            foreach (var exportAttribute in exports)
            {
                Assert.Equal(typeof(IFoo), ((ExportAttribute)exportAttribute).ContractType);
                Assert.Null(((ExportAttribute)exportAttribute).ContractName);
            }
        }

        [Fact]
        public void MapType_ConventionSelectedConstructor()
        {
            var builder = new ConventionBuilder();

            builder.
                ForTypesDerivedFrom<IFoo>().
                Export<IFoo>();

            var fooImplWithConstructorsTypeInfo = typeof(FooImplWithConstructors).GetTypeInfo();

            // necessary as BuildConventionConstructorAttributes is only called for type level query for attributes
            var constructor1 = fooImplWithConstructorsTypeInfo.DeclaredConstructors.Where(c => c.GetParameters().Length == 0).Single();
            var constructor2 = fooImplWithConstructorsTypeInfo.DeclaredConstructors.Where(c => c.GetParameters().Length == 1).Single();
            var constructor3 = fooImplWithConstructorsTypeInfo.DeclaredConstructors.Where(c => c.GetParameters().Length == 2).Single();

            Assert.Equal(0, builder.GetCustomAttributes(typeof(FooImplWithConstructors), constructor1).Count());
            Assert.Equal(0, builder.GetCustomAttributes(typeof(FooImplWithConstructors), constructor2).Count());

            var ci = constructor3;
            var attrs = builder.GetCustomAttributes(typeof(FooImplWithConstructors), ci);
            Assert.Equal(1, attrs.Count());
            Assert.Equal(typeof(ImportingConstructorAttribute), attrs.FirstOrDefault().GetType());
        }

        [Fact]
        public void MapType_OverridingSelectionOfConventionSelectedConstructor()
        {
            var builder = new ConventionBuilder();

            builder.
                ForTypesDerivedFrom<IFoo>().
                Export<IFoo>();

            builder.ForType<FooImplWithConstructors>()
                .SelectConstructor(cis => cis.Single(c => c.GetParameters().Length == 1));

            var fooImplWithConstructors = typeof(FooImplWithConstructors).GetTypeInfo();

            var constructor1 = fooImplWithConstructors.DeclaredConstructors.Where(c => c.GetParameters().Length == 0).Single();
            var constructor2 = fooImplWithConstructors.DeclaredConstructors.Where(c => c.GetParameters().Length == 1).Single();
            var constructor3 = fooImplWithConstructors.DeclaredConstructors.Where(c => c.GetParameters().Length == 2).Single();

            // necessary as BuildConventionConstructorAttributes is only called for type level query for attributes
            Assert.Equal(0, builder.GetCustomAttributes(typeof(FooImplWithConstructors), constructor1).Count());
            Assert.Equal(0, builder.GetCustomAttributes(typeof(FooImplWithConstructors), constructor3).Count());

            var ci = constructor2;
            var attrs = builder.GetCustomAttributes(typeof(FooImplWithConstructors), ci);
            Assert.Equal(1, attrs.Count());
            Assert.Equal(typeof(ImportingConstructorAttribute), attrs.FirstOrDefault().GetType());
        }

        [Fact]
        public void MapType_OverridingSelectionOfConventionSelectedConstructor_WithPartBuilderOfT()
        {
            var builder = new ConventionBuilder();

            builder.
                ForTypesDerivedFrom<IFoo>().
                Export<IFoo>();

            builder.ForType<FooImplWithConstructors>().
                SelectConstructor(param => new FooImplWithConstructors(param.Import<IEnumerable<IFoo>>()));

            var fooImplWithConstructors = typeof(FooImplWithConstructors).GetTypeInfo();

            var constructor1 = fooImplWithConstructors.DeclaredConstructors.Where(c => c.GetParameters().Length == 0).Single();
            var constructor2 = fooImplWithConstructors.DeclaredConstructors.Where(c => c.GetParameters().Length == 1).Single();
            var constructor3 = fooImplWithConstructors.DeclaredConstructors.Where(c => c.GetParameters().Length == 2).Single();

            // necessary as BuildConventionConstructorAttributes is only called for type level query for attributes
            Assert.Equal(0, builder.GetCustomAttributes(typeof(FooImplWithConstructors), constructor1).Count());
            Assert.Equal(0, builder.GetCustomAttributes(typeof(FooImplWithConstructors), constructor3).Count());

            var ci = constructor2;
            var attrs = builder.GetCustomAttributes(typeof(FooImplWithConstructors), ci);
            Assert.Equal(1, attrs.Count());
            Assert.Equal(typeof(ImportingConstructorAttribute), attrs.FirstOrDefault().GetType());
        }
    }
}
