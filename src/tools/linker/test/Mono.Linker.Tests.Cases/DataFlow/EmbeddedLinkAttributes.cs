using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[SetupCompileResource ("Dependencies/EmbeddedLinkAttributes.xml", "ILLink.LinkAttributes.xml")]
	[IgnoreLinkAttributes (false)]
	[RemovedResourceInAssembly ("test.exe", "ILLink.LinkAttributes.xml")]
	class EmbeddedLinkAttributes
	{
		public static void Main ()
		{
			var instance = new EmbeddedLinkAttributes ();

			instance.ReadFromInstanceField ();
		}

		Type _typeWithDefaultConstructor;

		[UnrecognizedReflectionAccessPattern (typeof (EmbeddedLinkAttributes), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (EmbeddedLinkAttributes), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		[RecognizedReflectionAccessPattern]
		private void ReadFromInstanceField ()
		{
			RequireDefaultConstructor (_typeWithDefaultConstructor);
			RequirePublicConstructors (_typeWithDefaultConstructor);
			RequireNonPublicConstructors (_typeWithDefaultConstructor);
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
	}
}
