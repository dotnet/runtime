using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[KeptMember(".ctor()")]
	public class MethodParametersDataFlow
	{
		public static void Main ()
		{
			var instance = new MethodParametersDataFlow ();

			// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
			//   - so the main validation is done by the UnrecognizedReflectionAccessPattern attributes.
			// The test doesn't really validate that things are marked correctly, so Kept attributes are here to make it work mostly.

			DefaultConstructorParameter (typeof (TestType));
			PublicConstructorsParameter (typeof (TestType));
			ConstructorsParameter (typeof (TestType));
			instance.InstanceMethod (typeof (TestType));
			instance.TwoAnnotatedParameters (typeof (TestType), typeof (TestType));
			instance.TwoAnnotatedParametersIntoOneValue(typeof (TestType), typeof (TestType));
			instance.NoAnnotation (typeof (TestType));
			instance.UnknownValue ();
			instance.AnnotatedValueToUnAnnotatedParameter (typeof (TestType));
			instance.UnknownValueToUnAnnotatedParameter ();
			instance.UnknownValueToUnAnnotatedParameterOnInterestingMethod ();
		}

		[Kept]
		// Validate the error message when annotated parameter is passed to another annotated parameter
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) },
			"The parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.MethodParametersDataFlow::DefaultConstructorParameter(System.Type)' " +
			"with dynamically accessed member kinds 'DefaultConstructor' is passed into the parameter 'type' " +
			"of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.MethodParametersDataFlow::RequirePublicConstructors(System.Type)' " +
			"which requires dynamically accessed member kinds `PublicConstructors`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicConstructors'.")]
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		private static void DefaultConstructorParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
			RequireDefaultConstructor (type);
			RequirePublicConstructors (type);
			RequireConstructors (type);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		private static void PublicConstructorsParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicConstructors)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
			RequireDefaultConstructor (type);
			RequirePublicConstructors (type);
			RequireConstructors (type);
		}

		[RecognizedReflectionAccessPattern]
		[Kept]
		private static void ConstructorsParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Constructors)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
			RequireDefaultConstructor (type);
			RequirePublicConstructors (type);
			RequireConstructors (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) })]
		[Kept]
		private void InstanceMethod (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
			RequireDefaultConstructor (type);
			RequirePublicConstructors (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) })]
		[Kept]
		private void TwoAnnotatedParameters (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicConstructors)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type2)
		{
			RequireDefaultConstructor (type);
			RequireDefaultConstructor (type2);
			RequirePublicConstructors (type);
			RequirePublicConstructors (type2);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) })]
		[Kept]
		private void TwoAnnotatedParametersIntoOneValue (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicConstructors)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type2)
		{
			Type t = type == null ? type : type2;
			RequireDefaultConstructor (t);
			RequirePublicConstructors (t);
		}

		// Validate the error message for the case of unannotated method return value passed to an annotated parameter.
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequireDefaultConstructor), new Type [] { typeof (Type) },
			"The parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.MethodParametersDataFlow::NoAnnotation(System.Type)' " +
			"with dynamically accessed member kinds 'None' is passed into " +
			"the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.MethodParametersDataFlow::RequireDefaultConstructor(System.Type)' " +
			"which requires dynamically accessed member kinds `DefaultConstructor`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'DefaultConstructor'.")]
		[Kept]
		private void NoAnnotation (Type type)
		{
			RequireDefaultConstructor (type);
		}

		// Validate error message when untracable value is passed to an annotated parameter.
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequireDefaultConstructor), new Type [] { typeof (Type) },
			"A value from unknown source is passed " +
			"into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.MethodParametersDataFlow::RequireDefaultConstructor(System.Type)' " +
			"which requires dynamically accessed member kinds `DefaultConstructor`. " +
			"It's not possible to guarantee that these requirements are met by the application.")]
		[Kept]
		private void UnknownValue ()
		{
			var array = new object [1];
			array [0] = this.GetType ();
			RequireDefaultConstructor ((Type)array [0]);
		}

		[RecognizedReflectionAccessPattern]
		[Kept]
		private void AnnotatedValueToUnAnnotatedParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
			RequireNothing (type);
		}

		[RecognizedReflectionAccessPattern]
		[Kept]
		private void UnknownValueToUnAnnotatedParameter ()
		{
			RequireNothing (this.GetType());
		}

		[RecognizedReflectionAccessPattern]
		[Kept]
		private void UnknownValueToUnAnnotatedParameterOnInterestingMethod ()
		{
			RequireDefaultConstructorAndNothing (typeof (TestType), this.GetType ());
		}

		[Kept]
		private static void RequireDefaultConstructor (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		private static void RequirePublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicConstructors)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		private static void RequireConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Constructors)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		private static void RequireNothing (Type type)
		{
		}

		[Kept]
		private static void RequireDefaultConstructorAndNothing (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type,
			Type type2)
		{
		}

		[Kept]
		class TestType
		{
			[Kept]
			public TestType() { }
			[Kept]
			public TestType (int arg) { }
			[Kept]
			private TestType (int arg1, int arg2) { }
		}
	}
}
