using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: UnconditionalSuppressMessage ("Test", "IL2072:Suppress unrecognized reflection pattern warnings in this assembly")]

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
#if !NETCOREAPP
	[Reference ("System.Core.dll")]
#endif
	[SkipKeptItemsValidation]
	[LogDoesNotContain ("TriggerUnrecognizedPattern()")]
	public class SuppressWarningsInAssembly
	{
		public static void Main ()
		{
			Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (SuppressWarningsInAssembly);
		}
	}
}
