// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

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

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void ReadFromInstanceProperty ()
		{
			RequirePublicParameterlessConstructor (PropertyWithPublicConstructor);
			RequirePublicConstructors (PropertyWithPublicConstructor);
			RequireNonPublicConstructors (PropertyWithPublicConstructor);
			RequireNothing (PropertyWithPublicConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void ReadFromStaticProperty ()
		{
			RequirePublicParameterlessConstructor (StaticPropertyWithPublicConstructor);
			RequirePublicConstructors (StaticPropertyWithPublicConstructor);
			RequireNonPublicConstructors (StaticPropertyWithPublicConstructor);
			RequireNothing (StaticPropertyWithPublicConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (PropertyWithPublicConstructor), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (PropertyWithPublicConstructor), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (PropertyWithPublicConstructor), new Type[] { typeof (Type) })]
		private void WriteToInstanceProperty ()
		{
			PropertyWithPublicConstructor = GetTypeWithPublicParameterlessConstructor ();
			PropertyWithPublicConstructor = GetTypeWithPublicConstructors ();
			PropertyWithPublicConstructor = GetTypeWithNonPublicConstructors ();
			PropertyWithPublicConstructor = GetUnkownType ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (StaticPropertyWithPublicConstructor), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (StaticPropertyWithPublicConstructor), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (StaticPropertyWithPublicConstructor), new Type[] { typeof (Type) })]
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

			[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (_fieldWithPublicConstructors))]
			[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			set {
				_fieldWithPublicConstructors = value;
			}
		}

		Type PropertyNonPublicConstructorsWithExplicitAccessors {
			[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "get_" + nameof (PropertyNonPublicConstructorsWithExplicitAccessors),
				new Type[] { }, returnType: typeof (Type))]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			get {
				return _fieldWithPublicConstructors;
			}

			[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (_fieldWithPublicConstructors))]
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
		}

		class TestAutomaticPropagationType
		{
			// Fully implicit property should work
			[UnrecognizedReflectionAccessPattern (typeof (TestAutomaticPropagationType), "set_" + nameof (ImplicitProperty), new Type[] { typeof (Type) })]
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
			[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
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
				[UnrecognizedReflectionAccessPattern (typeof (TestAutomaticPropagationType), "get_" + nameof (PropertyWithSimpleGetter), new Type[] { }, returnType: typeof (Type))]
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

			[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
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

			[LogContains ("Could not find a unique backing field for property 'System.Type Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow/TestAutomaticPropagationType::PropertyWithDifferentBackingFields()' " +
				"to propagate DynamicallyAccessedMembersAttribute. " +
				"The backing fields from getter 'System.Type Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow/TestAutomaticPropagationType::PropertyWithDifferentBackingFields_GetterField' " +
				"and setter 'System.Type Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow/TestAutomaticPropagationType::PropertyWithDifferentBackingFields_SetterField' are not the same.")]
			public void TestPropertyWithDifferentBackingFields ()
			{
				_ = PropertyWithDifferentBackingFields;
			}

			[CompilerGenerated]
			private Type PropertyWithDifferentBackingFields_GetterField;

			[CompilerGenerated]
			private Type PropertyWithDifferentBackingFields_SetterField;

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type PropertyWithDifferentBackingFields {
				get {
					return PropertyWithDifferentBackingFields_GetterField;
				}

				set {
					PropertyWithDifferentBackingFields_SetterField = value;
				}
			}

			[LogContains ("Trying to propagate DynamicallyAccessedMemberAttribute from property 'System.Type Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow/TestAutomaticPropagationType::PropertyWithExistingAttributes()' to its field 'System.Type Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow/TestAutomaticPropagationType::PropertyWithExistingAttributes_Field', but it already has such attribute.")]
			[LogContains ("Trying to propagate DynamicallyAccessedMemberAttribute from property 'System.Type Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow/TestAutomaticPropagationType::PropertyWithExistingAttributes()' to its setter 'Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow.TestAutomaticPropagationType.set_PropertyWithExistingAttributes(Type)', but it already has such attribute on the 'value' parameter.")]
			[LogContains ("Trying to propagate DynamicallyAccessedMemberAttribute from property 'System.Type Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow/TestAutomaticPropagationType::PropertyWithExistingAttributes()' to its getter 'Mono.Linker.Tests.Cases.DataFlow.PropertyDataFlow.TestAutomaticPropagationType.get_PropertyWithExistingAttributes()', but it already has such attribute on the return value.")]
			public void TestPropertyWithExistingAttributes ()
			{
				_ = PropertyWithExistingAttributes;
				PropertyWithExistingAttributes = null;
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			[CompilerGenerated]
			Type PropertyWithExistingAttributes_Field;

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type PropertyWithExistingAttributes {
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
				get { return PropertyWithExistingAttributes_Field; }
				[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
				set { PropertyWithExistingAttributes_Field = value; }
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