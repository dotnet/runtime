using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBody
{
	[SetupLinkerArgument ("--enable-opt", "unreachablebodies")]
	public class WorksWithDynamicDependency
	{
		public static void Main ()
		{
			Foo.StaticMethod ();
		}

		[Kept]
		class Foo
		{
			[Kept]
			[DynamicDependency ("InstanceMethod()")]
			public static void StaticMethod ()
			{
			}

			[Kept]
			[ExpectBodyModified]
			public void InstanceMethod ()
			{
				UsedByMethod ();
			}

			void UsedByMethod ()
			{
			}
		}
	}
}