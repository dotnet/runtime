using System;

public class TestJit {

	public static int test_nan ()
	{
		double nan1 = Double.NaN;
		double nan2 = Double.NaN;
		double neg_inf1 = Double.NegativeInfinity;
		double neg_inf2 = Double.NegativeInfinity;
		double pos_inf1 = Double.PositiveInfinity;
		double pos_inf2 = Double.PositiveInfinity;
		
		if (nan1 == nan2)
			return 1;
		
		if (!nan1.Equals(nan2))
			return 1;
			
		if (neg_inf1 != neg_inf2)
			return 1;

		if (!neg_inf1.Equals(neg_inf2))
			return 1;

		if (pos_inf1 != pos_inf2)
			return 1;

		if (!pos_inf1.Equals(pos_inf2))
			return 1;

		if (pos_inf1 == neg_inf1)
			return 1;

		if (pos_inf1.Equals (neg_inf1))
			return 1;

		if (pos_inf1 == nan1)
			return 1;
		
		return 0;
	}

	public static double test_call (double a, double b) {
		return a+b;
	}

	public static int test_alu ()
	{
		double a = 9, b = 6;
		
		if ((a + b) != 15)
			return 1;
		
		if ((a - b) != 3)
			return 1;

		if ((a * b) != 54)
			return 1;
		
		if ((a / 4) != 2.25)
			return 1;

		if ((a % 4) != 1)
			return 1;

		if (-a != -9)
			return 1;

		return 0;
	}
	
	public static int test_branch ()
	{
		double a = 0.5, b = 0.5, t;

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

		if (a > 1.0) return 1;
		
		if (a < 0.1) return 1;
		
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

		if (test_nan () != 0)
			return num;
		num++;

			
		return 0;
	}
}

