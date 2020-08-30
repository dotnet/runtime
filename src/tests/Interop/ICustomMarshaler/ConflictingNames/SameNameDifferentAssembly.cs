// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using TestLibrary;

public class RunInALC
{
    public static int Main(string[] args)
    {
        try
        {
            Assert.AreEqual(123, new CustomMarshalers.CustomMarshalerTest().ParseInt("123"));
            Assert.AreEqual(123, new CustomMarshalers2.CustomMarshalerTest().ParseInt("123"));
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 101;
        }
    }
}
