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
	[LogContains ("(37,4): Trim analysis warning IL2074: Mono.Linker.Tests.Cases.Logging.SourceLines.UnrecognizedReflectionPattern(): " +
		"The requirements declared via the 'DynamicallyAccessedMembersAttribute' on the return value of method 'Mono.Linker.Tests.Cases.Logging.SourceLines.GetUnknownType()' " +
		"don't match those on the field 'System.Type Mono.Linker.Tests.Cases.Logging.SourceLines::type'. " +
		"The source value must declare at least the same requirements as those declared on the target location it's assigned to ")]
	[LogContains ("(38,4): Trim analysis warning IL2074: Mono.Linker.Tests.Cases.Logging.SourceLines.UnrecognizedReflectionPattern(): " +
		"The requirements declared via the 'DynamicallyAccessedMembersAttribute' on the return value of method 'Mono.Linker.Tests.Cases.Logging.SourceLines.GetUnknownType()' " +
		"don't match those on the field 'System.Type Mono.Linker.Tests.Cases.Logging.SourceLines::type'. " +
		"The source value must declare at least the same requirements as those declared on the target location it's assigned to ")]
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
