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

		for (int a = 0; a != arr.Length; a++)
			Console.Write(arr[a].ToString("x") + " ");		

		Console.WriteLine();
	
		Console.WriteLine("test-ok");

		return 0;
	}
}


