
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	class StaticDefaultInterfaceMethodOnDerivedInterface
	{
		[Kept]
		public static void Main ()
		{
#if SUPPORTS_DEFAULT_INTERFACE_METHODS
			M<Instance>();
#endif
		}

#if SUPPORTS_DEFAULT_INTERFACE_METHODS

		[Kept]
		static int M<T>() where T : IBase {
			return T.Value;
		}

		[Kept]
		interface IBase {
			[Kept]
			static abstract int Value
			{
				[Kept]
				get;
			}
		}

		[Kept]
		[KeptInterface(typeof(IBase))]
		interface IMiddle : IBase {
			[Kept] // Should be removable -- Add link to bug before merge
			static int IBase.Value
			{
				[Kept] // Should be removable -- Add link to bug before merge
				get=>1;
			}
		}

		[Kept]
		[KeptInterface(typeof(IBase))]
		[KeptInterface(typeof(IMiddle))]
		interface IDerived : IMiddle {
			[Kept]
			static int IBase.Value
			{
				[Kept]
				get=>2;
			}
		}

		interface INotReferenced
		{}

		[Kept]
		[KeptInterface(typeof(IDerived))]
		[KeptInterface(typeof(IMiddle))]
		[KeptInterface(typeof(IBase))]
		struct Instance : IDerived, INotReferenced {
		}
#endif
	}
}
