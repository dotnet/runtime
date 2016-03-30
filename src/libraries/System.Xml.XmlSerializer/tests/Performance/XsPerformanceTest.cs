using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Xml.XmlSerializer.Tests.Performance
{
    #region XmlSerializer performance tests

    public class XsPerformanceTest
    {
        public static IEnumerable<object[]> SerializeMemberData()
        {
            return PerformanceTestCommon.PerformanceMemberData();
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberData))]
        public void XsSerializationTest(int iterations, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunSerializationPerformanceTest(iterations, testType, testSize, XsSerializerFactory.GetInstance());
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberData))]
        public void XsDeSerializationTest(int iterations, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunDeSerializationPerformanceTest(iterations, testType, testSize, XsSerializerFactory.GetInstance());
        }
    }

    #endregion

    #region XmlSerializer wrapper

    internal class XsSerializerFactory : SerializerFactory
    {
        private static XsSerializerFactory _instance = null;
        public static XsSerializerFactory GetInstance()
        {
            if (_instance == null)
            {
                _instance = new XsSerializerFactory();
            }
            return _instance;
        }

        public override IPerfTestSerializer GetSerializer()
        {
            return new XsSerializer();
        }
    }

    internal class XsSerializer : IPerfTestSerializer
    {
        private Serialization.XmlSerializer _serializer;

        public void Deserialize(Stream stream)
        {
            // Assumption: Deserialize() is always called after Init()
            // Assumption: stream != null
            stream.Position = 0;
            _serializer.Deserialize(stream);
        }

        public void Init(object obj)
        {
            _serializer = new Serialization.XmlSerializer(obj.GetType());
        }

        public void Serialize(object obj, Stream stream)
        {
            // Assumption: Serialize() is always called after Init()
            // Assumption: stream != null and stream position will be reset to 0 after this method
            _serializer.Serialize(stream, obj);
            stream.Position = 0;
        }
    }

    #endregion
}
