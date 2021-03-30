// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[SetupLinkAttributesFile ("XmlAnnotations.xml")]
	[ExpectedWarning ("IL2031", "Attribute type 'System.DoesNotExistAttribute' could not be found", FileName = "XmlAnnotations.xml")]
	[LogDoesNotContain ("IL2067: Mono.Linker.Tests.Cases.DataFlow.XmlAnnotations.ReadFromInstanceField():*", true)]
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

		Type _typeWithPublicParameterlessConstructor;

		Type PropertyWithPublicParameterlessConstructor { get; set; }

		[UnrecognizedReflectionAccessPattern (typeof (XmlAnnotations), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (XmlAnnotations), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		[RecognizedReflectionAccessPattern]
		private void ReadFromInstanceField ()
		{
			RequirePublicParameterlessConstructor (_typeWithPublicParameterlessConstructor);
			RequirePublicConstructors (_typeWithPublicParameterlessConstructor);
			RequireNonPublicConstructors (_typeWithPublicParameterlessConstructor);
		}

		[UnrecognizedReflectionAccessPattern (typeof (XmlAnnotations), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[RecognizedReflectionAccessPattern]
		private void TwoAnnotatedParameters (
			Type type,
			Type type2)
		{
			RequirePublicParameterlessConstructor (type);
			RequirePublicParameterlessConstructor (type2);
			RequirePublicConstructors (type);
			RequirePublicConstructors (type2);
		}

		[UnrecognizedReflectionAccessPattern (typeof (XmlAnnotations), nameof (RequirePublicParameterlessConstructor), new Type[] { typeof (Type) })]
		private void SpacesBetweenParametersWrongArgument (
			Type type,
			bool nonused)
		{
			RequirePublicParameterlessConstructor (type);
		}

		[RecognizedReflectionAccessPattern]
		private void GenericMethod<T> (
			T input,
			Type type)
		{
			RequirePublicParameterlessConstructor (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (XmlAnnotations), nameof (ReturnConstructorsFailure), new Type[] { typeof (Type) },
			returnType: typeof (Type))]
		private Type ReturnConstructorsFailure (
			Type publicParameterlessConstructorType)
		{
			return publicParameterlessConstructorType;
		}

		[UnrecognizedReflectionAccessPattern (typeof (XmlAnnotations), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (XmlAnnotations), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void ReadFromInstanceProperty ()
		{
			RequirePublicParameterlessConstructor (PropertyWithPublicParameterlessConstructor);
			RequirePublicConstructors (PropertyWithPublicParameterlessConstructor);
			RequireNonPublicConstructors (PropertyWithPublicParameterlessConstructor);
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

		class TestType { }

		class NestedType
		{
			Type _typeWithPublicParameterlessConstructor;

			[UnrecognizedReflectionAccessPattern (typeof (NestedType), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
			[UnrecognizedReflectionAccessPattern (typeof (NestedType), nameof (RequireConstructors), new Type[] { typeof (Type) })]
			[RecognizedReflectionAccessPattern]
			public void ReadFromInstanceField ()
			{
				RequirePublicParameterlessConstructor (_typeWithPublicParameterlessConstructor);
				RequirePublicConstructors (_typeWithPublicParameterlessConstructor);
				RequireConstructors (_typeWithPublicParameterlessConstructor);
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

			private static void RequireConstructors (
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
			{
			}
		}
	}
}
