// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Composition.Registration.Tests
{
    public class RegistrationBuilderTests
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

        private class FooImplWithConstructors2 : IFoo
        {
            public FooImplWithConstructors2() { }
            public FooImplWithConstructors2(IEnumerable<IFoo> ids) { }
            public FooImplWithConstructors2(int id, string name) { }
        }

        private class RealPart
        {
        }

        private class DiscoveredCatalog : AssemblyCatalog
        {
            public DiscoveredCatalog()
                : base("") { }
        }

        [Fact]
        public void ShouldSucceed()
        {
            var rb = new RegistrationBuilder();
            rb.ForType<RealPart>().Export();
            var cat = new AssemblyCatalog(typeof(RegistrationBuilderTests).Assembly, rb);
            var container = new CompositionContainer(cat);

            // Throws:
            // Can not determine which constructor to use for the type 'System.ComponentModel.Composition.Hosting.AssemblyCatalog, System.ComponentModel.Composition, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'.
            RealPart rp = container.GetExport<RealPart>().Value;
        }

        [Fact]
        public void MapType_ShouldReturnProjectedAttributesForType()
        {
            var builder = new RegistrationBuilder();

            builder.ForTypesDerivedFrom<IFoo>()
                .Export<IFoo>();

            TypeInfo projectedType1 = builder.MapType(typeof(FooImpl).GetTypeInfo());
            TypeInfo projectedType2 = builder.MapType(typeof(FooImplWithConstructors).GetTypeInfo());

            var exports = new List<object>();

            exports.AddRange(projectedType1.GetCustomAttributes(typeof(ExportAttribute), false));
            exports.AddRange(projectedType2.GetCustomAttributes(typeof(ExportAttribute), false));
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
            var builder = new RegistrationBuilder();

            builder.ForTypesDerivedFrom<IFoo>()
                .Export<IFoo>();

            TypeInfo projectedType1 = builder.MapType(typeof(FooImpl).GetTypeInfo());
            TypeInfo projectedType2 = builder.MapType(typeof(FooImplWithConstructors).GetTypeInfo());

            // necessary as BuildConventionConstructorAttributes is only called for type level query for attributes
            var typeLevelAttrs = projectedType2.GetCustomAttributes(false);

            ConstructorInfo constructor1 = projectedType2.GetConstructors().Where(c => c.GetParameters().Length == 0).Single();
            ConstructorInfo constructor2 = projectedType2.GetConstructors().Where(c => c.GetParameters().Length == 1).Single();
            ConstructorInfo constructor3 = projectedType2.GetConstructors().Where(c => c.GetParameters().Length == 2).Single();

            Assert.Equal(0, constructor1.GetCustomAttributes(false).Length);
            Assert.Equal(0, constructor2.GetCustomAttributes(false).Length);

            ConstructorInfo ci = constructor3;
            var attrs = ci.GetCustomAttributes(false);
            Assert.Equal(1, attrs.Length);
            Assert.Equal(typeof(ImportingConstructorAttribute), attrs[0].GetType());
        }

        [Fact]
        public void MapType_OverridingSelectionOfConventionSelectedConstructor()
        {
            var builder = new RegistrationBuilder();

            builder.ForTypesDerivedFrom<IFoo>()
                .Export<IFoo>();

            builder.ForType<FooImplWithConstructors>()
                .SelectConstructor(cis => cis[1]);

            TypeInfo projectedType1 = builder.MapType(typeof(FooImpl).GetTypeInfo().GetTypeInfo());
            TypeInfo projectedType2 = builder.MapType(typeof(FooImplWithConstructors).GetTypeInfo().GetTypeInfo());

            ConstructorInfo constructor1 = projectedType2.GetConstructors().Where(c => c.GetParameters().Length == 0).Single();
            ConstructorInfo constructor2 = projectedType2.GetConstructors().Where(c => c.GetParameters().Length == 1).Single();
            ConstructorInfo constructor3 = projectedType2.GetConstructors().Where(c => c.GetParameters().Length == 2).Single();

            // necessary as BuildConventionConstructorAttributes is only called for type level query for attributes
            var typeLevelAttrs = projectedType2.GetCustomAttributes(false);

            Assert.Equal(0, constructor1.GetCustomAttributes(false).Length);
            Assert.Equal(0, constructor3.GetCustomAttributes(false).Length);

            ConstructorInfo ci = constructor2;
            var attrs = ci.GetCustomAttributes(false);
            Assert.Equal(1, attrs.Length);
            Assert.IsType<ImportingConstructorAttribute>(attrs[0]);
        }

        [Fact]
        public void MapType_OverridingSelectionOfConventionSelectedConstructor_WithPartBuilderOfT()
        {
            var builder = new RegistrationBuilder();

            builder.ForTypesDerivedFrom<IFoo>()
                .Export<IFoo>();

            builder.ForType<FooImplWithConstructors2>().
                SelectConstructor(param => new FooImplWithConstructors2(param.Import<IEnumerable<IFoo>>()));

            TypeInfo projectedType1 = builder.MapType(typeof(FooImpl).GetTypeInfo().GetTypeInfo());
            TypeInfo projectedType2 = builder.MapType(typeof(FooImplWithConstructors2).GetTypeInfo().GetTypeInfo());

            ConstructorInfo constructor1 = projectedType2.GetConstructors().Where(c => c.GetParameters().Length == 0).Single();
            ConstructorInfo constructor2 = projectedType2.GetConstructors().Where(c => c.GetParameters().Length == 1).Single();
            ConstructorInfo constructor3 = projectedType2.GetConstructors().Where(c => c.GetParameters().Length == 2).Single();

            // necessary as BuildConventionConstructorAttributes is only called for type level query for attributes
            var typeLevelAttrs = projectedType2.GetCustomAttributes(false);

            Assert.Equal(0, constructor1.GetCustomAttributes(false).Length);
            Assert.Equal(0, constructor3.GetCustomAttributes(false).Length);

            ConstructorInfo ci = constructor2;
            var attrs = ci.GetCustomAttributes(false);
            Assert.Equal(1, attrs.Length);
            Assert.IsType<ImportingConstructorAttribute>(attrs[0]);
        }

        private interface IGenericInterface<T> { }

        [Export(typeof(IGenericInterface<>))]
        private class ClassExportingInterface<T> : IGenericInterface<T> { }

        [Fact]
        public void GenericInterfaceExportInRegistrationBuilder()
        {
            CompositionContainer container = CreateRegistrationBuilderContainer(typeof(ClassExportingInterface<>));
            IGenericInterface<string> v = container.GetExportedValue<IGenericInterface<string>>();
            Assert.IsAssignableFrom<IGenericInterface<string>>(v);
        }

        private class GenericBaseClass<T> { }

        [Export(typeof(GenericBaseClass<>))]
        private class ClassExportingBaseClass<T> : GenericBaseClass<T> { }

        [Fact]
        public void GenericBaseClassExportInRegistrationBuilder()
        {
            CompositionContainer container = CreateRegistrationBuilderContainer(typeof(ClassExportingBaseClass<>));
            GenericBaseClass<string> v = container.GetExportedValue<GenericBaseClass<string>>();
            Assert.IsAssignableFrom<GenericBaseClass<string>>(v);
        }

        [Export]
        private class GenericClass<T> { }

        [Fact]
        public void GenericExportInRegistrationBuilder()
        {
            CompositionContainer container = CreateRegistrationBuilderContainer(typeof(GenericClass<>));
            GenericClass<string> v = container.GetExportedValue<GenericClass<string>>();
            Assert.IsType<GenericClass<string>>(v);
        }

        [Export(typeof(ExplicitGenericClass<>))]
        private class ExplicitGenericClass<T> { }

        [Fact]
        public void ExplicitGenericExportInRegistrationBuilder()
        {
            CompositionContainer container = CreateRegistrationBuilderContainer(typeof(ExplicitGenericClass<>));
            ExplicitGenericClass<string> v = container.GetExportedValue<ExplicitGenericClass<string>>();
            Assert.IsType<ExplicitGenericClass<string>>(v);
        }

        [Export(typeof(ExplicitGenericClass<,>))]
        private class ExplicitGenericClass<T, U> { }

        [Fact]
        public void ExplicitGenericArity2ExportInRegistrationBuilder()
        {
            CompositionContainer container = CreateRegistrationBuilderContainer(typeof(ExplicitGenericClass<,>));
            ExplicitGenericClass<int, string> v = container.GetExportedValue<ExplicitGenericClass<int, string>>();
            Assert.IsType<ExplicitGenericClass<int, string>>(v);
        }

        private CompositionContainer CreateRegistrationBuilderContainer(params Type[] types)
        {
            var reg = new RegistrationBuilder();
            var container = new CompositionContainer(new TypeCatalog(types, reg));
            return container;
        }
    }
}
