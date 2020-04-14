// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

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
			_ = instance.PropertyConstructorsWithExplicitAccessors;
			instance.PropertyDefaultConstructorWithExplicitAccessors = null;
			instance.PropertyPublicConstructorsWithExplicitAccessors = null;
			instance.PropertyConstructorsWithExplicitAccessors = null;
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
		Type PropertyWithPublicConstructor { get; set; }

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
		static Type StaticPropertyWithPublicConstructor { get; set; }

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		private void ReadFromInstanceProperty ()
		{
			RequireDefaultConstructor (PropertyWithPublicConstructor);
			RequirePublicConstructors (PropertyWithPublicConstructor);
			RequireConstructors (PropertyWithPublicConstructor);
			RequireNothing (PropertyWithPublicConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		private void ReadFromStaticProperty ()
		{
			RequireDefaultConstructor (StaticPropertyWithPublicConstructor);
			RequirePublicConstructors (StaticPropertyWithPublicConstructor);
			RequireConstructors (StaticPropertyWithPublicConstructor);
			RequireNothing (StaticPropertyWithPublicConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (PropertyWithPublicConstructor), new Type [] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (PropertyWithPublicConstructor), new Type [] { typeof (Type) })]
		private void WriteToInstanceProperty ()
		{
			PropertyWithPublicConstructor = GetTypeWithDefaultConstructor ();
			PropertyWithPublicConstructor = GetTypeWithPublicConstructors ();
			PropertyWithPublicConstructor = GetTypeWithConstructors ();
			PropertyWithPublicConstructor = GetUnkownType ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (StaticPropertyWithPublicConstructor), new Type [] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "set_" + nameof (StaticPropertyWithPublicConstructor), new Type [] { typeof (Type) })]
		private void WriteToStaticProperty ()
		{
			StaticPropertyWithPublicConstructor = GetTypeWithDefaultConstructor ();
			StaticPropertyWithPublicConstructor = GetTypeWithPublicConstructors ();
			StaticPropertyWithPublicConstructor = GetTypeWithConstructors ();
			StaticPropertyWithPublicConstructor = GetUnkownType ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
		Type _fieldWithPublicConstructors;

		Type PropertyPublicConstructorsWithExplicitAccessors {
			[RecognizedReflectionAccessPattern]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
			get {
				return _fieldWithPublicConstructors;
			}

			[RecognizedReflectionAccessPattern]
			[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
			set {
				_fieldWithPublicConstructors = value;
			}
		}

		Type PropertyDefaultConstructorWithExplicitAccessors {
			[RecognizedReflectionAccessPattern]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
			get {
				return _fieldWithPublicConstructors;
			}

			[UnrecognizedReflectionAccessPattern(typeof (PropertyDataFlow), "set_" + nameof (PropertyDefaultConstructorWithExplicitAccessors), new Type [] { typeof (Type) })]
			[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
			set {
				_fieldWithPublicConstructors = value;
			}
		}

		Type PropertyConstructorsWithExplicitAccessors {
			[UnrecognizedReflectionAccessPattern (typeof (PropertyDataFlow), "get_" + nameof (PropertyConstructorsWithExplicitAccessors), new Type [] { })]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Constructors)]
			get {
				return _fieldWithPublicConstructors;
			}

			[RecognizedReflectionAccessPattern]
			[param: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Constructors)]
			set {
				_fieldWithPublicConstructors = value;
			}
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
	}
}