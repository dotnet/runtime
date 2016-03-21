using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xunit.Performance;
using System.Xml;
using SerializationTypes;

namespace System.Runtime.Serialization
{
    #region Performance tests configuration types

    public enum SerializerType
    {
        DataContractSerializer,
        DataContractJsonSerializer,
        XmlSerializer,
    }

    public enum TestType
    {
        SimpleType,
        ISerializable,
        Dictionary,
        ListOfInt,
        String,
        ByteArray,
        XmlElement
    }

    public enum TestSize
    {
        NotSpecified,
        Size128,
        Size1024,
        Size1M
    }

    public interface IPerfTestSerializer
    {
        void Init(object obj);
        void Serialize(object obj, Stream s);
        void Deserialize(Stream s);
        void Deserialize(string s);
    }

    public abstract class SerializerFactory
    {
        public abstract IPerfTestSerializer GetSerializer(SerializerType serializerType);
    }

    #endregion

    public class PerformanceTestCommon
    {

        #region Performance test configuration

        public static IEnumerable<object[]> PerformanceMemberData(SerializerType serializerType)
        {
            //yield return new object[] { 10000, TestType.ByteArray, TestSize.Size1024, serializerType };
            //yield return new object[] { 100, TestType.ByteArray, TestSize.Size1M, serializerType };
            //yield return new object[] { 10000, TestType.String, TestSize.Size128, serializerType };
            //yield return new object[] { 10000, TestType.String, TestSize.Size1024, serializerType };
            //yield return new object[] { 1000, TestType.ListOfInt, TestSize.Size128, serializerType };
            //yield return new object[] { 1000, TestType.ListOfInt, TestSize.Size1024, serializerType };
            //yield return new object[] { 1, TestType.ListOfInt, TestSize.Size1M, serializerType };
            //yield return new object[] { 1000, TestType.Dictionary, TestSize.Size128, serializerType };
            //yield return new object[] { 100, TestType.Dictionary, TestSize.Size1024, serializerType };
            //yield return new object[] { 10000, TestType.SimpleType, TestSize.NotSpecified, serializerType };
            //yield return new object[] { 10000, TestType.ISerializable, TestSize.NotSpecified, serializerType };
            yield return new object[] { 10000, TestType.XmlElement, TestSize.NotSpecified, serializerType };
        }

        #endregion

        #region Methods to create object for serialization tests

        public static byte[] CreateByteArray(int size)
        {
            byte[] obj = new byte[size];
            for (int i = 0; i < obj.Length; ++i)
            {
                obj[i] = (byte)(i % 256);
            }
            return obj;
        }

        public static Dictionary<int, string> CreateDictionaryOfIntString(int count)
        {
            Dictionary<int, string> dictOfIntString = new Dictionary<int, string>();
            for (int i = 0; i < count; ++i)
            {
                dictOfIntString[i] = i.ToString();
            }

            return dictOfIntString;
        }

        public static List<int> CreateListOfInt(int count)
        {
            return Enumerable.Range(0, count).ToList();
        }

        public static object GetSerializationObject(TestType testType, TestSize testSize)
        {
            int size = 0;
            switch (testSize)
            {
                case TestSize.Size128:
                    size = 128;
                    break;
                case TestSize.Size1024:
                    size = 1024;
                    break;
                case TestSize.Size1M:
                    size = 1024 * 1024; 
                    break;
            }
            Object obj = null;
            switch (testType)
            {
                case TestType.ByteArray:
                    obj = CreateByteArray(size);
                    break;
                case TestType.Dictionary:
                    obj = CreateDictionaryOfIntString(size);
                    break;
                case TestType.ISerializable:
                    obj = new ClassImplementingIXmlSerialiable() { StringValue = "Hello world" };
                    break;
                case TestType.ListOfInt:
                    obj = CreateListOfInt(size);
                    break;
                case TestType.SimpleType:
                    obj = new SimpleType() { P1 = "Foo", P2 = 123 };
                    break;
                case TestType.String:
                    obj = new string('k', size);
                    break;
                case TestType.XmlElement:
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(@"<html></html>");
                    XmlElement xmlElement = xmlDoc.CreateElement("Element");
                    xmlElement.InnerText = "Element innertext";
                    obj = xmlElement;
                    break;
            }
            return obj;
        }

        #endregion

        #region Methods to run serialization performance tests

        public static void RunSerializationPerformanceTest(int iterations, SerializerType serializerType, TestType testType, TestSize testSize, SerializerFactory serializerFactory)
        {
            var obj = GetSerializationObject(testType, testSize);

            var serializer = serializerFactory.GetSerializer(serializerType);
            serializer.Init(obj);

            using (var stream = new MemoryStream())
            {
                foreach (var iteration in Benchmark.Iterations)
                {
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            serializer.Serialize(obj, stream);
                        }
                    }
                }
            }
        }

        public static void RunDeSerializationPerformanceTest(int iterations, SerializerType serializerType, TestType testType, TestSize testSize, SerializerFactory serializerFactory)
        {
            var obj = GetSerializationObject(testType, testSize);

            var serializer = serializerFactory.GetSerializer(serializerType);
            serializer.Init(obj);

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(obj, stream);
                foreach (var iteration in Benchmark.Iterations)
                {
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            serializer.Deserialize(stream);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
