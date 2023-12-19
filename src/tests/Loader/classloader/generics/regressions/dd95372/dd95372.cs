// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class my
{
	[Fact]
	public static int TestEntryPoint()
	{
		try
		{
		    Type t = typeof(test<>);
		}
		catch (Exception e)
		{
		    Console.WriteLine("FAIL: {0}", e.Message);
		    return 99;
		}
        
		Console.WriteLine("PASS");
		return 100;
	}

}

public class test<T> : TopLevel<T>
    where T : IMember<IMember<T, object>, object>
{ }



public interface TopLevel<T> :
ISubIface<T, object>
where T :
    IMember<IMember<T, object>, object>
{ }

public interface ISubIface<T, U /*=object*/> :
IMembers<T, IMember<T, U /*=object*/>, object>
where T :
    IMember<IMember<T, U /*=object*/>, object>
{ }

public interface IMembers<T, U /*=IMember<T, U>*/, V /*=object*/>
where T :
    IMember<U /*=IMember<T, U>*/, V /*=object*/>
{ }

public interface IMember<T, U>
{ }


