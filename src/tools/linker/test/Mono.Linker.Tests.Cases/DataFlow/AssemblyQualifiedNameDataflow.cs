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
			TestNull ();
			TestMultipleValues ();
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
			"Type name strings used for dynamically accessing a type should be assembly qualified.",
			ProducedBy = ProducedBy.Trimmer)]
		static void TestUnqualifiedTypeNameWarns ()
		{
			RequirePublicConstructors ("System.Invalid.TypeName");
		}

		// https://github.com/dotnet/linker/issues/2528
		[ExpectedWarning ("IL2072", nameof (RequirePublicConstructors), ProducedBy = ProducedBy.Analyzer)]
		static void TestNull ()
		{
			Type type = null;
			RequirePublicConstructors (type.AssemblyQualifiedName); // Null should not warn - we know it's going to fail at runtime
		}

		[ExpectedWarning ("IL2072", nameof (RequirePublicConstructors), nameof (GetTypeWithNonPublicConstructors))]
		// https://github.com/dotnet/linker/issues/2273
		[ExpectedWarning ("IL2062", nameof (RequirePublicConstructors), ProducedBy = ProducedBy.Trimmer)]
		static void TestMultipleValues (int p = 0, object[] o = null)
		{
			Type type = p switch {
				0 => GetTypeWithPublicConstructors (),
				1 => GetTypeWithNonPublicConstructors (), // Should produce warning IL2072 due to mismatch annotation
				2 => null, // Should be ignored
				_ => (Type) o[0] // This creates an unknown value - should produce warning IL2062
			};

			RequirePublicConstructors (type.AssemblyQualifiedName);
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
