public class Test {

	public static int Main () {
		int[] ia = new int[32];
		
		for (int i = 0; i <ia.Length; i++)
			ia [i] = i*i;

		for (int i = 0; i <ia.Length; i++)
			if (ia [i] != i*i)
				return 1;
		
		if (ia.Rank != 1)
			return 1;

		// test Clone


		if (ia.GetValue (2) == null)
			return 1;
		
		return 0;
	}
}


