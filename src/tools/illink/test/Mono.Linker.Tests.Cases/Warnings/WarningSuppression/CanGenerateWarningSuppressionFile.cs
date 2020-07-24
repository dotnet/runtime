using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Warnings.WarningSuppression.Dependencies;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SetupLinkerCoreAction ("skip")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/TriggerWarnings_Lib.cs" })]
	[KeptAssembly ("library.dll")]
	[SetupLinkerAction ("link", "library.dll")]
	[SetupLinkerArgument ("--verbose")]
	[SetupLinkerArgument ("--generate-warning-suppressions")]
	public class CanGenerateWarningSuppressionFile
	{
		public static void Main ()
		{
			TriggerWarnings_Lib.Main ();
			var triggerWarnings = new Warnings ();
			triggerWarnings.Warning1 ();
			var getProperty = triggerWarnings.Warning2;
			var triggerWarningsFromNestedType = new Warnings.NestedType ();
			triggerWarningsFromNestedType.Warning3 ();
			var list = new List<int> ();
			triggerWarningsFromNestedType.Warning4 (ref list);
		}
	}

	[Kept]
	[KeptMember (".ctor()")]
	class Warnings
	{
		[Kept]
		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (CanGenerateWarningSuppressionFile);
		}

		[Kept]
		public void Warning1 ()
		{
			Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		[Kept]
		public int Warning2 {
			[Kept]
			get {
				Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
				return 0;
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class NestedType
		{
			[Kept]
			public void Warning3 ()
			{
				Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			}

			[Kept]
			public void Warning4<T> (ref List<T> p)
			{
				Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			}
		}
	}
}
