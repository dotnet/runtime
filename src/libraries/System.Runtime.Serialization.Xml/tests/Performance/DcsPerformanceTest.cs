using System.Collections.Generic;
using System.IO;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Runtime.Serialization.Xml.Tests.Performance
{
    #region DCS performance tests

    public class DcsPerformanceTest
    {
        public static IEnumerable<object[]> SerializeMemberData()
        {
            return PerformanceTestCommon.PerformanceMemberData();
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberData))]
        public void DcsSerializationTest(int iterations, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunSerializationPerformanceTest(iterations, testType, testSize, DcsSerializerFactory.GetInstance());
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberData))]
        public void DcsDeSerializationTest(int iterations, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunDeSerializationPerformanceTest(iterations, testType, testSize, DcsSerializerFactory.GetInstance());
        }
    }

    #endregion

    #region DCS serializer wrapper

    internal class DcsSerializerFactory : SerializerFactory
    {
        private static DcsSerializerFactory _instance = null;
        public static DcsSerializerFactory GetInstance()
        {
            if (_instance == null)
            {
                _instance = new DcsSerializerFactory();
            }
            return _instance;
        }

        public override IPerfTestSerializer GetSerializer()
        {
            return new DcsSerializer();
        }
    }

    internal class DcsSerializer : IPerfTestSerializer
    {
        private DataContractSerializer _serializer;

        public void Deserialize(Stream stream)
        {
            // Assumption: Deserialize() is always called after Init()
            // Assumption: stream != null
            stream.Position = 0;
            _serializer.ReadObject(stream);
        }

        public void Init(object obj)
        {
            _serializer = new DataContractSerializer(obj.GetType());
        }

        public void Serialize(object obj, Stream stream)
        {
            // Assumption: Serialize() is always called after Init()
            // Assumption: stream != null and stream position will be reset to 0 after this method
            _serializer.WriteObject(stream, obj);
            stream.Position = 0;
        }
    }

    #endregion
}
