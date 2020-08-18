using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[SkipKeptItemsValidation]
	[SkipRemainingErrorsValidation]
	[SetupLinkerArgument ("--warnaserror")]
	[SetupLinkerArgument ("--nowarn", "IL2006")]
	[LogDoesNotContain ("IL2006")]
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