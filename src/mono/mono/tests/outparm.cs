public class OutParm {

	public static void out_param (out int n) {
		n = 1;
	}
	public static void ref_param (ref int n) {
		n += 2;
	}
	public static int Main () {
		int n = 0;
		out_param (out n);
		if (n != 1)
			return 1;
		ref_param (ref n);
		if (n != 3)
			return 2;
		return 0;
	}
}


