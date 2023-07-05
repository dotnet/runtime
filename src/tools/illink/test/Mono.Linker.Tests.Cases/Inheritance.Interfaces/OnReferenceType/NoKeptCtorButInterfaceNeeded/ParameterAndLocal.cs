using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded
{
	public class ParameterAndLocal
	{
		public static void Main ()
		{
			Helper (null);
		}

		[Kept]
		static void Helper (Foo f)
		{
			IFoo i = f;
			i.Method ();
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			void Method ();
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		class Foo : IFoo
		{
			[Kept]
			public void Method ()
			{
			}
		}
	}
}