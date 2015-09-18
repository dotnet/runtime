// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using Microsoft.Xunit.Performance;

namespace XmlDocumentTests.XmlDocumentTests
{
    public class Perf_XmlDocument
    {
        [Benchmark]
        public void Create()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    new XmlDocument(); new XmlDocument(); new XmlDocument();
                    new XmlDocument(); new XmlDocument(); new XmlDocument();
                    new XmlDocument(); new XmlDocument(); new XmlDocument();
                }
            }
        }

        [Benchmark]
        public void LoadXml()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup
                var doc = new XmlDocument();

                // Actual perf testing
                using (iteration.StartMeasurement())
                    doc.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");
            }
        }

        [Benchmark]
        public void GetDocumentElement()
        {
            XmlNode element;
            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup
                var doc = new XmlDocument();
                doc.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");

                // Actual perf testing
                using (iteration.StartMeasurement())
                {
                    element = doc.DocumentElement; element = doc.DocumentElement; element = doc.DocumentElement;
                    element = doc.DocumentElement; element = doc.DocumentElement; element = doc.DocumentElement;
                    element = doc.DocumentElement; element = doc.DocumentElement; element = doc.DocumentElement;
                }
            }
        }
    }
}
