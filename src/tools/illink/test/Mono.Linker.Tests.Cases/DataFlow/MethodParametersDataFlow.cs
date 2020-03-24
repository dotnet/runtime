using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SetupCSharpCompilerToUse ("csc")]
	[KeptMember(".ctor()")]
	public class MethodParametersDataFlow
	{
		public static void Main ()
		{
			var instance = new MethodParametersDataFlow ();

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
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) })]
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

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequireDefaultConstructor), new Type [] { typeof (Type) })]
		[Kept]
		private void NoAnnotation (Type type)
		{
			RequireDefaultConstructor (this.GetType ());
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequireDefaultConstructor), new Type [] { typeof (Type) })]
		[Kept]
		private void UnknownValue ()
		{
			RequireDefaultConstructor (this.GetType ());
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
