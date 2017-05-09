using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Generics {
	class OverrideWithAnotherVirtualMethodOfSameNameWithDifferentParameterType {
		public static void Main (string [] args)
		{
			new Derived<double, int> ().Method (1.0);
		}

		[KeptMember (".ctor()")]
		public class Base<S> {
			[Kept]
			public virtual S Method (S arg)
			{
				return arg;
			}
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base<>), "K")]
		public class Derived<K, S> : Base<K> {
			[Kept]
			public override K Method (K arg)
			{
				return arg;
			}

			public virtual S Method (S arg)
			{
				return arg;
			}
		}
	}
}