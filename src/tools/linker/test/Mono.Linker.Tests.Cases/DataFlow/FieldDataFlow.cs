// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the UnrecognizedReflectionAccessPattern attributes.
	[SkipKeptItemsValidation]
	public class FieldDataFlow
	{
		public static void Main ()
		{
			var instance = new FieldDataFlow ();

			instance.ReadFromInstanceField ();
			instance.WriteToInstanceField ();

			instance.ReadFromStaticField ();
			instance.WriteToStaticField ();

			instance.ReadFromInstanceFieldOnADifferentClass ();
			instance.WriteToInstanceFieldOnADifferentClass ();

			instance.ReadFromStaticFieldOnADifferentClass ();
			instance.WriteToStaticFieldOnADifferentClass ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
		Type _typeWithDefaultConstructor;

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
		static Type _staticTypeWithDefaultConstructor;

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
		private void WriteToInstanceField ()
		{
			_typeWithDefaultConstructor = GetTypeWithDefaultConstructor ();
			_typeWithDefaultConstructor = GetTypeWithPublicConstructors ();
			_typeWithDefaultConstructor = GetTypeWithConstructors ();
			_typeWithDefaultConstructor = GetUnkownType ();
		}

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
		private void WriteToInstanceFieldOnADifferentClass ()
		{
			var store = new TypeStore ();

			store._typeWithDefaultConstructor = GetTypeWithDefaultConstructor ();
			store._typeWithDefaultConstructor = GetTypeWithPublicConstructors ();
			store._typeWithDefaultConstructor = GetTypeWithConstructors ();
			store._typeWithDefaultConstructor = GetUnkownType ();
		}

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
		private void WriteToStaticField ()
		{
			_staticTypeWithDefaultConstructor = GetTypeWithDefaultConstructor ();
			_staticTypeWithDefaultConstructor = GetTypeWithPublicConstructors ();
			_staticTypeWithDefaultConstructor = GetTypeWithConstructors ();
			_staticTypeWithDefaultConstructor = GetUnkownType ();
		}

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
		private void WriteToStaticFieldOnADifferentClass ()
		{
			TypeStore._staticTypeWithDefaultConstructor = GetTypeWithDefaultConstructor ();
			TypeStore._staticTypeWithDefaultConstructor = GetTypeWithPublicConstructors ();
			TypeStore._staticTypeWithDefaultConstructor = GetTypeWithConstructors ();
			TypeStore._staticTypeWithDefaultConstructor = GetUnkownType ();
		}

		private static void RequireDefaultConstructor (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			Type type)
		{
		}

		private static void RequirePublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicConstructors)]
			Type type)
		{
		}

		private static void RequireConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Constructors)]
			Type type)
		{
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
		private static Type GetTypeWithDefaultConstructor ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
		private static Type GetTypeWithPublicConstructors ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Constructors)]
		private static Type GetTypeWithConstructors ()
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

		class TypeStore
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
			public Type _typeWithDefaultConstructor;

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
			public static Type _staticTypeWithDefaultConstructor;
		}
	}
}
