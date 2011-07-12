using System;


namespace BufferTest
{
	class Test
	{
        public static int Main (string[] args)
        {
	        int size = 32;
	        byte[] array = new byte[size];


	        for (byte i = 0; i < size; i++) array[i] = i;

		Buffer.BlockCopy (array, 4, array, 5, 20);

		for (byte i = 0; i < size; i++) {
			byte expected;
			if (i > 4 && i < 25)
				expected = (byte)(i - 1);
			else
				expected = i;
			if (array [i] != expected) {
				Console.WriteLine ("error at " + i + " expected " + expected + " but got " + array [i]);
				return 1;
			}
		}

		return 0;
        }

	}
}
