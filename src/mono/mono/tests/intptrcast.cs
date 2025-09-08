using System;

public class Test {

        public static int Main () {
                IntPtr ip = (IntPtr)1;

		if (ip.ToString () != "1")
			return 1;

                return 0;
        }
}

