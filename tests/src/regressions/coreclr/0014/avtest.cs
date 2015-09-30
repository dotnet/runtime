using System;

class ReflectObj
{
	public static int Main( String [] str )
	{
		Random rand = null;
		try
		{
			rand.Next();
		}
		catch (NullReferenceException)
		{
			Console.WriteLine("Got expected NullReferenceException");
			Console.WriteLine("PASS");
			return 100;
		}
		catch (Exception e)
		{
			Console.WriteLine("Got unexpected exception: {0}", e);
		}

		Console.WriteLine("FAIL");
		return 0;
	}
}

