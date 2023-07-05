using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Extensibility
{
	// Repro for https://github.com/dotnet/linker/issues/2267.
#if !NETCOREAPP
	[IgnoreTestCase ("Specific to the illink build")]
#endif
	[SetupCompileBefore ("ResolveTypesSubStep.dll", new[] { "Dependencies/ResolveTypesSubStep.cs" }, new[] { "illink.dll", "Mono.Cecil.dll", "netstandard.dll" })]
	[SetupLinkerArgument ("--custom-step", "+OutputStep:ResolveTypesSubStep,ResolveTypesSubStep.dll")]
	[SetupLinkerAction ("copy", "test")]
	[SetupLinkerTrimMode ("link")]
	[KeptMember (".ctor()")]
	public class CustomStepCanResolveTypesAfterSweep
	{
		public static void Main ()
		{
			var f = TypeWithFields.primitiveField;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class TypeWithFields
		{
			[Kept]
			public static int primitiveField;

			[Kept]
			public static float unusedPrimitiveField;
		}
	}
}