using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	[SetupLinkerDescriptorFile ("InterfaceMarkOrderingDoesNotMatter.xml")]
	public class InterfaceMarkOrderingDoesNotMatter
	{
		public static void Main ()
		{
			CauseAToBeMarked ().AMethod ();
			CauseZToBeMarked ().ZMethod ();

			MMarked m = new MMarked ();

			B b = m;
			Y y = m;
			Nested.F f = m;

			b.BMethod ();
			y.YMethod ();
			f.FMethod ();
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
			[Kept]
			void CMethod ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (A))]
		[KeptInterface (typeof (B))]
		[KeptInterface (typeof (C))]
		[KeptInterface (typeof (D))]
		[KeptInterface (typeof (Nested.F))]
		[KeptInterface (typeof (Y))]
		[KeptInterface (typeof (Z))]
		[KeptInterface (typeof (E))]
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

			[Kept]
			public void CMethod ()
			{
			}

			[Kept]
			public void DMethod ()
			{
			}

			[Kept]
			public void FMethod ()
			{
			}

			[Kept]
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

		[Kept]
		interface D
		{
			[Kept]
			void DMethod ();
		}

		[Kept]
		interface E
		{
			[Kept]
			void EMethod ();
		}
	}
}