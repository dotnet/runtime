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
		if (sbah + obj.bah + obj.amethod () == 8)
			return 0;
		return 1;
	}
}


