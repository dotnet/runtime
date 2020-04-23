using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	public class InterfaceUsedOnlyAsConstraintIsKept
	{
		public static void Main ()
		{
			var a = new A ();
			Helper (a);
		}

		[Kept]
		static void Helper<T> (T arg) where T : IFoo
		{
		}

		[Kept]
		interface IFoo
		{
			void Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		class A : IFoo
		{
			public void Foo ()
			{
			}
		}
	}
}