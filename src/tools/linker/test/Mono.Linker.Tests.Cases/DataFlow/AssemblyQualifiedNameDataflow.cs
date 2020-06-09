using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the UnrecognizedReflectionAccessPattern attributes.
	[SkipKeptItemsValidation]
	class AssemblyQualifiedNameDataflow
	{
		static void Main ()
		{
			TestDefaultConstructor ();
			TestPublicConstructors ();
			TestConstructors ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (AssemblyQualifiedNameDataflow), nameof (RequirePublicConstructors), new Type[] { typeof (string) })]
		[UnrecognizedReflectionAccessPattern (typeof (AssemblyQualifiedNameDataflow), nameof (RequireNonPublicConstructors), new Type[] { typeof (string) })]
		static void TestDefaultConstructor ()
		{
			string type = GetTypeWithDefaultConstructor ().AssemblyQualifiedName;
			RequireDefaultConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (AssemblyQualifiedNameDataflow), nameof (RequireNonPublicConstructors), new Type[] { typeof (string) })]
		static void TestPublicConstructors ()
		{
			string type = GetTypeWithPublicConstructors ().AssemblyQualifiedName;
			RequireDefaultConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (AssemblyQualifiedNameDataflow), nameof (RequireDefaultConstructor), new Type[] { typeof (string) })]
		[UnrecognizedReflectionAccessPattern (typeof (AssemblyQualifiedNameDataflow), nameof (RequirePublicConstructors), new Type[] { typeof (string) })]
		static void TestConstructors ()
		{
			string type = GetTypeWithNonPublicConstructors ().AssemblyQualifiedName;
			RequireDefaultConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		private static void RequireDefaultConstructor (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.DefaultConstructor)]
			string type)
		{
		}

		private static void RequirePublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			string type)
		{
		}

		private static void RequireNonPublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			string type)
		{
		}

		private static void RequireNothing (string type)
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

	}
}
