using System;
using Mono.Linker.Tests.Cases.TestAttributeLib;

[assembly: MyAttribute]
[module: MyAttribute]

namespace Mono.Linker.Tests.Cases.TestAttributeLib
{
	[System.AttributeUsage (System.AttributeTargets.All, Inherited = false, AllowMultiple = true)]
	public sealed class MyAttribute : System.Attribute
	{
		public MyAttribute ()
		{
		}
	}

	public class Foo
	{
	}
}