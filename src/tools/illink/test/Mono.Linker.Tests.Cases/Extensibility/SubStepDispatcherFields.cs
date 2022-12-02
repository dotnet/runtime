using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Extensibility
{
	[SetupCompileBefore ("MyDispatcherFields.dll", new[] { "Dependencies/MyDispatcher.cs", "Dependencies/CustomSubStepFields.cs" }, new[] { "illink.dll", "Mono.Cecil.dll", "netstandard.dll" })]
	[SetupLinkerArgument ("--custom-step", "-MarkStep:MyDispatcher,MyDispatcherFields.dll")]
	public class SubStepDispatcherFields
	{
		public static void Main ()
		{
		}

		[Kept]
		public class NestedType
		{
			[Kept]
			public int field;
		}
	}
}