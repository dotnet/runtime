
public class TestIntOps {

	public static int simple_add (int a, int b) {
		return a+b;
	}

	public static int simple_sub (int a, int b) {
		return a-b;
	}
	
	public static int simple_mul (int a, int b) {
		return a*b;
	}
	
	public static int Main() {
		int num = 1;
	
		if (simple_add (1, 1)   != 2)  return num;
		num++;
		if (simple_add (31, -1) != 30) return num;
		num++;
		if (simple_sub (31, -1) != 32) return num;
		num++;
		if (simple_mul (12, 12) != 144) return num;
		num++;
		
		// add more meaningful tests
	
    	return 0;
	}
}
