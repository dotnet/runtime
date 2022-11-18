using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded
{
	public class ParameterOutAndLocal
	{
		public static void Main ()
		{
			Foo f;
			Helper (out f);
		}

		[Kept]
		static void Helper (out Foo f)
		{
			f = null;
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