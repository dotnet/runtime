using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

[assembly:SecurityCritical]

public class PlatformCode
{
	public static void Begin(string testName)
	{
		TestLibrary.TestFramework.BeginTestCase(testName);
	}
}
