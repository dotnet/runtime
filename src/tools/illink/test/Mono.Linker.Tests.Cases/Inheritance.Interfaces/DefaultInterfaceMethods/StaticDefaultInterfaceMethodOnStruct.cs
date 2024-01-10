using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	class StaticDefaultInterfaceMethodOnStruct
	{
		public static void Main ()
		{
#if SUPPORTS_DEFAULT_INTERFACE_METHODS
			Foo<Derived> ();
#endif
		}

#if SUPPORTS_DEFAULT_INTERFACE_METHODS
		[Kept]
		static void Foo<T> () where T : IBase
		{
			T.Foo ();
		}

		[Kept]
		interface IBase
		{
			[Kept]
			static abstract void Foo ();
		}

		[Kept]
		[KeptInterface (typeof (IBase))]
		interface IDerived : IBase
		{
			[Kept]
			static void IBase.Foo () { }
		}

		[Kept]
		[KeptInterface (typeof (IDerived))]
		[KeptInterface (typeof (IBase))]
		struct Derived : IDerived
		{
		}
#endif
	}
}
