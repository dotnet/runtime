using System;
using System.IO;

public class BinaryWrTest {
	public static int Main () {
		MemoryStream mr = new MemoryStream();
		BinaryWriter wr = new BinaryWriter(mr);

		wr.Write ((byte) 1);
		wr.Write ((int) 1);
		wr.Write ((int) -1);

		byte [] arr = mr.ToArray();

		Console.Write ("Array (should be: 1 1 0 0 0 ff ff ff ff): ");

		for (int a = 0; a != arr.Length; a++)
			Console.Write(arr[a].ToString("x") + " ");		

		Console.WriteLine();

		if (arr.Length != 9)
			return 4;

		if (arr[0] != 1) 
			return 1;

		if (arr[1] != 1 && arr[2] != 0 && arr[3] != 0 && arr[4] != 0)
			return 2;

		if (arr[5] != 0xff && arr[6] != 0xff && arr[7] != 0xff && arr[8] != 0xff)
			return 3;
	
		Console.WriteLine("test-ok");

		return 0;
	}
}


