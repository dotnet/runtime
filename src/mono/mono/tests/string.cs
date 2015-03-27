using System;

public class TestString {

	public static int Main() {
    		string a = "ddd";
	    	string b = "ddd";
		string c = "ddda";
		if (a != b)
			return 1;
		if (c != String.Concat(b , "a"))	
			return 2;
		if (!System.Object.ReferenceEquals(a, b))
			return 3;
		if (System.Object.ReferenceEquals(c, String.Concat(b, "a")))
			return 4;
		if (!Object.ReferenceEquals (String.Empty, ""))
			return 5;
	    	return 0;
	}
}
