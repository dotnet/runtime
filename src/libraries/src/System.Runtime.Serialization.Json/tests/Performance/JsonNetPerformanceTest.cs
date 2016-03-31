// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xunit.Performance;
using Newtonsoft.Json;
using Xunit;

namespace System.Runtime.Serialization.Json.Tests.Performance
{
    #region Json.NET performance tests

    public class JsonNetPerformanceTest
    {
        public static IEnumerable<object[]> SerializeMemberDataJsonNet()
        {
            return PerformanceTestCommon.PerformanceMemberData();
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberDataJsonNet))]
        public void JsonNetSerializationTest(int numberOfRuns, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunSerializationPerformanceTest(numberOfRuns, testType, testSize, JsonNetSerializerFactory.GetInstance());
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberDataJsonNet))]
        public void JsonNetDeSerializationTest(int numberOfRuns, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunDeserializationPerformanceTest(numberOfRuns, testType, testSize, JsonNetSerializerFactory.GetInstance());
        }
    }

    #endregion

    #region Json.Net serializer wrapper

    internal class JsonNetSerializerFactory : ISerializerFactory
    {
        private static readonly JsonNetSerializerFactory s_instance = new JsonNetSerializerFactory();
        public static JsonNetSerializerFactory GetInstance()
        {
            return s_instance;
        }

        public IPerfTestSerializer GetSerializer()
        {
            return new JsonNetSerializer();
        }
    }

    internal class JsonNetSerializer : IPerfTestSerializer
    {
        private JsonSerializer _serializer;

        public void Deserialize(Stream stream)
        {
            Debug.Assert(_serializer != null);
            Debug.Assert(stream != null);
            JsonReader reader = new JsonTextReader(new StreamReader(stream));
            _serializer.Deserialize(reader);
        }

        public void Init(object obj)
        {
            _serializer = new JsonSerializer();
        }

        public void Serialize(object obj, Stream stream)
        {
            Debug.Assert(_serializer != null);
            Debug.Assert(stream != null);
            var writer = new StreamWriter(stream);
            _serializer.Serialize(writer, obj);
            writer.Flush();
        }
    }

    #endregion
}
