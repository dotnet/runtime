// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

[AttributeUsage(AttributeTargets.Method)]
public class MyAttribute : Attribute
{
	public Type[] Types;
}

public class Test
{
	[MyAttribute(Types = new Type[]{typeof(string), typeof(void)})]
	public static int Main(String[] args) { return 0; }
}
