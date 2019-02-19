using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic {
	class MultiLevelNestedClassesAllRemovedWhenNonUsed {
		public static void Main ()
		{
		}

		public class A {
			public class AB {
				public class ABC {
				}

				public class ABD {
				}
			}
		}
	}
}