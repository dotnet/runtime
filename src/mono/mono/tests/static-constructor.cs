
public class GenRandom {
    	static int last = 42;
    	static int burp;

	static GenRandom () {
		/* 
		 * This is really at test of the compiler: it should init
		 * last before getting here.
		*/
		if (last != 42)
			burp = 5;
		else
			burp = 4;
	}
	public static int Main() {
		if (last != 42)
			return 1;
		if (burp != 4)
			return 1;
		return 0;
	}
}
