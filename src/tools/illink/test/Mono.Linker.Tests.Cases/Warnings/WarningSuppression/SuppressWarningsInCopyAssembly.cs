using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
#if !NETCOREAPP
	[Reference ("System.Core.dll")]
#endif
	[SetupLinkerAction ("copy", "test")]
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class SuppressWarningsInCopyAssembly
	{
		public static void Main ()
		{
			// Regression test for https://github.com/dotnet/runtime/issues/56252
			// where a compiler-generated method is marked before the source code with the suppression
			foreach (var type in GetTypeNames ())
				Console.WriteLine (type);
		}

		[UnconditionalSuppressMessage ("", "IL2026")]
		public static IEnumerable<string> GetTypeNames ()
		{
			foreach (var type in Assembly.GetCallingAssembly ().GetTypes ())
				yield return type.Name;
		}
	}
}
