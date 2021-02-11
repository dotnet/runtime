using System;
using System.IO;

public class Test {

	public static int Main () {
		FileStream s = new FileStream ((IntPtr)1, FileAccess.Write);
		StreamWriter sw = new StreamWriter (s);
		string ts = "This is another test";

		sw.WriteLine (ts);

		sw.WriteLine (123456);

		return 0;
	}
}


