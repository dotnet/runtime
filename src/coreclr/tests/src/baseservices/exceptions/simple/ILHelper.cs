// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public class ILHelper
{
	public static bool Int32Overflow()
	{
		int i = Int32.MaxValue;
		int j = 2;
		int k = i + j;
		return true;
	}
	public static bool Int64Overflow()
	{
		Int64 i = Int64.MaxValue;
		Int64 j = 2;
		Int64 k = i * j;
		return true;
	}
}
