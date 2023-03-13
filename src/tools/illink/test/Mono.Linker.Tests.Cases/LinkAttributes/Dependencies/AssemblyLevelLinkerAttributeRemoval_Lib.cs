// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
