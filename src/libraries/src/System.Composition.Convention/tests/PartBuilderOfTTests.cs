// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition.Convention.UnitTests;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Composition.Convention
{
    public class PartBuilderOfTTests
    {
        public interface IFirst { }

        private interface IFoo { }
        private class FooImpl
        {
            public string P1 { get; set; }
            public string P2 { get; set; }
            public IEnumerable<IFoo> P3 { get; set; }
        }
        private class FooImplWithConstructors
        {
            public FooImplWithConstructors() { }
            public FooImplWithConstructors(int id) { }
            public FooImplWithConstructors(IEnumerable<IFoo> ids) { }
            public FooImplWithConstructors(int id, string name) { }
        }

        [Export]
        public class OnImportsSatisfiedMultipleClass
        {
            public int OnImportsSatisfiedInvoked = 0;

            [Import("P1", AllowDefault = true)]
            public string P1 { get; set; }
            [Import("P2", AllowDefault = true)]
            public string P2 { get; set; }

            public int OnImportsSatisfiedInvalidReturnValue() { return 1; }
            public void OnImportsSatisfiedInvalidArgs(int arg1) { }

            public void OnImportsSatisfied1()
            {
                OnImportsSatisfiedInvoked += 2;
            }

            public void OnImportsSatisfied2()
            {
                OnImportsSatisfiedInvoked += 4;
            }
        }

        [Export]
        public class OnImportsSatisfiedConfiguredClass
        {
            public int OnImportsSatisfiedInvoked = 0;

            [Import("P1", AllowDefault = true)]
            public string P1 { get; set; }
            [Import("P2", AllowDefault = true)]
            public string P2 { get; set; }

            public int OnImportsSatisfiedInvalidReturnValue() { return 1; }
            public void OnImportsSatisfiedInvalidArgs(int arg1) { }

            [OnImportsSatisfied]
            public void OnImportsSatisfied()
            {
                ++OnImportsSatisfiedInvoked;
            }
        }

        [Export]
        public class OnImportsSatisfiedTestClass
        {
            public int OnImportsSatisfiedInvoked = 0;

            [Import("P1", AllowDefault = true)]
            public string P1 { get; set; }
            [Import("P2", AllowDefault = true)]
            public string P2 { get; set; }

            public int OnImportsSatisfiedInvalidReturnValue() { return 1; }
            public void OnImportsSatisfiedInvalidArgs(int arg1) { }

            public void OnImportsSatisfied()
            {
                ++OnImportsSatisfiedInvoked;
            }
        }

        [Export]
        public class OnImportsSatisfiedDerivedClass : OnImportsSatisfiedTestClass
        {
        }

        public class ExportValues
        {
            public ExportValues()
            {
                P1 = "Hello, World from P1";
                P2 = "Hello, World from P2";
            }

            [Export("P1")]
            public string P1 { get; set; }
            [Export("P2")]
            public string P2 { get; set; }
        }


        [Fact]
        public void NoOperations_ShouldGenerateNoAttributesOnAnyMember()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>();

            var attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P1");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P2");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P3");
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void ExportSelf_ShouldGenerateSingleExportAttribute()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().Export();

            var attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
            Assert.Equal(1, attributes.Count());
            Assert.NotNull(attributes[0] as ExportAttribute);

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P1");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P2");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P3");
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void ExportOfT_ShouldGenerateSingleExportAttributeWithContractType()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().Export<IFoo>();

            var attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);

            Assert.Equal(1, attributes.Count());

            var exportAttribute = attributes[0] as ExportAttribute;
            Assert.NotNull(exportAttribute);
            Assert.Equal(typeof(IFoo), exportAttribute.ContractType);
            Assert.Null(exportAttribute.ContractName);


            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P1");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P2");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P3");
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void AddMetadata_ShouldGeneratePartMetadataAttribute()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().Export<IFoo>().AddPartMetadata("name", "value");

            var attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
            Assert.Equal(2, attributes.Count());

            var exportAttribute = attributes.First((t) => t.GetType() == typeof(ExportAttribute)) as ExportAttribute;
            Assert.Equal(typeof(IFoo), exportAttribute.ContractType);
            Assert.Null(exportAttribute.ContractName);

            var mdAttribute = attributes.First((t) => t.GetType() == typeof(PartMetadataAttribute)) as PartMetadataAttribute;
            Assert.Equal("name", mdAttribute.Name);
            Assert.Equal("value", mdAttribute.Value);

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P1");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P2");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P3");
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void AddMetadataWithFunc_ShouldGeneratePartMetadataAttribute()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().Export<IFoo>().AddPartMetadata("name", t => t.Name);

            var attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
            Assert.Equal(2, attributes.Count());

            var exportAttribute = attributes.First((t) => t.GetType() == typeof(ExportAttribute)) as ExportAttribute;
            Assert.Equal(typeof(IFoo), exportAttribute.ContractType);
            Assert.Null(exportAttribute.ContractName);

            var mdAttribute = attributes.First((t) => t.GetType() == typeof(PartMetadataAttribute)) as PartMetadataAttribute;
            Assert.Equal("name", mdAttribute.Name);
            Assert.Equal(typeof(FooImpl).Name, mdAttribute.Value);

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P1");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P2");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P3");
            Assert.Equal(0, attributes.Count());
        }


        [Fact]
        public void ExportProperty_ShouldGenerateExportForPropertySelected()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().ExportProperty(p => p.P1);

            var attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P1");
            Assert.Equal(1, attributes.Count());

            var exportAttribute = attributes.First((t) => t.GetType() == typeof(ExportAttribute)) as ExportAttribute;
            Assert.Null(exportAttribute.ContractName);
            Assert.Null(exportAttribute.ContractType);

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P2");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P3");
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void ImportProperty_ShouldGenerateImportForPropertySelected()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().ImportProperty(p => p.P1);

            var attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P1");
            Assert.Equal(1, attributes.Count());

            var importAttribute = attributes.First((t) => t.GetType() == typeof(ImportAttribute)) as ImportAttribute;
            Assert.Null(importAttribute.ContractName);

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P2");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P3");
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void ImportProperty_ShouldGenerateImportForPropertySelected_And_ApplyImportMany()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().ImportProperty(p => p.P3);

            var attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P1");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P2");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P3");
            Assert.Equal(1, attributes.Count());

            var importAttribute = attributes.First((t) => t.GetType() == typeof(ImportManyAttribute)) as ImportManyAttribute;
            Assert.Null(importAttribute.ContractName);
        }

        [Fact]
        public void ExportPropertyWithConfiguration_ShouldGenerateExportForPropertySelected()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().ExportProperty(p => p.P1, c => c.AsContractName("hey").AsContractType<IFoo>());

            var attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P1");
            Assert.Equal(1, attributes.Count());

            var exportAttribute = attributes.First((t) => t.GetType() == typeof(ExportAttribute)) as ExportAttribute;
            Assert.Same("hey", exportAttribute.ContractName);
            Assert.Same(typeof(IFoo), exportAttribute.ContractType);

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P2");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P3");
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void ExportPropertyWithConfiguration_ShouldGenerateExportForAllProperties()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImpl>().ExportProperty(p => p.P1, c => c.AsContractName("hey").AsContractType<IFoo>());

            var attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P1");
            Assert.Equal(1, attributes.Count());

            var exportAttribute = attributes.First((t) => t.GetType() == typeof(ExportAttribute)) as ExportAttribute;
            Assert.Same("hey", exportAttribute.ContractName);
            Assert.Same(typeof(IFoo), exportAttribute.ContractType);

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P2");
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P3");
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void ConventionSelectsConstructor_SelectsTheOneWithMostParameters()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImplWithConstructors>();

            var selectedConstructor = GetSelectedConstructor(builder, typeof(FooImplWithConstructors));
            Assert.NotNull(selectedConstructor);
            Assert.Equal(2, selectedConstructor.GetParameters().Length);     // Should select public FooImplWithConstructors(int id, string name) { }


            var attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void ManuallySelectingConstructor_SelectsTheExplicitOne()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImplWithConstructors>().SelectConstructor(param => new FooImplWithConstructors(param.Import<int>()));

            var selectedConstructor = GetSelectedConstructor(builder, typeof(FooImplWithConstructors));
            Assert.NotNull(selectedConstructor);
            Assert.Equal(1, selectedConstructor.GetParameters().Length);     // Should select public FooImplWithConstructors(IEnumerable<IFoo>) { }

            var pi = selectedConstructor.GetParameters()[0];
            Assert.Equal(typeof(int), pi.ParameterType);

            var attributes = builder.GetDeclaredAttributes(typeof(FooImplWithConstructors), pi);
            Assert.Equal(1, attributes.Count());
            Assert.NotNull(attributes[0] as ImportAttribute);

            attributes = GetAttributesFromMember(builder, typeof(FooImplWithConstructors), null);
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void ManuallySelectingConstructor_SelectsTheExplicitOne_IEnumerableParameterBecomesImportMany()
        {
            var builder = new ConventionBuilder();
            builder.ForType<FooImplWithConstructors>().SelectConstructor(param => new FooImplWithConstructors(param.Import<IEnumerable<IFoo>>()));

            var selectedConstructor = GetSelectedConstructor(builder, typeof(FooImplWithConstructors));
            Assert.NotNull(selectedConstructor);
            Assert.Equal(1, selectedConstructor.GetParameters().Length);     // Should select public FooImplWithConstructors(IEnumerable<IFoo>) { }

            var pi = selectedConstructor.GetParameters()[0];
            Assert.Equal(typeof(IEnumerable<IFoo>), pi.ParameterType);

            var attributes = builder.GetDeclaredAttributes(typeof(FooImplWithConstructors), pi);
            Assert.Equal(1, attributes.Count());
            Assert.NotNull(attributes[0] as ImportManyAttribute);

            attributes = GetAttributesFromMember(builder, typeof(FooImplWithConstructors), null);
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void ExportInterfaceSelectorNull_ShouldThrowArgumentNull()
        {
            var builder = new ConventionBuilder();
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("interfaceFilter", () => builder.ForTypesMatching((t) => true).ExportInterfaces(null));
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("interfaceFilter", () => builder.ForTypesMatching((t) => true).ExportInterfaces(null, null));
        }

        [Fact]
        public void ImportSelectorNull_ShouldThrowArgumentNull()
        {
            var builder = new ConventionBuilder();
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("propertySelector", () => builder.ForTypesMatching<IFoo>((t) => true).ImportProperty(null));
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("propertySelector", () => builder.ForTypesMatching<IFoo>((t) => true).ImportProperty(null, null));
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("propertySelector", () => builder.ForTypesMatching<IFoo>((t) => true).ImportProperty<IFirst>(null));
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("propertySelector", () => builder.ForTypesMatching<IFoo>((t) => true).ImportProperty<IFirst>(null, null));
        }

        [Fact]
        public void ConstructorSelectorNull_ShouldThrowArgumentNull()
        {
            var builder = new ConventionBuilder();
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("constructorSelector", () => builder.ForTypesMatching<IFoo>((t) => true).SelectConstructor(null));
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("importConfiguration", () => builder.ForTypesMatching<IFoo>((t) => true).SelectConstructor(null, null));
        }

        [Fact]
        public void ExportSelectorNull_ShouldThrowArgumentNull()
        {
            var builder = new ConventionBuilder();
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("propertySelector", () => builder.ForTypesMatching<IFoo>((t) => true).ExportProperty(null));
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("propertySelector", () => builder.ForTypesMatching<IFoo>((t) => true).ExportProperty(null, null));
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("propertySelector", () => builder.ForTypesMatching<IFoo>((t) => true).ExportProperty<IFirst>(null));
            ExceptionAssert.ThrownMessageContains<ArgumentNullException>("propertySelector", () => builder.ForTypesMatching<IFoo>((t) => true).ExportProperty<IFirst>(null, null));
        }

        private static Attribute[] GetAttributesFromMember(ConventionBuilder builder, Type type, string member)
        {
            if (string.IsNullOrEmpty(member))
            {
                var list = builder.GetDeclaredAttributes(null, type.GetTypeInfo());
                return list;
            }
            else
            {
                var pi = type.GetRuntimeProperty(member);
                var list = builder.GetDeclaredAttributes(type, pi);
                return list;
            }
        }

        private static ConstructorInfo GetSelectedConstructor(ConventionBuilder builder, Type type)
        {
            ConstructorInfo reply = null;
            foreach (var ci in type.GetTypeInfo().DeclaredConstructors)
            {
                var li = builder.GetDeclaredAttributes(type, ci);
                if (li.Length > 0)
                {
                    Assert.True(reply == null);                   // Fail if we got more than one constructor
                    reply = ci;
                }
            }

            return reply;
        }

        [Fact]
        public void NotifyImportsSatisfied_ShouldSucceed()
        {
            var builder = new ConventionBuilder();
            builder.ForType<OnImportsSatisfiedTestClass>().NotifyImportsSatisfied(p => p.OnImportsSatisfied());
            var container = new ContainerConfiguration()
                .WithPart<OnImportsSatisfiedTestClass>(builder)
                .WithPart<ExportValues>(builder)
                .CreateContainer();
            var test = container.GetExport<OnImportsSatisfiedTestClass>();

            Assert.NotNull(test.P1);
            Assert.NotNull(test.P2);
            Assert.Equal(1, test.OnImportsSatisfiedInvoked);
        }

        [Fact]
        public void NotifyImportsSatisfiedAttributeAlreadyApplied_ShouldSucceed()
        {
            var builder = new ConventionBuilder();
            builder.ForType<OnImportsSatisfiedConfiguredClass>().NotifyImportsSatisfied(p => p.OnImportsSatisfied());
            var container = new ContainerConfiguration()
                .WithPart<OnImportsSatisfiedConfiguredClass>(builder)
                .WithPart<ExportValues>(builder)
                .CreateContainer();
            var test = container.GetExport<OnImportsSatisfiedConfiguredClass>();

            Assert.NotNull(test.P1);
            Assert.NotNull(test.P2);
            Assert.Equal(1, test.OnImportsSatisfiedInvoked);
        }

        [Fact]
        public void NotifyImportsSatisfiedAttributeAppliedToBaseClass_ShouldSucceed()
        {
            var builder = new ConventionBuilder();
            builder.ForType<OnImportsSatisfiedDerivedClass>().NotifyImportsSatisfied(p => p.OnImportsSatisfied());
            var container = new ContainerConfiguration()
                .WithPart<OnImportsSatisfiedDerivedClass>(builder)
                .WithPart<ExportValues>(builder)
                .CreateContainer();
            var test = container.GetExport<OnImportsSatisfiedDerivedClass>();

            Assert.NotNull(test.P1);
            Assert.NotNull(test.P2);
            Assert.Equal(1, test.OnImportsSatisfiedInvoked);
        }

        [Fact]
        public void NotifyImportsSatisfiedAttributeAppliedToDerivedClassExportBase_ShouldSucceed()
        {
            var builder = new ConventionBuilder();
            builder.ForType<OnImportsSatisfiedDerivedClass>().NotifyImportsSatisfied(p => p.OnImportsSatisfied());
            var container = new ContainerConfiguration()
                .WithPart<OnImportsSatisfiedTestClass>(builder)
                .WithPart<OnImportsSatisfiedDerivedClass>(builder)
                .WithPart<ExportValues>(builder)
                .CreateContainer();
            var test = container.GetExport<OnImportsSatisfiedTestClass>();

            Assert.NotNull(test.P1);
            Assert.NotNull(test.P2);
            Assert.Equal(0, test.OnImportsSatisfiedInvoked);
        }

        [Fact]
        public void NotifyImportsSatisfiedTwice_ShouldSucceed()
        {
            var builder = new ConventionBuilder();
            builder.ForType<OnImportsSatisfiedMultipleClass>().NotifyImportsSatisfied(p => p.OnImportsSatisfied1());
            builder.ForType<OnImportsSatisfiedMultipleClass>().NotifyImportsSatisfied(p => p.OnImportsSatisfied2());
            var container = new ContainerConfiguration()
                .WithPart<OnImportsSatisfiedMultipleClass>(builder)
                .WithPart<ExportValues>(builder)
                .CreateContainer();
            var test = container.GetExport<OnImportsSatisfiedMultipleClass>();

            Assert.NotNull(test.P1);
            Assert.NotNull(test.P2);
            Assert.Equal(6, test.OnImportsSatisfiedInvoked);
        }
    }
}
