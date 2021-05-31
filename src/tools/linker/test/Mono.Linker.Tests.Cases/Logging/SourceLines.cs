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
	[ExpectedWarning ("IL2074", FileName = "", SourceLine = 34, SourceColumn = 4)]
	[ExpectedWarning ("IL2074", FileName = "", SourceLine = 35, SourceColumn = 4)]
	[ExpectedWarning ("IL2091", FileName = "", SourceLine = 44, SourceColumn = 4)]
	[ExpectedWarning ("IL2089", FileName = "", SourceLine = 48, SourceColumn = 36)]
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

		static void UnrecognizedReflectionPattern ()
		{
			type = GetUnknownType (); // IL2074
			type = GetUnknownType (); // IL2074
		}

		static IEnumerable<int> GenericMethodIteratorWithRequirement<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TOuterMethod> ()
		{
			// Since this is iterator it will turn into a subclass with generic T
			// but this T doesn't inherit the DAM annotation.
			// So calling the LocationFunction which requires the DAM on T will generate a warning
			// but that warning comes from compiler generated code - there's no user code doing that.
			LocalFunction (); // IL2091 - The issue with attributes not propagating to the iterator generics
			yield return 1;

			// The generator code for LocalFunction inherits the DAM on the T
			static void LocalFunction () => type = typeof (TOuterMethod); // IL2089 - TOuterMethod is PublicMethods, but type is PublicConstructors
		}
	}
}
