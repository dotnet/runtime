using System;

class Class1 {
	static int Main(string[] args)
	{
		string s1 = "original";

		try {
			bool huh = s1.StartsWith(null);
		} catch (ArgumentNullException) {
		}

		if (s1.StartsWith("o")){
			return 0;
		} else {
			return 1;
		}
	}
}


