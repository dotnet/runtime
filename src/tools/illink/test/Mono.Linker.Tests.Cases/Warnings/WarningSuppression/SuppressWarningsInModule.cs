using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

[module: UnconditionalSuppressMessage ("Test", "IL2072:Suppress unrecognized reflection pattern warnings in this module")]
[module: UnconditionalSuppressMessage ("Test", "IL2026:Test that specifying an invalid scope will result in a global suppression being ignored.",
	Scope = "invalidScope", Target = "Non-existent target")]

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
#if !NET
	[Mono.Linker.Tests.Cases.Expectations.Metadata.Reference ("System.Core.dll")]
#endif
	[SkipKeptItemsValidation]
	[LogDoesNotContain ("TriggerUnrecognizedPattern()")]
	[LogContains ("warning IL2108:.*Invalid scope 'invalidScope' used in 'UnconditionalSuppressMessageAttribute'", regexMatch: true)]
	public class SuppressWarningsInModule
	{
		[ExpectedWarning ("IL2026", "TriggerUnrecognizedPattern()")]
		public static void Main ()
		{
			Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		[RequiresUnreferencedCode ("Test that the global unconditional suppression will not work when using an invalid scope.")]
		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (SuppressWarningsInModule);
		}
	}
}
