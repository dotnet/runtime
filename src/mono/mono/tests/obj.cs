using System;

public class TestObj {
	static public int sbah = 5;
	public int bah = 1;
	public int boh;

	public TestObj () {
		boh = 2;
	}
	public int amethod () {
		return boh;
	}
	public static int Main () {
		TestObj obj = new TestObj ();
		TestObj clone;
		
		if (sbah + obj.bah + obj.amethod () != 8)
			return 1;

		clone = (TestObj)obj.MemberwiseClone ();

		if (clone.boh != 2)
			return 1;
		
		return 0;
	}
}


