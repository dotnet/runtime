/*
mono max-min.exe              0m1.468s
mono -O=inline max-min.exe    0m1.087s
../mini/mono max-min.exe      0m0.511s
*/

class T {

	static int DoIt (int a, int b) {
		int x = 0;
		for (int j = 0; j < 200000; j++) {
			x += System.Math.Max (a, b);
			x += System.Math.Max (a, b);
			x += System.Math.Max (j, b);
			x += System.Math.Max (j, b);
			x += System.Math.Min (System.Math.Max (j, x), b);
			x += System.Math.Min (System.Math.Max (j, x), b);
		}
		return x;
	}
	
	static void Main () {
		for (int i = 0; i < 50; i++)
			DoIt (1, 5);
	}
}