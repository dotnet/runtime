// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[SetupLinkerAttributeDefinitionsFile ("XmlAnnotations.xml")]
	[LogContains ("warning IL2021: Could not parse argument 'NonValidArgument' specified in", true)]
	class XmlAnnotations
	{
		public static void Main ()
		{
			var instance = new XmlAnnotations ();

			instance.ReadFromInstanceField ();
			instance.TwoAnnotatedParameters (typeof (TestType), typeof (TestType));
			instance.SpacesBetweenParametersWrongArgument (typeof (TestType), true);
			instance.GenericMethod<String> ("nonUsed", typeof (TestType));
			instance.ReturnConstructorsFailure (null);
			instance.ReadFromInstanceProperty ();

			var nestedinstance = new NestedType ();

			nestedinstance.ReadFromInstanceField ();
		}

		Type _typeWithDefaultConstructor;

		Type PropertyWithDefaultConstructor { get; set; }

		[UnrecognizedReflectionAccessPattern (typeof (XmlAnnotations), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (XmlAnnotations), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void ReadFromInstanceField ()
		{
			RequireDefaultConstructor (_typeWithDefaultConstructor);
			RequirePublicConstructors (_typeWithDefaultConstructor);
			RequireNonPublicConstructors (_typeWithDefaultConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (XmlAnnotations), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		private void TwoAnnotatedParameters (
			Type type,
			Type type2)
		{
			RequireDefaultConstructor (type);
			RequireDefaultConstructor (type2);
			RequirePublicConstructors (type);
			RequirePublicConstructors (type2);
		}

		[UnrecognizedReflectionAccessPattern (typeof (XmlAnnotations), nameof (RequireDefaultConstructor), new Type[] { typeof (Type) })]
		private void SpacesBetweenParametersWrongArgument (
			Type type,
			bool nonused)
		{
			RequireDefaultConstructor (type);
		}

		[RecognizedReflectionAccessPattern]
		private void GenericMethod<T> (
			T input,
			Type type)
		{
			RequireDefaultConstructor (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (XmlAnnotations), nameof (ReturnConstructorsFailure), new Type[] { typeof (Type) },
			returnType: typeof (Type))]
		private Type ReturnConstructorsFailure (
			Type defaultConstructorType)
		{
			return defaultConstructorType;
		}

		[UnrecognizedReflectionAccessPattern (typeof (XmlAnnotations), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (XmlAnnotations), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void ReadFromInstanceProperty ()
		{
			RequireDefaultConstructor (PropertyWithDefaultConstructor);
			RequirePublicConstructors (PropertyWithDefaultConstructor);
			RequireNonPublicConstructors (PropertyWithDefaultConstructor);
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

		class TestType { }

		class NestedType
		{
			Type _typeWithDefaultConstructor;

			[UnrecognizedReflectionAccessPattern (typeof (NestedType), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
			[UnrecognizedReflectionAccessPattern (typeof (NestedType), nameof (RequireConstructors), new Type[] { typeof (Type) })]
			[RecognizedReflectionAccessPattern]
			public void ReadFromInstanceField ()
			{
				RequireDefaultConstructor (_typeWithDefaultConstructor);
				RequirePublicConstructors (_typeWithDefaultConstructor);
				RequireConstructors (_typeWithDefaultConstructor);
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

			private static void RequireConstructors (
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
			{
			}
		}
	}
}
