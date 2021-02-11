using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.ComponentModel;

public class LastClass
{
	public static int DefaultParam ([DefaultParameterValue (0x99)] int a, int b) { return 0; }

	public const int ConstField = 0x44; 
	public const object ConstField2 = null; 
	public const string ConstField3 = "hello world"; 

	/* LAMESPEC You can't define a default value using MSIL*/
	public int ConstProp { get; set; }

	public static void Main ()
	{
	
	}
}