// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xunit.Performance;
using System.Xml;
using SerializationTypes;

namespace System.Runtime.Serialization
{
    #region Performance tests configuration types

    public enum TestType
    {
        SimpleType,
        ISerializable,
        Dictionary,
        ListOfInt,
        String,
        ByteArray,
        XmlElement,
        DateTimeArray,
        ArrayOfSimpleType,
        ListOfSimpleType,
        DictionaryOfSimpleType,
        SimpleStructWithProperties,
        SimpleTypeWithFields
    }

    public interface IPerfTestSerializer
    {
        void Init(object obj);
        void Serialize(object obj, Stream s);
        void Deserialize(Stream s);
    }

    public interface ISerializerFactory
    {
        IPerfTestSerializer GetSerializer();
    }

    public class PerfTestConfig
    {
        public readonly int NumberOfRuns;
        public readonly TestType PerfTestType;
        public readonly int TestSize;

        public PerfTestConfig() { }

        public PerfTestConfig(int numOfRuns, TestType testType, int testSize)
        {
            NumberOfRuns = numOfRuns;
            PerfTestType = testType;
            TestSize = testSize;
        }

        public object[] ToObjectArray()
        {
            return new object[] { NumberOfRuns, PerfTestType, TestSize };
        }
    }

    #endregion

    public class PerformanceTestCommon
    {
        #region Performance test configuration

        public static IEnumerable<PerfTestConfig> PerformanceTestConfigurations()
        {
            yield return new PerfTestConfig(100, TestType.ByteArray, 1024);
            yield return new PerfTestConfig(5, TestType.ByteArray, 1024 * 1024);
            yield return new PerfTestConfig(10000, TestType.String, 128);
            yield return new PerfTestConfig(10000, TestType.String, 1024);
            yield return new PerfTestConfig(1000, TestType.ListOfInt, 128);
            yield return new PerfTestConfig(100, TestType.ListOfInt, 1024);
            yield return new PerfTestConfig(1, TestType.ListOfInt, 1024 * 1024);
            yield return new PerfTestConfig(1000, TestType.Dictionary, 128);
            yield return new PerfTestConfig(100, TestType.Dictionary, 1024);
            yield return new PerfTestConfig(10, TestType.SimpleType, 1);
            yield return new PerfTestConfig(1, TestType.SimpleType, 15);
            yield return new PerfTestConfig(1, TestType.SimpleTypeWithFields, 15);
            yield return new PerfTestConfig(10000, TestType.SimpleStructWithProperties, 1);
            yield return new PerfTestConfig(10000, TestType.ISerializable, -1);
            yield return new PerfTestConfig(10000, TestType.XmlElement, -1);
            yield return new PerfTestConfig(100, TestType.DateTimeArray, 1024);
            yield return new PerfTestConfig(1000, TestType.ArrayOfSimpleType, 128);
            yield return new PerfTestConfig(100, TestType.ArrayOfSimpleType, 1024);
            yield return new PerfTestConfig(1000, TestType.ListOfSimpleType, 128);
            yield return new PerfTestConfig(100, TestType.ListOfSimpleType, 1024);
            yield return new PerfTestConfig(1000, TestType.DictionaryOfSimpleType, 128);
            yield return new PerfTestConfig(100, TestType.DictionaryOfSimpleType, 1024);
        }

        public static IEnumerable<object[]> PerformanceMemberData()
        {
            foreach (PerfTestConfig config in PerformanceTestConfigurations())
            {
                yield return config.ToObjectArray();
            }
        }

        #endregion

        #region Methods to create object for serialization tests

        public static SimpleTypeWihtMoreProperties CreateSimpleTypeWihtMoreProperties(int height, int parentId, int currentId, int collectionSize, int childListSize)
        {
            int index = parentId * childListSize + (currentId + 1);
            var obj = new SimpleTypeWihtMoreProperties()
            {
                IntProperty = index,
                StringProperty = index + " string value",
                EnumProperty = (MyEnum)(index % (Enum.GetNames(typeof(MyEnum)).Length)),
                CollectionProperty = new List<string>(collectionSize),
                SimpleTypeList = new List<SimpleTypeWihtMoreProperties>(childListSize)
            };
            for (int i = 0; i < collectionSize; ++i)
            {
                obj.CollectionProperty.Add(index + "." + i);
            }
            if (height > 1)
            {
                for (int i = 0; i < childListSize; ++i)
                {
                    obj.SimpleTypeList.Add(CreateSimpleTypeWihtMoreProperties(height - 1, index, i, collectionSize, childListSize));
                }
            }
            return obj;
        }

        public static SimpleTypeWithMoreFields CreateSimpleTypeWithFields(int height, int parentId, int currentId, int collectionSize, int childListSize)
        {
            int index = parentId * childListSize + (currentId + 1);
            var obj = new SimpleTypeWithMoreFields()
            {
                IntField = index,
                StringField = index + " string value",
                EnumField = (MyEnum)(index % (Enum.GetNames(typeof(MyEnum)).Length)),
                CollectionField = new List<string>(collectionSize),
                SimpleTypeList = new List<SimpleTypeWithMoreFields>(childListSize)
            };
            for (int i = 0; i < collectionSize; ++i)
            {
                obj.CollectionField.Add(index + "." + i);
            }
            if (height > 1)
            {
                for (int i = 0; i < childListSize; ++i)
                {
                    obj.SimpleTypeList.Add(CreateSimpleTypeWithFields(height - 1, index, i, collectionSize, childListSize));
                }
            }
            return obj;
        }

        public static byte[] CreateByteArray(int size)
        {
            byte[] obj = new byte[size];
            for (int i = 0; i < obj.Length; ++i)
            {
                unchecked
                {
                    obj[i] = (byte)i;
                }
            }
            return obj;
        }

        public static Dictionary<int, string> CreateDictionaryOfIntString(int count)
        {
            Dictionary<int, string> dictOfIntString = new Dictionary<int, string>(count);
            for (int i = 0; i < count; ++i)
            {
                dictOfIntString[i] = i.ToString();
            }

            return dictOfIntString;
        }

        public static Dictionary<int, SimpleType> CreateDictionaryOfIntSimpleType(int count)
        {
            var dictOfIntSimpleType = new Dictionary<int, SimpleType>(count);
            for (int i = 0; i < count; ++i)
            {
                dictOfIntSimpleType[i] = new SimpleType() { P1 = i.ToString(), P2 = i };
            }

            return dictOfIntSimpleType;
        }

        public static List<SimpleType> CreateListOfSimpleType(int count)
        {
            var listOfSimpleType = new List<SimpleType>(count);
            for(int i = 0; i < count; i++)
            {
                listOfSimpleType.Add(new SimpleType() { P1 = i.ToString(), P2 = i });
            }

            return listOfSimpleType;
        }

        public static SimpleType[] CreateArrayOfSimpleType(int count)
        {
            var arrayOfSimpleType = new SimpleType[count];
            for(int i = 0; i < count; i++)
            {
                arrayOfSimpleType[i] = new SimpleType() { P1 = i.ToString(), P2 = i };
            }

            return arrayOfSimpleType;
        }

        public static List<int> CreateListOfInt(int count)
        {
            return Enumerable.Range(0, count).ToList();
        }

        public static DateTime[] CreateDateTimeArray(int count)
        {
            DateTime[] arr = new DateTime[count];
            int kind = (int)DateTimeKind.Unspecified;
            int maxDateTimeKind = (int) DateTimeKind.Local;
            DateTime val = DateTime.Now.AddHours(count/2);
            for (int i = 0; i < count; i++)
            {
                arr[i] = DateTime.SpecifyKind(val, (DateTimeKind)kind);
                val = val.AddHours(1);
                kind = (kind + 1)%maxDateTimeKind;
            }

            return arr;
        }

        public static object GetSerializationObject(TestType testType, int testSize)
        {
            Object obj = null;
            switch (testType)
            {
                case TestType.ByteArray:
                    obj = CreateByteArray(testSize);
                    break;
                case TestType.Dictionary:
                    obj = CreateDictionaryOfIntString(testSize);
                    break;
                case TestType.ISerializable:
                    obj = new ClassImplementingIXmlSerialiable() { StringValue = "Hello world" };
                    break;
                case TestType.ListOfInt:
                    obj = CreateListOfInt(testSize);
                    break;
                case TestType.SimpleType:
                    obj = CreateSimpleTypeWihtMoreProperties(testSize, 0, -1, 7, 2);
                    break;
                case TestType.SimpleTypeWithFields:
                     obj = CreateSimpleTypeWithFields(testSize, 0, -1, 7, 2);
                    break;
                case TestType.String:
                    obj = new string('k', testSize);
                    break;
                case TestType.XmlElement:
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(@"<html></html>");
                    XmlElement xmlElement = xmlDoc.CreateElement("Element");
                    xmlElement.InnerText = "Element innertext";
                    obj = xmlElement;
                    break;
                case TestType.DateTimeArray:
                    obj = CreateDateTimeArray(testSize);
                    break;
                case TestType.ArrayOfSimpleType:
                    obj = CreateArrayOfSimpleType(testSize);
                    break;
                case TestType.ListOfSimpleType:
                    obj = CreateListOfSimpleType(testSize);
                    break;
                case TestType.DictionaryOfSimpleType:
                    obj = CreateDictionaryOfIntSimpleType(testSize);
                    break;
                case TestType.SimpleStructWithProperties:
                    obj = new SimpleStructWithProperties() { Num = 1, Text = "Foo" };
                    break;
                default:
                    throw new ArgumentException();
            }
            return obj;
        }

        #endregion

        #region Methods to run serialization performance tests

        public static void RunSerializationPerformanceTest(int numberOfRuns, TestType testType, int testSize, ISerializerFactory serializerFactory)
        {
            var obj = GetSerializationObject(testType, testSize);

            var serializer = serializerFactory.GetSerializer();
            serializer.Init(obj);

            using (var stream = new MemoryStream())
            {
                foreach (var iteration in Benchmark.Iterations)
                {
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < numberOfRuns; i++)
                        {
                            serializer.Serialize(obj, stream);
                            stream.Position = 0;
                        }
                    }
                }
            }
        }

        public static void RunDeserializationPerformanceTest(int numberOfRuns, TestType testType, int testSize, ISerializerFactory serializerFactory)
        {
            var obj = GetSerializationObject(testType, testSize);

            var serializer = serializerFactory.GetSerializer();
            serializer.Init(obj);

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(obj, stream);
                stream.Position = 0;
                foreach (var iteration in Benchmark.Iterations)
                {
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < numberOfRuns; i++)
                        {
                            serializer.Deserialize(stream);
                            stream.Position = 0;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
