using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[SkipKeptItemsValidation]
	[SetupLinkerArgument ("--verbose")]
	[SetupLinkerArgument ("--warnaserror")]
	[SetupLinkerArgument ("--warn", "0")]
	[LogDoesNotContain ("IL2067")]
	public class CanNotWarnAsErrorForDisabledVersion
	{
		public static void Main ()
		{
			GetMethod ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static string type;

		static void GetMethod ()
		{
			_ = Type.GetType (type).GetMethod ("Method");
		}
	}
}
