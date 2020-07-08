using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
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
	}
}
