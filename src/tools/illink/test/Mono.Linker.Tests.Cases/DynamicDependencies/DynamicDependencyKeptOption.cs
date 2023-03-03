using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupLinkerArgument ("--keep-dep-attributes", "true")]
	class DynamicDependencyKeptOption
	{
		public static void Main ()
		{
			B.Test ();
		}

		class B
		{
			[Kept]
			int field;

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicDependencyAttribute))]

			[DynamicDependency ("field")]
			public static void Test ()
			{
			}
		}
	}
}