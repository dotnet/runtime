// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
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
        public void DcjsSerializationTest(int numberOfRuns, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunSerializationPerformanceTest(numberOfRuns, testType, testSize, DcjsSerializerFactory.GetInstance());
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberData))]
        public void DcjsDeSerializationTest(int numberOfRuns, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunDeserializationPerformanceTest(numberOfRuns, testType, testSize, DcjsSerializerFactory.GetInstance());
        }
    }

    #endregion

    #region DCJS serializer wrapper

    internal class DcjsSerializerFactory : ISerializerFactory
    {
        private static readonly DcjsSerializerFactory s_instance = new DcjsSerializerFactory();
        public static DcjsSerializerFactory GetInstance()
        {
            return s_instance;
        }

        public IPerfTestSerializer GetSerializer()
        {
            return new DcjsSerializer();
        }
    }

    internal class DcjsSerializer : IPerfTestSerializer
    {
        private DataContractJsonSerializer _serializer;

        public void Deserialize(Stream stream)
        {
            Debug.Assert(_serializer != null);
            Debug.Assert(stream != null);
            _serializer.ReadObject(stream);
        }

        public void Init(object obj)
        {
            _serializer = new DataContractJsonSerializer(obj.GetType());
        }

        public void Serialize(object obj, Stream stream)
        {
            Debug.Assert(_serializer != null);
            Debug.Assert(stream != null);
            _serializer.WriteObject(stream, obj);
        }
    }

    #endregion
}
