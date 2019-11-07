// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Composition.Convention.Tests
{
    public class PartBuilderTests
    {
        private class MyDoNotIncludeAttribute : Attribute { }

        [MyDoNotIncludeAttribute]
        public class MyNotToBeIncludedClass { }

        public class MyToBeIncludedClass { }

        public class ImporterOfMyNotTobeIncludedClass
        {
            [Import(AllowDefault = true)]
            public MyNotToBeIncludedClass MyNotToBeIncludedClass { get; set; }

            [Import(AllowDefault = true)]
            public MyToBeIncludedClass MyToBeIncludedClass { get; set; }
        }

        public interface IFirst { }

        private interface IFoo { }

        private class FooImpl
        {
            public string P1 { get; set; }
            public string P2 { get; set; }
            public IEnumerable<IFoo> P3 { get; set; }
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

            [OnImportsSatisfied]
            public void OnImportsSatisfied()
            {
                ++OnImportsSatisfiedInvoked;
            }
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
        public class OnImportsSatisfiedDerivedClass : OnImportsSatisfiedTestClass
        {
        }


        [Export]
        public class OnImportsSatisfiedTestClassPropertiesAndFields
        {
            public int OnImportsSatisfiedInvoked = 0;

            [Import("P1", AllowDefault = true)]
            public string P1 { get; set; }
            [Import("P2", AllowDefault = true)]
            public string P2 { get; set; }

            public int OnImportsSatisfiedInvalidReturnValue() { return 1; }
            public void OnImportsSatisfiedInvalidArgs(int arg1) { }

            public int OnImportsSatisfied3;              // Field
            public int OnImportsSatisfied4 { get; set; } // Property
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

        private class FooImplWithConstructors
        {
            public FooImplWithConstructors() { }
            public FooImplWithConstructors(int id) { }
            public FooImplWithConstructors(IEnumerable<IFoo> ids) { }
            public FooImplWithConstructors(int id, string name) { }
        }
        private class FooImplWithConstructorsAmbiguous
        {
            public FooImplWithConstructorsAmbiguous(string name, int id) { }
            public FooImplWithConstructorsAmbiguous(int id, string name) { }
        }

        [Fact]
        public void NoOperations_ShouldGenerateNoAttributes()
        {
            var builder = new ConventionBuilder();
            builder.ForType(typeof(FooImpl));

            Attribute[] attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
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
            builder.ForType(typeof(FooImpl)).Export();

            Attribute[] attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
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
            builder.ForType(typeof(FooImpl)).Export<IFoo>();

            Attribute[] attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);

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
            builder.ForType(typeof(FooImpl)).Export<IFoo>().AddPartMetadata("name", "value");

            Attribute[] attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
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
            builder.ForType(typeof(FooImpl)).Export<IFoo>().AddPartMetadata("name", t => t.Name);

            Attribute[] attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
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
            builder.ForType(typeof(FooImpl)).ExportProperties(p => p.Name == "P1");

            Attribute[] attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
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
            builder.ForType(typeof(FooImpl)).ImportProperties(p => p.Name == "P1");

            Attribute[] attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
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
        public void ImportProperties_ShouldGenerateImportForPropertySelected_And_ApplyImportMany()
        {
            var builder = new ConventionBuilder();
            builder.ForType(typeof(FooImpl)).ImportProperties(p => p.Name == "P3");

            Attribute[] attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
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
            builder.ForType(typeof(FooImpl)).ExportProperties(p => p.Name == "P1", (pi, c) => c.AsContractName("hey").AsContractType<IFoo>());

            Attribute[] attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
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
        public void ExportPropertyOfT_ShouldGenerateExportForPropertySelectedWithTAsContractType()
        {
            var builder = new ConventionBuilder();
            builder.ForType(typeof(FooImpl)).ExportProperties(p => p.Name == "P1", (p, c) => c.AsContractType<IFoo>());

            Attribute[] attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
            Assert.Equal(0, attributes.Count());

            attributes = GetAttributesFromMember(builder, typeof(FooImpl), "P1");
            Assert.Equal(1, attributes.Count());

            var exportAttribute = attributes.First((t) => t.GetType() == typeof(ExportAttribute)) as ExportAttribute;
            Assert.Null(exportAttribute.ContractName);
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
            builder.ForType(typeof(FooImplWithConstructors));

            ConstructorInfo selectedConstructor = GetSelectedConstructor(builder, typeof(FooImplWithConstructors));
            Assert.NotNull(selectedConstructor);
            Assert.Equal(2, selectedConstructor.GetParameters().Length);         // Should select public FooImplWithConstructors(int id, string name) { }

            Attribute[] attributes = GetAttributesFromMember(builder, typeof(FooImpl), null);
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void ManuallySelectingConstructor_SelectsTheExplicitOne()
        {
            var builder = new ConventionBuilder();
            builder.ForType(typeof(FooImplWithConstructors)).SelectConstructor(cis => cis.ElementAt(1));

            ConstructorInfo selectedConstructor = GetSelectedConstructor(builder, typeof(FooImplWithConstructors));
            Assert.NotNull(selectedConstructor);
            Assert.Equal(1, selectedConstructor.GetParameters().Length);     // Should select public FooImplWithConstructors(int) { }

            ParameterInfo pi = selectedConstructor.GetParameters()[0];
            Assert.Equal(typeof(int), pi.ParameterType);

            Attribute[] attributes = builder.GetDeclaredAttributes(typeof(FooImplWithConstructors), pi);
            Assert.Equal(1, attributes.Count());
            Assert.NotNull(attributes[0] as ImportAttribute);

            attributes = GetAttributesFromMember(builder, typeof(FooImplWithConstructors), null);
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void ManuallySelectingConstructor_SelectsTheExplicitOne_IEnumerableParameterBecomesImportMany()
        {
            var builder = new ConventionBuilder();
            builder.ForType(typeof(FooImplWithConstructors)).SelectConstructor(cis => cis.ElementAt(2));

            ConstructorInfo selectedConstructor = GetSelectedConstructor(builder, typeof(FooImplWithConstructors));
            Assert.NotNull(selectedConstructor);
            Assert.Equal(1, selectedConstructor.GetParameters().Length);     // Should select public FooImplWithConstructors(IEnumerable<IFoo>) { }

            ParameterInfo pi = selectedConstructor.GetParameters()[0];
            Assert.Equal(typeof(IEnumerable<IFoo>), pi.ParameterType);

            Attribute[] attributes = builder.GetDeclaredAttributes(typeof(FooImplWithConstructors), pi);
            Assert.Equal(1, attributes.Count());
            Assert.NotNull(attributes[0] as ImportManyAttribute);

            attributes = GetAttributesFromMember(builder, typeof(FooImplWithConstructors), null);
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void ExportInterfaceSelectorNull_ShouldThrowArgumentNull()
        {
            var builder = new ConventionBuilder();
            AssertExtensions.Throws<ArgumentNullException>("interfaceFilter", () => builder.ForTypesMatching((t) => true).ExportInterfaces(null));
            AssertExtensions.Throws<ArgumentNullException>("interfaceFilter", () => builder.ForTypesMatching((t) => true).ExportInterfaces(null, null));
        }

        [Fact]
        public void ImportSelectorNull_ShouldThrowArgumentNull()
        {
            var builder = new ConventionBuilder();
            AssertExtensions.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ImportProperties(null));
            AssertExtensions.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ImportProperties(null, null));
            AssertExtensions.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ImportProperties<IFirst>(null));
            AssertExtensions.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ImportProperties<IFirst>(null, null));
        }

        [Fact]
        public void ExportSelectorNull_ShouldThrowArgumentNull()
        {
            var builder = new ConventionBuilder();
            AssertExtensions.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ExportProperties(null));
            AssertExtensions.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ExportProperties(null, null));
            AssertExtensions.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ExportProperties<IFirst>(null));
            AssertExtensions.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ExportProperties<IFirst>(null, null));
        }

        [Fact]
        public void InsideTheLambdaCallGetCustomAttributesShouldSucceed()
        {
            var builder = new ConventionBuilder();
            builder.ForTypesMatching((t) => !t.GetTypeInfo().IsDefined(typeof(MyDoNotIncludeAttribute), false)).Export();
            CompositionHost container = new ContainerConfiguration()
                .WithPart<MyNotToBeIncludedClass>(builder)
                .WithPart<MyToBeIncludedClass>(builder)
                .CreateContainer();

            var importer = new ImporterOfMyNotTobeIncludedClass();
            container.SatisfyImports(importer);

            Assert.Null(importer.MyNotToBeIncludedClass);
            Assert.NotNull(importer.MyToBeIncludedClass);
        }


        [Fact]
        public void NotifyImportsSatisfiedAttributeAlreadyApplied_ShouldSucceed()
        {
            var builder = new ConventionBuilder();
            builder.ForTypesMatching(t => true).NotifyImportsSatisfied(mi => mi.Name == "OnImportsSatisfied");
            CompositionHost container = new ContainerConfiguration()
                .WithPart<OnImportsSatisfiedConfiguredClass>(builder)
                .WithPart<ExportValues>(builder)
                .CreateContainer();
            OnImportsSatisfiedConfiguredClass test = container.GetExport<OnImportsSatisfiedConfiguredClass>();

            Assert.NotNull(test.P1);
            Assert.NotNull(test.P2);
            Assert.Equal(1, test.OnImportsSatisfiedInvoked);
        }


        [Fact]
        public void NotifyImportsSatisfiedAttributeAppliedToBaseClass_ShouldSucceed()
        {
            var builder = new ConventionBuilder();
            builder.ForTypesMatching(t => true).NotifyImportsSatisfied(mi => mi.Name == "OnImportsSatisfied");
            CompositionHost container = new ContainerConfiguration()
                .WithPart<OnImportsSatisfiedDerivedClass>(builder)
                .WithPart<ExportValues>(builder)
                .CreateContainer();
            OnImportsSatisfiedDerivedClass test = container.GetExport<OnImportsSatisfiedDerivedClass>();

            Assert.NotNull(test.P1);
            Assert.NotNull(test.P2);
            Assert.Equal(1, test.OnImportsSatisfiedInvoked);
        }


        [Fact]
        public void NotifyImportsSatisfiedMultipleNotifications_ShouldSucceed()
        {
            var builder = new ConventionBuilder();
            builder.ForTypesMatching(t => true).NotifyImportsSatisfied(mi => mi.Name == "OnImportsSatisfied1");
            CompositionHost container = new ContainerConfiguration()
                .WithPart<OnImportsSatisfiedMultipleClass>(builder)
                .WithPart<ExportValues>(builder)
                .CreateContainer();
            OnImportsSatisfiedMultipleClass test = container.GetExport<OnImportsSatisfiedMultipleClass>();

            Assert.NotNull(test.P1);
            Assert.NotNull(test.P2);
            Assert.Equal(2, test.OnImportsSatisfiedInvoked);
        }

        [Fact]
        public void NotifyImportsSatisfiedTwice_ShouldSucceed()
        {
            var builder = new ConventionBuilder();
            builder.ForTypesMatching(t => true).NotifyImportsSatisfied(mi => mi.Name == "OnImportsSatisfied1" || mi.Name == "OnImportsSatisfied2");
            CompositionHost container = new ContainerConfiguration()
                .WithPart<OnImportsSatisfiedMultipleClass>(builder)
                .WithPart<ExportValues>(builder)
                .CreateContainer();
            OnImportsSatisfiedMultipleClass test = container.GetExport<OnImportsSatisfiedMultipleClass>();

            Assert.NotNull(test.P1);
            Assert.NotNull(test.P2);
            Assert.Equal(6, test.OnImportsSatisfiedInvoked);
        }


        [Fact]
        public void NotifyImportsSatisfiedInvalidMethod_ShouldSucceed()
        {
            var builder = new ConventionBuilder();
            builder.ForTypesMatching(t => true).NotifyImportsSatisfied(mi => mi.Name == "OnImportsSatisfied3" || mi.Name == "OnImportsSatisfied4");
            CompositionHost container = new ContainerConfiguration()
                .WithPart<OnImportsSatisfiedTestClassPropertiesAndFields>(builder)
                .WithPart<ExportValues>(builder)
                .CreateContainer();
            OnImportsSatisfiedTestClassPropertiesAndFields test = container.GetExport<OnImportsSatisfiedTestClassPropertiesAndFields>();

            Assert.NotNull(test.P1);
            Assert.NotNull(test.P2);
            Assert.Equal(0, test.OnImportsSatisfiedInvoked);
        }

        [Fact]
        public void NotifyImportsSatisfiedPropertiesAndFields_ShouldSucceed()
        {
            var builder = new ConventionBuilder();
            builder.ForTypesMatching(t => true).NotifyImportsSatisfied(mi => mi.Name == "OnImportsSatisfied5" || mi.Name == "OnImportsSatisfied6");
            CompositionHost container = new ContainerConfiguration()
                .WithPart<OnImportsSatisfiedTestClassPropertiesAndFields>(builder)
                .WithPart<ExportValues>(builder)
                .CreateContainer();
            OnImportsSatisfiedTestClassPropertiesAndFields test = container.GetExport<OnImportsSatisfiedTestClassPropertiesAndFields>();

            Assert.NotNull(test.P1);
            Assert.NotNull(test.P2);
            Assert.Equal(0, test.OnImportsSatisfiedInvoked);
        }

        private static Attribute[] GetAttributesFromMember(ConventionBuilder builder, Type type, string member)
        {
            if (string.IsNullOrEmpty(member))
            {
                Attribute[] list = builder.GetDeclaredAttributes(null, type.GetTypeInfo());
                return list;
            }
            else
            {
                PropertyInfo pi = type.GetRuntimeProperty(member);
                Attribute[] list = builder.GetDeclaredAttributes(type, pi);
                return list;
            }
        }

        private static ConstructorInfo GetSelectedConstructor(ConventionBuilder builder, Type type)
        {
            ConstructorInfo reply = null;
            foreach (ConstructorInfo ci in type.GetTypeInfo().DeclaredConstructors)
            {
                Attribute[] li = builder.GetDeclaredAttributes(type, ci);
                if (li.Length > 0)
                {
                    Assert.True(reply == null);                   // Fail if we got more than one constructor
                    reply = ci;
                }
            }

            return reply;
        }
    }
}
