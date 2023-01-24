using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.PreserveDependencies
{
	[SetupCompileBefore ("FakeSystemAssembly.dll", new[] { "Dependencies/PreserveDependencyAttribute.cs" })]
	public class PreserveDependencyField
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
			[PreserveDependency ("ExtraMethod1")]
			public int field;

			[Kept]
			static void ExtraMethod1 ()
			{
			}
		}
	}
}
