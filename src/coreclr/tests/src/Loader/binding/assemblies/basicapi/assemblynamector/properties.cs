// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;
using System.IO;
using System.Globalization;

public class AssemblyNameCtor_Basic
{
	
	public static int Main()
	{
		bool bFail = false;
		//int result = 0;
		
		try
		{
			
			//Assembly assm1, assm2;

			AssemblyName asmName1 = new AssemblyName ();
			asmName1.Name = "server1";

			AssemblyName asmName2 = new AssemblyName("server1");


			Console.WriteLine (asmName1);
			Console.WriteLine (asmName2);
			if (asmName1==asmName2)
			{
				Console.WriteLine ("test will fail: asmName1==asmName2");
				bFail = true;
			}
			

			Console.WriteLine (asmName1.ToString());
			Console.WriteLine (asmName2.ToString());
			if (asmName1.ToString()!=asmName2.ToString())
			{
				Console.WriteLine ("test will fail: asmName1.ToString()!=asmName2.ToString()");
				bFail = true;
			}

			Console.WriteLine ("PublicKeyToken = " + asmName1.GetPublicKeyToken());
			Console.WriteLine ("PublicKeyToken = " + asmName2.GetPublicKeyToken());			
			if (asmName1.GetPublicKeyToken()!=asmName2.GetPublicKeyToken())
			{
				Console.WriteLine ("test will fail: asmName1.ToString()!=asmName2.ToString()");
				bFail = true;
			}


			Console.WriteLine ("version = " + asmName1.Version);
			Console.WriteLine ("version = " + asmName2.Version);
			if (asmName1.Version!=asmName2.Version)
			{
				Console.WriteLine ("test will fail: asmName1.Version!=asmName2.Version");
				bFail = true;
			}
/*
			if (asmName1.Version!="0.0.0.0")
			{
				Console.WriteLine ("test will fail: asmName1.Version.ToString()!=\"0.0.0.0\"");
				bFail = true;
			}
*/
		
				
			Console.WriteLine ("******");

			//Console.WriteLine (asmName2.GetPublicKeyToken().ToString());
			//Console.WriteLine (asmName2.GetPublicKeyToken());

			Console.WriteLine ("bFail = {0}", bFail);
			if (bFail == true)
			{
				Console.WriteLine ("test failed");
				return 101;
			}			
			

		}
		catch (Exception e)
		{
			Console.WriteLine ("unexpected exception");
			Console.WriteLine (e);
			Console.WriteLine ("test failed");
			return 101;
		}

		Console.WriteLine ("test passed");
		return 100;
	}

}