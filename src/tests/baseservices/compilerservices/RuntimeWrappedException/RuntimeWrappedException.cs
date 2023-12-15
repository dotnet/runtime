// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Xunit;

public class Test 
{
    [Fact]
    public static int TestEntryPoint()
    {
        int retVal = 0;
        var thrower = new StringThrowerClass();
        try
        {
            thrower.InstanceMethod();
        }

        catch (RuntimeWrappedException ex)
        {

	    if ( !ex.WrappedException.ToString().Contains("Inside StringThrower") )
	    {
//		Console.WriteLine("Incorrect exception and/or message. Expected RuntimeWrappedException: An object that does not derive "+
//				  "from System.Exception has been wrapped in a RuntimeWrappedException.\n But actually got: " + ex.InnerException);
		return -1;
	    }
            
            StreamingContext ctx;
            
            retVal = 100;


        }
	catch (Exception ex)
	{
//	   Console.WriteLine("Incorrect exception thrown. Expected RuntimeWrappedException, but actually got: " + ex);
	   retVal = -2;
	}


        return retVal;


    }
}
