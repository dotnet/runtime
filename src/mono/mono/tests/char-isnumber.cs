using System;

class T {
	static int Main() {
		char[] chars = new char[] 
			{'0',  'F',  'f',  'x',   '1',  'n',   'a'};
		bool[] results = new bool[] 
			{true, false, false, false, true, false, false};

		for (int i = 0; i < chars.Length; ++i) {
			if (Char.IsNumber (chars [i]) != results [i]) {
				Console.WriteLine ("Char '{0}' failed", chars [i]);
				return 1;
			}
		}
		return 0;
	}
}
