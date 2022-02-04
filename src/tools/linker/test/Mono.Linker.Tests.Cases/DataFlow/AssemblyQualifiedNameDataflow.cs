using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the ExpectedWarning attributes.
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class AssemblyQualifiedNameDataflow
	{
		static void Main ()
		{
			TestPublicParameterlessConstructor ();
			TestPublicConstructors ();
			TestConstructors ();
			TestUnqualifiedTypeNameWarns ();
		}

		[ExpectedWarning ("IL2072", nameof (RequirePublicConstructors))]
		[ExpectedWarning ("IL2072", nameof (RequireNonPublicConstructors))]
		static void TestPublicParameterlessConstructor ()
		{
			string type = GetTypeWithPublicParameterlessConstructor ().AssemblyQualifiedName;
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[ExpectedWarning ("IL2072", nameof (RequireNonPublicConstructors))]
		static void TestPublicConstructors ()
		{
			string type = GetTypeWithPublicConstructors ().AssemblyQualifiedName;
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[ExpectedWarning ("IL2072", nameof (RequirePublicParameterlessConstructor))]
		[ExpectedWarning ("IL2072", nameof (RequirePublicConstructors))]
		static void TestConstructors ()
		{
			string type = GetTypeWithNonPublicConstructors ().AssemblyQualifiedName;
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[ExpectedWarning ("IL2105",
			"Type 'System.Invalid.TypeName' was not found in the caller assembly nor in the base library. " +
			"Type name strings used for dynamically accessing a type should be assembly qualified.")]
		static void TestUnqualifiedTypeNameWarns ()
		{
			RequirePublicConstructors ("System.Invalid.TypeName");
		}

		private static void RequirePublicParameterlessConstructor (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
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

	}
}
