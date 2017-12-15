// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Linq;
using System.Reflection;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition.AttributedModel
{
    public class AttributedModelDiscoveryTests
    {
        [Fact]
        public void CreatePartDefinition_TypeWithExports_ShouldHaveMultipleExports()
        {
            var definition = CreateDefinition(typeof(PublicComponentWithPublicExports));
            EnumerableAssert.AreEqual(definition.ExportDefinitions.Select(e => e.ContractName), "PublicField", "PublicProperty", "PublicDelegate");
        }

        public abstract class BaseClassWithPropertyExports
        {
            [Export("MyPropBase")]
            public virtual int MyProp { get; set; }
        }

        public class DerivedClassWithInheritedPropertyExports : BaseClassWithPropertyExports
        {
            public override int MyProp { get; set; }
        }

        [ActiveIssue(551341)]
        [Fact]
        public void ShowIssueWithVirtualPropertiesInReflectionAPI()
        {
            PropertyInfo propInfo = typeof(BaseClassWithPropertyExports).GetProperty("MyProp");

            // pi.GetCustomAttributes does not find the inherited attributes
            var c1 = propInfo.GetCustomAttributes(true);

            // Attribute.GetCustomAttributes does find the inherited attributes
            var c2 = Attribute.GetCustomAttributes(propInfo, true);

            // This seems like it should be a bug in the reflection API's... 
            Assert.NotEqual(c1, c2);
        }

        [Fact]
        public void CreatePartDefinition_TypeWithImports_ShouldHaveMultipleImports()
        {
            var definition = CreateDefinition(typeof(PublicImportsExpectingPublicExports));
            EnumerableAssert.AreEqual(definition.ImportDefinitions.Cast<ContractBasedImportDefinition>()
                                                           .Select(i => i.ContractName), "PublicField", "PublicProperty", "PublicDelegate", "PublicIGetString");
        }

        public class AnyImplicitExport
        {

        }

        [Fact]
        public void CreatePartDefinition_AnyType_ShouldHaveMetadataWithAnyImplicitCreationPolicy()
        {
            var definition = CreateDefinition(typeof(AnyImplicitExport));

            Assert.Equal(CreationPolicy.Any, definition.Metadata.GetValue<CreationPolicy>(CompositionConstants.PartCreationPolicyMetadataName));
        }

        [PartCreationPolicy(CreationPolicy.Any)]
        public class AnyExport
        {

        }

        [Fact]
        public void CreatePartDefinition_AnyType_ShouldHaveMetadataWithAnyCreationPolicy()
        {
            var definition = CreateDefinition(typeof(AnyExport));

            Assert.Equal(CreationPolicy.Any, definition.Metadata.GetValue<CreationPolicy>(CompositionConstants.PartCreationPolicyMetadataName));
        }

        [PartCreationPolicy(CreationPolicy.Shared)]
        public class SharedExport
        {

        }

        [Fact]
        public void CreatePartDefinition_SharedType_ShouldHaveMetadataWithSharedCreationPolicy()
        {
            var definition = CreateDefinition(typeof(SharedExport));

            Assert.Equal(CreationPolicy.Shared, definition.Metadata.GetValue<CreationPolicy>(CompositionConstants.PartCreationPolicyMetadataName));
        }

        [PartCreationPolicy(CreationPolicy.NonShared)]
        public class NonSharedExport
        {

        }

        [Fact]
        public void CreatePartDefinition_NonSharedType_ShouldHaveMetadataWithNonSharedCreationPolicy()
        {
            var definition = CreateDefinition(typeof(NonSharedExport));

            Assert.Equal(CreationPolicy.NonShared, definition.Metadata.GetValue<CreationPolicy>(CompositionConstants.PartCreationPolicyMetadataName));
        }

        [PartMetadata(CompositionConstants.PartCreationPolicyMetadataName, CreationPolicy.NonShared)]
        [PartMetadata("ShouldNotBeIgnored", "Value")]
        public class PartWithIgnoredMetadata
        {
        }

        [Fact]
        public void CreatePartDefinition_SharedTypeMarkedWithNonSharedMetadata_ShouldHaveMetadatWithSharedCreationPolicy()
        {
            // Type should just contain all the default settings of Shared
            var definition = CreateDefinition(typeof(PartWithIgnoredMetadata));

            // CompositionConstants.PartCreationPolicyMetadataName should be ignored
            Assert.NotEqual(CreationPolicy.NonShared, definition.Metadata.GetValue<CreationPolicy>(CompositionConstants.PartCreationPolicyMetadataName));

            // Key ShouldNotBeIgnored should actully be in the dictionary
            Assert.Equal("Value", definition.Metadata["ShouldNotBeIgnored"]);
        }

        [PartMetadata("BaseOnlyName", 1)]
        [PartMetadata("OverrideName", 2)]
        public class BasePartWithMetdata
        {

        }

        [PartMetadata("DerivedOnlyName", 3)]
        [PartMetadata("OverrideName", 4)]
        public class DerivedPartWithMetadata : BasePartWithMetdata
        {

        }

        [Fact]
        public void CreatePartDefinition_InheritedPartMetadata_ShouldNotContainPartMetadataFromBase()
        {
            var definition = CreateDefinition(typeof(DerivedPartWithMetadata));

            Assert.False(definition.Metadata.ContainsKey("BaseOnlyName"), "Should not inherit part metadata from base.");
            Assert.Equal(3, definition.Metadata["DerivedOnlyName"]);
            Assert.Equal(4, definition.Metadata["OverrideName"]);
        }

        [Fact]
        public void CreatePartDefinition_NoMarkedOrDefaultConstructorAsPartTypeArgument_ShouldSetConstructorToNull()
        {
            var definition = CreateDefinition(typeof(ClassWithNoMarkedOrDefaultConstructor));

            Assert.Null(definition.GetConstructor());
        }

        [Fact]
        public void CreatePartDefinition_MultipleMarkedConstructorsAsPartTypeArgument_ShouldSetConstructors()
        {
            var definition = CreateDefinition(typeof(ClassWithMultipleMarkedConstructors));

            Assert.Null(definition.GetConstructor());
        }

        [Fact]
        public void CreatePartDefinition_OneMarkedConstructorsAsPartTypeArgument_ShouldSetConstructorToMarked()
        {
            var definition = CreateDefinition(typeof(SimpleConstructorInjectedObject));

            ConstructorInfo constructor = definition.GetConstructor();
            Assert.NotNull(constructor);
            Assert.Equal(typeof(SimpleConstructorInjectedObject).GetConstructors()[0], constructor);
            Assert.Equal(constructor.GetParameters().Length, definition.ImportDefinitions.OfType<ReflectionParameterImportDefinition>().Count());
        }

        [Fact]
        public void CreatePartDefinition_OneDefaultConstructorAsPartTypeArgument_ShouldSetConstructorToDefault()
        {
            var definition = CreateDefinition(typeof(PublicComponentWithPublicExports));

            ConstructorInfo constructor = definition.GetConstructor();
            Assert.NotNull(constructor);

            Assert.Empty(constructor.GetParameters());
            Assert.Empty(definition.ImportDefinitions.OfType<ReflectionParameterImportDefinition>());
        }

        [Fact]
        public void CreatePartDefinition_OneMarkedAndOneDefaultConstructorsAsPartTypeArgument_ShouldSetConstructorToMarked()
        {
            var definition = CreateDefinition(typeof(ClassWithOneMarkedAndOneDefaultConstructor));
            var marked = typeof(ClassWithOneMarkedAndOneDefaultConstructor).GetConstructors()[0];
            Assert.True(marked.IsDefined(typeof(ImportingConstructorAttribute), false));

            ConstructorInfo constructor = definition.GetConstructor();
            Assert.NotNull(constructor);

            Assert.Equal(marked, constructor);
            Assert.Equal(marked.GetParameters().Length, definition.ImportDefinitions.OfType<ReflectionParameterImportDefinition>().Count());
        }

        [Fact]
        public void CreatePartDefinition_NoConstructorBecauseStatic_ShouldHaveNullConstructor()
        {
            var definition = CreateDefinition(typeof(StaticExportClass));

            ConstructorInfo constructor = definition.GetConstructor();
            Assert.Null(constructor);

            Assert.Empty(definition.ImportDefinitions.OfType<ReflectionParameterImportDefinition>());
        }

        [Fact]
        public void CreatePartDefinition_TwoZeroParameterConstructors_ShouldPickNonStaticOne()
        {
            var definition = CreateDefinition(typeof(ClassWithTwoZeroParameterConstructors));

            ConstructorInfo constructor = definition.GetConstructor();
            Assert.NotNull(constructor);
            Assert.False(constructor.IsStatic);

            Assert.Empty(definition.ImportDefinitions.OfType<ReflectionParameterImportDefinition>());
        }

        [Fact]
        [ActiveIssue(25498)]
        public void IsDiscoverable()
        {
            var expectations = new ExpectationCollection<Type, bool>();
            expectations.Add(typeof(ClassWithTwoZeroParameterConstructors), true);
            expectations.Add(typeof(SimpleConstructorInjectedObject), true);
            expectations.Add(typeof(StaticExportClass), true);
            expectations.Add(typeof(PublicComponentWithPublicExports), true);
            expectations.Add(typeof(ClassWithMultipleMarkedConstructors), true);
            expectations.Add(typeof(ClassWithNoMarkedOrDefaultConstructor), true);
            expectations.Add(typeof(ClassWhichOnlyHasDefaultConstructor), false);
            expectations.Add(typeof(ClassWithOnlyHasImportingConstructorButInherits), true);
            expectations.Add(typeof(ClassWithOnlyHasMultipleImportingConstructorButInherits), true);

            foreach (var e in expectations)
            {
                var definition = AttributedModelDiscovery.CreatePartDefinitionIfDiscoverable(e.Input, (ICompositionElement)null);

                bool result = (definition != null);

                Assert.Equal(e.Output, result);
            }
        }

        [Fact]
        [ActiveIssue(25498)]
        public void CreatePartDefinition_EnsureIsDiscoverable()
        {
            var expectations = new ExpectationCollection<Type, bool>();
            expectations.Add(typeof(ClassWithTwoZeroParameterConstructors), true);
            expectations.Add(typeof(SimpleConstructorInjectedObject), true);
            expectations.Add(typeof(StaticExportClass), true);
            expectations.Add(typeof(PublicComponentWithPublicExports), true);
            expectations.Add(typeof(ClassWithMultipleMarkedConstructors), true);
            expectations.Add(typeof(ClassWithNoMarkedOrDefaultConstructor), true);
            expectations.Add(typeof(ClassWhichOnlyHasDefaultConstructor), false);
            expectations.Add(typeof(ClassWithOnlyHasImportingConstructorButInherits), true);
            expectations.Add(typeof(ClassWithOnlyHasMultipleImportingConstructorButInherits), true);

            foreach (var e in expectations)
            {
                var definition = AttributedModelServices.CreatePartDefinition(e.Input, null, true);

                bool result = (definition != null);

                Assert.Equal(e.Output, result);
            }
        }

        [Fact]
        public void CreatePartDefinition_NotEnsureIsDiscoverable()
        {
            var expectations = new ExpectationCollection<Type, bool>();
            expectations.Add(typeof(ClassWithTwoZeroParameterConstructors), true);
            expectations.Add(typeof(SimpleConstructorInjectedObject), true);
            expectations.Add(typeof(StaticExportClass), true);
            expectations.Add(typeof(PublicComponentWithPublicExports), true);
            expectations.Add(typeof(ClassWithMultipleMarkedConstructors), true);
            expectations.Add(typeof(ClassWithNoMarkedOrDefaultConstructor), true);
            expectations.Add(typeof(ClassWhichOnlyHasDefaultConstructor), false);
            expectations.Add(typeof(ClassWithOnlyHasImportingConstructorButInherits), true);
            expectations.Add(typeof(ClassWithOnlyHasMultipleImportingConstructorButInherits), true);

            foreach (var e in expectations)
            {
                var definition = AttributedModelServices.CreatePartDefinition(e.Input, null, false);
                Assert.NotNull(definition);
            }
        }

        [Fact]
        public void CreatePart_ObjectInstance_ShouldProduceSharedPart()
        {
            var part = AttributedModelServices.CreatePart(typeof(MyExport));

            Assert.Equal(CreationPolicy.Shared, part.Metadata.GetValue<CreationPolicy>(CompositionConstants.PartCreationPolicyMetadataName));
        }

        private ReflectionComposablePartDefinition CreateDefinition(Type type)
        {
            var definition = AttributedModelDiscovery.CreatePartDefinition(type, null, false, ElementFactory.Create());

            Assert.Equal(type, definition.GetPartType());

            return definition;
        }

        [InheritedExport]
        [InheritedExport]
        [InheritedExport]
        [Export]
        [InheritedExport]
        [InheritedExport]
        [InheritedExport]
        public class DuplicateMixedExporter1
        {
        }

        [Fact]
        [ActiveIssue(710352)]
        public void MixedDuplicateExports_ShouldOnlyCollapseInheritedExport()
        {
            var def = AttributedModelServices.CreatePartDefinition(typeof(DuplicateMixedExporter1), null);
            Assert.Equal(2, def.ExportDefinitions.Count());
        }
    }
}
