using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded
{
	public class GenericTypeWithConstraint3
	{
		public static void Main ()
		{
			Foo f = null;
			Bar b = null;
			Bar<BaseFoo2, BaseBar2>.Helper (f, b);
		}

		[Kept]
		static class Bar<T, K> where T : IFoo where K : IBar
		{
			[Kept]
			public static void Helper (T arg, K arg2)
			{
			}
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		abstract class BaseFoo : IFoo
		{
		}

		[Kept]
		[KeptBaseType (typeof (BaseFoo))]
		abstract class BaseFoo2 : BaseFoo
		{
		}

		[Kept]
		[KeptInterface (typeof (IBar))]
		abstract class BaseBar : IBar
		{
		}

		[Kept]
		[KeptBaseType (typeof (BaseBar))]
		abstract class BaseBar2 : BaseBar
		{
		}

		[Kept]
		[KeptBaseType (typeof (BaseFoo2))]
		class Foo : BaseFoo2
		{
		}

		[Kept]
		[KeptBaseType (typeof (BaseBar2))]
		class Bar : BaseBar2
		{
		}

		[Kept]
		interface IFoo
		{
		}

		[Kept]
		interface IBar
		{
		}
	}
}