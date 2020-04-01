using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[KeptMember (".ctor()")]
	public class PropertyDataFlow
	{
		public static void Main ()
		{
			var instance = new PropertyDataFlow ();

			// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
			//   - so the main validation is done by the UnrecognizedReflectionAccessPattern attributes.
			// The test doesn't really validate that things are marked correctly, so Kept attributes are here to make it work mostly.

			instance.ReadFromInstanceProperty ();
			instance.WriteToInstanceProperty ();

			instance.ReadFromStaticProperty ();
			instance.WriteToStaticProperty ();

			_ = instance.PropertyDefaultConstructorWithExplicitAccessors;
			_ = instance.PropertyPublicConstructorsWithExplicitAccessors;
			_ = instance.PropertyConstructorsWithExplicitAccessors;
			instance.PropertyDefaultConstructorWithExplicitAccessors = null;
			instance.PropertyPublicConstructorsWithExplicitAccessors = null;
			instance.PropertyConstructorsWithExplicitAccessors = null;
		}

		[Kept]
		[KeptBackingField]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
		Type PropertyWithPublicConstructor { [Kept] get; [Kept] set; }

		[Kept]
		[KeptBackingField]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
		static Type StaticPropertyWithPublicConstructor { [Kept] get; [Kept] set; }

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		[Kept]
		private void ReadFromInstanceProperty ()
		{
			RequireDefaultConstructor (PropertyWithPublicConstructor);
			RequirePublicConstructors (PropertyWithPublicConstructor);
			RequireConstructors (PropertyWithPublicConstructor);
			RequireNothing (PropertyWithPublicConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		[Kept]
		private void ReadFromStaticProperty ()
		{
			RequireDefaultConstructor (StaticPropertyWithPublicConstructor);
			RequirePublicConstructors (StaticPropertyWithPublicConstructor);
			RequireConstructors (StaticPropertyWithPublicConstructor);
			RequireNothing (StaticPropertyWithPublicConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (PropertyWithPublicConstructor), new Type [] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (PropertyWithPublicConstructor), new Type [] { typeof (Type) })]
		[Kept]
		private void WriteToInstanceProperty ()
		{
			PropertyWithPublicConstructor = GetTypeWithDefaultConstructor ();
			PropertyWithPublicConstructor = GetTypeWithPublicConstructors ();
			PropertyWithPublicConstructor = GetTypeWithConstructors ();
			PropertyWithPublicConstructor = GetUnkownType ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (StaticPropertyWithPublicConstructor), new Type [] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (StaticPropertyWithPublicConstructor), new Type [] { typeof (Type) })]
		[Kept]
		private void WriteToStaticProperty ()
		{
			StaticPropertyWithPublicConstructor = GetTypeWithDefaultConstructor ();
			StaticPropertyWithPublicConstructor = GetTypeWithPublicConstructors ();
			StaticPropertyWithPublicConstructor = GetTypeWithConstructors ();
			StaticPropertyWithPublicConstructor = GetUnkownType ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
		Type _fieldWithPublicConstructors;

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
		Type PropertyPublicConstructorsWithExplicitAccessors {
			[RecognizedReflectionAccessPattern]
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
			get {
				return _fieldWithPublicConstructors;
			}

			[RecognizedReflectionAccessPattern]
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
			set {
				_fieldWithPublicConstructors = value;
			}
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
		Type PropertyDefaultConstructorWithExplicitAccessors {
			[RecognizedReflectionAccessPattern]
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
			get {
				return _fieldWithPublicConstructors;
			}

			[UnrecognizedReflectionAccessPattern(typeof (PropertyDataFlow), "set_" + nameof (PropertyDefaultConstructorWithExplicitAccessors), new Type [] { typeof (Type) })]
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
			set {
				_fieldWithPublicConstructors = value;
			}
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Constructors)]
		Type PropertyConstructorsWithExplicitAccessors {
			[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "get_" + nameof (PropertyConstructorsWithExplicitAccessors), new Type [] { })]
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Constructors)]
			get {
				return _fieldWithPublicConstructors;
			}

			[RecognizedReflectionAccessPattern]
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Constructors)]
			set {
				_fieldWithPublicConstructors = value;
			}
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
	}
}