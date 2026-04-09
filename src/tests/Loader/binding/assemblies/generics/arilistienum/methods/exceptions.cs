// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//array<T> IList properties

using System;
using System.IO;
using System.Reflection;
using System.Collections;
using Xunit;

public class GenClass<T>
{
	public T fld;
}

public class PropsArIList  
{

	[Fact]
	public static int TestEntryPoint()  
	{

		int result = 0;
		int i;
		try
		{
			//Part 1 - GenClass <int>
			Console.WriteLine("\ntest GenClass<int>");

			GenClass<int> obj1;
			obj1 = new GenClass<int>();
			obj1.fld = 3;
			Console.WriteLine (obj1.fld);

			GenClass<int>[] arGen;
			arGen = new GenClass<int>[5];
			
			for (i=0;i<5;i++) 
			{
				arGen[i] = new GenClass<int>();
				arGen[i].fld = i;
				Console.Write (arGen[i].fld + "\t");
			}
			Console.WriteLine();

			IList interf1 = (IList) arGen;

			try 
			{
				interf1.Add(obj1);
			} 
			catch (NotSupportedException e)
			{
				Console.WriteLine ("expected: " + e);
			}

			try 
			{
				interf1.Insert(1, obj1);
			} 
			catch (NotSupportedException e)
			{
				Console.WriteLine ("expected: " + e);
			}

			try 
			{
				interf1.Remove(arGen[0]);
			} 
			catch (NotSupportedException e)
			{
				Console.WriteLine ("expected: " + e);
			}

			try
			{
				interf1.RemoveAt (1);
			}
			catch (NotSupportedException e)
			{
				Console.WriteLine ("expected: " + e);
			}

			//Part 2 - GenClass <string>
			Console.WriteLine("\ntest GenClass<string>");

			GenClass<string> obj2;
			obj2 = new GenClass<string>();
			obj2.fld = "name";
			Console.WriteLine (obj2.fld);

			GenClass<string>[] arGenS;
			arGenS = new GenClass<string>[5];
			string aux = "none";
			for (i=0;i<5;i++) 
			{
				arGenS[i] = new GenClass<string>();
				aux = Convert.ToString(i);
				arGenS[i].fld = aux;
				Console.Write (arGenS[i].fld + "\t");
			}
			Console.WriteLine();

			IList interf2 = (IList) arGenS;

			try 
			{
				interf2.Add(obj2);
			} 
			catch (NotSupportedException e)
			{
				Console.WriteLine ("expected: " + e);
			}

			try 
			{
				interf2.Insert(1, obj2);
			} 
			catch (NotSupportedException e)
			{
				Console.WriteLine ("expected: " + e);
			}

			try 
			{
				interf2.Remove(arGenS[0]);
			} 
			catch (NotSupportedException e)
			{
				Console.WriteLine ("expected: " + e);
			}

			try
			{
				interf2.RemoveAt (1);
			}
			catch (NotSupportedException e)
			{
				Console.WriteLine ("expected: " + e);
			}

			result = 100; //pass
	
		}
		catch (Exception e)
		{
			Console.WriteLine ("unexpected exception..");
			Console.WriteLine (e); 
			Console.WriteLine ("test failed");
			return 101;
		}

		if (result==100) Console.WriteLine ("test passed");
		else Console.WriteLine ("test failed");

		return result;
	}
}
	
