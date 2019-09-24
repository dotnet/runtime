using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.PreserveDependencies
{
	[SetupLinkerArgument ("--keep-dep-attributes", "true")]
	class PreserveDependencyKeptOption
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
			[KeptAttributeAttribute (typeof (PreserveDependencyAttribute))]

			[PreserveDependency ("field")]
			public static void Test ()
			{
			}
		}
	}
}