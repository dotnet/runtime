using System;

public class fall_through {

	public static void Main(string[] args)
	{
		foreach(string str in args)
		{
			Console.WriteLine(str);

			switch(str)
			{
				case "test":
					Console.WriteLine("passed");
   					continue;

				default:
					return;
	    		}
		}
	}
}

