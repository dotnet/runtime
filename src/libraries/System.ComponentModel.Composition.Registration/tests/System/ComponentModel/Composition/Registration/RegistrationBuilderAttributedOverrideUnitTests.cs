// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Composition.Registration.Tests
{
    public class RegistrationBuilderAttributedOverrideUnitTests
    {
        public interface IContractA { }
        public interface IContractB { }

        public class AB : IContractA, IContractB { }

        private static class ContractNames
        {
            public const string ContractX = "X";
            public const string ContractY = "Y";
        }

        private static class MetadataKeys
        {
            public const string MetadataKeyP = "P";
            public const string MetadataKeyQ = "Q";
        }

        private static class MetadataValues
        {
            public const string MetadataValueN = "N";
            public const string MetadataValueO = "O";
        }

        // Flattened so that we can be sure nothing funky is going on in the base type

        private static void AssertHasDeclaredAttributesUnderConvention<TSource>(RegistrationBuilder convention)
        {
            AssertHasAttributesUnderConvention<TSource>(convention, typeof(TSource).GetCustomAttributes(true));
        }

        private static void AssertHasAttributesUnderConvention<TSource>(RegistrationBuilder convention, IEnumerable<object> expected)
        {
            TypeInfo mapped = convention.MapType(typeof(TSource).GetTypeInfo());
            var applied = mapped.GetCustomAttributes(true);

            // Was: CollectionAssert.AreEquivalent(expected, applied) - output is not much good.
            AssertEquivalentAttributes(expected, applied);
        }

        private static PropertyInfo GetPropertyFromAccessor<T>(Expression<Func<T, object>> property)
        {
            return (PropertyInfo)((MemberExpression)property.Body).Member;
        }

        private static void AssertHasDeclaredAttributesUnderConvention<TSource>(Expression<Func<TSource, object>> property, RegistrationBuilder convention)
        {
            PropertyInfo pi = GetPropertyFromAccessor(property);
            AssertHasAttributesUnderConvention<TSource>(property, convention, pi.GetCustomAttributes(true));
        }

        private static void AssertHasAttributesUnderConvention<TSource>(Expression<Func<TSource, object>> property, RegistrationBuilder convention, IEnumerable<object> expected)
        {
            TypeInfo mapped = convention.MapType(typeof(TSource).GetTypeInfo());
            PropertyInfo pi = GetPropertyFromAccessor(property);
            var applied = mapped.GetProperty(pi.Name).GetCustomAttributes(true);

            // Was: CollectionAssert.AreEquivalent(expected, applied) - output is not much good.
            AssertEquivalentAttributes(expected, applied);
        }

        private static void AssertEquivalentAttributes(IEnumerable<object> expected, IEnumerable<object> applied)
        {
            var expectedRemaining = expected.ToList();
            var unexpected = new List<object>();
            foreach (var appl in applied)
            {
                var matching = expectedRemaining.FirstOrDefault(e => object.Equals(e, appl));
                if (matching == null)
                {
                    unexpected.Add(appl);
                }
                else
                {
                    expectedRemaining.Remove(matching);
                }
            }

            var failures = new List<string>();
            if (expectedRemaining.Any())
            {
                failures.Add("Expected attributes: " + string.Join(", ", expectedRemaining));
            }

            if (unexpected.Any())
            {
                failures.Add("Did not expect attributes: " + string.Join(", ", unexpected));
            }

            Assert.Empty(failures);
        }

        // This set of tests is for exports at the class declaration level

        private static RegistrationBuilder ConfigureExportInterfaceConvention<TPart>()
        {
            var convention = new RegistrationBuilder();

            convention.ForType<TPart>()
                 .Export(eb => eb.AsContractType<IContractA>()
                     .AddMetadata(MetadataKeys.MetadataKeyP, MetadataValues.MetadataValueN));

            return convention;
        }

        private static object[] ExportInterfaceConventionAttributes = new object[] {
            new ExportAttribute(typeof(IContractA)),
            new ExportMetadataAttribute(MetadataKeys.MetadataKeyP, MetadataValues.MetadataValueN)
        };

        public class NoClassDeclarationOverrides : IContractA, IContractB { }

        [Fact]
        public void ExportInterfaceConvention_NoOverrides_ConventionApplied()
        {
            RegistrationBuilder convention = ConfigureExportInterfaceConvention<NoClassDeclarationOverrides>();

            AssertHasAttributesUnderConvention<NoClassDeclarationOverrides>(convention, ExportInterfaceConventionAttributes);
        }

        [Export(typeof(IContractB))]
        public class ExportContractBClassDeclarationOverride : IContractA, IContractB { }

        [Fact]
        public void ExportInterfaceConvention_ExportAttribute_ConventionIgnored()
        {
            RegistrationBuilder convention = ConfigureExportInterfaceConvention<ExportContractBClassDeclarationOverride>();

            AssertHasDeclaredAttributesUnderConvention<ExportContractBClassDeclarationOverride>(convention);
        }

        [ExportMetadata(MetadataKeys.MetadataKeyQ, MetadataValues.MetadataValueO)]
        public class ExportJustMetadataOverride : IContractA, IContractB { }

        [Fact]
        public void ExportInterfaceConvention_JustMetadataOverride_ConventionIgnored()
        {
            RegistrationBuilder convention = ConfigureExportInterfaceConvention<ExportJustMetadataOverride>();

            AssertHasDeclaredAttributesUnderConvention<ExportJustMetadataOverride>(convention);
        }

        [InheritedExport(typeof(IContractA))]
        public class InheritedExportContractAClassDeclarationOverride : IContractA, IContractB { }

        [Fact]
        public void ExportInterfaceConvention_InheritedExportAttribute_ConventionIgnored()
        {
            RegistrationBuilder convention = ConfigureExportInterfaceConvention<InheritedExportContractAClassDeclarationOverride>();

            AssertHasDeclaredAttributesUnderConvention<InheritedExportContractAClassDeclarationOverride>(convention);
        }

        [InheritedExport(typeof(IContractA))]
        public class BaseWithInheritedExport { }

        public class InheritedExportOnBaseClassDeclaration : BaseWithInheritedExport, IContractA, IContractB { }

        [Fact]
        public void ExportInterfaceConvention_InheritedExportOnBaseClassDeclaration_ConventionApplied()
        {
            RegistrationBuilder convention = ConfigureExportInterfaceConvention<InheritedExportOnBaseClassDeclaration>();

            AssertHasAttributesUnderConvention<InheritedExportOnBaseClassDeclaration>(convention,
                ExportInterfaceConventionAttributes.Concat(new object[] { new InheritedExportAttribute(typeof(IContractA)) }));
        }

        public class CustomExportAttribute : ExportAttribute { }

        [CustomExport]
        public class CustomExportClassDeclarationOverride : IContractA, IContractB { }

        [Fact]
        public void ExportInterfaceConvention_CustomExportAttribute_ConventionIgnored()
        {
            RegistrationBuilder convention = ConfigureExportInterfaceConvention<CustomExportClassDeclarationOverride>();

            AssertHasDeclaredAttributesUnderConvention<CustomExportClassDeclarationOverride>(convention);
        }

        [MetadataAttribute]
        public class CustomMetadataAttribute : Attribute { public string Z { get { return "Z"; } } }

        [CustomMetadata]
        public class CustomMetadataClassDeclarationOverride : IContractA, IContractB { }

        [Fact]
        public void ExportInterfaceConvention_CustomMetadataAttribute_ConventionIgnored()
        {
            RegistrationBuilder convention = ConfigureExportInterfaceConvention<CustomMetadataClassDeclarationOverride>();

            AssertHasDeclaredAttributesUnderConvention<CustomMetadataClassDeclarationOverride>(convention);
        }

        [PartCreationPolicy(CreationPolicy.NonShared),
         PartMetadata(MetadataKeys.MetadataKeyQ, MetadataValues.MetadataValueO),
         PartNotDiscoverable]
        public class NonExportClassDeclarationAttributes : IContractA, IContractB { }

        [Fact]
        public void ExportInterfaceConvention_NonExportClassDeclarationAttributes_ConventionApplied()
        {
            RegistrationBuilder convention = ConfigureExportInterfaceConvention<NonExportClassDeclarationAttributes>();

            var unionOfConventionAndDeclared = typeof(NonExportClassDeclarationAttributes)
                .GetCustomAttributes(true)
                .Concat(ExportInterfaceConventionAttributes)
                .ToArray();

            AssertHasAttributesUnderConvention<NonExportClassDeclarationAttributes>(convention, unionOfConventionAndDeclared);
        }

        public class ExportAtProperty
        {
            [Export]
            public IContractA A { get; set; }
        }

        [Fact]
        public void ExportInterfacesConvention_UnrelatedExportOnProperty_ConventionApplied()
        {
            RegistrationBuilder convention = ConfigureExportInterfaceConvention<ExportAtProperty>();
            AssertHasAttributesUnderConvention<ExportAtProperty>(convention, ExportInterfaceConventionAttributes);
        }

        // This set of tests is for exports at the property level

        private static RegistrationBuilder ConfigureExportPropertyConvention<TPart>(Expression<Func<TPart, object>> property)
        {
            var convention = new RegistrationBuilder();

            convention.ForType<TPart>()
                 .ExportProperty(property, eb => eb.AsContractType<IContractA>()
                     .AddMetadata(MetadataKeys.MetadataKeyP, MetadataValues.MetadataValueN));

            return convention;
        }

        private static object[] ExportPropertyConventionAttributes = new object[] {
            new ExportAttribute(typeof(IContractA)),
            new ExportMetadataAttribute(MetadataKeys.MetadataKeyP, MetadataValues.MetadataValueN)
        };

        public class NoPropertyDeclarationOverrides
        {
            public AB AB { get; set; }
        }

        [Fact]
        public void ExportPropertyConvention_NoOverrides_ConventionApplied()
        {
            RegistrationBuilder convention = ConfigureExportPropertyConvention<NoPropertyDeclarationOverrides>(t => t.AB);

            AssertHasAttributesUnderConvention<NoPropertyDeclarationOverrides>(t => t.AB, convention, ExportPropertyConventionAttributes);
        }

        public class ExportContractBPropertyDeclarationOverride
        {
            [Export(typeof(IContractB))]
            public AB AB { get; set; }
        }

        [Fact]
        public void ExportPropertyConvention_ExportAttribute_ConventionIgnored()
        {
            RegistrationBuilder convention = ConfigureExportPropertyConvention<ExportContractBPropertyDeclarationOverride>(t => t.AB);

            AssertHasDeclaredAttributesUnderConvention<ExportContractBPropertyDeclarationOverride>(t => t.AB, convention);
        }

        public class ExportJustMetadataPropertyDeclarationOverride
        {
            [ExportMetadata(MetadataKeys.MetadataKeyQ, MetadataValues.MetadataValueO)]
            public AB AB { get; set; }
        }

        [Fact]
        public void ExportPropertyConvention_JustMetadataOverride_ConventionIgnored()
        {
            RegistrationBuilder convention = ConfigureExportPropertyConvention<ExportJustMetadataPropertyDeclarationOverride>(t => t.AB);

            AssertHasDeclaredAttributesUnderConvention<ExportJustMetadataPropertyDeclarationOverride>(t => t.AB, convention);
        }

        public class CustomExportPropertyDeclarationOverride
        {
            [CustomExport]
            public AB AB { get; set; }
        }

        [Fact]
        public void ExportPropertyConvention_CustomExportAttribute_ConventionIgnored()
        {
            RegistrationBuilder convention = ConfigureExportPropertyConvention<CustomExportPropertyDeclarationOverride>(t => t.AB);

            AssertHasDeclaredAttributesUnderConvention<CustomExportPropertyDeclarationOverride>(t => t.AB, convention);
        }

        public class CustomMetadataPropertyDeclarationOverride
        {
            [CustomMetadata]
            public AB AB { get; set; }
        }

        [Fact]
        public void ExportPropertyConvention_CustomMetadataAttribute_ConventionIgnored()
        {
            RegistrationBuilder convention = ConfigureExportPropertyConvention<CustomMetadataPropertyDeclarationOverride>(t => t.AB);

            AssertHasDeclaredAttributesUnderConvention<CustomMetadataPropertyDeclarationOverride>(t => t.AB, convention);
        }

        public class NonExportPropertyDeclarationAttributes
        {
            [Import, ImportMany]
            public AB AB { get; set; }
        }

        [Fact]
        public void ExportPropertyConvention_NonExportPropertyDeclarationAttributes_ConventionApplied()
        {
            RegistrationBuilder convention = ConfigureExportPropertyConvention<NonExportPropertyDeclarationAttributes>(t => t.AB);

            var unionOfConventionAndDeclared = typeof(NonExportPropertyDeclarationAttributes)
                .GetProperty("AB")
                .GetCustomAttributes(true)
                .Concat(ExportPropertyConventionAttributes)
                .ToArray();

            AssertHasAttributesUnderConvention<NonExportPropertyDeclarationAttributes>(t => t.AB, convention, unionOfConventionAndDeclared);
        }

        // This set of tests is for imports at the property level
        private static RegistrationBuilder ConfigureImportPropertyConvention<TPart>(Expression<Func<TPart, object>> property)
        {
            var convention = new RegistrationBuilder();

            convention.ForType<TPart>()
                 .ImportProperty(property, ib => ib.AsMany(false).AsContractName(ContractNames.ContractX).AsContractType<AB>());

            return convention;
        }

        private static object[] ImportPropertyConventionAttributes = new object[] { new ImportAttribute(ContractNames.ContractX, typeof(AB)) };

        [Fact]
        public void ImportPropertyConvention_NoOverrides_ConventionApplied()
        {
            RegistrationBuilder convention = ConfigureImportPropertyConvention<NoPropertyDeclarationOverrides>(t => t.AB);

            AssertHasAttributesUnderConvention<NoPropertyDeclarationOverrides>(t => t.AB, convention, ImportPropertyConventionAttributes);
        }

        public class ImportContractYPropertyDeclarationOverride
        {
            [Import(ContractNames.ContractY)]
            public AB AB { get; set; }
        }

        [Fact]
        public void ImportPropertyConvention_ImportAttribute_ConventionIgnored()
        {
            RegistrationBuilder convention = ConfigureImportPropertyConvention<ImportContractYPropertyDeclarationOverride>(t => t.AB);

            AssertHasDeclaredAttributesUnderConvention<ImportContractYPropertyDeclarationOverride>(t => t.AB, convention);
        }

        public class ImportManyPropertyDeclarationOverride
        {
            [ImportMany]
            public AB[] AB { get; set; }
        }

        [Fact]
        public void ImportPropertyConvention_ImportManyAttribute_ConventionIgnored()
        {
            RegistrationBuilder convention = ConfigureImportPropertyConvention<ImportManyPropertyDeclarationOverride>(t => t.AB);

            AssertHasDeclaredAttributesUnderConvention<ImportManyPropertyDeclarationOverride>(t => t.AB, convention);
        }

        public class NonImportPropertyDeclarationAttributes
        {
            [Export, ExportMetadata(MetadataKeys.MetadataKeyP, MetadataValues.MetadataValueN)]
            public AB AB { get; set; }
        }

        [Fact]
        public void ImportPropertyConvention_NonImportPropertyDeclarationAttributes_ConventionApplied()
        {
            RegistrationBuilder convention = ConfigureImportPropertyConvention<NonImportPropertyDeclarationAttributes>(t => t.AB);

            var unionOfConventionAndDeclared = typeof(NonImportPropertyDeclarationAttributes)
                .GetProperty("AB")
                .GetCustomAttributes(true)
                .Concat(ImportPropertyConventionAttributes)
                .ToArray();

            AssertHasAttributesUnderConvention<NonImportPropertyDeclarationAttributes>(t => t.AB, convention, unionOfConventionAndDeclared);
        }

        // The following test is for importing constructors

        private class TwoConstructorsWithOverride
        {
            [ImportingConstructor]
            public TwoConstructorsWithOverride(IContractA a) { }

            public TwoConstructorsWithOverride(IContractA a, IContractB b) { }
        }

        [Fact]
        public void ConstructorConvention_OverrideOnDeclaration_ConventionIgnored()
        {
            var rb = new RegistrationBuilder();
            rb.ForType<TwoConstructorsWithOverride>()
                .SelectConstructor(pi => new TwoConstructorsWithOverride(pi.Import<IContractA>(), pi.Import<IContractB>()));

            TypeInfo mapped = rb.MapType(typeof(TwoConstructorsWithOverride).GetTypeInfo());

            ConstructorInfo conventional = mapped.GetConstructor(new[] { rb.MapType(typeof(IContractA).GetTypeInfo()), rb.MapType(typeof(IContractB).GetTypeInfo()) });
            var conventionalAttrs = conventional.GetCustomAttributes(true);
            Assert.False(conventionalAttrs.Any());

            ConstructorInfo overridden = mapped.GetConstructor(new[] { rb.MapType(typeof(IContractA).GetTypeInfo()) });
            var overriddenAttr = overridden.GetCustomAttributes(true).Single();
            Assert.Equal(new ImportingConstructorAttribute(), overriddenAttr);
        }

        // Tests follow for constructor parameters

        private static RegistrationBuilder ConfigureImportConstructorParameterConvention<TPart>()
        {
            var convention = new RegistrationBuilder();

            convention.ForType<TPart>()
                 .SelectConstructor(cis => cis.Single(), (ci, ib) => ib.AsMany(false).AsContractName(ContractNames.ContractX).AsContractType<IContractA>());

            return convention;
        }

        private static object[] ImportParameterConventionAttributes = new object[] { new ImportAttribute(ContractNames.ContractX, typeof(IContractA)) };

        private class NoConstructorParameterOverrides
        {
            public NoConstructorParameterOverrides(IContractA a) { }
        }

        [Fact]
        public void ConstructorParameterConvention_NoOverride_ConventionApplied()
        {
            RegistrationBuilder convention = ConfigureImportConstructorParameterConvention<NoConstructorParameterOverrides>();
            TypeInfo mapped = convention.MapType(typeof(NoConstructorParameterOverrides).GetTypeInfo());
            ParameterInfo pi = mapped.GetConstructors().Single().GetParameters().Single();
            var actual = pi.GetCustomAttributes(true);
            AssertEquivalentAttributes(ImportParameterConventionAttributes, actual);
        }

        private class ConstructorParameterImportContractX
        {
            public ConstructorParameterImportContractX([Import(ContractNames.ContractX)] IContractA a) { }
        }

        [Fact]
        public void ConstructorParameterConvention_ImportOnDeclaration_ConventionIgnored()
        {
            RegistrationBuilder convention = ConfigureImportConstructorParameterConvention<ConstructorParameterImportContractX>();
            TypeInfo mapped = convention.MapType(typeof(ConstructorParameterImportContractX).GetTypeInfo());
            ParameterInfo pi = mapped.GetConstructors().Single().GetParameters().Single();
            var actual = pi.GetCustomAttributes(true);
            AssertEquivalentAttributes(new object[] { new ImportAttribute(ContractNames.ContractX) }, actual);
        }

        // Tests for creation policy

        private static RegistrationBuilder ConfigureCreationPolicyConvention<T>()
        {
            var convention = new RegistrationBuilder();
            convention.ForType<T>().SetCreationPolicy(CreationPolicy.NonShared);
            return convention;
        }

        private static readonly IEnumerable<object> CreationPolicyConventionAttributes = new[] { new PartCreationPolicyAttribute(CreationPolicy.NonShared) };

        private class NoCreationPolicyDeclared { }

        [Fact]
        public void CreationPolicyConvention_NoCreationPolicyDeclared_ConventionApplied()
        {
            RegistrationBuilder convention = ConfigureCreationPolicyConvention<NoCreationPolicyDeclared>();
            AssertHasAttributesUnderConvention<NoCreationPolicyDeclared>(convention, CreationPolicyConventionAttributes);
        }

        [PartCreationPolicy(CreationPolicy.Shared)]
        private class SetCreationPolicy { }

        [Fact]
        public void CreationPolicyConvention_CreationPolicyDeclared_ConventionIgnored()
        {
            RegistrationBuilder convention = ConfigureCreationPolicyConvention<SetCreationPolicy>();
            AssertHasDeclaredAttributesUnderConvention<SetCreationPolicy>(convention);
        }

        [Export]
        private class UnrelatedToCreationPolicy { }

        [Fact]
        public void CreationPolicyConvention_UnrelatedAttributesDeclared_ConventionApplied()
        {
            RegistrationBuilder convention = ConfigureCreationPolicyConvention<UnrelatedToCreationPolicy>();
            AssertHasAttributesUnderConvention<UnrelatedToCreationPolicy>(convention,
                CreationPolicyConventionAttributes.Concat(typeof(UnrelatedToCreationPolicy).GetCustomAttributes(true)));
        }

        // Tests for part discoverability

        [PartNotDiscoverable]
        private class NotDiscoverablePart { }

        [Fact]
        public void AnyConvention_NotDiscoverablePart_ConventionApplied()
        {
            var convention = new RegistrationBuilder();
            convention.ForType<NotDiscoverablePart>().Export();
            AssertHasAttributesUnderConvention<NotDiscoverablePart>(convention, new object[] {
                new PartNotDiscoverableAttribute(),
                new ExportAttribute()
            });
        }

        // Tests for part metadata

        private static RegistrationBuilder ConfigurePartMetadataConvention<T>()
        {
            var convention = new RegistrationBuilder();
            convention.ForType<T>().AddMetadata(MetadataKeys.MetadataKeyP, MetadataValues.MetadataValueN);
            return convention;
        }

        private static readonly IEnumerable<object> PartMetadataConventionAttributes = new object[]
        {
            new PartMetadataAttribute(MetadataKeys.MetadataKeyP, MetadataValues.MetadataValueN)
        };

        private class NoDeclaredPartMetadata { }

        [Fact]
        public void PartMetadataConvention_NoDeclaredMetadata_ConventionApplied()
        {
            RegistrationBuilder convention = ConfigurePartMetadataConvention<NoDeclaredPartMetadata>();
            AssertHasAttributesUnderConvention<NoDeclaredPartMetadata>(convention, PartMetadataConventionAttributes);
        }

        [PartMetadata(MetadataKeys.MetadataKeyQ, MetadataValues.MetadataValueO)]
        private class PartMetadataQO { }

        [Fact]
        public void PartMetadataConvention_DeclaredQO_ConventionIgnored()
        {
            RegistrationBuilder convention = ConfigurePartMetadataConvention<PartMetadataQO>();
            AssertHasDeclaredAttributesUnderConvention<PartMetadataQO>(convention);
        }

        [PartCreationPolicy(CreationPolicy.NonShared), Export]
        private class PartMetadataUnrelatedAttributes { }

        [Fact]
        public void PartMetadataConvention_UnrelatedDeclaredAttributes_ConventionApplied()
        {
            RegistrationBuilder convention = ConfigurePartMetadataConvention<PartMetadataUnrelatedAttributes>();
            AssertHasAttributesUnderConvention<PartMetadataUnrelatedAttributes>(convention,
                PartMetadataConventionAttributes.Concat(typeof(PartMetadataUnrelatedAttributes).GetCustomAttributes(true)));
        }

        private interface IFoo { }

        [Export]
        private class ExportInterfacesExportOverride : IFoo { }

        private class ExportInterfacesExportConventionApplied : IFoo { }

        [Export(typeof(IFoo))]
        public class ExportInterfacesExportConvention : IFoo { }

        private static RegistrationBuilder ConfigureExportInterfacesConvention<T>()
        {
            var convention = new RegistrationBuilder();
            convention.ForType<T>().ExportInterfaces((t) => true);
            return convention;
        }

        [Fact]
        public void ConfigureExportInterfaces_ExportInterfaces_Overridden()
        {
            RegistrationBuilder convention = ConfigureExportInterfacesConvention<ExportInterfacesExportOverride>();
            AssertHasAttributesUnderConvention<ExportInterfacesExportOverride>(convention,
                typeof(ExportInterfacesExportOverride).GetCustomAttributes(true));
        }

        [Fact]
        public void ConfigureExportInterfaces_ExportInterfaces_ConventionApplied()
        {
            RegistrationBuilder convention = ConfigureExportInterfacesConvention<ExportInterfacesExportConventionApplied>();
            AssertHasAttributesUnderConvention<ExportInterfacesExportConventionApplied>(convention,
                typeof(ExportInterfacesExportConvention).GetCustomAttributes(true));
        }

        // Tests for chained RCs

        private class ConventionTarget { }

        [Fact]
        public void ConventionsInInnerAndOuterRCs_InnerRCTakesPrecendence()
        {
            var innerConvention = new RegistrationBuilder();
            innerConvention.ForType<ConventionTarget>().Export(eb => eb.AsContractName(ContractNames.ContractX));
            TypeInfo innerType = innerConvention.MapType(typeof(ConventionTarget).GetTypeInfo());

            var outerConvention = new RegistrationBuilder();
            outerConvention.ForType<ConventionTarget>().Export(eb => eb.AsContractName(ContractNames.ContractY));
            TypeInfo outerType = outerConvention.MapType(innerType/*.GetTypeInfo()*/);

            ExportAttribute export = outerType.GetCustomAttributes(false).OfType<ExportAttribute>().Single();

            Assert.Equal(ContractNames.ContractX, export.ContractName);
        }
    }
}
