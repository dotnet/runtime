public class TestJit {

	public static ulong test_call (ulong a, ulong b) {
		return a+b;
	}

	public static int test_alu ()
	{
		ulong a = 9, b = 6;
		
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

		b = 0;
		if (~b != 0xffffffffffffffff)
			return 1;

		return 0;
	}
	
	public static int test_branch ()
	{
		ulong a = 5, b = 5, t;
		
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

