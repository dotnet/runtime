using System;
using System.Runtime.InteropServices;

public class Test {

	[DllImport("cygwin1.dll", EntryPoint="puts", CharSet=CharSet.Ansi)]
	public static extern int puts (string name);

	public static int jagged ()
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

	public static int stest ()
	{
		string[] sa = new string[32];

		sa [0] = "This";
		sa [2] = "is";
		sa [10] = "a";
		sa [20] = "stupid";
		sa [21] = "Test";

		for (int i = 0; i < sa.Length; i++)
			if (sa [i] != null)
				puts (sa [i]);
		
		return 0;
	}
	
	public static int atest2 ()
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
	
	public static int atest ()
	{
		int[] ia = new int[32];

		for (int i = 0; i <ia.Length; i++)
			ia [i] = i*i;
		
		for (int i = 0; i <ia.Length; i++)
			if (ia [i] != i*i)
				return 1;
		
		if (ia.Rank != 1)
			return 1;

		if (ia.GetValue (2) == null)
			return 1;

		for (int i = 0; i <ia.Length; i++)
			ia.SetValue (i*i*i, i);

		for (int i = 0; i <ia.Length; i++)
			if ((int)ia.GetValue (i) != i*i*i)
				return 1;
		
		return 0;
	}
	
	public static int boxtest ()
	{
		int i = 123;
		object o = i;
		int j = (int) o;

		if (i != j)
			return 1;
		
		return 0;
	}

	public static int Main () {
	       
		if (boxtest () != 0)
			return 1;
	       
		if (atest () != 0)
			return 1;
		
		if (atest2 () != 0)
			return 1;
		if (atest2 () != 0)
			return 1;
		
		if (stest () != 0)
			return 1;
		
		if (jagged () != 0)
			return 1;
		
		
		return 0;
	}
}


