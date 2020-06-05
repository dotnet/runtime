using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	public class DynamicDependencyField
	{
		public static void Main ()
		{
			var b = new B ();
			b.field = 3;
		}

		[KeptMember (".ctor()")]
		class B
		{
			[Kept]
			[DynamicDependency ("ExtraMethod1")]
			public int field;

			[Kept]
			static void ExtraMethod1 ()
			{
			}
		}
	}
}
