public class TestJit {

	public static long test_call (long a, long b) {
		return a+b;
	}

	public static int test_shift ()
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
		int num = 1;

		if (test_shift () != 0)
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
		num++;
		
		return 0;
	}
}

