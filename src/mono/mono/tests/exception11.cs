using System;

public class Test {

	public static int Main (string[] args) {

		int c = 0;
		try
                        {
				c = 0;
                        }
		catch (Exception e)
                        {
				Console.WriteLine("Exception: {0}", e.Message);
                        }
		finally
                        { 
				Console.WriteLine("Finally... {0}", c++);
                        }

		if (c != 1)
			return 1;
		
		return 0;
	}
}
