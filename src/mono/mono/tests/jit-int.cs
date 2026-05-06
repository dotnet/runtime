using System;

public class TestJit {

	public static int test_short ()
	{
		int max = 32767;
		int min = -32768;

		int t1 = 0xffeedd;
		short s1 = (short)t1;
		int t2 = s1;

		if ((uint)t2 != 0xffffeedd) 
			return 1;
		
		Console.WriteLine (t2.ToString ("X"));
		
		if (Int16.Parse((min).ToString()) != -32768)
			return 1;

		if (Int16.Parse((max).ToString()) != 32767)
			return 1;

		return 0;
	}

	public static int test_call (int a, int b) {
		return a+b;
	}

	public static int test_shift ()
	{
		int a = 9, b = 1;
		
		if ((a << 1) != 18)
			return 1;

		if ((a << b) != 18)
			return 1;

		if ((a >> 1) != 4)
			return 1;
		
		if ((a >> b) != 4)
			return 1;

		a = -9;
		if ((a >> b) != -5)
			return 1;

		return 0;
	}
	
	public static int test_alu ()
	{
		int a = 9, b = 6;
		
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
		int a = 5, b = 5, t;
		
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
		if (test_short () != 0)
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

		if (test_shift () != 0)
			return num;
		num++;
		
		return 0;
	}
}

