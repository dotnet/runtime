using System;

public class A
{
	public static int Main()
	{

		long iNew;
		long iTotal;

		iTotal = 0;
		
		iNew = System.Threading.Interlocked.Add( ref iTotal, 2);

		Console.WriteLine("iNew = {0} iTotal = {1}", iNew, iTotal);

		if (iNew == iTotal)
		{
			Console.WriteLine("PASS");
			return 100;
		}
		else
		{
			Console.WriteLine("FAIL (iNew and iTotal should be equal)");
			return 0;
		}
	}
}
