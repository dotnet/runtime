// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class NonSerializableClass
{
    override public String ToString()
    {
	return "Fred";
    }
}

public class DataHelper
{
    public static String msg1 = "Exception from DataHelper.ThrowWithData()";
    public static String msg2 = "Exception from DataHelper.ThrowWithNonSerializableData()";

    public static String key1 = "'Twas Brillig and the slithy toves";
    public static String val1 = "Did gyre and gimble in the wabe";
    public static String key2 = "answer";
    public static int val2 = 42;
    public static String key3 = "nonserializable";
    public static NonSerializableClass val3 = new NonSerializableClass();
    
    public void ThrowWithData()
    {
	Exception e = new Exception(msg1);
	e.Data[key1] = val1;
	e.Data[key2] = val2;
	throw (e);
    }

    public void ThrowWithNonSerializableData()
    {
	Exception e = new Exception(msg2);
	e.Data[key3] = val3;
	throw (e);
    }
}

    
