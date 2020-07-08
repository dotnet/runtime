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

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type _typeWithPublicParameterlessConstructor;

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		static Type _staticTypeWithPublicParameterlessConstructor;

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) },
			"The field 'System.Type Mono.Linker.Tests.Cases.DataFlow.FieldDataFlow::_typeWithPublicParameterlessConstructor' " +
			"with dynamically accessed member kinds 'PublicParameterlessConstructor' is passed into " +
			"the parameter 'type' of method 'Mono.Linker.Tests.Cases.DataFlow.FieldDataFlow.RequirePublicConstructors(Type)' " +
			"which requires dynamically accessed member kinds 'PublicConstructors'. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicConstructors'.")]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void ReadFromInstanceField ()
		{
			RequirePublicParameterlessConstructor (_typeWithPublicParameterlessConstructor);
			RequirePublicConstructors (_typeWithPublicParameterlessConstructor);
			RequireNonPublicConstructors (_typeWithPublicParameterlessConstructor);
			RequireNothing (_typeWithPublicParameterlessConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (_typeWithPublicParameterlessConstructor))]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (_typeWithPublicParameterlessConstructor))]
		private void WriteToInstanceField ()
		{
			_typeWithPublicParameterlessConstructor = GetTypeWithPublicParameterlessConstructor ();
			_typeWithPublicParameterlessConstructor = GetTypeWithPublicConstructors ();
			_typeWithPublicParameterlessConstructor = GetTypeWithNonPublicConstructors ();
			_typeWithPublicParameterlessConstructor = GetUnkownType ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void ReadFromInstanceFieldOnADifferentClass ()
		{
			var store = new TypeStore ();

			RequirePublicParameterlessConstructor (store._typeWithPublicParameterlessConstructor);
			RequirePublicConstructors (store._typeWithPublicParameterlessConstructor);
			RequireNonPublicConstructors (store._typeWithPublicParameterlessConstructor);
			RequireNothing (store._typeWithPublicParameterlessConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (TypeStore), nameof (TypeStore._typeWithPublicParameterlessConstructor))]
		[UnrecognizedReflectionAccessPattern (typeof (TypeStore), nameof (TypeStore._typeWithPublicParameterlessConstructor))]
		private void WriteToInstanceFieldOnADifferentClass ()
		{
			var store = new TypeStore ();

			store._typeWithPublicParameterlessConstructor = GetTypeWithPublicParameterlessConstructor ();
			store._typeWithPublicParameterlessConstructor = GetTypeWithPublicConstructors ();
			store._typeWithPublicParameterlessConstructor = GetTypeWithNonPublicConstructors ();
			store._typeWithPublicParameterlessConstructor = GetUnkownType ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void ReadFromStaticField ()
		{
			RequirePublicParameterlessConstructor (_staticTypeWithPublicParameterlessConstructor);
			RequirePublicConstructors (_staticTypeWithPublicParameterlessConstructor);
			RequireNonPublicConstructors (_staticTypeWithPublicParameterlessConstructor);
			RequireNothing (_staticTypeWithPublicParameterlessConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (_staticTypeWithPublicParameterlessConstructor))]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (_staticTypeWithPublicParameterlessConstructor))]
		private void WriteToStaticField ()
		{
			_staticTypeWithPublicParameterlessConstructor = GetTypeWithPublicParameterlessConstructor ();
			_staticTypeWithPublicParameterlessConstructor = GetTypeWithPublicConstructors ();
			_staticTypeWithPublicParameterlessConstructor = GetTypeWithNonPublicConstructors ();
			_staticTypeWithPublicParameterlessConstructor = GetUnkownType ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (FieldDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void ReadFromStaticFieldOnADifferentClass ()
		{
			RequirePublicParameterlessConstructor (TypeStore._staticTypeWithPublicParameterlessConstructor);
			RequirePublicConstructors (TypeStore._staticTypeWithPublicParameterlessConstructor);
			RequireNonPublicConstructors (TypeStore._staticTypeWithPublicParameterlessConstructor);
			RequireNothing (TypeStore._staticTypeWithPublicParameterlessConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (TypeStore), nameof (TypeStore._staticTypeWithPublicParameterlessConstructor))]
		[UnrecognizedReflectionAccessPattern (typeof (TypeStore), nameof (TypeStore._staticTypeWithPublicParameterlessConstructor))]
		private void WriteToStaticFieldOnADifferentClass ()
		{
			TypeStore._staticTypeWithPublicParameterlessConstructor = GetTypeWithPublicParameterlessConstructor ();
			TypeStore._staticTypeWithPublicParameterlessConstructor = GetTypeWithPublicConstructors ();
			TypeStore._staticTypeWithPublicParameterlessConstructor = GetTypeWithNonPublicConstructors ();
			TypeStore._staticTypeWithPublicParameterlessConstructor = GetUnkownType ();
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

		class TypeStore
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			public Type _typeWithPublicParameterlessConstructor;

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			public static Type _staticTypeWithPublicParameterlessConstructor;
		}
	}
}
