using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class InterfaceMethodImplementedOnBaseClassDoesNotGetStripped
	{
		public static void Main ()
		{
			I1 i1 = new Derived ();
			i1.Used ();
		}

		public interface I1
		{
			void Unused ();

			[Kept]
			void Used ();
		}

		[KeptMember (".ctor()")]
		public class Base
		{
			public void Unused ()
			{
			}

			[Kept]
			public void Used ()
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base))]
		[KeptInterface (typeof (I1))]
		public class Derived : Base, I1
		{
		}
	}
}