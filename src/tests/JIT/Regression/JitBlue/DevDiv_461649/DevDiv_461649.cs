// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using Xunit;


namespace XSLTest
{
    public class Program
    {
        // In this test a dynamic method with tail-prefixed call is created.
        // One of the locals is not explicitly initialized but a flag to init locals is set.
        // (That never happens in normal C# methods due to C# definite assignment rules.)
        // The jit performs an optimization transforming the tail call into a loop.
        // The bug was that the local was only zero-initialized for the first iteration of the loop.

        [Fact]
        public static int TestEntryPoint()
        {
            string inputXml = "Input.xml";
            string inputXsl = "Transform.xsl";

            return DotNetXslCompiledTransform(inputXml, inputXsl);
        }

        private static int DotNetXslCompiledTransform(string inputXml, string inputXsl)
        {
            XslCompiledTransform transform = new XslCompiledTransform();
            transform.Load(inputXsl);

            StringWriter stringWriter = new StringWriter();
            XmlWriter writer = new XmlTextWriter(stringWriter);

            transform.Transform(inputXml, null, writer);

            string transformResult = stringWriter.ToString();
            if (transformResult == "<!--20.0 20.0 20.0 20.0 20.0--> 40 40 40 40 40")
            {
                Console.WriteLine("SUCCESS");
                return 100;
            }
            else
            {
                Console.WriteLine("FAILURE");
                return 0;
            }
        }
    }
}

