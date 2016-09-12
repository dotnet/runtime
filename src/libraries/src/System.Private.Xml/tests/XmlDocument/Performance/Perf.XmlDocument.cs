// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using Microsoft.Xunit.Performance;

namespace XmlDocumentTests.XmlDocumentTests
{
    public class Perf_XmlDocument
    {
        private const int innerIterations = 10000;

        [Benchmark]
        public void Create()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        new XmlDocument(); new XmlDocument(); new XmlDocument();
                        new XmlDocument(); new XmlDocument(); new XmlDocument();
                        new XmlDocument(); new XmlDocument(); new XmlDocument();
                    }
                }
            }
        }

        [Benchmark]
        public void LoadXml()
        {
            XmlDocument doc = new XmlDocument();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                        doc.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");
        }

        [Benchmark]
        public void GetDocumentElement()
        {
            XmlNode element;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        element = doc.DocumentElement; element = doc.DocumentElement; element = doc.DocumentElement;
                        element = doc.DocumentElement; element = doc.DocumentElement; element = doc.DocumentElement;
                        element = doc.DocumentElement; element = doc.DocumentElement; element = doc.DocumentElement;
                    }
                }
            }
        }
    }
}
