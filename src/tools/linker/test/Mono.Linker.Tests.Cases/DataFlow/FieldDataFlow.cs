// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.DefaultConstructor)]
		Type _typeWithDefaultConstructor;

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.DefaultConstructor)]
		static Type _staticTypeWithDefaultConstructor;

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) },
			"The field 'System.Type Mono.Linker.Tests.Cases.DataFlow.FieldDataFlow::_typeWithDefaultConstructor' " +
			"with dynamically accessed member kinds 'DefaultConstructor' is passed into " +
			"the parameter 'type' of method 'Mono.Linker.Tests.Cases.DataFlow.FieldDataFlow.RequirePublicConstructors(Type)' " +
			"which requires dynamically accessed member kinds 'PublicConstructors'. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicConstructors'.")]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void ReadFromInstanceField ()
		{
			RequireDefaultConstructor (_typeWithDefaultConstructor);
			RequirePublicConstructors (_typeWithDefaultConstructor);
			RequireNonPublicConstructors (_typeWithDefaultConstructor);
			RequireNothing (_typeWithDefaultConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (_typeWithDefaultConstructor))]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (_typeWithDefaultConstructor))]
		private void WriteToInstanceField ()
		{
			_typeWithDefaultConstructor = GetTypeWithDefaultConstructor ();
			_typeWithDefaultConstructor = GetTypeWithPublicConstructors ();
			_typeWithDefaultConstructor = GetTypeWithNonPublicConstructors ();
			_typeWithDefaultConstructor = GetUnkownType ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void ReadFromInstanceFieldOnADifferentClass ()
		{
			var store = new TypeStore ();

			RequireDefaultConstructor (store._typeWithDefaultConstructor);
			RequirePublicConstructors (store._typeWithDefaultConstructor);
			RequireNonPublicConstructors (store._typeWithDefaultConstructor);
			RequireNothing (store._typeWithDefaultConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (TypeStore), nameof (TypeStore._typeWithDefaultConstructor))]
		[UnrecognizedReflectionAccessPattern (typeof (TypeStore), nameof (TypeStore._typeWithDefaultConstructor))]
		private void WriteToInstanceFieldOnADifferentClass ()
		{
			var store = new TypeStore ();

			store._typeWithDefaultConstructor = GetTypeWithDefaultConstructor ();
			store._typeWithDefaultConstructor = GetTypeWithPublicConstructors ();
			store._typeWithDefaultConstructor = GetTypeWithNonPublicConstructors ();
			store._typeWithDefaultConstructor = GetUnkownType ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void ReadFromStaticField ()
		{
			RequireDefaultConstructor (_staticTypeWithDefaultConstructor);
			RequirePublicConstructors (_staticTypeWithDefaultConstructor);
			RequireNonPublicConstructors (_staticTypeWithDefaultConstructor);
			RequireNothing (_staticTypeWithDefaultConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (_staticTypeWithDefaultConstructor))]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (_staticTypeWithDefaultConstructor))]
		private void WriteToStaticField ()
		{
			_staticTypeWithDefaultConstructor = GetTypeWithDefaultConstructor ();
			_staticTypeWithDefaultConstructor = GetTypeWithPublicConstructors ();
			_staticTypeWithDefaultConstructor = GetTypeWithNonPublicConstructors ();
			_staticTypeWithDefaultConstructor = GetUnkownType ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void ReadFromStaticFieldOnADifferentClass ()
		{
			RequireDefaultConstructor (TypeStore._staticTypeWithDefaultConstructor);
			RequirePublicConstructors (TypeStore._staticTypeWithDefaultConstructor);
			RequireNonPublicConstructors (TypeStore._staticTypeWithDefaultConstructor);
			RequireNothing (TypeStore._staticTypeWithDefaultConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (TypeStore), nameof (TypeStore._staticTypeWithDefaultConstructor))]
		[UnrecognizedReflectionAccessPattern (typeof (TypeStore), nameof (TypeStore._staticTypeWithDefaultConstructor))]
		private void WriteToStaticFieldOnADifferentClass ()
		{
			TypeStore._staticTypeWithDefaultConstructor = GetTypeWithDefaultConstructor ();
			TypeStore._staticTypeWithDefaultConstructor = GetTypeWithPublicConstructors ();
			TypeStore._staticTypeWithDefaultConstructor = GetTypeWithNonPublicConstructors ();
			TypeStore._staticTypeWithDefaultConstructor = GetUnkownType ();
		}

		private static void RequireDefaultConstructor (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.DefaultConstructor)]
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

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.DefaultConstructor)]
		private static Type GetTypeWithDefaultConstructor ()
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

		class TypeStore
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.DefaultConstructor)]
			public Type _typeWithDefaultConstructor;

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.DefaultConstructor)]
			public static Type _staticTypeWithDefaultConstructor;
		}
	}
}
