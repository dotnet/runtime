/*
 * enum-intrins.cs: Tests for Enum.HasFlag () intrinsic.
 */

using System;

namespace Test {

	enum ByteEnum : byte {
		A = 1,
		B = 2,
		C = 4,
	}

	enum SByteEnum : sbyte {
		A = 1,
		B = 2,
		C = 4,
	}

	enum ShortEnum : short {
		A = 1,
		B = 2,
		C = 4,
	}

	enum UShortEnum : ushort {
		A = 1,
		B = 2,
		C = 4,
	}

	enum IntEnum : int {
		A = 1,
		B = 2,
		C = 4,
	}

	enum UIntEnum : uint {
		A = 1,
		B = 2,
		C = 4,
	}

	enum LongEnum : long {
		A = 1,
		B = 2,
		C = 4,
	}

	enum ULongEnum : ulong {
		A = 1,
		B = 2,
		C = 4,
	}

	public class Test {

		static int TestIntrinsic ()
		{
			var byteEnum1 = ByteEnum.A | ByteEnum.B;
			if (byteEnum1.HasFlag (ByteEnum.C))
				return 1;

			var byteEnum2 = ByteEnum.A | ByteEnum.C;
			if (!byteEnum2.HasFlag (ByteEnum.C))
				return 2;

			var sbyteEnum1 = SByteEnum.A | SByteEnum.B;
			if (sbyteEnum1.HasFlag (SByteEnum.C))
				return 3;

			var sbyteEnum2 = SByteEnum.A | SByteEnum.C;
			if (!sbyteEnum2.HasFlag (SByteEnum.C))
				return 4;

			var shortEnum1 = ShortEnum.A | ShortEnum.B;
			if (shortEnum1.HasFlag (ShortEnum.C))
				return 5;

			var shortEnum2 = ShortEnum.A | ShortEnum.C;
			if (!shortEnum2.HasFlag (ShortEnum.C))
				return 6;

			var ushortEnum1 = UShortEnum.A | UShortEnum.B;
			if (ushortEnum1.HasFlag (UShortEnum.C))
				return 7;

			var ushortEnum2 = UShortEnum.A | UShortEnum.C;
			if (!ushortEnum2.HasFlag (UShortEnum.C))
				return 8;

			var intEnum1 = IntEnum.A | IntEnum.B;
			if (intEnum1.HasFlag (IntEnum.C))
				return 9;

			var intEnum2 = IntEnum.A | IntEnum.C;
			if (!intEnum2.HasFlag (IntEnum.C))
				return 10;

			var uintEnum1 = UIntEnum.A | UIntEnum.B;
			if (uintEnum1.HasFlag (UIntEnum.C))
				return 11;

			var uintEnum2 = UIntEnum.A | UIntEnum.C;
			if (!uintEnum2.HasFlag (UIntEnum.C))
				return 12;

			var longEnum1 = LongEnum.A | LongEnum.B;
			if (longEnum1.HasFlag (LongEnum.C))
				return 13;

			var longEnum2 = LongEnum.A | LongEnum.C;
			if (!longEnum2.HasFlag (LongEnum.C))
				return 14;

			var ulongEnum1 = ULongEnum.A | ULongEnum.B;
			if (ulongEnum1.HasFlag (ULongEnum.C))
				return 15;

			var ulongEnum2 = ULongEnum.A | ULongEnum.C;
			if (!ulongEnum2.HasFlag (ULongEnum.C))
				return 16;

			return 0;
		}

		static int TestBoxed ()
		{
			/* The casts to Enum will make the JIT's pattern matching miss the call. */

			var byteEnum1 = ByteEnum.A | ByteEnum.B;
			if (byteEnum1.HasFlag ((Enum)(object) ByteEnum.C))
				return 1;

			var byteEnum2 = ByteEnum.A | ByteEnum.C;
			if (!byteEnum2.HasFlag ((Enum)(object) ByteEnum.C))
				return 2;

			var sbyteEnum1 = SByteEnum.A | SByteEnum.B;
			if (sbyteEnum1.HasFlag ((Enum)(object) SByteEnum.C))
				return 3;

			var sbyteEnum2 = SByteEnum.A | SByteEnum.C;
			if (!sbyteEnum2.HasFlag ((Enum)(object) SByteEnum.C))
				return 4;

			var shortEnum1 = ShortEnum.A | ShortEnum.B;
			if (shortEnum1.HasFlag ((Enum)(object) ShortEnum.C))
				return 5;

			var shortEnum2 = ShortEnum.A | ShortEnum.C;
			if (!shortEnum2.HasFlag ((Enum)(object) ShortEnum.C))
				return 6;

			var ushortEnum1 = UShortEnum.A | UShortEnum.B;
			if (ushortEnum1.HasFlag ((Enum)(object) UShortEnum.C))
				return 7;

			var ushortEnum2 = UShortEnum.A | UShortEnum.C;
			if (!ushortEnum2.HasFlag ((Enum)(object) UShortEnum.C))
				return 8;

			var intEnum1 = IntEnum.A | IntEnum.B;
			if (intEnum1.HasFlag ((Enum)(object) IntEnum.C))
				return 9;

			var intEnum2 = IntEnum.A | IntEnum.C;
			if (!intEnum2.HasFlag ((Enum)(object) IntEnum.C))
				return 10;

			var uintEnum1 = UIntEnum.A | UIntEnum.B;
			if (uintEnum1.HasFlag ((Enum)(object) UIntEnum.C))
				return 11;

			var uintEnum2 = UIntEnum.A | UIntEnum.C;
			if (!uintEnum2.HasFlag ((Enum)(object) UIntEnum.C))
				return 12;

			var longEnum1 = LongEnum.A | LongEnum.B;
			if (longEnum1.HasFlag ((Enum)(object) LongEnum.C))
				return 13;

			var longEnum2 = LongEnum.A | LongEnum.C;
			if (!longEnum2.HasFlag ((Enum)(object) LongEnum.C))
				return 14;

			var ulongEnum1 = ULongEnum.A | ULongEnum.B;
			if (ulongEnum1.HasFlag ((Enum)(object) ULongEnum.C))
				return 15;

			var ulongEnum2 = ULongEnum.A | ULongEnum.C;
			if (!ulongEnum2.HasFlag ((Enum)(object) ULongEnum.C))
				return 16;

			return 0;
		}

		public static int Main ()
		{
			int res;

			if ((res = TestIntrinsic ()) != 0)
				return res;

			if ((res = TestBoxed ()) != 0)
				return res;

			return 0;
		}
	}
}
