using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	[SetupLinkerDescriptorFile ("InterfaceMarkOrderingDoesNotMatter3.xml")]
	public class InterfaceMarkOrderingDoesNotMatter3
	{
		public static void Main ()
		{
			CauseAToBeMarked ().AMethod ();
			CauseZToBeMarked ().ZMethod ();
			CauseBToBeMarked ().BMethod ();
			CauseYToBeMarked ().YMethod ();
			CauseFToBeMarked ().FMethod ();
			var c = CauseCToBeMarked ();
		}

		[Kept]
		static A CauseAToBeMarked ()
		{
			return null;
		}

		[Kept]
		static Z CauseZToBeMarked ()
		{
			return null;
		}

		[Kept]
		static B CauseBToBeMarked ()
		{
			return null;
		}

		[Kept]
		static Y CauseYToBeMarked ()
		{
			return null;
		}

		[Kept]
		static Nested.F CauseFToBeMarked ()
		{
			return null;
		}

		[Kept]
		static C CauseCToBeMarked ()
		{
			return null;
		}

		[Kept]
		interface A
		{
			[Kept]
			void AMethod ();
		}

		[Kept]
		interface Z
		{
			[Kept]
			void ZMethod ();
		}

		[Kept]
		interface C
		{
			void CMethod ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (A))]
		[KeptInterface (typeof (B))]
		[KeptInterface (typeof (C))]
		[KeptInterface (typeof (Nested.F))]
		[KeptInterface (typeof (Y))]
		[KeptInterface (typeof (Z))]
		class MMarked : A, B, Y, Z, C, D, Nested.F, E
		{
			[Kept]
			public void AMethod ()
			{
			}

			[Kept]
			public void BMethod ()
			{
			}

			[Kept]
			public void YMethod ()
			{
			}

			[Kept]
			public void ZMethod ()
			{
			}

			public void CMethod ()
			{
			}

			public void DMethod ()
			{
			}

			[Kept]
			public void FMethod ()
			{
			}

			public void EMethod ()
			{
			}
		}

		[Kept]
		public static class Nested
		{
			[Kept]
			public interface F
			{
				[Kept]
				void FMethod ();
			}
		}

		[Kept]
		interface B
		{
			[Kept]
			void BMethod ();
		}

		[Kept]
		interface Y
		{
			[Kept]
			void YMethod ();
		}

		interface D
		{
			void DMethod ();
		}

		interface E
		{
			void EMethod ();
		}
	}
}