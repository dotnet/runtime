using System;


struct BoolStruct {
	bool x;
	public BoolStruct (bool x) { this.x = x; }
}

struct CharStruct {
	char x;
	public CharStruct (char x) { this.x = x; }
}

struct Int8Struct {
	sbyte x;
	public Int8Struct (sbyte x) { this.x = x; }
}

struct UInt8Struct {
	byte x;
	public UInt8Struct (byte x) { this.x = x; }
}

struct Int16Struct {
	short x;
	public Int16Struct (short x) { this.x = x; }
}

struct UInt16Struct {
	ushort x;
	public UInt16Struct (ushort x) { this.x = x; }
}

struct Int32Struct {
	int x;
	public Int32Struct (int x) { this.x = x; }
}

struct UInt32Struct {
	uint x;
	public UInt32Struct (uint x) { this.x = x; }
}

struct Int64Struct {
	long x;
	public Int64Struct (long x) { this.x = x; }
}

struct UInt64Struct {
	ulong x;
	public UInt64Struct (ulong x) { this.x = x; }
}

struct FloatStruct {
	float x;
	public FloatStruct (float x) { this.x = x; }
}

struct DoubleStruct {
	double x;
	public DoubleStruct (double x) { this.x = x; }
}


public class Driver {
	static void AssertEqual (object a, object b) {
		if (!a.Equals (b))
			throw new Exception (String.Format ("must be equal {0} {1}", a, b));
	}

	static void AssertNotEqual (object a, object b) {
		if (a.Equals (b))
			throw new Exception (String.Format ("must not be equal {0} {1}", a, b));
	}

	public static int Main () {
		AssertEqual (new BoolStruct (true), new BoolStruct (true));
		AssertNotEqual (new BoolStruct (false), new BoolStruct (true));

		AssertEqual (new CharStruct ('c'), new CharStruct ('c'));
		AssertNotEqual (new CharStruct ('d'), new CharStruct ('c'));

		AssertEqual (new Int8Struct (13), new Int8Struct (13));
		AssertEqual (new Int8Struct (0), new Int8Struct (0));
		AssertNotEqual (new Int8Struct (44), new Int8Struct (1));
		AssertNotEqual (new Int8Struct (0), new Int8Struct (55));

		AssertEqual (new UInt8Struct (13), new UInt8Struct (13));
		AssertEqual (new UInt8Struct (0), new UInt8Struct (0));
		AssertNotEqual (new UInt8Struct (44), new UInt8Struct (1));
		AssertNotEqual (new UInt8Struct (0), new UInt8Struct (55));

		AssertEqual (new Int16Struct (13), new Int16Struct (13));
		AssertEqual (new Int16Struct (0), new Int16Struct (0));
		AssertNotEqual (new Int16Struct (44), new Int16Struct (1));
		AssertNotEqual (new Int16Struct (0), new Int16Struct (55));

		AssertEqual (new UInt16Struct (13), new UInt16Struct (13));
		AssertEqual (new UInt16Struct (0), new UInt16Struct (0));
		AssertNotEqual (new UInt16Struct (44), new UInt16Struct (1));
		AssertNotEqual (new UInt16Struct (0), new UInt16Struct (55));

		AssertEqual (new Int32Struct (13), new Int32Struct (13));
		AssertEqual (new Int32Struct (0), new Int32Struct (0));
		AssertNotEqual (new Int32Struct (44), new Int32Struct (1));
		AssertNotEqual (new Int32Struct (0), new Int32Struct (55));

		AssertEqual (new UInt32Struct (13), new UInt32Struct (13));
		AssertEqual (new UInt32Struct (0), new UInt32Struct (0));
		AssertNotEqual (new UInt32Struct (44), new UInt32Struct (1));
		AssertNotEqual (new UInt32Struct (0), new UInt32Struct (55));

		AssertEqual (new Int64Struct (13), new Int64Struct (13));
		AssertEqual (new Int64Struct (0), new Int64Struct (0));
		AssertNotEqual (new Int64Struct (44), new Int64Struct (1));
		AssertNotEqual (new Int64Struct (0), new Int64Struct (55));

		AssertEqual (new UInt64Struct (13), new UInt64Struct (13));
		AssertEqual (new UInt64Struct (0), new UInt64Struct (0));
		AssertNotEqual (new UInt64Struct (44), new UInt64Struct (1));
		AssertNotEqual (new UInt64Struct (0), new UInt64Struct (55));

		AssertEqual (new FloatStruct (13), new FloatStruct (13));
		AssertEqual (new FloatStruct (0), new FloatStruct (0));
		AssertNotEqual (new FloatStruct (44), new FloatStruct (1));
		AssertNotEqual (new FloatStruct (0), new FloatStruct (55));

		AssertEqual (new DoubleStruct (13), new DoubleStruct (13));
		AssertEqual (new DoubleStruct (0), new DoubleStruct (0));
		AssertNotEqual (new DoubleStruct (44), new DoubleStruct (1));
		AssertNotEqual (new DoubleStruct (0), new DoubleStruct (55));

		return 0;
	}
}
