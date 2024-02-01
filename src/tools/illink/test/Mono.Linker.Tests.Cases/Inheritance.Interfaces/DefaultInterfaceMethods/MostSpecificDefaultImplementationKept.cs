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
			NotUsedInGeneric.Keep ();
			GenericType<UsedAsIBase2>.M ();
			GenericType2<UsedInUnconstrainedGeneric>.Keep ();
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
			static int IBase.Value {
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

		[Kept]
		[KeptInterface (typeof (IBase))]
		[KeptInterface (typeof (IMiddle))]
		interface IDerived2 : IMiddle
		{
			// https://github.com/dotnet/runtime/issues/97798
			// This shouldn't need to be kept. Implementor UsedInUnconstrainedGeneric is not passed as a constrained generic
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
		class NotUsedInGeneric : IDerived, INotReferenced
		{
			[Kept]
			public static void Keep () { }
		}

		[Kept]
		[KeptInterface (typeof (IBase))]
		[KeptInterface (typeof (IMiddle))]
		[KeptInterface (typeof (IDerived2))]
		class UsedInUnconstrainedGeneric : IDerived2, INotReferenced
		{
		}


		[Kept]
		class GenericType<T> where T : IBase
		{
			[Kept]
			public static int M () => T.Value;
		}

		[Kept]
		class GenericType2<T>
		{
			[Kept]
			public static void Keep() { }
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
