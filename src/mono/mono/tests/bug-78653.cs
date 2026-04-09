using System;

public class Tests {
	public static int[] ThrowAnException ()   {
		int[] arr = new int [10];
		int k = arr [11];
		Console.WriteLine ("Test failed");
		return arr;
	}
	public static int Main ( String[] args )   {
		try {
			int[] arr = ThrowAnException ();
			Console.WriteLine ("Test failed, really!");
			return 1;
		} catch (Exception e) {
			Console.WriteLine ("Test passed");
			return 0;
		}
	}
}
