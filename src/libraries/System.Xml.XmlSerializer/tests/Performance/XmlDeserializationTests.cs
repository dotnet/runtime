// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;
using SerializationTypes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace System.Runtime.Tests.Performance
{
    public class XmlDeserializationTests
    {
        public void RunXmlDeserializationTest(object obj, int iterations)
        {
            var serializer = new XmlSerializer(obj.GetType());

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, obj);
                foreach (var iteration in Benchmark.Iterations)
                {
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            stream.Position = 0;
                            serializer.Deserialize(stream);
                        }
                    }
                }
            }
        }

        public static byte[] CreateByteArray(int size)
        {
            byte[] obj = new byte[size];
            for (int i = 0; i < obj.Length; ++i)
            {
                obj[i] = (byte)(i % 256);
            }
            return obj;
        }

        [Benchmark]
        public void DeserializeByteArrayOf1K()
        {
            byte[] byteArrayOf1K = CreateByteArray(1024);
            RunXmlDeserializationTest(byteArrayOf1K, 10000);
        }

        [Benchmark]
        public void DeserializeByteArrayOf1M()
        {
            byte[] byteArrayOf1M = CreateByteArray(1024 * 1024);
            RunXmlDeserializationTest(byteArrayOf1M, 10);
        }

        [Benchmark]
        public void DeserializeStringOf128Byte()
        {
            string stringOf128Bytes = new string('a', 128);
            RunXmlDeserializationTest(stringOf128Bytes, 10000);
        }

        [Benchmark]
        public void DeserializeStringOf1024Byte()
        {
            string stringOf1024Bytes = new string('k', 1024);
            RunXmlDeserializationTest(stringOf1024Bytes, 10000);
        }

        private static List<int> CreateListOfInt(int count)
        {
            return Enumerable.Range(0, count).ToList();
        }

        [Benchmark]
        public void DeserializeListOfInt128()
        {
            List<int> listOfInt = CreateListOfInt(128);
            RunXmlDeserializationTest(listOfInt, 1000);
        }

        [Benchmark]
        public void DeserializeListOfInt1K()
        {
            List<int> listOfInt = CreateListOfInt(1024);
            RunXmlDeserializationTest(listOfInt, 1000);
        }

        [Benchmark]
        public void DeserializeListOfInt1M()
        {
            List<int> listOfInt = CreateListOfInt(1024 * 1024);
            RunXmlDeserializationTest(listOfInt, 1);
        }

        [Benchmark]
        public void DeserializeSimpleType()
        {
            SimpleType simpleType = new SimpleType() { P1 = "Foo", P2 = 123 };
            RunXmlDeserializationTest(simpleType, 10000);
        }

        [Benchmark]
        public void DeserializeIXmlSerializable()
        {
            var value = new ClassImplementingIXmlSerialiable() { StringValue = "Hello world" };
            RunXmlDeserializationTest(value, 10000);
        }

        [Benchmark]
        public void DeserializeXmlElement()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"<html></html>");
            XmlElement xmlElement = xmlDoc.CreateElement("Element");
            xmlElement.InnerText = "Element innertext";
            RunXmlDeserializationTest(xmlElement, 10000);
        }

    }
}
