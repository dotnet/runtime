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
    public class PartBuilderTests
    {
        private class MyDoNotIncludeAttribute : Attribute { }

        [MyDoNotIncludeAttribute]
        public class MyNotToBeIncludedClass { }

        public class MyToBeIncludedClass { }

        public class ImporterOfMyNotTobeIncludedClass
        {
            [Import(AllowDefault = true)]
            public MyNotToBeIncludedClass MyNotToBeIncludedClass;

            [Import(AllowDefault = true)]
            public MyToBeIncludedClass MyToBeIncludedClass;
        }

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
            public FooImplWithConstructors(IEnumerable<IFoo> ids) { }
            public FooImplWithConstructors(int id, string name) { }
        }

        private class FooImplWithConstructorsAmbiguous
        {
            public FooImplWithConstructorsAmbiguous(string name, int id) { }
            public FooImplWithConstructorsAmbiguous(int id, string name) { }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Reflection based tests")]
        public void NoOperations_ShouldGenerateNoAttributes()
        {
            var builder = InternalCalls.PartBuilder(t => true);

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts);

            Assert.Equal(0, typeAtts.Count());
            Assert.Equal(0, configuredMembers.Count);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Reflection based tests")]
        public void ExportSelf_ShouldGenerateSingleExportAttribute()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.Export();

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts);

            Assert.Equal(1, typeAtts.Count());
            Assert.Equal(0, configuredMembers.Count);
            Assert.Same(typeof(ExportAttribute), typeAtts.ElementAt(0).GetType());
            Assert.Null((typeAtts.ElementAt(0) as ExportAttribute).ContractType);
            Assert.Null((typeAtts.ElementAt(0) as ExportAttribute).ContractName);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Reflection based tests")]
        public void ExportOfT_ShouldGenerateSingleExportAttributeWithContractType()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.Export<IFoo>();

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts);

            Assert.Equal(1, typeAtts.Count());
            Assert.Equal(0, configuredMembers.Count);
            Assert.Same(typeof(ExportAttribute), typeAtts.ElementAt(0).GetType());
            Assert.Equal(typeof(IFoo), (typeAtts.ElementAt(0) as ExportAttribute).ContractType);
            Assert.Null((typeAtts.ElementAt(0) as ExportAttribute).ContractName);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Reflection based tests")]
        public void AddMetadata_ShouldGeneratePartMetadataAttribute()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.Export<IFoo>().AddMetadata("name", "value");

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts);

            Assert.Equal(2, typeAtts.Count());
            Assert.Equal(0, configuredMembers.Count);
            Assert.Same(typeof(ExportAttribute), typeAtts.ElementAt(0).GetType());
            Assert.True(typeAtts.ElementAt(0) is ExportAttribute);
            Assert.True(typeAtts.ElementAt(1) is PartMetadataAttribute);

            var metadataAtt = typeAtts.ElementAt(1) as PartMetadataAttribute;
            Assert.Equal("name", metadataAtt.Name);
            Assert.Equal("value", metadataAtt.Value);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Reflection based tests")]
        public void AddMetadataWithFunc_ShouldGeneratePartMetadataAttribute()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.Export<IFoo>().AddMetadata("name", t => t.Name);

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts);

            Assert.Equal(2, typeAtts.Count());
            Assert.Equal(0, configuredMembers.Count);
            Assert.Same(typeof(ExportAttribute), typeAtts.ElementAt(0).GetType());
            Assert.True(typeAtts.ElementAt(0) is ExportAttribute);
            Assert.True(typeAtts.ElementAt(1) is PartMetadataAttribute);

            var metadataAtt = typeAtts.ElementAt(1) as PartMetadataAttribute;
            Assert.Equal("name", metadataAtt.Name);
            Assert.Equal(typeof(FooImpl).Name, metadataAtt.Value);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Reflection based tests")]
        public void ExportProperty_ShouldGenerateExportForPropertySelected()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.Export<IFoo>().
                ExportProperties(p => p.Name == "P1");

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts);

            Assert.Equal(1, typeAtts.Count());
            Assert.Equal(1, configuredMembers.Count);

            Tuple<object, List<Attribute>> tuple = configuredMembers[0];
            Assert.Equal(typeof(FooImpl).GetProperty("P1"), tuple.Item1);

            List<Attribute> atts = tuple.Item2;
            Assert.Equal(1, atts.Count);

            var expAtt = atts[0] as ExportAttribute;
            Assert.Null(expAtt.ContractName);
            Assert.Null(expAtt.ContractType);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Reflection based tests")]
        public void ImportProperty_ShouldGenerateImportForPropertySelected()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.Export<IFoo>().
                ImportProperties(p => p.Name == "P2"); // P3 is string

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts);

            Assert.Equal(1, typeAtts.Count());
            Assert.Equal(1, configuredMembers.Count);

            Tuple<object, List<Attribute>> tuple = configuredMembers[0];
            Assert.Equal(typeof(FooImpl).GetProperty("P2"), tuple.Item1);

            List<Attribute> atts = tuple.Item2;
            Assert.Equal(1, atts.Count);

            var importAttribute = atts[0] as ImportAttribute;
            Assert.NotNull(importAttribute);
            Assert.Null(importAttribute.ContractName);
            Assert.Null(importAttribute.ContractType);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Reflection based tests")]
        public void ImportProperties_ShouldGenerateImportForPropertySelected_And_ApplyImportMany()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.Export<IFoo>().
                ImportProperties(p => p.Name == "P3"); // P3 is IEnumerable<IFoo>

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts);

            Assert.Equal(1, typeAtts.Count());
            Assert.Equal(1, configuredMembers.Count);

            Tuple<object, List<Attribute>> tuple = configuredMembers[0];
            Assert.Equal(typeof(FooImpl).GetProperty("P3"), tuple.Item1);

            List<Attribute> atts = tuple.Item2;
            Assert.Equal(1, atts.Count);

            var importManyAttribute = atts[0] as ImportManyAttribute;
            Assert.NotNull(importManyAttribute);
            Assert.Null(importManyAttribute.ContractName);
            Assert.Null(importManyAttribute.ContractType);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Reflection based tests")]
        public void ExportPropertyWithConfiguration_ShouldGenerateExportForPropertySelected()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.Export<IFoo>().
                ExportProperties(p => p.Name == "P1", (p, c) => c.AsContractName("hey"));

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts);

            Assert.Equal(1, typeAtts.Count());
            Assert.Equal(1, configuredMembers.Count);

            Tuple<object, List<Attribute>> tuple = configuredMembers[0];
            Assert.Equal(typeof(FooImpl).GetProperty("P1"), tuple.Item1);

            List<Attribute> atts = tuple.Item2;
            Assert.Equal(1, atts.Count);

            var expAtt = atts[0] as ExportAttribute;
            Assert.Equal("hey", expAtt.ContractName);
            Assert.Null(expAtt.ContractType);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Reflection based tests")]
        public void ExportPropertyOfT_ShouldGenerateExportForPropertySelectedWithTAsContractType()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.Export<IFoo>().
                ExportProperties<string>(p => p.Name == "P1");

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts);

            Assert.Equal(1, typeAtts.Count());
            Assert.Equal(1, configuredMembers.Count);

            Tuple<object, List<Attribute>> tuple = configuredMembers[0];
            Assert.Equal(typeof(FooImpl).GetProperty("P1"), tuple.Item1);

            List<Attribute> atts = tuple.Item2;
            Assert.Equal(1, atts.Count);

            var expAtt = atts[0] as ExportAttribute;
            Assert.Null(expAtt.ContractName);
            Assert.Equal(typeof(string), expAtt.ContractType);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Reflection based tests")]
        public void SetCreationPolicy_ShouldGeneratePartCreationPolicyAttributeForType()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.Export<IFoo>().SetCreationPolicy(CreationPolicy.NonShared);

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts);

            Assert.Equal(2, typeAtts.Count());
            Assert.Equal(0, configuredMembers.Count);

            var partCPAtt = (PartCreationPolicyAttribute)typeAtts.ElementAt(1);
            Assert.Equal(CreationPolicy.NonShared, partCPAtt.CreationPolicy);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Reflection based tests")]
        public void ConventionSelectsConstructor_SelectsTheOneWithMostParameters()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.Export<IFoo>();

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts, typeof(FooImplWithConstructors));

            Assert.Equal(1, typeAtts.Count());
            Assert.Equal(3, configuredMembers.Count);

            Tuple<object, List<Attribute>> tuple = configuredMembers[0]; // Constructor
            ConstructorInfo ci = typeof(FooImplWithConstructors).GetConstructors()[2];
            Assert.True(tuple.Item1 is ConstructorInfo);
            Assert.Same(ci, tuple.Item1);
            Assert.Equal(1, tuple.Item2.Count);
            Assert.True(tuple.Item2[0] is ImportingConstructorAttribute);

            tuple = configuredMembers[1]; // Parameter 1
            Assert.True(tuple.Item1 is ParameterInfo);
            Assert.Same(ci.GetParameters()[0], tuple.Item1);
            Assert.Equal(1, tuple.Item2.Count);
            Assert.True(tuple.Item2[0] is ImportAttribute);

            tuple = configuredMembers[2]; // Parameter 2
            Assert.True(tuple.Item1 is ParameterInfo);
            Assert.Same(ci.GetParameters()[1], tuple.Item1);
            Assert.Equal(1, tuple.Item2.Count);
            Assert.True(tuple.Item2[0] is ImportAttribute);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Reflection based tests")]
        public void ManuallySelectingConstructor_SelectsTheExplicitOne()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.Export<IFoo>().SelectConstructor((cis) => cis[1]);

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts, typeof(FooImplWithConstructors));

            Assert.Equal(1, typeAtts.Count());
            Assert.Equal(2, configuredMembers.Count);

            Tuple<object, List<Attribute>> tuple = configuredMembers[0]; // Constructor
            ConstructorInfo ci = typeof(FooImplWithConstructors).GetConstructors()[1];
            Assert.True(tuple.Item1 is ConstructorInfo);
            Assert.Same(ci, tuple.Item1);
            Assert.Equal(1, tuple.Item2.Count);
            Assert.True(tuple.Item2[0] is ImportingConstructorAttribute);

            tuple = configuredMembers[1]; // Parameter 1
            Assert.True(tuple.Item1 is ParameterInfo);
            Assert.Same(ci.GetParameters()[0], tuple.Item1);
            Assert.Equal(1, tuple.Item2.Count);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Reflection based tests")]
        public void ManuallySelectingConstructor_SelectsTheExplicitOne_IEnumerableParameterBecomesImportMany()
        {
            var builder = InternalCalls.PartBuilder(t => true);
            builder.Export<IFoo>().SelectConstructor((cis) => cis[1]);

            IEnumerable<Attribute> typeAtts;
            List<Tuple<object, List<Attribute>>> configuredMembers;
            GetConfiguredMembers(builder, out configuredMembers, out typeAtts, typeof(FooImplWithConstructors));

            Assert.Equal(1, typeAtts.Count());
            Assert.Equal(2, configuredMembers.Count);

            ConstructorInfo ci = typeof(FooImplWithConstructors).GetConstructors()[1];

            Tuple<object, List<Attribute>> tuple = configuredMembers[1]; // Parameter 1
            Assert.True(tuple.Item1 is ParameterInfo);
            Assert.Same(ci.GetParameters()[0], tuple.Item1);
            Assert.Equal(1, tuple.Item2.Count);
            Assert.Equal(typeof(ImportManyAttribute), tuple.Item2[0].GetType());
        }

        private static void GetConfiguredMembers(PartBuilder builder,
            out List<Tuple<object, List<Attribute>>> configuredMembers, out IEnumerable<Attribute> typeAtts,
            Type targetType = null)
        {
            if (targetType == null)
            {
                targetType = typeof(FooImpl);
            }

            configuredMembers = new List<Tuple<object, List<Attribute>>>();
            typeAtts = builder.BuildTypeAttributes(targetType);
            if (!builder.BuildConstructorAttributes(targetType, ref configuredMembers))
            {
                InternalCalls.PartBuilder_BuildDefaultConstructorAttributes(targetType, ref configuredMembers);
            }
            builder.BuildPropertyAttributes(targetType, ref configuredMembers);
        }

        [Fact]
        public void ExportInterfaceSelectorNull_ShouldThrowArgumentNull()
        {
            //Same test as above only using default export builder
            var builder = new RegistrationBuilder();
            Assert.Throws<ArgumentNullException>("interfaceFilter", () => builder.ForTypesMatching((t) => true).ExportInterfaces(null));
            Assert.Throws<ArgumentNullException>("interfaceFilter", () => builder.ForTypesMatching((t) => true).ExportInterfaces(null, null));
        }

        [Fact]
        public void ImportSelectorNull_ShouldThrowArgumentNull()
        {
            //Same test as above only using default export builder
            var builder = new RegistrationBuilder();
            Assert.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ImportProperties(null));
            Assert.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ImportProperties(null, null));
            Assert.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ImportProperties<IFirst>(null));
            Assert.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ImportProperties<IFirst>(null, null));
        }

        [Fact]
        public void ExportSelectorNull_ShouldThrowArgumentNull()
        {
            //Same test as above only using default export builder
            var builder = new RegistrationBuilder();
            Assert.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ExportProperties(null));
            Assert.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ExportProperties(null, null));
            Assert.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ExportProperties<IFirst>(null));
            Assert.Throws<ArgumentNullException>("propertyFilter", () => builder.ForTypesMatching((t) => true).ExportProperties<IFirst>(null, null));
        }

        [Fact]
        [ActiveIssue(35144, TargetFrameworkMonikers.UapAot)]
        public void InsideTheLambdaCallGetCustomAttributesShouldSucceed()
        {
            //Same test as above only using default export builder
            var builder = new RegistrationBuilder();
            builder.ForTypesMatching((t) => !t.IsDefined(typeof(MyDoNotIncludeAttribute), false)).Export();

            var types = new Type[] { typeof(MyNotToBeIncludedClass), typeof(MyToBeIncludedClass) };
            var catalog = new TypeCatalog(types, builder);

            CompositionService cs = catalog.CreateCompositionService();
            var importer = new ImporterOfMyNotTobeIncludedClass();
            cs.SatisfyImportsOnce(importer);

            Assert.Null(importer.MyNotToBeIncludedClass);
            Assert.NotNull(importer.MyToBeIncludedClass);
        }
    }
}
