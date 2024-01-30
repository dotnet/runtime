using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	class MostSpecificDefaultImplementationKept
	{
		[Kept]
		public static void Main ()
		{
#if SUPPORTS_DEFAULT_INTERFACE_METHODS
			M<UsedAsIBase> ();
			NotUsedAsIBase.Keep ();
			GenericType<UsedAsIBase2>.M ();

#endif
		}

#if SUPPORTS_DEFAULT_INTERFACE_METHODS

		[Kept]
		static int M<T> () where T : IBase
		{
			return T.Value;
		}

		[Kept]
		interface IBase
		{
			[Kept]
			static virtual int Value {
				[Kept]
				get => 0;
			}

			static virtual int Value2 {
				get => 0;
			}
		}

		[Kept]
		[KeptInterface (typeof (IBase))]
		interface IMiddle : IBase
		{
			[Kept] // Should be removable -- Add link to bug before merge
			static int IBase.Value {
				[Kept] // Should be removable -- Add link to bug before merge
				get => 1;
			}
		}

		[Kept]
		[KeptInterface (typeof (IBase))]
		[KeptInterface (typeof (IMiddle))]
		interface IDerived : IMiddle
		{
			[Kept]
			static int IBase.Value {
				[Kept]
				get => 2;
			}
		}

		interface INotReferenced
		{ }

		[Kept]
		[KeptInterface (typeof (IDerived))]
		[KeptInterface (typeof (IMiddle))]
		[KeptInterface (typeof (IBase))]
		class UsedAsIBase : IDerived, INotReferenced
		{
		}

		[Kept]
		class NotUsedAsIBase : IDerived, INotReferenced
		{
			[Kept]
			public static void Keep () { }
		}

		[Kept]
		class GenericType<T> where T : IBase
		{
			[Kept]
			public static int M () => T.Value;
		}

		[Kept]
		[KeptInterface (typeof (IDerived))]
		[KeptInterface (typeof (IMiddle))]
		[KeptInterface (typeof (IBase))]
		class UsedAsIBase2 : IDerived
		{
		}
#endif
	}
}
