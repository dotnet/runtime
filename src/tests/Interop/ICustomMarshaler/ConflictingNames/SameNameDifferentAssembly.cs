// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Xunit;

public class RunInALC
{
    public static int Main()
    {
        try
        {
            Assert.Equal(123, new CustomMarshalers.CustomMarshalerTest().ParseInt("123"));
            Assert.Equal(123, new CustomMarshalers2.CustomMarshalerTest().ParseInt("123"));
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 101;
        }
    }
}
