// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class Cls_1<T,U>{}
public class Cls_2 : Cls_1<I3,Cls_2>{}

public interface I1
{
	int Foo<T>() where T : I2;
}


public interface I2 : I1
{
	  new int Foo<T>() where T : Cls_1<I3,Cls_2>;
}

public interface I3 : I2
{
	 new int Foo<T>();
}

