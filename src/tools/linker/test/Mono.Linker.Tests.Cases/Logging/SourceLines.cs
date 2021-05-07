using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Logging
{
	[SkipKeptItemsValidation]
	[SetupCompileBefore ("FakeSystemAssembly.dll", new[] { "../PreserveDependencies/Dependencies/PreserveDependencyAttribute.cs" })]
	[SetupCompileArgument ("/debug:full")]
	[LogContains ("(33,4): Trim analysis warning IL2074")]
	[LogContains ("(34,4): Trim analysis warning IL2074")]
	[LogContains ("(38,3): Trim analysis warning IL2091")]
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
			type = GetUnknownType ();
			type = GetUnknownType ();
		}

		static IEnumerable<int> GenericMethodIteratorWithRequirement<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TOuterMethod> ()
		{
			// Since this is iterator it will turn into a subclass with generic T
			// but this T doesn't inherit the DAM annotation.
			// So calling the LocationFunction which requires the DAM on T will generate a warning
			// but that warning comes from compiler generated code - there's no user code doing that.
			LocalFunction ();
			yield return 1;

			// The generator code for LocalFunction inherits the DAM on the T
			static void LocalFunction () => type = typeof (TOuterMethod);
		}
	}
}
