// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[SkipKeptItemsValidation]
	[SetupCompileResource ("EmbeddedLinkAttributes.xml", "ILLink.LinkAttributes.xml")]
	[IgnoreLinkAttributes (false)]
	[RemovedResourceInAssembly ("test.exe", "ILLink.LinkAttributes.xml")]
	class EmbeddedLinkAttributes
	{
		public static void Main ()
		{
			var instance = new EmbeddedLinkAttributes ();

			instance.ReadFromInstanceField ();
			instance.ReadFromInstanceField2 ();
		}

		Type _typeWithPublicParameterlessConstructor;

		[UnrecognizedReflectionAccessPattern (typeof (EmbeddedLinkAttributes), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (EmbeddedLinkAttributes), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		[RecognizedReflectionAccessPattern]
		private void ReadFromInstanceField ()
		{
			RequirePublicParameterlessConstructor (_typeWithPublicParameterlessConstructor);
			RequirePublicConstructors (_typeWithPublicParameterlessConstructor);
			RequireNonPublicConstructors (_typeWithPublicParameterlessConstructor);
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

		Type _typeWithPublicFields;

		[UnrecognizedReflectionAccessPattern (typeof (EmbeddedLinkAttributes), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[RecognizedReflectionAccessPattern]
		private void ReadFromInstanceField2 ()
		{
			RequirePublicConstructors (_typeWithPublicFields);
			RequirePublicFields (_typeWithPublicFields);
		}

		private static void RequirePublicFields (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
			Type type)
		{
		}
	}
}
