using System;
using System.IO;

public class Test {

	public static int Main () {
		byte[] buf = new byte [20];
		int i;
		FileStream s = new FileStream ("stest.dat", FileMode.OpenOrCreate, FileAccess.ReadWrite);
		for (i=0; i < 20; ++i)
			buf [i] = 65;
		s.Write (buf, 0, 20);
		s.Position = 0;
		for (i=0; i < 20; ++i)
			buf [i] = 66;
		s.Read (buf, 0, 20);
		for (i=0; i < 20; ++i)
			if (buf [i] != 65)
				return 1;
		
		return 0;
	}
}


