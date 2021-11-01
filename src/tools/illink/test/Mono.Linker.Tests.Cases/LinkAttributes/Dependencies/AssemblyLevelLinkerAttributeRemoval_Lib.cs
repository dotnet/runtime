// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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