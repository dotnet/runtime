using System;

public class TryTest {
        public static void ThrowException() {
                throw new Exception();
        }

        public static int Main() {
		int state = 0;

		try {
			ThrowException();
			try {
				Console.WriteLine("In try block");
			} catch (Exception e) {
				state = 1;
				Console.WriteLine("------------------------");
				Console.WriteLine(e);
				Console.WriteLine("------------------------");
			}
		} catch {
			state = 2;
		}

		if (state != 2)
			return 1;

		Console.WriteLine("OK");
		return 0;
        }
}
