public class Fib {

	public static int fib (int n) {
		if (n < 2)
			return 1;
		return fib(n-2)+fib(n-1);
	}
	public static int Main () {
		if (fib (32) != 3524578)
			return 1;
		return 0;
	}
}


