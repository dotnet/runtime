using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Logging
{
	[SkipKeptItemsValidation]
	[SetupCompileBefore ("FakeSystemAssembly.dll", new[] { "../PreserveDependencies/Dependencies/PreserveDependencyAttribute.cs" })]
	[SetupCompileArgument ("/debug:full")]
	[LogContains ("(37,4): Trim analysis warning IL2006: Mono.Linker.Tests.Cases.Logging.SourceLines.UnrecognizedReflectionPattern(): " +
		"The return value of method 'Mono.Linker.Tests.Cases.Logging.SourceLines.GetUnknownType()' " +
		"with dynamically accessed member kinds 'None' is passed into the field 'System.Type Mono.Linker.Tests.Cases.Logging.SourceLines::type' which requires dynamically " +
		"accessed member kinds 'PublicConstructors'. To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicConstructors'.")]
	[LogContains ("(38,4): Trim analysis warning IL2006: Mono.Linker.Tests.Cases.Logging.SourceLines.UnrecognizedReflectionPattern(): " +
		"The return value of method 'Mono.Linker.Tests.Cases.Logging.SourceLines.GetUnknownType()' " +
		"with dynamically accessed member kinds 'None' is passed into the field 'System.Type Mono.Linker.Tests.Cases.Logging.SourceLines::type' which requires dynamically " +
		"accessed member kinds 'PublicConstructors'. To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicConstructors'.")]
	public class SourceLines
	{
		public static void Main ()
		{
			UnrecognizedReflectionPattern ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		private static Type type;

		private static Type GetUnknownType ()
		{
			return typeof (SourceLines);
		}

		private static void UnrecognizedReflectionPattern ()
		{
			type = GetUnknownType ();
			type = GetUnknownType ();
		}
	}
}
