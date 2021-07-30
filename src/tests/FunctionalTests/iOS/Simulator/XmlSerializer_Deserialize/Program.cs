// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public static class Program
{
    [DllImport("__Internal")]
    public static extern void mono_ios_set_summary (string value);

    public static async Task<int> Main(string[] args)
    {
        mono_ios_set_summary($"Starting functional test");

        using StringReader stringReader = new StringReader(@"<?xml version=""1.0"" encoding=""UTF-8""?>
            <TestClass>
                <TestData>sample</TestData> 
            </TestClass>");

        var serializer = new XmlSerializer(typeof(TestClass));
        TestClass obj = (TestClass)serializer.Deserialize(stringReader);

        var result = obj.TestData == "sample" ? 42 : 1;

        Console.WriteLine("Done!");
        await Task.Delay(5000);

        return result;
    }

    [XmlType("TestClass", AnonymousType = true, Namespace = "")]
    public class TestClass
    {
        public TestClass()
        {
        }

        [XmlElement("TestData")]
        public string TestData { get; set; }
    }
}
