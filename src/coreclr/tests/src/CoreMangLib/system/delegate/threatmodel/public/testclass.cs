// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

//This class is used to fake out the complier. It should be identical to ..\Tests except for visability rules

public class TestClassPublic
{
	public static int methodPublic()
	{
		return 1;
	}

	private static int methodPrivate()
	{
		return 2;
	}

	internal static int methodInternal()
	{
		return 3;
	}
}

public class TestClassInternal
{
	public static int methodPublic()
	{
		return 4;
	}

	private static int methodPrivate()
	{
		return 5;
	}

	internal static int methodInternal()
	{
		return 6;
	}
}
