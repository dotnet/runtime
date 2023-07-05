using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class GenericInterfaceWithGenericBaseMethodWithExplicit
	{
		public static void Main ()
		{
			IFoo<object, int> f = new FooWithBase<object, int> ();
			f.Method (null, 0);
		}

		[Kept]
		interface IFoo<T1, T2>
		{
			[Kept]
			void Method (T1 arg, T2 arg2);
		}

		[Kept]
		[KeptMember (".ctor()")]
		class BaseFoo<T1, T2>
		{
			public void Method (T1 arg, T2 arg2)
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (BaseFoo<,>), "T1", "T2")]
		[KeptInterface (typeof (IFoo<,>), "T1", "T2")]
		class FooWithBase<T1, T2> : BaseFoo<T1, T2>, IFoo<T1, T2>
		{
			[Kept]
			void IFoo<T1, T2>.Method (T1 arg, T2 arg2)
			{
			}
		}
	}
}