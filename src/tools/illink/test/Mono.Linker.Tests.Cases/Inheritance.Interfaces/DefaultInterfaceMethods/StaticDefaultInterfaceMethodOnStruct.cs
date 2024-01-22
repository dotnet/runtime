using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	class StaticDefaultInterfaceMethodOnStruct
	{
		public static void Main ()
		{
#if SUPPORTS_DEFAULT_INTERFACE_METHODS
			CallStaticInterfaceMethod<Derived> ();
#endif
		}

#if SUPPORTS_DEFAULT_INTERFACE_METHODS
		[Kept]
		static void CallStaticInterfaceMethod<T> () where T : IBase
		{
			T.StaticInterfaceMethod ();
		}

		[Kept]
		interface IBase
		{
			[Kept]
			static abstract void StaticInterfaceMethod ();
		}

		[Kept]
		[KeptInterface (typeof (IBase))]
		interface IDerived : IBase
		{
			[Kept]
			static void IBase.StaticInterfaceMethod () { }
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
