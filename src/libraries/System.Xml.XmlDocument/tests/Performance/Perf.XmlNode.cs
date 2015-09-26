// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using Microsoft.Xunit.Performance;

namespace XmlDocumentTests.XmlNodeTests
{
    public class Perf_XmlNode
    {
        [Benchmark]
        public void GetAttributes()
        {
            XmlAttributeCollection attr;
            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup
                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<a attr1='test' attr2='test2' />");
                XmlNode node = doc.DocumentElement;

                // Actual perf testing
                using (iteration.StartMeasurement())
                {
                    attr = node.Attributes; attr = node.Attributes; attr = node.Attributes;
                    attr = node.Attributes; attr = node.Attributes; attr = node.Attributes;
                    attr = node.Attributes; attr = node.Attributes; attr = node.Attributes;
                }
            }
        }

        [Benchmark]
        public void GetValue()
        {
            string value;
            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup
                var doc = new XmlDocument();
                doc.LoadXml("<a attr1='test' attr2='test2' />");
                XmlNode node = doc.DocumentElement;

                // Actual perf testing
                using (iteration.StartMeasurement())
                {
                    value = node.Value; value = node.Value; value = node.Value;
                    value = node.Value; value = node.Value; value = node.Value;
                    value = node.Value; value = node.Value; value = node.Value;
                }
            }
        }
    }
}
