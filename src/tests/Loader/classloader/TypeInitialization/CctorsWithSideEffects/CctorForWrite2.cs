// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Showed a JIT importer bug where a tree would be constructed for
// call / stsfld and then the type initialization call inserted before
// the entire tree.  The call needs to happen before type initialization.

using System;
using System.Runtime.CompilerServices;

public class CorrectException : Exception
{
}

public class CCC
{
    public static int Main() => ClassCallingCctor<object>.Test();
}

public class ClassCallingCctor<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Call() => throw new CorrectException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static int Test()
    {
	try
	{
	    ClassWithCctor<T>.StaticField = Call();
	}
	catch (CorrectException)
	{
	    return 100;
	}
	catch
	{
	}
	return 1;
    }
}

public class ClassWithCctor<T>
{
    public static int StaticField;
    static ClassWithCctor() => throw new Exception();
}
