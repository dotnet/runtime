// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Runtime.Serialization.Json.Tests.Performance
{
    #region DCJS performance tests

    public class DcjsPerformanceTest
    {
        public static IEnumerable<object[]> SerializeMemberData()
        {
            return PerformanceTestCommon.PerformanceMemberData();
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberData))]
        public void DcjsSerializationTest(int iterations, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunSerializationPerformanceTest(iterations, testType, testSize, DcjsSerializerFactory.GetInstance());
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberData))]
        public void DcjsDeSerializationTest(int iterations, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunDeSerializationPerformanceTest(iterations, testType, testSize, DcjsSerializerFactory.GetInstance());
        }
    }

    #endregion

    #region DCJS serializer wrapper

    internal class DcjsSerializerFactory : SerializerFactory
    {
        private static DcjsSerializerFactory _instance = null;
        public static DcjsSerializerFactory GetInstance()
        {
            if (_instance == null)
            {
                _instance = new DcjsSerializerFactory();
            }
            return _instance;
        }

        public override IPerfTestSerializer GetSerializer()
        {
            return new DcjsSerializer();
        }
    }

    internal class DcjsSerializer : IPerfTestSerializer
    {
        private DataContractJsonSerializer _serializer;

        public void Deserialize(Stream stream)
        {
            // Assumption: Deserialize() is always called after Init()
            // Assumption: stream != null
            stream.Position = 0;
            _serializer.ReadObject(stream);
        }

        public void Init(object obj)
        {
            _serializer = new DataContractJsonSerializer(obj.GetType());
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
