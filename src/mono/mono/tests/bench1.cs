using System;

public class Benchmark {

	public static int Run ()
	{
		int count = 32;
		int[] fibs = new int [count + 1];
		int index;
		int index2;
		int temp;

		fibs[0] = 1;
		fibs[1] = 1;
		for(index = 2; index <= count; ++index)
		{
			fibs[index] = fibs[index - 2] + fibs [index - 1];
		}

		for(index = 0; index <= count; ++index)
		{
			for(index2 = 1; index2 <= count; ++index2)
			{
				if(fibs[index2 - 1] < fibs[index2])
				{
					int ti = index2 - 1;
					temp = fibs[ti];
					fibs[ti] = fibs[index2];
					fibs[index2] = temp;
				}
			}
		}
		
		return fibs [0];
	}

	public static int Main () {
		for (int i = 0; i < 1000000; i++)
			if (Run () != 3524578)
				return 1;

		return 0;
	}
}


