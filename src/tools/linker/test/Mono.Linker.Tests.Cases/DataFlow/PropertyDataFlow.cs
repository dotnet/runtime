// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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

			_ = instance.PropertyDefaultConstructorWithExplicitAccessors;
			_ = instance.PropertyPublicConstructorsWithExplicitAccessors;
			_ = instance.PropertyNonPublicConstructorsWithExplicitAccessors;
			instance.PropertyDefaultConstructorWithExplicitAccessors = null;
			instance.PropertyPublicConstructorsWithExplicitAccessors = null;
			instance.PropertyNonPublicConstructorsWithExplicitAccessors = null;
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		Type PropertyWithPublicConstructor { get; set; }

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		static Type StaticPropertyWithPublicConstructor { get; set; }

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void ReadFromInstanceProperty ()
		{
			RequireDefaultConstructor (PropertyWithPublicConstructor);
			RequirePublicConstructors (PropertyWithPublicConstructor);
			RequireNonPublicConstructors (PropertyWithPublicConstructor);
			RequireNothing (PropertyWithPublicConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void ReadFromStaticProperty ()
		{
			RequireDefaultConstructor (StaticPropertyWithPublicConstructor);
			RequirePublicConstructors (StaticPropertyWithPublicConstructor);
			RequireNonPublicConstructors (StaticPropertyWithPublicConstructor);
			RequireNothing (StaticPropertyWithPublicConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (PropertyWithPublicConstructor), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (PropertyWithPublicConstructor), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (PropertyWithPublicConstructor), new Type[] { typeof (Type) })]
		private void WriteToInstanceProperty ()
		{
			PropertyWithPublicConstructor = GetTypeWithDefaultConstructor ();
			PropertyWithPublicConstructor = GetTypeWithPublicConstructors ();
			PropertyWithPublicConstructor = GetTypeWithNonPublicConstructors ();
			PropertyWithPublicConstructor = GetUnkownType ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (StaticPropertyWithPublicConstructor), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (StaticPropertyWithPublicConstructor), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (StaticPropertyWithPublicConstructor), new Type[] { typeof (Type) })]
		private void WriteToStaticProperty ()
		{
			StaticPropertyWithPublicConstructor = GetTypeWithDefaultConstructor ();
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

		Type PropertyDefaultConstructorWithExplicitAccessors {
			[RecognizedReflectionAccessPattern]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.DefaultConstructor)]
			get {
				return _fieldWithPublicConstructors;
			}

			[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (_fieldWithPublicConstructors))]
			[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.DefaultConstructor)]
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
	}
}