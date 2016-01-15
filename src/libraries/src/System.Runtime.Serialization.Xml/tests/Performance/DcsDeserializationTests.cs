// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;
using SerializationTypes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;

namespace System.Runtime.Tests.Performance
{
    public class DcsDeserializationTests
    {
        public void RunDcsDeserializationTest(object obj, int iterations)
        {
            var serializer = new DataContractSerializer(obj.GetType());

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
            RunDcsDeserializationTest(byteArrayOf1K, 10000);
        }

        [Benchmark]
        public void DeserializeByteArrayOf1M()
        {
            byte[] byteArrayOf1M = CreateByteArray(1024 * 1024);
            RunDcsDeserializationTest(byteArrayOf1M, 10);
        }

        [Benchmark]
        public void DeserializeStringOf128Byte()
        {
            string stringOf128Bytes = new string('a', 128);
            RunDcsDeserializationTest(stringOf128Bytes, 10000);
        }

        [Benchmark]
        public void DeserializeStringOf1024Byte()
        {
            string stringOf1024Bytes = new string('k', 1024);
            RunDcsDeserializationTest(stringOf1024Bytes, 10000);
        }

        private static List<int> CreateListOfInt(int count)
        {
            return Enumerable.Range(0, count).ToList();
        }

        [Benchmark]
        public void DeserializeListOfInt128()
        {
            List<int> listOfInt = CreateListOfInt(128);
            RunDcsDeserializationTest(listOfInt, 1000);
        }

        [Benchmark]
        public void DeserializeListOfInt1K()
        {
            List<int> listOfInt = CreateListOfInt(1024);
            RunDcsDeserializationTest(listOfInt, 1000);
        }

        [Benchmark]
        public void DeserializeListOfInt1M()
        {
            List<int> listOfInt = CreateListOfInt(1024 * 1024);
            RunDcsDeserializationTest(listOfInt, 1);
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
            RunDcsDeserializationTest(dictOfIntString, 1000);
        }

        [Benchmark]
        public void DeserilaizeDictionary1024()
        {
            Dictionary<int, string> dictOfIntString = CreateDictionaryOfIntString(1024);
            RunDcsDeserializationTest(dictOfIntString, 100);
        }

        [Benchmark]
        public void DeserializeSimpleType()
        {
            SimpleType simpleType = new SimpleType() { P1 = "Foo", P2 = 123 };
            RunDcsDeserializationTest(simpleType, 10000);
        }

        [Benchmark]
        public void DeserializeIXmlSerializable()
        {
            var value = new ClassImplementingIXmlSerialiable() { StringValue = "Hello world" };
            RunDcsDeserializationTest(value, 10000);
        }
    }
}
