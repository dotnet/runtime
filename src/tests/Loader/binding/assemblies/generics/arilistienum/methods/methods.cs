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
		int i, index = 0;
		bool bRes = false;
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

			Console.WriteLine ("testing IList.Contains");
			bRes = interf1.Contains (arGen[2]);
			if (bRes!=true) 
			{
				Console.WriteLine ("unexpected result: {0} \n test failed", bRes);
				return 110;
			}
			bRes = interf1.Contains (obj1);
			if (bRes!=false) 
			{
				Console.WriteLine ("unexpected result: {0}  \n test failed", bRes);
				return 110;
			}

			Console.WriteLine ("testing IList.IndexOf");
			index = interf1.IndexOf (arGen[2]);
			if (index!=2)
			{
				Console.WriteLine ("unexpected result: {0} \n test failed", index);
				return 110;
			}

			Console.WriteLine ("testing IList.Clear");
			interf1.Clear();

			for (i=0;i<5;i++)
			{
				if (arGen[i]!=null) 
				{
					Console.WriteLine ("unexpected result: element {0} is not null \n test failed", i);
					return 110;
				}

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

			Console.WriteLine ("testing IList.Contains");
			bRes = interf2.Contains (arGenS[2]);
			if (bRes!=true) 
			{
				Console.WriteLine ("unexpected result: {0} \n test failed", bRes);
				return 110;
			}
			bRes = interf2.Contains (obj2);
			if (bRes!=false) 
			{
				Console.WriteLine ("unexpected result: {0}  \n test failed", bRes);
				return 110;
			}

			bRes = interf2.Contains (obj1);
			if (bRes!=false) 
			{
				Console.WriteLine ("unexpected result: {0}  \n test failed", bRes);
				return 110;
			}


			Console.WriteLine ("testing IList.IndexOf");
			index = interf2.IndexOf (arGenS[2]);
			if (index!=2)
			{
				Console.WriteLine ("unexpected result: {0} \n test failed", index);
				return 110;
			}

			Console.WriteLine ("testing IList.Clear");
			interf2.Clear();

			for (i=0;i<5;i++)
			{
				if (arGenS[i]!=null) 
				{
					Console.WriteLine ("unexpected result: element {0} is not null \n test failed", i);
					return 110;
				}

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
	
