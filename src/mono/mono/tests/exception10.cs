using System;

public class Test {

	public static int Main (string[] args) {

		int c = 0;
		try
                        {
				throw new Exception("Test exception");
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
