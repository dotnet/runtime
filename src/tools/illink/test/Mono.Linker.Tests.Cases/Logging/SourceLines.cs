using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Logging
{
	[SkipKeptItemsValidation]
	[SetupCompileArgument ("/debug:full")]
	[ExpectedNoWarnings]
	[ExpectedWarning ("IL2074", FileName = "", SourceLine = 39, SourceColumn = 4)]
	[ExpectedWarning ("IL2074", FileName = "", SourceLine = 38, SourceColumn = 4)]
	[ExpectedWarning ("IL2089", FileName = "", SourceLine = 50, SourceColumn = 36)]
	public class SourceLines
	{
		public static void Main ()
		{
			UnrecognizedReflectionPattern ();
			GenericMethodIteratorWithRequirement<SourceLines> ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		static Type type;

		static Type GetUnknownType ()
		{
			return typeof (SourceLines);
		}

		// Analyzer test infrastructure doesn't support ExpectedWarning at the top-level.
		// This is OK because the test is meant to validate that the linker infrastructure produces the right line numbers,
		// and we have separate tests to check the line number of analyzer warnings.
		[ExpectedWarning ("IL2074", nameof (SourceLines) + "." + nameof (type), nameof (SourceLines) + "." + nameof (GetUnknownType) + "()", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL2074", nameof (SourceLines) + "." + nameof (type), nameof (SourceLines) + "." + nameof (GetUnknownType) + "()", ProducedBy = ProducedBy.Analyzer)]
		static void UnrecognizedReflectionPattern ()
		{
			type = GetUnknownType (); // IL2074
			type = GetUnknownType (); // IL2074
		}

		[ExpectedWarning ("IL2091", "LocalFunction()", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL2089", nameof (SourceLines) + "." + nameof (type), "TOuterMethod", ProducedBy = ProducedBy.Analyzer)]
		static IEnumerable<int> GenericMethodIteratorWithRequirement<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TOuterMethod> ()
		{
			LocalFunction ();
			yield return 1;

			// The generator code for LocalFunction inherits the DAM on the T
			static void LocalFunction () => type = typeof (TOuterMethod); // IL2089 - TOuterMethod is PublicMethods, but type is PublicConstructors
		}
	}
}
