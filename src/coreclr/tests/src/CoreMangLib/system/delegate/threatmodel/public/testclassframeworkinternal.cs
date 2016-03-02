// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
