// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the UnrecognizedReflectionAccessPattern attributes.
	[SkipKeptItemsValidation]
	public class PropertyDataFlow
	{
		public static void Main ()
		{
			var instance = new PropertyDataFlow ();

			instance.ReadFromInstanceProperty ();
			instance.WriteToInstanceProperty ();

			instance.ReadFromStaticProperty ();
			instance.WriteToStaticProperty ();

			_ = instance.PropertyPublicParameterlessConstructorWithExplicitAccessors;
			_ = instance.PropertyPublicConstructorsWithExplicitAccessors;
			_ = instance.PropertyNonPublicConstructorsWithExplicitAccessors;
			instance.PropertyPublicParameterlessConstructorWithExplicitAccessors = null;
			instance.PropertyPublicConstructorsWithExplicitAccessors = null;
			instance.PropertyNonPublicConstructorsWithExplicitAccessors = null;

			TestAutomaticPropagation ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		Type PropertyWithPublicConstructor { get; set; }

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		static Type StaticPropertyWithPublicConstructor { get; set; }

		[ExpectedWarning ("IL2099", nameof (PropertyWithUnsupportedType))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static object PropertyWithUnsupportedType { get; set; }

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		private void ReadFromInstanceProperty ()
		{
			RequirePublicParameterlessConstructor (PropertyWithPublicConstructor);
			RequirePublicConstructors (PropertyWithPublicConstructor);
			RequireNonPublicConstructors (PropertyWithPublicConstructor);
			RequireNothing (PropertyWithPublicConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		private void ReadFromStaticProperty ()
		{
			RequirePublicParameterlessConstructor (StaticPropertyWithPublicConstructor);
			RequirePublicConstructors (StaticPropertyWithPublicConstructor);
			RequireNonPublicConstructors (StaticPropertyWithPublicConstructor);
			RequireNothing (StaticPropertyWithPublicConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (PropertyWithPublicConstructor) + ".set", new Type[] { typeof (Type) }, messageCode: "IL2072")]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (PropertyWithPublicConstructor) + ".set", new Type[] { typeof (Type) }, messageCode: "IL2072")]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (PropertyWithPublicConstructor) + ".set", new Type[] { typeof (Type) }, messageCode: "IL2072")]
		private void WriteToInstanceProperty ()
		{
			PropertyWithPublicConstructor = GetTypeWithPublicParameterlessConstructor ();
			PropertyWithPublicConstructor = GetTypeWithPublicConstructors ();
			PropertyWithPublicConstructor = GetTypeWithNonPublicConstructors ();
			PropertyWithPublicConstructor = GetUnkownType ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (StaticPropertyWithPublicConstructor) + ".set", new Type[] { typeof (Type) }, messageCode: "IL2072")]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (StaticPropertyWithPublicConstructor) + ".set", new Type[] { typeof (Type) }, messageCode: "IL2072")]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (StaticPropertyWithPublicConstructor) + ".set", new Type[] { typeof (Type) }, messageCode: "IL2072")]
		private void WriteToStaticProperty ()
		{
			StaticPropertyWithPublicConstructor = GetTypeWithPublicParameterlessConstructor ();
			StaticPropertyWithPublicConstructor = GetTypeWithPublicConstructors ();
			StaticPropertyWithPublicConstructor = GetTypeWithNonPublicConstructors ();
			StaticPropertyWithPublicConstructor = GetUnkownType ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		Type _fieldWithPublicConstructors;

		Type PropertyPublicConstructorsWithExplicitAccessors {
			[RecognizedReflectionAccessPattern]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			get {
				return _fieldWithPublicConstructors;
			}

			[RecognizedReflectionAccessPattern]
			[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			set {
				_fieldWithPublicConstructors = value;
			}
		}

		Type PropertyPublicParameterlessConstructorWithExplicitAccessors {
			[RecognizedReflectionAccessPattern]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			get {
				return _fieldWithPublicConstructors;
			}

			[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (_fieldWithPublicConstructors),
				messageCode: "IL2069", message: new string[] { "value", nameof (PropertyPublicParameterlessConstructorWithExplicitAccessors) + ".set", nameof (_fieldWithPublicConstructors) })]
			[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			set {
				_fieldWithPublicConstructors = value;
			}
		}

		Type PropertyNonPublicConstructorsWithExplicitAccessors {
			[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (PropertyNonPublicConstructorsWithExplicitAccessors) + ".get",
				new Type[] { }, returnType: typeof (Type), messageCode: "IL2078", message: new string[] {
					nameof (_fieldWithPublicConstructors),
					nameof (PropertyNonPublicConstructorsWithExplicitAccessors) + ".get"
				})]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			get {
				return _fieldWithPublicConstructors;
			}

			[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (_fieldWithPublicConstructors), messageCode: "IL2069")]
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
			instance.TestPropertyWithIndexerWithMatchingAnnotations (null);
			instance.TestPropertyWithIndexerWithoutMatchingAnnotations (null);
		}

		class TestAutomaticPropagationType
		{
			// Fully implicit property should work
			[UnrecognizedReflectionAccessPattern (typeof (TestAutomaticPropagationType), nameof (ImplicitProperty) + ".set", new Type[] { typeof (Type) }, messageCode: "IL2072")]
			public void TestImplicitProperty ()
			{
				RequirePublicConstructors (ImplicitProperty);
				ImplicitProperty = GetTypeWithPublicParameterlessConstructor (); // This will warn since the setter requires public .ctors
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			static Type ImplicitProperty {
				get; set;
			}

			// Simple getter is not enough - we do detect the field, but we require the field to be compiler generated for this to work
			[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2077")]
			// Make sure we don't warn about the field in context of property annotation propagation.
			[LogDoesNotContain ("Could not find a unique backing field for property 'System.Type Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow/TestAutomaticPropagationType::PropertyWithSimpleGetter()'")]
			public void TestPropertyWithSimpleGetter ()
			{
				_ = PropertyWithSimpleGetter;
				RequirePublicConstructors (PropertyWithSimpleGetter_Field);
			}

			static Type PropertyWithSimpleGetter_Field;

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			static Type PropertyWithSimpleGetter {
				[UnrecognizedReflectionAccessPattern (typeof (TestAutomaticPropagationType), nameof (PropertyWithSimpleGetter) + ".get", new Type[] { }, returnType: typeof (Type), messageCode: "IL2078")]
				get {
					return PropertyWithSimpleGetter_Field;
				}
			}

			[RecognizedReflectionAccessPattern]
			public void TestPropertyWhichLooksLikeCompilerGenerated ()
			{
				// If the property was correctly recognized both the property getter and the backing field should get the annotation.
				RequirePublicConstructors (PropertyWhichLooksLikeCompilerGenerated);
				RequirePublicConstructors (PropertyWhichLooksLikeCompilerGenerated_Field);
			}

			[CompilerGenerated]
			private static Type PropertyWhichLooksLikeCompilerGenerated_Field;

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			static Type PropertyWhichLooksLikeCompilerGenerated {
				get {
					return PropertyWhichLooksLikeCompilerGenerated_Field;
				}
			}

			[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2077")]
			// Make sure we don't warn about the field in context of property annotation propagation.
			[LogDoesNotContain ("Could not find a unique backing field for property 'System.Type Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow/TestAutomaticPropagationType::InstancePropertyWithStaticField()'")]
			public void TestInstancePropertyWithStaticField ()
			{
				InstancePropertyWithStaticField = null;
				RequirePublicConstructors (InstancePropertyWithStaticField_Field);
			}

			[CompilerGenerated]
			private static Type InstancePropertyWithStaticField_Field;

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type InstancePropertyWithStaticField {
				// Nothing to warn about - the "value" is annotated with PublicConstructors and we're assigning
				// it to unannotated field - that's a perfectly valid operation.
				[RecognizedReflectionAccessPattern]
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

			[ExpectedWarning ("IL2042",
				"System.Type Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow/TestAutomaticPropagationType::PropertyWithDifferentBackingFields()")]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type PropertyWithDifferentBackingFields {
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

			[ExpectedWarning ("IL2056", "PropertyWithExistingAttributes", "PropertyWithExistingAttributes_Field")]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			[CompilerGenerated]
			Type PropertyWithExistingAttributes_Field;

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type PropertyWithExistingAttributes {
				[ExpectedWarning ("IL2043", "PropertyWithExistingAttributes", "PropertyWithExistingAttributes.get")]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
				get { return PropertyWithExistingAttributes_Field; }

				[ExpectedWarning ("IL2043", "PropertyWithExistingAttributes", "PropertyWithExistingAttributes.set")]
				[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
				set { PropertyWithExistingAttributes_Field = value; }
			}

			[RecognizedReflectionAccessPattern]
			public void TestPropertyWithIndexerWithMatchingAnnotations ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] Type myType)
			{
				var propclass = new PropertyWithIndexer ();
				propclass[1] = myType;
				propclass[1].RequiresPublicConstructors ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (PropertyWithIndexer), "Item.set", new Type[] { typeof (Type) }, messageCode: "IL2067")]
			[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), "RequiresNonPublicConstructors", new Type[] { typeof (Type) }, messageCode: "IL2072")]
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
					[ExpectedWarning ("IL2063", "PropertyWithIndexer.Item.get")]
					get => Property_Field[index];
					set => Property_Field[index] = value;
				}
			}
		}

		private static void RequirePublicParameterlessConstructor (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type type)
		{
		}

		private static void RequirePublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type type)
		{
		}

		private static void RequireNonPublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
		{
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

		private static Type GetUnkownType ()
		{
			return null;
		}

		private static void RequireNothing (Type type)
		{
		}
	}
}