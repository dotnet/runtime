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
    public class DcsSerializationTests
    {
        public void RunDcsSerializationTest(object obj, int iterations)
        {
            var serializer = new DataContractSerializer(obj.GetType());

            using (var stream = new MemoryStream())
            {
                foreach (var iteration in Benchmark.Iterations)
                {
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            serializer.WriteObject(stream, obj);
                            stream.Position = 0;
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
        public void SerializeByteArrayOf1K()
        {
            byte[] byteArrayOf1K = CreateByteArray(1024);
            RunDcsSerializationTest(byteArrayOf1K, 10000);
        }

        [Benchmark]
        public void SerializeByteArrayOf1M()
        {
            byte[] byteArrayOf1M = CreateByteArray(1024 * 1024);
            RunDcsSerializationTest(byteArrayOf1M, 100);
        }

        [Benchmark]
        public void SerializeStringOf128Byte()
        {
            string stringOf128Bytes = new string('a', 128);
            RunDcsSerializationTest(stringOf128Bytes, 10000);
        }

        [Benchmark]
        public void SerializeStringOf1024Byte()
        {
            string stringOf1024Bytes = new string('k', 1024);
            RunDcsSerializationTest(stringOf1024Bytes, 10000);
        }

        private static List<int> CreateListOfInt(int count)
        {
            return Enumerable.Range(0, count).ToList();
        }

        [Benchmark]
        public void SerializeListOfInt128()
        {
            List<int> listOfInt = CreateListOfInt(128);
            RunDcsSerializationTest(listOfInt, 1000);
        }

        [Benchmark]
        public void SerializeListOfInt1K()
        {
            List<int> listOfInt = CreateListOfInt(1024);
            RunDcsSerializationTest(listOfInt, 1000);
        }

        [Benchmark]
        public void SerializeListOfInt1M()
        {
            List<int> listOfInt = CreateListOfInt(1024 * 1024);
            RunDcsSerializationTest(listOfInt, 1);
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
        public void SerilaizeDictionary128()
        {
            Dictionary<int, string> dictOfIntString = CreateDictionaryOfIntString(128);
            RunDcsSerializationTest(dictOfIntString, 1000);
        }

        [Benchmark]
        public void SerilaizeDictionary1024()
        {
            Dictionary<int, string> dictOfIntString = CreateDictionaryOfIntString(1024);
            RunDcsSerializationTest(dictOfIntString, 100);
        }

        [Benchmark]
        public void SerializeSimpleType()
        {
            SimpleType simpleType = new SimpleType() { P1 = "Foo", P2 = 123 };
            RunDcsSerializationTest(simpleType, 10000);
        }

        [Benchmark]
        public void SerializeIXmlSerializable()
        {
            var value = new ClassImplementingIXmlSerialiable() { StringValue = "Hello world" };
            RunDcsSerializationTest(value, 10000);
        }
    }
}
