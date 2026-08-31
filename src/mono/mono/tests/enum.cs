using System;
using System.Collections.Generic;

namespace Test {

	enum ByteEnum : byte {
		A = 10
	}

	enum SByteEnum : sbyte {
		A = -11
	}

	enum ShortEnum : short {
		A = -12
	}

	enum UShortEnum : ushort {
		A = 13
	}

	enum IntEnum : int {
		A = -15
	}

	enum UIntEnum : uint {
		A = 16
	}

	enum LongEnum : long {
		A = -153453525432334L
	}

	enum ULongEnum : ulong {
		A = 164923797563459L
	}

	public enum YaddaYadda {
		buba,
		birba,
		dadoom,
	};

	public enum byteenum : byte {
		zero,
		one,
		two,
		three
	}

	public enum longenum: long {
		s0 = 0,
		s1 = 1
	}

	public enum sbyteenum : sbyte {
		d0,
		d1
	}

	public class Tests {
		public static int test_0_basic_enum_vals ()
		{
			YaddaYadda val = YaddaYadda.dadoom;
			byteenum be = byteenum.one;
			if (val != YaddaYadda.dadoom)
				return 1;
			if (be != (byteenum)1)
				return 2;
			return 0;
		}

		public static int test_0_byte_enum_hashcode ()
		{
			if (ByteEnum.A.GetHashCode () != EqualityComparer<ByteEnum>.Default.GetHashCode (ByteEnum.A))
				return 1;
			if (ByteEnum.A.GetHashCode () != ((byte)ByteEnum.A).GetHashCode () )
				return 2;
			return 0;
		}

		public static int test_0_sbyte_enum_hashcode ()
		{
			if (SByteEnum.A.GetHashCode () != EqualityComparer<SByteEnum>.Default.GetHashCode (SByteEnum.A))
				return 1;
			if (SByteEnum.A.GetHashCode () != ((sbyte)SByteEnum.A).GetHashCode () )
				return 2;
			return 0;
		}

		public static int test_0_short_enum_hashcode ()
		{
			if (ShortEnum.A.GetHashCode () != EqualityComparer<ShortEnum>.Default.GetHashCode (ShortEnum.A))
				return 1;
			if (ShortEnum.A.GetHashCode () != ((short)ShortEnum.A).GetHashCode () )
				return 2;
			return 0;
		}

		public static int test_0_ushort_enum_hashcode ()
		{
			if (UShortEnum.A.GetHashCode () != EqualityComparer<UShortEnum>.Default.GetHashCode (UShortEnum.A))
				return 1;
			if (UShortEnum.A.GetHashCode () != ((ushort)UShortEnum.A).GetHashCode () )
				return 2;
			return 0;
		}

		public static int test_0_int_enum_hashcode ()
		{
			if (IntEnum.A.GetHashCode () != EqualityComparer<IntEnum>.Default.GetHashCode (IntEnum.A))
				return 1;
			if (IntEnum.A.GetHashCode () != ((int)IntEnum.A).GetHashCode () )
				return 2;
			return 0;
		}

		public static int test_0_uint_enum_hashcode ()
		{
			if (UIntEnum.A.GetHashCode () != EqualityComparer<UIntEnum>.Default.GetHashCode (UIntEnum.A))
				return 1;
			if (UIntEnum.A.GetHashCode () != ((uint)UIntEnum.A).GetHashCode () )
				return 2;
			return 0;
		}

		public static int test_0_long_enum_hashcode ()
		{
			if (LongEnum.A.GetHashCode () != EqualityComparer<LongEnum>.Default.GetHashCode (LongEnum.A))
				return 1;
			if (LongEnum.A.GetHashCode () != ((long)LongEnum.A).GetHashCode () )
				return 2;
			return 0;
		}

		public static int test_0_ulong_enum_hashcode ()
		{
			if (ULongEnum.A.GetHashCode () != EqualityComparer<ULongEnum>.Default.GetHashCode (ULongEnum.A))
				return 1;
			if (ULongEnum.A.GetHashCode () != ((ulong)ULongEnum.A).GetHashCode () )
				return 2;
			return 0;
		}

		public static int Main (String[] args) {
			return TestDriver.RunTests (typeof (Tests), args);
		}

	}

}
