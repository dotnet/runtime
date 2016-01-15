// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;
using SerializationTypes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Xml;

namespace System.Runtime.Tests.Performance
{
    public class DcjsDeserializationTests
    {
        public void RunDcjsDeserializationTest(object obj, int iterations)
        {
            var serializer = new DataContractJsonSerializer(obj.GetType());

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, obj);
                foreach (var iteration in Benchmark.Iterations)
                {
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            stream.Position = 0;
                            serializer.ReadObject(stream);
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
            RunDcjsDeserializationTest(byteArrayOf1K, 10000);
        }

        [Benchmark]
        public void DeserializeByteArrayOf1M()
        {
            byte[] byteArrayOf1M = CreateByteArray(1024 * 1024);
            RunDcjsDeserializationTest(byteArrayOf1M, 10);
        }

        [Benchmark]
        public void DeserializeStringOf128Byte()
        {
            string stringOf128Bytes = new string('a', 128);
            RunDcjsDeserializationTest(stringOf128Bytes, 10000);
        }

        [Benchmark]
        public void DeserializeStringOf1024Byte()
        {
            string stringOf1024Bytes = new string('k', 1024);
            RunDcjsDeserializationTest(stringOf1024Bytes, 10000);
        }

        private static List<int> CreateListOfInt(int count)
        {
            return Enumerable.Range(0, count).ToList();
        }

        [Benchmark]
        public void DeserializeListOfInt128()
        {
            List<int> listOfInt = CreateListOfInt(128);
            RunDcjsDeserializationTest(listOfInt, 1000);
        }

        [Benchmark]
        public void DeserializeListOfInt1K()
        {
            List<int> listOfInt = CreateListOfInt(1024);
            RunDcjsDeserializationTest(listOfInt, 1000);
        }

        [Benchmark]
        public void DeserializeListOfInt1M()
        {
            List<int> listOfInt = CreateListOfInt(1024 * 1024);
            RunDcjsDeserializationTest(listOfInt, 1);
        }

        private static Dictionary<int, string> CreateDictionaryOfIntString(int count)
        {
            Dictionary<int, string> dictOfIntString = new Dictionary<int, string>();
            for (int i = 0; i < count; ++i)
            {
                dictOfIntString[i] = i.ToString();
            }

            return dictOfIntString;
        }

        [Benchmark]
        public void DeserilaizeDictionary128()
        {
            Dictionary<int, string> dictOfIntString = CreateDictionaryOfIntString(128);
            RunDcjsDeserializationTest(dictOfIntString, 1000);
        }

        [Benchmark]
        public void DeserilaizeDictionary1024()
        {
            Dictionary<int, string> dictOfIntString = CreateDictionaryOfIntString(1024);
            RunDcjsDeserializationTest(dictOfIntString, 100);
        }

        [Benchmark]
        public void DeserializeSimpleType()
        {
            SimpleType simpleType = new SimpleType() { P1 = "Foo", P2 = 123 };
            RunDcjsDeserializationTest(simpleType, 10000);
        }

        [Benchmark]
        public void DeserializeIXmlSerializable()
        {
            var value = new ClassImplementingIXmlSerialiable() { StringValue = "Hello world" };
            RunDcjsDeserializationTest(value, 10000);
        }
    }
}
