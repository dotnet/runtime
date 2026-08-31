using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class Test {

	public static int test_0_jagged ()
	{
		int[][] j2 = new int [3][];

		// does not work 
		// j2 [0] = new int[] {1, 2, 3};
		// j2 [1] = new int[] {1, 2, 3, 4, 5, 6};
		// j2 [2] = new int[] {1, 2, 3, 4, 5, 6, 7, 8, 9};

		j2 [0] = new int[3];
		j2 [1] = new int[6];
		j2 [2] = new int[9];

		for (int i = 0; i < j2.Length; i++)
			for (int j = 0; j < (i+1)*3; j++)
				j2 [i][j] = j;

		for (int i = 0; i < j2.Length; i++)
			for (int j = 0; j < (i+1)*3; j++)
				if (j2 [i][j] != j)
					return 1;
		return 0;
	}

	public static int test_0_stest ()
	{
		string[] sa = new string[32];

		sa [0] = "This";
		sa [2] = "is";
		sa [10] = "a";
		sa [20] = "stupid";
		sa [21] = "Test";

		for (int i = 0; i < sa.Length; i++){
			if (sa [i] != null)
				Console.WriteLine (sa [i]);
		}
		
		return 0;
	}
	
	public static int test_0_atest2 ()
	{
		int[,] ia = new int[32,32];

		for (int i = 0; i <ia.GetLength (0); i++)
			ia [i,i] = i*i;

		for (int i = 0; i <ia.GetLength (0); i++)
			if (ia [i,i] != i*i)
				return 1;

		for (int i = 0; i <ia.GetLength (0); i++)
			ia.SetValue (i*i*i, i, i);

		for (int i = 0; i <ia.GetLength (0); i++)
			if ((int)ia.GetValue (i, i) != i*i*i)
				return 1;

		return 0;
	}
	
	public static int test_0_atest ()
	{
		int[] ia = new int[32];

		for (int i = 0; i <ia.Length; i++)
			ia [i] = i*i;
		
		for (int i = 0; i <ia.Length; i++)
			if (ia [i] != i*i)
				return 1;
		
		if (ia.Rank != 1)
			return 2;

		if (ia.GetValue (2) == null)
			return 3;

		for (int i = 0; i <ia.Length; i++)
			ia.SetValue (i*i*i, i);

		for (int i = 0; i <ia.Length; i++)
			if ((int)ia.GetValue (i) != i*i*i){
				Console.WriteLine ("Crap: " + i + " " + (int) ia.GetValue (i) );

				return 4;
			}
		
		return 0;
	}

	enum Foo { a, b };
	public static int test_0_enum_array_casting () {
        var x = new Foo[10];
		try {
			var y = (IReadOnlyList<int>)(object)x;
		} catch (Exception e) {
			return 1;
		}
		try {
			var y = (IList<int>)(object)x;
		} catch (Exception e) {
			return 2;
		}

		try {
			var y = (ICollection<int>)(object)x;
		} catch (Exception e) {
			return 3;
		}

		try {
			var y = (IEnumerable<int>)(object)x;
		} catch (Exception e) {
			return 4;
		}

		try {
			var y = (IReadOnlyCollection<int>)(object)x;
		} catch (Exception e) {
			return 5;
		}
		return 0;
	}

	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Test), args);
	}
}


