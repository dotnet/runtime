using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

public class CritConstructorClass
{

	public int a;

	[SecurityCritical]
	public CritConstructorClass(int input)
	{
		a = input;
	}
}
