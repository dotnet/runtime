// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
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
        public void DcsSerializationTest(int numberOfRuns, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunSerializationPerformanceTest(numberOfRuns, testType, testSize, DcsSerializerFactory.GetInstance());
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberData))]
        public void DcsDeSerializationTest(int numberOfRuns, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunDeserializationPerformanceTest(numberOfRuns, testType, testSize, DcsSerializerFactory.GetInstance());
        }
    }

    #endregion

    #region DCS serializer wrapper

    internal class DcsSerializerFactory : ISerializerFactory
    {
        private static readonly DcsSerializerFactory s_instance = new DcsSerializerFactory();
        public static DcsSerializerFactory GetInstance()
        {
            return s_instance;
        }

        public IPerfTestSerializer GetSerializer()
        {
            return new DcsSerializer();
        }
    }

    internal class DcsSerializer : IPerfTestSerializer
    {
        private DataContractSerializer _serializer;

        public void Deserialize(Stream stream)
        {
            Debug.Assert(_serializer != null);
            Debug.Assert(stream != null);
            _serializer.ReadObject(stream);
        }

        public void Init(object obj)
        {
            _serializer = new DataContractSerializer(obj.GetType());
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
