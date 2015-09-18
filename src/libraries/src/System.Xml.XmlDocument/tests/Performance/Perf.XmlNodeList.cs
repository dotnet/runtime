// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using Microsoft.Xunit.Performance;

namespace XmlDocumentTests.XmlNodeListTests
{
    public class Perf_XmlNodeList
    {
        [Benchmark]
        public void Enumerator()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup
                var doc = new XmlDocument();
                doc.LoadXml("<a><sub1/><sub1/><sub2/><sub1/><sub2/><sub1/><sub2/><sub1/><sub2/><sub1/><sub2/><sub2/></a>");
                XmlNodeList list = doc.DocumentElement.ChildNodes;

                // Actual perf testing
                using (iteration.StartMeasurement())
                    foreach (var element in list) { }
            }
        }

        [Benchmark]
        public void GetCount()
        {
            int count;
            foreach (var iteration in Benchmark.Iterations)
            {
                // Setup
                var doc = new XmlDocument();
                doc.LoadXml("<a><sub1/><sub2/></a>");
                XmlNodeList list = doc.DocumentElement.ChildNodes;

                // Actual perf testing
                using (iteration.StartMeasurement())
                {
                    count = list.Count; count = list.Count; count = list.Count;
                    count = list.Count; count = list.Count; count = list.Count;
                    count = list.Count; count = list.Count; count = list.Count;
                }
            }
        }
    }
}
