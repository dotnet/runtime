using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

//This class is used to fake out the complier. It should be identical to ..\Tests except for visability rules

public class TestClassFrameworkInternal
{
	public static int methodPublic()
	{
		return 7;
	}

	private static int methodPrivate()
	{
		return 8;
	}

	internal static int methodInternal()
	{
		return 9;
	}
}
