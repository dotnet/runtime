using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded
{
	public class NestedInterfacesWithExplicitAndNormal1
	{
		public static void Main ()
		{
			MarkBase1AndBase3 (null, null);
			MarkClassTypeOnly (null);
		}

		[Kept]
		static void MarkBase1AndBase3 (IBase arg1, IBase3 arg3)
		{
			arg3.Method ();
		}

		[Kept]
		static void MarkClassTypeOnly (Foo arg)
		{
		}

		[Kept]
		interface IBase
		{
		}

		[Kept]
		[KeptInterface (typeof (IBase))]
		interface IBase2 : IBase
		{
			[Kept]
			void Method ();
		}

		[Kept]
		[KeptInterface (typeof (IBase2))]
		[KeptInterface (typeof (IBase))]
		interface IBase3 : IBase2
		{
		}

		[Kept]
		[KeptInterface (typeof (IBase3))]
		[KeptInterface (typeof (IBase2))]
		[KeptInterface (typeof (IBase))]
		class Foo : IBase3
		{
			[Kept]
			void IBase2.Method ()
			{
			}

			public void Method ()
			{
			}
		}
	}
}