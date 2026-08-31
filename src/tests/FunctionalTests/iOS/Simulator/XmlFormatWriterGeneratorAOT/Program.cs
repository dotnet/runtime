// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

public static class Program
{
    [DllImport("__Internal")]
    public static extern void mono_ios_set_summary (string value);

    public static async Task<int> Main(string[] args)
    {
        mono_ios_set_summary($"Starting functional test");

        var ds = new DataContractSerializer (typeof (IEnumerable<int>));
        using (var xw = XmlWriter.Create (System.IO.Stream.Null))
	        ds.WriteObject (xw, new int [] { 1, 2, 3 });

        Console.WriteLine("Done!");
        await Task.Delay(5000);

        return 42;
    }
}
