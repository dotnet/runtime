using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	class MostSpecificDefaultImplementationKeptInstance
	{
		[Kept]
		public static void Main ()
		{
			M (new UsedAsIBase());
		}


		[Kept]
		static int M (IBase ibase)
		{
			return ibase.Value;
		}

		[Kept]
		interface IBase
		{
			[Kept]
			int Value {
				[Kept]
				get => 0;
			}

			int Value2 {
				get => 0;
			}
		}

		[Kept]
		[KeptInterface (typeof (IBase))]
		interface IMiddle : IBase
		{
			int IBase.Value {
				get => 1;
			}

			int Value2 {
				get => 0;
			}
		}

		[Kept]
		[KeptInterface (typeof (IBase))]
		[KeptInterface (typeof (IMiddle))]
		interface IDerived : IMiddle
		{
			[Kept]
			int IBase.Value {
				[Kept]
				get => 2;
			}

			int Value2 {
				get => 0;
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
	}
}
