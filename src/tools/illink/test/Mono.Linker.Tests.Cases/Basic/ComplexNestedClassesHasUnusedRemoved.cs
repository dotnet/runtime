using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class ComplexNestedClassesHasUnusedRemoved
	{
		public static void Main ()
		{
			new A.AB.ABD ();
		}

		[Kept]
		public class A
		{
			[Kept]
			public class AB
			{
				public class ABC
				{
				}

				[Kept]
				[KeptMember (".ctor()")]
				public class ABD
				{
					public class ABDE
					{
					}
				}
			}
		}
	}
}