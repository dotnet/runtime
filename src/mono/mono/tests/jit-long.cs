using System;

public class TestJit {

	public static long test_call (long a, long b) {
		return a+b;
	}

	public static int test_shift_1 ()
	{
		long a = 9;
		int b = 1;
		
		if ((a >> b) != 4)
			return 1;
		
		if ((a >> 63) != 0)
			return 1;

		if ((a << 1) != 18)
			return 1;

		if ((a << b) != 18)
			return 1;

		a = -9;
		if ((a >> b) != -5)
			return 1;

		return 0;
	}

	public static int test_shift_2 ()
	{
		unchecked {
			long c = (long)0x800000ff00000000;
			long d = (long)0x8ef0abcd00000000;
			long t;
			int sa;
			
			t = c>>4;
			Console.WriteLine (t.ToString ("X"));
			if (t != (long)0xf800000ff0000000)
				return 1;

			if ((t << 4) != c)
				return 1;
			
			t = d>>40;
			Console.WriteLine (t.ToString ("X"));
			if (t != (long)0xffffffffff8ef0ab)
				return 1;

			if ((t << 40) != (long)0x8ef0ab0000000000)
				return 1;
			
			
		}
		
		return 0;
	}

	public static int test_shift_3 ()
	{
		checked {
			ulong c = 0x800000ff00000000;
			ulong d = 0x8ef0abcd00000000;
			ulong t;
			
			t = c >> 4;
			Console.WriteLine (t.ToString ("X"));
			if (t != 0x0800000ff0000000)
				return 1;

			if ((t << 4) != c)
				return 1;
			
			t = d >> 40;
			Console.WriteLine (t.ToString ("X"));
			if (t != 0x8ef0ab)
				return 1;

			if ((t << 40) != 0x8ef0ab0000000000)
				return 1;
		}

		return 0;
	}

	public static int test_alu ()
	{
		long a = 9, b = 6;
		
		if ((a + b) != 15)
			return 1;
		
		if ((a - b) != 3)
			return 1;

		if ((a & 8) != 8)
			return 1;

		if ((a | 2) != 11)
			return 1;

		if ((a * b) != 54)
			return 1;
		
		if ((a / 4) != 2)
			return 1;
		
		if ((a % 4) != 1)
			return 1;

		if (-a != -9)
			return 1;

		b = -1;
		if (~b != 0)
			return 1;

		return 0;
	}
	
	public static int test_branch ()
	{
		long a = 5, b = 5, t;
		
		if (a == b) t = 1; else t = 0;
		if (t != 1) return 1;

		if (a != b) t = 0; else t = 1;
		if (t != 1) return 1;

		if (a >= b) t = 1; else t = 0;
		if (t != 1) return 1;

		if (a > b) t = 0; else t = 1;
		if (t != 1) return 1;

		if (a <= b) t = 1; else t = 0;
		if (t != 1) return 1;

		if (a < b) t = 0; else t = 1;
		if (t != 1) return 1;

		return 0;
	}

	public static int Main() {
		int num = 0;

		num++;
		if (test_shift_1 () != 0)
			return num;
		num++;
		if (test_shift_2 () != 0)
			return num;
		num++;
		if (test_shift_3 () != 0)
			return num;
		
		num++;
		if (test_call (3, 5) != 8)
			return num;

		num++;
		if (test_branch () != 0)
			return num;

		num++;
		if (test_alu () != 0)
			return num;
		
		return 0;
	}
}

