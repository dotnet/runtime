using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[IgnoreTestCase ("Ignore in NativeAOT, see https://github.com/dotnet/runtime/issues/82447", IgnoredBy = Tool.NativeAot)]
	[SkipKeptItemsValidation]
	[SetupLinkerArgument ("--warnaserror")]
	[SetupLinkerArgument ("--nowarn", "IL2026,IL2075")]
	[LogDoesNotContain ("IL2026")]
	[LogDoesNotContain ("IL2075")]
	public class NoWarnRegardlessOfWarnAsError
	{
		public static void Main ()
		{
			GetMethod ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static string type;

		static void GetMethod ()
		{
			_ = Type.GetType (type).GetMethod ("Foo");
		}
	}
}
