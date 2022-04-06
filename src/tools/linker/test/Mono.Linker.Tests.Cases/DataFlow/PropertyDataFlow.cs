// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the ExpectedWarning attributes.
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class PropertyDataFlow
	{
		public static void Main ()
		{
			var instance = new PropertyDataFlow ();

			instance.ReadFromInstanceProperty ();
			instance.WriteToInstanceProperty ();

			instance.ReadFromStaticProperty ();
			instance.WriteToStaticProperty ();
			instance.WriteToStaticPropertyExpressionValue ();

			_ = instance.PropertyPublicParameterlessConstructorWithExplicitAccessors;
			_ = instance.PropertyPublicConstructorsWithExplicitAccessors;
			_ = instance.PropertyNonPublicConstructorsWithExplicitAccessors;
			instance.PropertyPublicParameterlessConstructorWithExplicitAccessors = null;
			instance.PropertyPublicConstructorsWithExplicitAccessors = null;
			instance.PropertyNonPublicConstructorsWithExplicitAccessors = null;

			TestAutomaticPropagation ();

			PropertyWithAttributeMarkingItself.Test ();
			new TestWriteToGetOnlyProperty ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		Type PropertyWithPublicConstructor { get; set; }

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		static Type StaticPropertyWithPublicConstructor { get; set; }

		[ExpectedWarning ("IL2099", nameof (PropertyWithUnsupportedType))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static object PropertyWithUnsupportedType { get; set; }

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors))]
		private void ReadFromInstanceProperty ()
		{
			PropertyWithPublicConstructor.RequiresPublicParameterlessConstructor ();
			PropertyWithPublicConstructor.RequiresPublicConstructors ();
			PropertyWithPublicConstructor.RequiresNonPublicConstructors ();
			PropertyWithPublicConstructor.RequiresNone ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors))]
		private void ReadFromStaticProperty ()
		{
			StaticPropertyWithPublicConstructor.RequiresPublicParameterlessConstructor ();
			StaticPropertyWithPublicConstructor.RequiresPublicConstructors ();
			StaticPropertyWithPublicConstructor.RequiresNonPublicConstructors ();
			StaticPropertyWithPublicConstructor.RequiresNone ();
		}

		[ExpectedWarning ("IL2072", nameof (PropertyDataFlow) + "." + nameof (PropertyWithPublicConstructor) + ".set", nameof (GetTypeWithPublicParameterlessConstructor))]
		[ExpectedWarning ("IL2072", nameof (PropertyDataFlow) + "." + nameof (PropertyWithPublicConstructor) + ".set", nameof (GetTypeWithNonPublicConstructors))]
		[ExpectedWarning ("IL2072", nameof (PropertyDataFlow) + "." + nameof (PropertyWithPublicConstructor) + ".set", nameof (GetUnknownType))]
		private void WriteToInstanceProperty ()
		{
			PropertyWithPublicConstructor = GetTypeWithPublicParameterlessConstructor ();
			PropertyWithPublicConstructor = GetTypeWithPublicConstructors ();
			PropertyWithPublicConstructor = GetTypeWithNonPublicConstructors ();
			PropertyWithPublicConstructor = GetUnknownType ();
		}

		[ExpectedWarning ("IL2072", nameof (PropertyDataFlow) + "." + nameof (StaticPropertyWithPublicConstructor) + ".set", nameof (GetTypeWithPublicParameterlessConstructor))]
		[ExpectedWarning ("IL2072", nameof (PropertyDataFlow) + "." + nameof (StaticPropertyWithPublicConstructor) + ".set", nameof (GetTypeWithNonPublicConstructors))]
		[ExpectedWarning ("IL2072", nameof (PropertyDataFlow) + "." + nameof (StaticPropertyWithPublicConstructor) + ".set", nameof (GetUnknownType))]
		private void WriteToStaticProperty ()
		{
			StaticPropertyWithPublicConstructor = GetTypeWithPublicParameterlessConstructor ();
			StaticPropertyWithPublicConstructor = GetTypeWithPublicConstructors ();
			StaticPropertyWithPublicConstructor = GetTypeWithNonPublicConstructors ();
			StaticPropertyWithPublicConstructor = GetUnknownType ();
		}

		[ExpectedWarning ("IL2072", nameof (PropertyDataFlow) + "." + nameof (StaticPropertyWithPublicConstructor) + ".set", nameof (GetTypeWithNonPublicConstructors))]
		[ExpectedWarning ("IL2072", nameof (GetTypeWithNonPublicConstructors), nameof (DataFlowTypeExtensions.RequiresAll))]
		private void WriteToStaticPropertyExpressionValue ()
		{
			(StaticPropertyWithPublicConstructor = GetTypeWithNonPublicConstructors ()).RequiresAll ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		Type _fieldWithPublicConstructors;

		Type PropertyPublicConstructorsWithExplicitAccessors {
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			get {
				return _fieldWithPublicConstructors;
			}

			[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			set {
				_fieldWithPublicConstructors = value;
			}
		}

		Type PropertyPublicParameterlessConstructorWithExplicitAccessors {
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			get {
				return _fieldWithPublicConstructors;
			}

			[ExpectedWarning ("IL2069", nameof (PropertyDataFlow) + "." + nameof (_fieldWithPublicConstructors),
				"'value'",
				nameof (PropertyPublicParameterlessConstructorWithExplicitAccessors) + ".set")]
			[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			set {
				_fieldWithPublicConstructors = value;
			}
		}

		Type PropertyNonPublicConstructorsWithExplicitAccessors {
			[ExpectedWarning ("IL2078", nameof (PropertyDataFlow) + "." + nameof (PropertyNonPublicConstructorsWithExplicitAccessors) + ".get",
					nameof (_fieldWithPublicConstructors))]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			get {
				return _fieldWithPublicConstructors;
			}

			[ExpectedWarning ("IL2069", nameof (PropertyDataFlow) + "." + nameof (_fieldWithPublicConstructors),
				"'value'",
				nameof (PropertyNonPublicConstructorsWithExplicitAccessors) + ".set")]
			[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			set {
				_fieldWithPublicConstructors = value;
			}
		}

		static void TestAutomaticPropagation ()
		{
			var instance = new TestAutomaticPropagationType ();
			instance.TestImplicitProperty ();
			instance.TestPropertyWithSimpleGetter ();
			instance.TestPropertyWhichLooksLikeCompilerGenerated ();
			instance.TestInstancePropertyWithStaticField ();
			instance.TestPropertyWithDifferentBackingFields ();
			instance.TestPropertyWithExistingAttributes ();
			instance.TestPropertyWithConflictingAttributes ();
			instance.TestPropertyWithConflictingNoneAttributes ();
			instance.TestPropertyWithIndexerWithMatchingAnnotations (null);
			instance.TestPropertyWithIndexerWithoutMatchingAnnotations (null);
		}

		class TestAutomaticPropagationType
		{
			// Fully implicit property should work
			[ExpectedWarning ("IL2072", nameof (TestAutomaticPropagationType) + "." + nameof (ImplicitProperty) + ".set")]
			public void TestImplicitProperty ()
			{
				ImplicitProperty.RequiresPublicConstructors ();
				ImplicitProperty = GetTypeWithPublicParameterlessConstructor (); // This will warn since the setter requires public .ctors
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			static Type ImplicitProperty {
				get; set;
			}

			// Simple getter is not enough - we do detect the field, but we require the field to be compiler generated for this to work
			[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
			// Make sure we don't warn about the field in context of property annotation propagation.
			[LogDoesNotContain ("Could not find a unique backing field for property 'System.Type Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow/TestAutomaticPropagationType::PropertyWithSimpleGetter()'")]
			public void TestPropertyWithSimpleGetter ()
			{
				_ = PropertyWithSimpleGetter;
				PropertyWithSimpleGetter_Field.RequiresPublicConstructors ();
			}

			static Type PropertyWithSimpleGetter_Field;

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			static Type PropertyWithSimpleGetter {
				[ExpectedWarning ("IL2078", nameof (TestAutomaticPropagationType) + "." + nameof (PropertyWithSimpleGetter) + ".get")]
				get {
					return PropertyWithSimpleGetter_Field;
				}
			}

			// This test creates a property and field which linker will recognized as a compiler generated backing field for the property
			// The purpose of the test is to verify, that linker flows the annotation to the backing field and since there's
			// no good way to reference actual compiler generated backing field, we "Fake" it here as compiler generated.
			// But this doesn't fool the analyzer - since it's seen as yet another user declared field, so it doesn't propagate the
			// annotation.
			// This discrepancy is currently by design and not worth the trouble to implement logic for it in the analyzer.
			// Producing fields which are annotated with CompilerGeneratedAttribute and making them look like backing fields
			// is highly unlikely to be done by anybody. If it happens, the analyzer will produce warnings which the linker will not
			// but those warnings are not really wrong, so it's better if the developer fixes them anyway.
			[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicConstructors) + "(Type)",
				nameof (TestAutomaticPropagationType) + "." + nameof (PropertyWhichLooksLikeCompilerGenerated_Field),
				ProducedBy = ProducedBy.Analyzer)]
			public void TestPropertyWhichLooksLikeCompilerGenerated ()
			{
				// If the property was correctly recognized both the property getter and the backing field should get the annotation.
				PropertyWhichLooksLikeCompilerGenerated.RequiresPublicConstructors ();
				PropertyWhichLooksLikeCompilerGenerated_Field.RequiresPublicConstructors ();
			}

			[CompilerGenerated]
			private static Type PropertyWhichLooksLikeCompilerGenerated_Field;

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			static Type PropertyWhichLooksLikeCompilerGenerated {
				// See above comment about fake compiler generated backing fields - this warning is expected from the analyzer
				[ExpectedWarning ("IL2078", nameof (TestAutomaticPropagationType) + "." + nameof (PropertyWhichLooksLikeCompilerGenerated) + ".get",
					nameof (TestAutomaticPropagationType) + "." + nameof (PropertyWhichLooksLikeCompilerGenerated_Field),
					ProducedBy = ProducedBy.Analyzer)]
				get {
					return PropertyWhichLooksLikeCompilerGenerated_Field;
				}
			}

			[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
			// Make sure we don't warn about the field in context of property annotation propagation.
			[LogDoesNotContain ("Could not find a unique backing field for property 'System.Type Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow/TestAutomaticPropagationType::InstancePropertyWithStaticField()'")]
			public void TestInstancePropertyWithStaticField ()
			{
				InstancePropertyWithStaticField = null;
				InstancePropertyWithStaticField_Field.RequiresPublicConstructors ();
			}

			[CompilerGenerated]
			private static Type InstancePropertyWithStaticField_Field;

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type InstancePropertyWithStaticField {
				// Nothing to warn about - the "value" is annotated with PublicConstructors and we're assigning
				// it to unannotated field - that's a perfectly valid operation.
				set {
					InstancePropertyWithStaticField_Field = value;
				}
			}

			public void TestPropertyWithDifferentBackingFields ()
			{
				_ = PropertyWithDifferentBackingFields;
			}

			[CompilerGenerated]
			private Type PropertyWithDifferentBackingFields_GetterField;

			[CompilerGenerated]
			private Type PropertyWithDifferentBackingFields_SetterField;

			// Analyzer doesn't try to detect backing fields of properties: https://github.com/dotnet/linker/issues/2273
			[ExpectedWarning ("IL2042",
				"Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow.TestAutomaticPropagationType.PropertyWithDifferentBackingFields",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2078",
				nameof (TestAutomaticPropagationType) + "." + nameof (PropertyWithDifferentBackingFields) + ".get",
				"Type",
				ProducedBy = ProducedBy.Analyzer)]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type PropertyWithDifferentBackingFields {
				[ExpectedWarning ("IL2078",
					nameof (TestAutomaticPropagationType) + "." + nameof (PropertyWithDifferentBackingFields) + ".get",
					ProducedBy = ProducedBy.Trimmer)]
				get {
					return PropertyWithDifferentBackingFields_GetterField;
				}

				set {
					PropertyWithDifferentBackingFields_SetterField = value;
				}
			}

			public void TestPropertyWithExistingAttributes ()
			{
				_ = PropertyWithExistingAttributes;
				PropertyWithExistingAttributes = null;
			}

			// Analyzer doesn't try to detect backing fields of properties: https://github.com/dotnet/linker/issues/2273
			[ExpectedWarning ("IL2056", "PropertyWithExistingAttributes", "PropertyWithExistingAttributes_Field",
				ProducedBy = ProducedBy.Trimmer)]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			[CompilerGenerated]
			Type PropertyWithExistingAttributes_Field;

			[ExpectedWarning ("IL2043", "PropertyWithExistingAttributes", "PropertyWithExistingAttributes.get", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL2043", "PropertyWithExistingAttributes", "PropertyWithExistingAttributes.set", ProducedBy = ProducedBy.Analyzer)]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type PropertyWithExistingAttributes {
				// On property/accessor mismatch, linker warns on accessor and analyzer warns on property https://github.com/dotnet/linker/issues/2654
				[ExpectedWarning ("IL2043", "PropertyWithExistingAttributes", "PropertyWithExistingAttributes.get",
					ProducedBy = ProducedBy.Trimmer)]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
				get { return PropertyWithExistingAttributes_Field; }

				// On property/accessor mismatch, linker warns on accessor and analyzer warns on property https://github.com/dotnet/linker/issues/2654
				[ExpectedWarning ("IL2043", "PropertyWithExistingAttributes", "PropertyWithExistingAttributes.set",
					ProducedBy = ProducedBy.Trimmer)]
				[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
				set { PropertyWithExistingAttributes_Field = value; }
			}

			// When the property annotation conflicts with the getter/setter annotation,
			// we issue a warning (IL2043 below) but respect the getter/setter annotations.
			[ExpectedWarning ("IL2072", nameof (TestAutomaticPropagationType) + "." + nameof (PropertyWithConflictingAttributes) + ".get",
				nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicConstructors) + "(Type)")]
			[ExpectedWarning ("IL2072", nameof (TestAutomaticPropagationType) + "." + nameof (PropertyWithConflictingAttributes) + ".set",
				nameof (PropertyDataFlow) + "." + nameof (GetTypeWithPublicConstructors) + "()")]
			public void TestPropertyWithConflictingAttributes ()
			{
				PropertyWithConflictingAttributes.RequiresPublicConstructors ();
				PropertyWithConflictingAttributes.RequiresNonPublicConstructors ();
				PropertyWithConflictingAttributes = GetTypeWithPublicConstructors ();
				PropertyWithConflictingAttributes = GetTypeWithNonPublicConstructors ();
			}

			// Analyzer doesn't try to detect backing fields of properties: https://github.com/dotnet/linker/issues/2273
			[ExpectedWarning ("IL2056", "PropertyWithConflictingAttributes", "PropertyWithConflictingAttributes_Field",
				ProducedBy = ProducedBy.Trimmer)]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			[CompilerGenerated]
			Type PropertyWithConflictingAttributes_Field;

			[ExpectedWarning ("IL2043", "PropertyWithConflictingAttributes", "PropertyWithConflictingAttributes.get", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL2043", "PropertyWithConflictingAttributes", "PropertyWithConflictingAttributes.set", ProducedBy = ProducedBy.Analyzer)]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type PropertyWithConflictingAttributes {
				// On property/accessor mismatch, linker warns on accessor and analyzer warns on property https://github.com/dotnet/linker/issues/2654
				[ExpectedWarning ("IL2043", "PropertyWithConflictingAttributes", "PropertyWithConflictingAttributes.get",
					ProducedBy = ProducedBy.Trimmer)]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)]
				get { return PropertyWithConflictingAttributes_Field; }

				// On property/accessor mismatch, linker warns on accessor and analyzer warns on property https://github.com/dotnet/linker/issues/2654
				[ExpectedWarning ("IL2043", "PropertyWithConflictingAttributes", "PropertyWithConflictingAttributes.set",
					ProducedBy = ProducedBy.Trimmer)]
				[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)]
				set { PropertyWithConflictingAttributes_Field = value; }
			}

			// When the property annotation is DAMT.None and this conflicts with the getter/setter annotations,
			// we don't produce a warning about the conflict, and just respect the property annotations.
			[ExpectedWarning ("IL2072", nameof (TestAutomaticPropagationType) + "." + nameof (PropertyWithConflictingNoneAttributes) + ".get",
				nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors) + "(Type)")]
			[ExpectedWarning ("IL2072", nameof (TestAutomaticPropagationType) + "." + nameof (PropertyWithConflictingNoneAttributes) + ".set",
				nameof (PropertyDataFlow) + "." + nameof (GetTypeWithNonPublicConstructors) + "()")]
			public void TestPropertyWithConflictingNoneAttributes ()
			{
				PropertyWithConflictingNoneAttributes.RequiresPublicConstructors ();
				PropertyWithConflictingNoneAttributes.RequiresNonPublicConstructors ();
				PropertyWithConflictingNoneAttributes = GetTypeWithPublicConstructors ();
				PropertyWithConflictingNoneAttributes = GetTypeWithNonPublicConstructors ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.None)]
			[CompilerGenerated]
			Type PropertyWithConflictingNoneAttributes_Field;

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type PropertyWithConflictingNoneAttributes {
				// See above comment about fake compiler generated backing fields - this warning is expected from analyzer
				[ExpectedWarning ("IL2078", nameof (TestAutomaticPropagationType) + "." + nameof (PropertyWithConflictingNoneAttributes) + ".get",
					nameof (TestAutomaticPropagationType) + "." + nameof (PropertyWithConflictingNoneAttributes_Field),
					ProducedBy = ProducedBy.Analyzer)]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.None)]
				get { return PropertyWithConflictingNoneAttributes_Field; }

				[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.None)]
				set { PropertyWithConflictingNoneAttributes_Field = value; }
			}

			public void TestPropertyWithIndexerWithMatchingAnnotations ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] Type myType)
			{
				var propclass = new PropertyWithIndexer ();
				propclass[1] = myType;
				propclass[1].RequiresPublicConstructors ();
			}

			// Trimmer and analyzer handle formatting of indexers differently.
			[ExpectedWarning ("IL2067", nameof (PropertyWithIndexer) + ".Item.set", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2067", nameof (PropertyWithIndexer) + ".this[Int32].set", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors) + "(Type)")]
			[LogDoesNotContain ("'Value passed to parameter 'index' of method 'Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow.TestAutomaticPropagationType.PropertyWithIndexer.Item.set'")]
			public void TestPropertyWithIndexerWithoutMatchingAnnotations (Type myType)
			{
				var propclass = new PropertyWithIndexer ();
				propclass[1] = myType;
				propclass[1].RequiresNonPublicConstructors ();
			}


			public class PropertyWithIndexer
			{
				Type[] Property_Field;

				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
				public Type this[int index] {
					// Trimmer and analyzer handle formatting of indexers differently.
					[ExpectedWarning ("IL2063", nameof (PropertyWithIndexer) + ".Item.get", ProducedBy = ProducedBy.Trimmer)]
					[ExpectedWarning ("IL2063", nameof (PropertyWithIndexer) + ".this[Int32].get", ProducedBy = ProducedBy.Analyzer)]
					get => Property_Field[index];
					set => Property_Field[index] = value;
				}
			}
		}

		class PropertyWithAttributeMarkingItself
		{
			class AttributeRequiresAllProperties : Attribute
			{
				public AttributeRequiresAllProperties (
					[DynamicallyAccessedMembersAttribute (DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
				{
				}
			}

			class TestPropertyWithAttributeMarkingSelfType
			{
				[AttributeRequiresAllProperties (typeof (TestPropertyWithAttributeMarkingSelfType))]
				public static bool TestProperty { get; set; }
			}

			public static void Test ()
			{
				// https://github.com/dotnet/linker/issues/2196
				// TestPropertyWithAttributeMarkingSelfType.TestProperty = true;
			}
		}

		class TestWriteToGetOnlyProperty
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
			public Type GetOnlyProperty { get; }

			// Analyzer doesn't warn about compiler-generated backing field of property
			[ExpectedWarning ("IL2074", nameof (TestWriteToGetOnlyProperty), nameof (GetUnknownType),
				ProducedBy = ProducedBy.Trimmer)]
			public TestWriteToGetOnlyProperty ()
			{
				GetOnlyProperty = GetUnknownType ();
			}
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		private static Type GetTypeWithPublicParameterlessConstructor ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		private static Type GetTypeWithPublicConstructors ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)]
		private static Type GetTypeWithNonPublicConstructors ()
		{
			return null;
		}

		private static Type GetUnknownType ()
		{
			return null;
		}
	}
}
