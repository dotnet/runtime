
public class TestIntOps {

	public static sbyte sbyte_add (sbyte a, sbyte b) {
		return (sbyte)(a+b);
	}

	public static short short_add (short a, short b) {
		return (short)(a+b);
	}

	public static double double_add (double a, double b) {
		return a+b;
	}

	public static int int_add (int a, int b) {
		return a+b;
	}

	public static int int_sub (int a, int b) {
		return a-b;
	}
	
	public static int int_mul (int a, int b) {
		return a*b;
	}
	
	public static int int_div (int a, int b) {
		return a/b;
	}

	public static int Main() {
		int num = 1;

		if (int_div (5, 2)   != 2)  return num;
		num++;

		if (int_add (1, 1)   != 2)  return num;
		num++;

		if (int_add (31, -1) != 30) return num;
		num++;

		if (int_sub (31, -1) != 32) return num;
		num++;

		if (int_mul (12, 12) != 144) return num;
		num++;

		if (sbyte_add (1, 1) != 2)  return num;
		num++;
		
		if (sbyte_add (31, -1) != 30)  return num;
		num++;
		
		if (short_add (1, 1) != 2)  return num;
		num++;
		
		if (short_add (31, -1) != 30)  return num;
		num++;
		
		if (double_add (1.5, 1.5) != 3)  return num;
		num++;

		// add more meaningful tests
	
    	return 0;
	}
}
