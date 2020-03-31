using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[KeptMember (".ctor()")]
	public class FieldDataFlow
	{
		public static void Main ()
		{
			var instance = new FieldDataFlow ();

			// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
			//   - so the main validation is done by the UnrecognizedReflectionAccessPattern attributes.
			// The test doesn't really validate that things are marked correctly, so Kept attributes are here to make it work mostly.

			instance.ReadFromInstanceField ();
			instance.WriteToInstanceField ();

			instance.ReadFromStaticField ();
			instance.WriteToStaticField ();

			instance.ReadFromInstanceFieldOnADifferentClass ();
			instance.WriteToInstanceFieldOnADifferentClass ();

			instance.ReadFromStaticFieldOnADifferentClass ();
			instance.WriteToStaticFieldOnADifferentClass ();
		}

		[Kept]
		[KeptAttributeAttribute(typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
		Type _typeWithDefaultConstructor;

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
		static Type _staticTypeWithDefaultConstructor;

		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) },
			"The field 'System.Type Mono.Linker.Tests.Cases.DataFlow.FieldDataFlow::_typeWithDefaultConstructor' " +
			"with dynamically accessed member kinds 'DefaultConstructor' is passed into " +
			"the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.FieldDataFlow::RequirePublicConstructors(System.Type)' " +
			"which requires dynamically accessed member kinds `PublicConstructors`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicConstructors'.")]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		private void ReadFromInstanceField ()
		{
			RequireDefaultConstructor (_typeWithDefaultConstructor);
			RequirePublicConstructors (_typeWithDefaultConstructor);
			RequireConstructors (_typeWithDefaultConstructor);
			RequireNothing (_typeWithDefaultConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (WriteToInstanceField), new Type [] { })]
		[Kept]
		private void WriteToInstanceField ()
		{
			_typeWithDefaultConstructor = GetTypeWithDefaultConstructor ();
			_typeWithDefaultConstructor = GetTypeWithPublicConstructors ();
			_typeWithDefaultConstructor = GetTypeWithConstructors ();
			_typeWithDefaultConstructor = GetUnkownType ();
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		private void ReadFromInstanceFieldOnADifferentClass ()
		{
			var store = new TypeStore ();

			RequireDefaultConstructor (store._typeWithDefaultConstructor);
			RequirePublicConstructors (store._typeWithDefaultConstructor);
			RequireConstructors (store._typeWithDefaultConstructor);
			RequireNothing (store._typeWithDefaultConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (WriteToInstanceFieldOnADifferentClass), new Type [] { })]
		[Kept]
		private void WriteToInstanceFieldOnADifferentClass ()
		{
			var store = new TypeStore ();

			store._typeWithDefaultConstructor = GetTypeWithDefaultConstructor ();
			store._typeWithDefaultConstructor = GetTypeWithPublicConstructors ();
			store._typeWithDefaultConstructor = GetTypeWithConstructors ();
			store._typeWithDefaultConstructor = GetUnkownType ();
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		private void ReadFromStaticField ()
		{
			RequireDefaultConstructor (_staticTypeWithDefaultConstructor);
			RequirePublicConstructors (_staticTypeWithDefaultConstructor);
			RequireConstructors (_staticTypeWithDefaultConstructor);
			RequireNothing (_staticTypeWithDefaultConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (WriteToStaticField), new Type [] { })]
		[Kept]
		private void WriteToStaticField ()
		{
			_staticTypeWithDefaultConstructor = GetTypeWithDefaultConstructor ();
			_staticTypeWithDefaultConstructor = GetTypeWithPublicConstructors ();
			_staticTypeWithDefaultConstructor = GetTypeWithConstructors ();
			_staticTypeWithDefaultConstructor = GetUnkownType ();
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		private void ReadFromStaticFieldOnADifferentClass ()
		{
			RequireDefaultConstructor (TypeStore._staticTypeWithDefaultConstructor);
			RequirePublicConstructors (TypeStore._staticTypeWithDefaultConstructor);
			RequireConstructors (TypeStore._staticTypeWithDefaultConstructor);
			RequireNothing (TypeStore._staticTypeWithDefaultConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (WriteToStaticFieldOnADifferentClass), new Type [] { })]
		[Kept]
		private void WriteToStaticFieldOnADifferentClass ()
		{
			TypeStore._staticTypeWithDefaultConstructor = GetTypeWithDefaultConstructor ();
			TypeStore._staticTypeWithDefaultConstructor = GetTypeWithPublicConstructors ();
			TypeStore._staticTypeWithDefaultConstructor = GetTypeWithConstructors ();
			TypeStore._staticTypeWithDefaultConstructor = GetUnkownType ();
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
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
		[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		private static Type GetTypeWithDefaultConstructor ()
		{
			return null;
		}

		[Kept]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
		[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		private static Type GetTypeWithPublicConstructors ()
		{
			return null;
		}

		[Kept]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Constructors)]
		[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		private static Type GetTypeWithConstructors ()
		{
			return null;
		}

		[Kept]
		private static Type GetUnkownType ()
		{
			return null;
		}

		[Kept]
		private static void RequireNothing (Type type)
		{
		}

		[Kept]
		[KeptMember(".ctor()")]
		class TypeStore
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
			public Type _typeWithDefaultConstructor;

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
			public static Type _staticTypeWithDefaultConstructor;
		}
	}
}
