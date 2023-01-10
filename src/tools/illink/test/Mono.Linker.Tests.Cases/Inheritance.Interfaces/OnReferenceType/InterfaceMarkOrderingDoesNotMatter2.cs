
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	[Kept]
	[KeptMember (".ctor()")]
	[KeptInterface (typeof (InterfaceMarkOrderingDoesNotMatter2_Z.A))]
	[KeptInterface (typeof (InterfaceMarkOrderingDoesNotMatter2_Z.B))]
	[KeptInterface (typeof (InterfaceMarkOrderingDoesNotMatter2_Z.C))]
	[KeptInterface (typeof (InterfaceMarkOrderingDoesNotMatter2_Z.D))]
	[KeptInterface (typeof (InterfaceMarkOrderingDoesNotMatter2_Z.Nested.F))]
	[KeptInterface (typeof (InterfaceMarkOrderingDoesNotMatter2_Z.Y))]
	[KeptInterface (typeof (InterfaceMarkOrderingDoesNotMatter2_Z.Z))]
	[KeptInterface (typeof (InterfaceMarkOrderingDoesNotMatter2_Z.E))]
	class InterfaceMarkOrderingDoesNotMatter2_A : InterfaceMarkOrderingDoesNotMatter2_Z.A, InterfaceMarkOrderingDoesNotMatter2_Z.B, InterfaceMarkOrderingDoesNotMatter2_Z.Y,
		InterfaceMarkOrderingDoesNotMatter2_Z.Z, InterfaceMarkOrderingDoesNotMatter2_Z.C, InterfaceMarkOrderingDoesNotMatter2_Z.D,
		InterfaceMarkOrderingDoesNotMatter2_Z.Nested.F, InterfaceMarkOrderingDoesNotMatter2_Z.E
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

	[SetupLinkerDescriptorFile ("InterfaceMarkOrderingDoesNotMatter2.xml")]
	public class InterfaceMarkOrderingDoesNotMatter2
	{
		public static void Main ()
		{
			CauseAToBeMarked ().AMethod ();
			CauseZToBeMarked ().ZMethod ();

			InterfaceMarkOrderingDoesNotMatter2_A mUsedInCode = new InterfaceMarkOrderingDoesNotMatter2_A ();

			InterfaceMarkOrderingDoesNotMatter2_Z.B b = mUsedInCode;
			InterfaceMarkOrderingDoesNotMatter2_Z.Y y = mUsedInCode;
			InterfaceMarkOrderingDoesNotMatter2_Z.Nested.F f = mUsedInCode;

			b.BMethod ();
			y.YMethod ();
			f.FMethod ();
		}

		[Kept]
		static InterfaceMarkOrderingDoesNotMatter2_Z.A CauseAToBeMarked ()
		{
			return null;
		}

		[Kept]
		static InterfaceMarkOrderingDoesNotMatter2_Z.Z CauseZToBeMarked ()
		{
			return null;
		}
	}

	[Kept]
	public class InterfaceMarkOrderingDoesNotMatter2_Z
	{
		[Kept]
		public interface A
		{
			[Kept]
			void AMethod ();
		}

		[Kept]
		public interface Z
		{
			[Kept]
			void ZMethod ();
		}

		[Kept]
		public interface C
		{
			[Kept]
			void CMethod ();
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
		public interface B
		{
			[Kept]
			void BMethod ();
		}

		[Kept]
		public interface Y
		{
			[Kept]
			void YMethod ();
		}

		[Kept]
		public interface D
		{
			[Kept]
			void DMethod ();
		}

		[Kept]
		public interface E
		{
			[Kept]
			void EMethod ();
		}
	}
}