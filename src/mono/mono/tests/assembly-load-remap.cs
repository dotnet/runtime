using System;
using System.IO;
using System.Reflection;

public class Tests
{
	public static void Main (string[] args)
	{
		if (args.Length != 1)
			throw new Exception ("Missing commandline args.");

		string versionLoad = args [0];
		var asm = Assembly.Load ("Microsoft.Build.Framework, Version=" + versionLoad + ", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

		if (asm == null)
			throw new Exception ("Assembly couldn't be loaded.");
	}
}
