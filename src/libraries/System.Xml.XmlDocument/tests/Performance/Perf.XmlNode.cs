// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using Microsoft.Xunit.Performance;

namespace XmlDocumentTests.XmlNodeTests
{
    public class Perf_XmlNode
    {
        private const int innerIterations = 10000;

        [Benchmark]
        public void GetAttributes()
        {
            // Setup
            XmlAttributeCollection attr;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<a attr1='test' attr2='test2' />");
            XmlNode node = doc.DocumentElement;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        attr = node.Attributes; attr = node.Attributes; attr = node.Attributes;
                        attr = node.Attributes; attr = node.Attributes; attr = node.Attributes;
                        attr = node.Attributes; attr = node.Attributes; attr = node.Attributes;
                    }
                }
            }
        }

        [Benchmark]
        public void GetValue()
        {
            // Setup
            string value;
            var doc = new XmlDocument();
            doc.LoadXml("<a attr1='test' attr2='test2' />");
            XmlNode node = doc.DocumentElement;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        value = node.Value; value = node.Value; value = node.Value;
                        value = node.Value; value = node.Value; value = node.Value;
                        value = node.Value; value = node.Value; value = node.Value;
                    }
                }
            }
        }
    }
}
